using UnityEngine;

public class EnemyAnimEvents : MonoBehaviour
{
    [Tooltip("If left empty, will auto-find the first RangedAttackModule in children.")]
    [SerializeField] private RangedAttackModule rangedAttack;

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private void Awake()
    {
        if (rangedAttack == null)
            rangedAttack = GetComponentInChildren<RangedAttackModule>();

        if (rangedAttack == null && logWarnings)
            Debug.LogWarning($"{name}: EnemyAnimEvents could not find a RangedAttackModule in children.", this);
    }

    // This is the method name your Animation Event must call.
    public void FireProjectile()
    {
        if (rangedAttack == null) return;
        rangedAttack.FireProjectileFromAnim();
    }
}
