# QA, security boundary, and release department deep dive

Date: 2026-10-10. Submitted to Dot. Planning only; no tests, build, audit, app launch, export, installation, transport/network contact, publication, source/config edits, or changes to existing artifacts occurred. This report is a bounded static review, not a formal security scan or a new qualification of historical test results. Only this new report was written.

## Department recommendation

Approve development of one offline conveyor challenge only after the cross-department contract is frozen and the human authorizes implementation. The studio must prove that grading rejects plausible wrong programs and cannot manufacture success through forces, toolbar Stop, stale snapshots, absent cartons, or old results. Qualify the exact offline artifact separately from the development executable. Internal challenge acceptance, native experience acceptance, external release acceptance, and physical PLC commissioning are different decisions.

Use Dot's proposed isolated renderer-free worker harness: freeze the editor document, briefly lock editing with a working Cancel action, explicitly state “fresh isolated trial,” and leave exploratory plant state unchanged. No interactive replay in the first slice. Label result traces as historical test evidence. Native parity must execute the same frozen programs, cases, scan period and plant configuration in the Godot integration; worker success alone cannot qualify scene wiring.

## Inspected evidence and limitations

Applicable parent and RungProof AGENTS, SECURITY, current README, CONTEXT, PROJECT_INFORMATION opening/current section, release review, offline candidate/ladder acceptance documents, baseline and departmental 01/02/04 plans were inspected. Exact source seams inspected include:

| Path under `rungproof-next/` | Static observation | Acceptance consequence |
| --- | --- | --- |
| `src/VirtualController/VirtualControllerSession.cs` | Explicit sample, execution, output commit and plant-step ordering; snapshot publication also occurs on Run, Stop, Reset and force operations; nonfinite elapsed time rejection and 1000-scans-per-Advance bound | Count completed scans through the proposed dedicated boundary; lifecycle publications must never satisfy movement/hold predicates. Batch cap is not a UI-latency guarantee. |
| `src/Scenes/ConveyorPlantModel.cs` | `LeadingEdge` becomes null after discharge, increments Completed; ObjectPresent is explicit; Reset restores loaded carton and counts; blocked beam includes a minimum hold interval | Require present carton and unchanged completion count, not only a position or beam Boolean. Raw geometry and held sensor state require distinct explanations. |
| `src/Connections/RuntimeBuildPolicy.cs`, `GuardedRuntimeClient.cs` | Build policy throws NotSupportedException for external transport; both constructor paths converge on policy enforcement | Exact offline executable must reject construction before channel factory/spawn. Menu hiding alone is insufficient. |
| `tests/OfflineDemo/Program.cs`, corresponding csproj | Executable test checks path and injected constructors and zero factory calls; defines RUNGPROOF_OFFLINE_DEMO and links Connections source | Useful policy fixture, but it compiles its own assembly; it cannot by itself prove the exported application has the right policy. |
| `tests/RungProof.Next.VirtualController.Tests.csproj`, `tests/Plant/RungProof.Next.Plant.Tests.csproj` | net10.0 executable tests, warnings treated as errors, source-linked controller/model | Run executable entrypoints, not assume a test framework discovers them. Isolate intermediate/output paths per project. |
| `src/VirtualController/LadderEditorProjectJson.cs`, `src/Workspace/WorkspaceRepository.cs` | Draft loading differs from runnable-program validation; workspace save uses same-directory temporary file then replacement | Retain invalid drafts; failure must preserve prior good data. Atomic replacement is not power-loss or backup proof. |
| `tools/build_offline_demo.ps1` | Fresh directory, dirty-source labeling, source/config fingerprints, export diagnostics/inventory checks, policy/shell/conveyor checks on exported console executable, manifest/hashes, separate notices | Extend for challenge content/evaluator identity and acceptance; do not silently reuse an older candidate. |

The dated release review describes previous local candidate checks, 197 historical legacy Python passes, malformed-input repairs, and outstanding external gates. Those counts are historical statements tied to that review, not reproduced here. Its copied-folder startup evidence is not a clean-machine test. `OFFLINE_LADDER_ACCEPTANCE.md` similarly records 2026-10-02 results. CONTEXT retains legacy Qt language and cannot override current Godot source. Baseline WIP inventory is not a recoverable backup; a worktree at HEAD alone omits current dependencies.

## Requirement and evidence register

