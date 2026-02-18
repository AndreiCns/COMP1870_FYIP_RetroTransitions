using UnityEngine;

public class EnvironmentMaterialSwapper : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Materials")]
    [SerializeField] private Material modernMaterial;
    [SerializeField] private Material retroMaterial;

    private Renderer rend;

    private void Awake()
    {
        // Cache once, no need to look it up every swap
        rend = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += ApplyStyle;
        else
            Debug.LogWarning($"[{name}] styleSwapEvent not assigned.", this);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= ApplyStyle;
    }

    private void ApplyStyle(StyleState state)
    {
        if (rend == null) return;

        // Swap between pre-made style materials
        rend.sharedMaterial = (state == StyleState.Modern)
            ? modernMaterial
            : retroMaterial;
    }
}
