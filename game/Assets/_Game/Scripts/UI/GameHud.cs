using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace WanderingCity
{
    public sealed class GameHud : MonoBehaviour
    {
        public string Page = "title";
        GameSession session;
        Canvas canvas; RectTransform root, menu;
        CanvasGroup gameplayGroup;
        TMP_FontAsset font;
        TMP_Text status, objective, prompt, notice, hotbar, location, menuInfo;
        Image health, damage;
        string renderedPage;
        float flash;
        readonly List<(EnemyAgent enemy, RectTransform rect, TMP_Text text)> enemyLabels = new List<(EnemyAgent, RectTransform, TMP_Text)>();
        readonly Color ivory = new Color(.94f, .92f, .83f), gold = new Color(.89f, .72f, .39f), panel = new Color(.07f, .13f, .15f, .94f);
        public static string ItemName(string id) => id == "wood" ? "风纹木材" : id == "stone" ? "原野石材" : id == "ore" ? "星辉矿石" : id == "core" ? "遗迹星核" : id == "potion" ? "晨露药剂" : id == "floor" ? "地板" : id == "wall" ? "墙体" : id == "roof" ? "屋顶" : id;
        public void Create(GameSession owner)
        {
            session = owner;
            var go = new GameObject("Adventure HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; root = go.GetComponent<RectTransform>();
            var scale = go.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1920, 1080); scale.matchWidthOrHeight = .5f;
            var canvasRoot = root; var gameplay = new GameObject("Gameplay overlay", typeof(RectTransform), typeof(CanvasGroup)); root = Rect(gameplay, canvasRoot, 0, 0, 1920, 1080); gameplayGroup = gameplay.GetComponent<CanvasGroup>(); gameplayGroup.blocksRaycasts = false;
            var osFont = Resources.Load<Font>("Fonts/NotoSansSC");
            font = TMP_FontAsset.CreateFontAsset(osFont, 32, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.isMultiAtlasTexturesEnabled = true;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Box(root, "Location panel", 40, 36, 390, 105, new Color(.08f, .16f, .17f, .88f));
            Label(root, "THE WANDERING CITY", 64, 53, 360, 28, 18, gold);
            location = Label(root, "旅人据点", 64, 84, 355, 46, 30, ivory);
            Box(root, "Objective panel", 40, 160, 390, 158, new Color(.08f, .16f, .17f, .84f));
            objective = Label(root, "", 62, 180, 345, 122, 22, ivory);
            Label(root, "M 地图   Tab 背包   Esc 暂停", 1440, 45, 440, 36, 20, ivory, TextAlignmentOptions.Right);
            Label(root, "N  /  北方遗迹", 770, 35, 380, 30, 20, gold, TextAlignmentOptions.Center);
            status = Label(root, "", 60, 924, 490, 45, 22, ivory);
            Box(root, "HP track", 60, 975, 355, 10, new Color(.12f, .22f, .22f, .9f));
            health = Box(root, "HP", 60, 975, 355, 10, new Color(.51f, .8f, .6f));
            hotbar = Label(root, "", 630, 975, 690, 55, 23, ivory, TextAlignmentOptions.Center);
            Label(root, "WASD 移动  /  Shift 奔跑  /  Space 跳跃\n左键 攻击  /  右键 闪避  /  Q 使用快捷物品", 1380, 948, 480, 75, 19, ivory, TextAlignmentOptions.Right);
            prompt = Label(root, "", 505, 800, 910, 110, 25, ivory, TextAlignmentOptions.Center);
            notice = Label(root, "", 465, 170, 990, 90, 25, gold, TextAlignmentOptions.Center);
            Label(root, "·", 948, 520, 24, 30, 30, ivory, TextAlignmentOptions.Center);
            damage = Box(root, "Damage", 0, 0, 1920, 1080, Color.clear); damage.raycastTarget = false;
            foreach (var enemy in session.World.Enemies) { var t = Label(root, "", 0, 0, 160, 55, 17, ivory, TextAlignmentOptions.Center); enemyLabels.Add((enemy, t.rectTransform, t)); }
            menu = Box(canvasRoot, "Menu overlay", 0, 0, 1920, 1080, new Color(.035f, .08f, .09f, .92f)).rectTransform;
        }
        public void Flash() { flash = .4f; }
        void Update()
        {
            if (session == null) return;
            gameplayGroup.alpha = session.Paused ? 0 : 1;
            flash = Mathf.Max(0, flash - Time.unscaledDeltaTime); damage.color = new Color(.6f, .08f, .03f, flash * .5f);
            var s = session.State;
            health.rectTransform.sizeDelta = new Vector2(355 * s.hp / 100f, 10);
            status.text = "旅人   " + s.hp + " / 100     长剑 Lv." + s.weaponLevel;
            location.text = WorldBuilder.RegionName(WorldBuilder.Region(session.Player.transform.position)); objective.text = Rules.Objective(s);
            hotbar.text = string.Join("     ", s.hotbar.Select((id, i) => (s.selectedSlot == i ? "<color=#E4BA68>" : "") + (i + 1) + " " + ItemName(id) + " ×" + s.Count(id) + (s.selectedSlot == i ? "</color>" : "")));
            prompt.text = session.Building ? "建造 / " + ItemName(session.BuildKind) + " ×" + s.Count(session.BuildKind) + "\n1 地板   2 墙体   3 屋顶   R 旋转   左键放置   X 拆除   B 退出" : session.Target != null && !session.Paused ? "[ E ]  " + session.Target.Label : "";
            notice.text = Time.unscaledTime < session.NoticeUntil ? session.Notice : "";
            foreach (var row in enemyLabels)
            {
                bool visible = !session.Paused && row.enemy.gameObject.activeSelf && Vector3.Distance(session.Player.transform.position, row.enemy.transform.position) < 20;
                Vector3 p = Camera.main.WorldToScreenPoint(row.enemy.transform.position + Vector3.up * 2.5f); visible &= p.z > 0;
                row.rect.gameObject.SetActive(visible);
                if (visible) { row.rect.position = new Vector3(p.x - 80 * canvas.scaleFactor, p.y, 0); row.text.text = "遗迹守卫 " + row.enemy.Hp + "/104\n" + (row.enemy.Action == EnemyAction.Attack ? "<color=#FFB26D>蓄力攻击 · 闪避！</color>" : ""); }
            }
            menu.gameObject.SetActive(session.Paused);
            if (Page != renderedPage) { RenderMenu(); renderedPage = Page; }
            if (menuInfo != null) menuInfo.text = InventoryText() + "\n\n" + (Time.unscaledTime < session.NoticeUntil || !session.Started ? session.Notice : "");
        }
        string InventoryText() => string.Join("    ", Rules.Items.Select(id => ItemName(id) + " ×" + session.State.Count(id))) + "\n背包 " + session.State.inventory.Count + "/20 格 · 每格最多 99 件";
        void RenderMenu()
        {
            foreach (Transform child in menu) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            menuInfo = null; if (Page == "") return;
            Label(menu, "THE WANDERING CITY  /  风息原野", 150, 92, 1600, 42, 22, gold);
            if (Page == "title")
            {
                Label(menu, "循风而行\n在旷野中，建一个家。", 150, 230, 1300, 190, 66, ivory);
                Label(menu, "一段关于探索、战斗与归途的单人冒险。\n从旅人据点出发，穿越树林与矿区，寻找沉眠遗迹中的星核。", 158, 470, 1150, 100, 27, ivory);
                Button(menu, "继续旅程", 160, 635, 360, () => session.Begin(false)); Button(menu, "开始新旅程", 555, 635, 360, () => { Page = "new"; });
                Label(menu, "WASD 移动 · 鼠标转动镜头 · E 交互 · Esc 暂停\n风格化原型 / Windows PC / 本地单人存档", 160, 750, 1200, 90, 23, ivory);
                menuInfo = Label(menu, "", 160, 905, 1580, 110, 19, gold); return;
            }
            if (Page == "new")
            {
                Label(menu, "开始新的旅程？", 150, 245, 1400, 90, 56, ivory);
                Label(menu, "已有存档会保留为带日期的归档文件；新旅程从据点开始。", 158, 390, 1500, 80, 27, ivory);
                Button(menu, "出发", 160, 550, 350, () => session.Begin(true)); Button(menu, "返回", 550, 550, 350, () => Page = session.Started ? "pause" : "title"); return;
            }
            Label(menu, Page == "craft" ? "工作台 / 为下一次远行做准备" : Page == "inventory" ? "行囊 / 旅途的收获" : Page == "map" ? "原野地图 / 选择你的道路" : "休息片刻", 150, 170, 1600, 82, 48, ivory);
            Button(menu, "返回旅程", 1480, 85, 280, () => session.SetMenu(false));
            if (Page == "craft")
            {
                int row = 0; foreach (var recipe in Rules.Recipes.Values) { string id = recipe.output; Label(menu, ItemName(id) + "\n" + string.Join(" + ", recipe.cost.Select(p => ItemName(p.Key) + " ×" + p.Value)), 175, 300 + row * 110, 990, 87, 25, ivory); Button(menu, "制作", 1290, 302 + row * 110, 320, () => session.Craft(id)); row++; }
                Label(menu, "旅人长剑 Lv.2 / 伤害 26 → 42\n遗迹星核 ×1 + 星辉矿石 ×5", 175, 755, 1000, 92, 25, gold); Button(menu, "升级武器", 1290, 755, 320, session.Upgrade);
            }
            else if (Page == "inventory")
            {
                for (int i = 0; i < Rules.Items.Length; i++) { string id = Rules.Items[i]; int col = i % 4, row = i / 4; var box = Box(menu, "Inventory card", 160 + col * 395, 310 + row * 185, 365, 150, panel); Label(box.rectTransform, ItemName(id), 24, 23, 320, 44, 28, ivory); Label(box.rectTransform, "× " + session.State.Count(id), 24, 85, 320, 42, 30, gold); }
                Button(menu, "使用药剂 +45 HP", 160, 740, 430, () => session.Result(Rules.Heal(session.State), "已恢复生命", "生命已满或药剂不足"));
                Label(menu, "快捷栏：1 药剂 · 2 地板 · 3 墙体 · 4 屋顶\n选择后按 Q 使用；模块需要先在工作台制作。", 660, 745, 1040, 90, 26, ivory);
            }
            else if (Page == "map") RenderMap();
            else
            {
                Label(menu, Rules.Objective(session.State), 165, 295, 1200, 130, 31, gold);
                Button(menu, "保存旅程", 160, 470, 390, () => session.Save(true)); Button(menu, "查看地图", 590, 470, 390, () => Page = "map"); Button(menu, "查看背包", 1020, 470, 390, () => Page = "inventory");
                Label(menu, "WASD 移动 / Shift 奔跑 / Space 跳跃 / 鼠标转动镜头\n左键 攻击 / 右键或 Ctrl 闪避 / E 交互 / Q 使用快捷物品\nB 建造 / R 旋转 / X 拆除 / F5 保存 / Tab 背包 / M 地图\n\n敌人橙色预警后会攻击，闪避开始时有短暂无敌。死亡保留背包。\n地板须在据点西侧网格内放置；屋顶需要同格地板及至少两面墙。", 165, 595, 1580, 235, 25, ivory);
                Button(menu, "新旅程", 160, 855, 290, () => Page = "new"); Button(menu, "保存并退出", 500, 855, 350, () => { if (session.Save(true)) Application.Quit(); });
            }
            menuInfo = Label(menu, "", 160, 950, 1600, 120, 20, gold);
        }
        void RenderMap()
        {
            var map = Box(menu, "Map", 170, 280, 1050, 625, new Color(.16f, .26f, .24f)).rectTransform;
            Vector2 Map(Vector3 p) => new Vector2((p.x + 105) / 210 * 1000 + 25, 590 - (p.z + 15) / 190 * 565);
            var points = new[] { ("旅人据点 / 工作台", new Vector3(0, 0, 0)), ("风息树林 / 木材", new Vector3(-48, 0, 65)), ("旧日采石场 / 矿石", new Vector3(64, 0, 59)), ("沉眠营地 / 星核", new Vector3(20, 0, 135)) };
            foreach (var pair in new[] { (0, 1), (0, 2), (1, 3), (2, 3) }) { Vector2 a = Map(points[pair.Item1].Item2), b = Map(points[pair.Item2].Item2); var road = Box(map, "Route", a.x, a.y, Vector2.Distance(a, b), 4, new Color(.54f, .57f, .4f)); road.rectTransform.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg); }
            foreach (var p in points) { var xy = Map(p.Item2); Box(map, "Landmark", xy.x - 8, xy.y - 8, 16, 16, gold); Label(map, p.Item1, xy.x - 125, xy.y + 15, 285, 45, 21, ivory, TextAlignmentOptions.Center); }
            Vector2 player = Map(session.Player.transform.position); Box(map, "You", player.x - 7, player.y - 7, 14, 14, Color.white);
            Label(menu, "白点 / 你的位置\n\n林间道路通向北方遗迹。\n树林与矿区可自由选择先后。\n偏离道路，也许会有新的发现。\n\n营地守卫\n" + session.State.defeated.Count(id => id.StartsWith("enemy-camp-")) + " / 5 已击败\n\n宝箱\n" + session.State.claimed.Count(id => id.StartsWith("chest-")) + " / 3 已发现", 1300, 315, 430, 560, 26, ivory);
        }
        RectTransform Rect(GameObject go, Transform parent, float x, float y, float w, float h) { var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect; }
        Image Box(Transform parent, string name, float x, float y, float w, float h, Color color) { var go = new GameObject(name, typeof(RectTransform), typeof(Image)); Rect(go, parent, x, y, w, h); var image = go.GetComponent<Image>(); image.color = color; return image; }
        TMP_Text Label(Transform parent, string text, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft) { var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); Rect(go, parent, x, y, w, h); var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.font = font; tmp.fontSize = size; tmp.color = color; tmp.text = text; tmp.alignment = alignment; tmp.raycastTarget = false; tmp.textWrappingMode = TextWrappingModes.Normal; return tmp; }
        void Button(Transform parent, string text, float x, float y, float width, Action action) { var image = Box(parent, text, x, y, width, 65, new Color(.24f, .34f, .32f)); var button = image.gameObject.AddComponent<Button>(); var colors = button.colors; colors.highlightedColor = new Color(1, .88f, .6f); button.colors = colors; button.onClick.AddListener(() => action()); Label(image.rectTransform, text, 8, 15, width - 16, 42, 25, ivory, TextAlignmentOptions.Center); }
        void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); if (font != null) Destroy(font); }
    }
}
