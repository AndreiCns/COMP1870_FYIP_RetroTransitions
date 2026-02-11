using UnityEngine;

public enum VisualStyle { Modern, Retro }

public class MuzzleFlashController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private ParticleSystem modernSmoke;
 

    [Header("Modern")]
    [SerializeField] private ParticleSystem modernFlash;

    [Header("Retro")]
    [SerializeField] private SpriteRenderer retroSpritePrefab;
    [SerializeField] private float retroLife = 0.06f;       // short, punchy
    [SerializeField] private float retroScale = 1.0f;
    [SerializeField] private bool randomFlipX = true;

    [Header("Style")]
    [SerializeField] private VisualStyle currentStyle = VisualStyle.Modern;

    public void SetStyle(VisualStyle style) => currentStyle = style;

    // Call this on fire (ideally from the same anim event as projectile)
    public void Play()
    {
        if (muzzle == null) return;

        if (currentStyle == VisualStyle.Modern)
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

            return;
        }

        // Retro sprite flash
        if (retroSpritePrefab == null) return;

        SpriteRenderer sr = Instantiate(retroSpritePrefab, muzzle.position, muzzle.rotation);
        sr.transform.localScale = Vector3.one * retroScale;

        if (randomFlipX)
            sr.flipX = Random.value > 0.5f;

        Destroy(sr.gameObject, retroLife);
    }

}
