using UnityEngine;

public class WeaponStyleSwap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject modernWeapon;
    [SerializeField] private GameObject retroWeapon;

    [Header("Listen to")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    private Animator modernAnim;
    private Animator retroAnim;

    // Cached state so we don’t check enum repeatedly.
    private bool isRetroActive;

    private static readonly int FireHash = Animator.StringToHash("Fire");

    private void Awake()
    {
        // Cache animators once – no runtime GetComponent calls.
        if (modernWeapon != null)
            modernAnim = modernWeapon.GetComponentInChildren<Animator>();

        if (retroWeapon != null)
            retroAnim = retroWeapon.GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void OnStyleChanged(StyleState newState)
    {
        // This class only handles visuals. No gameplay logic here.
        isRetroActive = newState == StyleState.Retro;

        if (modernWeapon != null)
            modernWeapon.SetActive(!isRetroActive);

        if (retroWeapon != null)
            retroWeapon.SetActive(isRetroActive);
    }

    public void Fire()
    {
        // Choose the currently visible weapon animator.
        Animator a = isRetroActive ? retroAnim : modernAnim;
        if (a == null) return;

        // Guard against accidental re-trigger spam while already in fire state.
        if (a.GetCurrentAnimatorStateInfo(0).IsName("shoot_001"))
            return;

        a.ResetTrigger(FireHash);
        a.SetTrigger(FireHash);
    }
}
