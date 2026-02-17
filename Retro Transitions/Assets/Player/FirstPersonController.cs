using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponStyleSwap weaponStyleSwap;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform weaponRecoil;
    [SerializeField] private Animator weaponAnimator;
    [SerializeField] private PlayerShootModule modernShoot;
    [SerializeField] private PlayerShootModule retroShoot;

    private PlayerShootModule ActiveShoot =>
        (modernShoot != null && modernShoot.gameObject.activeInHierarchy) ? modernShoot :
        (retroShoot != null && retroShoot.gameObject.activeInHierarchy) ? retroShoot :
        null;

    private readonly string shootTrigger = "Fire";

    [Header("Movement SFX (ONE source for footsteps + jump + landing)")]
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
    private Coroutine fovKickRoutine;
    private float defaultFOV;

    [Header("Weapon Bob")]
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private float bobAmount = 0.03f;
    private float bobTimer;
    private Vector3 weaponHolderInitialLocalPos;

    [Header("Weapon Recoil")]
    [SerializeField] private float recoilKickback = 0.15f;
    [SerializeField] private float recoilUp = 6f;
    [SerializeField] private float recoilRecovery = 12f;

    private Vector3 recoilCurrentPos;
    private Vector3 recoilTargetPos;
    private Vector3 recoilCurrentRot;
    private Vector3 recoilTargetRot;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;
    private Vector3 velocity;
    private int jumpCount;

    private float stepTimer;
    private bool wasGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (movementSfxSource == null)
        {
            Debug.LogError("[FPC] movementSfxSource is not assigned. Assign one AudioSource for movement SFX.");
            enabled = false;
            return;
        }

        if (movementLowPass == null)
            movementLowPass = movementSfxSource.GetComponent<AudioLowPassFilter>();

        if (movementLowPass == null)
            movementLowPass = movementSfxSource.gameObject.AddComponent<AudioLowPassFilter>();
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;
        else
            Debug.LogWarning("[FPC] styleSwapEvent is NULL. Assign the SAME GlobalStyleSwapEvent asset used by StyleSwapManager.");

        if (verboseLogs)
            Debug.Log($"[FPC] OnEnable. movementSfxSource={movementSfxSource.name}, lowPass={(movementLowPass ? "OK" : "NULL")}");
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void Start()
    {
        if (cam == null || playerCamera == null || weaponHolder == null || weaponRecoil == null)
        {
            Debug.LogError($"[FPC] Missing required references on '{gameObject.name}'.");
            enabled = false;
            return;
        }

        defaultFOV = cam.fieldOfView;
        weaponHolderInitialLocalPos = weaponHolder.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ApplyMovementFilter(isRetro: false);
    }

    // INPUT
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
        if (!ctx.performed) return;

        var shoot = ActiveShoot;
        if (shoot != null && !shoot.TryBeginFire())
            return;

        weaponAnimator.SetTrigger(shootTrigger);
        weaponStyleSwap?.Fire();

        recoilTargetPos -= new Vector3(0, 0, recoilKickback);
        recoilTargetRot += new Vector3(-recoilUp, 0, 0);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        HandleLook(dt);
        HandleMovement(dt);
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        HandleWeaponBob(dt);
        SyncWeaponToCamera();
        HandleWeaponRecoil(dt);
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

        // Landing detection (air -> ground with downward speed)
        if (!wasGrounded && isGroundedNow && velocity.y < -2f)
            PlayLandingSFX();

        wasGrounded = isGroundedNow;

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
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
        weaponHolder.rotation = playerCamera.rotation;
    }

    private void OnStyleChanged(StyleState newState)
    {
        if (verboseLogs)
            Debug.Log($"[FPC] OnStyleChanged -> {newState}");

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
