using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    public sealed class WorldBuilder : MonoBehaviour
    {
        public static Vector3 WorkbenchPosition = new Vector3(3, 1, 3);
        public readonly List<WorldInteractable> Interactions = new List<WorldInteractable>();
        public readonly List<EnemyAgent> Enemies = new List<EnemyAgent>();
        readonly List<GameObject> buildings = new List<GameObject>();
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly Dictionary<Vector2Int, List<WorldInteractable>> interactionCells = new Dictionary<Vector2Int, List<WorldInteractable>>();
        int indexedInteractions = -1;
        GameSession session; Transform terrainRoot, worldRoot; GameObject preview;
        Material previewMaterial;
        static Mesh formalCliff, resourceRock, resourceLog;

        public static Vector3 GroundPoint(float x, float z, float offset = 0) => new Vector3(x, GroundY(x, z) + offset, z);

        public static float GroundY(float x, float z, float fallbackY = 0f)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null) terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null && terrain.terrainData != null)
            {
                Vector3 tpos = terrain.transform.position;
                Vector3 tsize = terrain.terrainData.size;
                if (x >= tpos.x && x <= tpos.x + tsize.x && z >= tpos.z && z <= tpos.z + tsize.z)
                {
                    return tpos.y + terrain.SampleHeight(new Vector3(x, 0, z));
                }
            }
            if (Physics.Raycast(new Vector3(x, 250f, z), Vector3.down, out var hit, 300f, 1 << 0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }
            return fallbackY;
        }

        public void Create(GameSession owner)
        {
            session = owner;
            worldRoot = new GameObject("Wandering City / Authored world").transform;
            terrainRoot = new GameObject("Static navigation geometry").transform;
            terrainRoot.parent = worldRoot;

            var existingEnv = GameObject.Find("Environment");
            if (existingEnv == null && !DevelopmentVisualMode.Enabled)
            {
                var prefab = Resources.Load<GameObject>("AuthoredEnvironment");
                if (prefab != null)
                {
                    existingEnv = Instantiate(prefab);
                    existingEnv.name = "Environment";
                }
            }
            bool formalMode = existingEnv != null && !DevelopmentVisualMode.Enabled;

            if (formalMode)
            {
                // Formal outdoor stylized lighting & atmosphere
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 0.9f);
                RenderSettings.ambientEquatorColor = new Color(0.58f, 0.68f, 0.62f);
                RenderSettings.ambientGroundColor = new Color(0.35f, 0.38f, 0.3f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.68f, 0.82f, 0.88f);
                RenderSettings.fogDensity = 0.00135f;

                var existingSun = Object.FindFirstObjectByType<Light>();
                if (existingSun == null)
                {
                    var sunGo = new GameObject("Sun (Directional)").AddComponent<Light>();
                    sunGo.type = LightType.Directional;
                    sunGo.color = new Color(1f, 0.94f, 0.82f);
                    sunGo.intensity = 1.4f;
                    sunGo.shadows = LightShadows.Soft;
                    sunGo.transform.rotation = Quaternion.Euler(48, -35, 0);
                    sunGo.transform.SetParent(worldRoot);
                }

                // Explicit skybox assignment — harden against missing/invalid default
                var skyMat = Resources.Load<Material>("OutdoorSky");
                if (skyMat == null) skyMat = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
                    ? null : null; // No runtime fallback — if asset missing, sky relies on scene defaults
                // Try loading from art materials path via asset reference
                // Note: Resources.Load only works for assets in Resources folders
                if (RenderSettings.skybox == null || RenderSettings.skybox.shader == null || !RenderSettings.skybox.shader.isSupported)
                {
                    // Ensure camera uses solid color as safety fallback
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        mainCam.clearFlags = CameraClearFlags.SolidColor;
                        mainCam.backgroundColor = RenderSettings.fogColor;
                    }
                }
            }
            else
            {
                // Fallback graybox visuals for DevelopmentVisualMode / test mode
                RenderSettings.ambientLight = new Color(.65f, .73f, .77f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.64f, .77f, .78f);
                RenderSettings.fogDensity = .0035f;
                var sun = new GameObject("Late afternoon sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1, .89f, .7f);
                sun.intensity = 1.6f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(48, -35, 0);
                Shape("[PLACEHOLDER] Meadow", PrimitiveType.Cube, new Vector3(0, -.5f, 65), new Vector3(220, 1, 230), new Color(.34f, .48f, .32f), terrainRoot);
                Path(new Vector3(0, .025f, 4), new Vector3(-46, .025f, 62));
                Path(new Vector3(0, .025f, 4), new Vector3(61, .025f, 57));
                Path(new Vector3(-46, .025f, 62), new Vector3(20, .025f, 128));
                Path(new Vector3(61, .025f, 57), new Vector3(20, .025f, 128));
                Shape("[PLACEHOLDER] West boundary", PrimitiveType.Cube, new Vector3(-110, 8, 65), new Vector3(2, 16, 230), new Color(.35f, .43f, .4f), terrainRoot);
                Shape("[PLACEHOLDER] East boundary", PrimitiveType.Cube, new Vector3(110, 8, 65), new Vector3(2, 16, 230), new Color(.35f, .43f, .4f), terrainRoot);
                Shape("[PLACEHOLDER] North boundary", PrimitiveType.Cube, new Vector3(0, 8, 179), new Vector3(220, 16, 2), new Color(.35f, .43f, .4f), terrainRoot);
                Shape("[PLACEHOLDER] South boundary", PrimitiveType.Cube, new Vector3(0, 8, -49), new Vector3(220, 16, 2), new Color(.35f, .43f, .4f), terrainRoot);
                var rng = new System.Random(8317);
                for (int i = 0; i < 105; i++) { float x = -85 + (float)rng.NextDouble() * 75, z = 26 + (float)rng.NextDouble() * 86; if (Vector2.Distance(new Vector2(x, z), new Vector2(-48, 65)) < 7) continue; Tree(new Vector3(x, 0, z), 3 + (float)rng.NextDouble() * 4); }
                for (int i = 0; i < 30; i++) { float a = i * .7f; Rock(new Vector3(64 + Mathf.Cos(a) * (12 + i % 4 * 3), 1, 57 + Mathf.Sin(a) * (12 + i % 3 * 4)), 2 + i % 4); }
                for (int i = 0; i < 24; i++) { float a = i / 24f * Mathf.PI * 2; Vector3 p = new Vector3(Mathf.Cos(a) * 120, 5, 65 + Mathf.Sin(a) * 130); Rock(p, 16 + i % 4 * 4); }

                // Fallback camera: explicit solid color, do not rely on default skybox
                // Camera is created later in CreatePlayer, this ensures RenderSettings won't cause magenta
                RenderSettings.skybox = null;
            }

            // Grounded positions for interactive and gameplay items
            float benchGround = GroundY(3, 3, 0);
            WorkbenchPosition = new Vector3(3, benchGround + 0.5f, 3);
            Shape("Workshop platform", PrimitiveType.Cube, new Vector3(3, benchGround + .1f, 3), new Vector3(6, .2f, 5), new Color(.49f, .39f, .26f), terrainRoot);
            Shape("Workbench", PrimitiveType.Cube, WorkbenchPosition, new Vector3(2.2f, .35f, 1.1f), new Color(.31f, .23f, .18f), terrainRoot);
            foreach (float dx in new[] { -.85f, .85f })
                Shape("Bench leg", PrimitiveType.Cube, WorkbenchPosition + new Vector3(dx, -.5f, 0), new Vector3(.18f, .8f, .6f), new Color(.28f, .22f, .17f), terrainRoot);
            Interaction("workbench", "工作台 / 制作与升级", WorkbenchPosition + Vector3.up * .5f, null, true);

            float buildPlotGround = GroundY(-7.5f, 1.5f, 0);
            Shape("Build plot", PrimitiveType.Cube, new Vector3(-7.5f, buildPlotGround + .015f, 1.5f), new Vector3(12.2f, .025f, 12.2f), new Color(.46f, .48f, .34f), worldRoot, false);
            for (int i = 0; i <= 4; i++)
            {
                Shape("Grid", PrimitiveType.Cube, new Vector3(-13.5f + i * 3, buildPlotGround + .04f, 1.5f), new Vector3(.04f, .02f, 12), new Color(.67f, .66f, .44f), worldRoot, false);
                Shape("Grid", PrimitiveType.Cube, new Vector3(-7.5f, buildPlotGround + .04f, -4.5f + i * 3), new Vector3(12, .02f, .04f), new Color(.67f, .66f, .44f), worldRoot, false);
            }

            Landmark("旅人据点", new Vector3(0, GroundY(0, 9, 0), 9), new Color(.94f, .69f, .29f), 7);
            Landmark("风息树林", new Vector3(-48, GroundY(-48, 65, 0), 65), new Color(.53f, .79f, .52f), 11);
            Landmark("旧日采石场", new Vector3(64, GroundY(64, 59, 0), 59), new Color(.48f, .78f, .85f), 11);
            Landmark("沉眠营地", new Vector3(20, GroundY(20, 139, 0), 139), new Color(.94f, .51f, .3f), 15);

            // Steps & ramp
            if (DevelopmentVisualMode.Enabled)
            {
            var ramp = Shape("Quarry ramp", PrimitiveType.Cube, new Vector3(53, GroundY(53, 70, 1), 70), new Vector3(5, .5f, 12), new Color(.51f, .52f, .48f), terrainRoot);
            ramp.transform.rotation = Quaternion.Euler(-10, 0, 0);
            }
            for (int i = 0; i < 5; i++)
                Shape("Ancient stair", PrimitiveType.Cube, new Vector3(28, GroundY(28, 115 + i, 0) + i * .22f + .1f, 115 + i), new Vector3(5, .2f + i * .44f, 1), new Color(.5f, .52f, .46f), terrainRoot);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                Vector3 p = new Vector3(20 + Mathf.Cos(a) * 13, 0, 130 + Mathf.Sin(a) * 13);
                p.y = GroundY(p.x, p.z, 0) + 2.5f;
                Shape("Ruined pillar", PrimitiveType.Cylinder, p, new Vector3(1.5f, 2.5f + i % 2, 1.5f), new Color(.42f, .48f, .47f), terrainRoot);
            }

            // Resources
            for (int i = 0; i < 28; i++)
            {
                Vector3 p = i < 8 ? new Vector3(-17 + (i % 4) * 5, 0, 12 + i / 4 * 5) : new Vector3(-70 + (i % 5) * 9, 0, 40 + i / 5 * 9);
                p.y = GroundY(p.x, p.z, 0) + .7f;
                Resource("wood-" + i, "风纹木材 ×5", p, "wood", 5, new Color(.57f, .37f, .2f));
            }
            for (int i = 0; i < 20; i++)
            {
                Vector3 p = i < 6 ? new Vector3(11 + i % 3 * 4, 0, 12 + i / 3 * 6) : new Vector3(46 + i % 5 * 7, 0, 43 + i / 5 * 7);
                // Keep this deposit on the quarry terrace, away from the canyon cliff face. Stable ID is unchanged.
                if (i == 9) p = new Vector3(79, 0, 61);
                p.y = GroundY(p.x, p.z, 0) + .6f;
                Resource("stone-" + i, "原野石材 ×4", p, "stone", 4, new Color(.65f, .68f, .62f));
            }
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = i == 10 ? new Vector3(58, 0, 83) : new Vector3(51 + i % 4 * 7, 0, 66 + i / 4 * 6);
                p.y = GroundY(p.x, p.z, 0) + .8f;
                Resource("ore-" + i, "星辉矿石 ×2", p, "ore", 2, new Color(.28f, .78f, .84f));
            }

            // Chests
            Chest("chest-forest", new Vector3(-69, GroundY(-69, 94, 0) + .7f, 94), new Dictionary<string, int> { ["potion"] = 2, ["wood"] = 10 }, "林间宝箱 / 药剂 ×2、木材 ×10");
            Chest("chest-quarry", new Vector3(81, GroundY(81, 86, 0) + .7f, 86), new Dictionary<string, int> { ["ore"] = 4, ["stone"] = 10 }, "矿场宝箱 / 矿石 ×4、石材 ×10");
            Chest("chest-hidden", new Vector3(-5, GroundY(-5, 103, 0) + .7f, 103), new Dictionary<string, int> { ["potion"] = 3 }, "溪畔秘藏 / 药剂 ×3");
            Chest("camp-reward", new Vector3(20, GroundY(20, 141, 0) + .7f, 141), new Dictionary<string, int> { ["core"] = 1, ["ore"] = 5 }, "星核宝箱 / 星核 ×1、矿石 ×5");

            gameObject.AddComponent<ExplorationWorld>().Create(session, this, terrainRoot, worldRoot);

            var surface = terrainRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Volume;
            surface.center = new Vector3(0, 40, 70);
            surface.size = new Vector3(230, 110, 235);
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();

            for (int i = 0; i < 5; i++)
            {
                Vector3 p = new Vector3(11 + i % 3 * 8, 0, 122 + i / 3 * 11);
                p.y = GroundY(p.x, p.z, 0) + .1f;
                Enemy("enemy-camp-" + i, p);
            }
            Vector3[] wild = { new Vector3(-35, 0, 46), new Vector3(-63, 0, 82), new Vector3(47, 0, 48), new Vector3(76, 0, 79), new Vector3(-3, 0, 104) };
            for (int i = 0; i < wild.Length; i++)
            {
                Vector3 p = wild[i];
                p.y = GroundY(p.x, p.z, 0) + .1f;
                Enemy("enemy-wild-" + i, p);
            }
        }

        public GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Transform parent = null, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            if (!materials.TryGetValue(color, out var mat))
            {
                mat = new Material(Resources.Load<Material>("WorldMaterial"));
                mat.color = color;
                materials[color] = mat;
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!DevelopmentVisualMode.Enabled && (name.Contains("mesa") || name.Contains("shelf") || name.Contains("bank")))
            {
                if (formalCliff == null) formalCliff = OriginalMesh.Loft("Climbable weathered plateau", 12, new[]{.6f,.57f,.5f,.48f},0,12,.06f);
                go.GetComponent<MeshFilter>().sharedMesh = formalCliff;
            }

            if (!collision)
            {
                go.GetComponent<Collider>().enabled = false;
                go.layer = 2;
            }
            return go;
        }

        void Path(Vector3 from, Vector3 to)
        {
            var road = Shape("Worn path", PrimitiveType.Cube, (from + to) * .5f, new Vector3(4, .035f, Vector3.Distance(from, to)), new Color(.61f, .59f, .4f), worldRoot, false);
            road.transform.rotation = Quaternion.LookRotation(to - from);
        }

        void Tree(Vector3 p, float h)
        {
            Shape("[PLACEHOLDER] Tree trunk", PrimitiveType.Cylinder, p + Vector3.up * h * .4f, new Vector3(.65f, h * .4f, .65f), new Color(.28f, .27f, .2f), terrainRoot);
            Shape("[PLACEHOLDER] Canopy", PrimitiveType.Sphere, p + Vector3.up * h, new Vector3(h * .75f, h * .85f, h * .75f), new Color(.25f, .4f + h * .015f, .29f), worldRoot, false);
        }

        void Rock(Vector3 p, float size)
        {
            var r = Shape("[PLACEHOLDER] Weathered stone", PrimitiveType.Cube, p, new Vector3(size, size * .8f, size * .8f), new Color(.41f, .47f, .46f), terrainRoot);
            r.transform.rotation = Quaternion.Euler(0, p.x * 3, 12);
        }

        void Landmark(string name, Vector3 p, Color c, float height)
        {
            if (!DevelopmentVisualMode.Enabled) return;
            Shape(name, PrimitiveType.Cylinder, p + Vector3.up * height * .5f, new Vector3(.35f, height * .5f, .35f), new Color(.34f, .32f, .25f), terrainRoot);
            Shape(name + " banner", PrimitiveType.Cube, p + new Vector3(1.1f, height - 1, 0), new Vector3(2.2f, 2.5f, .12f), c, worldRoot, false);
        }

        WorldInteractable Interaction(string id, string label, Vector3 p, Dictionary<string, int> reward, bool workbench = false)
        {
            var go = new GameObject(id);
            go.transform.SetParent(worldRoot);
            go.transform.position = p;
            var item = go.AddComponent<WorldInteractable>();
            item.Id = id;
            item.Label = label;
            item.Reward = reward;
            item.Workbench = workbench;
            item.Session = session;
            Interactions.Add(item);
            return item;
        }

        void Resource(string id, string name, Vector3 p, string kind, int count, Color color)
        {
            var item = Interaction(id, name, p, new Dictionary<string, int> { [kind] = count });
            if(DevelopmentVisualMode.Enabled)
                Shape(name, kind == "wood" ? PrimitiveType.Cylinder : PrimitiveType.Sphere, p, Vector3.one, color, item.transform, false);
            else
            {
                var visual = Shape(name,PrimitiveType.Cube,p-Vector3.up*(kind=="wood"?.52f:.28f),kind=="wood"?new Vector3(.35f,1.35f,.35f):new Vector3(1.05f,.7f,.85f),color,item.transform,false);
                if(resourceRock==null)resourceRock=OriginalMesh.Rock(51);
                if(resourceLog==null)resourceLog=OriginalMesh.Loft("Fallen branch",7,new[]{.5f,.45f,.4f},.08f,61,.1f);
                visual.GetComponent<MeshFilter>().sharedMesh=kind=="wood"?resourceLog:resourceRock;
                if(kind=="wood")visual.transform.rotation=Quaternion.Euler(0,p.x*17,82);
            }
        }

        void Chest(string id, Vector3 p, Dictionary<string, int> reward, string label)
        {
            var item = Interaction(id, label, p, reward);
            Shape("Chest", PrimitiveType.Cube, p, new Vector3(1.4f, 1, 1), new Color(.44f, .29f, .19f), item.transform, false);
            Shape("Gold clasp", PrimitiveType.Cube, p + new Vector3(0, 0, -.51f), new Vector3(.2f, .65f, .1f), new Color(1, .79f, .35f), item.transform, false);
        }

        public PlayerMotor CreatePlayer(GameSession owner)
        {
            var go = new GameObject("Traveler");
            go.layer = 8;
            go.transform.position = Vector3.up;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = Vector3.up * .9f;
            cc.radius = .35f;
            cc.stepOffset = .35f;
            cc.slopeLimit = 48;
            var player = go.AddComponent<PlayerMotor>();
            player.Session = owner;
            player.Controller = cc;
            player.Traversal = go.AddComponent<PlayerTraversal>();
            player.Traversal.Initialize(player);
            var adapter = go.AddComponent<CharacterVisualAdapter>();
            adapter.Setup(player);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var cam = cameraObject.AddComponent<Camera>();
            cam.fieldOfView = 58;
            cam.farClipPlane = 1200;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            cam.backgroundColor = RenderSettings.fogColor;
            // Explicit camera clear mode: if skybox is valid use it, otherwise solid color
            if (RenderSettings.skybox != null && RenderSettings.skybox.shader != null && RenderSettings.skybox.shader.isSupported)
                cam.clearFlags = CameraClearFlags.Skybox;
            else
                cam.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.AddComponent<AudioListener>();
            var orbit = cameraObject.AddComponent<OrbitCamera>();
            orbit.Target = go.transform;
            orbit.Session = owner;
            return player;
        }

        void Enemy(string id, Vector3 p)
        {
            if (NavMesh.SamplePosition(p, out var nav, 5, NavMesh.AllAreas)) p = nav.position;
            var go = new GameObject(id);
            go.layer = 9;
            go.transform.position = p;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.center = Vector3.up * .9f;
            capsule.radius = .4f;
            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 400;
            agent.acceleration = 14;
            agent.stoppingDistance = 1.8f;
            agent.radius = .4f;
            agent.height = 1.8f;
            var e = go.AddComponent<EnemyAgent>();
            e.Id = id;
            e.Session = session;
            e.Agent = agent;
            e.Body = Shape("Stone sentinel", PrimitiveType.Capsule, p + Vector3.up, new Vector3(.85f, .8f, .7f), new Color(.28f, .33f, .42f), go.transform, false).transform;
            Shape("Amber eye", PrimitiveType.Cube, p + new Vector3(0, 1.6f, .36f), new Vector3(.5f, .1f, .1f), new Color(1, .7f, .25f), go.transform, false);
            e.Telegraph = Shape("Attack warning", PrimitiveType.Cylinder, p + Vector3.up * .08f, new Vector3(4.5f, .025f, 4.5f), new Color(.85f, .3f, .15f), go.transform, false).transform;
            e.Telegraph.gameObject.SetActive(false);
            e.Initialize();
            Enemies.Add(e);
        }

        public void SpawnDrop(string id, Vector3 p)
        {
            string drop = "drop-" + id;
            if (!Interactions.Any(i => i.Id == drop))
                Resource(drop, "守卫掉落 / 石材 ×2", p + Vector3.up * .6f, "stone", 2, new Color(.84f, .7f, .33f));
        }

        public void Restore(GameState state)
        {
            foreach (var e in Enemies)
            {
                e.gameObject.SetActive(!state.defeated.Contains(e.Id));
                e.ResetEncounter();
                if (state.defeated.Contains(e.Id)) SpawnDrop(e.Id, e.Home);
            }
            foreach (var drop in Interactions.Where(i => i.Id.StartsWith("drop-")))
                drop.gameObject.SetActive(state.defeated.Contains(drop.Id.Substring(5)) && !state.claimed.Contains(drop.Id));
            RefreshRewards();
            RebuildStructures();
        }

        public void RefreshRewards()
        {
            foreach (var item in Interactions)
                if (!item.Id.StartsWith("drop-") || session.State.defeated.Contains(item.Id.Substring(5)))
                    item.gameObject.SetActive(item.Available);
        }

        public void ResetEnemies()
        {
            foreach (var enemy in Enemies)
                if (enemy.gameObject.activeSelf)
                    enemy.ResetEncounter();
        }

        public WorldInteractable FindInteraction(Vector3 origin)
        {
            if (indexedInteractions != Interactions.Count)
            {
                interactionCells.Clear();
                foreach (var item in Interactions)
                {
                    var key = InteractionCell(item.transform.position);
                    if (!interactionCells.TryGetValue(key, out var cell))
                    {
                        cell = new List<WorldInteractable>();
                        interactionCells.Add(key, cell);
                    }
                    cell.Add(item);
                }
                indexedInteractions = Interactions.Count;
            }
            WorldInteractable nearest = null;
            float best = 3.6f * 3.6f;
            var center = InteractionCell(origin);
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (!interactionCells.TryGetValue(center + new Vector2Int(x, z), out var cell)) continue;
                    foreach (var item in cell)
                    {
                        float distance = (item.transform.position - origin).sqrMagnitude;
                        if (item.gameObject.activeSelf && item.Available && distance < best && CombatVisibility.Clear(origin, item.transform.position))
                        {
                            best = distance;
                            nearest = item;
                        }
                    }
                }
            }
            return nearest;
        }

        static Vector2Int InteractionCell(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x / 8), Mathf.FloorToInt(p.z / 8));
        public static string Region(Vector3 p) => p.z > 111 ? "ruins" : p.x < -24 && p.z > 28 ? "forest" : p.x > 32 && p.z > 28 ? "quarry" : "camp";
        public static string RegionName(string id) => id == "forest" ? "风息树林" : id == "quarry" ? "旧日采石场" : id == "ruins" ? "沉眠营地" : "旅人据点";

        public static void Geometry(string kind, int x, int z, int rotation, out Vector3 center, out Vector3 size)
        {
            float gy = GroundY(x * 3, z * 3, 0);
            center = new Vector3(x * 3, gy + (kind == "floor" ? .15f : kind == "roof" ? 2.9f : 1.6f), z * 3);
            size = kind == "wall" ? new Vector3(3, 2.6f, .18f) : new Vector3(3, .25f, 3);
            if (kind == "wall")
            {
                center += Quaternion.Euler(0, rotation * 90, 0) * new Vector3(0, 0, 1.42f);
                if (rotation % 2 == 1) size = new Vector3(.18f, 2.6f, 3);
            }
        }

        public bool ClearForBuilding(string kind, int x, int z, int rotation)
        {
            Geometry(kind, x, z, rotation, out var center, out var size);
            Vector3 player = session.Player.transform.position;
            if (kind == "floor" && Mathf.Abs(player.x - center.x) < 1.85f && Mathf.Abs(player.z - center.z) < 1.85f && player.y < center.y + 2) return false;
            return !Physics.CheckBox(center, size * .46f, Quaternion.identity, (1 << 8) | (1 << 9), QueryTriggerInteraction.Ignore);
        }

        public void RebuildStructures()
        {
            foreach (var go in buildings) { go.SetActive(false); Destroy(go); }
            buildings.Clear();
            foreach (var b in session.State.buildings)
            {
                Geometry(b.kind, b.x, b.z, b.rotation, out var center, out var size);
                var go = Shape("Building / " + b.id, PrimitiveType.Cube, center, size, b.kind == "roof" ? new Color(.29f, .39f, .36f) : new Color(.52f, .36f, .22f), worldRoot);
                var tag = go.AddComponent<BuildingTag>();
                tag.Id = b.id;
                buildings.Add(go);
            }
        }

        public void HidePreview() { if (preview != null) preview.SetActive(false); }

        public void UpdateBuilding()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb.digit1Key.wasPressedThisFrame) session.BuildKind = "floor";
            if (kb.digit2Key.wasPressedThisFrame) session.BuildKind = "wall";
            if (kb.digit3Key.wasPressedThisFrame) session.BuildKind = "roof";
            if (kb.rKey.wasPressedThisFrame) session.BuildRotation = (session.BuildRotation + 1) % 4;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(.5f, .5f));
            if (kb.xKey.wasPressedThisFrame && Physics.Raycast(ray, out var remove, 9, 1 << 0))
            {
                var tag = remove.collider.GetComponent<BuildingTag>();
                bool ok = tag != null && Rules.Remove(session.State, tag.Id);
                session.Result(ok, "模块已完整回收", "无法拆除：请先拆除上层模块，或清理背包");
                if (ok) RebuildStructures();
            }
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float distance) || distance > 12) { HidePreview(); return; }
            Vector3 point = ray.GetPoint(distance);
            int x = Mathf.RoundToInt(point.x / 3), z = Mathf.RoundToInt(point.z / 3);
            bool clear = ClearForBuilding(session.BuildKind, x, z, session.BuildRotation);
            bool valid = Rules.CanPlace(session.State, session.BuildKind, x, z, session.BuildRotation, clear) && session.State.Count(session.BuildKind) > 0;
            Geometry(session.BuildKind, x, z, session.BuildRotation, out var center, out var size);
            if (preview == null)
            {
                preview = Shape("Placement preview", PrimitiveType.Cube, center, size, Color.green, worldRoot, false);
                previewMaterial = preview.GetComponent<Renderer>().material;
            }
            preview.SetActive(true);
            preview.transform.position = center;
            preview.transform.localScale = size * .98f;
            previewMaterial.color = valid ? new Color(.3f, .85f, .65f) : new Color(.95f, .3f, .25f);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                bool ok = Rules.Place(session.State, session.BuildKind, x, z, session.BuildRotation, clear);
                session.Result(ok, "已放置 / " + GameHud.ItemName(session.BuildKind), "放置无效：检查模块数量、据点范围、角色碰撞和下层支撑");
                if (ok) RebuildStructures();
            }
        }

        public static void Pulse(Vector3 p, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.GetComponent<Collider>().enabled = false;
            go.transform.position = p;
            go.transform.localScale = Vector3.one * .4f;
            var mat = new Material(Resources.Load<Material>("WorldMaterial"));
            mat.color = color;
            go.GetComponent<Renderer>().material = mat;
            Destroy(go, .13f);
            Destroy(mat, .2f);
        }

        void OnDestroy()
        {
            foreach (var mat in materials.Values) Destroy(mat);
            if (previewMaterial != null) Destroy(previewMaterial);
        }
    }

    public sealed class BuildingTag : MonoBehaviour { public string Id; }
}
