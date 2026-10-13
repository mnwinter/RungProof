# Authorized Lab 10.6 sensing-contract exception

The resumed request explicitly adds real plant-generated feedback. This is a documented exception to the earlier authored-JSON freeze, limited to `lab-10-06-ten-motor-array-startup.scene.json`. Exact pre-change bytes are retained in `lab-10-06-before-zone-sensors.scene.json`.

Thirty inserted JSON lines declare ten existing original through-beam photoeye assets (`zone_sensor_i`), ten PC-owned BOOL input points (`zone_occupied_i`), and their photoeye visual bindings. No existing command, aggregate, action, ready/alarm point or controller program changes. The point name is a **sensing-line occupancy indication**, not whole-zone presence, motor feedback, permissive or fault diagnostic.

Each physical sensor is at world X=-6.82+1.4×i, adjustable stand span1.8m, optical height1.46m. Runtime measures the actual KIN beam mesh union's centroid plane relative to the process root, verifies carton-height/corridor alignment, then tests the finite carton interval with a 1e-9m boundary epsilon. Material/shaft/drum/UV integration precedes sensing projection; the next accepted controller scan samples that input image. Sensor visualization updates through the existing photoeye binding once, without recursive projection. Static sensor members must not intersect any measured drive-case or mounting-pad AABB.

Reset restores the .60m carton at center.30m: zone0's line at.18m is genuinely occupied; the other nine lines are clear. A carton can remain on a zone while its single sensing line is clear. No readiness, alarm, command or evaluator result is substituted for geometry.

The focused native verifier adds actual bound sensing input tags to the existing explicit QA documents, records preceding sampled versus subsequent projected images on transitions, distinguishes all-on travel from valid partial-code stall, and checks Reset feedback. Shell draft lifecycle checks run separately before those QA documents; they must not be conflated with authored lesson-program approval. Fresh coordinated build/native/render verification remains required for this changed contract.
