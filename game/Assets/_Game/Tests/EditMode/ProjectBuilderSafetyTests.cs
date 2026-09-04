using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using WanderingCity.Editor;

namespace WanderingCity.Tests
{
    public sealed class ProjectBuilderSafetyTests
    {
        [SetUp]
        public void SetUp()
        {
            ProjectBuilder.Prepare();
        }

        [Test]
        public void WorldSceneHasCompleteAuthoredHierarchy()
        {
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            Assert.IsTrue(File.Exists(worldPath), "World.unity must exist");

            var scene = EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid(), "Scene must be valid");

            var env = GameObject.Find("Environment");
            Assert.IsNotNull(env, "Environment root must exist in World.unity");

            var terrain = Object.FindFirstObjectByType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist in World.unity");
            Assert.IsNotNull(terrain.terrainData, "TerrainData must be assigned");

            var lighting = GameObject.Find("Lighting");
            Assert.IsNotNull(lighting, "Lighting root must exist in World.unity");

            var nav = GameObject.Find("Navigation");
            Assert.IsNotNull(nav, "Navigation root must exist in World.unity");

            var gameplay = GameObject.Find("Gameplay");
            Assert.IsNotNull(gameplay, "Gameplay root must exist in World.unity");

            var spawn = GameObject.Find("SpawnPoints");
            Assert.IsNotNull(spawn, "SpawnPoints root must exist in World.unity");
        }

        [Test]
        public void GroundYSamplesTerrainHeightWithoutInvalidValues()
        {
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);

            float yCenter = WorldBuilder.GroundY(0, 0, -999f);
            Assert.AreNotEqual(-999f, yCenter, "GroundY should return a computed ground height near base camp");
            Assert.IsFalse(float.IsNaN(yCenter));
            Assert.IsFalse(float.IsInfinity(yCenter));

            float yMountain = WorldBuilder.GroundY(180, 8, -999f);
            Assert.Greater(yMountain, yCenter + 15f, "Perimeter mountain elevation should be higher than valley floor");
        }
    }
}
