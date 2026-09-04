using System;
using System.IO;
using UnityEngine;

namespace WanderingCity
{
    [Serializable] public sealed class AudioPreferences
    {
        public int version = 1;
        public float Master = .8f, Music = .65f, Ambience = .65f, SFX = .8f;
        public void Sanitize()
        {
            float Clean(float value) => float.IsFinite(value) ? Mathf.Clamp01(value) : .8f;
            Master = Clean(Master); Music = Clean(Music); Ambience = Clean(Ambience); SFX = Clean(SFX);
        }
        public static AudioPreferences Load(string path)
        {
            try { var value = JsonUtility.FromJson<AudioPreferences>(File.ReadAllText(path)); if (value != null && value.version == 1) { value.Sanitize(); return value; } }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { }
            return new AudioPreferences();
        }
        public bool Save(string path)
        {
            try
            {
                Sanitize(); Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path + ".tmp", JsonUtility.ToJson(this, true));
                if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak"); else File.Move(path + ".tmp", path);
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return false; }
        }
    }
}
