#!/usr/bin/env python3
"""
실시간 WebSocket 모니터링 도구
SO-101 로봇의 관절 데이터를 실시간으로 시각화

사용법:
    python3 monitor_websocket.py ws://localhost:8765
    python3 monitor_websocket.py ws://192.168.1.100:8765
"""

import asyncio
import json
import sys
import time
from collections import deque
from datetime import datetime
import websockets

try:
    from rich.console import Console
    from rich.layout import Layout
    from rich.live import Live
    from rich.panel import Panel
    from rich.table import Table
    from rich.progress import BarColumn, Progress, TextColumn
    from rich.text import Text
    RICH_AVAILABLE = True
except ImportError:
    RICH_AVAILABLE = False
    print("⚠️  'rich' 라이브러리를 설치하면 더 나은 UI를 사용할 수 있습니다:")
    print("   pip install rich")
    print()


class WebSocketMonitor:
    def __init__(self, uri: str, use_rich: bool = True):
        self.uri = uri
        self.use_rich = use_rich and RICH_AVAILABLE
        self.is_connected = False
        self.message_count = 0
        self.error_count = 0
        self.start_time = None
        self.last_message_time = None
        self.latest_data = None
        self.fps_history = deque(maxlen=30)  # 최근 30개 프레임
        
        if self.use_rich:
            self.console = Console()
        
    def calculate_fps(self):
        """현재 FPS 계산"""
        if len(self.fps_history) < 2:
            return 0.0
        time_diffs = []
        for i in range(1, len(self.fps_history)):
            time_diffs.append(self.fps_history[i] - self.fps_history[i-1])
        avg_time = sum(time_diffs) / len(time_diffs)
        return 1.0 / avg_time if avg_time > 0 else 0.0
    
    def create_status_panel(self):
        """상태 패널 생성 (Rich UI)"""
        if not self.use_rich:
            return None
            
        status_table = Table(show_header=False, box=None, padding=(0, 1))
        status_table.add_column("Key", style="cyan")
        status_table.add_column("Value", style="green")
        
        # 연결 상태
        conn_status = "🟢 연결됨" if self.is_connected else "🔴 연결 끊김"
        status_table.add_row("상태", conn_status)
        
        # 서버 주소
        status_table.add_row("서버", self.uri)
        
        # 메시지 통계
        status_table.add_row("메시지 수", f"{self.message_count:,}")
        status_table.add_row("오류 수", f"{self.error_count}")
        
        # FPS
        fps = self.calculate_fps()
        status_table.add_row("FPS", f"{fps:.1f} Hz")
        
        # 실행 시간
        if self.start_time:
            elapsed = time.time() - self.start_time
            status_table.add_row("실행 시간", f"{elapsed:.1f}초")
        
        # 마지막 메시지 시간
        if self.last_message_time:
            last_time = datetime.fromtimestamp(self.last_message_time).strftime("%H:%M:%S.%f")[:-3]
            status_table.add_row("마지막 수신", last_time)
        
        return Panel(status_table, title="📊 연결 상태", border_style="blue")
    
    def create_motor_panel(self):
        """모터 데이터 패널 생성 (Rich UI)"""
        if not self.use_rich or not self.latest_data:
            return Panel("데이터 없음", title="🤖 로봇 관절 데이터")
        
        motors = self.latest_data.get("motors", {})
        
        if not motors:
            return Panel("데이터 없음", title="🤖 로봇 관절 데이터")
        
        # 테이블 생성
        table = Table(show_header=True, header_style="bold magenta", box=None)
        table.add_column("관절", style="cyan", width=20)
        table.add_column("위치", justify="right", style="yellow", width=12)
        table.add_column("전류 (mA)", justify="right", style="red", width=12)
        table.add_column("부하", justify="right", style="green", width=12)
        table.add_column("토크 강도", width=20)
        
        for motor_name, motor_data in motors.items():
            position = motor_data.get("position", 0)
            current = motor_data.get("current", 0)
            load = motor_data.get("load", 0)
            
            # 토크 강도 바 (전류 기반)
            torque_intensity = min(abs(current) / 500.0, 1.0)  # 500mA를 최대로
            bar_length = int(torque_intensity * 10)
            torque_bar = "█" * bar_length + "░" * (10 - bar_length)
            
            # 색상 결정
            if torque_intensity < 0.3:
                bar_color = "green"
            elif torque_intensity < 0.7:
                bar_color = "yellow"
            else:
                bar_color = "red"
            
            table.add_row(
                motor_name,
                f"{position:>8.2f}°",
                f"{current:>8.1f}",
                f"{load:>8.1f}",
                Text(torque_bar, style=bar_color)
            )
        
        return Panel(table, title="🤖 로봇 관절 데이터", border_style="green")
    
    def create_layout(self):
        """레이아웃 생성 (Rich UI)"""
        if not self.use_rich:
            return None
            
        layout = Layout()
        layout.split_column(
            Layout(name="header", size=3),
            Layout(name="body"),
        )
        
        # 헤더
        header_text = Text("🔍 WebSocket 실시간 모니터", style="bold white on blue", justify="center")
        layout["header"].update(Panel(header_text, border_style="blue"))
        
        # 본문을 상태와 데이터로 분할
        layout["body"].split_row(
            Layout(name="status", ratio=1),
            Layout(name="motors", ratio=2),
        )
        
        layout["status"].update(self.create_status_panel())
        layout["motors"].update(self.create_motor_panel())
        
        return layout
    
    def display_simple(self):
        """간단한 텍스트 출력 (Rich 없을 때)"""
        print(f"\r연결: {'✓' if self.is_connected else '✗'} | "
              f"메시지: {self.message_count} | "
              f"FPS: {self.calculate_fps():.1f} Hz", end="")
        
        if self.message_count % 10 == 0 and self.latest_data:
            print()  # 새 줄
            motors = self.latest_data.get("motors", {})
            for motor_name, motor_data in motors.items():
                pos = motor_data.get("position", 0)
                curr = motor_data.get("current", 0)
                load = motor_data.get("load", 0)
                print(f"  {motor_name:20} | Pos: {pos:7.2f}° | "
                      f"Current: {curr:7.1f}mA | Load: {load:7.1f}")
            print()
    
    async def monitor(self):
        """WebSocket 모니터링 메인 루프"""
        self.start_time = time.time()
        
        try:
            async with websockets.connect(self.uri) as websocket:
                self.is_connected = True
                print(f"✓ 서버 연결 성공: {self.uri}\n")
                
                if self.use_rich:
                    # Rich UI 사용
                    with Live(self.create_layout(), refresh_per_second=10, screen=True) as live:
                        async for message in websocket:
                            try:
                                self.latest_data = json.loads(message)
                                self.message_count += 1
                                self.last_message_time = time.time()
                                self.fps_history.append(self.last_message_time)
                                
                                # UI 업데이트
                                live.update(self.create_layout())
                                
                            except json.JSONDecodeError:
                                self.error_count += 1
                else:
                    # 간단한 텍스트 출력
                    async for message in websocket:
                        try:
                            self.latest_data = json.loads(message)
                            self.message_count += 1
                            self.last_message_time = time.time()
                            self.fps_history.append(self.last_message_time)
                            
                            self.display_simple()
                            
                        except json.JSONDecodeError:
                            self.error_count += 1
                            
        except websockets.exceptions.WebSocketException as e:
            print(f"\n✗ WebSocket 오류: {e}")
        except ConnectionRefusedError:
            print(f"\n✗ 연결 거부: {self.uri}")
            print("\n서버가 실행 중인지 확인하세요:")
            print("  lerobot-teleoperate ... --stream_data=true")
        except KeyboardInterrupt:
            print("\n\n모니터링 중단")
        finally:
            self.is_connected = False
            if self.message_count > 0:
                elapsed = time.time() - self.start_time
                avg_fps = self.message_count / elapsed
                print(f"\n통계:")
                print(f"  총 메시지: {self.message_count:,}")
                print(f"  평균 FPS: {avg_fps:.1f} Hz")
                print(f"  실행 시간: {elapsed:.1f}초")


async def main():
    # 명령줄 인자 처리
    if len(sys.argv) > 1:
        uri = sys.argv[1]
    else:
        uri = "ws://localhost:8765"
    
    print("=" * 80)
    print("🔍 WebSocket 실시간 모니터")
    print("=" * 80)
    print(f"서버: {uri}")
    print("종료: Ctrl+C")
    print("=" * 80)
    print()
    
    monitor = WebSocketMonitor(uri, use_rich=RICH_AVAILABLE)
    await monitor.monitor()


if __name__ == "__main__":
    asyncio.run(main())

