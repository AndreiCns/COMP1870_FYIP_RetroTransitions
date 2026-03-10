using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Lock")]
    [SerializeField] private KeyType requiredKey;

    [Header("Door")]
    [SerializeField] private Transform doorVisual;

    [Header("Audio (door SFX)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip lockedClip;
    [SerializeField] private AudioClip openClip;
    [SerializeField, Range(0f, 1f)] private float lockedVolume01 = 1f;
    [SerializeField, Range(0f, 1f)] private float openVolume01 = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.98f, 1.02f);
    [SerializeField] private float lockedMinInterval = 0.25f;

    [Header("Door Motion")]
    [SerializeField] private float openHeight = 3f;
    [SerializeField] private float openSpeed = 2f;
    [SerializeField] private bool snapOpenInstantly = false;

    [Header("Open Behaviour")]
    [SerializeField] private bool disableColliderOnOpen = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private float nextLockedTime;

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;

    private bool isOpen;
    private bool isMoving;

    private Collider doorCollider;

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
            sfxSource.spatialBlend = 1f;
        }

        if (doorVisual == null)
        {
            Debug.LogError("[LockedDoor] Assign doorVisual.", this);
            enabled = false;
            return;
        }

        closedLocalPos = doorVisual.localPosition;
        RecalculateOpenPosition();
    }

    private void OnValidate()
    {
        if (doorVisual == null)
            return;

        closedLocalPos = doorVisual.localPosition;
        RecalculateOpenPosition();
    }

    private void RecalculateOpenPosition()
    {
        openLocalPos = closedLocalPos + Vector3.up * openHeight;
    }

    public void Interact(GameObject interactor)
    {
        if (isOpen || isMoving)
            return;

        PlayerCombatState state = interactor.GetComponent<PlayerCombatState>();
        if (state == null)
        {
            if (verboseLogs)
                Debug.LogWarning("[LockedDoor] Interactor missing PlayerCombatState.", this);
            return;
        }

        if (!HasRequiredKey(state))
        {
            if (verboseLogs)
                Debug.Log($"[LockedDoor] Locked. Missing {requiredKey} key.", this);

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
            default: return false;
        }
    }

    private void Open()
    {
        PlayOpenSfx();
        RecalculateOpenPosition();

        if (snapOpenInstantly)
        {
            doorVisual.localPosition = openLocalPos;
            FinishOpen();
            return;
        }

        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving)
            return;

        doorVisual.localPosition = Vector3.MoveTowards(
            doorVisual.localPosition,
            openLocalPos,
            openSpeed * Time.deltaTime);

        bool reached = (doorVisual.localPosition - openLocalPos).sqrMagnitude <= 0.0001f;

        if (reached)
        {
            doorVisual.localPosition = openLocalPos;
            isMoving = false;
            FinishOpen();
        }
    }

    private void FinishOpen()
    {
        isOpen = true;
        isMoving = false;

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