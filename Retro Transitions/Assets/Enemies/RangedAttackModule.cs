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
    [SerializeField] private float shootStateTime = 0.25f; // how long we keep isShooting true

    private float fireTimer;
    private float shootTimer;
    private bool shotArmed;              // <- only allow ONE event per shot
    private Transform currentTarget;
    private EnemyVisualAnimatorProxy animProxy;
    private float attackRangeSqr;
    public void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;


    private void Awake()
    {
        animProxy = GetComponentInParent<EnemyVisualAnimatorProxy>();
        attackRangeSqr = attackRange * attackRange;
    }

    private void Update()
    {
        fireTimer -= Time.deltaTime;

        if (shootTimer > 0f)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
                animProxy?.SetBool(isShootingBool, false);
        }
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

        // Arm exactly ONE event spawn for this shot cycle
        shotArmed = true;

        // Drive animation (bool version)
        animProxy?.SetBool(isShootingBool, true);
        shootTimer = shootStateTime;

        // Start cooldown immediately
        fireTimer = fireCooldown;
    }

    // Animation Event calls this (via relay)
    public void FireProjectile_AnimEvent()
    {
        // If we already spawned for this shot cycle, ignore duplicates
        if (!shotArmed) return;
        shotArmed = false;

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
