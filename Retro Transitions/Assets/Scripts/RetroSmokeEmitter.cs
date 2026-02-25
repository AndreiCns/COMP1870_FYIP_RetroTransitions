using UnityEngine;

public class RetroSmokeEmitter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private StyleSwapEvent styleSwapEvent;
    [SerializeField] private StyleSwapManager styleSwapManager;

    [Header("Spawn")]
    [SerializeField] private RetroSmokePuff puffPrefab;
    [SerializeField] private int maxAlivePuffs = 20;
    [SerializeField] private Vector2 randomXZOffset = new Vector2(0.02f, 0.02f);

    [Header("Timing")]
    [Tooltip("Small delay so smoke doesn't pop instantly.")]
    [SerializeField] private float startDelay = 0.03f;
    [Tooltip("When cooldown remaining is below this, taper off smoke.")]
    [SerializeField] private float endFadeWindow = 0.12f;

    [Header("Rate")]
    [SerializeField] private float minSpawnInterval = 0.16f;
    [SerializeField] private float maxSpawnInterval = 0.06f;

    [Header("Scale")]
    [SerializeField] private Vector2 minScale = new Vector2(0.75f, 0.95f);
    [SerializeField] private Vector2 maxScale = new Vector2(1.05f, 1.25f);

    [Header("Recoil Inherit")]
    [SerializeField] private Transform followAnchor;
    [SerializeField] private float inheritStrength = 0.08f;

    private Vector3 lastAnchorPos;
    private Vector3 anchorVelocity;
    private float spawnTimer;
    private float currentCooldownStart = -1f;
    private bool isRetro;

    private void Awake()
    {
        if (combatController == null)
            combatController = GetComponentInParent<PlayerCombatController>();

        if (combatController == null)
        {
            Debug.LogError($"{name}: Missing PlayerCombatController reference.", this);
            enabled = false;
            return;
        }

        if (puffPrefab == null)
        {
            Debug.LogError($"{name}: Missing puffPrefab reference.", this);
            enabled = false;
            return;
        }

        if (styleSwapManager == null)
            styleSwapManager = Object.FindFirstObjectByType<StyleSwapManager>();

        if (followAnchor == null)
            followAnchor = transform;

        lastAnchorPos = followAnchor.position;
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;
        else
            Debug.LogWarning($"{name}: styleSwapEvent not assigned.", this);

        // Sync current state so isRetro is correct from the first frame
        if (styleSwapManager != null)
            OnStyleChanged(styleSwapManager.CurrentState);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void OnStyleChanged(StyleState state)
    {
        isRetro = state == StyleState.Retro;
        if (!isRetro) currentCooldownStart = -1f;
    }

    private void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        anchorVelocity = (followAnchor.position - lastAnchorPos) / dt;
        lastAnchorPos = followAnchor.position;

        if (!isRetro || !combatController.IsOnCooldown)
        {
            currentCooldownStart = -1f;
            return;
        }

        float remaining = combatController.CooldownRemaining;

        if (currentCooldownStart < 0f)
            currentCooldownStart = remaining;

        float elapsed = currentCooldownStart - remaining;
        if (elapsed < startDelay) return;

        if (transform.childCount >= maxAlivePuffs) return;

        float intensity01 = GetIntensity01(remaining, currentCooldownStart);
        float targetInterval = Mathf.Lerp(minSpawnInterval, maxSpawnInterval, intensity01);

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = Mathf.Max(0.02f, targetInterval);
        SpawnPuff(intensity01);
    }

    private float GetIntensity01(float remaining, float startDuration)
    {
        if (remaining <= endFadeWindow)
            return Mathf.Clamp01(remaining / Mathf.Max(0.001f, endFadeWindow));

        float rampWindow = 0.12f;
        float elapsed = startDuration - remaining;
        return Mathf.Clamp01(elapsed / rampWindow);
    }

    private void SpawnPuff(float intensity01)
    {
        Vector3 offset = new Vector3(
            Random.Range(-randomXZOffset.x, randomXZOffset.x),
            0f,
            Random.Range(-randomXZOffset.y, randomXZOffset.y)
        );

        RetroSmokePuff puff = Instantiate(puffPrefab, transform.position + offset, Quaternion.identity, transform);

        float sx = Mathf.Lerp(minScale.x, maxScale.x, intensity01);
        float sy = Mathf.Lerp(minScale.y, maxScale.y, intensity01);
        float scale = Random.Range(sx, sy);

        puff.transform.localScale *= scale;
        puff.AddVelocity(anchorVelocity * inheritStrength);
    }
}