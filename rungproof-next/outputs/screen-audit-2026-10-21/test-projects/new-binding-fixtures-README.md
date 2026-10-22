# New operator-binding learner QA inputs

Static JSON only; not UI-load or runtime proof. No PC/SIM feedback writes, no hidden scene sequence start, no automatic process completion. These fixtures consume the newly declared requests. Open normally, Verify/Load, Run, then actual physical controls. Observe actual PLC commands and PC feedback. Stop/Reset between tests.

App Stop clears Output tags; latched private cycle state may remain on Run for fixtures with qa_cycle. Use Reset to rearm; do not treat automatic resume as a mandated app policy.

## lab-2-11-inbound-tote-stop

`fixture-new-binding-lab-2-11.rpproj.json`

No exit sensor exists; Reset ends/rearms fixture. No completion claim. Await actual scanner/scan_complete feedback; never synthesize it.

## lab-2-12-assembly-lift

`fixture-new-binding-lab-2-12.rpproj.json`

Endpoints must come from actual plant PC feedback; if static, do not force them. Simultaneous requests give LOWER precedence only after RAISE clears; QA directional policy.

## lab-2-13-coolant-jug-fill

`fixture-new-binding-lab-2-13.rpproj.json`

Reset between cycles. 5-second discharge is explicit QA parameter, not scene specification. No fabricated jug/probe/fill percentage.

## lab-2-16-safe-drill

`fixture-new-binding-lab-2-16.rpproj.json`

Normal feed-to-bottom and hand-release cancellation only; no retract command exists. Full down/up completion cannot be expressed from declared PLC output alone without plant behavior evidence. New START required after loss.

## lab-2-19-service-door

`fixture-new-binding-lab-2-19.rpproj.json`

NC polarity follows exact scene declarations: false at endpoint. Requires actual loaded-controller feedback/motion. No state writes or reference sequence.

## lab-2-23-parcel-sorter

`fixture-new-binding-lab-2-23.rpproj.json`

Route policy exact legacy targets0/0.5/1; only real size sensors select. No timer-generated parcel detections/counts; Reset rearm. If sensors never present, output enables but batch completion unproven.

## lab-2-24-robot-cnc

`fixture-new-binding-lab-2-24.rpproj.json`

BOUNDARY fixture only: legitimate START-to-pickup infeed. No PLC positions/grip/door inputs or loading feedback declared; cannot invent safe robot/CNC full sequence. Do not credit full completion.
