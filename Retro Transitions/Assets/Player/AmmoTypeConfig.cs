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
    [Tooltip("One flash controller per ammo type. It self-manages modern vs retro internally.")]
    public MuzzleFlashController muzzleFlash;

    public MuzzleFlashController GetMuzzleFlash() => muzzleFlash;

    [Header("UI (optional)")]
    public string uiLabel = "BULL";

    [Header("Cooldown Smoke")]
    [Tooltip("If false, cooldown smoke won't play for this ammo type.")]
    public bool enableCooldownSmoke = true;
}
