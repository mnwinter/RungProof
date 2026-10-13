# Resource-wrapper Handle investigation — V13e

Read-only research, 2026-10-11. No application edits, engine launches, builds, cache changes, or forced collections. Handle root cause remains OPEN; material75 remains HELD.

## Local evidence

`rungproof-next/.tools/studio-verification/environments-v13-e.log` contains **40** exact `ERROR: System.InvalidOperationException: Handle is not initialized.` records. First begins at line414, following sump-pump PASS413. Terminal line1878 reports all77 environment cases PASS. The assertions completed, but this is an error-tainted run, not clean qualification.

The first stack crosses `GCHandle.FromIntPtr` → `ScriptManagerBridge.SwapGCHandleForType` → `ResourceLoader.Load<PackedScene>` → `CreateMappedAsset`305 → `CreateSelectorAsset`605 → `Compose`87 → `AddMigratedScene`4180. This identifies the loading boundary, not the failing resource owner. Existing `typed-engine-handle-diagnosis.md` establishes an earlier same-signature conveyor load; `environment-handle-lifecycle-dot-diagnosis-v13-c.md` documents why subsequent old-scene QueueFree cannot itself establish the cause of the preceding load.

Local `RungProof.Next.csproj` line3 targets net10.0. The recorded engine is `4.7.2.stable.mono.official.ed1daf0bf`; no new executable verification was performed here.

## Primary sources and distinctions

| Source | Verified behavior / bounded implication |
|---|---|
| [Official 4.7.2 release](https://github.com/godotengine/godot/releases/tag/4.7.2-stable) | Exact release/tag exists; release identifies commit ed1daf0. Exact-tag source is available, so research need not substitute current master. |
| [4.7.2 ScriptManagerBridge](https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs) | Swap first converts the old pointer, then checks whether its target was collected. An exception at conversion occurs before the normal null-target recovery. Catch logs the exception, clears the output handle and returns false: continued app execution does not erase it. Web-parsed line numbers differ from PDB1284; this report does not assert line parity. |
| [.NET10 GCHandle](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/GCHandle.cs) | FromIntPtr calls ThrowIfInvalid; this implementation throws HandleIsNotInitialized for a zero pointer. Given the recorded FromIntPtr frame, zero old input is the strongest source-derived discriminator. Exact loaded runtime patch was not measured. |
| [4.7.2 native C# binding](https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/modules/mono/csharp_script.cpp) | Reference callback upgrades weak handles when reference count exceeds1; the opposite downgrade branch explicitly excludes released handles, whereas the upgrade condition does not. Swap ownership is transferred before the managed call. An initialized weak binding with a released/zero handle is source-compatible with this signature. Our owner/refcount/binding state has not been captured, so this is a mechanism hypothesis, not a proven local cause. |
| [Godot issue112067](https://github.com/godotengine/godot/issues/112067) | OPEN, bug/confirmed/topic:dotnet as inspected. Original reporter observed a cached CompressedTexture2D owner with weak=true, released=true, zero handle and refcount2 while instantiating its referencing PackedScene. Oct5 reporter supplies the identical 4.7.2 ed1daf0bf build/signature. Maintainer marks confirmed and says the relevant team tracks it. This establishes a closely matching known engine issue, not local causality. IgnoreDeep reportedly removed that reporter's Handle error but another Instantiate crash remained. Linked repair commits are in forks; their existence is not proof of an upstream merged fix or inclusion in our pinned binary. |
| [Issue121018](https://github.com/godotengine/godot/issues/121018) | EditorResourcePicker/GC reproduction on4.7 was marked duplicate of112067. Editor assignment differs from our headless runtime; assembly reload is not required by our observed workload. |
| [ResourceLoader cache documentation](https://docs.godotengine.org/en/stable/classes/class_resourceloader.html) | REUSE reuses cached resources/dependencies. IGNORE still reuses external dependencies; IGNORE_DEEP propagates exclusion. Stable docs are not a binary audit. Cache mode changes are diagnostic variables with memory/performance/identity consequences, not a recommended blanket repair. |

## Discriminating experiment contract

Root owns the opt-in path trace and strong-reference experiment. Keep the same candidate, scene order, frame cadence and asset inventory; vary one lifetime intervention at a time.

1. Trace each load path/type/cache mode and completion before Instantiate; include native instance IDs and dependency paths/classes where safely observable. A load-boundary trace cannot identify which dependency's binding failed.
2. Compare baseline against retained managed PackedScene wrappers. A negative result does **not** falsify the texture/material dependency hypothesis. A second bounded diagnostic arm should retain relevant managed dependency wrappers too, separately recording their native IDs and count. Do not make this an unbounded permanent product cache.
3. Log natural GC collection counts and weak-wrapper survival without forced collection. A positive retention result supports lifetime/timing involvement but does not prove the exact race or a safe production solution; retention also changes timing and ownership.
4. Separate scene retirement cadence and environment resource restoration into independent arms if needed. An extra frame changes timing; it is not proof finalizers have finished.
5. Decisive native instrumentation/breakpoint: first failing callback's old pointer, createWeak, owner resource path/class, reference count, binding.inited, released/weak flags, and release/swap thread chronology. This distinguishes zero released binding from wrong ownership or other corruption. Existing app-only path tracing cannot supply all these values.
6. Treat any engine ERROR as a failed clean-run criterion even if77 assertions and exit0 remain. Retain full logs and candidate identity for each arm.

Do not suppress exceptions, retry successful loads until green, indiscriminately dispose cached resources, force global GC, or change all loads to IgnoreDeep as the repair. No pinned-version upgrade/fix inclusion claim is established by this research.
