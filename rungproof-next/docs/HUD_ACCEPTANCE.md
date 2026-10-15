# Simulator interface and HUD acceptance

Status: **baseline locked on 2026-09-18**.

This is the regression contract for the RungProof Next simulator shell.
Catalog expansion is frozen at 175 candidate families until this baseline
remains green through subsequent editor/runtime work. “Locked” means the
layout, safety boundary, authoring workflow, and verification gates below are
required behavior; it does not mean the enterprise simulator is complete.

## Supported viewport baseline

- Design viewport: 1600 x 900 logical pixels.
- Minimum accepted logical 3D aperture: 480 x 420 pixels.
- 1280 x 720 is supported through Godot `canvas_items` scaling.
- The toolbar, side docks, and compact diagnostics dock must not overlap.
- Every connection-authoring control must be visible without vertical
  scrolling at the design viewport.
- The diagnostics dock starts compact and can expand to 270 logical pixels.

## Required shell regions

1. Persistent, thin top toolbar with RungProof identity, scene title,
   local-runtime/guarded-PLC state, and a `TOOLS` menu.
2. The default **Operator console** follows the original RungProof hierarchy:
   current scene, declared actions, and local runtime on the left; a dominant
   3D plant viewport in the center; event history, scene equipment, and PLC
   health on the right; symbolic local-model point tables below; and compact
   Run/Stop/Reset transport at the bottom edge.
3. The point tables show declared ownership/type and current local-model values
   split by simulator-to-PLC and PLC-to-simulator direction. They never display
   physical address mappings or imply that the PLC is exchanging data while it
   is disconnected. The newer searchable scene/asset hierarchy remains
   available through `TOOLS > Viewer hierarchy`; it is not the default layout.
4. The **Engineering layout** is available from `TOOLS` for focused authoring.
   It retains the wide left Scenes/Assets/Workspace dock, right
   Inspector/Signals/Connections/PLC dock, and diagnostics dock required for
   workspace editing.
5. The 3D viewport always supports right-mouse orbit, middle-mouse pan, and
   wheel zoom. Opening a tool must not cover the transport controls.

## Startup and control safety

- Normal launch starts the local runtime **STOPPED**.
- The guarded PLC runtime starts **DISCONNECTED**.
- Run, Stop, and Reset are the only playback controls. There is no
  `stepPassed` command.
- Scene and workspace files contain symbolic points only. They contain no PLC
  endpoint, physical address, DB offset, `%I`, or `%Q` mapping.
- A saved signal mapping is never presented as executable unless a verified
  adapter handles that exact asset signal.

## Authoring baseline

- Browse all 32 migrated scenes without restarting the application.
- Search and place any candidate into the active scene.
- Select a placed object from the Workspace Hierarchy or directly in the
  viewport. Persistent groups appear as compact, recursively indented headers;
  selecting a header selects its complete group.
- Edit position, rotation, and positive scale; delete the selected placement.
- Duplicate and focus the selected placement, reset its rotation/scale, and
  optionally snap position and rotation to configurable increments.
- Manipulate the selected placement directly with color-coded move, rotate,
  and scale gizmos. A completed drag creates one undo step; `Escape` cancels
  the active drag and restores the starting transform.
- Select multiple placements from the Workspace list or by Ctrl-clicking the
  viewport. Batch duplicate, copy, paste, and delete operate as one undoable
  transaction.
- Drag on empty 3D viewport space to marquee-select projected placement
  centers. `Ctrl` or `Shift` adds to the current selection, grouped members
  expand to their complete group, and `Escape` cancels an active marquee.
- Create persistent, exclusive groups from two or more selected placements.
  Selecting any member selects the complete group. Group and ungroup are each
  undoable, and grouped move, rotate, and scale use one shared saved pivot.
- Groups may nest, but sibling groups cannot claim the same placement; invalid
  overlapping workspace data is rejected before it can create an ambiguous hierarchy.
- Edit a complete selected group's local X/Y/Z pivot through the compact Pivot
  modal, or reset it to the member centroid. The pivot is persisted and copied
  with the group; transforms use it as their fixed rotation/scale center.
- Rename a complete selected group through a focused modal editor. Names are
  trimmed, limited to 64 characters, nonblank, unique without regard to case,
  persisted, and undoable.
- Switch the transform gizmo between world axes and the primary selection's
  local axes. Group members retain their relative arrangement while moving or
  rotating, and group scale changes member offsets around the shared pivot.
- Align selected objects or groups on X, Y, or Z using the primary selection's
  pivot, or evenly distribute three or more selection units between the two
  outer pivots. Groups remain rigid units and each operation is one undo step.
- Clipboard pastes preserve relative transforms and connector links between
  copied members. Complete copied groups are recreated with remapped member
  IDs. Symbolic signal mappings are copied only when the destination scene
  contains a compatible point; incompatible mappings are explicitly skipped
  instead of corrupting the workspace.
- Undo and redo placement, transform, deletion, connector-link, and
  signal-mapping changes.
- Save and load version-5 workspaces containing placements, nested groups
  (including optional pivots), connector links, and symbolic signal mappings.
  Versions 1 through 4 remain loadable.
- Show an unsaved marker after workspace changes.
- Create only connector links allowed by compatible connector kinds.
- Create only signal mappings with compatible scalar types.
- Reject missing endpoints, self-links, duplicates, and incompatible links
  with an inline status message.
- Render connector links with endpoint markers and routed 3D lines.

## Signal-mapping truth states

