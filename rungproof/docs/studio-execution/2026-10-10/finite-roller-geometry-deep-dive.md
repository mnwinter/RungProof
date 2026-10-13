# Finite pallet roller geometry deep dive

Status: read-only proposal, 2026-10-11. Current delivered GLB accessor bounds and node transforms inspected offline; no engine import, native placement, controller execution, or rendered acceptance performed. Historical asset review claims are background only. No asset/source/controller changes.

## Reuse and measured evidence

- Conveyor ID `material-handling.pallet-roller-conveyor.1000x4000.v1`, GLB `assets/material_handling/pallet_roller_conveyor_1000x4000/delivery/pallet_roller_conveyor_1000x4000.glb`, SHA256 `299D871812543748195E8463AD56E5AA88C973EA1DD74D14EA470E4D7A60A75F`.
- Smallest load: existing empty timber pallet `loads.pallet.gma-48x40.v1`, GLB `assets/factory_kit/gma_wood_pallet/delivery/gma_wood_pallet.glb`, SHA256 `48EC39F71FEDA6C8D42C87A618DC34BDFF42716111729AF7B8085EA482C09698`. This is a pallet transport lesson, not a fictitious boxed load.
- Both deliveries have flat scene-root nodes, no child transforms. Calculations transform each POSITION accessor min/max corner through node quaternion/translation; not catalog nominal bounds. Full imported vertex/native bounds remain a gate.

| Delivered datum (Godot/glTF axes, metres) | Offline result |
|---|---|
| Roller count / pitch / radius | 31 / .128 / .038 |
| First / last roller centres | X −1.92 / +1.92, Y1.055, Z0 |
| First/last roller X surface extents | −1.958..−1.882 / 1.882..1.958 |
| Roller carrying crown / face | Y1.093; Z−.51..+.51 |
| Six support foot bottoms | Y.040; stations X−1.62,0,+1.62 |
| Pallet underside | Y0; three runners X0,±.48, each .145 wide and Z−.5..+.5 |
| Pallet deck footprint | X−.6075..+.6075; Z−.5..+.5 |
| Pallet top-board range | Y.2475.. .3225 (nail maxima need native union measurement) |
| Imported sensor/reflector centres | X−1.42,Y1.19; sensor Z+.755, reflector Z−.755 |
| Imported sensor lens centre | X−1.42,Y1.19,Z+.822 |

Catalog conveyor envelope 4.2×1.35×2.25 and connector points `[-2.05,0,1.1]` are descriptive metadata, not valid carrying-height/contact datums. Catalog pallet height .3 differs from measured board top .3225, so reject nominal bounds for seating.

## Smallest geometry contract

1. Unit-scale conveyor root Y−.040 grounds delivered feet; measured carrying crown becomes Y1.053. No model mesh rewrite. Seat unit-scale pallet root Y1.053 using measured bottom=0. No .65 carton assumption or inherited belt UV movement.
2. World-X translation only. Use negative-X travel to preserve existing sensor X−1.42 and its supported cable/brackets. Conservative full-pallet footprint remains inside outer roller centre range with .05m end reserve: centre X∈[−1.2625,+1.2625]. Reset at +1.2625; wrong program stops physically at −1.2625 while retaining true command. Recompute limits from imported actual bounds; numbers are proposed checks, not hardcoded proof.
3. Roller pitch .128 is less than each runner width .145: with the above conservative endpoint reserve every runner should always overlap a roller centre. Native swept-contact test must prove actual runner/roller contact throughout travel, not just total AABB containment.
4. Width fits: pallet Z±.5 inside roller face Z±.51. Delivered guides near Z±.585 require native inner-face and pallet-side clearance measurement; do not claim available clearance from catalog width alone.
5. Whole-cell XZ translation atomically includes conveyor, pallet, operator control and sensing references. Ground Y, scale, yaw, relative sensor/load placement and roller radius locked. Reset/undo/open restore initial pallet and roller home transforms; no hidden authored scene runtime.
6. Reuse a real existing momentary operator assembly, outside measured drive/sensor/service envelope with ≥1.2m clear standing/approach route. Exact operator pose must be chosen after native total-drive AABB and camera review, not inferred from 1m roller width.

## Two critical optical issues before implementation

**Lens faces outward.** Lens centre Z+.822 is farther from bed than sensor body Z+.755, while reflector is Z−.755. The delivery cannot honestly be described as an inward optical ray merely by drawing a beam across it. Small bounded installation correction proposal: group sensor body+lens about the body centre, yaw180°, keep bracket and cable junction fixed, so lens moves inward to approximately Z+.688; construct sensing ray between the corrected lens front and reflector inner face. Require native bracket/cable alignment and visual proof. If correction is not approved, explicitly scope feedback as symbolic occupancy; no claim of physically mounted optical sensing.

