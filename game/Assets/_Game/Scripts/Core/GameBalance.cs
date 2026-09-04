using UnityEngine;
namespace WanderingCity
{
    [CreateAssetMenu(menuName = "Wandering City/Game Balance")]
    public sealed class GameBalance : ScriptableObject
    {
        [Min(1)] public int normalDamage = 26, upgradedDamage = 42, enemyHealth = 104, enemyDamage = 18;
        [Min(.01f)] public float attackWindup = .16f, attackHitEnd = .34f, attackRecoveryEnd = .65f, dodgeDuration = .42f, invulnerability = .28f, dodgeCooldown = .9f;
    }
}
