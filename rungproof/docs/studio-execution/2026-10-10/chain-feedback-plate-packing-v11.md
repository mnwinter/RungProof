# Chain feedback plate packing correction, V11

Dot found actual V10 cabinet pilot pitch of .418 m with .42 m legend plates, creating a 2 mm overlap. The earlier V10 report's source-only claim that those plates fit was incorrect; V10 native/render evidence remains historical and is not accepted as final.

Root authorized a narrow geometry correction. Plate width is now min(.42 m, actual panel pitch minus .04 m), leaving at least .04 m between adjacent equal-width plates. Font width fit uses that actual width minus .02 m inset. At the observed .418 m pitch, plates are .378 m wide with .04 m clearance and .358 m available text width.

Pilot/controller geometry, device positions, bindings, text, colors and label centres are unchanged. Changed only the leased chain legend plate width/font-fit expressions in ControlInstallations.cs. Scoped whitespace check passed. No build, engine, UI or input used. V10 before and V11 after source/hash records are in SDD before/after chain-plate-packing directories. Dot final source review and root's fresh exact-binary native/render acceptance remain pending.
