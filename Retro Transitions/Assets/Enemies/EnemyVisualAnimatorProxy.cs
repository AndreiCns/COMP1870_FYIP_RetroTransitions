using UnityEngine;

public class EnemyVisualAnimatorProxy : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator modernAnimator;
    [SerializeField] private Animator retroAnimator;

    [Header("Drive both (recommended for style swapping)")]
    [SerializeField] private bool driveBothAnimators = true;

    public void SetStyleAnimators(Animator modern, Animator retro)
    {
        modernAnimator = modern;
        retroAnimator = retro;
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
            if (modernAnimator) modernAnimator.SetBool(param, value);
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
            if (modernAnimator) modernAnimator.SetTrigger(param);
        }
    }

    public void ResetTrigger(string param)
    {
        if (driveBothAnimators)
        {
            if (modernAnimator) modernAnimator.ResetTrigger(param);
            if (retroAnimator) retroAnimator.ResetTrigger(param);
        }
        else
        {
            if (modernAnimator) modernAnimator.ResetTrigger(param);
        }
    }
}
