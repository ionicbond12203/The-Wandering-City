# Technical Debt Register

审查日期 2026-09-05；基线 `640658d`。以下均 **Open**，仅完成治理不会自动偿还技术债。负责人是单人开发者（对应职责由 Codex 在授权任务中承担）。P0 阻止相关阶段晋级，P1 必须在扩大规模前处理，P2 在生产候选前处理；不表示当前所有问题均为已复现运行故障。

| ID / 优先级 | 证据与风险 | 责任 / milestone | 关闭条件与回滚保护 |
| --- | --- | --- | --- |
| TD01 / P1 | WorldBuilder 559 行混合场景创建、材质、敌人、索引、建造/恢复；GameSession 接线兼输入/存档；GameHud 创建全 UI 并直接访问状态 | Game Engineering / G04–G09 | 单一写入者/适配器/行为回归；旧调用和同 ID 可回退，不能只按行数拆文件 |
| TD02 / P1 | 一个 WanderingCity runtime asmdef；Core 的 SaveStore/ExplorationRules 依赖 Gameplay ExpansionCatalog，目录不是边界 | Technical Director / G04、G09 | 核心规则无场景依赖、依赖方向有测试/编译约束；渐进迁移 |
| TD03 / P1 | WorldBuilder、ExplorationWorld、ExpansionCatalog 硬编码位置，Rules 硬编码物品/配方/建造范围；WorldMapData 从 live Interactions 找目标 | Content / G03、G05、G09 | 已有内容定义和 ID 清单、空间查询/目标目录；旧坐标/奖励不变；不用扩大允许范围掩盖错误 |
| TD04 / P1 | Boot.LoadScene 同步单 World，全量环境/玩法驻留；GroundY 以单 Terrain 为主；无 chunk cancel/pin/unload | Open World / G06 | 两块试点生命周期、地形边界与 30 次往返证据；保留单场景回滚 |
| TD05 / P1 | Resources.Load 分布于 Session、WorldBuilder、CharacterVisualAdapter、AudioDirector、WeatherDirector、GameHud、WorldMapData | Asset Management / G05 | provider 内兼容加载、依赖/释放预算和失败测试；不一次删除 Resources |
| TD06 / P1 | PlayableNavigation 启动同步 BuildNavMeshData，移除场景 surface，单张区域/道路掩码；EnemyAgent.Return 每帧 SetDestination | Navigation / G07 | 烘焙连通/分块注销/请求预算与前后 profiling，原可达性不降低 |
| TD07 / P1 | EnemyAgent 每实体 Update，90m 仅停 agent；AudioDirector.Tick 与 HUD 每帧遍历敌人；缺调度/休眠实体预算 | AI / G08 | Near/Mid/Far、预算公平性与事件查询，真实 35 名与合成负载分别证明；近战行为不退化 |
| TD08 / P0（兼容） | SaveStore.Validate 引用活动世界目录、解谜/奖励和建造规则；SafePosition 引用地形/玩法范围；目录变化可使旧档无效 | Save / G03、G04 | 历史夹具、退役/alias、卸载对象快照、原子/备份/未知版本保护；不能忽略未知 ID 强行绿色 |
| TD09 / P1 | EnvironmentAuthoring 1056 行，Prepare 修改 World/环境 prefab 并重贴地；历史存在序列化 churn；部分生成已有版本保护但无全面所有权证明 | Tools / G02 | 手工 sentinel/自定义资源/GUID 保留、两次生成比较、显式 source/generated manifest；回滚只及生成输出 |
| TD10 / P1 | 运行时创建敌人、宝箱、建筑、mesh/material 和合成 AudioClip；正式静态生产内容/占位路径混合 | Technical Art / G05、G11 | 生产资产离线输入/烘焙、预算和 owner，placeholder 明确；保留调试 fallback |
| TD11 / P1 | 已有 LOD/instancing/SRP Batcher，但无 HLOD、统一 texture/shadow/驻留预算；材质实例与动态字体 atlas 的长期成本未测 | Rendering / G01、G10 | 原生 Build 对照、有效 draw/batch/GPU/显存及预算；LOD 不改变碰撞 |
| TD12 / P1 | GameHud.Update 每帧 LINQ/string.Join/文本赋值和全部敌人标签；地图每次根据已加载对象查询目标 | UI / G09 | 只读投影/事件更新与 GC 前后数据，六种分辨率/目标卸载测试 |
| TD13 / P0（质量） | BuildSmoke 有 ProfilerRecorder/FrameTiming，但短采样、等待时间、缺失 draw/batch 与不同捕获模式混用；无完整参考硬件路线/soak | QA / G01、G13 | 冻结硬件/预算、三次前台路线及长时 trace，缺失指标不能达标 |
| TD14 / P1 | Verify.ps1 依赖固定本机 Unity 路径，报告使用固定文件名；未显式隔离每次 run/检查过期结果或零/跳过用例；没有仓库 CI，Build 只做 Development | Build QA / G01、G12 | 唯一报告/失败封闭/退出码、Release 和 clean checkout + LFS 重现；保留失败日志 |
| TD15 / P0（发布） | docs/开放世界内容扩展记录.md 记录一次保存 QA 失败未定位，仅最终重跑成功；当前未知是否仍可复现 | QA + Save / G04、G13 | 找回失败证据、可复现/根因或有证据的环境归因、补回归；重跑通过不能直接关闭 |
| TD16 / P2 | AtmosphereAuthoring 以 Unity 内部 API 反射生成 Mixer；历史构建警告 Rock/Cliff 碰撞预烘焙未来版本要求 | Tools / G02、G12 | 引擎升级独立验证 Mixer 生成/碰撞；明确支持版本，保留锁定工具链 |
| TD17 / P1 | QA 证据多数在忽略的 artifacts，仅本机；不同历史 HEAD 的测试数量/截图可能被误当最新结果 | QA / G01、G12 | source/build hash manifest、失败保存、可持久导出的证据归档；历史结论保留日期 |

