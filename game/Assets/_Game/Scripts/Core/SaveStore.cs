using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace WanderingCity
{
    public sealed class SaveStore
    {
        readonly string path; readonly object gate = new object();
        [Serializable] sealed class VersionHeader { public int version; }
        public bool Blocked { get; private set; }
        public SaveStore(string path) { this.path = path; }
        public static bool Validate(GameState s)
        {
            if (s == null || s.version != 1 || s.hp < 0 || s.hp > 100 || s.weaponLevel < 1 || s.weaponLevel > 2 || s.inventory == null || s.inventory.Count > Rules.Slots || s.claimed == null || s.defeated == null || s.visited == null || s.hotbar == null || s.buildings == null) return false;
            if (s.inventory.Any(p => p == null || !Rules.Items.Contains(p.id) || p.count <= 0 || p.count > Rules.StackLimit) || s.claimed.Any(id => !WorldCatalog.RewardIds.Contains(id)) || s.defeated.Any(id => !WorldCatalog.EnemyIds.Contains(id))) return false;
            if (s.claimed.Count != s.claimed.Distinct().Count() || s.defeated.Count != s.defeated.Distinct().Count() || s.hotbar.Count != 4 || s.hotbar.Any(id => !Rules.Items.Contains(id)) || s.selectedSlot < 0 || s.selectedSlot > 3 || s.visited.Any(id => !new[] { "camp", "forest", "quarry", "ruins" }.Contains(id))) return false;
            if (s.claimed.Contains("camp-reward") && !WorldCatalog.CampEnemies.All(s.defeated.Contains)) return false;
            if (s.claimed.Any(id => id.StartsWith("drop-") && !s.defeated.Contains(id.Substring(5)))) return false;
            if (s.buildings.Count > 48 || s.buildings.Any(b => b == null || string.IsNullOrEmpty(b.id)) || s.buildings.Select(b => b.id).Distinct().Count() != s.buildings.Count) return false;
            var check = new GameState();
            foreach (var b in s.buildings.OrderBy(b => b.kind == "floor" ? 0 : b.kind == "wall" ? 1 : 2)) { if (!Rules.CanPlace(check, b.kind, b.x, b.z, b.rotation, true)) return false; check.buildings.Add(b); }
            return true;
        }
        public static void SafePosition(GameState s)
        {
            if (s.hp == 0 || !float.IsFinite(s.x) || !float.IsFinite(s.y) || !float.IsFinite(s.z) || Math.Abs(s.x) > 105 || s.z < -35 || s.z > 170 || s.y < -2 || s.y > 25) { s.x = 0; s.y = 1; s.z = 0; s.hp = 100; }
            if (!float.IsFinite(s.yaw)) s.yaw = 0;
        }
        bool Read(string file, out GameState state)
        {
            state = null; try { var s = new GameState { version = 0, hp = -1, weaponLevel = -1, selectedSlot = -1, inventory = null, claimed = null, defeated = null, visited = null, hotbar = null, buildings = null }; JsonUtility.FromJsonOverwrite(File.ReadAllText(file), s); if (!Validate(s)) return false; SafePosition(s); state = s; return true; } catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { return false; }
        }
        public bool Load(out GameState state, out string message)
        {
            lock (gate)
            {
                foreach (string file in new[] { path, path + ".bak" })
                {
                    try { if (File.Exists(file)) { var header = JsonUtility.FromJson<VersionHeader>(File.ReadAllText(file)); if (header != null && header.version > 0 && header.version != 1) { Blocked = true; state = null; message = "存档版本不兼容。文件已保护，请使用对应版本游戏，或归档后开始新旅程。"; return false; } } }
                    catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException) { /* Continue to validated backup recovery. */ }
                }
                if (Read(path, out state)) { Blocked = false; message = "已恢复旅程"; return true; }
                if (Read(path + ".bak", out state)) { Blocked = false; message = "主存档不可用，已恢复上一份备份"; return true; }
                Blocked = File.Exists(path) || File.Exists(path + ".bak");
                message = Blocked ? "存档损坏或版本不兼容。原文件已保留；选择新旅程会归档旧文件。" : "新的旅程即将开始"; return false;
            }
        }
        public bool Save(GameState state, out string message)
        {
            lock (gate)
            {
                if (Blocked || !Validate(state)) { message = "无法保存：存档被保护或状态无效"; return false; }
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(state, true));
                    using (var file = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None)) { file.Write(data, 0, data.Length); file.Flush(true); }
                    // Never replace a healthy backup with an already damaged primary.
                    if (File.Exists(path)) File.Replace(path + ".tmp", path, Read(path, out _) ? path + ".bak" : null);
                    else File.Move(path + ".tmp", path);
                    message = "旅程已保存"; return true;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { message = "保存失败：" + e.Message; return false; }
            }
        }
        public void Archive()
        {
            lock (gate) { string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"); foreach (string suffix in new[] { "", ".bak", ".tmp" }) if (File.Exists(path + suffix)) File.Move(path + suffix, path + ".archived-" + stamp + suffix); Blocked = false; }
        }
    }
}
