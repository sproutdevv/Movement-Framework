using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform Orientation;

    float xRotation;
    float yRotation;

    public PlayerMovement PlayerMovement;

    public float cameraSprintFOV;
    private float cameraStandardFOV;

    public Transform player;

    private void Start()
    {
        // standard camera config
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // sprinting fov config
        PlayerMovement = FindObjectOfType<PlayerMovement>();

        cameraStandardFOV = Camera.main.fieldOfView;
    }
   
    private void LateUpdate()
    {
        transform.position = player.transform.position + new Vector3(0, 0.5f, 0);

        // standard camera config
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        Orientation.rotation = Quaternion.Euler(0, yRotation, 0);

        // sprinting fov config
        // variables for some reason not working for targetFov, included temporary fix by adding numbers (future fixing necessary)
        float targetFov = PlayerMovement.isSprinting ? cameraSprintFOV : cameraStandardFOV;
        Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, targetFov, Time.deltaTime * 7f);
    }
}



