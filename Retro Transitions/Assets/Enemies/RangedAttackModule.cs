using UnityEngine;

public class RangedAttackModule : MonoBehaviour, IEnemyAttack
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireCooldown = 0.8f;
    [SerializeField] private float projectileSpeed = 25f;
    [SerializeField] private float damage = 10f;

    [Header("Aiming")]
    [Tooltip("Vertical offset added to target position to aim at chest/head-ish.")]
    [SerializeField] private float aimHeightOffset = 1.2f;

    [Header("Animation")]
    [SerializeField] private string shootTriggerName = "Shoot";

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private float fireTimer;
    private Transform currentTarget;
    private Animator cachedAnimator;

    private void Awake()
    {
        cachedAnimator = GetComponentInParent<Animator>();

        if (muzzle == null && logWarnings)
            Debug.LogWarning($"{name}: RangedAttackModule has no muzzle assigned.", this);

        if (projectilePrefab == null && logWarnings)
            Debug.LogWarning($"{name}: RangedAttackModule has no projectilePrefab assigned.", this);

        if (cachedAnimator == null && logWarnings)
            Debug.LogWarning($"{name}: No Animator found in parent hierarchy. Shoot trigger will do nothing.", this);
    }

    public bool CanAttack(Transform target)
    {
        if (target == null) return false;

        // Note: uses this module's transform position. If you want agent root, move module to root or reference it.
        float dist = Vector3.Distance(transform.position, target.position);
        return dist <= attackRange;
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    public void TickAttack(Transform target)
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        if (target == null) return;

        currentTarget = target;

        if (cachedAnimator != null)
            cachedAnimator.SetTrigger(shootTriggerName);

        fireTimer = fireCooldown;
    }

    /// <summary>
    /// Called by an Animation Event (via EnemyAnimEvents) to actually spawn the projectile.
    /// </summary>
    public void FireProjectileFromAnim()
    {
        if (projectilePrefab == null || muzzle == null)
            return;

        if (currentTarget == null)
            return;

        Vector3 aimPos = currentTarget.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (aimPos - muzzle.position).normalized;

        GameObject projGO = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));

        if (projGO.TryGetComponent(out Projectile proj))
            proj.Init(dir, projectileSpeed, damage, gameObject);
    }
}
