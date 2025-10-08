using UnityEngine;

public class JointBillboardUI : MonoBehaviour
{
    public Transform target;                 // the robot joint
    public Vector3 localOffset = new Vector3(0f, 0.08f, 0f); // offset from joint
    public Camera cam;                       // leave empty to auto-grab main

    void Awake() { if (!cam) cam = Camera.main; }

    void LateUpdate()
    {
        if (!target) return;

        // follow the joint using joint-local offset
        transform.position = target.TransformPoint(localOffset);

        if (!cam) return;

        // make front (Canvas +Z) face camera
        Vector3 toCam = cam.transform.position - transform.position;
        if (toCam.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
    }
}
