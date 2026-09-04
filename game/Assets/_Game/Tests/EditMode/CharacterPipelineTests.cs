using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WanderingCity.Editor;

namespace WanderingCity.Tests
{
    public sealed class CharacterPipelineTests
    {
        [OneTimeSetUp] public void Prepare() => ProjectBuilder.Prepare();
        [Test] public void ControllerHasCompleteMotionsAndTypedParameters()
        {
            var c = Resources.Load<RuntimeAnimatorController>("Traveler") as AnimatorController;
            foreach (var name in CharacterAnimationSet.Required) Assert.IsTrue(c.animationClips.Any(a => a.name == name), name);
            foreach (var p in new[] { "MoveSpeed", "VerticalVelocity", "ClimbX", "ClimbY", "LandImpact" }) Assert.IsTrue(c.parameters.Any(a => a.name == p && a.type == AnimatorControllerParameterType.Float), p);
            foreach (var p in new[] { "Grounded", "Sprint", "Climb", "Glide", "Dodge", "Hit", "Dead" }) Assert.IsTrue(c.parameters.Any(a => a.name == p && a.type == AnimatorControllerParameterType.Bool), p);
            Assert.IsTrue(c.parameters.Any(p => p.name == "AttackIndex" && p.type == AnimatorControllerParameterType.Int));
            var tree = c.layers[0].stateMachine.defaultState.motion as BlendTree;
            Assert.IsNotNull(tree); Assert.AreEqual("MoveSpeed", tree.blendParameter); Assert.AreEqual(4, tree.children.Length);
            Assert.IsTrue(c.layers[0].stateMachine.anyStateTransitions.All(t => !t.hasExitTime && !t.canTransitionToSelf));
        }
        [Test] public void PrototypeClipsMoveLimbsWithoutMovingGameplayRoot()
        {
            var prefab = Resources.Load<GameObject>("Traveler_Stylized"); var instance = Object.Instantiate(prefab);
            try
            {
                Assert.IsTrue(instance.GetComponent<CharacterRigBindings>().IsPrototype);
                var leg = instance.transform.Find("Rig/LeftLeg"); var initial = leg.localRotation;
                var run = Resources.Load<RuntimeAnimatorController>("Traveler").animationClips.First(c => c.name == "Run");
                run.SampleAnimation(instance, .155f);
                Assert.Greater(Quaternion.Angle(initial, leg.localRotation), 20);
                Assert.AreEqual(Vector3.zero, instance.transform.position);
                Assert.IsTrue(AnimationUtility.GetCurveBindings(run).All(b => b.path.StartsWith("Rig")));
            }
            finally { Object.DestroyImmediate(instance); }
        }
        [Test] public void CustomSlotNestedSocketsAndCollidersAreIsolated()
        {
            var custom = new GameObject("Licensed replacement fixture"); var player = new GameObject("Gameplay capsule");
            try
            {
                custom.AddComponent<Animator>().applyRootMotion = true;
                custom.AddComponent<BoxCollider>(); custom.AddComponent<Rigidbody>();
                var nested = new GameObject("nested rig").transform; nested.SetParent(custom.transform);
                var weapon = new GameObject("WeaponSocket").transform; weapon.SetParent(nested);
                var bindings = custom.AddComponent<CharacterRigBindings>(); bindings.Weapon = weapon;
                var capsule = player.AddComponent<CharacterController>();
                var motor = player.AddComponent<PlayerMotor>(); motor.Controller = capsule;
                var adapter = player.AddComponent<CharacterVisualAdapter>(); adapter.CharacterPrefabSlot = custom;
                adapter.Setup(motor);
                Assert.AreSame(custom, adapter.CharacterPrefabSlot); Assert.IsTrue(adapter.MissingAuthoredAnimations);
                Assert.AreEqual("nested rig", adapter.Rig.Weapon.parent.name);
                Assert.AreSame(adapter.Rig.Weapon, adapter.Blade.parent);
                Assert.IsTrue(capsule.enabled); Assert.IsFalse(adapter.Animator.applyRootMotion);
                Assert.IsTrue(adapter.PlayerVisualRoot.GetComponentsInChildren<Collider>(true).All(c => !c.enabled));
                Assert.IsTrue(adapter.PlayerVisualRoot.GetComponentsInChildren<Rigidbody>(true).All(b => b.isKinematic && !b.detectCollisions));
                foreach (var socket in new[] { adapter.Rig.Weapon, adapter.Rig.Back, adapter.Rig.Glider, adapter.Rig.CameraFocus, adapter.Rig.Head, adapter.Rig.Vfx }) Assert.IsTrue(socket.IsChildOf(adapter.PlayerVisualRoot));
                adapter.Setup(motor); Assert.AreEqual(1, adapter.PlayerVisualRoot.childCount);
                Assert.AreEqual(1, player.GetComponents<CharacterAnimationDriver>().Length);
            }
            finally { Object.DestroyImmediate(player); Object.DestroyImmediate(custom); }
        }
        [Test] public void AnimationSetOverridesAllNamedMotions()
        {
            var set = ScriptableObject.CreateInstance<CharacterAnimationSet>(); var clip = new AnimationClip();
            AnimatorOverrideController result = null;
            try
            {
                Assert.IsFalse(set.Complete);
                set.Clips = CharacterAnimationSet.Required.Select(n => new CharacterAnimationSet.Slot { State = n, Clip = clip }).ToArray();
                Assert.IsTrue(set.Complete); result = set.Apply(Resources.Load<RuntimeAnimatorController>("Traveler"));
                foreach (var name in CharacterAnimationSet.Required) Assert.AreSame(clip, result[name], name);
            }
            finally { if (result != null) Object.DestroyImmediate(result); Object.DestroyImmediate(set); Object.DestroyImmediate(clip); }
        }
    }
}
