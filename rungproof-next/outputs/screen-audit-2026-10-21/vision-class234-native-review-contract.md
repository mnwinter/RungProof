# Lab10.3 class2/3/4 native review timing

Source-derived comparison only. Existing learner fixture requires package_present AND vision_result_valid AND destination_clear AND class1..4 AND NOT sort_complete for BOTH sort_conveyor_run/diverter_enable. The plant latches class at first accepted moving scan; later visible selector changes do not redirect an already moving carton. Reset before each independent route.

`VisionSorterPlantModel.cs`: infeed X=-1.1 to3.3 at0.45m/s =9.778s. Index speed45deg/s. Then actual radial discharge0 to6.1 at0.45m/s =13.556s. Timings below assume every permissive staysTRUE, both PLC outputs stayTRUE, and accepted runtime scans advance continuously; paused wall time does not count.

|Class|Actual target yaw|Index interval from first accepted move|Ideal completion|Native geometric destination / terminal carton center|
|---|---:|---|---:|---|
|2|+22.5deg|9.778..10.278s|23.833s|lane2, scene-negative Z side; center about(8.936,Y,-2.334)|
|3|-22.5deg|9.778..10.278s|23.833s|lane3, scene-positive Z side; center about(8.936,Y,+2.334)|
|4|-67.5deg|9.778..11.278s|24.833s|lane4, farther positive Z direction; center about(5.634,Y,+5.636)|

Camera screen left/right is not world Z. Compare actual lane labels/geometry, not a guessed screen row. Source lane centers2/3 are(7.550,0,-/+1.760), yaw+/-22.5; lane4(5.060,0,4.250), yaw-67.5. `SceneSimulationRuntime.VisionSorter.cs` projects actual model carton/platen pose, publishes sort_phase, sort_latched_class, sort_complete, and animates only the selected lane belt by actual radial travel.

## Lean native evidence sampling

Five frames per independent route are enough to distinguish stages: baseline after exact chosen class+permissives; about9s infeed near table;11.5s aligned and starting outbound;17s clearly on selected receiving lane;26s final. Use one short native movie/contact strip across9..12s only if table alignment itself is uncertain; no need for full-frame OCR every motion tick. Because root motion37604 is already running, review its elapsed timestamps against these thresholds rather than restarting only for ideal timing.

Final actual visible evidence should show correct receiving lane, sort_latched_class unchanged, sort_completeTRUE, and both learner commandsFALSE on following scan (allow20ms). A readable Signals screenshot alone cannot establish correct geometry; retain a native scene frame with actual carton position. Any permissive loss freezes model and causes learner outputsOFF; restore resumes current phase. App Stop likewise freezes; Reset returns home and clears latch/completion. These are source expectations pending native observations, not PASS results.
