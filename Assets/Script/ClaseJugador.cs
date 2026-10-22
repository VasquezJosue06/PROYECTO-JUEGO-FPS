using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(VidaJugador), typeof(MovimientoJugador), typeof(DisparoJugador))]
public class ClaseJugador : MonoBehaviour
{
    [SerializeField, Tooltip("Estadisticas y armas del jugador. Cambiar durante Play reinicia vida y arsenal.")]
    private ClasesJugador claseSeleccionada;

    public ClasesJugador ClaseSeleccionada => claseSeleccionada;
    public ClasesJugador ClaseActual { get; private set; }
    private VidaJugador vida;
    private MovimientoJugador movimiento;
    private DisparoJugador disparo;

    private void Awake()
    {
        vida = GetComponent<VidaJugador>();
        movimiento = GetComponent<MovimientoJugador>();
        disparo = GetComponent<DisparoJugador>();
    }

    private void Start()
    {
        if (claseSeleccionada != null) SeleccionarClase(claseSeleccionada);
    }

    private void Update()
    {
        // Aplica los cambios del Inspector durante el juego.
        if (claseSeleccionada != ClaseActual && !SeleccionarClase(claseSeleccionada))
            claseSeleccionada = ClaseActual;
    }

    // Un solo punto para cambiar vida, movimiento y armas de la clase.
    public bool SeleccionarClase(ClasesJugador nuevaClase)
    {
        if (nuevaClase == null || !isActiveAndEnabled || vida == null || vida.vida <= 0)
            return false;
        if ((nuevaClase.armaPrincipal != null || nuevaClase.armaSecundaria != null) && disparo.soporteArma == null)
        {
            Debug.LogError("Asigna Gun Holder antes de seleccionar una clase.", this);
            return false;
        }
        if (nuevaClase == ClaseActual)
        {
            claseSeleccionada = nuevaClase;
            return true;
        }

        vida.AplicarVidaMaxima(nuevaClase.vidaMaxima);
        movimiento.AplicarClase(nuevaClase);
        disparo.AplicarClase(nuevaClase);
        ClaseActual = nuevaClase;
        claseSeleccionada = nuevaClase;
        return true;
    }
}
