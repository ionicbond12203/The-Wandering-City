#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    public sealed class CharacterBuildQA : MonoBehaviour
    {
        static string output;
        GameSession game; PlayerMotor player; CharacterAnimationDriver driver; OrbitCamera orbit;
        readonly List<Sample> samples = new List<Sample>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-characterQA");
            if (index < 0 || index + 1 >= args.Length) return;
            output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(output);
            GameSession.SavePathOverride = Path.Combine(output, "isolated-" + Guid.NewGuid().ToString("N"), "journey.json");
            Application.runInBackground = true;
            var go = new GameObject("Character build QA"); DontDestroyOnLoad(go); go.AddComponent<CharacterBuildQA>();
        }
        void OnEnable() => Application.logMessageReceived += Log;
        void OnDisable() => Application.logMessageReceived -= Log;
        void Log(string text, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            File.WriteAllText(Path.Combine(output, "failure.txt"), text + "\n" + stack); Application.Quit(1);
        }
        static void Require(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); }
        IEnumerator Start()
        {
            yield return new WaitUntil(() => GameSession.Current != null && GameSession.Current.Hud != null);
            game = GameSession.Current; game.Begin(true); player = game.Player; driver = player.VisualAdapter.Driver;
            player.enabled = false; driver.enabled = false; player.Animator.enabled = false;
            orbit = Camera.main.GetComponent<OrbitCamera>(); orbit.AcceptInput = false; orbit.SetDistance(4.6f);
            var canvas = FindFirstObjectByType<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = Camera.main; canvas.planeDistance = 1;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            Move(WorldBuilder.GroundPoint(0, -8, .15f)); orbit.Yaw = 155; orbit.Pitch = 8;
            Step(20); yield return Capture("idle", 0);
            Step(24, Vector3.forward); yield return Capture("run", 0);
            player.Traversal.Simulate(.02f, Vector3.forward, Vector2.up, false, false, true, false, false, false); Animate();
            Step(9, Vector3.forward); yield return Capture("jump", 2);
            var route = game.GetComponent<ExpandedWorld>().Routes[0];
            Move(route.foot + new Vector3(0, .15f, -7.7f)); orbit.Yaw = 0; orbit.Pitch = 5;
            Require(player.Traversal.TryClimb(), "climb entry");
            for (int i = 0; i < 30; i++) { player.Traversal.Simulate(.02f, Vector3.zero, Vector2.up, false, false, false, false, false, false); Animate(); }
            yield return Capture("climb", 12);
            Move(route.launch); orbit.Yaw = 150; orbit.Pitch = 2;
            Require(player.Traversal.TryGlide(), "glide entry"); Step(20, Vector3.forward); yield return Capture("glide", 14);
            Move(WorldBuilder.GroundPoint(0, -8, .15f)); orbit.Yaw = 150; orbit.Pitch = 8; Step(15);
            Require(player.StartAttack(), "attack entry"); Step(16); yield return Capture("combat", 6);
            // Existing authored ramp, sampled at its midpoint; use the collider's real surface Y.
            var ramp = FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Safe outlook approach / " + route.region.Name);
            Require(ramp != null, "slope ramp");
            var probe = ramp.position + Vector3.up * 30;
            Require(Physics.Raycast(probe, Vector3.down, out var hit, 60, game.Balance.solidMask), "slope surface");
            Move(hit.point + Vector3.up * .15f); orbit.Yaw = 0; orbit.Pitch = 8; player.transform.rotation = Quaternion.Euler(0, 90, 0);
            Step(20); yield return Capture("slope", 0);
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(new Report { passed = true, samples = samples.ToArray() }, true));
            Application.Quit(0);
        }
        void Move(Vector3 point)
        {
            game.State.x = point.x; game.State.y = point.y; game.State.z = point.z; game.State.yaw = 0;
            player.RestorePosition(); Physics.SyncTransforms(); driver.ResetPresentation();
            Require(Vector2.Distance(ExpansionCatalog.XZ(point), ExpansionCatalog.XZ(player.transform.position)) < .2f, "QA spawn corrected by collision");
        }
        void Step(int frames, Vector3 direction = default)
        {
            for (int i = 0; i < frames; i++)
            {
                player.AdvanceAction(.02f);
                player.Traversal.Simulate(.02f, direction, direction == Vector3.zero ? Vector2.zero : Vector2.up, false, false, false, false, false, false);
                Animate();
            }
        }
        void Animate() { driver.Tick(.02f); player.Animator.Update(.02f); }
        IEnumerator Capture(string name, int pose)
        {
            Require(driver.Pose == pose, name + " wrong animation pose: " + driver.Pose);
            // Complete the short blend while preserving the sampled gameplay facts / gait speed.
            player.Animator.Update(.15f);
            string expectedState = pose == 0 ? "Locomotion" : pose == 2 ? "JumpStart" : pose == 12 ? "ClimbMove" : pose == 14 ? "Glide" : "Attack1";
            Require(player.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == Animator.StringToHash(expectedState), name + " Animator transition did not finish");
            for (int i = 0; i < 20; i++) yield return null;
            yield return new WaitForEndOfFrame();
            var camera = Camera.main;
            float min = 1, max = 0;
            foreach (var renderer in player.VisualAdapter.Rig.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.transform.IsChildOf(player.Blade) || renderer.transform.IsChildOf(player.GlideSail)) continue;
                var bounds = renderer.bounds;
                for (int c = 0; c < 8; c++)
                {
                    var point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                    float y = camera.WorldToViewportPoint(point).y; min = Mathf.Min(min, y); max = Mathf.Max(max, y);
                }
            }
            float fraction = max - min; if (name == "idle") Require(fraction >= .3f && fraction <= .42f, "Character framing: " + fraction);
            var rt = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            var old = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); texture.Apply();
            RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(Path.Combine(output, "character-" + name + ".png"), texture.EncodeToPNG()); Destroy(texture);
            samples.Add(new Sample { name = name, pose = pose, bodyScreenHeight = fraction, position = player.transform.position, animatorState = player.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash });
        }
        [Serializable] sealed class Sample { public string name; public int pose, animatorState; public float bodyScreenHeight; public Vector3 position; }
        [Serializable] sealed class Report
        {
            public bool passed; public Sample[] samples;
            public string asset = "Original jointed prototype; authored Humanoid art and animation remain external dependencies";
            public string capture = "Windows Build URP offscreen; gameplay and Animator advanced at fixed steps, frozen for capture; not a human playthrough";
        }
    }
}
#endif
