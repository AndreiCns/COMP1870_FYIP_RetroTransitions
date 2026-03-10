using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SecretWall : MonoBehaviour, IInteractable
{
    [Header("Wall")]
    [SerializeField] private Transform wallVisual;

    [Header("Motion")]
    [SerializeField] private float moveDownDistance = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool snapOpenInstantly = false;

    [Header("Behaviour")]
    [SerializeField] private bool disableColliderOnOpen = true;
    [SerializeField] private bool onlyUnlockOnce = true;

    [Header("Popup")]
    [SerializeField] private WorldPopupUI popupUI;
    [SerializeField] private string popupMessage = "Secret unlocked";
    [SerializeField] private float popupDuration = 2.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField, Range(0f, 1f)] private float openVolume01 = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.98f, 1.02f);

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;

    private bool isOpen;
    private bool isMoving;

    private Collider wallCollider;

    private void Awake()
    {
        wallCollider = GetComponent<Collider>();

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (wallVisual == null)
        {
            Debug.LogError("[SecretWall] Assign wallVisual.", this);
            enabled = false;
            return;
        }

        closedLocalPos = wallVisual.localPosition;
        RecalculateOpenPosition();
    }

    private void OnValidate()
    {
        if (wallVisual == null)
            return;

        closedLocalPos = wallVisual.localPosition;
        RecalculateOpenPosition();
    }

    private void RecalculateOpenPosition()
    {
        openLocalPos = closedLocalPos + Vector3.down * moveDownDistance;
    }

    public void Interact(GameObject interactor)
    {
        if (isMoving)
            return;

        if (onlyUnlockOnce && isOpen)
            return;

        OpenSecret();
    }

    private void OpenSecret()
    {
        RecalculateOpenPosition();
        PlayOpenSfx();

        if (popupUI != null)
            popupUI.ShowMessage(popupMessage, popupDuration);

        if (snapOpenInstantly)
        {
            wallVisual.localPosition = openLocalPos;
            FinishOpen();
            return;
        }

        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving)
            return;

        wallVisual.localPosition = Vector3.MoveTowards(
            wallVisual.localPosition,
            openLocalPos,
            moveSpeed * Time.deltaTime);

        bool reached = (wallVisual.localPosition - openLocalPos).sqrMagnitude <= 0.0001f;

        if (reached)
        {
            wallVisual.localPosition = openLocalPos;
            isMoving = false;
            FinishOpen();
        }
    }

    private void FinishOpen()
    {
        isOpen = true;
        isMoving = false;

        if (disableColliderOnOpen && wallCollider != null)
            wallCollider.enabled = false;

        if (verboseLogs)
            Debug.Log("[SecretWall] Secret opened.", this);
    }

    private void PlayOpenSfx()
    {
        if (sfxSource == null || openClip == null)
            return;

        sfxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        sfxSource.PlayOneShot(openClip, openVolume01);
    }
}