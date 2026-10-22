# Lab2.11 START-binding diagnostic probe

**Diagnostic input only — not a normal solution, reference sequence, or runtime PASS.** No app source or scene declaration is changed. Preparation only serializes JSON; no loading, compilation, controller execution, UI input, or backend behavior test was performed.

Project: `probe-lab-2-11-start-binding.rpproj.json` (schema9). Exact scene: `lab-2-11-inbound-tote-stop`.

The three declared PC/PLC SceneIO points are retained with their exact names, roles, types, bindings and false initial values: PLC `conveyor_run`; PC `tote_at_scanner`, `scan_complete`. Internal SIM `cell_color` and `cycle_complete` are deliberately not PLC bindings, matching SceneLadderProject.EnsureSceneIoTable. One additional unbound false BOOL Memory, `qa_probe_false`, feeds a normally-open contact and ordinary assignment coil `conveyor_run`. No rung can set that memory true. No timer, sequencer, instance DB, invented PC START point, operator.start alias, controllerStartBinding or forced output is added.

## Visible UI diagnostic

1. Select the matching Lab2.11 scene. Preserve unrelated wanted drafts through normal Save; only discard the audit's disposable edits.
2. Use the normal File > Open Ladder Agent Project route. In its file dialog set the folder Path to this test-projects directory and File to the leaf probe filename, then Open.
3. Inspect the single NO `qa_probe_false` -> ordinary `conveyor_run` coil network and SceneIO table. Use visible Online > Verify + load offline, then Run. A JSON file existing is not proof the UI accepts or executes it.
4. Confirm actual local RUNNING status/scan progression, `qa_probe_false=FALSE` and `conveyor_run=FALSE`; transport remains disconnected. If verification or loading fails, log that observed failure and stop this diagnostic. Do not call the probe valid from preparation alone.
5. Click the scene's declared START / Start tote cycle control once. Capture the exact resulting workspace status/message, controller state and conveyor state. The source action is type=start with sequence=scan-cycle but no controllerStartBinding; Main's normal action routing requires that field even with a valid running controller. The live test is intended to establish whether this specific action is blocked for that reason.
6. No movement is expected from this deliberately false command rung. A stationary conveyor alone does not prove a missing binding. The required evidence is the visible action-block message while an independently valid controller is running. Record unrelated load, readiness or focus failure separately.
7. Stop/Reset using the normal UI and leave the diagnostic project clearly identified as disposable. It must never be presented as the complete tote cycle.

Format basis: the existing disposable schema9 Lab2.3 project, conventional OrganizationBlock/Continuous task and recursive Series/Contact/Assign serialization. Main.cs~5257–5283 owns the normal START binding check. SceneLadderProject.cs~56–80 excludes SIM-only points. Project/node/task/watch/symbol IDs are probe-specific; instances are empty.
