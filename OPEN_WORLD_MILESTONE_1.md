你现在是本项目的 Lead Gameplay Engineer / Technical Game Designer。

项目仓库：
ionicbond12203/The-Wandering-City

工作目录中的真实仓库内容是唯一事实来源。不要凭空假设项目结构、类名、API 或已有功能。

# 总体目标

将当前《The Wandering City》逐步升级为一款具有高品质“开放世界二次元动作 RPG”体验的原创游戏。

体验目标可以参考《原神》在以下方面的设计原则：

- 第三人称角色控制手感
- 开放世界自由探索
- 高低差和立体地图
- 攀爬、滑翔、冲刺等连续移动体验
- 兴趣点驱动探索
- 宝箱、解谜、采集、敌人营地形成探索循环
- 即时动作战斗
- 元素/属性之间形成组合关系
- 地图发现、传送点、探索进度
- 视觉地标引导玩家探索
- 玩家在前往主目标途中不断发现新的次级目标

但是：

禁止复制《原神》的源代码、模型、贴图、角色、角色名称、怪物、美术素材、音乐、音效、地图布局、任务文本、剧情、UI 图标、专有名称或其他受版权/商标保护的内容。

我们要复刻的是“设计思想、交互品质和系统深度”，不是复制其具体内容。

所有世界观、角色、技能名称、元素名称、美术资产、地图、建筑、怪物、剧情和 UI 必须保持 The Wandering City 自己的原创设计。

---

# 第一步：先理解仓库

在修改任何代码前，必须：

1. 阅读仓库根目录 `AGENTS.md`。
2. 阅读：
   - `game/README.md`
   - `docs/产品设计文档-MVP.md`
   - `docs/技术方案-MVP.md`
   - `docs/技术方案-MVP-验证清单.md`
   - `docs/Unity实现与验证记录.md`
3. 检查：
   - `game/Packages/manifest.json`
   - `game/ProjectSettings/`
   - `game/Assets/_Game/`
4. 找出当前：
   - Player Controller
   - Camera
   - Combat
   - Enemy AI
   - Interaction
   - Inventory
   - Save System
   - WorldBuilder
   - UI
   - Tests
   的具体实现文件。

不要因为本文档中提到某个类名就假定它存在。

以实际仓库代码为准。

完成检查以后，先输出一个简洁的：

## Repository Assessment

说明：

- 当前已经有什么
- 哪些系统可以直接复用
- 哪些系统需要扩展
- 哪些技术债可能影响开放世界扩展
- 本次准备修改哪些文件
- 本次明确不修改哪些系统

然后继续实施，不要停下来等待我的确认，除非遇到真正无法从仓库推断的破坏性选择。

---

# 技术基线

保持现有技术栈，不要无理由更换：

- Unity 6000.6.0f1
- C#
- URP
- Unity Input System
- CharacterController
- Cinemachine
- Animator
- AI Navigation / NavMesh
- uGUI + TextMeshPro
- ScriptableObject 配置
- JSON 本地存档
- Unity Test Framework

优先扩展现有架构，不要推倒重写。

不要为了“架构优雅”引入：

- ECS
- DOTS
- 第三方大型 Gameplay Framework
- 网络层
- 后端
- 数据库
- 不必要的 Service Locator
- 大型 DI 框架

除非仓库已经依赖且确实有必要。

---

# 最终产品方向

长期目标是让玩家获得以下循环：

据点 / 城市
→ 看见远方地标
→ 自主选择路线
→ 奔跑 / 跳跃 / 攀爬 / 滑翔
→ 发现兴趣点
→ 战斗 / 解谜 / 收集
→ 获得宝箱或成长资源
→ 解锁传送点
→ 开启新的探索区域
→ 强化角色
→ 前往更危险区域

探索本身必须有乐趣，而不是只依赖任务箭头。

---

# 重要原则：不要一次实现整个游戏

本次任务只完成：

# Milestone 1 — Open World Traversal & Exploration Foundation

目标是把当前 MVP 从“普通第三人称小地图冒险”升级为具有明显开放世界探索感的 Vertical Slice。

不要在本次任务实现完整多角色系统、抽卡、大型剧情或完整元素战斗。

---

# Milestone 1 功能范围

## 1. Character Locomotion 重构与打磨

检查当前 CharacterController。

在保留现有功能的基础上支持：

### Ground Movement

- Walking
- Running
- Sprinting
- Jump
- Falling
- Landing
- Dodge

