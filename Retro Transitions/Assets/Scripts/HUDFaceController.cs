using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDFaceController : MonoBehaviour
{
    public enum FaceState
    {
        Neutral,
        Hurt,
        Critical,
        CriticalHurt,
        Dead
    }

    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private Image faceImage;

    [Header("Sprites")]
    [SerializeField] private Sprite neutral;
    [SerializeField] private Sprite hurt;
    [SerializeField] private Sprite critical;
    [SerializeField] private Sprite criticalHurt;
    [SerializeField] private Sprite dead;

    [Header("Tuning")]
    [Tooltip("Below this HP%, the baseline face becomes Critical.")]
    [SerializeField, Range(0.05f, 0.95f)] private float criticalThreshold01 = 0.4f;
    [SerializeField] private float hurtFlashTime = 0.25f;

    private Coroutine flashRoutine;
    private bool isHurtFlashing;

    private void Awake()
    {
        if (faceImage == null)
            faceImage = GetComponentInChildren<Image>();

        if (health == null)
            health = FindFirstObjectByType<Health>();

        if (faceImage == null)
        {
            Debug.LogError("[HUDFace] faceImage missing.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged.AddListener(OnDamaged);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged.RemoveListener(OnDamaged);
    }

    private void Update()
    {
        if (health == null) return;

        if (isHurtFlashing)
            return;

        SetFace(GetCurrentFaceState());
    }

    private void OnDamaged(float amount)
    {
        if (health == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HurtFlash());
    }

    private IEnumerator HurtFlash()
    {
        isHurtFlashing = true;

        if (health.IsDead)
        {
            SetFace(FaceState.Dead);
        }
        else
        {
            float hp01 = GetHp01();
            bool isCritical = hp01 <= criticalThreshold01;

            SetFace(isCritical ? FaceState.CriticalHurt : FaceState.Hurt);
        }

        yield return new WaitForSecondsRealtime(hurtFlashTime);

        isHurtFlashing = false;
        flashRoutine = null;

        // Snap back to baseline immediately after flash ends.
        SetFace(GetCurrentFaceState());
    }

    public void SetFaceSprites(
        Sprite neutralSprite,
        Sprite hurtSprite,
        Sprite criticalSprite,
        Sprite criticalHurtSprite,
        Sprite deadSprite)
    {
        neutral = neutralSprite;
        hurt = hurtSprite;
        critical = criticalSprite;
        criticalHurt = criticalHurtSprite;
        dead = deadSprite;

        if (!isHurtFlashing)
            SetFace(GetCurrentFaceState());
    }

    private FaceState GetCurrentFaceState()
    {
        if (health == null) return FaceState.Neutral;
        if (health.IsDead) return FaceState.Dead;

        float hp01 = GetHp01();
        return (hp01 <= criticalThreshold01) ? FaceState.Critical : FaceState.Neutral;
    }

    private float GetHp01()
    {
        return health.Max <= 0f ? 0f : (health.Current / health.Max);
    }

    private void SetFace(FaceState state)
    {
        if (faceImage == null) return;

        Sprite target = state switch
        {
            FaceState.Neutral => neutral,
            FaceState.Hurt => hurt != null ? hurt : neutral,
            FaceState.Critical => critical != null ? critical : neutral,
            FaceState.CriticalHurt => criticalHurt != null ? criticalHurt : (hurt != null ? hurt : critical != null ? critical : neutral),
            FaceState.Dead => dead != null ? dead : (critical != null ? critical : neutral),
            _ => neutral
        };

        if (target != null && faceImage.sprite != target)
            faceImage.sprite = target;
    }
}