using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WanderingCity
{
    public sealed class ExpandedWorld : MonoBehaviour
    {
        public Vector3[] BaselinePositions { get; private set; }
        public Bounds BaselineBounds { get; private set; }
        public readonly List<PuzzleController> Puzzles = new List<PuzzleController>();
        readonly List<Mesh> ownedMeshes = new List<Mesh>();
        public readonly List<(RegionContent region, Vector3 foot, Vector3 launch, Vector3 landing)> Routes = new List<(RegionContent,Vector3,Vector3,Vector3)>();
        GameSession game; WorldBuilder world; ExplorationWorld exploration; Transform geometry, root;
        public void Create(GameSession owner,WorldBuilder builder,Transform solids,Transform parent)
        {
            game=owner;world=builder;geometry=solids;root=parent;exploration=GetComponent<ExplorationWorld>();
            var positions=world.Interactions.Select(i=>i.transform.position).Concat(exploration.Points.Values.Select(p=>p.transform.position)).ToArray();
            var baseline=new Bounds(positions[0],Vector3.zero);foreach(var p in positions)baseline.Encapsulate(p);BaselineBounds=baseline;BaselinePositions=positions;
            foreach(var r in ExpansionCatalog.Regions) CreateRegion(r);
        }
        Vector3 Ground(Vector2 p,float offset=0) => WorldBuilder.GroundPoint(p.x,p.y,offset);
        void CreateRegion(RegionContent r)
        {
            exploration.Region(r.Id,r.Name,Ground(r.Center,45),new Vector3(190,160,190));
            for(int i=0;i<ExpansionCatalog.Suffixes.Length;i++)
            {
                var type=i==0?PoiType.TeleportPoint:i==1||i==14?PoiType.Landmark:i<=4?PoiType.EnemyCamp:i<=8?PoiType.Treasure:i==9?PoiType.Puzzle:i<=12?PoiType.ResourceArea:PoiType.Secret;
                string title=i==0?r.Name+"信标":i==1?r.Landmark:i==2?"巡守营地":i==3?"岔路伏兵":i==4?"区域守望者":i==7?"共鸣封印匣":i==8?"守望者遗藏":i<=8?"行旅秘藏":i==9?"双音石阵":i==10?"枯木堆":i==11?"碎岩台":i==12?"星矿脉":i==13?"隐谷石龛":"踏风望台";
                var p=Ground(r.At(i),type==PoiType.Treasure||type==PoiType.TeleportPoint?.8f:0);
                exploration.Poi(r.Key(ExpansionCatalog.Suffixes[i]),title,type,p, i==1?16:10,Ground(r.At(i)+Vector2.down*3,.15f),i==7||i==8?TreasureTier.Rare:TreasureTier.Common);
            }
            var puzzle=new GameObject(r.Key("puzzle-controller")).AddComponent<PuzzleController>();puzzle.transform.SetParent(root);
            puzzle.Session=game;puzzle.Id=r.Key("puzzle");puzzle.Progress=new PuzzleProgress(r.Key("note-low"),r.Key("note-high"));Puzzles.Add(puzzle);
            for(int i=0;i<2;i++)
            {
                string id=r.Key(i==0?"note-low":"note-high");var p=Ground(r.At(9)+new Vector2(i==0?-6:7,i==0?3:-4),.8f);
                var item=new GameObject(id).AddComponent<WorldInteractable>();item.transform.SetParent(root);item.transform.position=p;
                item.Session=game;item.Id=id;item.Label=r.Name+" / 共鸣石";item.Puzzle=puzzle;world.Interactions.Add(item);
                world.Shape("Resonance stone",PrimitiveType.Cylinder,p,new Vector3(.8f,1.1f,.8f),r.Color,item.transform,false);
            }
            for(int cluster=0;cluster<3;cluster++) for(int i=0;i<3;i++)
            {
                string kind=new[]{"wood","stone","ore"}[cluster];var p=Ground(r.At(10+cluster)+new Vector2(i*2.3f-2,Mathf.Sin(i*2.4f)*2),.7f);
                world.Resource(r.Key(kind+"-"+i),GameHud.ItemName(kind)+" ×3",p,kind,3,r.Color);
            }
            // Landmark silhouettes differ by biome; all local geometry follows the terrain datum.
            var center=Ground(r.At(1));
            for(int i=0;i<3;i++)
            {
                var p=center+new Vector3((i-1)*4,0,i%2*3);
                var pillar=world.Shape(r.Landmark,PrimitiveType.Cube,p+Vector3.up*(7+i*2),new Vector3(2,14+i*4,2),r.Color,geometry);
                var mesh=OriginalMesh.Loft(r.Landmark,7,new[]{.5f,.6f,.3f},.05f,i+27,.07f);ownedMeshes.Add(mesh);pillar.GetComponent<MeshFilter>().sharedMesh=mesh;
                if(r.Id=="west-forest") {
                    var crown=world.Shape("Ancient guardian crown",PrimitiveType.Cube,p+Vector3.up*(16+i*2),new Vector3(12,8,10),r.Color,root,false);
                    var canopy=OriginalMesh.Canopy(i+80);ownedMeshes.Add(canopy);crown.GetComponent<MeshFilter>().sharedMesh=canopy;
                }
            }
            if(r.Id!="west-forest")world.Shape(r.Landmark+" lintel",PrimitiveType.Cube,center+Vector3.up*17,new Vector3(14,2,3),r.Color,geometry);
            if(r.Id=="south-meadow") for(int spoke=0;spoke<6;spoke++) {
                var blade=world.Shape("Windwheel sail",PrimitiveType.Cube,center+new Vector3(0,18,-3),new Vector3(17,.8f,.3f),r.Color,root,false);
                blade.transform.rotation=Quaternion.Euler(0,0,spoke*30);
            }
            if(r.Id=="sun-quarry") for(int side=-1;side<=1;side+=2) {
                world.Shape("Suspended counterweight",PrimitiveType.Cube,center+new Vector3(side*5,10,0),new Vector3(3,4,3),r.Color,root,false);
                world.Shape("Counterweight chain",PrimitiveType.Cube,center+new Vector3(side*5,14,0),new Vector3(.2f,6,.2f),Color.gray,root,false);
            }
            if(r.Id=="crown-ruins") for(int part=0;part<7;part++) {
                float angle=part*Mathf.PI/4;
                var fragment=world.Shape("Broken observatory ring",PrimitiveType.Cube,center+new Vector3(Mathf.Cos(angle)*9,24+Mathf.Sin(angle)*9,0),new Vector3(6,1.5f,2),r.Color,root,false);
                fragment.transform.rotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg+90);
            }
            // Hidden ruin approach bends between opaque rock walls; reward lies at its far end.
            var hidden=Ground(r.At(13));
            for(int i=0;i<4;i++)
            {
                var p=Ground(r.At(13)+new Vector2(i%2==0?-5:5,-12+i*6));
                world.Shape("Hidden approach wall",PrimitiveType.Cube,p+Vector3.up*4,new Vector3(3,8,7),r.Color,geometry);
            }
            // Climbable shortcut and a gentle, walkable ramp to the same 18m outlook.
            var foot=Ground(r.At(14)); const float height=18;
            var cliff=world.Shape("Climb shortcut / "+r.Name,PrimitiveType.Cube,foot+Vector3.up*9,new Vector3(14,height,14),r.Color,geometry);
            cliff.AddComponent<ClimbSurface>();
            var start=Ground(r.At(14)+Vector2.left*54);var end=foot+new Vector3(-7,height,0);
            var ramp=world.Shape("Safe outlook approach / "+r.Name,PrimitiveType.Cube,(start+end)*.5f-Vector3.up*.2f,new Vector3(Vector3.Distance(start,end),.4f,6),r.Color,geometry);
            ramp.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg);
            Vector3 launch=foot+new Vector3(0,height+.5f,8), landing=Ground(r.At(14)+Vector2.up*28,.15f);
            Routes.Add((r,foot,launch,landing));
            if(exploration.Points.TryGet(r.Key("lookout"),out var lookout))lookout.transform.position=foot+Vector3.up*height;
        }
        public void SpawnEnemies()
        {
            foreach(var r in ExpansionCatalog.Regions) {
                for(int encounter=0;encounter<2;encounter++)for(int i=0;i<2;i++)
                    world.Enemy(r.Key("guard-"+(encounter==0?"a":"b")+"-"+i),Ground(r.At(2+encounter)+new Vector2(i*4-2,2),.1f));
                world.Enemy(r.Key("warden"),Ground(r.At(4),.1f),true);
            }
        }
        public void ResetPuzzles() { foreach(var puzzle in Puzzles)puzzle.ResetProgress(); }
        void OnDestroy(){foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);}
    }
}
