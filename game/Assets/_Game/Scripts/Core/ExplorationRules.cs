using System;
using System.Linq;
using System.Collections.Generic;

namespace WanderingCity
{
    public enum PoiType { Landmark, TeleportPoint, Treasure, EnemyCamp, ResourceArea, Puzzle, Secret }
    public enum TreasureTier { Common, Rare }
    public static class ExplorationCatalog
    {
        static ExplorationCatalog()
        {
            PoiIds.UnionWith(ExpansionCatalog.PoiIds);
            TeleportIds.UnionWith(ExpansionCatalog.Regions.Select(r=>r.Key("beacon")));
            RegionIds.UnionWith(ExpansionCatalog.Regions.Select(r=>r.Id));
            TreasureIds.UnionWith(ExpansionCatalog.Regions.SelectMany(r=>new[]{"cache-a","cache-b","puzzle-cache","elite-cache"}.Select(r.Key)));
            PuzzleIds.UnionWith(ExpansionCatalog.Regions.Select(r=>r.Key("puzzle")));
        }
        public static readonly HashSet<string> PoiIds = new HashSet<string> { "base-beacon", "mesa-beacon", "wind-spire", "shelf-cache", "canyon-cache", "echo-puzzle", "north-camp", "ore-garden", "canyon-secret" };
        public static readonly HashSet<string> TeleportIds = new HashSet<string> { "base-beacon", "mesa-beacon" };
        public static readonly HashSet<string> RegionIds = new HashSet<string> { "wind-meadow", "echo-mesa", "split-canyon" };
        public static readonly HashSet<string> TreasureIds = new HashSet<string> { "shelf-cache", "canyon-cache" };
        public static readonly HashSet<string> PuzzleIds = new HashSet<string> { "echo-puzzle" };
    }
    public static class ExplorationRules
    {
        public static bool AddOnce(List<string> list, string id, HashSet<string> catalog)
        {
            if (string.IsNullOrWhiteSpace(id) || !catalog.Contains(id) || list.Contains(id)) return false;
            list.Add(id); return true;
        }
        public static bool Discover(GameState state, string id) => AddOnce(state.discoveredPOIIds, id, ExplorationCatalog.PoiIds);
        public static bool Activate(GameState state, string id)
        {
            if (!AddOnce(state.activatedTeleportIds, id, ExplorationCatalog.TeleportIds)) return false;
            Discover(state, id); return true;
        }
        public static bool CanTeleport(GameState state, string id) => state.hp > 0 && ExplorationCatalog.TeleportIds.Contains(id) && state.activatedTeleportIds.Contains(id);
        public static bool OpenTreasure(GameState state, string id, IDictionary<string, int> reward)
        {
            if (!ExplorationCatalog.TreasureIds.Contains(id) || state.hp <= 0 || state.openedTreasureIds.Contains(id) || !ExpansionCatalog.RewardUnlocked(state,id)) return false;
            if (!Rules.Transact(state, reward)) return false;
            state.openedTreasureIds.Add(id); Discover(state, id); return true;
        }
        public static bool CompletePuzzle(GameState state, string id, bool allActive) => allActive && AddOnce(state.completedPuzzleIds, id, ExplorationCatalog.PuzzleIds);
        public static bool ValidIds(List<string> ids, HashSet<string> catalog)
        {
            if (ids == null || ids.Count > catalog.Count) return false;
            var unique = new HashSet<string>();
            foreach (var id in ids) if (id == null || !catalog.Contains(id) || !unique.Add(id)) return false;
            return true;
        }
    }
    public sealed class StableRegistry<T>
    {
        readonly Dictionary<string, T> entries = new Dictionary<string, T>(StringComparer.Ordinal);
        public IEnumerable<T> Values => entries.Values;
        public void Register(string id, T value)
        {
            if (string.IsNullOrWhiteSpace(id) || entries.ContainsKey(id)) throw new ArgumentException("Missing or duplicate stable ID: " + id);
            entries.Add(id, value);
        }
        public bool TryGet(string id, out T value) => entries.TryGetValue(id, out value);
    }
    public sealed class PuzzleProgress
    {
        readonly HashSet<string> required, active = new HashSet<string>();
        public bool Complete => required.Count > 0 && active.Count == required.Count;
        public PuzzleProgress(params string[] nodes)
        {
            required = new HashSet<string>(nodes);
            if (required.Count == 0 || required.Count != nodes.Length || required.Contains(null) || required.Contains("")) throw new ArgumentException("Puzzle requires distinct node IDs");
        }
        public bool Activate(string id) => required.Contains(id) && active.Add(id);
        public void Reset() => active.Clear();
    }
}
