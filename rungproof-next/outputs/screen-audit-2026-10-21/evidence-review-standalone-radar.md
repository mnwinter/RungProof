# Standalone radar tank: bounded native review

Actual selected pixel crops in `standalone-radar-review-compact.jpg` show external gauge movement matching numeric level, inverse distance and idealized current: baseline35/3.10036/9.6; filling72.8/1.70536/15.648; draining57.46/2.27148/13.1936; off51.79/2.48073/12.2864. These current samples satisfy 4+0.16*level. Fill has inletTrue/drainFalse; manual drain reverses those commands while cycle remainsTrue; cycleOff clears both.

App Stop/held retains71.95/1.73673/15.512 with both commandsFalse; Run resumes fill and reaches99.53/0.718893/19.9248. Reset returns35/9.6, both requestsFalse/all actuatorsFalse; fresh repeat reaches64.68/14.348 with fillTrue. Gauge visible; internal surface/beam hidden by opaque tank, so no direct internal surface proof.

Inspection route remains a documented UI-access concern: scene help explicitly advertises an inspection view that ghosts shell/roof; physical shell/transmitter attempts leave tank_inspection_viewFalse and opaque shell. No explicit inspection control is exposed in these tested views, corroborated by root's static mapping inspection. Do not classify arbitrary shell clicks alone as an action failure. Record as inaccessible advertised-view candidate pending a defined exposed interaction contract; separate from the fixed11.7 common-stop defect.

No claims of automatic80/20 threshold cycling from this custom fixture, exact stop latency, hardware echo/current accuracy or hidden geometry. No reviewer UI input.
