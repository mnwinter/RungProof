# Dot V17 actual readout and tank-face review

Individually opened six readout PNGs and two tank-face PNGs under APP/.tools/studio-verification, and read all six `.readout.json` sidecars. No engine/build/input/product edits in this review. Capture identities/native suites separately attributed.

| Exact file | Actual finding |
|---|---|
| v17-ev-unequal.readouts.png | BAY A PLC kWh and BAY B PLC kWh large/clear and correctly grouped above respective1.234 and5.678 readouts. No label/control overlap. Sidecar identifies exact independent PLC points; these are explicit QA output values, not learner/controller/actual energy results. |
| v17-chicken-load.scale.png | LOAD PRODUCT sits on actual retained screen; small but readable. Deck empty, tray at input. No detached competing weight caption. |
| v17-chicken-weighing.scale.png | WEIGHING... readable within actual screen; tray on deck; product_weighed False. Sidecar weight0. |
| v17-chicken-stable.scale.png | MEASURED1.237kg readable but small, fitted inside screen; tray on deck and product_weighed True. Sidecar1.237. |
| v17-chicken-changed-mass.scale.png | MEASURED2.468kg fitted/readable, sidecar2.468. This mass comes from explicit existing-model QA fixture, not a new learner mass-selection feature. |
| v17-chicken-reset.scale.png | LOAD PRODUCT restored inside screen, weight0 and product_weighed False; tray back at input. |
| v17-tank-level.face.png | Close face clearly contains blank neutral cyan display window; no digits/units to decipher. Supports source audit's intentional illustration, not a failed live-value projection. Sidebar42/10.72 are separate symbolic points. |
| v17-tank-radar.face.png | Blank neutral cyan window clearly visible, no physical digits/units. Lower housing partially behind rail, but prior roof-mount evidence separate. Sidebar35/35 does not imply these numbers appear on instrument. |

## Bounded acceptance and diagnostic precision

EV bay identity ambiguity and Chicken floating-weight/blank-scale defect are visually closed in these sampled states. Chicken text is readable at close support-view size, not demonstrated comfortably readable at whole-room overview. Full print/application/current-editor behavior and source provenance remain separate native gates; changedmass and unequalEV frames carry explicit QA provenance.

Five Chicken sidecars report six `glyphVertices` each from Label3D.GenerateTriangleMesh.GetFaces. Accordingly call this **configured label mesh/extent fit**, not proof of per-character outline triangles. The earlier diagnostic comment/field terminology overstates what six vertices establish. It still checks returned extent against10mm screen inset/outward1.5mm depth; actual screenshots establish text appearance and absence of clipping. Do not present it as triangle-level glyph certification.

Tank face-readability evidence gap is resolved as intentionally blank static artwork, not repaired live digits. No tank numeric behavior or product display fix required from this finding. Physical instrument identification and ordinary user-view access remain bounded limitations. Material75 HELD, Handle OPEN, all77 objective ACTIVE; no overall approval.

## Two additional operator frames individually viewed

`v17-ev.operator.png`: new bay identity plates overlap top HUD; numeric readouts tiny. `v17-chicken.operator.png`: cabinet controls/data head visible, but actual scale is outside this cabinet-focused diagnostic view. These do not qualify ordinary user readout readability or reachability. No product regression inferred from diagnostic framing; the passing close details remain separately bounded. Total V17 images individually inspected in this report:10.

## Barrel hollow adapter plan decision

Source-reviewed current BarrelFill constructor and measured agent proposal. Approve only four hollow reducing sockets in SceneComposer.BarrelFill.cs: valve interfaces localZ±.20/Y2.45, outerradius.083; meter interfaces localZ±.15/Y2.45, outerradius.088; all innerradius.038, axiallength.08. Annular wall bands bridge retained small-pipe [.038,.045] radius and body valve [.068,.075]/meter [.073,.08] radius with positive volume while preserving minimum bore. Existing roots/bore/rings/runtime remain unchanged. Require actual generated annular/axial overlap, openbore and neighbor clearance/native renders; preserve four original negative-gap witnesses and add explicit new-chain evidence rather than relabeling original pairs connected. No solid plug, generic AABB-only claim or hidden outer coplanar tube acceptance.

**Superseding dimension approval:** final socket inner radius is **.039m for all four sockets**, replacing the initial .038m above. Outer radii remain .083m valve/.088m meter, length .08m, centers unchanged. This avoids coincident .038m inner surfaces across the retained-pipe overlap, leaves a deliberate1mm lumen step and preserves the original pipe's .038m minimum radius. Socket wall overlaps small-pipe annulus by6mm [.039,.045], with positive overlap to both larger body bands. Require8 actual socket-to-retained-part annular/axial overlap checks; preserve4 original negative pair witnesses. This is presentation geometry, not certified physical coupling design.
