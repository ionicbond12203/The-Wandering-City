using System;
using System.Collections.Generic;
using System.Linq;

namespace WanderingCity
{
    [Serializable] public sealed class ItemStack { public string id; public int count; public ItemStack(string id, int count) { this.id = id; this.count = count; } }
    [Serializable] public sealed class BuildingState
    {
        public string id, kind; public int x, z, rotation;
        public BuildingState(string kind, int x, int z, int rotation) { id = Guid.NewGuid().ToString("N"); this.kind = kind; this.x = x; this.z = z; this.rotation = rotation; }
    }
    [Serializable] public sealed class GameState
    {
        public int version = 2, hp = 100, weaponLevel = 1, selectedSlot;
        public float x, y = 1, z, yaw;
        public bool craftedPotion, placedBuilding;
        public List<ItemStack> inventory = new List<ItemStack> { new ItemStack("potion", 2) };
        public List<string> claimed = new List<string>(), defeated = new List<string>(), visited = new List<string>();
        public List<string> hotbar = new List<string> { "potion", "floor", "wall", "roof" };
        public List<BuildingState> buildings = new List<BuildingState>();
        public List<string> discoveredPOIIds = new List<string>(), activatedTeleportIds = new List<string>(), discoveredRegionIds = new List<string>();
        public List<string> openedTreasureIds = new List<string>(), completedPuzzleIds = new List<string>();
        public int Count(string id) => inventory.Where(s => s.id == id).Sum(s => s.count);
    }
    public sealed class Recipe
    {
        public readonly string output; public readonly Dictionary<string, int> cost;
        public Recipe(string output, params object[] pairs) { this.output = output; cost = new Dictionary<string, int>(); for (int i = 0; i < pairs.Length; i += 2) cost.Add((string)pairs[i], (int)pairs[i + 1]); }
    }
    public static class Rules
    {
        public const int Slots = 20, StackLimit = 99;
        public static readonly string[] Items = { "wood", "stone", "ore", "core", "potion", "floor", "wall", "roof" };
        public static readonly Dictionary<string, Recipe> Recipes = new Dictionary<string, Recipe>
        {
            ["potion"] = new Recipe("potion", "wood", 2, "stone", 1),
            ["floor"] = new Recipe("floor", "wood", 4, "stone", 2),
            ["wall"] = new Recipe("wall", "wood", 3, "stone", 1),
            ["roof"] = new Recipe("roof", "wood", 4, "stone", 1)
        };
        // Every resource operation validates a complete candidate before changing live state.
        public static bool Transact(GameState s, IDictionary<string, int> delta)
        {
            var counts = Items.ToDictionary(id => id, s.Count);
            foreach (var p in delta) { if (!counts.ContainsKey(p.Key)) return false; long n = (long)counts[p.Key] + p.Value; if (n < 0 || n > Slots * StackLimit) return false; counts[p.Key] = (int)n; }
            if (counts.Values.Sum(n => (n + StackLimit - 1) / StackLimit) > Slots) return false;
            var candidate = new List<ItemStack>();
            foreach (var p in counts) for (int left = p.Value; left > 0; left -= StackLimit) candidate.Add(new ItemStack(p.Key, Math.Min(left, StackLimit)));
            s.inventory = candidate; return true;
        }
        public static bool Claim(GameState s, string id, IDictionary<string, int> reward)
        {
            if (!WorldCatalog.RewardIds.Contains(id) || s.claimed.Contains(id) || (id == "camp-reward" && !WorldCatalog.CampEnemies.All(s.defeated.Contains))) return false;
            if (!Transact(s, reward)) return false; s.claimed.Add(id); return true;
        }
        public static bool Craft(GameState s, string id, bool atWorkbench)
        {
            if (!atWorkbench || s.hp <= 0 || !Recipes.TryGetValue(id, out var recipe)) return false;
            var delta = recipe.cost.ToDictionary(p => p.Key, p => -p.Value); delta[id] = 1;
            if (!Transact(s, delta)) return false; if (id == "potion") s.craftedPotion = true; return true;
        }
        public static bool Upgrade(GameState s, bool atWorkbench)
        {
            if (!atWorkbench || s.hp <= 0 || s.weaponLevel != 1 || !Transact(s, new Dictionary<string, int> { ["core"] = -1, ["ore"] = -5 })) return false;
            s.weaponLevel = 2; return true;
        }
        public static bool Heal(GameState s)
        {
            if (s.hp <= 0 || s.hp >= 100 || !Transact(s, new Dictionary<string, int> { ["potion"] = -1 })) return false;
            s.hp = Math.Min(100, s.hp + 45); return true;
        }
        public static bool CanPlace(GameState s, string kind, int x, int z, int rotation, bool clear)
        {
            if (s.hp <= 0 || !clear || !new[] { "floor", "wall", "roof" }.Contains(kind) || x < -4 || x > -1 || z < -1 || z > 2 || rotation < 0 || rotation > 3 || s.buildings.Count >= 48) return false;
            var cell = s.buildings.Where(b => b.x == x && b.z == z).ToList();
            if (cell.Any(b => b.kind == kind && (kind != "wall" || b.rotation == rotation))) return false;
            if (kind != "floor" && !cell.Any(b => b.kind == "floor")) return false;
            if (kind == "roof" && cell.Count(b => b.kind == "wall") < 2) return false;
            // Shared cell edges may only contain a single wall.
            if (kind == "wall") { int dx = rotation == 1 ? 1 : rotation == 3 ? -1 : 0; int dz = rotation == 0 ? 1 : rotation == 2 ? -1 : 0;
                if (s.buildings.Any(b => b.kind == "wall" && b.x == x + dx && b.z == z + dz && b.rotation == (rotation + 2) % 4)) return false; }
            return true;
        }
        public static bool Place(GameState s, string kind, int x, int z, int rotation, bool clear)
        {
            if (!CanPlace(s, kind, x, z, rotation, clear) || !Transact(s, new Dictionary<string, int> { [kind] = -1 })) return false;
            s.buildings.Add(new BuildingState(kind, x, z, rotation)); s.placedBuilding = true; return true;
        }
        public static bool Remove(GameState s, string id)
        {
            var b = s.buildings.Find(p => p.id == id); if (b == null || s.hp <= 0) return false;
            if (s.buildings.Any(p => p.x == b.x && p.z == b.z && p.id != id && ((b.kind == "floor" && p.kind != "floor") || (b.kind == "wall" && p.kind == "roof")))) return false;
            if (!Transact(s, new Dictionary<string, int> { [b.kind] = 1 })) return false;
            s.buildings.Remove(b); return true;
        }
        public static string Objective(GameState s)
        {
            if (!s.claimed.Any(id => id.StartsWith("wood-")) || !s.claimed.Any(id => id.StartsWith("stone-"))) return "01 / 采集补给\n在据点附近收集木材与石材，按 E 交互。";
            if (!s.craftedPotion) return "02 / 为远行做准备\n返回工作台，制作一瓶晨露药剂。";
            if (!s.placedBuilding) return "03 / 一个可以归来的地方\n制作地板，按 B 在据点西侧放置。";
            if (!s.claimed.Contains("camp-reward")) return "04 / 沉睡遗迹的回响\n探索北方营地，击败 5 名守卫并开启星核宝箱。";
            if (s.weaponLevel == 1) return "05 / 让星辉成为力量\n采集 5 枚矿石，回到工作台升级长剑。";
            if (!s.buildings.Any(b => b.kind == "roof")) return "06 / 风中的小屋\n在地板上放置至少两面墙，再搭上屋顶。";
            return "旅程完成 / 风仍在前方\n你已建立自己的据点。继续寻找隐藏宝箱与未探索的路径。";
        }
    }
    public static class WorldCatalog
    {
        public static readonly string[] CampEnemies = Enumerable.Range(0, 5).Select(i => "enemy-camp-" + i).ToArray();
        public static readonly HashSet<string> EnemyIds = new HashSet<string>(CampEnemies.Concat(Enumerable.Range(0, 5).Select(i => "enemy-wild-" + i)));
        public static readonly HashSet<string> RewardIds = new HashSet<string>(Enumerable.Range(0, 28).Select(i => "wood-" + i).Concat(Enumerable.Range(0, 20).Select(i => "stone-" + i)).Concat(Enumerable.Range(0, 12).Select(i => "ore-" + i)).Concat(EnemyIds.Select(id => "drop-" + id)).Concat(new[] { "camp-reward", "chest-forest", "chest-quarry", "chest-hidden" }));
    }
}
