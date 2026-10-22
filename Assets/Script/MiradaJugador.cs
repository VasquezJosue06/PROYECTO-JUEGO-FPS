using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class MiradaJugador : MonoBehaviour
{
    public static MiradaJugador Instancia;
    [Tooltip("Grados por píxel del mouse. El delta del Input System ya depende del cuadro.")]
    [FormerlySerializedAs("SensibilidadRaton"), SerializeField] private float sensibilidadRaton = 0.12f;
    [SerializeField] private Transform camara;

    private float rotacionX = 0f;
    private Vector2 entradaMirada;

    private float duracionTemblor = 0f;
    private float magnitudTemblor = 0.1f;
    private float velocidadDesvanecimientoTemblor = 1.5f;
    private Vector3 posicionInicialCamara;
    private Rigidbody cuerpoRigido;
    private float giroHorizontal;

    private void Awake()
    {
        Instancia = this;
        cuerpoRigido = GetComponent<Rigidbody>();
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (camara == null)
        {
            Debug.LogError("PlayerLoock necesita una cámara asignada en el campo Cam.", this);
            enabled = false;
            return;
        }
        posicionInicialCamara = camara.localPosition;
        rotacionX = NormalizeAngle(camara.localEulerAngles.x);
        giroHorizontal = cuerpoRigido != null ? cuerpoRigido.rotation.eulerAngles.y : transform.eulerAngles.y;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        AplicarMiradaRaton();
        ActualizarTemblor();
    }

    public void OnLook(InputValue valor)
    {
        entradaMirada = valor.Get<Vector2>();
    }

    private void AplicarMiradaRaton()
    {
        if (camara == null) return;
        // Mouse delta is an event value; consume it once instead of applying the
        // last event again on every rendered frame until the next input event.
        Vector2 frameLookInput = entradaMirada;
        entradaMirada = Vector2.zero;
        float mouseX = frameLookInput.x * sensibilidadRaton;
        float mouseY = frameLookInput.y * sensibilidadRaton;

        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -90, 90);

        camara.localRotation = Quaternion.Euler(rotacionX, 0f, 0f);

        giroHorizontal += mouseX;
        if (cuerpoRigido == null) transform.rotation = Quaternion.Euler(0f, giroHorizontal, 0f);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    void FixedUpdate()
    {
        if (cuerpoRigido != null) cuerpoRigido.MoveRotation(Quaternion.Euler(0f, giroHorizontal, 0f));
    }

    private void ActualizarTemblor()
    {
        if(duracionTemblor > 0)
        {
            camara.localPosition = posicionInicialCamara + Random.insideUnitSphere * magnitudTemblor;
            duracionTemblor -= Time.deltaTime * velocidadDesvanecimientoTemblor;
        }
        else
        {
            camara.localPosition = posicionInicialCamara;
        }
    }

    public void AplicarTemblor(float duracion, float magnitud)
    {
        duracionTemblor = duracion;
        magnitudTemblor = magnitud;
    }
}
