using UnityEngine;
using System.Collections.Generic;

public class PeriodicMovement : MonoBehaviour
{
    [System.Serializable]
    public class JointConfig
    {
        [Header("Joint Configuration")]
        public string jointName;
        public ArticulationBody articulationBody;
        
        [Header("Movement Parameters")]
        [Range(0f, 180f)]
        public float amplitude = 80f; // Amplitude in degrees
        
        [Range(0.1f, 10f)]
        public float frequency = 0.25f; // Frequency in Hz
        
        [Range(0f, 360f)]
        public float phaseOffset = 0f; // Phase offset in degrees
        
        [Range(-180f, 180f)]
        public float centerPosition = 0f; // Center position around which to oscillate
        
        [Header("Safety")]
        public bool useJointLimits = true;
        public bool enableMovement = true;
        
        [Header("Debug")]
        public bool showDebugInfo = false;
        
        // Internal variables
        [HideInInspector]
        public float currentTarget;
        [HideInInspector]
        public float currentTime;
    }
    
    [Header("General Settings")]
    public bool enableAllMovement = true;
    public bool useGlobalTimeScale = true;
    public float globalTimeScale = 1f;
    
    [Header("Joint Configurations")]
    public List<JointConfig> jointConfigs = new List<JointConfig>();
    
    [Header("Debug")]
    public bool showGlobalDebugInfo = false;
    public KeyCode toggleMovementKey = KeyCode.Space;
    public KeyCode resetPositionKey = KeyCode.R;
    
    private float globalTime = 0f;
    private bool isMovementEnabled = true;
    
    void Start()
    {
        // Initialize joint configurations
        InitializeJoints();
    }
    
    void Update()
    {
        // Handle input
        HandleInput();
        
        // Update global time
        if (useGlobalTimeScale)
        {
            globalTime += Time.deltaTime * globalTimeScale;
        }
        else
        {
            globalTime = Time.time;
        }
        
        // Update joint movements
        if (isMovementEnabled && enableAllMovement)
        {
            UpdateJointMovements();
        }
    }
    
    void InitializeJoints()
    {
        // If no joints are configured, try to find them automatically
        if (jointConfigs.Count == 0)
        {
            FindAndConfigureJoints();
        }
        
        // Initialize each joint
        foreach (var joint in jointConfigs)
        {
            if (joint.articulationBody != null)
            {
                joint.currentTime = 0f;
                joint.currentTarget = joint.centerPosition;
            }
        }
    }
    
    void FindAndConfigureJoints()
    {
        // Find all articulation bodies in the robot
        ArticulationBody[] articulationBodies = GetComponentsInChildren<ArticulationBody>();
        
        foreach (var body in articulationBodies)
        {
            // Skip the base (root) articulation body as it's usually fixed
            if (body.isRoot)
                continue;
                
            JointConfig newJoint = new JointConfig
            {
                jointName = body.name,
                articulationBody = body,
                amplitude = 30f,
                frequency = 1f,
                phaseOffset = 0f,
                centerPosition = 0f,
                useJointLimits = true,
                enableMovement = true,
                showDebugInfo = false
            };
            
            jointConfigs.Add(newJoint);
        }
        
        Debug.Log($"Found {jointConfigs.Count} joints for periodic movement");
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(toggleMovementKey))
        {
            isMovementEnabled = !isMovementEnabled;
            Debug.Log($"Movement {(isMovementEnabled ? "enabled" : "disabled")}");
        }
        
