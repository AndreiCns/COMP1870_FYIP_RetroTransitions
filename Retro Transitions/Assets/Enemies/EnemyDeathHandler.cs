using UnityEngine;
using UnityEngine.AI;

public class EnemyDeathHandler : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private EnemyVisualAnimatorProxy animProxy;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private MonoBehaviour[] disableOnDeath;

    [Header("Animation")]
    [SerializeField] private string dieTrigger = "Die";
    [SerializeField] private float destroyDelay = 3f; // set to 0 if the animation event destroys it
    [SerializeField] private bool verboseLogs = false;

    private bool dead;

    private void Awake()
    {
        // Fallbacks so the prefab works even if I forget to wire something
        if (health == null) health = GetComponent<Health>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animProxy == null) animProxy = GetComponent<EnemyVisualAnimatorProxy>();

        if (health != null)
            health.OnDied.AddListener(HandleDeath);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDied.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        // Guard against multiple death calls (e.g. double hits in the same frame)
        if (dead) return;
        dead = true;

        if (verboseLogs)
            Debug.Log($"[{name}] HandleDeath -> trigger '{dieTrigger}'", this);

        // Stop AI/combat scripts
        if (disableOnDeath != null)
        {
            foreach (var b in disableOnDeath)
                if (b != null) b.enabled = false;
        }

        // Stop navigation immediately
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        // No more blocking or taking damage
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Play death in whichever style is currently active
        animProxy?.SetTrigger(dieTrigger);

        // Fallback cleanup (if I'm not destroying via an animation event)
        if (destroyDelay > 0f)
            Destroy(gameObject, destroyDelay);
    }

    // Optional: call this from a death animation event
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
