# The Wandering City — Open World Visual Rebuild for Codex

你现在是本项目的 Lead Unity Open-World Engineer、Senior Technical Artist、Rendering Engineer 和 Environment Designer。

仓库：`ionicbond12203/The-Wandering-City`

目标分支：`main`

开始工作前先确认当前 HEAD。当前用户侧已知基线提交为：

`e00a841a0effd6161d1fef74a670f2f108f03c99`
`fix(render): resolve terrain and skybox material bugs and add visual QA render health checks`

如果仓库 HEAD 已更新，以实际 HEAD 为准，不要回退用户已有修复。

---

# 0. 任务目标

上一阶段已经修复 Unity magenta / pink shader failure，但当前画面仍然明显是“程序员原型”，而不是开放世界。

用户当前实际 Build 的主要问题：

- 玩家像在一个巨大绿色箱子/盆地里行走
- 地表核心区域几乎完全平
- 四周地形像垂直绿色墙
- 地平线被封死，没有真正的开放世界尺度感
- 树木仍然像 Cylinder + Sphere 组件拼装
- 石头和悬崖仍然像 Cube
- 草地主要只是绿色 Terrain Texture，不是真正密集植被
- 当前玩家仍然是明显的 primitive placeholder
- 天空和太阳像 Unity 默认组件
- 光影层次很弱
- 前景 / 中景 / 远景缺少明确层次
- HUD 仍然偏开发调试风格
- 整体仍然缺乏“看到远处地标并想走过去”的开放世界构图

本次目标不是增加更多 gameplay feature。

本次目标是：

# Milestone — Open World Topology & Visual Identity Rebuild

把项目从：

`flat prototype inside a box`

升级为：

`open, layered, stylized anime-inspired outdoor world vertical slice`

参考的是高品质二次元开放世界的设计原则，包括：

- 大尺度视野
- 起伏地形
- 山谷 / 山脊 / 悬崖
- 草地与植被密度
- 远景山体
- 云层与天空
- 自然光照
- 明确视觉地标
- 角色与环境比例
- 前景 / 中景 / 远景层次

禁止复制任何商业游戏的地图、角色、模型、Shader source、Texture、UI 图标、动画、音乐、剧情或专有名称。

必须保持 The Wandering City 原创。

---

# 1. Repository Assessment — 必须先做

修改代码前必须阅读：

- `AGENTS.md`
- `game/README.md`
- `docs/`
- `game/Assets/_Game/Editor/EnvironmentAuthoring.cs`
- `game/Assets/_Game/Editor/ProjectBuilder.cs`
- `game/Assets/_Game/Scripts/Gameplay/WorldBuilder.cs`
- `game/Assets/_Game/Scripts/Gameplay/ExplorationWorld.cs`
- `game/Assets/_Game/Scripts/Gameplay/CharacterVisualAdapter.cs`
- `game/Assets/_Game/Scripts/Gameplay/OrbitCamera.cs`
- `game/Assets/_Game/Scripts/UI/GameHud.cs`
- `game/Assets/_Game/Art/Shaders/`
- `game/Assets/_Game/Art/Materials/`
- `game/Assets/Settings/PC_RPAsset.asset`

先输出简洁：

## Repository Assessment

必须明确指出：

1. 为什么当前世界像一个“大箱子”
2. 哪些 primitive 仍然出现在 Formal Visual Mode
3. 当前 Terrain detail / tree pipeline 是否真的在使用 Terrain Detail / Terrain Tree system
4. 当前 post-processing 是否明确启用在 Main Camera
5. 当前 sky / sun / lighting 使用的具体机制
6. 当前哪些 gameplay object 已经使用 GroundY
7. 本次会修改哪些文件

然后直接实施，不等待确认。

---

# 2. 已确认的核心问题：不要重新误判

当前 `EnvironmentAuthoring.CalculateHeight()` 存在一个矩形低地走廊：

- X approximately `-78 .. 92`
- Z approximately `-32 .. 162`

这个主要 gameplay corridor 的高度变化目前只有约：

`0 .. 0.03m`

然后矩形外侧地形快速上升到约 `78m + noise`。

结果就是：

- 中央像桌面
- 四周像墙
- 玩家像被关在绿色盒子里

这不是开放世界。

本次必须彻底改变这个 topology。

---

# 3. Open World Terrain Rebuild

不要继续使用“矩形平坦核心 + 四周统一抬升”的算法。

重新设计 Terrain。

推荐目标：

