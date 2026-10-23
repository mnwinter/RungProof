# Demo5 ENABLE loss during actual gantry motion

Independent native pixels/action review; no input or state queries. PID12768/native1021×660; actual Demo5 Integrated Palletizer/local runtime/PLC disconnected. Eleven cited original SHA256 values match native-ui-actions.jsonl. Setup/default selector clarification remains in demo5-reset-permissive-clarification.md; accidental initial toggling of defaultTrue selectors is not classified as Reset failure.

[Before a](demo5-r1-moving-before-disable-a.png),5033, is actually home/XYZ0/cellinactive, despite its filename. [Before b](demo5-r1-moving-before-disable-b.png),5046, after accepted physicalStart, shows Y−780/YnegativeTrue/cell_activeTrue/homeFalse/cartonAttachedFalse and actual tool lower than home: this qualifies an unloaded moving axis.

[ENABLE OFF settled](demo5-r1-enable-off-settled.png),5060, and [held](demo5-r1-enable-off-held.png),5113, show Y−1200, all six axis directionsFalse, cell_activeFalse, same actual tool pose while scans advance. The visible accepted-action footer explicitly says system_enable=FALSE. These scrolled rows do not themselves show that input BOOL; do not misattribute a stale first-scroll frame. [Health settled](demo5-r1-health-enable-history-settled.png),6933, independently proves Start23:49:06→ENABLEFalse23:49:07→ENABLETrue23:49:32 as three distinct accepted actions.

[Restored held idle](demo5-r1-enable-restored-held-idle.png),5933, directly shows system_enableTrue/palletvalidTrue/step10/Y−1200, axis directionsFalse. [Restored axis hold](demo5-r1-enable-restored-axis-hold.png),5960, confirms XYZ0/−1200/0/cellinactive with same physical pose. Thus restoration alone does not restart motion.

[Fresh physicalStart](demo5-r1-fresh-start-resumes.png),7580, accepted Start/Resume: Y−2040, at_pickupTrue, actual tool lower. The sample already has axisOff at pickup, so its result is resumed travel reaching pickup, not necessarily an actively moving command in that exact frame. [Later travel](demo5-r1-resumed-layer-observe-2.png),8026, shows X5040/XnegativeTrue, placed1, cellactive and actual first carton supported on pallet, proving progression beyond the interruption.

[Endpoint top](demo5-r1-resumed-layer-endpoint-top.png),10900, shows homeTrue/XYZ0/all axis directionsFalse/cellinactive with actual four-carton layer supported on pallet. [Endpoint bottom](demo5-r1-resumed-layer-endpoint-bottom.png),10960, shows placed_cartons4/layer_completeTrue/step100/systemenableTrue/palletvalidTrue/tripFalse/palletizer_faultFalse. Final home state and supported layer agree; no exact placement precision is claimed.

**Q10 closes for qualified unloaded gantry travel→ENABLEloss/held→restoration-alone idle→freshStart recovery to supported four-carton layer/home.** No fault demonstrated. No carrying-load enable-loss policy, every-phase recovery, universal fault clearance, or safety-rated performance is inferred. Camera remains unchanged through behavioral comparisons.
