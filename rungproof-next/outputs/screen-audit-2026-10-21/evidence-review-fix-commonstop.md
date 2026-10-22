# QA-FUNC-002 post-fix native review

Reviewed selected actual native frames using `fix-commonstop-review-compact.jpg`, plus `fix-commonstop-r1-signals.png`. No reviewer UI input.

- R1 moving shows all three physical green run lamps and a supported pallet near the infeed. After the grounded red common-stop click, stopped/held show all three lamps dark and the same pallet pose across the two-second hold. Subsequent Signals show common_stop_request True, zone1/2/3_run False, and position -7.69496.
- Proper R3 release shows common_stop_request False while all three clear inputs remain True: outputs stay False, position still -7.69496. Release alone therefore does not restart this observed fixture.
- Clear-off sets all three inputs False and stays off. Fresh clear-on shows all three outputs True/physical greens and position advancing to -6.79494.
- The later actual final frame `fix-commonstop-r3-final-1792692707680804400.png` shows pallet at the receiver, position12.3 and route_complete True. All zone run commands remain True in this fixture at the endpoint; this review does not assert an automatic final motor stop.
- R4 Reset shows position -9.3, common stop False, all clears/runs False, complete False, physical lamps dark, and pallet restored to infeed.

QA-FUNC-002 is resolved for the tested station action, moving-stop hold, release/fresh-clear requalification, complete route, and Reset in this built app. Excluded stale-camera R2 release attempt and premature R3 'complete' frame (7.56/False). Exact input-to-stop latency and unsampled interlocks are not established.
