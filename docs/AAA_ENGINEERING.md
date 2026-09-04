# AAA Engineering

## 基线与目标边界

审查基线 `640658dac4ffc6a36392bfc620bfe5f65807bcae`，Unity 6000.6.0f1 (f7f8ed4d1e24)。Packages manifest 与 lock：URP 17.6.0、AI Navigation 2.0.14、Input System 1.20.0、Cinemachine 6.6.0、uGUI 2.6.0、Test Framework 1.8.0。以本地锁定配置为依据，没有在本次升级引擎或依赖。

下表是**长期目标契约，不是当前已实现声明**。现在 Core/Gameplay/UI 在同一 WanderingCity asmdef 内，Editor、EditMode、PlayMode 各有独立程序集。先通过适配器逐步迁移，再建立编译边界；禁止为目录整齐一次拆完。

目标依赖方向：不可变内容定义 + 纯规则 → 应用用例/世界状态 → Unity 场景适配器 → 表现与 UI。Save 将快照映射为 DTO，不依赖场景对象；Editor 生产运行时可读的内容，运行时不依赖 Editor。组合根负责接线和生命周期，不承载规则；UI 通过命令和只读投影交互，不直接变更库存/奖励。

## 系统边界

| 系统 | 唯一职责与状态所有权 | 输入/输出、生命周期与禁止依赖 | 现有入口与迁移方向 |
| --- | --- | --- | --- |
| World Streaming | chunk 驻留、加载优先级、取消、卸载与引用计数 | 消费玩家兴趣范围和内容 manifest；发布准备/激活/卸载事件；不拥有持久进度 | Boot 同步 World、WorldBuilder 全量启动 → 有界 additive chunk 服务；先同图试点 |
| World State | 按 Stable ID 保存权威实体/发现/奖励状态，和 GameObject 是否加载无关 | 接受事务命令，输出只读投影与事件；会话存活；加载绑定、卸载解绑；禁止直接磁盘写入 | GameState 公共列表、World.Restore → 状态仓库与场景 binder |
| Save System | 快照 DTO、格式版本、迁移、校验、写入与恢复 | 消费一致性快照及内容兼容目录；原子提交；禁止读取 Terrain、Transform 或 UI | SaveStore → 兼容目录适配器和迁移链；保留 v1/v2、备份、未知版本保护 |
| AI Simulation LOD | 实体模拟级别、时间预算、感知/决策调度 | Near 完整、Mid 降频、Far 抽象/休眠；晋降级有迟滞；死亡/奖励是世界状态事务；禁止全图逐帧更新 | EnemyAgent 90m 停寻路 → 有预算的调度器，近战语义保持 |
| Navigation | 分区导航数据、连通链接、路径请求预算与失败反馈 | 依赖 chunk 就绪；取消带 generation token；卸载先排空请求；不决定奖励/存档 | PlayableNavigation 同步全域构建 → Editor 烘焙、邻块连接、局部重建 |
| Character | 角色根、输入命令仲裁、生命与动作状态入口 | 单个权威运动根；读配置和输入；发动作事件；禁让视觉 collider/root motion 双重位移 | PlayerMotor → 输入适配器与动作仲裁分离 |
| Traversal | 位移、接地、体力、攀爬/滑翔状态转换 | 物理探测经接口；固定/受控步长；暂停、死亡、传送显式重置；临时动作不入档 | PlayerTraversal/Stamina 保留，空间查询接地形服务 |
| Combat | 攻击时序、合法取消、命中去重、伤害事务 | 使用 gameplay 时钟与配置，输出伤害/反馈事件；不依赖 AnimationClip.length、HUD 或材质 | PlayerMotor/EnemyAgent 中规则逐步抽取；不新增攻击/敌人 |
| Animation | 视觉姿态、重定向、挂点与可选 IK | 消费动作快照，不决定命中或进度；明确 in-place/root motion 策略；卸载清理覆盖控制器 | CharacterVisualAdapter/Driver/Bindings/AnimationSet 保留并资产校验 |
| Quest | 目标条件、进度投影与目的地 ID | 消费世界事件及持久状态；支持乱序探索；不搜索已加载交互物推断全世界任务 | Rules.Objective、WorldMapData.ObjectivePosition → 既有目标定义/查询服务 |
| Interaction | 附近候选索引、距离/视线检查、命令入口 | 注册、移动更新、卸载注销均显式；领奖委托事务；不直接写 Save | WorldBuilder interactionCells/WorldInteractable → 独立空间索引与交互用例 |
| Inventory | 物品堆叠、容量、制作/回收的原子事务 | 消费版本化物品/配方定义；失败无变化；输出变更事件；不依赖场景/HUD | Rules/GameState → 定义、运行状态、规则分离；建造保留依赖与守恒 |
| Audio | 声源预算、混音、音乐状态及播放句柄 | 消费区域/战斗/天气事件；设置独立持久化；空槽静音；卸载释放 clips | AudioDirector/Preferences/Library → 停止逐帧全敌人扫描，合成音离线生产 |
| Weather | 气象状态、种子/时钟与局部效果参数 | 输出只读天气帧给 Rendering/Audio；不直接改世界奖励；暂停策略显式 | WeatherDirector/WorldAtmosphere → 模拟与视觉绑定分离 |
| Rendering | 相机、光照、材质、阴影与后处理表现 | 消费配置和视觉状态；只负责 GPU 表现；禁止改 gameplay 状态；资源有 owner | URP PC + 自定义 shader，统一环境/天气写入所有权 |
| Asset Management | 异步获取/释放、依赖、缓存、失败与取消 | typed key/handle + manifest；资产所有权和驻留预算明确；配置缺失显式失败或声明 fallback | 多处 Resources.Load → 边界内兼容 provider，再按证据选择本地引用/Addressables |
| Content Authoring | 定义、放置、生成、校验、烘焙与差异预览 | source → generated → runtime；版本/种子/哈希可追踪；禁止常规 Build 覆盖手工 source | EnvironmentAuthoring/OpenWorldAuthoring/ExpansionAuthoring/ProjectBuilder → 小型生成步骤及 manifest |
| LOD/HLOD strategy | 近景 LOD、远景代理、阴影代理与 transition 规则 | 从烘焙源离线生成；与 streaming 驻留独立；碰撞不随视觉 LOD 消失 | 已有 LODGroup/Terrain Details → 按 chunk HLOD 试点与屏占误差评审 |
| Performance QA | 可重复路线、指标有效性、预算与回归检测 | Build hash、硬件、设置、路线、原始 trace；不可用指标使相关门禁未通过 | BuildSmoke/各 QA 短采样 → 统一有效性和前台路线/soak 证据 |
| Visual QA | 固定视点/状态捕获、像素健康与人工审查 | 原始 Build 图、相机/天气/曝光、分辨率、预期/实际；截图存在不代表艺术合格 | VisualRenderHealthChecks + 多个 BuildQA → 统一证据清单；保留捕获差异 |

