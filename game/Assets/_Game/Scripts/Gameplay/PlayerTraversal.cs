using UnityEngine;

namespace WanderingCity
{
    public enum TraversalState { Grounded, Jump, Fall, Sprint, Climb, LedgeTransition, Glide }

    public sealed class PlayerTraversal : MonoBehaviour
    {
        public PlayerMotor Motor;
        public TraversalState State { get; private set; }
        public Stamina Stamina { get; private set; }
        public float VerticalVelocity { get; private set; }
        public float Speed => horizontal.magnitude;
        public bool Grounded { get; private set; }
        public bool BlocksCombat => State == TraversalState.Climb || State == TraversalState.LedgeTransition || State == TraversalState.Glide;
        public bool LandedThisStep { get; private set; }
        GameBalance B => Motor.Session.Balance;
        CharacterController C => Motor.Controller;
        Vector3 horizontal, normal, ledgeTarget, ledgeLift;
        float grace, buffer, detach, ledgeTime;
        bool sprintLocked, ledgeRaised;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool InfiniteStamina, ShowClimbProbes;
#endif
        public void Initialize(PlayerMotor motor)
        {
            Motor = motor; Stamina = new Stamina(B.maxStamina, B.staminaRecovery, B.staminaDelay);
            C.slopeLimit = B.slopeLimit; C.stepOffset = B.stepHeight; C.minMoveDistance = 0; ResetMotion(true);
        }
        public void ResetMotion(bool refill = false)
        {
            State = TraversalState.Grounded; horizontal = Vector3.zero; VerticalVelocity = 0; grace = buffer = detach = 0; Grounded = false;
            if (refill) Stamina?.Refill();
        }
        public void Interrupt()
        {
            if (BlocksCombat) Detach();
            horizontal = Vector3.zero;
        }
        public void Detach()
        {
            State = TraversalState.Fall; VerticalVelocity = 0; detach = B.detachDelay; grace = buffer = 0;
        }
        bool CanTraverse => Motor.Session.Started && !Motor.Session.Paused && !Motor.Session.Building && Motor.Action == PlayerAction.Move && Motor.Session.State.hp > 0;
        bool Surface(RaycastHit hit)
        {
            var rule = hit.collider.GetComponent<ClimbSurface>();
            return rule != null && rule.Climbable && hit.collider.bounds.size.y >= B.climbMinHeight && Mathf.Abs(hit.normal.y) <= B.maxWallNormalY;
        }
        bool Probe(Vector3 origin, Vector3 direction, out RaycastHit hit) => Physics.Raycast(origin, direction, out hit, B.climbProbe, B.climbMask, QueryTriggerInteraction.Ignore) && Surface(hit);
        public bool TryClimb()
        {
            if (!CanTraverse || detach > 0 || Stamina.Current < B.staminaRestart || BlocksCombat) return false;
            if (!Probe(transform.position + Vector3.up * C.height * .55f, transform.forward, out var wall)) return false;
            normal = wall.normal; State = TraversalState.Climb; VerticalVelocity = 0; horizontal = Vector3.zero; grace = buffer = 0; return true;
        }
        public bool TryGlide()
        {
            if (!CanTraverse || BlocksCombat || Stamina.Current < B.staminaRestart) return false;
            if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, B.glideMinHeight, B.solidMask, QueryTriggerInteraction.Ignore)) return false;
            State = TraversalState.Glide; VerticalVelocity = Mathf.Min(0, VerticalVelocity); grace = buffer = 0; return true;
        }
        public bool SafeStandingPosition(Vector3 feet)
        {
            float radius = C.radius + .02f;
            return !Physics.CheckCapsule(feet + Vector3.up * (radius + .08f), feet + Vector3.up * (C.height - radius), radius, B.solidMask | (1 << 9), QueryTriggerInteraction.Ignore);
        }
        bool FindLedge()
        {
            Vector3 origin = transform.position + Vector3.up * B.ledgeReach - normal * B.ledgeForward;
            if (!Physics.Raycast(origin, Vector3.down, out var top, B.ledgeReach, B.solidMask, QueryTriggerInteraction.Ignore) || Vector3.Angle(top.normal, Vector3.up) > C.slopeLimit) return false;
            Vector3 target = top.point + Vector3.up * .08f;
            if (target.y < transform.position.y + .1f || !SafeStandingPosition(target)) return false;
            ledgeLift = new Vector3(transform.position.x, target.y, transform.position.z);
            if (!SafeStandingPosition(ledgeLift)) return false;
            ledgeTarget = target; ledgeRaised = false; ledgeTime = 0; State = TraversalState.LedgeTransition; return true;
        }
        void Move(Vector3 displacement)
        {
            // Bounded swept CharacterController moves preserve collision at sprint/dodge speed and low FPS.
            int steps = Mathf.Max(1, Mathf.CeilToInt(displacement.magnitude / Mathf.Max(.05f, B.maxMoveStep)));
            for (int i = 0; i < steps; i++)
            {
                var flags = C.Move(displacement / steps);
                if ((flags & CollisionFlags.Above) != 0 && VerticalVelocity > 0) VerticalVelocity = 0;
                if ((flags & CollisionFlags.Below) != 0 && VerticalVelocity < 0) VerticalVelocity = -B.groundSnap;
            }
        }
        public void Simulate(float dt, Vector3 direction, Vector2 input, bool sprint, bool walk, bool jump, bool climb, bool glide, bool cancel, Vector3? forcedVelocity = null)
        {
            if (dt <= 0 || !Motor.Session.InputReady || Motor.Action == PlayerAction.Dead) return;
            if (jump && CanTraverse && !BlocksCombat) buffer = B.jumpBuffer;
            if (cancel && BlocksCombat) Detach();
            if (climb) { if (State == TraversalState.Climb) Detach(); else TryClimb(); }
            if (glide) { if (State == TraversalState.Glide) Detach(); else TryGlide(); }
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / Mathf.Max(.01f, B.maxSimulationStep)));
            for (int i = 0; i < steps; i++) Step(dt / steps, direction, input, sprint, walk, forcedVelocity);
        }
        void Step(float dt, Vector3 direction, Vector2 input, bool sprint, bool walk, Vector3? forcedVelocity)
        {
            detach = Mathf.Max(0, detach - dt); buffer = Mathf.Max(0, buffer - dt);
            bool wasGrounded = Grounded;
            // isGrounded describes the previous Move and can be stale after restoring a position.
            Grounded = C.isGrounded && VerticalVelocity <= 0 && Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, .3f, B.solidMask, QueryTriggerInteraction.Ignore);
            LandedThisStep = Grounded && !wasGrounded;
            float drain = 0;
            if (State == TraversalState.LedgeTransition)
            {
                ledgeTime += dt;
                Vector3 destination = ledgeRaised ? ledgeTarget : ledgeLift;
                Move(Vector3.ClampMagnitude(destination - transform.position, B.ledgeSpeed * dt));
                if (Vector3.Distance(transform.position, destination) < .06f)
                {
                    if (ledgeRaised) { State = TraversalState.Grounded; VerticalVelocity = -B.groundSnap; }
                    else ledgeRaised = true;
                }
                if (ledgeTime > B.ledgeTimeout) Detach();
                drain = B.climbUpDrain;
            }
            else if (State == TraversalState.Climb)
            {
                Vector3 origin = transform.position + Vector3.up * C.height * .55f;
                bool found = Probe(origin, -normal, out var wall);
                if (!found && input.x != 0)
                {
                    Vector3 corner = Quaternion.AngleAxis(-Mathf.Sign(input.x) * B.cornerAngle, Vector3.up) * -normal;
                    found = Probe(origin, corner, out wall);
                }
                if (!found) { if (input.y <= 0 || !FindLedge()) Detach(); }
                else if (Vector3.Angle(normal, wall.normal) > B.cornerAngle) Detach();
                else
                {
                    normal = Vector3.Slerp(normal, wall.normal, B.turnSpeed * dt).normalized;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-normal), B.turnSpeed * dt);
                    Vector3 lateral = Vector3.Cross(Vector3.up, normal);
                    float correction = wall.distance - C.radius - B.wallGap;
                    Move((Vector3.up * input.y - lateral * input.x) * B.climbSpeed * dt - normal * Mathf.Clamp(correction, -B.climbSpeed * dt, B.climbSpeed * dt));
                    if (Grounded && input.y < 0) Detach();
                }
                drain = input.y > 0 ? B.climbUpDrain : input.y < 0 ? B.climbDownDrain : Mathf.Abs(input.x) > 0 ? B.climbSideDrain : B.climbIdleDrain;
            }
            else
            {
                if (Grounded)
                {
                    grace = B.coyoteTime;
                    if (State == TraversalState.Glide) State = TraversalState.Grounded;
                    VerticalVelocity = -B.groundSnap;
                }
                else grace = Mathf.Max(0, grace - dt);
                if (State == TraversalState.Glide)
                {
                    Vector3 steering = direction.sqrMagnitude > .01f ? direction : transform.forward;
                    horizontal = Vector3.MoveTowards(horizontal, steering * B.glideSpeed, B.glideAcceleration * dt);
                    VerticalVelocity = Mathf.MoveTowards(VerticalVelocity, -B.glideDescent, B.gravity * B.glideGravityMultiplier * dt);
                    drain = B.glideDrain;
                }
                else
                {
                    if (Stamina.Current >= B.staminaRestart && !sprint) sprintLocked = false;
                    bool runningFast = CanTraverse && Grounded && sprint && !sprintLocked && !Stamina.Exhausted && direction.sqrMagnitude > .01f;
                    float speed = runningFast ? B.sprintSpeed : walk ? B.walkSpeed : B.runSpeed;
                    Vector3 desired = direction * (Motor.Action == PlayerAction.Move ? speed : 0);
                    float rate = desired.sqrMagnitude < horizontal.sqrMagnitude ? B.deceleration : B.acceleration;
                    horizontal = Vector3.MoveTowards(horizontal, desired, rate * (Grounded ? 1 : B.airControl) * dt);
                    if (buffer > 0 && grace > 0 && CanTraverse)
                    {
                        VerticalVelocity = Mathf.Sqrt(2 * B.gravity * B.jumpHeight); grace = buffer = 0; Grounded = false;
                    }
                    VerticalVelocity = Mathf.Max(-B.terminalSpeed, VerticalVelocity - B.gravity * dt);
                    State = Grounded ? runningFast ? TraversalState.Sprint : TraversalState.Grounded : VerticalVelocity > 0 ? TraversalState.Jump : TraversalState.Fall;
                    if (runningFast) drain = B.sprintDrain;
                }
                if (direction.sqrMagnitude > .01f && Motor.Action == PlayerAction.Move) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), B.turnSpeed * dt);
                C.stepOffset = Grounded ? B.stepHeight : 0;
                Vector3 motion = forcedVelocity ?? horizontal;
                if (Grounded && VerticalVelocity <= 0 && Physics.SphereCast(transform.position + Vector3.up * (C.radius + .2f), C.radius * .85f, Vector3.down, out var ground, .5f, B.solidMask, QueryTriggerInteraction.Ignore) && Vector3.Angle(ground.normal, Vector3.up) <= C.slopeLimit)
                    motion = Vector3.ProjectOnPlane(motion, ground.normal).normalized * motion.magnitude;
                Move((motion + Vector3.up * VerticalVelocity) * dt);
                if (C.isGrounded && State == TraversalState.Glide) { State = TraversalState.Grounded; Grounded = true; }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (InfiniteStamina) Stamina.Refill(); else
#endif
            Stamina.Tick(dt, drain);
            if (Stamina.Exhausted)
            {
                sprintLocked = true;
                if (BlocksCombat) Detach();
                else if (State == TraversalState.Sprint) State = TraversalState.Grounded;
            }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnDrawGizmos()
        {
            if (!ShowClimbProbes || Motor == null) return;
            Gizmos.color = Color.cyan; Gizmos.DrawRay(transform.position + Vector3.up * C.height * .55f, transform.forward * B.climbProbe);
            if (State == TraversalState.LedgeTransition) { Gizmos.DrawWireSphere(ledgeTarget, C.radius); Gizmos.DrawLine(transform.position, ledgeLift); }
        }
#endif
    }
}
