using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRangedBrain : MonoBehaviour
{
    private enum State { Patrol, Combat, Search, Dead }

    [Header("Core")]
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private PatrolRoute patrolRoute;
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;

    [Header("Combat")]
    [Tooltip("Must implement IEnemyAttack (e.g., RangedAttackModule).")]
    [SerializeField] private MonoBehaviour attackModuleBehaviour; // must implement IEnemyAttack
    private IEnemyAttack attackModule;

    [Header("Movement")]
    [SerializeField] private float patrolPointTolerance = 1.0f;
    [SerializeField] private float searchDuration = 2.5f;
    [SerializeField] private float combatRepathRate = 0.2f;
    [SerializeField] private float strafeRadius = 4f;
    [SerializeField] private float strafeInterval = 1.2f;

    [Header("Animation Params")]
    [SerializeField] private string isWalkingParam = "isWalking";

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private NavMeshAgent agent;
    private State state = State.Patrol;

    private int patrolIndex;
    private Vector3 lastSeenPos;
    private float searchTimer;
    private float repathTimer;
    private float strafeTimer;

    // Helps stop spammy SetDestination calls when already going there
    private Vector3 lastDestination;
    private bool hasLastDestination;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        attackModule = attackModuleBehaviour as IEnemyAttack;
        if (attackModuleBehaviour != null && attackModule == null && logWarnings)
        {
            Debug.LogWarning($"{name}: Attack Module is set but does not implement IEnemyAttack.", this);
        }

        if (health != null)
            health.OnDied.AddListener(OnDied);

        // If perception has a target already, initialise lastSeenPos defensively
        if (perception != null && perception.target != null)
            lastSeenPos = perception.target.position;
        else
            lastSeenPos = transform.position;
    }

    private void Start()
    {
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (state == State.Dead) return;

        bool seesTarget = TryUpdateLastSeen(out Transform target);

        switch (state)
        {
            case State.Patrol:
                TickPatrol(seesTarget);
                break;

            case State.Combat:
                TickCombat(seesTarget, target);
                break;

            case State.Search:
                TickSearch(seesTarget);
                break;
        }

        UpdateLocomotionAnim();
    }

    // ----------------------
    // PERCEPTION
    // ----------------------
    private bool TryUpdateLastSeen(out Transform target)
    {
        target = null;

        if (perception == null)
            return false;

        if (perception.target == null)
            return false;

        target = perception.target;

        // Safer pattern: always provide a fallback pos
        Vector3 seenPos = lastSeenPos;
        bool sees = perception.CanSeeTarget(out seenPos);

        if (sees)
            lastSeenPos = seenPos;

        return sees;
    }

    // ----------------------
    // PATROL
    // ----------------------
    private void TickPatrol(bool seesTarget)
    {
        if (seesTarget)
        {
            EnterCombat();
            return;
        }

        if (agent == null) return;

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
            GoToNextPatrolPoint();
    }

    private void GoToNextPatrolPoint()
    {
        if (agent == null) return;
        if (patrolRoute == null || patrolRoute.points == null || patrolRoute.points.Length == 0) return;

        agent.isStopped = false;

        Vector3 dest = patrolRoute.GetPoint(patrolIndex);
        SetDestinationSafe(dest);

        patrolIndex = patrolRoute.NextIndex(patrolIndex);
    }

    // ----------------------
    // COMBAT
    // ----------------------
    private void EnterCombat()
    {
        state = State.Combat;
        repathTimer = 0f;
        strafeTimer = 0f;
    }

    private void TickCombat(bool seesTarget, Transform target)
    {
        if (!seesTarget || target == null)
        {
            EnterSearch();
            return;
        }

        FaceTarget(target);

        // Throttle decision updates (even if not used yet, this avoids future spam)
        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
            repathTimer = combatRepathRate;

        // Strafe movement cadence
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            Vector3 strafePos = PickStrafePosition(target.position);

            if (agent != null)
            {
                agent.isStopped = false;
                SetDestinationSafe(strafePos);
            }

            strafeTimer = strafeInterval;
        }

        // Shooting
        if (attackModule == null)
        {
            if (attackModuleBehaviour != null && logWarnings)
                Debug.LogWarning($"{name}: attackModuleBehaviour assigned but attackModule is null (interface cast failed).", this);
            return;
        }

        if (attackModule.CanAttack(target))
        {
            if (agent != null)
                agent.isStopped = true; // stop to shoot for readability

            // IMPORTANT: If you're using Animation Events to spawn projectiles,
            // TickAttack should only trigger the shoot animation / set target.
            attackModule.TickAttack(target);
        }
        else
        {
            // If cannot attack (out of range / cooldown), keep moving
            if (agent != null)
                agent.isStopped = false;
        }
    }

    private Vector3 PickStrafePosition(Vector3 around)
    {
        Vector2 r = Random.insideUnitCircle.normalized * strafeRadius;
        Vector3 desired = new Vector3(around.x + r.x, transform.position.y, around.z + r.y);

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    private void FaceTarget(Transform target)
    {
        if (target == null) return;

        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 12f);
    }

    // ----------------------
    // SEARCH
    // ----------------------
    private void EnterSearch()
    {
        state = State.Search;
        searchTimer = searchDuration;

        if (agent == null) return;

        agent.isStopped = false;
        SetDestinationSafe(lastSeenPos);
    }

    private void TickSearch(bool seesTarget)
    {
        if (seesTarget)
        {
            EnterCombat();
            return;
        }

        searchTimer -= Time.deltaTime;

        if (agent == null) return;

        // When reached last seen position, wait, then return to patrol
        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
        {
            if (searchTimer <= 0f)
            {
                state = State.Patrol;
                GoToNextPatrolPoint();
            }
        }
    }

    // ----------------------
    // ANIMATION
    // ----------------------
    private void UpdateLocomotionAnim()
    {
        if (animator == null || agent == null) return;

        bool moving = !agent.isStopped && agent.velocity.sqrMagnitude > 0.05f;
        animator.SetBool(isWalkingParam, moving);
    }

    // ----------------------
    // NAV DESTINATION HELPER
    // ----------------------
    private void SetDestinationSafe(Vector3 dest)
    {
        if (agent == null) return;

        // Avoid spamming SetDestination with basically the same value
        if (hasLastDestination && (dest - lastDestination).sqrMagnitude < 0.01f)
            return;

        agent.SetDestination(dest);
        lastDestination = dest;
        hasLastDestination = true;
    }

    // ----------------------
    // DEATH
    // ----------------------
    private void OnDied()
    {
        state = State.Dead;

        if (agent != null)
            agent.isStopped = true;

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }
}