移动必须相对于 Camera Forward。

要求：

- acceleration / deceleration
- air control
- configurable gravity
- configurable jump height
- stable slope handling
- grounded grace period / coyote time
- jump input buffering
- 避免 CharacterController 下坡抖动
- 避免台阶卡住
- 避免高速运动穿透明显障碍

角色朝向应该具有平滑转向，而不是瞬间 snap。

所有关键数值进入现有 Balance / ScriptableObject 配置体系，不要散落 magic number。

---

## 2. Stamina System

增加通用体力系统。

体力用于：

- Sprint
- Climb
- Glide

设计：

Max Stamina configurable。

Sprint：
持续消耗体力。

Climb：
根据移动方向消耗体力。

Glide：
缓慢持续消耗体力。

停止消耗后经过短暂 delay 开始恢复。

状态建议：

Current
Max
DrainRate
RecoveryRate
RecoveryDelay

体力耗尽：

Sprint：
自动退出 Sprint。

Climb：
角色失去抓附并掉落。

Glide：
收起滑翔并进入 Falling。

UI 增加简洁 stamina indicator。

不要复制任何已有游戏 UI。

---

## 3. Climbing System

实现可扩展的开放世界攀爬。

不是简单播放动画。

需要实际处理：

- 检测可攀爬表面
- 角色贴附墙面
- 向上 / 向下 / 横向移动
- Corner / Surface normal 基本处理
- Climb → Fall
- Climb → Ledge top
- Ground → Climb
- Jump/Fall → Climb
- stamina drain
- climb detach
- 防止穿墙
- 防止角色在不可攀爬的小物体上乱吸附

使用 LayerMask / Surface rule 判断是否可攀爬。

不要把所有 Collider 默认设成可攀爬。

至少设计：

Climbable
NonClimbable

或等价的数据驱动方案。

优先实现稳定、可测试的系统，不需要追求复杂 IK。

---

## 4. Ledge Detection

增加基本顶部翻越。

角色攀爬接近墙顶时：

检测：

- 前方墙体
- 顶部空间
- 目标站立位置是否合法

合法时：

Climb
→ LedgeTransition
→ Grounded

不要简单 teleport 穿过 Collider。

动画资源不足时允许先使用程序位移 + placeholder animation state。

---

## 5. Gliding

实现原创滑翔机制。

玩家在空中满足条件时按指定输入展开滑翔。

功能：

- reduced gravity
- controlled descent
- forward steering
- camera-relative steering
- stamina drain
- collision handling
- landing
- cancel glide
- stamina exhausted → fall

滑翔参数必须配置化：

- gravity multiplier
- forward speed
- steering acceleration
- vertical descent speed
- stamina drain

视觉资产可以继续使用原创 placeholder。

不要使用任何《原神》翅膀模型或素材。

---

## 6. Player State Machine

如果当前移动逻辑已经开始出现大量 bool：

isJumping
isDodging
isClimbing
isGliding
isAttacking
...

请把角色核心动作整理为明确状态。

至少考虑：

Grounded
Jump
Fall
Sprint
Dodge
Attack
Climb
Glide
Dead

不要求为设计模式而设计模式。

可以使用 enum + 明确 transition rules。

重点是解决：

- 攻击时能否跳跃
- 闪避时能否攀爬
- 攀爬时能否攻击
- 滑翔时受到攻击怎么办
- 死亡状态禁止什么
- UI 打开时禁止什么

所有 transition 必须可理解、可测试。

---

# 7. Exploration POI System

增加可复用兴趣点系统。

POI 类型至少支持：

- Landmark
- TeleportPoint
- Treasure
- EnemyCamp
- ResourceArea
- Puzzle
- Secret

POI 使用稳定 ID。

不要使用 Runtime Instance ID 做存档标识。

POI 可以有：

id
displayName
type
discovered
completed
worldPosition

至少实现：

玩家靠近 POI
→ Discover
→ UI 提示发现地点
→ 写入存档
→ Map 显示

已经发现的 POI 重载存档后保持状态。

---

# 8. Teleport Waypoints

增加原创传送节点。

功能：

未激活：
玩家接近并交互。

激活：
写入 Save。

地图：
显示已激活节点。

玩家选择节点：
传送到安全 SpawnPoint。

传送必须：

- 退出 Combat
- 重置临时移动状态
- 检查落点
- 正确处理 CharacterController
- 不允许传送到未激活节点

所有节点使用 Stable ID。

