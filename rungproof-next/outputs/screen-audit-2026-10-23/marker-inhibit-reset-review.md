# Service marker inhibit — active Reset and default recovery

Independent native review, no reviewer input. AppPID12768/client1021×660, actual Lab2.3 Service Marker Inhibit/local runtime/PLC disconnected. Five original hashes were recalculated and match `native-ui-actions.jsonl` records.

| Original | Actual pixels / feedback |
|---|---|
| [running-controls](marker-reset-r2-running-controls.png) | RUNNING scan15; white service marker visibly lit. |
| [inhibit-active-dark](marker-reset-r2-inhibit-active-dark.png) | Physical INHIBIT click(527,264); RUNNING1785; service marker visibly dark. |
| [inhibit-active-signals](marker-reset-r2-inhibit-active-signals.png) | RUNNING1852; exact point **marker_inhibit_pressedTrue**, service_marker_onFalse; physical marker remains dark. |
| [reset-defaults](marker-reset-r2-reset-defaults.png) | Actual App Playback Reset; STOPPED scan0; marker_inhibit_pressedFalse, **service_marker_onTrue**, white marker visibly lit. |
| [run-default-released](marker-reset-r2-run-default-released.png) | App Playback Run only; RUNNING scan8; inhibitFalse/outputTrue and marker still white. No further physical inhibit toggle. |

Source scene `scenes/migrated/lab-2-03-service-marker-inhibit.scene.json` declares PC BOOL marker_inhibit_pressed initialFalse and PLC BOOL service_marker_on initialTrue; documented normal rule is inverse input. Thus Reset restoring a lit output even while STOPPED is the actual declared scene baseline, not an all-output-zero violation. Do not generalize Stop projection behavior into Reset always forcing every outputFalse.

**Q22 closes for active-inhibit→Reset→Run-default scope.** No application fault is demonstrated. The post-Run outer footer retains the previous Reset event wording (stopped/new machine Start required), while current top/Device Trace explicitly show RUNNING8. Treat this as the existing generic last-event/current-status wording boundary; this report does not create another confirmed footer defect or duplicate QA-UI-002. Broader scene/curriculum acceptance is not claimed.
