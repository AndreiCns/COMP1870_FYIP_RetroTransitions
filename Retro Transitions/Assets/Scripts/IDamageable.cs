public interface IDamageable
{
    void TakeDamage(float amount, DamageInfo info);
}

public struct DamageInfo
{
    public UnityEngine.Vector3 Point;
    public UnityEngine.Vector3 Direction;
    public UnityEngine.GameObject Source;
}
