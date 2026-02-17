using UnityEngine;

public class BillboardFaceCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool lockY = true;

    private RangedAttackModule attackModule;

    private void Awake()
    {
        attackModule = GetComponentInParent<RangedAttackModule>();
    }

    void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 toCam = transform.position - targetCamera.transform.position;

        if (lockY) toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(toCam);
    }

    //  Called by Animation Event
    public void FireProjectile()
    {
        attackModule?.FireProjectile_AnimEvent();
    }

}
