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

    private float fireTimer;
    private StyleState currentStyle = StyleState.Modern;

    // Fire logic should always target the currently active visual weapon module.
    private PlayerShootModule ActiveShoot =>
        (modernShoot != null && modernShoot.gameObject.activeInHierarchy) ? modernShoot :
        (retroShoot != null && retroShoot.gameObject.activeInHierarchy) ? retroShoot :
        null;

    private void Awake()
    {
        if (combatState == null)
            combatState = GetComponent<PlayerCombatState>();

        // CombatState is required: it owns ammo + selection rules.
        if (combatState == null)
        {
            Debug.LogError("[Combat] PlayerCombatState missing.", this);
            enabled = false;
            return;
        }

        // Optional at runtime (you can still fire without style swapping), but warn early.
        if (weaponStyleSwap == null)
            Debug.LogWarning("[Combat] weaponStyleSwap not assigned.", this);

        // Keeps config lookup predictable (Bullet/Rocket/Shell/Plasma).
        if (ammoTypeConfigs == null || ammoTypeConfigs.Length != 4)
            Debug.LogWarning("[Combat] ammoTypeConfigs should be length 4 (Bullet/Rocket/Shell/Plasma).", this);
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
        // Timer lives here so ROF stays deterministic regardless of input spam.
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
        {
            if (verboseLogs) Debug.LogWarning($"[Combat] Missing config for {type}.", this);
            return false;
        }

        // Lock first so ammo consumption can never happen twice per accepted shot.
        if (!shoot.TryBeginFire(cfg.fireCooldown))
            return false;

        // Ammo is consumed only here (single source of truth).
        if (!combatState.TryConsumeAmmo(ammoPerShot))
        {
            if (verboseLogs) Debug.Log("[Combat] Click (no ammo).", this);
            return false;
        }

        // Push per-shot parameters before the anim event fires.
        shoot.SetDamage(cfg.damage);

        // Style determines which muzzle flash variant to use.
        MuzzleFlashController flashOverride = GetMuzzleOverride(cfg);
        if (flashOverride != null)
            shoot.SetMuzzleFlash(flashOverride);

        // Visual fire is driven by the active weapon animator (matches the anim event timing).
        weaponStyleSwap?.Fire();

        fireTimer = cfg.fireCooldown;

        if (verboseLogs)
            Debug.Log($"[Combat] Fired {type} | dmg={cfg.damage} cd={cfg.fireCooldown}", this);

        return true;
    }

    public bool TrySetAmmoType(AmmoType type)
    {
        // Selection rules live in CombatState (e.g., locked types if ammo = 0).
        bool ok = combatState.TrySetCurrentAmmoType(type);

        if (verboseLogs)
            Debug.Log($"[Combat] AmmoType -> {type} ({(ok ? "OK" : "LOCKED")})", this);

        return ok;
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

        return (currentStyle == StyleState.Retro)
            ? cfg.retroMuzzleFlashOverride
            : cfg.modernMuzzleFlashOverride;
    }

    private void OnStyleChanged(StyleState newState)
    {
        currentStyle = newState;
    }
}
