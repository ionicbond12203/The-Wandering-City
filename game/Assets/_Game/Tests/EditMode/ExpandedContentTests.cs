using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WanderingCity.Tests
{
    public sealed class ExpandedContentTests
    {
        [Test] public void FiveSeparatedRegionsHaveMatureContentAndStableIds()
        {
            Assert.AreEqual(5,ExpansionCatalog.Regions.Length);
            var ids=ExpansionCatalog.PoiIds.Concat(ExpansionCatalog.ResourceIds).Concat(ExpansionCatalog.EnemyIds).ToArray();
            Assert.AreEqual(ids.Length,ids.Distinct().Count());
            foreach(var r in ExpansionCatalog.Regions) {
                Assert.AreEqual(15,r.Sites.Length);
                Assert.AreEqual(4,ExplorationCatalog.TreasureIds.Count(id=>id.StartsWith(r.Key(""))));
                Assert.Contains(r.Key("beacon"),ExplorationCatalog.TeleportIds.ToArray());
                Assert.Contains(r.Key("puzzle"),ExplorationCatalog.PuzzleIds.ToArray());
                foreach(var other in ExpansionCatalog.Regions.Where(other=>other!=r))Assert.Greater(Vector2.Distance(r.Center,other.Center),200);
            }
            var points=ExpansionCatalog.Regions.SelectMany(r=>r.Sites.Select(p=>r.Center+p)).ToArray();
            float diameter=points.Max(a=>points.Max(b=>Vector2.Distance(a,b)));
            Assert.That(diameter,Is.InRange(600,800));
            Assert.Contains("mesa-beacon",ExplorationCatalog.PoiIds.ToArray());Assert.Contains("enemy-camp-0",WorldCatalog.EnemyIds.ToArray());
        }
        [Test] public void DistantDecorationIsExcludedAndRoadsRemainPlayable()
        {
            Assert.IsFalse(ExpansionCatalog.Playable(new Vector2(450,500)));
            foreach(var r in ExpansionCatalog.Regions)for(int i=1;i<r.Approach.Length;i++)
                for(int j=0;j<=20;j++)Assert.IsTrue(ExpansionCatalog.Playable(ExpansionCatalog.XZ(Vector3.Lerp(r.Approach[i-1],r.Approach[i],j/20f))));
            int covered=0,total=0;
            for(float x=-512;x<512;x+=8)for(float z=-420;z<604;z+=8){total++;if(ExpansionCatalog.Playable(new Vector2(x,z)))covered++;}
            Assert.Less((float)covered/total,.4f,"Decorative terrain should not enter navigation");
        }
        [Test] public void NewRewardsAreGatedAndLegacyV2StillValid()
        {
            var state=new GameState();Assert.IsTrue(SaveStore.Validate(state));
            foreach(var r in ExpansionCatalog.Regions) {
                Assert.IsFalse(ExpansionCatalog.RewardUnlocked(state,r.Key("puzzle-cache")));
                Assert.IsFalse(ExpansionCatalog.RewardUnlocked(state,r.Key("elite-cache")));
                state.completedPuzzleIds.Add(r.Key("puzzle"));state.defeated.Add(r.Key("warden"));
                Assert.IsTrue(ExpansionCatalog.RewardUnlocked(state,r.Key("puzzle-cache")));
                Assert.IsTrue(ExpansionCatalog.RewardUnlocked(state,r.Key("elite-cache")));
                ExplorationRules.Activate(state,r.Key("beacon"));
                state.x=r.Center.x;state.z=r.Center.y;state.y=r.Elevation+20;
                SaveStore.SafePosition(state);Assert.AreEqual(r.Center.x,state.x);Assert.AreEqual(r.Center.y,state.z);
            }
            Assert.IsTrue(SaveStore.Validate(state));
        }
        [Test] public void TerrainOccludesSomeMajorLandmarksFromBothOutlooks()
        {
            foreach(var region in new[]{ExpansionCatalog.Regions[0],ExpansionCatalog.Regions[3]}) {
                var foot=region.At(14);var eye=new Vector3(foot.x,TerrainHeightModel.Sample(foot.x,foot.y)+20,foot.y);int blocked=0;
                foreach(var target in ExpansionCatalog.Regions.Where(r=>r!=region)) {
                    var p=target.At(1);var end=new Vector3(p.x,TerrainHeightModel.Sample(p.x,p.y)+17,p.y);bool occluded=false;
                    for(int i=1;i<200;i++) {var sample=Vector3.Lerp(eye,end,i/200f);if(TerrainHeightModel.Sample(sample.x,sample.z)>sample.y){occluded=true;break;}}
                    if(occluded)blocked++;
                }
                Assert.Greater(blocked,0,region.Name+" must not reveal every major landmark");
            }
        }
    }
}
