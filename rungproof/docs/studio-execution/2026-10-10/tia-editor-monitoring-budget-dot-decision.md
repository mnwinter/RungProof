# Instruction monitoring: architecture and budget review

Disposition: original performance proposal FAILS current W1 evidence. Proposed reviewed qualification amendment below is not a pass, not a unilateral contract change and not whole-task approval. Root retains the original failed gate and decides whether to adopt this bounded offline viewer target before qualification.

## Evidence and decision

The checkpoint reports 1,000 warm-up and 10,000 raw runtime scans: off p50/p95/p99 0.0959/0.3511/0.5270 ms, on 0.6923/4.5002/9.7705 ms. Allocations are 262,094 off versus 1,241,435 on bytes/scan: added 979,341 bytes, about 0.934 MiB. Empty immutable events alone cost about 200 bytes each; 1,750 distinct W1 events therefore require about 350,022 bytes before operands, paths, indexes and publication snapshots. The original 256 KiB added-allocation proposal is structurally impossible for this current record-per-event representation. The original 15%/1 ms p95 and 2 ms p99 added-latency limits are not passed.

Recommendation: explicitly review a realistic offline educational-viewer budget first, instead of mandating a high-risk compact-storage rewrite solely to meet an unmeasured initial proposal. This preserves full observations and semantics. It does not establish real-time PLC execution or guarantee a 10 ms wall-clock scan. The current measured tail is already near the proposed ceiling, so repeated whole-session/native results can still fail and require architecture work.

Proposed W1 criteria: total monitoring-on runtime p95 <=5 ms and p99 <=10 ms; added allocation <=1.5 MiB/scan. Keep and report off/on distributions and matched paired/blocked differential measurements, but remove the original 15% requirement explicitly: a very fast submillisecond baseline makes it unsuitable as the controlling educational-viewer target. Do not subtract two independent percentile quantiles and call the result the percentile of added latency. No proposed criterion is declared achieved by the current single raw run.

The numerical amendment applies only to W1. Keep the original W2-W4 added-allocation <=4 MiB gate pending actual measurements; do not extrapolate W1 headroom or manufacture large-workload thresholds. Keep 64 MiB charged history, 96 MiB measured retained observation/history after GC, all count/context/trace/text/path caps and explicit omissions. Keep the original 60 Hz viewport, total frame p95 <=16.7 ms/p99 <=33.3 ms, refresh CPU p95 <=4 ms and visible subset <=200 boxes. If native render or accepted scheduling/backlog stability fails, W1 runtime limits alone cannot qualify the feature.

## Independent source review

Reusable mutable collector dictionaries are cleared only after Freeze copies the dictionaries and trace array. Observation builders return to their bounded pool only after constructors copy operands; every builder field is reset. Private ObservationCallPath uses a private copied array, while public supplied paths are copied. Origin binding uses reference identity to share one immutable bound event across indexes without modifying older captures. Count preparation is synchronous with recording, and final global counts are stamped at Freeze; trace eviction does not remove authoritative global final records/counts. Root-path key caching includes instruction/task/site/target/instance, with bounded keys and invocation ordinal intentionally separate from stable context identity. These are sound local allocation optimizations; no immediate new observer-state mutation blocker was found.

This review does not independently execute the 298 tests or the latest CALL red regression. Those must pass after final changes. Public object-valued fields are safe only within the supported scalar/immutable timer/counter payload contract; no claim of arbitrary object deep immutability follows. Conservative EstimatedBytes charges shared references repeatedly, preserving the history cap but not measuring exact retained heap. Report both charge and actual heap rather than equating them.

## Required qualification before approval

- Three independent uncontended Release-build runs per workload: 1,000 warm-up/10,000 samples, recorded machine/engine/.NET/power/viewport/background load. Alternate off/on order between runs to reduce order bias.
- W1-W4 runtime and real session/history, including repeated calls/watchdog/text robustness; retain every failed attempt and exact source/binary hash. W4 oversized text remains non-executable serializer robustness, not a larger language limit.
- Measure GC counts and pause duration; total pause time is not a pause distribution. Obtain actual pause/event durations before claiming the original GC-distribution requirement satisfied.
- Validate original session scheduling with full accepted clock/plant/history invariance and sustained backlog behavior at the 10 ms W1 rate. Offline slow-down must be explicitly measured/reported, never hidden by skipped captures/scans or reconstructed execution.
- Native renderer frame/refresh budgets and actual minimum-size readability. Source tests and raw profiling do not qualify UI rendering or physical DPI/input.
- Preserve strict every-scan golden comparisons for outputs, memory, timers/counters, task order, errors and abort phases. The profiler's final snapshot equality is a useful check, not a replacement for that golden suite.

Profiler risk: W2 checks the sum of global CALL counts equals500, while the intended criterion is500 main-task evaluations. Confirm periodic tasks target only the worker and do not contribute those same CALL IDs; otherwise filter exact task contexts or derive the due-task expected total. Do not suppress periodic execution to make the diagnostic pass.

## Compact architecture fallback

If reviewed budgets fail repeated whole-session/render runs, prefer a dedicated compact immutable capture: stable program metadata tables, typed value storage, contiguous immutable event structs/columns, integer path IDs, global/context/trace indexes referring to the same event storage, and lazily materialized read-only event views. It must retain all instruction-time operands/effects, exact counts/context/origin and historical copies; no last-record-only shortcut, reevaluation, mutable pooled published storage or silent capture omission. Freeze exact compatibility tests before migration. A bounded prototype comparing full serialized observations and golden controller traces should precede replacement. The 256 KiB target still needs feasibility measurement even with compact storage; it is not guaranteed by this proposal.

No source edits, build, engine or budget contract mutation were performed by Dot. This document is a reviewed next-step recommendation; current qualification remains open.
