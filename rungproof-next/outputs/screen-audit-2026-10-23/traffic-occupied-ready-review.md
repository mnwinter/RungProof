# Traffic READY loss while road occupied — independent native review

Independent original-pixel/action review; no input or state queries. PID12768/native1021×660; actual Lab11.12 Mobile Traffic Lights/local runtime/PLC disconnected. Seven helper originals and three root originals rehash-match respective result/action manifests (10matches). Only one queued roadA vehicle and two READY toggles are logged; no second queue or hidden state input.

[seek006](traffic-ready-occupied-r1-1792740753825830600/seek-006.png),13265: READYTrue, roadAoccupiedTrue/crossingclearFalse, Aposition−2.04, AgreenTrue/AredFalse, BredTrue. Actual blue vehicle supported approaching/in crossing.

[READYoff immediate](traffic-ready-occupied-r1-1792740753825830600/ready-off-immediate.png),13283, and [settled](traffic-ready-occupied-r1-1792740753825830600/ready-off-settled.png),13298: READYFalse, both roadredsTrue, bothgreens/ambersFalse, occupiedTrue. Aposition−1.572→−1.182; actual blue vehicle advances across marked roadway despite red withdrawal, representing already committed vehicle clearance rather than admission of another vehicle.

[READYon](traffic-ready-occupied-r1-1792740753825830600/ready-on-immediate.png),13308: READYTrue, occupiedTrue/allred, A−0.922. [after004](traffic-ready-occupied-r1-1792740753825830600/after-004.png),13368, still occupied/allred at A+0.638,60acceptedscans after restoration (1.2s at displayed20ms). [after009](traffic-ready-occupied-r1-1792740753825830600/after-009.png),13424, stilloccupied/allred at A2.094,116scans afterrestoration. Thus restored readiness and elapsed nominal allred delay do not override actual crossing occupancy in these samples.

[after010](traffic-ready-occupied-r1-1792740753825830600/after-010.png),13435: crossingclearTrue/AoccupiedFalse, AgreenTrue/AredFalse/BredTrue, A2.38. Clearance permits qualified release only after the vehicle clears. signal_conflictFalse throughout reviewed samples.

[Health](traffic-r1-health-off-on-accepted.png) directly shows accepted READYFalse and READYTrue00:32:36, with queueA00:32:34. Distinct entries plus matching action timestamps establish two accepted selector operations despite identical second-level timestamps; the logged off→on interval is about0.499s, not an exact timer acceptance measurement.

[Endpoint wide](traffic-r1-committed-endpoint-wide.png),18705, and [held](traffic-r1-committed-endpoint-wide-held.png),18765, show Aposition3.6/crossingclearTrue/bothoccupiedFalse, actual blue vehicle supported beyond crossing with unchanged endpoint pose. Later signal phase is Bgreen/Ared; ongoing alternating service is not a completion failure. RoadA head is back-facing here: do not claim directly seeing its front lenses. Actual visible Bhead and public command rows remain separate evidence.

**Q08 closes for occupied-road READYloss/allred, restoration while occupied maintaining allred beyond nominal delay, committed vehicle clearance and subsequent qualified release.** No defect demonstrated. Sparse originals do not prove every20ms state or exact delay edges; no extra queue/priority permutation or safety-rated claim is introduced.
