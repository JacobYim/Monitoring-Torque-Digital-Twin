using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Applies sinusoidal rotation to articulation body joints by manually setting xDrive.target every frame.
    /// </summary>
    public class PeriodicMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Amplitude of sinusoidal movement in degrees")]
        [Range(0f, 180f)]
        public float amplitude = 30f;
        
        [Tooltip("Frequency of sinusoidal movement in Hz")]
        [Range(0.1f, 5f)]
        public float frequency = 1f;
        
        [Tooltip("Phase offset in degrees")]
        [Range(0f, 360f)]
        public float phaseOffset = 0f;
        
        [Header("Debug")]
        public bool showDebugInfo = false;
        
        private List<ArticulationBody> joints = new List<ArticulationBody>();
        private float time = 0f;
        private bool initialized = false;
        
        void OnEnable()
        {
            InitializeJoints();
        }
        
        void Start()
        {
            InitializeJoints();
        }
        
        void InitializeJoints()
        {
            if (initialized) return;
            
            joints.Clear();
            
            // Find all articulation bodies in children
            ArticulationBody[] allBodies = GetComponentsInChildren<ArticulationBody>(true);
            
            foreach (var body in allBodies)
            {
                // Skip root articulation body (usually fixed)
                if (body.isRoot)
                    continue;
                
                // Only add joints that have XDrive enabled (revolute joints)
                var xDrive = body.xDrive;
                if (xDrive.lowerLimit != xDrive.upperLimit) // Has valid limits
                {
                    joints.Add(body);
                    if (showDebugInfo)
                    {
                        Debug.Log($"[PeriodicMovement] Found joint: {body.name}, Limits: [{xDrive.lowerLimit:F2}, {xDrive.upperLimit:F2}]");
                    }
                }
            }
            
            initialized = true;
            Debug.Log($"[PeriodicMovement] Initialized on {gameObject.name}. Found {joints.Count} joints.");
        }
        
        void FixedUpdate()
        {
            if (joints.Count < 3) return; // Need at least 3 joints (0-indexed: need indices 1 and 2)
            
            // Update time
            time += Time.fixedDeltaTime;
            
            // Only move the second and third joints (indices 1 and 2)
            for (int i = 1; i <= 2 && i < joints.Count; i++)
            {
                var joint = joints[i];
                if (joint == null) continue;
                
                // Calculate phase: second joint (i=1) has 0 offset, third joint (i=2) has 180 degree offset
                float jointPhaseOffset = (i == 1) ? 0f : 180f; // 180 degree phase difference
                float phase = (time * frequency * 2f * Mathf.PI) + (phaseOffset * Mathf.Deg2Rad) + (jointPhaseOffset * Mathf.Deg2Rad);
                float sinusoidalValue = Mathf.Sin(phase) * amplitude;
                
                // Get current drive settings
                var xDrive = joint.xDrive;
                
                // Calculate target within joint limits
                float center = (xDrive.lowerLimit + xDrive.upperLimit) * 0.5f;
                float target = center + sinusoidalValue;
                
                // Clamp to limits
                target = Mathf.Clamp(target, xDrive.lowerLimit, xDrive.upperLimit);
                
                // Set the target
                xDrive.target = target;
                joint.xDrive = xDrive;
                
                if (showDebugInfo)
                {
                    Debug.Log($"[PeriodicMovement] Joint {i} ({joint.name}): Target={target:F2}°, Sin={sinusoidalValue:F2}°, PhaseOffset={jointPhaseOffset}°, Time={time:F2}s");
                }
            }
        }
        
        void OnValidate()
        {
            // Reinitialize in editor
            if (Application.isPlaying)
            {
                initialized = false;
                InitializeJoints();
            }
        }
    }
}

