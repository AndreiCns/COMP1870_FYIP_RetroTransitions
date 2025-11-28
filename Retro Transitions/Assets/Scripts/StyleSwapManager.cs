using UnityEngine;
using UnityEngine.InputSystem;

public class StyleSwapManager : MonoBehaviour
{
    [Header("Config")]
    public StyleSwapEvent styleSwapEvent;

    [Header("Debug Input (optional)")]
    public InputActionReference toggleStyleAction;

    private StyleState currentState = StyleState.Modern;

    void OnEnable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed += OnTogglePressed;
    }

    void OnDisable()
    {
        if (toggleStyleAction != null)
            toggleStyleAction.action.performed -= OnTogglePressed;
    }

    void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        ToggleStyle();
    }

    public void ToggleStyle()
    {
        currentState = (currentState == StyleState.Modern) ? StyleState.Retro : StyleState.Modern;
        Debug.Log($"Style toggled to: {currentState}"); // ADD THIS LINE
        styleSwapEvent?.Raise(currentState);
    }

    public void ForceStyle(StyleState state)
    {
        currentState = state;
        styleSwapEvent?.Raise(currentState);
    }
}
