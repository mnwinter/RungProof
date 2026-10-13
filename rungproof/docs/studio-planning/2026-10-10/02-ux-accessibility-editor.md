# Department 02: UX, accessibility and editor experience

Planning recommendation to Dot, 2026-10-10. Implementation requires human authorization after studio synthesis and Dot review.

## Scope and evidence

Read planning baseline and product department report, applicable root/rungproof AGENTS, and focused current SimulatorShell.cs, LadderEditorCanvas.cs and Main.cs source. Applied brainstorming instructions to this architectural proposal; the user's studio approval workflow governs the sequence. No app launch, screenshot capture, builds, tests or runtime qualification occurred. This report identifies source-supported risks, not confirmed rendered defects. Human screen is reserved. Audience and conveyor-first remain proposals.

References below are relative to rungproof/rungproof-next/src/App and refer to the current working source, not a committed artifact. Baseline WIP must be preserved before implementation.

## Findings

1. SimulatorShell.cs:3923-3974 constructs one ladder workspace with TIA Portal and Studio 5000 views. Both are valuable familiar presentations, but onboarding into two branded styles risks implying complete vendor behavior. Challenge mode should reuse the existing document/runtime while introducing only the chosen presentation; do not implement a competing editor or claim vendor parity.
2. SimulatorShell.cs:2432-2495 applies toolbar clearance, minimum split ratios and compact defaults. Below 1900 logical pixels in split mode, project/tools/bottom docks may initially collapse, with reopen handles. At narrower widths the logic workspace minimum competes with the 400-pixel scene minimum. A challenge objective and result panel cannot simply be added above this layout without testing the available height and minimum widths.
3. SimulatorShell.cs:331-366 and 2460-2470 handle split dragging through pointer input. The examined divider path offers no equivalent explicit keyboard resize command. This is a scoped observation, not a repository-wide assertion that all split containers lack keyboard support.
4. LadderEditorCanvas.cs:48 makes the canvas focusable. Lines 223-301 consume GUI edit keys and gate global shortcuts on both visibility and focus. Copy/paste, deletion and horizontal moves exist. Preserve that gate: hidden vendor tabs and other focused controls must never consume edits. Those shortcuts do not by themselves establish complete keyboard selection/insertion/branch editing.
5. LadderEditorCanvas.cs:420-445, 594-628, 663-731 custom-draw labels with many fixed font sizes between 10 and 14 and geometry-based hit regions around lines 303 onward. Theme font changes alone cannot be assumed to scale these labels, spacing, hit testing or accessibility semantics. Long symbolic tags and numeric text require tests with actual content.
6. SimulatorShell.cs:4071-4110 queues unsaved scene drafts, permits saving invalid work in progress, presents Save/Discard/Cancel and initially focuses Cancel. Challenge navigation, starter reversion and campaign exit must reuse these safeguards rather than silently replace a document. Cancel should return to the invoking control and leave the document unchanged.
7. SimulatorShell.cs:5323-5327 invalidates verification display on changes; 5943-5956 builds structured validation results. Lines 10333-10421 contain historical verifier assertions for active monitor identity, invalidation on edits and restoration by undo. A new completion badge must use distinct tested-program identity, without replacing this existing monitor-match behavior.
8. Main.cs:534-560 separates application Run/Stop/Reset from process Stop pulsing. Lines 898-929 connect snapshot updates; 1021 explicitly warns publication precedes projection. UI result evidence must consume engineering's final copied observation contract rather than synthesize mixed-age signal rows.

## Recommended experience

Retain demonstrations and engineering workspace as existing entry paths. Add a clearly labeled challenge entry with a short objective, concept/prerequisite, expected test behavior and Continue/New Attempt. Avoid exposing all catalog scenes as a campaign. A learner should enter the selected scene and its correctly bound starter without traversing asset placement controls.

Recommended challenge workspace: persistent objective and progress header; editable ladder and framed process in the main split; compact relevant input/command/feedback table; expandable test evidence below. Use collapsible optional instruction help and hints, with their expansion state retained. Editor should be left and scene right if affordable within existing layout; current orientation is an implementation decision, not a prerequisite to the pilot. Preserve user-controlled split and provide editor-only/scene-only alternatives at compact widths. Do not shrink text until everything fits.

