# Original three-carton grouping installation

Scene 49 uses these original Blender sources and matching glTF deliveries.
They replace its disconnected substitute roller conveyor, pallet receiver,
spacing sensor and mislabeled stop. Other catalog assets remain unchanged.

Rebuild with `tools/modeling/build_package_grouping_installation.py` and the
pinned Blender, setting `RUNGPROOF_PROJECT_ROOT` to the RungProof Next root.
Run Godot import after exporting. `.gdignore` excludes editable sources from
the Godot runtime import.

Units are metres. Blender X is travel, Z is height and Blender +Y becomes
Godot -Z. Infeed rollers span X -5..3.2; receiving rollers span 3.2..7.
Both tops are at 0.9 m. A narrow bridge spans the joint at X 3.2. Roller
centres are at height 0.82, radius 0.08, with axes along Godot Z.

The stop root sits at world X 3. Its gate bottom/top are 0.95/1.25 m,
with 1 m upward release travel. The rod's upper face stays at 2.56 m;
its lower face follows the gate's top. Two floor-mounted guides and a
header support the gate/actuator assembly. The renderer stretches only
the telescoping rod and translates the gate from captured authored homes.

Capacity three, speed 0.4 m/s, one-second retraction and the accumulation
positions are original offline training choices, not manufacturer data.
The plant prescribes carton/roller travel and excludes slip, contact forces,
traction and collision dynamics. PLC logic owns counting and release; raw
feedback comes from actual positions. This is not hardware safety or
commissioning evidence. See scene help and the multi-angle review matrix.
