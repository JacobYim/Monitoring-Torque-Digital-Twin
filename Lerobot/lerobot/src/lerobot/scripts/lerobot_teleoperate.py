# Copyright 2024 The HuggingFace Inc. team. All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

"""
Simple script to control a robot from teleoperation.

Example:

```shell
lerobot-teleoperate \
    --robot.type=so101_follower \
    --robot.port=/dev/tty.usbmodem58760431541 \
    --robot.cameras="{ front: {type: opencv, index_or_path: 0, width: 1920, height: 1080, fps: 30}}" \
    --robot.id=black \
    --teleop.type=so101_leader \
    --teleop.port=/dev/tty.usbmodem58760431551 \
    --teleop.id=blue \
    --display_data=true
```

Example teleoperation with bimanual so100:

```shell
lerobot-teleoperate \
  --robot.type=bi_so100_follower \
  --robot.left_arm_port=/dev/tty.usbmodem5A460851411 \
  --robot.right_arm_port=/dev/tty.usbmodem5A460812391 \
  --robot.id=bimanual_follower \
  --robot.cameras='{
    left: {"type": "opencv", "index_or_path": 0, "width": 1920, "height": 1080, "fps": 30},
    top: {"type": "opencv", "index_or_path": 1, "width": 1920, "height": 1080, "fps": 30},
    right: {"type": "opencv", "index_or_path": 2, "width": 1920, "height": 1080, "fps": 30}
  }' \
  --teleop.type=bi_so100_leader \
  --teleop.left_arm_port=/dev/tty.usbmodem5A460828611 \
  --teleop.right_arm_port=/dev/tty.usbmodem5A460826981 \
  --teleop.id=bimanual_leader \
  --display_data=true
```

"""

import asyncio
import json
import logging
import threading
import time
from dataclasses import asdict, dataclass
from pprint import pformat
from typing import Any, Set

import rerun as rr
import websockets
from websockets.server import WebSocketServerProtocol

from lerobot.cameras.opencv.configuration_opencv import OpenCVCameraConfig  # noqa: F401
from lerobot.cameras.realsense.configuration_realsense import RealSenseCameraConfig  # noqa: F401
from lerobot.configs import parser
from lerobot.processor import (
    RobotAction,
    RobotObservation,
    RobotProcessorPipeline,
    make_default_processors,
)
from lerobot.robots import (  # noqa: F401
    Robot,
    RobotConfig,
    bi_so100_follower,
    hope_jr,
    koch_follower,
    make_robot_from_config,
    so100_follower,
    so101_follower,
)
from lerobot.teleoperators import (  # noqa: F401
    Teleoperator,
    TeleoperatorConfig,
    bi_so100_leader,
    gamepad,
    homunculus,
    koch_leader,
    make_teleoperator_from_config,
    so100_leader,
    so101_leader,
)
from lerobot.utils.import_utils import register_third_party_devices
from lerobot.utils.robot_utils import busy_wait
from lerobot.utils.utils import init_logging, move_cursor_up
from lerobot.utils.visualization_utils import init_rerun, log_rerun_data


@dataclass
class TeleoperateConfig:
    # TODO: pepijn, steven: if more robots require multiple teleoperators (like lekiwi) its good to make this possibele in teleop.py and record.py with List[Teleoperator]
    teleop: TeleoperatorConfig
    robot: RobotConfig
    # Limit the maximum frames per second.
    fps: int = 60
    teleop_time_s: float | None = None
    # Display all cameras on screen
    display_data: bool = False
    # Stream robot data via WebSocket for VR/external applications
    stream_data: bool = False
    stream_port: int = 8765
    stream_host: str = "0.0.0.0"


