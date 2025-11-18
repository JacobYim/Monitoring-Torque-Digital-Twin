using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

[System.Serializable]
public class JointUIPairData
{
    public string jointPath;
    public string targetUIPath;
    public string label;
    public float weight;
    public bool useAbsoluteValue;
    public float customMultiplier;
    public bool showDebug;
}

[System.Serializable]
public class JointToUIConfig
{
    public List<JointUIPairData> jointUIPairs = new List<JointUIPairData>();
    public float updatesPerSecond = 30f;
    public bool accumulateValues = false;
    public string accumulatedTargetUIPath;
    public string accumulatedLabel = "Combined";
    public bool showDebugInfo = false;
}

[System.Serializable]
public class JointUIPair
{
    [Header("Joint Configuration")]
    [Tooltip("The articulation body joint to monitor")]
    public ArticulationBody joint;
    
    [Tooltip("UI widget that receives a 0..1 value via SetValue")]
    public ImgsFillDynamic targetUI;
    
    [Header("Display Settings")]
    [Tooltip("Label to display on the UI widget")]
    public string label = "Joint";
    
    [Tooltip("Weight for this joint when accumulating values (0 = disabled)")]
    [Range(0f, 1f)]
    public float weight = 1f;
    
    [Tooltip("Use absolute value of joint position")]
    public bool useAbsoluteValue = true;
    
    [Tooltip("Custom multiplier for this joint's value")]
    public float customMultiplier = 1f;
    
    [Header("Debug")]
    [Tooltip("Show debug info for this joint")]
    public bool showDebug = false;
    
    // Internal state
    [HideInInspector]
    public float currentValue;
    [HideInInspector]
    public float currentRatio;
}

public class JointToUIManager : MonoBehaviour
{
    [Header("Joint/UI Pairs")]
    [Tooltip("List of joint/UI pairs to monitor")]
    public List<JointUIPair> jointUIPairs = new List<JointUIPair>();
    
    [Header("Global Settings")]
    [Tooltip("How often (per second) to sample and push UI updates")]
    public float updatesPerSecond = 30f;
    
    [Tooltip("Accumulate values from all joints instead of individual display")]
    public bool accumulateValues = false;
    
    [Tooltip("Target UI for accumulated values (when accumulateValues is true)")]
    public ImgsFillDynamic accumulatedTargetUI;
    
    [Tooltip("Label for accumulated values")]
    public string accumulatedLabel = "Combined";
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool showDebugInfo = false;
    
    [Header("Configuration Management")]
    [Tooltip("Default configuration file name (without extension)")]
    public string defaultConfigName = "JointToUI_Default";
    
    [Tooltip("Configuration file directory (relative to Assets folder)")]
    public string configDirectory = "Configs";
    
    [Space(10)]
    [Tooltip("Save current configuration as default")]
    public bool saveConfigButton = false;
    
    [Tooltip("Load default configuration")]
    public bool loadConfigButton = false;
    
    [Tooltip("Save configuration with custom name")]
    public string customConfigName = "MyConfig";
    public bool saveCustomConfigButton = false;
    
    private float _accum;
    private float _totalAccumulatedValue;
    
    void Start()
    {
        // Initialize labels for individual joints
        foreach (var pair in jointUIPairs)
        {
            if (pair.targetUI != null && !string.IsNullOrEmpty(pair.label))
            {
                pair.targetUI.SetLabel(pair.label);
            }
        }
        
        // Initialize accumulated UI label
        if (accumulateValues && accumulatedTargetUI != null && !string.IsNullOrEmpty(accumulatedLabel))
        {
            accumulatedTargetUI.SetLabel(accumulatedLabel);
        }
    }
    
