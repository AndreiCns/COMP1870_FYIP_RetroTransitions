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

    [Header("Muzzle VFX Prefabs")]
    public GameObject modernMuzzlePrefab;   // particle prefab
    public SpriteRenderer retroSpritePrefab; // sprite prefab

    public GameObject GetModernPrefab() => modernMuzzlePrefab;
    public SpriteRenderer GetRetroPrefab() => retroSpritePrefab;

    [Header("UI (optional)")]
    public string uiLabel = "BULL";

    [Header("Cooldown Smoke")]
    [Tooltip("If false, cooldown smoke won't play for this ammo type.")]
    public bool enableCooldownSmoke = true;

    [Header("SFX")]
    public AudioClip gunshotClip;
    public Vector2 gunshotPitch = new Vector2(0.97f, 1.03f);
    public float gunshotVolume01 = 0.8f;

    [Header("Recoil")]
    [Tooltip("Local Z kickback applied per shot (bigger = more push back).")]
    [Min(0f)] public float recoilKickback = 0.15f;

    [Tooltip("Pitch-up degrees applied per shot.")]
    [Min(0f)] public float recoilUp = 6f;

    [Tooltip("How fast recoil returns to zero (higher = snappier).")]
    [Min(0f)] public float recoilRecovery = 12f;

#if UNITY_EDITOR
private void OnValidate()
{
    name = $"AmmoTypeConfig_{type}";
}
#endif
}
