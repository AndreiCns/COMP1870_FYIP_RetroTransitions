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

    private void OnEnable()
    {
        // Re-resolve on each enable to handle camera changes between cycles
        targetCamera = ResolveCamera();
    }

    private void LateUpdate()
    {
        // Lazy retry if camera wasn't ready at OnEnable
        if (targetCamera == null)
        {
            targetCamera = ResolveCamera();
            if (targetCamera == null) return;
        }

        Vector3 toCam = transform.position - targetCamera.transform.position;

        if (lockY) toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(toCam);
    }

    private static Camera ResolveCamera()
    {
        if (Camera.main != null) return Camera.main;

        Camera[] cams = Camera.allCameras;
        for (int i = 0; i < cams.Length; i++)
            if (cams[i] != null && cams[i].enabled)
                return cams[i];

        return null;
    }
}