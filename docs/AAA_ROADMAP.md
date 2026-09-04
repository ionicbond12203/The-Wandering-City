# AAA Roadmap

本路线图是未来独立任务队列，不是一次实现授权。**当前仅授权 G00；完成后停止，不开始 G01。** G01–G13 全部 Planned，不能因文档存在自动标完成。沿用既有区域、敌人、武器与流程；内容扩展另立经授权计划。

每个 milestone 开始前冻结边界与证据，最多改变一个主要责任；需要进一步拆分时先给子项同样的验收字段。依赖仅允许指向前序条目。所有项目继承 [质量门槛](AAA_QUALITY_BAR.md)；性能“无回归”指同机同设置三次测量、不超过批准预算且超过 5% 恶化需分析批准。缺失指标不能通过。每项一个独立 commit。

## G00 — 仓库治理基线

- Dependencies: none
- Player outcome: 既有玩家进度和流程成为后续变更不可破坏的契约；本次不改变操作和内容。
- Non-goals: 不重构运行时，不扩图，不制作新资产，不启动后续 milestone。
- Technical acceptance: 六份治理文档、可追踪架构评估和债务；系统边界、质量阶段、依赖和回滚均明确。
- Visual/gameplay acceptance: 明确当前 Prototype 与 art blocker；不宣称视觉/玩法升级，原行为回归通过。
- Performance criteria: 无运行时代码或渲染配置变更；无性能提升/达标声明；记录测量缺口。
- Tests: 文档契约验证依赖顺序、必需字段、引用和系统覆盖，含非法输入反例；全量 EditMode/PlayMode。
- Build validation: Verify.ps1 -Build 成功；保存本次 XML/日志和摘要，检查生成资产副作用。
- Rollback boundary: 单独撤销治理文档/契约测试 commit，不修改 Stable ID、Save Schema 或玩家数据。

## G01 — 既有路线测量与证据基线

- Dependencies: G00
- Player outcome: 玩家报告的卡顿能按明确路线重现和定位，后续优化有可信比较。
- Non-goals: 不调渲染画质、不优化 AI、不改变世界布局。
- Technical acceptance: 统一 evidence manifest 与有效计数器；固定硬件/驱动/质量及冷暖启动；冻结 CPU/GPU、内存、nav/AI/加载预算。
- Visual/gameplay acceptance: 同一现有路线正常输入走查，截图用于定位；辅助传送单列。
- Performance criteria: 按质量协议预热与 3×10 分钟采样，记录 P95/P99、GC、内存峰值、draw/batch；不可用指标未通过；预算与基线分开。
- Tests: 无效/缺失计数器、样本数、证据 hash 与失败退出测试；全量回归。
- Build validation: Verify.ps1 -Build；前台运行测量，Development Profiler 归因，发布基线明确尚未获得。
- Rollback boundary: 仅撤销仪表和报告工具；保留失败/基线证据，不触碰存档。

## G02 — 无损内容生成与构建所有权

- Dependencies: G01
- Player outcome: 更新构建不会意外改变已有道路、地面、角色替换或存档落点。
- Non-goals: 不改变地形造型、不引入 chunk 或新内容。
- Technical acceptance: Generated/HandAuthored 所有权和 manifest；dry-run 差异；两次 Prepare 语义幂等；自定义 prefab/GUID/手工 sentinel 保留。
- Visual/gameplay acceptance: 既有相机前后截图和实际路线无布局差异；碰撞/落点保持。
- Performance criteria: 测量两次 authoring 时间/峰值内存和启动；不超 G01 预算，不用减少资产掩盖问题。
- Tests: 手工编辑保留、两次生成、失败中断恢复、GUID 与引用一致性；全量回归。
- Build validation: Verify.ps1 -Build；重复 Build 比较内容 manifest；源文件无未解释变化。
- Rollback boundary: 生成工具与派生输出一并回滚；手工源和 `.meta` 不覆盖。

