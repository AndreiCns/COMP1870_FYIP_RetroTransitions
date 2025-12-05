using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraRendererSwapper : MonoBehaviour
{
    public StyleSwapEvent styleSwapEvent;

    [Header("Renderer Indexes")]
    public int modernRendererIndex = 0;
    public int retroRendererIndex = 1;

    private UniversalAdditionalCameraData camData;

    void Awake()
    {
        camData = GetComponent<Camera>().GetUniversalAdditionalCameraData();
    }

    void OnEnable()
    {
        styleSwapEvent.OnStyleSwap += OnSwap;
    }

    void OnDisable()
    {
        styleSwapEvent.OnStyleSwap -= OnSwap;
    }

    void OnSwap(StyleState state)
    {
        if (state == StyleState.Modern)
        {
            camData.SetRenderer(modernRendererIndex);
            Debug.Log("[RendererSwap] Switched camera to MODERN renderer");
        }
        else
        {
            camData.SetRenderer(retroRendererIndex);
            Debug.Log("[RendererSwap] Switched camera to NTSC RETRO renderer");
        }
    }
}
