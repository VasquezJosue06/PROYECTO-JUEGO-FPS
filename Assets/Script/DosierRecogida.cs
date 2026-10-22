using UnityEngine;

/// <summary>Collectible briefcase that is picked up automatically when the player approaches.</summary>
[DisallowMultipleComponent]
public class DosierRecogida : MonoBehaviour
{
    [Tooltip("Animator Controller that contains the original @spin animation.")]
    [SerializeField] private RuntimeAnimatorController controladorGiro;
    [Tooltip("HUD icon shown when collected. If empty, a simple briefcase icon is generated.")]
    [SerializeField] private Sprite iconoInterfaz;
    [TextArea] [SerializeField] private string mensajeRecogida = "Dosier recogido";
    [Min(0f)] [SerializeField] private float duracionMensaje = 4f;
    [Min(0.1f)] [SerializeField] private float radioRecogida = 1.5f;
    [SerializeField] private Vector3 posicionLocalTransportada = new Vector3(0f, 0.85f, 0.65f);

    private bool recogido;
    private bool entregado;
    private Transform jugador;
    private Animator animadorDosier;
    private Collider colisionadorRecogida;
    private Renderer[] renderizadoresModelo;
    private bool[] estadosOriginalesRenderizadores;

    public static DosierRecogida DosierTransportado { get; private set; }
    public bool EstaTransportado => recogido && !entregado;
    private void Start()
    {
        colisionadorRecogida = GetComponent<Collider>();
        renderizadoresModelo = GetComponentsInChildren<Renderer>(true);
        estadosOriginalesRenderizadores = new bool[renderizadoresModelo.Length];
        for (int i = 0; i < renderizadoresModelo.Length; i++)
            estadosOriginalesRenderizadores[i] = renderizadoresModelo[i] != null && renderizadoresModelo[i].enabled;
        // The controller's curves are authored on the imported FBX hierarchy,
        // so prefer the Animator on the model child over one on the pickup wrapper.
        Animator[] animadores = GetComponentsInChildren<Animator>(true);
        foreach (Animator candidato in animadores)
        {
            if (candidato.transform != transform)
            {
                animadorDosier = candidato;
                break;
            }
        }
        if (animadorDosier == null) animadorDosier = GetComponent<Animator>();
        if (animadorDosier == null)
        {
            Transform raizModelo = transform.childCount > 0 ? transform.GetChild(0) : transform;
            animadorDosier = raizModelo.gameObject.AddComponent<Animator>();
        }
        if (animadorDosier == null)
        {
            Debug.LogWarning($"Dosier '{name}' has no Animator. Assign the DosierAnimatior controller to its model.", this);
            return;
        }
        ReproducirGiroDesdeInicio();
    }

    private void Update()
    {
        if (recogido) return;
        if (DosierTransportado != null) return;
        if (jugador == null)
        {
            MovimientoJugador found = FindAnyObjectByType<MovimientoJugador>();
            if (found == null) return;
            jugador = found.transform;
        }

        Vector3 offset = jugador.position - transform.position;
        if (offset.sqrMagnitude > radioRecogida * radioRecogida) return;

        if (DosierTransportado != null) return;
        recogido = true;
        DosierTransportado = this;
        if (colisionadorRecogida != null) colisionadorRecogida.enabled = false;
        transform.SetParent(jugador, false);
        transform.localPosition = posicionLocalTransportada;
        transform.localRotation = Quaternion.identity;
        EstablecerModeloVisible(false);
        Sprite icono = iconoInterfaz != null ? iconoInterfaz : DosierIcono.Crear();
        bool mostradoPorInterfaz = GestorInterfaz.Instancia != null && GestorInterfaz.Instancia.MostrarMensajeDosier(icono, mensajeRecogida, duracionMensaje);
        if (!mostradoPorInterfaz) DosierAvisoRecogida.Mostrar(icono, mensajeRecogida, duracionMensaje);
    }

    public void EntregarEn(Vector3 posicionMundo)
    {
        if (!EstaTransportado) return;
        entregado = true;
        recogido = true;
        transform.SetParent(null, true);
        transform.position = posicionMundo;
        if (colisionadorRecogida != null) colisionadorRecogida.enabled = false;
        if (DosierTransportado == this) DosierTransportado = null;
        // Freeze the model in its delivered pose; the spin is only for uncollected dosiers.
        if (animadorDosier != null) animadorDosier.enabled = false;
        EstablecerModeloVisible(true);
    }

    private void EstablecerModeloVisible(bool visible)
    {
        if (renderizadoresModelo == null) return;
        for (int i = 0; i < renderizadoresModelo.Length; i++)
        {
            if (renderizadoresModelo[i] != null)
                renderizadoresModelo[i].enabled = visible && estadosOriginalesRenderizadores[i];
        }
    }

    private void ReproducirGiroDesdeInicio()
    {
        if (animadorDosier == null) return;
        animadorDosier.enabled = true;
        if (controladorGiro != null) animadorDosier.runtimeAnimatorController = controladorGiro;
        animadorDosier.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animadorDosier.applyRootMotion = false;
        animadorDosier.Rebind();
        animadorDosier.Play("Base Layer.@spin", 0, 0f);
        animadorDosier.Update(0f);
    }

    private void OnDestroy()
    {
        if (DosierTransportado == this) DosierTransportado = null;
    }
}
