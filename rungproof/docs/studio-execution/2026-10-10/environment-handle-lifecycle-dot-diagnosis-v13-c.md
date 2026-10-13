# Dot: V13-c environment lifecycle diagnosis

Read-only source/log review. No build, engine, UI, product edits or reproduction performed by Dot. The captured root run supplies the observed failing workload; isolation remains pending.

## Verdict

The environment verifier reached its 77-scene terminal geometry PASS, but its log contains **96** `ERROR: System.InvalidOperationException: Handle is not initialized.` entries. Retain its geometric assertions as error-tainted evidence, not a clean native pass. The separately clean industrial-mount result remains its own bounded evidence. Handle root cause remains OPEN; material75 remains HELD.

## Exact first trigger

In `environments-v13-c.log`, the first error follows the sump-pump CASE PASS. Its stack is `GCHandle.FromIntPtr` → `ScriptManagerBridge.SwapGCHandleForType` → native ResourceLoader → `ResourceLoader.Load<PackedScene>` → `SceneComposer.CreateMappedAsset` line305 → `CreateControlledAsset` line558 → `Compose` line88 → `AddMigratedScene` line4180 → environment verifier line37. The bridge logs the exception; the verifier's ordinary try/catch does not convert it into a failing exit.

The failing load happens during replacement composition. In current `AddMigratedScene`, Compose precedes current-runtime/root RemoveChild and QueueFree (lines4200/4208). Therefore blaming the same replacement's subsequent QueueFree is unsupported. The stack does not identify the resource path or object owning the invalid old handle.

## Narrow lifecycle discrepancy

Industrial mount verification awaits ProcessFrame immediately **after AddMigratedScene**, then inspects the installation; it does not explicitly remove decoration each iteration. Environment verification creates and synchronously inspects decoration, snapshots process transforms, removes decoration using RemoveChild/QueueFree, restores presentation resources, checks state, and awaits ProcessFrame **at the loop end**. Thus the next composition occurs on the resumed continuation after decorative retirement; the current process remains alive until after its successor is composed.

The end-of-loop comment correctly expresses an intention to avoid accumulation, but one ProcessFrame signal is not evidence that every queued native deletion and managed wrapper/finalizer has completed. Both verifiers use the same compose-before-retire product path. Different allocation volume, managed references retained across an async suspension, and decorative-resource restore/release can affect collection/cache wrapper timing. None is proven causal by this source comparison.

## Ranked, falsifiable hypotheses

1. **Cached imported-resource wrapper/handle reattachment under churn.** Prediction: a minimal repeated ordinary scene replacement using the implicated imported path reproduces the bridge signature; retaining a diagnostic strong reference to the same PackedScene changes recurrence. First instrument exact asset path/cache state and wrapper identity, rather than altering production cache policy.
2. **Decorative removal/resource restore changes lifetime timing.** Prediction: otherwise identical diagnostic workload that leaves decoration attached until normal replacement behaves differently from the explicit end-of-case RemoveDemoEnvironment workload. Compare one variable and retain restore assertions in a separate case.
3. **Async continuation/deferred retirement cadence.** Prediction: moving only the diagnostic inspection await to the industrial verifier's position changes reproducibility. An extra frame may bound a timing dependency, but disappearance does not establish the invalid-handle owner or a product repair.

The existing typed-engine-handle diagnosis records the same ResourceLoader/bridge signature in earlier versions, including repeated environment workloads. This argues against treating V13's gallery repair or label helper as a demonstrated new cause.

## Next discriminator and acceptance

Root should retain exact binary identity and complete stderr/stdout, instrument the first failing asset/scene transition, then run a small diagnostic replacement loop at that transition with one lifecycle difference per run. Treat any bridge ERROR as failure of the clean diagnostic gate even if scene assertions pass. Inspect the pinned engine bridge implementation and resource ownership before selecting a repair. Do not suppress errors, weaken the route verifier, introduce generic production GC/manual disposal, or label a clean rerun as root-cause resolution.

## Bounded semantic-strip plan review

The proposed exact35 support-owned strips are approved for implementation under existing repair authorization, subject to actual visual acceptance. Preserve exact action wording/bindings, signal colors, heads/rings, interactor targets, IDs, reset behavior and support-follow transforms. Attach at measured cabinet front and above measured ring top; validate full plate envelope, neighboring pitch clearance and two-line metric fit. Hide only the specifically mapped old plate/text after the replacement is successfully added; missing mapping must fail honestly. The proposed .16m height, width limited by pitch and fixed font solve geometric fit only. Actual operator captures must show readable wording at normal viewing size; 2–4px old faces were not acceptable. These strips do not approve material rollout or close Handle.
