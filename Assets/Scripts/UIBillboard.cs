using UnityEngine;

/// <summary>
/// Makes any UI Canvas always face the camera (billboard effect).
/// Works with World Space Canvas in XR environments.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UIBillboard : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Camera to face. If not set, will automatically find the XR camera or use Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Whether to always face the camera.")]
    public bool faceCamera = true;

    [Header("Rotation Settings")]
    [Tooltip("Lock the X rotation (only rotate around Y axis).")]
    public bool lockXAxis = false;

    [Tooltip("Lock the Y rotation (only rotate around X axis).")]
    public bool lockYAxis = false;

    [Tooltip("Lock the Z rotation.")]
    public bool lockZAxis = false;

    [Header("Update Settings")]
    [Tooltip("Update in LateUpdate for better synchronization (recommended).")]
    public bool useLateUpdate = true;

    [Tooltip("Update in Update instead of LateUpdate.")]
    public bool useUpdate = false;

    private Canvas canvas;

    void Awake()
    {
        canvas = GetComponent<Canvas>();

        // Find camera if not assigned
        if (targetCamera == null)
        {
            targetCamera = FindXRCamera();
        }

        // Ensure canvas is in World Space mode
        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            if (targetCamera != null)
            {
                canvas.worldCamera = targetCamera;
            }
        }
    }

    void Update()
    {
        if (!useLateUpdate && useUpdate && faceCamera)
        {
            UpdateBillboard();
        }
    }

    void LateUpdate()
    {
        if ((useLateUpdate || !useUpdate) && faceCamera)
        {
            UpdateBillboard();
        }
    }

    void UpdateBillboard()
    {
        if (targetCamera == null)
        {
            // Try to find camera again in case it wasn't available at Awake
            targetCamera = FindXRCamera();
            if (targetCamera == null)
                return;
        }

        Vector3 toCamera = targetCamera.transform.position - transform.position;
        
        if (toCamera.sqrMagnitude > 1e-6f)
        {
            // Canvas +Z faces camera, so we use -toCamera
            Quaternion targetRotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);

            // Apply axis locks
            if (lockXAxis || lockYAxis || lockZAxis)
            {
                Vector3 euler = targetRotation.eulerAngles;
                
                if (lockXAxis)
                    euler.x = transform.rotation.eulerAngles.x;
                if (lockYAxis)
                    euler.y = transform.rotation.eulerAngles.y;
                if (lockZAxis)
                    euler.z = transform.rotation.eulerAngles.z;

                targetRotation = Quaternion.Euler(euler);
            }

            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Finds the XR camera automatically. Tries multiple methods to find the active XR camera.
    /// </summary>
    Camera FindXRCamera()
    {
        // Method 1: Try Camera.main (most common)
        if (Camera.main != null)
        {
            return Camera.main;
        }

        // Method 2: Find camera tagged as MainCamera
        GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (mainCamObj != null)
        {
            Camera cam = mainCamObj.GetComponent<Camera>();
            if (cam != null) return cam;
        }

        // Method 3: Search for camera in XR Origin structure
        GameObject xrOrigin = GameObject.Find("XR Origin");
        if (xrOrigin != null)
        {
            Transform cameraOffset = xrOrigin.transform.Find("Camera Offset");
            if (cameraOffset != null)
            {
                Transform mainCam = cameraOffset.Find("Main Camera");
                if (mainCam != null)
                {
                    Camera cam = mainCam.GetComponent<Camera>();
                    if (cam != null) return cam;
                }
            }
        }

        // Method 4: Find any active camera in the scene
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                return cam;
            }
        }

        return null;
    }

    /// <summary>
    /// Sets whether the UI should face the camera.
    /// </summary>
    public void SetFaceCamera(bool face)
    {
        faceCamera = face;
    }

    /// <summary>
    /// Sets the target camera.
    /// </summary>
    public void SetTargetCamera(Camera cam)
    {
        targetCamera = cam;
        if (canvas != null && cam != null)
        {
            canvas.worldCamera = cam;
        }
    }

    void OnValidate()
    {
        // Ensure only one update method is used
        if (useUpdate && useLateUpdate)
        {
            useLateUpdate = false;
        }
    }
}

