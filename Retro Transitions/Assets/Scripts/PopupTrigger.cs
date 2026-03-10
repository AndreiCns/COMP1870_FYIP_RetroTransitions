using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    [Header("Style Transition")]
    [SerializeField] private bool triggerStyleSwap;
    [SerializeField] private StyleSwapManager styleSwapManager;
    [SerializeField] private StyleState targetStyle = StyleState.Retro;
    [SerializeField] private float styleSwapDelay = 2f;
    [SerializeField] private bool cancelSwapIfPlayerLeaves;

    [Header("Scene Flow")]
    [SerializeField] private bool restartSceneAfterPopup;
    [SerializeField] private float restartDelay = 3f;

    private bool hasTriggered;
    private bool playerInside;
    private Coroutine delayedSwapRoutine;
    private Coroutine restartRoutine;

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (popupUI == null)
        {
            Debug.LogError("[PopupTrigger] Assign popupUI.", this);
            enabled = false;
            return;
        }

        if (triggerStyleSwap && styleSwapManager == null)
        {
            Debug.LogError("[PopupTrigger] Style swap is enabled but no StyleSwapManager is assigned.", this);
            enabled = false;
            return;
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

        playerInside = true;
        FireTrigger();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInside = false;

        // Optional cancel so the swap only happens if the player stays in the volume.
        if (!cancelSwapIfPlayerLeaves)
            return;

        if (delayedSwapRoutine != null)
        {
            StopCoroutine(delayedSwapRoutine);
            delayedSwapRoutine = null;
        }
    }

    public void TriggerPopup()
    {
        if (hasTriggered && triggerOnce)
            return;

        FireTrigger();
    }

    private void FireTrigger()
    {
        popupUI.ShowMessage(message, duration);

        // Keep trigger in charge of timing, manager in charge of swapping.
        if (triggerStyleSwap)
        {
            if (delayedSwapRoutine != null)
                StopCoroutine(delayedSwapRoutine);

            delayedSwapRoutine = StartCoroutine(DelayedStyleSwap());
        }

        // End-of-level flow can reuse the same popup trigger.
        if (restartSceneAfterPopup)
        {
            if (restartRoutine != null)
                StopCoroutine(restartRoutine);

            restartRoutine = StartCoroutine(RestartSceneAfterDelay());
        }

        hasTriggered = true;
    }

    private IEnumerator DelayedStyleSwap()
    {
        yield return new WaitForSeconds(styleSwapDelay);

        if (cancelSwapIfPlayerLeaves && !playerInside)
        {
            delayedSwapRoutine = null;
            yield break;
        }

        styleSwapManager.ForceStyle(targetStyle, "PopupTrigger");
        delayedSwapRoutine = null;
    }

    private IEnumerator RestartSceneAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}