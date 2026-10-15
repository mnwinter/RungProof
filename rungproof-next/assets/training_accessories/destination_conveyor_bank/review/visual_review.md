# Belt conveyor visual review

Review date: 2026-09-17

Disposition: **candidate only - not admitted to production**

Reference class: 600 mm wide, 6 m long, low-profile slider-bed conveyor for
light/medium unit loads. Proportion and mechanism decisions were checked
against the mk GUF-P 2000 product family and general conveyor pulley guidance.

## Defects found and corrected

1. Replaced disconnected top/return belt slabs with one continuous belt loop
   tangent to both end drums.
2. Removed the incompatible heavy bulk-conveyor pulley/drive arrangement.
3. Removed carrying rollers from the slider-bed path and added a stainless
   support bed plus correctly located return rollers.
4. Replaced the oversized floating drive hood with a direct hollow-shaft
   gearmotor, output flange, coupling guard, mount plate, and fasteners.
5. Moved the E-stop and pull switch completely outside the product envelope and
   onto visible fixed brackets.
6. Moved the pull cord below the conveying surface, added guides, a tail spring,
   and a visible fixed anchor.
7. Connected the tail take-up screws physically between each end plate and
   sliding bearing block.
8. Mounted the junction box to a support and routed power/control cables through
   the underside tray using smooth service bends.
9. Added longitudinal support bracing, belt splice detail, motor nameplate,
   reducer flange bolts, and mount fasteners.
10. Replaced the overexposed render setup with neutral multi-view inspection
    lighting.
11. Replaced the mirror-like black belt response with authored base-color,
    roughness, and normal maps, continuous path UVs, and a matte rubber shader.
12. Lowered the E-stop station below the belt and added a continuous visible
    drop bracket so it reads as fixed hardware from both sides.
13. Added topology, one-meter scale, motion-witness, and native Godot runtime
    evidence. The first runtime capture exposed and corrected an invalid
    below-floor camera conversion and excessive framing distance.

## Full-resolution view inspection

| View | Result | Inspection finding |
| --- | --- | --- |
| `hero.png` | Pass for candidate | Complete silhouette visible; no cropped drive or unsupported equipment. |
| `operator_side.png` | Pass for candidate | Pull cord, switch, supports, and junction box are outside the belt envelope and visibly mounted. |
| `drive_detail.png` | Pass for candidate | Drum, reducer, motor, flange, guard, mount, nameplate, and cable service loop read as one drive package. |
| `tail_detail.png` | Pass for candidate | Both take-up screws connect end plates to bearing slides; continuous belt wrap is visible. |
| `underside.png` | Pass for candidate | Return path, fixed controls, cable routing, supports, and bracing are exposed for inspection. |
| `end_alignment.png` | Pass for candidate | Belt/drum alignment and drive-side clearances are readable without hiding depth errors. |
| `blind_review.png` | Self-review only | Equipment reads as a supported flat-belt conveyor without labels, but independent scoring is still required. |
| `state_stopped.png` | Pass for candidate | Review-only carton establishes the initial material-flow position. |
| `state_running.png` | Pass for candidate | Carton advances 1.30 m, matching 0.65 m/s over the two-second evidence interval. |
| `scale_reference.png` | Pass | Ten 100 mm bands prove the one-meter reference beside the 1:1 asset. |
| `wireframe.png` | Pass | Source-mesh edge overlay shows continuous belt topology and smooth cylindrical segmentation without rendered facet lines. |
| `godot_runtime.png` | Pass for candidate | Native Vulkan/Forward+ capture confirms GLB import, materials, framing, and the complete assembly in Godot. |
| `godot_state_stopped.png` | Pass | Native capture with `RunCommand=false`; the review witness remains at its initial belt position. |
| `godot_state_running.png` | Pass | Native capture after the ramped controller advances the witness; Godot binds seven rotating nodes and one private UV-scrolling belt material. |

## Production blockers still open

- independent context-free recognition review and confidence score;
- final manufacturer-neutral detail pass for labels, cable clamps, and service
  access without copying proprietary geometry.

Topology, material, scale, and animation flags are reviewed. The catalog entry
remains quarantined until every blocker is closed.
