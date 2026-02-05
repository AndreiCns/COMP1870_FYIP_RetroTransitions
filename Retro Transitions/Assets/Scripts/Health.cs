using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onDamaged;
    [SerializeField] private UnityEvent onDied;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    // Read-only exposure for other scripts (fixes your CS1061)
    public UnityEvent<float> OnDamaged => onDamaged;
    public UnityEvent OnDied => onDied;

    private void Awake()
    {
        if (maxHealth <= 0f) maxHealth = 1f;
        Current = maxHealth;
    }

    public void TakeDamage(float amount, DamageInfo info)
    {
        if (IsDead) return;
        if (amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        onDamaged?.Invoke(amount);

        if (IsDead)
            onDied?.Invoke();
    }

    public void ResetHealth() => Current = maxHealth;
}
