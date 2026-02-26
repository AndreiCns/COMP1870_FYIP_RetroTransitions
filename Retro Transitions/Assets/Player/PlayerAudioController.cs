using System.Collections;
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
    [Tooltip("Pick one at random; will not repeat twice in a row.")]
    [SerializeField] private AudioClip[] jumpClips;
    [SerializeField] private float jumpVolume01 = 1f;
    [SerializeField] private Vector2 jumpPitchRange = new Vector2(0.95f, 1.05f);

    [SerializeField] private AudioClip landingClip;
    [SerializeField] private float landingVolume01 = 1f;
    [SerializeField] private Vector2 landingPitchRange = new Vector2(0.90f, 1.00f);

    [Header("Hurt")]
    [Tooltip("Will not repeat twice in a row.")]
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

    [Header("Heartbeat -> Music Ducking")]
    [Tooltip("Mixer that controls the music volume (music should be routed through this mixer).")]
    [SerializeField] private AudioMixer musicMixer;

    [Tooltip("Exposed parameter name on the music mixer (in dB).")]
    [SerializeField] private string musicVolumeParam = "MusicVol";

    [Tooltip("How much to lower music while heartbeat is active (negative dB).")]
    [SerializeField] private float musicDuckDb = -8f;

    [Tooltip("How quickly the music fades to/from ducked volume.")]
    [SerializeField] private float musicDuckFadeTime = 0.15f;

    [Header("Heartbeat -> PlayerSFX Ducking")]
    [SerializeField] private AudioMixer playerSfxMixer;
    [SerializeField] private string playerSfxVolumeParam = "PlayerSFXVol";
    [SerializeField] private float playerSfxDuckDb = -6f;
    [SerializeField] private float playerSfxDuckFadeTime = 0.12f;

    private bool hasPlayerSfxParam;
    private float cachedPlayerSfxDb;
    private Coroutine playerSfxDuckRoutine;

    private float nextHurtTime;
    private float heartbeatTimer;
    private bool heartbeatActive;

    private int lastJumpIndex = -1;
    private int lastHurtIndex = -1;
    private int lastFootstepIndex = -1;

    private bool hasMusicParam;
    private float cachedMusicDb;
    private Coroutine musicDuckRoutine;

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

        CacheMusicVolumeParam();
        CachePlayerSfxVolumeParam();
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
        StopMusicDuckImmediate(); // instant restore, no coroutine
        StopPlayerSfxDuckImmediate();
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
        AudioClip clip = PickNoRepeat(footstepClips, ref lastFootstepIndex);
        if (clip == null) return;

        PlayOneShot(clip, footstepVolume01, footstepPitchRange);
    }

    public void PlayJump()
    {
        AudioClip clip = PickNoRepeat(jumpClips, ref lastJumpIndex);
        if (clip == null) return;

        PlayOneShot(clip, jumpVolume01, jumpPitchRange);
    }

    public void PlayLanding()
    {
        if (landingClip == null) return;
        PlayOneShot(landingClip, landingVolume01, landingPitchRange);
    }

    public void PlayGunshot(AmmoTypeConfig cfg)
    {
        if (cfg == null || cfg.gunshotClip == null) return;

        Vector2 pitch = cfg.gunshotPitch;
        float vol = Mathf.Clamp01(cfg.gunshotVolume01 <= 0f ? 1f : cfg.gunshotVolume01);

        PlayOneShot(cfg.gunshotClip, vol, pitch);
    }

    public void PlayOneShot2D(AudioClip clip, float volume01, Vector2 pitchRange)
    {
        PlayOneShot(clip, volume01, pitchRange);
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

        AudioClip clip = PickNoRepeat(hurtClips, ref lastHurtIndex);
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

        StartMusicDuck(true);
        StartPlayerSfxDuck(true);
    }

    private void StopHeartbeat()
    {
        if (!heartbeatActive && !heartbeatSource.isPlaying)
            return;

        heartbeatActive = false;

        if (heartbeatSource.isPlaying)
            heartbeatSource.Stop();

        StartMusicDuck(false);
        StartPlayerSfxDuck(false);
    }

    private void PlayOneShot(AudioClip clip, float volume01, Vector2 pitchRange)
    {
        if (clip == null) return;

        oneShotSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume01) * masterVolume01);
    }

    private AudioClip PickNoRepeat(AudioClip[] clips, ref int lastIndex)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1)
        {
            lastIndex = 0;
            return clips[0];
        }

        int i = Random.Range(0, clips.Length);

        // quick re-roll to avoid same index
        if (i == lastIndex)
            i = (i + Random.Range(1, clips.Length)) % clips.Length;

        lastIndex = i;
        return clips[i];
    }

    // --- Music ducking (mixer param in dB) ---

    private void CacheMusicVolumeParam()
    {
        hasMusicParam = false;
        cachedMusicDb = 0f;

        if (musicMixer == null || string.IsNullOrWhiteSpace(musicVolumeParam))
            return;

        // Requires the param to be exposed in the AudioMixer.
        if (musicMixer.GetFloat(musicVolumeParam, out cachedMusicDb))
            hasMusicParam = true;
        else
            Debug.LogWarning($"{name}: Music mixer param '{musicVolumeParam}' not found/exposed. Heartbeat ducking disabled.", this);
    }

    private void StartMusicDuck(bool duck)
    {
        if (!hasMusicParam || !isActiveAndEnabled)
            return;

        float target = duck ? cachedMusicDb + musicDuckDb : cachedMusicDb;

        if (musicDuckRoutine != null)
            StopCoroutine(musicDuckRoutine);

        musicDuckRoutine = StartCoroutine(FadeMixerParam(musicVolumeParam, target, musicDuckFadeTime));
    }

    private void StopMusicDuckImmediate()
    {
        if (!hasMusicParam)
            return;

        // Never start coroutines during disable — just restore instantly.
        if (musicDuckRoutine != null)
        {
            StopCoroutine(musicDuckRoutine);
            musicDuckRoutine = null;
        }

        musicMixer.SetFloat(musicVolumeParam, cachedMusicDb);
    }

    private IEnumerator FadeMixerParam(string param, float targetDb, float time)
    {
        if (time <= 0f)
        {
            musicMixer.SetFloat(param, targetDb);
            yield break;
        }

        musicMixer.GetFloat(param, out float startDb);
        float t = 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / time);
            float v = Mathf.Lerp(startDb, targetDb, a);
            musicMixer.SetFloat(param, v);
            yield return null;
        }

        musicMixer.SetFloat(param, targetDb);
    }

    private void CachePlayerSfxVolumeParam()
    {
        hasPlayerSfxParam = false;
        cachedPlayerSfxDb = 0f;

        if (playerSfxMixer == null || string.IsNullOrWhiteSpace(playerSfxVolumeParam))
            return;

        if (playerSfxMixer.GetFloat(playerSfxVolumeParam, out cachedPlayerSfxDb))
            hasPlayerSfxParam = true;
        else
            Debug.LogWarning($"{name}: PlayerSFX mixer param '{playerSfxVolumeParam}' not found/exposed. PlayerSFX ducking disabled.", this);
    }

    private void StartPlayerSfxDuck(bool duck)
    {
        if (!hasPlayerSfxParam || !isActiveAndEnabled)
            return;

        float target = duck ? cachedPlayerSfxDb + playerSfxDuckDb : cachedPlayerSfxDb;

        if (playerSfxDuckRoutine != null)
            StopCoroutine(playerSfxDuckRoutine);

        playerSfxDuckRoutine = StartCoroutine(FadePlayerSfxParam(playerSfxVolumeParam, target, playerSfxDuckFadeTime));
    }

    private void StopPlayerSfxDuckImmediate()
    {
        if (!hasPlayerSfxParam)
            return;

        if (playerSfxDuckRoutine != null)
        {
            StopCoroutine(playerSfxDuckRoutine);
            playerSfxDuckRoutine = null;
        }

        playerSfxMixer.SetFloat(playerSfxVolumeParam, cachedPlayerSfxDb);
    }

    private IEnumerator FadePlayerSfxParam(string param, float targetDb, float time)
    {
        if (time <= 0f)
        {
            playerSfxMixer.SetFloat(param, targetDb);
            yield break;
        }

        playerSfxMixer.GetFloat(param, out float startDb);
        float t = 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / time);
            float v = Mathf.Lerp(startDb, targetDb, a);
            playerSfxMixer.SetFloat(param, v);
            yield return null;
        }

        playerSfxMixer.SetFloat(param, targetDb);
    }
}