"""Read Polaris tool poses and send them to Unity over UDP.

Each pose is converted from Polaris' right-handed frame to Unity's
left-handed frame by mirroring the Z axis. Units stay in millimetres.
"""
import json
import os
import socket
import time

import numpy as np
from sksurgerynditracker.nditracker import NDITracker

UNITY_IP = "127.0.0.1"
UNITY_PORT = 5005

# Every packet is also appended (one JSON per line) to logs/ next to polaris_api/
LOG_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "logs")
os.makedirs(LOG_DIR, exist_ok=True)
LOG_PATH = os.path.join(LOG_DIR, time.strftime("polaris_%Y%m%d_%H%M%S.jsonl"))

ROMFILES = [
    "./sroms/8700339.rom",
    "./sroms/8700340.rom",
]

# Mirror Z: right-handed -> left-handed
FLIP_Z = np.diag([1.0, 1.0, -1.0, 1.0])


def to_left_handed(matrix):
    return FLIP_Z @ matrix @ FLIP_Z


sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
log_file = open(LOG_PATH, "w")
print(f"Logging to {os.path.normpath(LOG_PATH)}")
tracker = NDITracker({"tracker type": "polaris", "romfiles": ROMFILES})

try:
    tracker.start_tracking()
    print(f"Sending to {UNITY_IP}:{UNITY_PORT} (Ctrl+C to stop)")

    while True:
        port_handles, timestamps, frame_numbers, tracking, quality = tracker.get_frame()

        tools = []
        for matrix in tracking:
            matrix = np.asarray(matrix)
            visible = not np.isnan(matrix).any()
            # Row-major 16 floats; zeros when the tool is not visible
            values = to_left_handed(matrix).flatten().tolist() if visible else [0.0] * 16
            tools.append({"visible": visible, "matrix": values})

        packet = json.dumps({"tools": tools})
        sock.sendto(packet.encode("utf-8"), (UNITY_IP, UNITY_PORT))
        log_file.write(packet + "\n")

except KeyboardInterrupt:
    pass
finally:
    tracker.stop_tracking()
    tracker.close()
    sock.close()
    log_file.close()
