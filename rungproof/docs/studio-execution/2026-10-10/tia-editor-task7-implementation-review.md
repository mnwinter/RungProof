# Task 7 implementation handoff for independent review

2026-10-11. Implemented source is frozen for review, not accepted as complete. Root reported `integration21` 266 passed and `appbuild17` succeeded. Native UI and exported qualification remain with root. This task ran no builds, app exports or physical PLC operations.

## Implemented

- Optional explicit Legacy/Parameterized block and CALL modes; stable variable/formal IDs; typed actual records and declared FB instance identities. Legacy mode remains shared-global; old interface metadata stays inert.
- Compiler checks modes, scalar types/enum domains, sections, complete actuals, writable actual roles, static alias overlap and FB instance type. Explicit `global:<id>` and `local:<id>` operands lower to ordinary instruction validation. Stable aggregate root ID plus leaf suffix is supported.
- Typed frames sample Inputs, initialize FC Output/Temp per invocation, resolve InOut ultimate aliases before effects, and copy Output only on normal fallthrough or RETURN. FB Output/Static survive invocations and Stop; Reset initializes them. Runtime rejects active-instance nesting and forwarded writable overlap. Aborted frames suppress copy-out but retain executed global/InOut and FB storage effects.
- Static FB timer quantum is consumed at most once per scan; reset does not replenish it. Legacy global timer policy is unchanged. Instance counter/edge storage is separate; FC edge path excludes task roots and, inside FBs, is relative to the innermost FB plus instance ancestry. Qualified storage segments are escaped to avoid ID collisions.
- Actual CALL copy-in/alias/copy-out/suppressed transfers attach to the original observation without recording another evaluation. `CalledFrameInvocationId` and `CalledInstanceId` distinguish invoked frame from parent context. Legacy CALL still reports Entered; typed CALL reports Completed/Aborted.
- Runtime schema2 and editor schema2 preserve modes, interfaces, stable IDs, bindings and instances. Runtime schema1 writer retains the frozen old shape. Document import/build/network clone/history save/reopen retain fields. Tag rename preserves existing stable ID.

## Evidence and compatibility rulings

Root retained red logs for silent mode loss, absent typed operand scopes, frame execution, depth boundary, shared instance edges/counters, double timer advance, project persistence, aggregate identity/member reads, missing CALL transfer observations, storage-key segment collision, nested FC edge sharing and unqualified ghost counter state. Root owns exact log names/counts and full build evidence.

Legacy-only deep call chains retain the preexisting compiler behavior and runtime watchdog. The 32-depth limit applies to paths containing parameterized blocks; this was clarified by root to preserve the existing schema1 20,000-block stack-safety regression. Mixed paths retain the limit. FC edge memory inside a shared FB uses the inner call path relative to that FB, even when different outer call sites invoke the same instance.

## Review targets / known issues to resolve before acceptance

1. `LadderEditorCommands.CopyOutput` now copies mode/bindings/instance after a reproduced output-paste failure (`integration22-red`). Root granted this helper-only edit. Retest pending at this note revision.
2. Bare aggregate leaf binding was reproduced and fixed using expanded declarations. **Remaining review target:** unqualified global TIMER/COUNTER member references may require the same rejection by checking their root declaration.
3. Runtime binding null Symbol and editor instance null Id were reproduced. Both readers now reject null required new-record fields; runtime records accept camel/Pascal case and JSON literal null is not converted to text. Retest pending. Writer record properties remain PascalCase while surrounding schema uses camelCase; review versioned consistency.
4. **Known likely entry gap:** empty EntryBlock with first block parameterized may reach a typed body without a frame; explicit typed entry and typed task entries are rejected but fallback needs a regression. Also check static alias rejection including aggregate leaves, malformed interface initial values, unknown enum values, duplicate symbol/instance identities, and typed calls mixing legacy callees.
5. Confirm call transfer retention/history payload accounting. CALL transfers are attached into the bounded instruction observation capture, while `Snapshot.CallInvocations` currently keeps all actual invocation transfer records for that attempted scan. History/performance budget must include them. Large-program performance is unqualified.
6. Add remaining acceptance coverage: true/false/true timer sequence within one scan, all timer reset combinations, typed literals/ENUM/STRING/INT domain cases, instance declaration mismatch and nested normal distinct-instance calls, parameterized monitoring on/off equality, and malformed runtime/editor JSON.
7. No authoring UI was added. Task8 must provide staged typed-call/instance/interface editing and explicit undoable conversion. New models default Legacy for compatibility. Explicit external cross-instance DB-member operands were not added; scoped Static/Output references resolve within an invoked FB instance.

## Files

New: `LadderCallBindings.cs`, `LadderCallFrames.cs`, `LadderInstanceStorage.cs`, `LadderProgramJson.Calls.cs`, `tests/ParameterizedCallTests.cs`.

Modified: `LadderProgram.cs`, `LadderCompiler.cs`, `LadderProgramJson.cs`, `VirtualControllerRuntime.cs`, `VirtualControllerRuntime.Observations.cs`, `InstructionObservation.cs`, `LadderEditorDocument.cs`, `LadderEditorHistory.cs`, `LadderEditorProjectJson.cs`. All under `src/VirtualController` except tests. Preserve other agents' prior dirty changes in those files; compare against the root baseline and task checkpoints, not HEAD.
