using UnityEngine;
using UnityEngine.AI;

public class EnemyDeathHandler : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private MonoBehaviour[] disableOnDeath; // brain, attack module, perception etc.

    [Header("Animation")]
    [SerializeField] private string dieTrigger = "Die";
    [SerializeField] private float destroyDelay = 3f; // fallback if you don't use anim event

    private bool dead;

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

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

        // stop AI & combat
        if (disableOnDeath != null)
        {
            foreach (var b in disableOnDeath)
                if (b != null) b.enabled = false;
        }

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

        // play death anim
        if (animator != null)
            animator.SetTrigger(dieTrigger);

        // fallback destroy (replace with animation event if you want precision)
        Destroy(gameObject, destroyDelay);
    }

    // OPTIONAL: call this via animation event at the end of the death clip
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
