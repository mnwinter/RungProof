# Chicken scale screen readout handoff

Only product file changed: src/Scenes/SceneSimulationRuntime.ChickenLabel.cs. SHA256 **2681DC216077B2F3B680A0F6180E6DCAE85464AADC86AA5D91145D422B6B6F2B**. Before copy: chicken-label-before-screen-readout-v17.cs beside this report. No build/engine/UI by this owner.

Existing training_accessory_5/MeasuredWeight path and publisher strings are retained. Reset locates exact visible WEIGH_readout_screen, measures its mesh AABB, composes its ancestor transforms into weigh-root space, and places the existing label centered1.5mm outward from local+Z screen face. Label is non-billboard, centered, white, outline0, font36. It follows existing template/world transformations without changing any field root, screen/head/support geometry or controller. Imported WEIGH_readout_static_text remains hidden. No competing floating value remains.

Font metrics fit LOAD PRODUCT, WEIGHING..., MEASURED1.237kg and the actual current unchanged F3 string into screen width/height minus20mm total inset, capped at pixelSize.0018. The largest existing state determines scale, so normal state changes do not jump size. Measured ink width/height and exact screen/sourcePoint provenance metadata are available for native checks. Arbitrarily large finite model masses remain displayable through fit; pixel readability for such theoretical values is not claimed.

Publisher, PC weight_kg, stability delay, tray/paper/application timing, reset/job semantics, FORMAT_TEXT/STRING output and plant model are unchanged. No new numeric binding or evaluator writes.

Root focused acceptance: compile; Reset LOAD PRODUCT with hidden static text; accepted loading WEIGHING...; after actual0.5s stability MEASURED1.237kg matching PC weight_kg; changed mass case matching unchanged F3 formatting; text AABB inside exact screen in local space, center/normal/front offset, no billboard, retained child path; template translation/rotation follow; actual operator/detail views proving text readable on physical screen with no old floating measurement. Existing typed learner regression should remain unchanged. Handle OPEN; material75 HELD.
