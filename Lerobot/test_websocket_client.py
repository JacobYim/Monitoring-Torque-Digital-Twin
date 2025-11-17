#!/usr/bin/env python3
"""
WebSocket 테스트 클라이언트
로봇 데이터 스트리밍 서버를 테스트하기 위한 간단한 Python 클라이언트
"""

import asyncio
import websockets
import json
import sys

async def test_client(uri="ws://localhost:8765", duration=10):
    """
    WebSocket 서버에 연결하여 데이터 수신 테스트
    
    Args:
        uri: WebSocket 서버 주소
        duration: 테스트 지속 시간 (초)
    """
    print("=" * 80)
    print("로봇 데이터 스트리밍 테스트 클라이언트")
    print("=" * 80)
    print(f"서버 주소: {uri}")
    print(f"테스트 시간: {duration}초\n")
    
    try:
        async with websockets.connect(uri) as websocket:
            print("✓ 서버 연결 성공!\n")
            
            message_count = 0
            start_time = asyncio.get_event_loop().time()
            
            while True:
                # 시간 체크
                elapsed = asyncio.get_event_loop().time() - start_time
                if elapsed >= duration:
                    break
                
                try:
                    # 데이터 수신 (타임아웃 1초)
                    message = await asyncio.wait_for(websocket.recv(), timeout=1.0)
                    message_count += 1
                    
                    # JSON 파싱
                    data = json.loads(message)
                    
                    # 데이터 표시
                    print(f"\r메시지 #{message_count} | 타임스탬프: {data['timestamp']:.3f}", end="")
                    
                    # 5개 메시지마다 상세 정보 표시
                    if message_count % 5 == 0:
                        print("\n" + "-" * 80)
                        print(f"{'모터':<20} | {'위치':>10} | {'전류(mA)':>10} | {'부하':>10}")
                        print("-" * 80)
                        
                        for motor_name, motor_data in data['motors'].items():
                            print(f"{motor_name:<20} | "
                                  f"{motor_data['position']:>10.2f} | "
                                  f"{motor_data['current']:>10.1f} | "
                                  f"{motor_data['load']:>10.1f}")
                        print()
                
                except asyncio.TimeoutError:
                    print("\n⚠ 데이터 수신 타임아웃 (1초)")
                    continue
                except json.JSONDecodeError as e:
                    print(f"\n✗ JSON 파싱 오류: {e}")
                    continue
            
            print(f"\n\n테스트 완료!")
            print(f"총 {message_count}개 메시지 수신")
            print(f"평균 수신율: {message_count / duration:.1f} msg/s")
            
    except websockets.exceptions.WebSocketException as e:
        print(f"\n✗ WebSocket 오류: {e}")
        print("\n서버가 실행 중인지 확인하세요:")
        print("  python3 robot_data_stream_server.py --port /dev/ttyACM0")
    except ConnectionRefusedError:
        print(f"\n✗ 연결 거부: {uri}")
        print("\n서버가 실행 중인지 확인하세요:")
        print("  python3 robot_data_stream_server.py --port /dev/ttyACM0")
    except KeyboardInterrupt:
        print("\n\n테스트 중단")


if __name__ == "__main__":
    # 명령줄 인자 처리
    server_uri = "ws://localhost:8765"
    test_duration = 10
    
    if len(sys.argv) > 1:
        server_uri = sys.argv[1]
    if len(sys.argv) > 2:
        test_duration = int(sys.argv[2])
    
    asyncio.run(test_client(server_uri, test_duration))

