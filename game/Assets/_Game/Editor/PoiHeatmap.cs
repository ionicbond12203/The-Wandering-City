using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WanderingCity.Editor
{
    public sealed class PoiHeatmap : EditorWindow
    {
        int radius=100; bool showDeadZones=true;
        [MenuItem("Wandering City/Open World/POI Heatmap")]
        static void Open()=>GetWindow<PoiHeatmap>("POI Heatmap");
        void OnEnable()=>SceneView.duringSceneGui+=DrawScene;
        void OnDisable()=>SceneView.duringSceneGui-=DrawScene;
        void OnGUI()
        {
            EditorGUILayout.LabelField("Playable content / 1 km terrain",EditorStyles.boldLabel);
            radius=new[]{50,100,200}[GUILayout.Toolbar(System.Array.IndexOf(new[]{50,100,200},radius),new[]{"50 m","100 m","200 m"})];
            showDeadZones=EditorGUILayout.Toggle("Show empty 100m corridors",showDeadZones);
            foreach(var r in ExpansionCatalog.Regions) {
                int count=ExpansionCatalog.Regions.Sum(other=>other.Sites.Count(p=>Vector2.Distance(other.Center+p,r.Center)<=radius));
                EditorGUILayout.LabelField(r.Name,$"{count} POIs / {radius}m");
            }
            EditorGUILayout.HelpBox("Green: dense · yellow: sparse · red: no POI within radius. Blue: safe routes; violet: climb/glide links. Decorative terrain is omitted. Play mode overlays use live POIs; editor preview uses stable authored sites.",MessageType.Info);
            if(GUILayout.Button("Frame playable world")) SceneView.lastActiveSceneView?.Frame(ExpansionCatalog.PlayableBounds,false);
            SceneView.RepaintAll();
        }
        void DrawScene(SceneView view)
        {
            var live=Object.FindObjectsByType<ExplorationPoi>(FindObjectsSortMode.None);
            var points=live.Length>0?live.Select(p=>p.transform.position).ToArray():ExpansionCatalog.Regions.SelectMany(r=>r.Sites.Select(s=>WorldBuilder.GroundPoint(r.Center.x+s.x,r.Center.y+s.y))).ToArray();
            foreach(var r in ExpansionCatalog.Regions) {
                Handles.color=r.Color;var c=WorldBuilder.GroundPoint(r.Center.x,r.Center.y,2);Handles.DrawWireCube(c,new Vector3(190,4,190));Handles.Label(c,r.Name);
                Handles.color=Color.cyan;for(int i=1;i<r.Approach.Length;i++)Handles.DrawLine(r.Approach[i-1],r.Approach[i],3);
                var foot=WorldBuilder.GroundPoint(r.At(14).x,r.At(14).y);Handles.color=Color.magenta;Handles.DrawLine(foot,foot+Vector3.up*18,4);Handles.DrawLine(foot+Vector3.up*18,WorldBuilder.GroundPoint(foot.x,foot.z+28),4);
            }
            foreach(var p in points) {
                int count=points.Count(other=>Vector2.Distance(ExpansionCatalog.XZ(p),ExpansionCatalog.XZ(other))<=radius);
                Handles.color=count>=8?new Color(.2f,.85f,.35f,.35f):new Color(1,.75f,.1f,.35f);Handles.DrawWireDisc(p+Vector3.up,Vector3.up,radius);Handles.Label(p,count.ToString());
            }
            if(!showDeadZones)return;
            for(float x=-360;x<340;x+=25)for(float z=-275;z<415;z+=25) {
                var p=new Vector2(x,z);if(!ExpansionCatalog.Playable(p)||points.Any(other=>Vector2.Distance(p,ExpansionCatalog.XZ(other))<=100))continue;
                Handles.color=new Color(1,.15f,.1f,.25f);Handles.DrawSolidDisc(WorldBuilder.GroundPoint(x,z,.2f),Vector3.up,12);
            }
        }
    }
}
