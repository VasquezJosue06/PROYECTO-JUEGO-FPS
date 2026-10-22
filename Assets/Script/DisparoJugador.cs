using UnityEngine;
using UnityEngine.InputSystem;

public class DisparoJugador : MonoBehaviour
{
    // PlayerClass aplica la clase; este script no modifica su asset.
    private ClasesJugador claseActual;
    [Header("Arma equipada")]
    public Arma armaActual;
    public Transform soporteArma;
    // Compatibilidad con escenas que aun no tienen una clase asignada.
    [SerializeField, HideInInspector] private Arma armaInicial;
    [SerializeField, HideInInspector] private Arma armaSecundaria;
    private bool iniciado;
    private readonly Arma[] armas = new Arma[2];
    private int espacioActivo;
    private int espacioAnterior = -1;
    private bool mantieneDisparo;
    private VidaJugador vidaJugador;
    private bool PuedeUsarArmas => isActiveAndEnabled &&
        (vidaJugador == null || vidaJugador.vida > 0);
    private InputAction cambioTeclado;
    private InputAction cambioRaton;
    private InputAction cambioAnterior;
    private InputAction accionGiro;

    private void Awake()
    {
        vidaJugador = GetComponent<VidaJugador>();
    }

    void Start()
    {
        iniciado = true;
        // Si hay una clase, PlayerClass se encarga de equipar las armas.
        ClaseJugador controladorClase = GetComponent<ClaseJugador>();
        if (controladorClase != null && controladorClase.isActiveAndEnabled && controladorClase.ClaseSeleccionada != null)
        {
            VincularEntradasCambioArma();
            return;
        }
        armas[0] = armaActual;
        if (armas[0] == null && armaInicial != null && soporteArma != null)
            armas[0] = Instantiate(armaInicial, soporteArma);
        armaActual = armas[0];
        if (armaActual == null) SeleccionarArma(1);
        VincularEntradasCambioArma();
    }

