using UnityEngine;

public class EnemyStyleSwap : MonoBehaviour
{
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [SerializeField] private GameObject modernVisual; // 3D version
    [SerializeField] private GameObject retroVisual;  // Sprite/billboard version

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += Apply;
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= Apply;
    }

    private void Apply(StyleState state)
    {
        // Toggle which visual root is active
        bool retro = state == StyleState.Retro;

        if (modernVisual) modernVisual.SetActive(!retro);
        if (retroVisual) retroVisual.SetActive(retro);
    }
}
