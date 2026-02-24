using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCombatState combatState;
    [SerializeField] private WeaponStyleSwap weaponStyleSwap;

    [Header("Shoot Modules")]
    [SerializeField] private PlayerShootModule modernShoot;
    [SerializeField] private PlayerShootModule retroShoot;

    [Header("Ammo Type Configs")]
    [SerializeField] private AmmoTypeConfig[] ammoTypeConfigs = new AmmoTypeConfig[4];

    [Header("Style")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Tuning")]
    [SerializeField] private int ammoPerShot = 1;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    public AmmoType CurrentAmmoType => combatState != null ? combatState.CurrentAmmoType : AmmoType.Bullet;


    private float fireTimer;
    private float lastShotCooldown = 0.01f; // used to normalize cooldown
    private StyleState currentStyle = StyleState.Modern;

    public bool IsOnCooldown => fireTimer > 0f;
    public float CooldownRemaining => Mathf.Max(0f, fireTimer);

    // 1 at shot start, 0 when ready again
    public float Cooldown01 => Mathf.Clamp01(fireTimer / lastShotCooldown);

    private PlayerShootModule ActiveShoot =>
        (modernShoot != null && modernShoot.gameObject.activeInHierarchy) ? modernShoot :
        (retroShoot != null && retroShoot.gameObject.activeInHierarchy) ? retroShoot :
        null;

    private void Awake()
    {
        if (combatState == null)
            combatState = GetComponent<PlayerCombatState>();

        if (combatState == null)
        {
            Debug.LogError("[Combat] PlayerCombatState missing.", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void Update()
    {
        // Cooldown is authoritative here (prevents input spam bypassing ROF).
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    public bool TryFire()
    {
        if (fireTimer > 0f)
            return false;

        var shoot = ActiveShoot;
        if (shoot == null)
            return false;

        AmmoType type = combatState.CurrentAmmoType;
        AmmoTypeConfig cfg = GetConfig(type);

        if (cfg == null)
            return false;

        if (!shoot.TryBeginFire(cfg.fireCooldown))
            return false;

        if (!combatState.TryConsumeAmmo(ammoPerShot))
            return false;

        shoot.SetDamage(cfg.damage);

        MuzzleFlashController flashOverride = GetMuzzleOverride(cfg);
        if (flashOverride != null)
            shoot.SetMuzzleFlash(flashOverride);

        weaponStyleSwap?.Fire();

        // Store for normalized fade.
        lastShotCooldown = Mathf.Max(0.01f, cfg.fireCooldown);
        fireTimer = cfg.fireCooldown;

        return true;
    }

    public bool TrySetAmmoType(AmmoType type)
    {
        return combatState.TrySetCurrentAmmoType(type);
    }

    private AmmoTypeConfig GetConfig(AmmoType type)
    {
        int i = (int)type;
        if (ammoTypeConfigs == null || ammoTypeConfigs.Length <= i)
            return null;

        return ammoTypeConfigs[i];
    }

    private MuzzleFlashController GetMuzzleOverride(AmmoTypeConfig cfg)
    {
        if (cfg == null) return null;

        return currentStyle == StyleState.Retro
            ? cfg.retroMuzzleFlashOverride
            : cfg.modernMuzzleFlashOverride;
    }

    private void OnStyleChanged(StyleState newState)
    {
        currentStyle = newState;
    }

    public AmmoTypeConfig CurrentConfig
    {
        get
        {
            if (combatState == null) return null;
            return GetConfig(combatState.CurrentAmmoType);
        }
    }

    public bool ShouldPlayCooldownSmoke
    {
        get
        {
            AmmoTypeConfig cfg = CurrentConfig;
            if (cfg == null) return true; // fail-safe: keep smoke on if missing config
            return cfg.enableCooldownSmoke;
        }
    }
}