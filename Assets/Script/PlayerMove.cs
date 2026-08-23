using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float movespeed = 5f;
    public float jumpforce = 5f;

    public Transform groundchek;
    public float grounddistance = 0.4f;
    public LayerMask groundMask;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isgrounded;
    private PlayerInput playerInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerInput = new PlayerInput();
    }


    void Update()
    {
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
        moveInput = value.Get<Vector2>();
    }

    void MovePlayer()
    {
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        direction.Normalize();
        rb.linearVelocity = new Vector3(direction.x * movespeed, rb.linearVelocity.y, direction.z * movespeed);
    }
}
