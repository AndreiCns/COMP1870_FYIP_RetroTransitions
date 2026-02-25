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
    // Array size now driven by AmmoTypeCount — adding a new AmmoType
    // is the only change needed; no magic numbers to hunt down.
    [SerializeField] private AmmoTypeConfig[] ammoTypeConfigs = new AmmoTypeConfig[PlayerCombatState.AmmoTypeCount];

    [Header("Style")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Tuning")]
    [SerializeField] private int ammoPerShot = 1;


    public AmmoType CurrentAmmoType => combatState != null ? combatState.CurrentAmmoType : AmmoType.Bullet;
    public bool IsOnCooldown => fireTimer > 0f;
    public float CooldownRemaining => Mathf.Max(0f, fireTimer);
    public float Cooldown01 => Mathf.Clamp01(fireTimer / lastShotCooldown);

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

        if (combatState == null)
        {
            Debug.LogError("[Combat] PlayerCombatState missing.", this);
            enabled = false;
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
        if (fireTimer > 0f || activeShoot == null)
            return false;

        AmmoTypeConfig cfg = GetConfig(combatState.CurrentAmmoType);
        if (cfg == null)
            return false;

        if (!activeShoot.TryBeginFire(cfg.fireCooldown))
            return false;

        if (!combatState.TryConsumeAmmo(ammoPerShot))
            return false;

        activeShoot.SetDamage(cfg.damage);

        MuzzleFlashController flash = cfg.GetMuzzleFlash();
        if (flash != null)
            activeShoot.SetMuzzleFlash(flash);

        weaponStyleSwap?.Fire();

        lastShotCooldown = Mathf.Max(0.01f, cfg.fireCooldown);
        fireTimer = cfg.fireCooldown;

        return true;
    }

    public bool TrySetAmmoType(AmmoType type) => combatState.TrySetCurrentAmmoType(type);

    

    private AmmoTypeConfig GetConfig(AmmoType type)
    {
        int i = (int)type;
        return (ammoTypeConfigs != null && i < ammoTypeConfigs.Length) ? ammoTypeConfigs[i] : null;
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
        yield return null;

        activeShoot =
            (modernShoot != null && modernShoot.isActiveAndEnabled) ? modernShoot :
            (retroShoot != null && retroShoot.isActiveAndEnabled) ? retroShoot :
            null;

        cacheRoutine = null;
    }
}