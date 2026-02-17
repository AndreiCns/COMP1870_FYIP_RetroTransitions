using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRangedBrain : MonoBehaviour
{
    private enum State { Idle, Chase, Dead }

    [Header("Core")]
    [SerializeField] private Transform target;                 // Player root
    [SerializeField] private Health health;
    [SerializeField] private EnemyVisualAnimatorProxy animProxy;

    [Header("Combat")]
    [SerializeField] private MonoBehaviour attackModuleBehaviour; // must implement IEnemyAttack
    private IEnemyAttack attackModule;

    [Tooltip("When player is within this distance (and LOS if enabled), enemy wakes up and chases.")]
    [SerializeField] private float aggroRange = 25f;

    [Tooltip("Enemy stops moving around this distance and shoots.")]
    [SerializeField] private float stopDistance = 12f;

    [Tooltip("Small buffer so it doesn’t jitter between stop/move.")]
    [SerializeField] private float distanceHysteresis = 1.5f;

    [Header("Line of sight (optional, 90s feel = simple ray)")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private Transform eyes;                   // optional
    [SerializeField] private float eyeHeightFallback = 1.6f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;   // set to Everything minus Enemy

    [Header("Animation")]
    [SerializeField] private string isWalkingParam = "isWalking";

    private NavMeshAgent agent;
    private State state = State.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animProxy == null) animProxy = GetComponent<EnemyVisualAnimatorProxy>();

        attackModule = attackModuleBehaviour as IEnemyAttack;

        if (health != null)
            health.OnDied.AddListener(OnDied);

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDied.RemoveListener(OnDied);
    }

    private void Update()
    {
        if (state == State.Dead) return;
        if (target == null || attackModule == null) { SetWalking(false); return; }

        float distToTarget = Vector3.Distance(transform.position, target.position);

        bool inAggro = distToTarget <= aggroRange;
        bool hasLos = !requireLineOfSight || HasLineOfSight(target);

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                agent.ResetPath();
                SetWalking(false);

                if (inAggro && hasLos)
                    state = State.Chase;
                break;

            case State.Chase:
                // If you want “classic” behaviour: once awake, don’t go back to idle.
                // If you DO want it to calm down when far, uncomment:
                // if (!inAggro) { state = State.Idle; break; }

                FaceTarget(target);

                float moveAt = stopDistance + distanceHysteresis;
                float stopAt = Mathf.Max(0f, stopDistance - distanceHysteresis);

                if (distToTarget > moveAt)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = stopDistance;
                    agent.SetDestination(target.position);
                    SetWalking(true);
                }
                else if (distToTarget < stopAt)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                    SetWalking(false);
                }
                else
                {
                    // inside hysteresis band: keep current state (prevents jitter)
                }

                // Shoot if in attack range (uses your module’s range)
                if (attackModule.CanAttack(target))
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                    SetWalking(false);

                    attackModule.TickAttack(target);
                }
                break;
        }
    }

    private bool HasLineOfSight(Transform t)
    {
        Vector3 origin = eyes != null ? eyes.position : (transform.position + Vector3.up * eyeHeightFallback);
        Vector3 targetPos = t.position + Vector3.up * 1.1f;

        Vector3 toTarget = targetPos - origin;
        float dist = toTarget.magnitude;
        if (dist <= 0.001f) return true;

        Vector3 dir = toTarget / dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, lineOfSightMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == t || hit.transform.root == t.root;
        }
        return false;
    }

    private void FaceTarget(Transform t)
    {
        Vector3 flat = t.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 12f);
    }

    private void SetWalking(bool walking)
    {
        animProxy?.SetBool(isWalkingParam, walking);
    }

    private void OnDied()
    {
        state = State.Dead;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        SetWalking(false);
    }
}
