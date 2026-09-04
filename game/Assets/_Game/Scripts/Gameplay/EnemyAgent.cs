using UnityEngine;
using UnityEngine.AI;

namespace WanderingCity
{
    public enum EnemyAction { Patrol, Chase, Attack, Return, Dead }
    public sealed class EnemyAgent : MonoBehaviour
    {
        public bool Elite;
        public int MaxHp => Mathf.RoundToInt(Session.Balance.enemyHealth * (Elite ? 2.5f : 1));
        public GameSession Session; public string Id; public int Hp = 104;
        public EnemyAction Action { get; private set; }
        public NavMeshAgent Agent;
        public Transform Body, Telegraph;
        public Vector3 Home;
        float phase, cooldown, repath, flash;
        bool struck;
        Material material;
        public void Initialize() { Home = transform.position; material = Body.GetComponent<Renderer>().material; ResetEncounter(); }
        public void ResetEncounter() { Hp = MaxHp; Action = EnemyAction.Patrol; phase = cooldown = 0; if (gameObject.activeInHierarchy && Agent.isOnNavMesh) { Agent.Warp(Home); Agent.isStopped = false; Agent.ResetPath(); } }
        void Update()
        {
            if (!Session.Started || Session.Paused || Action == EnemyAction.Dead) return;
            if (!Agent.isOnNavMesh) return;
            if ((Session.Player.transform.position-transform.position).sqrMagnitude > 90*90) { if(!Agent.isStopped) Agent.isStopped=true; return; }
            if(Agent.isStopped && Action!=EnemyAction.Attack) Agent.isStopped=false;
            cooldown -= Time.deltaTime; phase += Time.deltaTime; repath -= Time.deltaTime; flash -= Time.deltaTime;
            var player = Session.Player; float distance = Vector3.Distance(transform.position, player.transform.position);
            bool sees = distance < 15 && CombatVisibility.Clear(transform.position + Vector3.up, player.transform.position + Vector3.up);
            bool reachable = !Agent.pathPending && Agent.pathStatus == NavMeshPathStatus.PathComplete;
            material.color = flash > 0 ? Color.white : Action == EnemyAction.Attack ? new Color(1, .35f, .12f) : new Color(.28f, .33f, .42f);
            Telegraph.gameObject.SetActive(Action == EnemyAction.Attack);
            if (Action == EnemyAction.Attack)
            {
                Body.localRotation = Quaternion.Euler(Mathf.Sin(phase * 8) * 15, 0, 0);
                if (!struck && phase >= .8f) { struck = true; if (distance < 2.5f && reachable && CombatVisibility.Clear(transform.position + Vector3.up, player.transform.position + Vector3.up)) player.Damage(Mathf.RoundToInt(Session.Balance.enemyDamage * (Elite ? 1.5f : 1))); }
                if (phase > 1.3f) { Action = EnemyAction.Chase; Agent.isStopped = false; cooldown = 1; Body.localRotation = Quaternion.identity; } return;
            }
            if (player.Action == PlayerAction.Dead || Vector3.Distance(Home, player.transform.position) > 23 || (Action == EnemyAction.Chase && (!sees && distance > 18))) Action = EnemyAction.Return;
            else if (sees && Action != EnemyAction.Return) Action = EnemyAction.Chase;
            if (Action == EnemyAction.Chase)
            {
                if (repath <= 0) { Agent.SetDestination(player.transform.position); repath = .25f; }
                if (distance < 2.15f && sees && reachable && cooldown <= 0) { Action = EnemyAction.Attack; phase = 0; struck = false; Agent.isStopped = true; transform.LookAt(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z)); }
                else if (!Agent.pathPending && Agent.pathStatus != NavMeshPathStatus.PathComplete) Action = EnemyAction.Return;
            }
            else if (Action == EnemyAction.Return) { Agent.SetDestination(Home); if (Vector3.Distance(transform.position, Home) < 1) { Action = EnemyAction.Patrol; phase = 0; } }
            else if (repath <= 0) { Vector3 point = Home + new Vector3(Mathf.Sin(phase * .3f), 0, Mathf.Cos(phase * .3f)) * 3; if (NavMesh.SamplePosition(point, out var nav, 3, NavMesh.AllAreas)) Agent.SetDestination(nav.position); repath = 2; }
        }
        public void Damage(int amount)
        {
            if (Action == EnemyAction.Dead || amount <= 0) return;
            Hp = Mathf.Max(0, Hp - amount); flash = .15f; Session.Tone(1.7f);
            WorldBuilder.Pulse(transform.position + Vector3.up, new Color(1, .8f, .35f));
            if (Hp == 0) { Action = EnemyAction.Dead; Session.State.defeated.Add(Id); Session.World.SpawnDrop(Id, Home); gameObject.SetActive(false); Session.World.RefreshRewards(); Session.Save(); }
            else if (Action != EnemyAction.Attack) Action = EnemyAction.Chase;
        }
    }
}
