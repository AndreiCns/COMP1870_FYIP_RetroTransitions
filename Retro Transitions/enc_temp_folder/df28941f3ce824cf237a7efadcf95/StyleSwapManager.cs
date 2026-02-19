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

    private void OnEnable()
    {
        // Optional hotkey for quick testing
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed += OnTogglePressed;

        // Raise initial style after one frame so listeners are ready
        StartCoroutine(RaiseInitialStyleNextFrame());
    }

    private void OnDisable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed -= OnTogglePressed;
    }

    private IEnumerator RaiseInitialStyleNextFrame()
    {
        yield return null; // wait one frame
        RaiseStyle(currentState, "Initial");
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        ToggleStyle();
    }

    public void ToggleStyle()
    {
        currentState = (currentState == StyleState.Modern) ? StyleState.Retro : StyleState.Modern;
        RaiseStyle(currentState, "Toggle");
    }

    public void ForceStyle(StyleState state)
    {
        currentState = state;
        RaiseStyle(currentState, "Force");
    }

    private void RaiseStyle(StyleState state, string reason)
    {
        if (styleSwapEvent == null)
        {
            Debug.LogError("[StyleSwapManager] styleSwapEvent is NULL. Assign the shared StyleSwapEvent asset.");
            return;
        }

        if (verboseLogs)
            Debug.Log($"[StyleSwapManager] {reason} -> {state}");

        // Broadcast style change
        styleSwapEvent.Raise(state);

        // Sync audio snapshot with visual mode
        var audio = FindFirstObjectByType<GameAudioManager>();
        if (audio != null)
        {
            if (state == StyleState.Modern) audio.PlayModern();
            else audio.PlayRetro();
        }
    }
}