**Pallet is not a solid rectangle at beam height.** Grounded optical height is Y1.15, .097 above pallet underside. This crosses separated pallet block columns (bottom runners top .070; deck starts .2475), so full pallet AABB intersection falsely fills real gaps. Feedback must derive transformed actual solid block/board/runner mesh bounds intersecting the physical ray. Empty-pallet beam may clear between blocks during a wrong run; that is expected geometry, not sensor chatter to suppress. First stop contact remains a meaningful trial, with native next-scan stopping distance checked against actual block thickness. Do not silently substitute continuous envelope occupancy or invent a filled load.

## Existing hooks and clock risks

- `src/App/RollerConveyorController.cs`: radius .038 agrees offline delivery; local-Up rotation matches exported roller quaternion X90° (local long axis maps worldZ). Existing acceleration .60m/s² means signed speed ramps; do not translate pallet at a separate instant .45m/s while rollers ramp.
- `src/Workspace/WorkspaceSignalRuntimeAdapter.cs` attaches this controller but currently declares output feedback AUTHORED ONLY. Catalog photoeye metadata is not a functioning plant relation.
- `src/Scenes/SceneSimulationRuntime.cs` can drive roller controllers via the accepted bridge. New explicit finite-roller relation must disable autonomous physics after Ready and use the same accepted scan to advance both roller angle and pallet displacement from actual signed speed. Root/engineering owns choice of instant versus ramped policy; either must be explicit and tested.
- `SceneComposer.MultiConveyor.cs` measures roller crown for handoffs, providing reusable transform/AABB patterns; it does not prove this new finite workspace support or occupancy.
- Keep original static E-stop mesh noninteractive unless separately bound and disclosed as ordinary simulator input; do not imply a safety function.

## Required verification and visible acceptance

Native: exact31 roller bindings/radius/axes; all feet ground; measured pallet bottom/crown and side clearance; dense swept runner bearing/contact; inward lens and opposing reflector; actual solid-only sensing including gaps; signed ramp/motion/angle consistency; no autonomous ticks after Stop/Reset; finite terminal support; translated/undo/saved joint reopen identity; correct/wrong/invalid draft behavior; single accepted-clock provenance and observer-on/off trace equality.

Actual full+split rendered views: pallet clearly seated rather than hovering; visible roller rotation and moving pallet; drive/guards/supports preserved; corrected sensor mounting/beam visible; operator standing front clear; objective/editor/feedback readable. Reuse original blue steel, galvanized rollers and warm timber contrast; restrained warm-gray floor and purposeful logistics setting only after the material gate is resolved. No new assets, Blender, broad material propagation, striped/grid backgrounds, or oversized blank pad. Current human material approval remains open.
## Studio meeting addendum — acceleration/coast feasibility

Read-only critique: moving the beam upward into the empty pallet top deck does not provide a continuous X target. Current delivered top boards occupy local Y.2475.. .3225, grounded world Y1.3005..1.3755; midpoint1.338 is an interior proposal, whereas +.27 above seat gives1.323 (also interior offline). Native vertex/ray checks must select the actual interior, never nominal catalog height. Each board is only .135m along travel, at .18m pitch, with .045m open gaps. Blocks at the original grounded beam height are .155m along X. Neither is long enough to assume a .45m/s stop will remain blocked through deceleration .60m/s²: continuous coast .16875m plus next-scan travel .009/.018/.045 at20/40/100ms exceeds the same target. Discrete MoveToward integration must be tested, not replaced by the continuous bound.

Preferred smallest distinct roller lesson for feasibility experiment: prescribed .30m/s with the existing .60m/s² ramp and accepted-clock coasting, original beam height after inward lens correction, sensing actual pallet block solids. Continuous coast .075m plus one100ms travel .030m is less than .155m block width; this is a screening calculation only, not a native pass. Observer requires PLC command off on the next accepted scan, then waits for actual speed zero before the .5 accepted-second settled hold. No latch, fake load, invisible filled rectangle, or command-based speed-zero assertion.

If .45m/s is mandatory, options are explicit instant kinematic simplification (clearly disclosed) or measuring the existing palletized-case asset and raising sensor to a genuinely continuous case-body interval. That existing asset is in candidates as `loads.palletized-cases.gma-48x40.v1`; its real footing/side clearance/body union remains unmeasured here. Do not silently substitute it. Raising current lens into top-plank band also moves its bracket bottom above the supporting frame: it needs a connected extension and genuine cable endpoint extension, not merely sensor Y offset. Original-height corrected sensor avoids that extra intervention.

Unverified: imported native vertices and optical normal; sparse pallet solids/rounded edges; finite swept contact and guide clearance; actual discrete20/40/100ms ramp/coast and settled beam hold; complete native reset/move/undo/reopen; actual full/split visual readability. Meeting proposal only; no edits to production source, models, materials or controllers.
## Optical path blocker discovered from actual geometry render

