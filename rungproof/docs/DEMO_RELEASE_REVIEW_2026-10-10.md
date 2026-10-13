# RungProof demo release review

Qualification update: see `DEMO_QUALIFICATION_CONTINUATION_2026-10-10.md`. The earlier ZIP described below is retained as a baseline and lacks the subsequently verified CALL-chain compiler repair. The replacement candidate is `rungproof-next/artifacts/offline-demo/local-review-20261010-callgraph-fixed-quit/`. Its export, offline policy, shell/project guards, conveyor integration and sampled palletizer audit passed; final native and external release gates remain open. Use the continuation record for current candidate status.

## Decision and release scope

The current application has a credible offline demonstration core, but it is not yet ready for an externally distributed demo release. The shortest defensible release is a Windows offline preview containing the five authored demos and the ladder editor, with the remaining scene catalog clearly marked experimental. Live PLC capability needs its own controlled bench acceptance and must not become part of the demo promise by accident.

Reviewed source baseline: `1aeee4d7ed23fa938f5227942e7727b4ef6981dc`, clean `main` in the canonical Siemens-PLC-PC-Interface checkout. The adjacent RungProof folder is an empty Git repository. Findings below distinguish baseline defects, changes made during this review, proposed work, and external acceptance gates. No publish, deployment, PLC connection, or physical I/O action occurred.

The assessment covers application structure, controller execution, scene integration, file persistence, subprocess ownership, release tooling, documentation, asset evidence, native usability and accessibility risks. It is a broad source review with targeted reproductions and tests, not a line-by-line proof of every file or a formal completed security scan.

## Local candidate delivered after remediation

The review's actionable local blockers were subsequently repaired. An unsigned offline candidate now exports and passes checks on its actual executable. The ZIP is `rungproof-next/artifacts/offline-demo/RungProof-OfflineDemo-20261010.zip` (399,987,401 bytes); its SHA-256 is `978B17631D64486167D217E23DB390389DD5762DAB2B1CFEDB62300699D0C1A8`. The uncompressed candidate is `rungproof-next/artifacts/offline-demo/local-review-20261010-final/`, with a manifest, notices, runtime and separate build-evidence directory. The ZIP excludes build logs and its 199 manifest-listed runtime/document files were independently rehashed inside the archive.

The candidate starts from a copied folder outside the checkout, with spaces in its path and without a restore/build step. Native inspection confirmed its .NET host/runtime modules loaded from that copied folder. This is local isolation evidence on the existing workstation, not a clean Windows-machine test. The compiled offline policy prohibits both external-client constructors; direct emissions of disabled PLC menu actions leave the source local. The demo export has separate user data from the development application.

Exact build: Godot `4.7.2.stable.mono.official.ed1daf0bf`, SDK `10.0.401`, source baseline `1aeee4d` plus reviewed working-tree changes. Code/build-configuration fingerprint: `80894B3020908B9772EDF76138AA8A7B15D091D2C7FB86B7F0CE9F062C4C7830`. Exported `RungProof.Next.dll` SHA-256: `85905627AC7AC4A0039144CB0BB7A6533881D9F57CD75B9607BEFD8152DD07AD`. The build lane rejects source-code/configuration changes during export and records artifact hashes.

The candidate is suitable for local pilot evaluation. External demo release remains gated by clean-machine qualification, fuller artifact workflows/recovery, DPI/accessibility, minimum hardware, asset/notice rights, signing, support and distribution approval. Nothing was committed, pushed, merged, deployed or published. No live PLC connection occurred.

## Priority findings

