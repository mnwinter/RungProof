# Post-backup Lab 4.5 upper-boundary review

Independent original native screenshots only; no application/source, tracked-report or Git changes.

- small-count45-r4-a2-b4-physical.png directly shows A COUNT 2, B COUNT 4 and illuminated green stack segment. The paired r4 Signals frame shows channel_a_count 2, channel_b_count 4 and window_ready True.
- After one additional physical B action, small-count45-r6-a2-b5-physical.png and settled r6 Signals show A COUNT 2 retained, B COUNT 5, green segment dark and window_ready False. Both input pulse values are False in the settled frame, so this is the completed count rather than a held-button transient.

This closes the specific native A2/B4 inclusive upper-edge and A2/B5 out-of-window cases for the loaded V2 QA ladder. Earlier normal/lifecycle evidence remains separate. No new defect observed. This review does not add proof for all operand combinations, simultaneous edges or exact pulse timing.
