# Milestone 07：圆形小地图、全图与安全区 HUD

基线：`6d31673d5479636a443f8d5fe7846d82f51d4f41`。本次只实现 07，不包含 04–06、08 的角色、战斗或环境扩展。

## 裁切原因与布局

旧 Gameplay overlay 固定 1920×1080，小地图从绝对坐标 x=1730 开始。CanvasScaler 按宽高混合缩放时，部分窗口的逻辑画布宽度小于 1920，右侧 HUD 因此被裁掉。

现在 CanvasScaler 使用 Expand，SafeAreaRoot 根据 Screen.safeArea 更新归一化锚点。Gameplay overlay 拉伸填满安全区，下设 TopLeftHud、TopCenterHud、TopRightHud、BottomLeftHud、BottomCenterHud、CenterHud。右上组的 anchor/pivot 均为 (1,1)，右边距 40、上边距 36；位置、任务、生命、体力和快捷栏分别跟随所在锚点组。菜单使用安全区内居中、等比适配的独立画布区域。

SafeAreaHud 在分辨率或安全区改变后更新布局，并在 Editor / Development Build 对关键组的屏幕四角执行断言。测试包含不对称安全区边距。

## 地图与任务

- 编辑器菜单：`Wandering City > Map > Bake World Map`。从 World 场景中的实际 Terrain 读取尺寸、位置、高度、坡度和 Terrain Layer 权重，结合原创区域配色、共用道路模型和场景中存在的 Water 渲染体范围生成地图。
- 输出：`game/Assets/_Game/Art/UI/Maps/WorldMap_Base.png`，1024×1024。烘焙 API 也接受 2048 分辨率。
- `Resources/WorldMap.asset` 同时保存纹理引用和实际世界 Bounds，因此构建会携带地图纹理。BuildWindows 在 Prepare 后重新烘焙。
- WorldMapData 统一世界坐标、UV、全图和以玩家为中心的小地图坐标。小地图使用一张 RawImage、一个原创圆形 Sprite Mask 和原创几何标记；没有第二台世界相机。
- 玩家始终居中。未发现 POI 隐藏，已激活信标/已完成点显示青色。当前任务独立于发现状态显示，目标解析器跟随资源采集、工作台、建造、营地、升级和屋顶阶段变化。
- 圆外目标保留方位并夹到半径 86 的内圈；完整 16 像素目标标记仍在半径 100 的裁切范围内。距离在圆下方显示。
- 北向固定与随玩家旋转两种模式可在 M 地图中切换，也可通过 CircularMinimap Inspector 配置视距和模式。
- 全图默认围绕玩家以 3 倍打开，滚轮范围 1–8 倍，拖拽限制在地图边缘。标记和文字保持屏幕尺寸，点击标记查看名称、完成状态、距离和信标状态；已激活信标继续使用原有传送、落点检查及存档规则。

## 验证与证据

执行 `game/Tools/Verify.ps1 -Build`：70 项 EditMode、28 项 PlayMode 全部通过，Windows Development Build 成功。结果见本机 `artifacts/editmode-results.xml`、`playmode-results.xml`、`unity-build.log`。

新增测试覆盖地图构建引用、世界边界转换、玩家居中、旋转方位、圆边夹取、未知点隐藏、发现/传送序列化、真实 HUD 安全区、全图缩放/平移/复位及标记锚点；原有完整传送、阻挡落点、存档重载和游戏流程测试继续执行。

`MapBuildQA` 仅在 Development Build / Editor 且传入 `-mapQA <输出目录>` 时安装，使用随机隔离存档。它校验六个窗口分辨率、实际 HUD 四角、圆边任务标记、玩家居中、地图缩放、传送状态持久化和实际安全传送。

目标分辨率：1280×720、1366×768、1600×900、1920×1080、2560×1440、2560×1080。

截图输出到 `artifacts/map-qa-final/`：hud-720p.png、hud-768p.png、hud-900p.png、hud-1080p.png、hud-1440p.png、hud-ultrawide.png、minimap-objective-edge.png、minimap-rotate.png、world-map.png。result.json 记录最终结果。六种分辨率均通过实际安全区四角检查，构建 QA 的传送持久化和实际传送均通过。

截图来自真实 Windows Build。由于后台隐藏窗口的 framebuffer 截图为黑色，QA 使用 URP Render Request 离屏渲染，并临时将 HUD 切到 ScreenSpaceCamera；正常游戏为 ScreenSpaceOverlay。尺寸断言仍使用真实窗口与 Screen.safeArea。截图中的全发现及任务阶段由隔离 QA 状态准备，不代表实际玩家已完成探索；该验证不替代前台键鼠试玩或整段性能测量。

地图纹理、圆形遮罩与箭头均为项目原创程序生成素材。
