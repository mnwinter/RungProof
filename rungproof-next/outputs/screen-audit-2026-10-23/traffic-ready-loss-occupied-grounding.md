# Q08: READY loss during an occupied traffic crossing

Read-only grounding; no UI, model execution, application or fixture changes. Existing fixture: `../screen-audit-2026-10-21/test-projects/fixture-lab-11-12-mobile-traffic-lights.rpproj.json`, SHA256 `bf6bfa433dcbee521e55b8ee8d529650b352133fb28371139a0ef4408d6c9e45`; scene `lab-11-12-mobile-traffic-lights`. This is the existing educational contract, not real traffic engineering or a safety function.

## Expected behavior and ownership

| Condition | Existing contract and visible witness |
| --- | --- |
| Qualified vehicle enters | PLC owns all six lamps. A vehicle commits only with its own green, READY and both manual CLEAR inputs true, no green conflict, and no unfinished opposing committed vehicle. Physical motion and occupancy are plant-owned. |
| READY becomes false while occupied | Fixture moves `qa_traffic_phase` to 0. Both red commands true; both green and both amber false. A vehicle already committed keeps clearing its straight route as accepted runtime advances. Do not expect it to freeze. |
| READY restored while still occupied | Startup timer restarts. Even after its nominal 1 s expires, phase 0 waits for actual `crossing_clear=True`. Both red remain commanded during this restart hold. No separate Start is declared or required. |
| Crossing clears | Phase 0 releases to phase 1, **A green**, regardless of which direction was active before loss. Other direction remains red. Visible vehicles continue to their retained endpoints; they are not implicitly requeued. |

Exact frozen fixture evidence: phase-reset networks begin at lines 335/408/481/554; phase-0 TON is 1000 ms at 627–741; green periods are 6000 ms, amber and clearance 1000 ms each at 742–1430. The phase-0 completion network at 1974–2070 requires `qa_startup_red.Q` AND `crossing_clear`. Phase 3/6 completion also requires actual crossing clear. Green/amber output networks 2073–2519 require phase plus all three permissives; red networks 2521–2691 are NOT own green AND NOT own amber. Private `qa_traffic_phase`, `qa_reference_running` and timers are already declared in Watch at 2777–2803; no fixture change is needed if private phase qualification is useful.

`src/Scenes/MobileTrafficPlantModel.cs:13–28` defines speed 1.3 m/s, endpoints ±3.6 m, and footprint occupancy `Visible && abs(position)<2.2`. Lines 54–61 gate **new** commitment but unconditionally advance a previously committed vehicle until its endpoint. Queue is allowed only before a request or after completion (29–37). `SceneSimulationRuntime.MobileTraffic.cs:17–22,38–54` enforces PC/PLC ownership and derives actual occupied, crossing-clear, conflict and numeric positions from projected vehicles.

## Minimal native case

1. Load the exact fixture, Run, enable existing READY, A CLEAR and B CLEAR, then use QUEUE A once. These three controls are maintained toggles, despite pushbutton styling; scene lines 62–120 and action definitions 304–329 identify the actual labels. Do not toggle CLEAR during this case.
2. Capture a real moving A vehicle early in occupancy: `road_a_occupied=True`, `road_b_occupied=False`, `crossing_clear=False`, READY/CLEARs true, `signal_conflict=False`, and RUNNING. Prefer A position near −1.8 m, corroborated by original moving-vehicle pixels and a later increasing position. A queued vehicle at −3.6 is not yet an occupancy witness.
3. Physically toggle READY off; retain accepted control acknowledgement, READY=False, both red, all green/amber off, occupied feedback and actual changed vehicle pose/position. Restore READY promptly while occupancy remains true; no App Stop/Run/Reset or extra queue is needed.
4. Retain restored READY=True with occupied/crossing-clear=False and both red. If possible, show advancing scans for more than the nominal 50 accepted scans of startup while still occupied; private Watch `qa_traffic_phase=0` can remove normal all-red ambiguity. Then observe actual clear and eventual A-green/B-red, without an opposing entry during the occupied hold. Finish at the committed vehicle's endpoint; no hidden state or artificial feedback.

Source geometry gives about **3.38 s of accepted runtime** inside the occupied interval; A near −1.8 has about 3.08 s until crossing clear. Full travel is about 5.54 s. Startup adds nominal 1 s. These are opportunity estimates from constants, not freshly measured native timing. A short READY-off hold (~0.2–0.5 accepted-runtime seconds) followed by prompt restoration leaves time to observe startup-expired-but-occupied hold. Late loss near A +2.2 can miss that condition. One bounded ~12–15 s observation after setup is sufficient in nominal behavior; slower/unobserved sampling leaves the named witness pending.

If the existing camera makes B's physical lenses easier to see, QUEUE B alone is an alternative using the same existing controls: wait through initial A green/amber/clearance (nominal 9 s including startup), then trigger early B occupancy near +1.8 and inspect decreasing position. Restoration still restarts **A**, not B. A second queued direction is unnecessary for this finite loss/restoration case; prior native flow already covers opposing queued movement.

## False qualifiers and limits

- `road_a_clear`/`road_b_clear` are manual entry fixtures, not inverses of occupied feedback. They normally stay true while a vehicle occupies the crossing.
- `crossing_clear=False` does **not** globally require red: ordinary own-direction green legitimately coexists with its vehicle's occupancy. The all-red requirement here is the restart phase after READY loss.
- `signal_conflict=False` only means no simultaneous A/B green in this model; it does not alone prove every head's three-color exclusion or physical lens rendering.
- Initial/end-point `crossing_clear=True`, a queue acknowledgement alone, loss after the vehicle clears, or an App Stop with all lamps dark cannot close Q08. A late restore after exit does not prove the occupied restart hold.
- A sampled clear/green frame cannot prove every intervening scan. Keep chronology, scan gaps and original vehicle images. Do not require capturing a single 20 ms boundary or infer an unobserved contradictory-green absence.
- The previous independent native review (`../screen-audit-2026-10-21/evidence-review-lab1112.md`) already proves normal committed clearance under amber/red and occupied App Stop/Run. Its READY losses occurred at endpoints, so occupancy READY loss remains a distinct witness. It also notes A's head faces away; do not promote A's symbolic lamp commands into a claim about unseen physical lenses.

`src/App/Main.MobileTrafficReview.cs:107–112` contains a corresponding source-authored occupied READY-loss/restore assertion. It is causal reference only; this agent did not run it, and it is not the new native proof.
