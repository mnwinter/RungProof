# Standalone Scene 1 sensor-stop conveyor

Selected native pixels reviewed in `standalone-two-conveyors-lifecycle-review.jpg`; original screenshots retained. No UI input by reviewer.

R1 moving frame shows a supported carton upstream of the fixed photoeye; sensor-stop moves it to the beam, and held preserves that pose. Actual R1 monitor reads object_position0.5, simulated_photoeyeTrue, conveyor_runningFalse and parts_completed0. Thus physical approach and stopped-at-sensor behavior are demonstrated, not merely a command Boolean.

Proper R2 Reset monitor shows position0, simulated_photoeyeFalse, conveyor_runningFalse/count0 and restored upstream carton. Fresh Run/Start repeat-moving again advances the carton; repeat-signals returns position0.5/beamTrue/runFalse with the carton at the fixed sensor. Root's separate exact GUI checks support blocked Start while beam remains occupied and AppStop/Run retaining the blocked state; the compact independent visual strip directly verifies the normal/sensor-hold/Reset/repeat checkpoints, rather than claiming unsampled transitions.

No new defect observed. This is bounded learner-fixture behavior; no count increment is expected before any downstream exit in the displayed exercise. Exact travel speed/stop latency and continuous collision proof are not established.
