#!/usr/bin/env python3
"""
샘플 로봇 데이터 WebSocket 서버
실제 로봇 없이 테스트할 수 있도록 시뮬레이션 데이터를 전송

사용법:
    python3 sample_robot_server.py --port 8765 --rate 30
    python3 sample_robot_server.py --scenario idle
    python3 sample_robot_server.py --scenario moving
    python3 sample_robot_server.py --scenario heavy_load
"""

import asyncio
import json
import math
import time
import argparse
from typing import Dict, Any, Set
import websockets
from websockets.server import WebSocketServerProtocol


class RobotSimulator:
    """로봇 관절 시뮬레이터"""
    
    # SO-101 로봇의 6개 관절
    MOTORS = [
        "shoulder_pan",
        "shoulder_lift", 
        "elbow_flex",
        "wrist_flex",
        "wrist_roll",
        "gripper"
    ]
    
    def __init__(self, scenario: str = "normal"):
        """
        Args:
            scenario: 시나리오 (idle, normal, moving, heavy_load, random)
        """
        self.scenario = scenario
        self.start_time = time.time()
        
        # 각 관절의 초기 위치
        self.base_positions = {
            "shoulder_pan": 0.0,
            "shoulder_lift": 20.0,
            "elbow_flex": -30.0,
            "wrist_flex": 45.0,
            "wrist_roll": 0.0,
            "gripper": 15.0,
        }
        
        # 각 관절의 움직임 파라미터 (진폭, 주파수)
        self.motion_params = {
            "shoulder_pan": (15.0, 0.3),    # 진폭 15°, 주파수 0.3 Hz
            "shoulder_lift": (10.0, 0.2),
            "elbow_flex": (20.0, 0.4),
            "wrist_flex": (30.0, 0.5),
            "wrist_roll": (25.0, 0.6),
            "gripper": (20.0, 0.35),
        }
    
    def get_elapsed_time(self) -> float:
        """시작 후 경과 시간"""
        return time.time() - self.start_time
    
    def generate_position(self, motor: str, t: float) -> float:
        """관절 위치 생성 (시뮬레이션)"""
        base = self.base_positions[motor]
        
        if self.scenario == "idle":
            # 정지 상태 - 약간의 노이즈만
            import random
            return base + random.uniform(-0.5, 0.5)
        
        elif self.scenario == "normal" or self.scenario == "moving":
            # 부드러운 사인파 움직임
            amplitude, frequency = self.motion_params[motor]
            return base + amplitude * math.sin(2 * math.pi * frequency * t)
        
        elif self.scenario == "heavy_load":
            # 느리고 작은 움직임
            amplitude, frequency = self.motion_params[motor]
            return base + (amplitude * 0.3) * math.sin(2 * math.pi * frequency * 0.5 * t)
        
        elif self.scenario == "random":
            # 랜덤 움직임
            import random
            amplitude, _ = self.motion_params[motor]
            return base + random.uniform(-amplitude, amplitude)
        
        else:
            return base
    
    def generate_current(self, motor: str, position: float, t: float) -> float:
        """전류 생성 (토크 시뮬레이션)"""
        if self.scenario == "idle":
            # 정지 - 최소 전류
            import random
            return random.uniform(5.0, 15.0)
        
        elif self.scenario == "normal":
            # 일반 동작 - 중간 전류
            base_current = 50.0
            # 위치 변화에 따른 전류 변동
            variation = abs(math.sin(2 * math.pi * 0.2 * t)) * 100.0
            return base_current + variation
        
        elif self.scenario == "moving":
            # 활발한 움직임 - 높은 전류
            base_current = 80.0
            variation = abs(math.sin(2 * math.pi * 0.3 * t)) * 150.0
            return base_current + variation
        
        elif self.scenario == "heavy_load":
            # 고부하 - 매우 높은 전류
            base_current = 200.0
            variation = abs(math.sin(2 * math.pi * 0.1 * t)) * 200.0
            
            # 그리퍼는 특히 높음
            if motor == "gripper":
                base_current = 300.0
            
            return base_current + variation
        
        elif self.scenario == "random":
            import random
            return random.uniform(10.0, 300.0)
        
        return 50.0
    
    def generate_load(self, motor: str, current: float) -> float:
        """부하 생성 (전류에 비례)"""
        # 부하는 전류와 상관관계가 있음
        base_load = current * 1.5
        
        # 약간의 노이즈 추가
        import random
        noise = random.uniform(-20.0, 20.0)
        
        return max(0.0, base_load + noise)
    
    def generate_data(self) -> Dict[str, Any]:
        """완전한 로봇 데이터 생성"""
        t = self.get_elapsed_time()
        
        data = {
            "timestamp": time.time(),
            "motors": {}
        }
        
        for motor in self.MOTORS:
            position = self.generate_position(motor, t)
            current = self.generate_current(motor, position, t)
            load = self.generate_load(motor, current)
            
            data["motors"][motor] = {
                "position": round(position, 2),
                "current": round(current, 1),
                "load": round(load, 1)
            }
        
        return data


