# Standalone Conveyor Inspection Cell native review

Selected actual saved pixels were reviewed in `standalone-conveyor-selected-review.jpg`; no UI input was performed.

R1 moving-a→moving-b shows all three supported cartons translate relative to the fixed conveyor/photoeye/cabinet, so motion is demonstrated independently of a command BOOL. Station-stop and station-held show the same carton poses. R2 restarted→estop shows further travel before arrest; blocked-restart retains the E-stop carton poses. These establish bounded physical movement/arrest, not input-to-stop latency.

R4 repeat-moving→repeat-signals shows real changed carton positions after the reset/restart route. The actual repeat-signals panel reads conveyor_run True, conveyor_speed1.05, estop_ok True, parts_completed1 and photoeye_blocked False. Count1 is visibly present already in that named checkpoint: a later count wait must not be interpreted as persistent zero or no-motion failure. Full support/geometry visible; no belt surface motion or precise speed measurement claimed. Reset numeric evidence and E-stop BOOL details are supported separately by root's exact GUI monitor checks, not inferred from carton poses.

No new defect demonstrated in these selected frames. This finding does not resolve the separate static coverage concern for seven legacy sequence labs.

## App lifecycle follow-up
Selected R6 native strip shows App Stop/held identical carton poses, conveyor_run False and retained parts_completed42. Run alone retains the pose/runFalse/count42; fresh Start changes carton positions/runTrue and count43. This independently supports freeze/fresh-start behavior and retained counting. Additional root-monitored native R5 count26 and photoeyeTrue supplement the sampled sensor/count route; R4 wait with a closed Signals dock is excluded as QA visibility failure.
