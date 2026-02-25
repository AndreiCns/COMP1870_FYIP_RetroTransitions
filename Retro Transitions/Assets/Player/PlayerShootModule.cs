using UnityEngine;

public class PlayerShootModule : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private MuzzleFlashController muzzleFlash;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private Vector2 pitchVariation = new Vector2(0.97f, 1.03f);

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
        if (audioSource == null && logWarnings)
            Debug.LogWarning($"{name}: audioSource not assigned", this);
    }

    private void OnEnable()
    {
        // Always start with a clean lock — if this module was disabled mid-cooldown
        // (e.g. on a style swap) we don't want to inherit a stale lock from the
        // previous session. PlayerCombatController owns the authoritative cooldown.
        fireLockTimer = 0f;
    }

    private void OnDisable()
    {
        // Belt-and-suspenders: clear on disable too so there's no stale state
        // if the module is re-enabled before the coroutine cache runs.
        fireLockTimer = 0f;
    }

    private void Update()
    {
        if (fireLockTimer > 0f)
            fireLockTimer -= Time.deltaTime;
    }

    

    public void SetDamage(float newDamage) => damage = Mathf.Max(0f, newDamage);
    public void SetMuzzleFlash(MuzzleFlashController newFlash) { if (newFlash != null) muzzleFlash = newFlash; }

    public bool TryBeginFire(float lockDuration)
    {
        if (fireLockTimer > 0f)
            return false;

        fireLockTimer = Mathf.Max(0.01f, lockDuration);
        return true;
    }


    public void FireProjectile()
    {
        if (playerCamera == null) return;

        muzzleFlash?.Play();

        if (audioSource != null && gunshotClip != null)
        {
            audioSource.pitch = Random.Range(pitchVariation.x, pitchVariation.y);
            audioSource.PlayOneShot(gunshotClip);
        }

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
}