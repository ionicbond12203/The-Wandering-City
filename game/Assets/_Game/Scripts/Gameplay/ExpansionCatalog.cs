using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WanderingCity
{
    public sealed class RegionContent
    {
        public string Id, Name, Landmark;
        public Vector2 Center; public float Elevation; public Color Color;
        public Vector2[] Sites; public Vector3[] Approach;
        public Vector2 At(int i) => Center + Sites[i];
        public string Key(string suffix) => "exp-" + Id + "-" + suffix;
    }
    // Hand-authored irregular clearings and winding approaches. IDs never depend on instance order or scene.
    public static class ExpansionCatalog
    {
        // Sites: beacon, landmark, 3 encounters, 4 treasures, puzzle, 3 resource clusters, secret, climb foot.
        public static readonly RegionContent[] Regions = {
            new RegionContent { Id="south-meadow", Name="逐风花原", Landmark="远行风轮", Center=new Vector2(-80,-190), Elevation=8, Color=new Color(.83f,.71f,.35f),
                Sites=new[]{ V(8,4),V(-30,24),V(50,25),V(-43,-37),V(38,-54),V(-55,40),V(13,-31),V(-22,-61),V(46,-60),V(4,43),V(-16,12),V(33,-14),V(-40,-4),V(-65,-50),V(58,55)},
                Approach=new[]{P(0,3,-10),P(-18,5,-65),P(-70,8,-110),P(-80,8,-190)} },
            new RegionContent { Id="west-forest", Name="雾叶古林", Landmark="三根守望树", Center=new Vector2(-275,45), Elevation=18, Color=new Color(.22f,.44f,.34f),
                Sites=new[]{V(10,-5),V(-24,35),V(35,47),V(-41,-20),V(22,-56),V(-53,11),V(53,5),V(-31,-53),V(28,-63),V(-5,12),V(13,31),V(-13,-24),V(41,-29),V(-61,49),V(57,62)},
                Approach=new[]{P(-65,10,42),P(-128,14,8),P(-200,18,-5),P(-275,18,45)} },
            new RegionContent { Id="sun-quarry", Name="琥珀断岩场", Landmark="悬石天秤", Center=new Vector2(240,-90), Elevation=32, Color=new Color(.78f,.52f,.3f),
                Sites=new[]{V(-8,0),V(22,38),V(-42,35),V(41,-28),V(-33,-54),V(50,13),V(-57,2),V(8,-49),V(-39,-62),V(3,18),V(-28,-14),V(31,0),V(49,51),V(65,-51),V(-60,60)},
                Approach=new[]{P(105,10,-10),P(152,18,-26),P(185,27,-65),P(240,32,-90)} },
            new RegionContent { Id="crown-ruins", Name="星冠旧城", Landmark="断环观星台", Center=new Vector2(145,320), Elevation=54, Color=new Color(.51f,.61f,.76f),
                Sites=new[]{V(0,-7),V(28,30),V(-44,21),V(38,-30),V(-24,51),V(57,6),V(-51,-33),V(10,60),V(-31,59),V(3,14),V(-21,-18),V(46,47),V(18,-48),V(-65,50),V(59,-57)},
                Approach=new[]{P(20,30,160),P(60,36,195),P(119,45,238),P(145,54,320)} },
            new RegionContent { Id="veil-canyon", Name="回音帷谷", Landmark="折光石门", Center=new Vector2(-115,260), Elevation=15, Color=new Color(.43f,.64f,.64f),
                Sites=new[]{V(2,-9),V(-27,26),V(40,28),V(-42,-28),V(23,52),V(-56,10),V(50,-14),V(5,60),V(30,59),V(-8,8),V(19,21),V(-23,-13),V(33,-40),V(-61,52),V(58,-56)},
                Approach=new[]{P(-50,20,105),P(-94,21,147),P(-153,18,192),P(-115,15,260)} }
        };
        static ExpansionCatalog() { foreach(var region in Regions) region.Sites[5]=region.Sites[13]+Vector2.up*3; }
        static Vector2 V(float x,float z) => new Vector2(x,z);
        static Vector3 P(float x,float y,float z) => new Vector3(x,y,z);
        public static readonly string[] Suffixes={"beacon","landmark","encounter-a","encounter-b","elite","cache-a","cache-b","puzzle-cache","elite-cache","puzzle","wood","stone","ore","secret","lookout"};
        public static IEnumerable<string> PoiIds => Regions.SelectMany(r=>Suffixes.Select(r.Key));
        public static IEnumerable<string> EnemyIds => Regions.SelectMany(r=>new[]{"guard-a-0","guard-a-1","guard-b-0","guard-b-1","warden"}.Select(r.Key));
        public static IEnumerable<string> ResourceIds => Regions.SelectMany(r=>new[]{"wood","stone","ore"}.SelectMany(k=>Enumerable.Range(0,3).Select(i=>r.Key(k+"-"+i))));
        public static string PuzzleForTreasure(string id) => id=="canyon-cache" ? "echo-puzzle" : Regions.FirstOrDefault(r=>r.Key("puzzle-cache")==id)?.Key("puzzle");
        public static string[] EnemiesFor(string id)
        {
            if(id=="north-camp") return WorldCatalog.CampEnemies;
            foreach(var r in Regions) {
                if(id==r.Key("elite-cache") || id==r.Key("elite")) return new[]{r.Key("warden")};
                if(id==r.Key("encounter-a")) return new[]{r.Key("guard-a-0"),r.Key("guard-a-1")};
                if(id==r.Key("encounter-b")) return new[]{r.Key("guard-b-0"),r.Key("guard-b-1")};
            }
            return System.Array.Empty<string>();
        }
        public static bool RewardUnlocked(GameState state,string id)
        {
            string puzzle=PuzzleForTreasure(id);
            return (puzzle==null || state.completedPuzzleIds.Contains(puzzle)) && EnemiesFor(id).All(state.defeated.Contains);
        }
        public static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b,out float t)
        { t=Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);return Vector2.Distance(p,Vector2.Lerp(a,b,t)); }
        public static float RoadDistance(Vector2 p)
        {
            float d=float.MaxValue;
            foreach(var r in Regions) for(int i=1;i<r.Approach.Length;i++) d=Mathf.Min(d,SegmentDistance(p,XZ(r.Approach[i-1]),XZ(r.Approach[i]),out _));
            return d;
        }
        public static Vector2 XZ(Vector3 p) => new Vector2(p.x,p.z);
        public static bool Playable(Vector2 p,float margin=0)
        {
            if(p.x>=-110-margin && p.x<=110+margin && p.y>=-40-margin && p.y<=175+margin) return true;
            if(Regions.Any(r=>Mathf.Abs(p.x-r.Center.x)<92+margin && Mathf.Abs(p.y-r.Center.y)<92+margin)) return true;
            return RoadDistance(p)<18+margin;
        }
        public static Bounds PlayableBounds => new Bounds(new Vector3(-20,85,68),new Vector3(730,210,715));
        public static float ShapeTerrain(float x,float z,float height)
        {
            var p=V(x,z);
            // Preserve the original tutorial terrain and its climb fixtures exactly.
            if(x>=-105 && x<=105 && z>=-35 && z<=175) return height;
            float original=height;
            foreach(var ridge in new[]{new Vector2(-180,155),new Vector2(80,-125),new Vector2(105,195)})
                height+=65*Mathf.Exp(-2*(Mathf.Pow((x-ridge.x)/48,2)+Mathf.Pow((z-ridge.y)/48,2)));
            foreach(var r in Regions) {
                float distance=Vector2.Distance(p,r.Center);
                float blend=1-Mathf.SmoothStep(0,1,(distance-85)/55);
                height=Mathf.Lerp(height,r.Elevation+Mathf.PerlinNoise(x*.023f+17,z*.023f+39)*2,blend);
                for(int i=1;i<r.Approach.Length;i++) {
                    float d=SegmentDistance(p,XZ(r.Approach[i-1]),XZ(r.Approach[i]),out float t);
                    height=Mathf.Lerp(height,Mathf.Lerp(r.Approach[i-1].y,r.Approach[i].y,t),1-Mathf.SmoothStep(0,1,(d-6)/14));
                }
            }
            float edge=Mathf.Max(Mathf.Max(-105-x,x-105),Mathf.Max(-35-z,z-175));
            return Mathf.Lerp(original,height,Mathf.SmoothStep(0,1,edge/25));
        }
    }
}
