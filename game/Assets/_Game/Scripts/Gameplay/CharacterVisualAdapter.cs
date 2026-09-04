using UnityEngine;

namespace WanderingCity
{
    [DisallowMultipleComponent]
    public sealed class CharacterVisualAdapter : MonoBehaviour
    {
        public Transform PlayerVisualRoot;
        public GameObject CharacterPrefabSlot;
        public Transform Blade;
        public Transform GlideSail;
        public Animator Animator;
        public bool IsFallbackMode { get; private set; }

        public CharacterRigBindings Rig { get; private set; }
        public CharacterAnimationDriver Driver { get; private set; }
        public bool MissingAuthoredAnimations { get; private set; }
        AnimatorOverrideController ownedController;

        public void Setup(PlayerMotor motor, GameObject customPrefab = null)
        {
            if (PlayerVisualRoot == null)
            {
                PlayerVisualRoot = transform.Find("PlayerVisualRoot");
                if (PlayerVisualRoot == null)
                {
                    PlayerVisualRoot = new GameObject("PlayerVisualRoot").transform;
                    PlayerVisualRoot.SetParent(transform, false);
                }
            }
            for (int i = PlayerVisualRoot.childCount - 1; i >= 0; i--)
            {
                var child = PlayerVisualRoot.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            // Respect an Inspector-assigned slot when no call-site override was supplied.
            if (customPrefab != null) CharacterPrefabSlot = customPrefab;
            if (CharacterPrefabSlot == null) CharacterPrefabSlot = Resources.Load<CharacterPresentationSettings>("CharacterPresentation")?.CharacterPrefabSlot;
            if (CharacterPrefabSlot == null) CharacterPrefabSlot = Resources.Load<GameObject>("Traveler_Stylized");
            Blade = GlideSail = null;
            IsFallbackMode = DevelopmentVisualMode.UseFallbackPrimitives;
            GameObject instance;
            if (!IsFallbackMode && CharacterPrefabSlot != null)
            {
                instance = Instantiate(CharacterPrefabSlot, PlayerVisualRoot, false);
                instance.name = "Character";
            }
            else
            {
                if (IsFallbackMode) CreateDebugPrimitives(PlayerVisualRoot); else CreateStylizedTraveler(PlayerVisualRoot);
                instance = PlayerVisualRoot.GetChild(0).gameObject;
            }
            // Render assets cannot introduce a second collision / root-motion authority.
            foreach (var col in instance.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            Animator = instance.GetComponentInChildren<Animator>(true);
            if (Animator == null) Animator = instance.AddComponent<Animator>();
            foreach (var animator in instance.GetComponentsInChildren<Animator>(true)) { animator.applyRootMotion = false; if (animator != Animator) animator.enabled = false; }
            Rig = instance.GetComponent<CharacterRigBindings>() ?? instance.AddComponent<CharacterRigBindings>();
            Rig.Resolve(Animator);
            if (Blade == null)
            {
                foreach (var t in instance.GetComponentsInChildren<Transform>(true)) if (t.name == "Sword pivot") { Blade = t; break; }
                if (Blade == null) Blade = new GameObject("Sword pivot").transform;
            }
            Blade.SetParent(Rig.Weapon, false); Blade.localPosition = Vector3.zero; Blade.localRotation = Quaternion.identity;
            if (GlideSail == null) GlideSail = CreateStylizedSail(Rig.Glider);
            GlideSail.SetParent(Rig.Glider, false); GlideSail.localPosition = Vector3.zero;
            var controller = Resources.Load<RuntimeAnimatorController>("Traveler");
            if (ownedController != null) { if (Application.isPlaying) Destroy(ownedController); else DestroyImmediate(ownedController); }
            MissingAuthoredAnimations = !Rig.IsPrototype && (Rig.Animations == null || !Rig.Animations.Complete);
            ownedController = Rig.Animations != null ? Rig.Animations.Apply(controller) : null;
            Animator.runtimeAnimatorController = ownedController != null ? ownedController : controller;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (motor != null)
            {
                motor.Visual = PlayerVisualRoot; motor.Blade = Blade; motor.GlideSail = GlideSail;
                motor.Animator = Animator; motor.VisualAdapter = this;
                Driver = GetComponent<CharacterAnimationDriver>() ?? gameObject.AddComponent<CharacterAnimationDriver>();
                Driver.Motor = motor; Driver.Rig = Rig; Driver.ResetPresentation();
            }
            GlideSail.gameObject.SetActive(false);
        }
        void OnDestroy() { if (ownedController != null) Destroy(ownedController); }

        void CreateStylizedTraveler(Transform root)
        {
            var charRoot = new GameObject("[PLACEHOLDER] StylizedAdventurer");
            charRoot.transform.SetParent(root, false);

            Material charMat = Resources.Load<Material>("StylizedCharacterMat");
            if (charMat == null)
            {
                var envShader = Shader.Find("WanderingCity/StylizedEnvironment") ?? Shader.Find("Universal Render Pipeline/Lit");
                charMat = new Material(envShader);
                charMat.name = "StylizedCharacter_Runtime";
                charMat.SetColor("_BaseColor", new Color(0.22f, 0.42f, 0.48f));
                charMat.SetColor("_ShadowColor", new Color(0.12f, 0.22f, 0.28f));
                charMat.SetColor("_RimColor", new Color(0.75f, 0.9f, 0.95f));
                charMat.SetFloat("_RimPower", 3.2f);
            }

            Material skinMat = new Material(charMat);
            skinMat.SetColor("_BaseColor", new Color(0.95f, 0.82f, 0.72f));
            skinMat.SetColor("_ShadowColor", new Color(0.85f, 0.65f, 0.55f));

            Material hairMat = new Material(charMat);
            hairMat.SetColor("_BaseColor", new Color(0.28f, 0.20f, 0.16f));
            hairMat.SetColor("_ShadowColor", new Color(0.15f, 0.10f, 0.08f));

            Material scarfMat = new Material(charMat);
            scarfMat.SetColor("_BaseColor", new Color(0.82f, 0.28f, 0.22f));
            scarfMat.SetColor("_ShadowColor", new Color(0.55f, 0.16f, 0.12f));

            Material goldTrimMat = new Material(charMat);
            goldTrimMat.SetColor("_BaseColor", new Color(0.92f, 0.78f, 0.35f));
            goldTrimMat.SetColor("_ShadowColor", new Color(0.65f, 0.48f, 0.18f));

            // 1. Torso / Coat
            var coat = CreatePart("Coat", PrimitiveType.Cylinder, charRoot.transform, new Vector3(0, 0.85f, 0), new Vector3(0.52f, 0.55f, 0.42f), charMat);
            var tunicHem = CreatePart("TunicHem", PrimitiveType.Cylinder, charRoot.transform, new Vector3(0, 0.48f, 0), new Vector3(0.56f, 0.22f, 0.46f), charMat);

            // 2. Head & Hair
            var head = CreatePart("Head", PrimitiveType.Sphere, charRoot.transform, new Vector3(0, 1.55f, 0.02f), new Vector3(0.38f, 0.42f, 0.38f), skinMat);
            var hairMain = CreatePart("Hair_Main", PrimitiveType.Sphere, charRoot.transform, new Vector3(0, 1.62f, -0.04f), new Vector3(0.42f, 0.40f, 0.44f), hairMat);
            var hairBangs = CreatePart("Hair_Bangs", PrimitiveType.Cube, charRoot.transform, new Vector3(0, 1.68f, 0.14f), new Vector3(0.36f, 0.14f, 0.18f), hairMat);
            hairBangs.transform.localRotation = Quaternion.Euler(18f, 0, 0);

            // 3. Scarf
            var scarf = CreatePart("Scarf", PrimitiveType.Cylinder, charRoot.transform, new Vector3(0, 1.34f, 0.02f), new Vector3(0.46f, 0.12f, 0.42f), scarfMat);
            var scarfTail = CreatePart("ScarfTail", PrimitiveType.Cube, charRoot.transform, new Vector3(-0.16f, 1.15f, -0.22f), new Vector3(0.12f, 0.32f, 0.08f), scarfMat);
            scarfTail.transform.localRotation = Quaternion.Euler(15f, -20f, 12f);

            // 4. Belt & Satchel
            var belt = CreatePart("Belt", PrimitiveType.Cylinder, charRoot.transform, new Vector3(0, 0.78f, 0), new Vector3(0.54f, 0.08f, 0.44f), goldTrimMat);
            var satchel = CreatePart("Satchel", PrimitiveType.Cube, charRoot.transform, new Vector3(0.28f, 0.72f, 0), new Vector3(0.14f, 0.22f, 0.22f), hairMat);

            // 5. Backpack / Bedroll
            var pack = CreatePart("TravelPack", PrimitiveType.Cube, charRoot.transform, new Vector3(0, 1.05f, -0.28f), new Vector3(0.44f, 0.48f, 0.24f), hairMat);
            var bedroll = CreatePart("Bedroll", PrimitiveType.Cylinder, charRoot.transform, new Vector3(0, 1.34f, -0.28f), new Vector3(0.18f, 0.26f, 0.18f), scarfMat);
            bedroll.transform.localRotation = Quaternion.Euler(0, 0, 90f);

            // 6. Blade & Pivot
            var bladePivot = new GameObject("Sword pivot").transform;
            bladePivot.SetParent(root, false);
            bladePivot.localPosition = new Vector3(0.45f, 1f, 0.1f);
            Blade = bladePivot;

            var swordHilt = CreatePart("Hilt", PrimitiveType.Cylinder, bladePivot, new Vector3(0, 0, 0.15f), new Vector3(0.06f, 0.18f, 0.06f), goldTrimMat);
            swordHilt.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var swordGuard = CreatePart("Guard", PrimitiveType.Cube, bladePivot, new Vector3(0, 0, 0.32f), new Vector3(0.32f, 0.06f, 0.08f), goldTrimMat);
            var swordBlade = CreatePart("BladeMesh", PrimitiveType.Cube, bladePivot, new Vector3(0, 0, 0.95f), new Vector3(0.08f, 0.12f, 1.25f), skinMat);

            // 7. Glide Sail
            GlideSail = CreateStylizedSail(root);
        }

        Transform CreateStylizedSail(Transform root)
        {
            var sailRoot = new GameObject("Traveler / folding wind sail").transform;
            sailRoot.SetParent(root, false);
            sailRoot.localPosition = new Vector3(0, 1.95f, -0.35f);

            var envShader = Shader.Find("WanderingCity/StylizedEnvironment") ?? Shader.Find("Universal Render Pipeline/Lit");
            var sailMat = new Material(envShader);
            sailMat.name = "Sail_Runtime";
            sailMat.SetColor("_BaseColor", new Color(0.28f, 0.72f, 0.68f));
            sailMat.SetColor("_ShadowColor", new Color(0.16f, 0.45f, 0.42f));
            sailMat.SetColor("_RimColor", new Color(0.85f, 0.98f, 0.95f));
            sailMat.SetFloat("_RimPower", 2.5f);

            var sparMat = new Material(envShader);
            sparMat.SetColor("_BaseColor", new Color(0.88f, 0.76f, 0.42f));

            // Wing left & right
            var wingLeft = CreatePart("Wing_Left", PrimitiveType.Cube, sailRoot, new Vector3(-1.1f, 0, 0), new Vector3(1.7f, 0.05f, 0.95f), sailMat);
            wingLeft.transform.localRotation = Quaternion.Euler(0, 12f, -6f);
            var wingRight = CreatePart("Wing_Right", PrimitiveType.Cube, sailRoot, new Vector3(1.1f, 0, 0), new Vector3(1.7f, 0.05f, 0.95f), sailMat);
            wingRight.transform.localRotation = Quaternion.Euler(0, -12f, 6f);

            // Central spar
            var spar = CreatePart("Spar", PrimitiveType.Cylinder, sailRoot, Vector3.zero, new Vector3(0.08f, 1.8f, 0.08f), sparMat);
            spar.transform.localRotation = Quaternion.Euler(0, 0, 90f);

            return sailRoot;
        }

        void CreateDebugPrimitives(Transform root)
        {
            var debugRoot = new GameObject("[PLACEHOLDER] DebugPrimitives");
            debugRoot.transform.SetParent(root, false);

            var coatMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            coatMat.color = new Color(.18f, .38f, .43f);
            var headMat = new Material(coatMat) { color = new Color(.83f, .68f, .48f) };
            var packMat = new Material(coatMat) { color = new Color(.44f, .3f, .18f) };
            var swordMat = new Material(coatMat) { color = new Color(.86f, .9f, .84f) };
            var sailMat = new Material(coatMat) { color = new Color(.31f, .72f, .7f) };

            CreatePart("Coat", PrimitiveType.Capsule, debugRoot.transform, new Vector3(0, 0.9f, 0), new Vector3(.65f, .65f, .5f), coatMat);
            CreatePart("Head", PrimitiveType.Sphere, debugRoot.transform, new Vector3(0, 1.65f, 0), Vector3.one * .43f, headMat);
            CreatePart("Backpack", PrimitiveType.Cube, debugRoot.transform, new Vector3(0, 1.05f, -.35f), new Vector3(.5f, .6f, .25f), packMat);

            var blade = new GameObject("Sword pivot").transform;
            blade.SetParent(root, false);
            blade.localPosition = new Vector3(.45f, 1f, .1f);
            Blade = blade;
            CreatePart("Traveler sword", PrimitiveType.Cube, blade, new Vector3(0, 0, .65f), new Vector3(.09f, .15f, 1.4f), swordMat);

            var sail = CreatePart("Traveler / folding wind sail", PrimitiveType.Cube, root, new Vector3(0, 2f, -.4f), new Vector3(3.2f, .08f, 1.2f), sailMat);
            GlideSail = sail.transform;
        }

        static GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go;
            if(DevelopmentVisualMode.UseFallbackPrimitives) go=GameObject.CreatePrimitive(type);
            else
            {
                go=new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh=OriginalMesh.Loft(name,8,new[]{.4f,.5f,.42f},0,6,.04f);
                go.AddComponent<MeshRenderer>();
            }
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
