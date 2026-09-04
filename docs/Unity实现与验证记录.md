# Unity MVP 实现与验证记录

## Milestone 1 追加验证 / 2026-09-04

开放世界实现与设计细节见《开放世界扩展设计.md》。以下结果来自本次实际运行，不替代下方 MVP 历史记录。

| 命令 / 验证 | 最终结果 | 本地证据 |
| --- | --- | --- |
| `game/Tools/Verify.ps1 -Build` / Unity 编译 | 通过 | `artifacts/unity-editmode.log`、`unity-playmode.log`、`unity-build.log` |
| 同上 / EditMode | 38/38 通过 | `artifacts/editmode-results.xml` |
| 同上 / PlayMode | 26/26 通过 | `artifacts/playmode-results.xml` |
| 同上 / Windows x64 Development Build | 通过 | `artifacts/unity-build.log` 中 `WANDERING_CITY_BUILD_OK` |
| `node --test tests/*.test.mjs`（demo 目录） | 8/8 通过 | 本次终端输出 |
| `game/Builds/Windows/The Wandering City.exe -qaOutput "<仓库>/artifacts/open-world-final" -logFile "<仓库>/artifacts/open-world-final.log"` | 退出码 0，冒烟通过 | `artifacts/open-world-final/smoke-result.json`、日志 `WANDERING_CITY_SMOKE_OK` |

共 72 项自动测试，无跳过。新增覆盖体力消耗/恢复/延迟/耗尽/夹紧、稳定 ID 重复、发现幂等、信标激活与落点阻挡、宝箱事务与解谜解锁、v1→v2 迁移及备份保留、组件 Unity 脚本绑定；场景覆盖真实墙面抓附、耗尽/受伤掉落、顶部翻越、受阻翻越、滑翔转向与落地、coyote time、跳跃缓冲、低帧率碰撞、地图缩放平移，以及探索状态卸载场景后重新恢复。原战斗、资源、建造、奖励恢复与全部交互点的 NavMesh 可达性用例继续通过。

Windows 冒烟扩展了实际中层平台攀爬/翻越、探索宝箱、台地信标、滑翔、双石解谜、珍稀回声匣、JSON 往返和安全传送。流程仍使用测试传送和直接击败敌人缩短时间，不代表真人自然走完所有路线；攀爬和滑翔用实际 `PlayerTraversal.Simulate` 执行。截图查看确认攀爬画面、台地、滑翔风帆、体力条、中文区域名及探索地图/传送按钮存在。截图 01～09 位于上述输出目录，未纳入 Git。

环境：Intel Core Ultra 9 285H、Intel Arc 140T GPU、1920×1080、URP PC、60 FPS 上限。高台位置 300 帧离屏渲染采样：平均 16.794 ms，P95 16.671 ms，Unity 已分配内存 168.93 MB，Gen 0 GC 1 次。未观察到此短时场景的明显性能退化；这不是整段正常游玩的 CPU/GPU Profiler 验收，不能据此声称全地图稳定 60 FPS。

验证中发现并修复：翻越落点过于贴边；CharacterController 恢复位置后的历史接地标志阻止滑翔。两者都有回归测试。首次沙箱内执行 `game/Tools/Verify.ps1` 停在 Unity licensing 初始化，属环境问题，沙箱外重试成功；另修正脚本只等待 Editor 主进程，避免等待仍存活的 Hub 子进程。最终全部测试和构建通过。

仍需人工验收：镜头舒适度、坡面/台阶的连续手感、复杂网格转角、完整探索节奏与视线引导。当前地图缩略尺度下有标签密集情况，可放大查看；正式标签避让、正式美术、IK 和高质量动画不在本次占位实现中。

---

日期：2026-09-04。交付为可运行的 Windows 单人冒险占位原型，不等同于完成正式美术、真人体验或整段性能验收。

## 运行与版本

- 双击仓库根目录 `启动游戏.cmd`，或直接运行 `game/Builds/Windows/The Wandering City.exe`。
- 编辑器：Unity **6000.6.0f1**（f7f8ed4d1e24），Windows x64，Mono Development Build。
- 已解析且验证的包：URP 17.6.0、Input System 1.20.0、AI Navigation 2.0.14、Cinemachine 6.6.0（此编辑器内置）、uGUI 2.6.0、Test Framework 1.8.0。
- 依赖声明及实际解析版本提交于 `game/Packages/manifest.json` 与 `packages-lock.json`。
- 入口为 Boot → World 场景。操作、配方、存档位置与构建方法见 `game/README.md`。

## 已实现范围

| 模块 | 实现 |
| --- | --- |
| 第三人称操作 | 相对镜头移动、奔跑、跳跃、重力、CharacterController 碰撞、鼠标旋转、镜头障碍物检测；菜单暂停并释放鼠标 |
| 战斗 | 普通攻击准备/命中/收招、单次挥击去重、遮挡检测、闪避及无敌窗口、受击反馈、死亡回据点 |
| 世界 | 据点、树林、矿区、营地；固定坐标路线与地标；固定种子装饰；启动时构建静态 NavMesh |
| 敌人 | 10 名同类近战守卫，其中营地 5 名；巡逻、视线发现、追击、蓄力攻击、返回、死亡、持久化掉落 |
| 交互与奖励 | 28 处木材、20 处石材、12 处矿石、3 个探索宝箱、1 个营地星核宝箱；稳定 ID、距离及视线验证 |
| 背包与制作 | 20 格、每格 99 件、四个快捷槽、药剂及三种建筑模块配方、一次武器升级（26→42） |
| 建造 | 据点网格、地板/墙/屋顶、旋转、有效性预览、角色碰撞检查、支撑及共享墙边校验、按依赖拆除和完整回收模块 |
| 引导 | 从真实进度推导目标，支持提前探索；制作、搭建、营地奖励和升级后可继续探索 |
| UI | 中文 HUD、交互提示、背包、工作台、暂停菜单、原野地图、新旅程及继续旅程 |
| 存档 | 角色、背包、快捷槽、武器、目标标记、奖励、敌人、建筑；关键操作/定时/手动保存；临时文件原子替换及备份；未知版本保护；旧档归档 |

