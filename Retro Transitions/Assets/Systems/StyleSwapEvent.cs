using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Style Swap Event")]
public class StyleSwapEvent : ScriptableObject
{
    // Global style swap event
    public event UnityAction<StyleState> OnStyleSwap;

    public void Raise(StyleState newState) => OnStyleSwap?.Invoke(newState);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        // Reserved in case static state is added later
    }

    private void OnDisable()
    {
        // Clear listeners on domain reload / exiting play mode
        OnStyleSwap = null;
    }
}
