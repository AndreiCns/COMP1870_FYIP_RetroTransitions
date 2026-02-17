using UnityEngine;

public class BillboardFaceCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool lockY = true;

    void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        Vector3 toCam = transform.position - targetCamera.transform.position;

        if (lockY) toCam.y = 0f; // keep upright like Doom-style billboards
        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(toCam);
    }
}
