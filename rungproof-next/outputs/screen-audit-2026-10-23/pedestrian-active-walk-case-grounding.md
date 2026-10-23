# Q21 pedestrian active-WALK request — finite native grounding

Read-only fixture/scene analysis; no UI input or application changes. Existing fixture `../screen-audit-2026-10-21/test-projects/fixture-lab-5-07-pedestrian-crossing.rpproj.json`; scene `lab-5-07-pedestrian-crossing`.

**Physical CROSS is momentary.** Its legacy ID `toggle-crossing_request` is misleading if read in isolation: current action type is **pulse**, label **Press CROSS (momentary)**, point crossing_request. PATH BLOCK/CLEAR is a separate maintained toggle clear_to_finish. An accepted-action footer/Health record acknowledging the actual CROSS press is sufficient native route evidence; the one-scan input may already beFalse by capture. Do not demand capturing its20ms peak or treat CROSS as a maintained selector.

Exact public WALK predicate: pedestrian_walkTrue, pedestrian_stopFalse, vehicle_readyFalse, vehicle_amberFalse and vehicle_stopTrue; actual walking-person head lit/hand dark. WALK rung is qa_phase3 only in this frozen fixture. Phase timing parameters are amber1000ms→all-red500ms→WALK2000ms→clearance3000ms, then idle when clear_to_finishTrue. These are fixture nominal settings, not precision measurement requirements.

Request edge is sampled as crossing_request AND NOT qa_seen_request; qa_seen_request follows every request. Admission requires qa_phase0 AND clear_to_finish AND qa_request_edge. A pulse while WALK is active is therefore consumed as an event but not queued for later admission. It neither resets WALK timer nor stores a deferred request. Do not infer request acceptance from unchanged WALK alone: pair actual accepted control acknowledgement with the visible active-state predicate.

Minimum finite witness sequence:

1. PATHCLEAR and idle green/hand, fresh CROSS; observe actual WALK/publicpredicate.
2. One actual CROSS press during WALK, acknowledged; observe current sequence continuing rather than immediate restart. Capture ordinary clearance/hand then idle green/hand.
3. With no further CROSS, observe settled idle across at least the nominal full request-to-idle interval (~6.5s) using two bounded snapshots and advancing scans. No replayed amber/WALK. This is a finite no-replay observation window, not an infinite absence claim.
4. One fresh idle CROSS starts another cycle, proving the control was not permanently disabled.

Keep PATHCLEAR throughout to avoid mixing clearance blockage with admission. No simultaneous-priority, exact timer-edge or every-phase request matrix is added. Existing clearance-request rejection remains separate prior evidence; this named case specifically covers active WALK.
