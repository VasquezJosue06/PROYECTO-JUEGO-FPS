using UnityEngine;

/// <summary>Mission extraction point that accepts and places recovered dossiers.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class PuntoExtraccion : MonoBehaviour
{
    [Header("Punto de extracción")]
    [Range(0, 50)]
    [Tooltip("Cantidad de enemigos que hay que eliminar para completar este objetivo.")]
    public int enemigosRequeridos = 25;
    [Range(0, 6)]
    [Tooltip("Cantidad de dosieres que se deben entregar en este punto de extracción.")]
    public int dosieresRequeridos = 4;
    [Tooltip("Cada punto es el espacio donde se coloca el siguiente dosier entregado.")]
    public Transform[] puntosEntrega = new Transform[4];
    [TextArea] [SerializeField] private string mensajeEntrega = "Dosier entregado";
    [Min(0f)] [SerializeField] private float duracionMensaje = 4f;
    [Min(0.1f)] [SerializeField] private float tamanoMarcador = 0.75f;
    private int siguientePuntoEntrega;
    private bool victoriaActivada;
    private bool extraccionDesbloqueada;

    public static PuntoExtraccion Instancia { get; private set; }
    public int EnemigosRequeridos => enemigosRequeridos;
    public int DosieresRequeridos => dosieresRequeridos;

    private void Awake()
    {
        dosieresRequeridos = Mathf.Clamp(dosieresRequeridos, 0, 6);
        enemigosRequeridos = Mathf.Clamp(enemigosRequeridos, 0, 50);
        Instancia = this;
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Start()
    {
        if (GestorInterfaz.Instancia != null) GestorInterfaz.Instancia.ConfigurarObjetivos(enemigosRequeridos, dosieresRequeridos);
        EvaluarProgresoMision();
    }

    private void Reset()
    {
        BoxCollider activador = GetComponent<BoxCollider>();
        activador.isTrigger = true;
        activador.size = new Vector3(6f, 4f, 6f);
    }

    private void OnTriggerEnter(Collider otro)
    {
        IntentarEntregar(otro);
        ComprobarLlegadaExtraccion(otro);
    }

    private void OnTriggerStay(Collider otro)
    {
        IntentarEntregar(otro);
        ComprobarLlegadaExtraccion(otro);
    }

    private void IntentarEntregar(Collider otro)
    {
        if (dosieresRequeridos < 1 || extraccionDesbloqueada || victoriaActivada) return;
        if (otro.GetComponentInParent<MovimientoJugador>() == null) return;
        DosierRecogida dosier = DosierRecogida.DosierTransportado;
        if (dosier == null || !dosier.EstaTransportado) return;
        if (puntosEntrega == null || siguientePuntoEntrega >= dosieresRequeridos || siguientePuntoEntrega >= puntosEntrega.Length) return;
        Transform punto = puntosEntrega[siguientePuntoEntrega];
        if (punto == null) return;

        dosier.EntregarEn(punto.position);
        siguientePuntoEntrega++;
        if (GestorInterfaz.Instancia != null) GestorInterfaz.Instancia.RegistrarDosierRecuperado();
        Sprite icono = DosierIcono.Crear();
        bool mostradoPorInterfaz = GestorInterfaz.Instancia != null &&
            GestorInterfaz.Instancia.MostrarMensajeDosier(icono, mensajeEntrega, duracionMensaje);
        if (!mostradoPorInterfaz) DosierAvisoRecogida.Mostrar(icono, mensajeEntrega, duracionMensaje);
        EvaluarProgresoMision();
        ComprobarLlegadaExtraccion(otro);
    }

    public void RegistrarEnemigoEliminado()
    {
        EvaluarProgresoMision();
    }

    private void EvaluarProgresoMision()
    {
        bool enemigosListos = enemigosRequeridos == 0 ||
            (GestorInterfaz.Instancia != null && GestorInterfaz.Instancia.EnemigosEliminados >= enemigosRequeridos);
        bool dosieresListos = dosieresRequeridos == 0 || siguientePuntoEntrega >= dosieresRequeridos;
        bool objetivosListos = enemigosListos && dosieresListos;
        extraccionDesbloqueada = extraccionDesbloqueada || objetivosListos;
    }

    private void ComprobarLlegadaExtraccion(Collider otro)
    {
        if (!extraccionDesbloqueada || victoriaActivada) return;
        if (otro.GetComponentInParent<MovimientoJugador>() == null) return;

        victoriaActivada = true;
        if (GestorInterfaz.Instancia != null) GestorInterfaz.Instancia.MostrarInterfazVictoria();
        else Debug.LogWarning("No se encontró el gestor de interfaz para mostrar la pantalla de victoria.", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (puntosEntrega == null) return;
        Gizmos.color = new Color(0.1f, 0.85f, 1f, 0.9f);
        Vector3 tamano = Vector3.one * tamanoMarcador;
        foreach (Transform punto in puntosEntrega)
            if (punto != null) Gizmos.DrawWireCube(punto.position, tamano);
    }
}
