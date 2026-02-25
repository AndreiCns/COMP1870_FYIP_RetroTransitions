using UnityEngine;

public class EnemyStyleSwapListener : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;
    [SerializeField] private StyleSwapManager styleSwapManager;

    [Header("Visual Roots (keep BOTH active)")]
    [SerializeField] private GameObject modernVisual;
    [SerializeField] private GameObject retroVisual;

    [Header("Animators (must stay enabled)")]
    [SerializeField] private Animator modernAnimator;
    [SerializeField] private Animator retroAnimator;

    [Header("Renderer Swap")]
    [SerializeField] private bool autoCollectRenderers = true;

    [Header("Sync")]
    [Tooltip("Copies bool/float/int parameters across when swapping.")]
    [SerializeField] private bool syncParametersOnSwap = true;
    [Tooltip("Only use if both controllers match 1:1.")]
    [SerializeField] private bool syncStateTimeIfPossible = false;

    [Header("Ranged Attack Muzzle Swap")]
    [SerializeField] private Transform muzzleModern;
    [SerializeField] private Transform muzzleRetro;
    [SerializeField] private RangedAttackModule attack;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private Renderer[] modernRenderers;
    private Renderer[] retroRenderers;

    private void Awake()
    {
        if (autoCollectRenderers)
        {
            if (modernVisual) modernRenderers = modernVisual.GetComponentsInChildren<Renderer>(true);
            if (retroVisual) retroRenderers = retroVisual.GetComponentsInChildren<Renderer>(true);
        }

        if (modernAnimator) modernAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (retroAnimator) retroAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (styleSwapManager == null)
            styleSwapManager = Object.FindFirstObjectByType<StyleSwapManager>();
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleSwap;
        else
            Debug.LogWarning($"[{name}] styleSwapEvent not assigned.", this);

        // Sync on enable so enemies spawning mid-game start in the correct style
        if (styleSwapManager != null)
            OnStyleSwap(styleSwapManager.CurrentState);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleSwap;
    }

    private void OnStyleSwap(StyleState state)
    {
        bool toModern = state == StyleState.Modern;

        Transform selectedMuzzle = toModern ? muzzleModern : muzzleRetro;
        if (attack != null && selectedMuzzle != null)
            attack.SetMuzzle(selectedMuzzle);
        else if (verboseLogs && attack != null && selectedMuzzle == null)
            Debug.LogWarning($"[{name}] Selected muzzle is null for state {state}.", this);

        Animator fromA = toModern ? retroAnimator : modernAnimator;
        Animator toA = toModern ? modernAnimator : retroAnimator;

        if (syncParametersOnSwap && fromA != null && toA != null) CopyParameters(fromA, toA);
        if (syncStateTimeIfPossible && fromA != null && toA != null) TrySyncStateTime(fromA, toA);

        SetRenderersEnabled(modernRenderers, toModern);
        SetRenderersEnabled(retroRenderers, !toModern);

        if (verboseLogs)
            Debug.Log($"[{name}] Visual swap -> {state}", this);
    }

    private void SetRenderersEnabled(Renderer[] rends, bool enabled)
    {
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++)
            if (rends[i]) rends[i].enabled = enabled;
    }

    private void CopyParameters(Animator fromA, Animator toA)
    {
        toA.speed = fromA.speed;

        foreach (var p in fromA.parameters)
        {
            if (!HasParam(toA, p.nameHash)) continue;

            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool:
                    toA.SetBool(p.nameHash, fromA.GetBool(p.nameHash)); break;
                case AnimatorControllerParameterType.Float:
                    toA.SetFloat(p.nameHash, fromA.GetFloat(p.nameHash)); break;
                case AnimatorControllerParameterType.Int:
                    toA.SetInteger(p.nameHash, fromA.GetInteger(p.nameHash)); break;
                    // Triggers skipped intentionally - use bools for cross-style sync
            }
        }

        toA.Update(0f);
    }

    private bool HasParam(Animator a, int nameHash)
    {
        foreach (var p in a.parameters)
            if (p.nameHash == nameHash) return true;
        return false;
    }

    private void TrySyncStateTime(Animator fromA, Animator toA)
    {
        var st = fromA.GetCurrentAnimatorStateInfo(0);
        toA.Play(st.fullPathHash, 0, st.normalizedTime);
        toA.Update(0f);
    }
}