"""Offline IPC adversary. Imports no PLC adapter and opens no network socket."""
import json
import os
import sys
import time
from pathlib import Path

# Optional file-controlled fixture for the native scene playback verifier.
# This remains a standalone offline process: no PLC modules or sockets.
playback_mode_file = Path(sys.argv[1]) if len(sys.argv) > 1 else None
cycle_number = 0

for line in sys.stdin:
    request = json.loads(line)
    command = request.get("command")
    if command == "hang":
        time.sleep(60)
    elif command == "malformed":
        print("not JSON", flush=True)
        continue
    elif command == "close":
        break
    elif command == "reject":
        print(json.dumps({"ok": False, "error": "offline rejection"}), flush=True)
        continue
    elif command == "flood":
        sys.stderr.write("x" * 1_000_000)
        sys.stderr.flush()
    elif command == "delay":
        time.sleep(request.get("seconds", 0.15))

    result = {"pid": os.getpid(), "value": request.get("value")}
    if command == "connect":
        if request["profileId"] == "typed-scope-fixture.json":
            # Opt-in wire inspection; existing offline/playback replies stay unchanged.
            result["observedAuthorizedWriteScope"] = request["authorizedWriteScope"]
            result["observedExpectedDescriptor"] = request["expectedDescriptor"]
        if playback_mode_file:
            result.update(request["expectedDescriptor"])
        result.update({"sessionId": "offline-session", "sceneId": request["sceneId"],
                       "id": request["profileId"], "connected": True,
                       "ip": "offline", "rack": 0, "slot": 1, "cycleMs": 20,
                       "heartbeatTimeoutMs": 120})
        if playback_mode_file:
            result["heartbeatTimeoutMs"] = request["expectedDescriptor"]["heartbeatTimeoutMs"]
    elif command == "cycle":
        cycle_number += 1
        mode = request["pcPoints"].get("mode")
        if mode == "hang":
            time.sleep(60)
        result.update({"sessionId": request["sessionId"], "sceneId": request["sceneId"],
                       "connected": mode != "fault", "health": "fault" if mode == "fault" else "healthy",
                       "cycle": 1, "heartbeat": {"healthy": True, "reason": "matching_echo",
                                                 "last_echo": 1, "age_ms": 0},
                       "plcStatus": {"simulation_enable": True, "simulation_comm_ok": True,
                                     "simulation_timeout": False}, "plcPoints": {"motor": True}})
        if mode == "stale":
            result["sessionId"] = "another-session"
        if mode == "bad_status":
            result["plcStatus"]["simulation_enable"] = "true"
        if playback_mode_file:
            playback_mode = playback_mode_file.read_text(encoding="utf-8").strip()
            result["cycle"] = cycle_number
            result["plcPoints"] = {"conveyor_running": playback_mode != "disabled"}
            result["plcStatus"]["simulation_enable"] = playback_mode != "disabled"
    print(json.dumps({"ok": True, "result": result}), flush=True)