```text
Terrain size:
1024m × 1024m

Terrain height:
160m

Heightmap resolution:
513 minimum
1025 if performance / asset size allows
```

当前 gameplay POI 可以继续位于地图中央区域，但整个地图必须拥有自然的地形连续性。

## Required terrain regions

### Spawn Meadow
约 0–6m 起伏。

不能完全平。

需要：

- gentle rolling hills
- shallow depressions
- small ridges
- visible terrain undulation from third-person camera

### Forest
约 6–20m。

需要：

- rolling wooded hills
- small gullies
- uneven ground
- tree clusters on multiple elevations

### Quarry
约 12–35m。

需要：

- exposed rock
- natural cliffs
- terraced elevations
- climbable shortcut
- high viewpoint

### Ruins
约 20–48m。

需要：

- plateau
- broken slopes
- elevated approach
- distant visibility

### Valley / Stream Corridor
至少设计一条低地自然 corridor：

- shallow valley
- dry creek / stream route 或 equivalent
- 将不同区域自然连接

### Outer Highlands
约 35–80m。

### Distant Peaks
约 70–140m。

---

# 4. 绝对禁止再次做“地形围墙”

不要：

```text
rectangular boundary
continuous vertical ridge around the map
four-sided mountain wall
visible terrain edge
giant cube boundary
```

Terrain 边缘必须：

- 不规则
- 非对称
- 有缺口
- 有远方 valley opening
- 有多个不同距离层级的山体轮廓

从 Spawn 朝 North / East / South / West 四个方向看，至少三个方向必须能看到：

```text
sky + horizon + distant landscape
```

而不是整面绿色墙。

---

# 5. Horizon Architecture

建立真正的远景系统。

不要靠 Terrain 边界挡住玩家。

创建：

```text
Environment/
  Terrain/
  Midground/
  DistantLandscape/
```

DistantLandscape 包含：

- distant mountain silhouettes
- distant forest masses
- distant cliffs
- optional ruins silhouette

这些对象：

- 不需要 gameplay collider
- 可以 low poly
- 使用 LOD
- 不投高成本阴影
- 通过 fog / aerial perspective 融入天空

主 Terrain 边缘应该被远景自然遮挡，而不是做成墙。

Main Camera far clip 应从当前约 320m 调整到适合新世界尺度：

建议 `900–1400m`。

实际值通过视觉和性能 QA 决定。

---

# 6. Terrain Height Generation Architecture

不要继续把所有 terrain generation 塞在一个 `CalculateHeight()` if/else 中。

拆成可测试的 deterministic height model，例如：

```csharp
TerrainHeightModel.Sample(worldX, worldZ)
```

组合：

- BaseRollingNoise
- RidgeNoise
- ValleyMask
- ForestFeature
- QuarryFeature
- RuinsPlateauFeature
- DistantMountainFeature
- RoadSofteningMask
- SpawnSafetyMask

推荐使用多频 noise + feature masks。

不要只用单层 Perlin Noise。

需要：

- broad macro shape
- medium terrain forms
- subtle micro variation

不要让噪声本身决定地图设计。

必须保证 POI 路线是人为设计过的。

---

# 7. Preserve Gameplay Through Ground Anchors

Stable IDs、save schema、existing gameplay 不得破坏。

继续使用 `GroundY()` / `Terrain.SampleHeight()`。

但是不要到处写绝对 Y。

把重要位置逐步统一为：

```text
world X/Z
+
terrain ground
+
local offset
```

例如：

```csharp
GroundPoint(x, z, localHeight)
```

重点检查：

- PlayerSpawn
- Teleport
- Treasure
- POI
- Enemy camp
- Resources
- Workbench
- Building area
- ExplorationWorld mesa
- shelf
- canyon
- puzzle nodes
- BuildSmoke QA positions

所有对象 Terrain 改变后必须继续正确贴地。

---

# 8. REAL Grass Coverage

当前不要再只放几十个 `Grass_Tuft_01` prefab。

正式场景必须使用 Unity Terrain Detail system。

至少：

```text
GrassShort
GrassTall
FlowerDetail
SmallPlant
```

使用：

`Instanced Mesh + GPU Instancing`

创建 deterministic density maps。

草密度需要覆盖主要 meadow / forest clearing。

排除：

- roads
- building plot
- steep cliffs
- water/stream
- POI interaction footprint

目标：

玩家附近 0–40m 范围内，地表不能看起来只是一张绿色 texture。

Grass Shader 保留：

