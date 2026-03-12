using UnityEngine;
using UnityEngine.InputSystem;

public class StyleSwapManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Audio")]
    [Tooltip("Optional explicit reference. If not set, will try to find one in scene.")]
    [SerializeField] private GameAudioManager gameAudio;

    [Header("Debug Input (optional)")]
    [SerializeField] private InputActionReference toggleStyleAction;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    [Header("Transition FX (optional)")]
    [SerializeField] private StyleSwapTransitionFX transitionFX;

    [Header("Startup")]
    [SerializeField] private StyleState initialState = StyleState.Retro;

    private StyleState currentState;
    public StyleState CurrentState => currentState;

    private bool isTransitioning;
    private StyleState pendingTarget;

    private void Awake()
    {
        if (styleSwapEvent == null)
        {
            Debug.LogError("[StyleSwapManager] styleSwapEvent is NULL.", this);
            enabled = false;
            return;
        }

        if (gameAudio == null)
            gameAudio = FindFirstObjectByType<GameAudioManager>();

        currentState = initialState;

        // Apply immediately so listeners don't render one frame in the wrong style.
        ApplyStyle(currentState, "Initial");
    }

    private void OnEnable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed += OnTogglePressed;
    }

    private void OnDisable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed -= OnTogglePressed;
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        RequestStyleSwap("Toggle");
    }

    public void RequestStyleSwap(string reason)
    {
        StyleState target =
            currentState == StyleState.Modern
            ? StyleState.Retro
            : StyleState.Modern;

        StartStyleChange(target, reason);
    }

    public void ForceStyle(StyleState target, string reason = "Force")
    {
        if (currentState == target)
            return;

        StartStyleChange(target, reason);
    }

    private void StartStyleChange(StyleState target, string reason)
    {
        if (isTransitioning)
            return;

        if (transitionFX != null && transitionFX.IsPlaying)
            return;

        if (transitionFX == null || reason == "Initial")
        {
            ApplyStyle(target, reason);
            return;
        }

        isTransitioning = true;
        pendingTarget = target;

        transitionFX.Play(
            pendingTarget,
            onMidpoint: () => { ApplyStyle(pendingTarget, reason); },
            onComplete: () => { isTransitioning = false; }
        );
    }

    private void ApplyStyle(StyleState state, string reason)
    {
        currentState = state;

        if (verboseLogs)
            Debug.Log($"[StyleSwapManager] {reason} -> {currentState}");

        styleSwapEvent.Raise(currentState);

        if (gameAudio == null)
            return;

        if (currentState == StyleState.Modern)
            gameAudio.PlayModern();
        else
            gameAudio.PlayRetro();
    }
}