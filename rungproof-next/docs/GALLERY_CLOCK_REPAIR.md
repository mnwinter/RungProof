# Gallery clock repair — 2026-10-08

Later checkpoint: [typed lesson implementation](TYPED_LESSON_IMPLEMENTATION.md) closes the three typed-lesson gaps discussed below. This document retains the gallery checkpoint's historical evidence and scope.

The normal built-in gallery accepted `toggle-gallery`, published the SIM-owned
`gallery_animation=TRUE`, and displayed RUNNING, but its fan stayed frozen.
The shell assigned every scene a controller clock even when the gallery had no
PLC-owned points or loaded controller. Equipment physics callbacks remained
disabled; changing the command alone could not move the rotor.

The gallery now uses its local equipment clock in built-in mode while no
controller is loaded. Loading a controller explicitly takes clock ownership;
external mode still requires external playback. Changing `UsesExternalClock`
refreshes composed equipment callbacks immediately, including source changes
whose running/stopped state has not changed. PLC lessons retain their selected
controller clock, and blank exercises cannot execute their preview PLC rules.
No scene definition, lesson program, or PLC transport was added or changed.

## Verification

- Focused opt-in `--verify-gallery-clock` uses normal shell scene actions and
  actual engine physics callbacks, then checks the real fan rotor transform.
  Before the fix: FAIL, `localMotion=False`, `localResumes=False`, with Stop,
  Reset, external pause and blank-exercise guards passing. After the fix: all
  ten checks pass, including a memory-only fixture proving loaded-controller
  stopped/Run/Stop ownership and local recovery after unloading it. The fixture
  is not saved or supplied as a lesson.
- Native manual retest: gallery FALSE/STOPPED at entry; supported sidebar action
  made TRUE/RUNNING and the focused fan visibly rotated across captures. Stop
  made FALSE/STOPPED and held the rotor; Reset restored its authored pose.
  Native screenshots are retained in the verification conversation, not a new
  PNG archive. Owned native processes exited successfully; no forces were used
  in this repair retest.
- Build: zero warnings/errors. Virtual-controller suite: 149 passed, zero failed.
  Existing app-shell, virtual-controller, ladder-editor, numeric scene I/O,
  guarded-transfer and EV checks pass. All 26 existing scene contracts exit 0;
  only 19 contain active cases (38 cases). Gallery has no declared cases, and
  three unavailable exercises plus powder/guarded/EV have initial-only cases;
  these static exits are not active-response acceptance.
- Existing 3D scene-control verifier passes with the native renderer and review
  overlay. Earlier headless attempts failed its mouse-picking assertions on the
  pre-fix build; they are not reported as green tests. The isolated gallery
  regression establishes the specific red/green result without those picking
  assertions.

Local logs: `.tools/gallery-clock-{red,green,green-build,controller-tests}.log`,
`.tools/gallery-clock-native.log`, `.tools/gallery-clock-native-scene-controls.log`,
`.tools/gallery-clock-verify-*.log`, `.tools/gallery-clock-audit-*.log`, and
`.tools/gallery-clock-contract-reruns.json`. Exact per-scene dispositions remain
in `.tools/streamlined-77-coverage.{json,csv}` and
`.tools/quick-actuator-26-results.{json,csv}`.

## Bounded coverage and remaining unavailable behavior

| Evidence level | Scene count | Meaning |
| --- | ---: | --- |
| Fresh native direct/fixture response | 2 | Gallery repair retest; powder indicator channels tested earlier in the same pass before the gallery patch |
| Fresh modeled active response | 21 | 19 current standalone contracts; two current direct-command/loaded reference checks |
| Retained bounded response records | 51 | Reused evidence, not refreshed all-scene rendered acceptance |
| Active response unavailable | 3 | Drive string, chicken label, motor enum |
| Current confirmed failures | 0 | Gallery failure repaired and retested |

The 26 formerly missing response checks now have 23 bounded passes and three
unavailable dispositions. Default lesson programs remain blank where they were
blank. Forced/reference response never proves a completed lesson controller.
The powder equipment is explicitly static: only indicator channels were proved,
not powder flow, mixing or discharge mechanics. REAL threshold A/B behavior and
STRUCT root rename/load were checked separately earlier in the pass; root rename
is binding/load/projection proof, not changed motor semantics. ARRAY evidence is
reused because its root rewrite and aggregate leaf compilation share that path.

| Unavailable scene | Exact missing behavior | Smallest bounded option; larger option requires separate scope |
| --- | --- | --- |
| `lab-10-01-drive-alarm-code-string` | No active controller/rules; exposed inputs/outputs are BOOL flags. Actual STRING parsing and code matching are absent. | A valid independent BOOL I/O fixture could test indicator bindings only. Actual matching requires a defined string representation and supported matching semantics. |
| `lab-10-02-chicken-label-print` | No active controller/rules; flags do not model weighing, formatted label contents, printing or label application. | An independent BOOL I/O fixture could test the two indicators only. Real lesson behavior needs defined weight/label data and a modeled print/application process. |
| `lab-10-04-motor-enum-state` | No active controller/rules; `motor_running` and `state_valid` are BOOL channels, not an ENUM state machine. | An independent BOOL I/O fixture could test motor/indicator bindings only. ENUM behavior needs an explicit state representation, transition rules and fault/reset semantics. |

Generated empty I/O documents fail Verify+Load with EDIT001 (no networks), and
the supported BOOL force table requires a valid loaded controller. No broad
new programs were authored to fill these gaps. Simulator evidence is separate
from physical behavior, safety validation, vendor parity and live commissioning.

## Preservation

The repair starts from `a42d06e` on `agent/add-config-foundation`. Only the two
clock/regression source files and this evidence document belong to the local
checkpoint. All 107 unrelated files are checked against the retained baseline
hashes. Earlier temporary ladder edits were restored/reloaded without Save;
the powder force was cleared by Reset and the native monitor confirmed no
simulator forces. No push, merge, publication, deployment or live PLC action.
