# Three outdoor/exhibit locations: source-grounded plan

Planning only during current325-file capture freeze. Do not modify existing sources or build/capture until root releases freeze. Proposed implementation file: DemoEnvironmentPresentation.ProcessLocations.cs, private bool TryBuildProcessLocation(d,b,sx,sz,architecture); exact three variants only, false for others.

## Coordinate and protection contract

Keep WORLD axes. Convert parent-coordinate OperatorAccessBounds to environment-local by subtracting environment Position. O is passed conservative process/motion envelope; A is actual standing reserve; B=unionO+A for positioning context. Mirrored canonical u=sx*X,v=sz*Z point outward toward the selected rear corner. Compute canonical maximum edges from actual B, not JSON roots or historical cabinet coordinates. All upright context beyond B by minimum.65m. No yaw rotation, new colliders, bindings, runtime owners or decorative fake I/O. Ground/apron stays below authored datum and retains source roads/pits/water.

A continuous1.2m peripheral approach reserve is a proposed swept rectangle outside O connecting open foreground to A. It can enter A; never treat A as an obstacle. Inspect full route width and actual mesh/ray geometry before acceptance. These are static model reserves, not physical circulation certification.

## EV charging forecourt — charging-open-service-island

Source: SceneSimulationRuntime.Ev.cs/scene JSON. Bay assemblies atX±3 contain named EV_INLET; actual CONNECTOR_nose intersection and positional tolerance produce feedback. EV vehicles toggle visible with occupancy; there is no modeled translation to fabricate. Keep chargers, field energy meters/readers, pulse LEDs, connectors and lenses untouched. Preserve actual updated cabinet location/standing reserve discovered natively.

Form: a finished open charging forecourt, no warehouse/loading portal or hut. One rear service canopy spans the full two-bay width W=max(8.8,B.Size.X+1.8) and projects1.6m OUTWARD beyond rear edge vR=B.maxV+.8. Four ground columns at u=±W/2 around B.centerU and v=vR+.15/vR+1.45 support rear/outer headers and a single quiet metal roof. Roof/columns are outside all bay/connector/meter volumes; open sky remains over authored vehicles.

Footprint: continuous ground from B.minU-.9 toB.maxU+.9, B.minV-1.6 toB.maxV+2.6. Rear canopy foundation is continuous with this forecourt. Camera/front approach remains open; low rear service curb is only along outer canopy edge, with side ends grounded and no curb across route. No personnel-door metadata: canopy is open, not a claimed enclosed building.

Reserve:1.2m-wide strip along camera-front edge of O, joined to native operator A through its open front. Bay service access at meters/readers must remain visibly accessible; native static head/ray checks required. No added cable routing presented as functional.

Large faces: charcoal quiet asphalt-like concrete forecourt, pale cool canopy underside, restrained green-gray painted steel. Broad sky daylight and two supported canopy diffusers; no painted stripes/grids, semantic lenses unchanged. Distinct from parking garage's opaque two-portal facade and roadwork's exposed road/gravel trench.

## Mobile traffic roadworks — temporary-road-cutback

Source: MobileTrafficPlantModel.cs + SceneSimulationRuntime.MobileTraffic.cs. Vehicle centersX[-3.6,3.6],laneZ±.9; declared conservative half-length1/half-width.55 produces X[-4.6,4.6],Z[-1.45,1.45] horizontal travel. Actual cart bases and six lenses stay at authored field locations. Retain accepted-clock movement, request actions, Stop/Reset, conflict/occupancy behavior.

Form: complete temporary roadside work segment with contiguous service verge and an OPEN shallow excavation illustration OUTSIDE B; no tiny generic hut or room. Road axis remains authored WORLD X. Context must not rotate road to camera. Ground-level road continuation follows actual source road if present; if no authored road mesh exists, add only passive below-datum quiet road apron, preserving vehicle/support datum.

Work patch: rectangular rough gravel bed3.6m alongX×1.8m alongZ centered at(B.centerX, rearZ+sz*1.45), with rearZ selected fromactualB+ .8. Use a dark inset well/foundation atground level, low ground-bearing earth curb along threeouteredges; camera-side work-edge remains open. No decorative functional excavation plant, barriers between signal and vehicle sightlines, or fake radio/control wiring.

A connected angled-looking footprint is prohibited: preserve exactX/Z aligned verge. Low rear verge end-return1.8m meets work-bed rear curb; one grounded blank portable instruction board on outercorner uses static “TWO-DIRECTION TRAINING / QUEUE A / QUEUE B”, not a traffic approval sign. No service building, portal metadata or roof. Gravel and dark road material define actual large-area site contrast.

Reserve:1.2m dry approach strip alongcamera-front perimeter outsideO to actualA; no segment through X[-4.6,4.6],Z[-1.45,1.45] or signalcart/requests. Workpatchground staysoutsideO even if hidden vehicle meshes shrink startup bounds. Source conservative travel must be included explicitly in B beforebuilder acceptance.

Large faces: dark quiet asphalt, irregular aggregate/gravel ochre, restrained concrete curb. Open sky diffuse light; no repeated-line/striped backgrounds and no new road markings. Distinct from EV supported-canopy electrical forecourt and gallery open structural pavilion.

## Equipment gallery — open-exhibit-pavilion

Source: equipment-gallery.scene.json.21 reusable models in five rows, including active motor/conveyor/fan, lift travel2.2m atscale.72, drilltravel1.25m atscale.75, robot.8scale, shutter.72scale and rotarytable. These are independent displayed models, not one fabricated factory process. Discrete/analog level devices intentionally ondisplay stands. Retain every root, action, motion and field stand.

Form: complete OPEN exhibition pavilion. One connected L-shaped structural frame runs actual rear and side boundary outsideB+.85m. Rear perimeterbeam lengthW=max(12,B.Size.X+2.4) andsidebeamD=max(12,B.Size.Z+2.4) meetatsharedcorner with a single ground-bearing post. Additional posts atouterends support only1.25m-deep outward canopy ribbons. Roofheight=max(4.3,B.End.Y+.8) keeps all lift/robot/shuttermotion below but never puts roof across B. No opaque walls, hut, warehouse loadingdoor or fake plumbing.

Foundation: continuous quiet exhibition slab coveringB plus2.0mouterapron; expand fullyundercanopy ribbons/groundposts andclosedcorner. The exposedfront/two camera-near sides remainopen. Actual entrance is a3m clear OPEN gap atcamera-front corner, with no false personnelportal node on nonexistent wall.

Reserve:1.2m peripheral visitors/service strip outsideO, connecting openentrance to perimeterdisplayinformation boards; it doesnotclaim clearance betweenall authoredexhibitrows. One static taxonomy board mounted onrearframe andone onsideframe with actualcategories“DRIVES / MATERIAL HANDLING / SENSING” and“PROCESS / MOTION / OPERATOR DEVICES”; labels areexhibitcontext, not operationalI/O.

Large faces: warmneutral continuous polished-concrete-like floor, offwhite open canopy, darkbronze/charcoal structure. Broad diffuse skylight throughopenpavilion, supported edgefixture geometry. No blue industrial room, repeatinggrids, decorative extra equipment or raisedfloor over modelbases. Distinct from enclosed box-volume metrology room byactualabsenceofopaqueenvelope, roofperimeter topology andfullcatalogcourt size.

## Required gate

Read current native B/A, not historical dimensions ledger; check upright/footer/aperture separation andfullapproach sweep. Run existing all77 control/environment checks plus dedicated EV/mobile behavior audits. Captureactual full/split each and compare nearestsiblings. No geometry/source signature substitutes for visible purpose, readable controls or finished location quality.