地图/UI 是表现消费者：全图和小地图共享世界坐标服务、发现状态、内容位置目录及目标投影，未加载 POI 仍可查询；保留已发现/未发现规则、safe area、传送安全检查。字体与文本布局离线配置、事件更新，不能把全局内容扫描藏在刷新循环里。

## Stable ID、存档与分块交接

Stable ID 是内容身份，不是资源 GUID 或场景位置；已发行 ID 永不复用，移动/改名保留 ID。新的内容 manifest 维护 active/retired/alias、schema 与生成器版本。旧目录行为先原样适配；不能简单放宽 SaveStore.Validate 忽略未知 ID。未来区分未知版本、缺失内容、退役内容和真正损坏，保留不可识别记录及原文件，迁移失败禁止覆盖。

分块激活顺序：获取资产 → 验证定义/ID → 注册导航与碰撞 → 绑定世界快照 → 注册交互/模拟 → 显示并放行玩家。卸载顺序：停止新命令 → 完成或取消事务/路径 → 提交状态 → 注销 → 释放资产。取消、失败、重入与快速传送不得留下半激活块。传送需 pin 目标块，地面/胶囊/导航就绪后才能移动；失败保持原位置与状态。

存档与 chunk 分区不是同一概念。快照必须覆盖卸载对象；保存时采用一致版本的状态，异步 I/O 不得在后台访问 Unity 对象。迁移必须用真实结构的 v1/v2 夹具验证已领取奖励、敌人、建筑、发现和信标；新格式仅在旧格式兼容路径及备份验证后启用。回滚二进制不能读取未来格式时保留文件并提示，禁止强制降版覆盖。

## 资产生产契约

每类资产定义 source 路径、owner、许可证、单位/轴向、导入 preset、命名、预算、validator、输出 manifest 和替换入口。Generated 与 HandAuthored 所有权独立，普通验证默认无损；显式 regeneration 输出 dry-run diff，保护 `.meta` GUID、手工布局及自定义角色 slot。需迁移时备份生成输入并用两次生成比较语义及哈希；不能忽略输出差异。

角色从合法源模型 → Humanoid/蒙皮检查 → 动画重定向 → prefab wrapper/Bindings → Build 动作验收；环境从 source mesh/texture → 导入预算 → collider/LOD → prefab/chunk bake；音频从授权源 → 响度/循环检查 → 导入压缩/streaming → mixer/playlist。正式素材缺失时记录 blocker；不通过增加程序网格代码冒充正式艺术。

## 决策与迁移纪律

优先在原调用点引入适配器，增加守恒/旧档/生命周期测试后切换一个责任。ADR 可作为相关实现记录的一节，必须包含日期、基线、问题、备选、选择、证据、迁移、回滚与后果。新模块至少声明唯一写入者、依赖、线程/时钟、加载/卸载、错误处理与测试入口。禁止全局服务定位器替代清晰依赖。

构建锁定 Unity/Packages/平台，保留 clean checkout + LFS 恢复能力。现有 Verify/Prepare 会产生资产变化，不能假设只读或完全确定性；这是 [技术债](TECH_DEBT.md) 的 G02 范围。Release、CI、长时 profiling 和多块服务目前均未实现，由 [路线图](AAA_ROADMAP.md) 分项推进。