## G03 — 稳定内容身份与兼容目录

- Dependencies: G02
- Player outcome: 内容移动或名称调整后，已开宝箱、已败敌人和信标仍保持原进度。
- Non-goals: 不发布新 Save Schema，不迁移全部内容，不重排/复用旧 ID。
- Technical acceptance: 导出现有身份清单；active/retired/alias 契约；旧 ID 一对一保持，未知目录不会静默丢数据。
- Visual/gameplay acceptance: 用 v1/v2 既有进度跑同一奖励/传送流程，无重复领奖或错误回城。
- Performance criteria: 目录验证加载时间/内存有前后数据，满足 G01 存档/启动预算。
- Tests: 重复/空 ID、alias 冲突/环、移位不改身份、退役保留、未知状态保护；全量回归。
- Build validation: Verify.ps1 -Build；隔离旧档启动/保存/重开，清单包含在构建中。
- Rollback boundary: 保留旧目录适配器；撤销清单接入，不对玩家档做逆向改写。

## G04 — 世界状态与存档边界

- Dependencies: G03
- Player outcome: 地点暂时不在场景中仍能保留发现、战斗和奖励进度。
- Non-goals: 不切换世界加载方式，不引入云存档或新游戏规则。
- Technical acceptance: World State 唯一写入者，独立 DTO 快照与 binder；Save 不读取 Terrain/Transform；保留 v1/v2 与未知版本、备份保护。
- Visual/gameplay acceptance: 卸载测试对象再绑定状态，奖励/建筑/信标保持；自然流程与暂停/死亡一致。
- Performance criteria: 大小递增的测试快照测序列化/主线程阻塞/内存，满足 G01；不以清除状态减小存档。
- Tests: 旧档迁移、完整快照、重复绑定、故障注入、写入中断、卸载领奖事务；全量回归。
- Build validation: Verify.ps1 -Build；隔离 v1/v2 主档/备份故障矩阵和实际重启。
- Rollback boundary: 格式如需升级另立迁移子项；本项保留旧 DTO writer；禁止旧二进制覆盖未来格式。

## G05 — 既有内容定义与资产加载接口

- Dependencies: G04
- Player outcome: 现有地点和道具可以替换表现而不改变奖励、位置或存档。
- Non-goals: 不扩展内容、不全库重写、不强制引入 Addressables。
- Technical acceptance: 一个既有资源/宝箱族迁出 WorldBuilder 为定义与 prefab；typed 资产句柄、缺失/取消/释放契约，旧 Resources 在 provider 内适配。
- Visual/gameplay acceptance: 既有资产前后对照相同，交互与旧档往返可玩；placeholder 标签保留。
- Performance criteria: 获取/释放延迟、内存驻留/泄漏和首次显示满足 G01 预算；冷加载也测。
- Tests: 缺失资产、取消、重复释放、定义非法值、资源守恒与 ID；全量回归。
- Build validation: Verify.ps1 -Build；干净导入后 prefab/配置可用，缺失资源有明确失败证据。
- Rollback boundary: 仅一个内容族/provider 的切换开关；回滚保留相同 ID 和旧加载适配。

## G06 — 既有世界分块驻留试点

- Dependencies: G05
- Player outcome: 穿越既有相邻地点时可加载/卸载资源，返回仍保持进度。
- Non-goals: 不增加地图面积，不立即迁移整个 Terrain，不引入浮点原点或无限世界。
- Technical acceptance: 两个相邻既有区域试点 additive 生命周期；预算并发/驻留/取消；地形服务支持块查询；未加载内容可由目录查询。
- Visual/gameplay acceptance: 正常输入穿越和快速传送无空地/掉落/重复实体；跨边界前台截图与试玩。
- Performance criteria: 冻结加载激活 ms、最大驻留/并发和内存预算；30 次往返无单调泄漏，帧时间满足已批准预算。
- Tests: 取消/重入/失败加载、卸载状态、传送 pin、地形边界、交互索引注销；全量回归。
- Build validation: Verify.ps1 -Build；真实磁盘冷/暖加载与隔离进度重启；其余世界保留原路径。
- Rollback boundary: 同 ID 的单场景兼容入口保留；仅回滚试点驻留模式与场景 manifest。

