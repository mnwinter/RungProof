# RungProof Next architecture

```text
Godot desktop application
  |-- scene editor and catalog browser
  |-- PBR renderer, physics, animation, collision and picking
  |-- deterministic plant runtime using symbolic state
  |-- local IPC client
        |
Guarded PLC runtime process
  |-- operator authorization
  |-- exact profile and write-scope validation
  |-- watchdog and fail-safe disconnect behavior
  |-- Snap7 transport ownership
```

The Godot process is allowed to crash without owning or leaving a PLC transport
session behind. The guarded runtime remains the only module permitted to know
PLC endpoints and physical addresses.

## Asset storage

Each production asset has:

- editable Blender source under `assets/source/`;
- one optimized glTF/GLB delivery model under `assets/models/`;
- PBR textures under `assets/materials/`;
- authored LODs and collision geometry;
- connectors, kinematics, optional signals, and quality evidence in catalog
  metadata;
- a context-free thumbnail generated from the production renderer.

Passive equipment and structural parts normally have no signals. Actuated
equipment exposes symbolic commands and feedback only when behavior requires
them.

