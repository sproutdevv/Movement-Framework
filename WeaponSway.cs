using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Rotation Sway Settings")]
    [SerializeField] private float smooth;
    [SerializeField] private float swayMultiplier;

    public PlayerMovement PlayerMovement;

    [Header("Movement Sway Settings")]
    public float moveLeftZ;
    public float moveRightZ;

    private float startValueLeft;
    private float startValueRight;

    private float sprintValueLeft;
    private float sprintValueRight;

    public void Start()
    {
        startValueLeft = moveLeftZ;
        startValueRight = moveRightZ;

        sprintValueLeft = moveLeftZ * 2;
        sprintValueRight = moveRightZ * 2;
    }

    private void Update()
    {
        // get mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * swayMultiplier;
        float mouseY = Input.GetAxisRaw("Mouse Y") * swayMultiplier;

        // z-achsis config if the player sprints
        if (PlayerMovement.isSprinting)
        {
            moveLeftZ = sprintValueLeft;
            moveRightZ = sprintValueRight;
        }
        else
        {
            moveLeftZ = startValueLeft;
            moveRightZ = startValueRight;
        }
 
        // calculate target rotation
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY , Vector3.right * 10);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX , Vector3.up * 10);

        // z-achsis rotation config
        float moveRotationZ = Input.GetKey(KeyCode.A) ? moveLeftZ : 0;
        moveRotationZ += Input.GetKey(KeyCode.D) ? moveRightZ : 0;
        Quaternion rotationZ = Quaternion.Euler(0, 0, moveRotationZ);

        Quaternion targetRotation = rotationX * rotationY * rotationZ;

        // rotation
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smooth * Time.deltaTime);       
    }
}
