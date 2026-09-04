using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WanderingCity.Editor
{
    public static class ProjectBuilder
    {
        [MenuItem("Wandering City/Prepare playable scenes")]
        public static void Prepare()
        {
            if (!Directory.Exists("Assets/TextMesh Pro/Resources")) AssetDatabase.ImportPackage(Path.Combine(UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui").resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
            Directory.CreateDirectory("Assets/_Game/Scenes"); Directory.CreateDirectory("Assets/_Game/Resources");
            PlayerSettings.companyName = "WanderingCity"; PlayerSettings.productName = "The Wandering City"; PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080; PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; PlayerSettings.runInBackground = false;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]); settings.FindProperty("activeInputHandler").intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo();
            if (!File.Exists("Assets/_Game/Resources/Balance.asset")) AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameBalance>(), "Assets/_Game/Resources/Balance.asset");
            if (!File.Exists("Assets/_Game/Resources/WorldMaterial.mat")) AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")), "Assets/_Game/Resources/WorldMaterial.mat");
            if (!File.Exists("Assets/_Game/Resources/Traveler.controller"))
            {
                var controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/_Game/Resources/Traveler.controller"); controller.AddParameter("Speed", AnimatorControllerParameterType.Float); controller.AddParameter("Action", AnimatorControllerParameterType.Int);
                var machine = controller.layers[0].stateMachine;
                for (int i = 0; i < 5; i++) { var clip = new AnimationClip { name = ((PlayerAction)i).ToString() }; clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.z", AnimationCurve.EaseInOut(0, i == 2 ? -12 : 0, .4f, i == 4 ? 75 : 0)); AssetDatabase.AddObjectToAsset(clip, controller); var state = machine.AddState(clip.name); state.motion = clip; if (i == 0) machine.defaultState = state; var transition = machine.AddAnyStateTransition(state); transition.hasExitTime = false; transition.duration = .1f; transition.canTransitionToSelf = false; transition.AddCondition(AnimatorConditionMode.Equals, i, "Action"); }
            }
            var world = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); new GameObject("GameSession").AddComponent<GameSession>(); EditorSceneManager.SaveScene(world, "Assets/_Game/Scenes/World.unity");
            var boot = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); new GameObject("Boot").AddComponent<Boot>(); EditorSceneManager.SaveScene(boot, "Assets/_Game/Scenes/Boot.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/_Game/Scenes/Boot.unity", true), new EditorBuildSettingsScene("Assets/_Game/Scenes/World.unity", true) };
            AssetDatabase.SaveAssets(); Debug.Log("WANDERING_CITY_PREPARE_OK");
        }
        [MenuItem("Wandering City/Build Windows")]
        public static void BuildWindows()
        {
            Prepare(); Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/The Wandering City.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Windows build failed: " + report.summary.result);
            Debug.Log("WANDERING_CITY_BUILD_OK " + report.summary.totalSize);
        }
    }
}
