using UnityEngine;
using System.Linq;

namespace WanderingCity
{
    public sealed class ExplorationPoi : MonoBehaviour
    {
        public string Id, DisplayName; public PoiType Type; public float Radius; public Vector3 SpawnPoint;
        public GameSession Session; public GameObject RewardVisual;
        public bool Completed => Type == PoiType.TeleportPoint ? Session.State.activatedTeleportIds.Contains(Id) : Type == PoiType.Treasure ? Session.State.openedTreasureIds.Contains(Id) : Type == PoiType.Puzzle ? Session.State.completedPuzzleIds.Contains(Id) : Type == PoiType.EnemyCamp && ExpansionCatalog.EnemiesFor(Id).Length > 0 && ExpansionCatalog.EnemiesFor(Id).All(Session.State.defeated.Contains);
        public bool Discover()
        {
            if (!Session.Started || Session.State.hp <= 0 || !ExplorationRules.Discover(Session.State, Id)) return false;
            Session.Notify("发现 / " + DisplayName); Session.Save(); return true;
        }
        void OnTriggerEnter(Collider other) { if (other.GetComponent<PlayerMotor>() != null) Discover(); }
        public void Refresh() { if (RewardVisual != null) RewardVisual.SetActive(!Session.State.openedTreasureIds.Contains(Id)); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, Radius); }
#endif
    }
}
