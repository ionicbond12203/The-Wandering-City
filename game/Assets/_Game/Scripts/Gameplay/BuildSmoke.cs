#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    // Opt-in verification of the real Windows player. Uses an isolated save, never the player's journey.
    public sealed class BuildSmoke : MonoBehaviour
    {
        static string output;

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
            Camera.main.GetComponent<OrbitCamera>().AcceptInput = false;
            var canvas = FindFirstObjectByType<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = 1;
            for (int i = 0; i < 30; i++) yield return null;
            yield return Capture("01-title.png");
            game.Begin(false); game.SetMenu(false);
            for (int i = 0; i < 60; i++) yield return null;
            yield return Capture("02-camp.png");
            var orbit = Camera.main.GetComponent<OrbitCamera>();
            orbit.Pitch = 10;
            yield return Capture("open-world-spawn.png");
            string[] directions={"north","east","south","west"};
            for(int side=0;side<4;side++)
            {
                orbit.Yaw=side*90;
                for(int f=0;f<12;f++) yield return null;
                yield return Capture("open-world-"+directions[side]+".png");
            }
            MoveTo(game,WorldBuilder.GroundPoint(-38,48,.15f));orbit.Yaw=-35;
            for(int f=0;f<15;f++) yield return null;
            yield return Capture("open-world-forest.png");
            var terrain=Terrain.activeTerrain;
            Require(terrain.terrainData.detailScatterMode==DetailScatterMode.InstanceCountMode,"instance count grass scatter");
            long detailCount=0;
            for(int layer=0;layer<terrain.terrainData.detailPrototypes.Length;layer++)
                foreach(int count in terrain.terrainData.GetDetailLayer(0,0,terrain.terrainData.detailWidth,terrain.terrainData.detailHeight,layer))detailCount+=count;
            File.WriteAllText(Path.Combine(output,"terrain-details.txt"),"Detail instance density sum: "+detailCount);

            MoveTo(game,WorldBuilder.GroundPoint(45,47,.15f));orbit.Yaw=25;
            for(int f=0;f<15;f++) yield return null;
            yield return Capture("open-world-quarry.png");
            MoveTo(game,WorldBuilder.GroundPoint(0,0,.15f));orbit.Yaw=180;orbit.Pitch=5;
            orbit.SetDistance(2.7f);
            for(int f=0;f<20;f++) yield return null;
            yield return Capture("character-close.png");
            orbit.SetDistance(4.6f);orbit.Yaw=0;orbit.Pitch=12;

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
            // Exercise the new loop through its real motor/interaction APIs, without claiming a manual playthrough.
            game.Player.enabled = false;
            // Resolve positions from GroundY + structure offsets instead of hardcoded Y
            float shelfGround = WorldBuilder.GroundY(51, -8, 0);
            float shelfHeight = 6f;
            float mesaGround = WorldBuilder.GroundY(78, 8, 0);
            float mesaHeight = 14f;
            float canyonGround = WorldBuilder.GroundY(81, 32, 0);

            MoveTo(game, new Vector3(53, shelfGround + 0.1f, -17.6f));
            game.Player.transform.rotation = Quaternion.identity;
            var traversal = game.Player.Traversal;
            Require(traversal.TryClimb(), "enter authored shelf climb");
            for (int i = 0; i < 65; i++) { traversal.Simulate(.02f, Vector3.zero, Vector2.up, false, false, false, false, false, false); yield return null; }
            yield return Capture("06-climb.png");
            for (int i = 0; i < 120; i++) { traversal.Simulate(.02f, Vector3.zero, Vector2.up, false, false, false, false, false, false); yield return null; }
            float shelfTopY = shelfGround + shelfHeight;
            Require(game.Player.transform.position.y > shelfTopY - 0.1f && !traversal.BlocksCombat, "reach shelf ledge");
            MoveTo(game, new Vector3(51, shelfTopY + 0.1f, -9)); game.World.Interactions.Find(n => n.Id == "shelf-cache").Interact(); Require(game.State.openedTreasureIds.Contains("shelf-cache"), "exploration treasure");
            float mesaTopY = mesaGround + mesaHeight;
            MoveTo(game, new Vector3(75, mesaTopY + 0.1f, 8)); game.World.Interactions.Find(n => n.Id == "mesa-beacon").Interact(); Require(game.State.activatedTeleportIds.Contains("mesa-beacon"), "activate mesa beacon");
            for (int i = 0; i < 15; i++) yield return null;
            orbit.Yaw=-65;
            for(int f=0;f<15;f++)yield return null;
            yield return Capture("07-mesa.png");
            yield return Capture("open-world-high-viewpoint.png");
            MoveTo(game, new Vector3(81, mesaTopY, 21)); Require(traversal.TryGlide(), "deploy wind sail");
            game.Player.GlideSail.gameObject.SetActive(true);
            for (int i = 0; i < 45; i++) { traversal.Simulate(.02f, Vector3.forward, Vector2.up, false, false, false, false, false, false); yield return null; }
            yield return Capture("08-glide.png");
            yield return Capture("open-world-glide.png");
            game.Player.GlideSail.gameObject.SetActive(false);
            MoveTo(game, new Vector3(81, canyonGround + 0.1f, 28)); game.World.Interactions.Find(n => n.Id == "echo-west").Interact();
            MoveTo(game, new Vector3(81, canyonGround + 0.1f, 32)); game.World.Interactions.Find(n => n.Id == "echo-east").Interact();
            MoveTo(game, new Vector3(81, canyonGround + 0.1f, 34)); game.World.Interactions.Find(n => n.Id == "canyon-cache").Interact();
            Require(game.State.completedPuzzleIds.Contains("echo-puzzle") && game.State.openedTreasureIds.Contains("canyon-cache"), "puzzle reward");
            Require(game.Save(), "exploration save"); Require(game.Saves.Load(out var explorationSave, out _), "exploration reload");
            Require(explorationSave.activatedTeleportIds.Contains("mesa-beacon") && explorationSave.openedTreasureIds.Count == 2, "persistent exploration");
            game.SetMenu(true, "map"); for (int i = 0; i < 5; i++) yield return null; yield return Capture("09-exploration-map.png");
            game.SetMenu(false); Require(game.Exploration.Teleport("mesa-beacon"), "teleport to safe spawn");
            game.Player.enabled = true;
            using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
            using var allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            using var mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");
            double batchSum=0,drawSum=0,allocSum=0,cpuSum=0,gpuSum=0; int gpuSamples=0;
            var timings=new FrameTiming[1];
            var frameTimes = new List<float>(); long gcBefore = GC.CollectionCount(0); double start = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < 300; i++)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                frameTimes.Add(Time.unscaledDeltaTime * 1000);
                batchSum+=batches.LastValue;drawSum+=draws.LastValue;allocSum+=allocations.LastValue;cpuSum+=mainThread.LastValue/1000000.0;
                if(FrameTimingManager.GetLatestTimings(1,timings)>0 && timings[0].gpuFrameTime>0){gpuSum+=timings[0].gpuFrameTime;gpuSamples++;}
            }
            frameTimes.Sort();
            File.WriteAllText(Path.Combine(output, "smoke-result.json"), JsonUtility.ToJson(new Report { passed = true, batches = batches.Valid && batchSum>0?(float)(batchSum/300):-1, drawCalls = draws.Valid && drawSum>0?(float)(drawSum/300):-1, gcBytesPerFrame=allocations.Valid?(float)(allocSum/300):-1, cpuMainThreadMs=mainThread.Valid?(float)(cpuSum/300):-1, gpuMs=gpuSamples>0?(float)(gpuSum/gpuSamples):-1, unity = Application.unityVersion, device = SystemInfo.graphicsDeviceName, cpu = SystemInfo.processorType, width = Screen.width, height = Screen.height, averageMs = frameTimes.Average(), p95Ms = frameTimes[(int)(frameTimes.Count * .95f)], fps = (float)(300 / (Time.realtimeSinceStartupAsDouble - start)), allocatedMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f, gcCollections = (int)(GC.CollectionCount(0) - gcBefore), objective = Rules.Objective(game.State) }, true));
            Debug.Log("WANDERING_CITY_SMOKE_OK"); Application.Quit(0);
        }
        void MoveTo(GameSession game, Vector3 position) { game.State.x = position.x; game.State.y = position.y; game.State.z = position.z; game.Player.RestorePosition(); Physics.SyncTransforms(); }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            Require(texture.width==1920 && texture.height==1080,"native 1080p framebuffer");
            var samples = texture.GetPixels32(); Require(samples.Any(p => p.r > 40 || p.g > 40 || p.b > 40), "non-black rendered image " + name);
            // Magenta detection: fail if >1% of pixels are magenta (shader error indicator)
            int magentaCount = 0;
            foreach (var p in samples) { if (p.r > 200 && p.g < 80 && p.b > 200) magentaCount++; }
            float magentaRatio = (float)magentaCount / samples.Length;
            if (magentaRatio > 0.01f) Debug.LogError($"MAGENTA DETECTED in {name}: {magentaRatio:P2} of pixels — shader error");
            Require(magentaRatio <= 0.01f, "zero-magenta " + name);
            File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG()); Destroy(texture); yield return null;
        }

        void Require(bool result, string step) { if (!result) { Debug.LogError("SMOKE FAILED: " + step); File.WriteAllText(Path.Combine(output, "smoke-failure.txt"), step); Application.Quit(1); throw new InvalidOperationException(step); } }
        [Serializable] sealed class Report { public bool passed; public string renderMode = "URP native game framebuffer, 300 frames after screenshots; unavailable counters = -1; not a full gameplay benchmark"; public string unity, device, cpu, objective; public int width, height, gcCollections; public float averageMs, p95Ms, fps, allocatedMB, batches, drawCalls, gcBytesPerFrame, cpuMainThreadMs, gpuMs; }
    }
}
#endif
