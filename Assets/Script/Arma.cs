using UnityEngine;
using System.Collections.Generic;

// Conserva este componente y sus campos: los prefabs y las clases ya los usan.
public partial class Arma : MonoBehaviour
{
    [Header("Municion y cadencia")]
    [SerializeField] private float tiempoRecarga = 1f;
    public float frecuenciaDisparo = 0.15f;
    public int capacidadMaxima = 20;

    [Header("Sonido y efectos")]
    public AudioClip sonidoDisparo;

    [SerializeField] private GameObject proyectil;
    public Transform puntoGeneracionProyectil;

    [SerializeField] private GameObject fogonazoArma;
    [SerializeField] private GameObject armaSoltada;

    [Header("Retroceso sin animaciones")]
    [SerializeField] private float distanciaRetroceso = 0.1f;
    [SerializeField] private float velocidadRetroceso = 15f;

    [Header("Animaciones y tipo de recarga")]
    [Tooltip("Recarga un cartucho por ciclo (Scattergun).")]
    public bool escopetaAnimada;
    [Tooltip("Recarga el cargador completo (pistola). Tiene prioridad sobre cartuchos.")]
    public bool cargadorAnimado;
    [Header("Minigun")]
    public bool minigunAnimada;
    [Range(0f, 1f)] public float multiplicadorMovimientoGirando = 0.4782609f;
    [Header("Reserva de municion")]
    [Tooltip("-1 conserva la reserva ilimitada de las armas existentes.")]
    public int capacidadReserva = -1;
    [Header("Tipo de impacto")]
    [Tooltip("Impacto instantaneo. La trazadora solo muestra el recorrido.")]
    public bool impactoInstantaneo;
    [SerializeField] private bool caidaDanioDistancia = true;
    [Tooltip("0 usa la duracion del clip; debe coincidir con la velocidad del estado fire.")]
    [Min(0)] public float duracionAnimacionDisparo;
    [Min(0)] public float danioPerdigon = 6f;
    [Min(1)] [SerializeField] private float alcanceImpactoInstantaneo = 100f;
    [Header("Trazadoras compartidas")]
    public bool trazadorasVisibles;
    public GameObject prefabricadoTrazadora;
    [Min(0.001f)] [SerializeField] private float anchoTrazadora = 0.035f;
    [Min(0.01f)] [SerializeField] private float duracionTrazadora = 0.075f;
    [Header("Giro de la minigun")]
    public Transform tamborMinigun;
    [Tooltip("Eje longitudinal del tambor en el modelo exportado. Esta minigun usa X.")]
    public Vector3 ejeLocalTambor = Vector3.right;
    [Min(0)] public float gradosTamborPorSegundo = 1440f;
    [Min(0.01f)] [SerializeField] private float tiempoAceleracionTambor = 0.87f;
    [Min(0.01f)] [SerializeField] private float tiempoFrenadoTambor = 0.6f;
    private Quaternion rotacionReposoTambor;
    private Vector3 ejeTambor = Vector3.right;
    private float velocidadTambor;
    private float anguloTambor;
    private GrupoTrazadorasInstantaneas trazadoras;
    private readonly List<Vector3> finalesTrazadorasPendientes = new List<Vector3>(16);
    private int municionReserva;
    private float disponibleEn;
    private float giroCompletoEn;
    private bool giroEstabaCompleto;
    public int MunicionReserva => municionReserva;
    public bool EstaGirando { get; private set; }
    public float MultiplicadorMovimiento => EstaGirando ? multiplicadorMovimientoGirando : 1f;
    public bool PuedeCambiarArma => !EstaGirando;
    private bool inicializado;
    private bool UsaAnimaciones => escopetaAnimada || cargadorAnimado || minigunAnimada;
    [Header("Animator del arma")]
    public Animator animadorArma;
    [Header("Perdigones y dispersion")]
    [Min(1)] public int perdigonesPorDisparo = 10;
    [Range(0, 30)] public float gradosDispersion = 4f;
    [Header("Posicion respecto a la camara")]
    public Vector3 posicionEquipada = new Vector3(0, 0, 0.01f);
    public Vector3 angulosEulerEquipada = new Vector3(0, 90, 0);
    [Header("Camara del arma")]
    [SerializeField] private bool camaraArmaSeparada = true;
    [Range(20f, 100f)] [SerializeField] private float campoVisionArma = 50f;
    private Camera camaraArma;
    private bool camaraAdjunta;
    private bool camaraPrincipalIncluyeArma;
    private readonly Dictionary<GameObject, int> capasOriginales =
        new Dictionary<GameObject, int>();
    // Referencias reutilizadas: no buscar colliders ni clips por cada disparo.
    private readonly Dictionary<string, float> duracionesClips = new Dictionary<string, float>();
    private readonly List<Collider> colisionadoresGenerados = new List<Collider>();
    private readonly List<Collider> colisionadoresPerdigones = new List<Collider>();
    private readonly RaycastHit[] impactosApuntado = new RaycastHit[32];
    private Collider[] colisionadoresPropietario;
    private Transform propietario;
    private Coroutine recargaCartuchos;
    private Camera camaraApuntado;
    public int MunicionActual => municionActual;
    public bool EstaRecargando => estaRecargando;
    private int municionActual;
    private bool estaRecargando = false;
    private float siguienteDisparoEn = 0f;

    private Quaternion rotacionInicial;
    private Vector3 posicionInicial;
    private Vector3 desplazamientoRotacionRecarga = new Vector3(66, 50, 50);