## G07 — 分块导航与路径预算

- Dependencies: G06
- Player outcome: 守卫在既有跨块路线保持可达，加载边界不会造成隔空伤害或永久卡住。
- Non-goals: 不新增敌人类型，不修改战斗数值，不做全世界实时 NavMesh 重建。
- Technical acceptance: 试点导航离线烘焙、版本/链接校验、块就绪才接入、卸载取消；路径队列和 Return 重寻路预算。
- Visual/gameplay acceptance: 玩家跨界遭遇/返回与全部既有奖励可达；正常试玩观察追击和脱战。
- Performance criteria: 冷启动 nav 成本、路径 P95 等待、每帧请求峰值满足 G01/G06 预算，并与同步构建比较。
- Tests: 相邻边缘连通、缺失导航、取消陈旧请求、卸载追击、不可达不伤害；全量回归。
- Build validation: Verify.ps1 -Build；运行跨块追击、回返、传送与奖励可达 Build QA。
- Rollback boundary: 导航 baker/data/provider 同步回滚，保留原连通导航方案，ID/进度不动。

## G08 — AI Simulation LOD 调度

- Dependencies: G07
- Player outcome: 远处守卫不持续消耗完整模拟成本，靠近时行为与奖励一致。
- Non-goals: 不增敌、不增加离线掉落、不改变近战语义，不强制 ECS。
- Technical acceptance: Near/Mid/Far 频率/迟滞可配置；有全局预算与公平队列；Audio/UI 消费事件或局部查询。
- Visual/gameplay acceptance: 穿越阈值无突然攻击、重生或跳变；既有遭遇自然试玩通过。
- Performance criteria: 既有 35 名守卫与隔离合成负载分开测；调度 CPU、活跃数、GC 达预算；合成负载不宣称地图内容扩充。
- Tests: 晋降级、饥饿、暂停、死亡、卸载、重入与一次性奖励；全量回归。
- Build validation: Verify.ps1 -Build；Build 中距离往返和 CPU trace，保留基线比较。
- Rollback boundary: 调度器切回旧更新适配；状态和 Save 不改；不保留压力测试对象到内容库。

## G09 — 角色、交互与 UI 应用边界

- Dependencies: G08
- Player outcome: 同一输入在暂停、战斗、攀爬、滑翔和重载后产生一致结果，地图不因对象卸载丢目标。
- Non-goals: 不加战斗技能、不新增目标链、不做 UI 视觉重设计。
- Technical acceptance: 输入/动作仲裁与表现分离；只读目标投影/世界位置目录；UI 事件刷新；编译边界先覆盖这一责任。
- Visual/gameplay acceptance: 原控制、地图六种分辨率及既有完整循环正常输入验收；动画不控制伤害。
- Performance criteria: HUD GC/CPU 和角色动作采样与 G01 比较，满足预算，无新稳态分配。
- Tests: 状态互斥/取消窗口、地图未加载目标、失败交互不扣物、依赖方向；全量回归。
- Build validation: Verify.ps1 -Build；既有 map/character QA 和实际玩法走查。
- Rollback boundary: 每种用例适配独立可回退；保留行为、数据定义和同一 Save Schema。

## G10 — 既有环境 LOD/HLOD 验证

