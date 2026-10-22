# Independent Lab 4.5 review

Reviewed one full native matching-scene frame and nineteen saved checkpoints via two compact sheets that retain readable physical A/B numeric readouts, the green lamp, all six Signals values and runtime. Crops/source hashes are recorded in lab405-evidence-compact.manifest.json; full PNGs preserved. No UI input, backend execution, app changes or repair planning by this reviewer. The accepted v2 QA ladder was GUI-loaded; invalid v1/VC016 input rejection is not treated as an application defect.

## Actual count/window observations

Every physical A/B readout matches the corresponding channel_a_count/channel_b_count shown in Signals. At every settled checkpoint channel_a_pulse, channel_b_pulse and reset_pressed are False. Actual count pairs and window_ready are:

| Cases | A | B | window_ready / physical lamp |
|---|---|---|---|
| zero | 0 | 0 | False / dark |
| a1 | 1 | 0 | False / dark |
| a2 | 2 | 0 | False / dark |
| b1 | 2 | 1 | False / dark |
| b2 | 2 | 2 | False / dark |
| b3-lower | 2 | 3 | True / green |
| a3-upper | 3 | 3 | True / green |
| b4-upper | 3 | 4 | True / green |
| a4-outside | 4 | 4 | False / dark |
| physical reset | 0 | 0 | False / dark |
| reverse-b1 | 0 | 1 | False / dark |
| reverse-b2 | 0 | 2 | False / dark |
| reverse-b3 | 0 | 3 | False / dark |
| reverse-a1 | 1 | 3 | False / dark |
| reverse-a2 | 2 | 3 | True / green |
| app-stop | 0 | 0 | False / dark, STOPPED |
| app-run-retained | 2 | 3 | True / green, RUNNING |
| app-reset | 0 | 0 | False / dark, STOPPED/scan0 |
| reset-run | 0 | 0 | False / dark, RUNNING |

These physical button cases support independent accumulation in the tested range, lower-bound acceptance A2/B3, upper endpoint cases A3/B3 and A3/B4, rejection A4/B4, reset of both channels and readiness, reverse press-order acceptance, and output-image clearing on App Stop with retained internal counts republished on Run. App Reset clears that retained state. This agrees with the explicitly chosen reference window in the help; it is not a hardware-defined preset.

No new functional issue observed. Exact simultaneous/held input edges, rapid pulses, accumulator overflow, the remaining valid pair A2/B4, explicit B5-above-window rejection, and remaining editor/persistence/navigation/keyboard cases are unproven. Numeric I/O observations do not independently inspect the internal CTU instances. No full-scene PASS claimed.
