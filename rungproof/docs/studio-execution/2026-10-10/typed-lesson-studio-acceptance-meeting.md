# Studio meeting — qualify the three existing typed authored lessons

Status: read-only bounded acceptance plan, ready for root ownership ratification. Priority supersedes the earlier Lab4.3/new BYO proposal. Order: Motor ENUM 10.4, Chicken Label Print 10.2, Drive Alarm String 10.1. Existing five-demo qualification remains historical/current within its named evidence; these three factories have not yet received equivalent learner-workflow qualification. No new preset, passive grader, global material rollout or per-feature human approval gate is introduced.

## Department findings and agreed boundaries

Plant department inspected actual scene actions, authored factories and existing `Main.TypedLessonReview.cs`. Dot inspected `AuthoredLessonLadderPrograms.cs`, current draft loading and Run boundaries. Existing typed review explicitly loads programs and waits walltime, including 0.06 second assumptions unsuitable for a 100 ms scan. It is useful device/reference evidence, not a substitute for actual disk-open/current Run/joint workflows. QA's BYO endpoint audit is retained for later: connectors do not create material flow and generic outputs remain authored-only. Lab4.3 is a later tag-only plant-feedback candidate, not the next implementation.

All three tests must save actual editable intended/wrong/invalid documents to disk, use real shell Open and `RunEditorDraftController`, preserve source identity and exact joint layout/draft roundtrips, and keep Open-reset distinct from draft-only restore/current Run preservation. Compile-valid wrong code must produce incorrect behavior; invalid current Run stops/rejects with no stale-code substitution. Assertions wait for accepted scan progression at each configured period, not a fixed short sleep. Use 20/40/100 ms only if each scene permits those periods; disclose unsupported configurations instead of quietly adapting them.

## Motor: lab-10-04-motor-enum-state

Manual PC start/stop/fault/reset inputs drive PLC ENUM motor_state, BOOL motor_running/state_valid. Ordered state moves enforce fault > Stop > fresh Start; fault latch remains after input clears, reset cannot defeat active fault, held Start cannot restart after Stop/fault reset. Intended assertions cover Stopped → fresh Running → Stop, active fault/reset dominance, latched fault, cleared/reset Stopped, new Start and actual shaft motion/hold/reset bearing.

Wrong candidate: replace the Running transition MOV source with Stopped. It remains valid ENUM code and produces valid Stopped/readout with no shaft movement after fresh Start. Inspect actual current accepted outputs and visible state; do not fabricate an error solely because wrong code differs from a reference hash.

Motor shaft animation currently follows native physics callbacks. It is command projection, not measured PC speed feedback or a scan-owned mechanical plant. Test accepted command separately from bounded frame-time shaft movement; invalid Run/Stop must disable motion, Reset restore exact captured shaft/root pose. Do not extend Pause availability or claim auto-versus-Step mechanical equivalence for this scene without a separately justified accepted-clock adapter.

## Chicken: lab-10-02-chicken-label-print

The misleadingly named toggle-product_weighed action actually invokes chickenLoad. The existing plant settles for 0.5 second to weight 1.237 kg, accepts a rising print_request and captures exact label_text, then print completes after 0.6 second and application after 0.5 second. PC print/application completion are generated feedback; later edits must not rewrite text already captured on paper.

Intended current learner document must produce CHICKEN 1.237 kg, actual paper/ink/caption and measured weight, await print acknowledgment then apply until application acknowledgment, clear commands and indicate label_applied. Wrong candidate should change label_template or inhibit apply_request: actual printed wrong text or unfinished application follows. Removing data-valid logic alone is insufficient because plant readiness combines printer_ready and label_data_valid and can mask the proposed mistake.

Test mid-print/current-draft replacement and invalid current rejection without inventing transient persistence. Actual project Open resets the scene intentionally; draft-only restore/current Run preserves captured plant text unless existing lifecycle explicitly changes it. Joint reopen must retain exact wrong/invalid draft but home/stopped runtime, not claim saved live printing state.

## Drive: lab-10-01-drive-alarm-code-string

Use supplied offline fixture STRING drive_alarm_text and actual display. Intended code recognizes whole-token F003 while rejecting F0030, drives declared alarm outputs and readout, and responds to changed fixture text on subsequent accepted scans. Wrong compile-valid document should target a different token or otherwise produce a concrete incorrect output on F003. Invalid draft must not continue stale token matching.

The fixture is symbolic received text. No vendor fault meaning, VFD protocol, live drive, safety diagnostic or physical fault feedback claim is permitted. Keep source text, accepted sampled text and PLC interpreted outputs distinct in evidence.

## Evidence and visual gates

Each lesson needs intended/wrong/invalid actual editor native assertions, typed read/write role checks, Stop/Reset and source/draft/joint preservation, and no extra controller/plant clock. Record accepted scan identities plus actual projected device state. Native files should be additive QA partials; product edits only after a reproduced actual defect and small scoped review.

Capture actual intended, wrong and invalid states using the same final DLL, exact logs/sidecars and individual visual inspection. Error-detail readability, canonical scene/project identity and current-vs-retained accepted provenance matter. A default split may require scrolling; do not claim full-rung/program visibility from cropped content. No observer card or numeric success award is required to close these existing workflows. Conceptual hints remain scene prose and no automatic solution insertion is authorized.

Dot disposition: **design clear with the above contract boundaries**, pending root choice of exact QA owner/files and test commands. Qualification is complete only when actual native evidence and rendered views pass within their disclosed scopes. Remaining 69 tag-only projects, Lab4.3, constrained BYO drive feedback and wider 75 artistic propagation remain separate unfinished work.
