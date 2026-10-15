# Project information

## Canonical repository

- Product: **RungProof**
- Repository: `mnwinter/RungProof`
- Default branch: `main`
- Local clean checkout: `C:\Users\matt.winter\Documents\RungProof`

`mnwinter/PLC_Code_Visualizer` is the separate ControlsTrace product and is
not a RungProof source, destination, or migration repository.

## Current architecture

`rungproof-next/` is the current Godot 4.7.2/.NET 10 application. It contains
the integrated ladder editor, virtual controller, visual scene catalog,
training challenges, deterministic offline test harnesses, and current runtime
assets.

The surrounding repository contains only dependencies that the current source
references:

- original scene contracts used by migration/parity tooling;
- guarded PLC profiles and Python bridge code;
- the pinned Siemens interface vendor snapshot;
- third-party notices and contributor/project guidance.

Generated imports, SDK downloads, compiler outputs, release builds, logs,
backups, and superseded review archives are not source and are excluded.

## Verification boundary

Successful build, unit-test, and headless Godot checks prove only the local
offline software paths they exercise. They do not prove installed Siemens TIA
Portal parity, live hardware behavior, network readiness, or commissioning.

The guarded external-PLC path must retain exact ownership separation, explicit
write authorization, configured addresses, and PLC watchdog behavior. It must
never write physical `%I` or `%Q` addresses or control CPU operating state.

