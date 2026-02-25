using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private LayerMask hitMask = ~0;

    private Vector3 dir = Vector3.forward;
    private float speed = 0f;
    private float damage = 0f;
    private GameObject source;

    private bool isInitialised;
    private float remainingLife;

    public void Init(Vector3 direction, float newSpeed, float newDamage, GameObject newSource)
    {
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        speed = Mathf.Max(0f, newSpeed);
        damage = Mathf.Max(0f, newDamage);
        source = newSource;

        remainingLife = Mathf.Max(0.01f, lifeTime);
        isInitialised = true;
    }

    private void OnEnable()
    {
        // Full reset so pooled instances start clean. Init() must be called after this.
        isInitialised = false;
        remainingLife = Mathf.Max(0.01f, lifeTime);
        dir = Vector3.forward;
        speed = 0f;
        damage = 0f;
        source = null;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Check before lifetime so a pooled projectile that missed Init() fails immediately
        if (!isInitialised)
        {
            Debug.LogWarning($"Projectile '{name}' was never initialised.");
            Destroy(gameObject);
            return;
        }

        remainingLife -= dt;
        if (remainingLife <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        Vector3 step = dir * speed * dt;
        float dist = step.magnitude;

        if (dist <= 0.0001f) return;

        if (Physics.Raycast(start, dir, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
            TryApplyDamage(hit);
            Destroy(gameObject);
            return;
        }

        transform.position = start + step;
    }

    private void TryApplyDamage(RaycastHit hit)
    {
        if (hit.collider == null) return;
        if (source != null && hit.collider.transform.IsChildOf(source.transform)) return;
        if (damage <= 0f) return;

        if (hit.collider.TryGetComponent(out IDamageable dmg))
            dmg.TakeDamage(damage, new DamageInfo { Point = hit.point, Direction = dir, Source = source });
    }
}