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
    [SerializeField] private float retroLife = 0.06f;   // very short pop
    [SerializeField] private float retroScale = 1.0f;
    [SerializeField] private bool randomFlipX = true;

    [Header("Style")]
    [SerializeField] private VisualStyle currentStyle = VisualStyle.Modern;

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    public void SetStyle(VisualStyle style)
    {
        currentStyle = style;

        // Make sure modern particles aren't lingering when switching to retro
        if (currentStyle == VisualStyle.Retro)
            StopModernVFX();
    }

    // Called from weapon fire (animation event)
    public void Play()
    {
        if (muzzle == null)
        {
            if (logWarnings)
                Debug.LogWarning($"{name}: Missing muzzle reference.", this);
            return;
        }

        if (currentStyle == VisualStyle.Modern)
            PlayModern();
        else
        {
            StopModernVFX();
            PlayRetro();
        }
    }

    private void PlayModern()
    {
        // Re-align to muzzle each shot (weapon moves with recoil/bob)
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
                Debug.LogWarning($"{name}: Retro sprite not assigned.", this);
            return;
        }

        // Spawn as child so it follows the weapon naturally
        SpriteRenderer sr = Instantiate(retroSpritePrefab, muzzle);

        sr.transform.localPosition = Vector3.forward * 0.05f;
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
