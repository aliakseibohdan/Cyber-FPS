using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("Camera Effects")]
    [SerializeField] float maxTiltAngle = 5f;
    [SerializeField] float tiltSpeed = 5f;
    [SerializeField] float fovNormal = 80f;
    [SerializeField] float fovForwardMovement = 75f;
    [SerializeField] float fovBackwardMovement = 85f;
    [SerializeField] float fovChangeSpeed = 5f;
    private float currentTilt = 0f;

    [Header("References")]
    [SerializeField] Camera playerCamera;
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] WallRun wallRun;

    [Header("Mouse Sensetivity")]
    [SerializeField] private float sensX = 150;
    [SerializeField] private float sensY = 150;

    [SerializeField] Transform cameraHolder = null;
    [SerializeField] Transform orientation = null;

    float mouseX;
    float mouseY;

    private readonly float mouseSensitivityMultiplier = 0.01f;

    float xRotation;
    float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        wallRun = GetComponent<WallRun>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        mouseX = Input.GetAxisRaw("Mouse X");
        mouseY = Input.GetAxisRaw("Mouse Y");

        yRotation += mouseX * sensX * mouseSensitivityMultiplier;
        xRotation -= mouseY * sensY * mouseSensitivityMultiplier;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        float targetTilt = -horizontalInput * maxTiltAngle; // Tilt opposite to movement direction
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        float targetFov = fovNormal;
        if (verticalInput > 0.1f) // Moving forward
        {
            targetFov = fovForwardMovement;
        }
        else if (verticalInput < -0.1f) // Moving backward
        {
            targetFov = fovBackwardMovement;
        }

        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * fovChangeSpeed);
        }

        float currentWallRunTilt = (wallRun != null && wallRun.enabled) ? wallRun.Tilt : 0f;
        cameraHolder.transform.localRotation = Quaternion.Euler(xRotation, yRotation, currentTilt + currentWallRunTilt);
        orientation.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}