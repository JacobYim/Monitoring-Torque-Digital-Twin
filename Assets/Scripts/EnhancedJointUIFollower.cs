using UnityEngine;
using UnityEngine.UI;

public class EnhancedJointUIFollower : MonoBehaviour
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

    [Header("Anti-Occlusion")]
    [Tooltip("Always keep UI visible")]
    public bool preventOcclusion = true;
    
    [Tooltip("Layers that can occlude the UI")]
    public LayerMask occlusionLayers = -1;
    
    [Tooltip("Check for occlusion using multiple rays")]
    public bool useMultiRayOcclusion = true;
    
    [Tooltip("Number of rays for occlusion detection")]
    [Range(1, 8)]
    public int occlusionRays = 4;

    [Header("Smart Positioning")]
    [Tooltip("Automatically adjust position when occluded")]
    public bool smartPositioning = true;
    
    [Tooltip("Maximum offset from original position")]
    public float maxOffsetDistance = 0.3f;
    
    [Tooltip("Smooth movement when adjusting position")]
    public float positionSmoothSpeed = 3f;

    [Header("Visual Effects")]
    [Tooltip("Add outline when occluded")]
    public bool addOutlineWhenOccluded = true;
    
    [Tooltip("Outline color")]
    public Color outlineColor = Color.yellow;
    
    [Tooltip("Outline width")]
    [Range(0f, 0.05f)]
    public float outlineWidth = 0.01f;
    
    [Tooltip("Fade UI when occluded")]
    public bool fadeWhenOccluded = false;
    
    [Tooltip("Minimum alpha when occluded")]
    [Range(0f, 1f)]
    public float minAlpha = 0.4f;

    [Header("Debug")]
    [Tooltip("Show occlusion detection rays")]
    public bool showDebugRays = false;

    // Private variables
    private Vector3 originalOffset;
    private Vector3 targetOffset;
    private bool isOccluded = false;
    private CanvasGroup canvasGroup;
    private Outline[] outlines;
    private float currentAlpha = 1f;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        
        originalOffset = localOffset;
        targetOffset = localOffset;
        
        // Get or add canvas group for alpha control
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Get outline components
        outlines = GetComponentsInChildren<Outline>();
    }

    void Start()
    {
        // Create outline components if needed
        if (addOutlineWhenOccluded && outlines.Length == 0)
        {
            CreateOutlineComponents();
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // Update base position
        Vector3 basePosition = target.TransformPoint(localOffset);
        
        // Check for occlusion and adjust position
        if (preventOcclusion)
        {
            bool wasOccluded = isOccluded;
            isOccluded = CheckOcclusion(basePosition);
            
            if (isOccluded != wasOccluded)
            {
                OnOcclusionChanged(isOccluded);
            }
            
            if (smartPositioning && isOccluded)
            {
                targetOffset = FindBestOffset();
            }
            else if (!isOccluded)
            {
                targetOffset = originalOffset;
            }
            
            // Smoothly move to target offset
            localOffset = Vector3.Lerp(localOffset, targetOffset, positionSmoothSpeed * Time.deltaTime);
            basePosition = target.TransformPoint(localOffset);
        }

        // Apply position
        transform.position = basePosition;

        // Billboard toward camera
        if (faceCamera && cam)
        {
            Vector3 toCam = cam.transform.position - transform.position;
            if (toCam.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }

        // Distance-based scaling
        if (scaleWithDistance && cam)
        {
            float d = Vector3.Distance(cam.transform.position, transform.position);
            d = Mathf.Clamp(d, minDistance, maxDistance);
            float t = Mathf.InverseLerp(minDistance, maxDistance, d);
            float s = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = new Vector3(s, s, s);
        }
        
        // Update visual effects
        UpdateVisualEffects();
    }

    bool CheckOcclusion(Vector3 uiPosition)
    {
        if (!cam || occlusionRays <= 0) return false;
        
        Vector3 cameraPosition = cam.transform.position;
        Vector3 direction = (uiPosition - cameraPosition).normalized;
        float distance = Vector3.Distance(cameraPosition, uiPosition);
        
        // Cast multiple rays for better occlusion detection
        for (int i = 0; i < occlusionRays; i++)
        {
            Vector3 rayDirection = direction;
            
            // Add variation to rays
            if (i > 0)
            {
                float angle = (360f / occlusionRays) * i;
                Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
                if (right.sqrMagnitude < 0.1f)
                    right = Vector3.Cross(direction, Vector3.forward).normalized;
                
                rayDirection = Quaternion.AngleAxis(angle, direction) * right * 0.05f + direction;
            }
            
            Ray ray = new Ray(cameraPosition, rayDirection);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, distance, occlusionLayers))
            {
                if (showDebugRays)
                {
                    Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.red, 0.1f);
                }
                
                // Check if hit object is not the UI or its children
                if (hit.collider.gameObject != gameObject && 
                    !hit.collider.transform.IsChildOf(transform) &&
                    !hit.collider.transform.IsChildOf(target))
                {
                    return true;
                }
            }
            else if (showDebugRays)
            {
                Debug.DrawRay(ray.origin, ray.direction * distance, Color.green, 0.1f);
            }
        }
        
        return false;
    }

    Vector3 FindBestOffset()
    {
        Vector3[] testOffsets = {
            Vector3.up * 0.1f,
            Vector3.down * 0.1f,
            Vector3.left * 0.1f,
            Vector3.right * 0.1f,
            Vector3.forward * 0.1f,
            Vector3.back * 0.1f,
            Vector3.up * 0.2f,
            Vector3.up * 0.3f
        };
        
        foreach (Vector3 offset in testOffsets)
        {
            Vector3 testOffset = originalOffset + offset;
            if (testOffset.magnitude <= maxOffsetDistance)
            {
                Vector3 testPosition = target.TransformPoint(testOffset);
                if (!CheckOcclusion(testPosition))
                {
                    return testOffset;
                }
            }
        }
        
        return originalOffset; // Fallback
    }

    void OnOcclusionChanged(bool occluded)
    {
        if (occluded)
        {
            if (addOutlineWhenOccluded)
            {
                EnableOutline(true);
            }
        }
        else
        {
            EnableOutline(false);
        }
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
        // Add outline to all UI elements
        Image[] images = GetComponentsInChildren<Image>();
        Text[] texts = GetComponentsInChildren<Text>();
        
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
        
        outlines = GetComponentsInChildren<Outline>();
    }

    // Public methods
    public void SetPreventOcclusion(bool prevent)
    {
        preventOcclusion = prevent;
        if (!prevent)
        {
            EnableOutline(false);
            localOffset = originalOffset;
            targetOffset = originalOffset;
        }
    }

    public bool IsOccluded()
    {
        return isOccluded;
    }

    public void ForceUpdate()
    {
        if (target)
        {
            Vector3 testPosition = target.TransformPoint(localOffset);
            isOccluded = CheckOcclusion(testPosition);
            OnOcclusionChanged(isOccluded);
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (showDebugRays && target)
        {
            Gizmos.color = isOccluded ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
            
            if (smartPositioning && targetOffset != originalOffset)
            {
                Gizmos.color = Color.blue;
                Vector3 origPos = target.TransformPoint(originalOffset);
                Vector3 targetPos = target.TransformPoint(targetOffset);
                Gizmos.DrawLine(origPos, targetPos);
                Gizmos.DrawWireSphere(targetPos, 0.05f);
            }
        }
    }
}
