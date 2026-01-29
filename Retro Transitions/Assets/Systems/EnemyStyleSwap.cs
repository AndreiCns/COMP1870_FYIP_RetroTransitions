using UnityEngine;

public class EnemyStyleSwap : MonoBehaviour
{
    public StyleSwapEvent styleSwapEvent;

    public GameObject modernVisual; // 3D rig + animator
    public GameObject retroVisual;  // billboard sprite + sprite animator

    void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += Apply;
    }

    void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= Apply;
    }

    void Apply(StyleState state)
    {
        bool retro = state == StyleState.Retro;
        if (modernVisual) modernVisual.SetActive(!retro);
        if (retroVisual) retroVisual.SetActive(retro);
    }
}
