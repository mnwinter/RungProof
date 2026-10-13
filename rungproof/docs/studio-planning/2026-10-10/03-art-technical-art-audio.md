# Department 03: Art, technical art and audio

Planning recommendation to Dot, 2026-10-10. No implementation authorized. User approval remains required after departmental meeting and Dot review.

## Scope and evidence limits

Read root/rungproof AGENTS, current CONTEXT/README/PROJECT_INFORMATION, planning 00/01/02, focused Godot scene/camera/composer/runtime source, production/candidate catalogs, brand system, native approval register and dated release review. Applied architectural brainstorming; user's requested combined studio process governs approval sequence and document location. Inspected source only: no native launch, rendered qualification, build, asset generation, installation or PLC contact. Screenshots in historical asset folders are not fresh evidence of the proposed challenge layout. Audience/conveyor-first are provisional. The reference supplies scene/logic coexistence and approachable proportions, not permission to copy its visual identity.

## Findings

1. `rungproof-next/scenes/app/main.tscn` already uses plain dark environment color, neutral rough floor, key/fill lights and a Camera3D. Retain this background direction; do not introduce repeating grids, stripes, scan lines or graph-paper treatment.
2. `scenes/migrated/scene-1-conveyor-stop.scene.json` specifies perspective camera position [10.5,6.5,9.5], target [0,0.9,0], FOV 43 and actual conveyor/carton/photoeye/operator station/stacklight equipment. This is a scene contract, not a freshly accepted challenge composition. Scene metadata, including any `productionApproved:false`, cannot be overridden by individual asset approval.
3. `src/App/Main.cs:3943` FrameComposition fits transformed visible mesh bounds within the actual shell aperture, with extra envelopes for selected mechanisms. `src/App/SceneCameraController.cs` performs orbit/pan/zoom and projection offsets using perspective FOV/distance. Orthographic replacement is not a drop-in art setting; projection, framing and ray picking would require coordinated changes. Recommended slice retains perspective and evaluates authored angle/framing.
4. `src/Scenes/SceneComposer.cs:180-214` loads belt conveyor `material-handling.belt-conveyor.600x6000.v1`, preserves dimensional installation rules and adds ConveyorController. `:453` adjusts through-beam installation height/span without stretching sensor housings. `:731` maps ordinary boxes to corrugated carton. Changing geometry for appearance risks optical-plane/product alignment and established motion node contracts.
5. `src/Scenes/SceneSimulationRuntime.cs:1003-1092` advances the conveyor plant, publishes symbolic position/photoeye values, projects carton position and beam visibility from plant state. `ConveyorPlantModel.cs` includes finite sensor hold time after crossing. A blocked indicator must represent the declared feedback, not be independently guessed from a rendered overlap or a vanished beam. Geometry/feedback agreement must be checked at boundary states and documented if idealized.
6. `assets/catalog/production.catalog.json` labels conveyor, scene-core through-beam and operator stations approved with historical review IDs. `candidates.catalog.json` marks corrugated shipping carton candidate with incomplete topology/material/animation review fields. Older belt review text still says candidate-only. This is conflicting approval history requiring versioned reconciliation, not evidence that all deliverables are currently cleared.
7. `src/Catalog/AssetDefinition.cs` records source/delivery/collision/thumbnail, geometry/kinematic/signal and quality fields, but does not expose a complete ownership/license/provenance contract. Some candidate catalog entries have a generic visual copyright boundary; that statement is not legal clearance. `docs/DEMO_RELEASE_REVIEW_2026-10-10.md:183` explicitly leaves runtime/font/asset notices, reference provenance, employer IP and product-name review open.
8. `docs/brand/RUNGPROOF_BRAND_SYSTEM.md` selects canonical ladder-baseline SVGs, Navy/Cyan/Green tokens and technical typography. It also contains historical Fake PLC/runtime-safe wording; reconcile with current symbolic offline source and learning semantics. TRUE/active is not synonymous with correct/safe. Green command state must not imply passed challenge or physical protection.
9. Focused src/project.godot scan found no AudioStream or WAV/OGG/MP3 integration references. This supports proposing an optional audio layer; it does not prove no audio exists anywhere in ignored/generated folders. Sound is unnecessary for initial functional acceptance.

