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
        float actionTime, dodgeCooldown, deathTime;
        Vector3 velocity, dodgeDirection;
        readonly HashSet<EnemyAgent> hit = new HashSet<EnemyAgent>();
        readonly Collider[] overlaps = new Collider[24];
        public void RestorePosition()
        {
            Session.State.y = Mathf.Max(Session.State.y, WorldBuilder.GroundY(Session.State.x, Session.State.z) + .1f);
            Controller.enabled = false; transform.SetPositionAndRotation(new Vector3(Session.State.x, Session.State.y, Session.State.z), Quaternion.Euler(0, Session.State.yaw, 0)); Controller.enabled = true;
            // A valid numeric position can still be inside a newly placed structure.
            if (Physics.CheckCapsule(transform.position + Vector3.up * .5f, transform.position + Vector3.up * 1.5f, .32f, 1 << 0, QueryTriggerInteraction.Ignore)) { Controller.enabled = false; transform.position = WorldBuilder.GroundPoint(0, 0, .15f); Controller.enabled = true; }
            Action = PlayerAction.Move; actionTime = dodgeCooldown = 0; velocity = Vector3.zero; hit.Clear();
            if (Blade != null) Blade.localRotation = Quaternion.identity;
            Traversal?.ResetMotion(true);
            if (GlideSail != null) GlideSail.gameObject.SetActive(false);
            if (Animator != null) { Animator.SetBool("Dead", false); Animator.SetInteger("Action", 0); }
        }
        void Update()
        {
            if (!Session.InputReady) return;
            float dt = Time.deltaTime; actionTime += dt; dodgeCooldown -= dt;
            if (Action == PlayerAction.Dead) { if (Time.time >= deathTime) Session.Respawn(); return; }
            var kb = Keyboard.current; var mouse = Mouse.current; if (kb == null || mouse == null) return;
            Vector2 input = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0)).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            Vector3 direction = forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x;
            if (CanAct && !Session.Building)
            {
                if (mouse.leftButton.wasPressedThisFrame) StartAttack();
                else if (mouse.rightButton.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame) StartDodge(direction);
            }
            if (Action == PlayerAction.Attack)
            {
                Blade.localRotation = Quaternion.Euler(0, Mathf.Lerp(-90, 105, Mathf.Clamp01((actionTime - .12f) / .22f)), -35);
                if (actionTime >= Session.Balance.attackWindup && actionTime <= Session.Balance.attackHitEnd) Strike();
                if (actionTime >= Session.Balance.attackRecoveryEnd) { Action = PlayerAction.Move; Blade.localRotation = Quaternion.identity; }
            }
            if (Action == PlayerAction.Dodge && actionTime >= Session.Balance.dodgeDuration) Action = PlayerAction.Move;
            if (Action == PlayerAction.Hurt && actionTime >= .22f) Action = PlayerAction.Move;
            Traversal.Simulate(dt, direction, input, kb.leftShiftKey.isPressed, kb.leftAltKey.isPressed, kb.spaceKey.wasPressedThisFrame, kb.cKey.wasPressedThisFrame, kb.gKey.wasPressedThisFrame, kb.xKey.wasPressedThisFrame, Action == PlayerAction.Dodge ? dodgeDirection * Session.Balance.dodgeSpeed : (Vector3?)null);
            Visual.localPosition = new Vector3(0, CanAct && input.sqrMagnitude > 0 ? Mathf.Sin(Time.time * Traversal.Speed * 2) * .045f : 0, 0);
            velocity = transform.forward * Traversal.Speed;
            if (Animator != null && Animator.runtimeAnimatorController != null) { Animator.SetFloat("Speed", velocity.magnitude); Animator.SetInteger("Action", (int)Action); }
            if (Animator != null && Animator.runtimeAnimatorController != null)
            {
                Animator.SetFloat("VerticalVelocity", Traversal.VerticalVelocity); Animator.SetInteger("Traversal", (int)Traversal.State);
                Animator.SetBool("Grounded", Traversal.Grounded); Animator.SetBool("Climbing", Traversal.State == TraversalState.Climb);
                Animator.SetBool("Gliding", Traversal.State == TraversalState.Glide); Animator.SetBool("Attack", Action == PlayerAction.Attack);
                Animator.SetBool("Dodge", Action == PlayerAction.Dodge); Animator.SetBool("Dead", Action == PlayerAction.Dead);
            }
            if (GlideSail != null) GlideSail.gameObject.SetActive(Traversal.State == TraversalState.Glide);
            if (transform.position.y < -8) Session.Respawn();
        }
        public bool StartAttack()
        {
            if (!Session.Started || Session.Paused || !CanAct || Session.State.hp <= 0) return false; Action = PlayerAction.Attack; actionTime = 0; hit.Clear(); Session.Tone(.8f); return true;
        }
        public bool StartDodge(Vector3 direction)
        {
            if (!Session.Started || Session.Paused || !CanAct || dodgeCooldown > 0 || Session.State.hp <= 0) return false;
            Action = PlayerAction.Dodge; actionTime = 0; dodgeCooldown = Session.Balance.dodgeCooldown; dodgeDirection = direction.sqrMagnitude > .1f ? direction.normalized : transform.forward; Session.Tone(.7f); return true;
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
            Session.State.hp = Mathf.Max(0, Session.State.hp - amount); Session.Hud.Flash(); Session.Tone(.45f);
            Action = Session.State.hp == 0 ? PlayerAction.Dead : PlayerAction.Hurt; actionTime = 0; Blade.localRotation = Quaternion.identity;
            Traversal?.Interrupt();
            if (GlideSail != null) GlideSail.gameObject.SetActive(false);
            if (Animator != null) { Animator.SetBool("Dead", Action == PlayerAction.Dead); Animator.SetInteger("Action", (int)Action); }
            if (Action == PlayerAction.Dead) { deathTime = Time.time + 1.5f; Session.Notify("旅途未完 · 正在返回据点……"); }
            return true;
        }
    }
    public static class CombatVisibility
    {
        public static bool Clear(Vector3 from, Vector3 to) => !Physics.Linecast(from, to, 1 << 0, QueryTriggerInteraction.Ignore);
    }
}
