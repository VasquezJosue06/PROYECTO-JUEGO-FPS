using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLoock : MonoBehaviour
{
    public static PlayerLoock Instance;
    [Tooltip("Grados por píxel del mouse. El delta del Input System ya depende del cuadro.")]
    public float MouseSensitivity = 0.12f;
    public Transform cam;

    private float xRotation = 0f;
    private Vector2 lookInput;

    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;
    private float shakeFadeSpeed = 1.5f;
    private Vector3 initialCamPos;
    private Rigidbody body;
    private float yaw;

    private void Awake()
    {
        Instance = this;
        body = GetComponent<Rigidbody>();
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (cam == null)
        {
            Debug.LogError("PlayerLoock necesita una cámara asignada en el campo Cam.", this);
            enabled = false;
            return;
        }
        initialCamPos = cam.localPosition;
        xRotation = NormalizeAngle(cam.localEulerAngles.x);
        yaw = body != null ? body.rotation.eulerAngles.y : transform.eulerAngles.y;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        HandleMouseLook();
        HandleShake();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void HandleMouseLook()
    {
        if (cam == null) return;
        // Mouse delta is an event value; consume it once instead of applying the
        // last event again on every rendered frame until the next input event.
        Vector2 frameLookInput = lookInput;
        lookInput = Vector2.zero;
        float mouseX = frameLookInput.x * MouseSensitivity;
        float mouseY = frameLookInput.y * MouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90, 90);

        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        yaw += mouseX;
        if (body == null) transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    void FixedUpdate()
    {
        if (body != null) body.MoveRotation(Quaternion.Euler(0f, yaw, 0f));
    }

    void HandleShake()
    {
        if(shakeDuration > 0)
        {
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
        shakeDuration = dutarion;
        shakeMagnitude = magnitude;
    }
}
