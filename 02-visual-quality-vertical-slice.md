# The Wandering City — Visual Quality Vertical Slice

你是本项目的 Senior Unity Rendering Engineer、Technical Artist 和 Environment Engineer。

目标：

把当前程序生成 Graybox 画面升级成具有高品质 Stylized Anime Open World 视觉方向的原创 Vertical Slice。

参考目标是用户提供的 reference screenshot 所体现的：

- 明亮自然光照
- 大面积自然地形
- 草地
- 灌木
- 岩壁
- 树木
- 远景山体
- 二次元风格角色
- 清晰景深层次
- 简洁 HUD

注意：

不要复制任何第三方游戏的：

- 模型
- Texture
- Shader source
- UI 图标
- 地图
- Character
- Animation
- Music
- Asset

只研究其视觉设计原则。

所有新增内容必须保持 The Wandering City 原创。

---

## 0. Repository Assessment

修改前阅读：

- `AGENTS.md`
- `game/README.md`
- `docs/`
- `game/Assets/_Game/`
- `game/Assets/Settings/`
- `game/ProjectSettings/`
- `game/Packages/`

重点检查：

- `WorldBuilder.cs`
- `PlayerMotor.cs`
- `PlayerTraversal.cs`
- `OrbitCamera.cs`
- `GameHud.cs`
- `PC_RPAsset.asset`
- `PC_Renderer.asset`
- `DefaultVolumeProfile.asset`

先输出 Repository Assessment。

然后直接实施。

---

# 1. Critical Architecture Change

当前 WorldBuilder 使用：

```csharp
GameObject.CreatePrimitive()
```

创建：

- Terrain
- Trees
- Rocks
- Landmarks
- Player visuals

这只能作为 Graybox。

不要继续使用 Primitive 构建正式视觉世界。

重构 `WorldBuilder`，使其主要负责 Gameplay bootstrap：

- enemies
- interactables
- resources
- chests
- POI
- teleport
- persistent IDs
- NavMesh integration

正式环境放入 Unity `World` scene。

不要删除现有玩法。

---

# 2. Authored Environment

建立：

```text
Assets/_Game/Art/
Assets/_Game/Art/Environment/
Assets/_Game/Art/Vegetation/
Assets/_Game/Art/Rocks/
Assets/_Game/Art/Characters/
Assets/_Game/Art/Materials/
Assets/_Game/Art/Shaders/
Assets/_Game/Prefabs/
Assets/_Game/Prefabs/Environment/
```

`World.unity` 中建立：

```text
Environment
Gameplay
Lighting
Navigation
SpawnPoints
```

Environment 包含：

```text
Terrain
Cliffs
Vegetation
Roads
Landmarks
```

---

# 3. Terrain

用 Unity Terrain 替换当前 Meadow Cube。

第一阶段地图尺寸：

约 `500 × 500` 米。

必须具有明显高度差。

```text
Lowland:
0–15m

Midland:
15–40m

Highland:
40–80m

Landmark:
80m+
```

设计：

- meadow
- valley
- cliff
- hill
- elevated viewpoint
- glide route

出生点必须至少看到三个 visual landmarks。

不要复制任何已有商业游戏地图。

---

# 4. Terrain Materials

创建原创 Terrain Layers：

- Grass
- DryGrass
- Dirt
- Rock

允许程序生成简单占位 Texture，但所有 Asset 必须明确为项目原创 placeholder。

使用：

- slope
- height
- manual painting

组织 Terrain Layer。

悬崖区域不能只是绿色 Terrain 拉高。

需要独立 cliff mesh / rock modules。

---

# 5. Vegetation Pipeline

建立可扩展 vegetation prefab pipeline。

需要：

- Grass
- TallGrass
- Bush
- Flower
- Tree
- SmallRock

支持：

- GPU Instancing
- LODGroup
- distance culling

Terrain Detail 优先使用 Instanced Mesh。

避免：

每棵草一个 `MonoBehaviour Update`。

加入：

- random scale
- random rotation
- small color variation

目标：

玩家附近地面不再出现大块裸露纯色区域。

---

# 6. Stylized URP Shader

实现原创：

```text
StylizedEnvironment.shader
```

或 Shader Graph equivalent。

要求：

URP compatible。

支持：

- Base Map
- Base Color
- Normal
- Smoothness
- AO
- Shadow Tint
- Rim Light
- Lighting Ramp

不要做极硬 Two Tone Toon。

目标：

保留 PBR 空间感，但压缩明暗梯度，形成明亮的 stylized anime environment。

所有 Material 参数可调。

---

# 7. Grass Shader

Grass shader 支持：

- alpha clipping
- wind sway
- color variation
- distance fade
- GPU instancing

不要使用透明 blend 草。

优先 alpha clip。

---

# 8. Character Visual Integration

删除正式游戏中：

- Capsule body
- Sphere head
- Cube backpack
- Cube sword

作为玩家最终可见外观。

保留 fallback debug visual，但只能 Development Build 使用。

建立：

```text
PlayerVisualRoot
```

支持加载：

```text
Humanoid character prefab
```

如果仓库没有合法 Humanoid model：

