using UnityEngine;

public class RetroSmokePuff : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float riseSpeed = 0.12f;
    [SerializeField] private float lifetime = 0.6f;

    [Header("Billboard")]
    [SerializeField] private bool lockY = true;

    private float timer;
    private Camera cam;

    private Vector3 extraVelocity;

    public void AddVelocity(Vector3 v)
    {
        extraVelocity += v;
    }

    private void Awake()
    {
        cam = FindActiveCamera();
    }

    private void Update()
    {
        // Base rise + inherited kick.
        Vector3 v = (Vector3.up * riseSpeed) + extraVelocity;
        transform.position += v * Time.deltaTime;

        // Dampen the kick quickly so it feels like puff inertia.
        extraVelocity = Vector3.Lerp(extraVelocity, Vector3.zero, 10f * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void LateUpdate()
    {
        if (cam == null)
            cam = FindActiveCamera();

        if (cam == null)
            return;

        Vector3 toCam = transform.position - cam.transform.position;

        if (lockY)
            toCam.y = 0f;

        if (toCam.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(toCam);
    }

    private Camera FindActiveCamera()
    {
        if (Camera.main != null)
            return Camera.main;

        Camera[] cams = Camera.allCameras;
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] != null && cams[i].enabled)
                return cams[i];
        }

        return null;
    }
}