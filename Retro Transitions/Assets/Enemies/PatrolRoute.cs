using UnityEngine;

public class PatrolRoute : MonoBehaviour
{
    public Transform[] points;

    public Vector3 GetPoint(int index)
    {
        if (points == null || points.Length == 0) return transform.position;
        index = Mathf.Clamp(index, 0, points.Length - 1);
        return points[index].position;
    }

    public int NextIndex(int current) => (points == null || points.Length == 0) ? 0 : (current + 1) % points.Length;
}
