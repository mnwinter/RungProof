# Final verification run plan V10

Read-only preparation, 2026-10-11. Root is sole build/engine/capture owner; main screen and all user input remain reserved. This plan does not claim V10 execution or rendered approval.

## Freeze before execution

Record fresh V10 assembly/source hashes and build log, including mount helper, caption-fit changes and review sources. V9 baseline is `industrial-mount-source-freeze-v9.json`, assembly SHA256 `4597A187B712DD314D10B8EE8A7C2994858DB3C79DB7CE254C9B5D509DDCAEA9`. Retain both inventory JSON/MD baselines and V7 authored-scene hashes; mount QA deliberately checks all 77 scene JSON files against frozen V7, not a second current-source load. Keep original V9 logs and GUID evidence files.

## Six separate native gates

Use existing root engine invocation and fresh V10 log filenames. Pass each flag after Godot's `--` user-argument separator, one gate per process; flags are mutually dispatched, not a combined command.

| Gate | Existing flag | V9 acceptance to retain |
|---|---|---|
| Full industrial mount inventory | `--verify-industrial-mounts` | INDUSTRIAL_MOUNT_RESULT cases=2205 passed=2205 failed=0; scenes=77 lights=121 displayCandidates=54; GUID evidence JSON; exit0 |
| Full controls | `--verify-control-installations` | CONTROL_INSTALLATIONS_VERIFY PASS scenes=77; authoredCaseScenes71, functionalPicks163, acceptedPresses138, selectors17, prerequisiteBlockedActions8; exit0 |
| Repeat-cycle learner | `--verify-repeat-cycle-learner` | 290 named learner assertions, failures0 and PASS; exit0 |
| Repeat-cycle lifecycle | `--verify-repeat-cycle-lifecycle` | 47 named lifecycle assertions, failures0 and PASS; exit0 |
| Existing typed learner workflows | `--verify-typed-learner-workflow` | 456 named TYPED_LEARNER_CHECK assertions; failures0; actual editor/joint callbacks20/40/100; real PLC transport/connection FALSE; exit0 |
| Motor conveyor and supported templates | `--verify-motor-conveyor-zones` | MOTOR_CONVEYOR_AUDIT_RESULT failures0; actual draft lifecycle and VerifyTemplateProjectWorkflow are included by this entry; exit0 |

The repeat counts are intentionally assigned from actual V9 logs: learner290, lifecycle47. There is no separate template-only verification flag in current dispatch; the motor gate calls its workflow helper. Preserve its expected injected WORKSPACE_LOAD_FAILED negative-case warnings as labeled fault tests, but reject unanticipated engine errors, unhandled exceptions or failed assertions. Read both result and process exit; neither alone substitutes for the other. Any changed assertion count needs a explained source delta, not automatic comparison waiver.

V9 source logs inspected: `.tools/studio-verification/industrial-mount-{all77,control-installations,repeat-learner,repeat-lifecycle,typed-learner-workflow,motor-conveyor-zones}-v9.log`. All six end ROOT_GODOT_EXIT=0; result summaries report zero failures. No unexpected ERROR/Unhandled/FAIL match was found in these selected logs during this read-only review. Compile failure V8 was name-resolution failure, not functional RED.

## Label-fit reviewer checks and gaps

Current panel-button fitting (`SceneComposer.ControlInstallations.cs`, OperatorFaceLabel branch) measures configured text with actual font GetStringSize, centers the label in the retained legend-plate parent frame, includes X20mm/Y10mm total insets, and shrinks PixelSize without changing text/config. It retains a .0018 upper pixel-size cap. Actual head .70 scale applies afterward to both plate and label. Review the transformed plate dimensions, not original Blender text's left origin.

Potential edge cases to inspect: empty text, multiline/trailing newline, longest configured label, narrow plate, nonuniform plate-parent transform, rotated/reopened template assembly, fallback font versus explicit font, and outline/shadow extent. Extremely long text can mathematically fit yet be unreadable because there is no lower legibility threshold. Tiny plate dimensions are clamped to .001m rather than rejected. These are review limitations, not evidence that a failure occurs in current authored labels.

Industrial mount QA currently protects typed label paths and supports but does not prove every button/readout glyph lies within its plate/screen. No dedicated panel-label fitting test project or explicit per-label glyph-containment assertion was identified in the inspected source. Chicken presentation QA separately uses generated ink vertices and physical paper margins; it is not a generic HMI/button fit test. Do not use its ink pass to approve all captions/readouts. Readout STRING/ENUM/numeric screen fitting is a separate concern from button face-label fitting.

After gates pass, root must inspect actual V10 normal/detail renders of all affected mounted scenes, especially Lab4.3, long STRING heads, four-color tower, gallery, wastewater beacon, VFD station, Chicken machine readout and numeric selector panels. Use intended/wrong typed text, NO DATA, count/units and populated values. Verify retained supports, no floor bases, readable text, no overlap, and useful operator-facing orientation. Mounted metadata/geometry gate success does not establish rendered readability, hardware behavior, material approval or physical commissioning. Material75 gate remains separate.
