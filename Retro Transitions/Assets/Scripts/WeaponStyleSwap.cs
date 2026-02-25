using System;
using UnityEngine;

public class WeaponStyleSwap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject modernWeapon;
    [SerializeField] private GameObject retroWeapon;

    [Header("Listen To")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    // Raised after SetActive calls are done so other listeners (e.g.
    // PlayerCombatController) can safely check isActiveAndEnabled.
    public event Action<StyleState> OnWeaponsSwapped;

    private Animator modernAnim;
    private Animator retroAnim;
    private bool isRetroActive;

    private static readonly int FireHash = Animator.StringToHash("Fire");

   

    private void Awake()
    {
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

    

    public void Fire()
    {
        Animator a = isRetroActive ? retroAnim : modernAnim;
        if (a == null) return;

        if (a.GetCurrentAnimatorStateInfo(0).IsName("shoot_001"))
            return;

        a.ResetTrigger(FireHash);
        a.SetTrigger(FireHash);
    }

    

    private void OnStyleChanged(StyleState newState)
    {
        isRetroActive = newState == StyleState.Retro;

        // Activate/deactivate first...
        if (modernWeapon != null) modernWeapon.SetActive(!isRetroActive);
        if (retroWeapon != null) retroWeapon.SetActive(isRetroActive);

        // ...then notify dependents that GameObjects are in their final state.
        OnWeaponsSwapped?.Invoke(newState);
    }
}