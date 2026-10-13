# Typed native engine handle diagnostic

Read-only diagnosis of the existing v3 log; no build, engine run or product/QA source change. Root owns the planned exact-binary repeat. A clean repeat would bound recurrence, not establish the cause or a repair.

## Observed evidence

- `.tools/studio-verification/typed-final-v3-typed-learner-workflow.log:1324` contains one `System.InvalidOperationException: Handle is not initialized.`
- Stack: `GCHandle.FromIntPtr` -> Godot `ScriptManagerBridge.SwapGCHandleForType` -> native ResourceLoader call -> `ResourceLoader.Load<PackedScene>` -> `SceneComposer.CreateConveyor` line198 -> Compose -> AddMigratedScene -> LoadWorkspaceFromPath -> the QA Joint helper.
- Trigger context is exact joint reopen of Chicken Label, 100ms, compile-valid wrong template, after navigating away to workstation-call. It is resource loading during scene composition, before the learner program is run.
- The following log entries report conveyor nodes bound, Chicken scene/runtime created, workspace loaded, exact draft/layout restored, stopped reopen, and subsequent current-editor workflow assertions true. Root reports456 checks true and exit0. Those results do not erase the engine error.
- `typed-learner-native-v1.log` and `typed-learner-native-v2.log` contain no ERROR or EXCEPTION lines; both terminate failures0. They are different earlier builds, not exact-v3 reproduction tests.
- The same exception already occurs in historical `all77-final-environments-v8.log` (123 occurrences; first line429) and `whole-product-final-scene-environments-v1.log` (242 occurrences). Those traces include CreateConveyor/PackedScene.Instantiate. This establishes earlier occurrence of the signature, not a universal benign engine condition.

## Source lifetime evidence

`SceneComposer.cs:198-200` holds a local PackedScene returned by ResourceLoader and immediately instantiates it. This path has no explicit Resource.Dispose/Free and no application cache replacement at that point. The exception's failed handle operation is inside Godot's managed/native bridge; the log does not identify which object owns the old handle.

`Main.cs:4097` composes the replacement scene before removing/QueueFree-ing the current runtime/root at4117/4125. QueueFree is deferred. The QA Joint helper deliberately does navigate-away and reopen in one continuation, then awaits a frame. This increases repeated resource/scene churn but follows the existing five-demo persistence test pattern and normal product workspace APIs. Old/deferred scene overlap is a hypothesis to measure, not a proven use-after-free.

Chicken's accepted history subscriber is attached only after the joint reopen and detached before Reset. The error occurs earlier in the reopen; that subscriber is not active at this trigger. The QA has no direct ResourceLoader, Dispose, Free, GC collection or native-handle operations.

## Ranked hypotheses and limits

1. Resource-cache managed wrapper/GC handle reattachment during repeated load/instantiate: consistent with the bridge stack and historical occurrences. Not proven without the identity and lifetime of the oldGCHandle object.
2. Deferred old scene release interacting with cached resources across repeated replacements: plausible from product ordering and QA churn. The failed composition occurs before the current replacement's QueueFree, so attributing it to that specific QueueFree would be unsupported.
3. New typed learner algorithm or feedback mutation: weaker; error occurs in shared conveyor resource composition, and the same signature predates this QA. No incorrect feedback, malformed saved project, missing node, wrong command or current-draft fallback has been demonstrated by this trace.

Do not add generic forced-GC, manual disposal, cache-ignore, suppressed-log or delayed-retry workarounds without reproducing and isolating the failing resource lifetime. No product repair is proposed from this trace alone.

## Next discriminating step

Root's isolated exact-v3 rerun should retain full stderr/stdout and binary identity. Compare error count, exact scene/mode/cadence/reopen occurrence and all subsequent assertions. If reproducible, reduce only the workload to repeated ordinary Chicken/workstation joint reopen with the same resource path; retain native errors as failures of the diagnostic gate. Inspect the pinned engine bridge source and cached resource identity before choosing a lifecycle change. Native rendering/behavior acceptance remains separate from this engine-lifetime diagnostic.
