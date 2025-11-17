# 🎉 통합 VR 스트리밍 (lerobot-teleoperate 내장)

`lerobot-teleoperate` 실행 시 **자동으로 WebSocket 스트리밍**이 시작됩니다!

---

## ✨ 새로운 기능

**이제 별도의 스트리밍 서버가 필요 없습니다!**

기존:
```bash
# 터미널 1: 스트리밍 서버
python3 robot_data_stream_server.py --port /dev/ttyACM0

# 터미널 2: teleoperate
lerobot-teleoperate --robot.port=/dev/ttyACM0 ...
```

**새로운 방식:**
```bash
# 하나의 명령어로 teleoperate + 스트리밍!
lerobot-teleoperate \
    --robot.port=/dev/ttyACM0 \
    --stream_data=true \
    --stream_port=8765 \
    ... (기타 옵션)
```

---

## 🚀 사용 방법

### 기본 사용

```bash
cd /home/jacob/Desktop/New\ Folder
source venv/bin/activate

lerobot-teleoperate \
    --robot.type=so101_follower \
    --robot.port=/dev/ttyACM0 \
    --robot.id=jacob_follwer_1  \
    --robot.cameras='{"front": {"type": "opencv", "index_or_path": 0, "width": 640, "height": 480, "fps": 30}, "wrist": {"type": "opencv", "index_or_path": 2, "width": 640, "height": 480, "fps": 30}}' \
    --teleop.type=so101_leader \
    --teleop.port=/dev/ttyACM1 \
    --teleop.id=jacob_learder_1 \
    --display_data=true \
    --stream_data=true \
    --stream_port=8765
```

### 새로운 옵션

| 옵션 | 기본값 | 설명 |
|------|--------|------|
| `--stream_data` | `false` | WebSocket 스트리밍 활성화 |
| `--stream_port` | `8765` | WebSocket 서버 포트 |
| `--stream_host` | `"0.0.0.0"` | WebSocket 서버 호스트 |

---

## 📊 어떻게 작동하나요?

```
┌─────────────────────────────────────┐
│     lerobot-teleoperate             │
│  ┌───────────────────────────────┐  │
│  │  로봇 제어 루프                │  │
│  │  • Leader → Follower          │  │
│  │  • 카메라 읽기                 │  │
│  │  • 위치/전류/부하 읽기         │  │
│  └──────────┬──────────────────┬──┘  │
│             │                  │     │
│             ↓                  ↓     │
│    ┌──────────────┐   ┌────────────┐│
│    │ 화면 표시     │   │ WebSocket  ││
│    │ (display)    │   │ 브로드캐스트││
│    └──────────────┘   └──────┬─────┘│
└───────────────────────────────┼──────┘
                                │
                                ↓
                        ┌───────────────┐
                        │  VR (Unity)   │
                        │  C# Client    │
                        └───────────────┘
```

**장점:**
- ✅ 하나의 프로세스로 통합 관리
- ✅ 로봇 데이터를 중복으로 읽지 않음 (효율적)
- ✅ 자동 시작/종료 (teleoperate와 함께)
- ✅ 동일한 데이터 소스 (정확성)

---

## 🎮 Unity/C#에서 연결

**Unity 클라이언트는 변경 없이 동일하게 사용:**

```csharp
public class RobotVR : MonoBehaviour
{
    public RobotDataClientNewtonsoft robotClient;
    
    void Start()
    {
        // 로봇 컴퓨터의 IP로 연결
        robotClient.serverUrl = "ws://192.168.1.100:8765";
        robotClient.OnDataReceived += OnRobotDataReceived;
    }
    
    void OnRobotDataReceived(RobotDataComplete data)
    {
        // 관절 데이터 사용
        foreach (var motor in data.motors)
        {
            UpdateJoint(motor.Key, motor.Value);
        }
    }
}
```

---

## 📝 note 파일 명령어

`note` 파일이 업데이트되었습니다:

### 두 카메라 + VR 스트리밍 (라인 43-53)
```bash
lerobot-teleoperate \
    --robot.type=so101_follower \
    --robot.port=/dev/ttyACM0 \
    --robot.id=jacob_follwer_1  \
    --robot.cameras='{"front": {"type": "opencv", "index_or_path": 0, "width": 640, "height": 480, "fps": 30}, "wrist": {"type": "opencv", "index_or_path": 2, "width": 640, "height": 480, "fps": 30}}' \
    --teleop.type=so101_leader \
    --teleop.port=/dev/ttyACM1 \
    --teleop.id=jacob_learder_1 \
    --display_data=true \
    --stream_data=true \
    --stream_port=8765
```

