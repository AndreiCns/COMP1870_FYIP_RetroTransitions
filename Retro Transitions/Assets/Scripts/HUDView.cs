using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : MonoBehaviour
{
    [Header("Core Text")]
    [SerializeField] private TMP_Text ammoValue;
    [SerializeField] private TMP_Text healthValue;

    [Header("Ammo Rows (4)")]
    [SerializeField] private TMP_Text rowBulletLabel;
    [SerializeField] private TMP_Text rowBulletValue;

    [SerializeField] private TMP_Text rowShellLabel;
    [SerializeField] private TMP_Text rowShellValue;

    [SerializeField] private TMP_Text rowRocketLabel;
    [SerializeField] private TMP_Text rowRocketValue;

    [SerializeField] private TMP_Text rowPlasmaLabel;
    [SerializeField] private TMP_Text rowPlasmaValue;

    [Header("Upgrades (4 slots)")]
    [SerializeField] private Image[] upgradeSlotBgs = new Image[4];

    [Header("Keys")]
    [SerializeField] private Image keyBlue;
    [SerializeField] private Image keyYellow;
    [SerializeField] private Image keyRed;

    [Header("Crosshair")]
    [SerializeField] private Image crosshair;

    // --- Core ---

    public void SetAmmo(int value) => SetText(ammoValue, value.ToString());

    public void SetHealthPercent(int value)
    {
        // Clamp defensively so HUD never shows weird negatives / overflow.
        SetText(healthValue, Mathf.Clamp(value, 0, 999).ToString());
    }

    // --- Ammo panel ---

    public void SetAmmoRows(string bullet, string shell, string rocket, string plasma)
    {
        // UI layer only: caller owns formatting (e.g., "12/50").
        SetText(rowBulletValue, bullet);
        SetText(rowShellValue, shell);
        SetText(rowRocketValue, rocket);
        SetText(rowPlasmaValue, plasma);
    }

    public void SetAmmoRowHighlight(
        AmmoType activeType,
        bool bulletUnlocked,
        bool shellUnlocked,
        bool rocketUnlocked,
        bool plasmaUnlocked)
    {
        // Alpha-only highlight so it works for both modern and retro UI skins.
        SetRowAlpha(rowBulletLabel, rowBulletValue, activeType == AmmoType.Bullet, bulletUnlocked);
        SetRowAlpha(rowShellLabel, rowShellValue, activeType == AmmoType.Shell, shellUnlocked);
        SetRowAlpha(rowRocketLabel, rowRocketValue, activeType == AmmoType.Rocket, rocketUnlocked);
        SetRowAlpha(rowPlasmaLabel, rowPlasmaValue, activeType == AmmoType.Plasma, plasmaUnlocked);
    }

    // --- Upgrades / Keys / Crosshair ---

    public void SetUpgradeUnlocked(int slotIndex, bool unlocked)
    {
        if (upgradeSlotBgs == null) return;
        if (slotIndex < 0 || slotIndex >= upgradeSlotBgs.Length) return;

        Image img = upgradeSlotBgs[slotIndex];
        if (img == null) return;

        Color c = img.color;
        c.a = unlocked ? 1f : 0.35f;
        img.color = c;
    }

    public void SetKeys(bool blue, bool yellow, bool red)
    {
        SetIconAlpha(keyBlue, blue);
        SetIconAlpha(keyYellow, yellow);
        SetIconAlpha(keyRed, red);
    }

    public void SetCrosshairVisible(bool visible)
    {
        if (crosshair != null)
            crosshair.enabled = visible;
    }

    // --- Internals ---

    private void SetText(TMP_Text t, string value)
    {
        if (t == null) return;

        // Avoid unnecessary mesh rebuilds if the value hasn’t changed.
        if (t.text == value) return;

        t.text = value;
    }

    private void SetIconAlpha(Image img, bool on)
    {
        if (img == null) return;

        Color c = img.color;
        c.a = on ? 1f : 0.25f;
        img.color = c;
    }

    private void SetRowAlpha(TMP_Text label, TMP_Text value, bool isActive, bool isUnlocked)
    {
        if (label == null || value == null) return;

        float a;

        if (!isUnlocked)
            a = 0.25f; // locked
        else if (isActive)
            a = 1f;    // active
        else
            a = 0.55f; // unlocked but inactive

        SetAlpha(label, a);
        SetAlpha(value, a);
    }

    private void SetAlpha(TMP_Text t, float a)
    {
        if (t == null) return;

        Color c = t.color;
        c.a = a;
        t.color = c;
    }
}
