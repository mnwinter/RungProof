# Finite-belt observed trial proposal

Status: bounded implementation in progress under Matt's existing three-mode integration authorization. Dot reviewed the design. The earlier per-feature approval hold is superseded: the asynchronous objective question is optional design feedback, not a prerequisite. Completed finite adapter/indexed diagnostic evidence and the pending wider material rollout gate remain separate.

## Learner experience

Add one finite-preset-only card to the existing operator/scenario flow: Objective, optional Show hint, and Observed result. Objective: one momentary start should move the carton, continue after release, and stop at the physical beam. Hint explains momentary input and holding logic conceptually; no automatic code or authored solution. Report a bounded observed trial, never a canonical challenge pass, score, saved progress or physical safety claim.

## Observation contract

Use existing VirtualControllerSession.ScanCompleted, which occurs after the accepted scan has projected commands and advanced the plant. Copy actual declared binding-keyed sampled/effective inputs and applied outputs plus measured finite model/geometry after that tick. Observer has no plant, controller or output mutation API and owns no clock.

Require a new trial from measured Reset home, a real rising start request, positive actual displacement, released start with continued commanded travel, the first physical beam transition, sampled beam true with run false on exactly the following accepted scan, and stable supported carton/beam/runfalse for at least0.5s of accepted simulated time. At20ms this is25 accepted scans; do not use wall time or assume all programs have20ms periods. Record exact program identity, placement/relation identity, reset epoch and accepted scan/time. No pre-existing beam stop or old completed state may satisfy a fresh trial.

Wrong code reaching the finite limit with run still true: Needs attention, with the observed command/feedback/position shown. Missing progression or timeout: Incomplete/no complete observed trial, not a general claim that valid code is wrong. Manual Stop before hold criterion: Incomplete. Invalid/changed-unloaded draft, force, inconsistent aliases, invalid/missing/duplicate bindings, reset, layout, project replacement, cancellation or identity mismatch invalidates current trial. Every new epoch clears prior completion; historical data must never be shown as current success.

Canonical ChallengeEvaluator is unsuitable here: hardcoded operator.start/operator.stop/simulated_photoeye/conveyor_running,0.01m/scan, seven protocol cases and a different plant. Do not rename points to trick it or reuse its progress store/worker.

## Implementation and acceptance

A small pure finite observer, a Main partial consuming immutable accepted observations/lifecycle invalidation, and a finite-only Shell card with optional hint. Existing source scene description, scene operator panel and accepted ScanCompleted event are the integration points. Default preset remains tag-ready and stopped with no learner solution injected. All other scene modes unchanged.

Verify intended, valid wrong, invalid-current, changed-valid mismatch, no-start/never-beam timeout, manual Stop, force/alias inconsistency and reset/layout/project epochs. Compare complete plant/command/accepted-scan traces with observer enabled and disabled: they must be identical. Test accepted-time hold at multiple supported periods and large elapsed subdivision; no observations on rejected/stopped ticks. Inspect actual rendered full/split views for readable Objective, hidden/revealed hint, honest results and unobstructed scene controls. Dot independently reviews exact binary/images; overall studio completion and wider material rollout authorization remain separate.

## Approval provenance

Latest direct material feedback remains: "spend more time on color and texture its still very flate and bland", following "Revise before rollout". Revised two-room V3 option question remains unanswered. No wider material approval is inferred. Matt's existing three-mode authorization covers this bounded objective implementation. The earlier statement requiring a separate feature decision was an unnecessary hold and is superseded. The asynchronous question remains optional design feedback. Wider two-room color/material rollout still requires Matt's explicit approval; no implementation, pure test or studio review substitutes for it.


Current implementation checkpoint: immutable pure observer source exists, and root ran its focused suite successfully (37 checks at 20/50/100 ms). Main/Shell objective integration and native observer-enabled/disabled trace equivalence are in progress, not yet verified. No objective render acceptance is claimed.
