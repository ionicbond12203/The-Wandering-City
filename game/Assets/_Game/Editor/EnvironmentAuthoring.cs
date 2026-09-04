using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity.Editor
{
    public static class EnvironmentAuthoring
    {
        public const string ArtDir = "Assets/_Game/Art/Environment";
        public const string ShadersDir = "Assets/_Game/Art/Shaders";
        public const string MaterialsDir = "Assets/_Game/Art/Materials";
        public const string PrefabsDir = "Assets/_Game/Prefabs/Environment";
        public const string VolumesDir = "Assets/_Game/Art/Volumes";
        public const string TerrainDataPath = ArtDir + "/WorldTerrainData.asset";
        public const string VolumeProfilePath = VolumesDir + "/OutdoorStylized.asset";
        public const string AuthoredEnvPrefabPath = "Assets/_Game/Resources/AuthoredEnvironment.prefab";

        public static void EnsureAllEnvironmentAssets()
        {
            Directory.CreateDirectory(ArtDir);
            Directory.CreateDirectory(ShadersDir);
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory(PrefabsDir);
            Directory.CreateDirectory(VolumesDir);
            Directory.CreateDirectory("Assets/_Game/Resources");

            GenerateTextures();
            GenerateVolumeProfile();
            var materials = GenerateMaterials();
            var layers = GenerateTerrainLayers();
            var terrainData = GenerateTerrainData(layers);
            GenerateEnvironmentPrefabs(materials);
            GenerateLandmarkPrefabs(materials);
            GenerateTravelerPrefab(materials);
            CreateAuthoredEnvironmentPrefab(terrainData);
        }

        public static void GenerateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null && profile.components != null && profile.components.Count > 0 && profile.components[0] != null) return;

            if (profile != null)
            {
                AssetDatabase.DeleteAsset(VolumeProfilePath);
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "OutdoorStylized";
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            AssetDatabase.AddObjectToAsset(tone, profile);

            var colors = profile.Add<ColorAdjustments>(true);
            colors.saturation.Override(14f);
            colors.contrast.Override(10f);
            colors.postExposure.Override(0.12f);
            AssetDatabase.AddObjectToAsset(colors, profile);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.28f);
            bloom.scatter.Override(0.65f);
            AssetDatabase.AddObjectToAsset(bloom, profile);

            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(6f);
            AssetDatabase.AddObjectToAsset(wb, profile);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.14f);
            vignette.smoothness.Override(0.35f);
            AssetDatabase.AddObjectToAsset(vignette, profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        public static Dictionary<string, Material> GenerateMaterials()
        {
            var dict = new Dictionary<string, Material>();
            var envShader = Shader.Find("WanderingCity/StylizedEnvironment") ?? Shader.Find("Universal Render Pipeline/Lit");
            var grassShader = Shader.Find("WanderingCity/StylizedGrass") ?? envShader;

            // 1. Foliage material
            string foliagePath = MaterialsDir + "/StylizedFoliage.mat";
            var matFoliage = AssetDatabase.LoadAssetAtPath<Material>(foliagePath);
            if (matFoliage == null)
            {
                matFoliage = new Material(envShader);
                matFoliage.SetColor("_BaseColor", new Color(0.48f, 0.74f, 0.32f));
                matFoliage.SetColor("_ShadowColor", new Color(0.24f, 0.44f, 0.28f));
                matFoliage.SetColor("_RimColor", new Color(0.82f, 0.95f, 0.55f));
                matFoliage.SetFloat("_RimPower", 2.8f);
                matFoliage.enableInstancing = true;
                AssetDatabase.CreateAsset(matFoliage, foliagePath);
            }
            dict["Foliage"] = matFoliage;

            // 2. Bark material
            string barkPath = MaterialsDir + "/StylizedBark.mat";
            var matBark = AssetDatabase.LoadAssetAtPath<Material>(barkPath);
            if (matBark == null)
            {
                matBark = new Material(envShader);
                matBark.SetColor("_BaseColor", new Color(0.42f, 0.34f, 0.26f));
                matBark.SetColor("_ShadowColor", new Color(0.25f, 0.20f, 0.16f));
                matBark.enableInstancing = true;
                AssetDatabase.CreateAsset(matBark, barkPath);
            }
            dict["Bark"] = matBark;

            // 3. Rock material
            string rockPath = MaterialsDir + "/StylizedRock.mat";
            var matRock = AssetDatabase.LoadAssetAtPath<Material>(rockPath);
            if (matRock == null)
            {
                matRock = new Material(envShader);
                matRock.SetColor("_BaseColor", new Color(0.52f, 0.58f, 0.62f));
                matRock.SetColor("_ShadowColor", new Color(0.32f, 0.38f, 0.44f));
                matRock.SetColor("_RimColor", new Color(0.75f, 0.85f, 0.92f));
                matRock.SetFloat("_RimPower", 4.0f);
                matRock.enableInstancing = true;
                AssetDatabase.CreateAsset(matRock, rockPath);
            }
            dict["Rock"] = matRock;

            // 4. Cliff material
            string cliffPath = MaterialsDir + "/StylizedCliff.mat";
            var matCliff = AssetDatabase.LoadAssetAtPath<Material>(cliffPath);
            if (matCliff == null)
            {
                matCliff = new Material(envShader);
                matCliff.SetColor("_BaseColor", new Color(0.45f, 0.51f, 0.53f));
                matCliff.SetColor("_ShadowColor", new Color(0.28f, 0.33f, 0.38f));
                matCliff.enableInstancing = true;
                AssetDatabase.CreateAsset(matCliff, cliffPath);
            }
            dict["Cliff"] = matCliff;

            // 5. Grass tuft material
            string grassPath = MaterialsDir + "/StylizedGrassTuft.mat";
            var matGrass = AssetDatabase.LoadAssetAtPath<Material>(grassPath);
            if (matGrass == null)
            {
                matGrass = new Material(grassShader);
                matGrass.SetColor("_BaseColor", new Color(0.28f, 0.48f, 0.22f));
                matGrass.SetColor("_TipColor", new Color(0.68f, 0.86f, 0.38f));
                matGrass.enableInstancing = true;
                AssetDatabase.CreateAsset(matGrass, grassPath);
            }
            dict["Grass"] = matGrass;

            // 6. Flower material
            string flowerPath = MaterialsDir + "/StylizedFlower.mat";
            var matFlower = AssetDatabase.LoadAssetAtPath<Material>(flowerPath);
            if (matFlower == null)
            {
                matFlower = new Material(envShader);
                matFlower.SetColor("_BaseColor", new Color(0.98f, 0.82f, 0.35f));
                matFlower.SetColor("_ShadowColor", new Color(0.72f, 0.52f, 0.22f));
                matFlower.enableInstancing = true;
                AssetDatabase.CreateAsset(matFlower, flowerPath);
            }
            dict["Flower"] = matFlower;

            // 7. Glow material
            string glowPath = MaterialsDir + "/StylizedGlow.mat";
            var matGlow = AssetDatabase.LoadAssetAtPath<Material>(glowPath);
            if (matGlow == null)
            {
                matGlow = new Material(envShader);
                matGlow.SetColor("_BaseColor", new Color(1f, 0.78f, 0.32f));
                matGlow.SetColor("_ShadowColor", new Color(0.85f, 0.55f, 0.18f));
                matGlow.SetColor("_RimColor", new Color(1f, 0.96f, 0.65f));
                matGlow.SetFloat("_RimPower", 1.5f);
                matGlow.enableInstancing = true;
                AssetDatabase.CreateAsset(matGlow, glowPath);
            }
            dict["Glow"] = matGlow;

            // 8. Wood material
            string woodPath = MaterialsDir + "/StylizedWood.mat";
            var matWood = AssetDatabase.LoadAssetAtPath<Material>(woodPath);
            if (matWood == null)
            {
                matWood = new Material(envShader);
                matWood.SetColor("_BaseColor", new Color(0.48f, 0.36f, 0.24f));
                matWood.SetColor("_ShadowColor", new Color(0.28f, 0.20f, 0.14f));
                matWood.enableInstancing = true;
                AssetDatabase.CreateAsset(matWood, woodPath);
            }
            dict["Wood"] = matWood;

            // 9. Character material
            string charPath = MaterialsDir + "/StylizedCharacterMat.mat";
            var matChar = AssetDatabase.LoadAssetAtPath<Material>(charPath);
            if (matChar == null)
            {
                matChar = new Material(envShader);
                matChar.SetColor("_BaseColor", new Color(0.22f, 0.42f, 0.48f));
                matChar.SetColor("_ShadowColor", new Color(0.12f, 0.22f, 0.28f));
                matChar.SetColor("_RimColor", new Color(0.75f, 0.9f, 0.95f));
                matChar.SetFloat("_RimPower", 3.2f);
                matChar.enableInstancing = true;
                AssetDatabase.CreateAsset(matChar, charPath);
            }
            dict["Character"] = matChar;

            // 10. Terrain material — persistent asset, NOT a runtime material
            string terrainMatPath = MaterialsDir + "/StylizedTerrain.mat";
            var matTerrain = AssetDatabase.LoadAssetAtPath<Material>(terrainMatPath);
            if (matTerrain == null)
            {
                var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                matTerrain = new Material(terrainShader);
                matTerrain.name = "StylizedTerrain";
                matTerrain.enableInstancing = true;
                AssetDatabase.CreateAsset(matTerrain, terrainMatPath);
            }
            dict["Terrain"] = matTerrain;

            // 11. Skybox material — persistent asset for explicit skybox configuration
            string skyMatPath = MaterialsDir + "/OutdoorSky.mat";
            var matSky = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
            if (matSky == null)
            {
                var skyShader = Shader.Find("Skybox/Procedural");
                matSky = new Material(skyShader);
                matSky.name = "OutdoorSky";
                matSky.SetFloat("_SunDisk", 2); // High quality sun disk
                matSky.SetFloat("_SunSize", 0.04f);
                matSky.SetFloat("_SunSizeConvergence", 5f);
                matSky.SetFloat("_AtmosphereThickness", 1.05f);
                matSky.SetColor("_SkyTint", new Color(0.52f, 0.65f, 0.82f));
                matSky.SetColor("_GroundColor", new Color(0.37f, 0.42f, 0.35f));
                matSky.SetFloat("_Exposure", 1.25f);
                AssetDatabase.CreateAsset(matSky, skyMatPath);
            }
            dict["Sky"] = matSky;

            AssetDatabase.SaveAssets();
            return dict;
        }

        public static void GenerateEnvironmentPrefabs(Dictionary<string, Material> mats)
        {
            // 1. Tree_Stylized_01
            string treePath = PrefabsDir + "/Tree_Stylized_01.prefab";
            if (!File.Exists(treePath))
            {
                var treeGo = new GameObject("Tree_Stylized_01");
                var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(treeGo.transform);
                trunk.transform.localPosition = new Vector3(0, 2.2f, 0);
                trunk.transform.localScale = new Vector3(0.7f, 2.2f, 0.7f);
                trunk.GetComponent<Renderer>().sharedMaterial = mats["Bark"];

                // Clustered stylized canopy
                Vector3[] canopyOffsets = {
                    new Vector3(0, 4.6f, 0),
                    new Vector3(-0.6f, 5.2f, 0.4f),
                    new Vector3(0.7f, 5.0f, -0.3f),
                    new Vector3(0, 6.0f, 0)
                };
                Vector3[] canopyScales = {
                    new Vector3(3.4f, 2.4f, 3.2f),
                    new Vector3(2.6f, 2.2f, 2.6f),
                    new Vector3(2.5f, 2.2f, 2.5f),
                    new Vector3(2.0f, 2.0f, 2.0f)
                };

                for (int i = 0; i < canopyOffsets.Length; i++)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = "Canopy_" + i;
                    sphere.transform.SetParent(treeGo.transform);
                    sphere.transform.localPosition = canopyOffsets[i];
                    sphere.transform.localScale = canopyScales[i];
                    Object.DestroyImmediate(sphere.GetComponent<Collider>());
                    sphere.GetComponent<Renderer>().sharedMaterial = mats["Foliage"];
                }

                PrefabUtility.SaveAsPrefabAsset(treeGo, treePath);
                Object.DestroyImmediate(treeGo);
            }

            // 2. Bush_Stylized_01
            string bushPath = PrefabsDir + "/Bush_Stylized_01.prefab";
            if (!File.Exists(bushPath))
            {
                var bushGo = new GameObject("Bush_Stylized_01");
                var mainSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mainSphere.name = "Bush_Body";
                mainSphere.transform.SetParent(bushGo.transform);
                mainSphere.transform.localPosition = new Vector3(0, 0.6f, 0);
                mainSphere.transform.localScale = new Vector3(1.6f, 1.2f, 1.5f);
                mainSphere.GetComponent<Renderer>().sharedMaterial = mats["Foliage"];

                PrefabUtility.SaveAsPrefabAsset(bushGo, bushPath);
                Object.DestroyImmediate(bushGo);
            }

            // 3. Flower_Patch_01
            string flowerPath = PrefabsDir + "/Flower_Patch_01.prefab";
            if (!File.Exists(flowerPath))
            {
                var flowerGo = new GameObject("Flower_Patch_01");
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * 1.25f;
                    Vector3 pos = new Vector3(Mathf.Cos(angle) * 0.4f, 0.15f, Mathf.Sin(angle) * 0.4f);
                    var blossom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    blossom.name = "Blossom_" + i;
                    blossom.transform.SetParent(flowerGo.transform);
                    blossom.transform.localPosition = pos;
                    blossom.transform.localScale = Vector3.one * 0.25f;
                    Object.DestroyImmediate(blossom.GetComponent<Collider>());
                    blossom.GetComponent<Renderer>().sharedMaterial = mats["Flower"];
                }

                PrefabUtility.SaveAsPrefabAsset(flowerGo, flowerPath);
                Object.DestroyImmediate(flowerGo);
            }

            // 4. Grass_Tuft_01
            string grassTuftPath = PrefabsDir + "/Grass_Tuft_01.prefab";
            if (!File.Exists(grassTuftPath))
            {
                var tuftGo = new GameObject("Grass_Tuft_01");
                for (int i = 0; i < 3; i++)
                {
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "Blade_" + i;
                    quad.transform.SetParent(tuftGo.transform);
                    quad.transform.localPosition = new Vector3(0, 0.5f, 0);
                    quad.transform.localRotation = Quaternion.Euler(0, i * 60f, 0);
                    quad.transform.localScale = new Vector3(0.8f, 1.0f, 1.0f);
                    Object.DestroyImmediate(quad.GetComponent<Collider>());
                    quad.GetComponent<Renderer>().sharedMaterial = mats["Grass"];
                }

                PrefabUtility.SaveAsPrefabAsset(tuftGo, grassTuftPath);
                Object.DestroyImmediate(tuftGo);
            }

            // 5. Cliff_Modular_01 (with ClimbSurface)
            string cliffPath = PrefabsDir + "/Cliff_Modular_01.prefab";
            if (!File.Exists(cliffPath))
            {
                var cliffGo = new GameObject("Cliff_Modular_01");
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Cliff_Face";
                body.transform.SetParent(cliffGo.transform);
                body.transform.localPosition = new Vector3(0, 3.5f, 0);
                body.transform.localScale = new Vector3(8f, 7f, 4f);
                body.GetComponent<Renderer>().sharedMaterial = mats["Cliff"];
                cliffGo.AddComponent<ClimbSurface>();

                PrefabUtility.SaveAsPrefabAsset(cliffGo, cliffPath);
                Object.DestroyImmediate(cliffGo);
            }

            // 6. Rock_Boulder_01
            string rockPath = PrefabsDir + "/Rock_Boulder_01.prefab";
            if (!File.Exists(rockPath))
            {
                var rockGo = new GameObject("Rock_Boulder_01");
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Rock_Mesh";
                cube.transform.SetParent(rockGo.transform);
                cube.transform.localPosition = new Vector3(0, 0.7f, 0);
                cube.transform.localRotation = Quaternion.Euler(15f, 25f, 10f);
                cube.transform.localScale = new Vector3(1.8f, 1.4f, 1.5f);
                cube.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                PrefabUtility.SaveAsPrefabAsset(rockGo, rockPath);
                Object.DestroyImmediate(rockGo);
            }
        }

        public static void GenerateLandmarkPrefabs(Dictionary<string, Material> mats)
        {
            // 1. Landmark_Spire.prefab (Ancient Spire)
            string spirePath = PrefabsDir + "/Landmark_Spire.prefab";
            if (!File.Exists(spirePath))
            {
                var spireGo = new GameObject("Landmark_Spire");
                var plinth = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                plinth.name = "Plinth";
                plinth.transform.SetParent(spireGo.transform);
                plinth.transform.localPosition = new Vector3(0, 1.5f, 0);
                plinth.transform.localScale = new Vector3(8f, 1.5f, 8f);
                plinth.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                var tier1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tier1.name = "Tier1";
                tier1.transform.SetParent(spireGo.transform);
                tier1.transform.localPosition = new Vector3(0, 6f, 0);
                tier1.transform.localScale = new Vector3(5.5f, 4f, 5.5f);
                tier1.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                var tier2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tier2.name = "Tier2";
                tier2.transform.SetParent(spireGo.transform);
                tier2.transform.localPosition = new Vector3(0, 14f, 0);
                tier2.transform.localScale = new Vector3(3.8f, 4f, 3.8f);
                tier2.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "SpireShaft";
                shaft.transform.SetParent(spireGo.transform);
                shaft.transform.localPosition = new Vector3(0, 22f, 0);
                shaft.transform.localScale = new Vector3(2.2f, 5f, 2.2f);
                shaft.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                var crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crystal.name = "SpireCrystal";
                crystal.transform.SetParent(spireGo.transform);
                crystal.transform.localPosition = new Vector3(0, 28.5f, 0);
                crystal.transform.localScale = new Vector3(2.4f, 3.2f, 2.4f);
                Object.DestroyImmediate(crystal.GetComponent<Collider>());
                crystal.GetComponent<Renderer>().sharedMaterial = mats.ContainsKey("Glow") ? mats["Glow"] : mats["Flower"];

                PrefabUtility.SaveAsPrefabAsset(spireGo, spirePath);
                Object.DestroyImmediate(spireGo);
            }

            // 2. Landmark_Watchtower.prefab
            string towerPath = PrefabsDir + "/Landmark_Watchtower.prefab";
            if (!File.Exists(towerPath))
            {
                var towerGo = new GameObject("Landmark_Watchtower");
                var baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseBlock.name = "Foundation";
                baseBlock.transform.SetParent(towerGo.transform);
                baseBlock.transform.localPosition = new Vector3(0, 2f, 0);
                baseBlock.transform.localScale = new Vector3(6f, 4f, 6f);
                baseBlock.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                Vector3[] pillarOffsets = {
                    new Vector3(-2.2f, 8f, -2.2f),
                    new Vector3(2.2f, 8f, -2.2f),
                    new Vector3(-2.2f, 8f, 2.2f),
                    new Vector3(2.2f, 8f, 2.2f)
                };
                for (int i = 0; i < 4; i++)
                {
                    var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    p.name = "Pillar_" + i;
                    p.transform.SetParent(towerGo.transform);
                    p.transform.localPosition = pillarOffsets[i];
                    p.transform.localScale = new Vector3(0.5f, 4f, 0.5f);
                    p.GetComponent<Renderer>().sharedMaterial = mats["Bark"];
                }

                var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deck.name = "Deck";
                deck.transform.SetParent(towerGo.transform);
                deck.transform.localPosition = new Vector3(0, 12.3f, 0);
                deck.transform.localScale = new Vector3(6.5f, 0.6f, 6.5f);
                deck.GetComponent<Renderer>().sharedMaterial = mats.ContainsKey("Wood") ? mats["Wood"] : mats["Bark"];

                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.name = "Roof";
                roof.transform.SetParent(towerGo.transform);
                roof.transform.localPosition = new Vector3(0, 15f, 0);
                roof.transform.localRotation = Quaternion.Euler(0, 45f, 0);
                roof.transform.localScale = new Vector3(7f, 1.8f, 7f);
                roof.GetComponent<Renderer>().sharedMaterial = mats["Bark"];

                PrefabUtility.SaveAsPrefabAsset(towerGo, towerPath);
                Object.DestroyImmediate(towerGo);
            }

            // 3. Landmark_Monolith.prefab
            string monolithPath = PrefabsDir + "/Landmark_Monolith.prefab";
            if (!File.Exists(monolithPath))
            {
                var monolithGo = new GameObject("Landmark_Monolith");
                var leftPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftPillar.name = "Pillar_Left";
                leftPillar.transform.SetParent(monolithGo.transform);
                leftPillar.transform.localPosition = new Vector3(-4.5f, 9f, 0);
                leftPillar.transform.localRotation = Quaternion.Euler(3f, 0, 4f);
                leftPillar.transform.localScale = new Vector3(3.2f, 18f, 3.5f);
                leftPillar.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                var rightPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightPillar.name = "Pillar_Right";
                rightPillar.transform.SetParent(monolithGo.transform);
                rightPillar.transform.localPosition = new Vector3(4.5f, 9.5f, 0);
                rightPillar.transform.localRotation = Quaternion.Euler(-3f, 0, -5f);
                rightPillar.transform.localScale = new Vector3(3.2f, 19f, 3.5f);
                rightPillar.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                var arch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arch.name = "Arch_Lintel";
                arch.transform.SetParent(monolithGo.transform);
                arch.transform.localPosition = new Vector3(0, 18.2f, 0);
                arch.transform.localScale = new Vector3(13.5f, 2.6f, 4f);
                arch.GetComponent<Renderer>().sharedMaterial = mats["Rock"];

                var rune = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rune.name = "Ancient_Rune";
                rune.transform.SetParent(monolithGo.transform);
                rune.transform.localPosition = new Vector3(0, 12f, 0);
                rune.transform.localScale = new Vector3(1.8f, 2.4f, 1.8f);
                Object.DestroyImmediate(rune.GetComponent<Collider>());
                rune.GetComponent<Renderer>().sharedMaterial = mats.ContainsKey("Glow") ? mats["Glow"] : mats["Flower"];

                PrefabUtility.SaveAsPrefabAsset(monolithGo, monolithPath);
                Object.DestroyImmediate(monolithGo);
            }
        }

        public static void GenerateTravelerPrefab(Dictionary<string, Material> mats)
        {
            string prefabPath = "Assets/_Game/Resources/Traveler_Stylized.prefab";
            if (File.Exists(prefabPath)) return;

            var go = new GameObject("Traveler_Stylized");
            var charMat = mats.ContainsKey("Character") ? mats["Character"] : mats["Rock"];
            var skinMat = mats.ContainsKey("Flower") ? mats["Flower"] : mats["Rock"];
            var hairMat = mats["Bark"];

            var coat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coat.name = "Coat";
            coat.transform.SetParent(go.transform);
            coat.transform.localPosition = new Vector3(0, 0.85f, 0);
            coat.transform.localScale = new Vector3(0.52f, 0.55f, 0.42f);
            Object.DestroyImmediate(coat.GetComponent<Collider>());
            coat.GetComponent<Renderer>().sharedMaterial = charMat;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(go.transform);
            head.transform.localPosition = new Vector3(0, 1.55f, 0.02f);
            head.transform.localScale = new Vector3(0.38f, 0.42f, 0.38f);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.GetComponent<Renderer>().sharedMaterial = skinMat;

            var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair_Main";
            hair.transform.SetParent(go.transform);
            hair.transform.localPosition = new Vector3(0, 1.62f, -0.04f);
            hair.transform.localScale = new Vector3(0.42f, 0.40f, 0.44f);
            Object.DestroyImmediate(hair.GetComponent<Collider>());
            hair.GetComponent<Renderer>().sharedMaterial = hairMat;

            var scarf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scarf.name = "Scarf";
            scarf.transform.SetParent(go.transform);
            scarf.transform.localPosition = new Vector3(0, 1.34f, 0.02f);
            scarf.transform.localScale = new Vector3(0.46f, 0.12f, 0.42f);
            Object.DestroyImmediate(scarf.GetComponent<Collider>());
            scarf.GetComponent<Renderer>().sharedMaterial = skinMat;

            var pack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pack.name = "TravelPack";
            pack.transform.SetParent(go.transform);
            pack.transform.localPosition = new Vector3(0, 1.05f, -0.28f);
            pack.transform.localScale = new Vector3(0.44f, 0.48f, 0.24f);
            Object.DestroyImmediate(pack.GetComponent<Collider>());
            pack.GetComponent<Renderer>().sharedMaterial = hairMat;

            var bladePivot = new GameObject("Sword pivot");
            bladePivot.transform.SetParent(go.transform);
            bladePivot.transform.localPosition = new Vector3(0.45f, 1f, 0.1f);
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "BladeMesh";
            blade.transform.SetParent(bladePivot.transform);
            blade.transform.localPosition = new Vector3(0, 0, 0.75f);
            blade.transform.localScale = new Vector3(0.08f, 0.12f, 1.3f);
            Object.DestroyImmediate(blade.GetComponent<Collider>());
            blade.GetComponent<Renderer>().sharedMaterial = charMat;

            var sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sail.name = "Traveler / folding wind sail";
            sail.transform.SetParent(go.transform);
            sail.transform.localPosition = new Vector3(0, 1.95f, -0.35f);
            sail.transform.localScale = new Vector3(3.2f, 0.08f, 1.1f);
            Object.DestroyImmediate(sail.GetComponent<Collider>());
            sail.GetComponent<Renderer>().sharedMaterial = charMat;
            sail.SetActive(false);

            var animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("Traveler");
            animator.applyRootMotion = false;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        public static void PopulateSceneEnvironment(GameObject envRoot, Terrain terrain, Dictionary<string, Material> mats)
        {
            if (envRoot == null) return;

            // 1. Vegetation
            var veg = envRoot.transform.Find("Vegetation");
            if (veg != null && veg.childCount == 0)
            {
                var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Tree_Stylized_01.prefab");
                var bushPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Bush_Stylized_01.prefab");
                var flowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Flower_Patch_01.prefab");
                var grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Grass_Tuft_01.prefab");

                Vector2[] treePos = {
                    new Vector2(-40, 60), new Vector2(-48, 70), new Vector2(-55, 62), new Vector2(-42, 80), new Vector2(-60, 75),
                    new Vector2(-35, 52), new Vector2(-50, 48), new Vector2(-32, 68), new Vector2(-58, 88), new Vector2(-68, 65),
                    new Vector2(-15, 18), new Vector2(12, 24), new Vector2(28, 35), new Vector2(-22, 38), new Vector2(8, 55),
                    new Vector2(-8, 85), new Vector2(22, 95), new Vector2(-12, 110), new Vector2(32, 115), new Vector2(5, 130),
                    new Vector2(-18, -12), new Vector2(15, -16), new Vector2(-28, -20), new Vector2(26, -22), new Vector2(42, 15),
                    new Vector2(52, 28), new Vector2(38, 48)
                };

                for (int i = 0; i < treePos.Length; i++)
                {
                    if (treePrefab == null) break;
                    float x = treePos[i].x;
                    float z = treePos[i].y;
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : 0f;
                    var tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, veg);
                    tree.transform.position = new Vector3(x, y, z);
                    tree.transform.rotation = Quaternion.Euler(0, (i * 47f) % 360f, 0);
                    float scale = 0.9f + ((i * 17) % 7) * 0.05f;
                    tree.transform.localScale = Vector3.one * scale;
                }

                Vector2[] bushPos = {
                    new Vector2(-38, 56), new Vector2(-45, 65), new Vector2(-52, 58), new Vector2(-38, 74), new Vector2(-63, 71),
                    new Vector2(-12, 16), new Vector2(14, 21), new Vector2(25, 32), new Vector2(-20, 35), new Vector2(6, 52),
                    new Vector2(-6, 80), new Vector2(20, 90), new Vector2(-10, 105), new Vector2(30, 110), new Vector2(3, 125),
                    new Vector2(-15, -10), new Vector2(12, -14), new Vector2(40, 12), new Vector2(50, 25), new Vector2(36, 45),
                    new Vector2(-4, 6), new Vector2(5, 8), new Vector2(-8, -4), new Vector2(7, -6), new Vector2(10, 3)
                };

                for (int i = 0; i < bushPos.Length; i++)
                {
                    if (bushPrefab == null) break;
                    float x = bushPos[i].x;
                    float z = bushPos[i].y;
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : 0f;
                    var bush = (GameObject)PrefabUtility.InstantiatePrefab(bushPrefab, veg);
                    bush.transform.position = new Vector3(x, y, z);
                    bush.transform.rotation = Quaternion.Euler(0, (i * 73f) % 360f, 0);
                    float scale = 0.85f + ((i * 23) % 9) * 0.05f;
                    bush.transform.localScale = Vector3.one * scale;
                }

                Vector2[] flowerPos = {
                    new Vector2(-3, 4), new Vector2(4, 5), new Vector2(2, -3), new Vector2(-4, -2), new Vector2(6, 12),
                    new Vector2(-7, 14), new Vector2(10, 18), new Vector2(-14, 24), new Vector2(18, 30), new Vector2(-16, 42),
                    new Vector2(12, 45), new Vector2(-10, 58), new Vector2(15, 65), new Vector2(-5, 72), new Vector2(18, 80),
                    new Vector2(-8, 95), new Vector2(14, 105), new Vector2(2, 118), new Vector2(-12, -8), new Vector2(10, -10)
                };

                for (int i = 0; i < flowerPos.Length; i++)
                {
                    if (flowerPrefab == null) break;
                    float x = flowerPos[i].x;
                    float z = flowerPos[i].y;
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : 0f;
                    var flower = (GameObject)PrefabUtility.InstantiatePrefab(flowerPrefab, veg);
                    flower.transform.position = new Vector3(x, y, z);
                    flower.transform.rotation = Quaternion.Euler(0, (i * 59f) % 360f, 0);
                }

                Vector2[] grassPos = {
                    new Vector2(1, 2), new Vector2(-2, 3), new Vector2(3, -1), new Vector2(-1, -2), new Vector2(2, 4),
                    new Vector2(-3, 5), new Vector2(4, 2), new Vector2(0, 5), new Vector2(-2, 7), new Vector2(5, 9),
                    new Vector2(-6, 8), new Vector2(7, 14), new Vector2(-9, 16), new Vector2(12, 15), new Vector2(-11, 20),
                    new Vector2(8, 22), new Vector2(-5, 26), new Vector2(15, 28), new Vector2(-18, 32), new Vector2(20, 36),
                    new Vector2(-15, 48), new Vector2(10, 50), new Vector2(-22, 54), new Vector2(16, 58), new Vector2(-14, 66),
                    new Vector2(9, 74), new Vector2(-18, 82), new Vector2(12, 88), new Vector2(-7, 100), new Vector2(8, 108),
                    new Vector2(-14, 115), new Vector2(16, 120), new Vector2(-5, -6), new Vector2(6, -8), new Vector2(-8, -12),
                    new Vector2(10, -14), new Vector2(-12, -18), new Vector2(14, -20), new Vector2(18, -6), new Vector2(-16, -5)
                };

                for (int i = 0; i < grassPos.Length; i++)
                {
                    if (grassPrefab == null) break;
                    float x = grassPos[i].x;
                    float z = grassPos[i].y;
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : 0f;
                    var tuft = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, veg);
                    tuft.transform.position = new Vector3(x, y, z);
                    tuft.transform.rotation = Quaternion.Euler(0, (i * 83f) % 360f, 0);
                    float scale = 0.8f + ((i * 31) % 11) * 0.04f;
                    tuft.transform.localScale = Vector3.one * scale;
                }
            }

            // 2. Cliffs & Rocks
            var cliffs = envRoot.transform.Find("Cliffs");
            if (cliffs != null && cliffs.childCount == 0)
            {
                var cliffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Cliff_Modular_01.prefab");
                var rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Rock_Boulder_01.prefab");

                Vector3[] cliffPlacements = {
                    new Vector3(68, 0, 50), new Vector3(72, 0, 62), new Vector3(64, 0, 38), new Vector3(76, 0, 74),
                    new Vector3(-74, 0, 48), new Vector3(-76, 0, 62), new Vector3(25, 0, 150), new Vector3(-15, 0, 150)
                };

                for (int i = 0; i < cliffPlacements.Length; i++)
                {
                    if (cliffPrefab == null) break;
                    float x = cliffPlacements[i].x;
                    float z = cliffPlacements[i].z;
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : 0f;
                    var cliff = (GameObject)PrefabUtility.InstantiatePrefab(cliffPrefab, cliffs);
                    cliff.transform.position = new Vector3(x, y, z);
                    cliff.transform.rotation = Quaternion.Euler(0, (i * 65f) % 360f, 0);
                }

                Vector2[] rockPos = {
                    new Vector2(25, -5), new Vector2(35, 12), new Vector2(48, 22), new Vector2(58, 35),
                    new Vector2(-25, 15), new Vector2(-32, 28), new Vector2(-45, 38), new Vector2(-58, 50),
                    new Vector2(15, 82), new Vector2(-12, 75), new Vector2(24, 110), new Vector2(-18, 125),
                    new Vector2(8, -14), new Vector2(-15, -16), new Vector2(30, -18)
                };

                for (int i = 0; i < rockPos.Length; i++)
                {
                    if (rockPrefab == null) break;
                    float x = rockPos[i].x;
                    float z = rockPos[i].y;
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y : 0f;
                    var rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab, cliffs);
                    rock.transform.position = new Vector3(x, y, z);
                    rock.transform.rotation = Quaternion.Euler(10f, (i * 53f) % 360f, 5f);
                    float scale = 0.9f + ((i * 19) % 7) * 0.08f;
                    rock.transform.localScale = Vector3.one * scale;
                }
            }

            // 3. Landmarks
            var landmarks = envRoot.transform.Find("Landmarks");
            if (landmarks != null && landmarks.childCount == 0)
            {
                var spirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Landmark_Spire.prefab");
                var watchtowerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Landmark_Watchtower.prefab");
                var monolithPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Landmark_Monolith.prefab");

                // Spire at North (18, y, 145)
                if (spirePrefab != null)
                {
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(18, 0, 145)) + terrain.transform.position.y : 0f;
                    var spire = (GameObject)PrefabUtility.InstantiatePrefab(spirePrefab, landmarks);
                    spire.transform.position = new Vector3(18, y, 145);
                    spire.transform.rotation = Quaternion.Euler(0, 15f, 0);
                }

                // Watchtower at North-West (-65, y, 55)
                if (watchtowerPrefab != null)
                {
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(-65, 0, 55)) + terrain.transform.position.y : 0f;
                    var wt = (GameObject)PrefabUtility.InstantiatePrefab(watchtowerPrefab, landmarks);
                    wt.transform.position = new Vector3(-65, y, 55);
                    wt.transform.rotation = Quaternion.Euler(0, 45f, 0);
                }

                // Monolith at North-East (75, y, 45)
                if (monolithPrefab != null)
                {
                    float y = terrain != null ? terrain.SampleHeight(new Vector3(75, 0, 45)) + terrain.transform.position.y : 0f;
                    var mono = (GameObject)PrefabUtility.InstantiatePrefab(monolithPrefab, landmarks);
                    mono.transform.position = new Vector3(75, y, 45);
                    mono.transform.rotation = Quaternion.Euler(0, -30f, 0);
                }
            }
        }

        public static void GenerateTextures()
        {
            CreateTexture(ArtDir + "/Grass_Albedo.png", 256, 256, (x, y) =>
            {
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.5f + Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.5f;
                Color baseCol = new Color(0.42f, 0.65f, 0.32f);
                Color highCol = new Color(0.55f, 0.77f, 0.38f);
                Color darkCol = new Color(0.32f, 0.52f, 0.24f);
                return Color.Lerp(darkCol, Color.Lerp(baseCol, highCol, n), n * 0.8f + 0.2f);
            }, false);

            CreateTexture(ArtDir + "/Grass_Normal.png", 256, 256, (x, y) =>
            {
                float dx = (Mathf.PerlinNoise((x + 1) * 0.1f, y * 0.1f) - Mathf.PerlinNoise((x - 1) * 0.1f, y * 0.1f)) * 0.5f;
                float dy = (Mathf.PerlinNoise(x * 0.1f, (y + 1) * 0.1f) - Mathf.PerlinNoise(x * 0.1f, (y - 1) * 0.1f)) * 0.5f;
                return new Color(0.5f + dx, 0.5f + dy, 1f);
            }, true);

            CreateTexture(ArtDir + "/DryGrass_Albedo.png", 256, 256, (x, y) =>
            {
                float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                Color baseCol = new Color(0.64f, 0.68f, 0.38f);
                Color dryCol = new Color(0.76f, 0.74f, 0.44f);
                return Color.Lerp(baseCol, dryCol, n);
            }, false);

            CreateTexture(ArtDir + "/DryGrass_Normal.png", 256, 256, (x, y) =>
            {
                float dx = (Mathf.PerlinNoise((x + 1) * 0.12f, y * 0.12f) - Mathf.PerlinNoise((x - 1) * 0.12f, y * 0.12f)) * 0.5f;
                float dy = (Mathf.PerlinNoise(x * 0.12f, (y + 1) * 0.12f) - Mathf.PerlinNoise(x * 0.12f, (y - 1) * 0.12f)) * 0.5f;
                return new Color(0.5f + dx, 0.5f + dy, 1f);
            }, true);

            CreateTexture(ArtDir + "/Dirt_Albedo.png", 256, 256, (x, y) =>
            {
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.7f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.3f;
                Color baseCol = new Color(0.55f, 0.47f, 0.34f);
                Color darkCol = new Color(0.45f, 0.38f, 0.28f);
                return Color.Lerp(darkCol, baseCol, n);
            }, false);

            CreateTexture(ArtDir + "/Dirt_Normal.png", 256, 256, (x, y) =>
            {
                float dx = (Mathf.PerlinNoise((x + 1) * 0.15f, y * 0.15f) - Mathf.PerlinNoise((x - 1) * 0.15f, y * 0.15f)) * 0.5f;
                float dy = (Mathf.PerlinNoise(x * 0.15f, (y + 1) * 0.15f) - Mathf.PerlinNoise(x * 0.15f, (y - 1) * 0.15f)) * 0.5f;
                return new Color(0.5f + dx, 0.5f + dy, 1f);
            }, true);

            CreateTexture(ArtDir + "/Rock_Albedo.png", 256, 256, (x, y) =>
            {
                float n = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.6f + Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.4f;
                Color darkRock = new Color(0.38f, 0.44f, 0.47f);
                Color lightRock = new Color(0.52f, 0.58f, 0.62f);
                return Color.Lerp(darkRock, lightRock, n);
            }, false);

            CreateTexture(ArtDir + "/Rock_Normal.png", 256, 256, (x, y) =>
            {
                float dx = (Mathf.PerlinNoise((x + 1) * 0.08f, y * 0.08f) - Mathf.PerlinNoise((x - 1) * 0.08f, y * 0.08f)) * 0.8f;
                float dy = (Mathf.PerlinNoise(x * 0.08f, (y + 1) * 0.08f) - Mathf.PerlinNoise(x * 0.08f, (y - 1) * 0.08f)) * 0.8f;
                return new Color(0.5f + dx, 0.5f + dy, 1f);
            }, true);

            AssetDatabase.Refresh();
        }

        static void CreateTexture(string path, int width, int height, System.Func<int, int, Color> pixelFunc, bool isNormal)
        {
            if (File.Exists(path)) return;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, pixelFunc(x, y));
                }
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                if (isNormal)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                }
                importer.SaveAndReimport();
            }
        }

        public static TerrainLayer[] GenerateTerrainLayers()
        {
            string[] layerNames = { "Grass", "DryGrass", "Dirt", "Rock" };
            var layers = new TerrainLayer[layerNames.Length];

            for (int i = 0; i < layerNames.Length; i++)
            {
                string name = layerNames[i];
                string layerPath = $"{ArtDir}/{name}_Layer.terrainlayer";
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
                if (layer == null)
                {
                    layer = new TerrainLayer();
                    layer.name = name;
                    layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtDir}/{name}_Albedo.png");
                    layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ArtDir}/{name}_Normal.png");
                    layer.tileSize = name == "Grass" ? new Vector2(10, 10) : new Vector2(12, 12);
                    layer.smoothness = 0.1f;
                    AssetDatabase.CreateAsset(layer, layerPath);
                }
                layers[i] = layer;
            }
            AssetDatabase.SaveAssets();
            return layers;
        }

        // Terrain data version. Increment to force one-time regeneration after height model changes.
        // v4 = stable flat lowland corridor (<0.04m) with perimeter mountain ranges.
        const int TerrainDataVersion = 4;
        const string TerrainVersionKey = "WanderingCity_TerrainDataVersion";

        public static TerrainData GenerateTerrainData(TerrainLayer[] layers)
        {
            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            int storedVersion = EditorPrefs.GetInt(TerrainVersionKey, 0);

            if (terrainData != null && storedVersion >= TerrainDataVersion && terrainData.size.y == 100f)
                return terrainData;

            // One-time regeneration: delete stale data if version mismatch or wrong height dimension
            if (terrainData != null && (storedVersion < TerrainDataVersion || terrainData.size.y != 100f))
            {
                AssetDatabase.DeleteAsset(TerrainDataPath);
                terrainData = null;
            }

            terrainData = new TerrainData();
            terrainData.name = "WorldTerrainData";
            int res = 257;
            terrainData.heightmapResolution = res;
            terrainData.size = new Vector3(500, 100, 500);

            float[,] heights = new float[res, res];
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float worldX = -250f + (x / (float)(res - 1)) * 500f;
                    float worldZ = -150f + (z / (float)(res - 1)) * 500f;

                    float h = CalculateHeight(worldX, worldZ);
                    heights[z, x] = Mathf.Clamp01(h / 100f);
                }
            }
            terrainData.SetHeights(0, 0, heights);
            terrainData.terrainLayers = layers;

            int alphaRes = 256;
            terrainData.alphamapResolution = alphaRes;
            float[,,] alphas = new float[alphaRes, alphaRes, layers.Length];

            for (int z = 0; z < alphaRes; z++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    float normX = x / (float)(alphaRes - 1);
                    float normZ = z / (float)(alphaRes - 1);
                    float worldX = -250f + normX * 500f;
                    float worldZ = -150f + normZ * 500f;

                    float steepness = terrainData.GetSteepness(normX, normZ);
                    float worldY = terrainData.GetInterpolatedHeight(normX, normZ);

                    float roadDist = MinPathDistance(new Vector2(worldX, worldZ));
                    float roadFactor = Mathf.Clamp01(1f - (roadDist / 3.5f));

                    float rockWeight = Mathf.Clamp01((steepness - 22f) / 18f);
                    float dryGrassWeight = Mathf.Clamp01((worldY - 20f) / 30f) * (1f - rockWeight);
                    float dirtWeight = roadFactor * (1f - rockWeight);
                    float grassWeight = Mathf.Max(0f, 1f - rockWeight - dryGrassWeight - dirtWeight);

                    float total = grassWeight + dryGrassWeight + dirtWeight + rockWeight;
                    if (total > 0.001f)
                    {
                        alphas[z, x, 0] = grassWeight / total;
                        alphas[z, x, 1] = dryGrassWeight / total;
                        alphas[z, x, 2] = dirtWeight / total;
                        alphas[z, x, 3] = rockWeight / total;
                    }
                    else
                    {
                        alphas[z, x, 0] = 1f;
                    }
                }
            }
            terrainData.SetAlphamaps(0, 0, alphas);

            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            EditorPrefs.SetInt(TerrainVersionKey, TerrainDataVersion);
            AssetDatabase.SaveAssets();
            return terrainData;
        }

        static float CalculateHeight(float wx, float wz)
        {
            // Active gameplay corridor covers base camp (0,0), forest (-48, 65), quarry (64, 59), ruins (20, 139), canyon (81, 32), shelf approach (27, -8)
            bool inLowlandCorridor = (wx >= -78f && wx <= 92f && wz >= -32f && wz <= 162f);
            if (inLowlandCorridor)
            {
                // Very subtle micro-relief (< 0.04m) ensuring clean flat lowland floor
                return Mathf.PerlinNoise(wx * 0.05f + 20, wz * 0.05f + 20) * 0.03f;
            }

            // Outside active gameplay corridor: rise into Midland (15-40m), Highland (40-80m), Landmark/Peaks (80m+)
            float edgeDistX = 0f;
            if (wx > 92f) edgeDistX = wx - 92f;
            else if (wx < -78f) edgeDistX = -78f - wx;

            float edgeDistZ = 0f;
            if (wz > 162f) edgeDistZ = wz - 162f;
            else if (wz < -32f) edgeDistZ = -32f - wz;

            float edgeDist = Mathf.Sqrt(edgeDistX * edgeDistX + edgeDistZ * edgeDistZ);
            if (edgeDist > 0f)
            {
                float mountainSlope = Mathf.SmoothStep(0f, 120f, edgeDist) * 78f;
                float noise = Mathf.PerlinNoise(wx * 0.015f + 100, wz * 0.015f + 100) * 18f;
                return mountainSlope + noise * Mathf.Clamp01(edgeDist / 30f);
            }

            return 0f;
        }

        static float MinPathDistance(Vector2 p)
        {
            Vector2 p0 = new Vector2(0, 4);
            Vector2 pForest = new Vector2(-46, 62);
            Vector2 pQuarry = new Vector2(61, 57);
            Vector2 pRuins = new Vector2(20, 128);

            float d1 = DistToSegment(p, p0, pForest);
            float d2 = DistToSegment(p, p0, pQuarry);
            float d3 = DistToSegment(p, pForest, pRuins);
            float d4 = DistToSegment(p, pQuarry, pRuins);

            return Mathf.Min(Mathf.Min(d1, d2), Mathf.Min(d3, d4));
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            return Vector2.Distance(p, a + t * ab);
        }

        public static void CreateAuthoredEnvironmentPrefab(TerrainData terrainData)
        {
            // Check if existing prefab has a valid terrain material reference.
            // If m_MaterialTemplate is {fileID: 0}, delete and regenerate.
            if (File.Exists(AuthoredEnvPrefabPath))
            {
                var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AuthoredEnvPrefabPath);
                if (existingPrefab != null)
                {
                    var existingTerrain = existingPrefab.GetComponentInChildren<Terrain>();
                    if (existingTerrain != null && existingTerrain.materialTemplate != null)
                        return; // Prefab is valid, keep it
                }
                // Prefab exists but terrain material is null — delete and regenerate
                AssetDatabase.DeleteAsset(AuthoredEnvPrefabPath);
            }

            // Load the persistent terrain material — MUST exist as an asset, never runtime
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/StylizedTerrain.mat");
            if (terrainMat == null)
            {
                Debug.LogError("StylizedTerrain.mat not found! GenerateMaterials() must be called first.");
                return;
            }

            var envRoot = new GameObject("Environment");
            var terrainGo = new GameObject("Terrain");
            terrainGo.transform.SetParent(envRoot.transform);
            terrainGo.transform.position = new Vector3(-250, 0, -150);

            var terrain = terrainGo.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            terrain.materialTemplate = terrainMat; // Persistent asset reference — serializes correctly
            terrain.shadowCastingMode = ShadowCastingMode.On;

            var collider = terrainGo.AddComponent<TerrainCollider>();
            collider.terrainData = terrainData;

            new GameObject("Cliffs").transform.SetParent(envRoot.transform);
            new GameObject("Vegetation").transform.SetParent(envRoot.transform);
            new GameObject("Roads").transform.SetParent(envRoot.transform);
            new GameObject("Landmarks").transform.SetParent(envRoot.transform);

            PrefabUtility.SaveAsPrefabAsset(envRoot, AuthoredEnvPrefabPath);
            Object.DestroyImmediate(envRoot);
        }
    }
}
