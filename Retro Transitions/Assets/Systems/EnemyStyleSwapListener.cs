using UnityEngine;

public class EnemyStyleSwapListener : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Visual Roots (children under Enemy_Ranged_Root)")]
    [SerializeField] private GameObject modernVisual;
    [SerializeField] private GameObject retroVisual;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs;

    private void OnEnable()
    {
        if (styleSwapEvent == null)
        {
            Debug.LogError($"[{name}] StyleSwapEvent is NULL. Assign the same GlobalStyleSwapEvent used by StyleSwapManager.");
            return;
        }

        styleSwapEvent.OnStyleSwap += OnStyleSwap;
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleSwap;
    }

    private void OnStyleSwap(StyleState state)
    {
        if (modernVisual == null || retroVisual == null)
        {
            Debug.LogError($"[{name}] Missing modern/retro visual refs. Assign them in the inspector.");
            return;
        }

        bool isModern = state == StyleState.Modern;

        // Only toggle if needed (avoids unnecessary enable/disable churn)
        if (modernVisual.activeSelf != isModern)
            modernVisual.SetActive(isModern);

        if (retroVisual.activeSelf != !isModern)
            retroVisual.SetActive(!isModern);

        if (verboseLogs)
            Debug.Log($"[{name}] Visual swap -> {state}");
    }
}