| Priority | Finding | Consequence | Disposition |
| --- | --- | --- | --- |
| Release gate | Baseline had no standalone Godot export | Source launch required ignored developer tools, restore/build/import | Local offline candidate now exports and runs independently of the checkout; clean-machine acceptance and installer/signing remain open |
| Release gate | Current Godot application inherits historical Qt release/security/support documents | Contradictory renderer, scene and controller claims | Added current notices and an explicit offline candidate contract; broader product/support documentation still needs reconciliation |
| P2 | Null elements in shared ladder project collections escape rejection | Opening malformed user data throws rather than returning a readable diagnostic | Fixed with outside-in shape guards and regression tests |
| P2 | Arbitrarily small positive scan periods and unlimited catch-up | An empty 0.0001 ms program executes 200,000 scans for 20 ms elapsed time, taking about 0.4 s here | Compiler now requires at least 1 ms; each Advance yields after 1,000 scans and retains pending time |
| P2 | NaN elapsed time poisons the scan accumulator | Subsequent valid advances execute no scans until lifecycle reset | Fixed by rejecting nonfinite deltas before accumulator mutation |
| P2 | Help validation fails with 114 baseline issues | Some formatting conflicts; also missing current typed feedback and state points | Fixed the scene I/O tables in 18 documents; 295 assets/77 scenes now validate. Operation prose still needs separate review |
| P2 | Demo menu appeared after the full lab catalog | First-time users had to search through a large engineering surface | Fixed: demos appear first and welcome offers START DEMO 1 with an expected result; native exported workflow passed |
| P2 | Split-view instruction toolbar required horizontal scrolling | Important actions were clipped at 1600x900 | Fixed: compact category selector, wrapped command rows and adequate reserved height; both styles inspected natively and editor interaction verifier passed |
| P2 | Default Demo 5 framing made the process small | Difficult to see pickup and placement while explaining ladder | Fixed: process-bound default with explicit whole-scene view; four cycles sampled natively, process retained in split view; broader DPI acceptance remains open |
| P2 | Accessibility tree exposes the window frame but no application controls in this run | Screen-reader and UI automation support are unproven | Keyboard, focus, screen-reader and high-DPI acceptance required |
| P3 | Legacy Qt lifecycle test expects 25 training actions; actual count is 70 | Repository regression lane is red | Fixed by asserting the exact delivered lab-ID set and retaining fail-closed checks; all 197 legacy Python tests pass |
| Hardening | No explicit imported-file byte or collection-count budgets found | Wide JSON inputs can consume memory and UI time | Define and test practical budgets for shared project/workspace files |

The 1,000-scan limit is a work-count bound, not a hard real-time response guarantee. Pending time is retained; Stop and Reset deliberately clear it. Default 20 ms authored programs, ordinary one-scan stepping and the 25/100-scan review steps remain within the budget. Negative elapsed time retains existing clamping behavior. This runtime remains an offline instructional controller; the new minimum period is not a PLC performance claim.

## Code and architecture review

The strongest part of the codebase is the separation between typed ladder execution, I/O mapping, plant behavior and the guarded bridge. Compiler checks prevent writing input tags and reject invalid types/bindings. Controller tests exercise edges, timers, counters, task/block lifecycles, force release, Stop, Reset and restart. The execution coordinator makes sample, execute, commit, plant advance and publication order explicit. The per-scan jump watchdog prevents a backward JMP from running indefinitely and removes outputs when it trips.

Persistence has useful protections: readable work in progress is distinct from executable validation; saved projects carry their source scene; cross-scene opening preserves the loaded project; unsaved scene drafts participate in guards. Project and workspace saves write a temporary file in the destination directory before replacement. This reduces truncation risk, but does not constitute power-loss durability or backup recovery proof. Crash recovery, autosave ownership, corrupt-file preservation and schema-upgrade recovery need explicit acceptance.

The main maintainability concern is concentration of responsibilities. SimulatorShell.cs has 10,654 lines at baseline, and Main.cs has 4,579 plus many partial verification files. Menus, dialogs, editor mutation, persistence, runtime wiring and verifier assertions share these large classes. The pure controller layer is much easier to reason about than this shell. Extract focused document, connection-presentation and editor-view services gradually after the demo freeze. A wholesale UI rewrite would add release risk now.

The existing tests are substantial, but not all evidence levels are interchangeable. Pure plant tests establish modeled behavior; headless UI checks establish internal state and layout assertions; actual native screenshots establish visible behavior; none establishes physical PLC commissioning or mechanical clearance between all poses. Several verification routines are embedded in application source and are enabled by command-line flags. The release build should deliberately define which diagnostics ship and ensure no developer-only file paths or fixtures become user features.

