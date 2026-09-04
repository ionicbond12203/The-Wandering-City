using UnityEngine;

namespace WanderingCity
{
    // Project-owned sound design, not a musical score. Deterministic filtered noise and short chirps.
    public static class OriginalWorldAudio
    {
        public static AudioClip Create(string kind, bool loop)
        {
            const int rate = 22050; int count = loop ? rate * 8 : rate / 3;
            var samples = new float[count]; uint seed = 731; float low = 0;
            for (int i = 0; i < count; i++)
            {
                seed = seed * 1664525 + 1013904223; float noise = ((seed >> 8) / 8388607f - 1);
                low = Mathf.Lerp(low, noise, kind == "Rain" || kind == "Water" ? .18f : .035f);
                float t = i / (float)rate, value;
                if (loop)
                {
                    value = low * (kind == "Rain" ? .42f : kind == "Water" ? .5f : .28f);
                    if (kind == "Meadow" || kind == "Forest")
                    {
                        float phase = Mathf.Repeat(t + (kind == "Forest" ? 1.4f : 0), 2.6f);
                        if (phase < .22f) value += Mathf.Sin(t * (1800 + phase * 3200) * Mathf.PI * 2) * Mathf.Sin(phase / .22f * Mathf.PI) * .06f;
                    }
                    // Silence at both ends makes the bounded ambience buffer seam click-free.
                    value *= Mathf.Min(1, Mathf.Min(t, 8 - t) * 8);
                }
                else
                {
                    float u = i / (float)count, envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 12)) * Mathf.Pow(1 - u, 3);
                    // Fast attack, exponential tail; avoid a hard sample edge.
                    envelope = Mathf.Min(1, u * 60) * Mathf.Pow(1 - u, 3);
                    float frequency = kind == "UI" ? 700 : kind == "Pickup" ? 1050 : kind == "Teleport" ? 420 + u * 1200 : 150;
                    value = (kind == "Attack" || kind == "Traversal" || kind == "Hit" ? noise * .28f : Mathf.Sin(t * frequency * Mathf.PI * 2) * .18f) * envelope;
                }
                samples[i] = value;
            }
            var clip = AudioClip.Create("Original sound design / " + kind, count, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
    }
}
