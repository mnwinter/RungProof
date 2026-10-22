# Independent Lab 2.5 review

Reviewed one full native identity/layout frame (lab205-on-r2.png), then all eight r2 captures through lab205-evidence-compact.jpg. The derived manifest preserves source names, hashes and exact crop bounds. Compared pixels with the visible Signals values and current help; no UI input or backend behavior execution by this reviewer.

| Case | Actual selector_position | Actual bay_a_command | Actual bay_b_command | Physical lamps/runtime |
|---|---|---|---|---|
| lab205-off-r2 | 0 | False | False | Both dark / RUNNING |
| lab205-on-r2 | 1 | True | True | Both white / RUNNING |
| lab205-off-repeat-r2 | 0 | False | False | Both dark / RUNNING |
| lab205-on-repeat-r2 | 1 | True | True | Both white / RUNNING |
| lab205-app-stop-r2 | 1 | False | False | Both dark / STOPPED |
| lab205-app-run-r2 | 1 | True | True | Both white / RUNNING |
| lab205-app-reset-r2 | 0 | False | False | Both dark / STOPPED, scan0 |
| lab205-reset-run-r2 | 0 | False | False | Both dark / RUNNING |

This supports the physical selector cycling 0→1→0→1, both independent lamp bindings tracking their published command values, repeat operation, output removal on App Stop with selector retained at1, output restoration on Run, and active-selector reset to0 followed by a dark restart. It agrees with the documented two-position behavior. It does not establish arbitrary numeric inputs, independence under different command combinations, every save/load/keyboard/error path, or whole-app acceptance.

The visible off value is numeric0. The OCR's initial O character was an evidence-reading substitution; no app issue was observed. No new issue added and no repair plan selected.

Reusable outputs-only helper: compact_review.py takes an existing named capture plan, a new output filename, and physical ROI grounded in an inspected full frame. Default points/runtime ROIs match the current1600x900 panel layout and must be changed when the layout changes. It reads preserved PNGs, writes a sheet and source-hash/crop manifest, refuses overwrite, and makes no app calls or pass assertions. Example used here:

```powershell
python outputs/screen-audit-2026-10-21/compact_review.py outputs/screen-audit-2026-10-21/lab205-monitored-r2.json outputs/screen-audit-2026-10-21/lab205-evidence-compact.jpg --physical 660,305,980,490
```
