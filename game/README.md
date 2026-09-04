# The Wandering City / Unity 游戏

按 `docs/产品设计文档-MVP.md` 与 `docs/技术方案-MVP.md` 实现的 Windows 单机第三人称冒险原型。`demo/` 仍是独立网页参考，不是正式游戏入口。

## 长期维护入口

当前整体为 **Prototype**，已有局部技术切片，但未达到完整 Vertical Slice、Production Candidate 或 Production Ready；历史 milestone 名称不是品质认证。先阅读根 [AGENTS.md](../AGENTS.md)、[架构评估](../docs/REPOSITORY_ARCHITECTURE_ASSESSMENT.md)、[AAA 愿景](../docs/AAA_VISION.md)、[工程边界](../docs/AAA_ENGINEERING.md)、[质量门槛](../docs/AAA_QUALITY_BAR.md)、[路线图](../docs/AAA_ROADMAP.md) 与 [技术债及美术阻塞](../docs/TECH_DEBT.md)。每次只执行授权 milestone，完整测试/构建后独立提交；G00 治理不会自动启动下一项。

当前版本已重建 1024m 开放地形、Terrain Detail 草地、原创树石网格、远山和动态天空，并调整地面锚点与相机。实现、82 项测试、真实 Build 截图和性能边界见 [开放世界视觉重建记录](../docs/开放世界视觉重建记录.md)。角色及部分交互物仍为原型美术。

## 启动

- 已构建版本：运行 `game/Builds/Windows/The Wandering City.exe`。分享时复制整个 Windows 文件夹，不能只复制 exe。
- 编辑器：Unity Hub 添加本目录，使用 **6000.6.0f1**，打开 `Assets/_Game/Scenes/Boot.unity` 后按 Play。
- 如果需要准备场景和默认配置：菜单 `Wandering City > Prepare playable scenes`。Boot 缺失时创建，现有 World 会打开并更新生成环境与必需层级；它不是纯只读检查。先保留手工编辑并检查生成差异。当前内容布局仍有 WorldBuilder/ExpansionCatalog 代码内数据，长期迁移方向见工程边界，不应继续把正式内容全部堆入 WorldBuilder。
- 构建：菜单 `Wandering City > Build Windows`，或 `./Tools/Verify.ps1 -Build`。

Windows 键鼠和风格化几何占位资产采用技术方案的实施基线。锁定编辑器版本来自本机已安装正式版；包依赖提交于 `Packages/manifest.json` 和 `packages-lock.json`。

## 操作与流程

| 操作 | 按键 |
| --- | --- |
| 相对镜头移动 / 奔跑 | WASD / 左 Shift |
| 转动镜头 / 调整距离 | 鼠标 / 滚轮 |
| 跳跃 | Space |
| 普通攻击 | 鼠标左键 |
| 闪避 | 鼠标右键或左 Ctrl |
| 采集、宝箱、工作台 | E |
| 背包 / 地图 / 暂停 | Tab 或 I / M / Esc |
| 快捷选择 / 使用 | 1～4 / Q |
| 建造模式 | B；1 地板、2 墙、3 屋顶；R 旋转；左键放置；X 拆除 |
| 手动保存 | F5，或暂停菜单 |

## Milestone 04 / 角色动画管线

角色现支持独立关节演示、移动混合树、跳跃/落地/攀爬/滑翔/三段攻击状态，以及武器、背部、风帆和镜头挂点。攻击恢复期点击左键衔接下一段，或右键/左 Ctrl 闪避取消。通过 `Resources/CharacterPresentation.asset` 指定角色 prefab，再用 `CharacterRigBindings` 与动画集接入正式 Humanoid。当前仍是原创关节原型，正式模型、蒙皮和配套动画尚缺。接入步骤、限制与验证见 [角色动画管线记录](../docs/角色动画管线记录.md)。

## Milestone 05 / 内容扩展

