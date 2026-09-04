using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using WanderingCity.Editor;

namespace WanderingCity.Tests
{
    public sealed class AtmosphereTests
    {
        [OneTimeSetUp] public void Prepare() => ProjectBuilder.Prepare();
        [Test] public void MusicRegionHysteresisDoesNotFollowBoundaryJitter()
        {
            var state = new MusicStateMachine();
            for (int i = 0; i < 20; i++) state.Tick(.2f, i % 2 == 0 ? MusicMood.Forest : MusicMood.Meadow, true, false, false, false, false, false);
            Assert.AreEqual(MusicMood.Meadow, state.Current);
            state.Tick(3.1f, MusicMood.Forest, true, false, false, false, false, false); Assert.AreEqual(MusicMood.Forest, state.Current);
        }
        [Test] public void EliteAndCombatHoldThenReturnToStableExploration()
        {
            var state = new MusicStateMachine(); state.Tick(4, MusicMood.Ruins, true, false, false, false, false, false);
            state.Tick(.1f, MusicMood.Ruins, true, false, false, true, false, false); Assert.AreEqual(MusicMood.Combat, state.Current);
            state.Tick(.1f, MusicMood.Ruins, true, false, false, true, true, false); Assert.AreEqual(MusicMood.Elite, state.Current);
            state.Tick(2, MusicMood.Ruins, true, false, false, false, false, false); Assert.AreEqual(MusicMood.Elite, state.Current);
            state.Tick(4, MusicMood.Ruins, true, false, false, false, false, false); Assert.AreEqual(MusicMood.Ruins, state.Current); Assert.AreEqual("Exploration", state.Snapshot);
        }
        [Test] public void PauseMenuAndInteriorSnapshotsHavePriorityWithoutResettingMusic()
        {
            var state = new MusicStateMachine(); state.Tick(4, MusicMood.Forest, true, false, false, false, false, true);
            Assert.AreEqual("Interior/Quiet", state.Snapshot);
            state.Tick(10, MusicMood.Ruins, true, true, false, false, false, false); Assert.AreEqual("Paused", state.Snapshot); Assert.AreEqual(MusicMood.Forest, state.Current);
            state.Tick(1, MusicMood.Ruins, true, true, true, false, false, false); Assert.AreEqual("Menu", state.Snapshot);
            state.Tick(1, MusicMood.Meadow, false, true, true, false, false, false); Assert.AreEqual(MusicMood.Menu, state.Current);
        }
        [Test] public void VolumeSettingsPersistAndInvalidDataIsSafe()
        {
            var folder = Path.Combine(Path.GetTempPath(), "wandering-audio-" + Guid.NewGuid().ToString("N")); var path = Path.Combine(folder, "audio.json");
            try
            {
                var preferences = new AudioPreferences { Master = 0, Music = .2f, SFX = .4f, Ambience = .6f }; Assert.IsTrue(preferences.Save(path));
                var read = AudioPreferences.Load(path); Assert.AreEqual(0, read.Master); Assert.AreEqual(.2f, read.Music); Assert.AreEqual(.4f, read.SFX); Assert.AreEqual(.6f, read.Ambience);
                preferences.Master = float.NaN; preferences.Music = 5; preferences.Sanitize(); Assert.AreEqual(.8f, preferences.Master); Assert.AreEqual(1, preferences.Music);
                File.WriteAllText(path, "broken settings"); Assert.IsNotNull(AudioPreferences.Load(path));
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        [Test] public void WeatherTransitionsAreContinuousAndReachTarget()
        {
            var weather = new WeatherBlend(); weather.Request(WeatherKind.LightRain, 10); weather.Tick(5);
            Assert.AreEqual(.5f, weather.Weights.z, .001f); var previous = weather.Weights; weather.Request(WeatherKind.Cloudy, 10); Assert.AreEqual(previous, weather.Weights);
            weather.Tick(10); Assert.AreEqual(Vector3.up, weather.Weights); Assert.AreEqual(1, weather.Weights.x + weather.Weights.y + weather.Weights.z, .001f);
        }
        [Test] public void MixerAndWaterAssetsAreValidAndPondLeavesPoiLocationsDry()
        {
            var mixer = Resources.Load<AudioMixer>("WorldAudioMixer"); Assert.IsNotNull(mixer);
            foreach (string group in new[] { "Master", "Music", "Ambience", "SFX", "UI" }) Assert.IsTrue(mixer.FindMatchingGroups(group).Any(g => g.name == group), group);
            foreach (string snapshot in new[] { "Exploration", "Combat", "Menu", "Interior/Quiet", "Paused" }) Assert.IsNotNull(mixer.FindSnapshot(snapshot), snapshot);
            var atmosphere = Resources.Load<WorldAtmosphere>("WorldAtmosphere");
            Assert.IsNotNull(atmosphere.WaterMaterial); Assert.IsNotNull(atmosphere.RainMaterial);
            Assert.IsFalse(ShaderUtil.ShaderHasError(atmosphere.WaterMaterial.shader)); Assert.IsFalse(ShaderUtil.ShaderHasError(atmosphere.RainMaterial.shader)); Assert.IsFalse(ShaderUtil.ShaderHasError(Shader.Find("WanderingCity/StylizedSky")));
            Assert.Less(TerrainHeightModel.Sample(WaterBody.Center.x, WaterBody.Center.y), WaterBody.Level);
            var decorations = Resources.Load<GameObject>("AuthoredEnvironment").transform.Find("ExpandedRegions_v1");
            foreach (Transform region in decorations) foreach (Transform decoration in region)
            {
                var p = decoration.position; float distance = WaterBody.Distance(p.x, p.z);
                Assert.GreaterOrEqual(distance, 1.12f, "Pond should be clear of old decorations");
                if (distance < 1.4f) Assert.AreEqual(TerrainHeightModel.Sample(p.x, p.z), p.y, .02f, "Shore decoration should be grounded");
            }
            foreach (var region in ExpansionCatalog.Regions) foreach (var site in region.Sites) Assert.Greater(WaterBody.Distance(region.Center.x + site.x, region.Center.y + site.y), 1.2f);
        }
    }
}