The v2 optics render is framed, but the sensor ray cannot be accepted as optically feasible. Offline grounded guide range is Y1.105..1.165 at Z±.585; proposed ray Y1.150 passes through both stationary metal guides. The original sensor bracket is a solid box spanning approximately Z.585.. .745 and Y1.020..1.220, placing the corrected inward lens/ray inside its metal envelope as well. These are stationary hardware concerns, distinct from intentional pallet occlusion. Prior wood-only triangle checks and signed/contact checks did not test clear optical line of sight.

Root authorizes a red native-v5 stationary-triangle audit before any new mounting proposal. It records actual blocker names, world intersection coordinates and bounds against measured lens/reflector endpoints. Expected failure is a qualification blocker, not a request to weaken the test. No transparency, fake beam latch, guide cutout, sensor raising or asset rewrite is authorized in this step. Earlier passes remain qualified only for their bounded roller/contact/wood sensing checks.
## Alternative B — existing independent through-beam station (read-only proposal)

Native-v5 is genuinely blocked: both `FRAME_L_guide`, `FRAME_R_guide`, `SENSOR_photoeye_bracket` and `SENSOR_reflector_bracket` intersect the ray. Native actual guide Y is **1.045..1.165** (corrects the earlier offline lower-bound estimate); bracket Y1.020..1.220. These failures remain preserved.

Inspected existing asset `sensing.photoelectric.through-beam.v1`, delivery `assets/scene_core/through_beam_photoeye/delivery/through_beam_photoeye.glb`, SHA256 `2E00147536EB3167FF759A8880FDFEC976DF6EDB5849208E13DAF39CADEFCD9C`. This exact asset is already used by the finite belt preset. Do not confuse it with `sensing.photoelectric.through-beam-pair.v1`, whose separate authored arrangement is unsuitable without another audit.

Delivered flat-node GLB accessor/transform datums:

| Part | Actual local datum |
|---|---|
| TX / RX feet | X±.11, Y0.. .05, Z+.62..+.82 / −.82..−.62 |
| TX / RX housings | X±.09, Y.80..1.08, Z+.62..+.82 / −.82..−.62 |
| TX / RX lens centres | X0,Y.94,Z+.619 / −.619 |
| TX / RX inward lens faces | Z+.612 / −.612, nominal ray axis world-Z |
| Bracket Z spans | +.665..+.775 / −.775..−.665 (outside the optical segment) |
| Original pigtail connector | Y.090.. .180; X.094.. .146 at Z±.72 |

Proposed isolated prototype only: station root X−1.20, Y**measured native plank interior minus actual lens-centre Y**. Current offline board interior midpoint is pallet seat1.053 + (.2475+.3225)/2 =1.338, suggesting rootY.398. Unit scale and zero yaw preserve opposed housing→lens facing. Native must derive TX inward direction from housing centre to lens centre (−Z) and RX (+Z), not absolute cylinder-axis dot.

Two measured-foot pedestals connect world ground to each foot underside at approximatelyY.398. Suggested footprint actual foot X/Z plus .04 total margin, .26×.24, entirely below the retained foot; no floating post, no raised original bracket retrofit. AtX−1.20 these sit away from the original opticsX−1.42; native complete mesh collision checks required. Housing inner Z±.62 clears old guide outer Z±.6025 by .0175 offline. Pedestals extend toward Z±.60 but stay below the conveyor frame; actual all-solid nonintersection must confirm, not AABB convenience.

Optical segment from TX inner lensface(−1.20,derivedY,+.612) to RX innerface(−1.20,derivedY,−.612) clears guide tops1.165 by approximately.173 vertically. New brackets remain behind their lenses, not in that segment. Existing terminated M12 pigtails move with the asset, preserving cable-to-housing/connector geometry. No electrical-network connection is claimed, no cable endpoints manufactured to imply one. KIN_beam_dash optical witnesses are diagnostic optical geometry, not stationary opaque solids; all metal/plastic stationary geometry must be triangle-tested, excluding only actual endpoint lens faces and explicitly nonphysical optical witnesses. Retain old delivered sensor hardware unchanged/nonfunctional rather than concealing the failed optics.

At this beam height, sensing uses actual top-board triangles, with real .045m gaps; no full-pallet fill. StationX−1.20 gives theoretical first leading board entry near centre−.5925. Screening worst100ms extra travel.045 + full-speed continuous coast.16875 gives settled centre about−.80625, roughly.45625 above conservative terminal−1.2625. Native actual bevel intersection, sampling phase and exact integral20/40/100ms must replace that estimate. Protocol remains beam EVENT→next accepted command OFF→speed zero→supported stationary.5s hold; no requirement for continued beam occupancy.

Mandatory prototype checks: both optical faces actually opposed; clear stationary triangle ray; pedestal/feet ground and connected; new tower versus guide/original mount/drive/all swept pallet solids nonintersection; full-scale runner/guide/contact proof unchanged; actual wood entry/gap and signed ramp/coast margin20/40/100; actual end-view optics and bearing renders. This proposal is not production authorization or visual/material acceptance.