现有地形内新增逐风花原、雾叶古林、琥珀断岩场、星冠旧城、回音帷谷。有效内容跨度约 695m，共 84 个兴趣点、35 名敌人和 7 个信标；各新区包含谜题、精英、隐藏宝箱以及攀爬滑翔路线。导航限定于玩法区域，远处探索进度支持存档。105 项测试与 Windows 构建验收通过；测量、截图及性能边界见 [开放世界内容扩展记录](../docs/开放世界内容扩展记录.md)。

## Milestone 1 / 开放世界探索

Milestone 07 已加入安全区锚点 HUD 和圆形小地图：玩家居中、目标距离、圆边指引、已发现兴趣点与信标；在 M 全图中可切换北向固定/随玩家旋转、缩放平移、选择标记查看详情及传送。全图初始围绕玩家，使用实际地形边界和随构建烘焙的原创地图纹理。实现与截图验证方式见 [地图与安全区 HUD 实现记录](../docs/地图与安全区HUD实现记录.md)。

- WASD 默认跑步，按住左 Alt 步行，左 Shift 冲刺；蓝绿色横条显示体力。冲刺、攀爬和滑翔消耗体力，停止消耗 1.1 秒后恢复。
- 面向有攀爬标记的台地石壁按 **C** 抓附，W/S 上下，A/D 横移；再次 C 或 X 松开。空中也可按 C 抓墙。接近合法顶部时自动分两段翻越；普通树木、建筑、小石块不支持攀爬。
- 离地超过约 1.4 米时按 **G** 展开原创折叠风帆，WASD 相对镜头转向；再次 G 或 X 收起。落地、受伤或体力耗尽终止滑翔。
- 体力耗尽会退出冲刺/攀爬/滑翔。冲刺需松开 Shift 并恢复到 15 体力后重新启动；抓墙和展开滑翔也需要至少 15 体力。
- 据点东南侧缓坡通向 6 米中层平台；东侧回声台地高 14 米。缓坡是安全路线，石壁提供攀爬捷径；高台北侧可以向风隙峡谷滑翔，峡谷底部不需要攀爬也能步行进入。
- 出生附近的「归途信标」和高台上的「回声台地信标」需要靠近按 E 激活；M 地图中点击已激活的信标传送。系统检查落点、重置动作并解除敌人当前战斗状态。
- 靠近兴趣点/区域自动发现并保存。地图白点为玩家，金色为已发现兴趣点，青色为已完成点；未探索区域显示暗色，滚轮缩放，拖拽平移。
- 中层平台有常见行旅匣；峡谷内两块共鸣石均按 E 点亮后解锁珍稀回声匣。新旧宝箱均只能领取一次，资源沿用原背包系统。

存档升级到 v2，读取 v1 时保留全部原进度并初始化新增探索字段；未知版本继续禁止覆盖。体力属于临时 traversal 状态，读档、重生和安全传送恢复满值，不保存空中动作。未完成解谜的单个节点不持久化，已完成解谜永久保存。

参数位于 `Resources/Balance.asset`。Editor/Development Build 的 PlayerTraversal Inspector 可切换 Infinite Stamina / Show Climb Probes；选中 POI、区域显示范围；ExplorationWorld 组件菜单支持安全传送和清除发现记录（保留已领奖记录）。Release 不编译这些调试入口。

详细的组件边界、状态规则、坐标和限制见 `../docs/开放世界扩展设计.md`。

出生点北侧散落木材与石材，右前方桌子是工作台。制作药剂和建筑模块后，在西侧网格建造。地板需要空地，墙依赖同格地板，屋顶需要同格地板与至少两面墙。拆除先屋顶、再墙、最后地板，完整回收模块，失败不扣资源。

树林在西北，矿区在东北，营地在更北方。地图标出路线，三个隐藏奖励箱鼓励偏离主路。营地有五名守卫，全部击败后可领取星核，用星核 ×1 和矿石 ×5 将武器升级一次（伤害 26→42）。提前探索不会阻断目标链。完成小屋与升级后仍可继续探索。

