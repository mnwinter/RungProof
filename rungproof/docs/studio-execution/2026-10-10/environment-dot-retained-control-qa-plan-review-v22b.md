# Dot retained-control QA plan review V22b

## Decision

APPROVE bounded diagnostic implementation of six exact routed head targets described in seven-retained-control-qa-mapping-v20b.md. Existing conveyor E-stop semantics already have current V22b native scene-controls evidence; do not duplicate that stop/restart suite. This is a remaining picking/visible-feedback verification slice, not a product behavior or camera change and not blanket all77 completion.

Read actual authored JSON for gallery, drill and crossing, SceneControlInteractor.cs and Main.UserReadoutCapture.cs/Main.NumericCycleInputCapture.cs. The existing ordinary route uses Viewport.PushInput with real frames, the normal aperture and unchanged FOV. Existing picker evaluates only visible declared control meshes and does not establish non-control occluder visibility by itself: actual rendered target pixels must therefore be reviewed.

## Exact target contracts

| Scene / equipment | Target and authored accepted effect | Required bounded evidence |
|---|---|---|
| equipment-gallery / gallery_switch | KIN_pushbutton; toggle-gallery; gallery_animation false→true→false | Actual routed press/release, transient local cap feedback and authored home; fixed supporting root; normal Reset from true. |
| equipment-gallery / gallery_estop | KIN_estop; stop-all | First enter active gallery state through its normal switch route; click stop head and prove exact safeState gallery_animation=false, industrial_fan_run=false, emergency_stop=true, supported head pulse/home. No invented latch, estop_ok or safety rating. |
| equipment-gallery / gallery_selector | KIN_selector_handle; toggle-gallery | Actual false→true→false command; initialPosition=2 remains the supported pose unless existing adapter demonstrably changes it. Do not require a selector_position point or a detent rotation absent from source. |
| lab-2-16-safe-drill / left_hand_button | KIN_pushbutton; toggle-left-hand; left_hand_request | False→true→false while right request stays authored; local cap pulse/home and fixed housing; Reset from nonzero. |
| lab-2-16-safe-drill / right_hand_button | KIN_pushbutton; toggle-right-hand; right_hand_request | Same separate route with left unchanged; supported cap pulse/home and Reset from true. |
| lab-5-07-pedestrian-crossing / switch_2 | KIN_pushbutton; toggle-crossing_request; crossing_request | Exact authored pulse sets crossing_request true through press/release; stopped clock-held state retains request until normal Reset from true. Blank/stopped authored outputs remain unchanged; do not demand traffic phase outputs. |

## Implementation constraints and acceptance

1. One finite QA-only partial and minimal capture hooks/argument whitelist. Exact scene/equipment/action/type/initial-value guards; fail unsupported target. Preserve authored definitions and product picker/camera/runtime.
2. Begin normal FrameComposition and normal Reset. Orbit/pan/wheel only through existing ordinary mouse events; no direct camera transforms, FocusOn, FOV override or diagnostic station-view mix. Process frames between navigation stages, press/release and feedback observation. Finite event/time caps; capture countdown held before first await; catch diagnostic exceptions and quit failure.
3. Project actual head mesh bounds, require full visible head inside SceneViewportRect with margin, positive front-facing projection and actual target TryPick identity/action. Where face wording exists, require the complete identification text extent and review actual pixels for comfortable readability. Use rendered head visibility, not a broad equipment-root center or union AABB. HUD negative point must not dispatch an action.
4. Every routed click snapshots authored points and fixed housing/support transforms. Allow exact authored action effects: target toggle or pulse, and gallery stop-all safeState keys gallery_animation, industrial_fan_run, emergency_stop; retain all other points apart from measured intentional gallery clock-derived values. Gallery's intentional animation can move exhibition kinematic members: exclude only known source-driven members during its active interval, never blanket-ignore all node changes. No PLC solution injection or evaluator/output writes. Gallery fixture activation is explicitly QA provenance, not a learner result.
5. Observe actual asynchronous pulse through normal frames; SceneControlInteractor has a 0.28-second transient. Snapshot movement during pulse and authored home afterward; a single late snapshot is insufficient. Selector has no KIN_pushbutton/KIN_estop feedback target and must remain fixed if its current policy does. Normal Reset from true is substantive; do not reset only an already-false state.
6. Write per-target JSON with initial/navigation/click/release/home/reset sequence, exact points/poses/target projection/aperture and source/binary identity; save actual PNG after a defined phase. Gallery switch/E-stop shared frame and selector separate frame, drill pair and crossing field station are sufficient only if all target heads/words are actually unobstructed. Individual additional target frames are acceptable if shared framing cannot qualify both.

## Remaining objective boundary

This closes six precisely missing head routes, complementing current conveyor evidence and existing direct-action runtime suites. It does not require repeating traffic timing, drill process/safety preconditions, all gallery clocks, numeric cycles or all154 material frames. Ordinary discovery, physical keyboard/mouse interaction, unsampled views/DPI, real two-hand safety and commissioning remain separate. Any newly demonstrated camera/occlusion/picking defect must be reported before a product repair is proposed. Material rollout is human-approved; intermittent Handle root cause remains OPEN.

## Source-contract precision addendum

Checked action objects after the equipment mapping: crossing action name contains toggle but its actual type is pulse. It must never be tested as a repeated toggle. Gallery stop-all updates all three exact safeState keys, including emergency_stop=true; this authored simulation flag is supported and differs from inventing an estop_ok/latching/safety-rated contract. These corrections supersede any abbreviated table wording.

