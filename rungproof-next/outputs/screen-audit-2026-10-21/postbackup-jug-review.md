# Post-backup B5 jug native review

New untracked evidence report only; tracked source/reports and Git were not modified. Native UI driver remains root-exclusive.

Selected real screenshots are combined in `postbackup-jug-r1-review-compact.jpg` and `postbackup-jug-feedback-review.jpg`. R1 shows the supported jug move from infeed to station vicinity and then downstream. Actual complete Signals reads fill_percent100, high_level_probeTrue, cycle_completeTrue, jug_presentFalse, conveyor/valve/skidFalse. This supports the modeled fill/discharge endpoint after the180mm geometry-correct nozzle stroke; the original transport/fill0 failure is no longer present in this route.

R2 active→stopped shows further approach to the station before arrest. Held and stopped Signals retain fill1.81818/jug_presentTrue/highFalse/allthreecommandsFalse, with unchanged jug-to-station pose despite reframing. This establishes an interrupted partial-fill hold, not just an already-completed endpoint. R3 resumed-end native image reads fill100/highTrue/cycleTrue/jug_presentFalse/allthreecommandsFalse with the jug downstream.

Boundaries: phase filenames do not prove exact state at each named moment; snapshots do not establish exact2.2s filling or input-to-stop latency. Jug is opaque and an active liquid-stream sample is not independently legible in these selected frames; modeled fill/current feedback is distinguished from direct visual liquid-volume proof. Reset/fresh repeat and helper terminal confirmation pending parent report. No broad scene-complete claim beyond observed routes.

## Reset/fresh repeat and scoped closure
Parent confirms R3 resumed job terminal0 and exact completed/reset checks, followed by fresh Run/physicalStart R4 terminal0/all6 completed checks. Independent actual `postbackup-jug-reset-repeat-review.jpg` shows Reset moves the jug upstream and reads fill0/highFalse/cycleFalse/jugFalse/allthreecommandsFalse; fresh repeat moves it downstream again with fill100/highTrue/cycleTrue/jugFalse/allthreecommandsFalse.

QA-PLANT-001 Lab2.13 and QA-FUNC-001 physical Start subcases are now resolved for this B5 observed normal fill/discharge endpoint, active partial-fill Stop hold, resumed completion, Reset and fresh repeat. Exact dwell, visible stream and internal liquid volume remain the limits described above. This conclusion is post-backup evidence only; tracked reports/source and the verified backup commit were not changed.
