using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Lock")]
    [SerializeField] private KeyType requiredKey;

    [Header("Door Visuals")]
    [SerializeField] private Transform modernVisual;
    [SerializeField] private Transform retroVisual;

    [Header("Audio (door SFX)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip lockedClip;
    [SerializeField] private AudioClip openClip;
    [SerializeField, Range(0f, 1f)] private float lockedVolume01 = 1f;
    [SerializeField, Range(0f, 1f)] private float openVolume01 = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.98f, 1.02f);
    [SerializeField] private float lockedMinInterval = 0.25f;

    private float nextLockedTime;

    [Header("Door Motion")]
    [SerializeField] private float openHeight = 3f;
    [SerializeField] private float openSpeedModern = 2f;

    [Header("Open Behaviour")]
    [SerializeField] private bool disableColliderOnOpen = true;

    [Header("Style (optional)")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private Vector3 modernClosedPos;
    private Vector3 retroClosedPos;
    private Vector3 modernOpenPos;
    private Vector3 retroOpenPos;

    private bool isOpen;
    private bool isMoving;

    private Collider doorCollider;

    private StyleSwapManager styleSwapManager;
    private StyleState currentStyle = StyleState.Modern;

    private void Awake()
    {
        doorCollider = GetComponent<Collider>();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
        {
            Debug.LogWarning("[LockedDoor] No AudioSource found. Door SFX will be silent.", this);
        }
        else
        {
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 1f; // 3D door sound
        }

        if (modernVisual == null || retroVisual == null)
        {
            Debug.LogError("[LockedDoor] Assign modernVisual + retroVisual.", this);
            enabled = false;
            return;
        }

        modernClosedPos = modernVisual.position;
        retroClosedPos = retroVisual.position;

        modernOpenPos = modernClosedPos + Vector3.up * openHeight;
        retroOpenPos = retroClosedPos + Vector3.up * openHeight;

        styleSwapManager = FindFirstObjectByType<StyleSwapManager>();
    }

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleChanged;

        if (styleSwapManager != null)
            OnStyleChanged(styleSwapManager.CurrentState);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleChanged;
    }

    private void OnStyleChanged(StyleState newState)
    {
        currentStyle = newState;

        // If opened already, keep both visuals parked open so toggling style stays coherent.
        if (isOpen)
        {
            modernVisual.position = modernOpenPos;
            retroVisual.position = retroOpenPos;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (isOpen || isMoving) return;

        PlayerCombatState state = interactor.GetComponent<PlayerCombatState>();
        if (state == null)
        {
            if (verboseLogs) Debug.LogWarning("[LockedDoor] Interactor missing PlayerCombatState.", this);
            return;
        }

        if (!HasRequiredKey(state))
        {
            if (verboseLogs) Debug.Log($"[LockedDoor] Locked. Missing {requiredKey} key.", this);
            PlayLockedSfx();
            return;
        }

        Open();
    }

    private bool HasRequiredKey(PlayerCombatState state)
    {
        switch (requiredKey)
        {
            case KeyType.Blue: return state.HasBlueKey;
            case KeyType.Yellow: return state.HasYellowKey;
            case KeyType.Red: return state.HasRedKey;
        }

        return false;
    }

    private void Open()
    {
        PlayOpenSfx();
        // Same rule (key required), different presentation per style.
        if (currentStyle == StyleState.Retro)
        {
            modernVisual.position = modernOpenPos;
            retroVisual.position = retroOpenPos;
            FinishOpen();
            return;
        }

        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        modernVisual.position = Vector3.MoveTowards(modernVisual.position, modernOpenPos, openSpeedModern * Time.deltaTime);
        retroVisual.position = Vector3.MoveTowards(retroVisual.position, retroOpenPos, openSpeedModern * Time.deltaTime);

        if ((modernVisual.position - modernOpenPos).sqrMagnitude <= 0.0001f)
        {
            modernVisual.position = modernOpenPos;
            retroVisual.position = retroOpenPos;

            isMoving = false;
            FinishOpen();
        }
    }

    private void FinishOpen()
    {
        isOpen = true;
        isMoving = false;

        // Door stops blocking once opened.
        if (disableColliderOnOpen && doorCollider != null)
            doorCollider.enabled = false;
    }

    private void PlayLockedSfx()
    {
        if (sfxSource == null || lockedClip == null)
            return;

        if (Time.time < nextLockedTime)
            return;

        nextLockedTime = Time.time + lockedMinInterval;

        sfxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        sfxSource.PlayOneShot(lockedClip, lockedVolume01);
    }

    private void PlayOpenSfx()
    {
        if (sfxSource == null || openClip == null)
            return;

        sfxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        sfxSource.PlayOneShot(openClip, openVolume01);
    }
}