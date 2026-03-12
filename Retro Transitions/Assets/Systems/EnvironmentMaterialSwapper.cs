using UnityEngine;

public class EnvironmentMaterialSwapper : MonoBehaviour
{
    [System.Serializable]
    private struct MaterialSet
    {
        [SerializeField] private Material modernMaterial;
        [SerializeField] private Material retroMaterial;

        public Material GetMaterial(StyleState state)
        {
            return state == StyleState.Modern ? modernMaterial : retroMaterial;
        }

        public bool IsAssigned()
        {
            return modernMaterial != null && retroMaterial != null;
        }
    }

    [Header("Event")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;
    [SerializeField] private StyleSwapManager styleSwapManager;

    [Header("Materials")]
    [SerializeField] private MaterialSet[] materialSlots;

    private Renderer rend;

    private void Awake()
    {
        // Cache once, no need to look it up every swap
        rend = GetComponent<Renderer>();

        if (rend == null)
        {
            Debug.LogError($"[{name}] Missing Renderer.", this);
            enabled = false;
            return;
        }

        if (styleSwapEvent == null)
        {
            Debug.LogError($"[{name}] styleSwapEvent not assigned.", this);
            enabled = false;
            return;
        }

        if (styleSwapManager == null)
        {
            Debug.LogWarning($"[{name}] styleSwapManager not assigned. Current state won't be applied on enable.", this);
        }
    }

    private void OnEnable()
    {
        styleSwapEvent.OnStyleSwap += ApplyStyle;

        if (styleSwapManager != null)
            ApplyStyle(styleSwapManager.CurrentState);
    }

    private void OnDisable()
    {
        styleSwapEvent.OnStyleSwap -= ApplyStyle;
    }

    private void ApplyStyle(StyleState state)
    {
        if (rend == null || materialSlots == null || materialSlots.Length == 0)
            return;

        Material[] sharedMats = rend.sharedMaterials;

        if (sharedMats.Length != materialSlots.Length)
        {
            Debug.LogWarning(
                $"[{name}] Material slot mismatch. Renderer has {sharedMats.Length}, swapper has {materialSlots.Length}.",
                this);
        }

        int count = Mathf.Min(sharedMats.Length, materialSlots.Length);

        for (int i = 0; i < count; i++)
        {
            if (!materialSlots[i].IsAssigned())
                continue;

            sharedMats[i] = materialSlots[i].GetMaterial(state);
        }

        rend.sharedMaterials = sharedMats;
    }
}