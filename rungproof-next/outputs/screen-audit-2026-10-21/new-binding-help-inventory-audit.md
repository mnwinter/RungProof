# Seven new command-request help inventory audit

Source/doc comparison only; no UI, build or runtime checks. Six help files corrected here;2.13 coordinated with active jug owner.

| Scene | Declared request bindings | Current I/O inventory | Owner |
|---|---|---|---|
| lab-2-11-inbound-tote-stop | start_cycle_request | All declared request rows present | Coverage agent corrected |
| lab-2-12-assembly-lift | raise_request, lower_request | All declared request rows present | Coverage agent corrected |
| lab-2-13-coolant-jug-fill | start_fill_request | Missing: start_fill_request | Jug owner coordinated |
| lab-2-16-safe-drill | start_drill_request | All declared request rows present | Coverage agent corrected |
| lab-2-19-service-door | open_request, close_request | All declared request rows present | Coverage agent corrected |
| lab-2-23-parcel-sorter | start_sort_request | All declared request rows present | Coverage agent corrected |
| lab-2-24-robot-cnc | start_cnc_request | All declared request rows present | Coverage agent corrected |

Action tables now explicitly name loaded-controller request pulses; legacy preview names remain separate. Ownership/type/defaults follow current declarations. No automatic sequence or passive physics is invented.
