using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRangedBrain : MonoBehaviour
{
    // Tiny FSM: wait -> engage -> stop completely on death
    private enum State { Idle, Chase, Dead }

    [Header("Core")]
    [SerializeField] private Transform target;
    [SerializeField] private Health health;
    [SerializeField] private EnemyVisualAnimatorProxy animProxy;

    [Header("Combat")]
    // Script on this enemy that actually handles cooldown + projectile spawning
    [SerializeField] private MonoBehaviour attackModuleBehaviour;
    private IEnemyAttack attackModule;

    [Header("Ranges")]
    [SerializeField] private float aggroRange = 25f;
    [SerializeField] private float stopDistance = 12f;
    [SerializeField] private float distanceHysteresis = 1.5f;

    [Header("Line of Sight (optional)")]
    [SerializeField] private bool requireLineOfSight = false;
    [SerializeField] private Transform eyes;
    [SerializeField] private float eyeHeightFallback = 1.6f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    [Header("Animation Parameters")]
    [SerializeField] private string isWalkingParam = "isWalking";

    private NavMeshAgent agent;
    private State state = State.Idle;

    private float aggroRangeSqr;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // Keeps animation calls consistent across modern/retro visuals
        if (animProxy == null)
            animProxy = GetComponent<EnemyVisualAnimatorProxy>();

        attackModule = attackModuleBehaviour as IEnemyAttack;
        if (attackModule == null)
            Debug.LogError($"[{name}] EnemyRangedBrain: attackModuleBehaviour missing or not implementing IEnemyAttack.", this);

        if (health != null)
            health.OnDied.AddListener(OnDied);

        aggroRangeSqr = aggroRange * aggroRange;

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
        if (target == null || attackModule == null) return;

        Vector3 delta = target.position - transform.position;
        float distSqr = delta.sqrMagnitude;

        bool inAggro = distSqr <= aggroRangeSqr;
        bool hasLos = !requireLineOfSight || HasLineOfSight(target);

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                SetWalking(false);

                if (inAggro && hasLos)
                    state = State.Chase;

                break;

            case State.Chase:
                FaceTarget(target);

                // Hysteresis avoids jitter around the stop distance
                float moveAt = stopDistance + distanceHysteresis;
                float stopAt = Mathf.Max(0f, stopDistance - distanceHysteresis);

                bool inAttackRange = attackModule.CanAttack(target);

                if (distSqr > moveAt * moveAt)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = stopDistance;
                    agent.SetDestination(target.position);

                    SetWalking(true);
                }
                else if (distSqr < stopAt * stopAt)
                {
                    agent.isStopped = true;
                    agent.ResetPath();

                    SetWalking(false);
                }

                if (inAttackRange)
                {
                    // Stop and let the attack module handle timing + anim events
                    agent.isStopped = true;
                    SetWalking(false);

                    attackModule.TickAttack(target);
                }

                break;
        }
    }

    private bool HasLineOfSight(Transform t)
    {
        // Optional eye point; otherwise use a simple height offset
        Vector3 origin = eyes != null ? eyes.position : transform.position + Vector3.up * eyeHeightFallback;
        Vector3 targetPos = t.position + Vector3.up * 1.1f;

        Vector3 dir = targetPos - origin;
        float dist = dir.magnitude;

        if (dist <= 0.001f) return true;

        dir /= dist;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, lineOfSightMask, QueryTriggerInteraction.Ignore))
            return hit.transform == t || hit.transform.root == t.root;

        return false;
    }

    private void FaceTarget(Transform t)
    {
        // Keep rotation flat (especially important for billboards)
        Vector3 flat = t.position - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 12f);
    }

    private void SetWalking(bool value)
    {
        animProxy?.SetBool(isWalkingParam, value);
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
