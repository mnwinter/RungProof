# Handle next diagnostic: managed API limits

Read-only bounded audit. Read resource-load-controlled-experiment-v14.md and its preserved witness, pinned GodotSharp API XML and exact-tag primary engine source. No broad log search, implementation/build/engine. V14 baseline161/retained0/baseline-return0 remains **INCONCLUSIVE**; no retention mitigation is justified. Handle OPEN.

Pinned local API evidence: `.tools/godot/Godot_v4.7.2-stable_mono_win64/GodotSharp/Api/Debug/GodotSharp.xml`, SHA25621CE00E88B5370C594C74D00D842F8BF809389B95ECDFABB55FAF59EE56B9E0C.

| Available API | What it can actually establish |
|---|---|
| GodotObject.GetInstanceId / GetClass / NativeInstance | Native object ID/class/pointer for a wrapper already available to app. NativeInstance is the object pointer, **not** the C# GCHandle or binding handle. |
| RefCounted.GetReferenceCount | Current native reference count of a known live wrapper; not managed reference count, wrapper liveness history or failing callback refcount. Do not call Reference/Unreference as a diagnostic workaround. |
| Resource.ResourcePath / GetRid | Known resource path/renderer RID; neither identifies old binding handle. Embedded subresources may not have independent paths. |
| ResourceLoader.GetDependencies(path) | Dependency path/UID manifest before loading. Pinned docs specify path or UID::empty::fallbackPath; the middle component is **not a resource class**. It does not enumerate every imported internal mesh/material/texture wrapper or its lifetime. |
| ResourceLoader.HasCached(path) / GetCachedRef(path) | Cache availability / retrieval of cached resource reference. Retrieval can itself cross managed binding/reference setup and alter ownership; it is not passive inspection of an old released handle. Its result cannot safely be treated as the identity of the error owner inside a subsequent Load. |
| WeakReference to existing wrappers / GC.CollectionCount | Managed survival/collection-count observations only; no oldGCHandlePtr, native binding.inited/weak/released or callback owner. Creating/retaining additional wrappers changes the experiment. |

The preserved V14 witness logs BEGIN path/context/sequence before Load and returned PackedScene ID after Load. It reproduced sequence529 belt-conveyor GLB load, but its after-call ID cannot identify a dependency failing during the call. Four inline loads were untraced. Expanding path/known-wrapper logging may improve correlation, not prove the failing handle.

## Why app tracing cannot recover the old handle

Exact [4.7.2 native callback](https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/modules/mono/csharp_script.cpp) owns the binding/rc_owner and old handle; it transfers/clears handle ownership immediately before calling the managed swap. Exact [4.7.2 managed bridge](https://raw.githubusercontent.com/godotengine/godot/4.7.2-stable/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs) is internal UnmanagedCallersOnly, catches/logs its own exception and returns false. Ordinary app try/catch cannot obtain its arguments/owner. A first-chance exception observer can identify signature/time but supplies no native owner or old handle argument. Reflection into internals or converting an old freed pointer is not a supported inspection API.

The [net10 GCHandle source](https://raw.githubusercontent.com/dotnet/runtime/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/GCHandle.cs) makes the recorded FromIntPtr exception consistent with zero input. A zero handle has no target from which an app can recover the owner. This does not independently prove which native binding reached that state.

## Narrow next diagnostic proposal

Prefer a conditional debugger stop on the **first** SwapGCHandleForType old-pointer-zero call (or first HandleIsNotInitialized exception), keeping the exact pinned binary/workload and existing load sequence context. Record old pointer/createWeak and the native caller's rc_owner ID,class,Resource path,reference count,script_binding.inited and gchandle weak/released state. Do not dereference a freed handle to identify it. Preserve complete native call stack and thread IDs; native caller state is essential, not merely the managed exception stack.

If official native symbols cannot expose caller locals, the required next step is a separately qualified exact-ed1daf0bf diagnostic engine with observation-only instrumentation at binding release and before swap, recording those fields and chronological thread sequence. This would be a different diagnostic engine artifact, not an upgraded or repaired product. Local managed PDBs do not alone guarantee native symbols or accessible owner locals. Root should first verify debugger/symbol availability before authorizing such a build.

Success is one correlated owner/binding lifecycle witness, including its release→reference/swap sequence; a clean run or different error count is not success. No forced GC, global cache-mode changes, strong-reference workaround, exception suppression, upgrade or generic retry is proposed. Material75 HELD; no physical/PLC or release claim.

Root availability check: Get-Command found no cdb.exe,windbg.exe,vsdbg.exe,lldb.exe on PATH; standard Windows Kits10 x64 cdb/windbg and Program Files LLVM lldb paths are absent. No top-level native .pdb exists beside the pinned Godot executable. This bounded search does not establish that no debugger exists anywhere on the machine. No debugger installation, attachment or engine build performed. App-only correlation remains insufficient to identify the failing old native binding owner.
