using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
<<<<<<< HEAD
    [Header("Movimiento")]
    public float movespeed = 5f;
    public float jumpforce = 5f;

    [Header("Detección de suelo")]
=======
    public float movespeed = 5f;
    public float jumpforce = 5f;

>>>>>>> Develop
    public Transform groundchek;
    public float grounddistance = 0.4f;
    public LayerMask groundMask;

<<<<<<< HEAD
    [Header("Audio")]
=======
>>>>>>> Develop
    public AudioClip footStepSFX;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isgrounded;
<<<<<<< HEAD
=======
    private PlayerInput playerInput;
>>>>>>> Develop

    void Start()
    {
        rb = GetComponent<Rigidbody>();
<<<<<<< HEAD
=======
        playerInput = new PlayerInput();

>>>>>>> Develop
        StartCoroutine(PlayFootStep());
    }


    void Update()
    {
<<<<<<< HEAD
        // Se actualiza cada cuadro para que el salto responda con rapidez.
=======
>>>>>>> Develop
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
<<<<<<< HEAD
        // Conserva la dirección indicada por el jugador.
=======
>>>>>>> Develop
        moveInput = value.Get<Vector2>();
    }

    void MovePlayer()
    {
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        direction.Normalize();
<<<<<<< HEAD

        // Conserva la velocidad vertical para que gravedad y salto sigan actuando.
=======
>>>>>>> Develop
        rb.linearVelocity = new Vector3(direction.x * movespeed, rb.linearVelocity.y, direction.z * movespeed);
    }

    IEnumerator PlayFootStep()
    {
<<<<<<< HEAD
        while (true)
        {
            // Solo la velocidad horizontal cuenta como caminar.
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVelocity.sqrMagnitude > 0.01f && isgrounded)
=======
        while(true)
        {
            if(rb.linearVelocity.magnitude > 0.1f && isgrounded)
>>>>>>> Develop
            {
                AudioManager.Instance.PlaySFX(footStepSFX);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
