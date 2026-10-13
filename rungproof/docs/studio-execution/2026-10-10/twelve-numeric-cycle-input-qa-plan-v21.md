# Twelve numeric-cycle inputs: bounded QA plan

Proposal only; no product edits/build/engine. Dot review required before implementation. Reuse ordinary routed camera navigation from Main.UserReadoutCapture, never direct camera setters. Five exact scenes,12 inputs,62 complete-cycle clicks total; not a new lesson/evaluator or all77 input signoff.

## Exact immutable contracts

All points below are PC DINT, initial0; each equipment has catalogAssetId training.accessory.count_display.v1, display numeric, exact action; unique numericDisplay binding with shown label and omitted format (runtime defaultG).

| Scene suffix | Equipment | Action → point | Readout label | After each press from Reset, including wrap |
|---|---|---|---|---|
| lab-9-01-sum-function | numeric_display_0 | cycle-operand_a → operand_a | A NEXT |1,2,5,10,0 |
| same | numeric_display_1 | cycle-operand_b → operand_b | B NEXT |1,2,5,10,0 |
| lab-9-02-product-function | numeric_display_0 | cycle-factor_a → factor_a | A NEXT |1,2,5,10,0 |
| same | numeric_display_1 | cycle-factor_b → factor_b | B NEXT |1,2,5,10,0 |
| lab-9-03-sum-and-counter-function | numeric_display_0 | cycle-operand_a → operand_a | A NEXT |1,2,5,10,0 |
| same | numeric_display_1 | cycle-operand_b → operand_b | B NEXT |1,2,5,10,0 |
| lab-9-04-function-selector | numeric_display_0 | cycle-operand_a → operand_a | A NEXT |1,2,5,10,0 |
| same | numeric_display_1 | cycle-operand_b → operand_b | B NEXT |1,2,5,10,0 |
| same | numeric_display_2 | cycle-function_choice → function_choice | FUNC NEXT |1,2,99,0 |
| lab-9-10-box-volume | numeric_display_0 | cycle-length_mm → length_mm | L mm NEXT |250,500,720,850,1000,0 |
| same | numeric_display_1 | cycle-width_mm → width_mm | W mm NEXT |250,500,720,850,1000,0 |
| same | numeric_display_2 | cycle-height_mm → height_mm | H mm NEXT |250,500,720,850,1000,0 |

Exact99 function choice is intentional authored fixture; preserve it. Do not limit this test to SUM/PRODUCT or demand output results. Readout string is label + newline + numericG (e.g. A NEXT\n1, FUNC NEXT\n99, L mm NEXT\n250); compare numeric point by numeric equivalence while preserving declared DINT owner/type, not CLR boxing assumptions.

## Actual routing/source grounding

Main._Input at4790 onward handles left Pressed inside shell.SceneViewportRect; TryExecuteSceneControl at4810 rejects review overlays, calls SceneControlInteractor.TryPick then ExecuteSelectedControllerAction and accepted feedback. Release must not execute another cycle. SceneControlInteractor.TryPick at120 compares only bound visible mesh AABBs; it is not general scenery occlusion proof.

Main.TryExecuteSelectedControllerAction permits PC cycle input without a loaded PLC; final path calls runtime.ExecuteAction. CyclePoint at1611 chooses next authored values entry by equivalence, wraps, SetPoint and EvaluateRules. Numeric projection at1436 reads exact binding with defaultG and existing NumericReadout child. ResetActiveController is the existing shell reset callback; runtime ResetSimulation restores initial points/poses. The all77 control gate skips these non-head records at Main.ControlInstallationReview410–416. Existing VerifySceneControlFeedback2286 only directly checks representative box-volume display fixed feedback, not these12 actual click routes.

## Minimal QA mechanism and evidence

Reuse existing Main.UserReadoutCapture algorithm via a narrowly scoped numeric-cycle target mode: validate one of the12 exact scene/equipment contracts and select its existing NumericReadout/identification label before navigation. A separate diagnostic cycle-review partial supplies a post-navigation async callback. Invoke that callback **after** existing navigation point/pose invariance assertions and **before** marking capture prepared. This avoids duplicated camera math and keeps EV/Chicken behavior unchanged. Leave SceneCameraController, picker, JSON, source actions/bindings and runtime product code unchanged. Per scene load normal tag-only default, await layout, assert no virtual program/Run/external connection, stopped runtime and authored external-clock policy. Do not switch UsesExternalClock=false or call VerifyDeclaredCases: that would enable local illustrative PLC rules and weaken no-PLC scope.

For each input:

1. Use normal Reset callback, assert all authored initial points and NumericReadout initial label/0. Snapshot every field Node3D local transform, all points and source ID/action/type/owner/binding/value-list contracts. Result/output readouts must remain at authored initial values throughout; no fabricated SUM/product/volume calculation.
2. Navigate with real viewport middle/right/wheel events from current ordinary framing to the exact NumericReadout and its immutable passive identification. Keep18px font-line-box target,12px text margins, front-facing/behind checks and finite limits. Reject aperture changes/challenge/diagnostic camera flags. Check label center is a visible actual target inside aperture; corroborate exact TryPick identity as an observation before routed click, not as a substitute for click execution.
3. Push actual left press at that projected face/label center, await a real ProcessFrame; assert the expected next PC numeric value and exact readout text. Push left release at same point, await; assert value does not advance twice. Do not call ExecuteAction/ExecuteSelectedControllerAction as the test action. Snapshot all non-target points unchanged, including PLC outputs; require field/head/readout root transforms unchanged. Numeric housing is not a pushbutton and must not travel.
4. Continue remaining authored values through wrap:8 five-value inputs +1 four-value function input +3 six-value dimensions =62 presses and62 releases across12 targets. Keep navigation/input event caps per target, not one accidental global240 cap over all5 scenes. After full wrap invoke existing normal Reset callback again; assert initial points/text/poses and stopped/no-controller state. Reset must not implicitly run or author a ladder.
5. Simplest capture sequencing: after full cycle+Reset proof, issue one final actual press/release to the first nonzero value and return to root's existing next-frame save. This yields12 readable target images without a second image-save algorithm or mid-test overwrite:62 cycle-proof presses +12 capture-state presses =74 total, each with a release. Clearly record that Reset was verified before the final capture-state click; the image is not the Reset state. One selected input per capture process is the simplest root wrapper orchestration. Final identity includes exact image/assembly/engine hash and source freeze; no contact sheet substitution.

Per-input JSON: exact scene/equipment/action/point/PC DINT/values/format/label; normal initial and navigated camera/aperture; every routed event; exact picked identity; before/press/release numeric/readout values; non-target/output/geometry invariance; Reset results;18px line-box and configured quad rectangle evidence; first-nonzero image phase. Print named checks, counts/failures and exit nonzero on any failed/missing record; do not accept a click merely because TryExecuteSceneControl returns true (it also returns true when a picked action is blocked).

Actual image review must reject target occluded by a passive shelf/panel/HUD despite a successful bound-only pick. Do not alter picker occlusion policy in this task. This proves sampled scripted in-app numeric input reach/pick/cycle/reset integration. It proves no plant sequence, authored PLC calculation, physical mouse ergonomics, accessibility, safety or all77 final approval. Human/material gates and Handle remain separate.
