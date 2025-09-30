using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(JointToUIManager))]
public class JointToUIManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Configuration Actions", EditorStyles.boldLabel);
        
        JointToUIManager manager = (JointToUIManager)target;
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save Current Config", GUILayout.Height(25)))
        {
            manager.SaveCurrentConfiguration();
        }
        
        if (GUILayout.Button("Load Default Config", GUILayout.Height(25)))
        {
            manager.LoadConfiguration();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Save As...", GUILayout.Height(25)))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            manager.SaveCurrentConfiguration($"JointToUI_{timestamp}");
        }
        
        if (GUILayout.Button("Clear All Pairs", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear All Pairs", 
                "Are you sure you want to clear all joint/UI pairs?", "Yes", "Cancel"))
            {
                manager.ClearAllPairs();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Add New Joint/UI Pair", GUILayout.Height(25)))
        {
            manager.AddNewPair();
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Configuration files are saved to: Assets/Configs/", MessageType.Info);
    }
}
