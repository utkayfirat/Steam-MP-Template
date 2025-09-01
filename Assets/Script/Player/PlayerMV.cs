using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class PlayerMV : NetworkBehaviour
{
    [Header("Camera Support")]
    [SerializeField] private ViewbobbingController viewbobbingController;
    [SerializeField] private float baseFOV;
    [SerializeField] private float sprintFOV;
    [SerializeField] private float fovChangeSpeed;
    

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float crouchSpeed;
    public float groundDrag;
    [SerializeField] PhysicsMaterial noFrictionMat;
    [SerializeField] PhysicsMaterial fullFrictionMat;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public bool isCrouching;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public LayerMask whatIsGround;
    public bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    public Transform orientation;
    public float horizontalInput;
    public float verticalInput;

    Vector3 moveDirection;
    public Transform groundPos;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera playerCamera;

    [Header("Player Models 1/3")]
    [SerializeField] private GameObject[] character_3Ps;
    [SerializeField] private GameObject character_1P;

    [Header("Animator")]
    [SerializeField] private Animator OnlineAnimator;

    [SerializeField] private bool isWalking;
    public bool isRunning;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    public void SetPosition()
    {
        transform.position = new Vector3(Random.Range(-5, 5), 0.8f, Random.Range(-15, 7));
    }

    private CapsuleCollider capsule;
    private Vector3 normalCenter;
    private Vector3 crouchCenter;

    void Start()
    {
        baseFOV = playerCamera.fieldOfView;
        sprintFOV = baseFOV + 10f;
    }

    public override void OnStartAuthority()
    {
        readyToJump = true;
        playerCamera.gameObject.SetActive(true);

        //character_3P.GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
        foreach (var character3p in character_3Ps)
        {
            character3p.GetComponent<SkinnedMeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        //character_3P.GetComponentInChildren<SkinnedMeshRenderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        //character_3P.GetComponent<Animator>().enabled = false;

        if (SceneManager.GetActiveScene().name != "Game")
        {
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        capsule = GetComponent<CapsuleCollider>();
        normalCenter = capsule.center;
        crouchCenter = new Vector3(normalCenter.x, normalCenter.y - (normalHeight - crouchHeight) / 2f, normalCenter.z);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name.Equals("Game"))
        {
            if (!character_1P.activeSelf)
            {
                SetPosition();
                Cursor.lockState = CursorLockMode.Locked;
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                character_1P.SetActive(true);
            }

            if (isOwned)
            {
                //grounded = Physics.Raycast(groundPos.position, Vector3.down, 0.2f, whatIsGround);
                grounded = Physics.CheckSphere(groundPos.position, 0.3f, whatIsGround);

                MyInput();
                SpeedControl();
                StateHandler();

                // handle drag
                if (grounded)
                    rb.linearDamping = groundDrag;
                else
                    rb.linearDamping = 0;
            }
        }

    }

    private void FixedUpdate()
    {
        if (SceneManager.GetActiveScene().name.Equals("Game") && isOwned)
        {
            MovePlayer();
        }
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        bool wantCrouch = Input.GetKey(crouchKey) && grounded;

        if (isCrouching)
        {
            if (!wantCrouch)
            {
                if (CanStandUp())
                    isCrouching = false;
                else
                    isCrouching = true; 
            }
        }
        else
        {
            if (wantCrouch)
                isCrouching = true;
        }

        isRunning = Input.GetKey(sprintKey) && !isCrouching;

        if (Input.GetKeyDown(jumpKey) && readyToJump && grounded && !isCrouching)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        HandleCrouch();
    }


    private void StateHandler()
    {
        float targetBobAmp = 0f;
        if (grounded && isCrouching)
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
            targetBobAmp = viewbobbingController.bobAmplitudeCrouch;
        }
        else if (grounded && isRunning)
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
            if (isWalking)
                targetBobAmp = viewbobbingController.bobAmplitudeRun;
        }
        else if (grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
            targetBobAmp = viewbobbingController.bobAmplitudeBase;
        }
        else
        {
            state = MovementState.air;
            targetBobAmp = viewbobbingController.bobAmplitudeBase;
        }


        viewbobbingController.bobAmplitude = Mathf.Lerp(
            viewbobbingController.bobAmplitude,
            targetBobAmp,
            Time.deltaTime * fovChangeSpeed
        );
        
        float targetFOV = isRunning && isWalking ? sprintFOV : baseFOV;
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * fovChangeSpeed
        );

        float targetTiltBob = isRunning && isWalking ? viewbobbingController.tiltAmountRun : viewbobbingController.tiltAmountBase;
        viewbobbingController.tiltAmount = Mathf.Lerp(
            viewbobbingController.tiltAmount,
            targetTiltBob,
            Time.deltaTime * fovChangeSpeed
        );

        OnlineAnimator.SetBool("isGrounded", grounded);
        OnlineAnimator.SetBool("isCrouching", isCrouching);
        OnlineAnimator.SetBool("isCrouchingWithWalking", isCrouching && isWalking);
        OnlineAnimator.SetBool("isWalking", isWalking);
        if (isWalking)
            OnlineAnimator.SetBool("isRunning", state == MovementState.sprinting ? true : false);
        else
            OnlineAnimator.SetBool("isRunning", false);
    }

    [SerializeField] private float targetHeight;
    private float normalHeight = 1.747188f;
    private float crouchHeight = 1.163217f;

    private float normalCameraHeight = 0f;
    private float crouchCameraHeight = -0.6f;

    private float crouchCollSpeed = 6.5f;

    void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        Vector3 targetCenter = isCrouching ? crouchCenter : normalCenter;

        capsule.height = Mathf.Lerp(capsule.height, targetHeight, Time.deltaTime * crouchCollSpeed);
        capsule.center = Vector3.Lerp(capsule.center, targetCenter, Time.deltaTime * crouchCollSpeed);

        float targetCameraY = isCrouching ? crouchCameraHeight : normalCameraHeight;
        Vector3 camLocalPos = playerCamera.transform.localPosition;
        camLocalPos.y = Mathf.Lerp(camLocalPos.y, targetCameraY, Time.deltaTime * crouchCollSpeed);
        playerCamera.transform.localPosition = camLocalPos;

    }

    private bool wasOnSlopeLastFrame;

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSlope() && !exitingSlope)
        {
            if (isWalking)
            {
                Vector3 slopeDir = GetSlopeMoveDirection();
                rb.AddForce(slopeDir * moveSpeed * 20f, ForceMode.Force);
            }

            // Zemine yapıştırma ve aşağı çekme
            rb.AddForce(-slopeHit.normal * 30f, ForceMode.Force);
            rb.AddForce(Vector3.down * 30f, ForceMode.Force);
        }
        else if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        }

        if (wasOnSlopeLastFrame && !OnSlope() && rb.linearVelocity.y > 0)
        {
            if (rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            rb.AddForce(Vector3.down * 60f, ForceMode.Force);
            Debug.Log("S.Down Working!");
        }


        //! Bug issue -> IDK!? 
        rb.useGravity = !OnSlope();

        wasOnSlopeLastFrame = OnSlope();

        isWalking = moveDirection.magnitude > 0.1f;

        capsule.material = isWalking ? noFrictionMat : fullFrictionMat;
    }

    private void SpeedControl()
    {
        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        /* 
        Vector3 x = groundPos.position + new Vector3(0, 0.5f, 0);
            if (Physics.Raycast(x, orientation.forward, out var ds, 0.4f + 0.4f, whatIsGround))
            {
                Debug.Log("FRONT SLOPE DETECTION");
                Debug.DrawRay(x, orientation.forward * (0.4f + 0.4f), Color.yellow);
            }
            else
            {
                Debug.DrawRay(x, orientation.forward * (0.4f + 0.4f), Color.red);
            }
            */
        if (Physics.Raycast(groundPos.position, Vector3.down, out slopeHit, 0.4f + 0.7f, whatIsGround))
        {
            Debug.DrawRay(groundPos.position, Vector3.down * (0.4f + 0.7f), Color.green);
            float angel = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angel < maxSlopeAngle && angel != 0f;
        }
        else
        {
            Debug.DrawRay(groundPos.position, Vector3.down * (0.4f + 0.7f), Color.red);
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    [Header("Crouch Handler")]
    [SerializeField] private Transform headCheck;     //? oyuncunun kafasının hemen üstü (crouch'ta da kafaya yakın)
    [SerializeField] private LayerMask headBlockMask; //? tavan/engeller (zeminle aynı olabilir)
    [SerializeField] private float rayRadius = 0.7f;

    private bool CanStandUp()
    {
        bool hit = Physics.CheckSphere(headCheck.position, rayRadius, headBlockMask);
        Color c = hit ? Color.red : Color.green;
        Debug.DrawRay(headCheck.position, Vector3.up * rayRadius, c);
        return !hit;
    }
}
