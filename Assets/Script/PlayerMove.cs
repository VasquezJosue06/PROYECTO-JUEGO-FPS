using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("Movimiento")]
    public float movespeed = 5f;
    public float jumpforce = 5f;

    [Header("Detección de suelo")]
    public Transform groundchek;
    public float grounddistance = 0.4f;
    public LayerMask groundMask;

    [Header("Audio")]
    public AudioClip footStepSFX;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isgrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(PlayFootStep());
    }


    void Update()
    {
        // Se actualiza cada cuadro para que el salto responda con rapidez.
        CheckGround();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    void OnJump()
    {
        if (isgrounded)
        {
            rb.AddForce(new Vector3(0, jumpforce, 0), ForceMode.Impulse);
        }
        
    }

    void CheckGround()
    {
        isgrounded = Physics.CheckSphere(groundchek.position, grounddistance, groundMask);
    }

    void OnMove(InputValue value)
    {
        // Conserva la dirección indicada por el jugador.
        moveInput = value.Get<Vector2>();
    }

    void MovePlayer()
    {
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        direction.Normalize();

        // Conserva la velocidad vertical para que gravedad y salto sigan actuando.
        rb.linearVelocity = new Vector3(direction.x * movespeed, rb.linearVelocity.y, direction.z * movespeed);
    }

    IEnumerator PlayFootStep()
    {
        while (true)
        {
            // Solo la velocidad horizontal cuenta como caminar.
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVelocity.sqrMagnitude > 0.01f && isgrounded)
            {
                AudioManager.Instance.PlaySFX(footStepSFX);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