不要下载第三方商业资产。

创建：

```text
CharacterVisualAdapter
```

并保留清晰 prefab slot。

Gameplay 必须可以在没有最终 character art 时继续工作。

Character gameplay collider 仍使用 `CharacterController`，不要让 `Mesh Collider` 控制玩家。

---

# 9. Animation Integration

Animator 至少支持：

- Idle
- Walk
- Run
- Sprint
- Jump
- Fall
- Land
- Climb
- Glide
- Attack
- Dodge
- Hit
- Death

Gameplay timing 不依赖 `AnimationClip.length`。

保留现有 gameplay-driven timing。

---

# 10. Lighting

重新设计 Outdoor Lighting。

Directional Light：

- soft shadow
- warm sunlight

Environment：

- slightly cool ambient

增加 Skybox / procedural sky。

设置合理 Shadow Distance 和 Cascades。

禁止纯平 Ambient Color 作为最终方案。

---

# 11. Global Volume

创建项目专用：

```text
Assets/_Game/Art/Volumes/OutdoorStylized.asset
```

启用：

- Tonemapping
- Color Adjustments
- Bloom
- White Balance
- Vignette（极弱，可选）

目标：

- 明亮
- 高可读性
- 自然饱和
- 不过曝

Bloom 必须非常克制。

不要用 Post Processing 掩盖材质或 Lighting 问题。

---

# 12. SSAO

检查现有 PC Renderer 的 SSAO。

保留并调优。

目标：

草、岩石、角色脚下与悬崖缝隙获得适度接触阴影。

不要出现明显黑边。

---

# 13. Atmosphere

增加距离空气透视。

近景：

- 高对比
- 高饱和

中景：

- 轻微降低对比

远景：

- 逐渐向天空颜色融合

使用 Fog + Lighting 完成。

不要用巨型边界墙作为视觉世界边界。

玩家远处应该看到：

- mountain silhouette
- forest
- sky

而不是 Cube wall。

---

# 14. Camera

保留 `OrbitCamera + Cinemachine`。

重新调整：

- Default distance
- FOV
- Focus height
- Collision
- Sprint FOV
- Glide distance

玩家默认应该占屏幕高度约 `35–45%`。

镜头应突出角色和环境比例。

---

# 15. UI Simplification

重构 `GameHud` gameplay overlay。

正常游玩时移除大面积开发说明。

长期显示最多：

- MiniMap placeholder
- Objective
- HP
- Stamina
- Interaction
- Hotbar / Actions

例如：

```text
WASD
Shift
Space
C
G
```

不得永久占据屏幕。

Tutorial 输入提示做：

```text
contextual hint
+
auto fade
```

现有 inventory / pause / crafting 功能不能删除。

---

# 16. Development Art Mode

增加：

```text
DevelopmentVisualMode
```

允许：

```text
Fallback primitives
```

仅用于：

- Automated test
- Missing asset fallback
- Development build

正式 visual mode 禁止自动生成：

- sphere trees
- cube cliffs
- capsule player

作为主画面。

---

# 17. Performance

目标：

```text
Windows PC
1920×1080
60 FPS baseline
```

避免：

- 每株草一个 MonoBehaviour
- 大量独立 Material instance
- 每帧 FindObjectsOfType
- 每帧生成 Mesh
- 每帧 Instantiate
- 无限 Shadow Distance

使用：

- shared materials
- GPU instancing
- LOD
- culling
- static batching where appropriate

实际使用 Profiler 验证。

---

# 18. Required Screenshot QA

增加 Development Build screenshot QA point。

固定：

- player position
- camera yaw
- camera pitch
- time of day

生成：

```text
artifacts/visual/
```

截图至少包括：

```text
spawn.png
meadow.png
cliff.png
forest.png
glide-viewpoint.png
```

截图只用于回归比较，不能把“截图生成成功”当成美术质量通过。

---

# 19. Tests

不能破坏现有：

- EditMode
- PlayMode
- Smoke
- Build

新增测试重点不是测试颜色数值，而是：

- World gameplay objects exist
- Player fallback works
- Player prefab adapter works
- Climb surfaces still work
- NavMesh still valid
- POI works
- Save works

---

# 20. Definition of Done

进入 World 后：

不得再首先看到：

- flat green cube
- sphere forest
- cube boundary wall
- capsule character

目标至少达到：

1. 连续 Terrain 地形
2. 前景有 grass / small rocks
3. 中景有 bush / trees
4. 明显 cliff
5. 远处 landmark / mountain silhouette
6. outdoor stylized lighting
7. atmosphere fog
8. simplified HUD
9. character visual prefab architecture
10. gameplay systems remain functional

最后运行：

```powershell
game/Tools/Verify.ps1
```

如果环境允许：

```powershell
game/Tools/Verify.ps1 -Build
```

严格遵守 `AGENTS.md`。

创建 Git commit。

Commit：

```text
feat: establish stylized open world visual vertical slice
```

最后报告：

```text
Implementation Summary
Visual Changes
Architecture Changes
Assets Added
Shaders
Lighting
Performance
Tests
Known Limitations
Git Commit
```
