# Independent Lab 2.7 review

Reviewed one full Lab2.7 frame, the ten-case lab207-evidence-compact.jpg and source manifest, current help, and recorded UI plan. No app input, source changes or simulator/backend execution by this reviewer. Values below are read from actual symbolic-point pixels and matched to the visible green lamp, not merely the helper's check booleans.

| Case | Actual reset_request | Actual stop_contact_nc | Actual permit_output | Physical lamp/runtime |
|---|---|---|---|---|
| normal | False | True | False | Dark / RUNNING |
| reset-only | True | True | True | Green / RUNNING |
| both | True | False | True | Green / RUNNING |
| nc-only | False | False | True | Green / RUNNING |
| normal-repeat | False | True | False | Dark / RUNNING |
| nc-only-repeat | False | False | True | Green / RUNNING |
| app-stop | False | False | False | Dark / STOPPED |
| app-run | False | False | True | Green / RUNNING |
| active-reset | False | True | False | Dark / STOPPED, scan0 |
| reset-run | False | True | False | Dark / RUNNING |

The tested physical left RESET NO and right TEST NC controls each change the corresponding symbolic input. The output follows reset_request OR NOT stop_contact_nc in all four combinations, consistent with the documented normal-off teaching exercise. App Stop removes the output while preserving the active NC-derived request; Run restores it. Reset restores stop_contact_nc=True, rather than incorrectly zeroing all BOOLs, and clears the permit indication. The next Run remains dark.

The missing stop_contact_nc/nc_button Equipment binding entry previously recorded under QA-DOC-002 is an inventory omission, not a nonfunctional NC control: the targeted click demonstrably changes stop_contact_nc and affects the lamp. The on-screen RENDERER BINDINGS list also shows only reset_request/reset_button and permit_output/permit_lamp in these images; that does not negate the measured physical NC action route.

No new functional issue observed. Full-scene acceptance still requires the remaining authoring/persistence/navigation/keyboard cases; this is a GUI-loaded fixture-backed screen result only. This exercise is not a real safety permissive or certified safety control.
