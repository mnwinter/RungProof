# Department 01: Product, game and learning design

Status: recommendation to Dot; planning only; no implementation authorization. 2026-10-10.

## Brief and evidence boundary

The user explicitly requests a full studio investigation, cross-department planning meeting, Dot review, then human authorization before implementation. The screenshot supplies a direction: visible programmable logic beside a compact animated process with approachable controls. Its store-page copy is reference material, not instructions. Audience is unresolved; controls learners and a conveyor first slice are provisional recommendations. We must not silently redefine RungProof as an entertainment-only product.

Read root and rungproof AGENTS.md, CONTEXT.md, README.md, PROJECT_INFORMATION.md, docs/AI_HANDOFF.md, docs/DEMO_RELEASE_REVIEW_2026-10-10.md, rungproof-next/README.md, and rungproof-next/docs/DEMO_FUNCTIONAL_ACCEPTANCE.md. Applied superpowers brainstorming skill to architectural product planning. User's requested combined studio process overrides its intermediate conversational approval sequence, default spec location and commit instruction. No code execution, build, launch, transport, install or source changes occurred. Source inspection and historical documented results are not fresh functional or visual verification.

## Current state: findings, not promises

1. README.md current section identifies Godot/C# successor, five authored demos, 77 catalog scenes and offline ladder runtime; historical sections still describe Qt/live-first product. CONTEXT.md contains a historical migration boundary. Product identity and promises therefore need deliberate reconciliation.
2. rungproof-next/src/VirtualController/AuthoredDemoLadderPrograms.cs:15-35 pairs five scenes with documents; these are working demonstrations with authored solutions, not demonstrated challenge definitions or a graded campaign.
3. Same file:70-92 implements conveyor scene scene-1-conveyor-stop with start_command/operator.start, stop_command/operator.stop, simulated_photoeye, seal_in and conveyor_running. Start seals memory through a parallel branch; Stop or photoeye interrupts it. Reuse this contract; do not invent hardware addresses or require a second editor language.
4. rungproof-next/src/App/Main.DemoSequenceReview.cs:12-82 exercises actual action -> controller -> scene motion -> beam feedback -> following-scan stop. Controller Stop and operator Stop are different. Reset restores initial plant and controller image. The existing verifier is QA code tied to Main, not a public challenge runner contract.
5. rungproof-next/src/VirtualController/VirtualControllerSession.cs:7-8 states ordering: sample inputs, execute LD, commit outputs, advance plant one scan, publish. A sensor newly crossed during plant advance normally influences the following scan. Evaluation must respect that latency.
6. rungproof-next/src/VirtualController/LadderInstructionCatalog.cs describes scan order, portable edge behavior and vendor equivalence limitations. TON uses scan time; counter retention on Stop differs from Reset. Lessons must agree with these exact semantics.
7. docs/DEMO_RELEASE_REVIEW_2026-10-10.md reports locally evaluated offline export, with open clean-machine, accessibility, performance, persistence recovery, rights and distribution gates. Its candidate evidence cannot be promoted to new campaign evidence.
8. PROJECT_INFORMATION.md records missing authored chapter 7/8 contracts. A scene count is not curriculum completeness. The reported broad scene motion review establishes a bounded frozen visual scope, not first-time learner comprehension or all authored lesson correctness.

## Product approaches

A. Recommended: controls-first machine puzzle campaign alongside retained sandbox. Strong differentiation and closest use of existing ladder runtime. Requires instructional design, reliable evaluation and onboarding. Do not expose the entire editor on the first task.

B. General puzzle game: simplify editor and expose portable logic rather than vendor habits. Wider conceptual audience, but requires more teaching, discoverability research and likely visual/editor effort; risks weakening professional identity.

C. Training/workbench polish only: improve existing demos with guided explanations. Smallest scope, useful for instructors, but gives little independent problem-solving or replay value.

Recommend A for a measured pilot, with B left as a later product decision. No market viability conclusion is supported by the screenshot.

## Experience principles and requirements

