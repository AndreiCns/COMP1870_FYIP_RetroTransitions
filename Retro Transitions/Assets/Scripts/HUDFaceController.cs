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
 
    private bool isHurtFlashing;

    private void Awake()
    {
        if (faceImage == null)
            faceImage = GetComponentInChildren<UnityEngine.UI.Image>();

        if (health == null)
            health = FindObjectOfType<Health>();

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

        // While flashing hurt, don't let Update override it.
        if (isHurtFlashing)
            return;

        if (health.IsDead)
        {
            SetFace(FaceState.Dead);
            return;
        }

        float hp01 = health.Max <= 0f ? 0f : (health.Current / health.Max);

        if (hp01 <= criticalThreshold01)
            SetFace(FaceState.Critical);
        else
            SetFace(FaceState.Neutral);
    }

    private void OnDamaged(float amount)
    {
        if (health == null || health.IsDead)
        {
            SetFace(FaceState.Dead);
            return;
        }

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HurtFlash());
    }

    private IEnumerator HurtFlash()
    {
        isHurtFlashing = true;
        SetFace(FaceState.Hurt);

        yield return new WaitForSecondsRealtime(hurtFlashTime);

        isHurtFlashing = false;
        flashRoutine = null;
    }

    private void SetFace(FaceState state)
    {
        if (faceImage == null) return;

        Sprite target = state switch
        {
            FaceState.Neutral => neutral,
            FaceState.Hurt => hurt != null ? hurt : neutral,
            FaceState.Critical => critical != null ? critical : neutral,
            FaceState.Dead => dead != null ? dead : critical != null ? critical : neutral,
            _ => neutral
        };

        if (target != null && faceImage.sprite != target)
            faceImage.sprite = target;
    }
}
