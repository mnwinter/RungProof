# Barrel interface diagnosis and native witness handoff V17

Read-only plant audit; only newly authorized diagnostic Main.BarrelInterfaceWitness.cs added. Frozen SHA45615F775CCA7B402D661D32C893E6CDD45136B3F61DDA627F63CF22B7F37674. Root owns call/build/capture. No plant geometry, runtime, UI or input changed.

## Concrete source evidence

SceneComposer.BarrelFill.cs:49/54/65–67 builds supply outlet radius.045, valve bore.075, meter inlet/outlet.045 and meter bore.08, all wall.007. AddSumpRoute in SumpPiping.cs:129 onward generates48-sided hollow surfaces plus annular start/end rings. JSON retained roots tank_1 atZ-2.75,valve_2 at-1.50,training_accessory_6 at-.90,training_accessory_7 at0 align these exact endpoint centers atY2.45:

| Interface | World center Z | Nominal ring radial intervals | Nominal overlap |
|---|---:|---|---:|
| SUPPLY_outlet_end_ring → VALVE_bore_start_ring | -1.70 | [.038,.045] versus[.068,.075] | -.023m |
| VALVE_bore_end_ring → METER_inlet_start_ring | -1.30 | [.068,.075] versus[.038,.045] | -.023m |
| METER_inlet_end_ring → METER_bore_start_ring | -1.05 | [.038,.045] versus[.073,.080] | -.028m |
| METER_bore_end_ring → METER_outlet_start_ring | -.75 | [.073,.080] versus[.038,.045] | -.028m |

No reducer/adapter node found in actual constructors or later source adjustments. This is a credible radial discontinuity hypothesis from explicit hollow-surface construction, not an assertion of collision from projected overlap. Native retained vertices must decide. Matching.045 meteroutlet→nozzle interface is outside latest4pair diagnostic lease.

Main.BarrelFillAudit.cs:45–47 currently tests ring AABB face CENTER distance<1mm for source/valve/meter/nozzle. Center coincidence does not test overlapping annular surfaces and omits meter's two internal bore transitions. Thus that green check cannot alone resolve these four interfaces. Existing V14b source/meter captures establish readable labels but projected display/HUD occlusion leaves interface geometry unresolved.

## Native witness contract

WriteBarrelInterfaceWitness() operates only current barrel scene with capture path. Root hook owns invocation. Exact unique visible ring meshes are resolved under retained equipment roots; all actual surface vertices transform through mesh.GlobalTransform. From symmetric triangle vertex sets it computes world centroid, nondegenerate triangle plane normal, radialmin/max, plane deviation and vertex count. Each pair reports center distance, plane gap, lateral offset,normalalignment,radialintervaloverlap,coplanar/coaxial predicate and positiveAnnularOverlapWhenCoplanar.

Coplanar/coaxial qualification requires centers/planes within1mm,normalabsolute dot>=.9999,both ring plane deviations<=1mm. Positive radial overlap threshold10um. A negative radial overlap is serialized as observed gap, never thrown or labeledPASS. Missing/nonunique/hidden/null/degenerate/nonfinite vertex data is an invalid measurement and throws. Output _capturePath+.interfaces.json. Result is a native annular interval witness, not pressure/fluid continuity, engineering rating, triangleboolean or physical qualification.

## Finite follow-up

Root run exact barrel field-detail capture and read four retained ring results. If expected negative overlap is reproduced, Dot review a separate bounded reducer/adapter geometry repair preserving plant flow model/IDs/poses; do not fix data/behavior to hide the visual gap. Before any repair measure reducer bearing interfaces and neighboring static geometry. For visual context, an oblique X-side eye (relative direction approximately(6,2,1), target around(0,2.45,-1.2)) can separate the four axial connections from frontal meter display. Fit source/valve/meter/nozzle meshes within aperture, not full conveyor. Actual mesh/ring witness is decisive if projected display hides interfaces; a camera alone cannot prove mating surfaces.

Full meter display support source has METER_stem:radius.025,height.18,centerY2.54,connecting meter body/display. Source feet/legs bear tank bottom; nozzle clamp/hanger/header/posts supply its support. These existing support chains are separate from the4ring radial mismatch question. Preserve material75 hold and no physical commissioning/whole-product completion claims.
