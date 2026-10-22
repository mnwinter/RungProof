# Independent saved-frame review — Lab11.6 wastewater

Reviewed actual native PNGs only, no live UI/runtime/API/source execution. Terminal second-run files are present, including lab1106-r2-low-shutdown.png. This is bounded evidence review, not blanket scene PASS. No new defect established.

## Observed normal process and second-run repeat

| Saved native frame | Directly readable observations |
|---|---|
|lab1106-reset-repeat-baseline.png|RUNNING scan41; inflowTRUE, actual inflow4%/s; level37.24%,9.9584mA; highFALSE; treatment/clearFALSE; pump/valveFALSE, valve position0, actual transfer0. This is already post-reset Run+inflow, not exact35% stopped Reset baseline.|
|lab1106-repeat-await-high-1792662996137403000.png|RUNNING scan435; level68.76%,15.0016mA; actual inflow4,transfer0; highTRUE; both downstream permissivesFALSE,pump/valveFALSE. Compared with previous frame, rising level and instrument column heights are visible. This brackets high assertion between37.24 and68.76; exact rising threshold not established.|
|lab1106-r2-high-no-permissives.png|RUNNING scan3367; inflowFALSE; level100%,20mA; fullTRUE/highTRUE; both permissivesFALSE,pump/valveFALSE, valve position0, transfer0.|
|lab1106-r2-ready-no-clear.png|RUNNING scan3427; treatmentTRUE,outlet_clearFALSE;100%/20mA/highTRUE; pump/valveFALSE and actual transfer0. Establishes missing-clear blocks an otherwise high/treatment-ready state.|
|lab1106-r2-eligible-transfer.png|RUNNING scan3480; treatmentTRUE/clearTRUE/highTRUE; inflowFALSE; pump/valveTRUE, delivered valve position100, actual transfer8%/s; level96.8%,19.488mA; inhibitedFALSE. Native green indication on the cabinet differs from stopped-command frames.|
|lab1106-r2-low-shutdown.png|RUNNING scan4040; both downstream permissives stillTRUE; level20%,7.2mA; highFALSE,pump/valveFALSE,valve position0,transfer0,inhibitedFALSE. Low-level shutdown after actual drain is independently evidenced.|
|lab1106-await-transfer-1792662874493801300.png|Earlier run: level33%,9.28mA with highTRUE,pump/valveTRUE,position100,inflow4,transfer8. High retained below its rising region supports hysteresis rather than simple instantaneous high comparison.|

All listed mA values are numerically consistent with4+0.16*level_percent. This arithmetic cross-check is not calibrated instrumentation proof. Native colored level columns across the four vessels are visibly higher at100 than20, supporting shared bank presentation. Opaque tanks and full-scene camera do not independently reveal precise internal surface elevations or confirm equal-volume hydrodynamics.

## Interruption and recovery: narrow credit only

lab1106-clear-loss-held.png shows outlet_clearFALSE,treatmentTRUE; pump/valveFALSE,position0,transfer0,level37.88%,10.0608mA,highFALSE. lab1106-treatment-loss-held.png shows treatmentFALSE,clearTRUE with identical level/commands/rates. These establish sampled missing-permissive states and held level, but high was alreadyFALSE: these frames alone do NOT prove either loss interrupted active transfer. The r2 high/ready/no-clear→eligible pair supplies a stronger clear-permissive truth-table test; no equivalent highTRUE+treatmentFALSE+clearTRUE active-loss pair was reviewed here.

lab1106-stop-held.png: STOPPED scan8894; level37.88%,10.0608mA,commands/rates0. lab1106-run-resumed.png: RUNNING scan8906, same level and zero commands because highFALSE. This proves Stop/Run status and held inactive level, not resumption of an active pump stroke. No claim of active-phase Stop recovery from these two frames.

## Visual/coverage limits and findings

Four vessel structures, common header, pump, outlet valve and operator cabinet are visibly present. Cabinet green indication changes with eligible commands; transfer0/8 and delivered valve0/100 are readable native feedback. Fine valve lever travel/pump shaft rotation are too small in these full-scene captures for independent movement proof. Native snapshots do not measure actual hydraulic flow or exact scan-boundary timing. Overflow-rate behavior while inflow remainsON at100%, exact high assertion boundary, source-empty boundary and simultaneous active-trip cases remain unproven by reviewed frames.

No issue-master changes made. New evidence supports a normal rise/high→eligible drain→20% shutdown and a second run after reset/setup. Keep the first recovery batch bounded rather than attributing its already-inactive outputs to successful interruption of active transfer.

## R3 active-interruption follow-up — closes earlier inactive-state gap

Actual lab1106-r3-* native PNGs now reviewed through low-final. Exclude lab1111-baseline from elevator evidence: parent identifies it as still showing Lab11.6 after an unsuccessful navigation attempt. R3 fill helper's empty contact-sheet exception is QA postprocessing, not an app defect; its action steps could already have completed.

| R3 frame | Direct native observations |
|---|---|
|active|RUNNING scan571,level75.48%,16.0768mA,highTRUE,inflowFALSE; ready+clearTRUE,pump/valveTRUE,position100,transfer8. Establishes genuinely active transfer before loss.|
|clear-held|RUNNING scan645,clearFALSE,readyTRUE,highTRUE;level74.36%,15.8976mA,pump/valveFALSE,position0,transfer0. Cabinet green indications dark.|
|clear-restored|RUNNING scan665,clearTRUE,highTRUE;level73.24%,15.7184mA,pump/valveTRUE,position100,transfer8. Resumed drain, green indications restored.|
|treatment-held|RUNNING scan745,readyFALSE,clearTRUE,highTRUE;level71.16%,15.3856mA,pump/valveFALSE,position0,transfer0. Establishes missing-ready stops despite retained high and clear.|
|treatment-restored|RUNNING scan771,both permissivesTRUE,highTRUE;level69.08%,15.0528mA,pump/valveTRUE,position100,transfer8.|
|stop-held|STOPPED scan791,highTRUE,both permissivesTRUE;level65.88%,14.5408mA,commandsFALSE,position0,transfer0. Native stop.png and stop-held.png are visually identical; no advancing scan/level in held Stop.|
|run|RUNNING scan801,highTRUE,level64.28%,14.2848mA,commandsTRUE,position100,transfer8. Genuine active recovery after App Run.|
|low-final|RUNNING scan1351,level19.96%,7.1936mA,highFALSE,commandsFALSE,position0,transfer0,inhibitedFALSE. Recovered transfer completed at low threshold region.|

R3 now supports BOTH active permissive losses, restoration and active Stop/Run recovery, unlike the first batch. Saved held/cause/restored sequence and actual accepted-action status substantiate response; no exact one-scan latency or hydraulic physics claim. Final19.96% versus earlier20% is a sampled low-threshold region, not a new defect. No master issues edited and no new app issue established.

QA helper correction: ui_batch.py now detects zero requested capture frames and returns plan/step/capture/contact_sheet/status metadata instead of attempting a zero-height JPEG. Native input/capture behavior for nonempty plans is unchanged. Syntax checked without executing helper or sending input. SHA256 before4e00adca35f96a6df1d13edc8650ce5492cd27c6e615e49c3d4c214ebed99333; after92070803574a9a9e96ccdde6e6ac5be167d8df0cc4da4f2b586edc7f80216623.
