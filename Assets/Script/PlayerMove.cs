using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public float movespeed = 5f;
    public float jumpforce = 5f;
    public int airJumps;
    public float airJumpSpeed = 5f;
    public float gravity;
    public float backwardSpeedMultiplier = 1f;

    public Transform groundchek;
    public float grounddistance = 0.4f;
    public LayerMask groundMask;

    public AudioClip footStepSFX;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isgrounded;
    private int usedAirJumps;
    private bool jumpRequested;
    private PlayerHealth playerHealth;

    public void ApplyClass(Classes configuration)
    {
        movespeed = Mathf.Max(0f, configuration.moveSpeed);
        jumpforce = Mathf.Max(0f, configuration.jumpForce);
        airJumps = Mathf.Max(0, configuration.airJumps);
        airJumpSpeed = Mathf.Max(0f, configuration.airJumpSpeed);
        gravity = Mathf.Max(0f, configuration.gravity);
        backwardSpeedMultiplier = Mathf.Clamp01(configuration.backwardSpeedMultiplier);
        jumpRequested = false;
        // Keep air-jump consumption until landing, preventing free jumps on class changes.
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        StartCoroutine(PlayFootStep());
    }


    private void FixedUpdate()
    {
        CheckGround();
        if (gravity > 0f)
        {
            Vector3 automaticGravity = rb.useGravity ? Physics.gravity : Vector3.zero;
            rb.AddForce(Vector3.down * gravity - automaticGravity, ForceMode.Acceleration);
        }
        MovePlayer();
        if (jumpRequested) TryJump();
        jumpRequested = false;
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed && Time.timeScale > 0f &&
            (playerHealth == null || playerHealth.health > 0)) jumpRequested = true;
    }

    private void OnDisable()
    {
        jumpRequested = false;
        moveInput = Vector2.zero;
    }

    private void TryJump()
    {
        if (!isgrounded && usedAirJumps >= airJumps) return;
        float speed = isgrounded ? jumpforce : airJumpSpeed;
        if (!isgrounded) usedAirJumps++;
        // Set vertical speed so a falling Scout gets a full air jump, independent of mass.
        Vector3 velocity = rb.linearVelocity;
        velocity.y = speed;
        rb.linearVelocity = velocity;
        isgrounded = false;
    }

    void CheckGround()
    {
        // Ignore the ground probe during ascent: it overlaps the floor just after takeoff.
        isgrounded = groundchek != null && rb.linearVelocity.y <= 0.01f &&
            Physics.CheckSphere(groundchek.position, grounddistance, groundMask, QueryTriggerInteraction.Ignore);
        if (isgrounded) usedAirJumps = 0;
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void MovePlayer()
    {
        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        direction.Normalize();
        if (isgrounded && moveInput.y < 0f)
        {
            // TF2 limits the backwards component without slowing sideways movement.
            float backwards = Vector3.Dot(direction, transform.forward);
            if (backwards < -backwardSpeedMultiplier)
                direction += transform.forward * (-backwardSpeedMultiplier - backwards);
        }
        rb.linearVelocity = new Vector3(direction.x * movespeed, rb.linearVelocity.y, direction.z * movespeed);
    }

    IEnumerator PlayFootStep()
    {
        while(true)
        {
            if(rb.linearVelocity.magnitude > 0.1f && isgrounded)
            {
                AudioManager.Instance.PlaySFX(footStepSFX);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
