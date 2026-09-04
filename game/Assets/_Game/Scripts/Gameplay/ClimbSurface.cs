using UnityEngine;

namespace WanderingCity
{
    // No component, or Climbable=false, means NonClimbable. Layer and geometry checks also apply.
    public sealed class ClimbSurface : MonoBehaviour { public bool Climbable = true; }
}
