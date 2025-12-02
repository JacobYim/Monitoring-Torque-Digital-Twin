using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Controls UI visibility based on dropdown selection.
/// Finds UI GameObjects containing "UI" in their name and shows/hides them based on dropdown option.
/// </summary>
public class UIDropdownVisibilityController : MonoBehaviour
{
    [Header("Dropdown Reference")]
    [Tooltip("The dropdown component that controls UI visibility")]
    [SerializeField]
    TMP_Dropdown m_Dropdown;
    
    [Tooltip("Alternative: Standard Unity Dropdown (use if not using TMP)")]
    [SerializeField]
    Dropdown m_StandardDropdown;

    [Header("UI Settings")]
    [Tooltip("Search scope for finding UI GameObjects")]
    [SerializeField]
    SearchScope m_SearchScope = SearchScope.Scene;

    [Tooltip("Optional: Parent GameObject to search within (only used if Search Scope is 'Parent')")]
    [SerializeField]
    Transform m_SearchParent;

    [Tooltip("Search recursively in children")]
    [SerializeField]
    bool m_SearchRecursively = true;

    [Tooltip("Hide all UI objects when switching (prevents flickering)")]
    [SerializeField]
    bool m_HideAllOnSwitch = true;

    [Tooltip("List of UI GameObjects to control (if empty, will search automatically)")]
    [SerializeField]
    List<GameObject> m_UIObjects = new List<GameObject>();

    [Header("Debug")]
    [Tooltip("Show debug messages in console")]
    [SerializeField]
    bool m_ShowDebug = false;

    public enum SearchScope
    {
        Scene,      // Search entire scene
        Parent,     // Search within a specific parent
        Canvas      // Search within Canvas objects
    }

    private GameObject m_CurrentlyShownUI = null;
    private List<GameObject> m_AllUIObjects = new List<GameObject>();

