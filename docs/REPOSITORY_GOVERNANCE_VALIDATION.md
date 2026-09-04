# G00 Repository Governance Validation

日期：2026-09-05（Asia/Kuala_Lumpur）。源基线 `640658dac4ffc6a36392bfc620bfe5f65807bcae` 加本次治理文档、README 与 RepositoryGovernanceTests 工作区差异；对应交付为包含本记录的独立 Git commit。无运行时代码、Unity/包、渲染配置、Stable ID、Save Schema 或内容变更。

## 实际结果

| 验证 | 命令 / 结果 | 本地证据 |
| --- | --- | --- |
| Unity EditMode | `game/Tools/Verify.ps1 -Build`：95/95 Passed，failed=0，skipped=0 | artifacts/governance-20260905/editmode-results.xml、unity-editmode.log |
| Unity PlayMode | 同一命令：38/38 Passed，failed=0，skipped=0 | artifacts/governance-20260905/playmode-results.xml、unity-playmode.log |
| Windows x64 | 同一命令：退出码 0；`WANDERING_CITY_BUILD_OK 212160322` | artifacts/governance-20260905/unity-build.log；game/Builds/Windows/ |
| Demo 回归 | 在 demo 执行 `node --test tests/*.test.mjs`：8/8，0 failed/skipped | 本次终端结果 |
| 生成资产检查 | 23 文件、前后均 1912 序列化节点，32 轮图指纹对照全部一致 | artifacts/governance-20260905/generated-comparison.json、check_generated.py、generated-backup/ |

Unity 共 133 项通过，连同 Demo 为 141 项。新增 11 项 EditMode 契约检查治理文档/本地链接、20 个系统边界、milestone 必需字段和前序依赖，包含空验收、缺字段、重复字段、前向依赖、自循环、重复 ID、断开依赖的反例。它们只保护文档结构，不认证实际系统实现或艺术品质。

EditMode UTC：2026-09-04 16:23:41–16:24:51；PlayMode UTC：16:25:02–16:25:43。Unity 6000.6.0f1，URP 17.6.0，Windows x64 Mono Development Build。此次没有运行 Release、Build 自动冒烟、人工试玩、截图或 profiling；本项无视觉/玩法改动，不提出这些方面的新验收结论。

## 失败历史和构建限制

首次沙箱执行在许可初始化停滞，日志有 `LicenseClient-IONIC refused` 与 licensing 未初始化；未执行测试，保留为 `attempt-1-editmode.log`。停止本任务启动的 Unity 后，通过获准的沙箱外完整重跑获得上述结果；没有使用旧 XML 宣称成功。旧 Edit/Play XML 及旧 Play/Build 日志另留 previous-reports。

构建有 Unity 包 TextMeshPro 的 deprecated shader pragma 警告，以及 Unity 云服务连接超时（Curl 28）；构建仍明确成功。未修改第三方包来消除日志。此结果不是“全部日志零警告”声明。

验证生成了空白序列化和 prefab fileID 变化；先备份再规范化尾空白、局部和关联 prefab 引用做图指纹比较。构建进行时一次 ProjectSettings 比较不同，保留 during-build-comparison；构建退出后全部对照一致。图指纹为序列化回归检查，不是形式化图同构证明。确认任务开始时所有 tracked 文件干净、备份 SHA256 与当前文件一致后，仅恢复这 23 个生成文件；不夹带资产/设置 churn。

自动审批首次因担心覆盖用户已有改动拒绝恢复；补充初始 clean tracked 状态与逐文件备份检查后，明确路径的恢复获准并完成。用户已有五份未跟踪 04–08 milestone 文档原样保留，不提交、不删除。

## 证据校验和

SHA256：

| 文件 | Hash |
| --- | --- |
| editmode-results.xml | B312DA5CB75D20F19B71DDD7B38AA9C459F0F9244E3565C1BCA78FD9B3BD117C |
| playmode-results.xml | 730CCBF7C16F56865B4FAACB0AC12852BF884424CD72BFEDD0AAD63FD05514C8 |
| unity-build.log | D7DF204CE7D5B47CE23A00588DE4667F3240CABC27C68520725B2E3E2A471A8B |
| The Wandering City.exe | 718444F6A718FA668DA96EB0A58445B009673310E6129152A63DBF7A92B8DE00 |
| The Wandering City_Data/Managed/WanderingCity.dll | 6AFE604EB2556D6E1EB4500A100447D2AB086EAF6A99DC5140E63874D5C4F65F |

完整构建文件清单、工具链与治理源文件哈希保存在同目录 evidence-manifest.json。exe 哈希单独不能标识整个 Unity 游戏构建，必须连同 Data/Managed 和完整文件清单使用。artifacts/Builds 仍不纳入 Git；完整证据仅本机保存，长期远端归档仍是 TD17，未声称完成。

## 修改与剩余风险

更新 AGENTS.md、game/README.md；新增 AAA_VISION、AAA_ENGINEERING、AAA_QUALITY_BAR、AAA_ROADMAP、TECH_DEBT、REPOSITORY_ARCHITECTURE_ASSESSMENT、本记录；新增 RepositoryGovernanceTests.cs 及 `.meta`。G00 交付只建立长期框架；G01–G13 保持 Planned，不启动下一项。

当前仍为 Prototype。WorldBuilder/Session/HUD 多职责、硬编码目录、Resources、单场景加载、同步导航、AI 规模、Save/World State 耦合、正式美术/音频和长时性能/发布证据缺口均未在本次修复；完整责任与退出条件见 [技术债](TECH_DEBT.md)。
