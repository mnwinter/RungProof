# TIA parity suite runner

`tools/Invoke-TiaParitySuite.ps1` runs the existing local RungProof verification
commands in bounded profiles and writes one JSON result plus one log per
command. It is an orchestration tool; it does not replace the evidence registry
or turn local results into Siemens parity claims.

## Usage

Run from the repository root:

```powershell
.\tools\Invoke-TiaParitySuite.ps1 fast
.\tools\Invoke-TiaParitySuite.ps1 interaction -OutputPath .tools\tia-interaction
.\tools\Invoke-TiaParitySuite.ps1 visual -OutputPath C:\evidence\tia-visual.json
.\tools\Invoke-TiaParitySuite.ps1 full -CommandTimeoutSeconds 900
```

When `-OutputPath` is a directory, the runner creates a timestamped JSON file in
that directory. When it ends in `.json`, that exact file is used. The default is
`.tools/tia-parity-suite/`. Logs and visual artifacts are placed in sibling
directories named from the result file.

The process exits `0` only when every command in the selected profile passes and
every declared artifact exists. It exits `1` for a failed, timed-out, or skipped
command. A failed build causes dependent commands to be recorded as `skipped`
instead of running against stale binaries.

## Profiles

| Profile | Existing commands executed | Renderer |
| --- | --- | --- |
| `fast` | Bundled `.NET` build; controller/editor regression suite, including the evidence-matrix inventory checks | No Godot renderer |
| `interaction` | `fast` plus `--verify-editor-transactions`, `--verify-ladder-editor`, `--verify-call-authoring`, `--verify-operand-placement`, `--verify-project-tree-pointer`, `--verify-tia-empty-box`, `--verify-tia-monitor-sweep`, `--verify-tia-edit-sweep`, `--verify-tia-renderer-sweep`, and `--verify-editor-view-state` | Headless except operand placement, project-tree pointer input, the all-key edit and renderer sweeps, and cross-vendor editor view-state; those use the normal Vulkan renderer |
| `visual` | Bundled `.NET` build plus the all-key renderer/contact-sheet sweep and one bounded `--editor-zoom-review` run | Normal Vulkan; the 900-case catalog sweep and 174 declared review artifacts use 1600x1080, while the nested zoom review uses 1600x900 |
| `full` | Deduplicated union of `fast`, `interaction`, and `visual` | Mixed as listed above |

The visual profile exercises the complete 50-key local renderer catalog in both
selection states, at three zoom levels and three operand/tag display modes. It
also emits 150 selected-state detail images, 24 contact sheets, and a manifest,
then runs the separate bounded nested zoom review. Generation remains distinct
from human visual acceptance; the source-tree review record carries that verdict.

## JSON result contract

The result uses `schemaVersion: 1` and `resultKind:
"tia-parity-suite-run"`. Each command records:

- stable command ID and display name;
- execution status: `passed`, `failed`, or `skipped`;
- exit code and elapsed milliseconds;
- start/completion UTC timestamps;
- renderer, executable, and exact argument list;
- log path and declared artifact paths/existence;
- `gateIds`, reserved for explicit links to stable IDs in
  `docs/tia/evidence-registry.json`.

Only the exact catalog commands attach gate IDs. The renderer command resolves
to 100 stable `offline` and `selected` Siemens gate IDs; the edit and monitor
commands each resolve to their 50 stable state IDs. All other broad suite commands leave
`gateIds` empty because one successful command must not be translated
automatically into every catalog row passing. Registry
`localStatus` and `vendorStatus` remain evidence-review decisions, not runner
exit statuses.

The root result also states:

```json
{
  "offlineOnly": true,
  "installedTiaExecuted": false,
  "livePlcExecuted": false
}
```

These fields are fixed boundaries, not configurable shortcuts.

## What this runner does not prove

Passing any profile proves only that the listed local commands passed in that
run. It does not provide:

- matched captures from an installed TIA Portal version;
- TIA project import/export compatibility;
- OS-level manual operator-input evidence;
- live S7-1500 execution, commissioning, or equipment safety evidence;
- behavior for Siemens features documented as unsupported by RungProof.

Use the evidence registry and instruction matrix to determine which local,
vendor-reference, and external gates remain open.
