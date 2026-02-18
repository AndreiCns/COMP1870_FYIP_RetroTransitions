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
    // Drives the looping shoot state
    [SerializeField] private string isShootingBool = "isShooting";

    private float fireTimer;

    // Ensures only one projectile is spawned per shot cycle
    private bool shotArmed;

    private Transform currentTarget;
    private EnemyVisualAnimatorProxy animProxy;
    private float attackRangeSqr;

    // Updated on style swap so we spawn from the correct model
    public void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;

    private void Awake()
    {
        animProxy = GetComponentInParent<EnemyVisualAnimatorProxy>();
        attackRangeSqr = attackRange * attackRange;
    }

    private void Update()
    {
        // Simple cooldown
        fireTimer -= Time.deltaTime;
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
        if (fireTimer > 0f) return;
        if (target == null) return;
        if (!CanAttack(target)) return;

        currentTarget = target;

        shotArmed = true;

        // Stay in shoot state while target is valid
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

    private void SpawnProjectile()
    {
        if (projectilePrefab == null || muzzle == null) return;
        if (currentTarget == null) return;

        Vector3 aimPos = currentTarget.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (aimPos - muzzle.position).normalized;

        GameObject projGO = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));

        if (projGO.TryGetComponent(out Projectile proj))
            proj.Init(dir, projectileSpeed, damage, gameObject);
    }

    // Called at the end of the shoot animation
    public void OnShootAnimFinished()
    {
        if (currentTarget == null)
        {
            animProxy?.SetBool(isShootingBool, false);
            return;
        }

        if (CanAttack(currentTarget))
            return;

        animProxy?.SetBool(isShootingBool, false);
    }
}
