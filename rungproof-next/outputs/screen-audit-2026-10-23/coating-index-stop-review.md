# Coating initial-index AppStop / Run-alone native review

Independent original-pixel review; no input/state queries. PID12768/native1021×660, actual Lab5.10 Coating Line/local runtime, PLC disconnected. All10 cited original SHA256 values match native-ui-actions.jsonl. Frozen existing fixture and public phase qualification are documented in [phase-stop-case-grounding.md](phase-stop-case-grounding.md).

- [Wide ready](coating-r3-wide-ready-before-start.png): both readyTrue; position−2, station/exitFalse, index/spray/ventFalse and speed0. Actual supported gray workpiece lies at the inlet side inside the machine window.
- [Started same camera](coating-r3-started-same-camera.png): scan20917, position−1.84, indexTrue/speed0.5; accepted START momentary footer. The actual workpiece shifts relative to the unchanged machine window.
- [Active initial index](coating-r3-active-initial-index.png): scan20937, position−1.64, indexTrue/speed0.5, bothReadyTrue, sprayFalse/**ventFalse**, stationFalse/exitFalse. This complete public predicate uniquely qualifies initial indexing in this frozen fixture; private qa_phase is not directly observed.
- [AppStop](coating-r3-index-appstop.png) and [held](coating-r3-index-appstop-held.png): STOPPED20982, position−1.19, speed0, index/spray/ventFalse, both readinessTrue. The same-camera gray body is visibly further along its window than the active frame, then holds the same pose between Stop/held originals. No continued motion or scan change appears during the held interval.
- [Run alone](coating-r3-run-alone-index-resumes.png): RUNNING20992, position−1.09, indexTrue/speed0.5 and both readinessTrue; physical workpiece resumes. Log sequence has one physical START, then application Stop and Run, without intervening Reset or second START.
- [Recovery1](coating-r3-recovery-1.png): scan21167, actual stationTrue/position0.01; indexFalse/speed0, sprayTrue/ventTrue. Workpiece is now visibly at station.
- [Recovery2](coating-r3-recovery-2.png): scan21343, position0.92, indexTrue/speed0.5, sprayFalse/ventTrue; supported discharge motion observed. Sparse frames do not independently time spray/purge durations.
- [Recovery3](coating-r3-recovery-3.png): scan21521, exitTrue, stationFalse, position2.23/speed0, all three actuator commandsFalse. Gray workpiece visibly rests on the outfeed belt.
- [Recovery4](coating-r3-recovery-4.png): scan21697; same exit/position/output state and supported final physical pose.

**Q04 closes for this exact initial-index AppStop/held/Run-alone recovery path.** Actual moving part, bounded hold and resumed supported route to exit are independently visible. No failure demonstrated. This does not claim private phase observation, every phase/permissive timing permutation or exact spray timing. Earlier camera overshoots are QA setup, not application defects. No new mandatory case is introduced.