## Security and controls boundary

The current Next application constructs an ExternalPlcRuntimeClient. The old AGENTS description of Next as purely symbolic/offline is stale. Startup remains disconnected; the bridge is separate from the Godot process and has explicit authorization and ownership checks. Source review found profile path confinement, configuration fingerprints, exact descriptor/write-scope checks, DB-only address parsing, typed point validation, serialized exchanges, deadlines, bounded stderr and subprocess abort on invalid exchanges. The 18 connection tests use local fake child processes and make no PLC network connection.

No unauthorized write bypass or remote exploit was demonstrated. The malformed project finding is a local reliability/availability defect, not evidence of code execution. The bridge stdout line has no explicit size budget; its locally selected trusted-process context matters when assessing severity. Workspace shape validation also needs adversarial fixtures for null placements, vectors, members and links, although its outer UI handler catches exceptions more broadly than the ladder path.

For an offline demo, exclude the Python transport tools and physical profiles from the delivered artifact, and make external PLC controls unavailable by build policy. Hiding a button alone is insufficient: tests must prove the demo build cannot construct or start a real transport through menus, command-line paths or MCP paths. Preserve existing guarded live behavior in development and controlled bench builds. Changing the validated DB14 contract is outside this review's scope.

For a later live bench release, record the exact CPU, firmware, TIA update, endpoint, rack/slot and profile; prove ownership in a watch table; independently observe watchdog behavior, network loss, stale feedback, reconnect, fresh Run, Disconnect and Close. Prior transport-baseline records are not acceptance of this new Godot package. Simulated E-stop behavior is not a certified protective function.

A formal security scan, release-artifact secret review, dependency vulnerability review, SBOM and executable-signature verification remain open. No security-clean or safety-ready claim is warranted from this bounded assessment.

## Native usability review

Captured this run at an approximately 1600x900 application viewport. Screenshots and test logs are retained under rungproof-next/.tools/demo-release-review-20261009. No older screenshot was used as current visual evidence.

| Step | Task and observed health | Evidence and recommendation |
| --- | --- | --- |
| 1 | Startup dialog: readable and useful | 01-startup.png explains local mode, ladder loading and Stop/Reset. Add a direct Start Demo action with an expected first result |
| 2 | Operator view: coherent, visually readable at this size | 02-main.png shows clear Run/Stop/Reset, state and ownership columns. PLC DISCONNECTED remains prominent while local simulation is valid; prioritize the active execution source |
| 3 | Scenario selection: functional but overloaded | 03-scenario-menu.png shows a long menu with labs before demos. Distinguish authored demos, empty exercises and experimental catalog entries |
| 4 | Demo 5 operator view: functional, weak framing | 04-demo5.png shows readable actions but small machinery. Run and Start were accepted; subsequent feedback showed carton_at_pick false and gantry_home true after the cycle |
| 5 | Demo 5 ladder monitor: functional and legible | 05-demo5-ladder.png shows a green monitored CALL path, block tree and live scan count. TIA Portal/Studio 5000 tabs and PLC_1 [S7-1500] can imply vendor compatibility; use conspicuous style/simulator naming |
| 6 | Split view: useful, crowded | 06-split.png shows the scene beside live ladder. The instruction toolbar is horizontally clipped; only a small amount of ladder is visible above diagnostics and I/O |
| 7 | Stop and Reset: observed | Stop showed stopped state and removed commands; Reset restored scan 0, carton_at_pick true and stopped initial state. Exact timing remains deterministic-test evidence |
| 8 | Demo 1: visible threshold behavior verified | 07-demo1.png and 08-demo1-threshold.png show first two presses off and third press green/threshold_lamp true. Before Run, the authored demo displayed LADDER NOT LOADED; Run loaded it, making the pre-Run wording misleading |
| 9 | Demo 2: native timer transition observed | Request true initially left delayed_lamp false; a later snapshot showed delayed_lamp true and a green physical rendering. 09-demo2-timed-lamp.png. Exact two-second timing is controller-test evidence, not screenshot timing |
| 10 | Demo 3: native conveyor sequence observed | Run and Start moved the carton; photoeye became true at position 0.5 and conveyor_running became false. 10-demo3-photoeye-stop.png |
| 11 | Demo 4: native counter edge observed | After Run, valid-type/count-request inputs and a pallet-detected edge produced pallet_count 1. 11-demo4-count-one.png. Full batch threshold/reset paths remain deterministic-test evidence |
| 12 | Smaller window now inspected; high DPI, full keyboard and screen reader outstanding | Source and exported package inspected at 1200x675. Palette hides at short height and engineering panes require scrolling; full editing usability at that size is not passed |

