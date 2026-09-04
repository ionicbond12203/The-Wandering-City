# Repository Architecture Assessment

日期：2026-09-05。仓库：ionicbond12203/The-Wandering-City。审查 HEAD：`640658dac4ffc6a36392bfc620bfe5f65807bcae`。结论：**Prototype，有局部 Vertical Slice 技术验证；不是完整 Vertical Slice、Production Candidate 或 Production Ready。** 文档治理完善不等于运行时架构完成迁移。

## 已阅读与核对

根 AGENTS、game/README；docs 下全部 11 份既有记录：Demo体验说明、Unity实现与验证记录、产品设计文档-MVP、技术方案-MVP、技术方案-MVP-验证清单、开放世界扩展设计、开放世界视觉重建记录、开放世界内容扩展记录、地图与安全区HUD实现记录、角色动画管线记录、音频与环境氛围记录。

根目录历史 Codex 任务：OPEN_WORLD_MILESTONE_1、02 visual-quality、03 visual-rebuild、04 character、05 expansion、06 combat、07 map、08 atmosphere。04–08 在开始时为用户未跟踪文件，只读取、不修改或纳入本次提交。06 的任务要求不能视为当前已完成实现。

检查 ProjectVersion、manifest/lock、QualitySettings/GraphicsSettings、PC RP/Renderer、Scripts/Core、Gameplay、UI、Editor、两类 Tests、Verify、各 BuildQA 和最近 commits。

| 最近 commit | 实际主题 |
| --- | --- |
| 640658d | 音频/环境氛围 |
| 8074460 | 角色与动画接入管线 |
| 6fa23c7 | 既有地形内容扩展 |
| 3d262df | 地图与安全区 HUD |
| 6d31673 | 开放世界拓扑与视觉 |
| e00a841 | Terrain/skybox shader 与渲染健康修复 |
| 24bf7d5 | 风格化视觉技术切片 |
| a00b6bc | traversal / exploration 基础 |

## 现有能力与边界

Unity 6000.6.0f1 / URP 17.6.0，Cinemachine 6.6.0、AI Navigation 2.0.14、Input System 1.20.0、uGUI 2.6.0、Test Framework 1.8.0。Standalone 采用 QualitySettings 的 PC 管线 override，不能因为 GraphicsSettings 默认 pipeline 为空就误判 Built-in。PC 配置启用 SRP Batcher、100m 阴影、4 cascades；已有 SSAO、post-processing、自定义环境/草/天空/水 shader。

纯规则与组件已有一定分离：Stamina、资源事务、StableRegistry、v1→v2、原子替换/备份/未知版本保护、CharacterVisualAdapter 和独立位移 collider 都值得保留。交互有 8m 格索引，POI 发现使用 trigger；不能声称所有系统都在逐帧全图扫描。地图共享 bounds/texture 转换，没有额外全世界相机。

环境已有 Editor 烘焙 Terrain、mesh、prefab、Terrain Detail instancing、LOD 和确定性高度模型；不是每帧重新生成整个世界。但 WorldBuilder.Create 仍在启动生成大量玩法对象及占位表现，AudioDirector 在运行时合成 clips；离线生产与运行时职责没有完全分离。

## 扩展阻碍

| 主题 | 当前证据 | 长期影响 |
| --- | --- | --- |
| monolithic systems | WorldBuilder 559 行多职责、EnvironmentAuthoring 1056 行、GameSession/GameHud 直接连接具体系统；一个 runtime asmdef | 内容和规则改动波及启动/恢复/表现，目录无法约束依赖 |
| hard-coded world coordinates | WorldBuilder/ExplorationWorld/ExpansionCatalog、重生点、配方、建造范围 | 修改内容需要改 C#；GroundPoint 只解决贴地，不解决数据 authoring |
| runtime-generated production content | 启动创建宝箱/敌人/建筑/材质、程序 mesh；部分 primitive 无正式资产替代 | 启动成本、资源所有权与艺术生产受限；formalMode 不是 production art |
| direct Resources dependencies | 配置/环境/角色/动画/地图/字体/音频多点直接加载 | 缺 typed 依赖、异步取消与释放边界；加载策略难替换 |
| world streaming limitations | 同步单 World + 单 Terrain，全域恢复与全量列表 | 无法按内存预算卸载；地图目标依赖 live Interactions；多 terrain 查询不充分 |
| AI simulation scaling limits | 每 EnemyAgent Update、90m 停寻路，HUD/Audio 仍遍历敌人 | 远处成本仍随实体数增长，无模拟 LOD/公平预算；未测更大规模上限 |
| navigation scaling limits | PlayableNavigation 同步构建一张区域道路 NavMesh、Return 每帧请求 | 启动阻塞和跨块生命周期未解决；单个历史 278ms 样本不是规模承诺 |
| save/world-state coupling | SaveStore 校验直接依赖世界 ID、奖励与地形范围 | 退役内容可能使旧存档失败；必须先身份/兼容，再解耦状态和 streaming |
| rendering scalability issues | 已有 instancing/LOD 但无 HLOD/驻留/材质/纹理预算；HUD 高频字符串、动态字体 atlas | 地图扩展/正式资产会增加 CPU/GPU/内存成本；当前未量化极限 |
| content authoring bottlenecks | Prepare 调用多种生成、重贴地、保存环境 prefab；地图 Build 时重烘焙 | 手工作品所有权和生成 churn 风险，尚无全面的无损/可重现证明 |
| missing performance instrumentation | 已有 Recorder/FrameTiming 与 nav stopwatch，缺有效全路线 draw/batch、long soak、统一 manifest/预算 | 不应说“完全没有仪表”，也不能用限帧/隐藏窗口短采样证明 60 FPS |
| QA / release debt | Verify 固定报告名/本机路径，只有 Development Build；多套 QA 捕获模式不同、证据本机；一次保存 QA 失败未定位 | 长期不可追溯，重跑绿色可能掩盖问题，尚无 clean Release 认证 |

历史规模：1024m Terrain、约 695m 内容跨度、84 POI/35 敌人/7 信标，来源为既有扩展记录，不是本次新性能测量。地图大小与系统数量均不能替代开放世界连续性与内容质量。

## 阶段结论与行动

Prototype 验证玩法假设，允许明确占位；Vertical Slice 要一段代表性正式体验与可重复管线、试玩及性能全部验收；Production Candidate 要在约定规模可生产/测试/恢复；Production Ready 还需发行范围、硬件矩阵、长时质量与发布批准。当前角色/配乐/环境艺术、全路线性能、streaming、发布验证不足，不能晋级。

本次落实 [愿景](AAA_VISION.md)、[系统边界](AAA_ENGINEERING.md)、[质量门槛](AAA_QUALITY_BAR.md)、[依赖路线图](AAA_ROADMAP.md) 和 [技术债](TECH_DEBT.md)，加文档结构/依赖/链接契约测试。没有重构运行时或扩展内容。5–10 年最主要风险是身份与场景耦合、代码即内容数据库、全量模拟/加载、生成器侵入手工源及证据仅本机；路线图顺序专门处理这些依赖。
