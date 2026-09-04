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
        public CircularMinimap Minimap { get; private set; }
        TMP_FontAsset font;
        TMP_Text status, objective, prompt, notice, hotbar, location, menuInfo, tutorialHint;
        CanvasGroup tutorialGroup;
        float tutorialTimer = 8f, tutorialAlpha = 1f;
        Image health, damage, stamina;
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
            var scale = go.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1920, 1080); scale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var canvasRoot = root;
            var safe = new GameObject("SafeAreaRoot", typeof(RectTransform), typeof(SafeAreaHud)).GetComponent<RectTransform>();
            safe.SetParent(canvasRoot, false); SafeAreaHud.Apply(safe, Screen.safeArea, new Vector2(Screen.width, Screen.height));
            var layout = safe.GetComponent<SafeAreaHud>();
            var gameplay = new GameObject("Gameplay overlay", typeof(RectTransform), typeof(CanvasGroup));
            root = Rect(gameplay, safe, 0, 0, 0, 0); Stretch(root); gameplayGroup = gameplay.GetComponent<CanvasGroup>(); gameplayGroup.blocksRaycasts = false;
            RectTransform Group(string name, Vector2 anchor, Vector2 size, Vector2 offset)
            {
                var group = SafeAreaHud.Group(root, name, anchor, size, offset); layout.Critical.Add(group); return group;
            }
            var topLeft = Group("TopLeftHud", new Vector2(0, 1), new Vector2(390, 300), new Vector2(40, -36));
            var topCenter = Group("TopCenterHud", new Vector2(.5f, 1), new Vector2(990, 200), new Vector2(0, -35));
            var topRight = Group("TopRightHud", Vector2.one, new Vector2(260, 350), new Vector2(-40, -36));
            var bottomLeft = Group("BottomLeftHud", Vector2.zero, new Vector2(490, 130), new Vector2(60, 30));
            var bottomCenter = Group("BottomCenterHud", new Vector2(.5f, 0), new Vector2(690, 60), new Vector2(0, 45));
            var center = Group("CenterHud", Vector2.one * .5f, new Vector2(910, 430), Vector2.zero);
            var osFont = Resources.Load<Font>("Fonts/NotoSansSC");
            font = TMP_FontAsset.CreateFontAsset(osFont, 32, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.isMultiAtlasTexturesEnabled = true;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Box(topLeft, "Location panel", 0, 0, 390, 105, new Color(.08f, .16f, .17f, .28f));
            Label(topLeft, "THE WANDERING CITY", 24, 17, 360, 28, 18, gold);
            location = Label(topLeft, "旅人据点", 24, 48, 355, 46, 30, ivory);
            Box(topLeft, "Objective panel", 0, 124, 390, 158, new Color(.08f, .16f, .17f, .22f));
            objective = Label(topLeft, "", 22, 144, 345, 122, 22, ivory);

            var minimapRoot = Rect(new GameObject("Circular minimap", typeof(RectTransform)), topRight, 10, 12, 240, 240);
            Minimap = minimapRoot.gameObject.AddComponent<CircularMinimap>(); Minimap.Create(session, font);
            Label(topCenter, "N  /  北", 305, 0, 380, 30, 20, gold, TextAlignmentOptions.Center);
            status = Label(bottomLeft, "", 0, 0, 490, 45, 22, ivory);
            Box(bottomLeft, "HP track", 0, 51, 355, 10, new Color(.12f, .22f, .22f, .9f));
            health = Box(bottomLeft, "HP", 0, 51, 355, 10, new Color(.51f, .8f, .6f));
            Box(bottomLeft, "Stamina track", 0, 76, 355, 8, new Color(.12f, .22f, .22f));
            stamina = Box(bottomLeft, "Stamina", 0, 76, 355, 8, new Color(.35f, .79f, .87f));
            Label(bottomLeft, "体力", 0, 93, 100, 28, 16, ivory);
            hotbar = Label(bottomCenter, "", 0, 0, 690, 55, 23, ivory, TextAlignmentOptions.Center);

            // Contextual tutorial hint with auto-fade
            var tutGo = new GameObject("Contextual Tutorial", typeof(RectTransform), typeof(CanvasGroup));
            var tutRect = Rect(tutGo, topRight, 0, 290, 260, 60);
            tutorialGroup = tutGo.GetComponent<CanvasGroup>();
            tutorialHint = Label(tutRect, "WASD 移动 · Space 跳跃 · 左键 攻击", 0, 0, 260, 50, 16, ivory, TextAlignmentOptions.Right);

            prompt = Label(center, "", 0, 320, 910, 110, 25, ivory, TextAlignmentOptions.Center);
            notice = Label(topCenter, "", 0, 100, 990, 90, 25, gold, TextAlignmentOptions.Center);
            Label(center, "·", 443, 195, 24, 30, 30, ivory, TextAlignmentOptions.Center);
            damage = Box(root, "Damage", 0, 0, 1920, 1080, Color.clear); damage.raycastTarget = false; Stretch(damage.rectTransform);
            foreach (var enemy in session.World.Enemies) { var t = Label(root, "", 0, 0, 160, 55, 17, ivory, TextAlignmentOptions.Center); enemyLabels.Add((enemy, t.rectTransform, t)); }
            var menuFrame = SafeAreaHud.Group(safe, "Menu frame", Vector2.one * .5f, new Vector2(1920, 1080), Vector2.zero);
            menu = Box(menuFrame, "Menu overlay", 0, 0, 1920, 1080, new Color(.035f, .08f, .09f, .92f)).rectTransform;
        }
        public void Flash() { flash = .4f; }
        void Update()
        {
            if (session == null) return;
            var menuFrame = (RectTransform)menu.parent;
            var available = ((RectTransform)menuFrame.parent).rect.size;
            menuFrame.localScale = Vector3.one * Mathf.Min(1, Mathf.Min(available.x / 1920, available.y / 1080));
            gameplayGroup.alpha = session.Paused ? 0 : 1;
            flash = Mathf.Max(0, flash - Time.unscaledDeltaTime); damage.color = new Color(.6f, .08f, .03f, flash * .5f);
            var s = session.State;
            health.rectTransform.sizeDelta = new Vector2(355 * s.hp / 100f, 10);
            var energy = session.Player.Traversal.Stamina;
            stamina.rectTransform.sizeDelta = new Vector2(355 * energy.Current / energy.Max, 8);
            status.text = "旅人   " + s.hp + " / 100     长剑 Lv." + s.weaponLevel;
            location.text = session.Exploration.RegionNameAt(session.Player.transform.position); objective.text = Rules.Objective(s);
            hotbar.text = string.Join("     ", s.hotbar.Select((id, i) => (s.selectedSlot == i ? "<color=#E4BA68>" : "") + (i + 1) + " " + ItemName(id) + " ×" + s.Count(id) + (s.selectedSlot == i ? "</color>" : "")));
            prompt.text = session.Building ? "建造 / " + ItemName(session.BuildKind) + " ×" + s.Count(session.BuildKind) + "\n1 地板   2 墙体   3 屋顶   R 旋转   左键放置   X 拆除   B 退出" : session.Target != null && !session.Paused ? "[ E ]  " + session.Target.Label : "";
            notice.text = Time.unscaledTime < session.NoticeUntil ? session.Notice : "";
            tutorialGroup.gameObject.SetActive(DevelopmentVisualMode.Enabled);
            if (session.Started && !session.Paused && tutorialHint != null)
            {
                var trav = session.Player != null ? session.Player.Traversal : null;
                if (trav != null && trav.State == TraversalState.Climb)
                {
                    tutorialHint.text = "悬崖攀爬 · WASD 移动 · X 松开";
                    tutorialTimer = 3.5f;
                }
                else if (trav != null && trav.State == TraversalState.Glide)
                {
                    tutorialHint.text = "原野滑翔 · WASD 导向 · G 收翼";
                    tutorialTimer = 3.5f;
                }
                else if (tutorialTimer > 0f)
                {
                    tutorialTimer -= Time.unscaledDeltaTime;
                }

                float targetAlpha = tutorialTimer > 0f ? 1f : 0f;
                tutorialAlpha = Mathf.MoveTowards(tutorialAlpha, targetAlpha, Time.unscaledDeltaTime * 1.5f);
                if (tutorialGroup != null) tutorialGroup.alpha = tutorialAlpha;
            }
            foreach (var row in enemyLabels)
            {
                bool visible = !session.Paused && row.enemy.gameObject.activeSelf && Vector3.Distance(session.Player.transform.position, row.enemy.transform.position) < 20;
                Vector3 p = Camera.main.WorldToScreenPoint(row.enemy.transform.position + Vector3.up * 2.5f); visible &= p.z > 0;
                row.rect.gameObject.SetActive(visible);
                if (visible) { row.rect.position = new Vector3(p.x - 80 * canvas.scaleFactor, p.y, 0); row.text.text = (row.enemy.Elite ? "区域守望者 " : "遗迹守卫 ") + row.enemy.Hp + "/" + row.enemy.MaxHp + "\n" + (row.enemy.Action == EnemyAction.Attack ? "<color=#FFB26D>蓄力攻击 · 闪避！</color>" : ""); }
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
            var viewport = Box(menu, "Exploration map viewport", 350, 280, 625, 625, new Color(.07f, .13f, .15f)).rectTransform;
            viewport.gameObject.AddComponent<RectMask2D>();
            var map = Box(viewport, "Map content", 0, 0, 625, 625, new Color(.12f, .2f, .2f)).rectTransform;
            var navigation = viewport.gameObject.AddComponent<ExplorationMapInput>(); navigation.Content = map;
            var mapData = WorldMapData.Load();
            var terrainImage = new GameObject("World terrain texture", typeof(RectTransform), typeof(RawImage));
            Rect(terrainImage, map, 0, 0, 625, 625); terrainImage.GetComponent<RawImage>().texture = mapData.Texture; terrainImage.GetComponent<RawImage>().raycastTarget = false;
            Vector2 Map(Vector3 p) => mapData.WorldToMap(p, map.rect.size);
            Image MapMarker(string name, Vector2 position, float size, Color color)
            {
                var image = Box(map, name, position.x, position.y, size, size, color);
                image.rectTransform.pivot = Vector2.one * .5f; return image;
            }
            var details = Label(menu, "选择地图标记查看详情", 1020, 590, 700, 180, 23, ivory);
            foreach (var region in session.Exploration.Regions)
            {
                var bounds = region.GetComponent<BoxCollider>().bounds;
                Vector2 top = Map(new Vector3(bounds.min.x, 0, bounds.max.z));
                Vector2 bottom = Map(new Vector3(bounds.max.x, 0, bounds.min.z));
                bool discovered = session.State.discoveredRegionIds.Contains(region.Id);
                var area = Box(map, region.Id, top.x, top.y, bottom.x - top.x, bottom.y - top.y, discovered ? new Color(.24f, .36f, .3f, .2f) : new Color(.06f, .1f, .12f, .8f)); area.raycastTarget = false;
                Label(map, discovered ? region.DisplayName : "未踏足", top.x - 20, top.y - 25, 170, 30, 13, discovered ? ivory : Color.gray);
            }
            // Old MVP regions retain their existing discovery history.
            foreach (var point in new[] { ("forest", new Vector3(-48, 0, 65)), ("quarry", new Vector3(64, 0, 59)), ("ruins", new Vector3(20, 0, 135)), ("camp", Vector3.zero) })
            {
                if (!session.State.visited.Contains(point.Item1)) continue;
                var p = Map(point.Item2); Label(map, WorldBuilder.RegionName(point.Item1), p.x - 70, p.y, 180, 30, 18, ivory);
            }
            string selectedTeleport = null;
            Button(menu, "传送至所选信标", 1280, 450, 430, () => { if(selectedTeleport != null) session.Exploration.Teleport(selectedTeleport); else session.Notify("请先在地图选择已激活信标"); });
            foreach (var poi in session.Exploration.Points.Values)
            {
                if (!WorldMapData.Visible(session.State, poi.Id)) continue;
                var p = Map(poi.transform.position);
                var marker = MapMarker(poi.Id, p, 14, poi.Completed ? new Color(.3f, .9f, .85f) : gold); marker.raycastTarget = true;
                marker.rectTransform.sizeDelta = Vector2.one * 14;
                var select = marker.gameObject.AddComponent<Button>();
                select.onClick.AddListener(() => { selectedTeleport = poi.Type == PoiType.TeleportPoint && session.State.activatedTeleportIds.Contains(poi.Id) ? poi.Id : null; details.text = poi.DisplayName + "\n" + (poi.Type == PoiType.TeleportPoint ? "传送信标" : "兴趣点") + (poi.Completed ? " / 已完成" : " / 已发现") + "\n距离 " + Mathf.RoundToInt(Vector3.Distance(session.Player.transform.position, poi.transform.position)) + " m" + (poi.Type == PoiType.TeleportPoint ? (session.State.activatedTeleportIds.Contains(poi.Id) ? "\n信标已激活，可选择右侧传送按钮。" : "\n靠近信标按 E 激活。") : ""); });

            }
            var activeObjective = WorldMapData.ObjectivePosition(session);
            if (activeObjective.HasValue) { var p = Map(activeObjective.Value); var target = MapMarker("Quest objective", p, 12, gold); target.gameObject.AddComponent<Button>().onClick.AddListener(() => details.text = Rules.Objective(session.State)); }
            Vector2 player = Map(session.Player.transform.position); MapMarker("You", player, 14, Color.white).raycastTarget = false;
            navigation.Focus(player, 3);
            Label(menu, "白点 / 旅人   青色 / 已完成\n滚轮缩放 · 拖拽平移\n信标须先靠近并按 E 激活\n暗色地形 / 远景荒野", 1280, 285, 450, 145, 23, ivory);
            Label(menu, "兴趣点 " + session.State.discoveredPOIIds.Count + "/" + ExplorationCatalog.PoiIds.Count + "   ·   金色 / 当前目标", 1020, 875, 700, 50, 21, gold);
            Button(menu, "切换小地图方向", 1020, 780, 700, () => { Minimap.Orientation = Minimap.Orientation == MinimapOrientation.NorthUp ? MinimapOrientation.RotateWithPlayer : MinimapOrientation.NorthUp; details.text = "小地图方向：" + (Minimap.Orientation == MinimapOrientation.NorthUp ? "北向固定" : "跟随旅人旋转"); });
        }
        static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        RectTransform Rect(GameObject go, Transform parent, float x, float y, float w, float h) { var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect; }
        Image Box(Transform parent, string name, float x, float y, float w, float h, Color color) { var go = new GameObject(name, typeof(RectTransform), typeof(Image)); Rect(go, parent, x, y, w, h); var image = go.GetComponent<Image>(); image.color = color; return image; }
        TMP_Text Label(Transform parent, string text, float x, float y, float w, float h, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft) { var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); Rect(go, parent, x, y, w, h); var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.font = font; tmp.fontSize = size; tmp.color = color; tmp.text = text; tmp.alignment = alignment; tmp.raycastTarget = false; tmp.textWrappingMode = TextWrappingModes.Normal; return tmp; }
        void Button(Transform parent, string text, float x, float y, float width, Action action) { var image = Box(parent, text, x, y, width, 65, new Color(.24f, .34f, .32f)); var button = image.gameObject.AddComponent<Button>(); var colors = button.colors; colors.highlightedColor = new Color(1, .88f, .6f); button.colors = colors; button.onClick.AddListener(() => action()); Label(image.rectTransform, text, 8, 15, width - 16, 42, 25, ivory, TextAlignmentOptions.Center); }
        void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); if (font != null) Destroy(font); }
    }
}