Post-repair native evidence: 14-wrapped-palette-final.png and 15-studio-wrapped-palette.png show both editor styles with all Bit Logic commands visible at 1600x900. 16–22 capture focused Demo 5, successive normal placement endpoints, whole-scene restoration and split Stop/Reset. 23–28 capture the isolated exported application, disabled PLC controls, welcome entry, Demo 1 third-press result, demo-first menu/reset and minimum-size focused split view. These are sampled screenshots; no continuous between-frame geometry claim is made.

Scene actions and controller Run are different operations. The welcome flow should explicitly say: choose demo, Run the local controller, operate its Start/input control, observe output and feedback, Stop, Reset. Give each demo one visible expected result and an unobtrusive completion indicator. Show counts/timer state near the plant where relevant, not only in engineering panes.

The current visual style is consistent and provides text alongside many states. It is still dense: narrow sidebars, repeated diagnostic information, small labels and multiple names for the same scene/demo compete with the process. Focus a demo mode on the process and explanation; keep the full engineering workspace available as a deliberate next step. Avoid changing editor shortcuts or docking behavior without native regression checks.

Accessibility is not passed by screenshots. Test keyboard-only selection/editing, visible focus, dialogs and focus restoration, 125/150/200 percent DPI, contrast, color-independent monitoring, text scaling, motion pause and assistive-technology names. The poor captured accessibility tree is an observed limitation of this integration, not proof that every assistive technology fails.

## Fresh verification at review baseline

| Check | Result and limit |
| --- | --- |
| Pinned .NET build | PASS, zero warnings/errors |
| Virtual-controller suite | 159 PASS, 0 FAIL before repairs; 163 PASS after four meaningful regressions and fixes |
| Connection suite | 18 PASS; offline child processes only |
| Godot app-shell verifier | PASS, 77 scenes, 3 groups, 5 demos, 295 assets; expected missing-workspace negative-test warning |
| Ladder-editor verifier | PASS, editing, persistence, monitoring, docks, shortcuts and validation assertions |
| Demo 3 integration | PASS, normal output to carton transform to actual beam feedback and following-scan stop |
| Timer lessons | PASS, modeled scan boundaries and lifecycle |
| Palletizer audit | PASS, 2,576 samples, zero static candidates and zero reported failures; sampled prescribed kinematics only |
| Broad scene geometry/workflow verifier | PASS; bounds screen, not mechanical or live acceptance |
| Eight focused C# plant projects | PASS; model/reference checks, not every native workflow |
| Next Python tools | 37 PASS at baseline; 45 PASS after adding eight help-sync regressions, using bundled Python. Initial bridge venv run failed to import Pillow in three test modules |
| Legacy Python tools | Baseline 197 run, one stale-count failure; final 197 PASS after catalog-based assertion repair |
| Legacy Node tools | 28 PASS, no PLC connection attempted |
| Catalog validator | PASS, 63 production and 232 candidate assets |
| Production asset evidence | PASS for 63 production assets; candidate evidence remains advisory/pending |
| Strict production industrial reference validator | PASS, 63/63 coverage and comparisons; candidate reference coverage 193/232 |
| Help validator | Baseline FAIL with 114 issues; final PASS for 295 assets/77 scenes. Repeating the sync changes zero files |

