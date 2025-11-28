using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Style Swap Event")]
public class StyleSwapEvent : ScriptableObject
{
    public UnityAction<StyleState> OnStyleSwap;

    public void Raise(StyleState newState)
    {
        OnStyleSwap?.Invoke(newState);
    }
}
