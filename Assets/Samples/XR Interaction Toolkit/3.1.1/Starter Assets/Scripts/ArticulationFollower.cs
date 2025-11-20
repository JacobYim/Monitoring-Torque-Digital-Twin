using UnityEngine;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Makes an ArticulationBody follow the root transform of this GameObject.
    /// Simply updates the articulation body position to match the root transform every frame.
    /// </summary>
    public class ArticulationFollower : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The ArticulationBody to control. If not set, will search for one in children.")]
        ArticulationBody m_TargetArticulationBody;

        [SerializeField]
        [Tooltip("Whether to follow the rotation of the root transform.")]
        bool m_FollowRotation = true;

        [SerializeField]
        [Tooltip("Optional offset for the articulation body position relative to the root.")]
        Vector3 m_PositionOffset = Vector3.zero;

        /// <summary>
        /// The ArticulationBody that will follow the root transform.
        /// </summary>
        public ArticulationBody targetArticulationBody
        {
            get => m_TargetArticulationBody;
            set => m_TargetArticulationBody = value;
        }

        void Awake()
        {
            // Find the ArticulationBody if not assigned
            if (m_TargetArticulationBody == null)
            {
                // Try to find the root articulation body (one without a parent articulation body)
                ArticulationBody[] allBodies = GetComponentsInChildren<ArticulationBody>();
                
                foreach (var body in allBodies)
                {
                    // Check if this is a root articulation body (no parent with ArticulationBody)
                    Transform parent = body.transform.parent;
                    if (parent == null || parent.GetComponent<ArticulationBody>() == null)
                    {
                        m_TargetArticulationBody = body;
                        break;
                    }
                }
                
                // Fallback: just use the first one found
                if (m_TargetArticulationBody == null && allBodies.Length > 0)
                {
                    m_TargetArticulationBody = allBodies[0];
                }
                
                if (m_TargetArticulationBody == null)
                {
                    Debug.LogWarning($"ArticulationFollower on {gameObject.name}: No ArticulationBody found. Please assign one or ensure there's an ArticulationBody in the children.", this);
                    enabled = false;
                    return;
                }
            }
        }

        void LateUpdate()
        {
            if (m_TargetArticulationBody == null)
                return;

            // Calculate target position and rotation - same as how regular prefab follows
            Vector3 targetPosition = transform.position + transform.TransformVector(m_PositionOffset);
            Quaternion targetRotation = m_FollowRotation ? transform.rotation : m_TargetArticulationBody.transform.rotation;

            // Update BOTH the articulation body AND the visual transform
            // This ensures the visuals match the physics position
            
            // 1. Update the visual transform directly (for rendering)
            m_TargetArticulationBody.transform.position = targetPosition;
            if (m_FollowRotation)
            {
                m_TargetArticulationBody.transform.rotation = targetRotation;
            }
            
            // 2. Teleport the articulation body physics (for collisions)
            m_TargetArticulationBody.TeleportRoot(targetPosition, targetRotation);
        }

        void OnValidate()
        {
            // In editor, try to find the ArticulationBody if not assigned
            if (m_TargetArticulationBody == null && Application.isPlaying == false)
            {
                m_TargetArticulationBody = GetComponentInChildren<ArticulationBody>();
            }
        }
    }
}

