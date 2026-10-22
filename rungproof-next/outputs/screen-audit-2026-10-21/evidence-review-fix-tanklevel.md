# Tank Level /4–20mA post-fix native review

Actual `fix-tanklevel-review-compact.jpg` shows these Signals samples: stopped49.98→11.9968mA; Run fill61.74→13.8784; high cutoff80.08→16.8128; priority drain65.14→14.4224; low cutoff19.96→7.1936; Reset42→10.72; fresh repeat64.68→14.3488. Every sampled pair satisfies ideal4+0.16*level. High/low states and pump/drain commands match the displayed limits and physical gauge height. Both requestsTrue during drain produces pumpFalse/drainTrue; low cutoff closes both.

Corrected pre-dock gauge crop `fix-tanklevel-stop-gauge-review.jpg` shows gauge rises between initial filling and active Stop, then matches in Stop/held. Stopped monitor49.98/requestTrue/outputsFalse/both limitsFalse establishes a midrange interruption. Run resumes to61.74/pumpTrue. Reset42 clears requests/outputs and fresh repeat fills to64.68, confirming recovery/repeat.

QA-FUNC-001 two Tank Level pump/drain binding subcases resolved for these observed normal fill/high cutoff/drain priority/low cutoff/activeStop-Run/Reset/repeat routes in this built app. No direct internal surface/precise cutoff latency or real transmitter calibration claim. First three panels in the broad compact sheet crop the old camera incorrectly; use the corrected dedicated gauge strip for those physical comparisons. No reviewer UI input.
