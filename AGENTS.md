# The Wandering City — Repository Governance

适用于整个仓库。项目是长期开发的 Windows 单机第三人称开放世界；AAA-quality 是质量愿景，不是当前状态。当前整体为 **Prototype**，局部技术切片不等于完整 Vertical Slice。定义见 [质量门槛](docs/AAA_QUALITY_BAR.md)。

## 开始工作

阅读本文件、[游戏入口](game/README.md)、[愿景](docs/AAA_VISION.md)、[系统边界](docs/AAA_ENGINEERING.md)、[质量门槛](docs/AAA_QUALITY_BAR.md)、[路线图](docs/AAA_ROADMAP.md)、[技术债](docs/TECH_DEBT.md)，以及相关设计/实现记录。检查实际 HEAD、近期 commits、工作区和对应实现/测试；历史 milestone 提示词不是完成证明，也不是自动执行授权。

只执行当前授权 milestone。路线图按依赖顺序推进，完成后停止，不自动启动下一项。不覆盖用户已有改动，不为工作区 clean 删除或提交无关文件。

## 不可降低的工程原则

1. 所有功能必须保持数据驱动和可测试；配置与玩家运行状态分离，新增规则提供行为测试。
2. 禁止为了快速完成 milestone 引入无法长期扩展的 monolithic architecture。新增职责按明确所有权、依赖与生命周期拆分，不能继续堆进 WorldBuilder、GameSession 或 GameHud。
3. 禁止把 primitive / programmer art 称为 production art。程序生成资产也必须通过与手工资产相同的质量验收。
4. 缺少正式模型、动画、音频或贴图时，建立 production pipeline，使用明确标注的 placeholder，并在 TECH_DEBT 记录 art blocker、接入位置和退出条件；不允许伪造“AAA完成”。已有 formalMode 命名不构成验收。
5. 不破坏 Stable ID、Save Schema 和已有玩家进度。禁止用实例 ID、场景路径、数组位置重新派生持久化身份；目录变动须有旧档夹具、显式迁移/退役映射、备份与回滚方案，不静默丢弃未知状态。
6. 所有性能相关修改必须有 profiling evidence：同硬件、同路线、同构建配置的前后测量，记录 CPU/GPU、帧分位、内存、GC 和相关系统计数。缺失计数器标 unavailable，不能记成 0 或通过。
7. 所有视觉 milestone 必须有 Build screenshot evidence，记录构建、场景、相机、天气、分辨率、捕获方式与逐项审查结论。Scene View、概念图、生成图、截图文件存在均不能替代视觉验收。
8. 所有 gameplay milestone 必须有 automated tests + actual playable validation；通过测试驱动、传送或直接击败敌人的自动冒烟不等于真人自然完成流程。
9. 每项工作完成后运行 `game/Tools/Verify.ps1`，包括治理/文档改动；同步编写或更新相关测试。
10. 需要构建验证时运行 `game/Tools/Verify.ps1 -Build`。该入口同时执行两套 Unity 测试；不需要仅为命令形式重复执行。当前输出是 Windows x64 Development Build，不是 Release 认证。
11. 每次完整改动完成后创建对应 Git commit；每个完整 milestone 必须创建独立 Git commit。提交前检查 diff、测试、验证、生成资产变化；提交后报告 hash、message、status。不能夹带无关文件或玩家存档。
12. 不允许为了让测试通过而降低验收标准。确需变更产品要求时先记录理由、影响及新的等价/更强验证，不能追认失败为成功。
13. 不允许删除失败测试来获得绿色结果，也不允许以 Ignore、Skip、缩小样本或过滤测试掩盖失败。保留失败证据，修复根因并完整复跑。
14. 不允许在没有测量数据时声称满足性能目标；“tests pass”不等于 production quality，地图大、系统多或看起来更好均不是 AAA 证明。
15. 不允许复制商业游戏的代码、资产、地图、角色或其他版权内容。所有引入资产记录来源、作者、许可证、修改和用途；来源不明时保持 placeholder。

## 维护与交付

- 核心依赖方向、Stable ID / Save 迁移、Unity/包升级及生成器所有权变化，先在相关设计记录中写明决策、备选、兼容与回滚边界；不自动升级锁定依赖。
- 普通游戏功能不得新增直接 Resources 依赖；通过注入引用或受控资产服务接入。现有债务按路线图逐步迁移，禁止一次性删除现有加载路径。
- 正式静态内容优先离线生成/烘焙；运行时只做有预算的加载、实例化和模拟。程序化生产流程须确定性、可重建、保留 GUID，并保护手工创作区。
- 性能或视觉结论必须附可追踪证据；失败、阻塞和未测项目明确报告，不声称已通过。交付前确保全部测试和所需验证通过；环境阻塞则报告命令、日志、原因及未完成状态。
- Unity 资产与 `.meta` 成对提交；保留许可证与 Git LFS。不得提交 Library、Builds、临时报告或个人存档。机器可重现的证据摘要提交到 docs，完整证据按质量门槛保留。
- 单人维护优先小步、可回滚变更。测试优先验证真实行为、负面路径和迁移；治理测试只验证文档结构/引用，不能认证游戏品质。
