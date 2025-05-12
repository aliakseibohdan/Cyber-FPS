using UnityEngine;

public class WallRun : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Transform orientation;

    [Header("Detection")]
    [SerializeField] private float wallDistance = .6f;
    [SerializeField] private float minimumJumpHeight = 1.5f;

    [Header("Wall Running")]
    [SerializeField] private float wallRunGravity = 1f;
    [SerializeField] private float wallRunJumpForce = 6f;
    [SerializeField] private float wallRunJumpForceModifier = 100f;

    [Header("Camera")]
    [SerializeField] private Camera cam;
    [SerializeField] private float fov = 90f;
    [SerializeField] private float wallRunfov = 100f;
    [SerializeField] private float wallRunfovTime = 20f;
    [SerializeField] private float camTilt = 20f;
    [SerializeField] private float camTiltTime = 20f;

    public float Tilt { get; private set; }

    private bool wallLeft = false;
    private bool wallRight = false;

    RaycastHit leftWallHit;
    RaycastHit rightWallHit;

    private Rigidbody rb;

    bool CanWallRun()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minimumJumpHeight);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void CheckWall()
    {
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance);
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance);
    }

    private void Update()
    {
        CheckWall();

        if (CanWallRun())
        {
            if (wallLeft)
            {
                StartWallRun();
                Debug.Log("wall running on the left");
            }
            else if (wallRight)
            {
                StartWallRun();
                Debug.Log("wall running on the right");
            }
            else
            {
                StopWallRun();
            }
        }
        else
        {
            StopWallRun();
        }
    }

    void StartWallRun()
    {
        rb.useGravity = false;

        rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Force);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, wallRunfov, wallRunfovTime * Time.deltaTime);

        if (wallLeft)
            Tilt = Mathf.Lerp(Tilt, -camTilt, camTiltTime * Time.deltaTime);
        else if (wallRight)
            Tilt = Mathf.Lerp(Tilt, camTilt, camTiltTime * Time.deltaTime);


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (wallLeft)
            {
                Vector3 wallRunJumpDirection = transform.up + leftWallHit.normal;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.AddForce(wallRunJumpForceModifier * wallRunJumpForce * wallRunJumpDirection, ForceMode.Force);
            }
            else if (wallRight)
            {
                Vector3 wallRunJumpDirection = transform.up + rightWallHit.normal;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.AddForce(wallRunJumpForceModifier * wallRunJumpForce * wallRunJumpDirection, ForceMode.Force);
            }
        }
    }

    void StopWallRun()
    {
        rb.useGravity = true;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, wallRunfovTime * Time.deltaTime);
        Tilt = Mathf.Lerp(Tilt, 0, camTiltTime * Time.deltaTime);
    }
}