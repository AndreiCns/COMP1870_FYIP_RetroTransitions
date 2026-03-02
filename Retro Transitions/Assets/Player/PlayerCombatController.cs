using System.Collections;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCombatState combatState;
    [SerializeField] private WeaponStyleSwap weaponStyleSwap;
    [SerializeField] private StyleSwapManager styleSwapManager;

    [Header("Shoot Modules")]
    [SerializeField] private PlayerShootModule modernShoot;
    [SerializeField] private PlayerShootModule retroShoot;

    [Header("Ammo Type Configs")]
    [SerializeField] private AmmoTypeConfig[] ammoTypeConfigs = new AmmoTypeConfig[PlayerCombatState.AmmoTypeCount];

    [Header("Style")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Tuning")]
    [SerializeField] private int ammoPerShot = 1;

    // --- Public API used by HUD/Smoke/etc ---
    public AmmoType CurrentAmmoType => combatState != null ? combatState.CurrentAmmoType : AmmoType.Bullet;

    public bool IsOnCooldown => fireTimer > 0f;
    public float CooldownRemaining => Mathf.Max(0f, fireTimer);
    public float Cooldown01 => Mathf.Clamp01(lastShotCooldown <= 0.0001f ? 0f : fireTimer / lastShotCooldown);

    public AmmoTypeConfig CurrentConfig => combatState != null ? GetConfig(combatState.CurrentAmmoType) : null;

    public bool ShouldPlayCooldownSmoke
    {
        get
        {
            AmmoTypeConfig cfg = CurrentConfig;
            return cfg == null || cfg.enableCooldownSmoke;
        }
    }

    private float fireTimer;
    private float lastShotCooldown = 0.01f;

    private PlayerShootModule activeShoot;
    private Coroutine cacheRoutine;

    private void Awake()
    {
        if (combatState == null)
            combatState = GetComponent<PlayerCombatState>();

        if (styleSwapManager == null)
            styleSwapManager = FindFirstObjectByType<StyleSwapManager>();

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

        ScheduleCacheActiveShoot();
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;

        if (cacheRoutine != null)
        {
            StopCoroutine(cacheRoutine);
            cacheRoutine = null;
        }
    }

    private void Update()
    {
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    public bool TryFire()
    {
        if (fireTimer > 0f)
            return false;

        if (combatState == null)
            return false;

        AmmoTypeConfig cfg = GetConfig(combatState.CurrentAmmoType);
        if (cfg == null)
            return false;

        if (activeShoot == null)
            CacheActiveShootImmediate();

        if (activeShoot == null)
            return false;

        // Gate 1: module local lock
        if (!activeShoot.TryBeginFire(cfg.fireCooldown))
            return false;

        // Gate 2: ammo
        if (!combatState.TryConsumeAmmo(ammoPerShot))
            return false;

        // Ensure damage is set BEFORE the animation event calls FireProjectile()
        activeShoot.SetDamage(cfg.damage);

        activeShoot.FireImmediate(cfg);

        // Presentation that isn't tied to the projectile moment stays here (weapon anim / recoil etc.)
        weaponStyleSwap?.Fire();

        lastShotCooldown = Mathf.Max(0.01f, cfg.fireCooldown);
        fireTimer = cfg.fireCooldown;

        return true;
    }

    public bool TrySetAmmoType(AmmoType type) => combatState != null && combatState.TrySetCurrentAmmoType(type);

    private AmmoTypeConfig GetConfig(AmmoType type)
    {
        int i = (int)type;
        return (ammoTypeConfigs != null && i >= 0 && i < ammoTypeConfigs.Length) ? ammoTypeConfigs[i] : null;
    }

    private void OnStyleChanged(StyleState newState) => ScheduleCacheActiveShoot();

    private void ScheduleCacheActiveShoot()
    {
        if (cacheRoutine != null)
            StopCoroutine(cacheRoutine);

        cacheRoutine = StartCoroutine(CacheActiveShootNextFrame());
    }

    private IEnumerator CacheActiveShootNextFrame()
    {
        // Wait one frame so style swap listeners/modules have applied enabled/active state
        yield return null;

        CacheActiveShootImmediate();
        cacheRoutine = null;
    }

    private void CacheActiveShootImmediate()
    {
        StyleState state = styleSwapManager != null ? styleSwapManager.CurrentState : StyleState.Modern;

        // Pick by style FIRST (this fixes your bug).
        PlayerShootModule preferred = (state == StyleState.Modern) ? modernShoot : retroShoot;

        if (preferred != null && preferred.isActiveAndEnabled)
        {
            activeShoot = preferred;
            return;
        }

        // Fallback: whichever is active (in case one isn't wired)
        if (modernShoot != null && modernShoot.isActiveAndEnabled)
            activeShoot = modernShoot;
        else if (retroShoot != null && retroShoot.isActiveAndEnabled)
            activeShoot = retroShoot;
        else
            activeShoot = null;
    }
}