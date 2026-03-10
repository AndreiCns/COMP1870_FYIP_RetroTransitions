using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PopupTrigger : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private WorldPopupUI popupUI;
    [SerializeField, TextArea(2, 4)] private string message = "Press E to interact";
    [SerializeField] private float duration = 2.5f;

    [Header("Trigger Rules")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (popupUI == null)
        {
            Debug.LogError("[PopupTrigger] Assign popupUI.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter)
            return;

        if (hasTriggered && triggerOnce)
            return;

        if (!other.CompareTag(playerTag))
            return;

        popupUI.ShowMessage(message, duration);
        hasTriggered = true;
    }

    public void TriggerPopup()
    {
        if (hasTriggered && triggerOnce)
            return;

        popupUI.ShowMessage(message, duration);
        hasTriggered = true;
    }
}