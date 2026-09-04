using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.AI;

namespace WanderingCity.Tests
{
    public sealed class GameplayTests
    {
        Scene scene, previous; GameSession game; string folder;
        [UnitySetUp] public IEnumerator SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "wandering-play-" + Guid.NewGuid().ToString("N")); GameSession.SavePathOverride = Path.Combine(folder, "save.json");
            previous = SceneManager.GetActiveScene(); scene = SceneManager.CreateScene("TestAdventure"); SceneManager.SetActiveScene(scene);
            game = new GameObject("Game").AddComponent<GameSession>(); game.Begin(true); yield return null;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            SceneManager.SetActiveScene(previous); yield return SceneManager.UnloadSceneAsync(scene); GameSession.SavePathOverride = null; if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        void Teleport(Vector3 p) { game.State.x = p.x; game.State.y = p.y; game.State.z = p.z; game.Player.RestorePosition(); Physics.SyncTransforms(); }
        [UnityTest] public IEnumerator WorldContainsAllLandmarksAndPersistentObjects()
        {
            yield return null; Assert.AreEqual(10, game.World.Enemies.Count); Assert.AreEqual(71, game.World.Interactions.Count); Assert.IsTrue(game.World.Enemies.All(e => e.Agent.isOnNavMesh)); Assert.IsNotNull(Camera.main); Assert.IsNotNull(game.Hud);
            Assert.IsTrue(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing);
            Assert.GreaterOrEqual(Camera.main.farClipPlane,900);
            Assert.GreaterOrEqual(game.Player.transform.position.y,WorldBuilder.GroundY(0,0));
        }
        [UnityTest] public IEnumerator EveryRewardHasAReachableInteractionPosition()
        {
            yield return null; var missing = new List<string>();
            foreach (var item in game.World.Interactions)
            {
                bool found = false;
                for (int i = 0; i < 16; i++) { float angle = i * Mathf.PI / 8; Vector3 probe = item.transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2.6f; if (!NavMesh.SamplePosition(probe, out var nav, 2, NavMesh.AllAreas)) continue;
                    var path = new NavMeshPath(); if (NavMesh.CalculatePath(WorldBuilder.GroundPoint(0,0,.1f), nav.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete && Vector3.Distance(nav.position + Vector3.up, item.transform.position) < 3.6f && CombatVisibility.Clear(nav.position + Vector3.up, item.transform.position)) { found = true; break; } }
                if (!found)
                {
                    var diagnostics = new List<string>();
                    for(int n=0;n<16;n++)
                    {
                        var probe=item.transform.position+new Vector3(Mathf.Cos(n*Mathf.PI/8),0,Mathf.Sin(n*Mathf.PI/8))*2.6f;
                        if(!NavMesh.SamplePosition(probe,out var sample,2,NavMesh.AllAreas)){diagnostics.Add("no-nav");continue;}
                        var testPath=new NavMeshPath(); NavMesh.CalculatePath(WorldBuilder.GroundPoint(0,0,.1f),sample.position,NavMesh.AllAreas,testPath);
                        string blocker=Physics.Linecast(sample.position+Vector3.up,item.transform.position,out var hit,1<<0,QueryTriggerInteraction.Ignore)?hit.collider.name:"clear";
                        diagnostics.Add(testPath.status+"/"+blocker+"/"+Vector3.Distance(sample.position+Vector3.up,item.transform.position).ToString("F1"));
                    }
                    missing.Add(item.Id+" "+item.transform.position+": "+string.Join(",",diagnostics));
                }
            }
            Assert.IsEmpty(missing, "Unreachable interactions: " + string.Join(", ", missing));
        }
        [UnityTest] public IEnumerator DodgeInvulnerabilityExpiresBeforeRecovery()
        {
            Assert.IsTrue(game.Player.StartDodge(Vector3.forward)); yield return new WaitForSeconds(.32f); Assert.IsFalse(game.Player.Invulnerable); Assert.IsTrue(game.Player.Damage(18)); Assert.AreEqual(82, game.State.hp);
        }
        [UnityTest] public IEnumerator EnemyDiscoversChasesAndTelegraphsAttack()
        {
            var enemy = game.World.Enemies[0]; enemy.Home = WorldBuilder.GroundPoint(0,30); enemy.Agent.Warp(enemy.Home); Teleport(enemy.Home + Vector3.back * 5); yield return new WaitForSeconds(.4f); Assert.AreEqual(EnemyAction.Chase, enemy.Action);
            Teleport(enemy.transform.position + Vector3.back * 1.7f); yield return new WaitForSeconds(.3f); Assert.AreEqual(EnemyAction.Attack, enemy.Action); Assert.IsTrue(enemy.Telegraph.gameObject.activeSelf);
            yield return new WaitForSeconds(.9f); Assert.Less(game.State.hp, 100); Teleport(Vector3.up); yield return new WaitForSeconds(1.5f); Assert.IsTrue(enemy.Action == EnemyAction.Return || enemy.Action == EnemyAction.Patrol);
        }
        [UnityTest] public IEnumerator EnemyCannotDamagePlayerAtUnreachableHeight()
        {
            var enemy = game.World.Enemies[0]; Teleport(enemy.Home + Vector3.up * 8); game.Player.enabled = false; yield return new WaitForSeconds(2); Assert.AreEqual(100, game.State.hp); Assert.AreNotEqual(EnemyAction.Attack, enemy.Action);
        }
        [UnityTest] public IEnumerator AttackHitsOnlyOnceAndWallBlocksIt()
        {
            var enemy = game.World.Enemies[0]; enemy.Agent.enabled = false; enemy.transform.position = WorldBuilder.GroundPoint(0,22); Teleport(new Vector3(0, 0, 20)); yield return null;
            Assert.IsTrue(game.Player.StartAttack()); game.Player.Strike(); game.Player.Strike(); Assert.AreEqual(78, enemy.Hp);
            game.Player.RestorePosition(); var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = WorldBuilder.GroundPoint(0,21,1); wall.transform.localScale = new Vector3(4, 3, .25f); Physics.SyncTransforms();
            Assert.IsTrue(game.Player.StartAttack()); game.Player.Strike(); Assert.AreEqual(78, enemy.Hp); UnityEngine.Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator DodgeProtectsAndCannotRestartDuringAction()
        {
            yield return null; Assert.IsTrue(game.Player.StartDodge(Vector3.forward)); Assert.IsTrue(game.Player.Invulnerable); int hp = game.State.hp; Assert.IsFalse(game.Player.Damage(18)); Assert.AreEqual(hp, game.State.hp); Assert.IsFalse(game.Player.StartDodge(Vector3.right)); Assert.IsFalse(game.Player.StartAttack());
            game.Player.RestorePosition(); Assert.IsTrue(game.Player.Damage(18)); Assert.AreEqual(hp - 18, game.State.hp);
        }
        [UnityTest] public IEnumerator PauseBlocksCombatAndFreezesWorld()
        {
            game.SetMenu(true); Vector3 position = game.Player.transform.position; Assert.IsFalse(game.Player.StartAttack()); Assert.IsFalse(game.Player.StartDodge(Vector3.forward)); yield return null; Assert.AreEqual(position, game.Player.transform.position); Assert.AreEqual(0, Time.timeScale); Assert.AreEqual(0, GameObject.Find("Gameplay overlay").GetComponent<CanvasGroup>().alpha);
        }
        [UnityTest] public IEnumerator DeathRespawnPreservesInventoryAndRewards()
        {
            Rules.Transact(game.State, new Dictionary<string, int> { ["ore"] = 4 }); game.State.claimed.Add("chest-forest"); Assert.IsTrue(game.Player.Damage(100)); Assert.IsFalse(game.Player.StartAttack()); Assert.IsFalse(game.Player.Damage(10)); game.Respawn(); yield return null;
            Assert.AreEqual(100, game.State.hp); Assert.AreEqual(4, game.State.Count("ore")); Assert.Contains("chest-forest", game.State.claimed); Assert.Less(Vector3.Distance(game.Player.transform.position, WorldBuilder.GroundPoint(0,0,.1f)), 1);
        }
        [UnityTest] public IEnumerator InteractionUsesRangeAndCannotRepeatAfterReload()
        {
            var item = game.World.Interactions.Find(i => i.Id == "wood-0"); item.Interact(); Assert.AreEqual(0, game.State.Count("wood")); Teleport(item.transform.position - Vector3.up * .7f + Vector3.back); yield return null;
            item.Interact(); item.Interact(); Assert.AreEqual(5, game.State.Count("wood")); Assert.IsTrue(game.Saves.Load(out var loaded, out _)); Assert.Contains("wood-0", loaded.claimed); Assert.IsFalse(item.gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator CampDefeatPersistsRewardAndDrops()
        {
            foreach (var e in game.World.Enemies.Where(e => e.Id.StartsWith("enemy-camp-"))) { e.Damage(104); e.Damage(104); }
            yield return null; Assert.AreEqual(5, game.State.defeated.Count); Assert.AreEqual(5, game.World.Interactions.Count(i => i.Id.StartsWith("drop-"))); Assert.IsTrue(game.Saves.Load(out var loaded, out _)); Assert.AreEqual(5, loaded.defeated.Count);
            var reward = game.World.Interactions.Find(i => i.Id == "camp-reward"); Teleport(reward.transform.position + Vector3.right * 2); reward.Interact(); Assert.AreEqual(1, game.State.Count("core"));
        }
        [UnityTest] public IEnumerator BuildingsBlockPlayerAndRestoreAsColliders()
        {
            Teleport(new Vector3(-6, .1f, 0)); Assert.IsFalse(game.World.ClearForBuilding("floor", -2, 0, 0)); Teleport(Vector3.up); Rules.Transact(game.State, new Dictionary<string, int> { ["floor"] = 1, ["wall"] = 1 }); Assert.IsTrue(Rules.Place(game.State, "floor", -2, 0, 0, true)); Assert.IsTrue(Rules.Place(game.State, "wall", -2, 0, 0, true)); game.World.RebuildStructures(); yield return null; Physics.SyncTransforms();
            Assert.IsTrue(Physics.Linecast(WorldBuilder.GroundPoint(-6,0,1), WorldBuilder.GroundPoint(-6,3,1), 1 << 0)); Assert.IsTrue(game.Save()); Assert.IsTrue(game.Saves.Load(out var loaded, out _)); Assert.AreEqual(2, loaded.buildings.Count);
        }
        [UnityTest] public IEnumerator ControllerStopsAtWalls()
        {
            Teleport(new Vector3(0, .1f, 20)); var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = WorldBuilder.GroundPoint(0,22,1); wall.transform.localScale = new Vector3(10, 3, .3f); Physics.SyncTransforms();
            for (int i = 0; i < 30; i++) game.Player.Controller.Move(Vector3.forward * .2f + Vector3.down * .02f);
            Assert.Less(game.Player.transform.position.z, 21.6f); UnityEngine.Object.Destroy(wall); yield return null;
        }
        [UnityTest] public IEnumerator ReopeningSceneRestoresWorldAndPlayerProgress()
        {
            Rules.Claim(game.State, "wood-0", new Dictionary<string, int> { ["wood"] = 5 });
            Rules.Transact(game.State, new Dictionary<string, int> { ["floor"] = 2 }); Rules.Place(game.State, "floor", -2, 0, 0, true);
            game.World.Enemies.Find(e => e.Id == "enemy-wild-0").Damage(104);
            game.State.hp = 66; game.State.weaponLevel = 2; game.State.selectedSlot = 2; game.State.visited.Add("forest"); Assert.IsTrue(game.Save());
            SceneManager.SetActiveScene(previous); yield return SceneManager.UnloadSceneAsync(scene);
            scene = SceneManager.CreateScene("ReloadedAdventure"); SceneManager.SetActiveScene(scene); game = new GameObject("RestoredGame").AddComponent<GameSession>(); game.Begin(false); yield return null;
            Assert.AreEqual(66, game.State.hp); Assert.AreEqual(2, game.State.weaponLevel); Assert.AreEqual(2, game.State.selectedSlot); Assert.AreEqual(5, game.State.Count("wood")); Assert.AreEqual(1, game.State.Count("floor")); Assert.Contains("forest", game.State.visited);
            Assert.IsFalse(game.World.Interactions.Find(i => i.Id == "wood-0").gameObject.activeSelf); Assert.IsFalse(game.World.Enemies.Find(e => e.Id == "enemy-wild-0").gameObject.activeSelf);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<BuildingTag>(FindObjectsSortMode.None).Length);
            Assert.IsTrue(game.World.Interactions.Find(i => i.Id == "drop-enemy-wild-0").gameObject.activeSelf);
        }
    }
}
