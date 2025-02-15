using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Particles 
    public ParticleSystem jumpDust;
    public ParticleSystem slideDust;
    private Transform PlayerPosition;

    // Animation (dont know if i even need that)
    public Animator animator;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;
    Rigidbody rb;

    [Header("Movement")]
    [Tooltip("Speed of the Player when walking")]
    public float walkSpeed;
    [Tooltip("Speed of the Player when sprinting")]
    public float sprintSpeed;
    [Tooltip("This determines how much the Player gets dragged across the ground, if the value is set too high, the Player will feel really heavy and therefore move considerably slow")]
    public float groundDrag;
    private float moveSpeed;

    public bool readyToJump;
    public bool isCrouching;
    public bool isSprinting;

    [Header("Crouching")]
    [Tooltip("Speed of the Player when crouching")]
    public float crouchSpeed;
    private Vector3 playerScaleCrouch = new Vector3(1f, 0.5f, 1f);
    private Vector3 playerScaleNormal = new Vector3(1f, 1f, 1f);
    private RaycastHit obstacleHit1;
    private RaycastHit obstacleHit2;
    private RaycastHit obstacleHit3;
    private RaycastHit obstacleHit4;

    bool crouchingObstacle;

    [Header("Jumping")]
    [Tooltip("The Force that will be applied to the Player when jumping")]
    public float jumpForce;
    [Tooltip("A Cooldown that deactivates the Jump Function until its active again")]
    public float jumpCooldown;
    [Tooltip("This controls the ability to move freely in the Air (low value = less agility) (high value = more agility)")]
    public float airMovement;

    [Header("Sliding")]
    [Tooltip("Max sliding time if not on slope")]
    public float maxSlideTime;
    [Tooltip("Max sliding speed on slope")]
    public float maxSlideSlope;
    [Tooltip("Speed of the Player when sliding")]
    public float slideSpeed;
    private float accumulatedForce = 0;
    private float slideTimer;
    private float Momentum;
    private float accumulatedForceMax;
    private bool notOnSlope;

    bool isSliding;
    bool slidingUnderObstacle;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode slideKey = KeyCode.C;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public Transform orientation;

    public bool isGrounded;

    [Header("Slope Handling")]
    [Tooltip("The max angle of a slope the Player can handle")]
    public float maxSlopeAngle;
    [Tooltip("Speed of the Player when walking on Slope")]
    public float SlopeMovementSpeed;
    private RaycastHit slopeHit;

    private void Start()
    {
        // speed
        moveSpeed = walkSpeed;
        sprintSpeed = walkSpeed - walkSpeed + sprintSpeed;
        crouchSpeed = walkSpeed - walkSpeed + crouchSpeed;

        // bools
        isSprinting = false;
        readyToJump = true;

        // else
        rb = GetComponent<Rigidbody>();

        // Freeze Player to not fall over lol
        rb.freezeRotation = true;
    }

    private void Update()
    {
        MyInput();
        playerAnimation();
        SpeedControl();
        HandleSlipperyMovement();
        Jump();
        UnderGameObject();
        Crouch();
        StartSlide();
        StopSlide();
        Sprint();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        SlidingMovement();
    }

    private void MyInput()
    {
        // A and D keys
        horizontalInput = Input.GetAxisRaw("Horizontal");
        // W and S keys
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void playerAnimation()
    {
        if (moveDirection == Vector3.zero)
        {
            animator.SetBool("isIdle", true);
            animator.SetBool("isWalking", false);
        }
        else if (Input.GetKey(KeyCode.W))
        {
            animator.SetBool("isIdle", false);
            animator.SetBool("isWalking", true);
        }
    }

    private void MovePlayer()
    {
        // Calculating Direction of Movement
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // On slope
        if (OnSlope())
        {
            rb.AddForce(SlopeMovementSpeed * moveSpeed * GetSlopeMoveDirection(moveDirection), ForceMode.Force);

            // turn gravity off while on slope
            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        // on Ground
        else if (isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }

        // in Air
        else if (!isGrounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMovement, ForceMode.Force);
        }
    }

    // Limit speed
    private void SpeedControl()
    {
        // limiting speed on slope
        if (OnSlope())
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }

        // Gets only horizontal components of Speed
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // limit velocity if necessary
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void HandleSlipperyMovement()
    {
        // ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        // handle drag
        if (isGrounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;
    }

    private void Jump()
    {
        // if normal jump
        if (Input.GetKeyDown(jumpKey) && !isSliding && readyToJump && isGrounded && !isCrouching)
        {
            readyToJump = false;

            // Reset y velocity
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // What the Sigma
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            CreateDust();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Sprint()
    {
        // sprinting movement
        if (!isSliding && !isCrouching && Input.GetKey(sprintKey) && isGrounded || !isSliding && !isCrouching && Input.GetKey(sprintKey) && !readyToJump)
        {
            isSprinting = true;
            moveSpeed = sprintSpeed;
        }

        // mostly fixes bugs      
        if (isSliding && Input.GetKeyUp(sprintKey)
            || !readyToJump && Input.GetKeyUp(sprintKey)
            || readyToJump && Input.GetKeyUp(sprintKey)
            || readyToJump && !isSliding && !isCrouching && Input.GetKeyUp(sprintKey) && isGrounded)
        {
            isSprinting = false;
            moveSpeed = walkSpeed;
        }
    }

    private void Crouch()
    {
        // Normal crouching || other || crouching when sprinting backwards and pressing slide key 
        if (!isSprinting && crouchingObstacle && !isSliding && Input.GetKeyDown(slideKey)
            || !isSprinting && !isSliding && Input.GetKeyDown(slideKey)
            || isSprinting && !isSliding && Input.GetKey(KeyCode.S) && Input.GetKeyDown(slideKey) && isGrounded
            || slidingUnderObstacle
            || horizontalInput == 0 && verticalInput == 0 && transform.localScale == playerScaleNormal && !isSliding && !crouchingObstacle && !isSprinting && Input.GetKey(slideKey))
        {
            isCrouching = true;

            transform.localScale = playerScaleCrouch;
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse);

            moveSpeed = crouchSpeed;
        }

        // stand up
        if (readyToJump && !isSprinting && !isSliding && !crouchingObstacle && Input.GetKey(slideKey) == false)
        {
            isCrouching = false;

            transform.localScale = playerScaleNormal;
            moveSpeed = walkSpeed;
        }

        if (isCrouching && isSprinting && !isSliding && !crouchingObstacle && Input.GetKey(slideKey) == false)
        {
            isCrouching = false;

            transform.localScale = playerScaleNormal;
            moveSpeed = walkSpeed;
        }

        if (!crouchingObstacle && !isCrouching && readyToJump && !isSprinting && !isSliding && moveSpeed == crouchSpeed)
        {
            moveSpeed = walkSpeed;
        }
    }

    private void UnderGameObject()
    {
        // [Crouching or Sliding under GameObject]
        // UpperRaycast on left and right side of Player
        Vector3 raycastOrigin1 = transform.position + new Vector3(0.5f, 0, 0);
        Vector3 raycastOrigin2 = transform.position + new Vector3(-0.5f, 0, 0);

        // UpperRaycast on forward and downward side of Player
        Vector3 raycastOrigin3 = transform.position + new Vector3(0, 0, 0.5f);
        Vector3 raycastOrigin4 = transform.position + new Vector3(0, 0, -0.5f);

        // if one raycast detects Object above, then set bool to true
        if (transform.localScale == playerScaleCrouch && isGrounded && Physics.Raycast(raycastOrigin1, Vector3.up, out obstacleHit1, playerHeight * 0.5f + 0.5f) ||
        transform.localScale == playerScaleCrouch && isGrounded && Physics.Raycast(raycastOrigin2, Vector3.up, out obstacleHit2, playerHeight * 0.5f + 0.5f) ||
        transform.localScale == playerScaleCrouch && isGrounded && Physics.Raycast(raycastOrigin3, Vector3.up, out obstacleHit3, playerHeight * 0.5f + 0.5f) ||
        transform.localScale == playerScaleCrouch && isGrounded && Physics.Raycast(raycastOrigin4, Vector3.up, out obstacleHit4, playerHeight * 0.5f + 0.5f))
        {
            crouchingObstacle = true;
        }
        else
        {
            crouchingObstacle = false;
        }
    }

    private void StartSlide()
    {
        if (isSprinting && !isCrouching && Input.GetKey(KeyCode.S) == false)
        {
            if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && isGrounded)
            {
                slideTimer = maxSlideTime;

                isSliding = true;

                transform.localScale = playerScaleCrouch;
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            }
        }
    }

    private void SlidingMovement()
    {
        if (isSliding && !slidingUnderObstacle)
        {
            // sliding normal :(
            if (!OnSlope() || rb.velocity.y > -0.1f && accumulatedForce == slideSpeed * 10f)
            {
                // bool fixes bug (after sliding slope, player can slide to other slope which causes unexpected behaviour)
                notOnSlope = true;

                rb.AddForce(orientation.forward * accumulatedForce, ForceMode.Force);
                slideTimer -= Time.deltaTime;
                CreateDust();
            }
            else if (notOnSlope)
            {
                slideTimer -= Time.deltaTime;
            }

            if (slideTimer <= 0)
            {
                StopSlide();
            }

            // sliding slope :) (Do NOT change)
            if (OnSlope() && rb.velocity.y < 0.1f && !notOnSlope)
            {
                Momentum += Time.deltaTime / 7f;

                accumulatedForce += slideSpeed * 10f * Momentum;
                accumulatedForceMax = accumulatedForce;

                rb.AddForce(GetSlopeMoveDirection(orientation.forward) * accumulatedForce, ForceMode.Force);

                if (accumulatedForce >= maxSlideSlope)
                {
                    Momentum = 0;
                    accumulatedForce = accumulatedForceMax;
                }

                if (accumulatedForce + sprintSpeed >= maxSlideSlope)
                {
                    Momentum = 0;
                    accumulatedForce = accumulatedForceMax;
                }
            }

            // Decreasing slide speed when on Ground (Do NOT change)
            if (!OnSlope() && accumulatedForce > slideSpeed * 2)
            {
                notOnSlope = true;
                Momentum = 0;

                accumulatedForce -= Time.deltaTime * accumulatedForce * 4f;
                rb.AddForce(GetSlopeMoveDirection(orientation.forward) * accumulatedForce, ForceMode.Force);
            }
        }

        // reset values
        if (!isSliding)
        {
            Momentum = 0;
            accumulatedForce = slideSpeed * 10f;
        }
    }

    private void StopSlide()
    {
        if (crouchingObstacle && !isCrouching && isSliding && slideTimer <= 0 || crouchingObstacle && !isCrouching && isSliding && Input.GetKeyUp(slideKey))
        {
            notOnSlope = false;

            isSliding = false;
            slidingUnderObstacle = true;
        }

        else
        {
            slidingUnderObstacle = false;
        }

        // sliding normal
        if (!crouchingObstacle && !isCrouching && isSliding && slideTimer <= 0 || !crouchingObstacle && !isCrouching && isSliding && Input.GetKeyUp(slideKey))
        {
            notOnSlope = false;

            isSliding = false;
            transform.localScale = playerScaleNormal;

        }
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    void CreateDust()
    {
        if(!readyToJump)
        { 
            // Instantiate a new particle effect at the player's position and rotation
            ParticleSystem effect = Instantiate(jumpDust, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), jumpDust.transform.rotation);

            // Detach it so it doesnt follow the player
            effect.transform.parent = null;

            // Play the particle effect (optiomnal, as it plays automaticalny)
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + 4);
        }

        if (isSliding)
        {
            // Instantiate a new particle effect at the player's position and rotation
            ParticleSystem effect = Instantiate(slideDust, new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z), slideDust.transform.rotation);

            // Detach it so it doesn't follow the player
            effect.transform.parent = null;

            // Play the particle effect (optional, as it plays automatically)
            effect.Play();

            // What the Sigma
            Destroy(effect.gameObject, effect.main.duration + 1);
        }
    }
}