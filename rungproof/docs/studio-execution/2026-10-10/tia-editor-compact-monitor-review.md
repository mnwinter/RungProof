# Compact observation storage independent review

Reviewed 2026-10-11. Scope: FrozenObservationTable.cs, InstructionObservationCollector.cs, InstructionObservation.cs, VirtualControllerRuntime.Observations.cs, observation tests and session history charging. Static review only; no builds or tests run by this reviewer. Root reported integration-37 passing observation tests, with an unrelated intentional zoom failure.

## Decision

No concrete correctness defect found in this checkpoint. This is scoped source acceptance, not performance qualification or whole-editor acceptance. Keep the runtime/session performance and rendered monitoring gates open.

## Findings and remaining evidence

1. The new reuse and allocation tests at tests/InstructionObservationTests.cs:30-64 use public Record(), which stores Full records. They prove lazy origin binding and frozen index ownership, but do not directly exercise compact operand-buffer reuse or all packed nullable fields. Existing runtime tests exercise compact contacts/coils and preserve previously materialized values; they do not provide a complete packed-field round trip. Before final acceptance, add a compact-path regression retaining an unmaterialized capture across subsequent scans/reset, then read operands and all nullable edge/coil fields. Include mixed rich and compact slot reuse with trace capacity 0/1/2 and context overflow. This is a validation gap, not an observed semantic failure.
2. Lazy memoization is protected by a lock. No mutable published table/index/operand array escaped in the inspected path. A concurrent multi-reader test would strengthen this claim; the reviewer did not run one.
3. EstimatedBytes intentionally charges a conservative materialized record for each global/context/trace index, rather than measuring compact backing allocation. That preserves the previous history policy but cannot establish actual retained-heap or allocation budgets. Profile after materializing detail/history views as well as the runtime-only path; UI access can allocate cached rich records that a runtime-only profile never touches.

## Checks performed

- Packed fields do not overlap: result bit 0; enable bits 1-2; destination-before 3-4; stored-after 5-6; previous-known 7; previous-input 8-9; current-input 10-11; path-truncated 12.
- Compact eligibility excludes calculated/preset/control/CALL-frame/diagnostic state and non-Boolean values. Other instructions retain the rich record path.
- Count semantics preserve final per-ID counts in global/context views and event-time counts in trace; context counts remain attached to the last retained event for that context.
- Each mutable row slot has one reference per authoritative global entry, retained context entry and trace entry. Release clears data only when all references disappear. A new slot is acquired before replacing old indexes, so no currently retained row is overwritten. The bounded high-water limit is the sum of retained indexes plus one transient incoming row, rather than total loop iterations.
- Freeze copies rows, operand values, index dictionaries, trace indexes and counts; immutable descriptor/path objects may safely be shared. Reset clears working references without clearing published arrays.
- WithOrigin shares frozen backing and changes the origin of lazily materialized views without restamping the original capture. Its independent cache is bounded by retained rows and the two possible count presentations.
- Descriptor and metadata caches have explicit entry caps; collector root-path cache is capped by context capacity. Primitive operand values and state records used by runtime are immutable.

## Source hashes

SHA-256, relative to rungproof-next:

- src/VirtualController/FrozenObservationTable.cs: 27799EEE691AC7BAF0737B7A719BC4FE11392DEBEAB2241A77CBD36B4E497136
- src/VirtualController/InstructionObservationCollector.cs: 03C1FD471192953A2F6EA6C687D7C23CB2B3688AF33416584E93F03590C7245C
- src/VirtualController/InstructionObservation.cs: B31E6DD1887B1B0C9326BB6A7A7490C5BC880E98B9EF153467C173F8F2C7C0AE
- src/VirtualController/VirtualControllerRuntime.Observations.cs: 0998393D0AB229ADD400D240A850566FCB5C02DFF18498C149C91FB2CD92072A

Later optimization changes require review against these hashes.

## Rich-payload follow-up checkpoint

Reviewed later same day after runtime rich observations moved to RichObservationPayload backing rows. No new concrete correctness defect found in the inspected runtime path. Rich payload arrays are Freeze-owned copies; payload diagnostics reference fresh runtime-owned arrays that are never subsequently mutated, and public materialization defensively copies them. Rich slots clear on release/reset, independently of frozen payloads. Numeric before/calculated/after values, configured preset, control outcome, rounding/clamping bits, called frame/instance and diagnostics have explicit materialization fields. Oversized consumed text remains a bounded InstructionOperand before retention.

Added independent regressions (not run by this reviewer): LazyRichRuntimePayloadSurvivesScanAndReset and LazyRichSlotsRetainPayloadAcrossRecycling in CompactObservationRegressionTests.cs. The first reads actual runtime conversion/fault/timer/RETURN payload only after another scan/reset. The second checks rich slot recycling followed by compact overwrite, including CALL identity fields. Root owns execution and acceptance. Earlier source hashes describe the earlier checkpoint only; this addendum does not claim performance or final-candidate approval.

## Dense registered-map review

Ordering/source ownership: no concrete defect found. Freeze builds its order array from mutable global dictionary insertion order, not registration order; updates retain first-observed position. Counts and GlobalLast share sparse presence/order, and the registry clones caller IDs. Added DenseObservationMapsPreserveSparseFirstObservationOrder and ObservationRegistryOwnsItsInputArray; root owns execution.

New accounting finding: dense globalRows (int[registered count]) and countValues (long[registered count]) exist even for no observed IDs. EstimatedBytes currently iterates only present records/keys, so a zero-observation capture charges zero while retaining approximately 12 bytes per registered ID, excluding array/object overhead. With 20,000 registered IDs this is approximately 240 KB per sparse scan omitted from the history budget. Add a fixed backing-storage charge (including order as appropriate) or choose sparse storage for sparse captures. WithOrigin should preserve that same charge. This is a concrete history-accounting gap, separate from ordering correctness; root was notified before this checkpoint.
