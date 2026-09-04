using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace WanderingCity
{
    public sealed class AudioDirector : MonoBehaviour
    {
        public WorldAudioLibrary Library;
        public AudioMixer Mixer { get; private set; }
        public AudioPreferences Preferences { get; private set; }
        public MusicStateMachine State { get; } = new MusicStateMachine();
        public readonly List<string> StateLog = new List<string>();
        public int MusicStarts { get; private set; }
        public string PreferencesPath { get; private set; }
        public float RainAmount;
        public AudioSource[] MusicSources { get; private set; }
        GameSession session;
        readonly List<AudioClip> owned = new List<AudioClip>();
        readonly Dictionary<AudioSource, float> ambienceLevels = new Dictionary<AudioSource, float>();
        readonly Dictionary<WorldSound, AudioClip> cues = new Dictionary<WorldSound, AudioClip>();
        AudioSource effects, ui, wind, meadow, forest, ruins, water, rain;
        MusicMood played = (MusicMood)(-1), observed = (MusicMood)(-1); string snapshot;
        int active, playlistIndex; float fade = 1, poll;
        readonly float[] fadeFrom = new float[2], musicGains = new float[2];
        TraversalState previousTraversal;
        public void Initialize(GameSession owner)
        {
            session = owner; Library = Resources.Load<WorldAudioLibrary>("WorldAudio"); Mixer = Resources.Load<AudioMixer>("WorldAudioMixer");
            PreferencesPath = Path.Combine(GameSession.SavePathOverride != null ? Path.GetDirectoryName(GameSession.SavePathOverride) : Application.persistentDataPath, "audio-settings.json");
            Preferences = AudioPreferences.Load(PreferencesPath);
            MusicSources = new[] { Source("Score A", "Music", false), Source("Score B", "Music", false) };
            effects = Source("World effects", "SFX", false); ui = Source("Interface", "UI", false);
            wind = Ambience("Wind"); meadow = Ambience("Meadow"); forest = Ambience("Forest"); ruins = Ambience("Ruins"); rain = Ambience("Rain"); water = Ambience("Water");
            foreach (WorldSound cue in Enum.GetValues(typeof(WorldSound))) { var clip = OriginalWorldAudio.Create(cue.ToString(), false); owned.Add(clip); cues[cue] = clip; }
            if (Mixer != null) Mixer.updateMode = AudioMixerUpdateMode.UnscaledTime;
        }
        AudioSource Source(string name, string group, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = loop; source.volume = 0; source.spatialBlend = 0;
            if (Mixer != null) { var groups = Mixer.FindMatchingGroups(group); foreach (var g in groups) if (g.name == group) { source.outputAudioMixerGroup = g; break; } }
            return source;
        }
        AudioSource Ambience(string name)
        {
            var source = Source(name, "Ambience", true); source.clip = OriginalWorldAudio.Create(name, true); owned.Add(source.clip); source.Play(); ambienceLevels[source] = 0; return source;
        }
        void Update()
        {
            if (session == null || session.Player == null) return;
            Tick(Time.unscaledDeltaTime);
        }
        public void Tick(float dt)
        {
            var p = session.Player; bool combat = false, elite = false;
            foreach (var enemy in session.World.Enemies)
                if (enemy != null && enemy.gameObject.activeInHierarchy && (enemy.Action == EnemyAction.Chase || enemy.Action == EnemyAction.Attack) && Vector3.Distance(p.transform.position, enemy.transform.position) < 28)
                { combat = true; elite |= enemy.Elite; }
            bool quiet = Physics.Raycast(p.transform.position + Vector3.up * 1.8f, Vector3.up, out var roof, 4, session.Balance.solidMask) && roof.collider.GetComponent<BuildingTag>() != null;
            State.Tick(dt, RegionAt(p.transform.position), session.Started, session.Paused, session.Hud.Page != "pause" && session.Hud.Page != "settings", combat, elite, quiet);
            if (observed != State.Current) { observed = State.Current; Log("state=" + observed); }
            if (snapshot != State.Snapshot)
            {
                snapshot = State.Snapshot; Mixer?.FindSnapshot(snapshot)?.TransitionTo(.7f); Log("snapshot=" + snapshot);
            }
            if (played != State.Current && fade >= 1) { played = State.Current; playlistIndex = 0; SelectTrack(); Log("music=" + played + (MusicSources[active].clip == null ? " (empty slot / silence)" : " / " + MusicSources[active].clip.name)); }
            fade = Mathf.Min(1, fade + dt / 2);
            for (int i = 0; i < 2; i++)
            {
                musicGains[i] = Mathf.Lerp(fadeFrom[i], i == active && MusicSources[i].clip != null ? 1 : 0, Mathf.SmoothStep(0, 1, fade));
                MusicSources[i].volume = musicGains[i] * Preferences.Music * Preferences.Master;
                if (i != active && fade >= 1 && MusicSources[i].isPlaying) MusicSources[i].Stop();
            }
            // Missing playlists are not polled/restarted. A completed valid clip advances once.
            poll += dt;
            if (poll > .5f) { poll = 0; var source = MusicSources[active]; if (fade >= 1 && source.clip != null && !source.isPlaying && AudioSettings.dspTime > 0) { playlistIndex++; SelectTrack(); } }
            float gain = Preferences.Master * Preferences.Ambience;
            void Mix(AudioSource source, float target) { ambienceLevels[source] = Mathf.MoveTowards(ambienceLevels[source], target, dt * .4f); source.volume = ambienceLevels[source] * gain; }
            Mix(wind, .45f + RainAmount * .25f); Mix(meadow, State.Region == MusicMood.Meadow ? .7f : 0); Mix(forest, State.Region == MusicMood.Forest ? .8f : 0); Mix(ruins, State.Region == MusicMood.Ruins ? .65f : 0);
            Mix(rain, RainAmount); Mix(water, Mathf.Clamp01(1 - Vector2.Distance(ExpansionCatalog.XZ(p.transform.position), WaterBody.Center) / 50));
            effects.volume = ui.volume = Preferences.Master * Preferences.SFX;
            if (session.Started && !session.Paused && p.Traversal.State != previousTraversal)
            { previousTraversal = p.Traversal.State; if (previousTraversal != TraversalState.Grounded && previousTraversal != TraversalState.Fall) Play(WorldSound.Traversal); }
        }
        public static MusicMood RegionAt(Vector3 p)
        {
            foreach (var r in ExpansionCatalog.Regions) if (Mathf.Abs(p.x - r.Center.x) < 92 && Mathf.Abs(p.z - r.Center.y) < 92)
                return r.Id.Contains("forest") ? MusicMood.Forest : r.Id.Contains("meadow") ? MusicMood.Meadow : MusicMood.Ruins;
            string region = WorldBuilder.Region(p); return region == "forest" ? MusicMood.Forest : region == "quarry" || region == "ruins" ? MusicMood.Ruins : MusicMood.Meadow;
        }
        void SelectTrack()
        {
            var list = Library != null ? Library.Playlist(State.Current) : null; AudioClip next = null;
            if (list != null && list.Length > 0) for (int i = 0; i < list.Length; i++) { var clip = list[(playlistIndex + i) % list.Length]; if (clip != null) { next = clip; break; } }
            if (next != null && MusicSources[active].clip == next && MusicSources[active].isPlaying) return;
            fadeFrom[0] = musicGains[0]; fadeFrom[1] = musicGains[1]; active = 1 - active; fade = 0;
            MusicSources[active].Stop(); MusicSources[active].clip = next;
            if (next != null) { MusicSources[active].Play(); MusicStarts++; }
        }
        void Log(string message) { if (StateLog.Count >= 128) StateLog.RemoveAt(0); StateLog.Add(Time.unscaledTime.ToString("F2") + " " + message); }
        public void Play(WorldSound sound, float pitch = 1)
        {
            if (!cues.TryGetValue(sound, out var clip)) return; var source = sound == WorldSound.UI ? ui : effects;
            source.volume = Preferences.Master * Preferences.SFX; source.pitch = Mathf.Clamp(pitch, .5f, 2); source.PlayOneShot(clip);
        }
        public void ReloadLibrary(WorldAudioLibrary library) { Library = library; played = (MusicMood)(-1); }
        public bool SavePreferences() { Preferences.Sanitize(); return Preferences.Save(PreferencesPath); }
        void OnDestroy() { foreach (var clip in owned) if (clip != null) Destroy(clip); }
    }
}
