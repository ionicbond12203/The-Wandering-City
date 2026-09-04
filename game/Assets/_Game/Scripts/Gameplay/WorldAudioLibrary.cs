using UnityEngine;

namespace WanderingCity
{
    public enum MusicMood { Menu, Meadow, Forest, Ruins, Combat, Elite }
    public enum WorldSound { UI, Pickup, Attack, Hit, Teleport, Traversal, Interaction }
    [CreateAssetMenu(menuName = "Wandering City/World audio library")]
    public sealed class WorldAudioLibrary : ScriptableObject
    {
        [Tooltip("Empty slots intentionally produce silence. Populate only with licensed scores.")]
        public AudioClip[] Menu = new AudioClip[0], Meadow = new AudioClip[0], Forest = new AudioClip[0], Ruins = new AudioClip[0], Combat = new AudioClip[0], Elite = new AudioClip[0];
        public AudioClip[] Playlist(MusicMood mood) => mood == MusicMood.Menu ? Menu : mood == MusicMood.Forest ? Forest : mood == MusicMood.Ruins ? Ruins : mood == MusicMood.Combat ? Combat : mood == MusicMood.Elite ? Elite : Meadow;
    }
    public sealed class MusicStateMachine
    {
        public MusicMood Current { get; private set; } = MusicMood.Menu;
        public MusicMood Region { get; private set; } = MusicMood.Meadow;
        public string Snapshot { get; private set; } = "Menu";
        MusicMood candidate = MusicMood.Meadow;
        float regionTime, combatHold, eliteHold;
        public void Tick(float dt, MusicMood region, bool started, bool paused, bool menu, bool combat, bool elite, bool quiet)
        {
            dt = Mathf.Max(0, dt);
            if (!started) { Current = MusicMood.Menu; Snapshot = "Menu"; combatHold = eliteHold = 0; return; }
            if (paused) { Snapshot = menu ? "Menu" : "Paused"; return; }
            if (region != candidate) { candidate = region; regionTime = 0; }
            regionTime += dt; if (regionTime >= 3) Region = candidate;
            combatHold = combat ? 5 : Mathf.Max(0, combatHold - dt);
            eliteHold = elite ? 5 : Mathf.Max(0, eliteHold - dt);
            Current = eliteHold > 0 ? MusicMood.Elite : combatHold > 0 ? MusicMood.Combat : Region;
            Snapshot = combatHold > 0 || eliteHold > 0 ? "Combat" : quiet ? "Interior/Quiet" : "Exploration";
        }
    }
}