The initial Pillow failure is environment selection, not a runtime-app failure. The development setup should identify the Python tooling environment rather than imply the bridge venv covers all visual-review tools. Current package testing must use the same release assembly/assets, not a developer cache that can mask omitted files.

Final verification repeated the pinned build (zero warnings/errors), 163 controller checks (zero failures), current app-shell checks, Demo 3 mesh/feedback integration, both Python suites and help validation. The broader geometry, reference, connection, plant and Node results above were obtained earlier in this review; they were not all repeated after the targeted repairs. No real PLC transport was constructed or contacted by the controller regression suite.

## Changes implemented and remaining engineering work

Implemented: reject null ladder collection elements before dereference; reject nonfinite elapsed time; require a minimum 1 ms simulator scan; yield after 1,000 catch-up scans without discarding pending time; add four controller regressions; synchronize only scene IDs/I/O tables while preserving authored help; add eight malformed/duplicate/preservation/idempotence help-sync tests; repair the legacy catalog assertion; reconcile architecture notices; wrap the instruction palette with compact category selection; add demo-first navigation and welcome entry; focus the Demo 5 process with whole-scene restoration; prohibit transport in a compiled offline build; add its policy regression project, Windows export preset, required solution file, checked build lane and candidate operating/acceptance contract.

These are working-tree changes, not a committed, signed or released artifact. The new help synchronizer deliberately does not certify operation prose. For example, dual-spindle help still describes older reference-only limitations and its abbreviated safe-state/start-command tables omit the newer feed/retract controls. Current source has a controller-owned dual-spindle plant. That document needs a dedicated source-and-native workflow review before supporting that lab externally; the five-demo preview does not depend on it.

The observed toolbar clipping came from an outer horizontal ScrollContainer, fixed-width selection/command rows and category tabs. The repair preserves command identities and verification paths, replaces category tabs with a compact selector, wraps command rows and permits vertical overflow at unusual widths. The first native iteration exposed a clipped second row; reserved height was corrected and re-inspected. The exported editor interaction verifier also passed drag/drop, shortcuts, category instructions and help. Dense upstream engineering menus still scroll at narrow widths.

The camera repair fits the full active gantry, pickup table, pallet and carton/placed-carton bounds, preserving the existing framing margin. It does not move or hide equipment. Whole-scene framing remains selectable, and the choice survives aperture resizing; a new scene resets the default. Existing visual-review framing is preserved. Four native normal cycles and sampled endpoints, split Reset and process/whole restoration were inspected. The exported minimum-size view shows a larger active process, with unrelated edge equipment intentionally outside the process frame.

The Next export lane is implemented separately from the Qt lane. Matching official templates were downloaded and verified against SHA-512. The first export returned zero despite a missing C# solution and produced an unusable EXE/PCK; this was repaired, and the lane now rejects export errors and omitted assemblies. It inventories 2,548 packed resources, excluding tests, tools, authoring/review assets and Blender sources, and verifies the exact candidate executable. Runtime/engine notice copies are included; this does not complete asset/font/product legal clearance.

Changed-file scope: three controller files and their existing test runner; Main/SimulatorShell UI and build-policy wiring; guarded-client constructor plus new RuntimeBuildPolicy; project/solution/export configuration; new offline-policy tests/build script/candidate document; help generator/new sync tests and 18 help documents; one legacy Qt test; current architecture/security/support/release notices and this report. The reviewed baseline was clean, so there were no pre-existing tracked dirty changes to merge around. All work stayed local. Godot-generated untracked .uid files were preserved separately from authored changes; no reset, clean or broad removal was used.

## Final scoped acceptance ledger

