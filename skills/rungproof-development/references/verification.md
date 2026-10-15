# Verification selection

## Preflight

From the repository root:

```powershell
git rev-parse --show-toplevel
git remote -v
git branch --show-current
git status --short --branch
```

Confirm the diff contains only intended files before and after testing.

## Baseline application checks

Run from `rungproof-next/` using the pinned local tools:

```powershell
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore
& '.tools/dotnet/dotnet.exe' run --project tests/RungProof.Next.VirtualController.Tests.csproj --no-restore
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-app-shell
```

If `--no-restore` fails because a fresh checkout lacks packages, restore both
the application and test projects once, then rerun the no-restore commands.
Locate the console executable under `.tools/godot/` if the pinned bundle layout
differs. Do not substitute a non-.NET Godot build.

Use `rungproof-next/README.md` to select focused flags such as
`--verify-ladder-editor`, `--verify-virtual-controller`,
`--verify-scene-controls`, `--verify-scene-geometry`, or the scene-specific
`--audit-*` route. Do not run every expensive scene audit when a narrow change
has a clear focused verifier, but run cross-cutting suites for compiler,
persistence, scan, shared renderer, or shared plant changes.

## Python guarded-bridge checks

For changes under root `tools/`, profiles, or the pinned bridge, run the
relevant root Python unit tests with the local development environment. Ensure
the tests use their fake/in-memory transports and do not contact a PLC.

## Visual changes

Headless tests prove deterministic code paths, not appearance. For ladder,
shell, HUD, or 3D work:

1. build current source;
2. use the existing focused capture/review route when available;
3. capture the exact normal-renderer state and zoom;
4. inspect the image for clipping, alignment, continuity, state, and labels;
5. keep the capture local unless the project evidence contract requires it;
6. for parity, compare against a same-state installed-TIA capture.

Do not accept a screenshot merely because a file exists. Inspect it.

## Asset changes

Run from `rungproof-next/`:

```powershell
python .\tools\validate_asset_evidence.py
```

Then follow `docs/ASSET_QUALITY_STANDARD.md` for Blender-source provenance,
scale, delivery GLB, collision, Godot render, multi-angle review, help content,
and blind recognition. A candidate stays in `candidates.catalog.json` until all
promotion gates pass.

## Skill-only changes

For changes confined to this skill, validate from the repository root:

```powershell
py -3 "$env:USERPROFILE\.codex\skills\.system\skill-creator\scripts\quick_validate.py" .\skills\rungproof-development
```

Also inspect links and ensure every referenced repository file currently
exists. Application build/tests are not required unless the skill change also
changes application files.

## Report

Include commands, exit status, meaningful results, generated evidence inspected,
and exclusions. Keep these boundaries explicit:

- offline test is not normal-renderer visual proof;
- normal-renderer proof is not installed-TIA parity;
- installed-TIA parity is not live PLC or safety proof;
- package verification is not external release approval.
