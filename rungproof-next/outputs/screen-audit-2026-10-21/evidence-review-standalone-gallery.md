# Equipment gallery native review

Selected actual pairs are combined in `standalone-gallery-motion-review.jpg`; mapped actuator close crops in `standalone-gallery-four-mapped-on-review.jpg` (the second labeled motor crop is the separate pump, not the mapped motor; do not use that row for motor credit). Actual mapped motor is the small top/rear blue assembly at screen805..870/320..380.

ON/selectorON/repeat native pairs show changed fan blade and mapped rear motor rotor positions. Their local actuator pixel regions are unchanged between OFF and StopAll pairs. This supports bounded actual motor/fan presentation and stopping independently of gallery_animation values. Actual native header reads LOCAL RUNTIME RUNNING / LADDER NOT LOADED in ON frame; an assumption that it remains STOPPED is corrected.

CNC/empty-conveyor precise motion and stop remain unproven in these wide views. Pixel changes in their regions can include shadows from other animated objects. Drill strokes persist during OFF/StopAll, but gallery_drill is not one of the four gallery_animation bindings, so that is not classified as a mapped control defect. Root's exact Signals cases establish switch/selector flags, StopAll/emergency and Reset values separately. No universal four-asset motion PASS or defect is inferred from those flags.
