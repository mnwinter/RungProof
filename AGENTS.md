# AI and contributor instructions

This repository is the PLC Visual Simulator / RungProof project. Preserve the
existing application and its history. Do not replace working behavior with a
throwaway demo.

## Project rules

- Read `CONTEXT.md`, `PROJECT_INFORMATION.md`, `README.md`, and the relevant
  `docs/` material before making architectural changes.
- Preserve unrelated local work. Never use `git clean`, `git reset --hard`, or
  broad checkout/revert operations unless the user explicitly requests it.
- The local checkout is authoritative when the user asks to integrate and push.
  Inspect the branch and remote first, then commit and push the exact branch.
- A clean Git tree means the working files match the last commit; it does not
  mean the project has no history or generated assets.
- Do not report completion from static inspection alone. Run the relevant build,
  deterministic tests, and application verifier, and distinguish visual proof,
  simulator proof, and live-PLC proof.

## Controls and PLC rules

- Keep command, feedback, permissive, interlock, status, and fault signals
  distinct. Do not turn momentary safety inputs into latches accidentally.
- The built-in simulator and an external PLC are different execution sources.
  The simulator must remain usable without a PLC.
- The existing guarded S7 live path owns real PLC transport. It writes only the
  configured PC-owned scope, reads only configured PLC-owned points/status, and
  keeps playback state separate from connection state.
- Do not invent PLC addresses, protocol support, tag mappings, or hardware
  behavior. Use the validated profile and transport code already in the repo.
- Live PLC work requires the configured CPU/profile, exact scope confirmation,
  authorized controls-network access, and direct observation of the machine.
  Automated tests must use the in-memory adapter and must not contact a plant.

## RungProof Next boundary

`rungproof-next/` is the Godot/C# successor with the integrated ladder editor,
scene browser, split view, virtual controller, authored demos, and training
assets. Its current architecture is symbolic/offline and deliberately does not
own direct PLC transport. See `docs/AI_HANDOFF.md` before attempting to restore
the live-PLC bridge in that shell.

## Verification

From `rungproof-next/`:

```powershell
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore
& '.tools/dotnet/dotnet.exe' run --project tests/RungProof.Next.VirtualController.Tests.csproj --no-restore
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-app-shell
```

The exact Godot path may be one directory different depending on the pinned
tool bundle; locate it under `rungproof-next/.tools/godot/` if needed.
