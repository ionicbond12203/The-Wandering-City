using System.Collections.Generic;
using UnityEngine;

namespace WanderingCity
{
    public sealed class WorldInteractable : MonoBehaviour
    {
        public GameSession Session; public string Id, Label; public bool Workbench;
        public Dictionary<string, int> Reward;
        public bool Available => Workbench || !Session.State.claimed.Contains(Id);
        public void Interact()
        {
            if (!Available || Vector3.Distance(transform.position, Session.Player.transform.position + Vector3.up) > 3.6f || !CombatVisibility.Clear(Session.Player.transform.position + Vector3.up, transform.position)) return;
            if (Workbench) { Session.SetMenu(true, "craft"); return; }
            Session.Result(Rules.Claim(Session.State, Id, Reward), "获得 / " + Label, Id == "camp-reward" ? "先击败营地内全部 5 名守卫，或清理背包空间" : "背包空间不足或奖励已领取");
        }
    }
}