Use distinct groups for simulation controls (Run simulation, Stop simulation, Reset attempt) and process controls (Start conveyor, Stop conveyor). Reset description must state it preserves program edits. Revert starter is separate, explicitly destructive and protected. Run's helper text explains it enables execution and does not press process Start. An occupied photoeye shows a text state and visible indicator. Scene picking must have equivalent keyboard-reachable operator controls.

Test is a distinct lifecycle: preparing, validating, running case X/Y, canceled, failed, passed, infrastructure error. Prevent double invocation. During a test, edits must either be locked with visible explanation or explicitly versioned against the frozen tested document; never show a pass for newly edited code. Recommend short-lived edit lock with Cancel and retained edits. Case results stay inspectable after returning to normal interaction. Whether testing uses an isolated hidden session or resets the visible scene is an engineering/product decision; UI must disclose it and preserve the learner's program.

Failure evidence shows scenario, expected behavior, observed behavior, sampled scan/time and relevant signals. Compile/binding errors navigate to a rung/tag. Behavioral failures should not automatically identify an allegedly incorrect rung without causal evidence. A passing condition is explained in text. Keep live monitor and historical test trace visibly distinct; a live animation must not pretend to replay a failed test.

## Requirements and acceptance

| ID | Requirement | Acceptance evidence |
| --- | --- | --- |
| UX-001 | Objective, editing, relevant process controls and result reachable in one challenge workspace | Native rendered walkthrough and learner observation; no overlaps or inaccessible primary action |
| UX-002 | Compact fallback and adjustable layout | Native target viewport/DPI matrix; objective/results wrap or scroll; single-panel modes preserve state |
| UX-003 | Keyboard access to complete first challenge | Enter, select rung/contact, insert/delete/move/branch/edit tag, Run, process Start/Stop, Test, inspect result, save, cancel and exit without pointer; visible focus throughout |
| UX-004 | Shortcut scope and modal isolation preserved | Actual keyboard routing checks for hidden editor, unfocused canvas, text input, dialog and scene; Delete never mutates document outside focused editor |
| UX-005 | Noncolor signal/results and consistent terminology | True/False or On/Off plus meaning, textual Pass/Fail; commands and feedback named distinctly; monochrome review |
| UX-006 | Program/draft integrity | Navigation, failed/canceled tests and Reset retain exact edits/history; revert requires confirmation; failed save retains recoverable document |
| UX-007 | Legible scalable ladder rendering | Long tags, parallel branches, numeric annotations and selection visible at supported scale; hit testing and focus rectangles track scaling |
| UX-008 | Frozen result identity and explicit lifecycle | Editing invalidates current result; test result links to challenge version/program identity; canceled/infrastructure error never awards completion |
| UX-009 | Bounded status announcements | State transitions and first error announced/textual; no screen-reader chatter at scan rate; focus remains stable during monitor refresh |
| UX-010 | Assistive support investigated before claims | Authorized native UI inspection with Windows assistive tools; element names/roles/values and editor semantic navigation demonstrated or limitation explicitly documented |
| UX-011 | Motion/audio independence | Essential events readable in state/evidence text; optional camera/celebration motion suppressible; no required audio; cannot disable modeled plant motion and retain false travel evidence |
| UX-012 | Dialog recovery | Escape/Cancel restores invoking focus; Save/Discard wording names actual scope; no trapped focus, hidden modal or unannounced failure |

Minimum viewport and supported DPI must be human-approved. Proposed qualification matrix: 1280x720 and 1920x1080 at 100%, plus 1920x1080 at 125%, 150% and 200% OS scaling. Physical resolution is not logical viewport: record actual available content bounds per case. If 1280x720 is infeasible, declare a higher minimum and provide a clear minimum-size response, never silently clip. Proposed visual criterion: contrast measured on actual final colors, readable text at declared minimum, no overlapping labels or obscured actions. This is a proposed internal target, not an accessibility certification claim.

## Implementation seams and responsibilities

