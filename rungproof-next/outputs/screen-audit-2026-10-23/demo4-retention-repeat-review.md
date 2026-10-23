# Demo4 machine Stop / application Stop / full second batch

Independent review of12 behavioral originals and complete input chronology; all19 demo4-r1 native action capture hashes were recalculated and match. PID12768/native1021×660, actual Demo4 Batch Process Blocks/local runtime, PLC disconnected. No reviewer input.

Grounded physical controls: bottom START(565,375), STOP(594,379), RESET(625,383), NEW PALLET(657,387); top TYPE VALID(565,349), COUNT REQUEST(595,354). Input log verifies separate physical event clicks; no backend controller changes.

- [Count2](demo4-r1-count-two.png),14137: machine_runningTrue, typevalid/requestTrue/count2/validFalse; physical COUNT2 visible.
- [Actual machineStop](demo4-r1-actual-machine-stop.png),14200, and [held](demo4-r1-machine-stop-held.png),14276: accepted machineSTOP momentary footer, machine_runningFalse, retained count2, app scans continue. This repairs the evidence gap from an old misleading STOP filename; current actual response is directly visible.
- [Machine freshStart](demo4-r1-machine-restart-retains-two.png),14341: machine_runningTrue/count2, no phantom increment.
- [AppStop settled](demo4-r1-app-stop-settled.png) and [held](demo4-r1-app-stop-held.png): STOPPED15308, projected count0/machine_runningFalse, typevalid/countrequest remainTrue. [AppRun](demo4-r1-app-run-retained-count.png),15339: count2 restored without pallet event, machine_running remainsFalse. Internal count retention is inferred from actual republished result; not private DB inspection. A fresh physicalSTART is required and logged.
- Three further separate NEWPALLET clicks give [first full5](demo4-r1-first-full-five.png),16251: actual right count5/countvalidTrue and physical COUNT5.
- [MachineReset](demo4-r1-machine-reset-zero.png),16316: acceptedRESET, count0/runningFalse/validFalse while typevalid/requestTrue. [SecondStart](demo4-r1-second-start-zero.png),16375: runningTrue/count0, no phantom event.
- Five distinct postreset NEWPALLET inputs are logged: [second first](demo4-r1-second-batch-first.png),16435, count1; four additional clicks finish [second full5](demo4-r1-second-full-five.png),16575, actual rightSignals count5/countvalidTrue and physicalCOUNT5. Intermediate2/3/4 values were not separately captured, so no direct-pixel claim for those exact intermediate samples is made; input count and final outcome establish full second batch.

CurriculumPassed appears at secondbatch1 per its declared recovery proof. That label alone would not prove a full second five-count batch; final rightSignals/display5 supplies the extra required result. Persistent session evidence text still quoting count1 is historical pass evidence, not the current numeric result.

**Q09 closes for actual machineStop count retention/freshStart, applicationStop output-image reset and count republish, and machineReset followed by a full second valid five-event batch.** No defect demonstrated. No held-event or all-permissive permutation matrix is inferred. Actual machineStop and applicationStop behaviors remain distinct.
