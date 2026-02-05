using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float lifeTime = 4f;
    public LayerMask hitMask;

    private Vector3 dir;
    private float speed;
    private float damage;
    private GameObject source;

    public void Init(Vector3 direction, float speed, float damage, GameObject source)
    {
        this.dir = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.source = source;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        Vector3 step = dir * speed * dt;

        // Raycast step to avoid tunneling
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, step.magnitude, hitMask))
        {
            TryApplyDamage(hit);
            Destroy(gameObject);
            return;
        }

        transform.position += step;
    }

    void TryApplyDamage(RaycastHit hit)
    {
        if (hit.collider == null) return;

        // Don’t damage self
        if (source != null && hit.collider.transform.IsChildOf(source.transform)) return;

        if (hit.collider.TryGetComponent(out IDamageable dmg))
        {
            dmg.TakeDamage(damage, new DamageInfo
            {
                Point = hit.point,
                Direction = dir,
                Source = source
            });
        }
    }
}
