# Through-beam photoelectric sensor pair review

Review date: 2026-09-18

Disposition: **candidate only — not admitted to production**

Reference class: a floor-mounted through-beam photoelectric emitter/receiver
pair for unit-load conveyor detection. The assembly is deliberately
manufacturer-neutral: two independent housings, optical faces, status LEDs,
adjustable brackets, posts, base plates, pigtails, and visible M12-style cable
terminations.

## Defects found and corrected

1. The original review renders were generic four-angle captures and did not
   expose source topology, scale, optical alignment, or the underside.
2. The first dedicated wireframe pass rendered blank because review clones
   inherited hidden-render state. The renderer now explicitly enables each
   review clone before the wireframe pass.
3. The first scale view used an unlit default material and blended into the
   floor. The reference cube and floor now use explicit Principled materials.
4. Both pigtails previously stopped as bare dangling cable ends. The source,
   delivery GLB, and collision asset were rebuilt with visible M12-style
   connector/coupling terminations at the cable ends.

## Full-resolution inspection

| View | Result | Finding |
| --- | --- | --- |
| `hero.png` | Pass for candidate | The unsupported pair reads as an emitter/receiver optical detection station without labels. |
| `emitter_face.png` | Pass for candidate | Emitter optical face, bezel, LED, bracket, post, cable gland, and beam origin are visible. |
| `receiver_face.png` | Pass for candidate | Receiver-side housing and opposed optical path are visible; the body is not confused with a single diffuse sensor. |
| `rear_cable_mount.png` | Pass for candidate | Both cable glands and M12-style terminations are visibly continuous and mounted. |
| `underside.png` | Pass for candidate | Base plates, post supports, cable routing, and underside housings are inspectable. |
| `side_alignment.png` | Pass for candidate | Both optical faces are coplanar at the same beam height; the dashed witness is an optical path, not a solid member. |
| `scale_reference.png` | Pass for candidate | One-meter reference cube gives direct scale context for the approximately one-meter sensor elevation. |
| `wireframe.png` | Pass for candidate | Source-mesh review shows smooth circular optics and physically continuous housing/bracket/post/base construction. |
| `blind_review.png` | Pending independent review | Context-free image is prepared; no self-score is substituted for an independent family identification. |

## Production blockers

- Independent context-free recognition must identify the equipment family and
  record a confidence of at least 0.80 without catalog name/category context.
- The source and Godot delivery model must be inspected again after any future
  changes to sensor mounting, cable routing, or beam behavior.
