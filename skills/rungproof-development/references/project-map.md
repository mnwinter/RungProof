# RungProof project map

Use this map after reading the root project memory files. Confirm paths in the
current checkout because names and responsibilities can evolve.

## Governing files

- `AGENTS.md`: repository rules and baseline verification.
- `PROJECT_INFORMATION.md`: canonical repository and current-source boundary.
- `CONTEXT.md`: Plant Runtime, PLC Session, and Renderer Adapter language.
- `README.md`: entry point, retained dependencies, and external-PLC warning.
- `rungproof-next/README.md`: current application features and focused verifier
  switches.
- `rungproof-next/docs/DEVELOPMENT_SETUP.md`: pinned local toolchain.
- `rungproof-next/docs/TIA_INSTRUCTION_EVIDENCE_MATRIX.md`: current TIA/LAD
  acceptance ledger.
- `rungproof-next/docs/TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md`: governing
  scope, status, and work order for TIA V21 main-screen/property parity.
- `rungproof-next/docs/TIA_PORTAL_PARITY_AUDIT.md`: detailed sources, findings,
  evidence records, and remaining gates.
- `rungproof-next/docs/VIRTUAL_CONTROLLER_PROGRAM_SCHEMA.md`: saved ladder
  program format.
- `rungproof-next/docs/VIRTUAL_CONTROLLER_CONFORMANCE.md`: supported execution
  semantics and limits.
- `rungproof-next/docs/DEMO_FUNCTIONAL_ACCEPTANCE.md`: authored-demo functional
  requirements.
- `rungproof-next/docs/ASSET_QUALITY_STANDARD.md`: production asset gates.
- `rungproof-next/docs/OFFLINE_DEMO_CANDIDATE.md`: packaging lane and release
  limitations.

`AGENTS.md` mentions `docs/AI_HANDOFF.md`, but that file is not present in the
current repository. Do not assume it exists; use the governing files above.

## Active application

`rungproof-next/` is a Godot 4.7.2 .NET project targeting .NET 10.

- `src/VirtualController/`: ladder document/tree, compiler, runtime, calls,
  observations, persistence, tag/address handling, and authored programs.
- `src/App/`: application shell, ladder canvas, editing interactions,
  monitoring presentation, project tree, scene controls, and headless review
  routes.
- `src/Scenes/`, `src/Lessons/`, `src/Workspace/`: plant models, scene I/O,
  lesson lifecycle, and user-authored workspace behavior.
- `src/Connections/`: presentation and policy around the guarded external PLC
  path; it does not replace the Python S7 transport.
- `src/Catalog/` and `assets/catalog/`: asset contracts and accepted/candidate
  catalogs.
- `tests/`: deterministic virtual-controller/editor and plant-domain tests.
- `tools/`: project generators, asset checks, MCP helper, and audit utilities.

For Siemens-facing ladder work, begin with:

- catalog/semantics: `src/VirtualController/LadderInstructionCatalog.cs`;
- compile/execute: `LadderCompiler.cs`, `VirtualControllerRuntime*.cs`;
- persistence/editing: `LadderProgramJson*.cs`, `LadderEditor*.cs`;
- presentation: `src/App/LadderInstructionPresentation.cs`;
- rendering: `src/App/LadderEditorCanvas.cs`, `.TiaControls.cs`, and
  `.Monitoring.cs`;
- UI routing: `src/App/SimulatorShell*.cs` and `LadderProgramTree.cs`;
- main regressions: `tests/InstructionPresentationTests.cs`,
  `EditorCommandTests.cs`, `EditorTransactionTests.cs`,
  `RecursiveEditorTests.cs`, and `EditorMigrationTests.cs`.

## Retained bridge and source material

- `prototype/scenes/`: original scene contracts retained for parity/migration.
- `prototype/plc-profiles/`: explicit guarded PLC mappings.
- root `tools/plc_live.py`: guarded live session.
- root `tools/plc_bridge.py`: line-oriented Godot-to-Python bridge.
- root `tools/plc_diagnostics.py`: read-only diagnostic path.
- root `tools/test_plc_*.py`: in-memory/offline boundary tests.
- `vendor/siemens-plc-pc-interface/`: pinned transport source and notices.

Generated `.tools/`, `.godot/`, build outputs, logs, captures, and release
archives are evidence or local tooling, not source to commit unless a governing
document explicitly requires a tracked artifact.
