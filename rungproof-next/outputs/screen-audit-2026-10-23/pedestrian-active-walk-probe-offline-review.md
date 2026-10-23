# Q21 single active-WALK CROSS observer: offline review

Helper `pedestrian_active_walk_request_probe.py` is frozen at SHA256 `5db9bc4a4eaf679bc2e311b8dee9fb793e8bb42f5958dbe424c339b7673ef530`. This review validates the QA helper against saved pixels and synthetic bookkeeping, not the application's active-WALK behavior. The child agent made no bridge/UI calls.

Actual saved `pedestrian-r1-path-clear.png` parsed all eight public fields from the fixed native Signals rectangle (700,155,1020,305), enlarged 3x. PATH=True, vehicle_ready=True, pedestrian_stop=True, pedestrian_walk=False; the WALK predicate was correctly false. Runtime read RUNNING at scan 5593. Owned OCR worker exited 0. Evidence: `pedestrian-probe-offline-native-idle-1792737293137315300/result.json`.

The original control crop and its provenance were inspected. Root explicitly grounds the physical green CROSS at (441,381), distinct from PATH at (410,399). The saved native Health baseline has four records and no CROSS event. Health acknowledgement and actual physical lamp appearance remain independent native review obligations.

Synthetic tests passed exact WALK acceptance; each wrong and missing field; conflicting WALK; idle rejection; 399 versus 400 accepted scans; gaps of 39 versus 40 scans; retained parse misses; active output followed by later idle; non-monotonic scans; stopped runtime; and cutoff. They also reject an incomplete sequence with no sampled clearance and permanently flag amber or WALK returning after observed clearance before any sampled idle. These synthetic records are not runtime proof. Evidence: `pedestrian-probe-offline-guards-1792737293138447500/result.json`.

Root invocation, after restoring Signals and performing the FIRST idle CROSS:

```powershell
python rungproof-next/outputs/screen-audit-2026-10-23/pedestrian_active_walk_request_probe.py --session <PRIVATE_JSON> --cross-x 441 --cross-y 381 --prefix pedestrian-active-walk-r1
```

The helper sends at most one additional physical CROSS. It requires exact same-image public WALK plus PATHCLEAR and RUNNING, then checks image age at most 0.5 seconds after pinned process/client health. One uncertain delivery is never retried. It immediately captures actual pixels, then another frame approximately 0.3 seconds after delivery, before any post-click OCR. If these frames do not still qualify as WALK, active-WALK attribution stays partial.

Passive sampling then follows the fixed matched WALK through first observed clearance and first genuine idle. Amber or WALK returning after latched clearance is permanently surfaced as possible replay, including before first sampled idle. All complete public/scan samples across that transition must have gaps below 40 accepted scans; parse misses remain explicit. After first idle, collect at least 400 accepted scans (eight seconds for the frozen 20 ms fixture) with the same strict gap rule. Any readable active output after idle is surfaced without silently restarting the interval. A large gap, clock discontinuity, stopped runtime, missing clearance, or unfinished interval leaves no-replay evidence partial; it does not establish an app failure.

Runtime is bounded to 30 seconds plus the reviewed base's bounded OCR-worker cleanup; the 11:00 UTC cutoff is checked before actions/captures. No Start, Run, Reset, menu, Health, PATH, force, hidden state, or subsequent CROSS input exists in the helper. Physical lamp and fresh action acknowledgement remain false/pending in its result so root can compare Health to the pre-first-request baseline and independently verify two new CROSS events. Fresh idle CROSS proving renewed admission is a separate root-controlled case.
