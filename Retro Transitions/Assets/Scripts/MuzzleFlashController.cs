using UnityEngine;

public class MuzzleFlashController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform muzzle;

    [Header("Modern")]
    [SerializeField] private ParticleSystem modernFlash;
    [SerializeField] private ParticleSystem modernSmoke;

    [Header("Retro")]
    [SerializeField] private SpriteRenderer retroSpritePrefab;
    [SerializeField] private float retroLife = 0.06f;
    [SerializeField] private float retroScale = 1.0f;
    [SerializeField] private bool randomFlipX = true;

    [Header("Style")]
    [SerializeField] private StyleSwapEvent styleSwapEvent;   //  was VisualStyle enum
    [SerializeField] private StyleSwapManager styleSwapManager; // for OnEnable sync

    [Header("Debug")]
    [SerializeField] private bool logWarnings = true;

    private StyleState currentStyle = StyleState.Modern;

    //Lifecycle

    private void OnEnable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap += OnStyleSwap;

        // Sync immediately in case this object activates after the initial broadcast
        if (styleSwapManager != null)
            OnStyleSwap(styleSwapManager.CurrentState);
    }

    private void OnDisable()
    {
        if (styleSwapEvent != null)
            styleSwapEvent.OnStyleSwap -= OnStyleSwap;
    }

    private void OnStyleSwap(StyleState state)
    {
        currentStyle = state;
        if (currentStyle == StyleState.Retro)
            StopModernVFX();
    }

    // Public API

    public void Play()
    {
        if (muzzle == null)
        {
            if (logWarnings) Debug.LogWarning($"{name}: Missing muzzle reference.", this);
            return;
        }

        if (currentStyle == StyleState.Modern)
            PlayModern();
        else
        {
            StopModernVFX();
            PlayRetro();
        }
    }

    // Private

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
            if (logWarnings) Debug.LogWarning($"{name}: Retro sprite not assigned.", this);
            return;
        }

        SpriteRenderer sr = Instantiate(retroSpritePrefab, muzzle);
        sr.transform.localPosition = Vector3.forward * 0.05f;
        sr.transform.localRotation = Quaternion.identity;
        sr.transform.localScale = Vector3.one * retroScale;
        if (randomFlipX) sr.flipX = Random.value > 0.5f;
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