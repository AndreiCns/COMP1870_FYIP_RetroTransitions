using UnityEngine;

public class StyleSwapVisualToggle : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject modernVisual;
    [SerializeField] private GameObject retroVisual;

    [Header("Listen To")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    private void Awake()
    {
        if (modernVisual == null || retroVisual == null)
        {
            Debug.LogError("[StyleSwapVisualToggle] Missing visuals.", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (styleSwapEvent == null)
            return;

        styleSwapEvent.OnStyleSwap += OnStyleChanged;

        // Immediate sync using stored state
        OnStyleChanged(styleSwapEvent.LastState);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void OnStyleChanged(StyleState newState)
    {
        bool isRetro = newState == StyleState.Retro;

        modernVisual.SetActive(!isRetro);
        retroVisual.SetActive(isRetro);
    }
}