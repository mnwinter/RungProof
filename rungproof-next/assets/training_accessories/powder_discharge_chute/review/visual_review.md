# Powder discharge chute geometry review

Status: candidate; native static placement inspected in the powder-mixer scene.

The previous package was a copy of a roller-shutter door. Its recognition result
recognized a door, so it did not establish chute identity. Those files are
preserved under `superseded-roller-shutter/` and do not apply to the new model.

The replacement is original Blender geometry: an open inclined channel, two
sidewalls, raised lips, two cross-supports, four floor-supported legs and feet,
and frame braces. It has no curtain, door guides, motor or animated node contract.
The source and four Blender camera images are reproducible with
`tools/modeling/build_powder_discharge_chute.py` and Blender 5.2.2 LTS.

No OEM dimensional equivalence, bulk-flow behavior, fabrication suitability or
independent recognition is claimed. Catalog approval flags and the inherited
door reference are removed. On 2026-10-04 the replacement was inspected in the
native Windows powder-mixer scene from front-right, front-left, rear-left,
rear-right and overhead, including close views of the channel and supports.
The cross-supports stay below the inclined channel, the four feet rest on the
floor, and neighboring equipment clears the chute. Rear tank occlusion was
resolved with front/overhead views. The Blender builder also checks evaluated
cross-support corners against the channel plane. These bounded checks do not
grant independent recognition, production approval or material-flow acceptance.
