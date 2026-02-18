using UnityEngine;

public class EnemyAnimEventRelay : MonoBehaviour
{
    private RangedAttackModule attackModule;

    private void Awake()
    {
        // Animation lives on the child, shooting logic lives on the root.
        // Cache once so events can forward cleanly.
        attackModule = GetComponentInParent<RangedAttackModule>(true);

        if (attackModule == null)
            Debug.LogError($"[{name}] EnemyAnimEventRelay: No RangedAttackModule found in parents.", this);
    }

    // Called from the fire frame in the animation
    public void FireProjectile()
    {
        attackModule?.FireProjectile();
    }

    // Called on the last frame of the shoot animation
    public void OnShootAnimFinished()
    {
        attackModule?.OnShootAnimFinished();
    }
}
