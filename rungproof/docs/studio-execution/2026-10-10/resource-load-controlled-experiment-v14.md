# Resource-loader diagnostic: inconclusive retention experiment

2026-10-11. Root ran three sequential headless environment-verifier processes on the same diagnostic DLL `9DD9B08E9F6E66D6C8A5534915EB79B5DA8A0B1A61C6CB84CF8FDA3CB6C9F0C0`, pinned engine4.7.2.stable.mono.official.ed1daf0bf. No UI/input, GC forcing, exception suppression, cache-mode change or product mitigation.

Temporary instrumentation logged each mapped/conveyor/fallback PackedScene load before/after ResourceLoader. One opt-in arm retained each returned PackedScene by path while still calling ResourceLoader on every load. Four other inline load sites were outside the witness; this does not trace every imported dependency or identify its old GC handle.

| Process | Variable | Handle exceptions | Geometry terminal |
|---|---|---:|---|
| resource-trace-v14-baseline.log | Trace only |161|77 cases, exit0|
| resource-trace-v14-retain.log | Trace + managed PackedScene retention |0|77 cases, exit0|
| resource-trace-v14-baseline-return.log | Trace only again |0|77 cases, exit0|

All161 baseline exceptions occurred inside load sequence529, while composing lab-9-11-pallet-counting, context conveyor/conveyor_0, path `res://assets/material_handling/belt_conveyor_600x6000/delivery/belt_conveyor_600x6000.glb`. This narrows a reproduced boundary but does not identify the failing PackedScene or internal dependency wrapper. The later baseline being clean means the retention result cannot establish causality or justify claiming a repair.

The read-only ownership audit found no explicit imported-resource Dispose/Free. The source/primary-engine report identifies an upstream confirmed issue containing the exact pinned build and matching bridge signature; it is supporting context, not proof of our exact failed object. Handle remains OPEN. A clean terminal geometry message alone remains insufficient when the log contains errors.

Instrumentation and its exact modified composer were preserved under `.tools/studio-verification/SceneComposer.ResourceLoadWitness.v14.cs` and `SceneComposer.resource-trace-v14.cs`. Production composer calls were restored and the diagnostic partial removed from compilation. No retention workaround is shipped. Future useful diagnostics must identify the actual failing native object/dependency/lifetime rather than repeat uncontrolled clean/error counts.