---

# 9. Exploration Map

扩展当前地图 UI。

至少实现：

- Player marker
- discovered POIs
- activated teleport points
- basic region discovery
- zoom
- pan

如果当前地图实现不适合扩展，可以小范围重构。

不要照抄《原神》的 UI。

采用 The Wandering City 自己的视觉语言。

---

# 10. Fog / Region Discovery

实现轻量的区域发现机制。

不要一开始做复杂 GPU Fog of War。

可以采用数据驱动 Region：

Region A
Region B
Region C

玩家进入 Region Discovery Trigger 后：

undiscovered
→ discovered

地图显示对应区域。

状态保存到 Save。

Region 使用稳定 ID。

---

# 11. Open World POI Layout

改进当前 World 场景或 WorldBuilder。

当前已有：

- Base
- Forest
- Mine
- Enemy Camp

在不破坏原 MVP 流程前提下，把区域扩展成一个更明显的 Vertical Slice。

地图设计目标：

玩家站在出生点时至少能看到 2～3 个远方 Landmark。

设计三个不同高度层：

High
Mid
Low

加入：

- 山坡
- 峭壁
- 小峡谷
- 高台
- 可滑翔路线
- 可攀爬捷径

形成路线选择：

安全路线
vs
快速但需要攀爬/滑翔的路线。

不要制作《原神》现有区域的复制地图。

---

# 12. Exploration Distraction Loop

地图设计必须体现：

玩家准备去 A
→ 途中看见 B
→ B 附近发现 C
→ C 奖励把玩家带向 D

例如：

Teleport Tower
↓
远处 Treasure glow
↓
Climb route
↓
Small Enemy Camp
↓
Chest
↓
High viewpoint
↓
Glide shortcut

让地图具有“偏离主路线也会得到奖励”的结构。

---

# 13. Treasure / Reward

复用现有一次性奖励系统。

扩展 TreasureChest。

至少支持：

Common
Rare

或者使用本项目原创命名。

宝箱：

- Stable ID
- 可保存 opened state
- 不可重复领取
- Reward 配置化
- 动画/FX 可 placeholder

宝箱应该成为探索奖励的一部分。

---

# 14. Simple Environmental Puzzle Framework

本阶段只实现一个非常轻量、原创的 Puzzle Framework。

例如：

ActivationNode
→ PuzzleController
→ Reward

支持：

- 多个节点
- 所有条件完成
- Puzzle completed
- Spawn / unlock treasure

Puzzle 逻辑必须与具体表现解耦。

不要在本阶段实现完整元素反应。

---

# 15. Camera Quality

检查 Cinemachine 配置。

改善：

- camera collision
- wall clipping
- climb camera
- glide camera
- slope camera
- FOV during sprint
- FOV smooth transition

Sprint 可以轻微增加 FOV。

Glide 可以适当拉远。

不要使用强烈镜头晃动。

所有效果 configurable。

---

# 16. Animation Architecture

现阶段允许继续使用 placeholder animation。

但是 Animator 参数必须整理清楚。

例如：

Speed
VerticalVelocity
Grounded
Climbing
Gliding
Attack
Dodge
Dead

不要让 Gameplay Logic 依赖具体动画长度。

战斗命中时间继续由 gameplay config 控制。

动画只负责表现。

---

# 17. Save Migration

当前已有版本化 Save。

扩展存档字段：

Player：
- stamina（如果设计上需要持久化，否则说明为什么不保存）

World：
- discoveredPOIIds
- activatedTeleportIds
- discoveredRegionIds
- openedTreasureIds
- completedPuzzleIds

保持：

- 旧存档兼容或明确 migration
- backup recovery
- unknown save version protection
- 不重复领取奖励

禁止静默破坏旧存档。

---

# 18. Debug Tools

增加开发调试工具，仅限 Editor / Development Build。

建议：

- Infinite Stamina
- Show Climb Probes
- Show POI Radius
- Show Region Bounds
- Teleport to POI
- Reset Exploration State

不要让 Debug 功能进入 Release UI。

---

# 19. Performance

目前不需要真正 MMORPG 规模 Streaming。

但是新增系统不得明显造成：

- 每帧 FindObjectsOfType
- 每帧 LINQ 分配
- 高频 GC
- 全地图对象逐帧扫描
- 每个 POI 独立昂贵 Update

尽量采用：

trigger
event
cached reference
manager registration

等简单可靠方案。

Profiler 能测量的地方优先测量，不要凭感觉“优化”。