    void OnEnable()
    {
        // Subscribe to dropdown value changes
        if (m_Dropdown != null)
        {
            m_Dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
        else if (m_StandardDropdown != null)
        {
            m_StandardDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
        else
        {
            Debug.LogWarning($"[UIDropdownVisibilityController] No dropdown assigned on {gameObject.name}. Please assign either TMP_Dropdown or Dropdown.");
        }

        // Cache all UI objects for faster lookup
        CacheAllUIObjects();
        
        // Apply initial visibility
        UpdateUIVisibility();
    }

    void OnDisable()
    {
        // Unsubscribe from dropdown value changes
        if (m_Dropdown != null)
        {
            m_Dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
        if (m_StandardDropdown != null)
        {
            m_StandardDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
    }

    void OnDropdownValueChanged(int value)
    {
        if (m_ShowDebug)
        {
            string optionName = GetDropdownOptionName(value);
            Debug.Log($"[UIDropdownVisibilityController] Dropdown value changed to index {value}: '{optionName}'");
        }

        UpdateUIVisibility();
    }


    /// <summary>
    /// Caches all UI objects from dropdown options for faster lookup
    /// </summary>
    void CacheAllUIObjects()
    {
        m_AllUIObjects.Clear();

        // Get all dropdown options
        int optionCount = GetDropdownOptionCount();
        for (int i = 0; i < optionCount; i++)
        {
            string optionName = GetDropdownOptionName(i);
            if (!string.IsNullOrEmpty(optionName))
            {
                GameObject uiObj = FindUIByName(optionName);
                if (uiObj != null && !m_AllUIObjects.Contains(uiObj))
                {
                    m_AllUIObjects.Add(uiObj);
                }
            }
        }

        // Also add manually assigned UI objects
        if (m_UIObjects != null && m_UIObjects.Count > 0)
        {
            foreach (GameObject uiObj in m_UIObjects)
            {
                if (uiObj != null && !m_AllUIObjects.Contains(uiObj))
                {
                    m_AllUIObjects.Add(uiObj);
                }
            }
        }

        if (m_ShowDebug)
        {
            Debug.Log($"[UIDropdownVisibilityController] Cached {m_AllUIObjects.Count} UI objects from dropdown options.");
        }
    }

    /// <summary>
    /// Updates UI visibility based on current dropdown selection
    /// </summary>
    public void UpdateUIVisibility()
    {
        int selectedIndex = GetSelectedIndex();
        if (selectedIndex < 0)
        {
            return;
        }

        string selectedOptionName = GetDropdownOptionName(selectedIndex);
        if (string.IsNullOrEmpty(selectedOptionName))
        {
            if (m_ShowDebug)
            {
                Debug.LogWarning($"[UIDropdownVisibilityController] Could not get option name for index {selectedIndex}");
            }
            return;
        }

        // Hide ALL UI objects first to prevent multiple showing
        if (m_HideAllOnSwitch)
        {
            // Hide all cached UI objects
            foreach (GameObject uiObj in m_AllUIObjects)
            {
                if (uiObj != null)
                {
                    uiObj.SetActive(false);
                }
            }

            // Also hide currently shown UI if it's not in the cache
            if (m_CurrentlyShownUI != null && !m_AllUIObjects.Contains(m_CurrentlyShownUI))
            {
                m_CurrentlyShownUI.SetActive(false);
            }
        }

        // Find UI object by exact name
        GameObject matchingUI = FindUIByName(selectedOptionName);

        // Show matching UI object
        if (matchingUI != null)
        {
            matchingUI.SetActive(true);
            m_CurrentlyShownUI = matchingUI;
            
            // Add to cache if not already there
            if (!m_AllUIObjects.Contains(matchingUI))
            {
                m_AllUIObjects.Add(matchingUI);
            }
            
            if (m_ShowDebug)
            {
                Debug.Log($"[UIDropdownVisibilityController] Showing UI: {matchingUI.name} (matched with dropdown option: '{selectedOptionName}')");
            }
        }
        else
        {
            m_CurrentlyShownUI = null;
            if (m_ShowDebug)
            {
                Debug.LogWarning($"[UIDropdownVisibilityController] No UI object found with name: '{selectedOptionName}'");
            }
        }
    }

    /// <summary>
    /// Finds UI GameObject by exact name
    /// </summary>
    GameObject FindUIByName(string name)
    {
        // First check manually assigned UI objects
        if (m_UIObjects != null && m_UIObjects.Count > 0)
        {
            foreach (GameObject uiObj in m_UIObjects)
            {
                if (uiObj != null && uiObj.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return uiObj;
                }
            }
        }

        // Otherwise search in scene based on search scope
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);

        foreach (GameObject obj in allObjects)
        {
            // Check if object matches the name
            if (!obj.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Apply search scope filter
            bool inScope = false;
            switch (m_SearchScope)
            {
                case SearchScope.Scene:
                    inScope = true;
                    break;

                case SearchScope.Parent:
                    if (m_SearchParent != null)
                    {
                        inScope = IsChildOf(obj.transform, m_SearchParent);
                    }
                    else
                    {
                        inScope = true; // Fallback to scene search
                    }
                    break;

                case SearchScope.Canvas:
                    Canvas canvas = obj.GetComponentInParent<Canvas>();
                    inScope = canvas != null;
                    break;
            }

            if (inScope)
            {
                return obj;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if transform is a child of parent (recursively)
    /// </summary>
    bool IsChildOf(Transform child, Transform parent)
    {
        if (child == null || parent == null)
            return false;

        Transform current = child;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.parent;
        }

        return false;
    }


    int GetSelectedIndex()
    {
        if (m_Dropdown != null)
        {
            return m_Dropdown.value;
        }
        else if (m_StandardDropdown != null)
        {
            return m_StandardDropdown.value;
        }
        return -1;
    }

    string GetDropdownOptionName(int index)
    {
        if (m_Dropdown != null && m_Dropdown.options != null && index >= 0 && index < m_Dropdown.options.Count)
        {
            return m_Dropdown.options[index].text;
        }
        else if (m_StandardDropdown != null && m_StandardDropdown.options != null && index >= 0 && index < m_StandardDropdown.options.Count)
        {
            return m_StandardDropdown.options[index].text;
        }
        return "";
    }

    int GetDropdownOptionCount()
    {
        if (m_Dropdown != null && m_Dropdown.options != null)
        {
            return m_Dropdown.options.Count;
        }
        else if (m_StandardDropdown != null && m_StandardDropdown.options != null)
        {
            return m_StandardDropdown.options.Count;
        }
        return 0;
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

    // Public methods for external control
    /// <summary>
    /// Manually set which UI to show by name
    /// </summary>
    public void ShowUIByName(string uiName)
    {
        // Hide ALL UI objects first
        if (m_HideAllOnSwitch)
        {
            foreach (GameObject uiObj in m_AllUIObjects)
            {
                if (uiObj != null)
                {
                    uiObj.SetActive(false);
                }
            }
        }

        // Find and show matching UI
        GameObject matchingUI = FindUIByName(uiName);
        if (matchingUI != null)
        {
            matchingUI.SetActive(true);
            m_CurrentlyShownUI = matchingUI;
            if (m_ShowDebug)
            {
                Debug.Log($"[UIDropdownVisibilityController] Manually showing UI: {matchingUI.name}");
            }
        }
    }

    /// <summary>
    /// Refresh the cached UI objects list (call this if dropdown options change)
    /// </summary>
    public void RefreshUIObjects()
    {
        CacheAllUIObjects();
    }

    // Editor helper methods
    [ContextMenu("Update UI Visibility")]
    void UpdateUIVisibilityContext()
    {
        UpdateUIVisibility();
    }

    [ContextMenu("Refresh UI Objects Cache")]
    void RefreshUIObjectsContext()
    {
        CacheAllUIObjects();
        UpdateUIVisibility();
    }
}

