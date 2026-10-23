# Parcel diagnostic V7: root screen-test input

This is a saved learner project, not a simulator script or application repair. Load it through File Open in Lab2.23. Do not change the original V3/V4 files or treat this candidate as a shipped reference.

File: fixture-diagnostic-parcel223-v7-retentive-class-metered.rpproj.json
SHA256: 36de084f73c356c761440e21609b44dbb71a339573837349dc8422212c63fd6a
Schema10;27 networks;23variables. Current parser, compiler, scene bindings and editor-program roundtrip PASS. Pure actual-source accepted-scan model PASS is separate from native proof. No Godot delivered-support callback is in that model.

Existing inputs and commands are unchanged. Private memory timers use explicit LAD RetentiveOnDelay and phase/class-exit resets. Class1/2/3 infeed handoff allowances are300/500/700ms. These are diagnostic geometry-dependent training parameters, not new product timing requirements or hardware/safety ratings.

1. Select Lab2.23; File Open this file; discard only root-owned QA draft if prompted.
2. AppReset, AppRun, physical Start. Observe the actual three receivers, count3, allReceivedTRUE/cycleCompleteTRUE/misrouteFALSE, then all PLC BOOL commandsOFF and route0 on a later settled scan.
3. Reset/repeat. During medium/class2 phase30, after infeed_runFALSE but primary_table_loadedFALSE and table_transfer_runTRUE, click AppStop. Require actual pose hold and commandsOFF. ThenRun: infeed staysOFF during remaining centering, next size indication remains usable and the full batch finishes3.
4. Reset/fresh repeat to check queue/count/phase restoration. Credit only the phases actually observed. Source model interruption cases do not fill untested native cells.

Watch metadata preserves qa_step/qa_scan_step/qa_class. A timer may be observed in its LAD box. No Watch Modify, force, backend runtime API or private coordinate is needed. Use visible Watch/Signals/LAD text and physical poses as witnesses; avoid a fixed wall-time deadline where accepted scans are advancing slowly.

V5 generic700ms is a preserved failing counterexample. V6 ordinary-TON class timing passes normal operation but fails sufficiently late phase30 Stop/Run when Stop resets ET/Q. V7 exists to isolate that restart policy. A passing V7 demonstrates a viable LAD-owned policy; it does not validate every gate/occupancy semantics claim. See hard-issue-analysis.md for causal evidence and limitations.
