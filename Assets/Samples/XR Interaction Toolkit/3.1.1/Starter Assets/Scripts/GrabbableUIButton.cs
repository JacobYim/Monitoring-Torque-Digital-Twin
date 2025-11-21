using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// A UI button that is grab interactible and always faces the camera (billboard).
    /// Includes crosspad arrows for directional input.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GrabbableUIButton : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [Tooltip("Camera to face. If not set, will use Camera.main or find XR camera.")]
        public Camera targetCamera;

        [Tooltip("Whether to always face the camera.")]
        public bool faceCamera = true;

        [Header("Crosspad Arrows")]
        [Tooltip("Up arrow button (optional)")]
        public Button upArrow;

        [Tooltip("Down arrow button (optional)")]
        public Button downArrow;

        [Tooltip("Left arrow button (optional)")]
        public Button leftArrow;

        [Tooltip("Right arrow button (optional)")]
        public Button rightArrow;

        [Header("Arrow Events")]
        [Tooltip("Called when up arrow is pressed")]
        public UnityEngine.Events.UnityEvent onUpPressed;

        [Tooltip("Called when down arrow is pressed")]
        public UnityEngine.Events.UnityEvent onDownPressed;

        [Tooltip("Called when left arrow is pressed")]
        public UnityEngine.Events.UnityEvent onLeftPressed;

        [Tooltip("Called when right arrow is pressed")]
        public UnityEngine.Events.UnityEvent onRightPressed;

        private Canvas canvas;
        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private Collider col;

        void Awake()
        {
            canvas = GetComponent<Canvas>();
            grabInteractable = GetComponent<XRGrabInteractable>();

            // Ensure we have a Rigidbody for grab interactible
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Make kinematic so it doesn't fall
                rb.useGravity = false;
            }

            // Ensure we have a Collider for grab interactible
            col = GetComponent<Collider>();
            if (col == null)
            {
                // Add a BoxCollider that matches the Canvas size
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    boxCollider.size = new Vector3(rectTransform.rect.width, rectTransform.rect.height, 0.01f);
                }
                col = boxCollider;
            }

            // Find camera if not assigned
            if (targetCamera == null)
            {
                targetCamera = FindXRCamera();
            }

            // Setup arrow button listeners
            SetupArrowButtons();
        }

        void Start()
        {
            // Ensure canvas is set to World Space for XR
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                if (targetCamera != null)
                {
                    canvas.worldCamera = targetCamera;
                }
            }

            // Set high sorting order to render on top
            canvas.sortingOrder = 1000;
            canvas.overrideSorting = true;
        }

        void LateUpdate()
        {
            // Billboard: Always face the camera
            if (faceCamera && targetCamera != null)
            {
                Vector3 toCamera = targetCamera.transform.position - transform.position;
                if (toCamera.sqrMagnitude > 1e-6f)
                {
                    // Canvas +Z faces camera, so we use -toCamera
                    transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                }
            }
        }

        void SetupArrowButtons()
        {
            if (upArrow != null)
            {
                upArrow.onClick.AddListener(() => onUpPressed?.Invoke());
            }

            if (downArrow != null)
            {
                downArrow.onClick.AddListener(() => onDownPressed?.Invoke());
            }

            if (leftArrow != null)
            {
                leftArrow.onClick.AddListener(() => onLeftPressed?.Invoke());
            }

            if (rightArrow != null)
            {
                rightArrow.onClick.AddListener(() => onRightPressed?.Invoke());
            }
        }

        /// <summary>
        /// Finds the XR camera automatically.
        /// </summary>
        Camera FindXRCamera()
        {
            // Method 1: Try Camera.main
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

            // Method 4: Find any active camera
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
        /// Manually triggers an arrow direction.
        /// </summary>
        public void TriggerArrow(string direction)
        {
            switch (direction.ToLower())
            {
                case "up":
                    onUpPressed?.Invoke();
                    break;
                case "down":
                    onDownPressed?.Invoke();
                    break;
                case "left":
                    onLeftPressed?.Invoke();
                    break;
                case "right":
                    onRightPressed?.Invoke();
                    break;
            }
        }
    }
}