- alpha clipping
- wind sway
- root-tip gradient
- color variation

并确保：

- shader compiler error = 0
- GPU instancing works

---

# 9. Trees Must Stop Looking Like Sphere Components

当前 `Tree_Stylized_01` 是：

`Cylinder trunk + several Sphere canopy`

这只能作为 placeholder。

本次 Formal Visual Mode 至少创建 3 个原创树形变体：

```text
Tree_Stylized_A
Tree_Stylized_B
Tree_Stylized_C
```

不要直接使用 Unity Sphere/Cylinder 作为最终可见 mesh。

如果仓库没有外部合法 art asset：

使用 Editor Mesh generation 创建原创 low-poly mesh：

- irregular tapered trunk
- slight trunk bend
- asymmetric branch directions
- custom low-poly canopy lobes
- non-spherical silhouette
- vertex color variation

树不能全部同高、同冠形、同绿色、同旋转。

使用：

- random scale
- random yaw
- variant selection
- small hue/value variation

加入 `LODGroup`。

优先使用 Terrain Tree system 或可批处理 / instanced 的方案。

---

# 10. Rocks and Cliffs

当前 Cube cliff / rotated cube rock 不再允许作为 Formal Visual Mode 的主要景观。

创建原创 low-poly rock mesh generator 或 authored mesh assets：

至少：

```text
Rock_A
Rock_B
Rock_C

Cliff_A
Cliff_B
Cliff_C
```

特征：

- faceted irregular silhouette
- non-orthogonal surfaces
- no obvious cube outline
- reusable modular pieces
- climbable surfaces where appropriate

Cliff material 使用：

- triplanar / world-space mapping 或等价方式
- slope-aware color variation
- subtle normal detail

避免 UV 拉伸造成“绿色/岩石垂直条纹墙”。

---

# 11. Terrain Materials

当前 Terrain 需要减少：

- repeating streaks
- stretched texture
- uniform bright green

至少保留：

- Grass
- DryGrass
- Dirt
- Rock

重新调整 splat rules：

Grass:
gentle slope / low-mid altitude

DryGrass:
sun-exposed / mid-high altitude

Dirt:
roads / POI / erosion / valley

Rock:
steep slopes

加入 macro variation：

- large-scale color noise
- subtle slope tint
- altitude tint

不能看起来像 256×256 texture 被重复拉满整个地图。

---

# 12. Sky — 不再使用默认组件感 Procedural Sky

当前 `OutdoorSky.mat` 使用 `Skybox/Procedural`。

本次正式视觉不要继续把它作为最终天空。

创建项目原创：

`WanderingCity/StylizedSky`

推荐自定义 Skybox Shader 或 Sky Dome。

必须至少支持：

- zenith color
- horizon color
- horizon haze
- sun disc aligned with Directional Light
- soft sun halo
- procedural stylized clouds
- cloud movement
- cloud coverage
- cloud softness
- exposure

太阳不能再像“白色 Unity 圆盘”。

视觉太阳和 Directional Light 必须方向一致。

云层必须有：

- 大尺度形状
- 明暗层次
- 缓慢运动

不要追求写实 volumetric cloud。

目标是 stylized anime sky。

---

# 13. Lighting Rebuild

保留 URP。

不要迁移 HDRP。

当前仅 `Directional Light + Trilight Ambient + Fog` 层次不够。

必须：

1. 明确启用 Main Camera URP Post Processing
2. 使用 Global Volume
3. 检查 SSAO
4. 调整 shadow distance
5. 调整 cascade distribution
6. 调整 ambient / shadow tint
7. 使用 atmosphere fog

检查：

```csharp
camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
```

或 Unity 6 等价 API。

当前 URP shadow distance 大约 50m。

对这个 1km vertical slice：

建议起点：

`80–120m`

但必须通过性能验证。

禁止把 Shadow Distance 直接设 500m。

---

# 14. Optional Indirect Lighting

如果在当前自动化环境中可以可靠 bake：

考虑 Unity 6 URP Adaptive Probe Volumes。

用于：

- character indirect lighting
- trees
- cliffs
- ruins

如果不能稳定自动 bake：

不要让 APV 阻塞本阶段。

明确记录为 future enhancement。

---

# 15. Atmosphere / Depth

开放世界必须有空气透视。

近景：

- contrast high
- saturation normal/high
- sharp

中景：

- moderate contrast

远景：

- lower contrast
- slightly desaturated
- blend toward sky horizon

不要让远处山体和玩家脚下草地拥有相同对比度。

