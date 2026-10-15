# Original parking-entry training installation

These four original models are scoped to Scene 48. Editable Blender sources
and matching glTF deliveries are checked in; they do not replace reusable
vehicle/road/barrier catalog assets in other scenes.

Rebuild with the pinned Blender and
`tools/modeling/build_parking_entry_installation.py`, setting
`RUNGPROOF_PROJECT_ROOT` to the RungProof Next directory. Import deliveries in
Godot after rebuilding. Sources are excluded from Godot import with `.gdignore`.

Units are metres. Blender Z is height and Blender +Y becomes Godot -Z, the
vehicle's forward direction. The vehicle origin is its floor contact datum;
four wheel bottoms sit at local height zero. The pad top is 0.04 m. The boom's
origin is its hinge, with the cabinet shaft at local height 1.08 m; its moving
Godot pivot rotates around Z. The arm extends 3.6 m along local +X.

The original offline route has two bays and prescribed position/yaw motion.
Capacity, dimensions, speed and timing are training assumptions, not OEM
data. Static wheels do not model steering, traction or collision physics.
Accepted controller/plant scans own movement and position-derived feedback.
See the scene help and multi-angle review for tested scope and native evidence.
