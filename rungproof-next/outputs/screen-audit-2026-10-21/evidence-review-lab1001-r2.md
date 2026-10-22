# Lab10.1 default starter r2 evidence review

Reviewed baseline-r2 plus18 monitored-r2 native frames. Three compact sheets/hash manifest capture six Signals including actual STRING, three physical indicators and runtime. Excluded original wrong-control calibration records. No reviewer UI input/backend tests.

Actual STRING sequence read from native Signals: empty; Drive alarm F003: undervoltage; Drive alarm F030: overload; Drive alarm F0030: different code; Drive alarm XF003: prefix; Drive alarm f003: case differs; empty; F003 repeat. Only exact-code F003 message sets alarm_code_found, drive_alarm_active and alarm_match_validTrue. All three goFalse for F030/F0030/XF003/lowercase/empty. String-valid remainsTrue during these comparisons and resetFalse.

Physical alignment: matching F003 lights cabinet upper red stack segment, second stack bottom green, and the separate pedestal red alarm pilot. All three darken for rejected strings/invalid/reset/Stop and relight on valid recovery. The VFD/fieldbus message display is rendered but its tiny projected characters are not independently readable here; full message assertions come from flat Signals. No motor-rotation claim.

With retained F003, string-validOFF clears all three outputs/indicators; ON restores. Maintained alarm_resetON clears outputs/indicators despite valid retainedF003; OFF restores. AppStop retains validF003 but outputsFalse/indicatorsdark, STOPPEDscan2721. Run restoresallTrue/lit at2808. AppReset clears textempty and inputflagsFalse/outputsFalse, STOPPEDscan0; Run remainsclear/dark at88. Freshvalidempty staysdark at171; fresh F003 returnsallTrue/lit at255.

No new demonstrated issue. Default starter controller scope; selected-code editing variant handled separately. Token-boundary cases beyond the supplied message set, arbitrary string entry/limits, simultaneous changes and exact scan latency remain unproven. No whole-scene PASS claim.
