using UnityEngine;

/// <summary>On-screen pickup message fallback for scenes without a configured UiManager.</summary>
public sealed class DosierAvisoRecogida : MonoBehaviour
{
    private static DosierAvisoRecogida instancia;
    private string mensaje;
    private Sprite icono;
    private float ocultarEn;

    public static void Mostrar(Sprite iconoRecogida, string mensajeRecogida, float duracion)
    {
        if (instancia == null)
        {
            var objetoAnfitrion = new GameObject("DosierPickupNotice");
            DontDestroyOnLoad(objetoAnfitrion);
            instancia = objetoAnfitrion.AddComponent<DosierAvisoRecogida>();
        }
        instancia.icono = iconoRecogida;
        instancia.mensaje = mensajeRecogida;
        instancia.ocultarEn = Time.unscaledTime + Mathf.Max(0.1f, duracion);
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(mensaje) || Time.unscaledTime >= ocultarEn) return;

        const float ancho = 380f;
        const float height = 86f;
        Rect panel = new Rect((Screen.width - ancho) * 0.5f, Screen.height * 0.78f, ancho, height);
        GUI.Box(panel, GUIContent.none);
        if (icono != null)
            GUI.DrawTexture(new Rect(panel.x + 14f, panel.y + 13f, 60f, 60f), icono.texture, ScaleMode.ScaleToFit, true);

        var estilo = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        estilo.normal.textColor = Color.white;
        float izquierda = icono != null ? 88f : 20f;
        GUI.Label(new Rect(panel.x + izquierda, panel.y + 8f, ancho - izquierda - 14f, height - 16f), mensaje, estilo);
    }
}
