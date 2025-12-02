using UnityEngine;

/// <summary>
/// Configures Canvas for HUD-style UI in XR environments.
/// Supports both Screen Space - Camera and World Space modes.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class XRHUDCanvas : MonoBehaviour
{
    public enum HUDMode
    {
        ScreenSpaceCamera,  // Fixed to camera viewport (recommended for HUD)
        WorldSpaceFixed,    // World space, fixed position relative to camera
        WorldSpaceBillboard // World space, always faces camera
    }

    [Header("HUD Mode")]
    [Tooltip("Rendering mode for the HUD")]
    [SerializeField]
    HUDMode m_HUDMode = HUDMode.ScreenSpaceCamera;

    [Header("Camera Settings")]
    [Tooltip("XR Camera to attach to. If not set, will auto-detect.")]
    [SerializeField]
    Camera m_XRCamera;

    [Header("Screen Space - Camera Settings")]
    [Tooltip("Distance from camera (for Screen Space - Camera mode)")]
    [SerializeField]
    float m_CameraDistance = 0.5f;

    [Tooltip("Plane distance for Screen Space - Camera mode")]
    [SerializeField]
    float m_PlaneDistance = 0.3f;

    [Header("World Space Settings")]
    [Tooltip("Offset from camera position (for World Space modes)")]
    [SerializeField]
    Vector3 m_WorldSpaceOffset = new Vector3(0, 0, 0.5f);

    [Tooltip("Follow camera position (for World Space modes)")]
    [SerializeField]
    bool m_FollowCameraPosition = true;

    [Tooltip("Use billboard effect (always face camera)")]
    [SerializeField]
    bool m_UseBillboard = false;

    [Header("Canvas Settings")]
    [Tooltip("Sorting order (higher = on top)")]
    [SerializeField]
    int m_SortingOrder = 1000;

    [Tooltip("Override sorting")]
    [SerializeField]
    bool m_OverrideSorting = true;

    [Header("Update Settings")]
    [Tooltip("Update in LateUpdate for better synchronization")]
    [SerializeField]
    bool m_UseLateUpdate = true;

    private Canvas m_Canvas;
    private RectTransform m_RectTransform;
    private Transform m_CameraTransform;

    void Awake()
    {
        m_Canvas = GetComponent<Canvas>();
        m_RectTransform = GetComponent<RectTransform>();

        // Ensure Canvas component exists
        if (m_Canvas == null)
        {
            m_Canvas = gameObject.AddComponent<Canvas>();
        }

        // Find XR camera if not assigned
        if (m_XRCamera == null)
        {
            m_XRCamera = FindXRCamera();
        }
    }

    void Start()
    {
        // Ensure setup is applied after all components are initialized
        SetupCanvas();
        
        // Try to find camera again if still null
        if (m_XRCamera == null)
        {
            m_XRCamera = FindXRCamera();
            if (m_XRCamera != null)
            {
                SetupCanvas();
            }
        }
    }

    void Update()
    {
        if (!m_UseLateUpdate && m_HUDMode != HUDMode.ScreenSpaceCamera)
        {
            UpdateHUDPosition();
        }
    }

    void LateUpdate()
    {
        if (m_UseLateUpdate && m_HUDMode != HUDMode.ScreenSpaceCamera)
        {
            UpdateHUDPosition();
        }
    }

    void SetupCanvas()
    {
        if (m_Canvas == null) return;

        // Set sorting order
        m_Canvas.sortingOrder = m_SortingOrder;
        m_Canvas.overrideSorting = m_OverrideSorting;

        // Configure based on HUD mode
        switch (m_HUDMode)
        {
            case HUDMode.ScreenSpaceCamera:
                SetupScreenSpaceCamera();
                break;

            case HUDMode.WorldSpaceFixed:
                SetupWorldSpaceFixed();
                break;

            case HUDMode.WorldSpaceBillboard:
                SetupWorldSpaceBillboard();
                break;
        }
    }

    void SetupScreenSpaceCamera()
    {
        m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
        
        if (m_XRCamera != null)
        {
            m_Canvas.worldCamera = m_XRCamera;
            m_Canvas.planeDistance = m_PlaneDistance;
            
            // Ensure Canvas is enabled
            m_Canvas.enabled = true;
            
            // Set Canvas to render on the correct display
            if (m_XRCamera.targetDisplay >= 0)
            {
                m_Canvas.targetDisplay = m_XRCamera.targetDisplay;
            }
        }
        else
        {
            Debug.LogWarning($"[XRHUDCanvas] No XR camera assigned. Please assign a camera for Screen Space - Camera mode.", this);
        }
    }

    void SetupWorldSpaceFixed()
    {
        m_Canvas.renderMode = RenderMode.WorldSpace;
        
        if (m_XRCamera != null)
        {
            m_Canvas.worldCamera = m_XRCamera;
            m_CameraTransform = m_XRCamera.transform;
            
            // Set initial position
            if (m_FollowCameraPosition)
            {
                UpdateHUDPosition();
            }
        }
        else
        {
            Debug.LogWarning($"[XRHUDCanvas] No XR camera assigned. Please assign a camera for World Space mode.", this);
        }
    }

    void SetupWorldSpaceBillboard()
    {
        SetupWorldSpaceFixed();
        m_UseBillboard = true;
    }

    void UpdateHUDPosition()
    {
        if (m_CameraTransform == null)
        {
            if (m_XRCamera != null)
            {
                m_CameraTransform = m_XRCamera.transform;
            }
            else
            {
                return;
            }
        }

        if (m_FollowCameraPosition)
        {
            // Update position relative to camera
            Vector3 worldOffset = m_CameraTransform.rotation * m_WorldSpaceOffset;
            transform.position = m_CameraTransform.position + worldOffset;
        }

        if (m_UseBillboard)
        {
            // Face the camera
            Vector3 toCamera = m_CameraTransform.position - transform.position;
            if (toCamera.sqrMagnitude > 1e-6f)
            {
                // Canvas +Z faces camera, so we use -toCamera
                transform.rotation = Quaternion.LookRotation(-toCamera.normalized, m_CameraTransform.up);
            }
        }
    }

    /// <summary>
    /// Finds the XR camera automatically
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

        // Method 4: Search for XR Rig or similar structures
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("XR") || obj.name.Contains("Camera"))
            {
                Camera cam = obj.GetComponentInChildren<Camera>();
                if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    return cam;
                }
            }
        }

        // Method 5: Find any active camera in the scene
        Camera[] allCameras = FindObjectsOfType<Camera>(true);
        foreach (Camera cam in allCameras)
        {
            if (cam.enabled && cam.gameObject.activeInHierarchy)
            {
                return cam;
            }
        }

        return null;
    }

    // Public methods for runtime control
    public void SetHUDMode(HUDMode mode)
    {
        m_HUDMode = mode;
        SetupCanvas();
    }

    public void SetXRCamera(Camera camera)
    {
        m_XRCamera = camera;
        m_CameraTransform = camera != null ? camera.transform : null;
        SetupCanvas();
    }

    public void SetWorldSpaceOffset(Vector3 offset)
    {
        m_WorldSpaceOffset = offset;
    }

    public void SetFollowCamera(bool follow)
    {
        m_FollowCameraPosition = follow;
    }

    public void SetBillboard(bool billboard)
    {
        m_UseBillboard = billboard;
    }

    void OnValidate()
    {
        // Apply changes in editor
        if (Application.isPlaying && m_Canvas != null)
        {
            SetupCanvas();
        }
    }
}

