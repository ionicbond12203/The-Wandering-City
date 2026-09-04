using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WanderingCity.Editor
{
    public static class CharacterAuthoring
    {
        const string Path = "Assets/_Game/Resources/Traveler.controller";
        const string Version = "Traveler / Character pipeline v2";
        public static void EnsureController()
        {
            const string settingsPath = "Assets/_Game/Resources/CharacterPresentation.asset";
            if (AssetDatabase.LoadAssetAtPath<CharacterPresentationSettings>(settingsPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CharacterPresentationSettings>(), settingsPath);
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Path);
            if (controller != null && controller.name == Version) return;
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(Path);
            controller.layers = Array.Empty<AnimatorControllerLayer>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Path)) if (asset != controller) Object.DestroyImmediate(asset, true);
            controller.name = Version; controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (var p in new[] { "MoveSpeed", "VerticalVelocity", "ClimbX", "ClimbY", "LandImpact", "Turn", "ClimbRate", "ActionPhase" }) controller.AddParameter(p, AnimatorControllerParameterType.Float);
            foreach (var p in new[] { "Grounded", "Sprint", "Climb", "Glide", "Dodge", "Hit", "Dead" }) controller.AddParameter(p, AnimatorControllerParameterType.Bool);
            controller.AddParameter("Pose", AnimatorControllerParameterType.Int); controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
            controller.AddLayer("Full body"); var machine = controller.layers[0].stateMachine;
            var clips = CharacterAnimationSet.Required.ToDictionary(n => n, n => Clip(n, controller));
            var locomotion = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "MoveSpeed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(locomotion, controller);
            var balance = Resources.Load<GameBalance>("Balance");
            locomotion.AddChild(clips["Idle"], 0); locomotion.AddChild(clips["Walk"], balance.walkSpeed);
            locomotion.AddChild(clips["Run"], balance.runSpeed); locomotion.AddChild(clips["Sprint"], balance.sprintSpeed);
            var land = new BlendTree { name = "Landing impact", blendType = BlendTreeType.Simple1D, blendParameter = "LandImpact", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(land, controller); land.AddChild(clips["Idle"], 0); land.AddChild(clips["Land"], 1);
            var names = new[] { "Locomotion", "JumpStart", "Fall", "Land", "Dodge", "Attack1", "Attack2", "Attack3", "Hit", "Death", "ClimbIdle", "ClimbMove", "LedgeTransition", "Glide" };
            int[] poses = { 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
            for (int i = 0; i < names.Length; i++)
            {
                var state = machine.AddState(names[i], new Vector3(i % 4 * 240, i / 4 * 90));
                state.motion = i == 0 ? locomotion : names[i] == "Land" ? land : clips[names[i]];
                state.writeDefaultValues = false;
                if (i == 0) machine.defaultState = state;
                if (names[i] == "ClimbMove") { state.speedParameterActive = true; state.speedParameter = "ClimbRate"; }
                if (names[i].StartsWith("Attack") || names[i] == "Dodge" || names[i] == "Hit") { state.timeParameterActive = true; state.timeParameter = "ActionPhase"; }
                var transition = machine.AddAnyStateTransition(state); transition.hasExitTime = false;
                transition.duration = names[i] == "Death" || names[i].StartsWith("Attack") ? .06f : .12f;
                transition.canTransitionToSelf = false; transition.AddCondition(AnimatorConditionMode.Equals, poses[i], "Pose");
            }
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        }

        static AnimationClip Clip(string name, AnimatorController controller)
        {
            bool loop = new[] { "Idle", "Walk", "Run", "Sprint", "Fall", "ClimbIdle", "ClimbMove", "Glide" }.Contains(name);
            float duration = name == "Walk" ? .8f : name == "Run" ? .62f : name == "Sprint" ? .48f : name.StartsWith("Attack") ? .65f : name == "Dodge" ? .42f : name == "Land" ? .3f : 1;
            var clip = new AnimationClip { name = name, frameRate = 60 };
            string[] joints = { "Rig", "Rig/Chest", "Rig/Chest/LeftArm", "Rig/Chest/RightArm", "Rig/Chest/LeftArm/Forearm", "Rig/Chest/RightArm/Forearm", "Rig/LeftLeg", "Rig/RightLeg", "Rig/LeftLeg/Knee", "Rig/RightLeg/Knee" };
            foreach (string path in joints) for (int axis = 0; axis < 3; axis++)
            {
                var keys = new Keyframe[33];
                for (int k = 0; k < keys.Length; k++)
                {
                    float u = k / 32f; Vector3 angles = PoseAngles(name, path, u);
                    keys[k] = new Keyframe(u * duration, angles[axis]);
                }
                var curve = new AnimationCurve(keys);
                for (int k = 0; k < keys.Length; k++) AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.ClampedAuto);
                clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw." + "xyz"[axis], curve);
            }
            var hips = new Keyframe[33];
            for (int i = 0; i < hips.Length; i++)
            {
                float u = i / 32f;
                float dip = name == "Land" ? Mathf.Sin(u * Mathf.PI) * .22f : name == "Death" ? Mathf.SmoothStep(0, .68f, u) : name == "Dodge" ? .3f * Mathf.Sin(u * Mathf.PI) : 0;
                hips[i] = new Keyframe(u * duration, .78f - dip);
            }
            clip.SetCurve("Rig", typeof(Transform), "m_LocalPosition.y", new AnimationCurve(hips));
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings); clip.EnsureQuaternionContinuity();
            AssetDatabase.AddObjectToAsset(clip, controller); return clip;
        }
        static Vector3 PoseAngles(string name, string joint, float u)
        {
            bool arm = joint.EndsWith("Arm"), forearm = joint.EndsWith("Forearm"), leg = joint.EndsWith("Leg"), knee = joint.EndsWith("Knee");
            float side = joint.Contains("Left") ? -1 : 1, wave = Mathf.Sin(u * Mathf.PI * 2) * side;
            if (name == "Walk" || name == "Run" || name == "Sprint")
            {
                float amp = name == "Walk" ? 24 : name == "Run" ? 38 : 48;
                if (arm) return new Vector3(-wave * amp * .75f, 0, -side * 6);
                if (forearm) return new Vector3(name == "Walk" ? -12 : -55, 0, 0);
                if (leg) return new Vector3(wave * amp, 0, 0);
                if (knee) return new Vector3(Mathf.Max(0, -wave) * amp * 1.5f, 0, 0);
                if (joint == "Rig/Chest") return new Vector3(name == "Sprint" ? 12 : 5, wave * 5, 0);
            }
            if (name.StartsWith("Attack"))
            {
                int index = name[name.Length - 1] - '0';
                float swing = u < .25f ? Mathf.Lerp(0, -65, u / .25f) : u < .53f ? Mathf.Lerp(-65, 85, (u - .25f) / .28f) : Mathf.Lerp(85, 0, (u - .53f) / .47f);
                if (joint == "Rig/Chest") return new Vector3(8, swing * (index == 2 ? -.5f : .5f), 0);
                if (arm) return side > 0 ? new Vector3(index == 3 ? -110 + swing : -55, swing, -25) : new Vector3(-25, 0, 20);
                if (forearm) return new Vector3(-30, 0, 0);
                if (leg) return new Vector3(side * 15, 0, 0);
            }
            if (name == "ClimbIdle" || name == "ClimbMove" || name == "LedgeTransition")
            {
                float motion = name == "ClimbIdle" ? 0 : wave;
                if (arm) return new Vector3(-140 + motion * 30, 0, -side * 15);
                if (forearm) return new Vector3(-15, 0, 0);
                if (leg) return new Vector3(-35 - motion * 30, 0, side * 5);
                if (knee) return new Vector3(55 + motion * 25, 0, 0);
                if (joint == "Rig/Chest") return new Vector3(-8, 0, 0);
            }
            if (name == "Glide")
            {
                if (arm) return new Vector3(-150, 0, -side * 20);
                if (forearm) return new Vector3(-15, 0, 0);
                if (leg) return new Vector3(12 + side * 7, 0, 0);
                if (knee) return new Vector3(25, 0, 0);
                if (joint == "Rig/Chest") return new Vector3(12, 0, 0);
            }
            if (name == "JumpStart" || name == "Fall")
            {
                if (arm) return new Vector3(name == "JumpStart" ? -55 : -15, 0, -side * 25);
                if (leg) return new Vector3(side < 0 ? -35 : 15, 0, 0);
                if (knee) return new Vector3(35, 0, 0);
            }
            if (name == "Land" || name == "Dodge")
            {
                float weight = Mathf.Sin(u * Mathf.PI);
                if (leg) return new Vector3(-40 * weight, 0, 0);
                if (knee) return new Vector3(75 * weight, 0, 0);
                if (joint == "Rig/Chest") return new Vector3(30 * weight, 0, 0);
                if (arm) return new Vector3(-45 * weight, 0, -side * 12);
            }
            if (name == "Hit" && joint == "Rig/Chest") return new Vector3(-20 * Mathf.Sin(u * Mathf.PI), 0, 0);
            if (name == "Death" && joint == "Rig") return new Vector3(0, 0, Mathf.SmoothStep(0, 85, u));
            return arm ? new Vector3(0, 0, -side * 5) : Vector3.zero;
        }

        // Upgrade only our original, unskinned study; authored Humanoid assets are never modified.
        public static void RigPrototype(GameObject root)
        {
            var parts = root.GetComponentsInChildren<MeshRenderer>().Select(r => r.transform).ToArray();
            Transform Joint(string name, Transform parent, Vector3 world)
            { var t = new GameObject(name).transform; t.SetParent(parent, false); t.position = world; return t; }
            var hips = Joint("Rig", root.transform, new Vector3(0, .78f, 0));
            var chest = Joint("Chest", hips, new Vector3(0, 1.25f, 0));
            Transform rightHand = null;
            foreach (int side in new[] { -1, 1 })
            {
                string label = side < 0 ? "Left" : "Right";
                var arm = Joint(label + "Arm", chest, new Vector3(side * .32f, 1.3f, 0));
                var forearm = Joint("Forearm", arm, new Vector3(side * .33f, 1.02f, 0));
                var hand = Joint("Hand", forearm, new Vector3(side * .34f, .8f, .06f));
                var thigh = Joint(label + "Leg", hips, new Vector3(side * .14f, .76f, 0));
                var knee = Joint("Knee", thigh, new Vector3(side * .14f, .43f, 0));
                foreach (var part in parts.Where(p => Mathf.Sign(p.position.x) == side))
                {
                    if (part.name == "Sleeve") part.SetParent(arm, true);
                    if (part.name == "Hand") part.SetParent(hand, true);
                    if (part.name == "Boot") part.SetParent(knee, true);
                    if (part.name == "Leg")
                    {
                        part.localScale = new Vector3(.21f, .32f, .24f); part.position = new Vector3(side * .14f, .60f, 0); part.SetParent(thigh, true);
                        var shin = Object.Instantiate(part, knee); shin.name = "Shin"; shin.position = new Vector3(side * .14f, .29f, 0);
                    }
                }
                if (side > 0) rightHand = hand;
            }
            foreach (var part in parts.Where(p => p.parent == root.transform)) part.SetParent(chest, true);
            var rig = root.AddComponent<CharacterRigBindings>(); rig.IsPrototype = true;
            rig.Weapon = Joint("WeaponSocket", rightHand, rightHand.position);
            rig.Back = Joint("BackSocket", chest, new Vector3(0, 1.1f, -.3f));
            rig.Glider = Joint("GliderSocket", root.transform, new Vector3(0, 1.95f, -.35f));
            rig.CameraFocus = Joint("CameraFocus", root.transform, new Vector3(0, 1.2f, 0));
            rig.Head = Joint("HeadSocket", chest, new Vector3(0, 1.65f, 0));
            rig.Vfx = Joint("VfxSocket", hips, new Vector3(0, .9f, 0));
            rig.SecondaryMotion = parts.Where(p => p.name == "Cloak" || p.name == "Scarf" || p.name == "Side pack").ToArray();
            var sword = root.transform.Find("Sword pivot"); if (sword != null) { sword.SetParent(rig.Weapon, false); sword.localPosition = Vector3.zero; }
        }
    }
}