class WebSocketStreamer:
    """WebSocket server for streaming robot data to VR/external applications"""
    
    def __init__(self, host: str = "0.0.0.0", port: int = 8765):
        self.host = host
        self.port = port
        self.clients: Set[WebSocketServerProtocol] = set()
        self.loop = None
        self.server_task = None
        self.thread = None
        self.is_running = False
        
    async def handle_client(self, websocket: WebSocketServerProtocol):
        """Handle new WebSocket client connection"""
        client_addr = websocket.remote_address
        logging.info(f"WebSocket client connected: {client_addr}")
        self.clients.add(websocket)
        
        try:
            async for message in websocket:
                # Handle client messages (e.g., ping)
                try:
                    cmd = json.loads(message)
                    if cmd.get("type") == "ping":
                        await websocket.send(json.dumps({"type": "pong"}))
                except json.JSONDecodeError:
                    pass
        except websockets.exceptions.ConnectionClosed:
            logging.info(f"WebSocket client disconnected: {client_addr}")
        finally:
            self.clients.discard(websocket)
    
    async def broadcast_data(self, data: dict[str, Any]):
        """Broadcast data to all connected clients"""
        if not self.clients:
            return
        
        # Prepare JSON data (exclude camera images)
        stream_data = {
            "timestamp": time.time(),
            "motors": {}
        }
        
        for key, value in data.items():
            # Only include motor data (position, current, load)
            if key.endswith('.pos') or key.endswith('.current') or key.endswith('.load'):
                motor_name = key.rsplit('.', 1)[0]
                data_type = key.rsplit('.', 1)[1]
                
                if motor_name not in stream_data["motors"]:
                    stream_data["motors"][motor_name] = {}
                
                if data_type == 'pos':
                    stream_data["motors"][motor_name]["position"] = float(value)
                elif data_type == 'current':
                    stream_data["motors"][motor_name]["current"] = float(value)
                elif data_type == 'load':
                    stream_data["motors"][motor_name]["load"] = float(value)
        
        json_data = json.dumps(stream_data)
        
        # Send to all clients
        disconnected = set()
        for client in self.clients:
            try:
                await client.send(json_data)
            except websockets.exceptions.ConnectionClosed:
                disconnected.add(client)
        
        self.clients -= disconnected
    
    def broadcast_sync(self, data: dict[str, Any]):
        """Synchronous wrapper for broadcast_data"""
        if self.loop and self.is_running:
            asyncio.run_coroutine_threadsafe(self.broadcast_data(data), self.loop)
    
    async def start_server(self):
        """Start WebSocket server"""
        self.is_running = True
        logging.info(f"WebSocket streaming server started: ws://{self.host}:{self.port}")
        
        async with websockets.serve(self.handle_client, self.host, self.port):
            # Keep server running
            while self.is_running:
                await asyncio.sleep(0.1)
    
    def start(self):
        """Start WebSocket server in a separate thread"""
        def run_server():
            self.loop = asyncio.new_event_loop()
            asyncio.set_event_loop(self.loop)
            self.loop.run_until_complete(self.start_server())
        
        self.thread = threading.Thread(target=run_server, daemon=True)
        self.thread.start()
        time.sleep(0.5)  # Give server time to start
    
    def stop(self):
        """Stop WebSocket server"""
        self.is_running = False
        if self.loop:
            self.loop.call_soon_threadsafe(self.loop.stop)