Fog 参数必须配合新的 1km world scale 重调。

---

# 16. Character Visual

当前玩家仍然明显由 Cylinder / Sphere / Cube 组成。

这不是正式 character art。

## Rule

如果仓库里没有合法 Humanoid / SkinnedMesh character asset：

不要谎称“二次元角色完成”。

不要从网络随意下载版权来源不明模型。

必须保留：

`CharacterVisualAdapter`

以及：

`CharacterPrefabSlot`

让真实角色以后可直接替换。

## 本阶段允许的改进

可以把当前 placeholder 改成更有设计感的原创 low-poly traveler：

- clear head/body/arm/leg silhouette
- cloak / scarf
- boots
- asymmetrical travel pack
- weapon socket
- glider socket
- better proportions
- visible front/back distinction

但是：

Formal Visual Mode 不允许继续显示一个单圆柱身体 + 球头作为最终结果。

如果无法创建合理 humanoid mesh：

宁可清晰报告：

`Character art blocked by missing authored humanoid asset`

也不要浪费大量代码假装 AAA character。

---

# 17. Camera Composition

保留 Cinemachine。

优化：

- default distance
- shoulder / vertical framing
- pitch clamp
- collision
- FOV
- far clip
- glide camera

目标：

正常地面探索时角色约占屏幕高度：

`30–40%`

同时让玩家能看到：

- foreground
- midground landmark
- horizon

不要让角色过大挡住环境。

---

# 18. HUD

本任务不是 UI overhaul，但截图里开发 HUD 仍过重。

正常 gameplay 默认关闭：

- permanent control list
- debug text
- oversized opaque panels

保留：

- objective
- HP
- stamina
- minimap placeholder
- interaction prompt
- hotbar

开发调试 UI 放到：

`DevelopmentVisualMode`

或 debug toggle。

---

# 19. Formal Visual Mode 必须停止大规模 GameObject.CreatePrimitive

允许 primitive：

- debug probes
- collider-only geometry
- automated test fallback
- temporary gizmo
- DevelopmentVisualMode

Formal Visual Mode 的主要可见对象不允许继续依赖：

```text
Sphere tree
Cube rock
Cube cliff
Cylinder character
Cube mountain wall
```

新增测试 / audit：

扫描 Formal environment hierarchy。

报告任何明显的 placeholder primitive usage。

不要错误禁止 gameplay collision primitives。
只约束正式可见 Renderers。

---

# 20. Open World Composition Rules

从出生点必须能看到至少：

- 1 个 150m+ 中远景 landmark
- 1 个 elevated terrain feature
- 1 个 forest mass
- 1 个 distant mountain / ridge
- 大片天空

世界构图使用：

Foreground → Midground → Background

例如：

```text
foreground grass + rock
→ player
→ rolling meadow
→ tree cluster
→ cliff / ruins
→ distant ridge
→ sky / clouds
```

必须避免：

```text
flat floor
→ tree row
→ green wall
```

---

# 21. Exploration Route Design

在新 Terrain 上保留现有 gameplay loop，但重新组织视觉路线。

至少设计：

### Route A — Safe
Spawn → meadow → forest → quarry

### Route B — Vertical shortcut
Spawn → ridge → climb → high viewpoint → glide

### Route C — Discovery detour
main path → visual landmark → hidden valley / ruins → treasure

玩家在高处必须能看到下一个目标。

---

# 22. Performance

目标：

Windows PC
1920×1080
60 FPS baseline

不能通过堆数万个 GameObject 实现草地。

使用：

- Terrain Details
- GPU Instancing
- shared materials
- LODGroup
- Terrain trees
- culling
- SRP Batcher
- reasonable shadow distance

检查：

- draw calls
- batches
- GC allocations
- CPU main thread
- GPU frame time

不要在 runtime 每帧生成环境。

---

# 23. Visual QA Screenshots

实际启动游戏 Build。

固定生成：

```text
artifacts/visual/
  open-world-spawn.png
  open-world-north.png
  open-world-east.png
  open-world-south.png
  open-world-west.png
  open-world-forest.png
  open-world-quarry.png
  open-world-high-viewpoint.png
  open-world-glide.png
  character-close.png
```

这些必须是真实游戏 camera screenshot。

不是 Scene View。

---

# 24. Visual QA Acceptance

以下情况视为失败：

