# Demo 3 Engineering Frame Process: independent visibility review

Reviewed seven original native 1021×660 PNGs and recalculated SHA256 values. No UI, private runtime/camera queries, model execution or app changes by reviewer. Root supplied the input chronology; the originals independently show the visible outcomes. Correct scene title is **Demo 3 – Height Sorting Pusher Cell** throughout; PLC remains disconnected.

## Confirmed visible result

- `demo3-r1-rail-selected.png`, RUNNING scan 6: the original immersive lesson view shows the main conveyor, height portal, carton, pusher, side carrying surface and operator cabinet. They are present and renderable.
- After Engineering/Frame Process, `demo3-r1-engineering-home.png` scan 1465 and `demo3-r1-engineering-settled.png` scan 1751 show only a uniform dark wall-like surface in the central 3D aperture. No process or physical operator controls can be seen there. UI panels remain populated and scans advance; this is not a demonstrated crash or stopped controller.
- `demo3-r1-view-menu.png` scan 2638 visibly identifies the real View menu: Frame Process at approximately y=174 and Frame Whole Scene at y=194. Its background is still the empty/wall-like central view.
- `demo3-r1-frame-whole-scene.png` scan 3765 shows an exterior box/roof form in the Engineering aperture, not the process. A subsequent **Frame Process repeat**, scan 3831, again shows the uniform dark central surface. This is a stable repeated visibility failure in that route, not a single transitional frame.
- `demo3-r1-immersive-after-process.png` scan 3905 restores the visible machinery and physical controls in Immersive view. A working alternate view exists; it does not make Engineering's Frame Process result useful.

This independently supports a **confirmed rendering/framing problem in the tested Engineering route**, recorded as the requested **S2 candidate** for remediation triage. It is not a new sorting-logic, hidden-object or controller execution failure, and no all-resolution/all-scene claim follows.

## Narrow source-supported hypothesis

View commands call the same `FrameComposition` with `_frameWholeScene` set from the menu (`Main.cs:617–621`; `SimulatorShell.cs:3401–3419`). Process framing for Demo 3 substitutes its fixed low direction (8.8,0.72,5.2), FOV 35°, margin 1.17 and depth allowance 0.22 (`Main.DemoPresentationProfiles.cs:33–43`). The focus set includes the machinery and consolidated ControlInstallation, with environment architecture excluded from this fit (`Main.cs:4611–4626`).

Distance then scales inversely with the **actual unobstructed aperture**; Engineering's narrow central width can require a much larger distance along that nearly horizontal ray (`Main.cs:4725–4744`). The environment is a sibling built separately (`Main.DemoEnvironment.cs:45–58,80–93`; `Main.cs:4524–4528`). Neither this fit nor SceneCameraController's aperture offset update checks room-wall occlusion. It is therefore plausible that the Engineering fit moves the camera beyond opaque environment/material-entry architecture, with that surface filling the view. Whole Scene disables the demo profile and includes more process-root architecture, explaining why it can show the exterior envelope instead.

The originals do **not** expose actual private camera position, a ray hit, which wall is seen, or exact computed framing bounds. Projection-offset/bounds selection and wall occlusion are still competing parts of the source hypothesis. Do not claim camera coordinates or a uniquely proven root cause from these pixels. No implementation is selected here; preserve the useful immersive route and the authored room rather than treating invisibility as justification to remove geometry globally.

## Recalculated original hashes

| Original | SHA256 |
| --- | --- |
| demo3-r1-rail-selected.png | `70ebce7e172c5f820f1029b8f9a1e754e31a0603641c67e05d13c0e4a38f8fd6` |
| demo3-r1-engineering-home.png | `0a1b12e2bffb6bc2be5506e53fa133045c1a3064e56a929f03b7c9e39798b251` |
| demo3-r1-engineering-settled.png | `b0bc3fb62d709b46bd1fbb4940c72dc1652ec1960ccff8d1687362a95e33274a` |
| demo3-r1-view-menu.png | `926b4089f66183d0f6153f84d541cc06b899aa0174bfeaea63e715a9e91308ba` |
| demo3-r1-frame-whole-scene.png | `6bab8091c8878cbd29b51ded52f87338fce12d491e00e204912d0fc709a22b50` |
| demo3-r1-frame-process-repeat.png | `98f0f813f1d81a2ce61edff7b1a1069ce85e07902c732c2c591d59d499e48a08` |
| demo3-r1-immersive-after-process.png | `27c350779e200a542ca43868f0932cfabd6dedc87f5bbd0326c93d5aedcb35dc` |
