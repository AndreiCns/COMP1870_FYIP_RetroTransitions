using UnityEngine;

public class AmmoUpgradePickup : PickupBase
{
    [Header("Upgrade / Reward")]
    [SerializeField] private AmmoType ammoType = AmmoType.Shell;
    [SerializeField] private int grantAmmoAmount = 50;
    [SerializeField] private bool unlockIfLocked = true;
    [SerializeField] private bool autoSwitchOnUnlock = true;

    protected override bool TryApply(Collider player)
    {
        PlayerCombatState state = player.GetComponent<PlayerCombatState>();
        if (state == null) return false;

        bool wasUnlocked = state.IsAmmoUnlocked(ammoType);

        if (unlockIfLocked && !wasUnlocked)
            state.UnlockAmmoType(ammoType, giveStarterAmmo: false);

        if (grantAmmoAmount > 0)
        {
            int current = state.GetAmmo(ammoType);
            state.SetAmmo(ammoType, current + grantAmmoAmount);
        }

        if (autoSwitchOnUnlock && !wasUnlocked)
        {
            PlayerCombatController combat = player.GetComponent<PlayerCombatController>();
            if (combat != null)
                combat.TrySetAmmoType(ammoType);
            else
                state.TrySetCurrentAmmoType(ammoType);
        }

        return true;
    }
}