- **LIVE INPUT**: the current scene point drives an explicitly verified input
  on a simulator-side controller.
- **AUTHORED ONLY**: the mapping is valid and persisted, but runtime behavior
  has not been implemented and verified.
- Belt-conveyor and powered pallet-roller-conveyor `run_command`, `estop_ok`,
  and `speed_setpoint` inputs are the first supported live mappings.
- Asset output/readback publication remains authored-only. It must not mutate a
  PLC-owned point or synthesize feedback until ownership and scan semantics are
  implemented deliberately.

## Keyboard and pointer contract

- `Ctrl+S`: save workspace.
- `Ctrl+O`: load workspace.
- `Ctrl+Z`: undo.
- `Ctrl+Shift+Z` or `Ctrl+Y`: redo.
- `Delete`: delete selected placement.
- `Ctrl+D`: duplicate selected placement.
- `Ctrl+C`, `Ctrl+V`: copy and paste the current selection.
- `Ctrl+G`: group the current selection.
- `Ctrl+Shift+G`: ungroup the selected group.
- `F`: focus the camera on the selected placement.
- `W`, `E`, `R`: move, rotate, and scale gizmo modes.
- `Q`: toggle world/local transform axes.
- `Escape`: clear selection.
- Left drag on empty viewport space: marquee selection.
- `Ctrl`/`Shift` + marquee: add to the current selection.
- Right mouse: orbit; middle mouse: pan; wheel: zoom.

## Automated acceptance

Run from `rungproof-next` with the pinned tools:

```powershell
.\.tools\dotnet\dotnet.exe build
$godot = '.\.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
& $godot --headless --path . -- --verify-app-shell
& $godot --headless --path . -- --verify-hud
& $godot --headless --path . -- --verify-workspace
& $godot --headless --path . -- --verify-virtual-controller
.\.tools\dotnet\dotnet.exe run --project .\tests\RungProof.Next.VirtualController.Tests.csproj
python .\tools\validate_catalog.py
python .\tools\validate_gltf_kinematics.py
python .\tools\verify_scene_contracts.py
```

The virtual-controller verifier loads the bounded LD demonstration, exercises
seal-in, plant-driven photoeye stop, blocked restart, and Reset, validates the
live ladder/variable UI, and reports that no real PLC connection was attempted.

The HUD verifier checks the advanced authoring layout's non-overlap and
aperture rules plus visibility of the complete connection editor. The operator
console is capture-reviewed against the existing RungProof hierarchy. The workspace verifier exercises all three
gizmo modes, numeric transforms, snapping, duplication, multi-selection,
persistent and nested grouping, parent ungroup/undo promotion, naming, and custom-pivot persistence, marquee and
grouped-member expansion, alignment and distribution, shared-pivot group
move/rotate/scale, world/local axes, Workspace Hierarchy group-header
selection,
clipboard relationship and group preservation, batch deletion, undo
restoration, connector creation, signal mapping, save/reload, and a live
safe-state conveyor mapping.

## Visual acceptance evidence

The following captures were inspected at native resolution:

- `build/hud-final-shell-1600.png`
- `build/hud-final-workspace-1600.png`
- `build/hud-final-connections-1600.png`
- `build/hud-final-signals-1600.png`
- `build/hud-final-plc-1600.png`
- `build/hud-final-connections-1280.png`
- `build/hud-workspace-gizmo-1600x900.png`
- `build/hud-workspace-gizmo-1280x720.png`
- `build/hud-workspace-gizmo-rotate-1600.png`
- `build/hud-workspace-gizmo-scale-1600.png`
- `build/hud-workspace-gizmo-labeled-final-1600.png`
- `build/hud-workspace-gizmo-rotate-final-1600.png`
- `build/hud-workspace-multiselect-1600x900.png`
- `build/hud-workspace-multiselect-1280x720.png`
- `build/hud-workspace-group-world-1600.png`
- `build/hud-workspace-group-local-1600.png`
- `build/hud-workspace-group-local-1280.png`
- `build/hud-workspace-marquee-1600.png`
- `build/hud-workspace-marquee-1280.png`
- `build/hud-workspace-arrange-1600.png`
- `build/hud-workspace-arrange-1280.png`
- `build/hud-workspace-arrange-menu-1600.png`
- `build/hud-workspace-group-rename-1600.png`
- `build/hud-workspace-group-rename-1280.png`
- `build/hud-workspace-group-pivot-1600.png`
- `build/hud-workspace-group-pivot-1280.png`
- `build/hud-workspace-hierarchy-1600.png`
- `build/hud-workspace-hierarchy-1280.png`
- `build/hud-workspace-nested-hierarchy-1600.png`
- `build/hud-workspace-nested-hierarchy-1280.png`

Acceptance requires legible controls, no panel overlap, no clipped required
controls, a usable 3D aperture, clear stopped/disconnected states, a visible
selection outline, and explicit signal-mapping truth state.

The operator shell must also fill the full client rectangle at non-16:9 window
sizes without black side bars. Its header exposes File, View, Playback, Scene,
Tools, PLC, and Help. `Tools > Ladder Logic` must open a full programming
workspace whose TIA Portal and Studio 5000 tabs are both present and whose
validated program load remains explicitly offline.

## Deliberately outside this lock

- Authorized live PLC transport and reconnect behavior.
- Generic runtime adapters for the remaining catalog families.
- Asset output/readback publication into scene points.
- Production asset admission and the eventual 850-family/3,000-variant target.

Those items may extend this contract, but they must not regress it.
