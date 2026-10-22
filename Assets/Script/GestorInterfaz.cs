using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GestorInterfaz : MonoBehaviour
{
    public static GestorInterfaz Instancia;

    [SerializeField] private GameObject interfazImpacto;

    [SerializeField] private GameObject interfazMuerte;
    [Tooltip("Se crea automáticamente copiando la pantalla de Game Over para reutilizar su diseño y botón de reinicio.")]
    [SerializeField] private GameObject interfazVictoria;

    public TextMeshProUGUI textoMunicion;

    [SerializeField] private Image barraVida;
    [SerializeField] private Gradient gradienteVida;

    [Header("Dosier")]
    [SerializeField] private Image iconoDosier;
    [SerializeField] private TextMeshProUGUI mensajeDosier;

    [Header("Objetivos de misión")]
    private int enemigosEliminados;
    private int dosieresRecuperados;
    private TextMeshProUGUI textoObjetivos;
    private Coroutine mostrarBriefing;
    private bool briefingActivo;
    private int enemigosRequeridos = 25;
    private int dosieresRequeridos = 2;

    public int EnemigosEliminados => enemigosEliminados;

    private void Awake()
    {
        Time.timeScale = 1.0f;

        Instancia = this;
        bool escenaMision = SceneManager.GetActiveScene().name == "2Fort Playable";
        if (escenaMision)
        {
            CrearBotonMenuPrincipal(interfazMuerte);
            CrearInterfazVictoria();
            CrearBotonMenuPrincipal(interfazVictoria);
            CrearListaObjetivos();
            PuntoExtraccion extraccion = FindAnyObjectByType<PuntoExtraccion>();
            if (extraccion != null) ConfigurarObjetivos(extraccion.EnemigosRequeridos, extraccion.DosieresRequeridos);
            string briefing = MenuPrincipal.ConsumirMensajeMision();
            if (!string.IsNullOrEmpty(briefing))
            {
                briefingActivo = true;
                mostrarBriefing = StartCoroutine(MostrarBriefing(briefing));
            }
        }
        if (iconoDosier != null) iconoDosier.gameObject.SetActive(false);
        if (mensajeDosier != null) mensajeDosier.gameObject.SetActive(false);
    }

    public void ActualizarListaObjetivos()
    {
        RefrescarObjetivos();
    }

    public void ConfigurarObjetivos(int enemigos, int dosieres)
    {
        enemigosRequeridos = Mathf.Max(0, enemigos);
        dosieresRequeridos = Mathf.Max(0, dosieres);
        enemigosEliminados = Mathf.Min(enemigosEliminados, enemigosRequeridos);
        dosieresRecuperados = Mathf.Min(dosieresRecuperados, dosieresRequeridos);
        RefrescarObjetivos();
    }

    private System.Collections.IEnumerator MostrarBriefing(string briefing)
    {
        if (textoObjetivos == null) yield break;
        textoObjetivos.text = briefing;
        yield return new WaitForSecondsRealtime(4f);
        briefingActivo = false;
        RefrescarObjetivos();
        mostrarBriefing = null;
    }

    private void CrearListaObjetivos()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        GameObject panel = new GameObject("MissionObjectives", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(390f, 86f);
        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.035f, 0.045f, 0.06f, 0.35f);
        fondo.raycastTarget = false;

        GameObject textoObjeto = new GameObject("ObjectiveListText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(panel.transform, false);
        RectTransform textoRect = (RectTransform)textoObjeto.transform;
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(14f, 9f);
        textoRect.offsetMax = new Vector2(-10f, -8f);
        textoObjetivos = textoObjeto.GetComponent<TextMeshProUGUI>();
        textoObjetivos.font = TMP_Settings.defaultFontAsset;
        textoObjetivos.fontSize = 20f;
        textoObjetivos.color = Color.white;
        textoObjetivos.alignment = TextAlignmentOptions.Left;
        textoObjetivos.textWrappingMode = TextWrappingModes.NoWrap;
        RefrescarObjetivos();
    }

    public void RegistrarEnemigoEliminado()
    {
        if (enemigosRequeridos <= 0) return;
        enemigosEliminados = Mathf.Min(enemigosEliminados + 1, enemigosRequeridos);
        if (PuntoExtraccion.Instancia != null) PuntoExtraccion.Instancia.RegistrarEnemigoEliminado();
        RefrescarObjetivos();
    }

    public void RegistrarDosierRecuperado()
    {
        dosieresRecuperados = Mathf.Min(dosieresRecuperados + 1, dosieresRequeridos);
        RefrescarObjetivos();
    }

    private void RefrescarObjetivos()
    {
        if (textoObjetivos == null) return;
        if (briefingActivo) return;
        bool enemigosCompletados = enemigosEliminados >= enemigosRequeridos;
        bool dosieresCompletados = dosieresRecuperados >= dosieresRequeridos;
        if (enemigosCompletados && dosieresCompletados)
        {
            textoObjetivos.text = "<color=#7DE38D>• Ve al punto de extracción</color>";
            return;
        }

        string colorEnemigos = enemigosCompletados ? "#7DE38D" : "#FFFFFF";
        string colorDosieres = dosieresCompletados ? "#7DE38D" : "#FFFFFF";
        string objetivos = "";
        if (enemigosRequeridos > 0)
            objetivos = $"<color={colorEnemigos}>• Eliminar a todos los Enemigos {enemigosEliminados}/{enemigosRequeridos}</color>";
        if (dosieresRequeridos > 0)
        {
            if (!string.IsNullOrEmpty(objetivos)) objetivos += "\n";
            objetivos += $"<color={colorDosieres}>• Recuperar los Dosier {dosieresRecuperados}/{dosieresRequeridos}</color>";
        }
        textoObjetivos.text = objetivos;
        RectTransform panelRect = textoObjetivos.transform.parent as RectTransform;
        if (panelRect != null) panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, objetivos.Contains("\n") ? 86f : 54f);
    }

    public void MostrarIndicadorImpacto()
    {
        Instantiate(interfazImpacto, transform);
    }

    public void Reiniciar()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void VolverAlMenuPrincipal()
    {
        Time.timeScale = 1.0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Menu Principal");
    }

    public void IniciarMisionPrueba()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene("2Fort Playable");
    }

    public void SalirDelJuego()
    {
        Application.Quit();
    }

    public void MostrarInterfazMuerte()
    {
        OcultarInterfazJuego();
        if (interfazMuerte != null) interfazMuerte.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarInterfazVictoria()
    {
        OcultarInterfazJuego();
        if (interfazVictoria == null) CrearInterfazVictoria();
        if (interfazVictoria != null) interfazVictoria.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OcultarInterfazJuego()
    {
        if (barraVida != null)
        {
            // In the current HUD the bar is inside its own background panel.
            GameObject contenedorVida = barraVida.transform.parent != null
                ? barraVida.transform.parent.gameObject
                : barraVida.gameObject;
            contenedorVida.SetActive(false);
        }
        if (textoMunicion != null) textoMunicion.gameObject.SetActive(false);
    }

    private void CrearInterfazVictoria()
    {
        if (interfazVictoria != null || interfazMuerte == null) return;
        Transform parent = interfazMuerte.transform.parent;
        interfazVictoria = Instantiate(interfazMuerte, parent);
        interfazVictoria.name = "VictoryUi";
        interfazVictoria.SetActive(false);
        TMP_Text titulo = interfazVictoria.GetComponentInChildren<TMP_Text>(true);
        if (titulo != null) titulo.text = "VICTORIA";
        CrearBotonMenuPrincipal(interfazVictoria);
    }

    private void CrearBotonMenuPrincipal(GameObject panel)
    {
        if (panel == null) return;

        Button botonReinicio = panel.GetComponentInChildren<Button>(true);
        if (botonReinicio == null) return;

        RectTransform reinicio = botonReinicio.GetComponent<RectTransform>();
        if (reinicio != null) reinicio.anchoredPosition = new Vector2(reinicio.anchoredPosition.x, 42f);

        Transform existente = panel.transform.Find("MainMenuBTN");
        GameObject botonMenu = existente != null
            ? existente.gameObject
            : Instantiate(botonReinicio.gameObject, botonReinicio.transform.parent);
        botonMenu.name = "MainMenuBTN";
        RectTransform menuRect = botonMenu.GetComponent<RectTransform>();
        if (menuRect != null) menuRect.anchoredPosition = new Vector2(menuRect.anchoredPosition.x, -42f);
        TMP_Text textoBotonMenu = botonMenu.GetComponentInChildren<TMP_Text>(true);
        if (textoBotonMenu != null) textoBotonMenu.text = "MENU PRINCIPAL";
        Button botonMenuComponente = botonMenu.GetComponent<Button>();
        botonMenuComponente.onClick.RemoveAllListeners();
        botonMenuComponente.onClick.AddListener(VolverAlMenuPrincipal);
    }

    public void ActualizarVida(int vida, int vidaMaxima = 100)
    {
        if (barraVida == null) return;
        float vidaProporcional = Mathf.Clamp01((float)vida / Mathf.Max(1, vidaMaxima));
        if (gradienteVida == null) return;
        barraVida.color = gradienteVida.Evaluate(vidaProporcional);
        barraVida.fillAmount = vidaProporcional;

    }

    public bool MostrarMensajeDosier(Sprite icono, string mensaje, float duracion)
    {
        bool mostrado = false;
        if (iconoDosier != null)
        {
            iconoDosier.sprite = icono;
            iconoDosier.gameObject.SetActive(icono != null);
            mostrado = icono != null;
        }
        if (mensajeDosier != null)
        {
            mensajeDosier.text = mensaje;
            mensajeDosier.gameObject.SetActive(!string.IsNullOrWhiteSpace(mensaje));
            CancelInvoke(nameof(OcultarMensajeDosier));
            if (duracion > 0f) Invoke(nameof(OcultarMensajeDosier), duracion);
            mostrado |= !string.IsNullOrWhiteSpace(mensaje);
        }
        return mostrado;
    }

    private void OcultarMensajeDosier()
    {
        if (mensajeDosier != null) mensajeDosier.gameObject.SetActive(false);
    }
}

