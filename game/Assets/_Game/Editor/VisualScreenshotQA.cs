using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WanderingCity.Editor
{
    public static class VisualScreenshotQA
    {
        public struct Viewpoint
        {
            public string Name;
            public Vector3 Position;
            public float Pitch;
            public float Yaw;
            public float Fov;

            public Viewpoint(string name, Vector3 pos, float pitch, float yaw, float fov = 54f)
            {
                Name = name;
                Position = pos;
                Pitch = pitch;
                Yaw = yaw;
                Fov = fov;
            }
        }

        [MenuItem("Wandering City/Capture Visual QA Screenshots")]
        public static void CaptureAllScreenshots()
        {
            ProjectBuilder.Prepare();

            const string worldPath = "Assets/_Game/Scenes/World.unity";
            var scene = EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);

            string[] outputDirs = {
                "artifacts/visual",
                "../artifacts/visual"
            };

            foreach (var dir in outputDirs)
            {
                Directory.CreateDirectory(dir);
            }

            var viewpoints = new Viewpoint[]
            {
                // 1. Spawn point looking north towards meadow & ancient spire landmark
                new Viewpoint("spawn.png", new Vector3(0, 1.6f, 0), 10f, 15f, 54f),

                // 2. Meadow with lush flowers, grass tufts, and distant mountains
                new Viewpoint("meadow.png", new Vector3(12, 1.8f, 20), 8f, -25f, 54f),

                // 3. Cliff face with modular rock formations and monolith
                new Viewpoint("cliff.png", new Vector3(52, 3.5f, 36), 16f, 40f, 52f),

                // 4. Forest grove with stylized trees and watchtower silhouette
                new Viewpoint("forest.png", new Vector3(-35, 2.2f, 50), 12f, -50f, 54f),

                // 5. High elevated viewpoint for gliding overview of the lowland
                new Viewpoint("glide-viewpoint.png", new Vector3(20, 24f, 100), -12f, 175f, 58f),

                // 6. Render health check — wide view that captures sky, terrain, vegetation, and player area
                new Viewpoint("render-health.png", new Vector3(0, 8f, -10), 12f, 20f, 68f),
            };

            var camGo = new GameObject("QA_CaptureCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 800f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // If skybox is invalid, fallback to solid color to prevent false magenta
            if (RenderSettings.skybox == null || RenderSettings.skybox.shader == null || !RenderSettings.skybox.shader.isSupported)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
            }

            int width = 1920;
            int height = 1080;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            bool anyMagenta = false;

            try
            {
                foreach (var vp in viewpoints)
                {
                    cam.transform.position = vp.Position;
                    cam.transform.rotation = Quaternion.Euler(vp.Pitch, vp.Yaw, 0);
                    cam.fieldOfView = vp.Fov;

                    RenderTexture.active = rt;
                    cam.Render();

                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply();

                    // Magenta pixel detection (percentage-based threshold)
                    var pixels = tex.GetPixels32();
                    int magentaCount = 0;
                    foreach (var p in pixels)
                    {
                        if (p.r > 200 && p.g < 80 && p.b > 200) magentaCount++;
                    }
                    float magentaRatio = (float)magentaCount / pixels.Length;
                    if (magentaRatio > 0.01f)
                    {
                        Debug.LogError($"[Visual QA] MAGENTA DETECTED in {vp.Name}: {magentaRatio:P2} of pixels — shader error!");
                        anyMagenta = true;
                    }

                    byte[] pngData = tex.EncodeToPNG();

                    foreach (var dir in outputDirs)
                    {
                        string filePath = Path.Combine(dir, vp.Name);
                        File.WriteAllBytes(filePath, pngData);
                    }

                    Debug.Log($"[Visual QA] Captured: {vp.Name} at {vp.Position} (magenta: {magentaRatio:P2})");
                }

                // Log render diagnostic info
                var terrain = Object.FindFirstObjectByType<Terrain>();
                if (terrain != null && terrain.materialTemplate != null)
                    Debug.Log($"[Visual QA] Terrain shader: {terrain.materialTemplate.shader.name}");
                if (RenderSettings.skybox != null)
                    Debug.Log($"[Visual QA] Skybox shader: {RenderSettings.skybox.shader.name}");
            }
            finally
            {
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camGo);
            }

            if (anyMagenta)
                Debug.LogError("WANDERING_CITY_SCREENSHOTS_QA_MAGENTA_DETECTED — visual validation failed");
            else
                Debug.Log("WANDERING_CITY_SCREENSHOTS_QA_OK");
        }
    }
}
