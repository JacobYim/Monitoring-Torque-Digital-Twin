using UnityEngine;

public class JointUIFollower : MonoBehaviour
{
    [Header("Target joint to follow")]
    public Transform target;

    [Header("Local offset in target space (meters)")]
    public Vector3 localOffset = new Vector3(0f, 0.08f, 0f);

    [Header("Billboard")]
    public bool faceCamera = true;
    public Camera cam;

    [Header("Distance-based scaling (optional)")]
    public bool scaleWithDistance = true;
    public float minDistance = 0.4f;
    public float maxDistance = 4f;
    public float minScale = 0.0006f;
    public float maxScale = 0.0014f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Follow joint with offset in joint local space
        transform.position = target.TransformPoint(localOffset);

        // Billboard toward camera
        if (faceCamera && cam)
        {
            Vector3 toCam = cam.transform.position - transform.position;
            if (toCam.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }

        // Optional: keep UI readable at distance
        if (scaleWithDistance && cam)
        {
            float d = Vector3.Distance(cam.transform.position, transform.position);
            d = Mathf.Clamp(d, minDistance, maxDistance);
            float t = Mathf.InverseLerp(minDistance, maxDistance, d);
            float s = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = new Vector3(s, s, s);
        }
    }
}
