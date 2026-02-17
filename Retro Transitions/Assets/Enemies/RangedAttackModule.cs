using UnityEngine;

public class RangedAttackModule : MonoBehaviour, IEnemyAttack
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;              // IMPORTANT: put this on root (always active)
    [SerializeField] private GameObject projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private float attackRange = 18f;
    [SerializeField] private float fireCooldown = 0.8f;
    [SerializeField] private float projectileSpeed = 25f;
    [SerializeField] private float damage = 10f;

    [Header("Aiming")]
    [SerializeField] private float aimHeightOffset = 1.2f;

    [Header("Animation")]
    [SerializeField] private string shootTriggerName = "Shoot";
    [SerializeField] private float fireWindup = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private float fireTimer;
    private Transform currentTarget;

    private EnemyVisualAnimatorProxy animProxy;

    private bool pendingShot;
    private float pendingTimer;

    private float attackRangeSqr;

    private void Awake()
    {
        animProxy = GetComponentInParent<EnemyVisualAnimatorProxy>();
        attackRangeSqr = attackRange * attackRange;

        if (muzzle == null && logWarnings)
            Debug.LogWarning($"{name}: RangedAttackModule has no muzzle assigned.", this);

        if (projectilePrefab == null && logWarnings)
            Debug.LogWarning($"{name}: RangedAttackModule has no projectilePrefab assigned.", this);

        if (animProxy == null && logWarnings)
            Debug.LogWarning($"{name}: No EnemyVisualAnimatorProxy found in parent.", this);
    }

    private void Update()
    {
        if (!pendingShot) return;

        // Cancel if target lost
        if (currentTarget == null)
        {
            pendingShot = false;
            return;
        }

        pendingTimer -= Time.deltaTime;
        if (pendingTimer > 0f) return;

        pendingShot = false;
        FireProjectile();
    }

    public bool CanAttack(Transform target)
    {
        if (target == null) return false;

        Vector3 delta = target.position - transform.position;
        return delta.sqrMagnitude <= attackRangeSqr;
    }

    public Transform GetCurrentTarget() => currentTarget;

    public void TickAttack(Transform target)
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;
        if (target == null) return;

        // Gate by range again for safety
        if (!CanAttack(target)) return;

        currentTarget = target;

        // Trigger anim (retro/modern via proxy)
        animProxy?.SetTrigger(shootTriggerName);

        // Queue shot once per fire cycle
        pendingShot = true;
        pendingTimer = fireWindup;

        fireTimer = fireCooldown;
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || muzzle == null) return;
        if (currentTarget == null) return;

        Vector3 aimPos = currentTarget.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (aimPos - muzzle.position).normalized;

        GameObject projGO = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));

        if (projGO.TryGetComponent(out Projectile proj))
            proj.Init(dir, projectileSpeed, damage, gameObject);
    }
}
