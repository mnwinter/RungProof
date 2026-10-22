# CNC explicit controller implementation stages

1. Add pure command-driven mechanisms with no phase table: independent belt stock position, one Cartesian robot target with actual IK acceptance, maintained grip/access/clamp, geometric support/attachment, cut exposure only while actual interlocks permit.
2. Add narrow PalletRobotMotion controller pose/jaw API; preserve reference methods. Adapter projects real joint tool/stock positions, actual doors/tool and movable vise jaw. Reject target completion if IK fails. Interlocks block enclosure entry with closed access, unsupported release, clamp-on-held stock, cut without seated/clamped stock/closed access/robot-clear. No PLC output writes.
3. Add additive SceneIO contract and document old robot_run as motion enable only; expose targetXYZ/grip/access/clamp outputs and measured completion/interlocks. New learner V3 owns every corridor waypoint, grip/release, clamp, access, spindle and outfeed phase. A one-scan phase qualification avoids consuming stale at-target feedback on phase entry.
4. Pure mechanics + actual scene IO/compile validation; build only when root releases B3 snapshot; root alone conducts native full-cycle and inhibited-command acceptance.

No full robot collision/safety/hardware claim. Workspace and front-opening constraints are explicit educational geometric interlocks. Model timing/velocity values are simulation parameters. Existing standalone reference remains labeled reference; no controllerSequenceCommand or implicit recipe execution is introduced.
