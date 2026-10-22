# Lab 5.2 independent screen evidence review

Reviewed all 14 native lamp/runtime captures through `lab502-output-runtime-compact.jpg`, full `lab502-first-on.png` for scene identity and physical control, `lab502-timer-ui.json`, and matching `ui-action-log.jsonl` entries 102-116. No app input or backend checks by reviewer.

| Capture suffix | Physical lamp | Visible runtime scan/state |
| --- | --- | --- |
| idle | dark | 2205 RUNNING |
| first-on | green | 2230 RUNNING |
| active-retrigger-attempt | green | 2288 RUNNING |
| original-expired | dark | 2400 RUNNING |
| expired-stable | dark | 2457 RUNNING |
| fresh-second-on | green | 2480 RUNNING |
| stop-cancels | dark | 2495 STOPPED |
| run-after-stop | dark | 2504 RUNNING |
| after-stop-settled | dark | 2676 RUNNING |
| fresh-third-on | green | 2700 RUNNING |
| reset-active | dark | 0 STOPPED |
| reset-run | dark | 9 RUNNING |
| repeat-after-reset-on | green | 32 RUNNING |
| repeat-after-reset-expired | dark | 200 RUNNING |

The action log records repeated physical START clicks at (798,364), App Stop (131,884), App Run (52,884), and App Reset (215,884). Log timestamps identify action-step records; they are not exact accepted-input scan or exposure timestamps. The configured waits and observed scans support normal expiry, fresh triggering after expiry, active Stop cancellation with no restart on Run, active Reset cancellation, and a successful fresh pulse after Reset.

For the active retrigger attempt, the lamp was on at scan 2288 and already off at 2400, only 112 scans later at the displayed 20 ms cycle. This supports no full three-second extension from that attempt. It does not locate exact expiry or prove that the second input was accepted rather than simply clicked. No Signals monitor or timer elapsed/internal state was captured. The one-accepted-scan input/coalescing contract remains documented, not directly observed.

Help `docs/help/scenes/lab-5-02-timed-lamp-off.md` explicitly identifies the three-second TP as a training choice, active triggers ignored, expiry allowing fresh START, and Stop cancellation. No demonstrated issue in this fixture-backed evidence. Exact 150-scan timing boundary, held input, same-scan click coalescing, and accepted input pulse width remain unproven. No whole-scene PASS claim.