    private void VincularEntradasCambioArma()
    {
        DesvincularEntradasCambioArma();
        UnityEngine.InputSystem.PlayerInput input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input == null || input.actions == null) return;
        cambioTeclado = input.actions.FindAction("OnFoot/Change_K", false);
        cambioRaton = input.actions.FindAction("OnFoot/change_M", false);
        cambioAnterior = input.actions.FindAction("OnFoot/Change_Q", false);
        if (cambioAnterior != null) cambioAnterior.performed += CambiarAlAnterior;
        if (cambioTeclado != null) cambioTeclado.performed += CambiarConTeclado;
        if (cambioRaton != null) cambioRaton.performed += CambiarConRaton;
        accionGiro = new InputAction("MinigunSpin", InputActionType.Button, "<Mouse>/rightButton");
        accionGiro.AddBinding("<Gamepad>/leftTrigger");
        accionGiro.Enable();
    }

    public void AplicarClase(ClasesJugador configuracion)
    {
        mantieneDisparo = false;
        // Guarda el arma anterior y cancela su recarga antes de cambiar de clase.
        if (armaActual != null && armaActual != armas[0] && armaActual != armas[1])
        {
            armaActual.gameObject.SetActive(false);
            Destroy(armaActual.gameObject);
        }
        for (int posicionArreglo = 0; posicionArreglo < armas.Length; posicionArreglo++)
        {
            if (armas[posicionArreglo] != null)
            {
                armas[posicionArreglo].gameObject.SetActive(false);
                Destroy(armas[posicionArreglo].gameObject);
                armas[posicionArreglo] = null;
            }
        }
        armaActual = null;
        espacioActivo = 0;
        espacioAnterior = -1;
        claseActual = configuracion;
        armaInicial = configuracion.armaPrincipal;
        armaSecundaria = configuracion.armaSecundaria;
        SeleccionarArma(armaInicial != null ? 0 : 1);
        if (armaActual == null && GestorInterfaz.Instancia != null && GestorInterfaz.Instancia.textoMunicion != null)
            GestorInterfaz.Instancia.textoMunicion.text = "";
    }

    private void DesvincularEntradasCambioArma()
    {
        accionGiro?.Dispose();
        accionGiro = null;
        if (cambioAnterior != null) cambioAnterior.performed -= CambiarAlAnterior;
        if (cambioTeclado != null) cambioTeclado.performed -= CambiarConTeclado;
        if (cambioRaton != null) cambioRaton.performed -= CambiarConRaton;
    }

    private void OnEnable()
    {
        // La primera vez se enlazan los controles en Start.
        if (iniciado) VincularEntradasCambioArma();
    }

    private void OnDisable()
    {
        if (armaActual != null) armaActual.ActualizarEntradaMinigun(false, false);
        DesvincularEntradasCambioArma();
        mantieneDisparo = false;
    }

    private void CambiarConTeclado(InputAction.CallbackContext context)
    {
        if (context.control.name == "1") SeleccionarArma(0);
        else if (context.control.name == "2") SeleccionarArma(1);
    }

    private void CambiarConRaton(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<float>();
        if (Mathf.Approximately(scroll, 0f)) return;
        int direccion = scroll > 0f ? -1 : 1;
        for (int step = 1; step <= armas.Length; step++)
        {
            int posicionArreglo = (espacioActivo + direccion * step + armas.Length) % armas.Length;
            if (TieneArma(posicionArreglo))
            {
                SeleccionarArma(posicionArreglo);
                return;
            }
        }
    }

    private void CambiarAlAnterior(InputAction.CallbackContext context)
    {
        if (espacioAnterior >= 0) SeleccionarArma(espacioAnterior);
    }

    private bool TieneArma(int posicionArreglo)
    {
        return armas[posicionArreglo] != null || (posicionArreglo == 0 ? armaInicial != null : armaSecundaria != null);
    }

    public void SeleccionarArma(int posicionArreglo)
    {
        if (!PuedeUsarArmas) return;
        if (armaActual != null && !armaActual.PuedeCambiarArma) return;
        if (posicionArreglo < 0 || posicionArreglo >= armas.Length || !TieneArma(posicionArreglo)) return;
        if (armaActual != null && armaActual == armas[posicionArreglo]) return;
        if (armas[posicionArreglo] == null)
        {
            if (soporteArma == null) return;
            // Start se ejecuta antes del primer disparo; no hace falta un objeto temporal.
            Arma prefabricado = posicionArreglo == 0 ? armaInicial : armaSecundaria;
            armas[posicionArreglo] = Instantiate(prefabricado, soporteArma);
        }
        // Guardar el arma limpia su camara, efectos y recarga pendiente.
        if (armaActual != null)
        {
            espacioAnterior = espacioActivo;
            armaActual.gameObject.SetActive(false);
        }
        espacioActivo = posicionArreglo;
        armaActual = armas[posicionArreglo];
        armaActual.gameObject.SetActive(true);
    }

    void OnShoot()
    {
        mantieneDisparo = PuedeUsarArmas;
    }
    void OnShootRelease()
    {
        mantieneDisparo = false;
    }
    void OnReload()
    {
        if (!PuedeUsarArmas) return;
        if (armaActual != null && !armaActual.PuedeCambiarArma) return;
        if (armaActual != null) armaActual.IntentarRecargar();
    }

    void Update()
    {
        // Al morir se ignoran los controles aunque Update siga ejecutandose.
        if (!PuedeUsarArmas)
        {
            mantieneDisparo = false;
            if (armaActual != null) armaActual.ActualizarEntradaMinigun(false, false);
            return;
        }
        if (armaActual != null) armaActual.ActualizarEntradaMinigun(mantieneDisparo, accionGiro != null && accionGiro.IsPressed());
        if (mantieneDisparo && armaActual != null) armaActual.Disparar();
    }

    public void OnDrop()
    {
        if (!PuedeUsarArmas) return;
        if (armaActual != null && !armaActual.PuedeCambiarArma) return;
        if (armaActual == null || !armaActual.SoltarArma()) return;
        armas[espacioActivo] = null;
        if (espacioActivo == 0) armaInicial = null;
        else armaSecundaria = null;
        armaActual = null;
    }

    public bool PuedeRecogerArma(Arma prefabricado)
    {
        return PuedeUsarArmas && soporteArma != null && prefabricado != null &&
            (claseActual == null || claseActual.PermiteArma(prefabricado));
    }

    public bool EquiparArmaRecogida(Arma armaNueva, Arma prefabricadoOrigen)
    {
        if (armaNueva == null || !PuedeRecogerArma(prefabricadoOrigen)) return false;
        armas[espacioActivo] = armaNueva;
        armaActual = armaNueva;
        return true;
    }
}
