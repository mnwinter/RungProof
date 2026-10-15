# Production asset quality standard

An asset is rejected unless all gates pass.

## Visual identity

- A reviewer who is not shown the name, category, tags, or reference must
  identify the equipment family from the rendered image.
- Required confidence is at least 0.80.
- A wrong family identification is an automatic rejection regardless of how
  attractive the render looks.

## Geometry

- Silhouette, proportions, supports, connections, working envelope, and moving
  members match a documented real equipment family.
- Cylinders and curved castings use smooth shading and sufficient topology;
  polygon edges are never rendered as black facet lines.
- Fasteners and small details are modeled only when visible at inspection
  distance; normal maps carry repeated fine texture.
- Dimensions are authored in meters at 1:1 scale.
- Belts, chains, cables, hoses, guards, shafts, bearings, fasteners, brackets,
  and supports must form one mechanically continuous assembly. A recognizable
  collection of disconnected parts is still a rejection.
- Nothing may float, terminate in open space, cross a moving envelope, pass
  through another component, or rely on the camera angle to hide an invalid
  connection.
- Belt/chain paths must be tangent to their pulleys or sprockets and show both
  carrying and return paths where physically visible.

## Industrial reference provenance

- Every candidate intended for production must cite at least one official OEM
  product page, manual, or data sheet in
  `assets/catalog/industrial-reference-register.json` before promotion.
- The record names a generic equipment family and the specific visual/mechanical
  features modeled from it: envelope, mounting, process connection, optical
  face, actuator, connector location, or similar physical interface.
- A source URL is only `reference-identified`. Promotion requires a
  `compared-pass` record that names the source-to-model comparison and records
  the generic elements that were deliberately not copied. `remodel-required`
  means the source exposed a geometry mismatch and blocks promotion.
- It must explicitly list what remains generic. Do not reproduce logos, marks,
  product numbers, or trade dress; do not turn reference geometry into an
  unverified claim about dimensions, ratings, range, wiring, safety performance,
  material compatibility, or certification.
- Existing production assets without a record are remediation work, not proof
  that they were modeled from a real part. `validate_industrial_references.py`
  reports that gap; `--strict-production` is the backfill completion gate.
- `promote_candidate_asset.py` rejects a candidate that lacks a valid OEM-family
  reference record even if all rendering and recognition evidence passes.

## Materials

- PBR base color, metallic, roughness, normal, and ambient-occlusion channels
  are supplied where materially useful.
- Painted steel, stainless steel, aluminum, elastomer, glass, plastic, wood,
  and corrugated board remain visually distinguishable without labels.

## Simulation

- Pivots and axes match the physical mechanism.
- Travel limits and maximum rates are explicit.
- Collision geometry is separate from visual geometry.
- Every connector has an authored type and orientation.
- Signals are optional; passive assets must not receive invented I/O.

## Help contract

- Every catalog asset has a generated `docs/help/assets/<asset-id>.md` file.
  It lists only the catalog-declared signals, kinematic nodes, connector
  contracts, physical envelope, and production/candidate admission boundary.
- Every migrated scene has a generated `docs/help/scenes/<scene-id>.md` file.
  It lists every declared symbolic point, type, owner, initial state, action,
  equipment binding, and declared simulation safe state.
- Asset help must not invent PLC ownership or physical addresses. Scene help
  must not imply `%I`, `%Q`, DB offsets, a live PLC connection, or a certified
  safety function unless that fact is present in the authoritative contract.
- `tools/generate_help_documents.py` creates the help set from the catalog and
  scene JSON; `tools/validate_help_documents.py` fails when any declared asset
  signal/kinematic node or scene point is absent from its document.

## Review artifacts

- Every generated review image is inspected manually at full resolution. A
  contact sheet alone does not count as inspection.
- Required views include hero, both equipment sides, drive/mechanism detail,
  opposite-end/take-up detail, underside, and end-alignment views. Add views
  whenever an existing camera can hide a clearance or connection defect.
- one context-free blind-recognition image;
- wireframe/topology image;
- scale image beside a one-meter reference cube;
- animated-state captures for every kinematic axis.
- Each asset carries a written review record listing every view, observed
  defects, corrections, unresolved blockers, and final disposition.
- `tools/validate_asset_evidence.py` verifies that every production entry has
  these source, delivery, collision, thumbnail, and review artifacts on disk.
  Production also requires `review/independent_recognition.json` identifying
  the asset family, a non-self reviewer ID, and confidence of at least 0.80.
  Candidate entries may remain incomplete, but the report makes the evidence
  gap explicit and `--strict-candidates` turns that gap into a failing gate.

## Automatic rejection cues

- controls, sensors, cables, hoses, bearings, or guards without visible mounts;
- belt/roller/drum intersections or impossible return paths;
- inaccessible E-stops, pull cords crossing product flow, or guarding without
  believable attachment and service clearance;
- mixed equipment scale classes, such as a bulk-material pulley/drive on a
  low-profile unit-load frame;
- materials that cannot be distinguished without color labels;
- cropped mechanism views, dramatic lighting that hides geometry, or camera
  angles used in place of end/underside inspection.
