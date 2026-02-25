using UnityEngine;
using UnityEngine.Audio;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;

    [Header("Sources (routed to PlayerSFX)")]
    [Tooltip("2D one-shot source for footsteps, jump/land, hurt, gunshots.")]
    [SerializeField] private AudioSource oneShotSource;

    [Tooltip("2D looping source for low-health heartbeat.")]
    [SerializeField] private AudioSource heartbeatSource;

    [Header("Mixer Routing")]
    [Tooltip("Assign your PlayerSFX AudioMixerGroup here so snapshots affect all player SFX.")]
    [SerializeField] private AudioMixerGroup playerSfxGroup;

    [Header("Global")]
    [Tooltip("Master gain for all player SFX (one-shots + heartbeat).")]
    [SerializeField, Range(0f, 2f)] private float masterVolume01 = 1f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepVolume01 = 1f;
    [SerializeField] private Vector2 footstepPitchRange = new Vector2(0.95f, 1.05f);

    [Header("Jump / Landing")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private float jumpVolume01 = 1f;
    [SerializeField] private Vector2 jumpPitchRange = new Vector2(0.95f, 1.05f);

    [SerializeField] private AudioClip landingClip;
    [SerializeField] private float landingVolume01 = 1f;
    [SerializeField] private Vector2 landingPitchRange = new Vector2(0.90f, 1.00f);

    [Header("Hurt")]
    [SerializeField] private AudioClip[] hurtClips;
    [SerializeField] private float hurtVolume01 = 1f;
    [SerializeField] private Vector2 hurtPitchRange = new Vector2(0.98f, 1.02f);
    [SerializeField] private float hurtMinInterval = 0.15f;

    [Header("Heartbeat (low HP loop)")]
    [SerializeField] private AudioClip heartbeatLoop;
    [SerializeField] private float heartbeatVolume01 = 1f;
    [SerializeField, Range(0.05f, 0.95f)] private float lowHpThreshold01 = 0.25f;
    [SerializeField, Range(0f, 0.2f)] private float hysteresis01 = 0.05f;
    [SerializeField] private float heartbeatCheckInterval = 0.10f;

    private float nextHurtTime;
    private float heartbeatTimer;
    private bool heartbeatActive;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (oneShotSource == null || heartbeatSource == null)
        {
            Debug.LogError($"{name}: Missing AudioSource refs (oneShotSource/heartbeatSource).", this);
            enabled = false;
            return;
        }

        // Force 2D playback for consistent loudness (player-centric HUD-style audio).
        oneShotSource.spatialBlend = 0f;
        heartbeatSource.spatialBlend = 0f;

        // Enforce mixer routing so snapshots/effects hit all player SFX.
        if (playerSfxGroup != null)
        {
            oneShotSource.outputAudioMixerGroup = playerSfxGroup;
            heartbeatSource.outputAudioMixerGroup = playerSfxGroup;
        }
        else
        {
            Debug.LogWarning($"{name}: No PlayerSFX mixer group assigned. Audio will still play but routing may be wrong.", this);
        }

        heartbeatSource.loop = true;
        heartbeatSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        // Health drives hurt + heartbeat without hard coupling from other gameplay scripts.
        if (health != null)
            health.OnDamaged.AddListener(OnDamaged);
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged.RemoveListener(OnDamaged);

        StopHeartbeat();
    }

    private void Update()
    {
        // Cheap polling so heartbeat responds to healing as well as damage.
        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0f)
        {
            heartbeatTimer = heartbeatCheckInterval;
            UpdateHeartbeatState();
        }
    }

    // --- Public API (called by FPC / Shoot Modules) ---

    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;
        int i = Random.Range(0, footstepClips.Length);
        PlayOneShot(footstepClips[i], footstepVolume01, footstepPitchRange);
    }

    public void PlayJump()
    {
        if (jumpClip == null) return;
        PlayOneShot(jumpClip, jumpVolume01, jumpPitchRange);
    }

    public void PlayLanding()
    {
        if (landingClip == null) return;
        PlayOneShot(landingClip, landingVolume01, landingPitchRange);
    }

    public void PlayGunshot(AmmoTypeConfig cfg)
    {
        if (cfg == null || cfg.gunshotClip == null) return;

        // Per-ammo tuning comes from the config (clip, pitch range, volume).
        Vector2 pitch = cfg.gunshotPitch;
        float vol = Mathf.Clamp01(cfg.gunshotVolume01 <= 0f ? 1f : cfg.gunshotVolume01);

        PlayOneShot(cfg.gunshotClip, vol, pitch);
    }

    // --- Internals ---

    private void OnDamaged(float amount)
    {
        if (hurtClips == null || hurtClips.Length == 0)
            return;

        // Prevent spam stacking from rapid damage ticks
        if (Time.time < nextHurtTime)
            return;

        nextHurtTime = Time.time + hurtMinInterval;

        int index = Random.Range(0, hurtClips.Length);
        AudioClip clip = hurtClips[index];

        if (clip != null)
            PlayOneShot(clip, hurtVolume01, hurtPitchRange);

        // Re-check heartbeat immediately on damage
        UpdateHeartbeatState();
    }

    private void UpdateHeartbeatState()
    {
        if (health == null || heartbeatLoop == null)
        {
            StopHeartbeat();
            return;
        }

        float hp01 = (health.Max <= 0f) ? 1f : Mathf.Clamp01(health.Current / health.Max);

        // Hysteresis stops the loop rapidly toggling when hovering around the threshold.
        float onThreshold = lowHpThreshold01;
        float offThreshold = Mathf.Clamp01(lowHpThreshold01 + hysteresis01);

        if (!heartbeatActive && hp01 <= onThreshold)
            StartHeartbeat();
        else if (heartbeatActive && hp01 >= offThreshold)
            StopHeartbeat();
    }

    private void StartHeartbeat()
    {
        if (heartbeatActive) return;

        heartbeatActive = true;
        heartbeatSource.clip = heartbeatLoop;
        heartbeatSource.pitch = 1f;
        heartbeatSource.volume = Mathf.Clamp01(heartbeatVolume01) * masterVolume01;
        heartbeatSource.Play();
    }

    private void StopHeartbeat()
    {
        heartbeatActive = false;
        if (heartbeatSource.isPlaying)
            heartbeatSource.Stop();
    }

    private void PlayOneShot(AudioClip clip, float volume01, Vector2 pitchRange)
    {
        if (clip == null) return;

        oneShotSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume01) * masterVolume01);
    }

    // ADD THIS METHOD to your PlayerAudioController (anywhere in the Public API section).
    public void PlayOneShot2D(AudioClip clip, float volume01, Vector2 pitchRange)
    {
        PlayOneShot(clip, volume01, pitchRange);
    }
}