    void Update()
    {
        // Handle inspector button presses
        if (saveConfigButton)
        {
            saveConfigButton = false;
            SaveCurrentConfiguration();
        }
        
        if (loadConfigButton)
        {
            loadConfigButton = false;
            LoadConfiguration();
        }
        
        if (saveCustomConfigButton)
        {
            saveCustomConfigButton = false;
            SaveCurrentConfiguration(customConfigName);
        }
        
        if (jointUIPairs.Count == 0)
            return;
            
        _accum += Time.deltaTime;
        float interval = Mathf.Max(0.01f, 1f / Mathf.Max(1f, updatesPerSecond));
        if (_accum < interval)
            return;
        _accum = 0f;
        
        _totalAccumulatedValue = 0f;
        float totalWeight = 0f;
        
        // Process each joint/UI pair
        foreach (var pair in jointUIPairs)
        {
            if (pair.joint == null || pair.weight <= 0f)
                continue;
                
            float jointValue = GetJointValue(pair);
            pair.currentValue = jointValue;
            pair.currentRatio = GetJointRatio(pair);
            
            // Apply custom multiplier
            float finalValue = pair.currentRatio * pair.customMultiplier;
            
            if (accumulateValues)
            {
                // Accumulate for combined display
                _totalAccumulatedValue += finalValue * pair.weight;
                totalWeight += pair.weight;
            }
            else
            {
                // Individual display
                if (pair.targetUI != null)
                {
                    pair.targetUI.SetValue(Mathf.Clamp01(finalValue), _isDirectly: true);
                }
            }
            
            if (pair.showDebug || showDebugInfo)
            {
                Debug.Log($"{pair.label}: Value={jointValue:F2}°, Ratio={pair.currentRatio:F3}, Final={finalValue:F3}");
            }
        }
        
        // Update accumulated UI if enabled
        if (accumulateValues && accumulatedTargetUI != null && totalWeight > 0f)
        {
            float averageValue = _totalAccumulatedValue / totalWeight;
            accumulatedTargetUI.SetValue(Mathf.Clamp01(averageValue), _isDirectly: true);
        }
    }
    
    float GetJointValue(JointUIPair pair)
    {
        if (pair.joint == null)
            return 0f;
            
        // Get current joint angle in degrees
        float currentDeg = pair.joint.jointPosition.dofCount > 0
            ? pair.joint.jointPosition[0] * Mathf.Rad2Deg
            : 0f;
            
        return pair.useAbsoluteValue ? Mathf.Abs(currentDeg) : currentDeg;
    }
    
    float GetJointRatio(JointUIPair pair)
    {
        if (pair.joint == null)
            return 0f;
            
        // Get joint limits
        var drive = pair.joint.xDrive;
        float lower = drive.lowerLimit;
        float upper = drive.upperLimit;
        
        // Calculate ratio based on absolute values
        if (pair.useAbsoluteValue)
        {
            float maxLimit = Mathf.Max(Mathf.Abs(lower), Mathf.Abs(upper), 1e-3f);
            return Mathf.Clamp01(Mathf.Abs(pair.currentValue) / maxLimit);
        }
        else
        {
            // Normal ratio within limits
            float range = upper - lower;
            if (range <= 0f) return 0f;
            return Mathf.Clamp01((pair.currentValue - lower) / range);
        }
    }
    
    // Public methods for external control
    public void AddJointUIPair(ArticulationBody joint, ImgsFillDynamic ui, string label = "New Joint")
    {
        JointUIPair newPair = new JointUIPair
        {
            joint = joint,
            targetUI = ui,
            label = label,
            weight = 1f,
            useAbsoluteValue = true,
            customMultiplier = 1f,
            showDebug = false
        };
        
        jointUIPairs.Add(newPair);
    }
    
    public void RemoveJointUIPair(int index)
    {
        if (index >= 0 && index < jointUIPairs.Count)
        {
            jointUIPairs.RemoveAt(index);
        }
    }
    
    public void SetJointWeight(int index, float weight)
    {
        if (index >= 0 && index < jointUIPairs.Count)
        {
            jointUIPairs[index].weight = Mathf.Clamp01(weight);
        }
    }
    
    public void SetAccumulationMode(bool enabled)
    {
        accumulateValues = enabled;
    }
    
