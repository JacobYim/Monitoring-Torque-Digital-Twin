using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 로봇 데이터 시각화 예제
/// VR에서 로봇의 관절 상태를 표시하는 샘플 코드
/// </summary>
public class RobotVisualizerExample : MonoBehaviour
{
    [Header("참조")]
    public RobotDataClientNewtonsoft robotClient;
    
    [Header("UI 요소")]
    public TextMeshProUGUI statusText;
    public GameObject motorDisplayPrefab;
    
    [Header("모터 표시 설정")]
    public Color positionColor = Color.blue;
    public Color currentColor = Color.red;
    public Color loadColor = Color.yellow;
    
    private Dictionary<string, MotorDisplay> motorDisplays = new Dictionary<string, MotorDisplay>();
    private bool isInitialized = false;
    
    void Start()
    {
        if (robotClient == null)
        {
            robotClient = FindObjectOfType<RobotDataClientNewtonsoft>();
        }
        
        if (robotClient != null)
        {
            robotClient.OnDataReceived += OnRobotDataReceived;
            robotClient.OnConnected += OnRobotConnected;
            robotClient.OnDisconnected += OnRobotDisconnected;
        }
    }
    
    void OnDestroy()
    {
        if (robotClient != null)
        {
            robotClient.OnDataReceived -= OnRobotDataReceived;
            robotClient.OnConnected -= OnRobotConnected;
            robotClient.OnDisconnected -= OnRobotDisconnected;
        }
    }
    
    void OnRobotConnected()
    {
        UpdateStatusText("로봇 연결됨", Color.green);
    }
    
    void OnRobotDisconnected()
    {
        UpdateStatusText("로봇 연결 끊김", Color.red);
    }
    
    void OnRobotDataReceived(RobotDataComplete data)
    {
        // 첫 데이터 수신 시 UI 초기화
        if (!isInitialized)
        {
            InitializeMotorDisplays(data);
            isInitialized = true;
        }
        
        // 각 모터 데이터 업데이트
        foreach (var motor in data.motors)
        {
            string motorName = motor.Key;
            var motorData = motor.Value;
            
            if (motorDisplays.ContainsKey(motorName))
            {
                motorDisplays[motorName].UpdateData(motorData);
            }
        }
        
        UpdateStatusText($"업데이트: {System.DateTime.Now:HH:mm:ss.fff}", Color.green);
    }
    
    void InitializeMotorDisplays(RobotDataComplete data)
    {
        float yOffset = 0f;
        
        foreach (var motor in data.motors)
        {
            string motorName = motor.Key;
            
            // 모터 표시 UI 생성
            GameObject displayObj = Instantiate(motorDisplayPrefab, transform);
            displayObj.transform.localPosition = new Vector3(0, yOffset, 0);
            
            MotorDisplay display = displayObj.GetComponent<MotorDisplay>();
            if (display != null)
            {
                display.Initialize(motorName);
                motorDisplays[motorName] = display;
            }
            
            yOffset -= 0.15f; // 다음 항목 위치
        }
    }
    
    void UpdateStatusText(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
    }
}

/// <summary>
/// 개별 모터 표시 컴포넌트
/// </summary>
public class MotorDisplay : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI motorNameText;
    public TextMeshProUGUI positionText;
    public TextMeshProUGUI currentText;
    public TextMeshProUGUI loadText;
    
    [Header("시각화")]
    public Transform positionIndicator;
    public Renderer currentIndicator;
    public Renderer loadIndicator;
    
    private string motorName;
    
    public void Initialize(string name)
    {
        motorName = name;
        if (motorNameText != null)
        {
            motorNameText.text = name;
        }
    }
    
    public void UpdateData(MotorDataComplete data)
    {
        // 텍스트 업데이트
        if (positionText != null)
            positionText.text = $"Pos: {data.position:F2}°";
        
        if (currentText != null)
            currentText.text = $"Current: {data.current:F0} mA";
        
        if (loadText != null)
            loadText.text = $"Load: {data.load:F0}";
        
        // 시각적 인디케이터 업데이트
        if (positionIndicator != null)
        {
            // 위치를 회전으로 표시
            positionIndicator.localRotation = Quaternion.Euler(0, 0, data.position);
        }
        
        if (currentIndicator != null)
        {
            // 전류 강도를 색상으로 표시
            float intensity = data.TorqueIntensity;
            Color color = Color.Lerp(Color.green, Color.red, intensity);
            currentIndicator.material.color = color;
        }
        
        if (loadIndicator != null)
        {
            // 부하를 스케일로 표시
            float scale = 1f + data.LoadIntensity;
            loadIndicator.transform.localScale = Vector3.one * scale;
        }
    }
}

/// <summary>
/// 간단한 토크 바 시각화
/// </summary>
public class TorqueBar : MonoBehaviour
{
    public RectTransform barFill;
    public TextMeshProUGUI valueText;
    public Color lowColor = Color.green;
    public Color highColor = Color.red;
    
    private UnityEngine.UI.Image barImage;
    
    void Awake()
    {
        if (barFill != null)
        {
            barImage = barFill.GetComponent<UnityEngine.UI.Image>();
        }
    }
    
    public void SetValue(float value, float maxValue = 1000f)
    {
        float normalized = Mathf.Clamp01(value / maxValue);
        
        // 바 길이 설정
        if (barFill != null)
        {
            barFill.localScale = new Vector3(normalized, 1f, 1f);
        }
        
        // 색상 설정
        if (barImage != null)
        {
            barImage.color = Color.Lerp(lowColor, highColor, normalized);
        }
        
        // 값 텍스트 설정
        if (valueText != null)
        {
            valueText.text = $"{value:F0}";
        }
    }
}