- 仍能看到巨大四边围墙
- horizon 被四周连续 terrain wall 封死
- 主 gameplay area 高度差仍小于约 10m
- grass 主要只是一张地面 texture
- Formal mode 树仍然全部是 sphere canopy
- Formal mode cliff 仍明显是 giant cube
- 天空仍只有纯蓝 gradient + 白圆太阳
- Main Camera post processing 未启用
- screenshot 有 magenta shader error
- character 仍然只有 cylinder body + sphere head，却被报告成正式角色
- existing save / exploration / combat 被破坏

---

# 25. Automated Tests

保留现有全部测试。

新增 / 更新：

## Terrain_IsNotFlatInGameplayCore

在 gameplay core 网格采样。

要求：

- max - min elevation >= 18m
- 不能只测试 perimeter mountain

## Terrain_HasNoRectangularWallProfile

使用多个 radial / directional sample。

验证地形不是在某个矩形边界后统一急升。

该测试不要求视觉完美，但应防止重新出现：

`flat rectangle + wall`

## TerrainDetails_ArePopulated

验证：

- `detailPrototypes.Length >= 2`
- 至少一个 grass detail layer 有大量非零 cells

## Trees_HaveVariants

至少 3 个 tree visual variants。

## Camera_PostProcessingEnabled

真实 Main Camera：

- URP Additional Camera Data exists
- post processing enabled

## Sky_IsProjectStylizedSky

正式模式不再使用：

`Skybox/Procedural`

作为最终视觉天空。

## FormalVisual_NoErrorShader

保留现有 render-health checks。

## FormalEnvironment_NoBoundaryCubes

不得出现正式视觉：

West boundary
East boundary
North boundary
South boundary
或等价巨型墙体 renderer。

---

# 26. Build Smoke

更新 BuildSmoke 的 QA coordinates。

所有位置必须适配新 Terrain。

不要硬编码旧 absolute Y。

使用：

- World anchor
- GroundY
- POI transform
- authored QA marker

完成 Build Smoke 后：

截图也必须通过 magenta threshold。

---

# 27. Terrain Regeneration Safety

Terrain generation 继续版本化。

不要在每次：

`Prepare`
`Verify`
`Build`

中删除手工修改 Terrain。

如果需要本次升级：

提升 TerrainDataVersion。

仅执行一次 migration。

Prepare 必须保持 idempotent。

---

# 28. Implementation Order

严格按照：

## Phase A — topology
- Terrain size
- terrain height model
- remove box walls
- horizon opening
- gameplay grounding

编译 + 测试。

## Phase B — horizon
- distant landscape
- far clip
- fog
- landmarks

编译 + screenshot。

## Phase C — vegetation
- real Terrain grass details
- tree variants
- rock/cliff variants
- LOD / instancing

编译 + screenshot。

## Phase D — sky + lighting
- custom stylized sky
- sun alignment
- clouds
- post processing
- shadows

编译 + screenshot。

## Phase E — character
- adapter preservation
- improved placeholder or legal humanoid integration
- clearly report asset limitation

## Phase F — HUD + camera polish

## Phase G — full test/build/screenshot QA

不要一次写几千行以后才第一次编译。

每个 Phase 完成后先让 Unity compile clean。

---

# 29. Verification

必须实际运行：

```powershell
game/Tools/Verify.ps1
```

然后：

```powershell
game/Tools/Verify.ps1 -Build
```

如果 Build 可运行：

执行 Build Smoke / visual screenshots。

不允许仅凭 NUnit pass 声称视觉通过。

---

# 30. Git

遵守 `AGENTS.md`。

推荐独立 commit：

```text
feat: rebuild open world topology and environment visuals
```

如果拆分：

```text
feat: rebuild open world terrain topology
feat: add instanced vegetation and distant landscape
feat: add stylized sky and outdoor lighting
feat: improve formal character visual pipeline
test: add open world visual regression checks
```

每个 commit 必须可编译。

---

# 31. Final Report

最后只报告真实结果：

## Root Problems Found

## Open World Topology
- Terrain size
- elevation range
- horizon strategy

## Vegetation
- grass detail counts / mechanism
- tree variants
- LOD / instancing

## Sky & Lighting
- sky shader
- cloud solution
- sun
- post processing
- shadows

## Character
- what is real
- what remains placeholder

## Camera & HUD

## Performance

## Tests
实际运行命令和结果。

## Visual QA
列出截图路径。

## Known Limitations

## Git
commit hash
commit message
git status

禁止写：

“应该可以”
“理论上”
“看起来已经接近 AAA”

只报告真实验证。
