using UnityEngine;

public class WeaponStyleSwap : MonoBehaviour
{
    [Header("References")]
    public GameObject modernWeapon;
    public GameObject retroWeapon;

    [Header("Listen to")]
    public StyleSwapEvent styleSwapEvent;

    private Animator modernAnim;
    private Animator retroAnim;

    void Awake()
    {
        modernAnim = modernWeapon.GetComponentInChildren<Animator>();
        retroAnim = retroWeapon.GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        styleSwapEvent.OnStyleSwap += OnStyleChanged;
    }

    void OnDisable()
    {
        styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    void OnStyleChanged(StyleState newState)
    {
        bool isRetro = newState == StyleState.Retro;

        modernWeapon.SetActive(!isRetro);
        retroWeapon.SetActive(isRetro);
    }

    public void Fire()
    {
        if (retroWeapon.activeSelf)
            retroAnim?.SetTrigger("Fire");
        else
            modernAnim?.SetTrigger("Fire");
    }
}
