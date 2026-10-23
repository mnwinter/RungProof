# Demo3 ENABLE loss and physical route evidence

Independent original native pixel review, no UI/state queries. PID12768/native1021×660, Demo3 Height Sorting Pusher Cell/local runtime/PLC disconnected. Rehashed all36 route originals plus eight input/hold/Health/Signals originals:44 match native-ui-actions.jsonl records. [Derived route sheet](demo3-independent-route-sheet.jpg) crops originals(90,210)..(1000,470), resized to400×160, with frame indices; original pixels remain authoritative. Geometry interpretation follows [source grounding](demo3-route-geometry-grounding.md).

## ENABLE interruption

[Moving a](demo3-r1-moving-before-disable-a.png),5603, and [b](demo3-r1-moving-before-disable-b.png),5628, visibly show the carton moving relative to the fixed portal/main belt, green pilotlit. [Off held](demo3-r1-enable-off-physical-held.png),5716, has pilotdark and fixed carton pose; [Off Signals](demo3-r1-enable-off-signals-top.png),8000, directly shows conveyor_runningFalse/pusher_extendFalse/pusher_retractedTrue, stopped_loaded/count0. [Restored physical held](demo3-r1-enable-restored-physical-held.png),10331, remains same carton pose/pilotdark despite continuing scans. [Older accepted Health](demo3-r1-health-older-accepted-inputs.png) directly records ENABLEFalse23:55:26 and ENABLETrue23:57:07, followed by fresh START record above. This distinguishes accepted restoration from a guessed click. Restoration alone remains idle; physical freshSTART then begins the36-frame route window.

## Physical routes in the saved window

- [00](demo3-r1-route-observation-00.png),11270, displays HEIGHT420/DONE0 and short carton at station. [01](demo3-r1-route-observation-01.png)→[02](demo3-r1-route-observation-02.png),11323, show it proceeding along main conveyor toward right main exit with pusher retracted. By04 DONE1/carton cleared. This is actual short-main movement, not empty-belt texture.
- Tall carton approaches at08,11469; [10](demo3-r1-route-observation-10.png),11517, directly displays HEIGHT820/DONE1, actual pusher rod extended and carton displaced toward side receiver. [12](demo3-r1-route-observation-12.png),11565, shows DONE2 and tall carton supported on near side receiver; [15](demo3-r1-route-observation-15.png),11640, shows the same tall carton remaining there after rod/plate retract. It is not dragged back with pusher. This satisfies bounded tall-side placement; it does not prove far-end Z4.8 travel.
- [16](demo3-r1-route-observation-16.png) begins recycle; [18](demo3-r1-route-observation-18.png),11715, directly displays HEIGHT600/DONE2. Frames19→20 show main travel, and21 has DONE3. This is additional observed inclusive-boundary main behavior, not a newly imposed gate.

The model uses one recycled carton mesh, not three permanent independent cartons. Disappearance/reappearance between completed route and next load is therefore not a dropped parcel inference. No far side-exit sensor pulse or continuous support-at-every-instant claim is made.

[Final top Signals](demo3-r1-route-final-signals-top.png),16181, shows parts_completed22/conveyor_runningTrue/no overload/trip/pusher fault indicated. [Final bottom](demo3-r1-route-final-signals-bottom.png),16235, confirms system_enableTrue/pusher_retractedTrue. This authored controller continuously cycles; do not require automatic shutdown at3 or classify ongoing parts22+ as a defect. Root later AppStopped for checkpoint; this is not a natural completion endpoint.

## Scoped result

Native evidence supports Q11's exact bounded moving-ENABLEloss/held/restoration-alone-idle/freshSTART recovery plus short420-main and tall820-side physical routes. No new app failure demonstrated. This does not claim all curriculum recovery, every fault/height combination, far-side takeaway completion or safety-rated stopping. Coverage reconciliation may close Q11 for this supplied scope; no broader overall audit completion is inferred.
