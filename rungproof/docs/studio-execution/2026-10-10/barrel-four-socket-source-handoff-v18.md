# Four hollow barrel sockets: V18 source handoff

Dot approved final inner radius.039m (supersedes proposal.038): a1mm enlarged internal step avoids coincident inner-cylinder surfaces and preserves original76mm pipe lumen. Outer radii.083 valve/.088 meter, axiallength.08 and allposes unchanged from approved plan. This is presentation fitting geometry, not physical fitting/safety/pressure certification.

## Frozen changes

- ONLY productfile SceneComposer.BarrelFill.cs: finalB55964D97895E009B4C844BEDB13E9C0B538CC3FAA0B22FDAAA812F35470E1B1, before9EAEB63DD4FC9D8F30BB78287C4575717ABF89ED783072216BA3A5A3B4564FB1.
- Authorized diagnostic Main.BarrelInterfaceWitness.cs: finalCF8477853BD793B2F606771CADF846AE842FBBC1067D8073BDBDF7AFB12AEA84, before45615F775CCA7B402D661D32C893E6CDD45136B3F61DDA627F63CF22B7F37674.
- Exactcopies/hashes saved before edits at .superpowers/sdd/repeat-cycle-execution-tasks/before/barrel-four-sockets-v18. Both narrow no-index whitespacechecks exit0. Root notified freeze before docs. No build/engine/input.

Product adds only VALVE_inlet_reducing_socket/VALVE_outlet_reducing_socket atlocalZ±.20 and METER_inlet_reducing_socket/METER_outlet_reducing_socket at±.15,allY2.45. Each AddSumpRoute creates hollow body plus start/end annular rings atdatum±.04. Every original source/bore/ring/pointer/display/stream/groundsupport unchanged; equipmentJSON, roottransforms, flowmodel, rates/values, reset/click behavior untouched. Existing direct4ringpair results should remainnegative (honest provenance), because sockets bridge their volumes rather than making their endrings overlap.

## Native diagnostic extension

Original4interface results remain unchanged in output. Additional sockets array contains4actual body/ring vertex/triangle measurements and8exact socket-to-mate annularcontact results:

- start/endcapactualworldcenters/normals/radii and actualbodyaxialrange/length;
- actualbodyinner/outervertexradius;
- minimumTriangleSurfaceDistanceToBoreAxis from body AND bothannularcap triangles. Projects triangles to radial plane, catches origin-containing face/disk and measures allprojectededges. Thus rim-only vertices cannot falselyprove an open bore. Expected polygonalclearance~.039*cos(pi/48)=.0389165m, above original.038m radius;
- each socket actual radialband overlap with each exact retained annulus and positive axialbody overlap, datuminside span, coaxial normal/lateralalignment. Expected radialoverlap~6mm with smallpipe and7mm with largebody, axial40mm eachside;
- socket actualworldAABB and every positiveXYZ (>10um) overlap against visible plantmeshes. Exact allowlist ONLY ownbody+2caps and two explicitly matingbody/jointringpairs. Other3sockets are NOT excluded. Emits offendingpaths/overlapdimensions and noPositiveNonMatingPlantOverlap; observedfailure neverthrown/falselylabeledPASS. Missing/invalid meshes remain invalid measurement exceptions.

Mesh.GetFaces actualAPI verified against local GodotSharp4.7.2XML and existing repo usage. No custom invented triangleAPI.

## Required root evidence

Build frozen combined candidate. Read actual.interfaces.json:4oldnegativeobservationsretained,4hollowsockets,8positivecontacts,4throughOpenAtOriginalPipeRadius true,4noPositiveNonMatingPlantOverlap true. Inspect actualdimensions and gaps instead of relying only onbooleans. Run unchanged originalbarrel native behavior/model/phase/reset and allrequiredmount/control/environmentgates. Actualside/detail captures must show credible transitions with retained readouts/supports; individualroot/Dotreview required. Diagnosticplant-onlyneighborchecks omit decorativeenvironment; root checksbounds against existingprotectedvolume/environmentseparately. No material75 rollout or wholeproductcompletion follows.
