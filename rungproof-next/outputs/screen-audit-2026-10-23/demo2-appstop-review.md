# Demo 2 application Stop / retained-request TON restart

Independent original-pixel review of seven native PNGs and their `native-ui-actions.jsonl` records. All seven recalculated SHA256 values match. AppPID12768/native1021×660, actual Demo2 Delayed Lamp Timer title, local runtime, PLC disconnected. No input by reviewer.

| Original | Actual observed state |
|---|---|
| [request-on-partial-et](demo2-r1-request-on-partial-et.png) | Physical REQUEST selector click(338,242); RUNNING scan4828. Actual panel ELAPSED MS **300**; lamp dark. |
| [appstop](demo2-r1-appstop.png) | Actual application Stop(113,646); STOPPED scan4862; physical elapsed display0 and lamp dark. |
| [appstop-held](demo2-r1-appstop-held.png) | After logged1.5s no input, still STOPPED4862, elapsed0/dark. Tooltip distinguishes application Stop from machine Stop input. |
| [retained-request-signals](demo2-r1-appstop-retained-request-signals.png) | Same STOPPED4862; timer_requestTrue, machine_stopFalse/reset_pressedFalse, delay_elapsed_ms0 and delayed_lampFalse. This directly establishes retained request while projected elapsed/output are reset. |
| [run-early](demo2-r1-run-retained-request-early.png) | App Playback Run only, RUNNING4867, timer_requestTrue, elapsed100, delayed_lampFalse. No new REQUEST toggle. |
| [run-intermediate](demo2-r1-run-retained-request-intermediate.png) | RUNNING4918, elapsed**1120**, lampFalse, requestTrue. The leading1 is present; not120. |
| [run-complete](demo2-r1-run-retained-request-complete.png) | RUNNING5011, elapsed2000, delayed_lampTrue, requestTrue. Actual green physical lamp visibly lit; machine_stop/reset remainFalse. |

The input log shows REQUEST once, application Stop, view navigation to Engineering/Signals, then App Playback Run and waits. No machine STOP/RESET or second REQUEST toggle occurred. From the frozen Stop scan4862 to early4867, five accepted20ms scans yield100ms; later scan4918 gives56×20=1120ms. Thus this is a fresh TON interval after application Stop, rather than resuming its earlier300ms elapsed value. Completion is observed at the2000ms preset; exact wall-clock threshold timing is not asserted from sparse frames.

**Q07 closes for this exact scope:** application Stop clears TON elapsed/output while retaining the maintained REQUEST, and Run alone begins a fresh two-second interval ending in the lamp turning on. No defect demonstrated. It does not establish all curriculum recovery steps or substitute for machine STOP/RESET testing. Initial selection/transition frames that still showed the prior scene are not counted as a navigation defect; all behavior frames above identify actual Demo2.
