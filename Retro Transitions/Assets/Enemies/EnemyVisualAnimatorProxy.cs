using UnityEngine;

public class EnemyVisualAnimatorProxy : MonoBehaviour, IGameplayPausable
{
    [Header("Animators")]
    [SerializeField] private Animator modernAnimator;
    [SerializeField] private Animator retroAnimator;

    [Header("Drive both")]
    [SerializeField] private bool driveBothAnimators = true;

    private GameplayPauseManager pauseManager;
    private float modernPrevSpeed = 1f;
    private float retroPrevSpeed = 1f;

    private void Awake()
    {
        // Cache once here rather than searching on every OnEnable
        pauseManager = Object.FindFirstObjectByType<GameplayPauseManager>();
    }

    private void OnEnable()
    {
        if (pauseManager != null)
            pauseManager.Register(this);
    }

    private void OnDisable()
    {
        if (pauseManager != null)
            pauseManager.Unregister(this);
    }

    public void SetStyleAnimators(Animator modern, Animator retro)
    {
        modernAnimator = modern;
        retroAnimator = retro;
    }

    public void SetPaused(bool isPaused)
    {
        if (isPaused)
        {
            if (modernAnimator != null) { modernPrevSpeed = modernAnimator.speed; modernAnimator.speed = 0f; }
            if (retroAnimator != null) { retroPrevSpeed = retroAnimator.speed; retroAnimator.speed = 0f; }
            return;
        }

        if (modernAnimator != null) modernAnimator.speed = modernPrevSpeed;
        if (retroAnimator != null) retroAnimator.speed = retroPrevSpeed;
    }

    public void SetBool(string param, bool value)
    {
        if (modernAnimator != null) modernAnimator.SetBool(param, value);
        if (driveBothAnimators && retroAnimator != null) retroAnimator.SetBool(param, value);
    }

    public void SetTrigger(string param)
    {
        if (modernAnimator != null) modernAnimator.SetTrigger(param);
        if (driveBothAnimators && retroAnimator != null) retroAnimator.SetTrigger(param);
    }

    public void ResetTrigger(string param)
    {
        if (modernAnimator != null) modernAnimator.ResetTrigger(param);
        if (driveBothAnimators && retroAnimator != null) retroAnimator.ResetTrigger(param);
    }
}