- Dependencies: G09
- Player outcome: 远眺既有地标层次稳定，接近时细节切换不会明显闪现或改变碰撞。
- Non-goals: 不扩图、不迁移渲染管线、不用降低可读性换 FPS。
- Technical acceptance: 一个既有环境族的 LOD/HLOD、阴影代理、材质/纹理预算与离线生成；所有权和流式引用明确。
- Visual/gameplay acceptance: 相同相机/天气的近中远截图、连续接近录像与碰撞走查；审查轮廓和切换误差。
- Performance criteria: draw/batch、GPU、显存/内存、加载量有效数据，三次前后对照满足 G01；无效计数器阻止通过。
- Tests: LOD 引用/边界、资源释放、代理无 gameplay collider 改动、shader 健康；全量回归。
- Build validation: Verify.ps1 -Build；原生前台捕获和 Frame Debugger/Profiler 证据。
- Rollback boundary: 一个环境族的派生代理和配置回滚，原始源/GUID/碰撞不变。

## G11 — 正式代表性资产与 Vertical Slice

- Dependencies: G10
- Player outcome: 一条既有路线呈现一致的正式角色、动作、环境与声音，并保留原玩法。
- Non-goals: 不制作整世界内容；不以程序员美术或合成测试音宣布正式资产完成。
- Technical acceptance: 仅替换该路线代表性资产；导入规范、许可证、蒙皮/重定向/音频/纹理 validator 与替换入口；缺资产即 Blocked。
- Visual/gameplay acceptance: 动画师/开发者动作审片、人工试听、Build 视点审查、20–30 分钟自然玩法走查；按质量门槛批准 Vertical Slice。
- Performance criteria: 正式资产接入后的全路线 CPU/GPU/内存三次采样达预算；不能沿用占位资产数据。
- Tests: prefab/Avatar/动画集/音频路由、导入预算、碰撞与奖励回归；全量回归。
- Build validation: Verify.ps1 -Build；新旧档自然流程，截图与音频证据归档且来源完整。
- Rollback boundary: 每种资产替换作为独立子项提交；回滚引用和资产而不触动 gameplay 或 ID。

## G12 — 可重复交付与 Production Candidate

- Dependencies: G11
- Player outcome: 同一版本在清洁安装环境也能稳定启动、恢复进度，更新结果可预测。
- Non-goals: 不扩展发行内容，不宣称 Production Ready，不把 CI 绿色当试玩。
- Technical acceptance: 固定发行候选范围；自托管/本机可复现构建入口，Release 配置、证据 hash/归档、LFS/许可检查；保留 Development QA。
- Visual/gameplay acceptance: 同一切片在干净安装和重装后正常输入可玩，Release 无调试 UI，正式艺术审查无回退。
- Performance criteria: Release 与 Development 分别采样；30 次边界往返和长时预检满足已批准内存/帧预算。
- Tests: 构建失败/缺失报告必须失败、发布配置检查、旧档恢复、干净导入；全量回归不跳过。
- Build validation: Verify.ps1 -Build；另执行有记录的 Release 构建和清洁环境启动，归档当前/前一版本证据。
- Rollback boundary: 发布工具/配置独立 commit；前一安装包保留，数据前向兼容失败时禁止覆盖。

## G13 — 明确发行范围的 Production Ready 审核

- Dependencies: G12
- Player outcome: 已批准发行范围可长时间游玩，升级故障后进度有可靠恢复路径。
- Non-goals: 不在验收阶段新增内容或隐式扩大范围；发现缺陷另立小型修复 milestone。
- Technical acceptance: 已批准硬件矩阵、安装/升级/回滚矩阵、许可、严重缺陷与 art blocker 关闭，开发者具名批准；不是自动发布。
- Visual/gameplay acceptance: 无口头指导玩家走查既有流程，所有必需视觉/声音项目有真实审查结论。
- Performance criteria: 目标矩阵各至少 2 小时正常路线 soak；帧分位、GC、内存与加载预算达标，无崩溃或进度损坏。
- Tests: 完整 EditMode/PlayMode、保存故障/迁移、发布回归；未定位随机失败不得以重跑绿色关闭。
- Build validation: Verify.ps1 -Build 加 Release 安装包验收与校验和；全部证据持久归档。
- Rollback boundary: 只冻结/撤销发行候选；保留已安装版本与玩家数据，发布操作需单独授权。
