using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UIAlwaysOnTop : MonoBehaviour
{
    [Header("Rendering Settings")]
    [Tooltip("Render this UI on top of all other elements")]
    public bool alwaysOnTop = true;
    
    [Tooltip("Custom sorting order (higher = on top)")]
    public int customSortingOrder = 1000;
    
    [Tooltip("Override sorting for this canvas")]
    public bool overrideSorting = true;
    
    [Tooltip("Use world space rendering (for 3D UI)")]
    public bool useWorldSpace = true;
    
    [Tooltip("Camera for world space rendering")]
    public Camera worldCamera;

    [Header("Layer Settings")]
    [Tooltip("UI layer for occlusion culling")]
    public int uiLayer = 5;
    
    [Tooltip("Set UI layer automatically")]
    public bool setUILayer = true;

    private Canvas canvas;
    private int originalLayer;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        originalLayer = gameObject.layer;
        
        SetupCanvas();
    }

    void Start()
    {
        // Ensure setup is applied after all components are initialized
        SetupCanvas();
    }

    void SetupCanvas()
    {
        if (!canvas) return;

        // Set rendering order
        if (alwaysOnTop)
        {
            canvas.sortingOrder = customSortingOrder;
            canvas.overrideSorting = overrideSorting;
        }

        // Set world space rendering
        if (useWorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Find camera if not assigned
            if (worldCamera == null)
            {
                worldCamera = FindXRCamera();
            }
            
            if (worldCamera)
            {
                canvas.worldCamera = worldCamera;
            }
            else
            {
                Debug.LogWarning($"UIAlwaysOnTop on {gameObject.name}: No camera found. Please assign a camera or ensure Camera.main exists.", this);
            }
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // Set UI layer
        if (setUILayer)
        {
            SetUILayerRecursive(gameObject, uiLayer);
        }
    }

    void SetUILayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetUILayerRecursive(child.gameObject, layer);
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
        // Look for "XR Origin" -> "Camera Offset" -> "Main Camera"
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

    // Public methods for runtime control
    public void SetAlwaysOnTop(bool onTop)
    {
        alwaysOnTop = onTop;
        if (canvas)
        {
            canvas.sortingOrder = onTop ? customSortingOrder : 0;
            canvas.overrideSorting = onTop;
        }
    }

    public void SetSortingOrder(int order)
    {
        customSortingOrder = order;
        if (canvas && alwaysOnTop)
        {
            canvas.sortingOrder = order;
        }
    }

    public void SetWorldSpace(bool worldSpace, Camera cam = null)
    {
        useWorldSpace = worldSpace;
        if (cam) worldCamera = cam;
        SetupCanvas();
    }

    public void SetUILayer(int layer)
    {
        uiLayer = layer;
        SetUILayerRecursive(gameObject, layer);
    }

    // Reset to original settings
    public void ResetToOriginal()
    {
        if (canvas)
        {
            canvas.sortingOrder = 0;
            canvas.overrideSorting = false;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        SetUILayerRecursive(gameObject, originalLayer);
    }

    void OnValidate()
    {
        // Apply changes in editor
        if (Application.isPlaying)
        {
            SetupCanvas();
        }
    }
}
