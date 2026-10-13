# All77 native coverage review, V12

2026-10-11. Read-only source audit of Main.EnvironmentReview.cs, Main.ControlInstallationReview.cs and their geometry/environment producers. No test, engine, build, source edit, screen or input action. The six existing V12 gates already have independent terminal PASS evidence for exact DLL D6D35153A2116A0F7CD323B8C2CB6D627610AD0E1C3A167C9F8F669471EB158E. Root's dedicated environment run and expanded 154-frame review were pending when this audit was prepared; this report does not claim those results.

## Dedicated environment verifier: exact coverage

The actual dispatch flag is --verify-scene-environments, calling VerifyCatalogEnvironments (Main.cs:877–879). Optional --environment-evidence=<path> writes actual generated decorative mesh world AABBs and checked-opening/route counts. It is not an invented --verify-catalog-environments flag.

| Source checks | What they establish |
|---|---|
| EnvironmentReview.cs:27–34 | Exact equality of all catalog IDs with profile and art-direction ledger keys |
| :39–53 | Each actual composed scene has LocationArchitecture/purposeLayoutImplemented; decor/process are direct siblings, decor yaw zero; known collision/UI/motion node types excluded; decor does not enter the process subtree; ambient motion disabled |
| :55–61 | Upright decorative mesh world AABBs stay outside the supplied conservative protected process bounds; slabs below .05 m are exempt |
| :66–78 | Every DECLARED localOpeningBounds has at least 1 m local width, and generated decorative mesh AABBs do not occupy its interior with a .002 m inset |
| :79–108 | Every DECLARED localRouteBounds and axis-aligned localApproachPolyline sweep avoids visible decorative and process mesh AABBs at the inspected pose; polyline has at least two points and proposed sweep width is finite and at least1 m |
| :109–124 | Evidence records layout/location metadata, checked opening/route totals and generated decorative mesh AABBs |
| :127–138 | Removing decor preserves captured descendant process transforms and main camera transform; restores environment reference, key/fill colors/energy, floor visibility and floor material reference |
| :139–166 | One case per composed catalog scene, frame retirement between cases, final count and exit0 on success; exception exits1 |

These are useful passive-context and static separation invariants. They do not prove visual richness, convincing construction, material/texture quality, readable signage, unoccluded cameras, human reach, valid electrical installation or continuous machine clearance. A green ledger means actual construction exists with declared metadata; it does not independently judge whether a room is appropriate to its machine or visually repetitive.

ProtectedProcessBounds is built in Main.DemoEnvironment.cs:41–73 from currently visible process mesh bounds, selected EquipmentMotionController translation travel growth and per-profile MotionMargin. Cartesian gantry receives additional margin; selected motion kinds are explicitly enumerated. This is conservative presentation separation for those declared paths, not a discovered continuous envelope for every custom plant, rotating machine or moving product. Static route checks use the inspected actual process pose, not an exhaustive trajectory replay.

## All77 controls verifier: coverage and exclusions

--verify-control-installations visits every catalog entry unless an explicit --installation-scene-id filter narrows it. It compares current authored Simulation/config/equipment identities to the active definition, and declared equipment actions to actual interactor bindings. Selected mounted heads are exactly metadata kinds studentBench/operatorPanel; roof heads and machine-supported heads receive separate helper/industrial-mount checks, not inclusion by that filter.

The native V12 result independently read in industrial-mount-native-review-v12.md is PASS77 scenes/71 authored case scenes,163 scripted centroid picks,138 accepted button feedback cases,17 selectors,8 prerequisite-blocked actions,36 explicit projected indicators and21 bench lens/shelf checks. These totals are actual bounded cases, not a claim that each of77 scenes receives every type of test.

Actual controls source additionally checks sibling passive support, visible mounted heads, broad supported height/centre width, illustrative standing-access AABB against visible field/decor geometry, bench lens/tier intersection, selected head reset poses, configured lenses and motor fixtures. Compact hand-dryer has detailed measured width/row/contact/post/foot/readout invariants. Grouping includes a specifically labeled hypothetical90-degree door rectangle and authored carton translation envelope; those specialized checks are not generalized to all cabinets or plants.

Action coverage is deliberately bounded: retained field bindings are counted and skipped for strict adapted-head feedback; nonhead bindings are counted separately; multiple/channel lamp bindings and non-PLC-BOOL outputs are not forced by the generic output fixture. False→true→false projection checks are offline direct-output fixtures, not learner ladder validation. Authored local cases temporarily use runtime preview rules with UsesExternalClock=false, then restore the authored clock mode. They do not establish learner-program scan correctness for all77 scenes. Representative workstation uses an explicitly ephemeral verifier-only ladder; grouping and selected authored counter helpers have actual accepted controller scans. None installs a hidden default solution into other labs.

