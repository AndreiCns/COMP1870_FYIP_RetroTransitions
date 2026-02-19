using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Ammo Type Config", fileName = "AmmoTypeConfig_")]
public class AmmoTypeConfig : ScriptableObject
{
    [Header("Identity")]
    public AmmoType type = AmmoType.Bullet;

    [Header("Combat Tuning")]
    [Min(0f)] public float damage = 10f;

    [Tooltip("Seconds between shots for this ammo type (lower = faster).")]
    [Min(0.01f)] public float fireCooldown = 0.2f;

    [Header("Muzzle VFX")]
    [Tooltip("Optional override if you want a unique muzzle flash per ammo type.")]
    public MuzzleFlashController modernMuzzleFlashOverride;

    [Tooltip("Optional override if you want a unique muzzle flash per ammo type.")]
    public MuzzleFlashController retroMuzzleFlashOverride;

    [Header("UI (optional)")]
    public string uiLabel = "BULL";
}