所有资源操作在规则层先完整校验再提交。ScriptableObject 保存战斗配置，玩家状态独立存储。地图、角色和建筑使用代码生成的几何占位模型；建筑实例按模块 ID 和网格坐标恢复，目前还不是正式美术 prefab 库。Animator 提供占位状态表现，位移由代码控制。

## 自动测试结果

最终运行 `game/Tools/Verify.ps1 -Build`：

| 验证 | 结果 | 证据（本地，不纳入 Git） |
| --- | --- | --- |
| Unity Edit Mode | **29/29 通过** | `artifacts/editmode-results.xml` |
| Unity Play Mode | **14/14 通过** | `artifacts/playmode-results.xml` |
| 原有 Demo 测试 | **8/8 通过** | `node --test tests/*.test.mjs`，在 `demo/` 执行 |
| Windows x64 构建 | **通过** | `artifacts/unity-build.log`，含 `WANDERING_CITY_BUILD_OK` |
| Windows 自动冒烟 | **通过** | `artifacts/windows-final/smoke-result.json` 与 `artifacts/windows-final.log` |

共 **51 项自动测试通过**，无跳过用例。规则测试涵盖容量/材料不足不变更、堆叠、重复奖励、一次升级、恢复道具、建筑依赖与共享边、失败回收、乱序探索、完整冒险、JSON 往返、主档损坏、临时写入中断、未知版本和非法配置。

场景测试涵盖所有交互点的可达性、真实碰撞器阻挡、近战多次检测只命中一次、隔墙不命中、闪避窗口到期、暂停输入隔离、敌人发现/追击/蓄力/返回、不可达高度不造成伤害、死亡保留进度、重复交互、营地掉落、建筑碰撞，以及卸载再创建场景后的角色和世界恢复。

Windows 冒烟使用独立存档目录，通过实际交互/制作/奖励接口完成冒险及 JSON 往返。它会传送角色、直接击败敌人来缩短流程，因此不代表真人按正常速度完成了游戏；战斗逻辑由 Play Mode 用例独立验证。运行日志未出现游戏异常或错误，结尾为 `WANDERING_CITY_SMOKE_OK`。

## 画面与性能检查

检查了真实 Windows 构建输出的主菜单、据点、背包、工作台、地图、营地、小屋画面，确认中文显示、材质与场景存在，菜单不再与 HUD 叠字。截图保存在 `artifacts/windows-final/`。

由于后台隐藏窗口的屏幕缓冲截图为黑色，自动截图改用 URP Render Request 离屏渲染；冒烟时 Canvas 转为相机空间，正常游戏仍使用屏幕叠加 UI。此检查验证场景与界面渲染，不代替前台键鼠人工试玩。

短时采样环境：Intel Core Ultra 9 285H、Intel Arc 140T GPU，Windows、URP PC 默认画质，1920×1080，300 帧显式离屏渲染，限制 60 FPS。结果：

- 平均帧间隔约 **16.667 ms**，P95 约 **16.669 ms**，约 **59.95 FPS**。
- Unity 已分配内存约 **168 MB**，采样期间 Gen 0 GC **2 次**。

这是带帧率上限的短时离屏场景采样，不能据此宣布整段游戏稳定 60 FPS。参考 PC 门槛尚未由用户确定，也尚未完成 Unity Profiler 的整段 CPU/GPU/GC 分析。

## 验收边界与后续工作

- T02～T08、T10～T13 的核心自动化场景/规则已有对应覆盖；T09 有规则及实际建筑碰撞覆盖，旋转预览和手工放置仍需真人操作确认。
- T01 已验证碰撞器阻挡并实现坡面/台阶/重力/跳跃参数；坡面、台阶、狭窄区域和镜头舒适度仍需完整人工走查。
- T14 新玩家无口头指导完成流程、探索顺序、奖励可见性，以及 **20～30 分钟实际体验时长**尚未真人验收。
- T15 已构建并完成短时离屏采样，尚未完成参考硬件上的整段正常游玩性能验收。
- 当前使用占位几何、占位动画与合成提示音，正式角色美术、动作衔接、地图节奏和音效仍需制作打磨。

## 交付检查

- Unity 自有资产和包随附资产具有 `.meta`，未发现重复 GUID。
- 大型字体由 Git LFS 管理，保留 Noto Sans SC 的 OFL 许可证；Unity 模板和 TextMeshPro 资源保留原 GUID 及许可证。
- `Library`、`Temp`、日志、构建及自动化报告不纳入 Git；可通过脚本重新验证与构建。
- 新增启动脚本的目标可执行文件存在；交付前执行 Git 格式检查并提交对应 commit。
