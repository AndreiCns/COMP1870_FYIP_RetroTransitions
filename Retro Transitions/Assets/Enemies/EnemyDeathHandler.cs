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
    [SerializeField] private float destroyDelay = 3f; // set to 0 if using anim event
    [SerializeField] private bool verboseLogs = false;

    private bool dead;

    private void Awake()
    {
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
        if (dead) return;
        dead = true;

        if (verboseLogs) Debug.Log($"[{name}] HandleDeath -> trigger '{dieTrigger}'", this);

        // stop AI & combat
        if (disableOnDeath != null)
            foreach (var b in disableOnDeath)
                if (b != null) b.enabled = false;

        // stop nav
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        // stop collisions/hits
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // play death anim (modern or retro)
        animProxy?.SetTrigger(dieTrigger);

        // fallback destroy
        if (destroyDelay > 0f)
            Destroy(gameObject, destroyDelay);
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
