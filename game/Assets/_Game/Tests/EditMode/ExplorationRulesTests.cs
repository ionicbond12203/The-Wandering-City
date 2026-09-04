using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace WanderingCity.Tests
{
    public sealed class ExplorationRulesTests
    {
        [Test] public void StaminaDrainExhaustionAndClamp()
        {
            var stamina = new Stamina(100, 25, 1);
            stamina.Tick(2, 20); Assert.AreEqual(60, stamina.Current);
            stamina.Tick(9, 20); Assert.AreEqual(0, stamina.Current); Assert.IsTrue(stamina.Exhausted);
            stamina.Tick(-1, 10); stamina.Tick(float.NaN, 0); Assert.AreEqual(0, stamina.Current);
            stamina.Tick(20, 0); Assert.AreEqual(100, stamina.Current);
        }
        [Test] public void RecoveryHonorsDelayAcrossFrames()
        {
            var stamina = new Stamina(100, 20, 1); stamina.Tick(1, 50);
            stamina.Tick(.5f, 0); Assert.AreEqual(50, stamina.Current);
            stamina.Tick(1, 0); Assert.AreEqual(60, stamina.Current);
            stamina.Tick(.25f, 4); stamina.Tick(.75f, 0); Assert.AreEqual(59, stamina.Current);
        }
        [Test] public void DiscoveryAndStableRegistrationAreIdempotent()
        {
            var state = new GameState(); Assert.IsTrue(ExplorationRules.Discover(state, "wind-spire")); Assert.IsFalse(ExplorationRules.Discover(state, "wind-spire"));
            Assert.IsFalse(ExplorationRules.Discover(state, "unknown")); Assert.AreEqual(1, state.discoveredPOIIds.Count);
            var registry = new StableRegistry<int>(); registry.Register("spire", 1); Assert.Throws<ArgumentException>(() => registry.Register("spire", 2)); Assert.Throws<ArgumentException>(() => registry.Register("", 3));
            Assert.IsTrue(registry.TryGet("spire", out var value)); Assert.AreEqual(1, value);
        }
        [Test] public void WaypointRequiresActivationAndCannotUseOtherPois()
        {
            var state = new GameState(); Assert.IsFalse(ExplorationRules.CanTeleport(state, "mesa-beacon"));
            Assert.IsTrue(ExplorationRules.Activate(state, "mesa-beacon")); Assert.IsFalse(ExplorationRules.Activate(state, "mesa-beacon"));
            Assert.IsTrue(ExplorationRules.CanTeleport(state, "mesa-beacon")); Assert.Contains("mesa-beacon", state.discoveredPOIIds);
            Assert.IsFalse(ExplorationRules.Activate(state, "wind-spire")); state.hp = 0; Assert.IsFalse(ExplorationRules.CanTeleport(state, "mesa-beacon"));
        }
        [Test] public void PuzzleNeedsAllDistinctNodesAndCompletesOnce()
        {
            var state = new GameState(); var puzzle = new PuzzleProgress("a", "b");
            Assert.IsFalse(puzzle.Activate("unknown")); Assert.IsTrue(puzzle.Activate("a")); Assert.IsFalse(puzzle.Activate("a"));
            Assert.IsFalse(ExplorationRules.CompletePuzzle(state, "echo-puzzle", puzzle.Complete));
            Assert.IsTrue(puzzle.Activate("b")); Assert.IsTrue(ExplorationRules.CompletePuzzle(state, "echo-puzzle", puzzle.Complete));
            Assert.IsFalse(ExplorationRules.CompletePuzzle(state, "echo-puzzle", puzzle.Complete));
            Assert.Throws<ArgumentException>(() => new PuzzleProgress("a", "a"));
        }
        [Test] public void TreasureRequiresPuzzleAndRewardIsAtomicOnceOnly()
        {
            var state = new GameState(); var reward = new Dictionary<string, int> { ["ore"] = 6 };
            Assert.IsFalse(ExplorationRules.OpenTreasure(state, "canyon-cache", reward));
            ExplorationRules.CompletePuzzle(state, "echo-puzzle", true);
            Assert.IsTrue(ExplorationRules.OpenTreasure(state, "canyon-cache", reward)); Assert.IsFalse(ExplorationRules.OpenTreasure(state, "canyon-cache", reward)); Assert.AreEqual(6, state.Count("ore"));
            Rules.Transact(state, new Dictionary<string, int> { ["wood"] = 18 * 99 });
            Assert.IsFalse(ExplorationRules.OpenTreasure(state, "shelf-cache", new Dictionary<string, int> { ["stone"] = 1 })); Assert.IsFalse(state.openedTreasureIds.Contains("shelf-cache"));
        }
        [Test] public void ExplorationRoundTripAndVersionOneMigrationPreserveLegacyRewards()
        {
            string folder = Path.Combine(Path.GetTempPath(), "exploration-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            try
            {
                var state = new GameState(); var store = new SaveStore(Path.Combine(folder, "journey.json"));
                ExplorationRules.Activate(state, "mesa-beacon"); ExplorationRules.Discover(state, "wind-spire");
                state.discoveredRegionIds.Add("echo-mesa"); ExplorationRules.CompletePuzzle(state, "echo-puzzle", true);
                ExplorationRules.OpenTreasure(state, "canyon-cache", new Dictionary<string, int> { ["ore"] = 6 });
                Assert.IsTrue(store.Save(state, out _)); Assert.IsTrue(store.Load(out var loaded, out _)); Assert.AreEqual(JsonUtility.ToJson(state), JsonUtility.ToJson(loaded));
                state = new GameState(); state.claimed.Add("chest-forest"); state.visited.Add("forest"); state.version = 1;
                string oldJson = JsonUtility.ToJson(state);
                foreach (string field in new[] { "discoveredPOIIds", "activatedTeleportIds", "discoveredRegionIds", "openedTreasureIds", "completedPuzzleIds" }) oldJson = oldJson.Replace(",\"" + field + "\":[]", "");
                File.WriteAllText(Path.Combine(folder, "old.json"), oldJson);
                var old = new SaveStore(Path.Combine(folder, "old.json")); Assert.IsTrue(old.Load(out loaded, out _)); Assert.AreEqual(2, loaded.version);
                Assert.Contains("chest-forest", loaded.claimed); Assert.Contains("forest", loaded.visited); Assert.IsEmpty(loaded.openedTreasureIds); Assert.IsTrue(old.Save(loaded, out _));
                StringAssert.Contains("\"version\":1", File.ReadAllText(Path.Combine(folder, "old.json.bak")));
                Assert.IsFalse(Rules.Claim(loaded, "chest-forest", new Dictionary<string, int> { ["ore"] = 100 }));
            }
            finally { Directory.Delete(folder, true); }
        }
        [Test] public void InvalidExplorationStateIsRejected()
        {
            var state = new GameState(); state.activatedTeleportIds.Add("mesa-beacon"); Assert.IsFalse(SaveStore.Validate(state));
            state.discoveredPOIIds.Add("mesa-beacon"); Assert.IsTrue(SaveStore.Validate(state));
            state.discoveredPOIIds.Add("mesa-beacon"); Assert.IsFalse(SaveStore.Validate(state));
            state.discoveredPOIIds.RemoveAt(1); state.openedTreasureIds.Add("canyon-cache"); Assert.IsFalse(SaveStore.Validate(state));
        }
        [Test] public void AuthorableComponentsHaveUnityMonoScriptBindings()
        {
            var go = new GameObject("Authoring check");
            try
            {
                foreach (var type in new[] { typeof(ClimbSurface), typeof(ExplorationPoi), typeof(RegionDiscovery), typeof(PuzzleController) })
                {
                    var component = (MonoBehaviour)go.AddComponent(type);
                    var script = UnityEditor.MonoScript.FromMonoBehaviour(component);
                    Assert.IsNotNull(script, type.Name); Assert.AreEqual(type, script.GetClass());
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
