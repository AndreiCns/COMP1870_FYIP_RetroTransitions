using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public Transform weaponHolder;
    public float cameraShakeAmount = 0.2f;
    public float cameraShakeDuration = 0.05f;

    [Header("Look Settings")]
    public float mouseSensitivity = 50f;
    public float minPitch = -70f;
    public float maxPitch = 70f;

    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public int maxJumps = 2;

    [Header("FOV Kick")]
    public Camera cam;
    public float fovKickAmount = 8f;
    public float fovKickTime = 0.15f;
    private float defaultFOV;

    [Header("Weapon Bob")]
    public float bobSpeed = 10f;
    public float bobAmount = 0.03f;
    private float bobTimer = 0f;
    private Vector3 weaponHolderInitialLocalPos;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch = 0f;
    private Vector3 velocity;
    private int jumpCount = 0;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        defaultFOV = cam.fieldOfView;
        weaponHolderInitialLocalPos = weaponHolder.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ---------------------------------
    // INPUT EVENTS
    // ---------------------------------
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
            return;

        if (controller.isGrounded)
        {
            Jump();
        }
        else if (jumpCount < maxJumps)
        {
            Jump();
        }
    }

    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            StartCoroutine(DoCameraShake());
        }
    }

    // ---------------------------------
    // UPDATE LOOP
    // ---------------------------------
    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleWeaponBob();
        SyncWeaponToCamera();   // <- NEW: weapon follows camera perfectly
    }

    // ---------------------------------
    // LOOK
    // ---------------------------------
    void HandleLook()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // rotate camera
        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // rotate player horizontally
        transform.Rotate(Vector3.up * mouseX);
    }

    // ---------------------------------
    // MOVEMENT
    // ---------------------------------
    void HandleMovement()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpCount = 0;
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpCount++;

        StartCoroutine(FOVKick());
    }

    // ---------------------------------
    // FOV KICK
    // ---------------------------------
    IEnumerator FOVKick()
    {
        float t = 0;
        float target = defaultFOV + fovKickAmount;

        while (t < fovKickTime)
        {
            cam.fieldOfView = Mathf.Lerp(defaultFOV, target, t / fovKickTime);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0;
        while (t < fovKickTime)
        {
            cam.fieldOfView = Mathf.Lerp(target, defaultFOV, t / fovKickTime);
            t += Time.deltaTime;
            yield return null;
        }

        cam.fieldOfView = defaultFOV;
    }

    // ---------------------------------
    // CAMERA SHAKE FOR RECOIL
    // ---------------------------------
    IEnumerator DoCameraShake()
    {
        Vector3 originalPos = playerCamera.localPosition;
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            Vector3 randomOffset = Random.insideUnitSphere * cameraShakeAmount;
            playerCamera.localPosition = originalPos + randomOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.localPosition = originalPos;
    }

    // ---------------------------------
    // WEAPON BOB
    // ---------------------------------
    void HandleWeaponBob()
    {
        if (moveInput.magnitude > 0.1f && controller.isGrounded)
        {
            bobTimer += Time.deltaTime * bobSpeed;

            float x = Mathf.Sin(bobTimer) * bobAmount;
            float y = Mathf.Cos(bobTimer * 2f) * bobAmount * 1.3f;

            weaponHolder.localPosition =
                weaponHolderInitialLocalPos + new Vector3(x, y, 0f);
        }
        else
        {
            weaponHolder.localPosition = Vector3.Lerp(
                weaponHolder.localPosition,
                weaponHolderInitialLocalPos,
                Time.deltaTime * 8f
            );
        }
    }

    // ---------------------------------
    // NEW: WEAPON FOLLOWS CAMERA ROTATION
    // ---------------------------------
    void SyncWeaponToCamera()
    {
        weaponHolder.rotation = playerCamera.rotation;
    }
}
