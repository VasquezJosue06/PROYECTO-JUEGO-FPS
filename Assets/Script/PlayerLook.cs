using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLoock : MonoBehaviour
{
    public float MouseSensitivity = 30f;
    public Transform cam;

    private float xRotation = 0f;
    private Vector2 lookInput;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void HandleMouseLook()
    {
        float mouseX = lookInput.x * MouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * MouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90, 90);

        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }
}
