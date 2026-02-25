using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyDamageFeedback : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health health;
    [SerializeField] private EnemyVisualAnimatorProxy animProxy;
    [SerializeField] private StyleSwapEvent styleSwapEvent;

    [Header("Audio (spatial)")]
    [SerializeField] private GameAudioManager gameAudio;
    [SerializeField] private AudioSource audioSource;

    [Header("Hit SFX")]
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;
    [SerializeField] private float hitPitchMin = 0.97f;
    [SerializeField] private float hitPitchMax = 1.03f;

    [Header("Death SFX")]
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;
    [SerializeField] private float deathPitchMin = 0.9f;
    [SerializeField] private float deathPitchMax = 1.05f;

    [Header("Modern Hit VFX")]
    [SerializeField] private ParticleSystem modernHitVfx;

    [Header("Retro Hit VFX (Sprite)")]
    [SerializeField] private SpriteRenderer retroHitSprite;
    [SerializeField] private float retroSpriteDuration = 0.08f;

    [Header("Spawn")]
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("Freeze Frames")]
    [SerializeField] private bool freezeOnHit = true;
    [SerializeField, Range(0, 10)] private int freezeFrames = 2;

    [Header("Nav Freeze (optional)")]
    [Tooltip("If empty, will try to find in parent.")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Knockback (optional)")]
    [SerializeField] private bool knockbackOnHit = true;
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private float knockbackUp = 0f;
    [SerializeField] private float knockbackDuration = 0.08f;
    [SerializeField] private AnimationCurve knockbackEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Retro Knockback Tuning")]
    [Tooltip("Retro reads snappier, so slightly smaller + longer feels smoother.")]
    [SerializeField] private float retroDistanceMultiplier = 0.7f;
    [SerializeField] private float retroDurationMultiplier = 1.25f;

    [Tooltip("If assigned, uses this as hit direction source (e.g. player transform). If null, uses enemy->camera direction.")]
    [SerializeField] private Transform attacker;

    private Coroutine freezeRoutine;
    private Coroutine retroSpriteRoutine;
    private Coroutine knockbackRoutine;

    private bool cachedStopped;
    private float cachedSpeed;
    private float cachedAngularSpeed;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (animProxy == null)
            animProxy = GetComponentInParent<EnemyVisualAnimatorProxy>();

        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();

        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();

        if (vfxSpawnPoint == null)
            vfxSpawnPoint = transform;

        if (gameAudio == null)
            gameAudio = FindFirstObjectByType<GameAudioManager>();

        if (retroHitSprite != null)
            retroHitSprite.enabled = false;

        if (health == null)
        {
            Debug.LogError($"{name}: Missing Health reference.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        health.OnDamaged.AddListener(OnDamaged);
        health.OnDied.AddListener(OnDied);
    }

    private void OnDisable()
    {
        health.OnDamaged.RemoveListener(OnDamaged);
        health.OnDied.RemoveListener(OnDied);

        if (freezeRoutine != null) StopCoroutine(freezeRoutine);
        if (retroSpriteRoutine != null) StopCoroutine(retroSpriteRoutine);
        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);

        if (retroHitSprite != null) retroHitSprite.enabled = false;
        if (animProxy != null) animProxy.SetPaused(false);

        RestoreNavAfterFreeze();
    }

    private void OnDamaged(float damage)
    {
        if (health.IsDead)
            return;

        PlayRandomClip(hitClips, hitVolume, hitPitchMin, hitPitchMax);
        PlayHitVfx();

        // Hit-stop should happen first, then knockback starts during the stop window.
        if (freezeOnHit && freezeFrames > 0)
        {
            if (freezeRoutine != null)
                StopCoroutine(freezeRoutine);

            freezeRoutine = StartCoroutine(HitStopThenKnockbackRoutine(freezeFrames));
        }
        else
        {
            // No hit-stop: just do a smooth knockback.
            if (knockbackOnHit)
                StartKnockback();
        }
    }

    private void OnDied()
    {
        PlayRandomClip(deathClips, deathVolume, deathPitchMin, deathPitchMax);
    }

    private void PlayRandomClip(AudioClip[] clips, float volume, float pitchMin, float pitchMax)
    {
        if (audioSource == null || clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        if (gameAudio != null)
        {
            gameAudio.PlayOneShotOnSource(audioSource, clip, volume, pitchMin, pitchMax);
            return;
        }

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, volume);
    }

    private void PlayHitVfx()
    {
        StyleState state = styleSwapEvent != null ? styleSwapEvent.LastState : StyleState.Modern;

        if (state == StyleState.Retro)
        {
            PlayRetroSprite();
            return;
        }

        if (modernHitVfx == null)
            return;

        modernHitVfx.transform.position = vfxSpawnPoint.position;
        modernHitVfx.transform.rotation = vfxSpawnPoint.rotation;
        modernHitVfx.Play(true);
    }

    private void PlayRetroSprite()
    {
        if (retroHitSprite == null)
            return;

        retroHitSprite.transform.position = vfxSpawnPoint.position;
        retroHitSprite.enabled = true;

        if (retroSpriteRoutine != null)
            StopCoroutine(retroSpriteRoutine);

        retroSpriteRoutine = StartCoroutine(HideRetroSpriteAfterDelay());
    }

    private IEnumerator HideRetroSpriteAfterDelay()
    {
        yield return new WaitForSeconds(retroSpriteDuration);
        retroHitSprite.enabled = false;
        retroSpriteRoutine = null;
    }

    private IEnumerator HitStopThenKnockbackRoutine(int frames)
    {
        if (animProxy != null)
            animProxy.SetPaused(true);

        PauseNavForFreeze();

        // 1) Freeze for N frames
        for (int i = 0; i < frames; i++)
            yield return null;

        // 2) Start knockback while nav is still paused (smooth lurch)
        if (knockbackOnHit)
        {
            if (knockbackRoutine != null)
                StopCoroutine(knockbackRoutine);

            knockbackRoutine = StartCoroutine(KnockbackRoutine_NoNavOwnership());
            yield return knockbackRoutine;
        }

        // 3) Resume anim + nav
        if (animProxy != null)
            animProxy.SetPaused(false);

        RestoreNavAfterFreeze();

        freezeRoutine = null;
    }

    private void PauseNavForFreeze()
    {
        if (agent == null || !agent.enabled)
            return;

        cachedStopped = agent.isStopped;
        cachedSpeed = agent.speed;
        cachedAngularSpeed = agent.angularSpeed;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Avoid rotation drift during hit-stop.
        agent.speed = 0f;
        agent.angularSpeed = 0f;
    }

    private void RestoreNavAfterFreeze()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.isStopped = cachedStopped;
        agent.speed = cachedSpeed;
        agent.angularSpeed = cachedAngularSpeed;
    }

    private void StartKnockback()
    {
        if (knockbackDistance <= 0f)
            return;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        // No hit-stop path: knockback pauses nav briefly itself.
        knockbackRoutine = StartCoroutine(KnockbackRoutine_WithNavOwnership());
    }

    private IEnumerator KnockbackRoutine_WithNavOwnership()
    {
        bool hadAgent = agent != null && agent.enabled;

        if (hadAgent)
            PauseNavForFreeze();

        yield return KnockbackRoutine_Core();

        if (hadAgent)
            RestoreNavAfterFreeze();

        knockbackRoutine = null;
    }

    // Used during hit-stop: nav is already paused by freeze, so this routine doesn't restore it.
    private IEnumerator KnockbackRoutine_NoNavOwnership()
    {
        yield return KnockbackRoutine_Core();
        knockbackRoutine = null;
    }

    private IEnumerator KnockbackRoutine_Core()
    {
        if (knockbackDistance <= 0f)
            yield break;

        // Direction: away from attacker if provided, otherwise away from camera.
        Vector3 from;
        if (attacker != null)
            from = attacker.position;
        else if (Camera.main != null)
            from = Camera.main.transform.position;
        else
            from = transform.position - transform.forward;

        Vector3 dir = transform.position - from;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = -transform.forward;

        dir.Normalize();

        // Retro reads snappy: reduce distance + slightly longer duration.
        StyleState state = styleSwapEvent != null ? styleSwapEvent.LastState : StyleState.Modern;

        float dist = knockbackDistance;
        float dur = knockbackDuration;

        if (state == StyleState.Retro)
        {
            dist *= retroDistanceMultiplier;
            dur *= retroDurationMultiplier;
        }

        Vector3 totalOffset = dir * dist + Vector3.up * knockbackUp;

        float t = 0f;
        float duration = Mathf.Max(0.01f, dur);

        Vector3 applied = Vector3.zero;
        bool hadAgent = agent != null && agent.enabled;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            float eased = knockbackEase != null ? knockbackEase.Evaluate(u) : u;

            Vector3 targetApplied = totalOffset * eased;
            Vector3 step = targetApplied - applied;

            if (hadAgent)
                agent.Move(step);
            else
                transform.position += step;

            applied = targetApplied;
            yield return null;
        }
    }
}