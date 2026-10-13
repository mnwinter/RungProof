# Installation legend helper V13 — implementation handoff

Sole new source: `src/Scenes/SceneComposer.InstallationLegends.cs`, SHA256 **84A3E260C7034A0337EFCBBBE9FC7F6287BB814E9A69C66A624B3BA0732BEC0A**. Existing files untouched by this writer. Before-review snapshot: `rungproof-next/.tools/SceneComposer.InstallationLegends.before-review-v13.cs`.

Root integration: CreateSwitchAsset accepts scene.Id; replace its explicit face lookup with private `ResolveOperatorFaceLabel(string sceneId, SceneEquipment equipment)` (line53). After device packing/support creation and before template transforms, call private `ConfigureInstallationIdentification(Node3D root, SceneDefinition scene, Node3D installation)` (line230). Both live in SceneComposer partial and require no public API. Root owns integration, build and engine.

35 exact mapped missing switch faces/13 scenes from all77-operator-legend-plan-v13.md are retained; explicit configured faces precede map; genuine default START and emergency/rotary faces remain. Resolver checks switch type and exact config.action. Configure checks exact Simulation action ID/type/point. Action absence/drift throws instead of applying guessed semantics.

25 bench scenes and **91 retained devices** have explicit frozen identification text, equipment type/label/face/action and point-binding point/mode contracts. Bench action type/point also guarded. Numerical operands and outputs remain separate; selector labels show authored OFF/ON,OFF/FLASH,OFF/RUN,OFF/FAST,OFF/SLOW. Bay and beacon cycle labels show only actual indexes0/1 and0/1/2/3: no mode names inferred. Existing physical source labels and live readout values remain.

Coverage:
- equipment-gallery
- lab-2-01-workstation-call
- lab-2-02-dual-confirmation
- lab-2-03-service-marker-inhibit
- lab-2-04-two-station-call
- lab-2-05-bay-light-selector
- lab-2-06-ready-attention
- lab-2-07-dual-contact-permissive
- lab-2-08-inspection-vote
- lab-2-09-maintenance-beacon
- lab-2-25-inspection-toggle
- lab-4-01-press-count-lamp
- lab-4-02-counter-reset-lamp
- lab-4-05-dual-input-count-window
- lab-4-06-multi-press-confirmation
- lab-5-01-delayed-lamp
- lab-5-02-timed-lamp-off
- lab-5-03-rotary-flasher
- lab-5-04-alternating-lamps
- lab-5-05-variable-flash-rate
- lab-5-06-running-light-tower
- lab-9-01-sum-function
- lab-9-02-product-function
- lab-9-03-sum-and-counter-function
- lab-9-04-function-selector

Additionally exact panel pilot bindings: Lab2.10 collector_run_lamp→collector_run indicator→RUNNING; Lab2.12 top_limit_lamp→top_limit indicator→TOP LIMIT and bottom_limit_lamp→bottom_limit indicator→BOTTOM LIMIT. No chain lift labels duplicated.

Support owns `InstallationIdentification_<equipmentId>` plate and matching `_Ink` sibling, with equipmentId/sourceAction/sourceBindings/passivePresentation metadata; device gets installationIdentification association. No new CollisionObject/pick target, device child decoration, root pose, functional ID, binding or action. Measured retained head center in installation coordinates sets physical association; no dictionary-order LEFT/RIGHT labels. Plates attach against measured worktop/InstrumentTier front surface; bench .70m pitch preserved. Lower strip height .24m, upper .12m to avoid lower row silhouettes. Existing material appearance/footprint kept; no theme rollout.

Font64, pixel size .00065 fixed: readable intended glyph scale, word wrap to short authored lines; native actual font metrics must fit inset plate. Insufficient geometry throws; does not shrink to illegible glyphs. Upper-tier two-line fit and shadow/overlap still require actual operator/scene/split render review. These are source contracts, **not compiled, native or visual PASS**. Three current V12 operator PNGs inspected for actual defects: workstation-call, dual-input-count-window, sum-and-counter-function. They show illegible original microfaces and clarify shelf/device arrangement.

Required root QA:35 mapped+19 retained defaults+96 configured+emergency preserved; all25benches91associations present; no duplicate new support namespace; all bindings/config drift rejected; actual support contact/strip ink fit/no lower-head occlusion; selected3pilots correct functions; retained reset/pick/runtime behavior; copy/rebuild/template transforms carry passive supports once. Root sole build/engine. Material75 HELD; Handle OPEN.

