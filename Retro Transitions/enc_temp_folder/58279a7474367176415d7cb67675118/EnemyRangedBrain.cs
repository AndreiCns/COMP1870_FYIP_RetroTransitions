using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyRangedBrain : MonoBehaviour
{
    private enum State { Patrol, Combat, Search, Dead }

    [Header("Core")]
    public EnemyPerception perception;
    public PatrolRoute patrolRoute;
    public Health health;

    [Header("Combat")]
    public MonoBehaviour attackModuleBehaviour; // must implement IEnemyAttack
    private IEnemyAttack attackModule;

    [Header("Movement")]
    public float patrolPointTolerance = 1.0f;
    public float searchDuration = 2.5f;
    public float combatRepathRate = 0.2f;
    public float strafeRadius = 4f;
    public float strafeInterval = 1.2f;

    private NavMeshAgent agent;
    private State state = State.Patrol;

    private int patrolIndex;
    private Vector3 lastSeenPos;
    private float searchTimer;
    private float repathTimer;
    private float strafeTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        attackModule = attackModuleBehaviour as IEnemyAttack;

        if (health != null)
            health.OnDied.AddListener(OnDied);
    }

    void Start()
    {
        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (state == State.Dead) return;

        bool sees = perception != null && perception.CanSeeTarget(out Vector3 seenPos);
        if (sees) lastSeenPos = seenPos;

        switch (state)
        {
            case State.Patrol:
                TickPatrol(sees);
                break;

            case State.Combat:
                TickCombat(sees);
                break;

            case State.Search:
                TickSearch(sees);
                break;
        }
    }

    // ----------------------
    // PATROL
    // ----------------------
    void TickPatrol(bool seesTarget)
    {
        if (seesTarget)
        {
            EnterCombat();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
            GoToNextPatrolPoint();
    }

    void GoToNextPatrolPoint()
    {
        if (patrolRoute == null || patrolRoute.points == null || patrolRoute.points.Length == 0) return;

        agent.isStopped = false;
        agent.SetDestination(patrolRoute.GetPoint(patrolIndex));
        patrolIndex = patrolRoute.NextIndex(patrolIndex);
    }

    // ----------------------
    // COMBAT
    // ----------------------
    void EnterCombat()
    {
        state = State.Combat;
        repathTimer = 0f;
        strafeTimer = 0f;
    }

    void TickCombat(bool seesTarget)
    {
        if (!seesTarget)
        {
            EnterSearch();
            return;
        }

        Transform target = perception.target;
        if (target == null) { EnterSearch(); return; }

        FaceTarget(target);

        // Maintain spacing + mild strafe so it feels “boomer shooter”
        strafeTimer -= Time.deltaTime;
        if (strafeTimer <= 0f)
        {
            Vector3 strafePos = PickStrafePosition(target.position);
            agent.isStopped = false;
            agent.SetDestination(strafePos);
            strafeTimer = strafeInterval;
        }

        // Only shoot when in range (and optionally when agent isn't turning too hard)
        if (attackModule != null && attackModule.CanAttack(target))
        {
            agent.isStopped = true; // stop to shoot (classic readable behaviour)
            attackModule.TickAttack(target);
        }
    }

    Vector3 PickStrafePosition(Vector3 around)
    {
        // Choose a point on a circle around target
        Vector2 r = Random.insideUnitCircle.normalized * strafeRadius;
        Vector3 desired = new Vector3(around.x + r.x, transform.position.y, around.z + r.y);

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    void FaceTarget(Transform target)
    {
        Vector3 flat = target.position - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f) return;

        Quaternion rot = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 12f);
    }

    // ----------------------
    // SEARCH (lost target)
    // ----------------------
    void EnterSearch()
    {
        state = State.Search;
        searchTimer = searchDuration;
        agent.isStopped = false;
        agent.SetDestination(lastSeenPos);
    }

    void TickSearch(bool seesTarget)
    {
        if (seesTarget)
        {
            EnterCombat();
            return;
        }

        searchTimer -= Time.deltaTime;

        // When reached last seen position, wait a moment, then return to patrol
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
    // DEATH
    // ----------------------
    void OnDied()
    {
        state = State.Dead;
        agent.isStopped = true;
        // Minimal: disable colliders/AI. You can trigger ragdoll/anim here.
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }
}