def teleop_loop(
    teleop: Teleoperator,
    robot: Robot,
    fps: int,
    teleop_action_processor: RobotProcessorPipeline[tuple[RobotAction, RobotObservation], RobotAction],
    robot_action_processor: RobotProcessorPipeline[tuple[RobotAction, RobotObservation], RobotAction],
    robot_observation_processor: RobotProcessorPipeline[RobotObservation, RobotObservation],
    display_data: bool = False,
    duration: float | None = None,
    streamer: WebSocketStreamer | None = None,
):
    """
    This function continuously reads actions from a teleoperation device, processes them through optional
    pipelines, sends them to a robot, and optionally displays the robot's state. The loop runs at a
    specified frequency until a set duration is reached or it is manually interrupted.

    Args:
        teleop: The teleoperator device instance providing control actions.
        robot: The robot instance being controlled.
        fps: The target frequency for the control loop in frames per second.
        display_data: If True, fetches robot observations and displays them in the console and Rerun.
        duration: The maximum duration of the teleoperation loop in seconds. If None, the loop runs indefinitely.
        teleop_action_processor: An optional pipeline to process raw actions from the teleoperator.
        robot_action_processor: An optional pipeline to process actions before they are sent to the robot.
        robot_observation_processor: An optional pipeline to process raw observations from the robot.
    """

    display_len = max(len(key) for key in robot.action_features)
    start = time.perf_counter()

    while True:
        loop_start = time.perf_counter()

        # Get robot observation
        # Not really needed for now other than for visualization
        # teleop_action_processor can take None as an observation
        # given that it is the identity processor as default
        obs = robot.get_observation()
        
        # Stream robot data via WebSocket if enabled
        if streamer is not None:
            streamer.broadcast_sync(obs)

        # Get teleop action
        raw_action = teleop.get_action()

        # Process teleop action through pipeline
        teleop_action = teleop_action_processor((raw_action, obs))

        # Process action for robot through pipeline
        robot_action_to_send = robot_action_processor((teleop_action, obs))

        # Send processed action to robot (robot_action_processor.to_output should return dict[str, Any])
        _ = robot.send_action(robot_action_to_send)

        if display_data:
            # Process robot observation through pipeline
            obs_transition = robot_observation_processor(obs)

            log_rerun_data(
                observation=obs_transition,
                action=teleop_action,
            )

            # Prepare display data
            display_items = []
            
            # Add positions
            for key, value in obs.items():
                if key.endswith('.pos'):
                    display_items.append((key, value, 'POS'))
            
            # Add currents (torque)
            for key, value in obs.items():
                if key.endswith('.current'):
                    display_items.append((key, value, 'mA'))
            
            # Add loads (torque feedback)
            for key, value in obs.items():
                if key.endswith('.load'):
                    display_items.append((key, value, 'LOAD'))
            
            # Display table
            print("\n" + "-" * (display_len + 20))
            print(f"{'NAME':<{display_len}} | {'VALUE':>10} | {'UNIT':>5}")
            print("-" * (display_len + 20))
            for name, value, unit in display_items:
                if isinstance(value, (int, float)):
                    print(f"{name:<{display_len}} | {value:>10.2f} | {unit:>5}")
            
            move_cursor_up(len(display_items) + 7)

        dt_s = time.perf_counter() - loop_start
        busy_wait(1 / fps - dt_s)
        loop_s = time.perf_counter() - loop_start
        print(f"\ntime: {loop_s * 1e3:.2f}ms ({1 / loop_s:.0f} Hz)")

        if duration is not None and time.perf_counter() - start >= duration:
            return


@parser.wrap()
def teleoperate(cfg: TeleoperateConfig):
    init_logging()
    logging.info(pformat(asdict(cfg)))
    if cfg.display_data:
        init_rerun(session_name="teleoperation")

    # Initialize WebSocket streamer if enabled
    streamer = None
    if cfg.stream_data:
        streamer = WebSocketStreamer(host=cfg.stream_host, port=cfg.stream_port)
        streamer.start()
        logging.info(f"WebSocket streaming enabled: ws://{cfg.stream_host}:{cfg.stream_port}")

    teleop = make_teleoperator_from_config(cfg.teleop)
    robot = make_robot_from_config(cfg.robot)
    teleop_action_processor, robot_action_processor, robot_observation_processor = make_default_processors()

    teleop.connect()
    robot.connect()

    try:
        teleop_loop(
            teleop=teleop,
            robot=robot,
            fps=cfg.fps,
            display_data=cfg.display_data,
            duration=cfg.teleop_time_s,
            teleop_action_processor=teleop_action_processor,
            robot_action_processor=robot_action_processor,
            robot_observation_processor=robot_observation_processor,
            streamer=streamer,
        )
    except KeyboardInterrupt:
        pass
    finally:
        if cfg.display_data:
            rr.rerun_shutdown()
        if streamer:
            streamer.stop()
            logging.info("WebSocket streaming stopped")
        teleop.disconnect()
        robot.disconnect()


def main():
    register_third_party_devices()
    teleoperate()


if __name__ == "__main__":
    main()
