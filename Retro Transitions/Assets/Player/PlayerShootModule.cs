using UnityEngine;

public class PlayerShootModule : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private MuzzleFlashController muzzleFlash;

    [Header("Audio")]
    [SerializeField] private AudioSource gunshotSource;
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private Vector2 pitchVariation = new Vector2(0.97f, 1.03f);

    [Header("Tuning")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 200f;
    [SerializeField] private LayerMask hitMask;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;
    [SerializeField] private bool logWarnings = true;

    private GameObject owner;

    // Internal fire lock to enforce ROF independently of animation timing.
    private float fireLockTimer;

    public bool CanFire => fireLockTimer <= 0f;

    private void Awake()
    {
        // Root object is treated as the damage source.
        owner = transform.root.gameObject;

        // These aren’t fatal, but worth surfacing early during setup.
        if (playerCamera == null && logWarnings)
            Debug.LogWarning($"{name}: playerCamera not assigned", this);

        if (muzzle == null && logWarnings)
            Debug.LogWarning($"{name}: muzzle not assigned", this);

        if (gunshotSource == null && logWarnings)
            Debug.LogWarning($"{name}: gunshotSource not assigned", this);
    }

    private void Update()
    {
        // Countdown lock so firing stays deterministic even if input spams.
        if (fireLockTimer > 0f)
            fireLockTimer -= Time.deltaTime;
    }

    // --- Runtime configuration (driven by CombatController) ---

    public void SetDamage(float newDamage)
    {
        // Prevent accidental negative damage from config.
        damage = Mathf.Max(0f, newDamage);
    }

    public void SetMuzzleFlash(MuzzleFlashController newFlash)
    {
        if (newFlash == null) return;
        muzzleFlash = newFlash;
    }

    public bool TryBeginFire(float lockDuration)
    {
        // Hard gate: if locked, reject immediately.
        if (fireLockTimer > 0f)
            return false;

        // Lock duration mirrors weapon cooldown.
        fireLockTimer = Mathf.Max(0.01f, lockDuration);
        return true;
    }

    // Animation event: this handles the actual "shot".
    public void FireProjectile()
    {
        if (playerCamera == null) return;

        // Visual + audio feedback live here so timing matches the anim frame.
        muzzleFlash?.Play();

        if (gunshotSource != null && gunshotClip != null)
        {
            gunshotSource.pitch = Random.Range(pitchVariation.x, pitchVariation.y);
            gunshotSource.PlayOneShot(gunshotClip);
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        // Hitscan logic – no projectile object, purely raycast-based.
        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (drawDebugRay)
                Debug.DrawLine(origin, hit.point, Color.red, 1f);

            if (hit.collider.TryGetComponent(out IDamageable dmg))
            {
                dmg.TakeDamage(damage, new DamageInfo
                {
                    Point = hit.point,
                    Direction = direction,
                    Source = owner
                });
            }
        }
        else if (drawDebugRay)
        {
            Debug.DrawRay(origin, direction * range, Color.yellow, 1f);
        }
    }

    private void OnDisable()
    {
        // Reset lock when disabled to avoid stale state on weapon swap.
        fireLockTimer = 0f;
    }
}
