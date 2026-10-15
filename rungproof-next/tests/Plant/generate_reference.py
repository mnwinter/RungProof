"""Generate offline reference traces from the pinned canonical plant model."""
import hashlib
import json
from pathlib import Path
import sys

project = Path(__file__).resolve().parents[3]
source = project / "vendor" / "siemens-plc-pc-interface"
sys.path.insert(0, str(source))
from siemens_plc_pc_interface.components import (  # noqa: E402
    ConveyorInputs, ConveyorPhotoeye, ConveyorPhotoeyeConfig,
    ConveyorPusher, ConveyorPusherConfig, ConveyorPusherInputs,
)

cases = []
for name, pusher, steps in [
    ("conveyor stop at photoeye", False, [(0.1, True, False)] * 10 + [(0.1, False, False)] * 3),
    ("conveyor discharge counted once", False, [(0.1, True, False)] * 30),
    ("swept photoeye survives large step", False, [(2.8, True, False), (0.04, False, False), (0.07, False, False)]),
    ("photoeye hold decays after passing", False, [(1.5, True, False), (0.04, False, False), (0.07, False, False)]),
    ("pusher transfer and return", True, [(0.1, True, False)] * 10 + [(0.1, False, True)] * 4 + [(0.1, False, False)] * 4),
    ("partial stroke returns without transfer", True, [(0.1, True, False)] * 10 + [(0.1, False, True), (0.1, False, False)]),
    ("extending away from photoeye cannot transfer", True, [(0.4, False, True), (0.4, False, False)]),
    ("stopped zero time preserves plant", True, [(0, False, False), (0, True, True)]),
]:
    conveyor = ConveyorPhotoeyeConfig(1, 0.5, 0.2, 0.5, 0.1)
    model = ConveyorPusher(ConveyorPusherConfig(conveyor, 0.3, 0.8)) if pusher else ConveyorPhotoeye(conveyor)
    model.load_object()
    frames = []
    for seconds, run, extend in steps:
        snapshot = model.step(seconds, ConveyorPusherInputs(run, extend) if pusher else ConveyorInputs(run))
        frames.append({"seconds": seconds, "run": run, "extend": extend,
                       "leadingEdge": snapshot.object_leading_edge_m, "photoeye": snapshot.photoeye_blocked,
                       "completed": snapshot.completed_count, "state": snapshot.state.value,
                       "stroke": snapshot.pusher_position if pusher else 0,
                       "extended": snapshot.pusher_extended if pusher else False,
                       "retracted": snapshot.pusher_retracted if pusher else True,
                       "transferred": snapshot.object_transferred if pusher else False})
    cases.append({"name": name, "hasPusher": pusher, "frames": frames})

document = {"source": "rungproof/vendor/siemens-plc-pc-interface/siemens_plc_pc_interface/components.py",
            "sourceSha256": hashlib.sha256((source / "siemens_plc_pc_interface" / "components.py").read_bytes()).hexdigest(),
            "cases": cases}
Path(__file__).with_name("python-model-reference.json").write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
print(f"Generated {len(cases)} offline model cases; no transport created.")
