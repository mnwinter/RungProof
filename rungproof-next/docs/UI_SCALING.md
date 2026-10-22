# Desktop UI scaling

RungProof Next is a desktop engineering application. The complete workbench now
has a persistent **Interface scale** control in the application **View** menu
and in **Tools > MCP & Application Settings**. This control is independent of
the Ladder diagram's 75/100/150% zoom selector.

The supported workbench choices are Auto, 75%, 85%, 100%, and 115%. Auto uses
75% below a 1450x800 client area, 85% below an 1800x1000 client area, and 100%
when more workspace is available. Auto never enlarges the interface. The root
window uses Godot `CanvasItems` scaling with no fixed design canvas, so resizing
the window continues to expose additional engineering workspace instead of
stretching a fixed 1600x900 layout. Windows high-DPI awareness remains enabled.

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
UI_DENSITY_VERIFY PASS mode=CanvasItems factor=1 window=2048x1152 viewport=2048x1152 viewportMatchesScale=True controls=setting=auto resolved=100% ...
```

At 1600x900, Auto resolves to 85% and the verifier expects an approximately
1882x1059 virtual workspace. This verifier requires a real display. Godot's
headless display intentionally reports a synthetic 64x64 viewport and cannot
prove desktop DPI geometry.