## Art blockers

以下不是靠通过逻辑测试可关闭的 bug。负责人为 Technical Artist / Audio，目标 G11（发行范围内全部阻塞在 G13 前关闭）。本次不新增或替换资产。

| ID | 缺少的正式成果 / 当前 placeholder | 已有接入点 / production pipeline | 关闭证据 |
| --- | --- | --- | --- |
| ART01 | 正式旅人模型、蒙皮、Humanoid Avatar；当前为刚性分段原创网格 | CharacterPresentationSettings.CharacterPrefabSlot → CharacterVisualAdapter → CharacterRigBindings；源模型单位/轴向/许可/导入规范见角色记录 | 合法源文件、Avatar/蒙皮与 collider 分离测试、Build 近景与动作审查 |
| ART02 | 配套动作、转向/足底接触、重定向品质；当前生成动画和有限 secondary motion | CharacterAnimationSet 的 17 个 clips → controller/driver；保持 gameplay timing，后续 IK 按测量引入 | 动画完整性、坡面/攀爬/滑翔/战斗审片，无明显滑步/穿插，性能预算 |
| ART03 | 正式配乐、完整声音设计与混音；当前音乐空槽、合成 ambience/SFX | WorldAudioLibrary playlists + WorldAudioMixer；合法源 → 响度/循环/导入 → Build 试听 | 许可证、真实曲目和淡化测试、人工试听/混音审查；非零峰值不足以验收 |
| ART04 | 正式敌人、宝箱、建筑、地标及环境纹理；当前 primitive/原创程序网格与占位纹理 | 先 G02/G05 的 source/generated/prefab/LOD 管线，再在 G11 既有路线接入 | 来源、材质/LOD/collider 预算、原始 Build 图与艺术验收，不以 formalMode 名称替代 |

## 更新与关闭规则

每次相关工作记录 ID、日期、复现、影响、证据、状态和 commit；关闭需要可复现验证，不以“已经拆分”或“测试更绿”关闭。架构计划见 [系统边界](AAA_ENGINEERING.md)，依赖与验收见 [路线图](AAA_ROADMAP.md)。本次只新增登记，不声称修复上述运行时风险。
