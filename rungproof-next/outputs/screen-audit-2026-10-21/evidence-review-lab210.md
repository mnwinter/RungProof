# Independent Lab 2.10 review

Reviewed a full native identity/layout frame and all twelve saved cases through lab210-evidence-compact.jpg plus source-hash manifest, current help and recorded physical/UI plan. No UI input, backend execution or app changes by this reviewer. The loaded QA fixture selects an explicit basic seal-in policy; this review does not establish that it is an app-supplied default solution.

All settled captures show collector_start_request=False and collector_stop_request=False, consistent with released pulse actions. Actual run_latched/collector_run and the physical cabinet RUNNING lamp are:

| Cases | run_latched | collector_run | Physical running lamp/runtime |
|---|---|---|---|
| idle | False | False | Dark / RUNNING |
| start-release-sealed | True | True | Green / RUNNING |
| retained-idle after1.5second wait | True | True | Green / RUNNING |
| machine-stop | False | False | Dark / RUNNING |
| fresh-start | True | True | Green / RUNNING |
| machine-stop-repeat | False | False | Dark / RUNNING |
| fresh-start-repeat | True | True | Green / RUNNING |
| app-stop-observed | False | False | Dark / STOPPED |
| app-run-cleared-latch | False | False | Dark / RUNNING |
| restart-start | True | True | Green / RUNNING |
| active-reset | False | False | Dark / STOPPED, scan0 |
| reset-run | False | False | Dark / RUNNING |

The physical START pulse seals the command after release; it remains true through the idle observation. STOP clears it while app playback continues; repeat Start/Stop works. App Stop clears both PLC-owned latch/output values in this fixture path, so App Run alone does not restart. A fresh machine Start restores the run command. App Reset clears the active latch/output and Run remains stopped at the machine level.

The native fan geometry is visible in the reviewed frames, but its blade orientation/continuous rotation is not confidently distinguishable from these spaced still crops. Do not count fan motion as proved from collector_run=True or the RUNNING lamp alone. No absent-motion defect is established either; a bounded motion-focused observation remains needed.

No new functional issue identified. Unproven cases include simultaneous/held Start+Stop priority, rapid pulse acceptance, exact scan response, fan acceleration/coast behavior, and remaining editor/save/load/navigation/keyboard workflows. No global scene PASS or repair plan selected.
