# Parcel closed-gate boundary analysis

Isolated QA execution of the actual current `ParcelTransferPlantModel.cs`, linked read-only into `_agent-parcel-boundary-repro/ParcelBoundaryRepro.csproj`. No app state, native UI, app project, app tests, or application source was changed. The reproduction supplies commands; it is not screen proof and does not substitute for the native stall evidence.

## Result

**The floating-touch reopening hypothesis is ruled out for the authored dimensions tested. A real finite-gap/reaction-time leak is reproduced.**

Closed command with gate never previously opened:4000ticks at20ms, gate reported open0times. Leading parcel stopped at frontX=-2.7 exactly. For each authored length1.3/0.9/0.6m, the direct closed-stop roundtrip `(gate-halfLength)+halfLength` equals-2.7 with error0 and strictOverlapFALSE. This holds for authored doubles, float32 authored dimensions, and an axis-aligned float32 world-AABB extent calculation.

The actual scene source homes are-4.6/-5.8/-6.65, sizeXYZ respectively(1.3,1.1,1),(0.9,0.8,0.8),(0.6,0.5,0.6). Adapter obtains Godot float positions and delivered world bounds, then converts to double; the report tests both float dimension and world-AABB arithmetic variants rather than claiming an unqueried native bounds measurement. All variants reproduce the same qualitative leak.

## Reproduced V4 command-order path

The isolated runner mirrors the V4 phase snapshot/last-phase qualification, output-before-transition ordering, center admission inhibit, and geometric feedback decisions. It does not execute the app's ladder session. At scan453 (~9.06s), with phase30/closed gate command, the medium rear is alreadyX=-2.664 and the following small frontX=-2.684 (double variant). The gate plane isX=-2.7 and its bar extends to-2.685. The medium has legitimately cleared the bar, but the small parcel is already16mm through the gate plane. This is not floating epsilon.

The model's queue spacing is20mm. Rear-clear requires passing a15mm bar; one20ms tick at0.9m/s moves18mm, followed by controller scan/reaction. A following parcel at20mm gap can cross the gate plane before the close command is effective. `GateOpen = commandedOpen || gateOccupied` then correctly refuses to slam a bar through that overlapping small parcel, but there is no independent upstream metering mechanism to arrest it. The third parcel leaks onto the primary while the medium is being routed.

All3variants eventually produce sortedCount2, learner phase50/class2/route0.5, primaryTableLoadedFALSE, primaryTableClearFALSE, inhibitionTRUE; the unclassified small parcel is stranded aroundX=-0.788 with Route=-1. The isolated source has no Godot delivered-support callback installed, so it does **not** reproduce/claim every native value (its final TableAligned isTRUE). The count/phase/third-load overlap mechanism is directly demonstrated independently of rendering and support rejection.

## Boundary and next decision

Do not fix this by weakening the gate overlap predicate or its collision inhibition: a parcel is already materially inside the bar volume. A future remediation must establish a physical metering/spacing contract that permits closure under scan timing, or explicitly change the learner admission policy/fixture while preserving the stated mechanics. No remediation was applied here; application remains frozen as requested.

Run only this QA harness with `.tools/dotnet/dotnet.exe run --project _agent-parcel-boundary-repro/ParcelBoundaryRepro.csproj`. It never constructs Godot, a controller connection, or an app runtime.

- src\Scenes\ParcelTransferPlantModel.cs: SHA256 `ad2b8f8699aeb66f3e5fa653ead0cf22ceb71721ff863dce2c12f5053e9f5d56`
- _agent-parcel-boundary-repro\ParcelBoundaryRepro.csproj: SHA256 `fc43a5d02e4b62e2e9b2c508947f379130064490e69e84e98d2dae5b2fad3581`
- _agent-parcel-boundary-repro\Program.cs: SHA256 `05ee55c60ab6d1cd3489947b55b33426b3725846ad18d41c66cc1b6bdb5e3914`
- outputs\screen-audit-2026-10-21\postbackup-parcel-boundary-repro-1792712982898.jsonl: SHA256 `b3704ead94fd707cd3f178db4df4a91634e304afc9aa013670c9c1a3395295cd`
