using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace WanderingCity.Editor
{
    public static class OpenWorldAuthoring
    {
        const string MeshDir = EnvironmentAuthoring.ArtDir + "/Meshes";
        static Mesh SaveMesh(Mesh mesh, string key)
        {
            string path=MeshDir+"/"+key+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null) { Object.DestroyImmediate(mesh); return existing; }
            AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static GameObject Part(Transform parent,string name,Mesh mesh,Material material,Vector3 p,Vector3 scale)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        static void Save(GameObject go,string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go,EnvironmentAuthoring.PrefabsDir+"/"+name+".prefab");Object.DestroyImmediate(go);
        }
        static void Lod(GameObject root, Renderer[] near, Renderer[] far)
        {
            var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.07f,near),new LOD(.015f,far)});group.RecalculateBounds();
        }
        public static void Generate(Dictionary<string,Material> mats, TerrainData data)
        {
            System.IO.Directory.CreateDirectory(MeshDir);
            for(int variant=0;variant<3;variant++)
            {
                string suffix=((char)('A'+variant)).ToString();
                var tree=new GameObject("Tree_Stylized_"+suffix);
                var trunk=SaveMesh(OriginalMesh.Loft("Bent trunk",7,new[]{.5f,.36f,.23f,.08f},.6f,variant),"Trunk"+suffix);
                Part(tree.transform,"Bent trunk",trunk,mats["Bark"],new Vector3(0,2.5f,0),new Vector3(.8f,5,.8f));
                var crown=SaveMesh(OriginalMesh.Canopy(variant),"Crown"+suffix);
                for(int j=0;j<4;j++)
                {
                    float angle=j*2.3f+variant;
                    var branch=Part(tree.transform,"Branch",trunk,mats["Bark"],new Vector3(Mathf.Cos(angle)*.6f,3.4f+j*.4f,Mathf.Sin(angle)*.6f),new Vector3(.22f,2,.22f));
                    branch.transform.localRotation=Quaternion.Euler(Mathf.Sin(angle)*44,0,Mathf.Cos(angle)*44);
                    Part(tree.transform,"Crown lobe",crown,mats["Foliage"],new Vector3(Mathf.Cos(angle)*1.3f,4.5f+j*.55f,Mathf.Sin(angle)*1.1f),new Vector3(3.9f-variant*.45f,2.1f+variant*.5f,3.1f));
                }
                var near=tree.GetComponentsInChildren<Renderer>();
                var far=Part(tree.transform,"Distant crown",SaveMesh(OriginalMesh.Canopy(variant,5),"CrownLow"+suffix),mats["Foliage"],new Vector3(0,5.4f,0),new Vector3(5.7f,3.8f,4.9f));
                Lod(tree,near,new[]{far.GetComponent<Renderer>()});
                var col=tree.AddComponent<CapsuleCollider>();col.radius=.4f;col.height=4;col.center=Vector3.up*2;
                if(variant==0) PrefabUtility.SaveAsPrefabAsset(tree,EnvironmentAuthoring.PrefabsDir+"/Tree_Stylized_01.prefab");
                Save(tree,"Tree_Stylized_"+suffix);
                foreach(string kind in new[]{"Rock","Cliff"})
                {
                    var root=new GameObject(kind+"_"+suffix);
                    Vector3 size=kind=="Rock"?new Vector3(2.5f,1.8f,2):new Vector3(10,8,6);
                    var mesh=SaveMesh(OriginalMesh.Rock(variant+7),kind+suffix);
                    var body=Part(root.transform,"Faceted strata",mesh,mats[kind],Vector3.up*size.y*.45f,size);
                    body.AddComponent<MeshCollider>().sharedMesh=mesh;
                    var low=Part(root.transform,"Distant strata",SaveMesh(OriginalMesh.Rock(variant+7,5),kind+suffix+"Low"),mats[kind],Vector3.up*size.y*.45f,size);
                    Lod(root,new[]{body.GetComponent<Renderer>()},new[]{low.GetComponent<Renderer>()});
                    if(kind=="Cliff") root.AddComponent<ClimbSurface>();
                    if(variant==0) PrefabUtility.SaveAsPrefabAsset(root,EnvironmentAuthoring.PrefabsDir+(kind=="Rock"?"/Rock_Boulder_01.prefab":"/Cliff_Modular_01.prefab"));
                    Save(root,kind+"_"+suffix);
                }
            }
            // Replace small spherical foliage too.
            var bush=new GameObject("Bush_Stylized_01");
            Part(bush.transform,"Leaves",SaveMesh(OriginalMesh.Canopy(12),"Bush"),mats["Foliage"],Vector3.up*.5f,new Vector3(1.8f,1.2f,1.4f));Save(bush,bush.name);
            string[] names={"GrassShort","GrassTall","FlowerDetail","SmallPlant"};
            var prototypes=new DetailPrototype[4];
            for(int i=0;i<4;i++)
            {
                var go=new GameObject(names[i]);
                go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(OriginalMesh.Grass(i),names[i]);
                var material=mats["Grass"];
                if(i==2)
                {
                    string path=EnvironmentAuthoring.MaterialsDir+"/MeadowFlower.mat";
                    material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null) {material=new Material(mats["Grass"]);material.SetColor("_TipColor",new Color(.94f,.78f,.4f));AssetDatabase.CreateAsset(material,path);}
                }
                go.AddComponent<MeshRenderer>().sharedMaterial=material;
                string prefabPath=EnvironmentAuthoring.PrefabsDir+"/"+names[i]+".prefab";
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,prefabPath);Object.DestroyImmediate(go);
                prototypes[i]=new DetailPrototype{prototype=prefab,usePrototypeMesh=true,useInstancing=true,renderMode=DetailRenderMode.VertexLit,minWidth=.8f,maxWidth=1.3f,minHeight=.8f,maxHeight=1.2f,noiseSpread=.2f,noiseSeed=721+i,positionJitter=1,alignToGround=.4f};
            }
            bool resetDetails=data.detailScatterMode!=DetailScatterMode.InstanceCountMode;
            if(resetDetails) data.SetDetailScatterMode(DetailScatterMode.InstanceCountMode);
            if(data.detailPrototypes.Length!=4 || resetDetails || !System.IO.File.Exists("Assets/_Game/Art/Environment/ExpandedDetails_v2.txt"))
            {
                data.SetDetailResolution(1024,32);data.detailPrototypes=prototypes;
                for(int k=0;k<4;k++)
                {
                    var density=new int[1024,1024];
                    for(int z=0;z<1024;z++) for(int x=0;x<1024;x++)
                    {
                        float wx=TerrainHeightModel.Origin.x+x+.5f,wz=TerrainHeightModel.Origin.z+z+.5f;
                        if(!GrassAllowed(wx,wz) || data.GetSteepness(x/1023f,z/1023f)>32) continue;
                        float n=Mathf.PerlinNoise(wx*.11f+k*13+100,wz*.11f+100);
                        density[z,x]=k==0? (n>.32f?4:2) : k==1?(n>.65f?2:0):(n>.77f?1:0);
                    }
                    data.SetDetailLayer(0,0,k,density);
                }
            }
            System.IO.File.WriteAllText("Assets/_Game/Art/Environment/ExpandedDetails_v2.txt", "Region grass coverage v2");
            Traveler(mats);
            EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
        }
        public static bool GrassAllowed(float x,float z)
        {
            if(!ExpansionCatalog.Playable(new Vector2(x,z))) return false;
            foreach(var region in ExpansionCatalog.Regions) {
                if(Vector2.Distance(new Vector2(x,z),region.Center)<95) {
                    float coverage=region.Id=="sun-quarry"?.72f:region.Id=="crown-ruins"?.5f:region.Id=="veil-canyon"?.25f:0;
                    if(Mathf.PerlinNoise(x*.035f+110,z*.035f+220)<coverage)return false;
                }
                foreach(var site in region.Sites) if(Vector2.Distance(new Vector2(x,z),region.Center+site)<5) return false;
                var foot=region.At(14); if(x>foot.x-58 && x<foot.x+12 && Mathf.Abs(z-foot.y)<12) return false;
            }
            if(TerrainHeightModel.RoadDistance(new Vector2(x,z))<2.8f) return false;
            if(x>-16&&x<14&&z>-8&&z<12) return false;
            if(x>24&&x<100&&z>-23&&z<44) return false;
            if(TerrainHeightModel.ValleyMask(x,z)>.85f) return false;
            foreach(var p in new[]{new Vector2(-48,65),new Vector2(64,59),new Vector2(20,139),new Vector2(-69,94),new Vector2(81,86),new Vector2(-5,103)})
                if(Vector2.Distance(new Vector2(x,z),p)<4) return false;
            return true;
        }
        static void Traveler(Dictionary<string,Material> mats)
        {
            var root=new GameObject("Traveler / original low-poly study");
            Mesh cloth=SaveMesh(OriginalMesh.Loft("Tailored cloth",8,new[]{.48f,.38f,.43f,.29f},0,0,.04f),"TravelerCloth");
            Mesh head=SaveMesh(OriginalMesh.Loft("Face planes",8,new[]{.27f,.45f,.48f,.32f},0,1,.02f),"TravelerHead");
            Material skin=new Material(mats["Character"]);skin.SetColor("_BaseColor",new Color(.87f,.68f,.48f));
            string skinPath=EnvironmentAuthoring.MaterialsDir+"/TravelerSkin.mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(skinPath);
            if(existing!=null){Object.DestroyImmediate(skin);skin=existing;}else AssetDatabase.CreateAsset(skin,skinPath);
            Part(root.transform,"Tunic",cloth,mats["Character"],new Vector3(0,1.02f,0),new Vector3(.6f,.7f,.48f));
            Part(root.transform,"Face",head,skin,new Vector3(0,1.63f,.035f),new Vector3(.4f,.42f,.36f));
            Part(root.transform,"Hood",cloth,mats["Bark"],new Vector3(0,1.73f,-.07f),new Vector3(.46f,.24f,.42f));
            Part(root.transform,"Cloak",cloth,mats["Character"],new Vector3(0,.95f,-.2f),new Vector3(.88f,.98f,.23f));
            Part(root.transform,"Scarf",cloth,mats["Flower"],new Vector3(0,1.43f,.02f),new Vector3(.5f,.14f,.43f));
            for(int side=-1;side<=1;side+=2)
            {
                Part(root.transform,"Sleeve",cloth,mats["Character"],new Vector3(side*.32f,1.09f,0),new Vector3(.19f,.48f,.23f));
                Part(root.transform,"Hand",head,skin,new Vector3(side*.34f,.79f,.06f),new Vector3(.14f,.18f,.16f));
                Part(root.transform,"Leg",cloth,mats["Bark"],new Vector3(side*.14f,.47f,0),new Vector3(.21f,.55f,.24f));
                Part(root.transform,"Boot",cloth,mats["Bark"],new Vector3(side*.14f,.14f,.05f),new Vector3(.26f,.28f,.38f));
            }
            Part(root.transform,"Side pack",cloth,mats["Bark"],new Vector3(-.3f,.91f,-.24f),new Vector3(.34f,.42f,.25f));
            var socket=new GameObject("Sword pivot");socket.transform.SetParent(root.transform,false);socket.transform.localPosition=new Vector3(.45f,1,.1f);
            Part(socket.transform,"Blade",cloth,mats["Rock"],new Vector3(0,0,.7f),new Vector3(.1f,.1f,1.3f));
            root.AddComponent<Animator>().runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Game/Resources/Traveler.controller");
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/_Game/Resources/Traveler_Stylized.prefab");Object.DestroyImmediate(root);
        }
        public static void Populate( GameObject environment, Terrain terrain, Dictionary<string,Material> mats)
        {
            var oldVegetation=environment.transform.Find("Vegetation");
            if(oldVegetation!=null)
                for(int i=oldVegetation.childCount-1;i>=0;i--)
                    if(oldVegetation.GetChild(i).name.Contains("Grass_Tuft") || oldVegetation.GetChild(i).name.Contains("Flower_Patch"))
                        Object.DestroyImmediate(oldVegetation.GetChild(i).gameObject);
            var cylinderMesh=OriginalMesh.Loft("Full-height carved column",8,new[]{.48f,.5f,.43f,.36f},.04f,32,.08f);
            var columnVertices=cylinderMesh.vertices;
            for(int i=0;i<columnVertices.Length;i++)columnVertices[i].y*=2;
            cylinderMesh.vertices=columnVertices;cylinderMesh.RecalculateNormals();cylinderMesh.RecalculateBounds();
            cylinderMesh=SaveMesh(cylinderMesh,"CarvedCylinderFullHeight");
            foreach(var filter in environment.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null && AssetDatabase.GetAssetPath(filter.sharedMesh)==MeshDir+"/CarvedCylinder.asset")filter.sharedMesh=cylinderMesh;
            // Upgrade only generated built-in meshes, preserving scene objects and collision components.
            foreach(var filter in environment.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.sharedMesh==null || AssetDatabase.GetAssetPath(filter.sharedMesh)!="Library/unity default resources") continue;
                var type=filter.sharedMesh.name;
                Mesh mesh = type=="Sphere" ? OriginalMesh.Canopy(31) : OriginalMesh.Loft("Carved " + type,8,new[]{.48f,.5f,.43f,.36f},.04f,32,.08f);
                filter.sharedMesh=type=="Cylinder"?cylinderMesh:SaveMesh(mesh,"Carved"+type);
            }
            var cliffGroup=environment.transform.Find("Cliffs");
            if(cliffGroup!=null)
            {
                int index=0;
                foreach(Transform child in cliffGroup)
                {
                    if(!child.name.Contains("Cliff"))continue;
                    float x=index<4?108+index%2*12:-112-index%2*12;
                    child.position=WorldBuilder.GroundPoint(x,45+index%4*24);index++;
                }
            }
            var landmarks=environment.transform.Find("Landmarks");
            if(landmarks!=null) foreach(Transform landmark in landmarks)
                if(landmark.name.Contains("Monolith")) landmark.position=WorldBuilder.GroundPoint(108,115);
            ExpansionAuthoring.Populate(environment, mats);
            terrain.detailObjectDistance=70;terrain.detailObjectDensity=1;terrain.drawInstanced=true;
            var middle=environment.transform.Find("Midground");
            if(middle==null) {middle=new GameObject("Midground").transform;middle.SetParent(environment.transform);}
            if(middle.childCount==0)
            {
                var random=new System.Random(821);
                for(int i=0;i<330;i++)
                {
                    float x=-180+(float)random.NextDouble()*330,z=25+(float)random.NextDouble()*200;
                    if(TerrainHeightModel.RoadDistance(new Vector2(x,z))<6 || (x>-30&&x<95&&z<155))continue;
                    string suffix=((char)('A'+i%3)).ToString();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentAuthoring.PrefabsDir+"/Tree_Stylized_"+suffix+".prefab");
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,middle);go.transform.position=WorldBuilder.GroundPoint(x,z);go.transform.rotation=Quaternion.Euler(0,i*137.5f,0);go.transform.localScale=Vector3.one*(.9f+(float)random.NextDouble()*.8f);
                }
            }
            var distant=environment.transform.Find("DistantLandscape");
            if(distant==null) {distant=new GameObject("DistantLandscape").transform;distant.SetParent(environment.transform);}
            if(distant.childCount==0)
            {
                for(int i=0;i<13;i++)
                {
                    float a=i*2.4f, distance=650+i%3*100;
                    var root=new GameObject("Horizon ridge "+i);root.transform.SetParent(distant,false);root.transform.position=new Vector3(Mathf.Cos(a)*distance,30,90+Mathf.Sin(a)*distance);
                    var rock=SaveMesh(OriginalMesh.Rock(i+25,7),"Horizon"+i);
                    var body=Part(root.transform,"Silhouette",rock,mats["Rock"],Vector3.zero,new Vector3(310,130+i%4*40,230));
                    body.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                    var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.008f,new[]{body.GetComponent<Renderer>()})});group.RecalculateBounds();
                }
                var landmark=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentAuthoring.PrefabsDir+"/Landmark_Spire.prefab"),middle);
                landmark.name="North star observatory";landmark.transform.position=WorldBuilder.GroundPoint(30,205);landmark.transform.localScale=Vector3.one*1.5f;
            }
            for(int i=0;i<distant.childCount;i++)
            {
                var ridge=distant.GetChild(i);float angle=i*2.4f,distance=850+i%3*90;
                ridge.position=new Vector3(Mathf.Cos(angle)*distance,-18,90+Mathf.Sin(angle)*distance);
                var meshFilter=ridge.GetComponentInChildren<MeshFilter>();
                meshFilter.sharedMesh=SaveMesh(OriginalMesh.Mountain(i+10),"ErodedHorizon"+i);
                meshFilter.transform.localScale=new Vector3(560,115+i%4*22,420);
                ridge.GetComponent<LODGroup>().RecalculateBounds();
            }
        }
    }
}
