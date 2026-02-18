using UnityEngine;

public class BillboardFaceCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool lockY = true; // keep upright (useful for sprites)

    private RangedAttackModule attackModule;

    private void Awake()
    {
        // Cached in case the billboard needs attack context later
        attackModule = GetComponentInParent<RangedAttackModule>();
    }

    void LateUpdate()
    {
        // Fallback if no camera assigned in inspector
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 toCam = transform.position - targetCamera.transform.position;

        // Prevent vertical tilting for classic sprite look
        if (lockY) toCam.y = 0f;

        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(toCam);
    }
}
