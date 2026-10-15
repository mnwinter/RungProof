"""Regression for the zero-score/tie family guess that relabeled a door as a chute."""
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location(
    "registration", Path(__file__).resolve().parents[1] / "tools/register_training_asset_packages.py"
)
registration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(registration)


class FamilySelectionTests(unittest.TestCase):
    def test_unknown_chute_does_not_pick_alphabetically_first_door(self):
        with self.assertRaisesRegex(ValueError, "No model family"):
            registration.choose_family("powder discharge chute", [
                {"id": "access-control.door", "displayName": "Roller Shutter", "tags": []}
            ])

    def test_ambiguous_name_requires_a_choice(self):
        with self.assertRaisesRegex(ValueError, "Ambiguous"):
            registration.choose_family("motor", [
                {"id": "motor.a", "displayName": "AC Motor"},
                {"id": "motor.b", "displayName": "DC Motor"},
            ])

    def test_unique_supported_family_is_preserved(self):
        motor = {"id": "motor", "displayName": "AC induction motor"}
        self.assertIs(motor, registration.choose_family("induction motor", [
            {"id": "door", "displayName": "Roller Shutter"}, motor
        ]))


if __name__ == "__main__":
    unittest.main()
