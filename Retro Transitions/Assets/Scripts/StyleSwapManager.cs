using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class StyleSwapManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Debug Input (optional)")]
    [SerializeField] private InputActionReference toggleStyleAction;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;

    [Header("Transition FX (optional)")]
    [SerializeField] private StyleSwapTransitionFX transitionFX;

    private StyleState currentState = StyleState.Modern;
    public StyleState CurrentState => currentState;

    private GameAudioManager gameAudio;

    private bool isTransitioning;
    private StyleState pendingTarget;

    private void Awake()
    {
        // Required for the system to function.
        if (styleSwapEvent == null)
        {
            Debug.LogError("[StyleSwapManager] styleSwapEvent is NULL.", this);
            enabled = false;
            return;
        }

        // Central style brain; audio reacts to state.
        gameAudio = FindFirstObjectByType<GameAudioManager>();
    }

    private void OnEnable()
    {
        // Optional hotkey for fast iteration.
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed += OnTogglePressed;

        // Delay one frame so listeners are ready.
        StartCoroutine(RaiseInitialStyleNextFrame());
    }

    private void OnDisable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed -= OnTogglePressed;
    }

    private IEnumerator RaiseInitialStyleNextFrame()
    {
        yield return null;
        ApplyStyle(currentState, "Initial");
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        // Debug-only toggle.
        RequestStyleSwap("Toggle");
    }

    public void RequestStyleSwap(string reason)
    {
        // Prevent re-entry while transition is playing.
        if (isTransitioning)
            return;

        if (transitionFX != null && transitionFX.IsPlaying)
            return;

        StyleState target =
            (currentState == StyleState.Modern)
            ? StyleState.Retro
            : StyleState.Modern;

        // No FX assigned - swap instantly.
        if (transitionFX == null || reason == "Initial")
        {
            ApplyStyle(target, reason);
            return;
        }

        isTransitioning = true;
        pendingTarget = target;

        transitionFX.Play(
            onMidpoint: () =>
            {
                // Commit state only at transition midpoint.
                ApplyStyle(pendingTarget, reason);
            },
            onComplete: () =>
            {
                isTransitioning = false;
            }
        );
    }

    public void ForceStyle(StyleState target, string reason = "Force")
    {
        // Ignore if already transitioning.
        if (isTransitioning)
            return;

        // Ignore redundant requests.
        if (currentState == target)
            return;

        if (transitionFX == null || reason == "Initial")
        {
            ApplyStyle(target, reason);
            return;
        }

        isTransitioning = true;
        pendingTarget = target;

        transitionFX.Play(
            onMidpoint: () =>
            {
                ApplyStyle(pendingTarget, reason);
            },
            onComplete: () =>
            {
                isTransitioning = false;
            }
        );
    }

    private void ApplyStyle(StyleState state, string reason)
    {
        currentState = state;

        if (verboseLogs)
            Debug.Log($"[StyleSwapManager] {reason} -> {currentState}");

        // Broadcast to visual/audio listeners.
        styleSwapEvent.Raise(currentState);

        // Audio mirrors visual state.
        if (gameAudio != null)
        {
            if (currentState == StyleState.Modern)
                gameAudio.PlayModern();
            else
                gameAudio.PlayRetro();
        }
    }
}