using UnityEngine;

public class EnemyVisualAnimatorProxy : MonoBehaviour
{
    [Header("Visual Animators")]
    [SerializeField] private Animator modernAnimator;
    [SerializeField] private Animator retroAnimator;

    [Header("Optional: keep both animators in sync even when one is hidden")]
    [SerializeField] private bool driveBothAnimators = false;

    private Animator ActiveAnimator
    {
        get
        {
            if (modernAnimator != null && modernAnimator.gameObject.activeInHierarchy) return modernAnimator;
            if (retroAnimator != null && retroAnimator.gameObject.activeInHierarchy) return retroAnimator;
            return modernAnimator != null ? modernAnimator : retroAnimator;
        }
    }

    public void SetBool(string param, bool value)
    {
        if (driveBothAnimators)
        {
            if (modernAnimator) modernAnimator.SetBool(param, value);
            if (retroAnimator) retroAnimator.SetBool(param, value);
        }
        else
        {
            var a = ActiveAnimator;
            if (a) a.SetBool(param, value);
        }
    }

    public void SetTrigger(string param)
    {
        if (driveBothAnimators)
        {
            if (modernAnimator) modernAnimator.SetTrigger(param);
            if (retroAnimator) retroAnimator.SetTrigger(param);
        }
        else
        {
            var a = ActiveAnimator;
            if (a) a.SetTrigger(param);
        }
    }

    public void Play(string stateName, int layer = 0, float normalizedTime = 0f)
    {
        if (driveBothAnimators)
        {
            if (modernAnimator) modernAnimator.Play(stateName, layer, normalizedTime);
            if (retroAnimator) retroAnimator.Play(stateName, layer, normalizedTime);
        }
        else
        {
            var a = ActiveAnimator;
            if (a) a.Play(stateName, layer, normalizedTime);
        }
    }

    public void RebindAndUpdate()
    {
        // Useful if you see a 1-frame “wrong pose” after enabling a visual
        if (modernAnimator) { modernAnimator.Rebind(); modernAnimator.Update(0f); }
        if (retroAnimator) { retroAnimator.Rebind(); retroAnimator.Update(0f); }
    }
}
