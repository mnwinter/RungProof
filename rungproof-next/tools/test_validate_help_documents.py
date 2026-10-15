"""Regression checks for complete help-audit reporting and failure exit codes."""
import contextlib
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import validate_help_documents as audit


class HelpAuditTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="rungproof-help-audit-")
        self.root = Path(self.temporary.name).resolve()
        # Verify the absolute cleanup target before registering recursive cleanup.
        if self.root.parent != Path(tempfile.gettempdir()).resolve():
            raise RuntimeError("Unexpected fixture cleanup path")
        self.addCleanup(self.temporary.cleanup)
        self.root_patch = patch.object(audit, "ROOT", self.root)
        self.help_patch = patch.object(audit, "HELP", self.root / "docs/help")
        self.root_patch.start()
        self.help_patch.start()
        self.addCleanup(self.root_patch.stop)
        self.addCleanup(self.help_patch.stop)
        self.assets = [dict(id=name, signals=[], kinematics=[dict(nodePath=f"{name}-motion")])
                       for name in ("one", "two")]
        self.write("assets/catalog/production.catalog.json", json.dumps(dict(assets=self.assets)))
        self.write("assets/catalog/candidates.catalog.json", json.dumps(dict(assets=[])))
        self.write("scenes/catalog/original-scenes.catalog.json", json.dumps(dict(scenes=[])))
        self.write("docs/help/README.md", "# RungProof Next help index")
        for asset in self.assets:
            self.write_asset(asset, motion=True)

    def write(self, relative, content):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")

    def write_asset(self, asset, motion):
        content = (f"Asset ID: `{asset['id']}`\n## Industrial reference basis\n"
                   "No reusable external signals are declared. Do not invent I/O for this passive asset.")
        if motion:
            content += f"\n`{asset['kinematics'][0]['nodePath']}`"
        self.write(f"docs/help/assets/{asset['id']}.md", content)

    def run_audit(self):
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            exit_code = audit.main()
        return exit_code, output.getvalue()

    def test_reports_both_conflicts_and_recovers_to_valid_on_next_run(self):
        for asset in self.assets:
            self.write_asset(asset, motion=False)
        exit_code, output = self.run_audit()
        self.assertEqual(exit_code, 1)
        self.assertIn("one-motion", output)
        self.assertIn("two-motion", output)
        self.assertIn("issues=2 assets=2 scenes=0", output)
        for asset in self.assets:
            self.write_asset(asset, motion=True)
        exit_code, output = self.run_audit()
        self.assertEqual(exit_code, 0)
        self.assertIn("HELP_DOCUMENTS_VALID", output)
        self.assertNotIn("HELP_DOCUMENTS_ISSUE", output)

    def test_missing_file_is_reported_once_and_other_assets_are_still_checked(self):
        (self.root / "docs/help/assets/one.md").rename(self.root / "docs/help/assets/one.absent")
        self.write_asset(self.assets[1], motion=False)
        exit_code, output = self.run_audit()
        self.assertEqual(exit_code, 1)
        self.assertEqual(output.count("missing help document:"), 1)
        self.assertIn("two-motion", output)
        self.assertIn("issues=2", output)


if __name__ == "__main__":
    unittest.main()
