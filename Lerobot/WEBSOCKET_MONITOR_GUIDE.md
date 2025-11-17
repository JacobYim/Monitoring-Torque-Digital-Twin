# 🔍 WebSocket 실시간 모니터링 가이드

로봇 데이터 스트리밍을 실시간으로 모니터링하는 도구

---

## 📦 생성된 모니터링 스크립트

### 1. **monitor_websocket.py** (고급 버전, 권장)
- 실시간 UI (Rich 라이브러리 사용)
- 컬러풀한 테이블과 진행률 바
- FPS 및 통계 표시
- 토크 강도 시각화

### 2. **monitor_websocket_simple.py** (기본 버전)
- 최소 의존성 (websockets만 필요)
- 간단한 텍스트 출력
- 모든 환경에서 작동

---

## 🚀 빠른 시작

### 설치

**고급 버전 (권장):**
```bash
pip install rich websockets
```

**기본 버전:**
```bash
pip install websockets
```

### 사용법

#### 1단계: 로봇 teleoperate 시작

```bash
source venv/bin/activate

lerobot-teleoperate \
    --robot.type=so101_follower \
    --robot.port=/dev/ttyACM0 \
    --robot.id=jacob_follwer_1  \
    --teleop.type=so101_leader \
    --teleop.port=/dev/ttyACM1 \
    --teleop.id=jacob_learder_1 \
    --stream_data=true \
    --stream_port=8765
```

#### 2단계: 다른 터미널에서 모니터 실행

**로컬 모니터링:**
```bash
python3 monitor_websocket.py ws://localhost:8765
```

**원격 모니터링:**
```bash
python3 monitor_websocket.py ws://192.168.1.100:8765
```

**간단 버전:**
```bash
python3 monitor_websocket_simple.py ws://localhost:8765
```

---

## 🎨 monitor_websocket.py (고급 버전)

### 화면 구성

```
┌─────────────────────────────────────────────────────────────────────────────┐
│               🔍 WebSocket 실시간 모니터                                      │
├────────────────────────┬────────────────────────────────────────────────────┤
│ 📊 연결 상태           │ 🤖 로봇 관절 데이터                                 │
│                        │                                                     │
│ 상태     🟢 연결됨     │ 관절         위치      전류(mA)    부하    토크 강도│
│ 서버     ws://...      │ ─────────────────────────────────────────────────  │
│ 메시지 수   1,234      │ shoulder_pan   12.82°   125.5    234.0  ████░░░░░░│
│ 오류 수     0          │ shoulder_lift  25.18°    98.3    156.7  ███░░░░░░░│
│ FPS         30.0 Hz    │ elbow_flex    -43.24°    67.2     89.4  ██░░░░░░░░│
│ 실행 시간   45.3초     │ wrist_flex     78.94°    34.1     45.2  █░░░░░░░░░│
│ 마지막 수신 18:45:23   │ wrist_roll     13.56°    12.5     23.1  ░░░░░░░░░░│
│                        │ gripper        30.43°   156.8    345.6  █████░░░░░│
└────────────────────────┴────────────────────────────────────────────────────┘
```

### 기능

- ✅ **실시간 UI** - 초당 10회 업데이트
- 📊 **통계 정보** - 메시지 수, FPS, 실행 시간
- 🎨 **토크 시각화** - 전류 강도를 바로 표시
- 🟢🟡🔴 **색상 코딩** - 토크 강도에 따라 색상 변경
- ⚡ **실시간 FPS** - 최근 30프레임 평균

### 토크 강도 색상

| 강도 | 색상 | 설명 |
|------|------|------|
| 0-30% | 🟢 초록색 | 안전 |
| 30-70% | 🟡 노란색 | 주의 |
| 70-100% | 🔴 빨간색 | 경고 |

---

## 📝 monitor_websocket_simple.py (기본 버전)

### 화면 구성

```
====================================================================================================
📊 WebSocket 모니터 | 서버: ws://localhost:8765
====================================================================================================
메시지:    234 | FPS:  30.1 Hz | 오류:   0 | 실행 시간:   7.8초
----------------------------------------------------------------------------------------------------

⏱️  타임스탬프: 18:45:23.456

관절                 |       위치 |   전류(mA) |       부하 | 토크 바        
----------------------------------------------------------------------------------------------------
shoulder_pan         |     12.82° |      125.5 |      234.0 | ████░░░░░░░░░░░
shoulder_lift        |     25.18° |       98.3 |      156.7 | ███░░░░░░░░░░░░
elbow_flex           |    -43.24° |       67.2 |       89.4 | ██░░░░░░░░░░░░░
wrist_flex           |     78.94° |       34.1 |       45.2 | █░░░░░░░░░░░░░░
wrist_roll           |     13.56° |       12.5 |       23.1 | ░░░░░░░░░░░░░░░
gripper              |     30.43° |      156.8 |      345.6 | █████░░░░░░░░░░ ⚠️  높음
====================================================================================================
```

### 기능

- ✅ **최소 의존성** - websockets만 필요
- 📊 **기본 통계** - 메시지 수, FPS
- 📊 **토크 바** - ASCII 문자로 강도 표시
- ⚠️ **경고 표시** - 높은 토크 자동 감지

---

## 🔧 명령줄 옵션

### monitor_websocket.py

```bash
# 기본 사용 (localhost)
python3 monitor_websocket.py

# 서버 주소 지정
python3 monitor_websocket.py ws://192.168.1.100:8765

# 다른 포트
python3 monitor_websocket.py ws://localhost:9000
```

