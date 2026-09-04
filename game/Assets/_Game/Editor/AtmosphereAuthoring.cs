using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace WanderingCity.Editor
{
    public static class AtmosphereAuthoring
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static Type EditorType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio." + name)).First(t => t != null);
        static object Invoke(object owner, string method, params object[] args) => owner.GetType().GetMethods(Flags).First(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(owner, args);
        static object Get(object owner, string name) => owner.GetType().GetProperty(name, Flags).GetValue(owner);
        static void Set(object owner, string name, object value) => owner.GetType().GetProperty(name, Flags).SetValue(owner, value);
        public static void Ensure()
        {
            if (Resources.Load<WorldAudioLibrary>("WorldAudio") == null) AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WorldAudioLibrary>(), "Assets/_Game/Resources/WorldAudio.asset");
            var profile = Resources.Load<WorldAtmosphere>("WorldAtmosphere");
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<WorldAtmosphere>();
                var water = new Material(Shader.Find("WanderingCity/StylizedWater")) { name = "Original shallow pond" };
                AssetDatabase.CreateAsset(water, "Assets/_Game/Art/Materials/WorldWater.mat"); profile.WaterMaterial = water;
                var rain = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Light rain streaks" };
                rain.SetFloat("_Surface", 1); rain.SetFloat("_Blend", 0); rain.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); rain.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); rain.SetFloat("_ZWrite", 0); rain.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); rain.renderQueue = 3000;
                rain.SetColor("_BaseColor", new Color(.7f, .82f, .9f, .5f));
                AssetDatabase.CreateAsset(rain, "Assets/_Game/Art/Materials/WorldRain.mat"); profile.RainMaterial = rain;
                AssetDatabase.CreateAsset(profile, "Assets/_Game/Resources/WorldAtmosphere.asset");
            }
            EnsureMixer(); AssetDatabase.SaveAssets();
        }
        static void EnsureMixer()
        {
            const string path = "Assets/_Game/Resources/WorldAudioMixer.mixer";
            if (AssetDatabase.LoadAssetAtPath<AudioMixer>(path) != null) return;
            // Unity has no public editor mixer-creation API. Isolate version-specific reflection here;
            // the committed mixer is a normal asset and runtime code uses only public APIs.
            var type = EditorType("AudioMixerController");
            var mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", Flags).Invoke(null, new object[] { path });
            var master = Get(mixer, "masterGroup");
            var groups = new System.Collections.Generic.Dictionary<string, object> { ["Master"] = master };
            foreach (var name in new[] { "Music", "Ambience", "SFX", "UI" })
            { var group = Invoke(mixer, "CreateNewGroup", name, false); Invoke(mixer, "AddChildToParent", group, master); groups[name] = group; }
            var first = (Object)((Array)Get(mixer, "snapshots")).GetValue(0); first.name = "Exploration";
            var snapshotType = first.GetType(); var snapshots = Array.CreateInstance(snapshotType, 5); snapshots.SetValue(first, 0);
            string[] names = { "Exploration", "Combat", "Menu", "Interior/Quiet", "Paused" };
            for (int i = 1; i < names.Length; i++)
            { var snapshot = (Object)Activator.CreateInstance(snapshotType, new object[] { mixer }); snapshot.name = names[i]; AssetDatabase.AddObjectToAsset(snapshot, mixer); snapshots.SetValue(snapshot, i); }
            Set(mixer, "snapshots", snapshots); Set(mixer, "startSnapshot", first);
            for (int i = 0; i < names.Length; i++) foreach (var group in groups)
            {
                float gain = 0;
                if (i == 1 && group.Key == "Ambience") gain = -6;
                if (i == 2) gain = group.Key == "Ambience" ? -12 : group.Key == "SFX" ? -10 : 0;
                if (i == 3) gain = group.Key == "Ambience" ? -9 : group.Key == "Music" ? -3 : 0;
                if (i == 4) gain = group.Key == "SFX" ? -80 : group.Key == "Ambience" ? -18 : group.Key == "Music" ? -12 : 0;
                Invoke(group.Value, "SetValueForVolume", mixer, snapshots.GetValue(i), gain);
                EditorUtility.SetDirty((Object)group.Value); EditorUtility.SetDirty((Object)snapshots.GetValue(i));
            }
            EditorUtility.SetDirty(mixer);
        }
    }
}