    // Configuration Management Methods
    public void SaveCurrentConfiguration(string configName = null)
    {
        if (string.IsNullOrEmpty(configName))
            configName = defaultConfigName;
            
        JointToUIConfig config = new JointToUIConfig();
        
        // Save current settings
        config.updatesPerSecond = updatesPerSecond;
        config.accumulateValues = accumulateValues;
        config.accumulatedLabel = accumulatedLabel;
        config.showDebugInfo = showDebugInfo;
        
        // Save joint/UI pairs
        config.jointUIPairs.Clear();
        foreach (var pair in jointUIPairs)
        {
            JointUIPairData data = new JointUIPairData
            {
                jointPath = GetGameObjectPath(pair.joint?.gameObject),
                targetUIPath = GetGameObjectPath(pair.targetUI?.gameObject),
                label = pair.label,
                weight = pair.weight,
                useAbsoluteValue = pair.useAbsoluteValue,
                customMultiplier = pair.customMultiplier,
                showDebug = pair.showDebug
            };
            config.jointUIPairs.Add(data);
        }
        
        // Save accumulated target UI path
        config.accumulatedTargetUIPath = GetGameObjectPath(accumulatedTargetUI?.gameObject);
        
        // Save to file
        string json = JsonUtility.ToJson(config, true);
        string filePath = GetConfigFilePath(configName);
        
        try
        {
            // Ensure directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, json);
            Debug.Log($"Configuration saved to: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save configuration: {e.Message}");
        }
    }
    
    public void LoadConfiguration(string configName = null)
    {
        if (string.IsNullOrEmpty(configName))
            configName = defaultConfigName;
            
        string filePath = GetConfigFilePath(configName);
        
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"Configuration file not found: {filePath}");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(filePath);
            JointToUIConfig config = JsonUtility.FromJson<JointToUIConfig>(json);
            
            // Load settings
            updatesPerSecond = config.updatesPerSecond;
            accumulateValues = config.accumulateValues;
            accumulatedLabel = config.accumulatedLabel;
            showDebugInfo = config.showDebugInfo;
            
            // Load joint/UI pairs
            jointUIPairs.Clear();
            foreach (var data in config.jointUIPairs)
            {
                JointUIPair pair = new JointUIPair
                {
                    joint = FindGameObjectByPath(data.jointPath)?.GetComponent<ArticulationBody>(),
                    targetUI = FindGameObjectByPath(data.targetUIPath)?.GetComponent<ImgsFillDynamic>(),
                    label = data.label,
                    weight = data.weight,
                    useAbsoluteValue = data.useAbsoluteValue,
                    customMultiplier = data.customMultiplier,
                    showDebug = data.showDebug
                };
                jointUIPairs.Add(pair);
            }
            
            // Load accumulated target UI
            accumulatedTargetUI = FindGameObjectByPath(config.accumulatedTargetUIPath)?.GetComponent<ImgsFillDynamic>();
            
            Debug.Log($"Configuration loaded from: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load configuration: {e.Message}");
        }
    }
    
    string GetConfigFilePath(string configName)
    {
        string directory = Path.Combine(Application.dataPath, configDirectory);
        return Path.Combine(directory, $"{configName}.json");
    }
    
    string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "";
        
        string path = obj.name;
        Transform parent = obj.transform.parent;
        
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        
        return path;
    }
    
    GameObject FindGameObjectByPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        
        // Try to find by full path first
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (GetGameObjectPath(obj) == path)
                return obj;
        }
        
        // If not found by full path, try to find by name (last part of path)
        string[] pathParts = path.Split('/');
        if (pathParts.Length > 0)
        {
            string objectName = pathParts[pathParts.Length - 1];
            GameObject found = GameObject.Find(objectName);
            if (found != null)
            {
                Debug.LogWarning($"Found object by name '{objectName}' instead of full path '{path}'. Path may have changed.");
                return found;
            }
        }
        
        return null;
    }
    
    // Editor helper methods
    [ContextMenu("Add New Joint/UI Pair")]
    public void AddNewPair()
    {
        AddJointUIPair(null, null, $"Joint {jointUIPairs.Count + 1}");
    }
    
    [ContextMenu("Clear All Pairs")]
    public void ClearAllPairs()
    {
        jointUIPairs.Clear();
    }
    
    [ContextMenu("Save Current Configuration")]
    void SaveCurrentConfig()
    {
        SaveCurrentConfiguration();
    }
    
    [ContextMenu("Load Default Configuration")]
    void LoadDefaultConfig()
    {
        LoadConfiguration();
    }
    
    [ContextMenu("Save Configuration As...")]
    void SaveConfigAs()
    {
        // This would ideally open a file dialog, but for now just save with timestamp
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        SaveCurrentConfiguration($"JointToUI_{timestamp}");
    }
}


