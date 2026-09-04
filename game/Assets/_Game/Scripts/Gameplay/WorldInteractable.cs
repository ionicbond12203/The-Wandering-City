using System.Collections.Generic;
using UnityEngine;

namespace WanderingCity
{
    public sealed class WorldInteractable : MonoBehaviour
    {
        public GameSession Session; public string Id, Label; public bool Workbench;
        public Dictionary<string, int> Reward;
        public ExplorationPoi Exploration;
        public PuzzleController Puzzle;
        public bool Available => Exploration != null ? Exploration.Type != PoiType.Treasure || !Session.State.openedTreasureIds.Contains(Id) : Puzzle != null ? !Session.State.completedPuzzleIds.Contains("echo-puzzle") : Workbench || !Session.State.claimed.Contains(Id);
        public void Interact()
        {
            if (!Session.Started || Session.Paused || !Session.Player.CanAct || Session.State.hp <= 0 || !Available || Vector3.Distance(transform.position, Session.Player.transform.position + Vector3.up) > 3.6f || !CombatVisibility.Clear(Session.Player.transform.position + Vector3.up, transform.position)) return;
            if (Exploration != null)
            {
                if (Exploration.Type == PoiType.TeleportPoint) Session.Result(ExplorationRules.Activate(Session.State, Id), "信标已激活 / 在地图中选择传送", "信标已激活");
                else Session.Result(ExplorationRules.OpenTreasure(Session.State, Id, Reward), "获得 / " + Label, "宝箱未解锁、已领取或背包空间不足");
                Exploration.Refresh(); return;
            }
            if (Puzzle != null) { Puzzle.Activate(Id); return; }
            if (Workbench) { Session.SetMenu(true, "craft"); return; }
            Session.Result(Rules.Claim(Session.State, Id, Reward), "获得 / " + Label, Id == "camp-reward" ? "先击败营地内全部 5 名守卫，或清理背包空间" : "背包空间不足或奖励已领取");
        }
    }
}
