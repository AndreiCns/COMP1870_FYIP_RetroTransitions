using UnityEngine;

public enum AmmoType
{
    Bullet = 0,
    Shell = 1,
    Rocket = 2,
    Plasma = 3
}

public class PlayerCombatState : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private AmmoType currentAmmoType = AmmoType.Bullet;

    // Index matches AmmoType enum order.
    [SerializeField] private int[] ammoCounts = new int[4] { 99, 0, 0, 0 };

    // Max per ammo type (same enum indexing).
    [SerializeField] private int[] ammoMax = new int[4] { 200, 50, 50, 300 };

    [Header("Unlocks")]
    // Unlock state per ammo type (used for progression logic).
    [SerializeField] private bool[] ammoUnlocked = new bool[4] { true, false, false, false };

    [Header("Keys (optional)")]
    [SerializeField] private bool hasBlueKey;
    [SerializeField] private bool hasYellowKey;
    [SerializeField] private bool hasRedKey;

    public AmmoType CurrentAmmoType => currentAmmoType;

    public int GetAmmo(AmmoType type)
    {
        int i = (int)type;

        if (ammoCounts == null || ammoCounts.Length < 4) return 0;
        if (i < 0 || i >= ammoCounts.Length) return 0;

        return ammoCounts[i];
    }

    public void SetAmmo(AmmoType type, int amount)
    {
        EnsureArrays();

        int i = (int)type;
        int max = GetAmmoMax(type);

        // Clamp to valid range; prevents negative ammo bugs.
        ammoCounts[i] = Mathf.Clamp(amount, 0, max);
    }

    public bool IsAmmoUnlocked(AmmoType type)
    {
        int i = (int)type;

        if (ammoUnlocked == null || ammoUnlocked.Length < 4) return false;
        if (i < 0 || i >= ammoUnlocked.Length) return false;

        return ammoUnlocked[i];
    }

    // Tier = number of unlocked ammo types (used for progression scaling).
    public int UpgradeTier
    {
        get
        {
            int count = 0;

            for (int i = 0; i < 4; i++)
                if (ammoUnlocked != null && ammoUnlocked.Length > i && ammoUnlocked[i])
                    count++;

            return Mathf.Clamp(count, 1, 4);
        }
    }

    public bool HasBlueKey => hasBlueKey;
    public bool HasYellowKey => hasYellowKey;
    public bool HasRedKey => hasRedKey;

    private void Awake()
    {
        EnsureArrays();

        // Bullet must always remain available.
        ammoUnlocked[(int)AmmoType.Bullet] = true;

        // Fallback safety if current selection becomes invalid.
        if (!IsAmmoUnlocked(currentAmmoType))
            currentAmmoType = AmmoType.Bullet;
    }

    public bool TrySetCurrentAmmoType(AmmoType type)
    {
        if (!IsAmmoUnlocked(type))
            return false;

        currentAmmoType = type;
        return true;
    }

    public bool TryConsumeAmmo(int amount)
    {
        if (amount <= 0) return true;

        int current = GetAmmo(currentAmmoType);

        if (current < amount)
            return false;

        SetAmmo(currentAmmoType, current - amount);
        return true;
    }

    public void UnlockAmmoType(AmmoType type, bool giveStarterAmmo = true, int starterAmmo = 50)
    {
        EnsureArrays();

        int i = (int)type;
        ammoUnlocked[i] = true;

        // Optional starter ammo on unlock.
        if (giveStarterAmmo && ammoCounts[i] <= 0)
            ammoCounts[i] = Mathf.Max(0, starterAmmo);
    }

    public void GrantKeyBlue() => hasBlueKey = true;
    public void GrantKeyYellow() => hasYellowKey = true;
    public void GrantKeyRed() => hasRedKey = true;

    private void EnsureArrays()
    {
        // Defensive guard in case inspector resets array sizes.
        if (ammoCounts == null || ammoCounts.Length != 4)
            ammoCounts = new int[4];

        if (ammoUnlocked == null || ammoUnlocked.Length != 4)
            ammoUnlocked = new bool[4];

        if (ammoMax == null || ammoMax.Length != 4)
            ammoMax = new int[4];

        // Always keep Bullet unlocked.
        ammoUnlocked[(int)AmmoType.Bullet] = true;
    }

    public int GetAmmoMax(AmmoType type)
    {
        int i = (int)type;

        if (ammoMax == null || ammoMax.Length < 4) return 0;
        if (i < 0 || i >= ammoMax.Length) return 0;

        return ammoMax[i];
    }

    public string GetAmmoCurrentMaxText(AmmoType type)
    {
        int cur = GetAmmo(type);
        int max = GetAmmoMax(type);

        return $"{cur}/{max}";
    }
}
