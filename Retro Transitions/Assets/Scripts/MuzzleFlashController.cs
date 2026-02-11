using UnityEngine;

public enum VisualStyle { Modern, Retro }

public class MuzzleFlashController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;

    [Header("Modern")]
    [SerializeField] private ParticleSystem modernFlash;
    [SerializeField] private ParticleSystem modernSmoke;

    [Header("Retro")]
    [SerializeField] private SpriteRenderer retroSpritePrefab;
    [SerializeField] private float retroLife = 0.06f;     // short, punchy
    [SerializeField] private float retroScale = 1.0f;
    [SerializeField] private bool randomFlipX = true;

    [Header("Style")]
    [SerializeField] private VisualStyle currentStyle = VisualStyle.Modern;

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    public void SetStyle(VisualStyle style)
    {
        currentStyle = style;

        // If we switch to Retro, ensure modern particles are not lingering
        if (currentStyle == VisualStyle.Retro)
            StopModernVFX();
    }

    // Call this on fire (anim event)
    public void Play()
    {
        if (muzzle == null)
        {
            if (logWarnings) Debug.LogWarning($"{name}: MuzzleFlashController missing muzzle reference.", this);
            return;
        }

        if (currentStyle == VisualStyle.Modern)
        {
            PlayModern();
        }
        else
        {
            // Defensive: ensure modern systems never show in retro mode
            StopModernVFX();
            PlayRetro();
        }
    }

    private void PlayModern()
    {
        if (modernFlash != null)
        {
            modernFlash.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
            modernFlash.Play(true);
        }

        if (modernSmoke != null)
        {
            modernSmoke.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
            modernSmoke.Play(true);
        }
    }

    private void PlayRetro()
    {
        if (retroSpritePrefab == null)
        {
            if (logWarnings)
                Debug.LogWarning($"{name}: Retro style selected but retroSpritePrefab is not assigned.", this);
            return;
        }

        // Spawn as CHILD of muzzle so it follows weapon
        SpriteRenderer sr = Instantiate(retroSpritePrefab, muzzle);

        sr.transform.localPosition = Vector3.zero;
        sr.transform.localPosition += Vector3.forward * 0.05f;

        sr.transform.localRotation = Quaternion.identity;

        sr.transform.localScale = Vector3.one * retroScale;

        if (randomFlipX)
            sr.flipX = Random.value > 0.5f;

        Destroy(sr.gameObject, retroLife);
    }


    private void StopModernVFX()
    {
        if (modernFlash != null)
            modernFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (modernSmoke != null)
            modernSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
