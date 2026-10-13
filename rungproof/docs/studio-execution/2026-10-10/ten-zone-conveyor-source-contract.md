# Lab 10.6: purposeful ten-zone conveyor candidate

This supersedes the Lab 10.6 two-row motor test-rig proposal. Lab 10.4/10.5 keep their supported single-motor fixtures. Source is implemented; fresh native and rendered acceptance is pending.

The exact scene ID and motor_0…motor_9 identities remain. Ten inline world-X zones each span 1.4 m, with centers -6.3 + 1.4×index, 1 m belt width and a 1.29 m load-support datum. Each original motor is mounted at actual feet Y=.72, yaw90°, with its actual shaft exit measured into the corresponding cross-Z drive axis. Supported side platforms, grounded zone frames, measured coupling and a closed illustrative reduction case provide a visible mechanical purpose. The fixed 30.37:1 reduction illustrates 1450 RPM shaft to a .09 m drum at .45 m/s; it does not establish torque, friction, slip, sizing or safety.

For each zone the effective accepted command remains OR(motor_array_run, motor_i_run, motor_commands[i]). Ready/alarm permissives belong to the learner's controller. No new sensor, fault or output point is invented. Existing command-count and sequence readouts retain their original meaning.

One finite .60 m teaching carton starts fully inside zone0. Every zone overlapped by its leading/trailing edges must be commanded for travel. A stopped downstream zone clamps the leading edge at its entry; stopping a zone already overlapped freezes the carton without backward teleportation. The model checks the whole requested travel distance, preventing large-step tunneling. It holds the carton fully on zone9 at discharge; there is no automatic reload or invisible receiver. Because there is only one load, queue-spacing behavior is outside this candidate's scope.

Original shafts, geared drums and independent worn-belt UVs share the accepted runtime plant clock. Automatic motor callbacks are disabled even when the application refreshes playback clocks. Explicit Stop freezes the plant, shafts and UVs; explicit Run resumes; Reset restores load, bookkeeping, UV, mounted roots and authored local shaft poses.

Native APIs: MotorConveyorZonePlantModel.Step(double,bool[10]), Reset(), Center/LeadingEdge/TrailingEdge/Zone/AtDischarge; runtime HasMotorConveyorZones and CaptureMotorConveyorZones(); SceneComposer.VerifyMotorConveyorGeometry(root). Geometry root: MotorConveyorInstallation/Zone_i with motorId/zoneIndex metadata, ZoneBeltSurface, KIN_zone_drive and static Z0…Z9 labels. Original motor metadata motorConveyorZone=index maps identity explicitly.

Required fresh checks cover group/scalar/ARRAY/mixed OR, clearing one of multiple true sources, actual independent shafts/drums/belt offsets, stopped downstream and straddled-zone freeze, resume, fixed-delta equivalence, terminal hold, actual Main Stop/Reset, supported roots and exact existing operator picking. Fresh full/split and moving/close views must establish load, labels and belt witness visibility. Earlier motor-rig captures are historical evidence only.
