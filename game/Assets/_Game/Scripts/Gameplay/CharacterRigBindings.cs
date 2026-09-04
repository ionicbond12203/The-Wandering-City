using UnityEngine;

namespace WanderingCity
{
    // Put this on the visual prefab, never on the gameplay capsule. Explicit references win over bone lookup.
    public sealed class CharacterRigBindings : MonoBehaviour
    {
        public Transform Weapon, Back, Glider, CameraFocus, Head, Vfx;
        public Transform[] SecondaryMotion = new Transform[0];
        public CharacterAnimationSet Animations;
        public bool IsPrototype;

        public void Resolve(Animator animator)
        {
            Transform Bone(HumanBodyBones bone) => animator != null && animator.isHuman ? animator.GetBoneTransform(bone) : null;
            Weapon = Socket(Weapon, "WeaponSocket", Bone(HumanBodyBones.RightHand), new Vector3(.4f, 1, .1f));
            Back = Socket(Back, "BackSocket", Bone(HumanBodyBones.Chest), new Vector3(0, 1.1f, -.3f));
            Glider = Socket(Glider, "GliderSocket", null, new Vector3(0, 1.95f, -.35f));
            // Focus deliberately stays off animated bones to prevent gait-induced camera shake.
            CameraFocus = Socket(CameraFocus, "CameraFocus", null, new Vector3(0, 1.2f, 0));
            Head = Socket(Head, "HeadSocket", Bone(HumanBodyBones.Head), new Vector3(0, 1.65f, 0));
            Vfx = Socket(Vfx, "VfxSocket", Bone(HumanBodyBones.Hips), new Vector3(0, .9f, 0));
        }
        Transform Socket(Transform assigned, string socketName, Transform bone, Vector3 fallback)
        {
            if (assigned != null && assigned.IsChildOf(transform)) return assigned;
            foreach (var candidate in GetComponentsInChildren<Transform>(true))
                if (candidate.name == socketName) return candidate;
            var socket = new GameObject(socketName).transform;
            socket.SetParent(bone != null ? bone : transform, false);
            socket.localPosition = bone != null ? Vector3.zero : fallback;
            return socket;
        }
    }
}
