using UnityEngine;

// Reutiliza hasta 48 trazadoras por arma. No producen dano ni colisiones.
public sealed class GrupoTrazadorasInstantaneas : System.IDisposable
{
    private const int Capacidad = 48;
    private readonly LineRenderer[] lineas = new LineRenderer[Capacidad];
    private readonly float[] restante = new float[Capacidad];
    private readonly float[] duraciones = new float[Capacidad];
    private readonly Vector3[] origenes = new Vector3[Capacidad];
    private readonly Vector3[] direcciones = new Vector3[Capacidad];
    private readonly float[] distancias = new float[Capacidad];
    private readonly float[] progreso = new float[Capacidad];
    private readonly float velocidadRecorrido;
    private readonly float longitudCola;
    private readonly int[] marcosGenerados = new int[Capacidad];
    private readonly Material material;
    private int siguiente;
    private readonly GameObject prefabricado;
    private readonly Color tono;

    public GrupoTrazadorasInstantaneas(GameObject prefabOverride = null)
    {
        // El prefab compartido tambien se incluye en las compilaciones del juego.
        prefabricado = prefabOverride != null ? prefabOverride : Resources.Load<GameObject>("WeaponTracer");
        var settings = prefabricado != null ? prefabricado.GetComponent<VisualTrazadoraArma>() : null;
        if (settings == null || settings.material == null)
        {
            Debug.LogWarning("Asigna un prefab con WeaponTracerVisual y su material.");
            return;
        }
        material = settings.material;
        tono = settings.color;
        velocidadRecorrido = Mathf.Max(1f, settings.velocidadRecorrido);
        longitudCola = Mathf.Max(0.01f, settings.longitudCola);
    }

    public void Emitir(Vector3 inicio, Vector3 fin, float ancho, float duracion)
    {
        if (material == null || (fin - inicio).sqrMagnitude < 0.000001f) return;
        int posicionArreglo = siguiente;
        siguiente = (siguiente + 1) % Capacidad;
        if (lineas[posicionArreglo] == null)
        {
            // Sin padre y en la capa Default: las paredes ocultan la trazadora.
            var instancia = Object.Instantiate(prefabricado);
            instancia.name = "TF2 shot tracer";
            instancia.layer = 0;
            var line = instancia.GetComponent<VisualTrazadoraArma>().Inicializar();
            lineas[posicionArreglo] = line;
        }
        var tracer = lineas[posicionArreglo];
        tracer.gameObject.SetActive(true);
        tracer.startWidth = Mathf.Max(0.001f, ancho);
        tracer.endWidth = tracer.startWidth * 0.65f;
        tracer.startColor = tono;
        tracer.endColor = tono;
        origenes[posicionArreglo] = inicio;
        distancias[posicionArreglo] = Vector3.Distance(inicio, fin);
        direcciones[posicionArreglo] = (fin - inicio) / distancias[posicionArreglo];
        // Una chispa corta sale de la boca; la estela crece mientras avanza.
        progreso[posicionArreglo] = Mathf.Min(distancias[posicionArreglo], longitudCola * 0.1f);
        ActualizarSegmento(posicionArreglo);
        restante[posicionArreglo] = duraciones[posicionArreglo] = Mathf.Max(0.01f, duracion);
        marcosGenerados[posicionArreglo] = Time.frameCount;
    }

    public void ActualizarPaso(float tiempoTranscurrido)
    {
        for (int i = 0; i < Capacidad; i++)
        {
            if (lineas[i] == null || !lineas[i].gameObject.activeSelf) continue;
            // Muestra al menos un fotograma, incluso con pocos FPS.
            if (marcosGenerados[i] == Time.frameCount) continue;
            if (tiempoTranscurrido <= 0f) continue;
            if (progreso[i] < distancias[i])
            {
                progreso[i] = Mathf.Min(distancias[i], progreso[i] + velocidadRecorrido * tiempoTranscurrido);
                ActualizarSegmento(i);
                // Conserva un fotograma del impacto incluso si baja la tasa de FPS.
                continue;
            }
            restante[i] -= tiempoTranscurrido;
            if (restante[i] <= 0f) { lineas[i].gameObject.SetActive(false); continue; }
            Color color = tono;
            color.a *= restante[i] / duraciones[i];
            lineas[i].startColor = color;
            lineas[i].endColor = color;
        }
    }

    private void ActualizarSegmento(int posicionArreglo)
    {
        float head = progreso[posicionArreglo];
        float tail = Mathf.Max(0f, head - longitudCola);
        lineas[posicionArreglo].SetPosition(0, origenes[posicionArreglo] + direcciones[posicionArreglo] * tail);
        lineas[posicionArreglo].SetPosition(1, origenes[posicionArreglo] + direcciones[posicionArreglo] * head);
    }

    public void Limpiar()
    {
        for (int i = 0; i < Capacidad; i++)
            if (lineas[i] != null) lineas[i].gameObject.SetActive(false);
    }

    public void Dispose()
    {
        for (int i = 0; i < Capacidad; i++)
            if (lineas[i] != null)
            {
                Object.Destroy(lineas[i].gameObject);
                lineas[i] = null;
            }
    }
}
