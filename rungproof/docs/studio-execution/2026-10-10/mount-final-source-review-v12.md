# Dot scoped source review — V12

Approved for native/render verification. No source defect found in the two changed files. Visual acceptance remains HELD until actual V12 captures are individually inspected.

Independent freeze comparison against V11 shows exactly two changed source inputs: Main.ControlInstallationCapture.cs and SceneComposer.ControlInstallations.cs. All 423 current source hashes match V12 manifest; actual DLL matches D6D35153A2116A0F7CD323B8C2CB6D627610AD0E1C3A167C9F8F669471EB158E.

Chain plate width remains min(.42, pitch-.04), retaining 40 mm separation. Height .125 and lower centre desiredBottom-.110 leave plate top 47.5 mm below pilot placement bottom. FontSize 64 and requested PixelSize .0014 permit larger ink while actual fallback-font string width/height still clamp to plateWidth-.02 and plateHeight-.02. Exact three IDs, labels, binding guards, sourcePoint metadata and controller geometry are preserved. Labels and plates remain passive support children outside head placement packing. Actual operator readability must be judged from pixels; larger font parameters alone do not close the V11 finding.

Gallery-only mount-detail direction is station.GlobalBasis*(1.8,1.2,1.0). Other scene directions and all default/user camera paths are unchanged. The same actual visible mesh bounds and aperture fitter are retained, including full bench and associated tower. Elevated right-front angle addresses the straight-front occlusion plausibly, but a fresh actual capture is necessary to prove the foreground exhibit no longer blocks support contact.

No product edits, build, engine, UI or input used by Dot. Wider 75-scene material gate remains HELD. This source approval does not assert release, physical safety, commissioning, or final visual approval.
