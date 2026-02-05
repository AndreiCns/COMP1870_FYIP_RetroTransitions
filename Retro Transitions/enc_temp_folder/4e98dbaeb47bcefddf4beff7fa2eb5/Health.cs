using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 50f;
    public float Current { get; private set; }

    public UnityEvent<float> OnDamaged;   // passes damage amount
    public UnityEvent OnDied;

    void Awake() => Current = maxHealth;

    public void TakeDamage(float amount, DamageInfo info)
    {
        if (Current <= 0) return;

        Current = Mathf.Max(0, Current - amount);
        OnDamaged?.Invoke(amount);

        if (Current <= 0)
            OnDied?.Invoke();
    }
}
