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
    [SerializeField] private string isShootingBool = "isShooting";

    private float fireTimer;
    private bool shotArmed;
    private Transform currentTarget;

    private EnemyVisualAnimatorProxy animProxy;
    private float attackRangeSqr;

    public void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;
    public Transform GetCurrentTarget() => currentTarget;

    private void Awake()
    {
        animProxy = GetComponentInParent<EnemyVisualAnimatorProxy>();
        attackRangeSqr = attackRange * attackRange;
    }

    private void Update()
    {
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    public bool CanAttack(Transform target)
    {
        if (target == null) return false;
        return (target.position - transform.position).sqrMagnitude <= attackRangeSqr;
    }

    public void TickAttack(Transform target)
    {
        if (fireTimer > 0f || target == null || !CanAttack(target)) return;

        currentTarget = target;
        shotArmed = true;

        animProxy?.SetBool(isShootingBool, true);
        fireTimer = fireCooldown;
    }

    // Called by animation event at the fire frame
    public void FireProjectile()
    {
        if (!shotArmed) return;
        shotArmed = false;
        SpawnProjectile();
    }

    // Called at the end of the shoot animation
    public void OnShootAnimFinished()
    {
        if (currentTarget == null || !CanAttack(currentTarget))
            animProxy?.SetBool(isShootingBool, false);
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null || muzzle == null || currentTarget == null) return;

        Vector3 aimPos = currentTarget.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (aimPos - muzzle.position).normalized;

        GameObject projGO = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
        if (projGO.TryGetComponent(out Projectile proj))
            proj.Init(dir, projectileSpeed, damage, gameObject);
    }
}