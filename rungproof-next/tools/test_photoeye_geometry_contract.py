"""Keep beam installation validation consistent with the scene compositor."""
import json
from pathlib import Path
import unittest

from verify_scene_contracts import verify_all_photoeye_geometry, verify_photoeye_product_envelope


class PhotoeyeGeometryContractTests(unittest.TestCase):
    def scene(self, height):
        return {"equipment": [{"id": "beam", "type": "photoeye",
                               "config": {"beamCenterHeightM": height}}]}

    def test_accepts_authored_upper_chain_lift_sensor(self):
        root = Path(__file__).resolve().parents[1]
        scene = json.loads((root / "scenes/migrated/lab-4-09-chain-drive-lift.scene.json").read_text(encoding="utf-8-sig"))
        heights = [item["config"]["beamCenterHeightM"] for item in scene["equipment"]
                   if item["type"] == "photoeye"]
        self.assertIn(3.2, heights)
        verify_all_photoeye_geometry(scene["id"], scene)

    def test_accepts_compositor_minimum_and_normal_conveyor_height(self):
        for height in (0.3, 1.1, 3.2):
            with self.subTest(height=height):
                verify_all_photoeye_geometry("fixture", self.scene(height))

    def test_rejects_missing_non_numeric_non_finite_and_low_heights(self):
        for height in (None, True, False, "1.1", float("nan"), float("inf"),
                       -float("inf"), -1, 0, 0.299):
            with self.subTest(height=height):
                with self.assertRaises(ValueError):
                    verify_all_photoeye_geometry("fixture", self.scene(height))

    def inclined_scene(self):
        root = Path(__file__).resolve().parents[1]
        return json.loads((root / "scenes/migrated/scene-2-conveyor-pusher.scene.json").read_text(encoding="utf-8-sig"))

    def test_checks_inclined_beam_at_product_path_instead_of_lower_lens(self):
        scene = self.inclined_scene()
        verify_all_photoeye_geometry(scene["id"], scene)
        verify_photoeye_product_envelope(scene["id"], scene)

    def test_rejects_inclined_beam_that_misses_product_or_belt(self):
        for height in (0.4, 3.0):
            with self.subTest(height=height):
                scene = self.inclined_scene()
                sensor = next(item for item in scene["equipment"] if item["type"] == "photoeye")
                sensor["config"].update(beamCenterHeightM=height, positiveBeamCenterHeightM=height)
                with self.assertRaises(ValueError):
                    verify_photoeye_product_envelope(scene["id"], scene)

    def test_rejects_invalid_second_lens_and_unsupported_transform(self):
        scene = self.inclined_scene()
        sensor = next(item for item in scene["equipment"] if item["type"] == "photoeye")
        sensor["config"]["positiveBeamCenterHeightM"] = float("nan")
        with self.assertRaises(ValueError):
            verify_all_photoeye_geometry(scene["id"], scene)
        sensor["config"]["positiveBeamCenterHeightM"] = 2.4045945
        sensor["rotation"] = [0, 90, 0]
        with self.assertRaises(ValueError):
            verify_photoeye_product_envelope(scene["id"], scene)


if __name__ == "__main__":
    unittest.main()
