using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovimientoJugador : MonoBehaviour
{
    public float velocidadMovimiento = 5f;
    public float fuerzaSalto = 5f;
    public int saltosAereos;
    public float velocidadSaltoAereo = 5f;
    public float gravedad;
    public float multiplicadorVelocidadAtras = 1f;

    [Range(0.05f, 0.5f)] [SerializeField] private float radioDetectorSuelo = 0.22f;

    public Transform puntoChequeoSuelo;
    [SerializeField] private float distanciaSuelo = 0.4f;
    public LayerMask mascaraSuelo;

    [SerializeField] private AudioClip sonidoPasos;

    private Rigidbody cuerpoRigido;
    private Vector2 entradaMovimiento;
    private bool estaEnSuelo;
    private int saltosAereosUsados;
    private bool saltoSolicitado;
    private VidaJugador vidaJugador;
    private DisparoJugador disparo;
    private Vector3 normalSuelo = Vector3.up;
    private float ignorarSueloHasta;

    public void AplicarClase(ClasesJugador configuracion)
    {
        velocidadMovimiento = Mathf.Max(0f, configuracion.velocidadMovimiento);
        fuerzaSalto = Mathf.Max(0f, configuracion.fuerzaSalto);
        saltosAereos = Mathf.Max(0, configuracion.saltosAereos);
        velocidadSaltoAereo = Mathf.Max(0f, configuracion.velocidadSaltoAereo);
        gravedad = Mathf.Max(0f, configuracion.gravedad);
        multiplicadorVelocidadAtras = Mathf.Clamp01(configuracion.multiplicadorVelocidadAtras);
        saltoSolicitado = false;
        // Keep air-jump consumption until landing, preventing free jumps on class changes.
    }

    void Awake()
    {
        cuerpoRigido = GetComponent<Rigidbody>();
        vidaJugador = GetComponent<VidaJugador>();
        disparo = GetComponent<DisparoJugador>();
        if (cuerpoRigido != null)
        {
            cuerpoRigido.constraints |= RigidbodyConstraints.FreezeRotation;
            cuerpoRigido.interpolation = RigidbodyInterpolation.Interpolate;
            cuerpoRigido.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    void Start()
    {
        StartCoroutine(ReproducirPasos());
    }


    private void FixedUpdate()
    {
        ComprobarSuelo();
        if (gravedad > 0f)
        {
            Vector3 gravedadAutomatica = cuerpoRigido.useGravity ? Physics.gravity : Vector3.zero;
            cuerpoRigido.AddForce(Vector3.down * gravedad - gravedadAutomatica, ForceMode.Acceleration);
        }
        MoverJugador();
        if (saltoSolicitado) IntentarSaltar();
        saltoSolicitado = false;
    }

    void OnJump(InputValue valor)
    {
        if (valor.isPressed && Time.timeScale > 0f &&
            (vidaJugador == null || vidaJugador.vida > 0)) saltoSolicitado = true;
    }

    private void OnDisable()
    {
        saltoSolicitado = false;
        entradaMovimiento = Vector2.zero;
    }

    private void IntentarSaltar()
    {
        if (disparo != null && disparo.armaActual != null && disparo.armaActual.EstaGirando) return;
        if (!estaEnSuelo && saltosAereosUsados >= saltosAereos) return;
        float velocidad = estaEnSuelo ? fuerzaSalto : velocidadSaltoAereo;
        if (!estaEnSuelo) saltosAereosUsados++;
        // Set vertical speed so a falling Scout gets a full air jump, independent of mass.
        Vector3 velocidadActual = cuerpoRigido.linearVelocity;
        velocidadActual.y = velocidad;
        cuerpoRigido.linearVelocity = velocidadActual;
        estaEnSuelo = false;
        ignorarSueloHasta = Time.time + 0.12f;
    }

    void ComprobarSuelo()
    {
        // A sphere overlap against a wall counted as ground and could suspend the player.
        // Only a downward hit with a walkable normal resets jumps and enables ground control.
        estaEnSuelo = false;
        normalSuelo = Vector3.up;
        if (Time.time >= ignorarSueloHasta && puntoChequeoSuelo != null &&
            Physics.SphereCast(puntoChequeoSuelo.position + Vector3.up * 0.05f, radioDetectorSuelo,
                Vector3.down, out RaycastHit impacto, distanciaSuelo + 0.05f, mascaraSuelo,
                QueryTriggerInteraction.Ignore))
        {
            estaEnSuelo = impacto.normal.y >= 0.55f;
            if (estaEnSuelo) normalSuelo = impacto.normal;
        }
        if (estaEnSuelo) saltosAereosUsados = 0;
    }

    void OnMove(InputValue valor)
    {
        entradaMovimiento = valor.Get<Vector2>();
    }

    void MoverJugador()
    {
        Vector3 direccion = transform.right * entradaMovimiento.x + transform.forward * entradaMovimiento.y;
        direccion.Normalize();
        if (estaEnSuelo && entradaMovimiento.y < 0f)
        {
            // TF2 limits the backwards component without slowing sideways movement.
            float haciaAtras = Vector3.Dot(direccion, transform.forward);
            if (haciaAtras < -multiplicadorVelocidadAtras)
                direccion += transform.forward * (-multiplicadorVelocidadAtras - haciaAtras);
        }
        float velocidadArma = disparo != null && disparo.armaActual != null ? disparo.armaActual.MultiplicadorMovimiento : 1f;
        Vector3 deseado = direccion * velocidadMovimiento * velocidadArma;
        Vector3 velocidad = cuerpoRigido.linearVelocity;

        // On a ramp, world-horizontal velocity pushes the capsule into the slope and
        // loses speed. Project the desired movement onto the walkable surface instead.
        if (estaEnSuelo)
        {
            Vector3 direccionPendiente = Vector3.ProjectOnPlane(deseado, normalSuelo);
            if (direccionPendiente.sqrMagnitude > 0.001f)
                deseado = direccionPendiente.normalized * deseado.magnitude;
        }

        float vertical = velocidad.y;
        if (estaEnSuelo && deseado.y > 0f) vertical = Mathf.Max(vertical, deseado.y);
        cuerpoRigido.linearVelocity = new Vector3(deseado.x, vertical, deseado.z);
    }

    private IEnumerator ReproducirPasos()
    {
        while(true)
        {
            if(cuerpoRigido.linearVelocity.magnitude > 0.1f && estaEnSuelo && sonidoPasos != null && GestorAudio.Instancia != null)
            {
                GestorAudio.Instancia.ReproducirEfectoSonoro(sonidoPasos);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
