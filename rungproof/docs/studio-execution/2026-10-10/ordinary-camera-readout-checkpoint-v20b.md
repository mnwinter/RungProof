# Ordinary camera readout checkpoint V20b

QA-only changes add an opt-in finite input route to the existing capture wrapper. Product camera behavior and plant models are unchanged. Starting at normal scene framing, routed middle/right/wheel events reach both EV meters and the Chicken scale. HUD/uncaptured gestures leave the camera unchanged; navigation preserves stopped runtime points and field transforms.

Build passed with zero warnings/errors. Four native captures passed at 1600x900 (full scene and split for each lesson), terminal exit0 and no logged ERROR. Root inspected every actual PNG: EV 1.234/5.678 kWh and Chicken MEASURED 1.237 kg are readable and unobstructed. EV split clips some surrounding plate geometry, not target text. Sidecars retain exact event traces, initial/final poses and label extents. Font line box metric is not glyph ink height or accessibility certification.

Initial V20 evidence is retained: full views met12px line boxes but were too small; split correctly failed the diagnostic's mistaken IsLadderView guard, which also includes split. V20b uses visible nonempty aperture and18px font line boxes, preserving12px full-text margins. This correction changes only the QA path.

This establishes sampled scripted reachability for two lessons, not physical input, discoverability, all77 camera coverage or release approval. Material75 remains held for the pending human direction decision. Handle root cause remains OPEN; clean capture logs do not resolve it. No debugger installed or attached.

Images: .tools/studio-verification/v20b-ev-navigation.scene.png, v20b-ev-navigation.split.png, v20b-chicken-navigation.scene.png, v20b-chicken-navigation.split.png. Identity and metrics: ordinary-camera-readout-evidence-v20b.json.
