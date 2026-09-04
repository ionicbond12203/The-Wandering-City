#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    // Explicit opt-in, isolated save, real player URP render request; never installed in release builds.
    public sealed class MapBuildQA : MonoBehaviour
    {
        static string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-mapQA");
            if (index < 0 || index + 1 >= args.Length) return;
            output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(output);
            GameSession.SavePathOverride = Path.Combine(output, "isolated-" + Guid.NewGuid().ToString("N"), "journey.json");
            Application.runInBackground = true;
            var go = new GameObject("Map build QA"); DontDestroyOnLoad(go); go.AddComponent<MapBuildQA>();
        }
        void OnEnable() { Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }
        void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            File.WriteAllText(Path.Combine(output, "failure.txt"), message + "\n" + stack); Application.Quit(1);
        }
        void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        IEnumerator Start()
        {
            yield return new WaitUntil(() => GameSession.Current != null && GameSession.Current.Hud != null);
            var game = GameSession.Current; game.Begin(true); game.Player.enabled = false;
            Camera.main.GetComponent<OrbitCamera>().AcceptInput = false;
            var canvas = FindFirstObjectByType<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main; canvas.planeDistance = 1;
            Require(WorldMapData.Load().Texture != null, "Map texture missing from build");
            var sizes = new[] { new Vector2Int(1280,720), new Vector2Int(1366,768), new Vector2Int(1600,900), new Vector2Int(1920,1080), new Vector2Int(2560,1440), new Vector2Int(2560,1080) };
            var names = new[] { "hud-720p.png", "hud-768p.png", "hud-900p.png", "hud-1080p.png", "hud-1440p.png", "hud-ultrawide.png" };
            for (int i = 0; i < sizes.Length; i++)
            {
                Screen.SetResolution(sizes[i].x, sizes[i].y, FullScreenMode.Windowed);
                for (int f = 0; f < 40; f++) yield return null;
                Require(Screen.width == sizes[i].x && Screen.height == sizes[i].y, "Requested framebuffer resolution was not applied: " + sizes[i]);
                var layout = FindFirstObjectByType<SafeAreaHud>();
                foreach (var rect in layout.Critical) Require(SafeAreaHud.Inside(rect, Screen.safeArea, Camera.main), "Clipped " + rect.name);
                yield return Capture(names[i]);
            }
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            for (int f = 0; f < 30; f++) yield return null;
            // Exercise a distant, still-undiscovered quest target and both map orientations.
            game.State.claimed.Add("wood-0"); game.State.claimed.Add("stone-0"); game.State.craftedPotion = true; game.State.placedBuilding = true;
            game.Hud.Minimap.Refresh();
            Require(game.Hud.Minimap.ObjectiveMarker.anchoredPosition.magnitude > 85, "Objective should clamp to edge");
            Require(game.Hud.Minimap.PlayerMarker.anchoredPosition == Vector2.zero, "Player must remain centered");
            yield return Capture("minimap-objective-edge.png");
            game.Hud.Minimap.Orientation = MinimapOrientation.RotateWithPlayer; game.Player.transform.rotation = Quaternion.Euler(0, 90, 0);
            yield return Capture("minimap-rotate.png");
            // QA state is deliberately seeded for marker selection and persisted teleport coverage.
            foreach (var point in game.Exploration.Points.Values) ExplorationRules.Discover(game.State, point.Id);
            ExplorationRules.Activate(game.State, "base-beacon"); ExplorationRules.Activate(game.State, "mesa-beacon");
            Require(game.Save(), "QA save failed"); Require(game.Saves.Load(out var restored, out _), "QA restore failed");
            Require(ExplorationRules.CanTeleport(restored, "mesa-beacon"), "Teleport persistence failed");
            game.SetMenu(true, "map"); for (int f = 0; f < 10; f++) yield return null;
            var map = FindFirstObjectByType<ExplorationMapInput>();
            var pointer = new PointerEventData(EventSystem.current) { position = new Vector2(660, 555), scrollDelta = Vector2.up * 4, pointerPressRaycast = new RaycastResult { module = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() } };
            map.OnScroll(pointer); Require(map.Content.localScale.x > 1, "Map zoom failed");
            yield return Capture("world-map.png");
            Require(game.Exploration.Teleport("mesa-beacon"), "Activated teleport failed");
            File.WriteAllText(Path.Combine(output, "result.json"), "{\"passed\":true,\"resolutions\":6,\"capture\":\"Windows Build URP render request with camera-space HUD\",\"teleportPersistence\":true}");
            Application.Quit(0);
        }
        IEnumerator Capture(string name)
        {
            yield return null; // Let HUD text and orientation catch up to seeded QA state.
            yield return new WaitForEndOfFrame();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            RenderPipeline.SubmitRenderRequest(Camera.main, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); texture.Apply();
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
            Require(texture.GetPixels32().Any(p => p.r > 40 || p.g > 40 || p.b > 40), "Black capture " + name);
            File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG()); Destroy(texture);
        }
    }
}
#endif
