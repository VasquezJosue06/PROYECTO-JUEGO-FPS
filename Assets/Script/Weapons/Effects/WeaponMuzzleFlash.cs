using UnityEngine;

// Sigue la boca sin heredar la escala ni el giro de los huesos exportados.
[DefaultExecutionOrder(100)]
public sealed class WeaponMuzzleFlash : MonoBehaviour
{
    public Material material;
    [Min(0.01f)] public float size = 0.32f;
    [Min(0.01f)] public float duration = 0.12f;
    private Transform muzzle;
    private Light flashLight;
    private float intensity;
    private float elapsed;

    public void Initialize(Transform source, int layer)
    {
        muzzle = source;
        gameObject.layer = layer;
        transform.position = source.position;
        flashLight = GetComponent<Light>();
        if (flashLight != null)
        {
            intensity = flashLight.intensity;
            flashLight.cullingMask = 1 << layer;
        }
        var particles = gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = duration;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startRotation = Random.Range(0f, Mathf.PI * 2f);
        main.startColor = new Color(1f, 0.72f, 0.25f, 1f);
        main.maxParticles = 1;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var fade = particles.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        particles.Emit(1);
    }

    private void LateUpdate()
    {
        if (muzzle == null || !muzzle.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
        transform.position = muzzle.position;
        elapsed += Time.deltaTime;
        if (flashLight != null) flashLight.intensity = intensity * Mathf.Clamp01(1f - elapsed / duration);
        if (elapsed >= duration) Destroy(gameObject);
    }
}
