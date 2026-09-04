using UnityEngine;

namespace WanderingCity
{
    public enum WeatherKind { Clear, Cloudy, LightRain }
    [CreateAssetMenu(menuName = "Wandering City/World atmosphere")]
    public sealed class WorldAtmosphere : ScriptableObject
    {
        public float TransitionSeconds = 18;
        public bool AutomaticWeather = true;
        public float ClearSeconds = 180, CloudySeconds = 100, RainSeconds = 75;
        public Material WaterMaterial, RainMaterial;
    }
    public sealed class WeatherBlend
    {
        public WeatherKind Target { get; private set; }
        public Vector3 Weights { get; private set; } = Vector3.right;
        Vector3 start = Vector3.right; float age, duration;
        public void Request(WeatherKind kind, float seconds)
        { Target = kind; start = Weights; age = 0; duration = Mathf.Max(.01f, seconds); }
        public void Tick(float dt)
        {
            age += Mathf.Max(0, dt); var target = Target == WeatherKind.Clear ? Vector3.right : Target == WeatherKind.Cloudy ? Vector3.up : Vector3.forward;
            Weights = Vector3.Lerp(start, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / Mathf.Max(.01f, duration))));
        }
    }
}
