# Lab9.3 independent operand preset cycle review

Independent original native pixels and native-ui-actions.jsonl; no UI input/runtime queries. PID12768/client1021×660; actual Lab9.3 Sum and Counter Function/local runtime/PLC disconnected. Eleven cited originals rehashed against action records; all match. Existing repaired09-sum-counter reference was loaded through normal File UI, not altered for these cases.

[Qualified zero](sumcounter-r1-qualified-zero.png), scan4029, shows calculate_requestTrue/call_completeTrue/inputs_validTrue, A0/B0, sum_result0/result_validTrue, event_count1 and event_countedFalse. The physical qualification sequence toggles INPUTS VALID(409,306), CALCULATE(451,322), CALL COMPLETE(497,340).

| Physical input sequence | Original native operand/result values |
|---|---|
| Five separate A NEXT(556,357) clicks | [A1](sumcounter-r1-a-1.png), [A2](sumcounter-r1-a-2.png), [A5](sumcounter-r1-a-5.png), [A10](sumcounter-r1-a-10.png), [A0](sumcounter-r1-a-0.png): A1→2→5→10→0, B stays0; sum equals A at every frame. |
| Five separate B NEXT(618,378) clicks | [B1](sumcounter-r1-b-1.png), [B2](sumcounter-r1-b-2.png), [B5](sumcounter-r1-b-5.png), [B10](sumcounter-r1-b-10.png), [B0](sumcounter-r1-b-0.png): B1→2→5→10→0, A stays0; sum equals B at every frame. |

All ten preset originals directly show result_validTrue and event_count1, with calculate_request/call_complete/inputs_valid continuouslyTrue. Green result-valid indicator remains lit. In B10, both operand_b and sum_result are visibly numeric**10**, not OCRlettersIO. Accepted-action footers identify the corresponding operand cycle each time. Each operand control changes its own row independently and wraps to0 after10; the other operand stays unchanged. Holding CALL COMPLETE across these numeric edits does not add counts in the observed sequence.

**Q24 closes for both exposed preset cycles and their observed valid arithmetic/count behavior.** No product fault demonstrated. This is not a25-combination matrix, overflow/negative arithmetic test, or independent proof of all counter invalid/reset lifecycle behavior; those remain separately covered or bounded. No new crossproduct requirement is introduced.
