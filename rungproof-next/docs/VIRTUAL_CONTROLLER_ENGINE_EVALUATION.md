# Virtual-controller engine evaluation

Research date: 2026-09-30. Sources are upstream project repositories,
documentation, and license files. “Confirmed” below means the linked upstream
material states or demonstrates the capability. It does not mean RungProof
performed a compatibility or timing qualification. No candidate was downloaded,
executed, linked, or redistributed in Phase 1.

## Decision summary

Use a hybrid architecture. RungProof owns the program model, validation,
teaching state, deterministic plant coordinator, UI, and symbolic bindings. The
bounded BOOL LD executor is internal because element-level state is required and
the Phase 1 language surface is small. PLCopen XML remains an exchange boundary.
External compilers/runtimes remain potential reference backends behind a local,
typed worker adapter.

## Candidate details

### PLCopen XML / IEC 61131-10

- Upstream: https://www.plcopen.org/standards/xml-echange/
- Confirmed language role: graphical and textual IEC 61131-3 project exchange;
  PLCopen states the XML specification became IEC 61131-10.
- Execution/scan: none. It is a representation, not a runtime.
- Element inspection: stable IDs and graphical structure can support editor and
  diagnostic mapping, but live values must come from a separate engine.
- Windows/package/integration: XML parser and schema validation only; no external
  IDE should own the canonical RungProof project.
- Maintenance/license: PLCopen publishes version 2.01 and the IEC standard;
  schema/specification terms must be reviewed before bundling normative files.
- Offline/tests: local import/export is possible and fixture-testable.
- Risk: dialect/vendor extensions and lossless round trips. Suitable as a
  future import/export format, not the internal IR or backend.

### Beremiz and MatIEC

- Upstream: https://github.com/beremiz/beremiz and
  https://github.com/beremiz/matiec
- Confirmed languages/path: Beremiz is an IEC 61131-3 IDE/runtime environment;
  its PLCopen-oriented editor/build flow uses MatIEC. MatIEC translates IEC
  source representations into C/C++; it is compilation, not direct graphical
  LD interpretation inside RungProof.
- Scan/deterministic time: the generated runtime supplies cyclic execution, but
  a RungProof-controlled simulated clock and exact single-scan API were not
  confirmed from the public integration surface.
- Inspection: Beremiz supports online debugging. A supported API exposing every
  contact, branch, coil, and function-block transition to an embedded teaching
  UI was not confirmed.
- Windows/package/integration: Beremiz publishes Windows installation guidance;
  adopting it means packaging an IDE/build/runtime stack or isolating its CLI
  and generated runtime in a worker process.
- Maintenance: upstream repositories show ongoing project development; pinning
  and qualification would still be required.
- License: Beremiz IDE/CLI is GPL-2.0-or-later, its Python runtime is
  LGPL-2.0-or-later, and the repository describes a separate GPL-3.0 C++
  runtime. MatIEC is GPL-3.0. Linking, modification, distribution, and worker
  boundaries require legal/license review.
- Offline/tests: can operate locally after installation; build and runtime
  fixtures are automatable, but the toolchain is much larger than Phase 1.
- Risk/fit: strongest mature IEC toolchain candidate, but licensing, packaging,
  clock control, and teaching-state mapping make it a later isolated adapter
  spike rather than the Phase 1 dependency.

### OpenPLC Runtime v4

- Upstream: https://github.com/Autonomy-Logic/openplc-runtime
- Confirmed languages/path: the OpenPLC Editor accepts IEC programs, then the
  v4 workflow compiles generated sources (including STruC++ output) into a C++
  runtime package. Graphical LD is therefore compiled rather than directly
  interpreted by the runtime.
- Scan/deterministic time: the C/C++ runtime has an explicit cyclic task and
  Linux real-time scheduling support. A host-supplied virtual-time/single-scan
  contract for Windows was not confirmed.
- Inspection: REST/WebSocket services expose runtime/debug values. A documented
  stable mapping for every LD contact/branch/coil was not confirmed.
- Windows/package/integration: the v4 README is Linux-oriented and includes
  Python/Flask plus native build/runtime components. Best integration would be
  an isolated local process using a narrow typed protocol, not in-process code.
- Maintenance: v4 is the active upstream line observed at research time; pin a
  commit before any qualification.
- License: MIT in the upstream `LICENSE` file. Transitive/generated toolchain
  notices still require an exact-version audit.
- Offline/tests: runtime and REST service can run locally; worker-level contract
  tests are practical once packaged.
- Risk/fit: attractive reference-runtime spike because of permissive licensing,
  but deployment weight, Windows qualification, deterministic virtual time, and
  element-state visibility are unresolved.

### IronPLC

- Upstream: https://github.com/ironplc/ironplc
- Confirmed languages/path: Rust compiler/runtime/VM work centered on Structured
  Text, with PLCopen/TwinCAT input directions described upstream. It is not a
  proven direct graphical LD executor for this slice.
- Scan/inspection: a VM is useful for deterministic tests, but the exact
  fixed-time and per-LD-element debugging contracts required here were not
  confirmed.
- Windows/package/integration: Rust CLI/library packaging is plausible; an
  isolated CLI/worker would preserve the RungProof boundary.
- Maintenance/license: active public repository, MIT license, and the upstream
  README explicitly labels the project a prototype.
- Offline/tests: fully local and amenable to compiler/runtime fixtures.
- Risk/fit: promising future ST parser/reference backend; prototype status and
  LD gap make it unsuitable as the Phase 1 primary engine.

### RuSTy

- Upstream: https://github.com/PLC-lang/rusty
- Confirmed languages/path: Structured Text compiler using LLVM; not a direct LD
  execution backend.
- Scan/inspection: generated/native execution is possible, but RungProof virtual
  clock control and LD element-state inspection are not applicable or confirmed.
- Windows/package/integration: LLVM/native compiler packaging would be required;
  safest boundary is a worker process.
- Maintenance/license: public active project; repository licensing includes
  LGPL/GPL terms that require exact component review.
- Offline/tests: compilation can be local and fixture-tested.
- Risk/fit: useful later for ST experiments, with licensing and LLVM deployment
  costs; does not solve Phase 1 graphical LD diagnostics.

### Eclipse 4diac FORTE

- Upstream: https://eclipse.dev/4diac/4diac_forte/
- Confirmed languages/path: portable C++ runtime for IEC 61499 function-block
  applications.
- Scan/inspection: event-driven IEC 61499 execution differs materially from the
  requested IEC 61131-3 cyclic LD scan.
- Windows/package/integration: supports cross-platform native builds and local
  runtime deployment; test automation is feasible.
- Maintenance/license: maintained Eclipse project; exact EPL/third-party notices
  would require review if incorporated.
- Offline: yes after local installation/build.
- Risk/fit: using it as the primary backend would distort the requested PLC scan
  model. It remains a possible separate IEC 61499 learning mode, not an LD
  compatibility engine.

## Required gate for a future adapter

Before selection, a pinned candidate must pass an offline spike proving: Windows
installation; one-command reproducible build; exact license inventory; explicit
single-scan or host-time control; stable symbolic I/O; contact/branch/coil/timer
state mapping; malformed-program rejection; process crash/timeout containment;
zero physical transport configuration; and deterministic golden-trace tests.
