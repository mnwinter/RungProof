# Connection implementation meeting and bounded acceptance

Departments: runtime and controls (accepted-clock integration and measured PC feedback), authoring/persistence (explicit opt-in, version9 schema, exact current-draft retention), native QA (20/40/100ms lifecycle and history invariants), Dot (identity retirement, rollback, before-tick eligibility), root (integration and actual renderer inspection).

Accepted implementation: one additional existing unloaded belt in the existing Conveyor Stop scene. Its run command is a PLC BOOL output; measured running and speed are PC BOOL/REAL inputs. Normal scheduling and Step share one accepted integration callback. UI inspection does not move the belt. Missing feedback stops and publishes unavailable values; command detach may coast only while the running program still matches the current draft. External mode cannot use this offline registration.

Actual changes now include explicit connection button, current-draft-preserving registration, transactional rollback, valid deletion persistence, stopped Undo/Redo identity rebuild and measured-feedback summary counts. Rotated/scaled/conflicting targets cannot request registration. Native failures were reproduced before the corresponding repairs.

Final same-binary evidence and Dot visual review are pending. No whole-product completion, external PLC qualification or human material approval is claimed. Other75 material propagation remains held. Historical GCHandle issue remains open.
