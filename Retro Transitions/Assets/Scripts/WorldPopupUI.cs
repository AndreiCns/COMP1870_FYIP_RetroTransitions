using System.Collections;
using TMPro;
using UnityEngine;

public class WorldPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Timing")]
    [SerializeField] private float defaultDuration = 2.5f;
    [SerializeField] private bool hideOnStart = true;

    [Header("Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDuration = 0.2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (canvasGroup == null || messageText == null)
        {
            Debug.LogError("[WorldPopupUI] Missing required references.", this);
            enabled = false;
            return;
        }

        if (hideOnStart)
            HideImmediate();
    }

    public void ShowMessage(string message)
    {
        ShowMessage(message, defaultDuration);
    }

    public void ShowMessage(string message, float duration)
    {
        if (!enabled)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(message, duration));
    }

    public void HideImmediate()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator ShowRoutine(string message, float duration)
    {
        messageText.text = message;
        canvasGroup.gameObject.SetActive(true);

        if (useFade)
        {
            yield return FadeCanvas(canvasGroup.alpha, 1f);
        }
        else
        {
            canvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(duration);

        if (useFade)
        {
            yield return FadeCanvas(canvasGroup.alpha, 0f);
        }
        else
        {
            canvasGroup.alpha = 0f;
        }

        canvasGroup.gameObject.SetActive(false);
        currentRoutine = null;
    }

    private IEnumerator FadeCanvas(float from, float to)
    {
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}