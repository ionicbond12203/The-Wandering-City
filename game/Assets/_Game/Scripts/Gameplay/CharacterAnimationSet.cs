using System;
using System.Collections.Generic;
using UnityEngine;

namespace WanderingCity
{
    [CreateAssetMenu(menuName = "Wandering City/Character animation set")]
    public sealed class CharacterAnimationSet : ScriptableObject
    {
        [Serializable] public struct Slot { public string State; public AnimationClip Clip; }
        public Slot[] Clips = new Slot[0];
        public static readonly string[] Required = { "Idle", "Walk", "Run", "Sprint", "JumpStart", "Fall", "Land", "Dodge", "Attack1", "Attack2", "Attack3", "Hit", "Death", "ClimbIdle", "ClimbMove", "LedgeTransition", "Glide" };
        public bool Complete
        {
            get { foreach (var name in Required) if (!Array.Exists(Clips, s => s.State == name && s.Clip != null)) return false; return true; }
        }
        public AnimatorOverrideController Apply(RuntimeAnimatorController controller)
        {
            var result = new AnimatorOverrideController(controller);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(); result.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
                foreach (var slot in Clips)
                    if (slot.State == overrides[i].Key.name && slot.Clip != null)
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, slot.Clip);
            result.ApplyOverrides(overrides); return result;
        }
    }
}
