#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    // Opt-in verification of the real Windows player. Uses an isolated save, never the player's journey.
    public sealed class BuildSmoke : MonoBehaviour
    {
        static string output;
        RenderTexture target;
        float deadline;
        void OnEnable() { deadline = Time.realtimeSinceStartup + 120; Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }
        void OnLog(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { File.WriteAllText(Path.Combine(output, "smoke-failure.txt"), message + "\n" + stack); Application.Quit(1); } }
        void Update() { if (Time.realtimeSinceStartup > deadline) { Debug.LogError("Build smoke timed out"); enabled = false; } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-qaOutput"); if (index < 0 || index + 1 >= args.Length) return;
            output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(output);
            Application.runInBackground = true;
            GameSession.SavePathOverride = Path.Combine(output, "smoke-" + Guid.NewGuid().ToString("N"), "journey.json");
            var go = new GameObject("Build verification"); DontDestroyOnLoad(go); go.AddComponent<BuildSmoke>();
        }
        IEnumerator Start()
        {
            yield return new WaitUntil(() => GameSession.Current != null && GameSession.Current.Hud != null);
            var game = GameSession.Current;
            target = new RenderTexture(1920, 1080, 24); target.Create();
            var canvas = FindFirstObjectByType<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = 1;
            for (int i = 0; i < 30; i++) yield return null;
            yield return Capture("01-title.png");
            game.Begin(false); game.SetMenu(false);
            for (int i = 0; i < 60; i++) yield return null;
            yield return Capture("02-camp.png");
            foreach (string page in new[] { "inventory", "map", "craft" }) { game.SetMenu(true, page); for (int i = 0; i < 5; i++) yield return null; yield return Capture("03-" + page + ".png"); }
            game.SetMenu(false);
            for (int i = 0; i < 6; i++) { var item = game.World.Interactions.Find(n => n.Id == "wood-" + i); MoveTo(game, item.transform.position + Vector3.back); item.Interact(); }
            for (int i = 0; i < 3; i++) { var item = game.World.Interactions.Find(n => n.Id == "stone-" + i); MoveTo(game, item.transform.position + Vector3.back); item.Interact(); }
            MoveTo(game, new Vector3(2, .2f, 2));
            foreach (string recipe in new[] { "potion", "floor", "wall", "wall", "roof" }) game.Craft(recipe);
            foreach (var part in new[] { ("floor", 0), ("wall", 0), ("wall", 1), ("roof", 0) }) Require(Rules.Place(game.State, part.Item1, -2, 0, part.Item2, true), "place " + part.Item1);
            game.World.RebuildStructures();
            MoveTo(game, new Vector3(20, .2f, 112)); Require(WorldBuilder.Region(game.Player.transform.position) == "ruins", "ruins capture location");
            for (int i = 0; i < 60; i++) yield return null;
            yield return Capture("04-ruins.png");
            foreach (var enemy in game.World.Enemies.Where(e => e.Id.StartsWith("enemy-camp-"))) enemy.Damage(game.Balance.enemyHealth);
            var reward = game.World.Interactions.Find(n => n.Id == "camp-reward"); MoveTo(game, reward.transform.position + Vector3.right * 2); reward.Interact();
            MoveTo(game, new Vector3(2, .2f, 2)); game.Upgrade();
            Require(Rules.Objective(game.State).StartsWith("旅程完成"), "complete journey"); Require(game.Save(), "save"); Require(game.Saves.Load(out var restored, out _), "restore"); Require(JsonUtility.ToJson(restored) == JsonUtility.ToJson(game.State), "round trip");
            MoveTo(game, new Vector3(-6, .5f, -6)); Camera.main.GetComponent<OrbitCamera>().Yaw = 0;
            for (int i = 0; i < 15; i++) yield return null;
            yield return Capture("05-home.png");
            var frameTimes = new List<float>(); long gcBefore = GC.CollectionCount(0); double start = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < 300; i++) { RenderFrame(); yield return null; frameTimes.Add(Time.unscaledDeltaTime * 1000); }
            frameTimes.Sort();
            File.WriteAllText(Path.Combine(output, "smoke-result.json"), JsonUtility.ToJson(new Report { passed = true, unity = Application.unityVersion, device = SystemInfo.graphicsDeviceName, cpu = SystemInfo.processorType, width = Screen.width, height = Screen.height, averageMs = frameTimes.Average(), p95Ms = frameTimes[(int)(frameTimes.Count * .95f)], fps = (float)(300 / (Time.realtimeSinceStartupAsDouble - start)), allocatedMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f, gcCollections = (int)(GC.CollectionCount(0) - gcBefore), objective = Rules.Objective(game.State) }, true));
            Debug.Log("WANDERING_CITY_SMOKE_OK"); Application.Quit(0);
        }
        void MoveTo(GameSession game, Vector3 position) { game.State.x = position.x; game.State.y = position.y; game.State.z = position.z; game.Player.RestorePosition(); Physics.SyncTransforms(); }
        void RenderFrame() { Canvas.ForceUpdateCanvases(); RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target }); }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); RenderFrame();
            var previous = RenderTexture.active; RenderTexture.active = target; var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply(); RenderTexture.active = previous;
            var samples = texture.GetPixels32(); Require(samples.Any(p => p.r > 40 || p.g > 40 || p.b > 40), "non-black rendered image " + name);
            File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG()); Destroy(texture); yield return null;
        }
        void OnDestroy() { if (target != null) { target.Release(); Destroy(target); } }
        void Require(bool result, string step) { if (!result) { Debug.LogError("SMOKE FAILED: " + step); File.WriteAllText(Path.Combine(output, "smoke-failure.txt"), step); Application.Quit(1); throw new InvalidOperationException(step); } }
        [Serializable] sealed class Report { public bool passed; public string renderMode = "URP offscreen, 300 frames; not a full gameplay benchmark"; public string unity, device, cpu, objective; public int width, height, gcCollections; public float averageMs, p95Ms, fps, allocatedMB; }
    }
}
#endif