Current authored/config comparisons reload current source, so they alone would not detect a coordinated mutation to both current files and runtime. The separate industrial-mount verifier's immutable baseline/source-freeze checks provide the relevant independent preservation evidence for this candidate.

## Concrete missing invariants and reporting issues

1. **Opening/route checks can pass with zero declarations.** EnvironmentReview counts metadata but never requires an independently declared per-layout expected opening/route set or minimum. A missing route marker can silently remove its check. Open exhibition/roadway scenes legitimately need different expectations; require an exact per-purpose manifest rather than imposing a door on every scene.

2. **Routes are not proven to connect entrance to station.** Producers store localTerminalStandingRegion, localApproachReserve and other proposed reserves, but this verifier never reads these keys. Clear segment rectangles do not establish a route beginning at a real declared portal and terminating in the current operator standing zone. Record and verify endpoint/portal/terminal correspondence and mandatory connected route coverage if claiming circulation realism. Route presence itself is not physical clearance.

3. **Success text overstates runtime preservation.** EnvironmentReview.cs:157 prints runtime=unchanged, but the removal check captures descendant transforms/camera and global presentation references only. It does not compare runtime point values, controller snapshot, binding collections, lens materials, text or pause/force state. Narrow that success description or add an independent before/after state image before using it as full runtime non-intervention evidence. Existing six native gates remain their own bounded evidence; this is a concrete reporting mismatch, not a demonstrated current process mutation.

4. **Metadata validity is partial.** Sweep width is finite-checked, but polyline point coordinates are not explicitly finite-checked; opening/route AABB positions/sizes lack explicit finite/positive guards. Zero-length repeated segments are permitted. Current producers derive them from known bounded geometry, so no actual V12 invalid value is claimed. The verifier should reject malformed declarations rather than rely on AABB intersection behavior to expose them.

5. **Coordinate-frame assumption is implicit.** ProtectedProcessBounds is in Main's parent coordinates, whereas ReviewBounds(mesh) returns world coordinates. Current Main transform is identity as documented in AttachDemoEnvironment, making the current comparison consistent. EnvironmentReview does not assert that assumption or transform ProtectedProcessBounds to world before comparison. A future translated/rotated Main would make this comparison unreliable. This is a latent source limitation, not a proven current failure.

6. **No comprehensive construction/legibility invariant.** Neither environment nor general controls verifier checks load-bearing chains for every architectural member, all cabinet head extents/contact, display/button ink containment or native glyph size, material contrast/texture variation, roof completeness, camera obstruction or required scene-specific signage. The separate mounting2205 checks and the16-scene/52-image review closed their bounded device issues, including chain readability, but do not mechanically or visually certify every other architecture/label. New154 actual full/split frames need individual realism review; a contact sheet or green native result cannot replace it.

7. **Behavior exclusions are known-type checks, not a universal passive-node allowlist.** EnvironmentReview forbids listed collisions/UI/EquipmentMotionController/ConveyorController and disables the known ambient motion. It does not inspect every attached script or arbitrary future node subclass. Current source factories are intended passive builders; no unexpected behavior is demonstrated here. Avoid interpreting this list as proof against any future injected behavior.

No current fatal geometry/runtime source defect was established by this read-only audit. The concrete findings above are coverage gaps, a reporting overstatement and an identity-transform assumption. Their presence prevents overclaiming realism or full runtime invariance from the dedicated environment PASS; they do not negate already recorded exact-candidate bounded gates.

## Realism versus curriculum and remaining status

The existing69 fresh tag-only/factory-authoring limitation describes learner curriculum/program scaffolding. It is not evidence that those69 scenes lack room geometry, materials, mounted controls, functional modeled equipment or realism; do not use it to dismiss or halt the all77 realism review. Conversely, realistic native rooms and correct picking do not finish their learner curriculum. Keep these acceptance axes separate.

Root's dedicated environment run remains pending here and should record all77 terminal cases, exact candidate hash/exit, actual evidence counts and any route obstruction without relabeling it as a general art failure. Expanded154 full/split images require actual individual root/Dot inspection for readable controls, convincing support/location context, overlap and visual variation. Missing source invariants must remain explicit even if that run passes.

Wider75-scene material rollout remains HELD under its separate exact-two human gate. Handle-is-not-initialized root-cause diagnosis OPEN. No main-screen/input takeover, publication, live PLC, continuous physical clearance, commissioning or whole-product approval follows from these source/native checks.
