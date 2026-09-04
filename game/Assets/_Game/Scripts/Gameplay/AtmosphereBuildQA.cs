#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    public sealed class AtmosphereBuildQA : MonoBehaviour
    {
        static string output; GameSession game; OrbitCamera orbit;
        readonly List<Sample> samples = new List<Sample>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-atmosphereQA"); if (i < 0 || i + 1 >= args.Length) return;
            output = Path.GetFullPath(args[i + 1]); Directory.CreateDirectory(output);
            GameSession.SavePathOverride = Path.Combine(output, "isolated-" + Guid.NewGuid().ToString("N"), "journey.json");
            Application.runInBackground = true; var go = new GameObject("Atmosphere build QA"); DontDestroyOnLoad(go); go.AddComponent<AtmosphereBuildQA>();
        }
        void OnEnable() => Application.logMessageReceived += Log;
        void OnDisable() => Application.logMessageReceived -= Log;
        void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { File.WriteAllText(Path.Combine(output, "failure.txt"), message + "\n" + stack); Application.Quit(1); } }
        static void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
        IEnumerator Start()
        {
            yield return new WaitUntil(() => GameSession.Current != null && GameSession.Current.Weather != null);
            game = GameSession.Current; game.Audio.Tick(.1f); game.Begin(true); game.Player.enabled = false;
            game.Weather.Automatic = false; orbit = Camera.main.GetComponent<OrbitCamera>(); orbit.AcceptInput = false;
            var canvas = FindFirstObjectByType<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = 1;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            Move(WorldBuilder.GroundPoint(-145, -203, .15f)); orbit.Yaw = 0; orbit.Pitch = 15;
            foreach (WeatherKind kind in Enum.GetValues(typeof(WeatherKind)))
            {
                game.Weather.Request(kind); game.Weather.Tick(20); game.Audio.Tick(4);
                for (int i = 0; i < 70; i++) yield return null;
                yield return Capture(kind == WeatherKind.Clear ? "weather-clear" : kind == WeatherKind.Cloudy ? "weather-cloudy" : "weather-rain");
            }
            game.Weather.Request(WeatherKind.Clear); game.Weather.Tick(20); yield return Capture("water-stream");
            foreach (int index in new[] { 1, 3 })
            {
                var r = ExpansionCatalog.Regions[index]; var p = r.Center + new Vector2(-6, -23); Move(WorldBuilder.GroundPoint(p.x, p.y, .15f));
                var look = r.At(1) - p; orbit.Yaw = Mathf.Atan2(look.x, look.y) * Mathf.Rad2Deg; orbit.Pitch = 3; game.Audio.Tick(4); game.Audio.Tick(3);
                yield return Capture(index == 1 ? "lighting-forest" : "lighting-ruins");
            }
            // Drive real encounter state, then verify the director releases its combat hold.
            foreach (bool elite in new[] { false, true })
            {
                var enemy = game.World.Enemies.First(e => e.Elite == elite && e.gameObject.activeInHierarchy);
                Move(WorldBuilder.GroundPoint(enemy.Home.x, enemy.Home.z - 5, .15f)); enemy.Damage(1); game.Audio.Tick(.1f);
                Require(game.Audio.State.Current == (elite ? MusicMood.Elite : MusicMood.Combat), "Combat override failed");
            }
            Move(WorldBuilder.GroundPoint(-145, -203, .15f)); game.World.ResetEnemies(); game.Audio.Tick(6); game.Audio.Tick(3);
            Require(game.Audio.State.Current == MusicMood.Meadow, "Exploration return failed");
            game.SetMenu(true); game.Audio.Tick(.1f); Require(game.Audio.State.Snapshot == "Paused", "Pause snapshot failed");
            game.Audio.Preferences.Music = .3f; Require(game.Audio.SavePreferences(), "Audio settings save"); Require(Mathf.Abs(AudioPreferences.Load(game.Audio.PreferencesPath).Music - .3f) < .001f, "Audio settings reload");
            Require(game.Audio.MusicStarts == 0, "Empty music slots unexpectedly played");
            game.SetMenu(true, "settings"); yield return Capture("audio-settings");
            File.WriteAllLines(Path.Combine(output, "audio-state-log.txt"), game.Audio.StateLog);
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(new Report { passed = true, samples = samples.ToArray(), allocatedMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f }, true)); Application.Quit(0);
        }
        void Move(Vector3 p)
        {
            game.State.x = p.x; game.State.y = p.y; game.State.z = p.z; game.Player.RestorePosition(); Physics.SyncTransforms();
            Require(Vector2.Distance(ExpansionCatalog.XZ(p), ExpansionCatalog.XZ(game.Player.transform.position)) < .2f, "QA position corrected by collision");
        }
        IEnumerator Capture(string name)
        {
            for (int i = 0; i < 20; i++) yield return null;
            var durations = new float[120]; var audio = new float[1024]; float peak = 0;
            double previous = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < durations.Length; i++)
            {
                yield return null; double now = Time.realtimeSinceStartupAsDouble; durations[i] = (float)(1000 * (now - previous)); previous = now;
                AudioListener.GetOutputData(audio, 0); foreach (var sample in audio) peak = Mathf.Max(peak, Mathf.Abs(sample));
            }
            yield return new WaitForEndOfFrame();
            var watch = System.Diagnostics.Stopwatch.StartNew(); var target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
            RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previousRT = RenderTexture.active; RenderTexture.active = target; var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply(); RenderTexture.active = previousRT; RenderTexture.ReleaseTemporary(target);
            File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG()); Destroy(image);
            Array.Sort(durations); samples.Add(new Sample { name = name, hiddenFrameMeanMs = durations.Average(), hiddenFrameP95Ms = durations[114], captureMs = (float)watch.Elapsed.TotalMilliseconds, rainParticles = game.Weather.Rain.particleCount, audioPeak = peak, fogDensity = RenderSettings.fogDensity });
        }
        [Serializable] sealed class Sample { public string name; public float hiddenFrameMeanMs, hiddenFrameP95Ms, captureMs, audioPeak, fogDensity; public int rainParticles; }
        [Serializable] sealed class Report
        {
            public bool passed; public float allocatedMB; public Sample[] samples;
            public string music = "No licensed score in repository; playlist slots intentionally empty. Ambience and cues are original synthesized sound design, not final music.";
            public string capture = "Windows Build URP offscreen; 120 hidden-window frame intervals per state include frame limiting, not a foreground FPS certification.";
        }
    }
}
#endif
