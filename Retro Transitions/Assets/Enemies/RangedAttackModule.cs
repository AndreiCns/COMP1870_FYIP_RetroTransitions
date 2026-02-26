using UnityEngine;

public class RangedAttackModule : MonoBehaviour, IEnemyAttack
{
    [Header("Setup")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Audio (spatial)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] shootClips;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 1f;
    [SerializeField] private Vector2 shootPitchRange = new Vector2(0.98f, 1.02f);

    [Header("Tuning")]
    [SerializeField] private float attackRange = 18f;
    [SerializeField] private float fireCooldown = 0.8f;
    [SerializeField] private float projectileSpeed = 25f;
    [SerializeField] private float damage = 10f;

    [Header("Aiming")]
    [SerializeField] private float aimHeightOffset = 1.2f;

    [Header("Animation")]
    [SerializeField] private string isShootingBool = "isShooting";

    private float fireTimer;
    private bool shotArmed;
    private Transform currentTarget;

    private EnemyVisualAnimatorProxy animProxy;
    private float attackRangeSqr;

    public void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;
    public Transform GetCurrentTarget() => currentTarget;

    private void Awake()
    {
        animProxy = GetComponentInParent<EnemyVisualAnimatorProxy>();
        attackRangeSqr = attackRange * attackRange;

        if (audioSource == null)
            audioSource = GetComponentInParent<AudioSource>();

        if (audioSource == null)
            Debug.LogWarning($"[{name}] RangedAttackModule: No AudioSource found. Shoot SFX will be silent.", this);
    }

    private void Update()
    {
        if (fireTimer > 0f)
            fireTimer -= Time.deltaTime;
    }

    public bool CanAttack(Transform target)
    {
        if (target == null) return false;
        return (target.position - transform.position).sqrMagnitude <= attackRangeSqr;
    }

    public void TickAttack(Transform target)
    {
        if (fireTimer > 0f || target == null || !CanAttack(target)) return;

        currentTarget = target;
        shotArmed = true;

        animProxy?.SetBool(isShootingBool, true);
        fireTimer = fireCooldown;
    }

    // Called by animation event at the fire frame
    public void FireProjectile()
    {
        if (!shotArmed) return;
        shotArmed = false;

        PlayShootSfx();
        SpawnProjectile();
    }

    // Called at the end of the shoot animation
    public void OnShootAnimFinished()
    {
        if (currentTarget == null || !CanAttack(currentTarget))
            animProxy?.SetBool(isShootingBool, false);
    }

    private void PlayShootSfx()
    {
        if (audioSource == null || shootClips == null || shootClips.Length == 0) return;

        AudioClip clip = shootClips[Random.Range(0, shootClips.Length)];
        if (clip == null) return;

        audioSource.pitch = Random.Range(shootPitchRange.x, shootPitchRange.y);
        audioSource.PlayOneShot(clip, shootVolume);
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null || muzzle == null || currentTarget == null) return;

        Vector3 aimPos = currentTarget.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (aimPos - muzzle.position).normalized;

        GameObject projGO = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
        if (projGO.TryGetComponent(out Projectile proj))
            proj.Init(dir, projectileSpeed, damage, gameObject);
    }
}