## Reuse and production inventory

Paths below are beneath `rungproof-next/`; delivery source is distinct from runtime composition.

| Item | Source/delivery family | Pilot decision |
| --- | --- | --- |
| Belt conveyor | `assets/material_handling/belt_conveyor_600x6000/source/belt_conveyor_600x6000.blend`, matching `delivery/*.glb` | Reuse; preserve KIN nodes/material animation and installed dimensions |
| Through-beam | `assets/scene_core/through_beam_photoeye/source/through_beam_photoeye.blend`, matching `delivery/*.glb` | Reuse; verify beam/head clearance and states in challenge camera |
| Operator station | `assets/scene_core/operator_pushbutton_station/` and `single_pushbutton_station/` | Reuse only actual mapped variant; ordinary Start/Stop labels must not imply certified emergency stop |
| Carton | `assets/scene_loads/corrugated_shipping_carton/source/corrugated_shipping_carton.blend`, matching delivery | Reuse subject to resolving incomplete/current visual approval |
| Canonical brand | `assets/brand/rungproof-mark.svg`, `rungproof-lockup.svg`, `rungproof-icon.svg` | Retain; no redesign needed for slice |
| Scene settings | `scenes/migrated/scene-1-conveyor-stop.scene.json`, `scenes/app/main.tscn` | Challenge-specific framing/presentation must preserve existing demo path |
| New visual work | Signal badges, focused label styles, hint/selection highlight, challenge thumbnail | Small coherent vector/UI set; final native rendering required |
| Audio | None required | Optional later short UI/plant cues after runtime/accessibility gates |

Do not restyle the full asset catalog. No new conveyor or diorama assembly is budgeted until native review identifies a concrete reuse failure. No manufacturer logos/exact model claims; generic references establish proportions, not redistribution rights.

## Direction and alternatives

A. Recommended: compact industrial diorama using current meshes, plain background, controlled perspective, matte materials and readable labels. Closest to current technical identity, least maintenance risk. Focus on carton trajectory, optical station and process controls, rather than more props.

B. Stylized asset rebuild: improves cross-scene consistency but adds geometry/kinematic/provenance work and requires requalification. Defer until pilot demonstrates a specific comprehension problem existing assets cannot solve.

C. Orthographic/pixel-art presentation: appealing reference resemblance but creates projection/picking work or a replacement art pipeline. Not required to deliver paired logic and machine learning. Consider separately after evidence, without copying another game's silhouettes, textures, logo or layout.

Material polish should preserve recognizable belt/steel/carton distinction, avoid glare/bloom drowning sensor lenses, and leave operational colors available for meaning. Do not exaggerate sensor geometry or alter product position to make a passing solution appear correct. Optional instructional overlay can highlight beam path with explicit simulated label; it must not be treated as real emitted visible light or a measurement tool.

## Requirements and acceptance

| ID | Requirement | Required acceptance |
| --- | --- | --- |
| ART-001 | Compact framing retains product path, sensor and process station throughout task | Native idle/running/blocked/Stop/Reset screenshots at approved viewport/DPI; readable, no clipping or hidden state |
| ART-002 | Signal distinction survives monochrome and muted sound | Command/feedback text+shape/state labels match completed scan observation; no color-only result |
| ART-003 | Visuals never award or cause success | Incorrect programs visibly continue/stop according to runtime; no animation/evaluator output writes; geometry projection agrees with recorded observation |
| ART-004 | Mesh hierarchy, installations, dimensions and KIN contracts preserved | Relevant geometry/motion regression plus actual native scene review; asset names/import changes assessed explicitly |
| ART-005 | Plain background and restrained lighting | No repeating-line treatment; sensor/product/material contrast evaluated in final renderer |
| ART-006 | Selection/labels remain useful at compact split | Anchored labels unclipped, no overlap with operator controls/ladder, critical label available through equivalent I/O text; labels stay legible after resizing |
| ART-007 | Motion is informational, discretionary motion suppressible | No auto orbit/shake/flashing celebration; pause/static evidence available through supported lifecycle; suppressing decoration never changes modeled plant/evaluation |
| ART-008 | Current assets have traceable review and distribution evidence | Manifest maps shipped hashes to source, creator, reference, rights/license, review and notices; unresolved assets block external distribution |
| ART-009 | Canonical brand preserved without misleading claims | Existing SVG identity; current source state and local simulated semantics; active green cannot imply verified/safe |
| ART-010 | Audio optional and subordinate to state | Muted default acceptable; all task evidence textual; canceled/failed test never emits completion cue |

