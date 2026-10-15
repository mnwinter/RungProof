# Typed lesson implementation - 2026-10-08

All three previously unavailable behaviors now execute through editable ladder documents, normal Verify + Load, the offline controller and actual scene I/O. Scene adapters publish process feedback and animate committed commands.

## Behavior and native acceptance

| Lesson | Implemented behavior | Actual authored edit and restoration |
| --- | --- | --- |
| Drive alarm STRING | Ordinal whole-alphanumeric-token CODEMATCH processes received text, selected code and validity/Reset. F0030, prefixes/suffixes and lowercase do not match F003. Live readout displays the received text. | Tag-table F003 to F030, Apply and Verify + Load reversed F003/F030 results. Undo/reload restored F003. Stop cleared commands; Reset restored inputs and scan0. |
| Chicken weighing/printing | Product travel and 0.5 s settling publish 1.237 kg. Ladder FORMAT_TEXT formats weight; readiness gates 0.6 s printing, which captures immutable text. Print completion permits a separate 0.5 s application. Held requests do not duplicate jobs. Actual paper moves to the tray and carries black printed ink. | Template F3 to F2 and Verify + Load changed CHICKEN 1.237 kg to CHICKEN 1.24 kg, followed by actual application feedback. Undo/reload restored F3. Stop retained the printed job; Reset hid paper and restored home. Final native front/top inspection confirmed readable ink, no caption overlap and paper contact. |
| Motor ENUM | Actual retained Stopped/Running/Fault domain is changed by ladder MOV. Fault dominates Stop and fresh Start. A cleared fault remains latched until Reset; held Start cannot restart. Running/state-valid outputs derive from actual ENUM comparisons. Committed command rotates the shaft. | Network 5 MOV source Running to Stopped and Verify + Load made fresh Start remain Stopped/motor FALSE. Undo/reload restored Running and visible motion. Native fault/Stop/Reset priority checks passed. Stop cleared output/state; Reset restored home, inputs and scan0. |

Native sessions were closed after restoration. Native observations and edit steps were recorded during inspection; log files alone are not pixel evidence. Logs: `.tools/drive-string-native.log`, `.tools/chicken-string-native.log`, `.tools/motor-enum-native.log`, `.tools/chicken-final-ink-native.log`.

## Final verification

- Build: zero warnings/errors; `.tools/typed-final-build.log`.
- Controller: **159 passed, 0 failed**, `.tools/typed-core-tests.log`.
- Chicken process: **26 passed**, `.tools/chicken-label-plant-tests.log`.
- Actual editor/controller/scene integration: **33 checks, 0 failures**, `.tools/typed-typed-lessons.log`; normal compilation/binding, loaded scans, engine physics, actuator transforms and feedback.
- Editor regression: numeric-input-only ADD rejection preserves document and Undo; ENUM MOV and FORMAT_TEXT insertion compile. `numericDestinationGuard=True/True/True`, `.tools/typed-ladder-editor.log`.
- Virtual-controller UI, numeric scene I/O, ten gallery-clock checks and app shell pass. App shell loads **77 scenes/295 assets**, with one retained SYS-READY informational diagnostic. Logs `.tools/typed-virtual-controller.log`, `.tools/typed-numeric-scene-io.log`, `.tools/typed-gallery-clock.log`, `.tools/typed-app-shell.log`.
- Retained red evidence: initial type support 149 pass/1 fail (`typed-data-red.log`); malformed ENUM JSON 157/1 (`typed-json-red.log`); scene text validation 158/1 (`typed-scene-red.log`). Separate process/domain red runs were not recorded.
- JSON/scene declaration validation rejects malformed domains, invalid initial members and oversized text before execution. Existing SIM STRING points remain supported. Rejected sampled text leaves the text input image unchanged.

Final DLL SHA-256: `CDEF7CF0698D2F3E1EB273ED5FC82CABE3B8EAA92AE5E2D6474BF3E56F240998`.

The final native ink inspection used `CB3F0A1DFC4173C5C965B67AD143CF4F2F3005B96449719B43507CBE3D92453B`. Subsequent changes add the editor-verifier fixture's missing enabling contact and restore two original Unicode status-message arrows. Runtime/ink behavior is unchanged, and final engine/editor regressions pass. Earlier native authored edits occurred on earlier implementation builds; they are not represented as screenshots of the final DLL.

## Preservation and coverage

The original **107 unrelated untracked files are present with unchanged SHA-256 hashes**, checked against the original `.tools/final-review-staging-plan.json` values. Only the focused implementation/help/acceptance files are committed; no unrelated files are staged.

The ignored 77-scene and 26-former-gap JSON/CSV coverage records now attach fresh authored-controller/native proof only to these three rows, retaining previous row/checkpoint provenance. Current counts: nine supplied controllers, 67 empty PLC exercises, one additional blank SIM-only gallery; zero remaining functional gaps under this bounded reconciliation; 26/26 former-gap bounded records available. There are 11 unique native openings and eight semantic edit/reload checks across the combined pass. The other 74 records retain mixed historical scope, including 51 reused bounded records. Five-view evidence remains reused. This is not a fresh full-game or 77-scene multiangle acceptance claim.

## Reproduce and limits

Open a lesson in the normal workbench, inspect the provided editable ladder, select Verify + Load and Run. Drive: select F003, enable message validity, cycle messages and observe only the complete token matching. Chicken: load product, wait for stable 1.237 kg, enable valid data and printer readiness, then observe print/application completion. Motor: Start, Stop, fault, clear fault and Reset; a fresh Start edge is required after Stop/fault. Apply the edit examples above, Verify + Load, observe changed outputs, Undo/reload and Reset.

STRING is bounded to 255 UTF-16 code units without truncation; ENUM has 1-32 distinct identifiers. FORMAT_TEXT accepts exactly one invariant {0:F0} through {0:F6} placeholder; invalid/oversized results diagnose and clear the destination to prevent stale printing. These are local semantics, without Siemens ABI/TIA export/vendor instruction parity or live transport proof. Weighing uses a deterministic fixture, and paper transfer/contact is illustrative. Native paper layout was inspected for default F3 and edited F2 labels; arbitrary maximum-length labels on the fixed paper are not layout-validated. No physical machine or safety commissioning claim is made.