### 카메라 0만 + VR 스트리밍 (라인 57-67)
```bash
lerobot-teleoperate \
    --robot.type=so101_follower \
    --robot.port=/dev/ttyACM0 \
    --robot.id=jacob_follwer_1  \
    --robot.cameras='{"front": {"type": "opencv", "index_or_path": 0, "width": 640, "height": 480, "fps": 30}}' \
    --teleop.type=so101_leader \
    --teleop.port=/dev/ttyACM1 \
    --teleop.id=jacob_learder_1 \
    --display_data=true \
    --stream_data=true \
    --stream_port=8765
```

---

## 🔍 확인 방법

### 1. teleoperate 실행

```bash
source venv/bin/activate
lerobot-teleoperate ... --stream_data=true --stream_port=8765
```

**출력에서 확인:**
```
INFO ... WebSocket streaming enabled: ws://0.0.0.0:8765
INFO ... WebSocket streaming server started: ws://0.0.0.0:8765
```

### 2. 테스트 클라이언트로 확인

**다른 터미널에서:**
```bash
python3 test_websocket_client.py ws://localhost:8765 10
```

**성공하면:**
```
✓ 서버 연결 성공!

메시지 #5 | 타임스탬프: 1699999999.123
--------------------------------------------------------------------------------
모터                 |       위치 |   전류(mA) |       부하
--------------------------------------------------------------------------------
shoulder_pan         |      12.82 |      125.5 |      234.0
shoulder_lift        |      25.18 |       98.3 |      156.7
...
```

### 3. Unity에서 연결

Unity 콘솔에서:
```
✓ 서버 연결 성공: ws://192.168.1.100:8765
```

---

## 🔧 문제 해결

### 스트리밍이 시작되지 않음

**증상:** WebSocket 관련 로그가 없음

**해결:**
```bash
# --stream_data=true 옵션 확인
lerobot-teleoperate ... --stream_data=true
```

### 포트가 이미 사용 중

**증상:**
```
OSError: [Errno 98] Address already in use
```

**해결:**
```bash
# 1. 다른 스트리밍 서버 종료
pkill -f robot_data_stream_server

# 2. 또는 다른 포트 사용
lerobot-teleoperate ... --stream_port=8766
```

### Unity에서 연결 안 됨

**확인 사항:**
1. 로봇 컴퓨터 IP 주소 확인
   ```bash
   hostname -I
   ```

2. 방화벽 확인
   ```bash
   sudo ufw allow 8765
   ```

3. 테스트 클라이언트로 서버 동작 확인
   ```bash
   python3 test_websocket_client.py ws://로봇IP:8765 5
   ```

---

## 🆚 비교: 독립 서버 vs 통합 스트리밍

### 독립 스트리밍 서버 (`robot_data_stream_server.py`)

**장점:**
- teleoperate 없이도 독립 실행 가능
- 간단한 테스트에 유용

**단점:**
- 두 개의 프로세스 관리
- 로봇 포트 충돌 가능
- 데이터를 두 번 읽음 (비효율)

### 통합 스트리밍 (teleoperate 내장)

**장점:**
- ✅ 하나의 프로세스
- ✅ 자동 시작/종료
- ✅ 효율적 (데이터 한 번만 읽음)
- ✅ 간편한 사용

**단점:**
- teleoperate가 필요함
- (대부분의 사용 케이스에서는 문제 없음)

---

## 💡 권장 사용법

### 일반적인 사용 (로봇 제어 + VR)
```bash
# 통합 스트리밍 사용 (권장)
lerobot-teleoperate \
    --robot.port=/dev/ttyACM0 \
    --stream_data=true \
    ... (기타 옵션)
```

### 테스트/개발 (VR만)
```bash
# 독립 서버 사용
python3 robot_data_stream_server.py --port /dev/ttyACM0
```

---

## 📖 추가 문서

- **상세 가이드**: `ROBOT_STREAMING_README.md`
- **빠른 시작**: `VR_STREAMING_QUICK_START.md`
- **Unity 클라이언트**: `RobotDataClientNewtonsoft.cs`
- **명령어 모음**: `note` 파일

---

## 🎉 요약

**이제 `--stream_data=true` 옵션만 추가하면 끝!**

```bash
lerobot-teleoperate \
    <기존 옵션들...> \
    --stream_data=true \
    --stream_port=8765
```

Unity VR 앱에서 `ws://로봇IP:8765`로 연결하면 실시간으로 로봇의 모든 관절 상태를 받아볼 수 있습니다! 🚀

