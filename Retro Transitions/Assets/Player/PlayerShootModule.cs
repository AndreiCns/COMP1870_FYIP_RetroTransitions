using UnityEngine;

public class PlayerShootModule : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private Projectile projectilePrefab;

    [Header("Tuning")]
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private float damage = 10f;
    

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private Transform owner; // who fired (used by projectile to avoid self-hit)

    private void Awake()
    {
        owner = transform.root; // Player root
        if (muzzle == null && logWarnings) Debug.LogWarning($"{name}: muzzle not assigned", this);
        if (projectilePrefab == null && logWarnings) Debug.LogWarning($"{name}: projectilePrefab not assigned", this);
    }

    // CALLED BY ANIMATION EVENT
    public void FireProjectile()
    {
        if (muzzle == null || projectilePrefab == null) return;

        // Aim forward from camera/weapon direction
        Vector3 dir = muzzle.forward;

        Projectile p = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
        p.Init(dir, projectileSpeed, damage, owner.gameObject);
    }
}
