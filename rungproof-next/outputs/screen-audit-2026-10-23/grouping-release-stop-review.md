# Grouping release AppStop / held / Run recovery

Independent review of nine original native PNGs and action records; all nine rehashed SHA256 values match native-ui-actions.jsonl/helper result manifest. PID12768/client1021×660; actual Lab4.8 Package Grouping Station/local runtime, PLC disconnected. No reviewer input.

## Qualified interruption

[observe042](grouping-release-stop-r3-1792734815827933200/observe-042.png), scan8603, and [observe043](grouping-release-stop-r3-1792734815827933200/observe-043.png), scan8616, show count3/group_releaseTrue/group_conveyor_runTrue/group_readyTrue, raised gate stop_position100/stop_raisedTrue, transfer_completeFalse. Three cartons are visibly grouped on rollers near the raised stop assembly. [Menu recheck](grouping-release-stop-r3-1792734815827933200/menu-armed-release-recheck.png), scan8649, preserves that exact active-release/not-complete predicate; receiver_occupied has becomeTrue as the leading carton advances.

Exactly one application Stop is logged. [Immediate](grouping-release-stop-r3-1792734815827933200/stop-immediate.png) still shows menu/RUNNING scan8670 with prior commands: that frame is transitional, not sufficient accepted-Stop proof. [Settled held](grouping-release-stop-r3-1792734815827933200/stop-held.png) shows STOPPED8670, conveyor/release/readyFalse and projected count0, while actual gate remains100/raisedTrue, transfer_completeFalse and grouped cartons remain supported. [Root held-second](grouping-r3-held-second.png) independently shows the same STOPPED8670, actual carton/gate pose and feedback/commands. Thus a paired settled hold exists despite helper stopped_scan_heldFalse from comparing its transitional first frame.

## Run-alone recovery

[Run alone](grouping-r3-run-alone.png) shows RUNNING8700, count3 restored without a new load, conveyor/release/readyTrue, gate100, receiver_detectedTrue/occupiedTrue and completionFalse. Input plan uses App Playback Run only after Stop; no Start, Reset or new carton load.

[Recovery moving](grouping-r3-recovery-moving.png), scan8825, visibly shows the three-carton group advancing farther onto receiver rollers relative to the unchanged gate/scene. [Complete](grouping-r3-recovery-complete.png), scan9501, shows all three cartons together supported on receiver rollers beyond the stop, transfer_completeTrue, group_count3 retained, conveyor/release/readyFalse, gate position0/loweredTrue. This is actual supported transfer progression and completion, not merely a status label.

**Q02 closes for the qualified active raised-gate release→application Stop→settled hold→Run-alone completion scope.** No app fault demonstrated. Projected count0 during Stop differs from retained counter count3 restored by Run. Earlier r2b Stop after transfer already completed is invalid for phase qualification, not an application failure; r1 wrong-scene helper selection is QA setup. Neither invalid attempt supersedes this proof. No all-phase or universal recovery acceptance is claimed.
