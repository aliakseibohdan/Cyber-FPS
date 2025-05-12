using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] WallRun wallRun;

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
    }

    private void Update()
    {
        mouseX = Input.GetAxisRaw("Mouse X");
        mouseY = Input.GetAxisRaw("Mouse Y");

        yRotation += mouseX * sensX * mouseSensitivityMultiplier;
        xRotation -= mouseY * sensY * mouseSensitivityMultiplier;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraHolder.transform.rotation = Quaternion.Euler(xRotation, yRotation, wallRun.Tilt);
        orientation.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}