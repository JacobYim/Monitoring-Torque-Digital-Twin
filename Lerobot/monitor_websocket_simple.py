#!/usr/bin/env python3
"""
간단한 WebSocket 모니터링 도구 (의존성 없음)
websockets 라이브러리만 필요

사용법:
    python3 monitor_websocket_simple.py
    python3 monitor_websocket_simple.py ws://192.168.1.100:8765
"""

import asyncio
import json
import sys
import time
from collections import deque
from datetime import datetime

try:
    import websockets
except ImportError:
    print("❌ 'websockets' 라이브러리가 필요합니다:")
    print("   pip install websockets")
    sys.exit(1)


class SimpleMonitor:
    def __init__(self, uri: str):
        self.uri = uri
        self.message_count = 0
        self.error_count = 0
        self.start_time = None
        self.fps_history = deque(maxlen=10)
        self.latest_data = None
        
    def calculate_fps(self):
        """FPS 계산"""
        if len(self.fps_history) < 2:
            return 0.0
        time_diff = self.fps_history[-1] - self.fps_history[0]
        return (len(self.fps_history) - 1) / time_diff if time_diff > 0 else 0.0
    
    def display_status(self):
        """상태 표시"""
        fps = self.calculate_fps()
        elapsed = time.time() - self.start_time if self.start_time else 0
        
        # 화면 지우기 (간단한 방법)
        print("\n" * 2)
        print("=" * 100)
        print(f"📊 WebSocket 모니터 | 서버: {self.uri}")
        print("=" * 100)
        print(f"메시지: {self.message_count:>6} | "
              f"FPS: {fps:>5.1f} Hz | "
              f"오류: {self.error_count:>3} | "
              f"실행 시간: {elapsed:>6.1f}초")
        print("-" * 100)
        
    def display_motor_data(self):
        """모터 데이터 표시"""
        if not self.latest_data:
            print("데이터 대기 중...")
            return
        
        motors = self.latest_data.get("motors", {})
        timestamp = self.latest_data.get("timestamp", 0)
        
        print(f"\n⏱️  타임스탬프: {datetime.fromtimestamp(timestamp).strftime('%H:%M:%S.%f')[:-3]}")
        print()
        print(f"{'관절':<20} | {'위치':>10} | {'전류(mA)':>10} | {'부하':>10} | {'토크 바':<15}")
        print("-" * 100)
        
        for motor_name, motor_data in motors.items():
            position = motor_data.get("position", 0)
            current = motor_data.get("current", 0)
            load = motor_data.get("load", 0)
            
            # 토크 강도 바
            torque_intensity = min(abs(current) / 500.0, 1.0)
            bar_length = int(torque_intensity * 15)
            torque_bar = "█" * bar_length + "░" * (15 - bar_length)
            
            # 경고 표시
            warning = ""
            if torque_intensity > 0.8:
                warning = "⚠️  높음"
            elif torque_intensity > 0.5:
                warning = "⚡ 중간"
            
            print(f"{motor_name:<20} | {position:>9.2f}° | "
                  f"{current:>9.1f} | {load:>9.1f} | "
                  f"{torque_bar} {warning}")
        
        print("=" * 100)
    
    async def monitor(self):
        """모니터링 메인 루프"""
        self.start_time = time.time()
        
        try:
            print(f"\n서버에 연결 중: {self.uri}...")
            
            async with websockets.connect(self.uri) as websocket:
                print(f"✅ 연결 성공!\n")
                
                async for message in websocket:
                    try:
                        # 데이터 파싱
                        self.latest_data = json.loads(message)
                        self.message_count += 1
                        current_time = time.time()
                        self.fps_history.append(current_time)
                        
                        # 화면 업데이트 (매 메시지마다)
                        self.display_status()
                        self.display_motor_data()
                        
                    except json.JSONDecodeError as e:
                        self.error_count += 1
                        print(f"❌ JSON 파싱 오류: {e}")
                        
        except websockets.exceptions.WebSocketException as e:
            print(f"\n❌ WebSocket 오류: {e}")
            print("\n서버가 실행 중인지 확인하세요:")
            print("  lerobot-teleoperate ... --stream_data=true --stream_port=8765")
        except ConnectionRefusedError:
            print(f"\n❌ 연결 거부: {self.uri}")
            print("\n서버가 실행 중인지 확인하세요:")
            print("  lerobot-teleoperate ... --stream_data=true")
        except KeyboardInterrupt:
            print("\n\n⏹️  모니터링 중단")
        finally:
            if self.message_count > 0:
                elapsed = time.time() - self.start_time
                avg_fps = self.message_count / elapsed
                print("\n" + "=" * 100)
                print("📊 통계 요약")
                print("=" * 100)
                print(f"총 메시지:    {self.message_count:,}")
                print(f"평균 FPS:     {avg_fps:.2f} Hz")
                print(f"오류 수:      {self.error_count}")
                print(f"실행 시간:    {elapsed:.1f}초")
                print("=" * 100)


async def main():
    # 명령줄 인자
    uri = sys.argv[1] if len(sys.argv) > 1 else "ws://localhost:8765"
    
    print("=" * 100)
    print("🔍 WebSocket 간단 모니터")
    print("=" * 100)
    print(f"서버: {uri}")
    print("종료: Ctrl+C")
    print("=" * 100)
    
    monitor = SimpleMonitor(uri)
    await monitor.monitor()


if __name__ == "__main__":
    asyncio.run(main())

