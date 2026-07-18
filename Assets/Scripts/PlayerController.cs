using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float lookSmoothTime = 0.05f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask = -1;

    private CharacterController controller;
    private Camera playerCamera;

    private Vector2 moveInput;
    private bool isSprinting;
    private bool jumpRequested;

    private Vector2 smoothLook;
    private Vector2 lookVelocity;
    private float xRotation;

    private Vector3 verticalVelocity;
    private bool isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        ReadInput();
        HandleJump();
        ApplyGravity();
        Move();
        Look();
    }

    private void ReadInput()
    {
        var keyboard = Keyboard.current;

        Vector2 move = Vector2.zero;
        if (keyboard.wKey.isPressed) move.y += 1f;
        if (keyboard.sKey.isPressed) move.y -= 1f;
        if (keyboard.aKey.isPressed) move.x -= 1f;
        if (keyboard.dKey.isPressed) move.x += 1f;
        moveInput = move.normalized;

        isSprinting = keyboard.leftShiftKey.isPressed;

        if (keyboard.spaceKey.wasPressedThisFrame)
            jumpRequested = true;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            smoothLook = Vector2.SmoothDamp(smoothLook, delta, ref lookVelocity, lookSmoothTime);
        }
    }

    private void HandleJump()
    {
        if (!jumpRequested) return;
        jumpRequested = false;

        if (isGrounded)
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void ApplyGravity()
    {
        CheckGround();

        if (isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f;

        verticalVelocity.y += gravity * Time.deltaTime;
    }

    private void CheckGround()
    {
        if (controller == null) return;
        Vector3 bottom = transform.position + controller.center - Vector3.up * controller.height * 0.5f;
        isGrounded = Physics.CheckSphere(bottom, groundCheckRadius, groundMask);
    }

    private void Move()
    {
        if (controller == null) return;

        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        controller.Move(direction * (speed * Time.deltaTime));
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    private void Look()
    {
        if (playerCamera == null) return;

        Vector2 target = smoothLook * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation - target.y, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * target.x);
    }

    private void OnDrawGizmosSelected()
    {
        if (controller == null) return;
        Vector3 bottom = transform.position + controller.center - Vector3.up * controller.height * 0.5f;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(bottom, groundCheckRadius);
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
        player.transform.position = new Vector3(0f, 1f, 0f);

        Debug.Log("[PlayerController] Auto-spawned player", player);
    }
}
