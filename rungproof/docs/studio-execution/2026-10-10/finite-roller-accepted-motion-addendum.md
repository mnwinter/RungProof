# Finite roller accepted-motion implementation addendum

Status: READ-ONLY engineering breakdown. Native imported geometry qualification must pass before production model/schema/controller edits. Root/art own that experiment. No implementation or build has been performed here.

## Exact motion calculation

Use a positive transport-distance coordinate for−worldX movement, with speed magnitude v in[0,0.45]. Accepted run command selects target0.45 or0; acceleration sign is sign(target−v)×0.60. For accepted duration dt, t=min(dt,abs(target−v)/0.60). Integrated drive distance is v×t+0.5×a×t²+target×(dt−t); final speed is target if reached, otherwise v+a×dt. If target=v, distance=v×dt. This exactly splits within-step arrival at cruise or rest and avoids the current generic endvelocity×dt approximation. Keep the existing generic controller approximation/defaults unchanged outside this preset.

Pure snapshot proposal: immutable RollerTransportState(SpeedMps,DriveSurfaceTravelM,ClampedLoadTravelM,AtTerminal). Pure Advance(state,acceptedSeconds,acceptedRunCommand,supportedTravelLimit) returns a new state plus DriveDeltaM and LoadDeltaM. Both are positive−X transport distances; actual model projects positionX=homeX−ClampedLoadTravelM and roller phase from SIGNED world/local measured axis drive delta divided by measured radius0.038. Do not guess axis sign from nominal Blender/Godot coordinates; imported geometry experiment determines it.

At the finite support limit, material distance clamps; drive surface distance and rollers continue under a true command. ActualSpeed denotes drive surface speed, not clamped pallet velocity. This boundary is an explicit educational transport limit, not a force/slip/mechanical stop model. Manual simulation Stop freezes both clocks/poses immediately and reports visual speed0; learner commandfalse decelerates with accepted steps. Reset restores initial model and captured local roller/root/pallet homes.

## Feasibility bounds

Full-speed coast is0.16875m. From the exact geometric leading-edge crossing to its first post-step physical observation, sampling overshoot is at most0.009/0.018/0.045m at20/40/100ms. From that first observed event, the NEXT accepted scan samples beamtrue and commits runfalse BEFORE advancing the deceleration step; the remaining coast bound is0.16875m, with no additional full-speed scan added. Total exact-edge-to-settle bounds are0.17775/0.18675/0.21375m. Keep exact solid edge and first observed event as separate reference positions. Upper-deck nominal leading encounter at beamX−1.42 puts center≈−0.8125; worst100ms settledcenter≈−1.02625 remains above proposed terminal−1.2625 by0.23625m. Block-height detection has a different actual first solid edge; the native experiment must calculate its exact reserve and prove every supported bottom runner remains borne by actual rollers. If that reserve fails, reject the proposed layout instead of correcting the learner or latching feedback.

## Dedicated passive observer

Observation carries immutable epoch/currentprogram/bindings/force flags, exact accepted scan/time/period, sampled/effective start and beam, appliedrun, actual integrated drivespeed/surface/loaddistance, actual current wood-ray beam, measuredsupport and terminal. Require known measured reset home, real request/release with continuing travel, first actual beam event, EXACT next accepted sampledbeamtrue/PLCrunfalse, bounded coast, then actual drivespeed0 and supported stationary hold for0.5acceptedseconds. Current beam may clear through genuine pallet gaps while the historical trigger remains in the trace. Result wording: decelerated after beam detection and settled; never stopped on a continuously blocked beam. Do not reuse the constant-speed finite-belt observer or change its semantics.

## Concrete implementation sequence after geometry PASS

1. Pure transport utility and pure dedicated observer/tests, with source geometry parameters passed rather than guessed.
2. Add accepted projection/reset seam to RollerConveyorController: capture actual roller homes inReady; ApplyPlantTravel signed distance/speed rotates only registered rollers; Reset restores homes/speed. Existing _PhysicsProcess delegates its already-calculated displacement to projection, preserving its behavior. Bounded preset disables callback afterReady/Run/Reset and verifies no drift.
3. Explicit mutually exclusive preset relation/schema/source replacement and imported actual-solid sensor projection. Cache mesh triangles; transform sensing segment into actual mesh-local coordinates and intersect triangles. Whole-pallet bounds are broadphase only.
4. Main accepted sample→PLC→commit→motion→sensor nextscan pipeline, actual user menu/emptydraft, joint persistence/cache/rollback; no extra timer.
5. Focused native correct/wrong/invalid/force/StopReset/translation and observerenabled/disabled trace parity, then actual same-binary renders.

Pure tests: acceleration0→0.45 in0.75s travels0.16875m; full stop0.45→0 in0.75s travels0.16875m; period partitions20/40/100ms produce equivalent final speed/travel under the same command-transition times; within-step target arrival; zero/negative/nonfinite duration rejection; commandfalse whilepartiallyaccelerated; finite endpoint leaves drive advancing and loadfixed; no autonomous mutation; observer exactnextscan and genuinebeamclear duringcoast, timeout/Stop/reset/force/mismatch retirement. Native tests separately qualify sampled sensor transitions, same accepted clock and actual mesh bearing/angle; pure analytic tests do not establish these.

Native v1 preliminary measurements reported by root (not yet accepted because its hash writer failed): first observed centers at20/40/100ms are−0.86375/−0.87275/−0.86375; settled centers−1.0325/−1.0415/−1.0325. Each event-to-settle delta is exactly0.16875m; proposed terminal reserves are0.230/0.221/0.230m. These are preliminary geometry experiment outputs, not qualified evidence or production implementation.
