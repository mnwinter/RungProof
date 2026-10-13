# Scene-control verifier command setup — V22b

Read-only inspection confirms scene-controls-v22b.log starts conveyor-cell correctly. Main.cs:2000–2203 expects conveyor_run and conveyor-estop, performs viewport-routed clicks, then direct controller-seam checks. Main.cs:2288 intentionally loads box-volume for fixed-readout/button feedback; its late load is not startup contamination.

Headless invocation returned FAIL for routed Start/depression/E-stop while direct virtualRestarted passed. Even headless1600 failed according to root retained experiment: resolution alone does not make headless GUI routing valid. docs/OFFLINE_LADDER_ACCEPTANCE.md:65–70 explicitly requires a real window for pointer/layout checks; historical REVIEW_COMPLETION_LEDGER.md:148 records the same headless64x64 limitation.

Root reused capture wrapper no-focus settings plus tools/nonfocus_capture_bootstrap.gd with an actual secondary-display 1600×900 renderer, --verify-scene-controls and --capture-size=1600x900. scene-controls-v22b-native.log:37/53–56 reports picking, feedback, overlay, external-image and SCENE_CONTROL_VERIFY PASS; all listed final fields true, no ERROR match found. Root records exit0 and finally removed temporary override. This is a bounded scripted in-app input PASS, not physical operator input or safety acceptance.

Conventional documented invocation: `& $godot --path . --resolution 1600x900 -- --verify-scene-controls`. No --scene-id argument is required. Nonfocus mode additionally requires the existing bootstrap's real renderer and deferred secondary-window reveal; copying its initial transparent1px settings without that reveal would be invalid. No product source changed for this diagnosis.

