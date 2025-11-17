# 🚀 VR 스트리밍 빠른 시작 가이드

SO-101 로봇의 관절 위치, 전류(토크), 부하 데이터를 VR(C#/Unity)로 실시간 스트리밍하는 방법

---

## 📦 생성된 파일

### Python 서버 (로봇 측)
- **`robot_data_stream_server.py`** - WebSocket 서버 (메인)
- **`test_websocket_client.py`** - 테스트 클라이언트

### C# 클라이언트 (VR/Unity 측)
- **`RobotDataClient.cs`** - 기본 WebSocket 클라이언트
- **`RobotDataClientNewtonsoft.cs`** - Newtonsoft.Json 버전 (권장)
- **`RobotVisualizerExample.cs`** - VR 시각화 예제

### 문서
- **`ROBOT_STREAMING_README.md`** - 상세 사용 설명서
- **`VR_STREAMING_QUICK_START.md`** - 이 파일 (빠른 시작)

---

## ⚡ 3단계 빠른 시작

### 1️⃣ Python 서버 시작 (로봇 컴퓨터)

```bash
cd /home/jacob/Desktop/New\ Folder
source venv/bin/activate
python3 robot_data_stream_server.py --port /dev/ttyACM0 --rate 30
```

**성공하면 다음이 표시됩니다:**
```
================================================================================
SO-101 Follower 로봇 데이터 스트리밍 서버
================================================================================
WebSocket 서버: ws://0.0.0.0:8765
업데이트 주기: 30.0 Hz
Ctrl+C로 종료
================================================================================
✓ 로봇 연결 성공!
데이터 브로드캐스팅 시작 (업데이트 주기: 30.0 Hz)
```

### 2️⃣ 서버 테스트 (선택사항)

새 터미널에서:
```bash
cd /home/jacob/Desktop/New\ Folder
python3 test_websocket_client.py ws://localhost:8765 10
```

**성공하면 실시간 데이터가 표시됩니다:**
```
메시지 #5 | 타임스탬프: 1699999999.123
--------------------------------------------------------------------------------
모터                 |       위치 |   전류(mA) |       부하
--------------------------------------------------------------------------------
shoulder_pan         |      12.82 |      125.5 |      234.0
shoulder_lift        |      25.18 |       98.3 |      156.7
elbow_flex           |     -43.24 |       67.2 |       89.4
wrist_flex           |      78.94 |       34.1 |       45.2
wrist_roll           |      13.56 |       12.5 |       23.1
gripper              |      30.43 |      156.8 |      345.6
```

### 3️⃣ Unity VR에서 연결

#### A. 패키지 설치

1. **WebSocketSharp** 설치
   - https://github.com/sta/websocket-sharp
   - 또는 Unity Asset Store에서 다운로드

2. **Newtonsoft.Json** 설치
   - Unity Package Manager
   - "Add package by name..."
   - `com.unity.nuget.newtonsoft-json`

#### B. 스크립트 추가

1. `RobotDataClientNewtonsoft.cs`를 Unity 프로젝트에 복사
2. 빈 GameObject 생성 → "RobotDataClient"
3. `RobotDataClientNewtonsoft` 컴포넌트 추가
4. Inspector에서 설정:
   - **Server Url**: `ws://로봇_컴퓨터_IP:8765`
   - 예: `ws://192.168.1.100:8765`

#### C. 로봇 IP 찾기

로봇 컴퓨터에서:
```bash
hostname -I
```

#### D. 데이터 사용 예제

```csharp
using UnityEngine;

public class SimpleRobotVR : MonoBehaviour
{
    public RobotDataClientNewtonsoft robotClient;
    public Transform shoulderJoint;
    
    void Start()
    {
        // 데이터 수신 이벤트 등록
        robotClient.OnDataReceived += OnRobotDataReceived;
    }
    
    void OnRobotDataReceived(RobotDataComplete data)
    {
        // shoulder_pan 모터 데이터 가져오기
        var shoulderData = robotClient.GetMotorData("shoulder_pan");
        
        if (shoulderData != null)
        {
            // 위치 적용
            float angle = shoulderData.position;
            shoulderJoint.localRotation = Quaternion.Euler(0, angle, 0);
            
            // 토크 강도 (0~1)
            float torque = shoulderData.TorqueIntensity;
            Debug.Log($"Shoulder Torque: {torque:P0}");
            
            // 토크가 높으면 햅틱 피드백
            if (torque > 0.7f)
            {
                // VR 컨트롤러 진동 트리거
            }
        }
    }
}
```

---

## 📊 전송되는 데이터

각 모터마다 3가지 값이 실시간으로 전송됩니다:

| 데이터 | 단위 | 설명 | VR 활용 예시 |
|--------|------|------|--------------|
| **position** | degree | 관절 위치 | 아바타 관절 각도 |
| **current** | mA | 전류 (토크) | 힘 시각화, 햅틱 |
| **load** | 0-1000 | 부하 | 저항감 표현 |

### 모터 이름 (6개)
- `shoulder_pan` - 어깨 팬
- `shoulder_lift` - 어깨 리프트  
- `elbow_flex` - 팔꿈치
- `wrist_flex` - 손목 굽힘
- `wrist_roll` - 손목 회전
- `gripper` - 그리퍼

---

## 🎯 VR 활용 아이디어

### 1. 로봇 아바타 표시
```csharp
// 각 관절을 실제 로봇과 동기화
void UpdateRobotAvatar(RobotDataComplete data)
{
    foreach (var motor in data.motors)
    {
        Transform joint = GetJointTransform(motor.Key);
        joint.localRotation = Quaternion.Euler(0, motor.Value.position, 0);
    }
}
```

### 2. 토크 시각화
```csharp
// 토크를 색상으로 표시
void VisualizeTorque(MotorDataComplete motorData)
{
    float intensity = motorData.TorqueIntensity; // 0~1
    jointRenderer.material.color = Color.Lerp(
        Color.green,  // 낮은 토크
        Color.red,    // 높은 토크
        intensity
    );
}
```

### 3. 햅틱 피드백
```csharp
// 부하에 따라 VR 컨트롤러 진동
void HapticFeedback(MotorDataComplete motorData)
{
    float load = motorData.LoadIntensity;
    if (load > 0.5f)
    {
        OVRInput.SetControllerVibration(
            frequency: load,
            amplitude: load,
            OVRInput.Controller.RTouch
        );
    }
}
```

### 4. 충돌/부하 감지
```csharp
// 과부하 경고
void CheckOverload(RobotDataComplete data)
{
    foreach (var motor in data.motors)
    {
        if (motor.Value.LoadIntensity > 0.8f)
        {
            ShowWarning($"{motor.Key} 과부하!");
            PlayWarningSound();
        }
    }
}
```

---

## 🔧 문제 해결

### 서버가 시작되지 않음
```bash
# websockets 설치 확인
pip install websockets
```

### Unity에서 연결 안 됨
1. **로봇 IP 확인**
   ```bash
   hostname -I
   ```

2. **방화벽 포트 열기**
   ```bash
   sudo ufw allow 8765
   ```

3. **ping 테스트**
   ```bash
   ping 로봇_IP
   ```

### 데이터가 안 들어옴
1. **테스트 클라이언트로 확인**
   ```bash
   python3 test_websocket_client.py ws://로봇_IP:8765 5
   ```

2. **Unity 콘솔 확인**
   - Show Debug Logs 옵션 켜기
   - 에러 메시지 확인

---

## 📝 다음 단계

### 기본 기능 완료 후:

1. **양방향 제어**
   - VR에서 로봇 제어 명령 전송
   - 서버에서 명령 수신 및 실행

2. **카메라 스트리밍**
   - 로봇 카메라 영상을 VR로 전송
   - WebRTC 또는 MJPEG 스트리밍

3. **녹화/재생**
   - 로봇 동작 녹화
   - VR에서 재생 및 분석

4. **멀티 로봇**
   - 여러 로봇 동시 모니터링
   - 각 로봇별 WebSocket 포트

---

## 🆘 도움말

- **전체 문서**: `ROBOT_STREAMING_README.md`
- **Python 서버 옵션**: 
  ```bash
  python3 robot_data_stream_server.py --help
  ```
- **로봇 설정 확인**: `note` 파일 참조

---

## ✅ 체크리스트

- [ ] Python 서버 실행 성공
- [ ] 테스트 클라이언트로 데이터 수신 확인
- [ ] Unity에 WebSocketSharp 설치
- [ ] Unity에 Newtonsoft.Json 설치
- [ ] Unity에서 로봇 데이터 수신 성공
- [ ] VR에서 로봇 아바타 동기화
- [ ] 토크 시각화 구현
- [ ] 햅틱 피드백 구현 (선택)

---

🎉 **축하합니다!** 이제 VR에서 실시간으로 로봇 상태를 모니터링할 수 있습니다!

