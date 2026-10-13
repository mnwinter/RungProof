# Lab 6.8: source-preserving washroom redesign

The compact service housing is implemented and verified in the eight-location checkpoint. The original appliance remains a supported teaching cutaway; no retail-appliance replacement or wall-mounted conversion is claimed.

Actual native service-box bounds are 1.30 m wide ×0.95 m high ×0.30 m deep, with its bottom at0.90 m and top at1.85 m. A narrow grounded bearing post and foot replace the full-height cabinet body. The two-column/two-row layout preserves the retained device basis and derives spacing from measured delivered device widths. The standing reserve is 1.30 m wide ×1.0 m deep ×2.0 m high.

`varied-eight-owned-controls-v1.log` verifies actual picks, measured housing/support/rows, readout retention, output projection and access exclusion. `varied-eight-dryer-v1.log` passes the unchanged dedicated actual hand/sensor, heater-support, timing, Stop/resume/Reset and inhibition checks. Both belong to Debug DLL5028EE182F2245D79CADA20D53016D6FF495B8737035CF11277387EAA6E2B4A3. Dot viewed both current full/split images: compact service box and teaching plate are coherent; basin visibility remains limited. Final human visual acceptance remains open.

The authored appliance is an original teaching cutaway, not a retail hand dryer. `build_hand_dryer_assets.py` defines a 1.0 m enclosure with roof at 1.84 m, an open inlet/outlet, heater-support brackets, an inspection window, and a hand tray at 0.78 m. The sensor and hands occupy approximately 0.87 m elevation. Those relationships explain the lesson and must survive a presentation change.

## Preserve

- Keep every authored scene equipment ID, point owner/type, action, binding, controller and moving-node identity. The hands action remains `toggle-hands_inserted` on `switch_7`.
- Keep `training_accessory_3/HandPair`, all twelve hand meshes, `HAND_SENSOR_lens`, the station basis, and their relative transforms. The runtime tests a transformed 0.8 m segment against actual hand triangles; a raw toggle is insufficient.
- Keep the heater rails bearing on both enclosure brackets. Do not hide a support merely because it complicates appearance.
- Keep fan command eligibility, heater inhibition, countdown readout and output indicator meanings. Passive washroom plumbing must not acquire invented process bindings.

## Presentation design and remaining proposals

The generic four-column 3.1 m wide standing cabinet has been replaced by the compact, separately supported service arrangement above. Functional head ray picks and the measured operator approach are retained.

Any later wall-mounted conversion of the appliance would require original passive rear brackets/supports and an explicit named-component stand removal allowlist. It is not implemented. The current supported teaching cutaway is explicitly identified by its visible teaching plate; its internal cutaway and sensor/hand relationship are unchanged.

Place the entrance and a passive supported basin on a different wall, with a continuous floor and an unobstructed route to the dryer. Reserve separate basin standing, dryer hand interaction, and service-panel approach areas. The nearest-sibling comparison is the service-door lab: the washroom must be distinguished by its actual service wall, sanitary fixture, and user-facing hand station, rather than a new wall color.

## Required evidence

Run `--audit-hand-dryer` after the change. Its existing checks cover heater-bracket bearing, actual hand/sensor intersection, hand clearance against fixed meshes, the 500 accepted 20 ms tick reference interval, withdrawal/reinsertion, Stop/resume/Reset, malformed heater commands, and moving hands out of the sensor beam. Keep these checks intact.

Also run the all-scene control installation verifier and environment opening/protected-volume checks. Add truthful compact-support geometry checks where the generic standing-cabinet contract no longer applies; do not silently relax its assertions. Inspect actual full and split renders, plus an inserted-hands view, for support continuity, readout visibility and fixture overlap.

## Authorization

The parent approved the classroom and dispatch envelopes as structural bases and authorized useful remaining-scene work. Final visual acceptance remains open. This proposal requires source and geometry review before its mounting changes; it does not impose a new human permission gate.
