# Lab 9.1 independent screen evidence review

Reviewed 26 unique unambiguous native captures, seven Signals, physical A/B/SUM readouts, green validity lamp, and runtime. `lab901-review-unique.json` lists included files; four `lab901-review-compact-*.jpg` sheets and source SHA256 manifest preserve review provenance. Sheet layout is an inventory, not chronological order: the fresh B-wrap frame precedes lifecycle frames by visible scan. Full selected2-5 frame confirms scene and crop locations. No UI input or backend checks by reviewer.

Excluded `lab901-A2-B5.png` entirely from ordered/timing credit because the tester reused its name and overwrote it; its historical OCR may describe a different scan. This is a QA evidence naming fault, not an app issue. Fresh `lab901-B-wrap5-final.png` independently shows A2/B5/SUM7, validityTrue, greenlamp, RUNNINGscan9190.

At A2/B5, all eight combinations of A-valid / B-valid / CALCULATE are visible: 000,100,110,010,011,111,101,001. Only111 lights green and sets sum_result_validTrue. Before the first valid calculation SUM remains0. After calculating7, loss of any permissive darkens the lamp and clears validity while the numeric SUM retains7. Help specifically requires validity to clear; retention of the old number alone is not a demonstrated defect. Re-enabling111 restores green/valid7.

With all permissivesTrue, unique A selection frames show A5/B5/SUM10; A10/B5/15; A0/B5/5; A1/B5/6. Earlier selected2-5 and perm111 frames supply A2/B5/7, but the excluded duplicate prevents attributing a complete uninterrupted A wrap trace to its filename. Unique B frames show A2/B10/SUM12; B0/2; B1/3; B2/4; fresh B5/7. Every observed physical readout agrees with corresponding Signals, and all these calculation frames show validityTrue/green.

App Stop: input A2/B5 and all three permissives retained, SUM0/validFalse/dark, STOPPEDscan9252. App Run: same inputs, SUM7/validTrue/green, RUNNING9337. Active App Reset: A0/B0, all permissivesFalse, SUM0/validFalse/dark, STOPPEDscan0. Subsequent Run remains zero/false/dark at scan88.

No new demonstrated issue. This is the explicitly loaded sum reference, not default empty-editor behavior. Unproven: negative/arbitrary values outside the manual0/1/2/5/10 selection set, overflow, simultaneous changes, exact update latency, and uninterrupted A-cycle wrap ordering at the excluded checkpoint. No whole-scene PASS claim.
