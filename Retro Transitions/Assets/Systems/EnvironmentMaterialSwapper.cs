using UnityEngine;

public class EnvironmentMaterialSwapper : MonoBehaviour
{
    [Header("Event Reference")]
    public StyleSwapEvent styleSwapEvent;

    [Header("Materials")]
    public Material modernMaterial;
    public Material retroMaterial;

    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += ApplyStyle;
    }

    void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= ApplyStyle;
    }

    void ApplyStyle(StyleState state)
    {
        if (rend == null) return;

        rend.sharedMaterial = (state == StyleState.Modern)
            ? modernMaterial
            : retroMaterial;
    }
}
