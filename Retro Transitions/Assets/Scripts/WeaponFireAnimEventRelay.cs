using UnityEngine;

public class WeaponFireAnimEventRelay : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerShootModule shootModule;

    [Header("Debug")]
    [SerializeField] private bool log = false;

    private void Awake()
    {
        if (shootModule == null)
            shootModule = GetComponentInParent<PlayerShootModule>(true);

        if (shootModule == null)
            Debug.LogError($"{name}: No PlayerShootModule found for anim event relay.", this);
    }

    // Animation Event calls THIS
    public void FireProjectile()
    {
        if (log) Debug.Log($"[{name}] FireProjectile anim event relayed.", this);
        shootModule?.FireProjectile();
    }
}