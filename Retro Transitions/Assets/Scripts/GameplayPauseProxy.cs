using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class GameplayPauseProxy : MonoBehaviour, IGameplayPausable
{
    [Header("Disable these behaviours")]
    [SerializeField] private MonoBehaviour[] disableBehaviours;

    [Header("Optional")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private NavMeshAgent navAgent;

    private GameplayPauseManager pauseManager;

    private bool isPaused;
    private bool wasAgentStopped;

    private void Awake()
    {
        // Auto-cache for common roots (Player / Enemy).
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (navAgent == null)
            navAgent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        pauseManager = FindFirstObjectByType<GameplayPauseManager>();
        if (pauseManager != null)
            pauseManager.Register(this);
    }

    private void OnDisable()
    {
        if (pauseManager != null)
            pauseManager.Unregister(this);
    }

    public void SetPaused(bool paused)
    {
        // Avoid churn if multiple callers spam pause.
        if (isPaused == paused)
            return;

        isPaused = paused;

        if (playerInput != null)
            playerInput.enabled = !paused;

        if (navAgent != null)
        {
            if (paused)
            {
                wasAgentStopped = navAgent.isStopped;
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
                navAgent.ResetPath();
            }
            else
            {
                navAgent.isStopped = wasAgentStopped;
            }
        }

        if (disableBehaviours == null)
            return;

        for (int i = 0; i < disableBehaviours.Length; i++)
        {
            MonoBehaviour b = disableBehaviours[i];
            if (b == null)
                continue;

            b.enabled = !paused;
        }
    }
}