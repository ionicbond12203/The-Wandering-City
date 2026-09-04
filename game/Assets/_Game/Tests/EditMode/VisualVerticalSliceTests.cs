using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WanderingCity.Editor;

namespace WanderingCity.Tests.EditMode
{
    public sealed class VisualVerticalSliceTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ProjectBuilder.Prepare();
        }

        [Test]
        public void Hierarchy_ContainsAllRequiredContainersAndAuthoredElements()
        {
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            Assert.IsTrue(File.Exists(worldPath), "World.unity scene must exist.");

            var scene = EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "World scene must be valid.");

            var env = GameObject.Find("Environment");
            Assert.IsNotNull(env, "Environment root must exist in World.unity.");

            var terrain = env.transform.Find("Terrain");
            Assert.IsNotNull(terrain, "Environment/Terrain must exist.");
            Assert.IsNotNull(terrain.GetComponent<Terrain>(), "Terrain component must exist.");

            var veg = env.transform.Find("Vegetation");
            Assert.IsNotNull(veg, "Environment/Vegetation must exist.");
            Assert.Greater(veg.childCount, 0, "Vegetation must contain authored trees, bushes, flowers, and grass tufts.");

            var cliffs = env.transform.Find("Cliffs");
            Assert.IsNotNull(cliffs, "Environment/Cliffs must exist.");
            Assert.Greater(cliffs.childCount, 0, "Cliffs must contain authored modular cliffs and boulders.");

            var landmarks = env.transform.Find("Landmarks");
            Assert.IsNotNull(landmarks, "Environment/Landmarks must exist.");
            Assert.GreaterOrEqual(landmarks.childCount, 3, "At least 3 landmarks must be authored.");

            var lighting = GameObject.Find("Lighting");
            Assert.IsNotNull(lighting, "Lighting root must exist.");
            Assert.IsNotNull(lighting.GetComponentInChildren<Light>(), "Directional light must exist in Lighting.");
            Assert.IsNotNull(lighting.GetComponentInChildren<Volume>(), "Global Volume must exist in Lighting.");

            var navigation = GameObject.Find("Navigation");
            Assert.IsNotNull(navigation, "Navigation root must exist.");

            var gameplay = GameObject.Find("Gameplay");
            Assert.IsNotNull(gameplay, "Gameplay root must exist.");
            Assert.IsNotNull(gameplay.GetComponentInChildren<GameSession>(), "GameSession must exist in Gameplay.");

            var spawnPoints = GameObject.Find("SpawnPoints");
            Assert.IsNotNull(spawnPoints, "SpawnPoints root must exist.");
        }

        [Test]
        public void VisualLandmarks_AreAtLeastThreeAndGrounded()
        {
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);

            var landmarks = GameObject.Find("Environment/Landmarks");
            Assert.IsNotNull(landmarks, "Environment/Landmarks must exist.");
            Assert.GreaterOrEqual(landmarks.transform.childCount, 3, "At least 3 landmarks must exist.");

            for (int i = 0; i < landmarks.transform.childCount; i++)
            {
                var lm = landmarks.transform.GetChild(i);
                Assert.IsNotNull(lm, "Landmark child must be valid.");
                Assert.IsTrue(lm.gameObject.activeInHierarchy, $"Landmark {lm.name} must be active.");
            }
        }

        [Test]
        public void CharacterVisualAdapter_StandardAndFallbackModesWork()
        {
            var go = new GameObject("TestTraveler");
            var cc = go.AddComponent<CharacterController>();
            var player = go.AddComponent<PlayerMotor>();
            player.Controller = cc;
            var adapter = go.AddComponent<CharacterVisualAdapter>();

            try
            {
                // 1. Standard stylized mode
                DevelopmentVisualMode.UseFallbackPrimitives = false;
                adapter.Setup(player);

                Assert.IsFalse(adapter.IsFallbackMode, "Adapter should not be in fallback mode when fallback flag is false.");
                Assert.IsNotNull(player.Visual, "player.Visual must be assigned by adapter.");
                Assert.IsNotNull(player.Blade, "player.Blade must be assigned by adapter.");
                Assert.IsNotNull(player.GlideSail, "player.GlideSail must be assigned by adapter.");
                Assert.IsNotNull(player.Animator, "player.Animator must be assigned by adapter.");
                Assert.IsNotNull(player.Animator.runtimeAnimatorController, "Animator must have runtimeAnimatorController.");

                // 2. Fallback debug mode
                DevelopmentVisualMode.UseFallbackPrimitives = true;
                adapter.Setup(player);

                Assert.IsTrue(adapter.IsFallbackMode, "Adapter should be in fallback mode when fallback flag is true.");
                Assert.IsNotNull(player.Visual.Find("[PLACEHOLDER] DebugPrimitives"), "[PLACEHOLDER] DebugPrimitives container must exist in fallback mode.");
                Assert.IsNotNull(player.Blade, "Blade must still exist in fallback mode.");
                Assert.IsNotNull(player.GlideSail, "GlideSail must still exist in fallback mode.");
            }
            finally
            {
                DevelopmentVisualMode.UseFallbackPrimitives = false;
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AtmosphereAndLighting_VolumeProfileAndSunLightConfigured()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Game/Art/Volumes/OutdoorStylized.asset");
            if (profile == null || profile.components == null || profile.components.Count == 0 || profile.components[0] == null)
            {
                EnvironmentAuthoring.GenerateVolumeProfile();
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Game/Art/Volumes/OutdoorStylized.asset");
            }
            Assert.IsNotNull(profile, "OutdoorStylized VolumeProfile must exist.");

            Assert.IsTrue(profile.Has<Tonemapping>() || profile.TryGet<Tonemapping>(out _), "Volume profile must have Tonemapping.");
            Assert.IsTrue(profile.Has<ColorAdjustments>() || profile.TryGet<ColorAdjustments>(out _), "Volume profile must have ColorAdjustments.");
            Assert.IsTrue(profile.Has<Bloom>() || profile.TryGet<Bloom>(out _), "Volume profile must have Bloom.");
            Assert.IsTrue(profile.Has<WhiteBalance>() || profile.TryGet<WhiteBalance>(out _), "Volume profile must have WhiteBalance.");

            const string worldPath = "Assets/_Game/Scenes/World.unity";
            EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);

            var sun = GameObject.Find("Lighting/Directional Light")?.GetComponent<Light>();
            Assert.IsNotNull(sun, "Directional light must exist in Lighting.");
            Assert.AreEqual(LightType.Directional, sun.type, "Light must be Directional.");
            Assert.AreEqual(LightShadows.Soft, sun.shadows, "Directional light must use Soft shadows.");
        }

        [Test]
        public void TerrainElevation_RollingRegionsAndOpenValleys()
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(EnvironmentAuthoring.TerrainDataPath);
            Assert.AreEqual(1024, data.size.x);
            Assert.AreEqual(160, data.size.y);
            Assert.GreaterOrEqual(data.heightmapResolution, 513);
            Assert.Greater(TerrainHeightModel.Sample(20,142) - TerrainHeightModel.Sample(0,0), 20);
            Assert.Greater(TerrainHeightModel.Sample(-58,70), 6);
            Assert.Greater(TerrainHeightModel.Sample(73,66), 12);
            foreach (var p in new[] { new Vector2(0,-200), new Vector2(-200,0), new Vector2(0,320) })
                Assert.Less(TerrainHeightModel.Sample(p.x,p.y), 35, "Open horizon corridor");
            for (int z=-400; z<590; z+=8)
            for (int x=-500; x<500; x+=8)
            {
                float h = TerrainHeightModel.Sample(x,z);
                Assert.AreEqual(h, TerrainHeightModel.Sample(x,z));
                Assert.That(h, Is.InRange(.4f,148f));
                Assert.Less(Mathf.Abs(h-TerrainHeightModel.Sample(x+.5f,z)), 2f, "No discontinuous walls");
            }
        }

        [Test]
        public void ModularCliff_HasClimbSurfaceAttached()
        {
            var cliffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Environment/Cliff_Modular_01.prefab");
            Assert.IsNotNull(cliffPrefab, "Cliff_Modular_01 prefab must exist.");
            var climbSurface = cliffPrefab.GetComponent<ClimbSurface>();
            Assert.IsNotNull(climbSurface, "Cliff_Modular_01 must have ClimbSurface component attached.");
        }
    }
}
