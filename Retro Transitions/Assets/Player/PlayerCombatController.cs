using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCombatState combatState;
    [SerializeField] private WeaponStyleSwap weaponStyleSwap;
    [SerializeField] private Animator weaponAnimator;

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

    private readonly string shootTrigger = "Fire";

    private float fireTimer;
    private StyleState currentStyle = StyleState.Modern;

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

        if (weaponAnimator == null)
            Debug.LogWarning("[Combat] weaponAnimator not assigned.", this);

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
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    public void TryFire()
    {
        if (fireTimer > 0f)
            return;

        var shoot = ActiveShoot;
        if (shoot == null)
            return;

        AmmoType type = combatState.CurrentAmmoType;
        AmmoTypeConfig cfg = GetConfig(type);

        if (cfg == null)
        {
            if (verboseLogs) Debug.LogWarning($"[Combat] Missing config for {type}.", this);
            return;
        }

        if (!combatState.TryConsumeAmmo(ammoPerShot))
        {
            if (verboseLogs) Debug.Log("[Combat] Click (no ammo).", this);
            return;
        }

        // Configure the shot BEFORE the anim event fires.
        shoot.SetDamage(cfg.damage);

        MuzzleFlashController flashOverride = GetMuzzleOverride(cfg);
        if (flashOverride != null)
            shoot.SetMuzzleFlash(flashOverride);

        // Shoot module gates timing (anim must finish)
        if (!shoot.TryBeginFire())
            return;

        if (weaponAnimator != null)
            weaponAnimator.SetTrigger(shootTrigger);

        weaponStyleSwap?.Fire();

        fireTimer = cfg.fireCooldown;

        if (verboseLogs)
            Debug.Log($"[Combat] Fired {type} | dmg={cfg.damage} cd={cfg.fireCooldown}", this);
    }

    public bool TrySetAmmoType(AmmoType type)
    {
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
