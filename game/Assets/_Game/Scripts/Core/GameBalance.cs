using UnityEngine;
namespace WanderingCity
{
    [CreateAssetMenu(menuName = "Wandering City/Game Balance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Min(1)] public int normalDamage = 26, upgradedDamage = 42, enemyHealth = 104, enemyDamage = 18;
        [Min(.01f)] public float attackWindup = .16f, attackHitEnd = .34f, attackRecoveryEnd = .65f, dodgeDuration = .42f, invulnerability = .28f, dodgeCooldown = .9f;
        [Header("Traversal")]
        public float walkSpeed = 2.4f, runSpeed = 4.8f, sprintSpeed = 8, dodgeSpeed = 14;
        public float acceleration = 30, deceleration = 38, airControl = .4f, turnSpeed = 14;
        public float gravity = 24, jumpHeight = 1.6f, terminalSpeed = 35, groundSnap = 4;
        public float coyoteTime = .12f, jumpBuffer = .15f, slopeLimit = 48, stepHeight = .35f;
        public float maxMoveStep = .18f, maxSimulationStep = .025f;
        [Header("Stamina")]
        public float maxStamina = 100, staminaRecovery = 25, staminaDelay = 1.1f;
        public float sprintDrain = 16, climbUpDrain = 15, climbSideDrain = 10, climbDownDrain = 5, climbIdleDrain = 3, glideDrain = 7;
        public float staminaRestart = 15;
        [Header("Climb / ledge")]
        public LayerMask climbMask = 1, solidMask = 1;
        public float climbSpeed = 2.6f, climbProbe = .95f, wallGap = .06f, climbMinHeight = 2;
        public float maxWallNormalY = .25f, cornerAngle = 65, detachDelay = .45f;
        public float ledgeReach = 1.6f, ledgeForward = .95f, ledgeSpeed = 3.5f, ledgeTimeout = 2;
        [Header("Glide")]
        public float glideGravityMultiplier = .15f, glideSpeed = 7, glideAcceleration = 8, glideDescent = 2.5f, glideMinHeight = 1.4f;
        [Header("Camera")]
        public float cameraFov = 58, sprintFov = 64, cameraBlend = 5, glideCameraDistance = 2, climbCameraHeight = .3f, cameraRadius = .24f;
        [Header("Exploration rewards")]
        public int commonTreasureOre = 2, rareTreasureOre = 6, rareTreasurePotions = 2;
    }
}
