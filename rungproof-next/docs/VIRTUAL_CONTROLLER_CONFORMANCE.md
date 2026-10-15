# Virtual controller Phase 1 conformance

This matrix describes the implemented RungProof subset. “Aligned” means the
concept follows IEC 61131-3 terminology and expected Boolean LD behavior; it
does not mean the runtime is certified or vendor-equivalent.

| Capability | Phase 1 | Evidence / rule |
|---|---:|---|
| BOOL variables | Supported | Input, memory, and output roles; Boolean initial value required |
| Normally-open contact | Supported | Reads one symbolic BOOL |
| Normally-closed contact | Supported | Logical inverse of one symbolic BOOL |
| Rising edge contact | Supported | TIA P uses a unique declared BOOL edge-memory operand: compare stored FALSE to current TRUE, update storage, and pass power for one scan. Authored initial/reset memory controls the first evaluation; Stop preserves it. Legacy imports without explicit memory retain baseline-suppressed internal compatibility state. Portable Logix XIC+ONS presentation is not vendor data-layout equivalence. |
| Falling edge contact | Supported | TIA N uses a unique declared BOOL edge-memory operand: compare stored TRUE to current FALSE, update storage, and pass power for one scan. Authored initial/reset memory controls the first evaluation; Stop preserves it. Legacy imports without explicit memory retain baseline-suppressed internal compatibility state. Portable Logix XIO+ONS/OSF presentation is not vendor data-layout equivalence. |
| Output/internal coil | Supported | Target must be BOOL memory or output; inputs are rejected |
| Set / latch coil | Supported | TIA S / Logix OTL presentation; writes TRUE only when rung is true and otherwise preserves state |
| Reset / unlatch coil | Supported | TIA R / Logix OTU presentation; writes FALSE only when rung is true and otherwise preserves state |
| Series logic | Supported | Evaluated left-to-right as AND |
| Parallel branches | Supported | Evaluated top-to-bottom as OR; at least two branches |
| Multiple networks | Supported | Executed in JSON document order; later instructions observe earlier writes, including set/reset priority |
| Multiple blocks / routines | Supported | Declared entry block plus reusable callable blocks with separate ordered network lists |
| Block / routine lifecycle | Supported | Create, stable-ID rename, and reference-safe delete from the vendor project-object tool page; controller entry, task-owned, CALL/JSR-targeted, and final blocks are protected |
| Contextual instruction editing | Supported | Double-click or right-click Properties opens only the selected contact, compare, coil, timer, counter, numeric, CALL/JSR, RETURN/RET, JMP/LABEL/LBL, or network/rung fields; the bottom dock is reserved for errors, output, and watch data |
| Resizable engineering docks | Supported | Project/organizer, central editor, instruction/tag tools, and diagnostics/watch regions use visible 12-pixel draggable split bars with independent collapse/reopen controls; dock contents fill their assigned region without stealing editor stretch space or overflowing into adjacent panels |
| CALL / JSR | Supported | True rung executes target block inline before the caller's next network |
| RETURN / RET | Supported | True rung exits the current call frame and resumes its caller; false rung falls through; return from a task entry ends only that due task invocation |
| JMP / LABEL / LBL | Supported | True JMP resumes at a unique label in the same block/routine; false JMP falls through; missing and duplicate labels fail validation; a deterministic scan watchdog stops the offline controller and drives outputs safe on an infinite backward-jump loop |
| Call validation | Supported | Missing targets and direct/indirect recursive cycles are rejected before load |
| Fixed scan period | Supported | Positive `scanPeriodMs`; demo uses 20 ms |
| Immutable UI snapshot | Supported | Variables, outputs, element states, scan count, simulated time, lifecycle state |
| Run / Stop / Reset | Supported | Stop forces BOOL outputs false and numeric outputs zero; Reset clears controller and plant state |
| Plant input/output binding | Supported | Active-scene symbolic BOOL, INT, DINT, and REAL points; owner/type checked and fail closed |
| Active-scene binding browser | Supported | Tag editor lists only compatible points from the attached scene contract, displays type/owner/purpose, and provides explicit unbound state |
| Scene binding validation | Supported | `IO001`-`IO004` reject missing points, unsupported types, invalid direction/ownership, and duplicate output drivers before load |
| Unbound tag semantics | Supported | Empty binding samples no scene point and commits no scene output; no implicit tag-name fallback |
| TIMER instances | Supported | Memory-role instances expose elapsed time, timing state, and done state |
| TON | Supported | Non-retentive on-delay; accumulates one fixed scan period while true, clamps at preset, resets while false |
| TOF | Supported | Done sets immediately while true; falling edge holds done during the scan-quantized preset and then clears it |
| TP | Supported | Rising edge starts one fixed pulse; input changes do not extend it and another pulse requires input to return false |
| TONR / RTO | Supported | Retentive on-delay accumulates while true, pauses while false, and preserves accumulated/done state through simulator Stop |
| Timer reset / RT / RES | Supported | A true reset rung clears the selected timer's accumulated, timing, input, and done state before later networks execute |
| Timer contacts | Supported | TIA-style `.Q` and Logix-style `.DN` read done; `.TT` reads timing |
| COUNTER instances | Supported | Memory-role instances publish preset, accumulated count, count-input state, and done state |
| CTU | Supported | Counts only false-to-true rung transitions; count is retentive across false scans and Stop |
| CTD | Supported | Decrements on false-to-true rung transitions; done is true at accumulated values less than or equal to zero |
| Counter load | Supported | A true LOAD/LD rung initializes accumulated count from the configured preset and resets that instruction's edge memory |
| Counter reset / RES | Supported | A true reset rung clears accumulated, edge memory, and done state |
| Counter contacts | Supported | TIA-style `.Q` and Logix-style `.DN` read the shared done state |
| Numeric comparisons | Supported | EQ, NE, GT, GE, LT, and LE produce Boolean rung continuity |
| Numeric operands | Supported | INT/DINT/REAL tags, literals, counter ACC/CV/PRE/PV, and timer ET/PT |
| INT / DINT / REAL execution | Supported | Typed initialization, numeric input sampling, monitoring, comparisons, and JSON persistence |
| DINT execution | Supported | Distinct signed 32-bit type with nearest-even REAL conversion, range clamping, monitoring, JSON persistence, and scene I/O binding |
| Numeric scene I/O | Supported | PC-owned INT/DINT/REAL points sample before logic; PLC-owned numeric outputs commit after logic; Stop drives numeric outputs to zero |
| Symbolic scene-I/O image mapping | Supported | One pure mapper translates scene point bindings to controller tag inputs and controller outputs back to scene points for BOOL/INT/DINT/REAL without resolving physical addresses or constructing a PLC transport |
| MOVE / MOV | Supported subset | TIA MOVE copies a matching modeled INT, DINT, or REAL source to one writable destination only when EN is true; mismatched numeric types are rejected under the supported IEC-checked contract. The TIA renderer uses MOVE with lower IN/OUT1 pins and no type selector; Logix uses MOV. Siemens extended OUT2... outputs, disabled IEC checking, broader types, and a separately authored downstream BOOL action after the same box are unsupported. |
| ADD / SUB / MUL / DIV / MOD | Supported | Two numeric operands; INT/DINT destinations round nearest-even and clamp, REAL destinations retain fractions |
| ABS / NEG / SQRT | Supported | One numeric operand; current Logix presentation uses SQRT and help records the pre-v36 SQR alias |
| EXPT / LN / trigonometry | Supported subset | EXPT requires a REAL base/result and supports INT, DINT, or REAL exponent selection; LREAL and other Siemens integer widths are unsupported. LN, SIN/COS/TAN, and ASIN/ACOS/ATAN accept the modeled REAL width only; trigonometric inputs and inverse outputs are radians. Negative LN and out-of-range ASIN/ACOS write an invalid REAL and drop ENO. LN(0) follows the Siemens-documented nonnegative boundary and stores negative infinity. |
| TRUNC | Supported subset | Accepts a REAL input, truncates toward zero, and selects INT/DINT/REAL output independently. Wider Siemens input/output types are unsupported. Integer overflow drops ENO and preserves the simulator destination; invalid REAL input to REAL output writes NaN. |
| NORM_X / SCALE_X | Supported subset | Three numeric operands use MIN, VALUE, MAX. NORM_X independently selects an INT/DINT/REAL input-range type and REAL output; SCALE_X selects REAL VALUE and an INT/DINT/REAL MIN/MAX/OUT type. Wider Siemens widths are unsupported. VALUE may extend beyond the range when the result remains representable. The simulator preserves the destination on range/invalid/overflow faults; exact Siemens fault-result values remain unverified. Logix identifies these as CPT-equivalent simulator macros rather than native instructions. |
| CONVERT | Supported subset | TIA renders the catalog operation with the LAD mnemonic `CONV`. Source and destination independently select INT, DINT, or REAL; REAL-to-integer conversion rounds nearest-even. Wider Siemens conversion types are unsupported. Overflow drops ENO and preserves the simulator destination; exact Siemens fault-result values remain unverified. Logix ordinary destination conversion is shown through MOV. |
| ROUND / CEIL / FLOOR | Supported subset | Each accepts modeled REAL input and independently selects INT, DINT, or REAL output. ROUND uses nearest-even ties; CEIL selects the next higher integer and FLOOR the next lower. Integer overflow drops ENO and preserves the simulator destination; invalid REAL output writes NaN. Wider Siemens input/output types and authoritative integer-fault OUT values remain unsupported/unverified. TIA presents native operations; Logix identifies these as simulator CPT macros, not native Logix instructions. |
| Numeric runtime diagnostics | Supported | Dynamic divide/modulo-by-zero and typed arithmetic/conversion overflow preserve destination; Siemens floating unary domain faults write NaN where documented. Each publishes a distinct diagnostic used to render ENO false. |
| Extended math and conversion | Partial | Wider bit-string/time/string conversion, arrays, and vendor-specific math status-bit semantics are deliberately not implied |
| FBD / ST | Not supported | Future language front ends; no parser or runtime claim |
| Retentive memory | Not supported | Reset clears all memory |
| Continuous tasks | Supported | Execute once on every deterministic base scan |
| Periodic tasks | Supported | Period must be an integer multiple of `scanPeriodMs`; execution occurs on matching scan boundaries |
| Task priorities | Supported | Lower numeric priority executes first when multiple tasks are due on the same scan; document order breaks ties |
| Task / OB lifecycle | Supported | Create, stable-ID rename, schedule edit, and delete with Undo/Redo; the final task/OB is protected |
| Interrupts / preemption | Not supported | Scheduling is cooperative at deterministic scan boundaries; no asynchronous interrupt or execution-time model is implied |
| Editor undo / redo | Supported | Exact mutable-document snapshots preserve stable IDs, invalid work in progress, blocks, tasks, tags, and instruction properties; bounded to 150 UI operations |
| Editable project persistence | Supported | `.rpproj.json` Save/Open preserves the complete mutable editor document, including compiler-invalid work in progress and next stable-ID allocation; opening never loads the runtime, while malformed structure fails closed with `EPJ001` |
| New project lifecycle | Supported | New Project atomically resets tags, watches, blocks/routines, tasks/OBs, scan period, active/entry block, stable-ID allocation, clipboard, history, validation state, and live-monitor equivalence before creating one valid starter block/task and starter I/O tags |
| Project dirty-state tracking | Supported | TIA and Studio editor tabs show active block/routine, current `.rpproj.json` filename, and vendor-appropriate unsaved marker computed from exact project serialization; Save/Open establish a clean baseline and Undo/Redo can cross it accurately |
| Persistent watch table / Watch List | Supported | Project JSON stores an ordered typed symbol list; add/remove/add-all/clear participate in Undo/Redo; live rows show value, role, quality/scan or simulator force, and scene binding |
| Graphical element selection/editing | Supported | Canvas hit-testing selects exact contacts, comparisons, outputs, or wire insertion slots; contacts/comparisons insert at an exact series index, and selected contacts expose operand/type editing, stable-ID left/right movement, exact deletion, and visual feedback |
| Ladder clipboard editing | Supported | Edit/context menus and Ctrl+C/Ctrl+V copy selected condition instructions or complete networks/rungs; paste uses the selected series slot or following rung position, regenerates every stable rung/branch/instruction ID, and participates in Undo/Redo |
| Ladder layout integrity | Supported | Dynamic network/rung height reserves 92 px branch bands for symbol tags and captions; base and monitored conductors are segmented around contact, comparison, and output symbols; all parallel condition paths visibly rejoin before the one shared output instruction |
| Parallel branch authoring | Supported | Branches can be added or removed explicitly; deletion preserves at least one logic path and participates in Undo/Redo |
| Tag definition management | Supported | Add and edit BOOL/INT/DINT/REAL/TIMER/COUNTER definitions, roles, typed startup values, and bindings; INT/DINT ranges and finite REAL values are validated; Reset and JSON persistence preserve the authored startup values; rename propagates atomically across all symbolic operands; referenced deletion is blocked and unused deletion is undoable |
| Project Find All | Supported | Case-insensitive semantic search across declarations, tags, blocks/routines, tasks/OBs, rung labels, instruction kinds, and operands |
| Cross-reference | Supported | Exact symbol index distinguishes declarations, reads, writes, CALL/JSR sites, RETURN/RET program control, and scheduled entry points; member references resolve to their root tag |
| Result navigation | Supported | Double-click selects the owning block/routine and network/rung or the tag declaration in either vendor workbench |
| Context instruction help | Supported | Catalog-backed TIA/Logix names, purpose, operands, scan behavior, restrictions, and examples for every editor-supported instruction; F1/menu/toolbar access |
| Structured compiler Error List | Supported | Verify failures show code, exact document path, and message; double-click navigates to matching tags, tasks/OBs, blocks/routines, and networks/rungs; edits mark results stale and successful verification clears the list |
| Simulator BOOL I/O forcing | Supported | Offline input-image override before logic and output-image override after logic; conspicuous UI state; never persisted to the program |
| Force safety lifecycle | Supported | Stop de-energizes outputs while forces remain armed; Reset clears all forces; memory and non-BOOL targets are rejected |
| Graphical power-flow monitoring | Supported | Both vendor canvases color executed rails, branch prefixes, contacts, comparisons, and output instructions from stable runtime element IDs and display lifecycle/scan number |
| Monitor/editor synchronization | Supported | Highlights are shown only when executable editor logic and the loaded runtime program match; executable edits clear stale state and Undo restores it only after equivalence returns; watch-list metadata does not require a controller reload |
| Physical PLC force / online edit | Prohibited | No PLC transport exists; simulator forces can never address or write physical I/O |
| Real PLC connection or write | Prohibited | Virtual-controller core contains no PLC transport dependency |
| End-to-end offline engineering workflow | Supported | One executable acceptance test authors, saves, reopens, binding-validates, compiles, loads, scans, exchanges scene I/O, publishes monitor state, applies a simulator-only force, verifies safe Stop, and verifies Reset clears forces |

