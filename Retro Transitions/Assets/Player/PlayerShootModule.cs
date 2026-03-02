using UnityEngine;

public class PlayerShootModule : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private MuzzleFlashController muzzleFlash;

    [Header("Refs")]
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private PlayerAudioController playerAudio;

    [Header("Tuning")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 200f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;
    [SerializeField] private bool logWarnings = true;
    [SerializeField] private bool logFireEvent = false;

    public bool CanFire => fireLockTimer <= 0f;

    private float fireLockTimer;
    private GameObject owner;

    private void Awake()
    {
        owner = transform.root.gameObject;

        if (combatController == null)
            combatController = GetComponentInParent<PlayerCombatController>();

        if (playerAudio == null)
            playerAudio = GetComponentInParent<PlayerAudioController>();

        if (muzzleFlash == null)
            muzzleFlash = GetComponentInChildren<MuzzleFlashController>(true);

        if (muzzle == null && muzzleFlash != null)
        {
            // If your muzzle flash controller already knows the muzzle, you can ignore this.
            // Otherwise try find a child called "Muzzle" as a convention.
            Transform t = transform.Find("Muzzle");
            if (t != null) muzzle = t;
        }

        if (playerCamera == null)
            playerCamera = GetComponentInParent<Camera>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (logWarnings)
        {
            if (playerCamera == null) Debug.LogWarning($"{name}: playerCamera not found/assigned.", this);
            if (combatController == null) Debug.LogWarning($"{name}: combatController not found/assigned.", this);
        }
    }

    private void OnEnable() => fireLockTimer = 0f;
    private void OnDisable() => fireLockTimer = 0f;

    private void Update()
    {
        if (fireLockTimer > 0f)
            fireLockTimer -= Time.deltaTime;
    }

    public void SetDamage(float newDamage) => damage = Mathf.Max(0f, newDamage);

    public bool TryBeginFire(float lockDuration)
    {
        if (fireLockTimer > 0f)
            return false;

        fireLockTimer = Mathf.Max(0.01f, lockDuration);
        return true;
    }

    private void FireProjectileInternal(AmmoTypeConfig cfg)
    {
        if (playerCamera == null || cfg == null)
        {
            if (logWarnings) Debug.LogWarning($"{name}: Fire blocked (missing camera or cfg).", this);
            return;
        }

        // VFX + SFX (timed to the animation event)
        muzzleFlash?.Play(cfg);
        playerAudio?.PlayGunshot(cfg);

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (drawDebugRay) Debug.DrawLine(origin, hit.point, Color.red, 1f);

            if (hit.collider.TryGetComponent(out IDamageable dmg))
                dmg.TakeDamage(damage, new DamageInfo { Point = hit.point, Direction = direction, Source = owner });
        }
        else if (drawDebugRay)
        {
            Debug.DrawRay(origin, direction * range, Color.yellow, 1f);
        }
    }

    // Animation Event calls this (no params)
    public void FireProjectile()
    {
        if (logFireEvent)
            Debug.Log($"[{name}] FireProjectile animation event received.", this);

        if (combatController == null)
        {
            if (logWarnings) Debug.LogWarning($"{name}: Fire blocked (combatController null).", this);
            return;
        }

        AmmoTypeConfig cfg = combatController.CurrentConfig;
        if (cfg == null)
        {
            if (logWarnings) Debug.LogWarning($"{name}: Fire blocked (CurrentConfig null).", this);
            return;
        }

        FireProjectileInternal(cfg);
    }
    public void FireImmediate(AmmoTypeConfig cfg)
    {
        if (cfg == null) return;
        FireProjectileInternal(cfg);
    }
}