---

# 20. Testing

必须遵守仓库 AGENTS.md。

所有新增核心逻辑需要测试。

EditMode 至少覆盖：

Stamina：
- drain
- recovery
- exhaustion
- clamp

POI：
- discover idempotency
- duplicate stable ID handling

Teleport：
- locked waypoint cannot teleport
- activated waypoint persists

Treasure：
- reward once only

Puzzle：
- completion idempotency

Save：
- 新 exploration state round trip
- migration
- corrupt save fallback（如果相关逻辑被修改）

PlayMode 至少覆盖：

- Player can enter climb
- stamina exhaustion exits climb
- Player can enter glide
- glide ends on landing
- waypoint activation
- treasure persistence
- POI discovery
- save/reload exploration state

测试必须针对实际行为，不要只测试 getter/setter。

---

# 21. Validation

完成修改后运行项目现有验证流程。

优先使用：

`game/Tools/Verify.ps1`

如果适用：

`game/Tools/Verify.ps1 -Build`

至少：

- Unity compilation passes
- EditMode passes
- PlayMode passes

如果某项无法在当前环境运行：

必须明确记录：

- command
- failure reason
- 是否属于代码问题
- 需要人工执行什么

不要声称“测试通过”除非真的运行并通过。

---

# 22. Documentation

更新：

`game/README.md`

说明新增：

- Sprint/Stamina
- Climb
- Glide
- Teleport
- POI discovery
- Map exploration

新增：

`docs/开放世界扩展设计.md`

内容包括：

1. Player Traversal
2. State transitions
3. Stamina
4. Climbing detection
5. Gliding
6. POI model
7. Teleport
8. Region discovery
9. Save schema
10. Future expansion

文档要描述实际实现，而不是理想设计。

---

# 23. Git

严格遵守 `AGENTS.md`。

修改完成后：

1. `git diff`
2. 运行测试
3. 检查异常文件
4. 创建 Git commit
5. `git status`

工作区最终必须干净。

Commit message 建议：

feat: add open world traversal and exploration foundation

如果工作量明显过大，需要多个逻辑独立 commit，可以拆分，例如：

feat: add stamina and traversal states

feat: add climbing and gliding

feat: add exploration POI and teleport

feat: persist exploration progress

test: cover open world exploration systems

但每一个 commit 都必须处于可构建、可理解的状态。

---

# 明确不要做的事情

本次不要实现：

- 抽卡
- 商城
- 联机
- 后端
- 完整七元素系统
- 四角色切换
- 大规模剧情系统
- 配音
- 正式 AAA 美术
- 巨型无缝地图
- MMO streaming
- 程序化无限世界
- 复制任何《原神》资源

也不要删除当前：

- crafting
- building
- inventory
- combat
- save
- existing MVP flow

这些系统以后仍然属于 The Wandering City。

---

# Architecture Rule

优先：

small composable components
+
data-driven configuration
+
stable IDs
+
testable pure C# rules

而不是一个巨大：

PlayerController.cs
WorldManager.cs
GameManager.cs

不要继续制造 God Object。

如果当前类已经过大，允许在保证行为不变和测试覆盖的前提下进行有针对性的拆分。

---

# 完成本次任务的 Definition of Done

玩家必须能够在实际 World 场景完成：

Spawn
→ Run
→ Sprint
→ 消耗 Stamina
→ Jump
→ 接近 Cliff
→ Climb
→ 到达高处
→ Jump / Glide
→ 飞向 POI
→ Discover POI
→ 激活 Teleport
→ 打开 Treasure
→ 查看 Map
→ 保存
→ 退出
→ 重新加载
→ POI / Teleport / Treasure 状态仍然正确

并且：

- 原有战斗可玩
- 原有采集可玩
- 原有制作可玩
- 原有建造可玩
- 原有存档不被破坏
- 自动测试通过
- 项目可编译
- 创建 Git commit
- 工作区 clean

---

# 最终回复格式

完成后只给我以下信息：

## Implementation Summary
实际实现了什么。

## Architecture Changes
新增或重构了哪些核心组件。

## Files Changed
列出主要文件。

## Controls
新增操作按键。

## Tests
实际运行的命令以及结果。

## Performance Notes
有没有发现明显性能风险。

## Known Limitations
本阶段仍未解决什么。

## Git
commit hash
commit message
git status

不要用“应该可以”“理论上通过”这样的表述。

只报告真实验证结果。