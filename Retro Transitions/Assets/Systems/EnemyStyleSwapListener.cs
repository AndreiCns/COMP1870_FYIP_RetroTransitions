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

    [Header("Renderer swap")]
    [SerializeField] private bool autoCollectRenderers = true;

    [Header("Sync (SAFE)")]
    [Tooltip("Copies parameter values (bool/float/int) from currently visible animator to the other.")]
    [SerializeField] private bool syncParametersOnSwap = true;

    [Tooltip("Only enable if BOTH controllers have identical state names/structure. Not recommended for rig vs sprite.")]
    [SerializeField] private bool syncStateTimeIfPossible = false;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private Renderer[] modernRenderers;
    private Renderer[] retroRenderers;

    [SerializeField] private Transform muzzleModern;
    [SerializeField] private Transform muzzleRetro;
    [SerializeField] private RangedAttackModule attack;

    private void Awake()
    {
        if (autoCollectRenderers)
        {
            if (modernVisual) modernRenderers = modernVisual.GetComponentsInChildren<Renderer>(true);
            if (retroVisual) retroRenderers = retroVisual.GetComponentsInChildren<Renderer>(true);
        }

        // Keep both animators ticking even if hidden (critical!)
        if (modernAnimator) modernAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (retroAnimator) retroAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleSwap;
        else
            Debug.LogWarning($"[{name}] styleSwapEvent not assigned.", this);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleSwap;
    }

    private void OnStyleSwap(StyleState state)
    {

        bool toModern = state == StyleState.Modern;

        if (attack != null)
            attack.SetMuzzle(toModern ? muzzleModern : muzzleRetro);


        Animator fromA = toModern ? retroAnimator : modernAnimator; // currently visible
        Animator toA = toModern ? modernAnimator : retroAnimator; // becoming visible

        if (syncParametersOnSwap && fromA != null && toA != null)
            CopyParameters(fromA, toA);

        if (syncStateTimeIfPossible && fromA != null && toA != null)
            TrySyncStateTime(fromA, toA);

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
        // Copy speed too
        toA.speed = fromA.speed;

        foreach (var p in fromA.parameters)
        {
            // Only copy params that exist on the target animator (avoids warnings)
            if (!HasParam(toA, p.nameHash)) continue;

            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool:
                    toA.SetBool(p.nameHash, fromA.GetBool(p.nameHash));
                    break;

                case AnimatorControllerParameterType.Float:
                    toA.SetFloat(p.nameHash, fromA.GetFloat(p.nameHash));
                    break;

                case AnimatorControllerParameterType.Int:
                    toA.SetInteger(p.nameHash, fromA.GetInteger(p.nameHash));
                    break;

                case AnimatorControllerParameterType.Trigger:
                    // Triggers can’t be "read". Don’t try to copy.
                    // Use bools (like your isShooting) for cross-style sync.
                    break;
            }
        }

        // Force immediate evaluation so the first visible frame is correct
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
        // Only safe if both controllers share the same state names/paths.
        var st = fromA.GetCurrentAnimatorStateInfo(0);
        // If target doesn't have this state, Play will log warnings - so bail.
        // Unity doesn't give a direct "HasState" here without AnimatorController access, so keep this OFF for your setup.
        toA.Play(st.fullPathHash, 0, st.normalizedTime);
        toA.Update(0f);
    }
}
