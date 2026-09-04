using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WanderingCity.Editor
{
    public static class WorldMapBaker
    {
        public const string OutputPath = "Assets/_Game/Art/UI/Maps/WorldMap_Base.png";
        [MenuItem("Wandering City/Map/Bake World Map")]
        public static void Bake()
        {
            const string path = "Assets/_Game/Scenes/World.unity";
            var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var terrain = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>()).First();
                BakeTerrain(terrain, 1024);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        public static void BakeTerrain(Terrain terrain, int resolution)
        {
            var data = terrain.terrainData; var origin = terrain.transform.position;
            var weights = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            var palette = data.terrainLayers.Select(layer => LayerColor(layer.name)).ToArray();
            var regions = terrain.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<RegionDiscovery>()).ToArray();
            var water = terrain.gameObject.scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>())
                .Where(r => r.name.ToLowerInvariant().Contains("water")).Select(r => r.bounds).ToArray();
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
            var pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++) for (int x = 0; x < resolution; x++)
            {
                float u = (x + .5f) / resolution, v = (y + .5f) / resolution;
                float height = data.GetInterpolatedHeight(u, v), slope = data.GetSteepness(u, v);
                var p = origin + new Vector3(u * data.size.x, height, v * data.size.z);
                Color color = Color.black;
                for (int layer = 0; layer < palette.Length; layer++) color += palette[layer] * weights[Mathf.Min((int)(v * data.alphamapHeight), data.alphamapHeight - 1), Mathf.Min((int)(u * data.alphamapWidth), data.alphamapWidth - 1), layer];
                if (palette.Length == 0) color = LayerColor("grass");
                color = Color.Lerp(color, new Color(.65f, .64f, .52f), Mathf.Clamp01(height / data.size.y) * .65f);
                color *= Mathf.Lerp(1.12f, .57f, slope / 90);
                color = Color.Lerp(color, LayerColor(WorldBuilder.Region(p)), .08f);
                // Authored road network shares the same distance model as the terrain layers.
                if (TerrainHeightModel.RoadDistance(new Vector2(p.x, p.z)) < 2.8f) color = new Color(.72f, .65f, .43f);
                foreach (var region in regions) if (region.Bounds.Contains(p)) color = Color.Lerp(color, LayerColor(region.Id), .12f);
                foreach (var body in water) if (p.x >= body.min.x && p.x <= body.max.x && p.z >= body.min.z && p.z <= body.max.z && p.y <= body.max.y) color = new Color(.22f, .47f, .55f);
                pixels[y * resolution + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply(); Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath); importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true; importer.maxTextureSize = resolution; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            const string resourcePath = "Assets/_Game/Resources/WorldMap.asset";
            var map = AssetDatabase.LoadAssetAtPath<WorldMapData>(resourcePath);
            if (map == null) { map = ScriptableObject.CreateInstance<WorldMapData>(); AssetDatabase.CreateAsset(map, resourcePath); }
            map.Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputPath); map.WorldBounds = new Bounds(origin + data.size * .5f, data.size);
            EditorUtility.SetDirty(map); AssetDatabase.SaveAssets();
            Debug.Log("WORLD_MAP_BAKE_OK " + OutputPath);
        }
        static Color LayerColor(string name)
        {
            name = name.ToLowerInvariant();
            if (name.Contains("forest")) return new Color(.19f, .36f, .26f);
            if (name.Contains("quarry")) return new Color(.57f, .53f, .4f);
            if (name.Contains("ruins")) return new Color(.45f, .49f, .46f);
            if (name.Contains("drygrass")) return new Color(.48f, .49f, .29f);
            if (name.Contains("rock") || name.Contains("mesa")) return new Color(.48f, .5f, .44f);
            if (name.Contains("dirt") || name.Contains("path")) return new Color(.58f, .49f, .32f);
            if (name.Contains("sand") || name.Contains("canyon")) return new Color(.65f, .6f, .4f);
            return new Color(.28f, .43f, .29f);
        }
    }
}
