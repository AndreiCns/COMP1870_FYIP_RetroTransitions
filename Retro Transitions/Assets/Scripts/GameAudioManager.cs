using UnityEngine;
using UnityEngine.Audio;

public class GameAudioManager : MonoBehaviour
{
    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Clip")]
    // Same track in both modes — style change is done through the mixer.
    [SerializeField] private AudioClip musicClip;

    [Header("Mixer Snapshots")]
    [SerializeField] private AudioMixerSnapshot modernSnapshot;
    [SerializeField] private AudioMixerSnapshot retroSnapshot;

    [SerializeField] private float transitionTime = 0.35f;

    private void Start()
    {
        // Start music once at runtime
        if (musicSource != null && musicClip != null)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }

        PlayModern(); // default state
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
