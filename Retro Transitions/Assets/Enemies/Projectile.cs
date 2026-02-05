using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private LayerMask hitMask = ~0; // default: hit everything

    // Runtime state
    private Vector3 dir = Vector3.forward;
    private float speed = 0f;
    private float damage = 0f;
    private GameObject source;

    private bool isInitialised;
    private float remainingLife;

    public void Init(Vector3 direction, float newSpeed, float newDamage, GameObject newSource)
    {
        // Validate & normalise
        dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        speed = Mathf.Max(0f, newSpeed);
        damage = Mathf.Max(0f, newDamage);
        source = newSource;

        remainingLife = Mathf.Max(0.01f, lifeTime);
        isInitialised = true;
    }

    private void OnEnable()
    {
        // Prevent pooled projectiles from inheriting old state
        isInitialised = false;
        remainingLife = Mathf.Max(0.01f, lifeTime);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Lifetime handling (more pool-friendly than Destroy + Invoke)
        remainingLife -= dt;
        if (remainingLife <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // If Init() was never called, fail loudly (helps debugging)
        if (!isInitialised)
        {
            Debug.LogWarning($"Projectile '{name}' was never initialised. Destroying to avoid stray objects.");
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        Vector3 step = dir * speed * dt;
        float dist = step.magnitude;

        // If speed is 0, don't raycast/move
        if (dist <= 0.0001f) return;

        // Raycast step to avoid tunneling
        if (Physics.Raycast(start, dir, out RaycastHit hit, dist, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Snap to hit point for correct VFX placement later
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

        // Don’t damage self
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
