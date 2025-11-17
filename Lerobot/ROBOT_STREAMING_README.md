# SO-101 로봇 데이터 스트리밍 시스템

VR(C#/Unity)에서 SO-101 Follower 로봇의 실시간 데이터를 받아볼 수 있는 WebSocket 기반 스트리밍 시스템입니다.

## 📋 목차
- [시스템 구성](#시스템-구성)
- [설치 및 실행](#설치-및-실행)
- [Python 서버](#python-서버)
- [Unity/C# 클라이언트](#unityc-클라이언트)
- [데이터 형식](#데이터-형식)
- [문제 해결](#문제-해결)

---

## 🔧 시스템 구성

```
┌─────────────────┐         WebSocket          ┌─────────────────┐
│  SO-101 Robot   │ ←──── (ws://IP:8765) ────→ │   VR (Unity)    │
│   + Python      │         JSON Data           │   + C# Client   │
│   Server        │                             │                 │
└─────────────────┘                             └─────────────────┘
```

### 전송 데이터
각 관절(모터)마다:
- **Position** (위치) - normalized 값
- **Current** (전류) - mA 단위, 토크와 직접 관련
- **Load** (부하) - 토크 피드백

---

## 🚀 설치 및 실행

### 1. Python 서버 설치

필요한 패키지 설치:
```bash
cd /home/jacob/Desktop/New\ Folder
source venv/bin/activate
pip install websockets
```

### 2. Python 서버 실행

기본 실행:
```bash
python3 robot_data_stream_server.py --port /dev/ttyACM0
```

옵션 지정:
```bash
python3 robot_data_stream_server.py \
    --port /dev/ttyACM0 \
    --ws-host 0.0.0.0 \
    --ws-port 8765 \
    --rate 30
```

**파라미터:**
- `--port`: 로봇 시리얼 포트 (기본값: /dev/ttyACM0)
- `--ws-host`: WebSocket 서버 호스트 (기본값: 0.0.0.0)
- `--ws-port`: WebSocket 서버 포트 (기본값: 8765)
- `--rate`: 업데이트 주기 Hz (기본값: 30)

### 3. 서버 테스트

Python 테스트 클라이언트로 확인:
```bash
python3 test_websocket_client.py ws://localhost:8765 10
```

---

## 🐍 Python 서버

### robot_data_stream_server.py

SO-101 Follower 로봇과 통신하여 실시간 데이터를 WebSocket으로 브로드캐스트합니다.

**주요 기능:**
- 로봇에서 위치, 전류, 부하 데이터 읽기
- 여러 클라이언트 동시 연결 지원
- 설정 가능한 업데이트 주기 (기본 30 Hz)
- JSON 형식으로 데이터 전송

**실행 예제:**
```bash
# 기본 실행
python3 robot_data_stream_server.py --port /dev/ttyACM0

# 60 Hz 업데이트
python3 robot_data_stream_server.py --port /dev/ttyACM0 --rate 60

# 다른 포트 사용
python3 robot_data_stream_server.py --port /dev/ttyACM0 --ws-port 9000
```

---

## 🎮 Unity/C# 클라이언트

### 필요한 패키지

1. **WebSocketSharp** (필수)
   - GitHub: https://github.com/sta/websocket-sharp
   - Unity Asset Store에서도 다운로드 가능

2. **Newtonsoft.Json** (권장)
   - Unity Package Manager → "com.unity.nuget.newtonsoft-json"
   - 더 나은 JSON 파싱 지원

### 클라이언트 파일

#### 1. RobotDataClient.cs (기본 버전)
Unity JsonUtility 사용, 간단한 구현

#### 2. RobotDataClientNewtonsoft.cs (권장)
Newtonsoft.Json 사용, 완전한 기능

#### 3. RobotVisualizerExample.cs
데이터 시각화 예제

### Unity 설정

1. **클라이언트 설정:**
   ```csharp
   // 빈 GameObject 생성
   GameObject clientObj = new GameObject("RobotDataClient");
   
   // 컴포넌트 추가
   RobotDataClientNewtonsoft client = clientObj.AddComponent<RobotDataClientNewtonsoft>();
   
   // 서버 주소 설정 (Python 서버의 IP)
   client.serverUrl = "ws://192.168.1.100:8765";
   ```

2. **데이터 수신:**
   ```csharp
   // 이벤트 등록
   client.OnDataReceived += (data) => {
       // 모든 모터 데이터
       foreach (var motor in data.motors) {
           string motorName = motor.Key;
           var motorData = motor.Value;
           
           Debug.Log($"{motorName}: Pos={motorData.position}, " +
                     $"Current={motorData.current}mA, " +
                     $"Load={motorData.load}");
       }
   };
   ```

3. **특정 모터 데이터 가져오기:**
   ```csharp
   // shoulder_pan 모터 데이터
   var shoulderData = client.GetMotorData("shoulder_pan");
   if (shoulderData != null) {
       float torqueIntensity = shoulderData.TorqueIntensity; // 0~1
       float loadIntensity = shoulderData.LoadIntensity;     // 0~1
   }
   ```

### VR에서 활용 예제

```csharp
using UnityEngine;

public class RobotArmVR : MonoBehaviour
{
    public RobotDataClientNewtonsoft robotClient;
    public Transform shoulderJoint;
    public Renderer torqueVisualizer;
    
    void Update()
    {
        // shoulder_pan 데이터 가져오기
        var shoulderData = robotClient.GetMotorData("shoulder_pan");
        
        if (shoulderData != null)
        {
            // 관절 회전 (위치 기반)
            shoulderJoint.localRotation = Quaternion.Euler(
                0, shoulderData.position, 0
            );
            
            // 토크 시각화 (색상)
            float torque = shoulderData.TorqueIntensity;
            torqueVisualizer.material.color = Color.Lerp(
                Color.green, Color.red, torque
            );
            
            // 햅틱 피드백 (부하 기반)
            if (shoulderData.LoadIntensity > 0.7f)
            {
                // VR 컨트롤러 진동
                TriggerHapticFeedback(shoulderData.LoadIntensity);
            }
        }
    }
}
```

---

## 📊 데이터 형식

### JSON 구조

```json
{
  "timestamp": 1699999999.123,
  "motors": {
    "shoulder_pan": {
      "position": 12.82,
      "current": 125.5,
      "load": 234.0
    },
    "shoulder_lift": {
      "position": 25.18,
      "current": 98.3,
      "load": 156.7
    },
    "elbow_flex": {
      "position": -43.24,
      "current": 67.2,
      "load": 89.4
    },
    "wrist_flex": {
      "position": 78.94,
      "current": 34.1,
      "load": 45.2
    },
    "wrist_roll": {
      "position": 13.56,
      "current": 12.5,
      "load": 23.1
    },
    "gripper": {
      "position": 30.43,
      "current": 156.8,
      "load": 345.6
    }
  }
}
```

### 데이터 설명

| 필드 | 타입 | 단위 | 설명 |
|------|------|------|------|
| `timestamp` | float | 초 | Unix timestamp |
| `position` | float | - | Normalized 위치 (-100 ~ 100) |
| `current` | float | mA | 전류 (토크와 비례) |
| `load` | float | - | 부하 (0 ~ 1000) |

### 모터 이름

- `shoulder_pan` - 어깨 팬
- `shoulder_lift` - 어깨 리프트
- `elbow_flex` - 팔꿈치 굽힘
- `wrist_flex` - 손목 굽힘
- `wrist_roll` - 손목 회전
- `gripper` - 그리퍼

---

## 🔍 문제 해결

### 1. 서버가 시작되지 않음

**오류:**
```
ModuleNotFoundError: No module named 'websockets'
```

**해결:**
```bash
source venv/bin/activate
pip install websockets
```

### 2. 로봇 연결 실패

**오류:**
```
ConnectionError: Failed to write 'Torque_Enable' on id_=X
```

**해결:**
1. 로봇 전원 확인
2. USB 케이블 확인
3. 포트 확인:
   ```bash
   lerobot-find-port
   ```
4. 다른 프로그램이 포트 사용 중인지 확인:
   ```bash
   pkill -f lerobot-teleoperate
   ```

### 3. Unity에서 연결 안 됨

**문제:** WebSocket 연결 실패

**해결:**
1. 서버 IP 주소 확인:
   ```bash
   ip addr show
   ```

2. 방화벽 확인:
   ```bash
   sudo ufw allow 8765
   ```

3. Unity 콘솔에서 오류 메시지 확인

4. 테스트 클라이언트로 서버 동작 확인:
   ```bash
   python3 test_websocket_client.py ws://서버IP:8765
   ```

### 4. 데이터가 느리게 도착함

**해결:**
1. 업데이트 주기 낮추기:
   ```bash
   python3 robot_data_stream_server.py --rate 15
   ```

2. 네트워크 지연 확인:
   ```bash
   ping 서버IP
   ```

### 5. JSON 파싱 오류 (Unity)

**문제:** Unity JsonUtility가 Dictionary를 지원하지 않음

**해결:** `RobotDataClientNewtonsoft.cs` 사용 (Newtonsoft.Json 필요)

---

## 📝 사용 예제 (전체 플로우)

### 1단계: Python 서버 시작

```bash
cd /home/jacob/Desktop/New\ Folder
source venv/bin/activate
python3 robot_data_stream_server.py --port /dev/ttyACM0 --rate 30
```

출력:
```
================================================================================
SO-101 Follower 로봇 데이터 스트리밍 서버
================================================================================
WebSocket 서버: ws://0.0.0.0:8765
업데이트 주기: 30.0 Hz
Ctrl+C로 종료
================================================================================
로봇 연결 중: /dev/ttyACM0
✓ 로봇 연결 성공!
데이터 브로드캐스팅 시작 (업데이트 주기: 30.0 Hz)
```

### 2단계: Unity VR 앱에서 연결

```csharp
// Unity 스크립트
public class VRRobotController : MonoBehaviour
{
    public RobotDataClientNewtonsoft robotClient;
    
    void Start()
    {
        robotClient.serverUrl = "ws://192.168.1.100:8765";
        robotClient.OnConnected += () => Debug.Log("로봇 연결됨!");
        robotClient.OnDataReceived += OnRobotDataReceived;
    }
    
    void OnRobotDataReceived(RobotDataComplete data)
    {
        // 모든 관절 업데이트
        UpdateRobotVisualization(data);
    }
}
```

### 3단계: 데이터 확인

Python 테스트:
```bash
python3 test_websocket_client.py ws://localhost:8765 5
```

---

## 🎯 다음 단계

이제 다음과 같은 기능을 추가할 수 있습니다:

1. **양방향 통신** - VR에서 로봇 제어
2. **카메라 스트리밍** - 로봇 카메라 영상 전송
3. **녹화/재생** - 동작 기록 및 재생
4. **멀티 로봇** - 여러 로봇 동시 제어

질문이나 문제가 있으면 언제든지 물어보세요! 🚀

