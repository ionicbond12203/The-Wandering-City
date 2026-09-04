using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WanderingCity.Editor
{
    public static class ExpansionAuthoring
    {
        public static void Populate(GameObject environment,Dictionary<string,Material> materials)
        {
            if(environment.transform.Find("ExpandedRegions_v1")!=null)return;
            var root=new GameObject("ExpandedRegions_v1").transform;root.SetParent(environment.transform);
            foreach(var region in ExpansionCatalog.Regions)
            {
                var group=new GameObject(region.Name).transform;group.SetParent(root);var random=new System.Random(1900+System.Array.IndexOf(ExpansionCatalog.Regions,region));
                for(int i=0;i<100;i++)
                {
                    var p=region.Center+new Vector2((float)random.NextDouble()*180-90,(float)random.NextDouble()*180-90);
                    bool nearSite=false;foreach(var site in region.Sites)nearSite|=Vector2.Distance(p,region.Center+site)<8;
                    var foot=region.At(14);nearSite|=p.x>foot.x-58&&p.x<foot.x+12&&Mathf.Abs(p.y-foot.y)<12;
                    if(nearSite || ExpansionCatalog.RoadDistance(p)<8)continue;
                    bool tree=region.Id=="west-forest" || region.Id=="south-meadow"&&i%3==0;
                    string kind=tree?"Tree_Stylized":"Rock";string suffix=((char)('A'+i%3)).ToString();
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentAuthoring.PrefabsDir+"/"+kind+"_"+suffix+".prefab");
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,group);go.transform.position=WorldBuilder.GroundPoint(p.x,p.y);
                    go.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
                    float scale=tree?1.2f+(float)random.NextDouble()*.8f:.8f+(float)random.NextDouble()*1.5f;
                    go.transform.localScale=Vector3.one*scale;
                }
            }
        }
    }
}
