using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmmoUpgradePickup : MonoBehaviour
{
    [Header("Upgrade / Reward")]
    [SerializeField] private AmmoType ammoType = AmmoType.Shell;
    [SerializeField] private int grantAmmoAmount = 50;
    [SerializeField] private bool unlockIfLocked = true;
    [SerializeField] private bool autoSwitchOnUnlock = true;

    private void Awake()
    {
        // Ensure this behaves as a trigger pickup
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerCombatState state = other.GetComponent<PlayerCombatState>();
        if (state == null) return;

        bool wasUnlocked = state.IsAmmoUnlocked(ammoType);

        // Unlock first so the player can immediately use it
        if (unlockIfLocked && !wasUnlocked)
            state.UnlockAmmoType(ammoType, giveStarterAmmo: false);

        if (grantAmmoAmount > 0)
        {
            int current = state.GetAmmo(ammoType);
            state.SetAmmo(ammoType, current + grantAmmoAmount);
        }

        // Auto-equip only when this pickup actually unlocks something new
        if (autoSwitchOnUnlock && !wasUnlocked)
        {
            PlayerCombatController combat = other.GetComponent<PlayerCombatController>();
            if (combat != null)
                combat.TrySetAmmoType(ammoType);
            else
                state.TrySetCurrentAmmoType(ammoType);
        }

        Destroy(gameObject);
    }
}
