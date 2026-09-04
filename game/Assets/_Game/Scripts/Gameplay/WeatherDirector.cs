using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WanderingCity
{
    public sealed class WeatherDirector : MonoBehaviour
    {
        public WeatherBlend Weather { get; } = new WeatherBlend();
        public WorldAtmosphere Profile { get; private set; }
        public ParticleSystem Rain { get; private set; }
        public bool Automatic = true;
        GameSession session; Material sky, originalSky; Light sun; float age; int cycle;
        Volume volume; VolumeProfile originalVolume, volumeCopy;
        public void Initialize(GameSession owner)
        {
            session = owner; Profile = Resources.Load<WorldAtmosphere>("WorldAtmosphere");
            if (Profile == null) { enabled = false; return; }
            Automatic = Profile.AutomaticWeather;
            originalSky = RenderSettings.skybox; if (originalSky != null) { sky = new Material(originalSky); RenderSettings.skybox = sky; }
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (light.type == LightType.Directional) { sun = light; break; }
            volume = FindFirstObjectByType<Volume>();
            if (volume != null)
            {
                originalVolume = volume.sharedProfile; volumeCopy = volume.profile;
                if (volumeCopy.TryGet<ColorAdjustments>(out var color)) { color.saturation.Override(3); color.contrast.Override(6); color.postExposure.Override(.05f); }
                if (volumeCopy.TryGet<Bloom>(out var bloom)) { bloom.intensity.Override(.16f); bloom.threshold.Override(1.1f); }
            }
            var water = new GameObject("Water"); water.transform.SetParent(transform, false); water.AddComponent<WaterBody>().Create(Profile.WaterMaterial);
            var rainObject = new GameObject("Local light rain"); rainObject.transform.SetParent(transform, false); Rain = rainObject.AddComponent<ParticleSystem>(); Rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = Rain.main; main.loop = true; main.startLifetime = 1.4f; main.startSpeed = 13; main.startSize = .015f; main.maxParticles = 700; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startColor = new Color(.74f, .86f, .94f, .55f);
            var emission = Rain.emission; emission.rateOverTime = 0;
            var shape = Rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(22, 18, .1f);
            var renderer = Rain.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.lengthScale = 5; renderer.velocityScale = .035f; renderer.sharedMaterial = Profile.RainMaterial; renderer.shadowCastingMode = ShadowCastingMode.Off;
            Rain.transform.rotation = Quaternion.Euler(90, 0, 0); Rain.useAutoRandomSeed = false; Rain.randomSeed = 508; Rain.Play(); Apply();
        }
        public void Request(WeatherKind kind) { Weather.Request(kind, Profile.TransitionSeconds); age = 0; }
        void Update() { if (session != null && session.Started && !session.Paused) Tick(Time.deltaTime); }
        public void Tick(float dt)
        {
            if (Profile == null) return;
            age += dt;
            float hold = Weather.Target == WeatherKind.Clear ? Profile.ClearSeconds : Weather.Target == WeatherKind.Cloudy ? Profile.CloudySeconds : Profile.RainSeconds;
            if (Automatic && age > hold) { cycle = (cycle + 1) % 4; Request(cycle == 0 ? WeatherKind.Clear : cycle == 2 ? WeatherKind.LightRain : WeatherKind.Cloudy); }
            Weather.Tick(dt); Apply();
        }
        void Apply()
        {
            var w = Weather.Weights; float cloud = w.y + w.z, rain = w.z;
            if (sky != null) { sky.SetFloat("_Coverage", .35f + cloud * .43f); sky.SetFloat("_Exposure", 1 - cloud * .13f); sky.SetColor("_Zenith", Color.Lerp(new Color(.12f, .36f, .66f), new Color(.3f, .4f, .5f), cloud)); sky.SetColor("_Horizon", Color.Lerp(new Color(.72f, .84f, .86f), new Color(.58f, .66f, .7f), cloud)); sky.SetFloat("_Rain", rain); }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .00135f + cloud * .0007f + rain * .001f;
            RenderSettings.fogColor = Color.Lerp(new Color(.68f, .82f, .88f), new Color(.55f, .64f, .69f), cloud);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(.72f, .82f, .9f), new Color(.57f, .65f, .73f), cloud);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.58f, .68f, .62f), new Color(.48f, .55f, .58f), cloud);
            RenderSettings.ambientGroundColor = new Color(.3f, .34f, .3f);
            if (sun != null) { sun.intensity = Mathf.Lerp(1.15f, .72f, cloud); sun.color = Color.Lerp(new Color(1, .92f, .78f), new Color(.84f, .9f, 1), cloud); }
            if (session.Audio != null) session.Audio.RainAmount = rain;
            if (Rain != null && session.Player != null)
            {
                Rain.transform.position = session.Player.transform.position + Vector3.up * 12;
                bool indoors = Physics.Raycast(session.Player.transform.position + Vector3.up * 1.8f, Vector3.up, 18, session.Balance.solidMask);
                var emission = Rain.emission; emission.rateOverTime = indoors ? 0 : 350 * rain;
            }
        }
        void OnDestroy()
        {
            if (sky != null) { if (RenderSettings.skybox == sky) RenderSettings.skybox = originalSky; Destroy(sky); }
            if (volumeCopy != null) { if (volume != null) volume.sharedProfile = originalVolume; foreach (var component in volumeCopy.components) Destroy(component); Destroy(volumeCopy); }
        }
    }
}
