# Q19 chain-lift upstroke Stop grounding

Read-only fixture/source/prior-review inspection, 2026-10-23. No UI, simulator/model execution, fixture or application changes. Observer preparation waits for root's actual current native frame. Cutoff2026-10-23T11:00:00Z.

Existing input: `outputs/screen-audit-2026-10-21/test-projects/existing-chain-lift-installation-reference.rpproj.json`, SHA256 `09ef3c473eb1eef48638f80ad8d6df2d51cc35114c0e50a7af6f4f9402f6851a`. It is schema1,20ms scans, empty configured Watch list (line931), with private BOOLs start_edge/start_accepted and no private phase/height variable. All source paths below are relative to `rungproof-next`.

## Qualifying native phase

Require same-image **RUNNING**, cycle_activeTrue, lift_enableTrue, lift_lowerFalse, chain_runFalse, carton_on_liftTrue, lift_homeFalse, lift_upperFalse, carton_at_receiverFalse and transfer_faultFalse. destination_clearTrue corroborates the valid carried-load route. Retain actual images showing the loaded carriage between endpoints; two advancing physical poses are stronger than command bits alone. No public numeric height point is declared, so do not invent one for an OCR predicate.

This is the loaded **upstroke**, not the initial horizontal index. The fixture's raise rung is cycle_active AND carton_on_lift AND NOT lift_upper (fixture794–856); horizontal and empty-return rungs are separate (699–793,857–927). The plant derives HOME/UPPER from actual bounded height and carton_on_lift from full-footprint support (`src/Scenes/ChainLiftPlantModel.cs:19–29`). Its native adapter projects those points from that same model (`SceneSimulationRuntime.ChainLift.cs:87–100`).

The upstroke is nominally4s:2.1m stroke at Stroke/4 per second (model13,65). From Reset, first index is approximately6.25s: initialX−2.7 to fully carried center−0.825 at0.3m/s (model14–15,68–75). These are accepted plant times with scan quantization, not fixed-delay Stop instructions. Root can arm Playback during the first index, then let a screen observer identify the actual upstroke.

## Stop and authorization policy

App Stop clears all five PLC BOOL outputs, including **cycle_active**, while retaining plant load/carriage positions. Run alone must hold the interrupted pose: start_accepted is an Assign result of a fresh edge, and cycle_active's self-seal was cleared. A new physical START pulse at the held upstroke satisfies the existing on-lift+destination-clear acceptance branch and restores authorization; lift_enable resumes from retained height (fixture179–455,586–698).

The intended continuation is UPPER→horizontal discharge to receiver→empty-carriage lower→HOME. Final native endpoint: carton_at_receiverTrue, receiver_occupiedTrue, destination_clearFalse, lift_homeTrue, carton_on_liftFalse, cycle_completeTrue, cycle_activeFalse and all three motion commandsFalse, faultFalse. Completion is a **PLC Output**, not plant-invented completion. A later App Stop also clears that Output; it must not be interpreted as losing the physical carton.

Runtime output clearing/pulse disposal is in `VirtualControllerRuntime.cs:257–282` and `VirtualControllerSession.cs:101–111`. The source-authored integrated audit explicitly checks Stop hold, Run-alone hold, fresh-START movement across load/up/discharge/down (`Main.ChainLiftInstallationAudit.cs:182–197`); this states policy but does not replace native upstroke proof. Help92–101 agrees with that policy.

## Common false qualifiers

- HomeTrue/box_presentTrue/destination_clearTrue is the Reset baseline. chain_runTrue/homeTrue is initial horizontal loading, not vertical rise.
- lift_enableTrue alone can include the scan reaching UPPER. lift_homeFalse/upperFalse alone also describes a stopped intermediate carriage or descent. Require direction/authorization plus actual pose.
- chain_runFalse alone includes idle and stopped states. lift_lowerTrue is empty-carriage descent, not the pending upstroke.
- Receiver beam clear is not equivalent to empty receiver: the footprint beam clears after the carton passes, while receiver_occupied staysTrue (model24–29). destination_clearFalse is expected at successful final discharge.
- Equipment named switch_8/switch_9/switch_10 displays box/home/destination feedback. These are not exposed feedback toggles. The scene has exactly one action: `Start carton cycle (momentary)`→lift_start, with physical **START** on lift_start_station (`scenes/migrated/lab-4-09-chain-drive-lift.scene.json:231,370–375`). Do not use Watch Modify or a fabricated box/destination button.

## Minimal normal UI case

1. Select actual4.9, import the unchanged reference, Verify+Load and Run; confirm carton upstream/carriageHOME/no fault.
2. Press the grounded physical START. Observe first index, then qualify the actual loaded upstroke above. App Stop once before UPPER; capture immediate and held scan/pose/commands.
3. App Run alone; observe advancing scans with the same loaded intermediate pose and no motion command. This held interval is expected.
4. Press physical START freshly. Observe continued rise→UPPER discharge→actual lowering→final HOME and supported receiver carton with the endpoint values above.

Prior `evidence-review-lab49.md:3–9` already establishes normal physical rise/discharge/final HOME, index Stop→Run-alone hold→fresh-START recovery and Reset. It explicitly excludes a wrong read-only LED click and a prematurely named completed frame. The finite missing witness is **Stop during actual vertical motion**, its held pose, correct reauthorization, and an actual lowering observation during recovery. No whole-scene PASS or nonexistent feedback-control test follows from the earlier evidence.
