using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CapsuleCollider playerCollider;
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

    [Header("Crouching")]
    [SerializeField] float crouchSpeed = 2f;
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] float standingHeight = 2f;
    [SerializeField] float crouchTransitionSpeed = 10f;
    [SerializeField] Transform playerCameraTransform;
    [SerializeField] Vector3 standingCameraPosition;
    [SerializeField] Vector3 crouchingCameraPosition;
    private bool isCrouching = false;

    [Header("Sliding")]
    [SerializeField] float slideForce = 500f;
    [SerializeField] float slideDuration = 0.75f;
    [SerializeField] float slideJumpBoostMultiplier = 1.5f;
    private bool isSliding = false;
    private float slideTimer;

    [Header("Keybinds")]
    [SerializeField] KeyCode jumpKey = KeyCode.Space;
    [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] KeyCode crouchKey = KeyCode.C;

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

        playerCollider = GetComponentInChildren<CapsuleCollider>();
        standingHeight = playerCollider.height;
        standingCameraPosition = playerCameraTransform.localPosition;
        crouchingCameraPosition = new Vector3(playerCameraTransform.localPosition.x,
                                              standingCameraPosition.y - (standingHeight - crouchHeight),
                                              playerCameraTransform.localPosition.z);

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
        HandleCrouchInput();

        if (Input.GetKeyDown(jumpKey) && IsGrounded)
        {
            Jump();
        }

        if (isCrouching && Input.GetKey(sprintKey) && !isSliding)
        {
            if (!Physics.Raycast(transform.position, Vector3.up, standingHeight - playerCollider.height + 0.1f))
            {
                isCrouching = false;
            }
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
            float currentJumpForce = jumpForce;
            if (isSliding)
            {
                currentJumpForce *= slideJumpBoostMultiplier;
                StopSlide();
            }
            rb.AddForce(transform.up * currentJumpForce, ForceMode.Impulse);
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

    void HandleCrouchInput()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            if (isCrouching)
            {
                if (!Physics.Raycast(transform.position, Vector3.up, standingHeight - crouchHeight + 0.1f))
                {
                    isCrouching = false;
                }
            }
            else
            {
                bool isSprinting = Input.GetKey(sprintKey);
                if (isSprinting)
                {
                    StartSlide();
                }
                else
                {
                    isCrouching = true;
                    ApplyCrouchState();
                }
            }
        }
    }

    void ApplyCrouchState()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        Vector3 targetCameraPos = isCrouching ? crouchingCameraPosition : standingCameraPosition;

        playerCollider.height = Mathf.Lerp(playerCollider.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        playerCollider.center = Vector3.Lerp(playerCollider.center, new Vector3(0, targetHeight / 2f, 0), Time.deltaTime * crouchTransitionSpeed);

        if (playerCameraTransform != null)
        {
            playerCameraTransform.localPosition = Vector3.Lerp(playerCameraTransform.localPosition, targetCameraPos, Time.deltaTime * crouchTransitionSpeed);
        }

        if (isCrouching && !isSliding)
        {
            moveSpeed = Mathf.Lerp(moveSpeed, crouchSpeed, acceleration * Time.deltaTime);
        }
    }

    void StartSlide()
    {
        isSliding = true;
        isCrouching = true;
        slideTimer = slideDuration;
        rb.AddForce(moveDirection.normalized * slideForce, ForceMode.Force);
    }

    void HandleSliding()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0 || !Input.GetKey(crouchKey) || rb.linearVelocity.magnitude < (walkSpeed * 0.8f))
            {
                StopSlide();
            }
        }
    }

    void StopSlide()
    {
        isSliding = false;
        if (!Input.GetKey(crouchKey))
        {
            isCrouching = false;
        }
    }
}