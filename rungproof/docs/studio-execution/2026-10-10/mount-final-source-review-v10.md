# Dot final mounting source review V10

2026-10-11. CHANGES REQUESTED for one chain feedback-legend packing defect. Camera changes approved for actual capture verification.

Reviewed current frozen ControlInstallations.cs SHA DE5DA4B91F59FC64398114E308A771790836B10815EEFF23AC20CEB9EC94E454, ControlInstallationCapture.cs SHA ED1E7E927D9FD189AD7410D00A817044A23AF5F57AEB3663EF627AD23DAE6737, and capture_demo_nonfocus.ps1 SHA 66628139BFECD5E568F08F4F85BE4933026E3A64B27615D01413FB2CC170DD50. No product edit, engine or input use.

## Required correction

P2: chain FeedbackLegendPlate has fixed width 0.42 m and is added after the retained-head packing measurement. Column pitch has a 0.38 m minimum; the four selected chain devices are one switch and three single indicators, so there is no trainingAccessory forcing the 0.70 m pitch. If these small retained heads select the minimum pitch, adjacent 0.42 m plates overlap by 0.04 m. Font fit against each plate does not prevent this physical overlap. Account for plate width plus clearance in chain pitch before cabinet width calculation, or size each plate to less than pitch with an explicit gap and fit its ink to that actual size. Verify final actual native legend envelope separation as well as render readability. Source build passing does not close this defect.

The three labels correctly guard their exact indicator bindings: switch_8 box_present, switch_9 lift_home, switch_10 destination_clear. Support-owned passive label/plate nodes leave equipment roots/lenses/controllers and tag/config/selection behavior intact. Their positive-Z placement follows the cabinet-local front and moves with the support.

## Camera approval and limits

machine-support requires exact Chicken scene and actual frame arm/post. Its WEIGH mesh inventory includes full actual scale, housing and supports; visible-mesh filtering excludes hidden legacy poles from fit. ev-readouts requires exact EV scene and retained exact meter roots. mount-detail requires actual ControlInstallation plus its associated heads. CLI validates new modes and exact scene restrictions; unknown mode throws. These modes are reachable only from the existing capture-path frame branch.

Fit projects all transformed mesh corners on camera right/up/depth axes. Height-preserving perspective uses viewport-height scaling against aperture X/Y, adds depth and 16 percent margin, and has a finite minimum distance. CaptureCurrentView invokes the existing aperture HOffset/VOffset adjustment so target framing accounts for off-centre shell aperture. The chosen view directions are nonvertical; no degenerate cross product occurs for current call sites.

These changes adjust the camera/controller only inside the diagnostic capture instance, with no persisted/saved camera mutation or normal scene default edit. Actual images must still establish support visibility, no occluder, full four-tier head framing and readable EV ink; geometry fit omits Label3D text extents and cannot prove readability by itself. Primary screen and input remain reserved.
