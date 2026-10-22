# Lab 11.19 independent native review

Reviewed17proper r1 native frames using three lab1119-review sheets retaining hopper/gate/motor/valve area, three physical stacks, six Signals and runtime. Loaded reviewer fixture has a two-second mix interval; this is a QA parameter, not an independently established scene-duration specification. No reviewer UI/backend operations.

Reset23 and recipe-alone85 keep outputs/pilotsdark. Recipe+ready146 starts dose_run only and lights first green stack. Dose-complete205 switches to mixer_run only/second green stack. Timed checkpoint342 switches to discharge_valve_open only/third green stack. Physical indications agree with native three-output exclusivity throughout the17captured states. The gate and valve assemblies are visible, but their small projected poses do not give reliable independent full-stroke or exact opening proof here; shaft rotation and actual powder transfer are not established.

Readyloss403 clears all outputs/lamps despite retainedrecipe/dosecomplete. Readyrestore461 starts freshmix (dischargeFalse), then602discharges. Recipeloss664 clears all; restore725 starts mix,863discharges. Dropping dose_complete923 returns to dosing and closes discharge. AppStop976 clears all outputs with recipe/readyretained and dosecompleteFalse; Run1001 resumes dosing. Fresh dosecomplete1061 startsmix,1198discharges. AppReset0 clears all six points/lamps. This supports input-driven phase selection, requalification, timed discharge checkpoints and repeat, not continuous material processing.

No new issue. Actual powder mass/flow, gate/valve stroke limits, motor rotation, exact mix timer boundaries, mid-mix Stop retention and full input truth combinations remain unproven. No whole-scene PASS.