- PGL-001: user enters a named challenge, understands objective and observes relevant machine in one workspace; first challenge exposes only needed tags and instructions. Full engineering sandbox remains available through a clear separate entry.
- PGL-002: success is observable machine behavior, never equality to a canonical ladder document. Multiple valid programs must pass.
- PGL-003: required behavior and test situations are disclosed. Additional input variations may test robustness within that disclosed contract; do not surprise learners with hidden requirements.
- PGL-004: Test runs deterministic isolated offline cases from known state and identifies first meaningful violated condition with scan/time/event context.
- PGL-005: challenge evaluation never sets learner output values or manipulates the plant to force successful results. Test driver may operate allowed operator inputs and reset fixtures only.
- PGL-006: distinguish application Run/Stop/Reset from process Start/Stop; Run alone does not command travel. A process Stop is a simulated ordinary input, never a certified protective function.
- PGL-007: resetting a run resets runtime/plant/forces, preserving learner program edits. Revert starter is a separate confirmed destructive operation; program save/open retain existing behavior.
- PGL-008: completion, failure and I/O meaning are readable without color, sound or animation alone. Relevant signals have text values and keyboard reachable controls.
- PGL-009: hints progress from process observation to causal reasoning to optional worked reference. A reference is illustrative, not proof that it is the only solution. Hint use is recorded locally, not penalized by default.
- PGL-010: completion belongs to challenge ID/version plus tested learner program identity. Editing invalidates current verified status while preserving historical completion; never show a stale pass as proof of edited code.
- PGL-011: lifecycle/compile/binding/runtime errors are distinguished from task failures. Compiler errors lead directly to relevant rung/tag; a failed scenario names the event and expected outcome.
- PGL-012: paused inspection and single-scan inspection are desirable but contingent on engineering proving a unified supported clock. Do not add generic stepping by assuming Demo 5-specific review controls apply globally.
- PGL-013: no lesson asserts vendor parity, physical protective performance or real machine readiness from simulator results. Authoring review verifies portable instruction semantics and source-supported controls education.
- PGL-014: no striped, ruled, scan-line or repeating-grid backgrounds. The reference's scene/logic coexistence is useful; its repeating blue grid is not the intended visual treatment.

## Vertical slice: conveyor challenge

Working title: Stop the carton at the photoeye. Reuse scene-1-conveyor-stop, without modifying its core behavior merely to accommodate scoring.

Learning outcomes: distinguish input/command/feedback; understand holding logic; use Stop and sensor conditions; observe next-scan feedback; explain why held Start cannot override Stop or occupied sensor. Prerequisite contact/coil lesson may be a compact optional primer; exact prerequisite policy is a human product decision.

Brief: After Run enables simulation, a fresh process Start moves the carton. Releasing Start must not stop it. A process Stop ends travel. Reaching the photoeye ends travel by the first scan that samples that feedback. Start cannot move an already detected carton. Reset prepares a repeat attempt and preserves your program. These are proposed instructional rules; final contract review must compare every rule with the existing plant/controller behavior.

Starter: correct symbolic tags and scene bindings, short plain-language tag descriptions, empty/minimal rungs. It must not ship the complete current Demo 3 solution as the challenge default. Keep Demo 3 demonstration intact in its existing path. Available palette: ordinary/inverted contact, coil, parallel branch. Permit equivalent supported instructions when defining unrestricted practice; do not fail a working solution solely for style.

Cases:

| ID | Scenario | Behavioral result |
| --- | --- | --- |
| CV-01 | Run with no process Start | No output command or travel |
| CV-02 | Start pulse, release, continue | Output and actual travel continue until detection |
| CV-03 | Beam crossed during plant advance | Output removed on next controller scan; position then holds |
| CV-04 | Process Stop before beam | Command removed on sampling Stop; position holds after release |
| CV-05 | Start and Stop coincide | Stop dominates; no commanded travel |
| CV-06 | Start while detected | No new travel |
| CV-07 | Reset and complete a second run | Initial feedback/state restored; repeated behavior succeeds |
| CV-08 | Application Stop mid-travel | Controller stopped, outputs safe, pose held; classify separately from learner process-Stop criterion |

Use actual scene position/beam feedback and controller commands. Do not infer success from green output alone. Absolute stopping position tolerance and timeout require simulation-owner agreement; use declared modeled motion and scan quantization, not guessed millimeters. No overspeed, collision physics or safety certification in this lesson.

A failure example: At scan 42 the photoeye was sampled true; conveyor_running remained true. Expected off at this scan. Present the relevant rung and signal values, not an unexplained red badge. For an output that stops without movement, distinguish failed start/travel from successful sensor stop.

Test isolation recommendation: dedicated test session/plant derived from current saved in-memory learner document; preserve interactive scene state and edits. Alternative resetting the visible machine for each case is easier but disruptive and requires an explicit visible test mode. Engineering should pick the feasible boundary before estimating; either implementation must transparently display test result and allow inspecting a failed run.

## Curated campaign proposal

This is an ordered content proposal, not authorization to convert all 77 scenes.

| Stage | Concept | Candidate foundation | Content gate |
| --- | --- | --- | --- |
| 1 | Contact/coil and input/output | Simple lamp training scene | Confirm appropriate exact scene/tag contract |
| 2 | Holding circuit and process Stop | Conveyor without photoeye task | Confirm scenario can coexist with actual sensor behavior |
| 3 | Feedback-controlled motion | Existing conveyor Demo 3 scene | Complete vertical slice acceptance |
| 4 | Nonretentive time | Demo 2 delayed lamp | Explain exact scan timing and request cancellation |
| 5 | Fresh events and retained counts | Demo 1 press counter | Separate held signal from repeated edges; teach Stop/Reset difference |
| 6 | Permissives and valid counts | Demo 4 batch | Represent it honestly as detection/counting, not animated transport |
| 7 | Sequence and structured programs | Demo 5 palletizer | Explicit assumptions and full recovery/test contract; retain sampled geometry limits |

