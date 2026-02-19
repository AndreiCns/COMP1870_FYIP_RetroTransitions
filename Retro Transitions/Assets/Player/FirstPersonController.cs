using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform weaponRecoil;
    [SerializeField] private PlayerCombatController combat;

    [Header("Movement SFX")]
    [SerializeField] private AudioSource movementSfxSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float stepInterval = 0.5f;

    [Header("Jump & Landing Audio")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landingClip;
    [SerializeField] private float jumpVolume = 1f;
    [SerializeField] private float landingVolume = 1f;

    [Header("Retro Audio")]
    [SerializeField] private AudioLowPassFilter movementLowPass;
    [SerializeField] private StyleSwapEvent styleSwapEvent;
    [SerializeField] private float retroCutoff = 1200f;
    [SerializeField] private float retroResonanceQ = 1.1f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 50f;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private int maxJumps = 2;

    [Header("FOV Kick")]
    [SerializeField] private float fovKickAmount = 8f;
    [SerializeField] private float fovKickTime = 0.15f;

    [Header("Weapon Bob")]
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private float bobAmount = 0.03f;

    [Header("Weapon Recoil")]
    [SerializeField] private float recoilKickback = 0.15f;
    [SerializeField] private float recoilUp = 6f;
    [SerializeField] private float recoilRecovery = 12f;

    private Vector3 recoilCurrentPos;
    private Vector3 recoilTargetPos;
    private Vector3 recoilCurrentRot;
    private Vector3 recoilTargetRot;

    private float bobTimer;
    private float defaultFOV;
    private Coroutine fovKickRoutine;

    private Vector3 weaponHolderInitialLocalPos;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;
    private Vector3 velocity;
    private int jumpCount;
    private float stepTimer;
    private bool wasGrounded;

    // Held-fire prevents InputSystem "performed" spam and keeps ROF deterministic.
    private bool fireHeld;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Movement SFX is critical for feedback; fail early if it's missing.
        if (movementSfxSource == null)
        {
            Debug.LogError("[FPC] movementSfxSource not assigned.", this);
            enabled = false;
            return;
        }

        // Keep the low-pass on the movement source so retro mode is a simple toggle.
        if (movementLowPass == null)
            movementLowPass = movementSfxSource.GetComponent<AudioLowPassFilter>();

        if (movementLowPass == null)
            movementLowPass = movementSfxSource.gameObject.AddComponent<AudioLowPassFilter>();

        // Combat is optional at edit-time, but the controller expects it at runtime.
        if (combat == null)
            combat = GetComponent<PlayerCombatController>();
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;
        else
            Debug.LogWarning("[FPC] styleSwapEvent not assigned.", this);

        if (verboseLogs)
            Debug.Log($"[FPC] movementSfxSource={movementSfxSource.name}, lowPass={(movementLowPass ? "OK" : "NULL")}", this);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void Start()
    {
        // These are hard requirements; better to disable than chase nulls mid-playtest.
        if (cam == null || playerCamera == null || weaponHolder == null || weaponRecoil == null)
        {
            Debug.LogError($"[FPC] Missing required references on '{gameObject.name}'.", this);
            enabled = false;
            return;
        }

        defaultFOV = cam.fieldOfView;
        weaponHolderInitialLocalPos = weaponHolder.localPosition;

        // FPS feel: lock cursor by default.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Start in modern unless style system tells us otherwise.
        ApplyMovementFilter(isRetro: false);
    }

    // InputSystem callbacks
    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (controller.isGrounded || jumpCount < maxJumps)
            Jump();
    }

    public void OnFire(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            fireHeld = true;
        else if (ctx.canceled)
            fireHeld = false;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        HandleLook(dt);
        HandleMovement(dt);

        // Fire attempts are polled so cooldown + ammo are always the single source of truth.
        if (fireHeld && combat != null)
        {
            if (combat.TryFire())
                ApplyRecoilKick();
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        // LateUpdate keeps weapon visuals smooth after movement/camera updates.
        HandleWeaponBob(dt);
        SyncWeaponToCamera();
        HandleWeaponRecoil(dt);
    }

    private void ApplyRecoilKick()
    {
        // Only kick on a confirmed shot (not on input press).
        recoilTargetPos -= new Vector3(0, 0, recoilKickback);
        recoilTargetRot += new Vector3(-recoilUp, 0, 0);
    }

    private void HandleLook(float dt)
    {
        float mouseX = lookInput.x * mouseSensitivity * dt;
        float mouseY = lookInput.y * mouseSensitivity * dt;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMovement(float dt)
    {
        bool isGroundedNow = controller.isGrounded;

        // Landing sound only on actual air->ground transitions with noticeable fall speed.
        if (!wasGrounded && isGroundedNow && velocity.y < -2f)
            PlayLandingSFX();

        wasGrounded = isGroundedNow;

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // Keeps the controller planted on slopes/steps.
            jumpCount = 0;
        }

        Vector2 clamped = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 move = (transform.right * clamped.x + transform.forward * clamped.y);
        float moveAmount = move.magnitude;

        velocity.y += gravity * dt;

        Vector3 totalMove = (move * moveSpeed) + new Vector3(0f, velocity.y, 0f);
        controller.Move(totalMove * dt);

        HandleFootsteps(dt, moveAmount);
    }

    private void Jump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpCount++;

        PlayJumpSFX();

        if (fovKickRoutine != null)
            StopCoroutine(fovKickRoutine);

        fovKickRoutine = StartCoroutine(FOVKick());
    }

    private void PlayJumpSFX()
    {
        if (jumpClip == null) return;

        movementSfxSource.pitch = Random.Range(0.95f, 1.05f);
        movementSfxSource.PlayOneShot(jumpClip, jumpVolume);
    }

    private void PlayLandingSFX()
    {
        if (landingClip == null) return;

        movementSfxSource.pitch = Random.Range(0.9f, 1.0f);
        movementSfxSource.PlayOneShot(landingClip, landingVolume);
    }

    private void HandleFootsteps(float dt, float moveAmount)
    {
        if (!controller.isGrounded || moveAmount < 0.1f)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= dt;

        if (stepTimer <= 0f)
        {
            PlayFootstepSFX();
            stepTimer = stepInterval;
        }
    }

    private void PlayFootstepSFX()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = Random.Range(0, footstepClips.Length);
        movementSfxSource.pitch = Random.Range(0.95f, 1.05f);
        movementSfxSource.PlayOneShot(footstepClips[index], 1f);
    }

    private IEnumerator FOVKick()
    {
        float t = 0f;
        float target = defaultFOV + fovKickAmount;

        while (t < fovKickTime)
        {
            cam.fieldOfView = Mathf.Lerp(defaultFOV, target, t / fovKickTime);
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < fovKickTime)
        {
            cam.fieldOfView = Mathf.Lerp(target, defaultFOV, t / fovKickTime);
            t += Time.deltaTime;
            yield return null;
        }

        cam.fieldOfView = defaultFOV;
        fovKickRoutine = null;
    }

    private void HandleWeaponBob(float dt)
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
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, weaponHolderInitialLocalPos, dt * 8f);
        }
    }

    private void HandleWeaponRecoil(float dt)
    {
        recoilCurrentPos = Vector3.Lerp(recoilCurrentPos, recoilTargetPos, dt * recoilRecovery);
        weaponRecoil.localPosition = recoilCurrentPos;

        recoilCurrentRot = Vector3.Lerp(recoilCurrentRot, recoilTargetRot, dt * recoilRecovery);
        weaponRecoil.localRotation = Quaternion.Euler(recoilCurrentRot);

        recoilTargetPos = Vector3.Lerp(recoilTargetPos, Vector3.zero, dt * recoilRecovery);
        recoilTargetRot = Vector3.Lerp(recoilTargetRot, Vector3.zero, dt * recoilRecovery);
    }

    private void SyncWeaponToCamera()
    {
        // Keeps the weapon orientation locked to the camera even during character rotation.
        weaponHolder.rotation = playerCamera.rotation;
    }

    private void OnStyleChanged(StyleState newState)
    {
        if (verboseLogs)
            Debug.Log($"[FPC] Style -> {newState}", this);

        ApplyMovementFilter(newState == StyleState.Retro);
    }

    private void ApplyMovementFilter(bool isRetro)
    {
        if (movementLowPass == null) return;

        movementLowPass.enabled = isRetro;

        if (isRetro)
        {
            movementLowPass.cutoffFrequency = retroCutoff;
            movementLowPass.lowpassResonanceQ = retroResonanceQ;
        }
    }
}