### monitor_websocket_simple.py

```bash
# 기본 사용
python3 monitor_websocket_simple.py

# 서버 주소 지정
python3 monitor_websocket_simple.py ws://192.168.1.100:8765
```

---

## 💡 사용 시나리오

### 1. 로컬 개발 및 테스트

```bash
# 터미널 1: teleoperate
lerobot-teleoperate ... --stream_data=true

# 터미널 2: 모니터
python3 monitor_websocket.py ws://localhost:8765
```

### 2. 원격 모니터링

로봇이 다른 컴퓨터에서 실행 중일 때:

```bash
# 로봇 컴퓨터 IP가 192.168.1.100인 경우
python3 monitor_websocket.py ws://192.168.1.100:8765
```

### 3. 성능 테스트

FPS와 지연 시간 확인:

```bash
# 고급 모니터로 FPS 확인
python3 monitor_websocket.py ws://localhost:8765
```

### 4. 문제 진단

연결 문제나 데이터 손실 감지:

```bash
# 오류 수와 메시지 수 모니터링
python3 monitor_websocket_simple.py ws://localhost:8765
```

---

## 🎯 모니터링 항목 설명

### 연결 상태

| 항목 | 설명 |
|------|------|
| **상태** | 🟢 연결됨 / 🔴 연결 끊김 |
| **메시지 수** | 수신한 총 메시지 수 |
| **오류 수** | JSON 파싱 오류 등 |
| **FPS** | 초당 메시지 수신 빈도 |
| **실행 시간** | 모니터 시작 후 경과 시간 |

### 관절 데이터

| 항목 | 단위 | 설명 |
|------|------|------|
| **위치** | degree (°) | 관절 각도 |
| **전류** | mA (밀리암페어) | 토크와 직접 관련 |
| **부하** | 0-1000 | 토크 피드백 |
| **토크 강도** | 바 | 시각적 강도 표시 |

### 관절 이름

- `shoulder_pan` - 어깨 팬
- `shoulder_lift` - 어깨 리프트
- `elbow_flex` - 팔꿈치 굽힘
- `wrist_flex` - 손목 굽힘
- `wrist_roll` - 손목 회전
- `gripper` - 그리퍼

---

## 🔍 문제 해결

### 연결 실패

**증상:**
```
❌ 연결 거부: ws://localhost:8765
```

**해결:**
1. teleoperate가 실행 중인지 확인
2. `--stream_data=true` 옵션 확인
3. 포트 번호 확인 (`--stream_port=8765`)
4. 방화벽 설정 확인

```bash
# teleoperate 프로세스 확인
ps aux | grep lerobot-teleoperate

# 포트 사용 확인
netstat -an | grep 8765
```

### Rich UI가 작동하지 않음

**증상:** 간단한 텍스트만 표시됨

**해결:**
```bash
pip install rich
```

### 데이터가 표시되지 않음

**증상:** "데이터 없음" 또는 빈 테이블

**해결:**
1. 로봇이 캘리브레이션되었는지 확인
2. teleoperate가 정상 작동 중인지 확인
3. 테스트 클라이언트로 확인:
   ```bash
   python3 test_websocket_client.py ws://localhost:8765 5
   ```

### FPS가 낮음

**원인:**
- 네트워크 지연
- 로봇 읽기 속도 제한
- 컴퓨터 성능

**해결:**
- 로컬 연결 사용 (localhost)
- teleoperate의 `--fps` 옵션 조정

---

## 📊 통계 해석

### 정상 범위

| 항목 | 정상 범위 | 비고 |
|------|-----------|------|
| **FPS** | 20-60 Hz | teleoperate의 fps 설정에 따름 |
| **메시지 손실** | 0% | 오류 수 = 0이 이상적 |
| **토크** | 0-300 mA | 일반 동작 시 |
| **부하** | 0-500 | 일반 동작 시 |

### 경고 상황

| 상황 | 설명 | 조치 |
|------|------|------|
| **FPS < 10** | 데이터 전송 지연 | 네트워크 확인 |
| **오류 수 증가** | 데이터 손상 | 서버 재시작 |
| **토크 > 400 mA** | 과부하 | 동작 중지 |
| **연결 끊김** | 서버 문제 | teleoperate 확인 |

---

## 🎓 활용 예제

### 1. 성능 벤치마크

```bash
# 30초간 FPS 측정
timeout 30 python3 monitor_websocket.py ws://localhost:8765
```

### 2. 로그 저장

```bash
# 출력을 파일로 저장
python3 monitor_websocket_simple.py ws://localhost:8765 > robot_log.txt
```

### 3. 토크 모니터링

고급 버전으로 토크 강도를 실시간 확인하며 로봇 조작

---

## 📝 요약

### 사용 흐름

1. **teleoperate 시작** (`--stream_data=true`)
2. **모니터 실행** (`python3 monitor_websocket.py`)
3. **데이터 확인** (위치, 전류, 부하)
4. **통계 분석** (FPS, 메시지 수)

### 선택 가이드

| 상황 | 권장 스크립트 |
|------|---------------|
| 일반 사용 | `monitor_websocket.py` (고급) |
| 최소 환경 | `monitor_websocket_simple.py` |
| 성능 테스트 | `monitor_websocket.py` |
| 로그 저장 | `monitor_websocket_simple.py` |

---

🎉 **이제 로봇의 모든 관절 상태를 실시간으로 모니터링할 수 있습니다!**

