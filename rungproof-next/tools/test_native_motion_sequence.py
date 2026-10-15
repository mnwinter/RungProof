"""Recording-integrity regression tests; synthetic PNGs are never visual evidence."""
import json
from pathlib import Path
import tempfile
import unittest
from PIL import Image
from tools.capture_native_motion_review import validate_sequence


class SequenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.records = [{"kind": "setup"},
                        {"kind": "frame", "reviewFrame": 0, "renderedFrame": 0},
                        {"kind": "frame", "reviewFrame": 1, "renderedFrame": 1},
                        {"kind": "end", "completed": True}]
        self.write_trace()
        (self.directory / "capture.log").write_text("3 frames at 50 FPS\n")
        for index in range(3):
            Image.new("RGB", (2400, 1350), (index, 0, 0)).save(self.directory / f"frame{index:08d}.png")

    def write_trace(self):
        (self.directory / "trace.jsonl").write_text("\n".join(json.dumps(row) for row in self.records))

    def test_valid_recording_remains_unaccepted(self):
        result = validate_sequence(self.directory)
        self.assertFalse(result["accepted"])
        self.assertEqual((result["count"], result["traceCount"]), (3, 2))
        self.assertEqual(result["finalUntracedFrame"], "frame00000002.png")
        self.assertEqual(len({row["sha256"] for row in result["frames"]}), 3)

    def test_trace_gap_rejected(self):
        self.records[2]["renderedFrame"] = 2
        self.write_trace()
        with self.assertRaisesRegex(ValueError, "consecutive"):
            validate_sequence(self.directory)

    def test_missing_png_rejected(self):
        (self.directory / "frame00000001.png").unlink()
        with self.assertRaisesRegex(ValueError, "every traced frame"):
            validate_sequence(self.directory)

    def test_wrong_dimensions_rejected(self):
        Image.new("RGB", (1600, 900)).save(self.directory / "frame00000001.png")
        with self.assertRaisesRegex(ValueError, "dimensions"):
            validate_sequence(self.directory)

    def test_runtime_error_rejected(self):
        (self.directory / "capture.log").write_text("ERROR: callback failed\n3 frames at 50 FPS\n")
        with self.assertRaisesRegex(ValueError, "runtime error"):
            validate_sequence(self.directory)

    def test_movie_count_mismatch_rejected(self):
        (self.directory / "capture.log").write_text("2 frames at 50 FPS\n")
        with self.assertRaisesRegex(ValueError, "frame count"):
            validate_sequence(self.directory)

    def test_incomplete_trace_rejected(self):
        self.records[-1]["completed"] = False
        self.write_trace()
        with self.assertRaisesRegex(ValueError, "completion"):
            validate_sequence(self.directory)


if __name__ == "__main__":
    unittest.main()
