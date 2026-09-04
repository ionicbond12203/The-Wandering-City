using UnityEngine;

namespace WanderingCity
{
    /// <summary>Original, deterministic terrain in metres. Features are placed by design, not by a perimeter mask.</summary>
    public static class TerrainHeightModel
    {
        public const int Resolution = 513;
        public const float Size = 1024, Height = 160;
        public static readonly Vector3 Origin = new Vector3(-512, 0, -420);
        static float Hill(float x, float z, float cx, float cz, float rx, float rz) =>
            Mathf.Exp(-2f * (Mathf.Pow((x - cx) / rx, 2) + Mathf.Pow((z - cz) / rz, 2)));
        static float Noise(float x, float z, float frequency, float seed) => Mathf.PerlinNoise(x * frequency + seed, z * frequency + seed);
        public static float ValleyMask(float x, float z) => Hill(x, z, 10 + 15 * Mathf.Sin(z * .022f), z, 19, 1);
        public static float BaseRollingNoise(float x, float z) => 1.2f + 5 * Noise(x, z, .012f, 41) + 1.1f * Noise(x, z, .047f, 83) + .18f * Noise(x, z, .14f, 19);
        public static float ForestFeature(float x, float z) => 11 * Hill(x, z, -58, 70, 65, 85);
        public static float QuarryFeature(float x, float z) => 23 * Hill(x, z, 73, 66, 66, 64);
        public static float RuinsPlateauFeature(float x, float z) => 32 * Hill(x, z, 20, 142, 85, 83);
        public static float DistantMountainFeature(float x, float z) =>
            105 * Hill(x, z, -285, 300, 130, 160) + 126 * Hill(x, z, 260, 390, 165, 155) +
            67 * Hill(x, z, 350, -140, 145, 160) + 72 * Hill(x, z, -330, -230, 160, 110);
        public static float RidgeNoise(float x, float z) => 22 * Hill(x, z, 185, 30, 115, 180) + 16 * Hill(x, z, -155, 175, 110, 150);
        static float Pad(float value, float x, float z, float cx, float cz, float radius, float target, float feather = 8)
        {
            float d = Vector2.Distance(new Vector2(x,z), new Vector2(cx,cz));
            return Mathf.Lerp(target, value, Mathf.SmoothStep(0, 1, (d - radius) / feather));
        }
        public static float Sample(float x, float z)
        {
            float h = BaseRollingNoise(x,z) + ForestFeature(x,z) + QuarryFeature(x,z) + RuinsPlateauFeature(x,z) + RidgeNoise(x,z) + DistantMountainFeature(x,z);
            h -= ValleyMask(x,z) * 2.2f;
            // Small construction and interaction pads; the meadow itself remains rolling.
            h = Pad(h,x,z,-7.5f,1.5f,9,2.1f);
            h = Pad(h,x,z,3,3,4,2.1f);
            h = Pad(h,x,z,8,2,3,2.1f);
            // Keep authored climb fixtures and their landings on a shared local datum.
            h = Pad(h,x,z,51,-8,16,5);
            h = Pad(h,x,z,78,8,17,5);
            h = Pad(h,x,z,81,32,14,5);
            return Mathf.Clamp(h, .4f, 148);
        }
        public static float RoadDistance(Vector2 p)
        {
            Vector2 a = new Vector2(0,4), b = new Vector2(-46,62), c = new Vector2(61,57), d = new Vector2(20,128);
            return Mathf.Min(Mathf.Min(Segment(p,a,b),Segment(p,a,c)), Mathf.Min(Segment(p,b,d),Segment(p,c,d)));
        }
        static float Segment(Vector2 p, Vector2 a, Vector2 b) => Vector2.Distance(p, a + Mathf.Clamp01(Vector2.Dot(p-a,b-a) / (b-a).sqrMagnitude) * (b-a));
    }
}
