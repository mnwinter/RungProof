# Lab 4.6 independent screen evidence review

Scope: 17 saved native UI frames named in `lab406-monitored.json`, inspected as physical readout/lamp, six Signals, and runtime crops. Full `lab406-b3-exact.png` confirms the selected scene and crop locations. Derived sheets `lab406-evidence-compact-1.jpg` and `-2.jpg` have source hashes in `lab406-evidence-compact.manifest.json`. No app input or backend checks performed by reviewer.

| Capture suffix | Actual A,B digits and Signals | confirmation_valid / physical green lamp |
| --- | --- | --- |
| zero | 0,0 | False / dark |
| a1 | 1,0 | False / dark |
| a2 | 2,0 | False / dark |
| b1 | 2,1 | False / dark |
| b2-not-yet | 2,2 | False / dark |
| b3-exact | 2,3 | True / lit |
| a3-exceeded | 3,3 | False / dark |
| reset | 0,0 | False / dark |
| reverse-b1 | 0,1 | False / dark |
| reverse-b2 | 0,2 | False / dark |
| reverse-b3 | 0,3 | False / dark |
| reverse-a1 | 1,3 | False / dark |
| reverse-a2 | 2,3 | True / lit |
| app-stop | 0,0 | False / dark |
| app-run-retained | 2,3 | True / lit |
| app-reset | 0,0 | False / dark |
| reset-run | 0,0 | False / dark |

All settled button_a_pressed, button_b_pressed, and reset_pressed readings are False. Physical reset remains RUNNING. App Stop is STOPPED; Run republishes retained 2,3. App Reset is STOPPED at scan 0; following Run remains zero/dark.

These observations agree with `docs/help/scenes/lab-4-06-multi-press-confirmation.md`: exact A=2/B=3 is an explicitly chosen training assumption, order is unrestricted, and Stop clears count outputs while retaining counter memory. No new demonstrated issue.

Unproven: extra B beyond 3, held/simultaneous button behavior, counter overflow and internal counter implementation. This is a fixture-backed case review, not a blanket scene PASS. Invalid v1 VC016 fixture rejection is outside this successful v2 evidence and is not an app defect.
