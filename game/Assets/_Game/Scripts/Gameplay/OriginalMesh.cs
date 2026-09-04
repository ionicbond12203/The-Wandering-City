using System.Collections.Generic;
using UnityEngine;

namespace WanderingCity
{
    /// <summary>Original faceted ring lofts; no built-in primitive mesh dependencies.</summary>
    public static class OriginalMesh
    {
        public static Mesh Loft(string name, int sides, float[] radii, float bend, int seed, float irregular = .15f)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>();
            var colors = new List<Color>(); var indices = new List<int>();
            var rings = new Vector3[radii.Length, sides];
            for (int r=0; r<radii.Length; r++)
            for (int i=0; i<sides; i++)
            {
                float t = r / (float)(radii.Length-1), a = i * Mathf.PI * 2 / sides;
                float variation = 1 + irregular * Mathf.Sin(i*7.13f + seed + r*.8f);
                rings[r,i] = new Vector3(Mathf.Cos(a)*radii[r]*variation + bend*t*t, t-.5f, Mathf.Sin(a)*radii[r]*variation);
            }
            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int n = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                foreach (var p in new[] {a,b,c}) { uv.Add(new Vector2(p.x+.5f,p.y+.5f)); colors.Add(Color.white * (.90f + .1f * Mathf.Sin(p.x*3+seed))); }
                indices.Add(n); indices.Add(n+1); indices.Add(n+2);
            }
            for (int r=0;r<radii.Length-1;r++)
            for (int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;
                Triangle(rings[r,i],rings[r+1,i],rings[r+1,j]);
                Triangle(rings[r,i],rings[r+1,j],rings[r,j]);
            }
            for(int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;
                Triangle(new Vector3(0,-.5f,0),rings[0,i],rings[0,j]);
                Triangle(new Vector3(bend,.5f,0),rings[radii.Length-1,j],rings[radii.Length-1,i]);
            }
            var mesh = new Mesh {name=name}; mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetColors(colors); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
        public static Mesh Rock(int seed=1, int sides=9) => Loft("Weathered rock " + seed,sides,new[]{.28f,.53f,.46f,.24f},.12f,seed,.27f);
        public static Mesh Canopy(int seed, int sides=9) => Loft("Wind-shaped crown " + seed,sides,new[]{.12f,.58f,.48f,.08f},.22f,seed,.32f);
        public static Mesh Mountain(int seed)
        {
            const int resolution=21;
            var v=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            for(int z=0;z<resolution;z++) for(int x=0;x<resolution;x++)
            {
                float u=x/(float)(resolution-1),w=z/(float)(resolution-1);
                float radius=Mathf.Max(0,1-4*(u-.5f)*(u-.5f)-4*(w-.5f)*(w-.5f));
                float h=Mathf.Pow(radius,1.7f)*(.72f+.28f*Mathf.PerlinNoise(u*6+seed,w*6+seed));
                v.Add(new Vector3(u-.5f,h,w-.5f));uv.Add(new Vector2(u,w));
                if(x==resolution-1||z==resolution-1)continue;
                int i=z*resolution+x;
                indices.Add(i);indices.Add(i+resolution);indices.Add(i+1);
                indices.Add(i+1);indices.Add(i+resolution);indices.Add(i+resolution+1);
            }
            var mesh=new Mesh{name="Eroded horizon "+seed};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static Mesh Grass(int variant)
        {
            var v=new List<Vector3>(); var uv=new List<Vector2>(); var tr=new List<int>();
            for(int i=0;i<7;i++)
            {
                float a=i*2.4f, h=.35f+(i%3)*.13f+variant*.12f;
                Vector3 p=new Vector3(Mathf.Cos(a)*.27f,0,Mathf.Sin(a)*.27f), side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.06f;
                int n=v.Count; v.Add(p-side);v.Add(p+side);v.Add(p+Vector3.up*h+side*.8f);
                uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(.5f,1)); tr.Add(n);tr.Add(n+1);tr.Add(n+2);
            }
            var m=new Mesh{name="Meadow blades " + variant};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
}
