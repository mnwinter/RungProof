# Drawbridge descent acknowledgement-loss independent review

Independent review of six original native PNGs and `native-ui-actions.jsonl`, 2026-10-23 UTC. No UI input or application-state queries were performed by this reviewer. Action records identify app PID12768, client1021×660; visible title is Lab5.8 Drawbridge Control, local runtime RUNNING, PLC disconnected. Each of the six originals below was rehashed and matches its recorded SHA256.

## Completed observations

| Original | Visible actual state |
|---|---|
| [raised-ready](drawbridge-r1-raised-ready.png) | Scan19242; bridge_angle70, bridge_raisedTrue, bridge_homeFalse; bridge_requestTrue and traffic_stoppedTrue. Both bridge directionsFalse at raised limit. barriers_closedTrue/barrier_angle0; traffic_releaseFalse/traffic_stopTrue. |
| [actual-descent-before-ack-loss](drawbridge-r1-actual-descent-before-ack-loss.png) | After cabinet BRIDGE click(467,257), scan20205; bridge_requestFalse, bridge_lowerTrue, angle57.05, both endpoint feedbackFalse. This is an actual descending intermediate position, rather than a command at an unchanged endpoint. |
| [descent-ack-lost](drawbridge-r1-descent-ack-lost.png) | After cabinet STOPPED click(489,263), scan20255; accepted-action footer explicitly reports maintained traffic_stopped=FALSE. bridge_lowerFalse, bridge_raiseFalse; actual angle45.85; both endpointsFalse. |
| [descent-ack-lost-held](drawbridge-r1-descent-ack-lost-held.png) | With no further machine control input and a logged3s wait, scan20431; angle still45.85, both motion commandsFalse, traffic_stoppedFalse. |
| [held-wide-first](drawbridge-r1-held-wide-first.png) | Scan22210; actual hinged deck visibly tilted above the river, barriers visibly across the roadway. Signals remain angle45.85, lowerFalse, traffic_stoppedFalse, barriers_closedTrue, traffic_releaseFalse. |
| [held-wide-second](drawbridge-r1-held-wide-second.png) | Same camera and actual deck/barrier pose after logged2s wait; scan22335. Same angle45.85 and inhibited descent commands/ack state. |

The close-view snapshots establish measured descent from70→57.05 before acknowledgement withdrawal, then command removal and held45.85 with scans continuing. The paired wide images independently establish a real tilted-deck pose held between captures; the conclusion does not rely only on a numeric Signals row. The deck geometry in the two wide originals shows no visible displacement. Closed-barrier feedback and actual barrier pose remain consistent.

`motion_inhibited` remainsFalse: this sequence removes the PLC descent command when the acknowledgement is absent; it does not demonstrate an active forbidden command refused by the plant. RUNNING remains visible throughout, so this is **machine acknowledgement loss**, not application Stop.

## Boundary at this review

Acknowledgement restoration, renewed descent and home/barrier reopening are **pending** at this report stage. No recovery result or full-cycle acceptance is claimed. No defect is established by the completed stages. Timing is bounded by saved native frames and advancing scan values, not continuous video.

Camera framing/panning during the interval was ordinary view navigation; it is not evidence of process input or a navigation defect. The original log stores some timestamps in−07:00 despite its `utc` property:21:11:58−07 through21:12:58−07 correspond to04:11:58–04:12:58 UTC on October23.

## Restoration completed — subsequent original evidence

The following four additional original PNG hashes were recalculated and match their action-log SHA256 records. The only restored process input is the same physical STOPPED selector at(489,263), at04:14:24 UTC; other subsequent recorded operations are waits or camera framing.

| Original | Actual pixels and measured feedback |
|---|---|
| [ack-restored-lowering](drawbridge-r1-ack-restored-lowering.png) | Scan26565; accepted-action footer reports traffic_stopped=TRUE. bridge_lowerTrue, actual bridge_angle **36.4** (decimal visibly present; not OCR364), bridge_homeFalse; barriers remain closed and traffic_releaseFalse. This proves resumed descent from held45.85 without another BRIDGE request. |
| [home-recovery-progress](drawbridge-r1-home-recovery-progress.png) | Scan26740; angle0/homeTrue, lowerFalse; barrier_angle63, barrier_openTrue, barriers_openFalse; traffic_releaseFalse. Home is reached before completed barrier opening/traffic release. |
| [home-recovered](drawbridge-r1-home-recovered.png) | Scan26918; angle0/homeTrue; barrier_angle90/barriers_openTrue, barriers_closedFalse; traffic_releaseTrue/traffic_stopFalse. |
| [home-final-wide](drawbridge-r1-home-final-wide.png) | Scan28835; same home/open/release feedback. Actual hinged deck visibly flat across river and both barrier arms upright/open. The physical recovered pose agrees with measured state. |

**Q01 is closed for this exact scope:** qualification loss during actual descent removes descent command and holds measured angle/real tilted pose; restoring the physical STOPPED acknowledgement resumes lowering, reaches measured home, then opens barriers and releases traffic. No defect is demonstrated. This update supersedes only the pending-restoration boundary above; it adds no broader phase/permutation acceptance requirement and does not claim every drawbridge behavior or the whole audit complete.
