using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WanderingCity
{
    public enum PlayerAction { Move, Attack, Dodge, Hurt, Dead }
    public sealed class PlayerMotor : MonoBehaviour
    {
        public GameSession Session;
        public CharacterController Controller;
        public Transform Visual, Blade;
        public Animator Animator;
        public PlayerTraversal Traversal;
        public Transform GlideSail;
        public CharacterVisualAdapter VisualAdapter;
        public PlayerAction Action { get; private set; }
        public bool CanAct => Action == PlayerAction.Move && (Traversal == null || !Traversal.BlocksCombat);
        public bool Invulnerable => Action == PlayerAction.Dodge && actionTime < Session.Balance.invulnerability;
        public float AttackAge => actionTime;
        public int AttackIndex { get; private set; } = 1;
        bool comboQueued;
        public bool ComboWindow => Action == PlayerAction.Attack && actionTime >= Session.Balance.attackHitEnd && actionTime < Session.Balance.attackRecoveryEnd;
        float actionTime, dodgeCooldown, deathTime;
        Vector3 dodgeDirection;
        readonly HashSet<EnemyAgent> hit = new HashSet<EnemyAgent>();
        readonly Collider[] overlaps = new Collider[24];
        public void RestorePosition()
        {
            Session.State.y = Mathf.Max(Session.State.y, WorldBuilder.GroundY(Session.State.x, Session.State.z) + .1f);
            Controller.enabled = false; transform.SetPositionAndRotation(new Vector3(Session.State.x, Session.State.y, Session.State.z), Quaternion.Euler(0, Session.State.yaw, 0)); Controller.enabled = true;
            // A valid numeric position can still be inside a newly placed structure.
            if (Physics.CheckCapsule(transform.position + Vector3.up * .5f, transform.position + Vector3.up * 1.5f, .32f, 1 << 0, QueryTriggerInteraction.Ignore)) { Controller.enabled = false; transform.position = WorldBuilder.GroundPoint(0, 0, .15f); Controller.enabled = true; }
            Action = PlayerAction.Move; actionTime = dodgeCooldown = 0; hit.Clear();
            if (Blade != null) Blade.localRotation = Quaternion.identity;
            Traversal?.ResetMotion(true);
            if (GlideSail != null) GlideSail.gameObject.SetActive(false);
            comboQueued = false; AttackIndex = 1; VisualAdapter?.Driver?.ResetPresentation();
            if (Animator != null && Animator.runtimeAnimatorController != null) { Animator.Rebind(); Animator.Update(0); }
        }
        void Update()
        {
            if (!Session.InputReady) return;
            float dt = Time.deltaTime;
            AdvanceAction(dt);
            if (Action == PlayerAction.Dead) { if (Time.time >= deathTime) Session.Respawn(); return; }
            var kb = Keyboard.current; var mouse = Mouse.current; if (kb == null || mouse == null) return;
            Vector2 input = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0)).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            Vector3 direction = forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x;
            if (!Session.Building)
            {
                if (mouse.leftButton.wasPressedThisFrame) StartAttack();
                else if (mouse.rightButton.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame) StartDodge(direction);
            }
            Traversal.Simulate(dt, direction, input, kb.leftShiftKey.isPressed, kb.leftAltKey.isPressed, kb.spaceKey.wasPressedThisFrame, kb.cKey.wasPressedThisFrame, kb.gKey.wasPressedThisFrame, kb.xKey.wasPressedThisFrame, Action == PlayerAction.Dodge ? dodgeDirection * Session.Balance.dodgeSpeed : (Vector3?)null);
            if (transform.position.y < -8) Session.Respawn();
        }
        public void AdvanceAction(float dt)
        {
            if (dt <= 0 || !Session.InputReady || Action == PlayerAction.Dead) return;
            float before = actionTime; actionTime += dt; dodgeCooldown -= dt;
            if (Action == PlayerAction.Attack)
            {
                if (actionTime >= Session.Balance.attackWindup && before <= Session.Balance.attackHitEnd) Strike();
                if (actionTime >= Session.Balance.attackRecoveryEnd)
                {
                    if (comboQueued) { AttackIndex = AttackIndex % 3 + 1; actionTime = 0; hit.Clear(); comboQueued = false; }
                    else Action = PlayerAction.Move;
                }
            }
            if (Action == PlayerAction.Dodge && actionTime >= Session.Balance.dodgeDuration) Action = PlayerAction.Move;
            if (Action == PlayerAction.Hurt && actionTime >= .22f) Action = PlayerAction.Move;
        }
        public bool StartAttack()
        {
            if (!Session.Started || Session.Paused || Session.State.hp <= 0) return false;
            if (ComboWindow && AttackIndex < 3) { comboQueued = true; return true; }
            if (!CanAct) return false;
            Action = PlayerAction.Attack; AttackIndex = 1; actionTime = 0; comboQueued = false; hit.Clear(); Session.Audio?.Play(WorldSound.Attack); return true;
        }
        public bool StartDodge(Vector3 direction)
        {
            // Only recovery can be cancelled; windup and active-hit frames remain committed.
            if (!Session.Started || Session.Paused || (!CanAct && !ComboWindow) || dodgeCooldown > 0 || Session.State.hp <= 0) return false;
            Action = PlayerAction.Dodge; comboQueued = false; actionTime = 0; dodgeCooldown = Session.Balance.dodgeCooldown;
            dodgeDirection = direction.sqrMagnitude > .1f ? direction.normalized : transform.forward; Session.Audio?.Play(WorldSound.Traversal); return true;
        }
        public void Strike()
        {
            if (Action != PlayerAction.Attack || Session.State.hp <= 0 || Session.Paused) return;
            Vector3 origin = transform.position + Vector3.up;
            int count = Physics.OverlapSphereNonAlloc(origin + transform.forward * 1.1f, 1.6f, overlaps, 1 << 9, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) { var enemy = overlaps[i].GetComponentInParent<EnemyAgent>(); if (enemy == null || hit.Contains(enemy) || !CombatVisibility.Clear(origin, enemy.transform.position + Vector3.up)) continue;
                hit.Add(enemy); enemy.Damage(Session.State.weaponLevel == 1 ? Session.Balance.normalDamage : Session.Balance.upgradedDamage); }
        }
        public bool Damage(int amount)
        {
            if (amount <= 0 || Action == PlayerAction.Dead || Invulnerable || Session.State.hp <= 0) return false;
            Session.State.hp = Mathf.Max(0, Session.State.hp - amount); Session.Hud.Flash(); Session.Audio?.Play(WorldSound.Hit);
            comboQueued = false; Action = Session.State.hp == 0 ? PlayerAction.Dead : PlayerAction.Hurt; actionTime = 0; Blade.localRotation = Quaternion.identity;
            Traversal?.Interrupt();
            if (GlideSail != null) GlideSail.gameObject.SetActive(false);
            VisualAdapter?.Driver?.Tick(.001f);
            if (Action == PlayerAction.Dead) { deathTime = Time.time + 1.5f; Session.Notify("旅途未完 · 正在返回据点……"); }
            return true;
        }
    }
    public static class CombatVisibility
    {
        public static bool Clear(Vector3 from, Vector3 to) => !Physics.Linecast(from, to, 1 << 0, QueryTriggerInteraction.Ignore);
    }
}
