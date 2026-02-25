using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class PickupBase : MonoBehaviour
{
    [Header("Consume")]
    [SerializeField] private bool destroyOnPickup = true;

    [Header("Audio (routed via GameAudioManager)")]
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField] private float pickupVolume01 = 1f;
    [SerializeField] private Vector2 pickupPitchRange = new Vector2(0.98f, 1.02f);

    private Collider col;
    private GameAudioManager audioManager;

    protected virtual void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;

        audioManager = FindFirstObjectByType<GameAudioManager>();
        if (audioManager == null)
            Debug.LogWarning($"{name}: No GameAudioManager found. Pickup SFX will be silent.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (!TryApply(other))
            return;

        if (pickupSfx != null && audioManager != null)
            audioManager.PlaySfxOneShot(pickupSfx, pickupVolume01, pickupPitchRange.x, pickupPitchRange.y);

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