# Barrel four-socket repair proposal V17 (pending Dot approval)

Actual native witness v17-barrel.interfaces.png.interfaces.json SHA1F3E2F4371657CF00CFFBEEBEAA35ECCD903FFDCDFCA8C4D70DE42A0059AD97F confirms all4coaxial/planar retainedring interfaces have no positive annular overlap: valvepairs-.02299846m, meterpairs-.027997017m. Center offsets<2um, plane gaps0. This is measured hollow-interface discontinuity, not projected overlap. Current BarrelFill.cs SHA9EAEB63DD4FC9D8F30BB78287C4575717ABF89ED783072216BA3A5A3B4564FB1. No implementation authorized by this proposal.

## Preferred minimal source scope

Only SceneComposer.BarrelFill.cs, four additive passive hollow reducing sockets. Retain every original bore/tube/ring/node name/mesh and equipment transform; no sourceJSON, point, value, model, flow rate, valve state, reset or interaction changes. Use existing AddSumpRoute straight cylindrical construction with48sides and real nonzero wall/thickness. This gives credible bored adapters rather than a flat disk spanning a hole.

| New body root/name | Local center | Outer / inner radius | Axial length | World axial interval |
|---|---|---|---|---|
| valve_2/VALVE_inlet_reducing_socket | (0,2.45,-.20) | .083 / .038m | .08m | Z[-1.74,-1.66] |
| valve_2/VALVE_outlet_reducing_socket | (0,2.45,+.20) | .083 / .038m | .08m | Z[-1.34,-1.26] |
| training_accessory_6/METER_inlet_reducing_socket | (0,2.45,-.15) | .088 / .038m | .08m | Z[-1.09,-1.01] |
| training_accessory_6/METER_outlet_reducing_socket | (0,2.45,+.15) | .088 / .038m | .08m | Z[-.79,-.71] |

Call AddSumpRoute with centers(0,2.45,centerZ-.04)→(0,2.45,centerZ+.04), outerradius above and wall=outer-.038. Retained .045pipe wall annulus[.038,.045] and largerbody annulus[.068,.075]or[.073,.080] enter the bored socket material over40mm each side of datum. The inner .038m aperture remains through-open, not capped. Outer collar stands8mm proud of existing largerbody, avoiding coincident equal-radius cylinders on overlapping axial stretches. New annular start/end caps lie40mm away from old ring datum, avoiding coplanar face overlays. Existing disconnected end rings become internal to an actual thick adapter; they are not hidden/deleted or falsely declared to mate directly.

Existing source part clearances support feasibility: valveactuator minY2.55 vsnewoutertop2.533 leaves17mm; valve saddleZ±.06 vs sockets nearestlocalZ±.16 leaves100mm. Meterdisplay minY2.54 vsnewoutertop2.538 leaves2mm vertically but socketZ intervals[-.19,-.11]/[.11,.19] are65mm separate from displayZ[-.045,.045], so no 3D overlap. Meterstem atZ±.025 is85mm clear of sockets. These are source coordinates, to be independently checked against actual world meshes; they do not justify projected-overlap claims or arbitrary tolerances.

## Native verification changes, separate from product source

Root owns test/capture integration. Preserve original Main.BarrelFillAudit center-distance/datums checks and originalbore/path provenance; describe them honestly as alignment evidence. Keep original4ring witness values (negative overlap) after repair: additive sockets bridge them, so changing those values toPASS would be false evidence.

Add actual socket-specific verification:

1. Unique visible4bodies and8start/end ringmeshes under exactretained roots. Measure actual vertices/transforms, not only names/metadata. Verify original4ring vertices/transforms/radii unchanged from witness.
2. Actualsocket caps planar/coaxial with retained datums; measuredcap centers straddle each datum by40mm, radiusranges inner.038 and outer.083/.088 within1mm. Straight bodyvertices span those caps; allsameaxis/radii, nondegenerate triangles. Check no triangles cover center/open bore; inspect actualmininner radius>=.038 minus float tolerance.
3. Actualsocket hollow wall at joint spans both retained annular bands, with positive radial overlap and40mm axial overlap each side. Test both narrower pipe and largerbody. Do not reuse the old coplanar ringpair test for noncoplanar socketcaps. Interval/coaxial native proof is bounded annulargeometry, not fluid pressure or commissioning acceptance.
4. Actualsocket world AABBs against all retained NON-mating field/architecture parts; intentionally mating tube/body overlap allowed only via exactlistedIDs/nodes. No generic ignore-all-pipes. All original grounded supports, barrel travel/rim and readout paths retained. Repeat existing native barrel flow/model/repeat/reset verification unchanged.
5. Actual oblique detail frame of source→valve→meter→nozzle shows four socket transitions and open-looking construction without display masking; retain readable source/meter view and fullfield context separately. Nativevertex proof resolves interfaces even where projection overlaps. No sourcecamera changes.

## Finite decision

Approve/revise4socket dimensions/names and exactsingleproductfile scope; implement once approved, then fresh nativewitness+socket checks+actualdetail. This proposal adds no pressure rating, physicalsafety claim, extra lesson behavior or material75 rollout.
