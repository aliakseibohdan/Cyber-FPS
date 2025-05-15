using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CapsuleCollider playerCollider;

    public Transform orientation;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float airMultiplier = 0.4f;
    private readonly float movementMultiplier = 10f;

    [Header("Sprinting")]
    [SerializeField] float walkSpeed = 8f;
    [SerializeField] float sprintSpeed = 15f;
    [SerializeField] float acceleration = 10f;

    [Header("Jumping")]
    public float jumpForce = 500f;

    [Header("Crouching")]
    [SerializeField] float crouchSpeedMultiplier = 0.5f;
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] float standingHeight = 2f;
    [SerializeField] float crouchTransitionSpeed = 10f;
    private bool isCrouching = false;

    [Header("Camera Control (Crouch)")]
    [SerializeField] Transform cameraPosition;
    [SerializeField] Vector3 standingCameraLocalPos = new(0, 1.8f, 0);
    [SerializeField] Vector3 crouchingCameraLocalPos = new(0, 0.8f, 0);

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
    [SerializeField] float slideDrag = 1f;
    [SerializeField] float airDrag = 2f;

    [Header("Ground Detection")]
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float groundDistance = 0.2f;
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
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, (playerCollider != null ? playerCollider.height : standingHeight) / 2 + 0.5f))
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

        if (playerCollider == null)
        {
            Debug.LogError("PlayerMovement: CapsuleCollider not found!", this);
            enabled = false;
            return;
        }

        standingHeight = playerCollider.height;

        if (windStripesParticles != null)
        {
            var emission = windStripesParticles.emission;
            emission.rateOverTime = 0f;
        }
        else
        {
            Debug.LogWarning("Wind Stripes Particle System not assigned.", this);
        }

        moveSpeed = walkSpeed;
    }

    private void Update()
    {
        if (playerCollider == null) return;

        IsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        Debug.DrawRay(groundCheck.position, new Vector3(0, -groundDistance, 0), Color.red, duration: .3f);

        MyInput();
        HandleCrouchInput();
        ApplyStance();
        ControlSpeed();
        ControlDrag();
        ControlWindStripes();
        HandleSliding();

        if (Input.GetKeyDown(jumpKey) && IsGrounded)
        {
            Jump();
        }

        if (isCrouching && !isSliding && Input.GetKey(sprintKey))
        {
            if (CanStandUp())
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
        float currentBaseSpeed;
        currentBaseSpeed = Input.GetKey(sprintKey) && IsGrounded && !isSliding ? sprintSpeed : walkSpeed;

        if (isCrouching && !isSliding)
        {
            currentBaseSpeed *= crouchSpeedMultiplier;
        }

        moveSpeed = Mathf.Lerp(moveSpeed, currentBaseSpeed, acceleration * Time.deltaTime);
    }

    void ControlDrag()
    {
        if (isSliding)
        {
            rb.linearDamping = slideDrag;
        }
        else
        {
            rb.linearDamping = IsGrounded ? groundDrag : airDrag;
        }
    }

    private void FixedUpdate()
    {
        if (playerCollider == null) return;
        MovePlayer();
    }

    void MovePlayer()
    {
        if (isSliding) return;

        Vector3 directionToMove = OnSlope() && IsGrounded ? slopeMoveDirection.normalized : moveDirection.normalized;

        if (IsGrounded)
        {
            rb.AddForce(movementMultiplier * moveSpeed * directionToMove, ForceMode.Acceleration);
        }
        else // In Air
        {
            rb.AddForce(airMultiplier * movementMultiplier * moveSpeed * directionToMove, ForceMode.Acceleration);
        }
    }

    void HandleCrouchInput()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            if (isSliding) return;

            if (isCrouching)
            {
                if (CanStandUp())
                {
                    isCrouching = false;
                    Debug.Log("Trying to stand up. CanStandUp: true");
                }
                else Debug.Log("Trying to stand up. CanStandUp: false - Obstructed");
            }
            else
            {
                bool isSprintingCurrently = Input.GetKey(sprintKey) && (new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude > walkSpeed * 0.9f);
                if (isSprintingCurrently && IsGrounded)
                {
                    StartSlide();
                }
                else
                {
                    isCrouching = true;
                    Debug.Log("Trying to crouch normally.");
                }
            }
        }
    }

    void ApplyStance()
    {
        float targetActualHeight = isCrouching ? crouchHeight : standingHeight;
        float currentColliderHeight = playerCollider.height;

        playerCollider.height = Mathf.Lerp(currentColliderHeight, targetActualHeight, Time.deltaTime * crouchTransitionSpeed);
        Vector3 targetColliderCenter = new(playerCollider.center.x, playerCollider.height / 2f, playerCollider.center.z);
        playerCollider.center = Vector3.Lerp(playerCollider.center, targetColliderCenter, Time.deltaTime * crouchTransitionSpeed);

        if (cameraPosition != null)
        {
            Vector3 targetCameraPos = isCrouching ? crouchingCameraLocalPos : standingCameraLocalPos;
            cameraPosition.localPosition = Vector3.Lerp(cameraPosition.localPosition, targetCameraPos, Time.deltaTime * crouchTransitionSpeed);
        }
    }

    private bool CanStandUp()
    {
        float radius = playerCollider.radius;
        Vector3 castStart = transform.position + Vector3.up * (crouchHeight + radius + 0.05f);
        float castDistance = standingHeight - crouchHeight - radius;

        if (castDistance <= 0.01f) return true;

        Debug.DrawRay(castStart, Vector3.up * castDistance, Color.red, 2f);
        return !Physics.SphereCast(castStart, radius, Vector3.up, out _, castDistance, ~groundMask, QueryTriggerInteraction.Ignore);
    }

    void StartSlide()
    {
        if (!IsGrounded) return;

        isSliding = true;
        isCrouching = true;
        slideTimer = slideDuration;
        Vector3 slideDirection = moveDirection.magnitude > 0.1f ? moveDirection.normalized : orientation.forward;
        rb.AddForce(slideDirection * slideForce, ForceMode.Impulse);
        Debug.Log("Started Sliding");
    }

    void HandleSliding()
    {
        if (!isSliding) return;

        slideTimer -= Time.deltaTime;

        // Stop sliding if timer runs out, crouch key is released, speed is too low, or no longer grounded
        if (slideTimer <= 0 || !Input.GetKey(crouchKey) || rb.linearVelocity.magnitude <= walkSpeed || !IsGrounded)
        {
            StopSlide();
        }
    }

    void StopSlide()
    {
        if (!isSliding) return;

        isSliding = false;
        Debug.Log("Stopped Sliding");
        // If crouch key is NOT held after slide, try to stand up
        if (!Input.GetKey(crouchKey))
        {
            if (CanStandUp())
            {
                isCrouching = false;
            }
            // If cannot stand, remain crouched (isCrouching is still true from StartSlide)
        }
        // If crouch key IS still held, player remains crouching (isCrouching is true).
        // ApplyStance() in Update will continue to manage the crouched height.
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
}