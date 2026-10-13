# Unified simulation integration checkpoint

Matt resumed work after clarifying that demos, lessons, build-your-own, realism,
and the logic editor belong to one product. Preserve the existing implementation.
Valid incorrect PLC code must produce its actual incorrect plant behavior;
lesson evaluation observes behavior rather than correcting outputs.

## Verified on resumption

- Preserved ten-zone sources compile: zero warnings/errors.
- Native `--verify-control-installations` passes all 77 scenes; evidence:
  `rungproof-next/.tools/studio-verification/resumed-tenzone-controls.log`.
- This does not establish ten-zone transport correctness or visual acceptance.
  Its fresh accepted-program verifier and full/split renders remain required.

## Reuse and gaps found by studio source inspection

- WorkspaceDocument already persists supplemental placements, transforms,
  connector/signal links, groups, and SourceSceneId. Existing repository and
  undo/redo can be retained. It does not persist overrides to authored equipment.
- Main creates the immutable catalog base before restoring added placements.
  SceneDefinition already supplies equipment IDs, config, simulation, training,
  and verification contracts suitable for cloned editable templates.
- WorkspaceSignalRuntimeAdapter supports belt/roller inputs; other catalog
  assets and output feedback remain explicitly authored-only. Connector topology
  persistence is not general physical plant composition.
- VirtualControllerSession already owns ordered accepted scans and snapshot
  diagnostics. Reuse that clock and its element/tag observations.
- Dot found Run can retain an older loaded program after editor changes. Define
  and verify current-draft versus accepted-program lifecycle before claiming
  that invalid current code cannot execute. Never silently load a solution.
- Ten-zone behavior uses the current lab's exact aliases. Generalized editable
  use must resolve declared mappings consistently for both motors and transport.
  Do not expose a movable/configurable module whose runtime disagrees with it.
- The current ten-zone application is one finite carton, with no zone-presence
  feedback points or torque/slip model. Command counts are not sensor evidence.

## Next bounded milestone

1. Prove intended and valid incorrect programs through existing editor build,
   binding validation, load, accepted scans, plant, and actual mesh projection.
   Include independent aliases, stopped handoff, finite overlap, Stop/Reset.
2. Resolve stale-program Run semantics with explicit diagnostic behavior and
   tests for invalid draft, valid changed draft, and unchanged loaded program.
3. Add versioned authored-equipment overrides to the existing workspace model,
   compose from a cloned SceneDefinition, and include overrides in save/load and
   undo. Keep process-coupled transforms constrained until supported by adapters.
4. Join workspace and ladder project references, command/feedback inspection,
   and observed lesson results using existing scan snapshots.
5. Expand supported modules and editable templates, retaining the full 77-scene
   realism review and fresh Dot visual approval. No release approval is implied.

No publishing, live PLC, paid assets, scheduled work, or input takeover is
authorized by this checkpoint. The broader editor integration is not complete.

## Concrete ten-zone verification checkpoint

Qualified candidate DLL SHA256:
`AA4E0219320030076EAD0359182FAC32D51A29AEA0EAD3B2AAF1547087C68416`.
Fresh native `resumed-tenzone-unified-native-v2.log` reports zero failures.
The all-zone QA program reaches discharge at accepted scan 1701, center 13.7.
Valid partial QA code stalls at accepted scan 501, center 1.1, zone 0.
Actual shaft/drum/per-zone UV/carton, one accepted tick, stationary stators,
Main Stop and Reset are checked. Mixed alias image fixtures are separately
labeled and do not substitute for program execution evidence.

The test builds a LadderEditorDocument and directly loads its compiled program.
It does not prove visible shell editing, Verify + Load, current-draft policy,
persisted project lifecycle, or authored lesson assessment. V1 launch lacked
the pinned .NET environment and is retained as failed, not qualifying evidence.

Root personally viewed both actual full/split images in
`tenzone-purpose-render-v1`. Motors obscure the belt and zone labels at default
views; wall legend is tiny. Composition is not visually accepted. Dot review
is requested. Entry/exit plant-generated feedback remains missing; command
counts are not feedback. Human Run lifecycle changes are in progress using a
separate editor-draft wrapper so diagnostic explicitly loaded programs remain
valid without silently rebuilding unrelated UI drafts.

## Feedback and editor lifecycle candidate

- Ten actual sensing-line photoeyes and PC-owned BOOLs are now declared in
  the Lab 10.6 contract. This is an explicit exception to the earlier authored
  JSON freeze; its baseline is retained as
  `lab-10-06-before-zone-sensors.scene.json`. Occupancy means interception of
  one measured beam line, not whole-zone coverage or inferred readiness.
- Candidate compiles with zero warnings/errors. Pure controller tests now
  report 164 passed / zero failed in `stopped-session-regression-after.log`.
  The regression failed before the stopped-clock guard (expected zero scans,
  observed 1000). Stopped calls now neither sample/commit/tick nor accrue time;
  an unaccepted scan cannot commit an output or advance the plant.
- `tenzone-feedback-draft-native-v3.log` is FAILED, not qualifying evidence:
  the editor lifecycle fixture rejected its purported valid program and then
  dereferenced a missing session. The fixture owner is diagnosing the real
  scene contract/validation; no assertion is being relaxed.
- Root personally inspected `tenzone-feedback-render-v2` full image. Higher
  open-front framing exposes belts; the cabinet no longer blocks an endpoint.
  Zone indices remain occluded and require grounded rear-rail relocation.
  Dot is reviewing both images; no visual approval is claimed.
- A further required QA case consumes actual final-beam feedback in PLC logic,
  stopping all zone commands on the next accepted scan. Sampling unused input
  tags alone will not be treated as proof of the complete feedback loop.

The failed lifecycle fixture's root cause is identified: `estop_ok` in
`conveyor-cell` is SIM-owned, so the input binding validator correctly rejected
it. The fixture now uses the declared PC-owned `photoeye_blocked` input.
`AddMigratedScene` also attaches the actual scene to the shell; validation is
not bypassed. The fixed fixture remains unqualified until a fresh native run.
`stopped-session-regression-final.log` passes 164/164, including stopping during
input sampling with zero output commits and zero plant ticks for that rejected
scan. Dot personally qualified both V2 render frames but still requires visible
zone labels and passing lifecycle/feedback tests.
