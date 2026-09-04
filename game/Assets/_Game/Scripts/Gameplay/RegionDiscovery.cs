using UnityEngine;

namespace WanderingCity
{
    public sealed class RegionDiscovery : MonoBehaviour
    {
        public GameSession Session; public string Id, DisplayName;
        public Bounds Bounds;
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerMotor>() == null || !Session.Started) return;
            if (ExplorationRules.AddOnce(Session.State.discoveredRegionIds, Id, ExplorationCatalog.RegionIds)) { Session.Notify("踏入新区域 / " + DisplayName); Session.Save(); }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnDrawGizmosSelected() { var box = GetComponent<BoxCollider>(); if (box != null) { Gizmos.color = Color.cyan; Gizmos.DrawWireCube(transform.position, box.size); } }
#endif
    }
}
