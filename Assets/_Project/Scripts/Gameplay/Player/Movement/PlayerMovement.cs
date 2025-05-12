using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private readonly float playerHeight = 2f;

    [SerializeField] Transform orientation;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float airMultiplier = 0.4f;
    private readonly float movementMultiplier = 10f;

    [Header("Sprinting")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 6f;
    [SerializeField] float acceleration = 10f;

    [Header("Jumping")]
    public float jumpForce = 14f;

    [Header("Keybinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Drag")]
    [SerializeField] float groundDrag = 6f;
    [SerializeField] float airDrag = 2f;

    [Header("Ground Detection")]
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.1f;
    public bool IsGrounded { get; private set; }

    [Header("Wind Particles")]
    [SerializeField] ParticleSystem windStripesParticles;
    [SerializeField] float maxEmissionRate = 50f;
    [SerializeField] float emissionSmoothingFactor = 5f;

    float horizontalMovement;
    float verticalMovement;

    Vector3 moveDirection;
    Vector3 slopeMoveDirection;

    Rigidbody rb;
    RaycastHit slopeHit;

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight / 2 + 0.5f))
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
        }
        return false;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (windStripesParticles != null)
        {
            var emission = windStripesParticles.emission;
            emission.rateOverTime = 0f;
        }
        else
        {
            Debug.LogWarning("Wind Stripes Particle System not assigned.", this);
        }
    }

    private void Update()
    {
        IsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        MyInput();
        ControlDrag();
        ControlSpeed();
        ControlWindStripes();

        if (Input.GetKeyDown(jumpKey) && IsGrounded)
        {
            Jump();
        }

        if (OnSlope())
        {
            slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
        }
    }

    void MyInput()
    {
        horizontalMovement = Input.GetAxisRaw("Horizontal");
        verticalMovement = Input.GetAxisRaw("Vertical");

        moveDirection = orientation.forward * verticalMovement + orientation.right * horizontalMovement;
        moveDirection.Normalize();
    }

    void Jump()
    {
        if (IsGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    void ControlSpeed()
    {
        float targetSpeed = Input.GetKey(sprintKey) && IsGrounded ? sprintSpeed : walkSpeed;
        moveSpeed = Mathf.Lerp(moveSpeed, targetSpeed, acceleration * Time.deltaTime);
    }

    void ControlDrag()
    {
        rb.linearDamping = IsGrounded ? groundDrag : airDrag;
    }

    void ControlWindStripes()
    {
        if (windStripesParticles == null) return;

        float currentHorizontalSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

        float speedRatio = 0f;
        float speedRange = sprintSpeed - walkSpeed;

        if (speedRange > 0.01f)
        {
            speedRatio = Mathf.Clamp01((currentHorizontalSpeed - walkSpeed) / speedRange);
        }
        else if (currentHorizontalSpeed >= sprintSpeed)
        {
            speedRatio = 1f;
        }

        float targetEmission = speedRatio * maxEmissionRate;

        var emission = windStripesParticles.emission;
        float currentEmission = emission.rateOverTime.constant;

        emission.rateOverTime = Mathf.Lerp(currentEmission, targetEmission, Time.deltaTime * emissionSmoothingFactor);

        if (targetEmission > 0.1f && !windStripesParticles.isPlaying)
        {
            windStripesParticles.Play();
        }
        else if (targetEmission <= 0.1f && windStripesParticles.isPlaying)
        {

            windStripesParticles.Stop();
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        if (IsGrounded && !OnSlope())
        {
            rb.AddForce(movementMultiplier * moveSpeed * moveDirection.normalized, ForceMode.Acceleration);
        }
        else if (IsGrounded && OnSlope())
        {
            rb.AddForce(movementMultiplier * moveSpeed * slopeMoveDirection.normalized, ForceMode.Acceleration);
        }
        else if (!IsGrounded)
        {
            rb.AddForce(airMultiplier * movementMultiplier * moveSpeed * moveDirection.normalized, ForceMode.Acceleration);
        }
    }
}