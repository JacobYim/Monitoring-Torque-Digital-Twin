#!/usr/bin/env python3
"""
SO-101 Follower 로봇의 각 관절 서보모터 토크 간단 모니터링
실행 중인 lerobot-teleoperate와 별개로 다른 포트에서 작동합니다
"""

import time
import sys
from lerobot.motors.dynamixel.dynamixel import DynamixelMotorsBus
from lerobot.robots.configs import SO101_FOLLOWER_MOTORS, SO101_FOLLOWER_CALIBRATION

def main():
    # 포트 선택 (기본값: /dev/ttyACM0, teleoperate가 사용 중이면 변경 필요)
    port = "/dev/ttyACM0" if len(sys.argv) < 2 else sys.argv[1]
    
    print("=" * 100)
    print("SO-101 Follower 서보모터 토크 실시간 모니터링")
    print("=" * 100)
    print(f"\n포트: {port}")
    print("주의: 이 스크립트는 teleoperate와 동시에 같은 포트를 사용할 수 없습니다.")
    print("teleoperate를 먼저 중지하거나 다른 포트를 사용하세요.\n")
    
    # SO-101 Follower 버스 연결
    bus = DynamixelMotorsBus(
        port=port,
        motors=SO101_FOLLOWER_MOTORS,
        calibration=SO101_FOLLOWER_CALIBRATION,
    )
    
    try:
        print("로봇 연결 중...")
        bus.connect()
        print("✓ 연결 성공!\n")
        
        # 모터 이름 가져오기
        motor_names = list(bus.motors.keys())
        print(f"감지된 모터 ({len(motor_names)}개): {', '.join(motor_names)}\n")
        print("=" * 100)
        print("실시간 토크 모니터링 (Ctrl+C로 종료)")
        print("=" * 100)
        print("\nPresent_Current: 현재 전류 [mA] - 토크와 비례")
        print("Present_PWM: 현재 PWM 출력 [%] - 모터 출력")
        print("Present_Position: 현재 위치 [normalized]\n")
        
        # 헤더 출력
        print(f"{'시간':^10} | " + " | ".join([f"{m:^15}" for m in motor_names]))
        print("-" * (15 + 18 * len(motor_names)))
        
        # 실시간 모니터링 루프
        iteration = 0
        while True:
            try:
                # 현재 전류 읽기 (토크와 직접 관련)
                currents = bus.sync_read("Present_Current")
                
                # 현재 시간
                current_time = time.strftime("%H:%M:%S")
                
                # 데이터 행 출력
                if iteration % 3 == 0:  # 전류
                    line = f"{current_time:^10} | "
                    for motor in motor_names:
                        current_ma = currents.get(motor, 0)
                        line += f"{current_ma:>6.0f}mA [{motor[:4]}] | "
                    print(line)
                elif iteration % 3 == 1:  # PWM
                    pwms = bus.sync_read("Present_PWM")
                    line = f"{'PWM':^10} | "
                    for motor in motor_names:
                        pwm_val = pwms.get(motor, 0) * 100  # PWM을 %로 변환
                        line += f"{pwm_val:>6.1f}% [{motor[:4]}] | "
                    print(line)
                else:  # 위치
                    positions = bus.sync_read("Present_Position")
                    line = f"{'위치':^10} | "
                    for motor in motor_names:
                        pos_val = positions.get(motor, 0)
                        line += f"{pos_val:>7.3f} [{motor[:4]}] | "
                    print(line)
                    print()  # 빈 줄 추가
                
                iteration += 1
                time.sleep(0.3)  # 300ms마다 업데이트
                
            except KeyboardInterrupt:
                print("\n\n모니터링 종료")
                break
            except Exception as e:
                print(f"\n읽기 오류: {e}")
                time.sleep(0.5)
                
    except Exception as e:
        print(f"\n오류 발생: {e}")
        print("\n문제 해결 방법:")
        print("1. 로봇이 연결되어 있는지 확인")
        print("   lerobot-find-port 명령어로 포트 확인")
        print("2. 다른 프로그램(lerobot-teleoperate)이 포트를 사용 중이지 않은지 확인")
        print("   pkill -f lerobot-teleoperate")
        print("3. 로봇이 캘리브레이션되어 있는지 확인")
        print("4. 올바른 포트를 사용하고 있는지 확인")
        print(f"   현재 포트: {port}")
        
    finally:
        if bus.is_connected:
            print("\n로봇 연결 해제 중...")
            bus.disconnect()
            print("✓ 연결 해제 완료")

if __name__ == "__main__":
    main()

