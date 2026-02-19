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

    [Tooltip("Ammo counts per type, index matches AmmoType enum order.")]
    [SerializeField] private int[] ammoCounts = new int[4] { 99, 0, 0, 0 };

    [Tooltip("Max ammo per type, index matches AmmoType enum order.")]
    [SerializeField] private int[] ammoMax = new int[4] { 200, 50, 50, 300 };


    [Header("Unlocks")]
    [Tooltip("Unlocked ammo types. Upgrade tier is derived from this.")]
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
        ammoCounts[(int)type] = Mathf.Max(0, amount);
    }

    public bool IsAmmoUnlocked(AmmoType type)
    {
        int i = (int)type;
        if (ammoUnlocked == null || ammoUnlocked.Length < 4) return false;
        if (i < 0 || i >= ammoUnlocked.Length) return false;
        return ammoUnlocked[i];
    }

    public int UpgradeTier
    {
        get
        {
            // Tier is number of unlocked ammo types (1..4).
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

        // Always keep at least Bullet unlocked.
        ammoUnlocked[(int)AmmoType.Bullet] = true;

        // If current type is locked, fall back to Bullet.
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
        if (current < amount) return false;

        SetAmmo(currentAmmoType, current - amount);
        return true;
    }

    public void UnlockAmmoType(AmmoType type, bool giveStarterAmmo = true, int starterAmmo = 50)
    {
        EnsureArrays();

        int i = (int)type;
        ammoUnlocked[i] = true;

        if (giveStarterAmmo && ammoCounts[i] <= 0)
            ammoCounts[i] = Mathf.Max(0, starterAmmo);
    }

    public void GrantKeyBlue() => hasBlueKey = true;
    public void GrantKeyYellow() => hasYellowKey = true;
    public void GrantKeyRed() => hasRedKey = true;

    private void EnsureArrays()
    {
        if (ammoCounts == null || ammoCounts.Length != 4)
            ammoCounts = new int[4];

        if (ammoUnlocked == null || ammoUnlocked.Length != 4)
            ammoUnlocked = new bool[4];

        if (ammoMax == null || ammoMax.Length != 4)
            ammoMax = new int[4];

        // Avoid “all false” state if something resets in inspector.
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
