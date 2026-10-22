# Lab 2.14 independent native review

Reviewed27normalmotion frames, normal/repeated/recovered finals, reset and Stop/Run checkpoints in five lab214-review sheets. Actual tank level, status stack, eight Signals and runtime retained. Loaded reviewer ladder scope only; no reviewer UI/backend operations.

Physical cyan level rises with pump off: captured sump_level25.8769→43.1077→62.0615, both floatsFalse. At78 high_float=True/pump=True/green status, then high_float clears while pumping continues at61.8429→43.6143→26.2143 (both floatsFalse). This directly supports the captured deadband latch. At20 low_float=True/pump=False/cycle_complete=True/amber status, physical level low. Frames07..26 retain that completed state without another fill/pump cycle. Normal final and fresh repeated final also remain20/highFalse/lowTrue/pumpFalse/enableTrue; the extra START observation does not start a second cycle.

Reset STOPPED0 returns level22, cycle_enable/complete/pumpFalse and red status. Subsequent pumping interruption is captured STOPPED223 at50.6571 with both floatsFalse, pump/enableFalse/red. LateStop223 holds identical level/pose after the planned interval. Run258 restores the fixture's retained cycle_enable/pumpTrue/green and lowers level36.1571 without a fresh START. This is explicit loaded fixture automatic-resume policy, not a general restart requirement. Recovered final2859 is20/lowTrue/highFalse/pumpFalse/completeTrue/enableTrue/amber. Physical tank and stack agree with these states. OCR Iow/low spelling was an evidence-reading alias, not an app failure.

No new issue. Exact float thresholds between samples, abnormal float combinations, motor rotation, pump failure, continuous flow and distinct machine Stop paths remain unproven. No whole-scene PASS.
