using UnityEngine;

public class PlayerCombatState : MonoBehaviour
{
    // Single source of truth for ammo type count — driven by the enum itself,
    // so adding a fifth AmmoType automatically updates all array sizes.
    public static readonly int AmmoTypeCount = System.Enum.GetValues(typeof(AmmoType)).Length;

    [Header("Ammo")]
    [SerializeField] private AmmoType currentAmmoType = AmmoType.Bullet;

    // Index matches AmmoType enum order.
    [SerializeField] private int[] ammoCounts = new int[AmmoTypeCount];
    [SerializeField] private int[] ammoMax = new int[AmmoTypeCount];

    [Header("Unlocks")]
    [SerializeField] private bool[] ammoUnlocked = new bool[AmmoTypeCount];

    [Header("Keys (optional)")]
    [SerializeField] private bool hasBlueKey;
    [SerializeField] private bool hasYellowKey;
    [SerializeField] private bool hasRedKey;

    

    public AmmoType CurrentAmmoType => currentAmmoType;
    public bool HasBlueKey => hasBlueKey;
    public bool HasYellowKey => hasYellowKey;
    public bool HasRedKey => hasRedKey;

    // Tier = number of unlocked ammo types (used for progression scaling).
    public int UpgradeTier
    {
        get
        {
            int count = 0;
            for (int i = 0; i < AmmoTypeCount; i++)
                if (ammoUnlocked != null && ammoUnlocked.Length > i && ammoUnlocked[i])
                    count++;
            return Mathf.Clamp(count, 1, AmmoTypeCount);
        }
    }

    

    private void Awake()
    {
        EnsureArrays();

        // Bullet must always remain available.
        ammoUnlocked[(int)AmmoType.Bullet] = true;

        // Fallback safety if current selection becomes invalid.
        if (!IsAmmoUnlocked(currentAmmoType))
            currentAmmoType = AmmoType.Bullet;
    }

  

    public int GetAmmo(AmmoType type)
    {
        int i = (int)type;
        return IsValidIndex(ammoCounts, i) ? ammoCounts[i] : 0;
    }

    public void SetAmmo(AmmoType type, int amount)
    {
        EnsureArrays();
        int i = (int)type;
        ammoCounts[i] = Mathf.Clamp(amount, 0, GetAmmoMax(type));
    }

    public int GetAmmoMax(AmmoType type)
    {
        int i = (int)type;
        return IsValidIndex(ammoMax, i) ? ammoMax[i] : 0;
    }

    public string GetAmmoCurrentMaxText(AmmoType type) =>
        $"{GetAmmo(type)}/{GetAmmoMax(type)}";

    public bool TryConsumeAmmo(int amount)
    {
        if (amount <= 0) return true;
        int current = GetAmmo(currentAmmoType);
        if (current < amount) return false;
        SetAmmo(currentAmmoType, current - amount);
        return true;
    }

   

    public bool IsAmmoUnlocked(AmmoType type)
    {
        int i = (int)type;
        return IsValidIndex(ammoUnlocked, i) && ammoUnlocked[i];
    }

    public void UnlockAmmoType(AmmoType type, bool giveStarterAmmo = true, int starterAmmo = 50)
    {
        EnsureArrays();
        int i = (int)type;
        ammoUnlocked[i] = true;
        if (giveStarterAmmo && ammoCounts[i] <= 0)
            ammoCounts[i] = Mathf.Max(0, starterAmmo);
    }

    public bool TrySetCurrentAmmoType(AmmoType type)
    {
        if (!IsAmmoUnlocked(type)) return false;
        currentAmmoType = type;
        return true;
    }

   

    public void GrantKeyBlue() => hasBlueKey = true;
    public void GrantKeyYellow() => hasYellowKey = true;
    public void GrantKeyRed() => hasRedKey = true;

    

    private void EnsureArrays()
    {
        // Resize if the enum has grown since the arrays were serialised.
        if (ammoCounts == null || ammoCounts.Length != AmmoTypeCount) ammoCounts = new int[AmmoTypeCount];
        if (ammoMax == null || ammoMax.Length != AmmoTypeCount) ammoMax = new int[AmmoTypeCount];
        if (ammoUnlocked == null || ammoUnlocked.Length != AmmoTypeCount) ammoUnlocked = new bool[AmmoTypeCount];

        // Bullet always unlocked regardless of serialised state.
        ammoUnlocked[(int)AmmoType.Bullet] = true;
    }

    private static bool IsValidIndex(System.Array arr, int i) =>
        arr != null && i >= 0 && i < arr.Length;
}