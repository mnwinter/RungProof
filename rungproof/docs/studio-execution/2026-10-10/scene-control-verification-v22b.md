# V22b routed scene-control verification

Current DLL: 7D1F6FC463D885BF6076EB5C4E32ABAFC74B9FDDDC2EBFA950A76B53A41E7813.

Initial headless invocation and headless resolution1600x900 rerun failed routed click checks. These are retained in scene-controls-v22b.log and scene-controls-v22b-1600.log. The existing verifier requires a rendered viewport; box-volume at the end is an intentional feedback subcase, not wrong startup selection.

Root reused the existing capture launcher's no-focus override and nonfocus_capture_bootstrap.gd with gl_compatibility, --verify-scene-controls and --capture-size=1600x900. Log scene-controls-v22b-native.log confirms primary=0 secondary=1 size1600x900 noFocus=true mousePassthrough=true. Terminal exit0, zero ERROR lines, all SCENE_CONTROL_VERIFY fields PASS including routed start, cap depression/restoration, E-stop latch/stop, restart block, reset release and restart. CONTROL_FEEDBACK_VERIFY and EXTERNAL_IMAGE_VERIFY also PASS. DLL remained unchanged. Temporary override removed in finally and absence verified.

This is scripted viewport input, not physical keyboard/mouse acceptance. It closes the existing conveyor routed check on V22b, not the six other retained-control routes. No product source change was needed.