敌人橙色圆盘表示蓄力攻击。闪避前 0.28 秒免伤，持续 0.42 秒，冷却 0.9 秒。角色死亡后回据点，保留背包、已击败敌人及奖励进度。

## 存档

`%USERPROFILE%/AppData/LocalLow/WanderingCity/The Wandering City/journey.json`。关键操作后自动保存，探索中每 30 秒保存，也可手动保存。

主档通过临时文件原子替换，保留 `.bak`。主档损坏时尝试备份；未知版本禁止自动覆盖。开始新旅程会把旧主档、备份和临时文件重命名为 `.archived-日期`，保留原内容。无损档则从据点开始。自动测试和 Windows 冒烟使用隔离目录，不碰玩家存档。

## 实现结构

- `Scripts/Core`：独立资源规则、配置与版本化存档；所有资源修改先校验后提交。
- `Scripts/Gameplay`：CharacterController、Cinemachine 镜头、NavMesh 敌人、交互和固定地图生成。
- `Scripts/UI`：uGUI + TextMeshPro 中文 HUD、背包、工作台、暂停与地图。
- `Resources/Balance.asset`：战斗数值配置；`Traveler.controller`：角色状态机和原创原型动画，可通过动画集覆盖。
- `Tests/EditMode`：资源守恒、幂等、非法状态、建筑依赖、存档损坏和完整流程。
- `Tests/PlayMode`：实际场景的攻击遮挡、碰撞、交互、重生、建筑与奖励恢复。

地图由固定坐标和固定种子装饰生成，运行时只在启动构建一次静态 NavMesh；建造区与敌人区域分离。不包含玩家地形挖掘、联网或云存档。美术和音效包含项目原创的程序化内容，后续可替换模型、动画和声音素材。

## 音频与环境氛围（Milestone 08）

标题和暂停菜单提供声音设置，主音量、音乐、环境与音效自动持久化。音乐系统支持区域、战斗和精英战优先级、迟滞与双声源淡化；正式音乐槽目前为空，需后续导入已授权配乐。已有原创风声、鸟鸣、流水、雨声及操作提示音。

南部草地新增浅池及原创 URP 水面；晴、阴、小雨渐变联动天空、雾、光照和声音。天气配置位于 `Resources/WorldAtmosphere.asset`，音乐列表位于 `Resources/WorldAudio.asset`。实现、验证方式与限制见 [音频与环境氛围记录](../docs/音频与环境氛围记录.md)。

## 验证

`Tools/Verify.ps1` 依次运行 Unity Edit Mode、Play Mode；加 `-Build` 构建 Windows。日志和 XML 输出至仓库根 `artifacts/`。

开发构建可使用 `-qaOutput "绝对输出目录"` 运行隔离的自动冒烟：截图、通过交互接口采集、制作建造、领取营地奖励、升级和存档往返。冒烟中的传送和直接击败敌人仅用于验证流程及渲染，不代表真实玩家完成了一次冒险，也不替代战斗测试或人工试玩。它会生成截图及短时性能采样 JSON，然后退出。

验收证据与尚待人工确认的项目见 `../docs/Unity实现与验证记录.md`。

## 素材来源

- Noto Sans SC：Google Fonts，SIL Open Font License 1.1，原许可证在 `Assets/_Game/Resources/Fonts/OFL.txt`；源文件：https://github.com/google/fonts/tree/main/ofl/notosanssc。
- TextMeshPro Essential Resources、URP 设置：本机 Unity 官方包/模板随附内容，保留原 GUID 与许可文件。
- 自有字体等大型二进制文件采用 Git LFS；`Library`、`Temp`、本机构建与报告不纳入版本管理。

这是一版可玩占位原型。20～30 分钟体验、镜头舒适度、正式角色美术及参考硬件上的整段 60 FPS 验收仍需后续真人试玩与性能分析，不能用自动化规则测试替代。
