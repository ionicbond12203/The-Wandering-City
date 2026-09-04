using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("Wandering City/Prepare playable scenes")]
        public static void Prepare()
        {
            if (!Directory.Exists("Assets/TextMesh Pro/Resources"))
                AssetDatabase.ImportPackage(Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui").resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);

            Directory.CreateDirectory("Assets/_Game/Scenes");
            Directory.CreateDirectory("Assets/_Game/Resources");
            PlayerSettings.companyName = "WanderingCity";
            PlayerSettings.productName = "The Wandering City";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = false;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();

            if (!File.Exists("Assets/_Game/Resources/Balance.asset"))
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameBalance>(), "Assets/_Game/Resources/Balance.asset");
            if (!File.Exists("Assets/_Game/Resources/WorldMaterial.mat"))
                AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")), "Assets/_Game/Resources/WorldMaterial.mat");

            EnsureTravelerController();
            AtmosphereAuthoring.Ensure();

            // Ensure authored art, terrain, layers, textures and environment prefab exist
            EnvironmentAuthoring.EnsureAllEnvironmentAssets();

            // Safe scene handling: Never wipe existing authored World.unity scene!
            EnsureWorldScene();
            EnsureBootScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Game/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/_Game/Scenes/World.unity", true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log("WANDERING_CITY_PREPARE_OK");
        }

        static void EnsureTravelerController() => CharacterAuthoring.EnsureController();

        public static void EnsureWorldScene()
        {
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            bool exists = File.Exists(worldPath);
            Scene scene;

            if (exists)
            {
                scene = EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // Ensure essential root objects
            EnsureHierarchyInScene(scene);
            EditorSceneManager.SaveScene(scene, worldPath);
        }

        static void EnsureHierarchyInScene(Scene scene)
        {
            // 1. Environment root & Terrain
            var env = GameObject.Find("Environment");
            if (env == null)
            {
                env = new GameObject("Environment");
                new GameObject("Cliffs").transform.SetParent(env.transform);
                new GameObject("Vegetation").transform.SetParent(env.transform);
                new GameObject("Roads").transform.SetParent(env.transform);
                new GameObject("Landmarks").transform.SetParent(env.transform);
            }
            else if (PrefabUtility.IsPartOfPrefabInstance(env))
            {
                PrefabUtility.UnpackPrefabInstance(env, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            // Ensure Terrain inside Environment
            var terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(EnvironmentAuthoring.TerrainDataPath);
                if (terrainData != null)
                {
                    var terrainGo = new GameObject("Terrain");
                    terrainGo.transform.SetParent(env.transform);
                    terrainGo.transform.position = TerrainHeightModel.Origin;
                    terrain = terrainGo.AddComponent<Terrain>();
                    terrain.terrainData = terrainData;
                    terrainGo.AddComponent<TerrainCollider>().terrainData = terrainData;
                }
            }

            if (terrain != null)
            {
                terrain.transform.position = TerrainHeightModel.Origin;
                terrain.terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(EnvironmentAuthoring.TerrainDataPath);
                terrain.GetComponent<TerrainCollider>().terrainData = terrain.terrainData;
            }

            // Populate authored environment elements (vegetation, cliffs, landmarks)
            var mats = EnvironmentAuthoring.GenerateMaterials();
            EnvironmentAuthoring.PopulateSceneEnvironment(env, terrain, mats);

            // Harden terrain material: ensure persistent asset is assigned
            if (terrain != null)
            {
                var terrainMat = mats.ContainsKey("Terrain") ? mats["Terrain"]
                    : AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/StylizedTerrain.mat");
                if (terrainMat != null) terrain.materialTemplate = terrainMat;
            }

            // Existing generated decorations must follow updated ground heights too.
            foreach (string container in new[] { "Vegetation", "Cliffs", "Landmarks" })
            {
                var group = env.transform.Find(container);
                if (group == null) continue;
                foreach (Transform child in group)
                {
                    var p = child.position;
                    child.position = WorldBuilder.GroundPoint(p.x, p.z);
                }
            }
            OpenWorldAuthoring.Populate(env, terrain, mats);
            PrefabUtility.SaveAsPrefabAsset(env, EnvironmentAuthoring.AuthoredEnvPrefabPath);
            // 2. Lighting root
            var lighting = GameObject.Find("Lighting");
            if (lighting == null) lighting = new GameObject("Lighting");
            var sunLight = Object.FindFirstObjectByType<Light>();
            if (sunLight == null)
            {
                var sunGo = new GameObject("Directional Light");
                sunGo.transform.SetParent(lighting.transform);
                var light = sunGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.94f, 0.82f);
                light.intensity = 1.4f;
                light.shadows = LightShadows.Soft;
                sunGo.transform.rotation = Quaternion.Euler(46, -38, 0);
            }
            else
            {
                sunLight.type = LightType.Directional;
                sunLight.color = new Color(1f, 0.94f, 0.82f);
                sunLight.intensity = 1.4f;
                sunLight.shadows = LightShadows.Soft;
                if (sunLight.transform.parent == null)
                {
                    sunLight.transform.SetParent(lighting.transform);
                }
            }

            RenderSettings.sun = Object.FindFirstObjectByType<Light>();

            // Global volume in Lighting
            var volume = Object.FindFirstObjectByType<Volume>();
            if (volume == null)
            {
                var volGo = new GameObject("Global Volume");
                volGo.transform.SetParent(lighting.transform);
                var vol = volGo.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Game/Art/Volumes/OutdoorStylized.asset")
                    ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/DefaultVolumeProfile.asset");
            }

            // Atmosphere & Ambient Lighting
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.00135f;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.92f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.78f, 0.90f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.65f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.38f, 0.32f);
            RenderSettings.subtractiveShadowColor = new Color(0.42f, 0.45f, 0.55f);

            // Explicit skybox assignment — harden against missing/invalid default
            var skyMat = mats.ContainsKey("Sky") ? mats["Sky"]
                : AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/OutdoorSky.mat");
            if (skyMat != null) RenderSettings.skybox = skyMat;

            // 3. Navigation root with NavMeshSurface
            var nav = GameObject.Find("Navigation");
            if (nav == null) nav = new GameObject("Navigation");
            var navSurface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (navSurface == null)
            {
                navSurface = nav.AddComponent<NavMeshSurface>();
                navSurface.collectObjects = CollectObjects.Volume;
            }
            navSurface.RemoveData(); navSurface.enabled = false;
            // Runtime PlayableNavigation builds one connected mesh clipped to actual regions and roads.

            // 4. Gameplay root with GameSession
            var gameplay = GameObject.Find("Gameplay");
            if (gameplay == null) gameplay = new GameObject("Gameplay");
            var session = Object.FindFirstObjectByType<GameSession>();
            if (session == null)
            {
                var sessionGo = new GameObject("GameSession");
                sessionGo.transform.SetParent(gameplay.transform);
                sessionGo.AddComponent<GameSession>();
            }
            else if (session.transform.parent == null)
            {
                session.transform.SetParent(gameplay.transform);
            }

            // 5. SpawnPoints root
            var spawnRoot = GameObject.Find("SpawnPoints");
            if (spawnRoot == null)
            {
                spawnRoot = new GameObject("SpawnPoints");
                var playerSpawn = new GameObject("PlayerSpawn");
                playerSpawn.transform.SetParent(spawnRoot.transform);
                playerSpawn.transform.position = WorldBuilder.GroundPoint(0, 0, .1f);
            }
        }

        public static void EnsureBootScene()
        {
            const string bootPath = "Assets/_Game/Scenes/Boot.unity";
            if (File.Exists(bootPath)) return;
            var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Boot").AddComponent<Boot>();
            EditorSceneManager.SaveScene(boot, bootPath);
        }

        [MenuItem("Wandering City/Build Windows")]
        public static void BuildWindows()
        {
            Prepare();
            WorldMapBaker.Bake();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/The Wandering City.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log("WANDERING_CITY_BUILD_OK " + report.summary.totalSize);
        }
    }
}
