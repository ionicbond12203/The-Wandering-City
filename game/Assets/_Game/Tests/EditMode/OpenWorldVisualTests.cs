using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WanderingCity.Editor;

namespace WanderingCity.Tests
{
    public class OpenWorldVisualTests
    {
        [OneTimeSetUp] public void Prepare() => ProjectBuilder.Prepare();
        [Test] public void GrassHasFourInstancedPrototypesAndExcludesInteractionAreas()
        {
            var data=AssetDatabase.LoadAssetAtPath<TerrainData>(EnvironmentAuthoring.TerrainDataPath);
            Assert.AreEqual(4,data.detailPrototypes.Length);
            Assert.AreEqual(DetailScatterMode.InstanceCountMode,data.detailScatterMode);
            foreach(var detail in data.detailPrototypes) {Assert.IsTrue(detail.useInstancing);Assert.IsNotNull(detail.prototype);}
            Assert.IsFalse(OpenWorldAuthoring.GrassAllowed(-7,1));
            Assert.IsFalse(OpenWorldAuthoring.GrassAllowed(51,-8));
            Assert.IsFalse(OpenWorldAuthoring.GrassAllowed(-48,65));
            var map=data.GetDetailLayer(0,0,data.detailWidth,data.detailHeight,0);
            long total=0; foreach(int cell in map) total+=cell;
            Assert.Greater(total,50000,"Real near-field coverage, not a handful of tufts");
        }
        [Test] public void FormalEnvironmentAndTravelerHaveNoBuiltinPrimitiveRenderMeshes()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/World.unity",OpenSceneMode.Single);
            foreach(var root in new[]{GameObject.Find("Environment"),Resources.Load<GameObject>("Traveler_Stylized")})
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                Assert.IsNotNull(filter.sharedMesh,filter.name);
                Assert.AreNotEqual("Library/unity default resources",AssetDatabase.GetAssetPath(filter.sharedMesh),filter.name);
            }
            var horizon=GameObject.Find("Environment/DistantLandscape");
            Assert.Greater(horizon.transform.childCount,4);
            Assert.IsEmpty(horizon.GetComponentsInChildren<Collider>());
        }
        [Test] public void ThreeTreeRockAndCliffVariantsHaveLod()
        {
            foreach(string kind in new[]{"Tree_Stylized","Rock","Cliff"})
            foreach(string variant in new[]{"A","B","C"})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentAuthoring.PrefabsDir+"/"+kind+"_"+variant+".prefab");
                Assert.IsNotNull(prefab);
                Assert.GreaterOrEqual(prefab.GetComponent<LODGroup>().GetLODs().Length,2);
            }
        }
        [Test] public void MigratedColumnsKeepTheirOriginalHeight()
        {
            var column=AssetDatabase.LoadAssetAtPath<Mesh>(EnvironmentAuthoring.ArtDir+"/Meshes/CarvedCylinderFullHeight.asset");
            Assert.AreEqual(2,column.bounds.size.y,.001f);
        }
        [Test] public void SkyUsesOriginalShaderAndCompiles()
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(EnvironmentAuthoring.MaterialsDir+"/OutdoorSky.mat");
            Assert.AreEqual("WanderingCity/StylizedSky",mat.shader.name);
            Assert.IsFalse(ShaderUtil.ShaderHasError(mat.shader));
        }
    }
}
