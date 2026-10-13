"""Contract sync must never erase authored operating guidance."""
import unittest

import generate_help_documents as helpgen


class SceneHelpSyncTests(unittest.TestCase):
    def setUp(self):
        self.scene = {"id": "current", "simulation": {"points": [
            {"name": "feedback", "type": "BOOL", "owner": "PC", "initial": True},
            {"name": "command", "type": "STRING", "owner": "PLC", "initial": ""},
            {"name": "phase", "type": "INT", "owner": "SIM", "initial": 3}]}}
        self.table = "| Point | Type | Owner | Initial |\n| --- | --- | --- | --- |\n| obsolete | BOOL | PLC | False |\n"

    def sync(self, document):
        function = getattr(helpgen, "sync_scene_io", None)
        self.assertIsNotNone(function, "narrow sync_scene_io API is missing")
        return function(document, self.scene)

    def test_preserves_prose_and_replaces_only_table_and_identifier(self):
        before = "# Authored\n\nScene ID: `old`\n\n## Operation\n\nKeep this exact procedure.\n\n"
        after = "\nKeep this I/O note.\n\n## Sequence\n\nKeep this sequence.\n"
        result = self.sync(before + "## Symbolic I/O\n\n" + self.table + after)
        self.assertTrue(result.startswith(before.replace("`old`", "`current`")))
        self.assertTrue(result.endswith(after))
        self.assertIn("| `feedback` | `BOOL` | **PC** | `True` |", result)
        self.assertIn("| `command` | `STRING` | **PLC** | `` |", result)
        self.assertIn("| `phase` | `INT` | **SIM** | `3` |", result)
        self.assertNotIn("obsolete", result)
        self.assertEqual(self.sync(result), result)

    def test_adds_missing_contract_without_erasing_authored_document(self):
        authored = "# Custom\n\nWhole operating procedure.\n"
        result = self.sync(authored)
        self.assertIn("Whole operating procedure.\n", result)
        self.assertIn("Scene ID: `current`", result)
        self.assertIn("## Expected I/O to operate this scene", result)

    def test_rejects_duplicate_sections(self):
        with self.assertRaises(ValueError):
            self.sync("## Symbolic I/O\n" + self.table + "## Expected I/O to operate this scene\n" + self.table)

    def test_preserves_authored_extra_column_explanations(self):
        result = self.sync("## Symbolic I/O\n| Point | Type | Owner | Initial | Meaning |\n"
                           "| --- | --- | --- | --- | --- |\n"
                           "| `feedback` | BOOL | PC | False | Actual completion feedback |\n")
        self.assertIn("Actual completion feedback", result)
        self.assertIn("| `feedback` | `BOOL` | **PC** | `True` |", result)

    def test_rejects_malformed_table(self):
        with self.assertRaises(ValueError):
            self.sync("## Symbolic I/O\n| Point | Type |\n| broken | BOOL |\n")

    def test_rejects_unrelated_table_in_io_section(self):
        with self.assertRaises(ValueError):
            self.sync("## Symbolic I/O\n| Action | Target | Description | Note |\n"
                      "| --- | --- | --- | --- |\n| Start | motor | Begin | Hold |\n")

    def test_rejects_duplicate_scene_ids(self):
        with self.assertRaises(ValueError):
            self.sync("Scene ID: `a`\nScene ID: `b`\n")

    def test_empty_contract_replaces_old_table(self):
        self.scene["simulation"]["points"] = []
        result = self.sync("## Symbolic I/O\n" + self.table)
        self.assertIn("This migrated scene declares no symbolic I/O points.", result)
        self.assertNotIn("obsolete", result)


if __name__ == "__main__":
    unittest.main()