        if (Input.GetKeyDown(resetPositionKey))
        {
            ResetAllJointsToCenter();
        }
    }
    
    void UpdateJointMovements()
    {
        foreach (var joint in jointConfigs)
        {
            if (joint.articulationBody == null || !joint.enableMovement)
                continue;
                
            // Calculate sinusoidal target
            float time = useGlobalTimeScale ? globalTime : Time.time;
            float phase = (time * joint.frequency * 2f * Mathf.PI) + (joint.phaseOffset * Mathf.Deg2Rad);
            float sinusoidalValue = Mathf.Sin(phase) * joint.amplitude;
            joint.currentTarget = joint.centerPosition + sinusoidalValue;
            
            // Apply safety limits
            if (joint.useJointLimits)
            {
                joint.currentTarget = ApplyJointLimits(joint);
            }
            
            // Set the joint target
            SetJointTarget(joint);
            
            // Update debug info
            if (joint.showDebugInfo || showGlobalDebugInfo)
            {
                Debug.Log($"{joint.jointName}: Target = {joint.currentTarget:F2}°, Time = {time:F2}s");
            }
        }
    }
    
    float ApplyJointLimits(JointConfig joint)
    {
        if (joint.articulationBody == null)
            return joint.currentTarget;
            
        // Get joint limits from the articulation body
        var xDrive = joint.articulationBody.xDrive;
        float lowerLimit = xDrive.lowerLimit;
        float upperLimit = xDrive.upperLimit;
        
        // Clamp the target within limits
        return Mathf.Clamp(joint.currentTarget, lowerLimit, upperLimit);
    }
    
    void SetJointTarget(JointConfig joint)
    {
        if (joint.articulationBody == null)
            return;
            
        // Set the target for the X drive (most common for revolute joints)
        var xDrive = joint.articulationBody.xDrive;
        xDrive.target = joint.currentTarget;
        joint.articulationBody.xDrive = xDrive;
    }
    
    void ResetAllJointsToCenter()
    {
        foreach (var joint in jointConfigs)
        {
            if (joint.articulationBody == null)
                continue;
                
            joint.currentTarget = joint.centerPosition;
            SetJointTarget(joint);
        }
        
        Debug.Log("All joints reset to center positions");
    }
    
    // Public methods for external control
    public void SetJointMovement(int jointIndex, bool enabled)
    {
        if (jointIndex >= 0 && jointIndex < jointConfigs.Count)
        {
            jointConfigs[jointIndex].enableMovement = enabled;
        }
    }
    
    public void SetJointAmplitude(int jointIndex, float amplitude)
    {
        if (jointIndex >= 0 && jointIndex < jointConfigs.Count)
        {
            jointConfigs[jointIndex].amplitude = Mathf.Clamp(amplitude, 0f, 180f);
        }
    }
    
    public void SetJointFrequency(int jointIndex, float frequency)
    {
        if (jointIndex >= 0 && jointIndex < jointConfigs.Count)
        {
            jointConfigs[jointIndex].frequency = Mathf.Clamp(frequency, 0.1f, 10f);
        }
    }
    
    public void SetGlobalMovement(bool enabled)
    {
        isMovementEnabled = enabled;
    }
    
    public void SetGlobalTimeScale(float timeScale)
    {
        globalTimeScale = Mathf.Clamp(timeScale, 0.1f, 5f);
    }
    
    // Gizmos for visualization
    void OnDrawGizmos()
    {
        if (!showGlobalDebugInfo)
            return;
            
        // Draw debug information for each joint
        foreach (var joint in jointConfigs)
        {
            if (joint.articulationBody == null || !joint.showDebugInfo)
                continue;
                
            // Draw a line indicating the current target position
            Vector3 jointPosition = joint.articulationBody.transform.position;
            Vector3 direction = joint.articulationBody.transform.right; // Assuming X-axis rotation
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(jointPosition, jointPosition + direction * 0.1f);
            
            // Draw amplitude range
            Gizmos.color = Color.yellow;
            Vector3 amplitudeStart = jointPosition + direction * (joint.centerPosition - joint.amplitude) * 0.01f;
            Vector3 amplitudeEnd = jointPosition + direction * (joint.centerPosition + joint.amplitude) * 0.01f;
            Gizmos.DrawLine(amplitudeStart, amplitudeEnd);
        }
    }
}
