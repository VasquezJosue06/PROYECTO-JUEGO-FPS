using UnityEngine;

// Reutiliza hasta 48 trazadoras por arma. No producen dano ni colisiones.
public sealed class HitscanTracerPool : System.IDisposable
{
    private const int Capacity = 48;
    private readonly LineRenderer[] lines = new LineRenderer[Capacity];
    private readonly float[] remaining = new float[Capacity];
    private readonly float[] lifetimes = new float[Capacity];
    private readonly Vector3[] origins = new Vector3[Capacity];
    private readonly Vector3[] directions = new Vector3[Capacity];
    private readonly float[] distances = new float[Capacity];
    private readonly float[] progress = new float[Capacity];
    private readonly float travelSpeed;
    private readonly float tailLength;
    private readonly int[] spawnedFrames = new int[Capacity];
    private readonly Material material;
    private int next;
    private readonly GameObject prefab;
    private readonly Color tint;

    public HitscanTracerPool(GameObject prefabOverride = null)
    {
        // El prefab compartido tambien se incluye en las compilaciones del juego.
        prefab = prefabOverride != null ? prefabOverride : Resources.Load<GameObject>("WeaponTracer");
        var settings = prefab != null ? prefab.GetComponent<WeaponTracerVisual>() : null;
        if (settings == null || settings.material == null)
        {
            Debug.LogWarning("Asigna un prefab con WeaponTracerVisual y su material.");
            return;
        }
        material = settings.material;
        tint = settings.color;
        travelSpeed = Mathf.Max(1f, settings.travelSpeed);
        tailLength = Mathf.Max(0.01f, settings.tailLength);
    }

    public void Emit(Vector3 start, Vector3 end, float width, float lifetime)
    {
        if (material == null || (end - start).sqrMagnitude < 0.000001f) return;
        int slot = next;
        next = (next + 1) % Capacity;
        if (lines[slot] == null)
        {
            // Sin padre y en la capa Default: las paredes ocultan la trazadora.
            var instance = Object.Instantiate(prefab);
            instance.name = "TF2 shot tracer";
            instance.layer = 0;
            var line = instance.GetComponent<WeaponTracerVisual>().Initialize();
            lines[slot] = line;
        }
        var tracer = lines[slot];
        tracer.gameObject.SetActive(true);
        tracer.startWidth = Mathf.Max(0.001f, width);
        tracer.endWidth = tracer.startWidth * 0.65f;
        tracer.startColor = tint;
        tracer.endColor = tint;
        origins[slot] = start;
        distances[slot] = Vector3.Distance(start, end);
        directions[slot] = (end - start) / distances[slot];
        // Una chispa corta sale de la boca; la estela crece mientras avanza.
        progress[slot] = Mathf.Min(distances[slot], tailLength * 0.1f);
        UpdateSegment(slot);
        remaining[slot] = lifetimes[slot] = Mathf.Max(0.01f, lifetime);
        spawnedFrames[slot] = Time.frameCount;
    }

    public void Tick(float deltaTime)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (lines[i] == null || !lines[i].gameObject.activeSelf) continue;
            // Muestra al menos un fotograma, incluso con pocos FPS.
            if (spawnedFrames[i] == Time.frameCount) continue;
            if (deltaTime <= 0f) continue;
            if (progress[i] < distances[i])
            {
                progress[i] = Mathf.Min(distances[i], progress[i] + travelSpeed * deltaTime);
                UpdateSegment(i);
                // Conserva un fotograma del impacto incluso si baja la tasa de FPS.
                continue;
            }
            remaining[i] -= deltaTime;
            if (remaining[i] <= 0f) { lines[i].gameObject.SetActive(false); continue; }
            Color color = tint;
            color.a *= remaining[i] / lifetimes[i];
            lines[i].startColor = color;
            lines[i].endColor = color;
        }
    }

    private void UpdateSegment(int slot)
    {
        float head = progress[slot];
        float tail = Mathf.Max(0f, head - tailLength);
        lines[slot].SetPosition(0, origins[slot] + directions[slot] * tail);
        lines[slot].SetPosition(1, origins[slot] + directions[slot] * head);
    }

    public void Clear()
    {
        for (int i = 0; i < Capacity; i++)
            if (lines[i] != null) lines[i].gameObject.SetActive(false);
    }

    public void Dispose()
    {
        for (int i = 0; i < Capacity; i++)
            if (lines[i] != null)
            {
                Object.Destroy(lines[i].gameObject);
                lines[i] = null;
            }
    }
}
