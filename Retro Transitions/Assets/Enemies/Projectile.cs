using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private LayerMask hitMask = ~0;

    // Runtime values set on spawn
    private Vector3 dir = Vector3.forward;
    private float speed = 0f;
    private float damage = 0f;
    private GameObject source;

    private bool isInitialised;
    private float remainingLife;

    public void Init(Vector3 direction, float newSpeed, float newDamage, GameObject newSource)
    {
        // Clamp inputs so bad values don’t cause weird behaviour
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        speed = Mathf.Max(0f, newSpeed);
        damage = Mathf.Max(0f, newDamage);
        source = newSource;

        remainingLife = Mathf.Max(0.01f, lifeTime);
        isInitialised = true;
    }

    private void OnEnable()
    {
        // Reset in case this is reused later (e.g. pooling)
        isInitialised = false;
        remainingLife = Mathf.Max(0.01f, lifeTime);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Kill after lifetime expires
        remainingLife -= dt;
        if (remainingLife <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // If Init was never called, don’t let it exist
        if (!isInitialised)
        {
            Debug.LogWarning($"Projectile '{name}' was never initialised.");
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        Vector3 step = dir * speed * dt;
        float dist = step.magnitude;

        if (dist <= 0.0001f) return;

        // Raycast per step to avoid tunnelling
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

        // Ignore hits on the shooter
        if (source != null && hit.collider.transform.IsChildOf(source.transform)) return;
        if (damage <= 0f) return;

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
