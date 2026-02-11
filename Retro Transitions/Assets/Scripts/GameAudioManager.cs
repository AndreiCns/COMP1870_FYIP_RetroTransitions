using UnityEngine;
using UnityEngine.Audio;

public class GameAudioManager : MonoBehaviour
{
    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Clip (same clip for both styles)")]
    [SerializeField] private AudioClip musicClip;

    [Header("Audio Mixer Snapshots")]
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
}
