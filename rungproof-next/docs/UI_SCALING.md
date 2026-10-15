# Desktop UI scaling

RungProof Next is a desktop engineering application. Its root viewport uses
Godot's disabled stretch mode so one UI unit maps to one window pixel. A larger
window therefore exposes more editor workspace instead of enlarging every
toolbar, tree, and Ladder element.

Windows high-DPI awareness remains enabled. Do not restore `canvas_items`
stretch for the application shell: that scales the fixed 1600x900 design size
to the current resolution and makes the UI 1.2x at 1920x1080, 1.28x at
2048x1152, and 1.6x at 2560x1440.

The Ladder workbench uses three nested draggable split regions: project versus
editor, editor versus instruction tools, and program editor versus the bottom
diagnostics/watch dock. Each split bar is permanently visible, uses a larger
12-pixel minimum grab area, and has a high-contrast background. The project,
tool, and diagnostics panels also retain independent collapse/reopen controls.
Minimum panel sizes prevent accidental zero-width panes while still permitting
substantial resizing. The native Ladder interaction verifier changes and
restores all three split offsets in addition to testing every collapse/reopen
pair. Dock hosts do not request editor stretch space; their visible panels fill
the assigned split region. This keeps the central ladder canvas dominant while
allowing the 340-pixel default instruction browser to show normal instruction
names without leaking controls outside its boundary.

Run the real-window density verifier at representative resolutions:

```powershell
.\.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe --path . --resolution 2048x1152 -- --verify-ui-density
```

Expected evidence includes:

```text
UI_DENSITY_VERIFY PASS mode=Disabled factor=1 window=2048x1152 viewport=2048x1152 oneToOne=True
```

This verifier requires a real display. Godot's headless display intentionally
reports a synthetic 64x64 viewport and cannot prove desktop DPI geometry.
