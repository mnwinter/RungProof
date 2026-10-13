# Barrel valve actuator stem: V19 source handoff

Dot/root-approved stem implemented ONLY in SceneComposer.BarrelFill.cs: VALVE_actuator_stem, solid vertical cylinder radius.018m,center(0,2.620,0),height.200. Actual nominal bottom2.520/top2.720. Original bore, actuator/pointer roots/poses and all4sockets unchanged. No flow/model/state/JSON changes.

## Freeze provenance

BarrelFill.cs finalD19BAD03037C5D337A37C45138CCB4CED4FADD76DC9322172601F25FA401CFE2 (beforeB55964D97895E009B4C844BEDB13E9C0B538CC3FAA0B22FDAAA812F35470E1B1). Authorized diagnostic Main.BarrelInterfaceWitness.cs finalE1A2AA369DF47B15841C27B4A2A390AC09717F76803EE7976CE4537FDEB92206 (beforeCF8477853BD793B2F606771CADF846AE842FBBC1067D8073BDBDF7AFB12AEA84). Exactbeforecopies/hashes retained SDD before/barrel-valve-stem-v19. Scoped whitespacechecks exit0. Root reports current build passed and both pointer-pose captures running. No build/engine/input from author; actual native witness values/render inspection pending at this handoff.

## Bounded actual geometry witness

Existing4negativeoriginalring observations,4socket openbore/8contact proofs and exactnonmatingneighborintersections remain intact. New valveStem output records actualPointerRotationY and actual fill_valve_open point. No pose mutation or tick from witness.

Curved bearing uses actual retained48-sided annularring outervertices, ordered into X/Y polygon. Intersects actual top polygon segments at left/right stem footprint extremes; convex upperprofile minimum across interval lies at an endpoint. Stem extent uses actual known-cylinder worldmeshAABB, conservatively spanning its complete footprint. Record shell top endpoints/minimum, minimum bearing penetration and actual retainedinnervertexmaximumY. Requirehorizontal retainedboreaxis, stemfullZfootprint insideboreaxialspan, positive curvedbearing and stemminY strictly above innerboretop. This is curved-wall geometry, not merebodyAABB overlap.

Actuator records positiveactualXYZ overlap. Pointer proof reads actual BoxMeshlocalbounds and actualglobalbasis lengths/orthogonality/verticalY, computes minhalfwidth=.020m inscribedcircle independent ofyaw, actualaxiscenteroffset, stemradius and verticaloverlap. Requireinscribedradius>stemradius+offset and positiveYoverlap. This proves centeredbearing at every Yrotation without rotating any runtimeobject; bothactualclosed/open nativeframes stillverify sampledpresentations.

Expected nominal geometry: stemmin2.520 exceedsinnerboremax2.518 by2mm; curvedouterwall minimum overradius.018 footprint approximately2.52265–2.52282, so at least~2.65mm bearing. ActuatorY2.55–2.69 fully intersectsstem. Pointerbottom2.705/top2.735 overlapsstemtop2.720 by15mm; .020m inscribedfootprint vs.018stem gives2mm radialmargin. Actual witness must establish these values rather than treating nominaldimensions asPASS.

## Acceptance boundaries

Root owns current build and bothposes field-joints captures, including exactcapturedpoint/rotation identity. Read rawcurvedbearing/lumen/boxproof data and individually inspect actualPNGsupport/labelclearance. Missing/invalidvertices throw; observedcontact failures serializedhonestly. No new all77 or broadsource scope added for thisstem. Physicalvalveconstruction, pressure/fluidqualification, continuouscollision and material75 rollout remain outside thisboundedpresentationrepair.