class SampleRobotServer:
    """샘플 로봇 데이터 WebSocket 서버"""
    
    def __init__(self, host: str = "0.0.0.0", port: int = 8765, 
                 rate: float = 30.0, scenario: str = "normal"):
        self.host = host
        self.port = port
        self.rate = rate
        self.update_interval = 1.0 / rate
        self.scenario = scenario
        
        self.clients: Set[WebSocketServerProtocol] = set()
        self.simulator = RobotSimulator(scenario=scenario)
        self.is_running = False
        
        self.message_count = 0
        self.client_count = 0
    
    async def handle_client(self, websocket: WebSocketServerProtocol):
        """클라이언트 연결 처리"""
        client_addr = websocket.remote_address
        self.client_count += 1
        print(f"✅ 클라이언트 연결: {client_addr} (총 {len(self.clients) + 1}개)")
        
        self.clients.add(websocket)
        
        try:
            async for message in websocket:
                # 클라이언트 메시지 처리 (ping 등)
                try:
                    cmd = json.loads(message)
                    if cmd.get("type") == "ping":
                        await websocket.send(json.dumps({"type": "pong"}))
                except json.JSONDecodeError:
                    pass
        except websockets.exceptions.ConnectionClosed:
            pass
        finally:
            self.clients.discard(websocket)
            print(f"❌ 클라이언트 연결 종료: {client_addr} (남은: {len(self.clients)}개)")
    
    async def broadcast_data(self):
        """데이터 브로드캐스트"""
        print(f"📡 데이터 브로드캐스팅 시작 (시나리오: {self.scenario}, {self.rate} Hz)")
        
        while self.is_running:
            try:
                # 샘플 데이터 생성
                data = self.simulator.generate_data()
                json_data = json.dumps(data)
                
                # 연결된 모든 클라이언트에게 전송
                if self.clients:
                    disconnected = set()
                    for client in self.clients:
                        try:
                            await client.send(json_data)
                            self.message_count += 1
                        except websockets.exceptions.ConnectionClosed:
                            disconnected.add(client)
                    
                    self.clients -= disconnected
                
                # 업데이트 주기 대기
                await asyncio.sleep(self.update_interval)
                
            except Exception as e:
                print(f"❌ 오류: {e}")
                await asyncio.sleep(1.0)
    
    async def start(self):
        """서버 시작"""
        self.is_running = True
        
        print("=" * 80)
        print("🤖 샘플 로봇 데이터 WebSocket 서버")
        print("=" * 80)
        print(f"서버 주소:      ws://{self.host}:{self.port}")
        print(f"업데이트 주기:   {self.rate} Hz")
        print(f"시나리오:       {self.scenario}")
        print(f"관절 수:        {len(RobotSimulator.MOTORS)}개")
        print("=" * 80)
        print("\n시나리오 설명:")
        print("  • idle       - 정지 상태 (최소 토크)")
        print("  • normal     - 일반 동작 (중간 토크)")
        print("  • moving     - 활발한 움직임 (높은 토크)")
        print("  • heavy_load - 고부하 작업 (매우 높은 토크)")
        print("  • random     - 랜덤 데이터")
        print()
        print("종료: Ctrl+C")
        print("=" * 80)
        print()
        
        # WebSocket 서버 시작
        async with websockets.serve(self.handle_client, self.host, self.port):
            # 데이터 브로드캐스트 태스크 시작
            await self.broadcast_data()


async def main():
    parser = argparse.ArgumentParser(description="샘플 로봇 데이터 WebSocket 서버")
    parser.add_argument("--host", default="0.0.0.0", help="서버 호스트")
    parser.add_argument("--port", type=int, default=8765, help="서버 포트")
    parser.add_argument("--rate", type=float, default=30.0, help="업데이트 주기 (Hz)")
    parser.add_argument("--scenario", 
                        choices=["idle", "normal", "moving", "heavy_load", "random"],
                        default="normal",
                        help="시뮬레이션 시나리오")
    
    args = parser.parse_args()
    
    server = SampleRobotServer(
        host=args.host,
        port=args.port,
        rate=args.rate,
        scenario=args.scenario
    )
    
    try:
        await server.start()
    except KeyboardInterrupt:
        print("\n\n⏹️  서버 종료 중...")
        server.is_running = False
        print(f"\n통계:")
        print(f"  총 메시지: {server.message_count:,}")
        print(f"  총 연결: {server.client_count}")
    except Exception as e:
        print(f"\n❌ 오류: {e}")


if __name__ == "__main__":
    asyncio.run(main())

