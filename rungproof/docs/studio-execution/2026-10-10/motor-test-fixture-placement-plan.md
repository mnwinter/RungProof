# Motor test-fixture placement correction

READ-ONLY design before source edits during v3 capture. Actual v3 motor ENUM/STRUCT/array images show motors sitting directly on floor. Industrial cabinet heads are already mounted but that does not install the plant. All three scenes need credible motor test fixtures.

## Evidence / actual asset datum

SceneComposer.CreateControlledAsset uses drives.motor.ac-induction.v1, ContinuousRotation, TargetPrefix KIN_motor_, SpeedRpm1450, RotationAxis localX. Compose assigns the source root IDs/position/rotation/scale, then final context/control installations. SceneSimulationRuntime constructor records every direct equipment root's initial Position after composition; reset restores those positions. EquipmentMotionController Ready separately captures local shaft/fan target transforms and ResetMotion restores them.

Read actual GLB JSON/accessor bounds rather than catalog envelope: MOTOR_foot_L/R centerY.08, local vertexY[-.08,.08], so bottomY0. FootX[-.55,.55], Z[.15,.33]/[-.33,-.15]. Existing four MOTOR_mount heads lieX+/-.42,Z+/-.24,Y.17. KIN_motor_shaft origin(.88,.48,0), localX axis; shaft and cooling fan are the ONLY animated members. The stator/feet remain stationary. No nominal motor-scale change or root yaw is proposed.

Exact affected IDs: lab-10-04-motor-enum-state motor_0; lab-10-05-motor-struct-data motor_0; lab-10-06-ten-motor-array-startup motor_0 through motor_9. Their source BOOL/ENUM/STRUCT/ARRAY logic, numeric displays/unspecified-unit constraints, field root identities and existing UI controls remain untouched.

## Geometry contract

For each motor, obtain actual stationary foot AABB in process-root coordinates from its composed mesh transform. Validate two named feet/flat common mounting plane; reject unsupported tilt rather than applying arbitrary lift. Keep root X/Z/basis/scale exactly unchanged. Translate rootY by .72m minus actual foot-bottomY, so actual foot-bottom datum ends exactly at .72m. Compute rig widths from measured feet with explicit margin; do not place a slab through the stator/body or hide existing feet.

Single rigs10.4/10.5: visible steel mounting plate minimum1.65x.95m, thickness.06m, topY.72. Its plan fully contains both measured feet plus.12m each side. Plate centered on measured foot footprint, not shaft center. Four grounded steel legs .08square below the plate corners, feet .22x.22x.025m, leg upper ends exactly at plate bottom. Lower longitudinal/cross rails join the legs atY.18 and a rear frame header underplate joins their tops. A neutral dark coated steel frame and medium satin metal plate visibly distinguish rig from cabinet. Existing motor mount heads remain; add only original bounded underplate anchor/side-lug witnesses tied to their actual axes, no duplicate hidden bolt heads or decorative electrical cable pretending to supply the motor. Plate/feet touch at the measured datum; frame is disjoint from stationary motor body except intentional fastening contact.

Array10.6: preserve all ten X/Z centers and individual root IDs. Group the original two WORLD-Z rows0/2.5 from source with native measured tolerance; no arbitrary spatial clustering. Each row gets one continuous structural bench top/underframe spanning all five actual foot AABBs plus.20m ends, depth>=1.0m, topY.72m. Individual thin bolted mounting pads are flush/inset into this top, NOT a second coplanar overlay or repeated taller pedestal. Grounded bearing legs at each motor station (or each gap if measured envelope requires), continuous long rails and transverse braces support the row. No extra motor, coupling, conveyor, cable connected to nonexistent panel circuit or invented feedback. Two rows remain separated by their original2.5m center spacing; no walkway/maintenance-clearance certification.

Support nodes are unbound direct process children under MotorTestInstallation, with explicit installed motor IDs and actual top/foot/shaft datum metadata. No scene EquipmentIds/controller/binding/actions are added. Do not parent motor under rig: root addressing and runtime initial-position/reset ownership remain intact. ConfigureMotorTestFixtures(root,scene) runs only for exact three IDs, before ConfigureControlInstallation and runtime/Ready. Root coordinates the single Compose hook; owner edits only new SceneComposer.MotorTestFixtures.cs after freeze release.

## Required verification

- Exact12 motor roots retained; before/after worldX/Z, basis/scale and localKIN transform invariants. Expected rootY delta computed from actual stationary foot bounds.
- Measured foot bottoms equal actual plate/bench-topY within.001m; plate horizontal plan contains both feet; each leg/rail bears plate and grounded foot. No mounting slab penetrates stator, fan, drive-end shaft or control action target.
- Stationary foot/stator transforms remain fixed while actual accepted BOOL/ENUM/STRUCT/ARRAY output commands rotate shaft/fan; stop freezes; reset restores mounted roots and original localKIN state. Retain all existing authored native contracts; do not weaken cases to accommodate geometry.
- Array each independent ID motion remains independent; aggregate/legacy/group OR semantics unchanged. New geometry does not imply physical torque/current/startup timing.
- Recompute B from actual installed plant plus fullmotion and update environment/cabinet placement via existing measured composition flow. Preserve actual standing A, exact route sweeps and same ray-pick action IDs, no new colliders/picking surfaces.
- Fresh full/split views of all three AND close rig side views demonstrate actual plate-to-foot bearing, grounded frames, no motor loose-floor appearance and unobscured heads/labels. Current default camera may crop added bench height; revise presentation only if required by actual evidence, never declare success from source metadata.

This is an instructional mechanical mounting witness, not physical electrical/mechanical safety, commissioning, torque validation, guard compliance or live power wiring evidence.