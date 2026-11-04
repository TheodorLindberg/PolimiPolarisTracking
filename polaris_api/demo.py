"""Send fake Polaris data to Unity in the same format as main.py (no tracker needed).

Tool 0 moves in a circle while spinning; tool 1 bobs up and down and
drops out of view for 1 second every 5 seconds.
"""
import json
import os
import math
import socket
import time

import numpy as np

UNITY_IP = "127.0.0.1"
UNITY_PORT = 5005

# Every packet is also appended (one JSON per line) to logs/ next to polaris_api/
LOG_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "logs")
os.makedirs(LOG_DIR, exist_ok=True)
LOG_PATH = os.path.join(LOG_DIR, time.strftime("demo_%Y%m%d_%H%M%S.jsonl"))

RATE_HZ = 60
SPEED = 0.2  # motion speed multiplier (1.0 = one circle every ~6 s)

# Mirror Z: right-handed -> left-handed (same as main.py)
FLIP_Z = np.diag([1.0, 1.0, -1.0, 1.0])


def to_left_handed(matrix):
    return FLIP_Z @ matrix @ FLIP_Z


def pose(x, y, z, yaw):
    """4x4 pose in Polaris frame (mm), rotated by `yaw` radians about Y."""
    c, s = math.cos(yaw), math.sin(yaw)
    return np.array([
        [c, 0, s, x],
        [0, 1, 0, y],
        [-s, 0, c, z],
        [0, 0, 0, 1],
    ])


sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
log_file = open(LOG_PATH, "w")
print(f"Logging to {os.path.normpath(LOG_PATH)}")
print(f"Sending demo data to {UNITY_IP}:{UNITY_PORT} (Ctrl+C to stop)")
start = time.time()

try:
    while True:
        t = time.time() - start

        # Polaris frame: tools sit around 1.5 m in front of the camera (negative Z)
        m = t * SPEED
        tool0 = pose(200 * math.cos(m), 200 * math.sin(m), -1500, m)
        tool1 = pose(-150, 100 * math.sin(2 * m), -1300, 0)
        tool1_visible = (t % 5) < 4

        tracking = [tool0, tool1 if tool1_visible else np.full((4, 4), np.nan)]

        tools = []
        for matrix in tracking:
            visible = not np.isnan(matrix).any()
            values = to_left_handed(matrix).flatten().tolist() if visible else [0.0] * 16
            tools.append({"visible": visible, "matrix": values})

        packet = json.dumps({"tools": tools})
        sock.sendto(packet.encode("utf-8"), (UNITY_IP, UNITY_PORT))
        log_file.write(packet + "\n")
        time.sleep(1 / RATE_HZ)

except KeyboardInterrupt:
    pass
finally:
    sock.close()
    log_file.close()