| Gate | Final status |
| --- | --- |
| Reproducible internal Windows offline export | PASS; checked build lane, required assembly, resource inventory, code/configuration stability check and hashes |
| Actual artifact transport policy | PASS; constructor rejected, bridge factory called zero times, disabled UI actions cannot change source |
| Actual artifact shell/persistence/demo selection | PASS; 77 scenes, 3 groups, 5 demos, 295 assets; exact five scene/editor/program matches |
| Actual artifact ladder interaction | PASS; both styles, editing/undo, shortcuts, drag/drop, help, monitoring, watches and lifecycle assertions |
| Actual artifact conveyor integration | PASS; output to mesh motion to beam feedback to following-scan stop, plus Stop/Reset |
| Actual artifact palletizer audit | PASS; zero reported failures/static candidates, prescribed routes and sampled bounds only |
| Native isolated artifact | PASS for startup/relaunch, welcome Demo 1 entry/third-press result, Stop/Reset, demo-first menu, offline entries, palette and focused split views |
| ZIP integrity | PASS; 199 manifest-listed files rehashed within archive; build evidence excluded |
| Development controller/connection regression | PASS; 163 controller checks, 18 offline fake-child-process connection checks; no real PLC |
| Native all-five core smoke workflows on final artifact | PASS for the bounded core interactions and Stop/Reset: Demo 1 third-press lamp; Demo 2 delayed lamp; Demo 3 photoeye stop; Demo 4 five-edge batch; Demo 5 one-carton pickup/place/home. Full acceptance remains PARTIAL: final-artifact four-carton layer, mid-cycle recovery and broader failure-path scripts were not exercised in this smoke pass |
| Clean Windows machine, standard account/read-only installation, upgrade/rollback | OPEN |
| 1200x675 full editing, high DPI, keyboard-only, screen-reader acceptance | OPEN; minimum window sampled with stated limitations; 1600x900 editing is the tested default |
| Minimum GPU, VM/RDP, measured performance and long-duration soak | OPEN |
| Corrupt/interrupted save recovery and practical imported-file budgets | OPEN |
| Representative-user pilot, formal security/dependency review, rights/signing/support/distribution | OPEN |
| Live PLC, physical safety/commissioning, vendor parity, continuous clearance | OUTSIDE offline preview; unverified |

### Final-package native smoke follow-up

The same copied final candidate at `C:\Users\mattn\AppData\Local\Temp\RungProof Demo Review 20261010 fa5254cb` was launched again and Demos 2–5 were operated through the actual Windows UI. No source or package rebuild occurred. Evidence is under `rungproof-next/.tools/demo-release-review-20261009/`; screenshots 29–45 record the following observed results:

| Demo | Observed core behavior | Stop / Reset | Screenshots |
| --- | --- | --- | --- |
| 2 | Request true initially left delayed_lamp false; later delayed_lamp true with green lamp. This was a sequence observation, not a precision timing measurement | Stop cleared lamp; Reset cleared request and returned scan 0 | 29–31 |
| 3 | Carton moved with conveyor_running true; simulated_photoeye then true at object_position 0.5 and conveyor_running false | Stop entered STOPPED; Reset restored position 0, photoeye false and scan 0 | 32–35 |
| 4 | Enabling permissives after a held detection did not fabricate an edge. Five fresh valid detection edges produced pallet_count 5 and pallet_count_valid true | Stop published count 0/valid false; Reset cleared detection/type inputs and scan 0. Internal counter retention across Stop was not separately checked here | 36–40 |
| 5 | One carton was picked under vacuum, transported, visibly placed, and gantry returned home; placed_cartons 1, cycle_in_progress false, palletizer_fault false, vacuum_pick/gantry_cycle false | Stop entered STOPPED; Reset removed the placed carton, restored a carton at pickup and placed_cartons 0/scan 0 | 41–45 |

The owned application closed normally with exit code 0 and a subsequent window inventory contained no RungProof window. The original ZIP SHA-256 remains `978B17631D64486167D217E23DB390389DD5762DAB2B1CFEDB62300699D0C1A8`. This closes the previously missing all-five core native artifact smoke evidence; it does not close clean-machine qualification or the fuller recovery/multi-carton release gates.

## Packaging and release engineering

