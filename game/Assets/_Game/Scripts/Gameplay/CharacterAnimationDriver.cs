using UnityEngine;

namespace WanderingCity
{
    // Presentation consumes motor facts. No animation event or clip duration can authorize damage or movement.
    [DefaultExecutionOrder(20)]
    public sealed class CharacterAnimationDriver : MonoBehaviour
    {
        public PlayerMotor Motor;
        public CharacterRigBindings Rig;
        public int Pose { get; private set; }
        public float LandingWeight { get; private set; }
        float landingTime, previousFall, yaw, turn, phase;
        Vector3 previousPosition;
        Quaternion[] secondaryRest;
        CharacterRigBindings restingRig;
        public void ResetPresentation()
        {
            previousPosition = transform.position; yaw = transform.eulerAngles.y;
            previousFall = landingTime = turn = phase = 0; Pose = 0;
            if (Rig != null && Rig != restingRig)
            {
                restingRig = Rig;
                secondaryRest = new Quaternion[Rig.SecondaryMotion.Length];
                for (int i = 0; i < secondaryRest.Length; i++) secondaryRest[i] = Rig.SecondaryMotion[i] != null ? Rig.SecondaryMotion[i].localRotation : Quaternion.identity;
            }
        }
        void Update() { if (Motor != null && Motor.Session.InputReady) Tick(Time.deltaTime); }
        public void Tick(float dt)
        {
            if (Motor == null || Motor.Traversal == null || dt <= 0) return;
            var t = Motor.Traversal; var a = Motor.Animator;
            if (a == null || a.runtimeAnimatorController == null) return;
            float measuredSpeed = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up).magnitude / dt;
            previousPosition = transform.position;
            measuredSpeed = Mathf.Min(measuredSpeed, Motor.Session.Balance.sprintSpeed);
            // Measure displacement so walking into a wall cannot keep the feet running.
            if (t.Grounded && previousFall < -1)
            {
                LandingWeight = Mathf.InverseLerp(2, 16, -previousFall); landingTime = Mathf.Lerp(.12f, .3f, LandingWeight);
            }
            previousFall = t.Grounded ? 0 : Mathf.Min(previousFall, t.VerticalVelocity);
            landingTime = Mathf.Max(0, landingTime - dt);
            Pose = Motor.Action == PlayerAction.Dead ? 10 : Motor.Action == PlayerAction.Hurt ? 9 :
                Motor.Action == PlayerAction.Dodge ? 5 : Motor.Action == PlayerAction.Attack ? 5 + Motor.AttackIndex :
                t.State == TraversalState.Climb ? (t.ClimbInput.sqrMagnitude > .01f ? 12 : 11) :
                t.State == TraversalState.LedgeTransition ? 13 : t.State == TraversalState.Glide ? 14 :
                !t.Grounded ? (t.VerticalVelocity > 0 ? 2 : 3) : landingTime > 0 ? 4 : 0;
            a.SetInteger("Pose", Pose); a.SetFloat("MoveSpeed", measuredSpeed, .08f, dt);
            float actionDuration = Motor.Action == PlayerAction.Dodge ? Motor.Session.Balance.dodgeDuration : Motor.Action == PlayerAction.Hurt ? .22f : Motor.Session.Balance.attackRecoveryEnd;
            a.SetFloat("ActionPhase", Mathf.Clamp01(Motor.AttackAge / Mathf.Max(.01f, actionDuration)));
            a.SetFloat("VerticalVelocity", t.VerticalVelocity); a.SetBool("Grounded", t.Grounded);
            a.SetBool("Sprint", t.State == TraversalState.Sprint); a.SetBool("Climb", t.State == TraversalState.Climb);
            a.SetBool("Glide", t.State == TraversalState.Glide); a.SetInteger("AttackIndex", Motor.AttackIndex);
            a.SetBool("Dodge", Motor.Action == PlayerAction.Dodge); a.SetBool("Hit", Motor.Action == PlayerAction.Hurt); a.SetBool("Dead", Motor.Action == PlayerAction.Dead);
            a.SetFloat("ClimbX", t.ClimbInput.x); a.SetFloat("ClimbY", t.ClimbInput.y);
            a.SetFloat("LandImpact", LandingWeight);
            a.SetFloat("ClimbRate", t.ClimbInput.y < -.01f ? -t.ClimbInput.magnitude : Mathf.Max(.15f, t.ClimbInput.magnitude));
            turn = Mathf.Lerp(turn, Mathf.Clamp(Mathf.DeltaAngle(yaw, transform.eulerAngles.y) / dt / 180, -1, 1), 1 - Mathf.Exp(-8 * dt)); yaw = transform.eulerAngles.y;
            a.SetFloat("Turn", turn);
            if (Motor.GlideSail != null) Motor.GlideSail.gameObject.SetActive(t.State == TraversalState.Glide && Motor.Action != PlayerAction.Dead);
            if (Rig != null && Motor.Blade != null)
            {
                bool sheathed = t.BlocksCombat || Motor.Action == PlayerAction.Dead;
                var socket = sheathed ? Rig.Back : Rig.Weapon;
                if (socket != null && Motor.Blade.parent != socket)
                {
                    Motor.Blade.SetParent(socket, false); Motor.Blade.localPosition = Vector3.zero;
                    Motor.Blade.localRotation = sheathed ? Quaternion.Euler(65, 0, 20) : Quaternion.identity;
                }
            }
            phase += dt;
        }
        void LateUpdate()
        {
            if (Rig == null || secondaryRest == null || Motor == null || !Motor.Session.InputReady) return;
            for (int i = 0; i < secondaryRest.Length; i++) if (Rig.SecondaryMotion[i] != null)
            {
                float sway = Motor.Action == PlayerAction.Dead ? 0 : Mathf.Sin(phase * 4 + i) * Mathf.Min(6, Motor.Traversal.Speed);
                Rig.SecondaryMotion[i].localRotation = secondaryRest[i] * Quaternion.Euler(sway, turn * 5, -turn * 4);
            }
        }
    }
}
