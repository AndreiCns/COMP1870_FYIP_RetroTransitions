using UnityEngine;

public class WeaponCooldownSmoke : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Smoke Roots")]
    [SerializeField] private GameObject modernSmokeRoot;
    [SerializeField] private GameObject retroSmokeRoot;

    [Header("Tuning")]
    [SerializeField] private bool disableWhenNotCooling = true;

    private StyleState currentStyle = StyleState.Modern;

    private void Awake()
    {
        if (combatController == null)
            combatController = GetComponentInParent<PlayerCombatController>();

        if (combatController == null)
        {
            Debug.LogError($"{name}: Missing PlayerCombatController reference.", this);
            enabled = false;
            return;
        }

        SetSmokeActive(false, false);
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

        SetSmokeActive(false, false);
    }

    private void Update()
    {
        bool cooling = combatController.IsOnCooldown;

        if (!cooling && disableWhenNotCooling)
        {
            SetSmokeActive(false, false);
            return;
        }

        // Only one smoke variant should ever be active.
        bool wantModern = cooling && currentStyle == StyleState.Modern;
        bool wantRetro = cooling && currentStyle == StyleState.Retro;

        SetSmokeActive(wantModern, wantRetro);
    }

    private void OnStyleChanged(StyleState newState)
    {
        currentStyle = newState;

        // Prevent 1-frame overlap when swapping mid-cooldown.
        SetSmokeActive(false, false);
    }

    private void SetSmokeActive(bool modernActive, bool retroActive)
    {
        if (modernSmokeRoot != null && modernSmokeRoot.activeSelf != modernActive)
            modernSmokeRoot.SetActive(modernActive);

        if (retroSmokeRoot != null && retroSmokeRoot.activeSelf != retroActive)
            retroSmokeRoot.SetActive(retroActive);
    }
}