using NUnit.Framework;
using UnityEngine;

namespace WanderingCity.Tests
{
    public sealed class MapHudTests
    {
        [Test] public void BakedMapIsAvailableThroughBuildResourceReference()
        {
            var map = WorldMapData.Load(); Assert.NotNull(map); Assert.NotNull(map.Texture);
            Assert.AreEqual(1024, map.Texture.width); Assert.Greater(map.WorldBounds.size.x, 0);
            Assert.That(Vector2.Distance(map.WorldToUV(map.WorldBounds.min), Vector2.zero), Is.LessThan(.0001));
            Assert.That(Vector2.Distance(map.WorldToUV(map.WorldBounds.max), Vector2.one), Is.LessThan(.0001));
            Assert.AreEqual(Vector2.one * .5f, map.WorldToUV(map.WorldBounds.center));
            Assert.AreEqual(new Vector2(0, 625), map.WorldToMap(map.WorldBounds.min, Vector2.one * 625));
        }
        [TestCase(0, 0, 0, 0, 0)]
        [TestCase(0, 10, 0, 0, 20)]
        [TestCase(1000, 0, 0, 86, 0)]
        [TestCase(0, 1000, 90, -86, 0)]
        [TestCase(1000, 0, 90, 0, 86)]
        public void PlayerCenterBearingAndClamping(float x, float z, float yaw, float expectedX, float expectedY)
        {
            var player = new Vector3(41, 25, -17);
            var actual = WorldMapData.MinimapOffset(player + new Vector3(x, 7, z), player, yaw, 2, 86);
            Assert.Less(Vector2.Distance(actual, new Vector2(expectedX, expectedY)), .001f);
            Assert.LessOrEqual(actual.magnitude, 86.001f);
        }
        [Test] public void DiagonalClampPreservesBearingAndMarkerFitsCircle()
        {
            var result = WorldMapData.MinimapOffset(new Vector3(900, 0, 1200), Vector3.zero, 0, 1, 86);
            Assert.AreEqual(.75f, result.x / result.y, .0001f); Assert.AreEqual(86, result.magnitude, .001f);
            Assert.Less(result.magnitude + Mathf.Sqrt(8 * 8 + 8 * 8), 100);
        }
        [Test] public void UnknownPointsStayHiddenAndDiscoverySurvivesSerialization()
        {
            var state = new GameState(); Assert.IsFalse(WorldMapData.Visible(state, "mesa-beacon"));
            ExplorationRules.Activate(state, "mesa-beacon");
            var restored = JsonUtility.FromJson<GameState>(JsonUtility.ToJson(state));
            Assert.IsTrue(WorldMapData.Visible(restored, "mesa-beacon")); Assert.IsTrue(ExplorationRules.CanTeleport(restored, "mesa-beacon"));
        }
        [TestCase(1280, 720)] [TestCase(1366, 768)] [TestCase(1600, 900)]
        [TestCase(1920, 1080)] [TestCase(2560, 1440)] [TestCase(2560, 1080)]
        public void SafeAreaAnchorsIncludeAsymmetricInsets(int width, int height)
        {
            var root = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                var screen = new Vector2(width, height); var safe = new Rect(48, 24, width - 80, height - 60);
                SafeAreaHud.Apply(root, safe, screen);
                Assert.AreEqual(safe.min / screen, root.anchorMin); Assert.AreEqual(safe.max / screen, root.anchorMax);
                Assert.AreEqual(Vector2.zero, root.offsetMin); Assert.AreEqual(Vector2.zero, root.offsetMax);
                var group = SafeAreaHud.Group(root, "TopRightHud", Vector2.one, new Vector2(260, 350), new Vector2(-40, -36));
                Assert.AreEqual(Vector2.one, group.anchorMax); Assert.AreEqual(Vector2.one, group.pivot);
                // Expand scaling leaves at least the reference canvas dimensions at all supported aspect ratios.
                float scale = Mathf.Min(width / 1920f, height / 1080f);
                Assert.GreaterOrEqual(safe.width / scale, 1700); Assert.GreaterOrEqual(safe.height / scale, 980);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
        }
    }
}
