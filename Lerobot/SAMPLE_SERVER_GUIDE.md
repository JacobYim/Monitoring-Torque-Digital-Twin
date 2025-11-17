# 🤖 샘플 로봇 서버 가이드

로봇 없이도 테스트할 수 있는 샘플 데이터 WebSocket 서버

---

## 🎯 용도

**실제 로봇이 없을 때 다음을 테스트:**
- WebSocket 모니터링 도구
- Unity VR 클라이언트
- 데이터 파싱 및 처리
- 네트워크 연결
- UI/UX 개발

---

## 🚀 빠른 시작

### 기본 사용

```bash
python3 sample_robot_server.py
```

### 옵션 지정

```bash
python3 sample_robot_server.py --port 8765 --rate 30 --scenario normal
```

---

## 📊 시나리오

### 1. **idle** - 정지 상태

로봇이 거의 움직이지 않음

```bash
python3 sample_robot_server.py --scenario idle
```

**특징:**
- 위치: 거의 고정 (±0.5° 노이즈)
- 전류: 5-15 mA (최소)
- 부하: 매우 낮음
- 용도: 대기 상태 테스트

### 2. **normal** - 일반 동작 (기본값)

정상적인 로봇 작업

```bash
python3 sample_robot_server.py --scenario normal
```

**특징:**
- 위치: 부드러운 사인파 움직임
- 전류: 50-150 mA (중간)
- 부하: 중간
- 용도: 일반 개발 및 테스트

### 3. **moving** - 활발한 움직임

로봇이 빠르게 움직임

```bash
python3 sample_robot_server.py --scenario moving
```

**특징:**
- 위치: 큰 진폭의 움직임
- 전류: 80-230 mA (높음)
- 부하: 높음
- 용도: 성능 테스트, 토크 시각화

### 4. **heavy_load** - 고부하 작업

무거운 물체를 다루는 상황

```bash
python3 sample_robot_server.py --scenario heavy_load
```

**특징:**
- 위치: 느리고 작은 움직임
- 전류: 200-400 mA (매우 높음)
- 부하: 매우 높음
- 그리퍼: 300-500 mA (과부하)
- 용도: 경고 시스템 테스트, 과부하 감지

### 5. **random** - 랜덤 데이터

예측 불가능한 데이터

```bash
python3 sample_robot_server.py --scenario random
```

**특징:**
- 위치: 랜덤
- 전류: 10-300 mA (랜덤)
- 부하: 랜덤
- 용도: 엣지 케이스 테스트, 오류 처리

---

## 🔧 명령줄 옵션

| 옵션 | 기본값 | 설명 |
|------|--------|------|
| `--host` | `0.0.0.0` | 서버 호스트 주소 |
| `--port` | `8765` | 서버 포트 번호 |
| `--rate` | `30.0` | 업데이트 주기 (Hz) |
| `--scenario` | `normal` | 시뮬레이션 시나리오 |

### 예제

```bash
# 포트 9000, 60Hz, 고부하 시나리오
python3 sample_robot_server.py --port 9000 --rate 60 --scenario heavy_load

# localhost만, 10Hz, idle
python3 sample_robot_server.py --host localhost --rate 10 --scenario idle
```

---

## 📡 데이터 형식

샘플 서버는 실제 로봇과 **동일한 형식**으로 데이터를 전송합니다:

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

---

## 💡 사용 시나리오

### 1. 모니터링 도구 테스트

```bash
# 터미널 1: 샘플 서버
python3 sample_robot_server.py --scenario moving

# 터미널 2: 모니터
python3 monitor_websocket.py ws://localhost:8765
```

### 2. Unity VR 개발

```bash
# 샘플 서버 실행
python3 sample_robot_server.py --scenario normal --rate 30

# Unity에서 연결
# RobotDataClientNewtonsoft.serverUrl = "ws://localhost:8765"
```

### 3. 다양한 시나리오 테스트

```bash
# 1분간 idle 테스트
timeout 60 python3 sample_robot_server.py --scenario idle

# 고부하 상황 테스트
python3 sample_robot_server.py --scenario heavy_load
```

### 4. 네트워크 성능 테스트

```bash
# 고속 데이터 전송 (100Hz)
python3 sample_robot_server.py --rate 100 --scenario moving
```

---

## 🎮 완전한 테스트 흐름

### 단계별 가이드

#### 1단계: 샘플 서버 시작

```bash
cd /home/jacob/Desktop/New\ Folder
python3 sample_robot_server.py --scenario normal --rate 30
```

**출력:**
```
================================================================================
🤖 샘플 로봇 데이터 WebSocket 서버
================================================================================
서버 주소:      ws://0.0.0.0:8765
업데이트 주기:   30.0 Hz
시나리오:       normal
관절 수:        6개
================================================================================

시나리오 설명:
  • idle       - 정지 상태 (최소 토크)
  • normal     - 일반 동작 (중간 토크)
  • moving     - 활발한 움직임 (높은 토크)
  • heavy_load - 고부하 작업 (매우 높은 토크)
  • random     - 랜덤 데이터

종료: Ctrl+C
================================================================================

📡 데이터 브로드캐스팅 시작 (시나리오: normal, 30.0 Hz)
```

#### 2단계: 모니터로 확인

**새 터미널에서:**
```bash
python3 monitor_websocket.py ws://localhost:8765
```

**또는 간단 버전:**
```bash
python3 monitor_websocket_simple.py ws://localhost:8765
```

#### 3단계: Unity VR 테스트

Unity에서:
```csharp
robotClient.serverUrl = "ws://localhost:8765";
```

#### 4단계: 다른 시나리오 테스트

