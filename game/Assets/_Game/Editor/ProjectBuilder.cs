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

        static void EnsureTravelerController()
        {
            if (!File.Exists("Assets/_Game/Resources/Traveler.controller"))
            {
                var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/_Game/Resources/Traveler.controller");
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                controller.AddParameter("Action", AnimatorControllerParameterType.Int);
                var machine = controller.layers[0].stateMachine;
                for (int i = 0; i < 5; i++)
                {
                    var clip = new AnimationClip { name = ((PlayerAction)i).ToString() };
                    clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", AnimationCurve.EaseInOut(0, i == 2 ? -12 : 0, .4f, i == 4 ? 75 : 0));
                    AssetDatabase.AddObjectToAsset(clip, controller);
                    var state = machine.AddState(clip.name);
                    state.motion = clip;
                    if (i == 0) machine.defaultState = state;
                    var transition = machine.AddAnyStateTransition(state);
                    transition.hasExitTime = false;
                    transition.duration = .1f;
                    transition.canTransitionToSelf = false;
                    transition.AddCondition(AnimatorConditionMode.Equals, i, "Action");
                }
            }

            var traveler = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Game/Resources/Traveler.controller");
            string[] requiredParams = { "Speed", "Action", "VerticalVelocity", "Grounded", "Climbing", "Gliding", "Attack", "Dodge", "Dead", "Traversal" };
            foreach (string parameter in requiredParams)
            {
                if (!System.Array.Exists(traveler.parameters, p => p.name == parameter))
                {
                    var paramType = parameter == "Speed" || parameter == "VerticalVelocity" ? AnimatorControllerParameterType.Float :
                                    parameter == "Action" || parameter == "Traversal" ? AnimatorControllerParameterType.Int :
                                    AnimatorControllerParameterType.Bool;
                    traveler.AddParameter(parameter, paramType);
                }
            }

            var traversalMachine = traveler.layers[0].stateMachine;
            foreach (var transition in traversalMachine.anyStateTransitions)
            {
                if (transition.destinationState != null && transition.destinationState.name == "Move" && !System.Array.Exists(transition.conditions, c => c.parameter == "Traversal"))
                    transition.AddCondition(AnimatorConditionMode.Less, 4, "Traversal");
            }

            foreach (var mode in new[] { TraversalState.Climb, TraversalState.LedgeTransition, TraversalState.Glide })
            {
                string name = "Traversal / " + mode;
                if (System.Array.Exists(traversalMachine.states, s => s.state.name == name)) continue;
                var clip = new AnimationClip { name = name };
                clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.x", AnimationCurve.Constant(0, 1, mode == TraversalState.Glide ? 18 : -8));
                AssetDatabase.AddObjectToAsset(clip, traveler);
                var state = traversalMachine.AddState(name);
                state.motion = clip;
                var transition = traversalMachine.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.duration = .15f;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, 0, "Action");
                transition.AddCondition(AnimatorConditionMode.Equals, (int)mode, "Traversal");
            }

            EditorUtility.SetDirty(traveler);
            EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<GameBalance>("Assets/_Game/Resources/Balance.asset"));
        }

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
            navSurface.center = new Vector3(0, 40, 70);
            navSurface.size = new Vector3(230, 110, 235);
                navSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            }
            navSurface.collectObjects = CollectObjects.Volume;
            navSurface.center = new Vector3(0,40,70);
            navSurface.size = new Vector3(230,110,235);
            navSurface.BuildNavMesh();

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
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/The Wandering City.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log("WANDERING_CITY_BUILD_OK " + report.summary.totalSize);
        }
    }
}