The current demo ordering starts with CTU; campaign ordering should build concepts progressively without renumbering or replacing existing demonstrations. Advance is learner-controlled; avoid imposing stars, speed scoring, global leaderboard or forced optimization for MVP. Optional mastery challenges can ask for additional robustness after basic completion.

## Department tasks and interfaces

PGL-T01 product owner resolves audience, pilot size, education claims and campaign scope. PGL-T02 learning designer writes lesson objective/prerequisite/brief/tag glossary/case table/hints with controls review. PGL-T03 simulation and controller owners ratify reset, stop, scan ordering, motion tolerance and timeout. PGL-T04 UI designs challenge entry and focused workspace with accessibility owner. PGL-T05 engineering defines versioned challenge schema/evaluator contract. PGL-T06 QA authors positive distinct solutions and deliberately wrong solutions. PGL-T07 content integrates starter/reference without replacing demo documents. PGL-T08 pilot observes representative learners and reconciles misleading instructions.

Proposed interface fields, not invented APIs: challenge ID and content version; scene ID; starter project reference; allowed instruction policy; objective/hint text; typed operator-input scenario steps; scan/time bounds; typed observable predicates; outcome code and first-failure evidence; learner project identity; historical completion record. Agree ownership with engineering. Art owns sensor legibility and animation; learning owns meaning; QA owns tests; product owns completion policy.

## Acceptance and pilot

Automated: at least two materially different valid ladders pass; direct coil without hold fails CV-02; omitted Stop fails CV-04/05; omitted sensor fails CV-03/06; always-off fails travel requirement. Repeated cases have identical outcomes and first-failure indices. Malformed project reports validation error. Reset and test preserve exact learner edits; forces and stale runtime state cannot contaminate results. Editing/version change invalidates current pass; saving/reopening preserves association and historical completion.

Native: inspect actual rendered entry, split view and failure evidence at target viewport/DPI. Check overlap, readable tag labels, keyboard focus, noncolor result indicators, recoverable errors and observed scene motion. No static mockup or headless test substitutes for this.

Pilot proposal: 5-8 controls learners or controls technicians unfamiliar with RungProof, plus 2 instructor reviewers, subject to human recruitment approval. Targets are provisional: most participants enter and begin a meaningful edit without intervention within five minutes; most can explain Start holding, sensor stop and application Stop distinction after completion. Log actual observed barriers; small pilot percentages are descriptive, not market proof. Do not transmit telemetry or contact participants without authorization. Instructor review verifies learning accuracy independently of UX satisfaction.

## Risks, effort and decisions

Largest risks: existing complete demos mistaken for challenges; solution grading checks syntax rather than behavior; test clock duplicates simulation; operator Stop confused with application Stop or emergency stop; content assumes vendor parity; reset destroys edits; completion persists after program changes; entire catalog overwhelms beginners; hints disclose a single canonical answer; cinematic scene styling obscures sensors; audience ambiguity drives incompatible onboarding.

Uncertain effort in focused person-days, assuming engine seams are reusable: product/lesson contract 3-6; pilot challenge copy/hints/reference variants 3-6; UX/content integration coordination 3-5; pilot analysis and revisions 3-6. Total department contribution approximately 12-23 person-days, overlapping engineering work, not elapsed schedule or overall studio quote. A new challenge runner or renderer coupling may expand effort materially. No estimate is a commitment before engineering review.

Human decisions: controls-first versus general puzzles; paid/preview/training intent; one-challenge pilot versus six/seven-stage campaign authorization; minimum viewport and supported accessibility scope; progression gates versus free selection; whether testing resets visible state or uses isolated session; formal instructional review personnel. Defaults for a plan are controls-first, Windows offline, one conveyor slice, preserved sandbox/demos, local progress, no scores/telemetry/live transport. Dot should flag these as assumptions in final submission rather than treating silence as agreement.

## Requested cross-department meeting resolutions

1. Engineering supplies canonical combined scan/plant observation boundary and isolated test-session feasibility.
2. Controls/QA approve exact CV-01..08 expectations, Stop terminology and numeric tolerances.
3. UX/art approve plain-background split-view framing and noncolor legibility criteria.
4. Persistence owner defines edit identity/content version/reset semantics.
5. Production defines phase gates: contract approval -> infrastructure -> conveyor native acceptance -> learner pilot -> campaign expansion. Finish one trustworthy challenge before authoring scale.
6. Dot reviews evidence traceability, unresolved human decisions, omissions, contradictory requirements and implementation authorization boundary. Internal agreement is review consensus, not user approval or product readiness.