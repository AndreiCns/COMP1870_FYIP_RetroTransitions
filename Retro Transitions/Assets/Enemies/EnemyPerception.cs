using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // Assign Player root
    public Transform eyes;                   // Optional eye transform

    [Header("Vision (Simple 90s)")]
    public float viewDistance = 30f;         // Detection radius
    public LayerMask lineOfSightMask;        // Environment + Player
    public float eyeHeightFallback = 1.6f;   // If eyes not assigned

    [Header("Debug")]
    public bool debugDraw = false;

    /// <summary>
    /// Returns true if enemy has direct LOS to player within viewDistance.
    /// 360° vision. No FOV cone.
    /// </summary>
    public bool CanSeeTarget(out Vector3 lastSeenPos)
    {
        lastSeenPos = Vector3.zero;

        if (target == null)
            return false;

        Vector3 origin = eyes != null
            ? eyes.position
            : transform.position + Vector3.up * eyeHeightFallback;

        // Aim roughly at chest height (prevents foot clipping issues)
        Vector3 targetPos = target.position + Vector3.up * 1.1f;

        Vector3 toTarget = targetPos - origin;
        float distance = toTarget.magnitude;

        if (distance > viewDistance)
            return false;

        Vector3 direction = toTarget.normalized;

        bool hit = Physics.Raycast(
            origin,
            direction,
            out RaycastHit rayHit,
            distance,
            lineOfSightMask,
            QueryTriggerInteraction.Ignore
        );

        bool sees = hit && rayHit.transform.root == target.root;

        if (debugDraw)
            Debug.DrawLine(origin, targetPos, sees ? Color.green : Color.red);

        if (sees)
            lastSeenPos = target.position;

        return sees;
    }
}
