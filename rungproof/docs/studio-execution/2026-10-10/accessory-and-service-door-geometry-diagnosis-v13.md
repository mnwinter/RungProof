# Accessory/service-door actual geometry diagnosis V13

Read-only review of root-produced v13-c geometry witnesses. No engine/build/input by this reviewer. Historical accessory-geometry-v13-c.json FAIL preserved; subsequent accessory-geometry-v13-c-floor-qualified.json FAIL preserved.

Ground failure was exact inventory mismatch: RemainingProductionNine.cs:30–31 hides CatalogContinuousGround and creates visible WholeLocationFoundation. Both dumps contain it, topY=-.004999995m. Grounded plinth/feet bottomY0: measured5mm gap, within unchanged12mm AABB tolerance and actual plan bearing. Checker now recognizes that exact verified namespace, still requires actual mesh bounds/contact.

Floor-qualified witness confirms powder complete chain PASS: both beam-pad-worktop contacts, four leg/feet contacts, actual ground bearing and no mesh interference with other equipment. Nearest authored neighbor is valve_4/FIELD_JUNCTION_box, gap1.121766925m. This is bounded world AABB evidence, not calibrated load-path or triangle-level bearing.

Relay had a genuine independent containment failure: RELAY_BASE minZ1.356134653; DIN_RAIL minZ1.496134758; backplate frontZ1.496134758. The retained base extends140mm behind rail, crossing plate/escaping open bay. Full base X[4.063999176,4.403999329], rail X[3.683999300,4.784000], rail Y[.832500041,.887499988]. Approved two spacers have X centers3.763999300 and4.704000, width.08m, so base X separation approximately.26m on each side. Height.04m fits actual rail height. Rear depth measured .140000105m. Full backplate moves to complete retained minimumZ; no retained root/node transform changes in repair. Both new spacer chains and full XYZ containment must be checked from a fresh witness before claiming repaired.

New frozen source SceneComposer.AccessorySupports.cs SHA256 FCDE8CB2DE2D85B899652E8F03265A3B95687D46EDCD9D0BD48EC113E8FBBDC1. Checker SHA25620E3027862A5DB49F140FC27248F303900E7B422E7E51343564562B364375143. Metadata bearingMesh now RelayBay_RailSpacer_Left; QA must independently check Right too. Old contract report describes historical direct rail plate contact and is superseded by this measured repair addendum.

## Service door measured bounds

Actual v13-c-lab-2-19-service-door.geometry.json cabinet body X[4.659999847,6.479999542], Y[.140000105,2.259999752], Z[-.552500010,-.092499971]. Door X[4.709999561,6.429999828], Y[.210000038,2.189999819], Z[-.092500001,-.057500005]. Right shutter guide X[2.930000067,3.169999838], Z[-.170000002,.170000002], leaving1.489999m horizontal clearance to cabinet body. Shutter header endsX3.174999952, also1.485m from cabinet body; header startsY3.505m, above cabinet maximum.

No positive-volume AABB intersection of cabinet body with any visible DemoEnvironmentPresentation architectural mesh in witness. Functional front pushbutton centers: OPEN X5.000,Y1.1672,Z-.0099; STOP X5.380; CLOSE X5.760 with sameY/Z. Actual lens functions remain on the front cabinet area; no shutter guide reaches their X locations. The recorded overview left-edge occlusion is therefore not established as a cabinet-surround collision by this witness.

Diagnostic operator camera source uses installation position plus front5.5m, eyeheight1.60m,targetheight1.10m; actual native pixel pick assertions and actual scene/operator/side images must determine whether intended front view is accessible/readable. Dump lacks camera projection/pick collider data and cannot certify line of sight or human ergonomics. Recommend fresh operator plus side view before any geometry move. Do not move shutter or disable occlusion checks based on overview alone. No service-door source edit performed.
