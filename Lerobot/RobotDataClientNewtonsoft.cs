using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// SO-101 Follower 로봇 데이터 수신 클라이언트 (Newtonsoft.Json 버전)
/// 더 나은 JSON 파싱을 위해 Newtonsoft.Json 사용
/// 
/// 설치 방법:
/// 1. Unity Package Manager에서 "com.unity.nuget.newtonsoft-json" 설치
/// 2. WebSocketSharp 플러그인 설치
/// </summary>
public class RobotDataClientNewtonsoft : MonoBehaviour
{
    [Header("서버 설정")]
    public string serverUrl = "ws://localhost:8765";
    
    [Header("연결 상태")]
    public bool isConnected = false;
    
    [Header("디버그")]
    public bool showDebugLogs = true;
    public bool showDataInConsole = false;
    
    private WebSocket ws;
    private RobotDataComplete latestData;
    private readonly object dataLock = new object();
    
    // 이벤트
    public event Action<RobotDataComplete> OnDataReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;
    
    void Start()
    {
        ConnectToServer();
    }
    
    void OnDestroy()
    {
        DisconnectFromServer();
    }
    
    public void ConnectToServer()
    {
        if (ws != null && ws.IsAlive)
        {
            Debug.LogWarning("이미 서버에 연결되어 있습니다.");
            return;
        }
        
        try
        {
            ws = new WebSocket(serverUrl);
            
            ws.OnOpen += (sender, e) =>
            {
                isConnected = true;
                if (showDebugLogs) Debug.Log($"✓ 서버 연결 성공: {serverUrl}");
                OnConnected?.Invoke();
            };
            
            ws.OnMessage += (sender, e) =>
            {
                try
                {
                    // Newtonsoft.Json으로 파싱
                    var json = JObject.Parse(e.Data);
                    
                    RobotDataComplete data = new RobotDataComplete
                    {
                        timestamp = (double)json["timestamp"],
                        motors = new Dictionary<string, MotorDataComplete>()
                    };
                    
                    // motors 딕셔너리 파싱
                    var motorsObj = json["motors"] as JObject;
                    if (motorsObj != null)
                    {
                        foreach (var motor in motorsObj)
                        {
                            string motorName = motor.Key;
                            var motorData = motor.Value;
                            
                            data.motors[motorName] = new MotorDataComplete
                            {
                                position = (float)motorData["position"],
                                current = (float)motorData["current"],
                                load = (float)motorData["load"]
                            };
                        }
                    }
                    
                    lock (dataLock)
                    {
                        latestData = data;
                    }
                    
                    if (showDataInConsole)
                    {
                        Debug.Log($"로봇 데이터: {data.motors.Count}개 모터");
                    }
                    
                    // 이벤트 호출
                    OnDataReceived?.Invoke(data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"데이터 파싱 오류: {ex.Message}\n{ex.StackTrace}");
                }
            };
            
            ws.OnError += (sender, e) =>
            {
                Debug.LogError($"WebSocket 오류: {e.Message}");
            };
            
            ws.OnClose += (sender, e) =>
            {
                isConnected = false;
                if (showDebugLogs) Debug.Log($"서버 연결 종료: {e.Reason}");
                OnDisconnected?.Invoke();
            };
            
            ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"서버 연결 실패: {ex.Message}");
        }
    }
    
    public void DisconnectFromServer()
    {
        if (ws != null)
        {
            ws.Close();
            ws = null;
        }
        isConnected = false;
    }
    
    /// <summary>
    /// 최신 로봇 데이터 가져오기
    /// </summary>
    public RobotDataComplete GetLatestData()
    {
        lock (dataLock)
        {
            return latestData;
        }
    }
    
    /// <summary>
    /// 특정 모터의 데이터 가져오기
    /// </summary>
    public MotorDataComplete GetMotorData(string motorName)
    {
        lock (dataLock)
        {
            if (latestData != null && latestData.motors != null)
            {
                if (latestData.motors.ContainsKey(motorName))
                {
                    return latestData.motors[motorName];
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// 모든 모터 이름 가져오기
    /// </summary>
    public List<string> GetMotorNames()
    {
        lock (dataLock)
        {
            if (latestData != null && latestData.motors != null)
            {
                return new List<string>(latestData.motors.Keys);
            }
        }
        return new List<string>();
    }
}

/// <summary>
/// 로봇 전체 데이터 (완전한 버전)
/// </summary>
public class RobotDataComplete
{
    public double timestamp;
    public Dictionary<string, MotorDataComplete> motors;
}

/// <summary>
/// 모터 데이터 (완전한 버전)
/// </summary>
public class MotorDataComplete
{
    public float position;      // 위치 (normalized, -100 ~ 100 정도)
    public float current;       // 전류 (mA) - 토크 표시용
    public float load;          // 부하 (0 ~ 1000) - 토크 피드백
    
    /// <summary>
    /// 토크 강도 (0 ~ 1)
    /// </summary>
    public float TorqueIntensity => Mathf.Abs(current) / 1000f;
    
    /// <summary>
    /// 부하 강도 (0 ~ 1)
    /// </summary>
    public float LoadIntensity => Mathf.Abs(load) / 1000f;
}

