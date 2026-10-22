using UnityEngine;

// Sigue la boca sin heredar la escala ni el giro de los huesos exportados.
[DefaultExecutionOrder(100)]
public sealed class FogonazoArma : MonoBehaviour
{
    public Material material;
    [Min(0.01f)] [SerializeField] private float tamano = 0.32f;
    [Min(0.01f)] [SerializeField] private float duracion = 0.12f;
    private Transform bocaArma;
    private Light luzFogonazo;
    private float intensidad;
    private float transcurrido;

    public void Inicializar(Transform origen, int capa)
    {
        bocaArma = origen;
        gameObject.layer = capa;
        transform.position = origen.position;
        luzFogonazo = GetComponent<Light>();
        if (luzFogonazo != null)
        {
            intensidad = luzFogonazo.intensity;
            luzFogonazo.cullingMask = 1 << capa;
        }
        var particles = gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = duracion;
        main.startSpeed = 0f;
        main.startSize = tamano;
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
        if (bocaArma == null || !bocaArma.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
        transform.position = bocaArma.position;
        transcurrido += Time.deltaTime;
        if (luzFogonazo != null) luzFogonazo.intensity = intensidad * Mathf.Clamp01(1f - transcurrido / duracion);
        if (transcurrido >= duracion) Destroy(gameObject);
    }
}
