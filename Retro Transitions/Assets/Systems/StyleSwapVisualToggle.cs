using UnityEngine;

public class StyleSwapVisualToggle : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject modernVisual;
    [SerializeField] private GameObject retroVisual;

    [Header("Listen To")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    private StyleSwapManager styleSwapManager;

    private void Awake()
    {
        if (modernVisual == null || retroVisual == null)
        {
            Debug.LogError("[StyleSwapVisualToggle] Missing visuals.", this);
            enabled = false;
            return;
        }

        // Used for sync when objects spawn after initial broadcast.
        styleSwapManager = FindFirstObjectByType<StyleSwapManager>();

        // Safe default before we sync.
        modernVisual.SetActive(true);
        retroVisual.SetActive(false);
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;

        // Sync immediately in case we're enabled after the initial event.
        if (styleSwapManager != null)
            OnStyleChanged(styleSwapManager.CurrentState);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void OnStyleChanged(StyleState newState)
    {
        bool isRetro = newState == StyleState.Retro;

        // Visual swap only.
        modernVisual.SetActive(!isRetro);
        retroVisual.SetActive(isRetro);
    }
}
