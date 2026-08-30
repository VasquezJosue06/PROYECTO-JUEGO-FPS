using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLoock : MonoBehaviour
{
<<<<<<< HEAD
    // Instancia compartida utilizada por PlayerHealth para aplicar el temblor.
    public static PlayerLoock Instance;

    [Header("Cámara")]
=======
    public static PlayerLoock Instance;
>>>>>>> Develop
    public float MouseSensitivity = 30f;
    public Transform cam;

    private float xRotation = 0f;
    private Vector2 lookInput;

<<<<<<< HEAD
    [Header("Temblor de cámara")]
=======
>>>>>>> Develop
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;
    private float shakeFadeSpeed = 1.5f;
    private Vector3 initialCamPos;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
<<<<<<< HEAD
        // El cursor queda reservado al control de la cámara mientras el jugador vive.
=======
>>>>>>> Develop
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        initialCamPos = cam.localPosition;
    }

    void Update()
    {
        HandleMouseLook();
        HandleShake();
    }

    public void OnLook(InputValue value)
    {
<<<<<<< HEAD
        // El Input System entrega el desplazamiento del mouse o stick derecho.
=======
>>>>>>> Develop
        lookInput = value.Get<Vector2>();
    }

    void HandleMouseLook()
    {
        float mouseX = lookInput.x * MouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * MouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
<<<<<<< HEAD
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
=======
        xRotation = Mathf.Clamp(xRotation, -90, 90);
>>>>>>> Develop

        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleShake()
    {
<<<<<<< HEAD
        if (shakeDuration > 0)
        {
            // Aplica un desplazamiento temporal sin alterar la posición base de la cámara.
=======
        if(shakeDuration > 0)
        {
>>>>>>> Develop
            cam.localPosition = initialCamPos + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime * shakeFadeSpeed;
        }
        else
        {
            cam.localPosition = initialCamPos;
        }
    }

    public void AddShake(float dutarion, float magnitude)
    {
<<<<<<< HEAD
        // Reinicia el temblor con la intensidad solicitada.
=======
>>>>>>> Develop
        shakeDuration = dutarion;
        shakeMagnitude = magnitude;
    }
}
