using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraRendererSwapper : MonoBehaviour
{
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Renderer Indexes")]
    // Must match the renderer order in the URP asset
    [SerializeField] private int modernRendererIndex = 0;
    [SerializeField] private int retroRendererIndex = 1;

    private UniversalAdditionalCameraData camData;

    void Awake()
    {
        var cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError($"[{name}] CameraRendererSwapper: No Camera component found.", this);
            enabled = false;
            return;
        }

        camData = cam.GetUniversalAdditionalCameraData();
    }

    void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnSwap;
        else
            Debug.LogWarning($"[{name}] CameraRendererSwapper: styleSwapEvent not assigned.", this);
    }

    void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnSwap;
    }

    void OnSwap(StyleState state)
    {
        // Swap between modern and retro render pipelines
        if (state == StyleState.Modern)
        {
            camData.SetRenderer(modernRendererIndex);
            Debug.Log("[RendererSwap] Camera -> Modern");
        }
        else
        {
            camData.SetRenderer(retroRendererIndex);
            Debug.Log("[RendererSwap] Camera -> Retro");
        }
    }
}
