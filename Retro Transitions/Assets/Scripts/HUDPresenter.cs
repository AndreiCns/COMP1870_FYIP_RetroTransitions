using UnityEngine;

public class HUDPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HUDView view;
    [SerializeField] private Health health;
    [SerializeField] private PlayerCombatState combatState;

    private void Awake()
    {
        if (view == null)
            view = GetComponentInChildren<HUDView>();

        if (combatState == null)
            combatState = FindFirstObjectByType<PlayerCombatState>();

        if (health == null && combatState != null)
            health = combatState.GetComponent<Health>();

        if (health == null)
            health = FindFirstObjectByType<Health>();

        if (view == null || health == null || combatState == null)
        {
            Debug.LogError("[HUDPresenter] Missing refs.", this);
            enabled = false;
        }
    }



    private void Update()
    {
        UpdateHealth();
        UpdateAmmo();
        UpdateUpgrades();
        UpdateKeys();
    }

    private void UpdateHealth()
    {
        int hpPercent = health.Max <= 0f ? 0 : Mathf.RoundToInt((health.Current / health.Max) * 100f);
        view.SetHealthPercent(hpPercent);
    }

    private void UpdateAmmo()
    {
        AmmoType t = combatState.CurrentAmmoType;
        view.SetAmmo(combatState.GetAmmo(t));

        string bullet = combatState.GetAmmoCurrentMaxText(AmmoType.Bullet);
        string shell = combatState.GetAmmoCurrentMaxText(AmmoType.Shell);
        string rocket = combatState.GetAmmoCurrentMaxText(AmmoType.Rocket);
        string plasma = combatState.GetAmmoCurrentMaxText(AmmoType.Plasma);

        view.SetAmmoRows(bullet, shell, rocket, plasma);
        view.SetAmmoRowHighlight(
          combatState.CurrentAmmoType,
          combatState.IsAmmoUnlocked(AmmoType.Bullet),
          combatState.IsAmmoUnlocked(AmmoType.Shell),
          combatState.IsAmmoUnlocked(AmmoType.Rocket),
          combatState.IsAmmoUnlocked(AmmoType.Plasma)
          );


    }

    private void UpdateUpgrades()
    {
        // Slot 1..4 maps to ammo unlocks in your narrative order.
        view.SetUpgradeUnlocked(0, combatState.IsAmmoUnlocked(AmmoType.Bullet));
        view.SetUpgradeUnlocked(1, combatState.IsAmmoUnlocked(AmmoType.Shell));
        view.SetUpgradeUnlocked(2, combatState.IsAmmoUnlocked(AmmoType.Rocket));
        view.SetUpgradeUnlocked(3, combatState.IsAmmoUnlocked(AmmoType.Plasma));
    }

    private void UpdateKeys()
    {
        view.SetKeys(combatState.HasBlueKey, combatState.HasYellowKey, combatState.HasRedKey);
    }
}
