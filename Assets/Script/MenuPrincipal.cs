using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class MenuPrincipal : MonoBehaviour
{
    private Coroutine transicion;

    public void IniciarMisionPrueba()
    {
        if (transicion == null) transicion = StartCoroutine(PrepararMision());
    }

    public void SalirDelJuego()
    {
        Application.Quit();
    }

    public static string ConsumirMensajeMision()
    {
        string mensaje = PlayerPrefs.GetString("BriefingMisionPendiente", "");
        PlayerPrefs.DeleteKey("BriefingMisionPendiente");
        PlayerPrefs.Save();
        return mensaje;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RestablecerMensajePendiente()
    {
    }

    private IEnumerator PrepararMision()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var panel = new GameObject("MissionBriefing", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(Image), typeof(CanvasGroup));
        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Canvas overlayCanvas = panel.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 1000;
        CanvasScaler scaler = panel.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1024, 768);
        DontDestroyOnLoad(panel);
        // Keep the coroutine alive after the menu scene unloads during scene loading.
        DontDestroyOnLoad(gameObject);
        var fondo = panel.GetComponent<Image>();
        fondo.color = Color.black;
        fondo.raycastTarget = true;

        var textoObjeto = new GameObject("BriefingText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(panel.transform, false);
        var textoRect = (RectTransform)textoObjeto.transform;
        textoRect.anchorMin = new Vector2(0.12f, 0.2f);
        textoRect.anchorMax = new Vector2(0.88f, 0.8f);
        textoRect.offsetMin = Vector2.zero;
        textoRect.offsetMax = Vector2.zero;

        TextMeshProUGUI texto = textoObjeto.GetComponent<TextMeshProUGUI>();
        texto.font = TMP_Settings.defaultFontAsset;
        texto.fontSize = 30;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Left;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.text = "";
        texto.maxVisibleCharacters = 0;

        const string mensaje = "Mercenario,\n\nMis informantes confirman que los enemigos esconden dos dosieres en la zona. Elimínalos a todos y recupera ambos documentos.\n\nCumple el encargo y tu recompensa será generosa. No me decepciones.";
        texto.text = mensaje;
        float duracionEscritura = Mathf.Clamp(mensaje.Length * 0.045f, 6f, 14f);
        float transcurrido = 0f;
        while (transcurrido < duracionEscritura)
        {
            transcurrido += Time.unscaledDeltaTime;
            texto.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(mensaje.Length * transcurrido / duracionEscritura), 0, mensaje.Length);
            yield return null;
        }

        texto.maxVisibleCharacters = mensaje.Length;
        yield return new WaitForSecondsRealtime(1.6f);

        PlayerPrefs.SetString("BriefingMisionPendiente", "Briefing recibido");
        PlayerPrefs.Save();
        var grupo = panel.GetComponent<CanvasGroup>();
        grupo.alpha = 1f;
        AsyncOperation carga = SceneManager.LoadSceneAsync("2Fort Playable");
        while (!carga.isDone) yield return null;

        transcurrido = 0f;
        while (transcurrido < 0.8f)
        {
            transcurrido += Time.unscaledDeltaTime;
            grupo.alpha = 1f - Mathf.Clamp01(transcurrido / 0.8f);
            yield return null;
        }
        Destroy(panel);
        Destroy(gameObject);
    }
}
