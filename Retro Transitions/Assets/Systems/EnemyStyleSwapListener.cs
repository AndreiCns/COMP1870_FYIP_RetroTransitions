using UnityEngine;

public class EnemyStyleSwapListener : MonoBehaviour
{
    [Header("Event")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Visual Roots (keep BOTH active)")]
    [SerializeField] private GameObject modernVisual;
    [SerializeField] private GameObject retroVisual;

    [Header("Animators (must stay enabled)")]
    [SerializeField] private Animator modernAnimator;
    [SerializeField] private Animator retroAnimator;

    [Header("Optional")]
    [SerializeField] private bool syncAnimatorOnSwap = true;
    [SerializeField] private bool autoCollectRenderers = true;

    private Renderer[] modernRenderers;
    private Renderer[] retroRenderers;

    private void Awake()
    {
        if (autoCollectRenderers)
        {
            if (modernVisual) modernRenderers = modernVisual.GetComponentsInChildren<Renderer>(true);
            if (retroVisual) retroRenderers = retroVisual.GetComponentsInChildren<Renderer>(true);
        }

        // Safety: keep animators updating even if hidden
        if (modernAnimator) modernAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (retroAnimator) retroAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleSwap;
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleSwap;
    }

    private void OnStyleSwap(StyleState state)
    {
        bool toModern = state == StyleState.Modern;

        // Determine which animator is currently visible vs target
        Animator fromA = toModern ? retroAnimator : modernAnimator;
        Animator toA = toModern ? modernAnimator : retroAnimator;

        if (syncAnimatorOnSwap && fromA != null && toA != null)
            SyncAnimatorState(fromA, toA);

        // Swap visibility (NOT GameObject active state)
        SetRenderersEnabled(modernRenderers, toModern);
        SetRenderersEnabled(retroRenderers, !toModern);
    }

    private void SetRenderersEnabled(Renderer[] rends, bool enabled)
    {
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++)
            if (rends[i]) rends[i].enabled = enabled;
    }

    private void SyncAnimatorState(Animator fromA, Animator toA)
    {
        // Assumes same layer structure (layer 0 at least)
        var st = fromA.GetCurrentAnimatorStateInfo(0);

        // Jump target animator to same state and time
        toA.Play(st.fullPathHash, 0, st.normalizedTime);
        toA.Update(0f);

        // Copy speed too (important if you slow death / hit reactions)
        toA.speed = fromA.speed;
    }
}
