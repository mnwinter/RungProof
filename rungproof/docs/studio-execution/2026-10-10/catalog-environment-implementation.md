# Complete catalog environment update

Matt authorized extending the reviewed complete-scene direction to **all scenes and labs**, explicitly correcting the initial five-demo interpretation. Scope is the current 77-entry catalog, including the equipment gallery and base tank scenes. This is presentation work in the preserved studio worktree, with no publication or PLC operation.

## Studio agreement

### Active additional gate: distinct purposeful locations

The mounted-control catalog images are now baseline evidence only. Matt rejected repeated beige rooms and requires meaningful differences in each scene's location, architecture, layout, colors and material treatment. Tint changes and swapped props on an identical shell do not meet this gate. The studio is preparing an explicit 77-scene direction ledger and reusable architecture builders; Dot requires six contrasting native rendered representatives, including two related student labs, before accepting the direction. All 77 scenes then require fresh scene/split inspection against frozen source. See `environment-diversity-dot-criteria.md`. No completion or new diversity PASS is asserted here.

The subsequent layout-fixes capture contains five replacement pairs: XY cabinet framing, two tower/shelf intersections, luggage panel occlusion and EV panel occlusion. These repairs are independent of the pending distinct-location requirement; they do not convert old room captures into acceptance of the new direction.

### Current completion gate: realistic installation

Matt explicitly incorporated a further full-catalog realism correction into the current RungProof goal. The earlier static environment capture pass is historical evidence, not completion of this expanded gate. Every scene must be audited for component support, functional placement, usable operator access, credible scale, and machine/room alignment. Simple instructional button/lamp/relay/display exercises require a coherent student test bench with supported components and visual wiring context. Complex machines require grouped operator controls in credible cabinets/panels; field sensors and actuators remain at their functional machine locations. Decorative shelves behind scattered floor controls do not satisfy this requirement.

The implementation must preserve equipment IDs, tag semantics, action bindings, animation members, Reset poses and actual pick targets. Installation transforms must be established before runtime, feedback and motion controllers bind. Exclusive ownership separates control-installation integration, environmental context repairs, catalog inventory and Dot review. Acceptance reports separately count JSON inventory, actual rendered review, corrected placements and focused tag-to-animation verification. Actual simple-bench and larger-machine renders are required before claiming catalog completion.

The 77-scene placement inventory is `realism-placement-inventory.json` (version 3): 24 student-bench candidates, 52 machine/service settings and one equipment gallery. It lists individual equipment roles, field exceptions and placement-side recommendations. This is inventory coverage, not proof that 77 scenes have passed the mounting update.

Matt rejected the initial Lab 2.1 bench composition because it blocked access to stocked shelving. That V1 layout is withdrawn. Training rooms now omit the conflicting shelves, secondary benches and filler props. The bench has a 0.95 m worktop and an explicitly recorded 1.0 m deep front standing zone across its width, with the room's open cutaway front providing approach. These are illustrative dimensions, not ergonomic or regulatory qualification.

Dot actually inspected all four `realism-supervised-v4` full/split frames for Lab 2.1 and Lab 4.8. Both pass the bounded visual prototype review: clear bench access and a proportionate 1.82 m wide, 1.85 m high painted industrial cabinet with actuator bezels, legends and grouped pilot feedback. The count display stays at its conveyor station as a field readout. Functional verification remains a prerequisite for catalog rollout. Cabinet legends are small at the preserved wide split-view camera; the sidebar actions remain readable. Neither this review nor those four frames represent Matt's acceptance or full-catalog completion.

`realism-bench-native-v3.log` passes the corrected bench's authored cases, exact functional-head ray pick, accepted local press/release, root reset restoration and independent lamp emission/output checks. A separate explicit verifier-only in-memory reference rung proves the actual built-in controller scan path, output-to-lens illumination, release and Stop/Reset. It does not ship a starter solution into the intentionally blank learner project. V1 and V2 test failures were harness clock/reference setup errors; their logs are retained. Broad rendered rollout remains held pending human review of the corrected representatives.

