using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class UIAlwaysVisible : MonoBehaviour
{
    [Header("Visibility Settings")]
    [Tooltip("Always render UI on top of 3D objects")]
    public bool alwaysOnTop = true;
    
    [Tooltip("Use outline/glow effect when occluded")]
    public bool useOutlineWhenOccluded = true;
    
    [Tooltip("Fade UI when occluded instead of outline")]
    public bool fadeWhenOccluded = false;
    
    [Tooltip("Minimum alpha when occluded")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;

    [Header("Occlusion Detection")]
    [Tooltip("Layers to check for occlusion (exclude UI layer)")]
    public LayerMask occlusionLayers = -1;
    
    [Tooltip("Distance to check for occlusion")]
    public float occlusionCheckDistance = 10f;
    
    [Tooltip("Number of rays to cast for occlusion detection")]
    [Range(1, 8)]
    public int occlusionRays = 4;

    [Header("Smart Positioning")]
    [Tooltip("Automatically adjust position to avoid occlusion")]
    public bool smartPositioning = true;
    
    [Tooltip("Maximum offset distance for smart positioning")]
    public float maxOffsetDistance = 0.5f;
    
    [Tooltip("Smooth movement speed for position adjustment")]
    public float positionSmoothSpeed = 2f;

    [Header("Outline Effect")]
    [Tooltip("Outline color when occluded")]
    public Color outlineColor = Color.yellow;
    
    [Tooltip("Outline width")]
    [Range(0f, 0.1f)]
    public float outlineWidth = 0.02f;
    
    [Tooltip("Outline intensity")]
    [Range(0f, 2f)]
    public float outlineIntensity = 1f;

    [Header("Debug")]
    [Tooltip("Show occlusion rays in scene view")]
    public bool showDebugRays = false;

    // Private variables
    private Canvas canvas;
    private Camera mainCamera;
    private CanvasGroup canvasGroup;
    private Outline[] outlines;
    private Image[] images;
    private Text[] texts;
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isOccluded = false;
    private float currentAlpha = 1f;

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main;
        if (!mainCamera) mainCamera = FindObjectOfType<Camera>();
        
        // Ensure canvas renders on top
        if (alwaysOnTop)
        {
            canvas.sortingOrder = 1000;
            canvas.overrideSorting = true;
        }
        
        // Get or add canvas group for alpha control
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Get UI components for outline effect
        outlines = GetComponentsInChildren<Outline>();
        images = GetComponentsInChildren<Image>();
        texts = GetComponentsInChildren<Text>();
        
        originalPosition = transform.position;
        targetPosition = originalPosition;
    }

    void Start()
    {
        // Create outline components if needed
        if (useOutlineWhenOccluded && outlines.Length == 0)
        {
            CreateOutlineComponents();
        }
    }

    void LateUpdate()
    {
        if (!mainCamera) return;

        // Check for occlusion
        bool wasOccluded = isOccluded;
        isOccluded = CheckOcclusion();
        
        // Handle visibility changes
        if (isOccluded != wasOccluded)
        {
            OnOcclusionChanged(isOccluded);
        }
        
        // Smart positioning
        if (smartPositioning && isOccluded)
        {
            UpdateSmartPosition();
        }
        
        // Update visual effects
        UpdateVisualEffects();
    }

    bool CheckOcclusion()
    {
        if (occlusionRays <= 0) return false;
        
        Vector3 uiPosition = transform.position;
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 direction = (uiPosition - cameraPosition).normalized;
        float distance = Vector3.Distance(cameraPosition, uiPosition);
        
        // Cast multiple rays to check for occlusion
        for (int i = 0; i < occlusionRays; i++)
        {
            Vector3 rayDirection = direction;
            
            // Add slight variation to rays for better coverage
            if (i > 0)
            {
                float angle = (360f / occlusionRays) * i;
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
                Vector3 up = Vector3.Cross(right, direction).normalized;
                rayDirection = Quaternion.AngleAxis(angle, direction) * right * 0.1f + direction;
            }
            
            Ray ray = new Ray(cameraPosition, rayDirection);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, Mathf.Min(distance, occlusionCheckDistance), occlusionLayers))
            {
                if (showDebugRays)
                {
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 0.1f);
                }
                
                // Check if the hit object is not the UI itself
                if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
            else if (showDebugRays)
            {
                Debug.DrawRay(ray.origin, ray.direction * Mathf.Min(distance, occlusionCheckDistance), Color.green, 0.1f);
            }
        }
        
        return false;
    }

    void OnOcclusionChanged(bool occluded)
    {
        if (occluded)
        {
            // UI became occluded
            if (useOutlineWhenOccluded)
            {
                EnableOutline(true);
            }
            
            if (fadeWhenOccluded)
            {
                targetPosition = FindBestVisiblePosition();
            }
        }
        else
        {
            // UI became visible
            EnableOutline(false);
            targetPosition = originalPosition;
        }
    }

    void UpdateSmartPosition()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);
        }
    }

    Vector3 FindBestVisiblePosition()
    {
        Vector3 cameraPosition = mainCamera.transform.position;
        Vector3 uiPosition = originalPosition;
        Vector3 direction = (uiPosition - cameraPosition).normalized;
        
        // Try different offset positions
        Vector3[] offsets = {
            Vector3.up * 0.1f,
            Vector3.down * 0.1f,
            Vector3.left * 0.1f,
            Vector3.right * 0.1f,
            Vector3.forward * 0.1f,
            Vector3.back * 0.1f
        };
        
        foreach (Vector3 offset in offsets)
        {
            Vector3 testPosition = uiPosition + offset;
            float distance = Vector3.Distance(cameraPosition, testPosition);
            
            if (distance <= maxOffsetDistance)
            {
                // Check if this position is visible
                Vector3 testDirection = (testPosition - cameraPosition).normalized;
                Ray ray = new Ray(cameraPosition, testDirection);
                RaycastHit hit;
                
                if (!Physics.Raycast(ray, out hit, distance, occlusionLayers) || 
                    hit.collider.gameObject == gameObject || 
                    hit.collider.transform.IsChildOf(transform))
                {
                    return testPosition;
                }
            }
        }
        
        return originalPosition; // Fallback to original position
    }

    void UpdateVisualEffects()
    {
        if (fadeWhenOccluded)
        {
            float targetAlpha = isOccluded ? minAlpha : 1f;
            currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, 5f * Time.deltaTime);
            canvasGroup.alpha = currentAlpha;
        }
    }

    void EnableOutline(bool enable)
    {
        foreach (Outline outline in outlines)
        {
            if (outline != null)
            {
                outline.enabled = enable;
                if (enable)
                {
                    outline.effectColor = outlineColor;
                    outline.effectDistance = Vector2.one * outlineWidth;
                }
            }
        }
    }

    void CreateOutlineComponents()
    {
        // Add outline to images
        foreach (Image image in images)
        {
            if (image != null && image.GetComponent<Outline>() == null)
            {
                Outline outline = image.gameObject.AddComponent<Outline>();
                outline.effectColor = outlineColor;
                outline.effectDistance = Vector2.one * outlineWidth;
                outline.enabled = false;
            }
        }
        
        // Add outline to texts
        foreach (Text text in texts)
        {
            if (text != null && text.GetComponent<Outline>() == null)
            {
                Outline outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = outlineColor;
                outline.effectDistance = Vector2.one * outlineWidth;
                outline.enabled = false;
            }
        }
        
        // Refresh outline array
        outlines = GetComponentsInChildren<Outline>();
    }

    // Public methods for external control
    public void SetAlwaysOnTop(bool onTop)
    {
        alwaysOnTop = onTop;
        if (canvas != null)
        {
            canvas.sortingOrder = onTop ? 1000 : 0;
            canvas.overrideSorting = onTop;
        }
    }

    public void SetOcclusionLayers(LayerMask layers)
    {
        occlusionLayers = layers;
    }

    public bool IsOccluded()
    {
        return isOccluded;
    }

    public void ForceUpdate()
    {
        isOccluded = CheckOcclusion();
        OnOcclusionChanged(isOccluded);
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (showDebugRays && mainCamera != null)
        {
            Gizmos.color = isOccluded ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            if (smartPositioning && targetPosition != originalPosition)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(originalPosition, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.05f);
            }
        }
    }
}
