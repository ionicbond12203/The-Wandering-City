#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    public sealed class ExpandedBuildQA : MonoBehaviour
    {
        static string output; GameSession game;OrbitCamera orbit;double renderMs;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-worldQA");if(i<0||i+1>=args.Length)return;
            output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(output);
            GameSession.SavePathOverride=Path.Combine(output,"isolated-"+Guid.NewGuid().ToString("N"),"journey.json");
            Application.runInBackground=true;var go=new GameObject("Expanded world build QA");DontDestroyOnLoad(go);go.AddComponent<ExpandedBuildQA>();
        }
        void OnEnable()=>Application.logMessageReceived+=OnLog;
        void OnDisable()=>Application.logMessageReceived-=OnLog;
        void OnLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){File.WriteAllText(Path.Combine(output,"failure.txt"),message+"\n"+stack);Application.Quit(1);}}
        void Require(bool value,string name){if(!value)throw new InvalidOperationException(name);}
        IEnumerator Start()
        {
            yield return new WaitUntil(()=>GameSession.Current!=null&&GameSession.Current.Hud!=null);
            game=GameSession.Current;game.Begin(true);game.Player.enabled=false;orbit=Camera.main.GetComponent<OrbitCamera>();orbit.AcceptInput=false;
            var canvas=FindFirstObjectByType<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=Camera.main;canvas.planeDistance=1;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);for(int i=0;i<20;i++)yield return null;
            var world=game.GetComponent<ExpandedWorld>();
            string[] names={"meadow","forest","quarry","ruins","canyon"};
            for(int i=0;i<ExpansionCatalog.Regions.Length;i++)
            {
                var r=ExpansionCatalog.Regions[i];var p=r.Center+new Vector2(-6,-23);Move(WorldBuilder.GroundPoint(p.x,p.y,.15f));
                var aim=r.At(1)-p;orbit.Yaw=Mathf.Atan2(aim.x,aim.y)*Mathf.Rad2Deg;orbit.Pitch=-8;
                yield return Capture("region-"+names[i]+".png");
                ExplorationRules.Activate(game.State,r.Key("beacon"));Require(game.Exploration.Teleport(r.Key("beacon")),"Unsafe beacon "+r.Id);
                Require(game.Save(),"save");Require(game.Saves.Load(out var restored,out _),"reload");Require(Mathf.Abs(restored.x-game.State.x)<.01f,"Far position was reset");
            }
            Move(world.Routes[0].foot+new Vector3(0,18.15f,5));orbit.Yaw=22;orbit.Pitch=12;yield return Capture("world-highpoint-a.png");
            Move(world.Routes[3].foot+new Vector3(0,18.15f,5));orbit.Yaw=-120;orbit.Pitch=12;yield return Capture("world-highpoint-b.png");
            var route=world.Routes[0];Move(route.launch);orbit.Yaw=0;orbit.Pitch=8;
            Require(game.Player.Traversal.TryGlide(),"Glide route launch failed");
            for(int i=0;i<40;i++)game.Player.Traversal.Simulate(.02f,Vector3.forward,Vector2.up,false,false,false,false,false,false);
            game.Player.GlideSail.gameObject.SetActive(true);
            yield return Capture("route-glide.png");
            var hidden=ExpansionCatalog.Regions[4].At(13);Move(WorldBuilder.GroundPoint(hidden.x,hidden.y-6,.15f));orbit.Yaw=0;orbit.Pitch=4;yield return Capture("route-hidden.png");
            var all=game.World.Interactions.Select(i=>i.transform.position).Concat(game.Exploration.Points.Values.Select(p=>p.transform.position)).ToArray();
            var bounds=new Bounds(all[0],Vector3.zero);foreach(var p in all)bounds.Encapsulate(p);
            int total=0,empty=0,baselineEmpty=0,navigable=0;for(float x=-508;x<512;x+=8)for(float z=-416;z<604;z+=8){total++;var p=new Vector2(x,z);if(!all.Any(a=>Vector2.Distance(p,ExpansionCatalog.XZ(a))<=100))empty++;if(!world.BaselinePositions.Any(a=>Vector2.Distance(p,ExpansionCatalog.XZ(a))<=100))baselineEmpty++;if(ExpansionCatalog.Playable(p))navigable++;}
            double start=Time.realtimeSinceStartupAsDouble;for(int i=0;i<180;i++)yield return null;
            var nav=game.GetComponent<PlayableNavigation>();
            var report=new Report{passed=true,baselineBounds=world.BaselineBounds.size,expandedBounds=bounds.size,diameter=all.Max(a=>all.Max(b=>Vector2.Distance(ExpansionCatalog.XZ(a),ExpansionCatalog.XZ(b)))),poiCount=game.Exploration.Points.Values.Count(),enemyCount=game.World.Enemies.Count,interactionCount=game.World.Interactions.Count,emptyBeyond100mPercent=100f*empty/total,baselineEmptyBeyond100mPercent=100f*baselineEmpty/total,baselineDiameter=world.BaselinePositions.Max(a=>world.BaselinePositions.Max(b=>Vector2.Distance(ExpansionCatalog.XZ(a),ExpansionCatalog.XZ(b)))),navigationMaskPercent=100f*navigable/total,navBounds=ExpansionCatalog.PlayableBounds.size,navTriangles=nav.TriangleCount,navBuildMs=nav.BuildMilliseconds,averageHiddenFrameMs=1000*(Time.realtimeSinceStartupAsDouble-start)/180,allocatedMB=Profiler.GetTotalAllocatedMemoryLong()/1048576f,renderRequestTotalMs=renderMs};
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));Application.Quit(0);
        }
        void Move(Vector3 p){game.State.x=p.x;game.State.y=p.y;game.State.z=p.z;game.Player.RestorePosition();Physics.SyncTransforms();Require(Vector2.Distance(ExpansionCatalog.XZ(p),ExpansionCatalog.XZ(game.Player.transform.position))<.2f,"QA point was corrected by collision: "+p);}
        IEnumerator Capture(string name)
        {
            for(int i=0;i<12;i++)yield return null;yield return new WaitForEndOfFrame();
            var clock=System.Diagnostics.Stopwatch.StartNew();var target=RenderTexture.GetTemporary(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
            RenderPipeline.SubmitRenderRequest(Camera.main,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
            Require(texture.GetPixels32().Any(p=>p.r>40||p.g>40||p.b>40),"Black capture "+name);
            File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());Destroy(texture);renderMs+=clock.Elapsed.TotalMilliseconds;
        }
        [Serializable] sealed class Report
        {
            public bool passed;public Vector3 baselineBounds,expandedBounds,navBounds;public float baselineDiameter,baselineEmptyBeyond100mPercent,diameter,emptyBeyond100mPercent,navigationMaskPercent,allocatedMB;
            public int poiCount,enemyCount,interactionCount,navTriangles;public double navBuildMs,averageHiddenFrameMs,renderRequestTotalMs;
            public string capture="Windows Build URP offscreen with camera-space HUD; hidden-frame timing is not a foreground gameplay benchmark";
        }
    }
}
#endif
