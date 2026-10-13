# Monitoring allocation optimization checkpoint

Scope: capture implementation only; no instruction is omitted, no capacities or acceptance budgets changed. Root owns all execution and qualification.

## Implemented

- Reuse the registered-ID set and mutable collector index buckets between attempts. Freeze still copies dictionaries and the trace array; Reset releases working references without mutating retained captures.
- Reuse bounded temporary observation builders after their operands have been copied into immutable events. Reset all builder fields before reuse.
- Prepare counts before constructing runtime events, avoiding the redundant count-stamping record clone. The public collector entrypoint still defensively clones caller-owned events.
- Intern an immutable private-array call path once per frame. Publicly supplied lists still undergo defensive copies. Each attempt and Reset clears the runtime path cache.
- Cache root task context keys by complete stable identity, including task, site, target and instance, with a 40,000-key cap. Invocation ordinals remain on observations, not stable context keys.
- Bind publication origin once per shared immutable event across global/context/trace indexes. Keep original captures untouched.
- Preserve the conservative history charge while eliminating temporary estimate arrays and collection iterators.

## Evidence so far

Root-run raw-runtime diagnostic; not whole-session/history/render qualification. W1 has 1,750 observable IDs and continuous plus periodic task executions. The latest 1,000-warmup / 10,000-scan run is `perf-w1-long-03.log` in the implementation state directory.

| Mode | p50 ms | p95 ms | p99 ms | Bytes per scan |
|---|---:|---:|---:|---:|
| Monitoring off | 0.0959 | 0.3511 | 0.5270 | 262,094 |
| Monitoring on | 0.6923 | 4.5002 | 9.7705 | 1,241,435 |

The earlier short preliminary run reported approximately 3.79 MB/scan on. Workload lengths differ, so this is evidence of substantial allocation reduction, not a rigorous comparative latency qualification. The latest added allocation remains about 979 KB/scan and fails the original 256 KiB proposal. Added tail latency also fails the original limits. No budget is marked passed.

Two regressions were added: immutable origin binding preserves shared events without rewriting earlier origins; interned paths and counts survive subsequent scans and Reset. Existing on/off golden, repeated invocation, overflow and prior-capture immutability tests remain relevant. Root runs the tests.

## Remaining decision

The optional profiler `--record-size` diagnostic measures the retained empty event object lower bound, excluding all operands, paths, dictionaries, snapshots and history. If 1,750 empty records alone exceed 256 KiB, meeting that budget requires a compact immutable backing representation with lazily materialized event views, not additional pooling of published mutable objects. That redesign must preserve all API semantics, exact context/count/provenance and history immutability. Alternatively, Dot can review a justified budget change; this agent has not changed the contract.

Independent review, uncontended repeated W1 runs, W2-W4, session/history, retained-memory and native-render qualification remain open.

## Empty-record measurement and next qualification harness

Root confirmed the standalone empty immutable event diagnostic at 200.0128 bytes/event: 350,022 bytes for 1,750 events, already above 262,144 bytes before operands, paths, indexes or snapshots. This is a lower bound for the current object representation, not a justification to silently waive the original budget. Proposed decision for Dot: retain the original gate as failed while choosing between a compact immutable columnar representation (substantial architecture work) or explicitly approved offline-simulator budgets supported by repeated whole-session measurements. No replacement thresholds are set in this checkpoint.

The profiler now accepts `samples warmup workload scope`, where workload is W1, W2, W3, W3-watchdog or W4 and scope is runtime (default) or session. Example: `-- 100 20 W2 session`. Start with a short smoke to validate workload assertions before 1,000/10,000 qualification runs.

- W2: exactly 2,000 network definitions, 10,000 contact/comparison leaves, 10,000 tags, 100 CALL sites, 500 main-task call evaluations per scan; four periodic tasks at 10/20/100/500 ms. Shared worker also executes for the slower tasks.
- W3: repeated CALL/jump/shared-counter execution completes exactly 9,999 networks. W3-watchdog restarts each stopped trial and asserts 10,000 executed networks followed by abort on the rejected 10,001st visit.
- W4: 100 runnable text operations at the existing 255-character language limit. Separate robustness fixtures cover 0/64/4,096/65,536-unit observation text, oversized language rejection, 40,001 contexts, trace eviction and preserved global count.
- Session mode retains real history envelopes and reports charged bytes, retained/dropped entries, post-GC managed heap, GC collection counts and total pause duration. These are measurements, not a GC pause histogram or a native-render qualification.
- Every mode compares the final full scalar/output/timer/counter/task/diagnostic state between instrumentation off and on. Scope output identifies raw runtime versus session history.

The new harness has not been run by this agent; root owns compilation and execution. W4 oversized single-envelope history omission and native rendering remain covered by separate functional acceptance work and still require qualification evidence.

## Compact capture checkpoint (supersedes the representation discussion above)

The approved compact immutable implementation is now in place. Frozen captures own private compact row, operand, rich payload and index arrays. Public records remain constructible and are materialized lazily; immutable origin views share backing without rewriting prior captures. Exact global/context/trace counts, independent frame ordinals, insertion order, nullable state and rich instruction payloads are preserved. The collector reuses only unpublished working slots, with reference counts protecting retained indexes.

Program symbol registries are shared immutable identity. Each scan owns dense global/count arrays and first-observed order, replacing duplicate string dictionaries. Boolean operand records are canonical immutable values. UI consumers use metadata summaries and per-instruction contexts to avoid materializing an entire scan for one visible block. History estimates do not materialize records and now explicitly charge owned backing capacity even for registered IDs that never execute.

Root reported integration42 at 314 passing tests before the newest ordering and sparse-accounting regressions. The latest short W2 diagnostic, perf-w2-phases-04.log, reported 6,644,219 bytes/scan on and 2,530,158 off: 4,114,061 added bytes (about 3.923 MiB). This is narrowly below the 4 MiB proposal in a smoke run, not qualification. Earlier representation measurements above are historical and do not describe the compact implementation's final performance.

Temporary allocation phase hooks have been removed from production code. The profiler's --layout prints backing layout; the older --allocation-phases spelling remains a layout-only compatibility alias. Root owns fresh builds, repeated uncontended runtime/session qualification, retained and materialized-history measurements and native rendering checks. No budget or release gate is declared passed by this checkpoint.
