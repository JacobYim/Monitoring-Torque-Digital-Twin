#!/usr/bin/env python3
"""
SO-101 Follower 로봇 데이터 스트리밍 서버
WebSocket을 통해 실시간으로 관절 위치, 전류, 부하 데이터를 VR 애플리케이션에 전송

사용법:
    python3 robot_data_stream_server.py --port /dev/ttyACM0 --ws-port 8765
"""

import asyncio
import json
import time
import argparse
from typing import Dict, Set
import websockets
from websockets.server import WebSocketServerProtocol

from lerobot.motors.feetech.feetech import FeetechMotorsBus
from lerobot.robots.configs import SO101_FOLLOWER_MOTORS, SO101_FOLLOWER_CALIBRATION


class RobotDataStreamer:
    def __init__(self, robot_port: str = "/dev/ttyACM0", update_rate: float = 30.0):
        """
        Args:
            robot_port: 로봇이 연결된 시리얼 포트
            update_rate: 초당 업데이트 횟수 (Hz)
        """
        self.robot_port = robot_port
        self.update_rate = update_rate
        self.update_interval = 1.0 / update_rate
        
        # WebSocket 클라이언트 연결 관리
        self.connected_clients: Set[WebSocketServerProtocol] = set()
        
        # 로봇 버스
        self.bus = None
        self.is_running = False
        
    def connect_robot(self):
        """로봇에 연결"""
        print(f"로봇 연결 중: {self.robot_port}")
        self.bus = FeetechMotorsBus(
            port=self.robot_port,
            motors=SO101_FOLLOWER_MOTORS,
            calibration=SO101_FOLLOWER_CALIBRATION,
        )
        self.bus.connect()
        print("✓ 로봇 연결 성공!")
        
    def disconnect_robot(self):
        """로봇 연결 해제"""
        if self.bus and self.bus.is_connected:
            print("\n로봇 연결 해제 중...")
            self.bus.disconnect()
            print("✓ 로봇 연결 해제 완료")
    
    def read_robot_data(self) -> Dict:
        """로봇에서 현재 상태 읽기"""
        if not self.bus or not self.bus.is_connected:
            raise ConnectionError("로봇이 연결되어 있지 않습니다")
        
        # 위치 읽기
        positions = self.bus.sync_read("Present_Position")
        
        # 전류 읽기 (토크와 직접 관련)
        currents = self.bus.sync_read("Present_Current")
        
        # 부하 읽기 (토크 피드백)
        loads = self.bus.sync_read("Present_Load")
        
        # 데이터 구조화
        data = {
            "timestamp": time.time(),
            "motors": {}
        }
        
        for motor_name in self.bus.motors.keys():
            data["motors"][motor_name] = {
                "position": float(positions.get(motor_name, 0)),
                "current": float(currents.get(motor_name, 0)),
                "load": float(loads.get(motor_name, 0))
            }
        
        return data
    
    async def handle_client(self, websocket: WebSocketServerProtocol):
        """WebSocket 클라이언트 연결 처리"""
        client_addr = websocket.remote_address
        print(f"✓ 새 클라이언트 연결: {client_addr}")
        
        self.connected_clients.add(websocket)
        
        try:
            # 클라이언트로부터 메시지 수신 (연결 유지)
            async for message in websocket:
                # 클라이언트가 보낸 명령 처리 (선택사항)
                try:
                    cmd = json.loads(message)
                    if cmd.get("type") == "ping":
                        await websocket.send(json.dumps({"type": "pong"}))
                except json.JSONDecodeError:
                    pass
                    
        except websockets.exceptions.ConnectionClosed:
            print(f"✗ 클라이언트 연결 종료: {client_addr}")
        finally:
            self.connected_clients.discard(websocket)
    
    async def broadcast_robot_data(self):
        """모든 연결된 클라이언트에게 로봇 데이터 브로드캐스트"""
        print(f"데이터 브로드캐스팅 시작 (업데이트 주기: {self.update_rate} Hz)")
        
        while self.is_running:
            try:
                # 로봇 데이터 읽기
                data = self.read_robot_data()
                
                # JSON으로 변환
                json_data = json.dumps(data)
                
                # 모든 클라이언트에게 전송
                if self.connected_clients:
                    # 연결이 끊긴 클라이언트 제거
                    disconnected = set()
                    for client in self.connected_clients:
                        try:
                            await client.send(json_data)
                        except websockets.exceptions.ConnectionClosed:
                            disconnected.add(client)
                    
                    self.connected_clients -= disconnected
                
                # 업데이트 주기에 맞춰 대기
                await asyncio.sleep(self.update_interval)
                
            except Exception as e:
                print(f"오류 발생: {e}")
                await asyncio.sleep(1.0)
    
    async def start_server(self, ws_host: str = "0.0.0.0", ws_port: int = 8765):
        """WebSocket 서버 시작"""
        self.is_running = True
        
        print("=" * 80)
        print("SO-101 Follower 로봇 데이터 스트리밍 서버")
        print("=" * 80)
        print(f"WebSocket 서버: ws://{ws_host}:{ws_port}")
        print(f"업데이트 주기: {self.update_rate} Hz")
        print("Ctrl+C로 종료")
        print("=" * 80)
        
        # 로봇 연결
        self.connect_robot()
        
        # WebSocket 서버 시작
        async with websockets.serve(self.handle_client, ws_host, ws_port):
            # 데이터 브로드캐스팅 시작
            await self.broadcast_robot_data()


async def main():
    parser = argparse.ArgumentParser(description="로봇 데이터 스트리밍 서버")
    parser.add_argument("--port", default="/dev/ttyACM0", help="로봇 시리얼 포트")
    parser.add_argument("--ws-host", default="0.0.0.0", help="WebSocket 서버 호스트")
    parser.add_argument("--ws-port", type=int, default=8765, help="WebSocket 서버 포트")
    parser.add_argument("--rate", type=float, default=30.0, help="업데이트 주기 (Hz)")
    
    args = parser.parse_args()
    
    streamer = RobotDataStreamer(
        robot_port=args.port,
        update_rate=args.rate
    )
    
    try:
        await streamer.start_server(ws_host=args.ws_host, ws_port=args.ws_port)
    except KeyboardInterrupt:
        print("\n\n서버 종료 중...")
    finally:
        streamer.is_running = False
        streamer.disconnect_robot()


if __name__ == "__main__":
    asyncio.run(main())