| ID | Requirement | Required future evidence / owner |
| --- | --- | --- |
| QA-01 | Grading accepts different valid ladder structures and rejects each wrong behavior | Pure fixtures plus identical native parity fixtures; QA + controls |
| QA-02 | Trace preserves sampled inputs, applied command, post-step plant feedback and accepted scan identity | Boundary assertions and batch-versus-single-step equivalence; architecture |
| QA-03 | No nonprogram action awards success | Force/input mutation/toolbar Stop/reset/scene-switch/cancel probes; architecture + QA |
| QA-04 | Result belongs to exact frozen challenge/program/plant/evaluator/period identity | Hash, revision, concurrent edit, stale callback and retry tests; architecture |
| QA-05 | Draft and progress recovery fail without losing existing work | Fault injection, malformed fixtures and restart acceptance; architecture + QA |
| QA-06 | Grading and JSON/trace allocation bounded; cancellation responsive | Resource-budget and scheduling tests with measured latency on reference hardware; architecture + QA |
| QA-07 | Offline challenge cannot construct or start external transport | Policy unit fixture plus exported executable/menu/direct-handler/entry-path probes; security boundary owner |
| QA-08 | Existing working demos/editor/sandbox and authorized development bridge behavior preserved | Scoped regression lanes and in-memory/fake channel tests; QA |
| QA-09 | Actual challenge render and keyboard path usable at supported sizes/scales | Native screenshots/walkthroughs and input routing observations; UX + QA |
| QA-10 | Exact distributed package qualified and rights/support claims accurate | Clean Windows, manifest/signature/notices/rights/support evidence; release owner + human |

Map QA-01/02/04 to ARCH-02 through ARCH-06 and ARCH-10; QA-05/06 to ARCH-07 through ARCH-09; QA-09 to UX-001 through UX-012. These identifiers belong in the synthesis acceptance matrix, not only narrative prose.

## Grading qualification: positive and negative fixtures

All fixtures are proposed tests, not existing APIs or executed results. Fixed 20 ms is the challenge policy. Nonconforming imported program period is InvalidProgram with an explanation; do not rewrite the learner's program silently. Use binding identities so renamed tags still work.

Positive fixtures: (A) contact-and-coil seal-in using the output contact; (B) independent internal running-memory latch with stop/sensor reset and a separately driven output. Each must survive Start release, stop at the beam, honor process Stop and simultaneous Start/Stop priority, remain stopped, and work after explicit fresh reset. Add renamed-tag variants. Controls approves the concrete ladder contents and scan ordering before fixture generation. Do not require a reference-program hash or a specific rung topology.

| Fixture / probe | Required classification and discriminating evidence |
| --- | --- |
| Empty or always-off | Fail travel/start objective; a loaded stopped carton at origin is never success. |
| Unconditional/always-on | Fail cold start and sensor/Stop cases; elapsed travel alone cannot pass. |
| No holding path | Fail released-Start travel case after the one-scan pulse; distinguish pulse lifetime from sensor stop. |
| No process Stop contact | Fail operator Stop before beam; toolbar runtime Stop must not be substituted. |
| No sensor contact | Fail at beam; capture first sampled-true scan with output still on. |
| Start priority / restart after Stop | Fail simultaneous Start+Stop or sustained post-stop case. |
| Oscillation / automatic restart | Fail hold window without a fresh Start. |
| Discharge masquerading as position zero | Fail presence/count/state predicates; discharged carton must not pass even if published position resets. |
| Output initial value true | InvalidProgram or explicit cold-start failure; runtime Reset restoring declared initial values must not be misread as safe. |
| Sensor/output force assisted exploratory success | No grade; trial starts fresh with zero forces. Direct force mutation during a trial is blocked or aborts as InvalidTrial, never Passed. |
| Manual point toggle or scene action during trial | Isolated harness cannot receive it; verify no leaked exploratory UI mutation reaches trial adapter. |
| Duplicate/reversed/type-invalid binding, forbidden imported instruction | InvalidProgram before execution; hidden palette is not eligibility enforcement. |
| Runtime watchdog/diagnostics, nonfinite data, maximum scan count | Failed or infrastructure/invalid classification according to typed cause; no completion award. |
| Cancel, close, navigation, toolbar Stop, repeated Test | One run epoch; canceled old worker cannot publish pass after another test begins. Preserve exploratory runtime state and draft. |

Ordinary stop success must witness actual travel, a raw/held beam transition, next sampled beam true, applied conveyor command false, retained carton, unchanged Completed count and a stopped hold window. At 0.5 m/s and 20 ms, commanded travel is 0.01 m per scan. A beam reached after plant step n is available to logic at n+1; require off at n+1. Do not demand off at n or use wall time. Numerical boundary epsilon and stop interval must be jointly frozen with controls after model fixtures, not guessed from screenshots. Verify initially blocked sensor and sensor edge just before/after step separately if those are authored cases.

Run each case with fresh runtime/model/pulses/backlog/forces except deliberately specified repeat-without-reset scenarios. Verify running the complete fixture matrix twice and in reversed case order produces identical typed outcomes/traces. Verify many Advance batches versus one-scan stepping and lifecycle publications yield identical accepted-scan evaluation. Copy mutable dictionaries; retain no reference whose values change later.

