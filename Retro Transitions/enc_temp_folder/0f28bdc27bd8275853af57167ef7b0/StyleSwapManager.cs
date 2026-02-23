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

    private StyleState currentState = StyleState.Modern;
    public StyleState CurrentState => currentState;

    private GameAudioManager gameAudio;

    private void Awake()
    {
        // Required for the system to function.
        if (styleSwapEvent == null)
        {
            Debug.LogError("[StyleSwapManager] styleSwapEvent is NULL.", this);
            enabled = false;
            return;
        }

        // Cache once; this is the central style brain.
        gameAudio = FindFirstObjectByType<GameAudioManager>();
    }

    private void OnEnable()
    {
        // Optional hotkey for fast iteration.
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed += OnTogglePressed;

        // Delay one frame so listeners have subscribed.
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
        RaiseStyle(currentState, "Initial");
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        ToggleStyle();
    }

    public void ToggleStyle()
    {
        currentState = (currentState == StyleState.Modern)
            ? StyleState.Retro
            : StyleState.Modern;

        RaiseStyle(currentState, "Toggle");
    }

    public void ForceStyle(StyleState state)
    {
        currentState = state;
        RaiseStyle(currentState, "Force");
    }

    private void RaiseStyle(StyleState state, string reason)
    {
        if (verboseLogs)
            Debug.Log($"[StyleSwapManager] {reason} -> {state}");

        // Broadcast visual state change.
        styleSwapEvent.Raise(state);

        // Keep audio presentation in sync with visual mode.
        if (gameAudio != null)
        {
            if (state == StyleState.Modern) gameAudio.PlayModern();
            else gameAudio.PlayRetro();
        }
    }
}