## Validation diagnostics

| Code | Meaning |
|---|---|
| `VC001` | Missing name or unknown variable reference |
| `VC002` | Unsupported or inconsistent data type |
| `VC003` | Malformed series/parallel structure |
| `VC004` | Missing or duplicate stable element ID |
| `VC005` | Invalid write target or TIMER role |
| `VC006` | Network does not have exactly one output instruction |
| `VC007` | Invalid timer preset |
| `VC008` | Invalid counter preset |
| `VC009` | Invalid literal numeric operation, including zero divisors and operands outside the supported real-number domain |
| `VC010` | Missing entry block or CALL/JSR target |
| `VC011` | Recursive block call cycle |
| `VC012` | Invalid task configuration, period, priority, or entry-block target |
| `VC013` | A TIMER instance is assigned to more than one timer instruction; explicit timer-reset references are allowed |
| `VC014` | Missing, duplicate, or unresolved block-local JMP/LABEL/LBL name |
| `VC100` | Unsupported schema/language or invalid program-level setting |
| `IO001` | Bound point is not declared by the active scene contract |
| `IO002` | Tag/point type is unsupported or incompatible; BOOL, INT, DINT, and REAL require exact type matches |
| `IO003` | Binding violates tag role or scene-point ownership/direction, or attempts to bind a memory tag |
| `IO004` | More than one PLC output tag drives the same active-scene point |

Malformed programs and invalid active-scene bindings are not executable. The UI
displays the code, JSON path, and message instead of attempting a partial run.

## Demonstration acceptance

The included conveyor program proves:

- a momentary simulated Start input seals in the command;
- simulated Stop breaks the seal;
- the conveyor moves only while its output is true;
- the plant photoeye becomes true when the product reaches it;
- the photoeye opens the NC inhibit, stops the conveyor, and clears the seal;
- Start cannot restart while the photoeye remains occupied;
- Reset clears controller memory, scan count, product position, and photoeye.
