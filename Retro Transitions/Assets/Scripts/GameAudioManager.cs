using UnityEngine;
using UnityEngine.Audio;

public class GameAudioManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;

    [Header("Player SFX")]
    [Tooltip("One-shot source routed to PlayerSFX mixer group.")]
    [SerializeField] private AudioSource playerSfxSource;

    [SerializeField] private float playerSfxVolume = 1f;

    [Header("Mixer Snapshots")]
    [SerializeField] private AudioMixerSnapshot modernSnapshot;
    [SerializeField] private AudioMixerSnapshot retroSnapshot;
    [SerializeField] private float transitionTime = 0.35f;

    private void Start()
    {
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }

        PlayModern();
    }

    public void PlayModern()
    {
        modernSnapshot?.TransitionTo(transitionTime);
    }

    public void PlayRetro()
    {
        retroSnapshot?.TransitionTo(transitionTime);
    }

    public void PlayGunshot(AmmoTypeConfig cfg)
    {
        if (cfg == null || playerSfxSource == null)
            return;

        if (cfg.gunshotClip == null)
            return;

        Vector2 pitchRange = cfg.gunshotPitch;
        playerSfxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);

        playerSfxSource.PlayOneShot(cfg.gunshotClip, playerSfxVolume);
    }

    public void PlayFootstep(AudioClip clip, float volume01 = 1f, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        if (clip == null || playerSfxSource == null)
            return;

        playerSfxSource.pitch = Random.Range(pitchMin, pitchMax);
        playerSfxSource.PlayOneShot(clip, Mathf.Clamp01(volume01) * playerSfxVolume);
    }

    public void PlayJump(AudioClip clip, float volume01 = 1f)
    {
        if (clip == null || playerSfxSource == null)
            return;

        playerSfxSource.pitch = Random.Range(0.95f, 1.05f);
        playerSfxSource.PlayOneShot(clip, Mathf.Clamp01(volume01) * playerSfxVolume);
    }

    public void PlayLanding(AudioClip clip, float volume01 = 1f)
    {
        if (clip == null || playerSfxSource == null)
            return;

        playerSfxSource.pitch = Random.Range(0.9f, 1.0f);
        playerSfxSource.PlayOneShot(clip, Mathf.Clamp01(volume01) * playerSfxVolume);
    }
}