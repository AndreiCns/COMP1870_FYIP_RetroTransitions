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

    [Header("Audio")]
    [SerializeField] private PlayerAudioController playerAudio;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 50f;
    [SerializeField] private float minPitch = -70f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private int maxJumps = 2;

    [Header("Footsteps")]
    [SerializeField] private float stepInterval = 0.5f;
    [SerializeField] private float minFootstepSpeed = 1.0f; // m/s

    [Header("Grounding / Slopes")]
    [Tooltip("SphereCast distance to find the surface under the player.")]
    [SerializeField] private float groundCheckDistance = 0.35f;
    [Tooltip("Downward velocity used while grounded to keep contact on slopes/steps.")]
    [SerializeField] private float groundStickForce = 12f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Landing Detection")]
    [SerializeField] private float minAirTimeForLanding = 0.12f;
    [SerializeField] private float minFallSpeedForLanding = -3.0f;
    [SerializeField] private float landingCooldown = 0.12f;

    [Header("FOV Kick")]
    [SerializeField] private float fovKickAmount = 8f;
    [SerializeField] private float fovKickTime = 0.15f;

    [Header("Weapon Bob")]
    [SerializeField] private float bobSpeed = 10f;
    [SerializeField] private float bobAmount = 0.03f;

    [Header("Weapon Recoil")]
    [SerializeField] private float recoilRecovery = 12f;

    [Header("Interact")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactMask = ~0;

    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;

    private Vector3 velocity;
    private int jumpCount;

    private bool wasGrounded;
    private float airTime;
    private float lastLandingTime;

    private float stepTimer;

    private RaycastHit groundHit;
    private bool hasGroundHit;

    private float bobTimer;
    private float defaultFOV;
    private Coroutine fovKickRoutine;
    private Vector3 weaponHolderInitialLocalPos;

    private Vector3 recoilCurrentPos;
    private Vector3 recoilTargetPos;
    private Vector3 recoilCurrentRot;
    private Vector3 recoilTargetRot;

    private bool fireHeld;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (playerAudio == null)
            playerAudio = GetComponent<PlayerAudioController>();

        if (combat == null)
            combat = GetComponent<PlayerCombatController>();
    }

    private void Start()
    {
        if (cam == null || playerCamera == null || weaponHolder == null || weaponRecoil == null)
        {
            Debug.LogError($"[FPC] Missing required references on '{gameObject.name}'.", this);
            enabled = false;
            return;
        }

        defaultFOV = cam.fieldOfView;
        weaponHolderInitialLocalPos = weaponHolder.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        TryInteract();
    }

    public void OnAmmo1(InputAction.CallbackContext ctx) { if (ctx.performed) combat?.TrySetAmmoType(AmmoType.Bullet); }
    public void OnAmmo2(InputAction.CallbackContext ctx) { if (ctx.performed) combat?.TrySetAmmoType(AmmoType.Shell); }
    public void OnAmmo3(InputAction.CallbackContext ctx) { if (ctx.performed) combat?.TrySetAmmoType(AmmoType.Rocket); }
    public void OnAmmo4(InputAction.CallbackContext ctx) { if (ctx.performed) combat?.TrySetAmmoType(AmmoType.Plasma); }

    private void Update()
    {
        float dt = Time.deltaTime;

        HandleLook(dt);
        HandleMovement(dt);

        if (fireHeld && combat != null)
        {
            AmmoTypeConfig cfg = combat.CurrentConfig;

            if (combat.TryFire())
                ApplyRecoilKick(cfg);
        }
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
        bool groundedNow = controller.isGrounded;

        // Collect the surface normal so we can move cleanly on slopes.
        UpdateGroundHit();

        // Track "real airtime" so slope grounding flicker doesn't spam land events.
        if (!groundedNow) airTime += dt;

        if (!wasGrounded && groundedNow)
        {
            bool validAir = airTime >= minAirTimeForLanding;
            bool validFall = velocity.y <= minFallSpeedForLanding;
            bool cooledDown = Time.time >= lastLandingTime + landingCooldown;

            if (validAir && validFall && cooledDown)
            {
                PlayLandingSFX();
                lastLandingTime = Time.time;
            }

            airTime = 0f;
        }

        if (groundedNow && wasGrounded)
            airTime = 0f;

        wasGrounded = groundedNow;

        Vector2 clamped = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 wishMove = (transform.right * clamped.x + transform.forward * clamped.y);
        wishMove = Vector3.ClampMagnitude(wishMove, 1f);

        // Project horizontal movement onto the slope plane to avoid ramp jitter.
        Vector3 moveOnSurface = wishMove;
        if (groundedNow && hasGroundHit)
        {
            float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);

            if (slopeAngle <= controller.slopeLimit)
                moveOnSurface = Vector3.ProjectOnPlane(wishMove, groundHit.normal).normalized * wishMove.magnitude;
        }

        // Gravity + ground stick keeps CC glued to uneven triangles.
        velocity.y += gravity * dt;

        if (groundedNow && velocity.y <= 0f)
        {
            velocity.y = -groundStickForce;
            jumpCount = 0;
        }

        Vector3 totalMove = (moveOnSurface * moveSpeed) + Vector3.up * velocity.y;
        controller.Move(totalMove * dt);

        float horizontalSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        HandleFootsteps(dt, horizontalSpeed);
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

    private void HandleFootsteps(float dt, float horizontalSpeed)
    {
        if (!controller.isGrounded || horizontalSpeed < minFootstepSpeed)
        {
            // Don't slam to zero or you'll re-trigger instantly when speed flickers.
            stepTimer = Mathf.Min(stepTimer, stepInterval);
            return;
        }

        stepTimer -= dt;

        if (stepTimer <= 0f)
        {
            PlayFootstepSFX();
            stepTimer = stepInterval;
        }
    }

    private bool UpdateGroundHit()
    {
        // Slightly above feet so we still find ground on steps/edges.
        Vector3 origin = transform.position + controller.center + Vector3.up * 0.05f;
        float radius = Mathf.Max(0.05f, controller.radius * 0.95f);

        hasGroundHit = Physics.SphereCast(
            origin,
            radius,
            Vector3.down,
            out groundHit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        return hasGroundHit;
    }

    private void ApplyRecoilKick(AmmoTypeConfig cfg)
    {
        if (cfg == null) return;

        recoilTargetPos -= new Vector3(0f, 0f, cfg.recoilKickback);
        recoilTargetRot += new Vector3(-cfg.recoilUp, 0f, 0f);

        recoilRecovery = Mathf.Max(0.01f, cfg.recoilRecovery);
    }

    private void PlayJumpSFX() => playerAudio?.PlayJump();
    private void PlayLandingSFX() => playerAudio?.PlayLanding();
    private void PlayFootstepSFX() => playerAudio?.PlayFootstep();

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

    private void TryInteract()
    {
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Ignore))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null) return;

            interactable.Interact(gameObject);
        }
    }
}