    void Start()
    {
        municionActual = capacidadMaxima;
        municionReserva = capacidadReserva;
        propietario = transform.root;
        colisionadoresPropietario = propietario.GetComponentsInChildren<Collider>();
        if (UsaAnimaciones)
        {
            if (animadorArma == null) animadorArma = GetComponentInChildren<Animator>();
            GuardarDuracionesAnimacion();
            camaraApuntado = GetComponentInParent<Camera>();
            if (camaraApuntado == null) camaraApuntado = Camera.main;
            foreach (Transform child in GetComponentsInChildren<Transform>())
                if (puntoGeneracionProyectil == null && child.name == "muzzle") puntoGeneracionProyectil = child;
            if (GetComponentInParent<DisparoJugador>() != null && camaraApuntado != null)
            {
                transform.position = camaraApuntado.transform.TransformPoint(posicionEquipada);
                transform.rotation = camaraApuntado.transform.rotation * Quaternion.Euler(angulosEulerEquipada);
            }
            ConfigurarCamaraArma();
            ReproducirAnimacion("@draw");
            siguienteDisparoEn = Time.time + DuracionClip("@draw");
            disponibleEn = siguienteDisparoEn;
        }
        if (minigunAnimada) BuscarTamborMinigun();
        if (impactoInstantaneo && trazadorasVisibles) trazadoras = new GrupoTrazadorasInstantaneas(prefabricadoTrazadora);
        inicializado = true;
        rotacionInicial = transform.localRotation;
        posicionInicial = transform.localPosition;
        ActualizarInterfazMunicion();
    }

    public void Disparar()
    {
        if (!inicializado || !isActiveAndEnabled || minigunAnimada || Time.timeScale <= 0f) return;
        if (UsaAnimaciones)
        {
            DispararConAnimacion();
            return;
        }
        if(estaRecargando) return;
        if(Time.time < siguienteDisparoEn) return;

        if(municionActual <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        siguienteDisparoEn = Time.time + frecuenciaDisparo;
        municionActual--;
        ActualizarInterfazMunicion();

        GestorAudio.Instancia.ReproducirEfectoSonoro(sonidoDisparo, 0.25f);

        Quaternion adjustedRotation = puntoGeneracionProyectil.rotation * Quaternion.Euler(-1f, -1f, 0);

        Instantiate(proyectil, puntoGeneracionProyectil.position, adjustedRotation);
        Instantiate(fogonazoArma, puntoGeneracionProyectil.position, puntoGeneracionProyectil.rotation);

        StopCoroutine(nameof(Retroceso));
        StartCoroutine(nameof(Retroceso));
    }

    public bool SoltarArma()
    {
        if (armaSoltada == null) return false;
        if (GestorInterfaz.Instancia != null && GestorInterfaz.Instancia.textoMunicion != null)
            GestorInterfaz.Instancia.textoMunicion.text = "";
        Instantiate(armaSoltada, transform.position, transform.rotation);
        Destroy(gameObject);
        return true;
    }

    private void ActualizarInterfazMunicion()
    {
        if (GestorInterfaz.Instancia != null && GestorInterfaz.Instancia.textoMunicion != null)
            GestorInterfaz.Instancia.textoMunicion.text = capacidadReserva >= 0
                ? municionActual + " / " + municionReserva : municionActual.ToString();
    }

    private void OnDisable()
    {
        EstaGirando = false;
        giroEstabaCompleto = false;
        velocidadTambor = 0;
        anguloTambor = 0;
        if (inicializado && tamborMinigun != null) tamborMinigun.localRotation = rotacionReposoTambor;
        finalesTrazadorasPendientes.Clear();
        trazadoras?.Limpiar();
        if (minigunAnimada && animadorArma != null && animadorArma.runtimeAnimatorController != null)
        {
            animadorArma.SetBool("Spin", false);
            animadorArma.SetBool("Fire", false);
        }
        if (inicializado && !UsaAnimaciones)
        {
            transform.localPosition = posicionInicial;
            transform.localRotation = rotacionInicial;
        }
        QuitarCamaraArma();
        StopAllCoroutines();
        recargaCartuchos = null;
        estaRecargando = false;
    }

    private void OnEnable()
    {
        // Al volver a equipar, recupera la camara y reproduce la animacion de sacar el arma.
        if (!inicializado) return;
        if (UsaAnimaciones)
        {
            ConfigurarCamaraArma();
            ReproducirAnimacion("@draw");
            siguienteDisparoEn = Mathf.Max(siguienteDisparoEn, Time.time + DuracionClip("@draw"));
            disponibleEn = siguienteDisparoEn;
        }
        ActualizarInterfazMunicion();
    }

    private void LateUpdate()
    {
        AnimarTamborMinigun(Time.deltaTime);
        trazadoras?.ActualizarPaso(Time.deltaTime);
        if (camaraArma != null && camaraApuntado != null)
        {
            // Sincroniza la proyeccion antes de calcular la salida de las trazadoras.
            camaraArma.fieldOfView = Mathf.Clamp(campoVisionArma, 20f, 100f);
            camaraArma.rect = camaraApuntado.rect;
            camaraArma.aspect = camaraApuntado.aspect;
            camaraArma.enabled = camaraApuntado.enabled;
        }
        EmitirTrazadorasPendientes();
    }

    private void OnDestroy()
    {
        trazadoras?.Dispose();
        // Limpia los efectos y cualquier camara que aun exista.
        QuitarCamaraArma();
        if (camaraArma != null) Destroy(camaraArma.gameObject);
    }

}
