# Independent Lab 2.1 evidence review

Read-only review of actual PNG pixels, recorded UI action plans, and help. No app input, simulator tests, source edits, or remediation plan.

## Verified observations

- lab-2-01-entry.png: correct scene, STOPPED, ladder not loaded, lamp dark.
- lab-2-01-run.png: blank Run is blocked with actionable EDIT001/no ladder networks. This is expected for the learner-authored exercise.
- lab-2-01-authoring-contact.png: one visible NO material_call_pressed contact driving material_call_on coil. The old no-network detail remains visible while top Error List status changes to NOT VERIFIED.
- lab-2-01-run-authored.png: runtime/monitor RUNNING at scan25 and monitored ladder present, but NOT VERIFIED, EDIT001/no networks, and RUN BLOCKED/current draft invalid remain.
- lab-2-01-editor-settled.png: same contradictory messages persist at runtime scan5030/monitor scan5029. This establishes a persistent diagnostic-presentation issue, not merely a switch/capture delay. Registered QA-UI-002.
- lab201-held-checkpoints.jpg: dark initial lamp; blue while mouse held; still blue after mouse release; App Stop dark; App Run blue again; App Reset dark. Purpose says while held, action table explicitly says toggle. Registered QA-SPEC-002; do not call this a functional input defect before resolving the contract.

## Draft/Discard evidence boundary

The Unsaved scene workspace dialog explicitly includes scene or ladder draft edits and offers Discard changes (lab202-entry-or-draft-prompt.png). Destination Lab 2.2 is visibly loaded afterward (lab202-ready.png). Later breadth Run of Lab 2.1 succeeds without the original no-network gate (breadth-lab-2-01-workstation-call-run.png). This is a useful candidate for controlled reentry testing, but does not establish the content of a restored draft versus cached loaded controller. Registered QA-NAV-CANDIDATE-001 with expected semantics unverified.

No complete exercise PASS is claimed: the intended momentary/toggle behavior, repeated press-off action, input behavior while stopped, visible editor state after reentry, save/load, keyboard interaction, and complete scene lifecycle remain open.