The catalog audit, art/engineering audit and Dot review agreed on an explicit profile for every scene, reusable environment families, world-axis-aligned architecture, and special handling for outdoor, submerged and vertical-transfer settings. Camera direction selects which room sides are open, rather than rotating the room relative to machinery. Existing machine transforms, process routes, controls, bindings, runtime and camera framing remain authored.

Eight families cover controls training, warehouse/packaging, machining, process utilities, roads, open utility sites, building services and the equipment gallery. Profile variants distinguish receiving/dispatch, food/chemical packaging, commissioning, machining/drives, charging/waterfront and washroom/baggage/access contexts. Palette accents belong to furnishings; functional signal colors remain meaningful.

Decorative meshes are siblings of the process, with no collision, picking, I/O or controller behavior. Original procedural textures and cached materials avoid external asset dependencies and line/grid backgrounds. Context uses conservative rectangular presentation envelopes, not certified physical clearance. Authored ground datum is zero; pit, waterway and elevated-installation exceptions retain ground and receive only peripheral aprons.

## Implementation and review

1. Explicit 77-ID profile ledger, independently compared with the catalog.
2. Shared world-aligned family builders, connected furnishings, recessed architecture and appropriate outdoor context.
3. Scene-local neutral lighting with exact restoration, conditional floor replacement and preserved authored materials.
4. Native structural verification across all 77 actual compositions; build and affected shell, challenge and scene-contract checks.
5. Actual Compatibility-renderer full and split captures for every scene, indexed evidence, independent studio review and targeted recaptures for findings.

The first rendered review caught coplanar wall-base surfaces and insufficient differentiation of specialty settings. Wall bases now sit inside their walls; furniture placement follows the final room dimensions. Outdoor utility equipment has a visible lateral service shelter. Elevator architecture uses a lower service facade instead of an oversized empty wall. The bridge retains its original floor geometry with a scene-local waterway finish, matched to the authored water material. Specialty recaptures closed those presentation findings before the complete capture pass.

## Verification results

- C# build: zero warnings and errors.
- Native environment composition verifier: **77/77 PASS**, including exact profile coverage, sibling hierarchy, world axes, decoration without control/collision behavior, preserved process transforms and camera, and restoration of floor material/visibility and lighting.
- Native authored runtime cases: **71/71 PASS**. Six catalog entries have no authored verification cases; the composition verifier still covers them.
- Demo sequence and app-shell/challenge checks passed within their recorded scopes. Static captures do not verify keyboard navigation, screen readers or minimum hardware performance.
- The combined static/runtime contract command remains **BLOCKED** by existing metadata drift in `lab-10-04-motor-enum-state`: catalog `equipmentCount=6`, authored scene equipment count `8`. This update does not alter either file or relabel the blocked command as passing.

During the expanded realism pass, the catalog was narrowly reconciled to the existing Lab 10.4 definition: equipment count 6→8, addition of `trainingAccessory` to that entry's type list, aggregate equipment 631→633, switches 150→151 and training accessories 134→135. The authored scene JSON remains unchanged. A fresh invocation of the original metadata validator passes all 77 entries. The earlier combined-command BLOCKED log remains historical evidence; it is not retroactively relabeled PASS, and a fresh runtime audit is required for the new mounting transforms.

The final capture directory is `rungproof-next/.tools/studio-verification/catalog-final-v1`. Its manifest records each native image hash and requested-scene log. The searchable `gallery.html` links full-resolution images; it does not generate or alter their pixels. Independent review covers every full and split image before final acceptance.

Known visual limitations are repeated family furnishings, occasional upper room geometry behind the translucent toolbar, tiny authored equipment labels, and existing XY palletizer foreground cropping under the preserved camera. These are bounded presentation limitations rather than evidence of motion clearance or accessibility acceptance.

## Boundaries

These new environments belong to the current development build in the preserved studio worktree; they are not included in the older immutable V5 package. Existing V5 exports are unchanged. The earlier Demo4 zero-count report was a misread; its defect HOLD is withdrawn. Broader motion/input, DPI/accessibility, minimum GPU, clean-machine, rights, signing, distribution, live-PLC and physical-safety gates remain separate. No commit, push, deploy or automation is part of this update.
