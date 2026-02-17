using UnityEngine;

public class RangedAttackModule : MonoBehaviour, IEnemyAttack
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;
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

    private float fireTimer;
    private Transform currentTarget;
    private EnemyVisualAnimatorProxy animProxy;
    private float attackRangeSqr;

    private void Awake()
    {
        animProxy = GetComponentInParent<EnemyVisualAnimatorProxy>();
        attackRangeSqr = attackRange * attackRange;
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
        if (!CanAttack(target)) return;

        currentTarget = target;

        // drive both animators / active animator through proxy
        animProxy?.SetTrigger(shootTriggerName);

        // IMPORTANT: actual projectile spawn is now done by ANIMATION EVENT
        fireTimer = fireCooldown;
    }

    // THIS is what your Animation Event should call
    public void FireProjectile_AnimEvent()
    {
        FireProjectileInternal();
    }

    private void FireProjectileInternal()
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
