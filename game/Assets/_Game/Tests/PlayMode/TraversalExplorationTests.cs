using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace WanderingCity.Tests
{
    public sealed class TraversalExplorationTests
    {
        Scene scene, previous; GameSession game; string folder;
        PlayerTraversal T => game.Player.Traversal;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Path.GetTempPath(), "traversal-" + Guid.NewGuid().ToString("N")); GameSession.SavePathOverride = Path.Combine(folder, "save.json");
            previous = SceneManager.GetActiveScene(); scene = SceneManager.CreateScene("TraversalTest"); SceneManager.SetActiveScene(scene);
            game = new GameObject("Game").AddComponent<GameSession>(); game.Begin(true); yield return null; game.Player.enabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; SceneManager.SetActiveScene(previous); yield return SceneManager.UnloadSceneAsync(scene); GameSession.SavePathOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        void Position(Vector3 p, float yaw = 0)
        {
            p.y += WorldBuilder.GroundY(p.x,p.z);
            game.State.x = p.x; game.State.y = p.y; game.State.z = p.z; game.State.yaw = yaw; game.Player.RestorePosition(); Physics.SyncTransforms();
        }
        void Step(Vector3 direction = default, Vector2 input = default, bool sprint = false, bool jump = false)
            => T.Simulate(.02f, direction, input, sprint, false, jump, false, false, false);
        GameObject Wall(float height = 5, bool climbable = true)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = WorldBuilder.GroundPoint(0,22,height/2); wall.transform.localScale = new Vector3(10, height, 1);
            if (climbable) wall.AddComponent<ClimbSurface>(); Physics.SyncTransforms(); return wall;
        }
        [UnityTest] public IEnumerator ClimbUsesOptInSurfaceAndExhaustionDetaches()
        {
            var wall = Wall(climbable: false); Position(new Vector3(0, .1f, 20.9f)); Assert.IsFalse(T.TryClimb());
            wall.AddComponent<ClimbSurface>(); Assert.IsTrue(T.TryClimb()); Assert.IsFalse(game.Player.StartAttack()); Assert.IsFalse(game.Player.StartDodge(Vector3.forward));
            float initial = game.Player.transform.position.y;
            for (int i = 0; i < 25; i++) Step(input: Vector2.up);
            Assert.Greater(game.Player.transform.position.y, initial + .5f); Assert.Less(T.Stamina.Current, T.Stamina.Max);
            T.Stamina.Tick(100, 100); Step(); Assert.AreEqual(TraversalState.Fall, T.State); Assert.IsFalse(T.TryClimb()); yield return null;
        }
        [UnityTest] public IEnumerator ClimbLedgeTransitionUsesCollisionAndReachesTop()
        {
            Wall(3); Position(new Vector3(0, .1f, 20.9f)); Assert.IsTrue(T.TryClimb()); bool ledge = false;
            for (int i = 0; i < 180; i++) { Step(input: Vector2.up); ledge |= T.State == TraversalState.LedgeTransition; }
            Assert.IsTrue(ledge); Assert.Greater(game.Player.transform.position.y, 2.9f); Assert.Greater(game.Player.transform.position.z, 21.7f); Assert.IsFalse(T.BlocksCombat); yield return null;
        }
        [UnityTest] public IEnumerator BlockedLedgeDoesNotTunnelThroughCeiling()
        {
            Wall(3); var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube); ceiling.transform.position = WorldBuilder.GroundPoint(0,22,4); ceiling.transform.localScale = new Vector3(5, .3f, 4); Physics.SyncTransforms();
            Position(new Vector3(0, .1f, 20.9f)); Assert.IsTrue(T.TryClimb());
            for (int i = 0; i < 180; i++) Step(input: Vector2.up);
            Assert.Less(game.Player.transform.position.y, WorldBuilder.GroundY(0,22)+2.5f); Assert.Less(game.Player.transform.position.z, 21.5f); yield return null;
        }
        [UnityTest] public IEnumerator GlideSteersDrainsAndEndsOnLanding()
        {
            Position(new Vector3(0, 8, 20)); Assert.IsTrue(T.TryGlide());
            for (int i = 0; i < 250; i++) Step(Vector3.right, Vector2.right);
            Assert.Greater(game.Player.transform.position.x, 5); Assert.Less(game.Player.transform.position.y, WorldBuilder.GroundY(game.Player.transform.position.x,game.Player.transform.position.z)+.3f); Assert.AreNotEqual(TraversalState.Glide, T.State); yield return null;
        }
        [UnityTest] public IEnumerator GlideDamageExhaustionAndPauseTransitionRules()
        {
            Position(new Vector3(0, 8, 20)); game.SetMenu(true); Assert.IsFalse(T.TryGlide()); game.SetMenu(false); Assert.IsTrue(T.TryGlide());
            Assert.IsFalse(game.Player.StartAttack()); Assert.IsTrue(game.Player.Damage(1)); Assert.AreEqual(TraversalState.Fall, T.State); Assert.IsFalse(T.TryClimb());
            Position(new Vector3(0, 8, 20)); Assert.IsTrue(T.TryGlide()); T.Stamina.Tick(100, 100); Step(); Assert.AreEqual(TraversalState.Fall, T.State); Assert.IsFalse(T.TryGlide()); yield return null;
        }
        [UnityTest] public IEnumerator GroundContactIsRecheckedAfterRestoringAnAirbornePosition()
        {
            Position(new Vector3(75, 14.1f, 8)); for (int i = 0; i < 20; i++) Step();
            Assert.IsFalse(T.TryGlide()); Assert.IsTrue(game.Player.Controller.isGrounded);
            Position(new Vector3(81, 14, 21)); Assert.IsTrue(T.TryGlide()); Step(Vector3.forward, Vector2.up);
            Assert.AreEqual(TraversalState.Glide, T.State); Assert.Less(T.Stamina.Current, T.Stamina.Max); yield return null;
        }
        [UnityTest] public IEnumerator CoyoteJumpAndBufferedLandingJumpUseRealContact()
        {
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube); platform.transform.position = WorldBuilder.GroundPoint(0,20,1.5f); platform.transform.localScale = new Vector3(2, 3, 2); Physics.SyncTransforms();
            Position(new Vector3(0, 3.1f, 20)); for (int i = 0; i < 15; i++) Step(); Assert.IsTrue(T.Grounded);
            for (int i = 0; i < 100 && T.Grounded; i++) Step(Vector3.right, Vector2.right);
            Assert.IsFalse(T.Grounded); Step(jump: true); Assert.Greater(T.VerticalVelocity, 0, "coyote jump after leaving platform");
            Position(new Vector3(5, .25f, 20)); Step(jump: true); bool jumped = false;
            for (int i = 0; i < 10; i++) { Step(); jumped |= T.VerticalVelocity > 0; }
            Assert.IsTrue(jumped, "buffered input fires on contact"); yield return null;
        }
        [UnityTest] public IEnumerator LowFrameRateMotionStillStopsAtWalls()
        {
            Wall(); Position(new Vector3(0, .1f, 20));
            T.Simulate(.25f, Vector3.forward, Vector2.up, true, false, false, false, false, false, Vector3.forward * 40);
            Assert.Less(game.Player.transform.position.z, 21.3f); yield return null;
        }
        [UnityTest] public IEnumerator MapZoomAndPanOperateWithinViewportBounds()
        {
            ExplorationRules.Activate(game.State, "base-beacon");
            game.SetMenu(true, "map"); yield return null;
            var map = UnityEngine.Object.FindFirstObjectByType<ExplorationMapInput>(); Assert.IsNotNull(map);
            var input = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = new Vector2(700, 500), scrollDelta = Vector2.up * 4 };
            map.OnScroll(input); Assert.Greater(map.Content.localScale.x, 1);
            input.delta = new Vector2(-10000, 10000); map.OnDrag(input);
            Vector2 size = ((RectTransform)map.transform).rect.size * (map.Content.localScale.x - 1);
            Assert.That(map.Content.anchoredPosition.x, Is.InRange(-size.x, 0)); Assert.That(map.Content.anchoredPosition.y, Is.InRange(0, size.y));
            map.Content.Find("base-beacon").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            bool detailsShown = false;
            foreach (var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
                detailsShown |= text.text.Contains("归途信标") && text.text.Contains("距离");
            Assert.IsTrue(detailsShown, "Selecting a discovered marker opens its details");
            map.Focus(new Vector2(312.5f, 312.5f), 1); Assert.AreEqual(Vector2.zero, map.Content.anchoredPosition);
            Assert.AreEqual(Vector3.one, map.Content.localScale);
            map.Focus(new Vector2(0, 0), 999); Assert.AreEqual(8, map.Content.localScale.x);
            Assert.AreEqual(Vector2.zero, map.Content.anchoredPosition);
            yield return null;
            var playerMarker = (RectTransform)map.Content.Find("You");
            Assert.AreEqual(Vector2.one * .5f, playerMarker.pivot);
            Assert.AreEqual(.125f, playerMarker.localScale.x, .001f);
        }
        [UnityTest] public IEnumerator MinimapUsesDiscoveryAndTracksLivePlayerAndQuest()
        {
            var minimap = game.Hud.Minimap;
            game.State.discoveredPOIIds.Remove("mesa-beacon"); minimap.Refresh();
            var marker = minimap.transform.Find("Circular clip/mesa-beacon");
            Assert.IsFalse(marker.gameObject.activeSelf);
            ExplorationRules.Activate(game.State, "mesa-beacon"); minimap.Refresh(); Assert.IsTrue(marker.gameObject.activeSelf);
            Position(new Vector3(-200, 1, -100), 90); minimap.Orientation = MinimapOrientation.RotateWithPlayer; minimap.Refresh();
            Assert.AreEqual(Vector2.zero, minimap.PlayerMarker.anchoredPosition);
            Assert.AreEqual(0, minimap.PlayerMarker.localEulerAngles.z, .001f);
            Assert.LessOrEqual(((RectTransform)marker).anchoredPosition.magnitude, 86.001f);
            game.State.claimed.Add("wood-0"); game.State.claimed.Add("stone-0"); game.State.craftedPotion = true; game.State.placedBuilding = true;
            minimap.Refresh(); Assert.AreEqual(86, minimap.ObjectiveMarker.anchoredPosition.magnitude, .001f);
            Assert.IsTrue(minimap.ObjectiveMarker.gameObject.activeSelf);
            yield return null;
        }
        [UnityTest] public IEnumerator HudGroupsStayInsideActualCanvasAndInsetSafeArea()
        {
            yield return null; Canvas.ForceUpdateCanvases();
            var layout = UnityEngine.Object.FindFirstObjectByType<SafeAreaHud>();
            foreach (var group in layout.Critical) Assert.IsTrue(SafeAreaHud.Inside(group, Screen.safeArea), group.name);
            var root = (RectTransform)layout.transform;
            var inset = new Rect(35, 20, Screen.width - 65, Screen.height - 45);
            SafeAreaHud.Apply(root, inset, new Vector2(Screen.width, Screen.height)); Canvas.ForceUpdateCanvases();
            foreach (var group in layout.Critical) Assert.IsTrue(SafeAreaHud.Inside(group, inset), group.name);
        }
        [UnityTest] public IEnumerator SprintAccelerationJumpAndDodgeCannotEnterClimb()
        {
            Position(new Vector3(0, .1f, 20)); for (int i = 0; i < 10; i++) Step();
            Step(Vector3.forward, Vector2.up, true); float first = T.Speed;
            for (int i = 0; i < 20; i++) Step(Vector3.forward, Vector2.up, true);
            Assert.Greater(T.Speed, first); Assert.Less(T.Stamina.Current, T.Stamina.Max);
            Step(jump: true); Assert.AreEqual(TraversalState.Jump, T.State); Assert.Greater(T.VerticalVelocity, 0);
            Position(new Vector3(0, .1f, 20.9f)); Wall(); Assert.IsTrue(game.Player.StartDodge(Vector3.right)); Assert.IsFalse(T.TryClimb()); yield return null;
        }
        [UnityTest] public IEnumerator WaypointActivationRejectsLockedAndBlockedDestinations()
        {
            Assert.IsFalse(game.Exploration.Teleport("mesa-beacon"));
            var item = game.World.Interactions.Find(i => i.Id == "mesa-beacon"); Position(new Vector3(75, 14.1f, 8)); item.Interact();
            Assert.Contains("mesa-beacon", game.State.activatedTeleportIds); Position(Vector3.up); Assert.IsTrue(game.Exploration.Teleport("mesa-beacon")); Assert.Greater(game.Player.transform.position.y, 14);
            var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube); obstruction.transform.position = WorldBuilder.GroundPoint(75,8,15); obstruction.transform.localScale = new Vector3(3, 2, 3); Physics.SyncTransforms();
            Position(Vector3.up); Assert.IsFalse(game.Exploration.Teleport("mesa-beacon")); Assert.Less(game.Player.transform.position.x, 1); yield return null;
        }
        [UnityTest] public IEnumerator TriggerDiscoveryTreasurePuzzleAndReloadArePersistent()
        {
            Position(new Vector3(75, 14.1f, 8)); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert.Contains("mesa-beacon", game.State.discoveredPOIIds); Assert.Contains("echo-mesa", game.State.discoveredRegionIds);
            game.World.Interactions.Find(i => i.Id == "mesa-beacon").Interact();
            Position(new Vector3(51, 6.1f, -9)); var chest = game.World.Interactions.Find(i => i.Id == "shelf-cache"); chest.Interact(); chest.Interact(); Assert.AreEqual(game.Balance.commonTreasureOre, game.State.Count("ore"));
            Position(new Vector3(81, .1f, 28)); game.World.Interactions.Find(i => i.Id == "echo-west").Interact();
            Position(new Vector3(81, .1f, 32)); game.World.Interactions.Find(i => i.Id == "echo-east").Interact();
            Assert.Contains("echo-puzzle", game.State.completedPuzzleIds); Position(new Vector3(81, .1f, 34)); game.World.Interactions.Find(i => i.Id == "canyon-cache").Interact();
            Assert.Contains("canyon-cache", game.State.openedTreasureIds); Assert.IsTrue(game.Save());
            SceneManager.SetActiveScene(previous); yield return SceneManager.UnloadSceneAsync(scene);
            scene = SceneManager.CreateScene("RestoredTraversal"); SceneManager.SetActiveScene(scene); game = new GameObject("Restored").AddComponent<GameSession>(); game.Begin(false); yield return null;
            Assert.Contains("mesa-beacon", game.State.discoveredPOIIds); Assert.Contains("echo-mesa", game.State.discoveredRegionIds); Assert.Contains("echo-puzzle", game.State.completedPuzzleIds);
            Assert.IsFalse(game.World.Interactions.Find(i => i.Id == "shelf-cache").Available); Assert.IsFalse(game.World.Interactions.Find(i => i.Id == "canyon-cache").Available);
            Assert.IsTrue(game.Exploration.Teleport("mesa-beacon"));
        }
    }
}
