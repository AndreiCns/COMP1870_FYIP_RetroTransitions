using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class PickupBase : MonoBehaviour
{
    [Header("Consume")]
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Audio (player-routed)")]
    [Tooltip("Plays through the player's PlayerAudioController (PlayerSFX mixer group).")]
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private float pickupVolume01 = 1f;
    [SerializeField] private Vector2 pickupPitchRange = new Vector2(0.98f, 1.02f);

    [Header("Modern Visual Motion (optional)")]
    [Tooltip("Assign the modern visual root ONLY (3D mesh root). Retro billboard should not be animated here.")]
    [SerializeField] private Transform modernVisualRoot;
    [SerializeField] private bool enableModernMotion = true;

    [Tooltip("Vertical bob amplitude in units.")]
    [SerializeField] private float floatAmplitude = 0.15f;

    [Tooltip("Vertical bob speed in cycles per second.")]
    [SerializeField] private float floatFrequency = 1.25f;

    [Tooltip("Yaw rotation speed in degrees per second.")]
    [SerializeField] private float rotateYawSpeed = 90f;

    [Tooltip("Use unscaled time so motion still plays during brief pause/freeze frames.")]
    [SerializeField] private bool useUnscaledTime = true;

    private Collider col;

    private Vector3 modernStartLocalPos;
    private float timeOffset;

    protected virtual void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;

        if (modernVisualRoot != null)
        {
            modernStartLocalPos = modernVisualRoot.localPosition;
            timeOffset = Random.Range(0f, 10f); // de-sync multiple pickups
        }
        else if (enableModernMotion)
        {
            Debug.LogWarning($"{name}: enableModernMotion is on but modernVisualRoot is not assigned.", this);
        }
    }

    private void Update()
    {
        if (!enableModernMotion || modernVisualRoot == null)
            return;

        float t = (useUnscaledTime ? Time.unscaledTime : Time.time) + timeOffset;

        // Float (local)
        float y = Mathf.Sin(t * Mathf.PI * 2f * floatFrequency) * floatAmplitude;
        modernVisualRoot.localPosition = modernStartLocalPos + new Vector3(0f, y, 0f);

        // Rotate (yaw only)
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        modernVisualRoot.Rotate(0f, rotateYawSpeed * dt, 0f, Space.Self);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!TryApply(other))
            return;

        // Keep pickups "player-heard" and snapshot-consistent by routing through PlayerAudioController.
        if (pickupSfx != null)
        {
            PlayerAudioController pac = other.GetComponentInParent<PlayerAudioController>();
            if (pac != null)
                pac.PlayOneShot2D(pickupSfx, pickupVolume01, pickupPitchRange);
            else
                Debug.LogWarning($"{name}: No PlayerAudioController found on Player. Pickup SFX will be silent.", this);
        }

        Consume();
    }

    private void Consume()
    {
        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    // Return true only if the pickup succeeded and should be consumed.
    protected abstract bool TryApply(Collider player);
}