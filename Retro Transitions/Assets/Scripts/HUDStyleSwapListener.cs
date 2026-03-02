using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDStyleSwapListener : MonoBehaviour
{
    [Serializable]
    private struct ImageSwap
    {
        [SerializeField] private Image target;
        [SerializeField] private Sprite modernSprite;
        [SerializeField] private Sprite retroSprite;

        public void Apply(StyleState state)
        {
            if (target == null) return;

            Sprite s = state == StyleState.Modern ? modernSprite : retroSprite;
            if (s != null && target.sprite != s)
                target.sprite = s;
        }
    }

    [Serializable]
    private struct FaceSwap
    {
        [Header("Modern")]
        [SerializeField] private Sprite modernNeutral;
        [SerializeField] private Sprite modernHurt;
        [SerializeField] private Sprite modernCritical;
        [SerializeField] private Sprite modernCriticalHurt;
        [SerializeField] private Sprite modernDead;

        [Header("Retro")]
        [SerializeField] private Sprite retroNeutral;
        [SerializeField] private Sprite retroHurt;
        [SerializeField] private Sprite retroCritical;
        [SerializeField] private Sprite retroCriticalHurt;
        [SerializeField] private Sprite retroDead;

        public void Apply(StyleState state, HUDFaceController face)
        {
            if (face == null) return;

            if (state == StyleState.Modern)
                face.SetFaceSprites(modernNeutral, modernHurt, modernCritical, modernCriticalHurt, modernDead);
            else
                face.SetFaceSprites(retroNeutral, retroHurt, retroCritical, retroCriticalHurt, retroDead);
        }
    }

    [Header("Event")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Panel BG Swaps (per Image)")]
    [SerializeField] private ImageSwap[] panelBgSwaps;

    [Header("Fonts (global)")]
    [SerializeField] private TMP_FontAsset modernFont;
    [SerializeField] private TMP_FontAsset retroFont;
    [SerializeField] private TMP_Text[] allTexts;

    [Header("Face Swaps")]
    [SerializeField] private HUDFaceController faceController;
    [SerializeField] private FaceSwap faceSwap;

    [Header("Optional - Crosshair")]
    [SerializeField] private Image crosshair;
    [SerializeField] private Sprite modernCrosshair;
    [SerializeField] private Sprite retroCrosshair;

    private void Awake()
    {
        if (styleSwapEvent == null)
        {
            Debug.LogError($"{nameof(HUDStyleSwapListener)} missing StyleSwapEvent.", this);
            enabled = false;
            return;
        }

        // Face swap is optional, but if it exists we prefer a local reference.
        if (faceController == null)
            faceController = GetComponentInChildren<HUDFaceController>(true);
    }

    private void OnEnable()
    {
        styleSwapEvent.OnStyleSwap += OnStyleSwap;
    }

    private void OnDisable()
    {
        styleSwapEvent.OnStyleSwap -= OnStyleSwap;
    }

    private void Start()
    {
        // Sync visuals to the project’s default style at boot.
        Apply(StyleState.Modern);
    }

    private void OnStyleSwap(StyleState state)
    {
        Apply(state);
    }

    private void Apply(StyleState state)
    {
        ApplyPanelBgs(state);
        ApplyFonts(state);

        // Face + crosshair are purely cosmetic; safe to skip if unassigned.
        faceSwap.Apply(state, faceController);
        ApplyCrosshair(state);
    }

    private void ApplyPanelBgs(StyleState state)
    {
        if (panelBgSwaps == null) return;

        for (int i = 0; i < panelBgSwaps.Length; i++)
            panelBgSwaps[i].Apply(state);
    }

    private void ApplyFonts(StyleState state)
    {
        if (allTexts == null) return;

        TMP_FontAsset f = state == StyleState.Modern ? modernFont : retroFont;
        if (f == null) return;

        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text t = allTexts[i];
            if (t == null) continue;

            if (t.font != f)
                t.font = f;
        }
    }

    private void ApplyCrosshair(StyleState state)
    {
        if (crosshair == null) return;

        Sprite s = state == StyleState.Modern ? modernCrosshair : retroCrosshair;
        if (s != null && crosshair.sprite != s)
            crosshair.sprite = s;
    }
}