서버를 종료(Ctrl+C)하고 다른 시나리오로 재시작:

```bash
# 고부하 테스트
python3 sample_robot_server.py --scenario heavy_load

# 랜덤 테스트
python3 sample_robot_server.py --scenario random
```

---

## 📊 시나리오별 데이터 범위

| 시나리오 | 위치 변화 | 전류 (mA) | 부하 | 특징 |
|----------|-----------|-----------|------|------|
| **idle** | ±0.5° | 5-15 | 낮음 | 정지 |
| **normal** | ±15° | 50-150 | 중간 | 일반 |
| **moving** | ±20° | 80-230 | 높음 | 빠름 |
| **heavy_load** | ±5° | 200-400 | 매우 높음 | 과부하 |
| **random** | 랜덤 | 10-300 | 랜덤 | 예측 불가 |

---

## 🔍 모니터링 예상 결과

### normal 시나리오

```
관절              위치      전류(mA)    부하      토크 바
─────────────────────────────────────────────────────────
shoulder_pan      12.82°    125.5      234.0     ████░░░░░░
shoulder_lift     25.18°     98.3      156.7     ███░░░░░░░
elbow_flex       -43.24°     67.2       89.4     ██░░░░░░░░
wrist_flex        78.94°     34.1       45.2     █░░░░░░░░░
wrist_roll        13.56°     12.5       23.1     ░░░░░░░░░░
gripper           30.43°    156.8      345.6     █████░░░░░
```

### heavy_load 시나리오

```
관절              위치      전류(mA)    부하      토크 바
─────────────────────────────────────────────────────────
shoulder_pan       5.32°    325.8      512.3     ████████░░ ⚠️ 높음
shoulder_lift     18.45°    298.4      445.6     ███████░░░ ⚠️ 높음
elbow_flex       -28.12°    267.9      389.2     ███████░░░
wrist_flex        42.67°    245.3      356.8     ██████░░░░
wrist_roll         3.89°    223.1      334.5     ██████░░░░
gripper           12.34°    456.7      678.9     █████████░ ⚠️ 높음
```

---

## 🎓 개발 워크플로우

### 단계 1: 기본 연결 테스트

```bash
python3 sample_robot_server.py --scenario idle
python3 monitor_websocket_simple.py ws://localhost:8765
```

### 단계 2: UI 개발

```bash
python3 sample_robot_server.py --scenario normal --rate 30
# Unity에서 UI 개발 및 테스트
```

### 단계 3: 토크 시각화 테스트

```bash
python3 sample_robot_server.py --scenario moving --rate 30
# 토크 강도 바, 색상 변화 확인
```

### 단계 4: 경고 시스템 테스트

```bash
python3 sample_robot_server.py --scenario heavy_load
# 과부하 경고, 알림 테스트
```

### 단계 5: 성능 테스트

```bash
python3 sample_robot_server.py --scenario moving --rate 60
# 높은 FPS에서 안정성 확인
```

### 단계 6: 실제 로봇 연결

```bash
# 샘플 서버 종료
# 실제 로봇으로 전환
lerobot-teleoperate ... --stream_data=true --stream_port=8765
```

---

## 🆚 샘플 서버 vs 실제 로봇

| 항목 | 샘플 서버 | 실제 로봇 |
|------|-----------|-----------|
| **데이터 형식** | ✅ 동일 | ✅ 동일 |
| **업데이트 주기** | ✅ 설정 가능 | ✅ 설정 가능 |
| **관절 수** | ✅ 6개 | ✅ 6개 |
| **데이터 범위** | ⚠️ 시뮬레이션 | ✅ 실제 |
| **실시간성** | ⚠️ 예측 가능 | ✅ 실제 반응 |
| **하드웨어 필요** | ❌ 불필요 | ✅ 필요 |

---

## 🔧 문제 해결

### 포트가 이미 사용 중

**증상:**
```
OSError: [Errno 98] Address already in use
```

**해결:**
```bash
# 다른 포트 사용
python3 sample_robot_server.py --port 8766

# 또는 기존 서버 종료
pkill -f sample_robot_server
```

### 클라이언트가 연결되지 않음

**확인 사항:**
1. 서버가 실행 중인지 확인
2. 포트 번호 일치 확인
3. 방화벽 설정 확인

```bash
# 서버 실행 확인
ps aux | grep sample_robot_server

# 포트 사용 확인
netstat -an | grep 8765
```

---

## 📝 note 파일에 추가된 명령어

```bash
# 샘플 로봇 서버 (로봇 없이 테스트)
python3 sample_robot_server.py --scenario normal --rate 30

# 다양한 시나리오
python3 sample_robot_server.py --scenario idle
python3 sample_robot_server.py --scenario moving
python3 sample_robot_server.py --scenario heavy_load
```

---

## 🎉 요약

### 사용 흐름

1. **샘플 서버 시작**
   ```bash
   python3 sample_robot_server.py --scenario normal
   ```

2. **모니터 또는 VR 클라이언트 연결**
   ```bash
   python3 monitor_websocket.py ws://localhost:8765
   ```

3. **다양한 시나리오 테스트**
   - idle → normal → moving → heavy_load

4. **실제 로봇으로 전환**
   ```bash
   lerobot-teleoperate ... --stream_data=true
   ```

### 장점

✅ 로봇 없이 개발 가능
✅ 다양한 시나리오 테스트
✅ 안전한 테스트 환경
✅ 빠른 반복 개발
✅ 실제 로봇과 동일한 데이터 형식

---

**이제 로봇이 없어도 모든 기능을 테스트할 수 있습니다!** 🚀

