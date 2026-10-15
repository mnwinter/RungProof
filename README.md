# RungProof

RungProof is a Godot/.NET visual PLC logic simulator with an integrated ladder
editor, virtual controller, authored industrial scenes, training challenges,
and a guarded optional Siemens S7 connection path.

This repository is the current RungProof source. The former
`Siemens-PLC-PC-Interface` repository contents were preserved in Git history
before this source became `main`.

## Start the application

The application project is in [`rungproof-next`](rungproof-next/README.md).
Its portable Godot and .NET toolchains are local-only and intentionally not
stored in Git.

```powershell
cd .\rungproof-next
.\RUN-RUNGPROOF-NEXT.cmd
```

See [`rungproof-next/docs/DEVELOPMENT_SETUP.md`](rungproof-next/docs/DEVELOPMENT_SETUP.md)
for the pinned tool versions and first-run layout.

## Repository layout

- `rungproof-next/` - current Godot/C# application, runtime assets, tests, and
  developer tooling.
- `prototype/scenes/` - original scene contracts retained for deterministic
  migration and parity checks.
- `prototype/plc-profiles/` - explicit guarded PLC connection profiles.
- `tools/` - the minimal external-PLC bridge and its offline tests.
- `vendor/` - pinned Siemens interface source and third-party license files
  required by the guarded bridge and plant-model parity tests.
- `skills/rungproof-development/` - reusable project handoff for AI coding
  agents, with routed architecture, TIA-evidence, safety, and verification
  guidance.

Generated builds, imported Godot caches, portable SDKs, virtual environments,
logs, backups, and old review archives are intentionally excluded.

## Verification

From `rungproof-next` with the pinned local toolchain installed:

```powershell
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj
& '.tools/dotnet/dotnet.exe' run --project tests/RungProof.Next.VirtualController.Tests.csproj
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-app-shell
```

These checks are offline. They do not prove Siemens TIA Portal parity, a live
PLC connection, or safe commissioning on equipment.

## Controls boundary

RungProof is a simulator and training tool, not a safety controller. External
PLC operation requires the configured profile, an explicit operator action,
the declared write scope, a PLC-side watchdog, and normal machine safety
controls. Never expose S7 PUT/GET directly to an untrusted network.

