# Lab 4.11 independent native review

Reviewed 25 normal motion captures, final native Signals, reset and five recovery checkpoints using four compact motion sheets and lab411-recovery-review.jpg. Loaded reference scope only; no reviewer UI or backend operations.

The barrel indexes beneath the fill station, its blue level rises while the overhead source level falls, then it moves to the outfeed. At motion10 the native Signals show at_fill=True, valve=True, flow=20, barrel/source=1.2/198.8 L. Subsequent captured pairs are 19.6/180.4, 38/162, 56.8/143.2, 72.4/127.6, 93.6/106.4, 112/88, 130.8/69.2 and 149.6/50.4. Every pair totals 200 L. Motion19 reaches 150/50, fill_complete=True, valve=False and flow=0 while outfeed resumes. Final lab411-completed-final.png (scan5911) shows barrel_parked/exit_beam/cycle_complete=True; active, infeed and valve=False; flow=0 and fill_fault=False. The physical barrel is at the downstream end.

Reset returns the barrel upstream, barrel/source=0/200, completion=False and commands off. Stop catches filling: lab411-stop-filling-early.png and -late.png both STOPPED scan599, at_fill/fill_beam=True, barrel/source=46.4/153.6, valve/infeed/active=False and flow=0. Positions and levels remain identical across the 1.2-second interval. Run alone (scan647) retains those levels and remains inactive. Fresh START (scan690) restores valve/active=True and flow20 with 60.8/139.2 L. Recovered final lab411-recovered-150L-final.png (scan4064) again shows 150/50, parked/complete=True, commands off, flow0 and no fault. Captured conservation and recovery are supported; exact physical numeric display text is not required for these Signals readings.

No new issue. Downstream obstruction, low source/fault injection, overflow prevention, exact flow and scan timing, the distinct machine Stop path, and continuous motion between captures remain unproven. No whole-scene PASS.

