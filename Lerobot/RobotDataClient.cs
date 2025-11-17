using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;

/// <summary>
/// SO-101 Follower 로봇 데이터 수신 클라이언트 (Unity/C#)
/// WebSocket을 통해 실시간으로 로봇의 관절 위치, 전류, 부하 데이터를 받아옵니다
/// 
/// 사용법:
/// 1. Unity에 WebSocketSharp 플러그인 설치 필요
///    https://github.com/sta/websocket-sharp
/// 2. 이 스크립트를 GameObject에 추가
/// 3. Inspector에서 서버 주소 설정 (예: ws://192.168.1.100:8765)
/// </summary>
public class RobotDataClient : MonoBehaviour
{
    [Header("서버 설정")]
    [Tooltip("WebSocket 서버 주소 (예: ws://192.168.1.100:8765)")]
    public string serverUrl = "ws://localhost:8765";
    
    [Header("연결 상태")]
    public bool isConnected = false;
    
    [Header("디버그 옵션")]
    public bool showDebugLogs = true;
    
    // WebSocket 클라이언트
    private WebSocket ws;
    
    // 로봇 데이터
    private RobotData latestData;
    private readonly object dataLock = new object();
    
    // 이벤트
    public event Action<RobotData> OnDataReceived;
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
    
    /// <summary>
    /// 서버에 연결
    /// </summary>
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
                    RobotData data = JsonUtility.FromJson<RobotData>(e.Data);
                    
                    lock (dataLock)
                    {
                        latestData = data;
                    }
                    
                    // 메인 스레드에서 이벤트 호출
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        OnDataReceived?.Invoke(data);
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"데이터 파싱 오류: {ex.Message}");
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
    
    /// <summary>
    /// 서버 연결 해제
    /// </summary>
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
    /// 최신 로봇 데이터 가져오기 (스레드 안전)
    /// </summary>
    public RobotData GetLatestData()
    {
        lock (dataLock)
        {
            return latestData;
        }
    }
    
    /// <summary>
    /// 특정 모터의 데이터 가져오기
    /// </summary>
    public MotorData GetMotorData(string motorName)
    {
        lock (dataLock)
        {
            if (latestData != null && latestData.motors != null)
            {
                foreach (var motor in latestData.motors)
                {
                    if (motor.name == motorName)
                    {
                        return motor;
                    }
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Ping 전송 (연결 유지)
    /// </summary>
    public void SendPing()
    {
        if (ws != null && ws.IsAlive)
        {
            string ping = "{\"type\":\"ping\"}";
            ws.Send(ping);
        }
    }
}

/// <summary>
/// 로봇 전체 데이터 구조
/// </summary>
[Serializable]
public class RobotData
{
    public double timestamp;
    public List<MotorData> motors;
    
    // JSON 파싱을 위해 Dictionary를 List로 변환
    public static RobotData FromJson(string json)
    {
        var tempData = JsonUtility.FromJson<RobotDataRaw>(json);
        var robotData = new RobotData
        {
            timestamp = tempData.timestamp,
            motors = new List<MotorData>()
        };
        
        // motors 딕셔너리를 리스트로 변환
        // Note: Unity JsonUtility는 Dictionary를 지원하지 않으므로
        // 실제 구현에서는 다른 JSON 라이브러리(Newtonsoft.Json 등) 사용 권장
        
        return robotData;
    }
}

[Serializable]
public class RobotDataRaw
{
    public double timestamp;
    // motors는 별도 파싱 필요
}

/// <summary>
/// 개별 모터 데이터
/// </summary>
[Serializable]
public class MotorData
{
    public string name;         // 모터 이름
    public float position;      // 위치 (normalized)
    public float current;       // 전류 (mA) - 토크와 직접 관련
    public float load;          // 부하 - 토크 피드백
}

/// <summary>
/// Unity 메인 스레드에서 작업 실행을 위한 디스패처
/// (별도 파일로 분리 권장)
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _executionQueue = new Queue<Action>();
    
    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }
    
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
    
    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue()?.Invoke();
            }
        }
    }
}

