using UnityEngine;

public class PlayerShootModule : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzle;
    [SerializeField] private MuzzleFlashController muzzleFlash;

    [Header("Audio")]
    [SerializeField] private AudioSource gunshotSource;
    [SerializeField] private AudioClip gunshotClip;
    [SerializeField] private Vector2 pitchVariation = new Vector2(0.97f, 1.03f);

    [Header("Tuning")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 200f;
    [SerializeField] private LayerMask hitMask;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = false;
    [SerializeField] private bool logWarnings = true;

    private GameObject owner;

    public bool CanFire => canFire;
    private bool canFire = true;

    private void Awake()
    {
        owner = transform.root.gameObject;

        if (playerCamera == null && logWarnings)
            Debug.LogWarning($"{name}: playerCamera not assigned", this);
        if (muzzle == null && logWarnings)
            Debug.LogWarning($"{name}: muzzle not assigned", this);
        if (gunshotSource == null && logWarnings)
            Debug.LogWarning($"{name}: gunshotSource not assigned", this);
    }

    // Called by your INPUT (before setting animator trigger)
    public bool TryBeginFire()
    {
        if (!canFire) return false;
        canFire = false;
        return true;
    }

    // CALLED BY ANIMATION EVENT (at firing frame)
    public void FireProjectile()
    {
        if (playerCamera == null) return;

        muzzleFlash?.Play();

        if (gunshotSource != null && gunshotClip != null)
        {
            gunshotSource.pitch = Random.Range(pitchVariation.x, pitchVariation.y);
            gunshotSource.PlayOneShot(gunshotClip);
        }

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (drawDebugRay)
                Debug.DrawLine(origin, hit.point, Color.red, 1f);

            if (hit.collider.TryGetComponent(out IDamageable dmg))
            {
                dmg.TakeDamage(damage, new DamageInfo
                {
                    Point = hit.point,
                    Direction = direction,
                    Source = owner
                });
            }
        }
        else
        {
            if (drawDebugRay)
                Debug.DrawRay(origin, direction * range, Color.yellow, 1f);
        }
    }

    // CALLED BY ANIMATION EVENT (last frame of shoot anim)
    public void OnShootAnimFinished()
    {
        canFire = true;
    }

    // Safety: if weapon gets disabled mid-shot
    private void OnDisable()
    {
        canFire = true;
    }
}