## Isolation, identity, persistence and budgets

Frozen identity includes program content SHA-256, challenge revision, evaluator revision, plant parameters/content identity, scan period and run epoch. Define canonical program serialization once; whitespace/property ordering must not accidentally create inconsistent identities. Historical pass is retained as history, but never attached to edited current code; Undo behavior must explicitly compare identity. Cancel/Failed/Invalid/Infrastructure outcomes never award progress. Completion save failure displays “passed, progress not saved” with recoverable evidence rather than pretending persistence succeeded.

Adversarial fixtures: null root/collection/item, missing fields, unknown enum/ID/revision, duplicate IDs/output bindings, excessive depth/width/string length, NaN/nonfinite numeric source, truncated data, future schema, path traversal in challenge IDs, invalid UTF-8, and untrusted failure/help text. IDs must not become filesystem paths. Render text without unintended markup/commands. Challenge predicates are validated enums/data, not arbitrary scripts/C#/shell/eval.

Provisional budgets to ratify at the meeting: challenge input 256 KiB, 32 cases, 32 stimuli/case, 10000 scans/case, 100000 scans/run, retained trace at most 4096 rows plus compact first-failure/window summaries, progress input 1 MiB/10000 entries. Limit JSON depth to a documented small value (propose 32), bounded identifier/display text lengths, total instruction count and branch depth for imported draft eligibility. Existing engineering project limits need a separately calibrated policy; do not impose these challenge-specific values on all old projects. Test exactly-at-limit and one-over rejection before allocation/work. Work count alone cannot guarantee responsiveness; cooperative worker cancellation checks at every completed scan and batch boundaries, proposed cancel observation under 250 ms on declared test hardware, must be measured. Check each case plus overall budget; no timeout converts to pass.

Persistence probes cover canceled save dialog, denied/write-protected directory, full-disk injected failure, temp-write failure, replacement failure, existing corrupt file preservation, restart after pass, no autosave over existing user work, schema upgrade and downgrade refusal, and independent demo/development user-data paths. Preserve valid older data on failed write. Race-test two concurrent save attempts if production repository allows them; otherwise serialize them explicitly. Worker result delivery must not perform file I/O on a UI callback without bounded scheduling/error handling.

## Existing verification lanes and proposed additions

No commands below were executed. From `rungproof-next/`, preserve the app's normal Godot assembly location for app build. Independent console suites need separate directories for all three MSBuild properties. Example existing suite:

```powershell
$taskQaRoot = Join-Path $PWD '.tools/studio-qa/controller'
& '.tools/dotnet/dotnet.exe' restore 'tests/RungProof.Next.VirtualController.Tests.csproj' "-p:BaseIntermediateOutputPath=$taskQaRoot/obj/" "-p:MSBuildProjectExtensionsPath=$taskQaRoot/obj/"
& '.tools/dotnet/dotnet.exe' build 'tests/RungProof.Next.VirtualController.Tests.csproj' --no-restore "-p:BaseIntermediateOutputPath=$taskQaRoot/obj/" "-p:MSBuildProjectExtensionsPath=$taskQaRoot/obj/" "-p:OutputPath=$taskQaRoot/bin/"
& '.tools/dotnet/dotnet.exe' "$taskQaRoot/bin/RungProof.Next.VirtualController.Tests.dll"
```

Repeat with independent `plant`, `offline-policy`, `connections`, and new proposed `challenges` directories for `tests/Plant/RungProof.Next.Plant.Tests.csproj`, `tests/OfflineDemo/RungProof.Next.OfflineDemo.Tests.csproj`, `tests/Connections/RungProof.Next.Connections.Tests.csproj`, and proposed `tests/Challenges/RungProof.Next.Challenges.Tests.csproj`. Confirm each actual AssemblyName/output DLL before execution; default console projects use filename stem. Do not share obj between neighboring projects or accidentally run an old DLL after a failed build. Record command/exit/assertion markers and exact executable/source identity. Restore/dependencies and setup environment are implementation-lane work, not performed here.

Existing app build: `& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore`. Existing engine: `.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe`; locate/confirm before use. Existing verifiers include `--verify-app-shell`, `--verify-virtual-controller`, `--verify-ladder-editor`, `--verify-demo-sequence`, `--verify-split-view`, `--verify-scene-controls`, `--verify-gallery-clock`, `--verify-external-playback`, `--verify-offline-build-policy`. Use headless only for valid internal-state checks; inspect each verifier's window requirement. Proposed `--verify-challenge-conveyor` does not exist yet; implementation must create it using identical fixture contracts. Existing scene contract command is `py -3 tools/verify_scene_contracts.py --godot <confirmed executable>` from Next; Python availability must be established in execution lane. Existing legacy Python/JS security and lifecycle tools remain separate surfaces; scoped owners select relevant regressions, not claim all surfaces checked from a Next-only suite.

