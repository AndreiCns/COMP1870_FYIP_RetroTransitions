using UnityEngine;

public class WeaponCooldownSmoke : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private StyleSwapEvent styleSwapEvent;
    [SerializeField] private StyleSwapManager styleSwapManager;

    [Header("Smoke Roots")]
    [SerializeField] private GameObject modernSmokeRoot;
    [SerializeField] private GameObject retroSmokeRoot;

    [Header("Modern Continuous Smoke")]
    [SerializeField] private ParticleSystem modernSmokePS;
    [SerializeField] private float modernMaxRate = 25f;
    [SerializeField] private float modernMinRate = 0f;
    [Tooltip("Higher = tighter mid-peak")]
    [SerializeField] private float fadePower = 2.5f;

    [Header("Modern Size")]
    [SerializeField] private float modernMinSize = 0.03f;
    [SerializeField] private float modernMaxSize = 0.08f;

    private StyleState currentStyle = StyleState.Modern;

    private ParticleSystem.EmissionModule modernEmission;
    private ParticleSystem.MainModule modernMain;

    private void Awake()
    {
        if (combatController == null)
            combatController = GetComponentInParent<PlayerCombatController>();

        if (combatController == null)
        {
            Debug.LogError($"{name}: Missing PlayerCombatController.", this);
            enabled = false;
            return;
        }

        if (styleSwapManager == null)
            styleSwapManager = Object.FindFirstObjectByType<StyleSwapManager>();

        if (modernSmokePS != null)
        {
            modernEmission = modernSmokePS.emission;
            modernMain = modernSmokePS.main;
            modernMain.loop = true;
            modernEmission.enabled = true;
        }

        SetSmokeActive(false, false);
        ApplyModernIntensity(0f);
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;

        // Sync so enabling mid-session in retro mode drives the correct root
        if (styleSwapManager != null)
            OnStyleChanged(styleSwapManager.CurrentState);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;

        SetSmokeActive(false, false);
    }

    private void Update()
    {
        bool cooling = combatController.IsOnCooldown;

        if (cooling && !combatController.ShouldPlayCooldownSmoke)
        {
            SetSmokeActive(false, false);
            ApplyModernIntensity(0f);
            return;
        }

        bool wantModern = cooling && currentStyle == StyleState.Modern;
        bool wantRetro = cooling && currentStyle == StyleState.Retro;

        SetSmokeActive(wantModern, wantRetro);

        if (wantModern)
        {
            float t = combatController.Cooldown01;
            float p = 1f - t;
            float bell = Mathf.Sin(p * Mathf.PI);
            float intensity = Mathf.Pow(bell, fadePower);
            ApplyModernIntensity(intensity);
        }
        else
        {
            ApplyModernIntensity(0f);
        }
    }

    private void OnStyleChanged(StyleState newState)
    {
        currentStyle = newState;
        SetSmokeActive(false, false);
        ApplyModernIntensity(0f);
    }

    private void ApplyModernIntensity(float intensity01)
    {
        if (modernSmokePS == null) return;

        float rate = Mathf.Lerp(modernMinRate, modernMaxRate, intensity01);
        modernEmission.rateOverTime = rate;

        Color c = modernMain.startColor.color;
        c.a = intensity01;
        modernMain.startColor = c;

        float size = Mathf.Lerp(modernMinSize, modernMaxSize, intensity01);
        modernMain.startSize = size;

        if (rate <= 0.01f && modernSmokePS.isPlaying)
            modernSmokePS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        else if (rate > 0.01f && !modernSmokePS.isPlaying)
            modernSmokePS.Play(true);
    }

    private void SetSmokeActive(bool modernActive, bool retroActive)
    {
        if (modernSmokeRoot != null && modernSmokeRoot.activeSelf != modernActive)
            modernSmokeRoot.SetActive(modernActive);

        if (retroSmokeRoot != null && retroSmokeRoot.activeSelf != retroActive)
            retroSmokeRoot.SetActive(retroActive);
    }
}