using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Style Swap Event")]
public class StyleSwapEvent : ScriptableObject
{
    public UnityAction<StyleState> OnStyleSwap;

    public void Raise(StyleState newState) => OnStyleSwap?.Invoke(newState);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        // Nothing here per-instance, but keeps the pattern in mind.
        // If you later store static events, reset them here.
    }

    private void OnDisable()
    {
        // Editor safety: clear listeners when domain reload / stop play
        OnStyleSwap = null;
    }
}
