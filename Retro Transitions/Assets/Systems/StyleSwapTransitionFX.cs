using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class StyleSwapTransitionFX : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Image overlayImage;
    [SerializeField] private CanvasGroup overlayGroup;

    [Header("Glitch Sprites (alternate each frame)")]
    [SerializeField] private Sprite glitchSpriteA;
    [SerializeField] private Sprite glitchSpriteB;

    [Header("Freeze (gameplay pause)")]
    [SerializeField] private int freezeFrames = 3;

    [Tooltip("Gameplay pause manager (pauses registered systems during transition).")]
    [SerializeField] private GameplayPauseManager pauseManager;

    [Header("Timing")]
    [SerializeField] private float preSwapHoldRealtime = 0.03f;
    [SerializeField] private float postSwapHoldRealtime = 0.05f;

    [Header("CRT Snap Tuning")]
    [SerializeField] private float rollPixels = 120f;
    [SerializeField] private float tearPixels = 40f;
    [SerializeField] private float settleJitterPixels = 6f;
    [SerializeField] private float flashAlpha = 1f;

    [Header("Transition SFX (own mixer group)")]
    [SerializeField] private AudioSource transitionSource;

    [SerializeField] private AudioClip modernToRetroClip;
    [SerializeField] private AudioClip retroToModernClip;

    [SerializeField, Range(0f, 1f)] private float transitionVolume01 = 1f;
    [SerializeField] private Vector2 transitionPitchRange = new Vector2(0.98f, 1.02f);

    private bool isPlaying;
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        // Transition still works without pause manager, but gameplay won't freeze.
        if (pauseManager == null)
            Debug.LogWarning("[StyleSwapTransitionFX] pauseManager is NULL (transition will not pause gameplay).", this);

        // Allow either Image or CanvasGroup to define the overlay root.
        if (overlayRoot == null && overlayImage != null)
            overlayRoot = overlayImage.gameObject;

        if (overlayRoot == null && overlayGroup != null)
            overlayRoot = overlayGroup.gameObject;

        // FX cannot run without a valid overlay.
        if (overlayRoot == null)
        {
            Debug.LogError("[StyleSwapTransitionFX] Missing overlayRoot.", this);
            enabled = false;
            return;
        }
        if (transitionSource == null)
            transitionSource = GetComponent<AudioSource>();

        if (transitionSource != null)
        {
            transitionSource.playOnAwake = false;
            transitionSource.loop = false;
        }
        else
        {
            Debug.LogWarning("[StyleSwapTransitionFX] No transitionSource set (transition SFX will be silent).", this);
        }

        overlayRoot.SetActive(false);
    }

    public void Play(StyleState targetStyle, Action onMidpoint, Action onComplete = null)
    {
        if (isPlaying)
            return;

        StartCoroutine(PlayRoutine(targetStyle, onMidpoint, onComplete));
    }

    private IEnumerator PlayRoutine(StyleState targetStyle, Action onMidpoint, Action onComplete)
    {
        isPlaying = true;

        overlayRoot.SetActive(true);
        SetOverlayAlpha(0f);

        if (preSwapHoldRealtime > 0f)
            yield return new WaitForSecondsRealtime(preSwapHoldRealtime);

        PlayTransitionSfx(targetStyle);

        // Pause registered gameplay systems (no Time.timeScale hack).
        pauseManager?.SetPaused(true);

        try
        {
            for (int i = 0; i < freezeFrames; i++)
            {
                ApplyCrtSnap(i);
                FlipGlitchSprite(i);
                yield return null;
            }

            // Manager commits style change here.
            onMidpoint?.Invoke();

            if (postSwapHoldRealtime > 0f)
                yield return new WaitForSecondsRealtime(postSwapHoldRealtime);
        }
        finally
        {
            pauseManager?.SetPaused(false);

            ResetOverlayTransform();
            SetOverlayAlpha(0f);
            overlayRoot.SetActive(false);

            onComplete?.Invoke();
            isPlaying = false;
        }
    }

    private void FlipGlitchSprite(int frameIndex)
    {
        if (overlayImage == null)
            return;

        if (glitchSpriteA == null || glitchSpriteB == null)
            return;

        // Simple alternating flicker.
        overlayImage.sprite = (frameIndex % 2 == 0)
            ? glitchSpriteA
            : glitchSpriteB;
    }

    private void ApplyCrtSnap(int frameIndex)
    {
        RectTransform rt = overlayImage != null ? overlayImage.rectTransform : null;
        if (rt == null)
            return;

        // Frame pattern: roll -> tear -> settle.
        switch (frameIndex)
        {
            case 0:
                rt.anchoredPosition = new Vector2(0f, -rollPixels);
                SetOverlayAlpha(flashAlpha);
                break;

            case 1:
                rt.anchoredPosition = new Vector2(tearPixels, rollPixels * 0.35f);
                SetOverlayAlpha(flashAlpha * 0.75f);
                break;

            default:
                float x = UnityEngine.Random.Range(-settleJitterPixels, settleJitterPixels);
                float y = UnityEngine.Random.Range(-settleJitterPixels, settleJitterPixels);
                rt.anchoredPosition = new Vector2(x, y);
                SetOverlayAlpha(0.85f);
                break;
        }
    }

    private void SetOverlayAlpha(float a)
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = a;
            return;
        }

        if (overlayImage != null)
        {
            Color c = overlayImage.color;
            c.a = a;
            overlayImage.color = c;
        }
    }

    private void ResetOverlayTransform()
    {
        RectTransform rt = overlayImage != null ? overlayImage.rectTransform : null;
        if (rt == null)
            return;

        rt.anchoredPosition = Vector2.zero;
    }

    private void PlayTransitionSfx(StyleState targetStyle)
    {
        if (transitionSource == null)
            return;

        AudioClip clip = targetStyle == StyleState.Retro ? modernToRetroClip : retroToModernClip;
        if (clip == null)
            return;

        transitionSource.pitch = UnityEngine.Random.Range(transitionPitchRange.x, transitionPitchRange.y);
        transitionSource.PlayOneShot(clip, transitionVolume01);
    }

}