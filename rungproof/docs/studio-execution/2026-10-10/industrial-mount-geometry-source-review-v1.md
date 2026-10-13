# Industrial mount geometry source review v1

2026-10-11. Verdict: APPROVED FOR BUILD AND NATIVE VERIFICATION; no blocking source finding in the bounded geometry patch. Rendered/global acceptance remains pending.

Reviewed exact leased before/current ControlInstallationPlans and ControlInstallations differences, current StatusTowerMounts and TemplateOverrides, new MachineReadoutMounts, and mount-repairs-department-report-v1.md. No product edits or engine/build/input use occurred.

## Preservation and mounting

Exact ten light gaps and eleven plain display heads are assigned to their appropriate bench, panel or roof inventory. Selected single pilots retain LENS_single and controller identity; their lens/bezel surfaces use the existing panel conversion and 90-degree front-facing branch. Four-tier sequence tower remains in roof mounting with all tier children retained. Wastewater accepts only its exact scene, equipment ID, trainingAccessory type and alarm_beacon catalog ID; arbitrary accessories remain rejected.

Head roots stay siblings of ControlInstallation, preserving controller and dynamic readout child paths. Changes do not edit scene config/bindings, evaluator, plant timing, runtime annotations or source poses. Known cookie and EV mount namespace additions are asset-scoped rather than generic accessory removal. The Chicken helper retains the full scale/root/deck/readout housing and creates an L-shaped frame post/arm after hiding only the old independent foot/mast. Post overlaps the selected machine side-frame top by 20 mm; arm top meets the readout housing underside, with endpoints extending into the post and housing footprint. This is an illustrative continuous bearing path, not an engineering load-strength calculation or a triangular brace.

VFD retains drive, rear backboard and mount rails and moves through the measured service cabinet plan with its independent base/mast hidden. Its exterior service-panel suitability, facing/readability and potential visual obstruction remain actual render checks; source does not turn its static DEMO screen into live alarm feedback.

## Packing and transform evidence

Affected panel sizing measures retained widths/heights at the same 0.70 scale and indicator orientation used in placement. Largest measured width plus 180 mm controls column pitch; largest height plus 150 mm controls row pitch. Cabinet width/height therefore conservatively accommodate measured column/row envelopes. Pilot measurement precedes pilot mesh conversion, so it can overestimate a later smaller lens; actual native fit checks remain required. Measurement omits Label3D text bounds, camera projection and collision selection envelopes: legibility and click-target acceptance still require their separate checks.

Roof widths include measured retained head widths, per-head minimum plate width, 160 mm gaps and 200 mm outer margin. This changes shared spacing/sizing of previously mounted towers too; regression is not limited to sixteen directly repaired scenes. Native checks must verify full head envelope/coupler contact and cabinet contact, not merely head centre coordinates. Retained source orientation can affect Z envelopes; width measurement alone does not prove depth clearance.

installationEquipmentIds explicitly unions panel/bench IDs and roof IDs with deduplication. TemplateOverride uses that union and stored original transforms with one target/original delta, keeping support and sibling heads synchronized and restoring original transforms without accumulated drift. Current supported templates all create the metadata; arbitrary unknown/unsupported BYO scenes are not claimed. Chicken support is internal to its machine root, deliberately outside the cabinet assembly association.

## Remaining acceptance

Run frozen-source inventory preservation checks and corrected support-bearing QA, existing control/tower fixture checks, supported template delta/restore and joint reopen/rebuild. Verify all 77 compose and previously mounted controls retain functional paths/output projections/reset. The existing direct output fixtures establish projection only, not learner-program behavior.

Inspect actual affected normal/detail renders for enclosure/panel fit, full head envelopes, frame contact, no residual floor supports, no machine/access obstruction, readable typed/numeric strings and configured lens tiers. Source approval does not certify native collision/input target placement, readable Label3D text or all-scene visual coverage. Lab 4.3 visual acceptance and wider 75-scene materials remain HELD until their respective evidence/approval gates close.