A Windows export preset and repeatable Next candidate lane are now implemented in `rungproof-next/tools/build_offline_demo.ps1`. The older run_checks.ps1 lane builds Qt and cannot qualify Godot. The new manifest records source identity/dirty state, code/configuration fingerprint, tool versions and runtime hashes. The checked export excludes developer tools, test fixtures and authoring/review material; the actual application has its compiled offline policy. Complete rights/support/signing review before using this internal candidate as an external release.

A source launcher is useful for developers, but requiring SDKs and NuGet access is not a polished demo installation. Decide portable ZIP versus installer. Test a clean supported Windows environment without the developer checkout, offline first/repeat launch, paths with spaces, a standard account, read-only install location, corrupt/missing resources, upgrade, uninstall and rollback. Verify no unexpected listener, browser, child process or transport remains after closing the actual package.

Forward+ rendered successfully on an RTX 5070 Ti in this run. That does not establish integrated-GPU, VM or RDP compatibility. Measure startup time, scene-switch time, memory, frame pacing and UI response on declared minimum hardware. Establish a tested rendering fallback or accurately narrow the hardware promise. Do not inherit the older software-QWidget VM claim.

Version, About, package metadata, changelog and tag must agree. Add a compact diagnostics export with build identity, execution source, renderer and active scene, excluding plant data and secrets. Establish signing, distribution and rollback procedures before broader external delivery.

## Assessments easily missed

| Area | Required decision or evidence |
| --- | --- |
| Educational validity | Declare the supported instruction subset and lifecycle semantics; review timer, edge, task and force explanations with a controls educator |
| Vendor parity | TIA/Logix-style editing is not a TIA project/ACD import-export or CPU-equivalence promise; separate styles from proven compatibility |
| Scene quality | Candidate inclusion is not asset approval; restrict supported demo assets or disclose candidates clearly |
| Documentation | Reconcile actual operation, not just exact table formatting; contract-validator green does not prove lesson instructions correct |
| Recovery | Preserve unreadable projects; test interrupted saves, failed replacements, retained drafts, schema versions and backups |
| Privacy | Decide logs, retention, diagnostic contents and opt-in sharing; do not assume telemetry exists or is absent from an uninspected artifact |
| Rights and distribution | Review engine/runtime/font/asset notices, reference-model provenance, ownership/employer IP and product-name clearance; no legal clearance concluded here |
| Support | Publish contact, supported environments, known limitations, reproducible issue template and version-identifiable diagnostics |
| Pilot effectiveness | Observe representative users completing a demo without coaching; measure first-success time, failures and support intervention |
| Release operations | Assign freeze owner, release approver, rollback artifact and criteria for stopping distribution |

## Outlook and work order

The product's strongest opportunity is making the relationship between ladder logic, plant motion and feedback observable in one offline workbench. Five authored demos provide a coherent first experience. The 77-scene catalog offers breadth, but breadth also multiplies support, reference accuracy and regression work. A narrow, honest preview is more useful now than a broad compatibility promise the evidence cannot support.

Do not add another large feature set before proving the delivery and teaching loop. After the demo pilot, prioritize clearer lesson outcomes, a reliable project workflow and evidence-backed scene quality. Gradually reduce shell coupling. Live PLC support, full curriculum coverage and vendor parity should have separate acceptance programs.

The first local remediation stages are complete: robustness/help regressions, candidate contract, demo entry/framing and checked offline export. Remaining order: qualify the exact package on a clean machine; complete artifact workflows, recovery and DPI/accessibility checks; observe a small user pilot; resolve rights/support/dependency findings; freeze and sign the approved artifact. The original review's 2–4 focused engineering-week allowance was a judgment estimate for the full narrow-preview work, not a measured remaining schedule. Much local work is now done; remaining duration depends on access to qualification environments and external release decisions. A live PLC release or broad hardware support requires additional qualification.

Release only when the actual artifact starts without developer tools, all supported demo paths pass, malformed input is rejected without losing work, applicable checks are green or explicitly excluded by a documented product boundary, and support/rights/distribution gates have owners and evidence. A passing local build does not close those gates.
