# Lab 5.7 independent selected-phase review

Reviewed22selected native checkpoints in three lab507-selected-review sheets, preserving actual far-side pedestrian/vehicle heads, eight Signals and runtime. No full-frame sweep; no reviewer UI/backend operations. Loaded fixture scope only. This scene supplies no moving people/vehicles and intentionally omits flashing-hand teaching.

Idle5006has vehiclegreen/amberhand, readyTrue/pedestrian_stopTrue, walkFalse. Fresh CROSS5063 producesphysicalvehicleamber+hand; allred5114red+hand; walk5146physicalwhitewalkingfigure+vehiclered with walkTrue/pedstopFalse. PATHblocked5168keeps walk/red initially; later5284clearance returnshand/red and holds5449with clear_to_finishFalse. CROSSwhileunavailable5470does not create a later queuedwalk: PATHrestore5497returnsidlegreen/hand;5604remainsidle. These are bounded phase/checkpoint observations, not exact duration or continuous mutual-exclusion proof.

Fresh CROSS5700enterswalk/red beforeAppStop. Stop5734clears all lightoutputs in first capture; held5734keeps stoppedscan and allvehiclecolors/walkFalse, with pedestrian_stopTrue in native Signals (physicalheadsremainunlit duringStop). Run5743shows startupred/hand/sequence_runningTrue and later5939idlegreen/hand. Thus AppStop interruptswalk and Run reinitializes throughstartup rather than resumingwalk. Exact post-Stop pedestrian point/render policy is not separately specified or classified as a defect from these snapshots.

ResetRun25has PATHFalse, physicalred/hand, walkFalse andsequenceTrue. Later223holdsred/hand awaitingPATH. PATHTrue251idlegreen/hand; freshCROSS271amber,347walk/red,462hand/redclearance,633idlegreen/hand/sequenceFalse. All running physicalphaseindications match native Signals in reviewed checkpoints.

No new issue. Exactphase/allred durations, repeatedCROSSduringactivewalk, scan-continuous conflictingindications, alternatecameraheads and flashing-hand behavior are unproven or outside this lesson. No whole-scene PASS.
