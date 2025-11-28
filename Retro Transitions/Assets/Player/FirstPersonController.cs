using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public Transform weaponHolder;
    public StyleSwapManager styleSwapManager; // Assign in Inspector
    public WeaponStyleSwap weaponStyleSwap;   // Assign in Inspector

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

    [Header("Weapon Recoil")]
    public Transform weaponRecoil;
    public float recoilKickback = 0.15f;
    public float recoilUp = 6f;
    public float recoilRecovery = 12f;

    private Vector3 recoilCurrentPos;
    private Vector3 recoilTargetPos;
    private Vector3 recoilCurrentRot;
    private Vector3 recoilTargetRot;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch = 0f;
    private Vector3 velocity;
    private int jumpCount = 0;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cam == null || playerCamera == null || weaponHolder == null || weaponRecoil == null)
        {
            Debug.LogError($"FirstPersonController on '{gameObject.name}' is missing required references.");
            enabled = false;
            return;
        }

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

        if (controller.isGrounded || jumpCount < maxJumps)
            Jump();
    }

    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        weaponStyleSwap?.Fire();

        // Procedural recoil
        recoilTargetPos -= new Vector3(0, 0, recoilKickback);
        recoilTargetRot += new Vector3(-recoilUp, 0, 0);
    }

    // ---------------------------------
    // UPDATE LOOP
    // ---------------------------------
    void Update()
    {
        float dt = Time.deltaTime;

        HandleLook(dt);
        HandleMovement(dt);
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        HandleWeaponBob(dt);
        SyncWeaponToCamera();
        HandleWeaponRecoil(dt);
    }

    // ---------------------------------
    // LOOK
    // ---------------------------------
    void HandleLook(float dt)
    {
        float mouseX = lookInput.x * mouseSensitivity * dt;
        float mouseY = lookInput.y * mouseSensitivity * dt;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    // ---------------------------------
    // MOVEMENT
    // ---------------------------------
    void HandleMovement(float dt)
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            jumpCount = 0;
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        velocity.y += gravity * dt;

        Vector3 totalMove = (move * moveSpeed) + new Vector3(0f, velocity.y, 0f);
        controller.Move(totalMove * dt);
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
    // WEAPON BOB
    // ---------------------------------
    void HandleWeaponBob(float dt)
    {
        if (moveInput.sqrMagnitude > 0.01f && controller.isGrounded)
        {
            bobTimer += dt * bobSpeed;

            float x = Mathf.Sin(bobTimer) * bobAmount;
            float y = Mathf.Cos(bobTimer * 2f) * bobAmount * 1.3f;

            weaponHolder.localPosition = weaponHolderInitialLocalPos + new Vector3(x, y, 0f);
        }
        else
        {
            weaponHolder.localPosition = Vector3.Lerp(
                weaponHolder.localPosition,
                weaponHolderInitialLocalPos,
                dt * 8f
            );
        }
    }

    void HandleWeaponRecoil(float dt)
    {
        recoilCurrentPos = Vector3.Lerp(recoilCurrentPos, recoilTargetPos, dt * recoilRecovery);
        weaponRecoil.localPosition = recoilCurrentPos;

        recoilCurrentRot = Vector3.Lerp(recoilCurrentRot, recoilTargetRot, dt * recoilRecovery);
        weaponRecoil.localRotation = Quaternion.Euler(recoilCurrentRot);

        recoilTargetPos = Vector3.Lerp(recoilTargetPos, Vector3.zero, dt * recoilRecovery);
        recoilTargetRot = Vector3.Lerp(recoilTargetRot, Vector3.zero, dt * recoilRecovery);
    }

    void SyncWeaponToCamera()
    {
        weaponHolder.rotation = playerCamera.rotation;
    }
}
