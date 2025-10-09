using UnityEngine;
public class JointBillboardUI : MonoBehaviour
{
    public Transform target;                 // the robot joint
    public Vector3 localOffset = new Vector3(0f, 0.08f, 0f); // offset from joint
    public Camera cam;                       // leave empty to auto-grab main
    // Line mode options
    public bool isLine = false;              // when true, UI stays fixed and draws a line to target
    public bool useInitialPositionAsFixed = true; // capture Start position as fixed point
    public Vector3 fixedWorldPosition;       // used when isLine == true and not using initial position
    // Optional LineRenderer settings (used only when isLine == true)
    public LineRenderer lineRenderer;        // assign or auto-created if missing
    public float lineWidth = 0.01f;          // meters
    public Color lineColor = Color.white;
    public Material lineMaterial;            // optional; if null, uses default
    [Min(0f)] public float lineOffsetFromUI = 0.05f;     // shorten line near UI to avoid covering it
    [Min(0f)] public float lineOffsetFromTarget = 0f;    // optional: shorten line near target
    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (useInitialPositionAsFixed)
            fixedWorldPosition = transform.position;
        EnsureLineRendererInitializedIfNeeded();
    }
    void EnsureLineRendererInitializedIfNeeded()
    {
        if (!isLine) return;
        if (!lineRenderer)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (!lineRenderer)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        lineRenderer.enabled = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        if (lineMaterial)
            lineRenderer.material = lineMaterial;
    }
    void LateUpdate()
    {
        if (!target) return;
        if (isLine)
        {
            // keep UI at a fixed world position
            transform.position = fixedWorldPosition;
            // billboard towards camera (Canvas +Z faces camera)
            if (cam)
            {
                Vector3 toCam = cam.transform.position - transform.position;
                if (toCam.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
            // draw a line from target (with local offset) to this UI
            EnsureLineRendererInitializedIfNeeded();
            if (lineRenderer)
            {
                Vector3 start = target.TransformPoint(localOffset);
                Vector3 end = transform.position;

                // Apply offsets so the line doesn't overlap the UI (and optionally the target)
                Vector3 seg = end - start;
                float segLen = seg.magnitude;
                if (segLen > 1e-6f)
                {
                    Vector3 dir = seg / segLen;
                    float startOff = Mathf.Clamp(lineOffsetFromTarget, 0f, segLen);
                    float endOff = Mathf.Clamp(lineOffsetFromUI, 0f, Mathf.Max(0f, segLen - startOff));
                    start += dir * startOff;   // move start away from target
                    end   -= dir * endOff;     // move end away from UI center
                }

                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
                if (!lineRenderer.enabled) lineRenderer.enabled = true;
            }
        }
        else
        {
            // follow the joint using joint-local offset
            transform.position = target.TransformPoint(localOffset);
            // billboard towards camera (Canvas +Z faces camera)
            if (cam)
            {
                Vector3 toCam = cam.transform.position - transform.position;
                if (toCam.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
            // hide line if present
            if (lineRenderer && lineRenderer.enabled) lineRenderer.enabled = false;
        }
    }
}
