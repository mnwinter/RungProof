# Shipping pallet manual endpoint JOG rejection

Independent review of12 native originals/action log, no input/state queries. PID12768/client1021×660; actual Lab2.18 Shipping Pallet Accumulation/local runtime/PLC disconnected. All12 original SHA256 values match native-ui-actions.jsonl.

[Manual mode](pickup-r2-manual-mode.png),9514, directly shows auto_modeFalse/pallet_position0/pickupFalse, accepted selectorFalse footer. Correct physical gray selector(563,447) establishes manual mode; wrong earlier r1 target left autoTrue and its JOG rejection was not an endpoint test or product failure.

Five separate actual blue JOG(618,477) presses produce:

| Original | Actual native result |
|---|---|
| [Jog1 active](pickup-r2-jog-one-active.png),9581 | manualFalse, conveyorTrue, position8.76254/pickupFalse, acceptedJOG footer. |
| [Jog1 ended](pickup-r2-jog-one-ended.png),9808 | conveyorFalse/position24.097. |
| [Jog2 ended](pickup-r2-jog-2-ended.png),11650 | conveyorFalse/position48.1939. Pallet enters visible supported belt region. |
| [Jog3 ended](pickup-r2-jog-3-ended.png),11907 | position72.2909/runFalse, actual supported pallet farther along belt. |
| [Jog4 ended](pickup-r2-jog-4-ended.png),12165 | position96.3879/runFalse. |
| [Jog5 ended](pickup-r2-jog-5-ended.png),12425 | position100/pickupTrue/cycle_completeTrue/runFalse, loaded pallet supported at pickup end. |

[Health baseline](pickup-r2-health-before-rejection.png),18985, contains five distinct accepted JOG records and no new rejection. [Qualified manual pickup](pickup-r2-qualified-manual-pickup.png),19053, directly shows autoFalse, pickupTrue, position100, conveyorFalse with same supported pallet.

One further physical JOG is logged. [Rejected](pickup-r2-endpoint-jog-rejected.png),19121, newly displays red Action blocked/scene permissives not satisfied; position100/pickupTrue/manualFalse/runFalse. [Held](pickup-r2-rejected-held.png),19397, retains exactly the same actual pose and state across276scans=5.52s at displayed20ms. [New Health rejection](pickup-r2-health-new-rejection.png),19465, explicitly adds00:24:21 Action blocked above prior accepted JOGs. This proves a fresh rejection rather than merely reusing a stale footer.

**Q18 closes for confirmed manual-mode pickup endpoint rejecting a new JOG without moving or asserting conveyor output.** CompletionTrue is a status, not an actuator command that should be cleared by this rejected input. No defect or exact jog-distance accuracy is claimed; observed increments are not normalized to fictional25units. Broader auto/Reset/lifecycle acceptance remains separately scoped.
