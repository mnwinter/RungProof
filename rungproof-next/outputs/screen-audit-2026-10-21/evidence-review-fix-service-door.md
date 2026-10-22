# Lab2.19 command-driven door post-fix native review

Actual selected pixels in `fix-plant219-review-compact.jpg` show closed shutter→partial opening→fully open aperture. Open native Signals: position0/openNCFalse/closedNCTrue/both motor commandsFalse. Subsequent physical Close moves shutter down; stationStop/held preserves the same intermediate shutter edge. Native stopped position28/bothNCTrue/both outputsFalse is a valid mid-travel hold.

`fix-plant219-recovery-review.jpg` shows AppRun+freshClose recovers to fully closed position100/openNCTrue/closedNCFalse/outputsOff. Reset keeps closed baseline and clears cycleCompleteFalse; fresh Run/Open repeat returns to0/openNCFalse/closedNCTrue/outputsOff/completeTrue. R3 Close while appSTOPPED was a QA missing-Run omission, not a failed close input. Endpoint convention0=open/100=closed is preserved.

QA-PLANT-0012.19 missing mechanics and QA-FUNC-001 OPEN/CLOSE delivery subcases resolved for this tested build's normal endpoints, stationStop active hold, fresh recovery, Reset and repeat. Simultaneous commands/precise latency remain regression or untested boundaries rather than native claims. No reviewer UI input.
