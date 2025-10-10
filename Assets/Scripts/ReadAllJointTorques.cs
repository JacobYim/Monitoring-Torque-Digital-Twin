using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ReadAllJointTorques : MonoBehaviour
{
    [Header("Scan")]
    [Tooltip("If set, finds all ArticulationBody components in children. If unset, only this GameObject's component is used.")]
    public bool includeChildren = true;

    [Tooltip("Include inactive GameObjects when scanning children.")]
    public bool includeInactive = false;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float lerp = 0.2f;

    [Header("Torque Computation")]
    [Tooltip("Include gravity compensation in total torque")]
    public bool includeGravityCompensation = true;
    
    [Tooltip("Include Coriolis/centrifugal terms in total torque")]
    public bool includeCoriolisTerms = true;
    
    [Tooltip("Include PD target torque in total torque")]
    public bool includePDTargetTorque = true;
    
    [Tooltip("Show debug info for torque components")]
    public bool showTorqueDebug = false;

    // Cached joints and per-joint state
    private List<ArticulationBody> joints = new List<ArticulationBody>();
    private Dictionary<ArticulationBody, float> smoothedTorqueByJoint = new Dictionary<ArticulationBody, float>();
    private Dictionary<string, ArticulationBody> jointByName = new Dictionary<string, ArticulationBody>();
    
    // Per-joint torque components for debugging
    private Dictionary<ArticulationBody, float> gravityTorque = new Dictionary<ArticulationBody, float>();
    private Dictionary<ArticulationBody, float> coriolisTorque = new Dictionary<ArticulationBody, float>();
    private Dictionary<ArticulationBody, float> pdTargetTorque = new Dictionary<ArticulationBody, float>();
    private Dictionary<ArticulationBody, float> rawJointForce = new Dictionary<ArticulationBody, float>();

    // Scratch buffer for snapshot queries
    private readonly List<float> lastEfforts = new List<float>();

    void OnEnable()
    {
        RefreshJoints();
    }

    public void RefreshJoints()
    {
        joints.Clear();
        smoothedTorqueByJoint.Clear();
        jointByName.Clear();
        gravityTorque.Clear();
        coriolisTorque.Clear();
        pdTargetTorque.Clear();
        rawJointForce.Clear();

        if (includeChildren)
        {
            var found = GetComponentsInChildren<ArticulationBody>(includeInactive);
            for (int i = 0; i < found.Length; i++)
            {
                AddJoint(found[i]);
            }
        }
        else
        {
            var single = GetComponent<ArticulationBody>();
            if (single) AddJoint(single);
        }
    }

    private void AddJoint(ArticulationBody body)
    {
        if (!body) return;
        if (joints.Contains(body)) return;
        joints.Add(body);
        smoothedTorqueByJoint[body] = 0f;
        gravityTorque[body] = 0f;
        coriolisTorque[body] = 0f;
        pdTargetTorque[body] = 0f;
        rawJointForce[body] = 0f;
        string key = body.name;
        if (!jointByName.ContainsKey(key))
            jointByName.Add(key, body);
    }

    void FixedUpdate()
    {
        // Update total torque per joint (raw + gravity + coriolis + PD)
        for (int i = 0; i < joints.Count; i++)
        {
            var ab = joints[i];
            if (!ab) continue;

            // Get raw joint force
            var jf = ab.jointForce; // ArticulationReducedSpace
            float rawEffort = 0f;
            if (jf.dofCount > 0)
            {
                rawEffort = jf[0]; // Most robot joints are single DoF
            }
            rawJointForce[ab] = rawEffort;

            // Compute torque components
            float gravityComp = includeGravityCompensation ? ComputeGravityCompensation(ab) : 0f;
            float coriolisComp = includeCoriolisTerms ? ComputeCoriolisTorque(ab) : 0f;
            float pdComp = includePDTargetTorque ? ComputePDTargetTorque(ab) : 0f;

            // Store components for debugging
            gravityTorque[ab] = gravityComp;
            coriolisTorque[ab] = coriolisComp;
            pdTargetTorque[ab] = pdComp;

            // Total torque = raw joint force + all computed components
            float totalTorque = rawEffort + gravityComp + coriolisComp + pdComp;

            // Apply smoothing
            float prev = smoothedTorqueByJoint[ab];
            float smooth = Mathf.Lerp(prev, totalTorque, lerp);
            smoothedTorqueByJoint[ab] = smooth;

            // Debug output
            if (showTorqueDebug)
            {
                Debug.Log($"{ab.name}: Raw={rawEffort:F2}, Gravity={gravityComp:F2}, Coriolis={coriolisComp:F2}, PD={pdComp:F2}, Total={totalTorque:F2}");
            }
        }
    }

    private float ComputeGravityCompensation(ArticulationBody ab)
    {
        if (!ab) return 0f;
        
        // Get gravity vector in world space
        Vector3 gravity = Physics.gravity;
        
        // For revolute joints, compute gravity torque about joint axis
        if (ab.jointType == ArticulationJointType.RevoluteJoint)
        {
            // Get joint axis in world space
            Vector3 jointAxis = ab.transform.TransformDirection(Vector3.right); // Assuming X-axis rotation
            
            // Get center of mass position relative to joint
            Vector3 comOffset = ab.centerOfMass - ab.transform.position;
            
            // Compute gravity torque: τ = r × (m * g) · axis
            Vector3 gravityForce = ab.mass * gravity;
            Vector3 torqueVector = Vector3.Cross(comOffset, gravityForce);
            float gravityTorque = Vector3.Dot(torqueVector, jointAxis);
            
            return gravityTorque;
        }
        
        return 0f;
    }

    private float ComputeCoriolisTorque(ArticulationBody ab)
    {
        if (!ab) return 0f;
        
        // Simplified Coriolis/centrifugal computation
        // For a more accurate model, you'd need the full Jacobian and mass matrix
        
        // Get joint velocity
        var jv = ab.jointVelocity;
        float jointVel = jv.dofCount > 0 ? jv[0] : 0f;
        
        // Get joint position
        var jp = ab.jointPosition;
        float jointPos = jp.dofCount > 0 ? jp[0] : 0f;
        
        // Simplified Coriolis term: C(q, q̇) ≈ c * q̇² * sin(q)
        // This is a rough approximation - real Coriolis matrix is more complex
        float coriolisCoeff = 0.1f; // Adjust based on your robot
        float coriolisTerm = coriolisCoeff * jointVel * jointVel * Mathf.Sin(jointPos);
        
        return coriolisTerm;
    }

    private float ComputePDTargetTorque(ArticulationBody ab)
    {
        if (!ab) return 0f;
        
        // Get PD controller parameters
        var xDrive = ab.xDrive;
        float kp = xDrive.stiffness;
        float kd = xDrive.damping;
        
        // Get position and velocity errors
        var jp = ab.jointPosition;
        var jv = ab.jointVelocity;
        
        float currentPos = jp.dofCount > 0 ? jp[0] : 0f;
        float currentVel = jv.dofCount > 0 ? jv[0] : 0f;
        float targetPos = xDrive.target;
        
        // PD control law: τ = Kp * (target - current) - Kd * velocity
        float positionError = targetPos - currentPos;
        float pdTorque = kp * positionError - kd * currentVel;
        
        return pdTorque;
    }

    // Accessors
    public int JointCount => joints.Count;

    public ArticulationBody GetJoint(int index)
    {
        if (index < 0 || index >= joints.Count) return null;
        return joints[index];
    }

    public float GetTorqueNm(ArticulationBody joint)
    {
        if (!joint) return 0f;
        if (smoothedTorqueByJoint.TryGetValue(joint, out float value))
            return value;
        return 0f;
    }

    public float GetTorqueNmByIndex(int index)
    {
        var j = GetJoint(index);
        return GetTorqueNm(j);
    }

    public float GetTorqueNmByName(string jointName)
    {
        if (string.IsNullOrEmpty(jointName)) return 0f;
        if (jointByName.TryGetValue(jointName, out var j))
            return GetTorqueNm(j);
        return 0f;
    }

    public bool TryGetTorqueNm(string jointName, out float torqueNm)
    {
        torqueNm = 0f;
        if (jointByName.TryGetValue(jointName, out var j))
        {
            torqueNm = GetTorqueNm(j);
            return true;
        }
        return false;
    }

    // Snapshot arrays for UI/telemetry
    public void GetAllJointNames(List<string> names)
    {
        if (names == null) return;
        names.Clear();
        for (int i = 0; i < joints.Count; i++)
            names.Add(joints[i] ? joints[i].name : "<null>");
    }

    public void GetAllTorquesNm(List<float> torques)
    {
        if (torques == null) return;
        torques.Clear();
        for (int i = 0; i < joints.Count; i++)
        {
            var j = joints[i];
            torques.Add(j ? smoothedTorqueByJoint[j] : 0f);
        }
    }

    // Immediate (unsmoothed) read for a specific joint if needed
    public float ReadInstantTorqueNm(ArticulationBody joint)
    {
        if (!joint) return 0f;
        var jf = joint.jointForce;
        return jf.dofCount > 0 ? jf[0] : 0f;
    }

    // Access individual torque components for debugging/analysis
    public float GetRawJointForce(ArticulationBody joint)
    {
        if (!joint) return 0f;
        return rawJointForce.TryGetValue(joint, out float value) ? value : 0f;
    }

    public float GetGravityTorque(ArticulationBody joint)
    {
        if (!joint) return 0f;
        return gravityTorque.TryGetValue(joint, out float value) ? value : 0f;
    }

    public float GetCoriolisTorque(ArticulationBody joint)
    {
        if (!joint) return 0f;
        return coriolisTorque.TryGetValue(joint, out float value) ? value : 0f;
    }

    public float GetPDTargetTorque(ArticulationBody joint)
    {
        if (!joint) return 0f;
        return pdTargetTorque.TryGetValue(joint, out float value) ? value : 0f;
    }

    // Get all torque components for a joint
    public void GetTorqueComponents(ArticulationBody joint, out float raw, out float gravity, out float coriolis, out float pd, out float total)
    {
        raw = GetRawJointForce(joint);
        gravity = GetGravityTorque(joint);
        coriolis = GetCoriolisTorque(joint);
        pd = GetPDTargetTorque(joint);
        total = GetTorqueNm(joint);
    }
}



