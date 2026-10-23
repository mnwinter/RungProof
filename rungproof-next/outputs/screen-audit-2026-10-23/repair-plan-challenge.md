# Repair-plan challenge: reentrant Run and radar menu lifecycle

Read-only critique of repair-plan.md/radar-visible-route-decision.md against current source. No application changes, runtime execution, UI or Git/D operations.

## Concrete correction: accepted Run must survive synchronous subscribers

The footer package's wording “publish once at accepted ordinary local Run boundary” needs an explicit supersession guard. `VirtualControllerSession.Run():92–98` calls runtime.Run, creates a Run publication and synchronously invokes PublicationPublished then SnapshotPublished. Subscribers can call Stop/Reset/Pause or replace the session before Run returns. Existing NotifyPublication/NotifySnapshot identity guards stop delivering the superseded envelope, but a naive unconditional `SetWorkspaceStatus("RUNNING")` after `Run()` would announce a revoked run. Checking only Snapshot.State==Running also misses Stop→Run reentrancy/newer Run; checking session identity alone misses same-session lifecycle changes.

Minimum defensible one-shot approach without a broad status framework:

1. At the ordinary local call site capture exact session reference, current scene/runtime reference, validated loaded-draft identity and pre-Run publication revision/identity.
2. Call that captured session.Run().
3. Publish success only if Main still owns that exact session/scene, its current publication is **the direct expected Run envelope** (Run kind, prior revision+1, unchanged session/load/execution identity), current stateRunning and notPaused, and current editor draft still matches the loaded validated program. With Run returningvoid, matching the expected immediate revision is a conservative way to distinguish a newer reentrant publication. Alternatively return the immutable Run envelope from Session.Run and require reference identity; that is clearer but expands the API change. These are proposed designs, not existing methods.
4. If superseded, do not overwrite the newer lifecycle/readiness outcome. Do not restore an earlier caller's outer status or report accepted Run merely because current state eventually becameRunning again.

The exact public publication property names should be used from existing types during implementation; no invented token API is required. This guard must wrap actual Run outcome, not PublishVerifiedDraftState before Run. Treat challenge/external/readiness branches separately; ordinary validation success is not permission to overwrite their errors.

Meaningful small regression: subscriber on Run publication calls Stop; actual RunRequested finishes with stopped state and no green outerRunning. Repeat with a SnapshotPublished Run callback calling Reset/Pause, plus Stop→Run reentrancy (newer revision remains authoritative). A session-replacement callback must not let the old caller touch the new session's status. Keep original invalidEDIT001→validRun outerlabel/color and same-program cases. These test actual publication supersession rather than mirroring UI text construction. Native original repro still required after implementation.

## Radar route assessment

Scene-specific View item using existing SceneActionRequested is coherent; no global operator-bank reveal or PLC-looking physical control is needed. Conditional exactscene+declaredaction+BOOL SIM point, actual-runtime-derived checked state and Reset/scene refresh are necessary. Do not optimistically toggle checked state before dispatcher acceptance.

Existing local unloaded-controller dispatcher permits nonPLC toggle; a stopped loaded controller also reaches ordinary toggle. Scan-inspectionPause routes through its existing queue/block policy before toggle, so the decision to preserve that gate is sound. A viewing request whilePause is blocked must retain prior checkmark and show genuine rejection, not ghost/reset anything. Stop and Reset are different: Reset restores sceneinitialFalse; ordinary Stop safeState may preserve inspection because radar safeState need not include that SIM point. Derive behavior from actual reset/runtime rather than a second UIflag.

Additional narrow lifecycle guard: menu callback must resolve the **current** active scene/action at activation time, not close over an old radar runtime from menu creation. Hide/disable item on scene switch; reject late activation after switch and refresh actual check state afterward. No direct renderer/project method invocation, controller load/Run, PLCwrite or processgeometry mutation is justified by presentation-only inspection.

## Impact on the proposed plan

Keep existing priorities/scope. Refine footer acceptance with supersession identity and add the small reentrant regression. Radar decision is already appropriately conditional; add late-menu activation/rejectedPause checkmark behavior to its bounded lifecycle test. These are correctness obligations, not additional audit discovery permutations or authorization to implement now.
