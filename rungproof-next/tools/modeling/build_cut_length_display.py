"""Compatibility entry point: build only the static cut-length readout."""
import importlib.util
import os
from pathlib import Path

root = Path(os.environ["RUNGPROOF_PROJECT_ROOT"])
spec = importlib.util.spec_from_file_location("static_readouts", root / "tools/modeling/build_static_training_readouts.py")
module = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(module)
module.build_readouts(root, ["cut_length_display"])
