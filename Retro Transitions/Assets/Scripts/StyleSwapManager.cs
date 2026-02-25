using System.Collections;
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

    private StyleState currentState = StyleState.Modern;
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

        // Best: assign in inspector. Fallback: find.
        if (gameAudio == null)
            gameAudio = FindFirstObjectByType<GameAudioManager>();
    }

    private void OnEnable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed += OnTogglePressed;

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
        RequestStyleSwap("Toggle");
    }

    public void RequestStyleSwap(string reason)
    {
        if (isTransitioning)
            return;

        if (transitionFX != null && transitionFX.IsPlaying)
            return;

        StyleState target =
            (currentState == StyleState.Modern)
            ? StyleState.Retro
            : StyleState.Modern;

        if (transitionFX == null || reason == "Initial")
        {
            ApplyStyle(target, reason);
            return;
        }

        isTransitioning = true;
        pendingTarget = target;

        transitionFX.Play(
            onMidpoint: () => { ApplyStyle(pendingTarget, reason); },
            onComplete: () => { isTransitioning = false; }
        );
    }

    public void ForceStyle(StyleState target, string reason = "Force")
    {
        if (isTransitioning)
            return;

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

        if (gameAudio != null)
        {
            if (currentState == StyleState.Modern)
                gameAudio.PlayModern();
            else
                gameAudio.PlayRetro();
        }
    }
}