using System.Collections;
using UnityEngine;

public class LedgeGrab : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform playerObj;
    [SerializeField] private Transform cameraPosition;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerMovement playerMovement;
    private CapsuleCollider playerCollider;

    [Header("Detection")]
    [SerializeField] private float ledgeDetectionLength = 0.7f;
    [SerializeField] private float ledgeSphereCastRadius = 0.25f;
    [SerializeField] private float maxLedgeGrabDistance = 1.0f; // Max distance to initiate a grab
    [SerializeField] private float minLedgeHeight = 0.5f; // Minimum height above player to consider a ledge
    [SerializeField] private float maxLedgeHeight = 2.5f; // Maximum height to grab
    [SerializeField] private float minWallAngle = 80f; // Minimum angle for wall to be considered vertical enough
    [SerializeField] private LayerMask ledgeGrabMask;

    [Header("Ledge Grab Settings")]
    [SerializeField] private float ledgeGrabDuration = 0.4f;  // Time to position player during grab
    [SerializeField] private float pullUpDuration = 0.6f;     // Time to complete pull up animation
    [SerializeField] private float horizontalOffset = 0.25f;  // Distance from wall when hanging
    [SerializeField] private float verticalHangingOffset = 0.5f; // Distance from ledge when hanging
    [SerializeField] private float finalHeightOffset = 0.2f;  // Final height offset when standing on ledge

    [Header("Input")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;

    // State variables
    private bool isLedgeDetected = false;
    private bool isHanging = false;
    private bool isPullingUp = false;
    private Vector3 ledgePos;
    private Vector3 wallNormal;
    private Vector3 hangingPosition;
    private Vector3 finalPosition;

    private void Start()
    {
        if (playerObj == null) playerObj = transform;
        if (orientation == null) orientation = GetComponent<PlayerMovement>()?.orientation;
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        playerCollider = GetComponentInChildren<CapsuleCollider>();

        if (rb == null || playerMovement == null || playerCollider == null || orientation == null)
        {
            Debug.LogError("LedgeGrab: Required components not assigned!", this);
            enabled = false;
        }

        // If ledgeGrabMask is not set, default to same as ground mask
        if (ledgeGrabMask.value == 0)
        {
            ledgeGrabMask = LayerMask.GetMask("Default");
            Debug.LogWarning("LedgeGrab: ledgeGrabMask not set, defaulting to 'Default' layer.", this);
        }
    }

    private void Update()
    {
        if (isHanging || isPullingUp)
        {
            // If hanging and jump pressed, start pull up
            if (isHanging && Input.GetKeyDown(jumpKey))
            {
                StartCoroutine(PullUpCoroutine());
            }
            return;
        }

        DetectLedge();

        if (isLedgeDetected && !playerMovement.IsGrounded &&
            Input.GetKeyDown(jumpKey) && !isHanging && !isPullingUp)
        {
            // Check if we're close enough to the ledge
            float distanceToLedge = Vector3.Distance(transform.position, hangingPosition);
            if (distanceToLedge <= maxLedgeGrabDistance)
            {
                StartCoroutine(LedgeGrabCoroutine());
            }
        }
    }

    private void DetectLedge()
    {
        isLedgeDetected = false;

        if (playerMovement.IsGrounded || isHanging || isPullingUp)
            return;

        // Cast a ray forward from upper part of the collider to detect edges
        float castHeight = transform.position.y + playerCollider.height * 0.5f;
        Vector3 castStart = new(transform.position.x, castHeight, transform.position.z);
        Vector3 castDirection = orientation.forward;

        Debug.DrawRay(castStart, castDirection * ledgeDetectionLength, Color.red);

        // First check if there's a wall in front
        if (Physics.SphereCast(castStart, ledgeSphereCastRadius, castDirection, out RaycastHit wallHit,
                              ledgeDetectionLength, ledgeGrabMask))
        {
            // Check if the wall is vertical enough
            float wallAngle = Vector3.Angle(wallHit.normal, Vector3.up);
            if (wallAngle < minWallAngle)
                return; // Wall isn't vertical enough

            wallNormal = wallHit.normal;
            Vector3 wallPos = wallHit.point;

            // Now cast up from slightly in front of the wall hit point to detect ledge
            Vector3 upCastStart = wallPos - wallNormal * 0.05f; // Slightly inside the wall
            upCastStart.y = transform.position.y; // Start at player's feet level

            Debug.DrawRay(upCastStart, Vector3.up * maxLedgeHeight, Color.green);

            if (Physics.Raycast(upCastStart, Vector3.up, out RaycastHit ceilingHit, maxLedgeHeight, ledgeGrabMask))
            {
                // Now cast down to find top surface
                Vector3 downCastStart = ceilingHit.point + wallNormal * 0.2f; // Move slightly out from wall
                downCastStart.y += 0.1f; // Move slightly above ceiling hit

                Debug.DrawRay(downCastStart, Vector3.down * 0.3f, Color.blue);

                if (Physics.Raycast(downCastStart, Vector3.down, out RaycastHit ledgeHit, 0.3f, ledgeGrabMask))
                {
                    // Calculate height difference between player and ledge
                    float heightDifference = ledgeHit.point.y - transform.position.y;

                    // Check if the ledge is within grabable height range
                    if (heightDifference >= minLedgeHeight && heightDifference <= maxLedgeHeight)
                    {
                        ledgePos = ledgeHit.point;
                        isLedgeDetected = true;

                        // Calculate hanging and final positions
                        hangingPosition = ledgePos - wallNormal * horizontalOffset;
                        hangingPosition.y = ledgePos.y - verticalHangingOffset;

                        finalPosition = ledgePos + wallNormal * 0.5f;
                        finalPosition.y = ledgePos.y + finalHeightOffset;

                        Debug.DrawLine(transform.position, hangingPosition, Color.yellow);
                        Debug.DrawLine(hangingPosition, finalPosition, Color.magenta);
                    }
                }
            }
        }
    }

    private IEnumerator LedgeGrabCoroutine()
    {
        // Disable player movement and gravity
        playerMovement.enabled = false;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        // Transition to hanging position
        float elapsedTime = 0;
        transform.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);

        // Calculate rotation to face away from wall
        Quaternion targetRot = Quaternion.LookRotation(-wallNormal);

        while (elapsedTime < ledgeGrabDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / ledgeGrabDuration;

            // Smooth the movement (ease in-out)
            float smoothT = t * t * (3f - 2f * t);

            transform.SetPositionAndRotation(Vector3.Lerp(startPos, hangingPosition, smoothT), Quaternion.Slerp(startRot, targetRot, smoothT));
            yield return null;
        }

        // Ensure we reach the exact position
        transform.SetPositionAndRotation(hangingPosition, targetRot);
        isHanging = true;

        Debug.Log("Grabbed ledge");
    }

    private IEnumerator PullUpCoroutine()
    {
        isHanging = false;
        isPullingUp = true;

        // Transition to final position
        float elapsedTime = 0;
        Vector3 startPos = transform.position;

        while (elapsedTime < pullUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / pullUpDuration;

            // Use a different easing function for pull-up (ease out quad)
            float smoothT = t * (2 - t);

            transform.position = Vector3.Lerp(startPos, finalPosition, smoothT);

            yield return null;
        }

        // Ensure we reach the exact position
        transform.position = finalPosition;

        // Re-enable player movement
        rb.isKinematic = false;
        rb.useGravity = true;
        playerMovement.enabled = true;
        isPullingUp = false;

        Debug.Log("Pulled up from ledge");
    }

    // Allow external cancellation of ledge hanging
    public void CancelLedgeGrab()
    {
        if (isHanging || isPullingUp)
        {
            StopAllCoroutines();

            isHanging = false;
            isPullingUp = false;

            rb.isKinematic = false;
            rb.useGravity = true;
            playerMovement.enabled = true;

            Debug.Log("Ledge grab canceled");
        }
    }
}