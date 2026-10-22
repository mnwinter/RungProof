# Current remediation build and evidence

## Current artifact and live transport

B3 is the current running build, root-confirmed. DLL SHA256:
`f03ba27cd5c765b67f47700cb9b2bd81c107b0237f0584715c07e8ba296f7cc9`.
GUI5916, console44048, app exec86449; bridgev9PID36292/exec86754 at the same51946 URL/token. Old968/1772/44040 were stopped exactly. [Saved B3 health](remediation-b3-live-health.json) and [current identity](current-built-app-identity.json) record provenance; active work is in [SESSION-STATE](SESSION-STATE.json).

B3 includes the new door, jug and drill source. CNC and parcel changes are ahead of B3 and are **not running**. Do not transfer source-level expectations onto this process. Current root work: door loader76774 active; no B3 door/jug/drill screen closure yet.

## Current checks

| Check | Result | Evidence / boundary |
|---|---|---|
| B3 build | PASS,0warnings/0errors | [Build log](remediation-build-b3.log); build is not screen behavior proof. |
| B3 controller suite |418PASS/2FAIL | [Test log](remediation-controller-tests-b3.log). Remaining failures are watch-schema Expected9/actual10 and TIA local artifact hash. No full-suitePASS. |
| PLC boundary | No real transport constructed or connection attempted | Same controller log; no live-PLC evidence. |
| Native new plant retests | Pending B3 | Door/jug/drill implemented/built; native B3 evidence must be reviewed before closure. |
| Source ahead of build | CNC/parcel | Requires coordinated rebuild before native testing; no runtime credit. |

Confirmed pinned-tool commands, working directory `rungproof-next/`:

```powershell
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore
& '.tools/dotnet/dotnet.exe' run --project tests/RungProof.Next.VirtualController.Tests.csproj --no-restore
```

Earlier Godot commands remain historical check evidence, not a claim they were rerun for B3:

```powershell
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-app-shell
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-ladder-editor
```

[Historical app-shell log](remediation-app-shell.log) shows targeted AUDIT_DIAGNOSTICS_VERIFY PASS for stale-error clearing and invalid-error retention, but overall APP_SHELL_VERIFY FAIL for saved Demo4/menu mismatch. [Historical ladder-editor verifier](remediation-editor-verifier.log) fails watchTable preflight. Exact native authoring reproduction for QA-UI-002 remains pending. Earlier controller runs410/2 and414/2 are superseded by B3's418/2 for current automated status; their logs remain preserved.

## Verified scope and unresolved work

Native reviews establish bounded fume, original common-stop, repaired shipped9.3 reference, both tank routes/scaling,2.11 tote and2.12 lift closures. Visionclass2/3/4 receiving routes are independently reviewed, with class1 documented earlier. Documentation inventories/pulse procedure are source-verified; rendered-help behavior is not required for those wording defects. See the [current plan](../../docs/SCREEN_AUDIT_REMEDIATION_PLAN.md), [finding masters](provisional-issues.md) and [77-scene index](scene-issue-index.md) for exact scopes and original failures.

QA-PLANT-001 now has five native-confirmed instances2.11/12/13/16/19:2.11/12 bounded fixes verified; jug/drill/door B3 retests pending.2.23/24 remain source-only candidates.2.13 target-load provenance failure is separately resolved in native loading; it did not prove a Discard callback defect. Keep the earlier weak navigation candidate separate.

All77 scene normal routes have dispositions; every overall remainsPARTIAL. Remaining legacy process paths, exact native diagnostic reproduction and independent case gaps prevent full-goal completion. QA click misses, wrong expectations, unreadable filenames and failed transport inputs never become app defects or test passes. Preserve unrelated dirty work; no commit, publication, livePLC action or acceptance claim is added by this report.
