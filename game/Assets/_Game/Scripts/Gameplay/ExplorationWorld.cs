using System.Collections.Generic;
using UnityEngine;

namespace WanderingCity
{
    public sealed class ExplorationWorld : MonoBehaviour
    {
        public readonly StableRegistry<ExplorationPoi> Points = new StableRegistry<ExplorationPoi>();
        public readonly List<RegionDiscovery> Regions = new List<RegionDiscovery>();
        public PuzzleController Puzzle { get; private set; }
        GameSession session;
        WorldBuilder world;
        Transform root;
        public void Create(GameSession owner, WorldBuilder builder, Transform geometry, Transform worldRoot)
        {
            session = owner; world = builder; root = worldRoot;
            // New eastern loop, outside the existing resource and combat routes: low 0, mid 6, high 14.
            Color rock = new Color(.43f, .51f, .48f);
            var mesa = world.Shape("Echo mesa / climb shortcut", PrimitiveType.Cube, new Vector3(78, 7, 8), new Vector3(22, 14, 22), rock, geometry);
            mesa.AddComponent<ClimbSurface>();
            var shelf = world.Shape("Mid shelf / climbable", PrimitiveType.Cube, new Vector3(51, 3, -8), new Vector3(20, 6, 18), rock, geometry);
            shelf.AddComponent<ClimbSurface>();
            // Safe long ramps reach both heights without stamina. Top meets the platforms without a lip.
            Ramp("Shelf walking approach", new Vector3(27, 3, -8), 30, 6, 8, geometry);
            Ramp("Mesa walking approach", new Vector3(53, 10, 8), 28, 8, 7, geometry);
            world.Shape("Shelf connector", PrimitiveType.Cube, new Vector3(46, 3, 4), new Vector3(14, 6, 10), rock, geometry);
            world.Shape("Split canyon west bank", PrimitiveType.Cube, new Vector3(72, 3, 32), new Vector3(10, 6, 15), rock, geometry).AddComponent<ClimbSurface>();
            world.Shape("Split canyon east bank", PrimitiveType.Cube, new Vector3(90, 3, 32), new Vector3(10, 6, 15), rock, geometry).AddComponent<ClimbSurface>();
            world.Shape("Canyon footpath", PrimitiveType.Cube, new Vector3(81, .03f, 32), new Vector3(7, .05f, 19), new Color(.61f, .59f, .4f), root, false);
            Poi("base-beacon", "归途信标", PoiType.TeleportPoint, new Vector3(8, .8f, 4), 9, new Vector3(8, .15f, 1));
            Poi("mesa-beacon", "回声台地信标", PoiType.TeleportPoint, new Vector3(77, 14.8f, 8), 10, new Vector3(75, 14.15f, 8));
            Poi("wind-spire", "听风尖塔", PoiType.Landmark, new Vector3(84, 14, 14), 12);
            Poi("shelf-cache", "行旅匣 / 常见", PoiType.Treasure, new Vector3(53, 6.8f, -9), 9, tier: TreasureTier.Common);
            Poi("canyon-cache", "回声匣 / 珍稀", PoiType.Treasure, new Vector3(81, .8f, 35), 7, tier: TreasureTier.Rare);
            Poi("echo-puzzle", "双石共鸣", PoiType.Puzzle, new Vector3(81, 0, 29), 8);
            Poi("north-camp", "沉眠守卫营地", PoiType.EnemyCamp, new Vector3(20, 0, 130), 20);
            Poi("ore-garden", "星砂矿苑", PoiType.ResourceArea, new Vector3(62, 0, 77), 14);
            Poi("canyon-secret", "风隙小径", PoiType.Secret, new Vector3(81, 0, 24), 5);
            Puzzle = new GameObject("Echo puzzle controller").AddComponent<PuzzleController>(); Puzzle.transform.SetParent(root); Puzzle.Session = session;
            AddNode("echo-west", new Vector3(79, .8f, 28)); AddNode("echo-east", new Vector3(83, .8f, 32));
            Region("wind-meadow", "风息原野", new Vector3(0, 8, 30), new Vector3(110, 30, 125));
            Region("echo-mesa", "回声台地", new Vector3(64, 12, 3), new Vector3(65, 32, 44));
            Region("split-canyon", "风隙峡谷", new Vector3(81, 8, 32), new Vector3(28, 22, 19));
        }
        void Ramp(string name, Vector3 center, float run, float rise, float width, Transform geometry)
        {
            var go = world.Shape(name, PrimitiveType.Cube, center - Vector3.up * .15f, new Vector3(Mathf.Sqrt(run * run + rise * rise), .3f, width), new Color(.54f, .56f, .43f), geometry);
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(rise, run) * Mathf.Rad2Deg);
        }
        void Poi(string id, string name, PoiType type, Vector3 position, float radius, Vector3 spawn = default, TreasureTier tier = TreasureTier.Common)
        {
            var go = new GameObject(id); go.transform.SetParent(root); go.transform.position = position;
            var poi = go.AddComponent<ExplorationPoi>(); poi.Id = id; poi.DisplayName = name; poi.Type = type; poi.Session = session; poi.SpawnPoint = spawn; poi.Radius = radius;
            Points.Register(id, poi);
            var trigger = go.AddComponent<SphereCollider>(); trigger.radius = radius; trigger.isTrigger = true;
            var body = go.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            Color color = type == PoiType.Treasure ? new Color(.96f, .72f, .25f) : new Color(.3f, .83f, .83f);
            if (type == PoiType.Landmark || type == PoiType.TeleportPoint)
            {
                world.Shape(name + " shaft", PrimitiveType.Cylinder, position + Vector3.up * 3, new Vector3(.6f, 3, .6f), color, go.transform, false);
                world.Shape(name + " split crown", PrimitiveType.Cube, position + Vector3.up * 6, new Vector3(3, .6f, 1), color, go.transform, false);
            }
            if (type == PoiType.TeleportPoint || type == PoiType.Treasure)
            {
                var item = go.AddComponent<WorldInteractable>(); item.Id = id; item.Label = name; item.Session = session; item.Exploration = poi; world.Interactions.Add(item);
                if (type == PoiType.Treasure)
                {
                    item.Reward = new Dictionary<string, int> { ["ore"] = tier == TreasureTier.Rare ? session.Balance.rareTreasureOre : session.Balance.commonTreasureOre };
                    if (tier == TreasureTier.Rare) item.Reward["potion"] = session.Balance.rareTreasurePotions;
                    poi.RewardVisual = world.Shape(name, PrimitiveType.Cube, position, new Vector3(1.4f, .8f, 1), color, go.transform, false);
                }
            }
        }
        void AddNode(string id, Vector3 position)
        {
            var go = new GameObject(id); go.transform.SetParent(root); go.transform.position = position;
            var item = go.AddComponent<WorldInteractable>(); item.Id = id; item.Label = "共鸣石 / 点亮"; item.Session = session; item.Puzzle = Puzzle; world.Interactions.Add(item);
            world.Shape(id, PrimitiveType.Cylinder, position, new Vector3(.65f, .8f, .65f), new Color(.56f, .73f, .85f), go.transform, false);
        }
        void Region(string id, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject(id); go.transform.SetParent(root); go.transform.position = center;
            var region = go.AddComponent<RegionDiscovery>(); region.Id = id; region.DisplayName = name; region.Session = session;
            var trigger = go.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = size;
            region.Bounds = new Bounds(center, size);
            var body = go.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            Regions.Add(region);
        }
        public bool Teleport(string id)
        {
            if (!session.Started || !ExplorationRules.CanTeleport(session.State, id) || !Points.TryGet(id, out var point) || session.Player.Action == PlayerAction.Dead) return false;
            var player = session.Player; Vector3 destination = point.SpawnPoint;
            if (!Physics.Raycast(destination + Vector3.up, Vector3.down, out var floor, 2, session.Balance.solidMask, QueryTriggerInteraction.Ignore) || Vector3.Angle(floor.normal, Vector3.up) > player.Controller.slopeLimit) return false;
            destination = floor.point + Vector3.up * .1f;
            if (!player.Traversal.SafeStandingPosition(destination)) { session.Notify("传送落点被阻挡"); return false; }
            session.State.x = destination.x; session.State.y = destination.y; session.State.z = destination.z;
            player.RestorePosition(); world.ResetEnemies(); session.ExitBuilding(); session.Target = null; session.SetMenu(false); session.Save(); session.Notify("循信标返回 / " + point.DisplayName); return true;
        }
        public void Restore()
        {
            Puzzle?.ResetProgress();
            foreach (var point in Points.Values) point.Refresh();
        }
        public string RegionNameAt(Vector3 position)
        {
            for (int i = Regions.Count - 1; i >= 0; i--) if (Regions[i].Bounds.Contains(position)) return Regions[i].DisplayName;
            return WorldBuilder.RegionName(WorldBuilder.Region(position));
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Reset exploration (keeps claimed treasures to prevent reward farming)")]
        public void ResetExploration()
        {
            session.State.discoveredPOIIds.Clear(); session.State.discoveredRegionIds.Clear(); session.State.activatedTeleportIds.Clear();
            Restore(); session.Save();
        }
        [ContextMenu("Teleport to activated mesa")]
        void DebugTeleport() => Teleport("mesa-beacon");
#endif
    }
}
