using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using WanderingCity.Editor;

namespace WanderingCity.Tests.EditMode
{
    /// <summary>
    /// Visual render health checks. Magenta is a release blocker.
    /// These tests validate that all formal materials, terrain, and skybox use
    /// supported shaders without compiler errors.
    /// </summary>
    public sealed class VisualRenderHealthChecks
    {
        [Test]
        public void Terrain_HasPersistentSupportedMaterial()
        {
            ProjectBuilder.Prepare();
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);

            var terrain = Object.FindFirstObjectByType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist in World scene.");
            Assert.IsNotNull(terrain.materialTemplate, "Terrain.materialTemplate must not be null (was {fileID: 0}).");
            Assert.IsNotNull(terrain.materialTemplate.shader, "Terrain material shader must not be null.");
            Assert.IsTrue(terrain.materialTemplate.shader.isSupported, 
                $"Terrain shader '{terrain.materialTemplate.shader.name}' must be supported on this GPU.");

            // Validate shader name is the URP terrain shader
            string shaderName = terrain.materialTemplate.shader.name;
            Assert.IsTrue(
                shaderName.Contains("Terrain") || shaderName.Contains("Universal Render Pipeline"),
                $"Terrain shader must be a URP Terrain shader, got: '{shaderName}'");

            // Validate no shader compiler errors
            Assert.IsFalse(ShaderUtil.ShaderHasError(terrain.materialTemplate.shader),
                $"Terrain shader '{shaderName}' has compiler errors: {FormatShaderErrors(terrain.materialTemplate.shader)}");
        }

        [Test]
        public void Sky_HasPersistentSupportedMaterial()
        {
            ProjectBuilder.Prepare();
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);

            // Verify RenderSettings.skybox is set and valid
            var skybox = RenderSettings.skybox;
            if (skybox == null)
            {
                // If no skybox, camera must use SolidColor clear
                var cam = Camera.main;
                if (cam == null)
                {
                    // No camera in editor scene is acceptable — will be created at runtime
                    Assert.Pass("No skybox and no camera in editor scene; runtime creates camera with SolidColor fallback.");
                    return;
                }
                Assert.AreEqual(CameraClearFlags.SolidColor, cam.clearFlags,
                    "When skybox is null, camera must use SolidColor clear flags to prevent magenta.");
                return;
            }

            Assert.IsNotNull(skybox.shader, "Skybox material shader must not be null.");
            Assert.IsTrue(skybox.shader.isSupported,
                $"Skybox shader '{skybox.shader.name}' must be supported on this GPU.");

            // Validate no shader compiler errors
            Assert.IsFalse(ShaderUtil.ShaderHasError(skybox.shader),
                $"Skybox shader '{skybox.shader.name}' has compiler errors: {FormatShaderErrors(skybox.shader)}");
        }

        [Test]
        public void FormalMaterials_AllShadersSupported()
        {
            // Validate all materials in Assets/_Game/Art/Materials/
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Game/Art/Materials" });
            Assert.Greater(guids.Length, 0, "At least one material must exist in Art/Materials.");

            var errors = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string matName = Path.GetFileNameWithoutExtension(path);

                if (mat.shader == null)
                {
                    errors.Add($"{matName}: shader is null");
                    continue;
                }

                if (!mat.shader.isSupported)
                    errors.Add($"{matName}: shader '{mat.shader.name}' is not supported");

                if (mat.shader.name == "Hidden/InternalErrorShader")
                    errors.Add($"{matName}: uses error shader (Hidden/InternalErrorShader)");

                if (ShaderUtil.ShaderHasError(mat.shader))
                    errors.Add($"{matName}: shader '{mat.shader.name}' has compiler errors — {FormatShaderErrors(mat.shader)}");
            }

            Assert.IsEmpty(errors, "Material shader errors:\n" + string.Join("\n", errors));
        }

        [Test]
        public void NoFormalRendererUsesErrorShader()
        {
            ProjectBuilder.Prepare();
            const string worldPath = "Assets/_Game/Scenes/World.unity";
            EditorSceneManager.OpenScene(worldPath, OpenSceneMode.Single);

            var errors = new List<string>();

            // Check all Renderer components in the scene
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                        errors.Add($"Renderer '{r.gameObject.name}': material '{mat.name}' uses error shader");
                }
            }

            // Check all Terrain components
            var terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            foreach (var t in terrains)
            {
                if (t.materialTemplate == null)
                    errors.Add($"Terrain '{t.gameObject.name}': materialTemplate is null");
                else if (t.materialTemplate.shader == null || t.materialTemplate.shader.name == "Hidden/InternalErrorShader")
                    errors.Add($"Terrain '{t.gameObject.name}': materialTemplate uses error shader");
            }

            Assert.IsEmpty(errors, "Error shader usage:\n" + string.Join("\n", errors));
        }

        [Test]
        public void ProjectShaders_HaveNoCompilerErrors()
        {
            // Validate all custom shaders in the project
            string[] guids = AssetDatabase.FindAssets("t:Shader", new[] { "Assets/_Game/Art/Shaders" });

            var errors = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null) continue;

                if (ShaderUtil.ShaderHasError(shader))
                {
                    var messages = ShaderUtil.GetShaderMessages(shader);
                    var messageTexts = new List<string>();
                    foreach (var msg in messages)
                        messageTexts.Add($"  [{msg.severity}] {msg.message} (line {msg.line})");
                    errors.Add($"{shader.name}:\n{string.Join("\n", messageTexts)}");
                }
            }

            Assert.IsEmpty(errors, "Shader compiler errors:\n" + string.Join("\n", errors));
        }

        static string FormatShaderErrors(Shader shader)
        {
            if (shader == null) return "(null shader)";
            var messages = ShaderUtil.GetShaderMessages(shader);
            if (messages == null || messages.Length == 0) return "(no messages)";
            var parts = new List<string>();
            foreach (var msg in messages)
                parts.Add($"[{msg.severity}] {msg.message} (line {msg.line})");
            return string.Join("; ", parts);
        }
    }
}
