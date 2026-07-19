using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float airControl = 0.3f;

    [Header("Sprint")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private float staminaMax = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 30f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -15f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private Vector3 crouchCameraOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.28f;
    [SerializeField] private LayerMask groundMask = -1;
    [SerializeField] private float stepOffset = 0.3f;

    [Header("Camera")]
    [SerializeField] private float mouseSensitivity = 1f; // lower default sensitivity for smoother aiming
    [SerializeField] private float lookSmoothTime = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = false;
    [SerializeField] private bool showDebugOverlay = false;

    [Header("Head Bob")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float bobAmount = 0.04f;
    [SerializeField] private float bobFrequency = 10f;

    [Header("FOV Effects")]
    [SerializeField] private bool enableFOVEffects = true;
    [SerializeField] private float baseFOV = 70f;
    [SerializeField] private float sprintFOVIncrease = 8f;
    [SerializeField] private float fovChangeSpeed = 5f;

    public enum MovementState
    {
        Standing,
        Walking,
        Running,
        Crouching,
        Jumping,
        Falling
    }

    private CharacterController controller;
    private Camera playerCamera;
    private Transform cameraTransform;
    private Vector3 defaultCameraLocalPos;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction interactAction;

    private NoiseEmitter noiseEmitter;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector3 currentVelocity;
    private Vector3 verticalVelocity;

    private bool isGrounded;
    private bool wasGrounded;
    private bool sprintHeld;
    private bool jumpRequested;
    private bool crouchRequested;
    private bool isCrouching;
    private bool staminaDepleted;
    private bool wasMoving;

    private float currentStamina;
    private float xRotation;
    private float currentControllerHeight;
    private Vector3 currentCameraCrouchOffset;
    private Vector2 smoothLook;
    private Vector2 lookVelocity;
    private float footstepTimer;
    private GUIStyle debugStyle;

    public bool IsWalking { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsJumping { get; private set; }
    public bool IsGrounded => isGrounded;
    public bool IsCrouching { get; private set; }
    public float VerticalVelocity => verticalVelocity.y;
    public float MovementSpeed => currentVelocity.magnitude;
    public float StaminaNormalized => currentStamina / staminaMax;
    public float CurrentStamina => currentStamina;
    public float StaminaMax => staminaMax;
    public MovementState CurrentMovementState { get; private set; }

    public float MovementNoiseMultiplier
    {
        get
        {
            if (isCrouching) return 0.3f;
            if (CurrentMovementState == MovementState.Running) return 1.0f;
            if (CurrentMovementState == MovementState.Walking) return 0.6f;
            return 0.5f;
        }
    }

    public float VisibilityMultiplier => isCrouching ? 0.5f : 1.0f;

    public event System.Action OnFootstep;
    public event System.Action OnLand;
    public event System.Action OnJump;
    public event System.Action<bool> OnCrouchToggle;
    public event System.Action OnInteract;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.stepOffset = stepOffset;
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingHeight * 0.5f, 0f);
        controller.slopeLimit = 45f;
        currentControllerHeight = standingHeight;

        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();

        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            defaultCameraLocalPos = cameraTransform.localPosition;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = staminaMax;
        wasGrounded = isGrounded = true;

        noiseEmitter = GetComponent<NoiseEmitter>();
        if (noiseEmitter == null)
            noiseEmitter = gameObject.AddComponent<NoiseEmitter>();

        debugStyle = new GUIStyle();
        debugStyle.normal.textColor = Color.white;
        debugStyle.fontSize = 12;

        CreateInputActions();
    }

    private void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += _ => moveInput = Vector2.zero;

        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += _ => lookInput = Vector2.zero;

        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.performed += _ => sprintHeld = true;
        sprintAction.canceled += _ => sprintHeld = false;

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.performed += _ => jumpRequested = true;

        crouchAction = new InputAction("Crouch", InputActionType.Button);
        crouchAction.AddBinding("<Keyboard>/leftCtrl");
        crouchAction.performed += _ => crouchRequested = true;

        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.performed += _ => OnInteract?.Invoke();
        interactAction.Enable();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        sprintAction?.Enable();
        jumpAction?.Enable();
        crouchAction?.Enable();
        interactAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        sprintAction?.Disable();
        jumpAction?.Disable();
        crouchAction?.Disable();
        interactAction?.Disable();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        lookAction?.Dispose();
        sprintAction?.Dispose();
        jumpAction?.Dispose();
        crouchAction?.Dispose();
        interactAction?.Dispose();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        HandleCrouchInput();
        HandleCrouchTransition();
        HandleJumpInput();
        CheckGround();
        ApplyGravity();
        ApplyHorizontalMovement();
        HandleLook();
        HandleStamina();
        HandleFootsteps();
        HandleLanding();
        UpdateAnimationHooks();
        ApplyHeadBob();
        ApplyFOV();
    }

    private void HandleCrouchInput()
    {
        if (!crouchRequested) return;
        crouchRequested = false;

        isCrouching = !isCrouching;

        if (isCrouching)
        {
            sprintHeld = false;
        }
        else
        {
            if (!CanStand())
                isCrouching = true;
        }

        currentControllerHeight = isCrouching ? crouchHeight : standingHeight;
        OnCrouchToggle?.Invoke(isCrouching);
    }

    private void HandleCrouchTransition()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        currentControllerHeight = Mathf.Lerp(controller.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.height = currentControllerHeight;
        controller.center = new Vector3(0f, currentControllerHeight * 0.5f, 0f);

        if (cameraTransform != null)
        {
            Vector3 targetOffset = isCrouching ? crouchCameraOffset : Vector3.zero;
            currentCameraCrouchOffset = Vector3.Lerp(currentCameraCrouchOffset, targetOffset, crouchTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = defaultCameraLocalPos + currentCameraCrouchOffset;
        }
    }

    private bool CanStand()
    {
        float checkHeight = standingHeight - controller.height;
        if (checkHeight <= 0.01f) return true;

        Vector3 checkCenter = transform.position + Vector3.up * (controller.height + checkHeight * 0.5f);
        Collider[] hits = Physics.OverlapSphere(checkCenter, controller.radius * 0.9f, groundMask);
        return hits.Length == 0;
    }

    private void HandleJumpInput()
    {
        if (!jumpRequested) return;
        jumpRequested = false;

        if (!isGrounded) return;

        if (isCrouching)
        {
            if (!CanStand()) return;
            isCrouching = false;
            currentControllerHeight = standingHeight;
            OnCrouchToggle?.Invoke(false);
        }

        verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        IsJumping = true;
        OnJump?.Invoke();
        NoiseManager.EmitAt(transform.position, 4f, 0.3f, NoiseType.Impact, gameObject);
    }

    private void CheckGround()
    {
        Vector3 bottom = transform.position + controller.center - Vector3.up * controller.height * 0.5f;
        isGrounded = Physics.CheckSphere(bottom, groundCheckRadius, groundMask);
    }

    private void ApplyGravity()
    {
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
            IsJumping = false;
        }

        verticalVelocity.y += gravity * Time.deltaTime;

        controller.Move(verticalVelocity * Time.deltaTime);

        if ((controller.collisionFlags & CollisionFlags.Above) != 0 && verticalVelocity.y > 0)
            verticalVelocity.y = 0f;
    }

    private void ApplyHorizontalMovement()
    {
        Vector3 inputDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (inputDir.magnitude > 1f) inputDir.Normalize();

        float targetSpeed;
        if (!isGrounded)
            targetSpeed = walkSpeed * airControl;
        else if (isCrouching)
            targetSpeed = crouchSpeed;
        else if (sprintHeld && canSprint && !staminaDepleted && moveInput.magnitude > 0.1f)
            targetSpeed = sprintSpeed;
        else if (moveInput.magnitude > 0.1f)
            targetSpeed = walkSpeed;
        else
            targetSpeed = 0f;

        Vector3 targetVelocity = inputDir * targetSpeed;
        float accel = isGrounded ? acceleration : acceleration * airControl;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, accel * Time.deltaTime);

        controller.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleLook()
    {
        if (playerCamera == null) return;

        Vector2 delta = lookInput * mouseSensitivity;
        smoothLook = Vector2.SmoothDamp(smoothLook, delta, ref lookVelocity, lookSmoothTime);

        xRotation = Mathf.Clamp(xRotation - smoothLook.y, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * smoothLook.x);
    }

    private void HandleStamina()
    {
        if (sprintHeld && isGrounded && !isCrouching && moveInput.magnitude > 0.1f)
        {
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrainRate * Time.deltaTime);
            if (currentStamina <= 0f) staminaDepleted = true;
        }
        else
        {
            currentStamina = Mathf.Min(staminaMax, currentStamina + staminaRegenRate * Time.deltaTime);
            if (currentStamina > staminaMax * 0.3f) staminaDepleted = false;
        }
    }

    private void HandleFootsteps()
    {
        bool isMoving = isGrounded && currentVelocity.magnitude > 0.1f;

        if (!isMoving)
        {
            footstepTimer = 0f;
            wasMoving = false;
            return;
        }

        if (!wasMoving && isMoving)
            footstepTimer = 0f;

        wasMoving = true;
        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            OnFootstep?.Invoke();
            if (noiseEmitter != null)
                noiseEmitter.EmitNoise(MovementNoiseMultiplier, MovementNoiseMultiplier);
            footstepTimer = isCrouching ? 0.7f : (sprintHeld ? 0.35f : 0.5f);
        }
    }

    private void HandleLanding()
    {
        if (!wasGrounded && isGrounded && verticalVelocity.y < -1f)
        {
            OnLand?.Invoke();
            float landingStrength = Mathf.Abs(verticalVelocity.y) / 20f;
            NoiseManager.EmitAt(transform.position, 6f, landingStrength, NoiseType.Impact, gameObject);

            if (CameraShaker.Instance != null && landingStrength > 0.15f)
                CameraShaker.Instance.Shake(landingStrength * 0.15f, 0.15f);
        }
        wasGrounded = isGrounded;
    }

    private void UpdateAnimationHooks()
    {
        bool isMoving = currentVelocity.magnitude > 0.1f;

        IsWalking = isGrounded && isMoving && !sprintHeld && !isCrouching;
        IsRunning = isGrounded && sprintHeld && !isCrouching && isMoving;
        IsJumping = !isGrounded && verticalVelocity.y > 1f;
        IsCrouching = isCrouching;

        if (isCrouching)
            CurrentMovementState = MovementState.Crouching;
        else if (!isGrounded && verticalVelocity.y > 1f)
            CurrentMovementState = MovementState.Jumping;
        else if (!isGrounded)
            CurrentMovementState = MovementState.Falling;
        else if (sprintHeld && isMoving)
            CurrentMovementState = MovementState.Running;
        else if (isMoving)
            CurrentMovementState = MovementState.Walking;
        else
            CurrentMovementState = MovementState.Standing;
    }

    private void ApplyHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null) return;

        bool isMoving = isGrounded && currentVelocity.magnitude > 0.5f;
        if (!isMoving)
        {
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, defaultCameraLocalPos + currentCameraCrouchOffset,
                Time.deltaTime * 5f);
            return;
        }

        float speed = currentVelocity.magnitude;
        float freq = bobFrequency * (speed / sprintSpeed);
        float bobY = Mathf.Sin(Time.time * freq) * bobAmount * (isCrouching ? 0.3f : 1f);
        float bobX = Mathf.Sin(Time.time * freq * 0.5f) * bobAmount * 0.3f * (isCrouching ? 0.3f : 1f);

        Vector3 targetPos = defaultCameraLocalPos + currentCameraCrouchOffset + new Vector3(bobX, bobY, 0f);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPos, Time.deltaTime * 8f);
    }

    private void ApplyFOV()
    {
        if (!enableFOVEffects || playerCamera == null) return;

        float targetFOV = baseFOV;
        if (sprintHeld && isGrounded && !isCrouching && currentVelocity.magnitude > 1f)
            targetFOV += sprintFOVIncrease;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || controller == null) return;

        Vector3 bottom = transform.position + controller.center - Vector3.up * controller.height * 0.5f;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(bottom, groundCheckRadius);

        if (isCrouching)
        {
            Vector3 standCenter = transform.position + Vector3.up * (controller.height + (standingHeight - controller.height) * 0.5f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(standCenter, controller.radius * 0.9f);
        }
    }

    private void OnGUI()
    {
        if (!showDebugOverlay) return;

        GUI.Label(new Rect(10, 10, 300, 20), $"State: {CurrentMovementState}", debugStyle);
        GUI.Label(new Rect(10, 30, 300, 20), $"Speed: {MovementSpeed:F2} m/s", debugStyle);
        GUI.Label(new Rect(10, 50, 300, 20), $"Grounded: {isGrounded} | Crouch: {isCrouching}", debugStyle);
        GUI.Label(new Rect(10, 70, 300, 20), $"Stamina: {currentStamina:F0}/{staminaMax:F0}", debugStyle);
    }

    public void Interact()
    {
        OnInteract?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoSpawnPlayer()
    {
        if (FindFirstObjectByType<PlayerController>() != null)
            return;

        var player = new GameObject("Player");
        player.tag = "Player";

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 1f, 0f);

        var cam = FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            cam.transform.SetParent(player.transform);
            cam.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            cam.transform.localRotation = Quaternion.identity;
        }

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInteraction>();

        var spawnPoint = GameObject.Find("SpawnPoint");
        player.transform.position = spawnPoint != null
            ? spawnPoint.transform.position
            : new Vector3(0f, 1f, 0f);

        Debug.Log("[PlayerController] Auto-spawned player", player);
    }
}
