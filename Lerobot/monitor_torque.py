#!/usr/bin/env python3
"""
SO-101 Follower 로봇의 각 관절 서보모터 토크 모니터링 스크립트
Present_Current를 읽어서 실시간으로 표시합니다.
"""

import time
from lerobot.motors.dynamixel.dynamixel import DynamixelMotorsBus
from lerobot.robots.configs import SO101_FOLLOWER_MOTORS, SO101_FOLLOWER_CALIBRATION

def main():
    print("=" * 80)
    print("SO-101 Follower 서보모터 토크 모니터링")
    print("=" * 80)
    
    # SO-101 Follower 버스 연결
    bus = DynamixelMotorsBus(
        port="/dev/ttyACM0",
        motors=SO101_FOLLOWER_MOTORS,
        calibration=SO101_FOLLOWER_CALIBRATION,
    )
    
    try:
        print("\n로봇 연결 중...")
        bus.connect()
        print("✓ 연결 성공!")
        
        # 모터 이름 가져오기
        motor_names = list(bus.motors.keys())
        print(f"\n감지된 모터: {', '.join(motor_names)}")
        print("\n" + "=" * 80)
        print("실시간 토크 모니터링 (Ctrl+C로 종료)")
        print("=" * 80)
        print("\nPresent_Current 단위: mA (밀리암페어)")
        print("Present_PWM 단위: % (0-100%)\n")
        
        # 헤더 출력
        header = "| Time  |"
        for motor in motor_names:
            header += f" {motor:>12} |"
        print(header)
        print("-" * len(header))
        
        # 실시간 모니터링 루프
        while True:
            try:
                # 현재 전류 읽기 (토크와 직접 관련)
                currents = bus.sync_read("Present_Current")
                
                # PWM 값도 함께 읽기
                pwms = bus.sync_read("Present_PWM")
                
                # 현재 시간
                current_time = time.strftime("%H:%M:%S")
                
                # 전류 값 출력
                line = f"| {current_time} |"
                for motor in motor_names:
                    current_ma = currents.get(motor, 0)
                    line += f" {current_ma:>10.1f}mA |"
                print(line)
                
                # PWM 값 출력 (선택적)
                # pwm_line = f"| PWM    |"
                # for motor in motor_names:
                #     pwm_val = pwms.get(motor, 0)
                #     pwm_line += f" {pwm_val:>10.1f}% |"
                # print(pwm_line)
                
                time.sleep(0.1)  # 100ms마다 업데이트
                
            except KeyboardInterrupt:
                print("\n\n모니터링 종료")
                break
            except Exception as e:
                print(f"\n읽기 오류: {e}")
                time.sleep(0.5)
                
    except Exception as e:
        print(f"\n오류 발생: {e}")
        print("\n문제 해결 방법:")
        print("1. 로봇이 /dev/ttyACM0에 연결되어 있는지 확인")
        print("2. 로봇이 캘리브레이션되어 있는지 확인")
        print("3. 다른 프로그램에서 포트를 사용하고 있지 않은지 확인")
        
    finally:
        if bus.is_connected:
            print("\n로봇 연결 해제 중...")
            bus.disconnect()
            print("✓ 연결 해제 완료")

if __name__ == "__main__":
    main()

