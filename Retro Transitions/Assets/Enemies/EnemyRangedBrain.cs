using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRangedBrain : MonoBehaviour
{
    private enum State { Idle, Chase, Dead }

    [Header("Core")]
    [SerializeField] private Transform target;
    [SerializeField] private Health health;
    [SerializeField] private EnemyVisualAnimatorProxy animProxy;

    [Header("Combat")]
    [SerializeField] private MonoBehaviour attackModuleBehaviour; // must implement IEnemyAttack
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
    [SerializeField] private string isShootingParam = "isShooting";

    private NavMeshAgent agent;
    private State state = State.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animProxy == null)
            animProxy = GetComponent<EnemyVisualAnimatorProxy>();

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
        if (target == null || attackModule == null) return;

        float dist = Vector3.Distance(transform.position, target.position);
        bool inAggro = dist <= aggroRange;
        bool hasLos = !requireLineOfSight || HasLineOfSight(target);

        switch (state)
        {
            case State.Idle:
                agent.isStopped = true;
                SetWalking(false);
                SetShooting(false);

                if (inAggro && hasLos)
                    state = State.Chase;
                break;

            case State.Chase:
                FaceTarget(target);

                float moveAt = stopDistance + distanceHysteresis;
                float stopAt = Mathf.Max(0f, stopDistance - distanceHysteresis);

                bool inAttackRange = attackModule.CanAttack(target);

                if (dist > moveAt)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = stopDistance;
                    agent.SetDestination(target.position);

                    SetWalking(true);
                    SetShooting(false);
                }
                else if (dist < stopAt)
                {
                    agent.isStopped = true;
                    agent.ResetPath();

                    SetWalking(false);
                }

                if (inAttackRange)
                {
                    agent.isStopped = true;
                    SetWalking(false);
                    SetShooting(true);

                    attackModule.TickAttack(target);
                }
                else
                {
                    SetShooting(false);
                }

                break;
        }
    }

    private bool HasLineOfSight(Transform t)
    {
        Vector3 origin = eyes != null ? eyes.position : transform.position + Vector3.up * eyeHeightFallback;
        Vector3 targetPos = t.position + Vector3.up * 1.1f;

        Vector3 dir = (targetPos - origin);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return true;

        dir /= dist;

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

    private void SetWalking(bool value)
    {
        animProxy?.SetBool(isWalkingParam, value);
    }

    private void SetShooting(bool value)
    {
        animProxy?.SetBool(isShootingParam, value);
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
        SetShooting(false);
    }
}
