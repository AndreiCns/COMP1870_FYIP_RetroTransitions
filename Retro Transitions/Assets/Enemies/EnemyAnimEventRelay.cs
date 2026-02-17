using UnityEngine;

public class EnemyAnimEventRelay : MonoBehaviour
{
    private RangedAttackModule attackModule;

    private void Awake()
    {
        attackModule = GetComponentInParent<RangedAttackModule>(true);
        if (attackModule == null)
            Debug.LogError($"[{name}] EnemyAnimEventRelay: No RangedAttackModule found in parents.", this);
    }

    // If your Animation Event is still named "FireProjectile"
    public void FireProjectile()
    {
        attackModule?.FireProjectile_AnimEvent();
    }

    // If you renamed the Animation Event to "FireProjectile_AnimEvent"
    public void FireProjectile_AnimEvent()
    {
        attackModule?.FireProjectile_AnimEvent();
    }
}
