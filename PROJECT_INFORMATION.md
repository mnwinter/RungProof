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

## AI development handoff

`skills/rungproof-development/SKILL.md` is the maintained entry point for an AI
coding agent continuing RungProof development. It routes to focused references
for the project map, controls and live-PLC boundaries, Siemens TIA evidence,
and verification. Keep it aligned when those governing contracts change.

## Siemens TIA Portal V21 UI parity

The current main-screen and property-management acceptance ledger is
`rungproof-next/docs/TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md`; the active
execution order and status dashboard is
`rungproof-next/docs/TIA_PARITY_BATCH_PLAN.md`. Keep local source, automated
gate, screen-control, and installed-TIA V21 evidence distinct. UI/property
parity remains in progress, installed-TIA visual matching is unverified, and
Demo 1–5 ladder-logic remediation stays deferred until the GUI/property gates
are closed or each gap has an explicit accepted disposition.

## Verification boundary

Successful build, unit-test, and headless Godot checks prove only the local
offline software paths they exercise. They do not prove installed Siemens TIA
Portal parity, live hardware behavior, network readiness, or commissioning.

The guarded external-PLC path must retain exact ownership separation, explicit
write authorization, configured addresses, and PLC watchdog behavior. It must
never write physical `%I` or `%Q` addresses or control CPU operating state.

