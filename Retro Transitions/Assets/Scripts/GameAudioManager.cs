using UnityEngine;
using UnityEngine.Audio;

public class GameAudioManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;

    [Header("Mixer Snapshots")]
    [Tooltip("Default, clean mix (no retro degradation).")]
    [SerializeField] private AudioMixerSnapshot modernSnapshot;

    [Tooltip("Retro/degraded mix (filters/bitcrush/limiter etc.).")]
    [SerializeField] private AudioMixerSnapshot retroSnapshot;

    [SerializeField] private float transitionTime = 0.35f;

    private void Awake()
    {
        // Safe warnings only — music is optional in some test scenes.
        if (musicClip != null && musicSource == null)
            Debug.LogWarning($"{name}: Music clip assigned but no musicSource set.", this);
    }

    private void Start()
    {
        // Music playback is separate from SFX and unaffected by player routing refactors.
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        // Start in Modern unless something else overrides it.
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
}