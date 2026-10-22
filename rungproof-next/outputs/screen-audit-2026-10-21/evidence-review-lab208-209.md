# Independent Labs 2.8 and 2.9 review

Reviewed full identity/layout frames, actual physical-stacklight/Signals/runtime crops in lab208-evidence-compact.jpg and lab209-evidence-compact.jpg, source manifests and recorded UI plans. No UI input, backend execution, app changes or repair planning by this reviewer. Actual values below were read from image pixels and checked against help; helper success alone was not accepted as evidence.

## Lab 2.8 — Inspection Vote Stacklight (eleven cases)

| Cases | vote_a | vote_b | pass | hold | conflict | Visible stacklight |
|---|---|---|---|---|---|---|
| 00 and 00-repeat | False | False | False | False | False | Dark |
| 10 | True | False | True | False | False | Green only |
| 11 and 11-repeat | True | True | False | False | True | Red only |
| 01 and 01-repeat | False | True | False | True | False | Amber only |
| app-stop | True | True | False | False | False | Dark / STOPPED |
| app-run | True | True | False | False | True | Red only / RUNNING |
| active-reset | False | False | False | False | False | Dark / STOPPED, scan0 |
| reset-run | False | False | False | False | False | Dark / RUNNING |

Physical left VOTE A and right VOTE B controls correspond to the named symbolic inputs. All four input combinations, repeated no-vote/hold/conflict states and exclusive color outputs are supported. App Stop removes all outputs but retains both votes; Run restores conflictRed. Reset from both votes active clears both and leaves the next Run dark. This agrees with documented single-vote green/amber and simultaneous-vote red behavior.

## Lab 2.9 — Maintenance Beacon Selector (fourteen cases)

| Cases | Actual position | red | amber | green | Visible result |
|---|---|---|---|---|---|
| off, cycle1-pos0, cycle2-pos0 | 0 | False | False | False | Dark / RUNNING |
| cycle1-pos1, cycle2-pos1, active-before-stop | 1 | True | False | False | Red only / RUNNING |
| cycle1-pos2, cycle2-pos2 | 2 | False | True | False | Amber only / RUNNING |
| cycle1-pos3, cycle2-pos3 | 3 | False | False | True | Green only / RUNNING |
| app-stop | 1 | False | False | False | Dark / STOPPED |
| app-run | 1 | True | False | False | Red only / RUNNING |
| active-reset | 0 | False | False | False | Dark / STOPPED, scan0 |
| reset-run | 0 | False | False | False | Dark / RUNNING |

Recorded physical selector clicks traverse two complete 0→1→2→3→0 cycles with exact numeric positions and exclusive corresponding channels visible. App Stop retains position1 but darkens all outputs; Run restores red. Reset from active position1 restores0/all dark; next Run stays dark. These are display simulation states; the help's lockoutRed wording does not establish a real isolation/safety function.

No new application issue was observed. Results apply to these GUI-loaded-fixture cases only. Remaining scope includes arbitrary/out-of-range selector entry, transient/rapid input, held-control semantics, exact scan response, authoring/edit/cancel/persistence, keyboard accessibility, navigation restoration and fresh-session reproducibility. No whole-scene or full-app PASS claimed.