Existing packaging command: `& ./tools/build_offline_demo.ps1`; `-AllowDirty` is only for a reviewed internal WIP candidate. Packaging script currently fingerprints code/config; challenge content must join fingerprint and artifact manifest so challenge changes during export are rejected too. Add challenge verifier to exported executable checks. Require nonzero exits, missing PASS marker, error diagnostics, failed assertions, stale DLL or forced termination to fail the gate; green geometry assertions with shutdown diagnostics are not a clean verifier exit.

## Native and release acceptance

Native QA requires human-authorized screen access during implementation: actual screenshots and keyboard interaction, not emitted button events alone. Matrix proposes 1600x900 and 1200x675 logical viewport, 100/125/150/200% OS scaling on adequately sized physical displays; final supported matrix belongs to product/UX. Test long tags, branches, error/result wrapping, objective/hints open, narrow split, menus/dialogs, visible focus and runtime monitor. Execute challenge entirely by keyboard; Delete/Ctrl shortcuts must not mutate an unfocused/hidden editor or leak through dialogs. Run screen-reader semantic feasibility explicitly; historical frame-only accessibility tree cannot be called accessible. Noncolor labels and reduced camera/celebration motion must preserve observable plant truth. No repeating-line/grid/blueprint backgrounds. Review active process framing and sensor labels separately from collision/physical claims.

Exact offline build: constructor rejection before channel factory; direct disabled handler invocation remains local; command-line/diagnostic/MCP paths cannot switch source; PCK inventory excludes Python transport, physical profiles, tests/developer sources/secrets. Process inspection confirms no listener/bridge/child surviving exit. Tests use fake local channels or in-memory adapters; no plant contact. Do not add any live bench test to this challenge lane. A formal security/dependency/secret/SBOM review remains an explicit later release gate and must follow its applicable skill when actually commissioned.

Clean machine: standard nonadmin Windows account, no SDK/Godot/Python/build tools, network disconnected, copied folder with spaces/non-ASCII path, read-only installation directory and writable user data; first launch/relaunch/save/open/failed draft/cancel/close; complete five demos and challenge workflows; corrupt-file recovery; source independence; minimum GPU/Forward+ failure diagnostics; VM/RDP scope separately decided; update/rollback preserving drafts. Hash manifest after copy and retain native logs. Existing workstation copied-folder success does not close this gate.

Release approval also requires asset/font/model/audio provenance and redistribution rights, accurate runtime notices, product naming/trademark review, signing/signature timestamp and final artifact hash, privacy/telemetry policy, support/contact/known limitations, distribution/update ownership and representative learner pilot. New art inclusion is not rights acceptance. Vendor-style names must not imply supported vendor execution/import or certification. Signing may affect final hash, so qualify and record the final signed package. Human release authority remains separate from human implementation authority.

## Work packages, dependencies and estimates

Planning ranges in focused person-days, not calendar promises; overlap other departments and are not additive studio totals.

1. QA contract and fixture specification (2-4): controls/architecture freeze timing, case objectives/identity; deliver fixtures and first-failure matrix before grader implementation.
2. Harness mutation/isolation/budget suite (4-7): architecture runner seam; prove valid diversity and every negative class. Owner QA with architecture reviewer.
3. Native coordinator, persistence and offline policy regressions (4-7): UX/coordinator contracts; exact document comparisons, cancellation epochs, injected persistence failures and fake-channel paths.
4. Native visual/keyboard/DPI acceptance and retest (3-6): implemented challenge and art framing, authorized desktop slot; accessibility semantic remediation separately estimated after feasibility.
5. Exact package/clean machine/release dossier (3-6): release infrastructure, target machine, rights/signing owners. External dependencies can dominate elapsed time.

Total QA/release work allowance 16-30 person-days, overlapping developer test effort. Full campaign adds per-challenge fixture, content, accessibility and art qualification; no extrapolation to a complete game release is justified yet.

## Meeting decisions and Dot gate

Resolve before comprehensive plan approval: isolate trial and preserve exploratory state; dedicated completed-scan seam; fixed period/import eligibility; exact predicates/tolerance; cancel/close/navigation semantics; canonical result hash and revision scope; concrete budgets; supported viewport/DPI/accessibility scope; owners for recovery/rights/signing/clean machine; preserved WIP integration strategy. No arbitrary scheduled monitoring is authorized.

Dot should return the plan for revision if any requirement lacks owner, acceptance evidence, scoped files/dependencies, negative fixtures or a clear unverified external gate. Recommend Conditional planning approval only after those choices are reconciled across sections. Dot's finding of no remaining plan defect authorizes presentation to the human; it does not authorize implementation, certify current source, or close external release/physical safety gates.
