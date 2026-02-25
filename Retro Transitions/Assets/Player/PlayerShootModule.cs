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
    [SerializeField] private LayerMask hitMask;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;
    [SerializeField] private bool logWarnings = true;

    

    public bool CanFire => fireLockTimer <= 0f;

    private float fireLockTimer;
    private GameObject owner;

    private void Awake()
    {
        owner = transform.root.gameObject;

        if (playerCamera == null && logWarnings)
            Debug.LogWarning($"{name}: playerCamera not assigned", this);
        if (muzzle == null && logWarnings)
            Debug.LogWarning($"{name}: muzzle not assigned", this);
        if (combatController == null && logWarnings)
            Debug.LogWarning($"{name}: combatController not assigned", this);
        if (playerAudio == null && logWarnings)
            Debug.LogWarning($"{name}: playerAudio not assigned", this);

    }

    private void OnEnable()
    {
        // Reset local lock if this module gets disabled/enabled on style swap.
        fireLockTimer = 0f;
    }

    private void OnDisable()
    {
        fireLockTimer = 0f;
    }

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
            return;

        // VFX (config + global style handled inside controller)
        muzzleFlash?.Play(cfg);

        // SFX (per ammo type clip handled by audio manager)
        playerAudio?.PlayGunshot(cfg);

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (drawDebugRay)
                Debug.DrawLine(origin, hit.point, Color.red, 1f);

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
        if (combatController == null)
            return;

        FireProjectileInternal(combatController.CurrentConfig);
    }
}