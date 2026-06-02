using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class VelocityPlayer : MonoBehaviour
{
    // --- ARCHITECTURE ---
    public enum PlayerState { Normal, Ghosting, Gliding, Rewinding }

    [Header("Architecture")]
    public PlayerState currentState = PlayerState.Normal;

    // --- REWIND DATA STRUCTURE ---
    public struct PointInTime
    {
        public Vector3 position;
        public Quaternion rotation;
        public float cameraPitch;
    }

    [Header("Animation")]
    public Animator playerAnimator;

    [Header("Time Rewind Settings")]
    public float maxRewindSeconds = 4f;
    private List<PointInTime> rewindHistory = new List<PointInTime>();

    [Header("Movement & Game Feel")]
    public float walkSpeed = 8f;
    public float jumpHeight = 2f;
    public float acceleration = 15f;
    [Range(0f, 0.5f)] public float coyoteTime = 0.15f;
    [Range(0f, 0.5f)] public float jumpBufferTime = 0.15f;
    public float maxCameraTilt = 2.5f;
    public float tiltSpeed = 5f;

    [Header("Echo Dash Settings")]
    public float ghostSpeedMultiplier = 3f;
    public float maxGhostTime = 3f;
    public float snapMomentumMultiplier = 2.5f;
    public float bulletGlideDuration = 0.4f;
    public GameObject shellPrefab;

    [Header("Visuals & Audio")]
    public LineRenderer tetherLine;
    public AudioSource dashStartAudio;
    public AudioSource snapImpactAudio;
    public AudioSource rewindAudio;

    [Header("Physics Audio")]
    public AudioSource moonJumpAudio;
    public AudioSource crushLandAudio;
    private bool wasGrounded;

    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;
    public Transform playerCamera;

    private CharacterController controller;
    private Vector3 verticalVelocity;
    private Vector3 currentHorizontalVelocity;
    private Vector3 impactMomentum;
    private float cameraPitch = 0f;
    private float currentTilt = 0f;

    private float coyoteTimeCounter;
    private float airTimer = 0f;
    private float jumpBufferCounter;
    private float currentGhostTimer = 0f;
    private float currentGlideTimer = 0f;
    private GameObject activeShell;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (tetherLine != null) tetherLine.enabled = false;
    }

    void Update()
    {
        // 1. Check for Rewind Trigger FIRST
        if (Input.GetKeyDown(KeyCode.F) && currentState != PlayerState.Rewinding)
        {
            StartRewind();
        }

        if (Input.GetKeyUp(KeyCode.F) && currentState == PlayerState.Rewinding)
        {
            StopRewind();
        }

        // 2. Handle State Machine
        if (currentState != PlayerState.Rewinding)
        {
            HandleMouseLook();
            UpdateTimers();
        }

        switch (currentState)
        {
            case PlayerState.Normal:
                HandleNormalInput();
                HandleMovement(walkSpeed, true);
                break;

            case PlayerState.Ghosting:
                HandleGhostLogic();
                HandleMovement(walkSpeed * ghostSpeedMultiplier, true);
                break;

            case PlayerState.Gliding:
                HandleGlideLogic();
                HandleMovement(walkSpeed, false); // NO GRAVITY during glide
                break;

            case PlayerState.Rewinding:
                Camera cam = playerCamera.GetComponent<Camera>();
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 80f, 5f * Time.unscaledDeltaTime);
                break;
        }
    }

    void FixedUpdate()
    {
        if (currentState == PlayerState.Rewinding)
        {
            if (rewindHistory.Count > 0)
            {
                PointInTime point = rewindHistory[0];

                controller.enabled = false;
                transform.position = point.position;
                transform.rotation = point.rotation;
                cameraPitch = point.cameraPitch;
                playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, currentTilt);
                controller.enabled = true;

                rewindHistory.RemoveAt(0);
            }
            else
            {
                StopRewind();
            }
        }
        else
        {
            if (rewindHistory.Count > Mathf.Round(maxRewindSeconds / Time.fixedUnscaledDeltaTime))
            {
                rewindHistory.RemoveAt(rewindHistory.Count - 1);
            }

            PointInTime newPoint;
            newPoint.position = transform.position;
            newPoint.rotation = transform.rotation;
            newPoint.cameraPitch = cameraPitch;

            rewindHistory.Insert(0, newPoint);
        }
    }

    private void StartRewind()
    {
        currentState = PlayerState.Rewinding;

        // Audio Polish
        if (dashStartAudio != null) dashStartAudio.Stop();
        if (rewindAudio != null) rewindAudio.Play();

        // Safe Delete
        if (activeShell != null)
        {
            activeShell.SetActive(false);
            Destroy(activeShell);
        }

        if (tetherLine != null) tetherLine.enabled = false;
    }

    private void StopRewind()
    {
        currentState = PlayerState.Normal;
        verticalVelocity = Vector3.zero;
        impactMomentum = Vector3.zero;

        if (rewindAudio != null) rewindAudio.Stop();

        Camera cam = playerCamera.GetComponent<Camera>();
        cam.fieldOfView = 60f;
    }

    private void HandleNormalInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift)) EnterGhostState();

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f) ExecuteJump(1f);
    }

    private void EnterGhostState()
    {
        currentState = PlayerState.Ghosting;
        currentGhostTimer = 0f;

        if (shellPrefab != null)
        {
            activeShell = Instantiate(shellPrefab, transform.position, transform.rotation);

            Animator shellAnim = activeShell.GetComponentInChildren<Animator>();
            if (playerAnimator != null && shellAnim != null)
            {
                // 1. Force the clone to use the exact same logic brain as the player
                shellAnim.runtimeAnimatorController = playerAnimator.runtimeAnimatorController;

                // 2. Grab the exact frame of animation
                AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
                shellAnim.Play(stateInfo.fullPathHash, 0, stateInfo.normalizedTime);

                // 3. THE MAGIC FIX: Force Unity to render the pose immediately this exact millisecond
                shellAnim.Update(0f);

                // 4. Now that it is rendered, freeze it
                shellAnim.speed = 0f;
            }

            if (dashStartAudio != null) dashStartAudio.Play();
        }
        if (tetherLine != null) tetherLine.enabled = true;
    }

    private void HandleGhostLogic()
    {
        currentGhostTimer += Time.unscaledDeltaTime;

        if (tetherLine != null && activeShell != null)
        {
            Vector3 offset = new Vector3(0, 1f, 0);
            Vector3 startPos = transform.position + offset;
            Vector3 snapDirection = playerCamera.forward;

            Vector3 displacement = transform.position - activeShell.transform.position;
            if (displacement.magnitude > 0.5f) snapDirection = displacement.normalized;

            float launchSpeed = walkSpeed * ghostSpeedMultiplier * snapMomentumMultiplier;
            Vector3 simulatedVelocity = snapDirection * launchSpeed;

            int lineResolution = 30;
            float timeStep = 0.05f;

            tetherLine.positionCount = lineResolution;
            Vector3 currentSimPos = startPos;

            for (int i = 0; i < lineResolution; i++)
            {
                tetherLine.SetPosition(i, currentSimPos);
                Vector3 nextSimPos = currentSimPos + (simulatedVelocity * timeStep);
                float stepDistance = Vector3.Distance(currentSimPos, nextSimPos);

                if (Physics.Raycast(currentSimPos, (nextSimPos - currentSimPos).normalized, out RaycastHit hit, stepDistance))
                {
                    tetherLine.positionCount = i + 1;
                    tetherLine.SetPosition(i, hit.point);
                    break;
                }

                currentSimPos = nextSimPos;
                float simulatedTime = i * timeStep;
                if (simulatedTime > bulletGlideDuration)
                {
                    simulatedVelocity.y += Physics.gravity.y * timeStep;
                }
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) || currentGhostTimer >= maxGhostTime)
        {
            SnapToShell();
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f) ExecuteJump(1.5f);
    }

    private void SnapToShell()
    {
        if (tetherLine != null) tetherLine.enabled = false;

        if (dashStartAudio != null) dashStartAudio.Stop();

        Vector3 snapDirection = playerCamera.forward;
        if (activeShell != null)
        {
            Vector3 displacement = transform.position - activeShell.transform.position;
            if (displacement.magnitude > 0.5f) snapDirection = displacement.normalized;

            activeShell.SetActive(false);
            Destroy(activeShell);
        }

        verticalVelocity = Vector3.zero;
        currentHorizontalVelocity = Vector3.zero;

        if (controller.isGrounded && snapDirection.y < 0)
        {
            snapDirection.y = 0;
            snapDirection.Normalize();
        }

        impactMomentum = snapDirection * (walkSpeed * ghostSpeedMultiplier * snapMomentumMultiplier);

        currentGlideTimer = bulletGlideDuration;
        currentState = PlayerState.Gliding;

        if (snapImpactAudio != null) snapImpactAudio.Play();

        CameraShake shaker = playerCamera.GetComponent<CameraShake>();
        if (shaker != null) StartCoroutine(shaker.Shake(0.15f, 0.2f));

        playerCamera.GetComponent<Camera>().fieldOfView = 100f;
    }

    private void HandleGlideLogic()
    {
        currentGlideTimer -= Time.unscaledDeltaTime;
        if (currentGlideTimer <= 0)
        {
            currentState = PlayerState.Normal;
        }
    }

    private void UpdateTimers()
    {
        if (controller.isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.unscaledDeltaTime;

        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.unscaledDeltaTime;
    }

    private void HandleMovement(float currentTargetSpeed, bool applyGravity)
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 targetDirection = (transform.right * inputX + transform.forward * inputZ).normalized;
        Vector3 targetVelocity = targetDirection * currentTargetSpeed;

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", targetDirection.magnitude);
            playerAnimator.SetBool("IsGrounded", controller.isGrounded);
        }

        currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, acceleration * Time.unscaledDeltaTime);
        controller.Move(currentHorizontalVelocity * Time.unscaledDeltaTime);

        if (impactMomentum.magnitude > 0.1f)
        {
            controller.Move(impactMomentum * Time.unscaledDeltaTime);
            impactMomentum = Vector3.Lerp(impactMomentum, Vector3.zero, 3f * Time.unscaledDeltaTime);
        }

        bool isGrounded = controller.isGrounded;

        // --- NEW: AIR TIMER TRACKING ---
        if (!isGrounded)
        {
            airTimer += Time.unscaledDeltaTime;
        }

        // --- THE FIXED LANDING DETECTION ---
        if (isGrounded && !wasGrounded)
        {
            // Only trigger if we were falling for more than 0.15 seconds
            if (airTimer > 0.15f)
            {
                if (Physics.gravity.y < -40f) // Crush Mode
                {
                    if (crushLandAudio != null) crushLandAudio.Play();
                    if (playerAnimator != null) playerAnimator.SetTrigger("HeroLanding");

                    CameraShake shaker = playerCamera.GetComponent<CameraShake>();
                    if (shaker != null) StartCoroutine(shaker.Shake(0.15f, 0.25f));
                }
            }


            airTimer = 0f; // Reset the timer the moment we touch the ground
        }
        wasGrounded = isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        if (applyGravity) verticalVelocity.y += Physics.gravity.y * Time.unscaledDeltaTime;
        else verticalVelocity.y = 0f;

        controller.Move(verticalVelocity * Time.unscaledDeltaTime);

        Camera cam = playerCamera.GetComponent<Camera>();
        if (currentState != PlayerState.Rewinding)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 60f, 5f * Time.unscaledDeltaTime);
        }
    }

    private void ExecuteJump(float stateMultiplier)
    {
        coyoteTimeCounter = 0f;
        jumpBufferCounter = 0f;

        // NEW: Play Moon Jump sound
        if (Physics.gravity.y > -10f && moonJumpAudio != null)
        {
            if (moonJumpAudio != null) moonJumpAudio.Play();
            if (playerAnimator != null) playerAnimator.SetTrigger("MoonJump");
        }
        else
        {
            if (playerAnimator != null) playerAnimator.SetTrigger("NormalJump");
        }

        float worldJumpMod = 1f;
        if (WorldFluxManager.Instance != null) worldJumpMod = WorldFluxManager.Instance.currentJumpMultiplier;

        float finalJumpHeight = jumpHeight * stateMultiplier * worldJumpMod;
        verticalVelocity.y = Mathf.Sqrt(finalJumpHeight * -2f * Physics.gravity.y);
    }

    private void HandleMouseLook()
    {
        Vector2 lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        cameraPitch -= lookInput.y * mouseSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);

        float targetTilt = -Input.GetAxis("Horizontal") * maxCameraTilt;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.unscaledDeltaTime);

        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, currentTilt);
        transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);
    }

    public Vector3 velocity { get { return verticalVelocity; } }

    public void ApplySmashResistance(float multiplier)
    {
        if (verticalVelocity.y < 0) verticalVelocity.y *= multiplier;
    }
}