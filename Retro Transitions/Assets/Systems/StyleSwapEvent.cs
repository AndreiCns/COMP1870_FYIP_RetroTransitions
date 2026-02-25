using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Style Swap Event")]
public class StyleSwapEvent : ScriptableObject
{
    public event UnityAction<StyleState> OnStyleSwap;

    // Stores the last applied style globally
    public StyleState LastState { get; private set; } = StyleState.Modern;

    public void Raise(StyleState newState)
    {
        LastState = newState;
        OnStyleSwap?.Invoke(newState);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        // Reserved for future static state
    }

    private void OnDisable()
    {
        OnStyleSwap = null;
    }
}