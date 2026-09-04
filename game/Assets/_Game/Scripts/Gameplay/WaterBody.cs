using UnityEngine;
using UnityEngine.Rendering;

namespace WanderingCity
{
    public sealed class WaterBody : MonoBehaviour
    {
        public static readonly Vector2 Center = new Vector2(-145, -180);
        public static readonly Vector2 Radius = new Vector2(21, 13);
        public const float Level = 8.25f;
        Mesh mesh;
        public static float Distance(float x, float z) => new Vector2((x - Center.x) / Radius.x, (z - Center.y) / Radius.y).magnitude;
        public static float ShapeTerrain(float x, float z, float height)
        {
            float r = Distance(x, z); if (r >= 1.2f) return height;
            float basin = Mathf.Lerp(7.35f, 8.65f, Mathf.SmoothStep(0, 1, r));
            return Mathf.Lerp(basin, height, Mathf.SmoothStep(0, 1, (r - 1) / .2f));
        }
        public void Create(Material material)
        {
            gameObject.name = "Windmirror pond / shallow water";
            transform.position = new Vector3(Center.x, Level, Center.y);
            const int segments = 96; var vertices = new Vector3[segments + 1]; var uv = new Vector2[vertices.Length]; var indices = new int[segments * 3];
            uv[0] = Vector2.one * .5f;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments; vertices[i + 1] = new Vector3(Mathf.Cos(angle) * Radius.x * 1.08f, 0, Mathf.Sin(angle) * Radius.y * 1.08f); uv[i + 1] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .5f + Vector2.one * .5f;
                indices[i * 3] = 0; indices[i * 3 + 1] = (i + 1) % segments + 1; indices[i * 3 + 2] = i + 1;
            }
            mesh = new Mesh { name = "Original pond surface / 96 triangles", vertices = vertices, uv = uv, triangles = indices }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = true;
        }
        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
