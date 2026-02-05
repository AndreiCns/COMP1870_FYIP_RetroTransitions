using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Vision")]
    public float viewDistance = 20f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public LayerMask lineOfSightMask; // typically: Default + Player, exclude Enemy

    [Header("Target")]
    public Transform target; // assign Player transform
    public Transform eyes;


    public bool CanSeeTarget(out Vector3 lastSeenPos)
    {
        lastSeenPos = Vector3.zero;
        if (target == null) return false;

        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist > viewDistance) return false;

        Vector3 dir = toTarget / dist;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewAngle * 0.5f) return false;

        // LOS check (ray from "eyes" height)
        Vector3 origin = transform.position + Vector3.up * 1.6f;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, viewDistance, lineOfSightMask))
        {
            if (hit.transform == target)
            {
                lastSeenPos = target.position;
                return true;
            }
        }

        return false;
    }
}
