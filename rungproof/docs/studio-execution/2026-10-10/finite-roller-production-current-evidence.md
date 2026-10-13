# Finite pallet roller production checkpoint

Status: IN_PROGRESS. This is one bounded offline workspace preset, not all-scene visual approval or release approval.

## Final bounded acceptance

Current final DLL `B931FC3644D10642F623AEE2F82EDE15F286B1C8820CFF9ABD19D92F78A4F6EC`, build v7 zero warnings/errors. On this same unchanged binary, core native v7 terminated exit0/failures0, prior finite belt regression v2 terminated exit0/failures0, and app-shell regression v2 passed 77 scenes/3 groups/5 demos plus unsaved-work and external-profile/no-transport guards.

Root and Dot personally inspected all five `finite-roller-product-capture-v3` PNGs and all four `two-room-material-revision-v6` PNGs; each capture's exact DLL identity is the current B931 binary. Dot's production report is PASS_WITH_LIMITS. The room report is preview only, with hard shadows/dark trim and sparse dispatch detail remaining; human material approval and all77 current visual approval are false.

Direct invocation of the named pure-test DLLs passed transport 39 and observer 95 checks. An initial `dotnet run` transport invocation printed the observer banner because the colocated test apphost was reused; its log is retained and is NOT transport evidence. Direct named DLL execution removes that ambiguity. Controller regression passed 164 checks. Final identities and source hashes are frozen in `.tools/studio-verification/finite-roller-production-final-evidence.json`.

The root classroom lighting revision passes the incoming scene ID explicitly: `_currentSceneId` is assigned after environment composition, so using that field would miss the incoming classroom and could affect the following scene. Existing lighting restore retains scene isolation.

No export, publication, live PLC connection, physical load-bearing qualification or whole-catalog release approval is inferred.

## Geometry baseline

External sensor installation B2 passed native geometry v2 with exact DLL `358069B4C07DF96D292215C497AF4492BCCDFB61C6F77CA73107FCF8853954DC`. Dot inspected the four real geometry renders and recorded PASS_WITH_LIMITS in `finite-roller-external-geometry-dot-v2.md`. The original embedded-optics V5 rejection and external B1 hardware-overlap rejection remain retained. B2 moves each complete TX/RX subassembly outward 0.12 m with grounded pedestals; no global GLB changes. Optical feedback intersects actual pallet wood triangles and retains real gaps.

## Actual controller and project evidence

Core native v3 and v4 terminated exit0, `WORKSPACE_ROLLER_RESULT failures=0`. Core v4 includes actual intended/wrong programs at 20/40/100 ms, next accepted sampled beam ordering, analytic acceleration/coast, 31 actual roller phases, terminal load clamp with continued drive, explicit Stop/Reset, invalid current/reopened draft rejection, whole-cell translation and history, staged rollback and three-way same-source draft-cache separation.

Enabled/disabled observation compares complete accepted scan payloads, plant snapshots and all 31 actual transforms. Actual input forces are obeyed by the plant and retire observation; Reset clears forces and starts a new home epoch. Valid draft-only changes preserve the runtime snapshot while retiring historical observation.

Dot found that Stop replaced an established NeedsAttention failure with Incomplete. The narrow fix preserves Observed, NeedsAttention and Unavailable immutable evidence. Pure observer now passes 95 checks. Native v4 covers wrong next-scan and terminal Stop history at all three periods. Controller regression passes 164 checks with no real PLC constructed or connected.

## Historical build-v6 render candidate (superseded by final acceptance above)

Build v6 passes with zero warnings/errors, 305 source files. Exact DLL `0FD247F556B4CC173698D5123C6C29C0143F33910E438001E72B3E816678CA31`.

`finite-roller-product-capture-v1/intended.scene.png.log` correctly rejected a clipped success card. Explanatory text was shortened and the actual card-fit assertion retained. Candidate v2 captures use actual File menu creation, saved joint project reopen and accepted execution, with ephemeral QA captions and the clock held. Production defaults still supply no learner ladder.

Intended v2: accepted scan 288, recorded physical trigger scan 225, command OFF, drive 0, pallet center -0.762499833 m, current beam clear, Observed after the supported stationary hold. Coasting is captured separately to distinguish command OFF from nonzero drive speed. Final v3 captures and same-binary native/Dot review supersede this stage as recorded above.

## Material scope

Human requested more color/texture because the rooms remained flat/bland. V4 material revisions are limited to Lab 2.1 and Lab 4.8. Dispatch separates warm concrete from cool cladding and lifts dark wall paint; classroom lowers plaster brightness and adds local diffuse fill. Root further balanced classroom-only directional key/fill energy. Other 75 scenes have not received this material pass. Final fresh room pairs and human review remain required.
