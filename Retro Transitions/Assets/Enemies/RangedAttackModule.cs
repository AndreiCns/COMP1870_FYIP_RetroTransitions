using UnityEngine;

public class RangedAttackModule : MonoBehaviour, IEnemyAttack
{
    [Header("Setup")]
    public Transform muzzle;
    public GameObject projectilePrefab;

    [Header("Tuning")]
    public float attackRange = 15f;
    public float fireCooldown = 0.8f;
    public float projectileSpeed = 25f;
    public float damage = 10f;

    private float fireTimer;

    public bool CanAttack(Transform target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= attackRange;
    }

    public void TickAttack(Transform target)
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;
        if (target == null || muzzle == null || projectilePrefab == null) return;

        // Aim (simple: look at target chest)
        Vector3 aimPos = target.position + Vector3.up * 1.2f;
        Vector3 dir = (aimPos - muzzle.position).normalized;

        GameObject projGO = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
        if (projGO.TryGetComponent(out Projectile proj))
        {
            proj.Init(dir, projectileSpeed, damage, gameObject);
        }

        fireTimer = fireCooldown;
    }
}
