using System.Linq;
using UnityEngine;

namespace WanderingCity
{
    public sealed class WorldMapData : ScriptableObject
    {
        public Texture2D Texture;
        public Bounds WorldBounds;
        public static WorldMapData Load() => Resources.Load<WorldMapData>("WorldMap");
        public Vector2 WorldToUV(Vector3 p) => new Vector2((p.x - WorldBounds.min.x) / WorldBounds.size.x, (p.z - WorldBounds.min.z) / WorldBounds.size.z);
        public Vector2 WorldToMap(Vector3 p, Vector2 size) { var uv = WorldToUV(p); return new Vector2(uv.x * size.x, (1 - uv.y) * size.y); }
        public static Vector2 MinimapOffset(Vector3 world, Vector3 player, float yaw, float pixelsPerMeter, float radius)
        {
            var delta = world - player;
            var offset = (Vector2)(Quaternion.Euler(0, 0, yaw) * new Vector3(delta.x, delta.z)) * pixelsPerMeter;
            return Vector2.ClampMagnitude(offset, radius);
        }
        public static bool Visible(GameState state, string id) => state.discoveredPOIIds.Contains(id);

        // Resolve the current quest against the live interaction registry, including remaining resources.
        public static Vector3? ObjectivePosition(GameSession game)
        {
            var s = game.State;
            string prefix = !s.claimed.Any(id => id.StartsWith("wood-")) ? "wood-" : !s.claimed.Any(id => id.StartsWith("stone-")) ? "stone-" : null;
            if (prefix == null && !s.craftedPotion) return WorldBuilder.WorkbenchPosition;
            if (prefix == null && !s.placedBuilding) return WorldBuilder.GroundPoint(-7.5f, 1.5f);
            if (prefix == null && !s.claimed.Contains("camp-reward")) prefix = "camp-reward";
            if (prefix == null && s.weaponLevel == 1)
            {
                if (s.Count("ore") >= 5) return WorldBuilder.WorkbenchPosition;
                prefix = "ore-";
            }
            if (prefix != null)
            {
                var target = game.World.Interactions.Where(i => i.Id != null && i.Id.StartsWith(prefix) && i.Available)
                    .OrderBy(i => (i.transform.position - game.Player.transform.position).sqrMagnitude).FirstOrDefault();
                return target == null ? (Vector3?)null : target.transform.position;
            }
            return !s.buildings.Any(b => b.kind == "roof") ? WorldBuilder.GroundPoint(-7.5f, 1.5f) : (Vector3?)null;
        }
    }
}
