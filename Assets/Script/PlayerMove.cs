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

    [Range(0.05f, 0.5f)] public float groundProbeRadius = 0.22f;

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
    private PlayerShoothing shooting;
    private Vector3 groundNormal = Vector3.up;
    private float ignoreGroundUntil;

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
        shooting = GetComponent<PlayerShoothing>();
        if (rb != null)
        {
            rb.constraints |= RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
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
        if (shooting != null && shooting.gun != null && shooting.gun.IsSpinning) return;
        if (!isgrounded && usedAirJumps >= airJumps) return;
        float speed = isgrounded ? jumpforce : airJumpSpeed;
        if (!isgrounded) usedAirJumps++;
        // Set vertical speed so a falling Scout gets a full air jump, independent of mass.
        Vector3 velocity = rb.linearVelocity;
        velocity.y = speed;
        rb.linearVelocity = velocity;
        isgrounded = false;
        ignoreGroundUntil = Time.time + 0.12f;
    }

    void CheckGround()
    {
        // A sphere overlap against a wall counted as ground and could suspend the player.
        // Only a downward hit with a walkable normal resets jumps and enables ground control.
        isgrounded = false;
        groundNormal = Vector3.up;
        if (Time.time >= ignoreGroundUntil && groundchek != null &&
            Physics.SphereCast(groundchek.position + Vector3.up * 0.05f, groundProbeRadius,
                Vector3.down, out RaycastHit hit, grounddistance + 0.05f, groundMask,
                QueryTriggerInteraction.Ignore))
        {
            isgrounded = hit.normal.y >= 0.55f;
            if (isgrounded) groundNormal = hit.normal;
        }
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
        float weaponSpeed = shooting != null && shooting.gun != null ? shooting.gun.MovementMultiplier : 1f;
        Vector3 desired = direction * movespeed * weaponSpeed;
        Vector3 velocity = rb.linearVelocity;

        // On a ramp, world-horizontal velocity pushes the capsule into the slope and
        // loses speed. Project the desired movement onto the walkable surface instead.
        if (isgrounded)
        {
            Vector3 slopeDirection = Vector3.ProjectOnPlane(desired, groundNormal);
            if (slopeDirection.sqrMagnitude > 0.001f)
                desired = slopeDirection.normalized * desired.magnitude;
        }

        float vertical = velocity.y;
        if (isgrounded && desired.y > 0f) vertical = Mathf.Max(vertical, desired.y);
        rb.linearVelocity = new Vector3(desired.x, vertical, desired.z);
    }

    IEnumerator PlayFootStep()
    {
        while(true)
        {
            if(rb.linearVelocity.magnitude > 0.1f && isgrounded && footStepSFX != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(footStepSFX);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
