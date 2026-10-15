"""Line-oriented bridge for the existing guarded PLC runtime.

The Godot shell never imports or reimplements S7 transport. It starts this
process only when the operator selects External PLC, and exchanges JSON lines
with the existing tools.plc_live.LivePlcController.
"""

from __future__ import annotations

import json
from pathlib import Path
import sys
from typing import Any

from plc_live import LivePlcController
from plc_diagnostics import run_read_only_plc_test


def reply(payload: dict[str, Any]) -> None:
    sys.stdout.write(json.dumps(payload, separators=(",", ":")) + "\n")
    sys.stdout.flush()


def main() -> int:
    profile_dir = Path(sys.argv[1]).resolve()
    controller = LivePlcController(profile_dir)
    try:
        for line in sys.stdin:
            if not line.strip():
                continue
            request = json.loads(line)
            command = request.get("command")
            if command == "describe":
                reply({"ok": True, "descriptor": controller.describe_profile(request["profileId"])})
            elif command == "connect":
                reply({"ok": True, "result": controller.connect(
                    profile_id=request["profileId"],
                    scene_id=request["sceneId"],
                    execute=request.get("execute") is True,
                    authorized_write_scope=request["authorizedWriteScope"],
                )})
            elif command == "cycle":
                reply({"ok": True, "result": controller.cycle(
                    session_id=request["sessionId"],
                    scene_id=request["sceneId"],
                    pc_points=request["pcPoints"],
                )})
            elif command == "diagnostic":
                reply({"ok": True, "result": run_read_only_plc_test(
                    profile_dir,
                    request["profileId"],
                )})
            elif command == "disconnect":
                reply({"ok": True, "result": controller.disconnect(session_id=request.get("sessionId"))})
            elif command == "close":
                controller.close()
                reply({"ok": True})
                return 0
            else:
                raise ValueError(f"Unknown PLC bridge command: {command}")
    except Exception as exc:
        reply({"ok": False, "error": f"{type(exc).__name__}: {exc}"})
        controller.close()
        return 1
    finally:
        controller.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
