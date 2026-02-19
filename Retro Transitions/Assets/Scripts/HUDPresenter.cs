using UnityEngine;

public class HUDPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HUDView view;
    [SerializeField] private Health health;
    [SerializeField] private PlayerCombatState combatState;

    private void Awake()
    {
        // Try local refs first so the prefab can be dropped anywhere.
        if (view == null)
            view = GetComponentInChildren<HUDView>();

        // State is usually on the player, but Find is OK here (one-off, not per-frame).
        if (combatState == null)
            combatState = FindFirstObjectByType<PlayerCombatState>();

        // Health typically sits alongside combat state on the player root.
        if (health == null && combatState != null)
            health = combatState.GetComponent<Health>();

        if (health == null)
            health = FindFirstObjectByType<Health>();

        // HUD needs all three to function; fail fast instead of spamming null refs.
        if (view == null || health == null || combatState == null)
        {
            Debug.LogError("[HUDPresenter] Missing refs.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        // Simple polling is fine for now; HUDView already avoids redundant text updates.
        UpdateHealth();
        UpdateAmmo();
        UpdateUpgrades();
        UpdateKeys();
    }

    private void UpdateHealth()
    {
        // Present HP as % for the Doom-style HUD layout.
        int hpPercent = health.Max <= 0f ? 0 : Mathf.RoundToInt((health.Current / health.Max) * 100f);
        view.SetHealthPercent(hpPercent);
    }

    private void UpdateAmmo()
    {
        AmmoType active = combatState.CurrentAmmoType;

        view.SetAmmo(combatState.GetAmmo(active));

        // Formatting stays in CombatState so HUD stays dumb and skin-friendly.
        string bullet = combatState.GetAmmoCurrentMaxText(AmmoType.Bullet);
        string shell = combatState.GetAmmoCurrentMaxText(AmmoType.Shell);
        string rocket = combatState.GetAmmoCurrentMaxText(AmmoType.Rocket);
        string plasma = combatState.GetAmmoCurrentMaxText(AmmoType.Plasma);

        view.SetAmmoRows(bullet, shell, rocket, plasma);

        bool bulletUnlocked = combatState.IsAmmoUnlocked(AmmoType.Bullet);
        bool shellUnlocked = combatState.IsAmmoUnlocked(AmmoType.Shell);
        bool rocketUnlocked = combatState.IsAmmoUnlocked(AmmoType.Rocket);
        bool plasmaUnlocked = combatState.IsAmmoUnlocked(AmmoType.Plasma);

        view.SetAmmoRowHighlight(active, bulletUnlocked, shellUnlocked, rocketUnlocked, plasmaUnlocked);
    }

    private void UpdateUpgrades()
    {
        // Slots are a simple “progress bar” of unlocks.
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
