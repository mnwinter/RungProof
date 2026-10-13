# Finite ordinary camera capture hooks

Read-only evaluation of root's proposal; no source edits/build/UI/engine. Requires Dot design review before implementation. Scope: EV and Chicken scripted in-app navigation from ordinary framing, not physical input/usability approval.

## Existing ordering verified

Main.cs:4495 decrements capture frames; at remaining1, line4508 FrameComposition sets normal camera/LookAt and calls CaptureCurrentView(center), then PrepareReadoutPresentationCapture, then ApplyControlInstallationCaptureView and typed detail framing. Saving occurs on the next process frame when counter reaches0. This is the correct initial seam, but a navigation routine cannot simply await frames there: capture would continue/decrement/save, or reenter normal framing and destroy the routed-input result.

Main._Process at4464 also reframes when actual aperture differs from _lastFramedAperture. Initial FrameComposition synchronizes that value; while navigation runs, an aperture change must cause explicit failure/restart from normal frame, not unnoticed programmatic reframing. SceneCameraController.ApplyView updates H/V offsets after each routed event using current provider/distance. Existing normal FrameComposition uses ChallengeSceneAperture; controller provider uses raw shell SceneViewportRect. For exact ordinary EV/Chicken operator views no challenge should be active; assert these rectangles agree before navigation. Do not generalize this test to challenge layout without resolving that difference.

## Smallest proposed integration

One new Main.CameraReadoutNavigationReview.cs partial owns pending/completed flags, async bounded sequence and sidecar. Root adds one opt-in capture argument and capture tool forwarding, plus these narrow Main.cs seams:

1. Add navigation pending to capture-countdown guard before4495. This pauses screenshot countdown only while real input events/ProcessFrame waits execute; it must not disable controller._Input or alter camera process routing.
2. At remaining1, once-only normal FrameComposition and existing readout preparation run. Then start the async navigation routine and return before any diagnostic station/typed detail setters. On subsequent frames, completed flag skips normal framing/preparation/diagnostic camera setters, lets countdown reach0 and saves the final actual navigated view. Reject simultaneous station-view/detail capture flags rather than silently mixing them.
3. Existing aperture-reframe block remains ordinary behavior. Record fixed initial aperture; if resize/UI change occurs during this finite sequence, mark trial invalid and avoid PASS. No camera setters introduced by navigation helper.

Preparation flags retain explicit unequal EV PLC projection/Chicken actual load-advance state provenance; navigation must not write field points or advance plant. Snapshot runtime/points/field transforms before/after, and keep clock stationary through the existing appropriate diagnostic hold seam when necessary. No external transport.

## Input/measurement contract

Use actual GetViewport().PushInput(event,true), not direct controller._Input calls: middle press/motion/release, right press/motion/release and wheel-up button events at actual aperture center. Buttons include viewport coordinates; motion includes Position and Relative. Await real ProcessFrame after stages. Input-start tests outside dock/HUD must leave camera unchanged; captured release outside and view-switch cancellation follow existing policy. No global OS input.

Finite event-derived targeting may read exact retained label/housing world positions as QA oracle, but changes target only through middle motion. Pan delta follows actual camera X/Y and max(distance*.0016,.002) scale; orbit follows actual .006rad/pixel; wheel scales .88. Log the sequence and capped retries; no fallback FocusOn/SetCamera/LookAt/FOV/offset assignment. Assert initial/final FOV unchanged.

EV: both exact A/B PLC kWh captions plus independent numeric values fully inside actual aperture, facing camera, with readable grouping. Chicken: actual screen and unchanged measured string inside aperture, facing camera. Measure actual configured label extents/line metrics (not individual glyph outlines), document minimum line-pixel height before acceptance and inspect actual final PNG. Suggested native1600×900 threshold12px/line is provisional for Dot, not already approved. Geometry occlusion still requires rendered inspection; projected bounds alone cannot prove visibility.

Save event trace, initial normal/final camera poses and ViewTarget, aperture, projected label rectangles/line-height, scene/DLL/engine/image identity and exact fixture scope. This can qualify sampled **ordinary app camera reachability through scripted input routing**, leaving physical keyboard/mouse ergonomics, discoverability, DPI and all77 hands-on input UNVERIFIED. No unconditional new all77 render batch or camera refactor required.
