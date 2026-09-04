using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderingCity
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Current { get; private set; }
        public static string SavePathOverride;
        public GameBalance Balance { get; private set; }
        public GameState State { get; private set; }
        public SaveStore Saves { get; private set; }
        public PlayerMotor Player { get; private set; }
        public WorldBuilder World { get; private set; }
        public ExplorationWorld Exploration { get; private set; }
        public GameHud Hud { get; private set; }
        public bool Paused { get; private set; }
        public bool Started { get; private set; }
        public bool Building { get; private set; }
        public string BuildKind = "floor";
        public int BuildRotation;
        public string Notice = "欢迎来到风息原野";
        public float NoticeUntil;
        public WorldInteractable Target;
        float autoSaveAt;
        public AudioDirector Audio { get; private set; }
        public WeatherDirector Weather { get; private set; }
        public bool AtWorkbench => Player != null && Vector3.Distance(Player.transform.position, WorldBuilder.WorkbenchPosition) < 4;
        public bool InputReady => Started && !Paused && (Application.isFocused || SavePathOverride != null);

        void Awake()
        {
            Current = this; Time.timeScale = 1; Application.targetFrameRate = 60;
            Balance = Resources.Load<GameBalance>("Balance") ?? ScriptableObject.CreateInstance<GameBalance>();
            Saves = new SaveStore(SavePathOverride ?? Path.Combine(Application.persistentDataPath, "journey.json"));
            Saves.Load(out var saved, out var message); State = saved ?? new GameState(); Notice = message;
            World = gameObject.AddComponent<WorldBuilder>(); World.Create(this);
            Exploration = GetComponent<ExplorationWorld>();
            Player = World.CreatePlayer(this); World.Restore(State);
            Hud = gameObject.AddComponent<GameHud>(); Hud.Create(this);
            Audio = gameObject.AddComponent<AudioDirector>(); Audio.Initialize(this);
            Weather = gameObject.AddComponent<WeatherDirector>(); Weather.Initialize(this);
            SetMenu(true, "title");
        }
        public void Begin(bool fresh)
        {
            if (fresh) { try { Saves.Archive(); } catch (Exception e) { Notify("无法归档旧存档：" + e.Message); return; } State = new GameState(); World.Restore(State); Exploration.Restore(); }
            if (Saves.Blocked) { Notify(Notice); return; }
            Started = true; Player.RestorePosition(); SetMenu(false); Notify(fresh ? "WASD 移动 · 鼠标转动镜头 · E 交互 · Esc 查看帮助" : Notice);
        }
        void Update()
        {
            var kb = Keyboard.current; if (kb == null) return;
            if (Started && kb.escapeKey.wasPressedThisFrame) { if (Building) { Building = false; World.HidePreview(); } else SetMenu(!Paused, "pause"); }
            if (!InputReady) return;
            if (kb.tabKey.wasPressedThisFrame || kb.iKey.wasPressedThisFrame) { SetMenu(true, "inventory"); return; }
            if (kb.mKey.wasPressedThisFrame) { SetMenu(true, "map"); return; }
            if (kb.bKey.wasPressedThisFrame) { Building = !Building; if (!Building) World.HidePreview(); Notify("建造区在据点西侧 · 1/2/3 选择模块 · R 旋转 · 鼠标左键放置 · X 拆除"); }
            if (kb.f5Key.wasPressedThisFrame) Save(true);
            if (Building) { World.UpdateBuilding(); return; }
            if (kb.digit1Key.wasPressedThisFrame) State.selectedSlot = 0;
            if (kb.digit2Key.wasPressedThisFrame) State.selectedSlot = 1;
            if (kb.digit3Key.wasPressedThisFrame) State.selectedSlot = 2;
            if (kb.digit4Key.wasPressedThisFrame) State.selectedSlot = 3;
            if (kb.qKey.wasPressedThisFrame) { if (State.hotbar[State.selectedSlot] == "potion") Result(Rules.Heal(State), "恢复 45 点生命", "无法使用：生命已满或药剂不足"); else { BuildKind = State.hotbar[State.selectedSlot]; Building = true; } }
            Target = World.FindInteraction(Player.transform.position + Vector3.up);
            if (Target != null && kb.eKey.wasPressedThisFrame && Player.CanAct) Target.Interact();
            string region = WorldBuilder.Region(Player.transform.position);
            if (Player.transform.position.x >= -105 && Player.transform.position.x <= 105 && Player.transform.position.z >= -35 && Player.transform.position.z <= 175 && !State.visited.Contains(region)) { State.visited.Add(region); Notify("发现地点 / " + WorldBuilder.RegionName(region)); Save(); }
            if (Time.unscaledTime > autoSaveAt) { Save(); autoSaveAt = Time.unscaledTime + 30; }
        }
        public void Result(bool ok, string success, string fail)
        {
            Notify(ok ? success : fail); if (ok) { Audio?.Play(WorldSound.Pickup); Save(); World.RefreshRewards(); }
        }
        public void Craft(string kind) => Result(Rules.Craft(State, kind, AtWorkbench), "制作完成 / " + GameHud.ItemName(kind), "材料不足、背包已满或不在工作台附近");
        public void Upgrade() => Result(Rules.Upgrade(State, AtWorkbench), "长剑已升级 · 攻击力 26 → 42", "需要星核 ×1、矿石 ×5；仅可在工作台升级一次");
        public void SetMenu(bool paused, string page = "pause") { Paused = paused; Time.timeScale = paused ? 0 : 1; Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = paused; if (Hud != null) Hud.Page = paused ? page : ""; }
        public void ExitBuilding() { Building = false; World.HidePreview(); }
        public bool Save(bool announce = false)
        {
            if (!Started || Saves.Blocked) return false;
            State.x = Player.transform.position.x; State.y = Player.transform.position.y; State.z = Player.transform.position.z; State.yaw = Player.transform.eulerAngles.y;
            bool ok = Saves.Save(State, out var message); if (announce || !ok) Notify(message); return ok;
        }
        public void Notify(string text) { Notice = text; NoticeUntil = Time.unscaledTime + 5; }
        public void Tone(float pitch = 1) => Audio?.Play(WorldSound.Interaction, pitch);
        public void Respawn()
        {
            State.hp = 100; State.x = 0; State.y = 1; State.z = 0; State.yaw = 0;
            Player.RestorePosition(); World.ResetEnemies(); Notify("已在据点重生 · 物品与探索进度保留"); Save();
        }
        void OnApplicationFocus(bool focus) { if (!focus && Started && !Paused && SavePathOverride == null) SetMenu(true); }
        void OnApplicationQuit() { if (Started) Save(); Time.timeScale = 1; }
        void OnDestroy() { if (Current == this) Current = null; Time.timeScale = 1; }
    }
}
