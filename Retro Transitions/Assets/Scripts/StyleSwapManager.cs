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
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed += OnTogglePressed;

        // Apply current style AFTER listeners have subscribed
        StartCoroutine(RaiseInitialStyleNextFrame());
    }

    private void OnDisable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed -= OnTogglePressed;
    }

    private IEnumerator RaiseInitialStyleNextFrame()
    {
        yield return null; // wait 1 frame so OnEnable subscriptions happen
        RaiseStyle(currentState, reason: "Initial");
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        ToggleStyle();
    }

    public void ToggleStyle()
    {
        currentState = (currentState == StyleState.Modern) ? StyleState.Retro : StyleState.Modern;
        RaiseStyle(currentState, reason: "Toggle");
    }

    public void ForceStyle(StyleState state)
    {
        currentState = state;
        RaiseStyle(currentState, reason: "Force");
    }

    private void RaiseStyle(StyleState state, string reason)
    {
        if (styleSwapEvent == null)
        {
            Debug.LogError("[StyleSwapManager] styleSwapEvent is NULL. Assign the same GlobalStyleSwapEvent asset used by listeners.");
            return;
        }

        if (verboseLogs)
            Debug.Log($"[StyleSwapManager] {reason} raise -> {state} | event='{styleSwapEvent.name}' id={styleSwapEvent.GetInstanceID()}");

        styleSwapEvent.Raise(state);

        // Optional: switch music / ambience based on state
        var audio = FindFirstObjectByType<GameAudioManager>();
        if (audio != null)
        {
            if (state == StyleState.Modern) audio.PlayModern();
            else audio.PlayRetro();
        }
    }
}
