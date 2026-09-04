using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;

namespace WanderingCity
{
    public sealed class PlayableNavigation : MonoBehaviour
    {
        NavMeshData data; NavMeshDataInstance instance; Mesh ground;
        public double BuildMilliseconds { get; private set; }
        public int TriangleCount { get; private set; }
        public void Build()
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            // Remove the old, small scene surface. One connected mesh covers regions and connecting roads.
            foreach(var surface in FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None)) {surface.RemoveData();surface.enabled=false;}
            var vertices=new List<Vector3>();var triangles=new List<int>(); const int step=3;
            var bounds=ExpansionCatalog.PlayableBounds;
            for(float z=bounds.min.z;z<bounds.max.z;z+=step)for(float x=bounds.min.x;x<bounds.max.x;x+=step)
            {
                if(!ExpansionCatalog.Playable(new Vector2(x+step*.5f,z+step*.5f)))continue;
                int first=vertices.Count;
                vertices.Add(WorldBuilder.GroundPoint(x,z));vertices.Add(WorldBuilder.GroundPoint(x,z+step));
                vertices.Add(WorldBuilder.GroundPoint(x+step,z+step));vertices.Add(WorldBuilder.GroundPoint(x+step,z));
                triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);
            }
            ground=new Mesh{name="Playable terrain only",indexFormat=IndexFormat.UInt32};ground.SetVertices(vertices);ground.SetTriangles(triangles,0);ground.RecalculateBounds();TriangleCount=triangles.Count/3;
            Physics.SyncTransforms();
            var sources=new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds,1<<0,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);
            sources.RemoveAll(s=>s.shape==NavMeshBuildSourceShape.Terrain || !ExpansionCatalog.Playable(ExpansionCatalog.XZ(s.transform.MultiplyPoint3x4(Vector3.zero)),12));
            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=ground,transform=Matrix4x4.identity,area=0});
            var settings=NavMesh.GetSettingsByID(0);settings.overrideVoxelSize=true;settings.voxelSize=.25f;
            data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
            instance=NavMesh.AddNavMeshData(data);clock.Stop();BuildMilliseconds=clock.Elapsed.TotalMilliseconds;
        }
        void OnDestroy(){if(instance.valid)instance.Remove();if(data!=null)Destroy(data);if(ground!=null)Destroy(ground);}
    }
}