Camera/art acceptance is native visual evidence, not numerical bounding boxes alone. Geometry assertions do not prove continuous clearance, physical safety or measurement fidelity. Agree viewport/DPI matrix with UX and hardware/performance targets with QA before locking composition.

## Interfaces and cross-department contracts

Engineering supplies copied final-scan observation containing scene/challenge/program/run identity, scan index, simulation time, typed relevant points, projected equipment state and lifecycle. Art presenter consumes it without generating commands. UX owns text placement and equivalent keyboard controls; Learning approves sensor/command labels and causal wording. Runtime owns motion/beam state, not decorative tween clocks. Renderer interpolation must never generate evaluation samples or unobserved signal transitions.

Proposed presentation metadata: focus equipment IDs, desired camera direction/FOV, conservative motion envelope, readable signal IDs and label anchors, optional cosmetic motion/audio preferences. These are proposed fields, not existing APIs; Architecture decides storage/version compatibility. Missing anchors should fall back to readable I/O text, not silently hide evidence.

Audio, if later approved, subscribes to meaningful transition events tagged with run/test identity rather than every scan snapshot. Avoid repeated start loops on every update; Stop/Reset/Cancel/scene switch dispose plant loops and queued cues. Only an evaluator's valid pass event can trigger optional completion sound. No simulated safety alarm claims or emergency noises required. Audio designer and QA verify volume/mute persistence and no change to simulation timing.

## Proposed work packages

| Task | Owner and dependencies | Deliverable/exit |
| --- | --- | --- |
| A1: audit exact shipped assets and historical approval contradictions | Art + Production; baseline freeze | Per-asset provenance/review disposition, candidate carton resolved or excluded |
| A2: native composition review after authorization | Art + UX; viewport target | Agreed conveyor angle/focus/lighting and documented defects; no wholesale rebuild |
| A3: presentation metadata and signal contract | Art + Architecture + Learning | Explicit ownership/versioning and final-scan label mapping |
| A4: focused camera/label/material polish | Technical art + UX; A2/A3 | Existing sandbox/demo preserved; challenge scene states readable |
| A5: native visual/accessibility/motion/performance qualification | QA + Art + UX | Screenshot/state matrix, monocrome/muted/reduced-decoration review and issues closed |
| A6: optional sound | Audio + Engineering; separate scope approval | Small original/licensed cues, event isolation, mute/lifecycle verification |

Uncertain focused person-days: A1 2-4; A2 2-3; A3 1-3; A4 4-8; A5 3-6. Required art/technical-art slice approximately 12-24 person-days, overlapping UX/QA/Production and not additive to their full estimates. Optional A6 2-5 days excluding procurement/rights delays. New asset rebuild or orthographic migration needs a separate estimate; six challenges cannot be estimated by multiplying one scene because each has different motion/readability risks. No calendar promise or budget price is established.

## Risks, decisions and meeting position

Highest risks: camera fitted to initial pose clips later path; visual labels read mismatched scan; green means both active and pass; carton candidate silently treated approved; historical generic/reference notes substituted for rights; scalable UI shrinks 3D evidence to invisibility; scripted polish moves a carton independently of learner logic; audio loops survive reset; promising reduced motion by stopping the plant while awarding travel success.

Human decisions for final authorization: audience, conveyor pilot scope, required supported viewport/hardware, art direction reuse vs rebuild, whether audio is in pilot (recommend no), distribution scope and rights owner. Native visual/accessibility and release rights gates remain unresolved until qualified.

Meeting recommendation: adopt reuse-first plain-background industrial diorama, retained perspective, text+shape signal evidence, no required sound, no rebrand. Obtain engineering final-scan observation and UX label/aperture contract before cosmetic implementation. Dot should reject any plan that calls current art visually accepted from static inspection, assumes all catalog approvals are rights clearance, or changes plant/mapping behavior for presentation. Department is ready to critique the combined plan; this report is not a final meeting signature.
