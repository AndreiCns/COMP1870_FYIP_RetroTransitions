using System.Collections;
using UnityEngine;

public class HUDFaceController : MonoBehaviour
{
    public enum FaceState
    {
        Neutral,
        Hurt,
        Critical,
        Dead
    }

    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private UnityEngine.UI.Image faceImage;

    [Header("Sprites")]
    [SerializeField] private Sprite neutral;
    [SerializeField] private Sprite hurt;
    [SerializeField] private Sprite critical;
    [SerializeField] private Sprite dead;

    [Header("Tuning")]
    [SerializeField] private float criticalThreshold01 = 0.2f;
    [SerializeField] private float hurtFlashTime = 0.25f;

    private Coroutine flashRoutine;

    // While this is true, Update won't overwrite the hurt sprite.
    private bool isHurtFlashing;

    private void Awake()
    {
        if (faceImage == null)
            faceImage = GetComponentInChildren<UnityEngine.UI.Image>();

        
        if (health == null)
            health = FindFirstObjectByType<Health>();


        // Face image is required; without it this component can't do anything useful.
        if (faceImage == null)
        {
            Debug.LogError("[HUDFace] faceImage missing.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // React to damage so the hurt flash feels immediate.
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

        // Hurt flash is a temporary override.
        if (isHurtFlashing)
            return;

        if (health.IsDead)
        {
            SetFace(FaceState.Dead);
            return;
        }

        float hp01 = health.Max <= 0f ? 0f : (health.Current / health.Max);

        // Doom-style: only show "critical" when low, otherwise neutral.
        if (hp01 <= criticalThreshold01)
            SetFace(FaceState.Critical);
        else
            SetFace(FaceState.Neutral);
    }

    private void OnDamaged(float amount)
    {
        // If we died from the hit, force dead face immediately.
        if (health == null || health.IsDead)
        {
            SetFace(FaceState.Dead);
            return;
        }

        // Restart flash so rapid hits still feel responsive.
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HurtFlash());
    }

    private IEnumerator HurtFlash()
    {
        isHurtFlashing = true;
        SetFace(FaceState.Hurt);

        // Realtime so pause/slowmo doesn't break HUD feedback.
        yield return new WaitForSecondsRealtime(hurtFlashTime);

        isHurtFlashing = false;
        flashRoutine = null;
    }

    private void SetFace(FaceState state)
    {
        if (faceImage == null) return;

        // Graceful fallbacks so missing sprites don't hard-break the HUD.
        Sprite target = state switch
        {
            FaceState.Neutral => neutral,
            FaceState.Hurt => hurt != null ? hurt : neutral,
            FaceState.Critical => critical != null ? critical : neutral,
            FaceState.Dead => dead != null ? dead : (critical != null ? critical : neutral),
            _ => neutral
        };

        if (target != null && faceImage.sprite != target)
            faceImage.sprite = target;
    }
}