Proposed challenge presentation adapter receives challenge metadata, frozen test lifecycle/results, program dirty/identity status, and relevant typed signals. It emits intent requests for Test/Cancel/Hint/navigation and existing application/process actions. Engineering owns actual runtime effects; UI never writes learner outputs to complete a task. No new APIs are claimed here.

Likely touchpoints: SimulatorShell.BuildLadderWorkspace/BuildLadderEnvironment for an additive focused presentation; existing draft action guards for navigation; existing validation and monitor identity refresh paths; LadderEditorCanvas for semantic keyboard selection, scaling and accessible text representation; Main only for delegating challenge intent to an engineering-owned coordinator. Prefer new focused presenter/view files over expanding the already large shell with evaluator/content logic. Avoid a broad shell rewrite during the slice.

Art owns scene labels, palette, camera framing and indicator shapes. UX owns hierarchy, control terminology, focus routes and dialogs. Learning owns objectives/tag descriptions/failure explanation. Runtime owns observation timing. QA owns functional and native routing evidence. Product owns audience/minimum hardware and stated accessibility scope.

Accessibility is a separate feasibility gate: custom drawing cannot be assumed discoverable to assistive technology. A semantic rung/element tree synchronized with canvas selection is an option; converting the complete renderer to individual controls is a larger alternative. Decide after a narrow native probe using the pinned Godot build and Windows stack, verifying documented supported mechanisms before code. Do not invent an accessibility API or promise full screen-reader support from static source.

## Alternatives and tradeoffs

A. Add focused challenge presentation around existing editor: recommended; smallest behavioral risk and preserves familiar workflows; requires disciplined adapters and compact-layout qualification.

B. Rebuild a simplified editor: cleaner beginner vocabulary and potential semantics, but duplicates undo, save, validation, monitoring and instruction maintenance; defer until pilot demonstrates existing editor cannot teach the task.

C. Add a mission sidebar to current engineering shell: fastest visible concept, but dense controls and insufficient split widths could undermine comprehension. Accept only as an internal experiment after authorization, not polished release scope.

Full vendor-style switching in the first lesson is optional; reuse existing styles behind settings/sandbox. Never label them complete TIA/Studio emulation.

## Verification plan — not executed

After implementation and WIP-preservation gate, use repository-supported commands from rungproof-next:

```powershell
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore
& '.tools/dotnet/dotnet.exe' run --project tests/RungProof.Next.VirtualController.Tests.csproj --no-restore
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-app-shell
```

Locate pinned executables and confirm project output isolation before running; multiple C# projects can collide in intermediate paths. QA/engineering should provide exact isolated invocation where needed. Existing shell verifier must pass preserved flows; add meaningful challenge intent/navigation/identity checks without confusing emitted button signals with real keyboard coverage. Native walkthrough, actual screenshots, keyboard events, OS scaling and assistive checks are separate required evidence. No new verifier flags are asserted to exist.

## Effort, risks and meeting requests

Uncertain focused person-days: flows/content wireframes and states 3-5; additive challenge presentation integration 6-12; keyboard/focus/compact layout/scaling changes 5-10; native accessibility feasibility 2-4; native QA fixes/pilot iteration 4-8. Department range 20-39 person-days; estimates overlap engineering/QA and must not be summed blindly. Full semantic screen-reader editor or editor rebuild would require separate estimate after probe. These are planning ranges, not elapsed schedule commitments.

Critical risks: compact layout hides learning evidence; fixed canvas font geometry; another language/editor doubles maintenance; test trace mixed with current scan; pointer-only process input; shortcuts leak into hidden tabs; Reset destroys drafts; campaign switch bypasses unsaved guard; pass badge remains after edits; uncertified assistive claims; visual polish obscures inputs/feedback.

Meeting must resolve: (1) engine's atomic observation/result contract and test-lock/cancel semantics; (2) visible versus isolated test session and replay availability; (3) supported viewport/DPI and truthful accessibility scope; (4) rung keyboard/semantic navigation feasibility; (5) art camera framing/label contract without repeating-line backgrounds; (6) whether challenge orientation changes are necessary for slice; (7) precise ownership of draft persistence and result identity. Dot should reject a plan that omits native visual and keyboard qualification or advertises accessibility without evidence. Human final authorization must select audience/slice/accessibility scope and preserve working state before source changes.
