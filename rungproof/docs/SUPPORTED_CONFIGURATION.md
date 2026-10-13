# Supported configuration

## Current Godot demo preparation

The table below applies to the historical Qt pilot. The current Godot/C#
application now has an internal unsigned offline Windows x64 candidate with
exported policy, shell, conveyor and sampled palletizer checks. External
distribution and minimum Windows/GPU/VM/DPI qualification remain open.
Do not inherit the Qt software-renderer claims.
See [demo release review](DEMO_RELEASE_REVIEW_2026-10-10.md) for the current
evidence, proposed offline preview scope and remaining qualification gates.
The current repaired package and exact acceptance status are recorded in
[qualification continuation](DEMO_QUALIFICATION_CONTINUATION_2026-10-10.md).
The proposed Windows 11/16 GB/Forward+ pilot target in
[release operations](DEMO_RELEASE_OPERATIONS_2026-10-10.md) is a qualification
proposal, not a tested minimum-hardware claim.

## Historical Qt pilot configuration

This is the release support matrix. `TBD` means the configuration is not yet
approved and blocks the corresponding pilot capability.

## Native application

| Item | Pilot value | Evidence/status |
|---|---|---|
| Product version | `0.2.0-pilot.2` | Disconnect-telemetry correction candidate; live delta pending |
| Architecture | Windows x64 | PyInstaller Windows build path exists |
| Windows edition/build | Windows Server 2019 Standard, `10.0.17763.0`, AMD64 | Recorded in accepted pilot.1 bench preflight; unchanged target for pilot.2 delta |
| VM platform/version | VMware Workstation 17 Player | Recorded in accepted pilot.1 bench result; unchanged target for pilot.2 delta |
| Minimum display | 1920x1080 target | Live bench screenshots exist; explicit final visual approval remains |
| Default renderer | `software-qwidget` | Locked Qt tests and packaged self-test pass |
| Optional renderer | Qt 3D by explicit `--renderer qt3d` | Workstation option, not default VM promise |
| Supported native scene | Scene 2 - Conveyor Pusher | Only production/native live scene |
| Review-only scenes | S03-S06 | No Run, Reset, Test PLC, or Connect capability |
| Browser/server runtime | Not packaged or supported | Must remain absent from native artifact |
| Network listener | None | Packaged window test observed zero listeners |
| Build default PLC-write capability | Disabled | Prevents an ordinary package build from silently enabling writes |
| Internal PLC test candidate | Enabled only with `-EnableRealPlc` | Required to perform the live bench acceptance |

## PLC-enabled pilot capability

The pilot.1 baseline below was observed on the actual isolated bench. Pilot.2
keeps this exact profile/target and requires the packaged delta qualification
before it is called accepted. The operator-authorized internal test build
exposes **Connect Real PLC** only for that controlled work.

| Item | Required value |
|---|---|
| TIA Portal | TIA Portal V17 Update 9 / STEP 7 Professional V17 Update 9 |
| CPU family | S7-1500, CPU 1512SP-1 PN, order number `6ES7 512-1DK01-0AB0` |
| CPU firmware | V2.9 |
| Rack/slot | `0/1`, endpoint `10.70.9.201` |
| Data block | Standard/non-optimized DB14 with the documented exact offsets |
| Cycle | 20 ms reviewed profile value |
| Transport timeout | 2000 ms reviewed profile value |
| Heartbeat timeout | 1000 ms reviewed profile value |
| Field I/O | No production I/O connected; isolated training bench only |
| Network | Controlled lab/controls network; endpoint recorded in acceptance evidence |

The DB14 ownership map remains defined by the packaged profile and
`packaging/vm/README-VM.txt`. It must be observed in a TIA watch table during
acceptance; source tests are not commissioning evidence.

## Unsupported configurations

- Production machines or safety-rated functions.
- Physical `%I` or `%Q` writes.
- Arbitrary DB layouts, optimized DB14, or unreviewed profile edits.
- Generic claims covering every S7-1200/S7-1500 firmware or TIA version.
- Scene 1, S03-S06, training labs, or user-authored scenes as native live PLC
  sessions.
- Automatic discovery, unattended connection, remote internet operation, or
  use as a substitute for a PLC watchdog.
