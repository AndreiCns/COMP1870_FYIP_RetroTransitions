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

    private void OnStyleChanged(StyleState newState)
    {
        bool isRetro = newState == StyleState.Retro;

        if (modernWeapon != null)
            modernWeapon.SetActive(!isRetro);

        if (retroWeapon != null)
            retroWeapon.SetActive(isRetro);
    }

    public void Fire()
    {
        if (retroWeapon != null && retroWeapon.activeSelf)
            retroAnim?.SetTrigger("Fire");
        else
            modernAnim?.SetTrigger("Fire");
    }
}
