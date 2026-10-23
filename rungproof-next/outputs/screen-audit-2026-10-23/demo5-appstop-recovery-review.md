# Demo5 genuine application Stop / Run-alone recovery

Independent original-pixel/source review; no UI input/state queries. PID12768/client1021×660, Demo5 Integrated Palletizer/local runtime/PLC disconnected. Nine available setup/behavior capture SHA256 values rehash-match native-ui-actions.jsonl. Source and native behavior are separated below; final full-layer endpoint remains pending at this initial review.

[MachineReset](demo5-appstop-r1-machine-reset-retains-permissives.png),2720, directly shows system_enableTrue/pallet_position_validTrue, step0/placed0, no fault. This is not the earlier setup error that toggled initialTrue selectors off. [Moving a](demo5-appstop-r1-moving-a.png),4213, is actually home/axis0/cellinactive; [Moving b](demo5-appstop-r1-moving-b.png),4226, after physicalStart, directly shows Y−780/YnegativeTrue/cellactive and actual lower tool pose, qualifying unloaded gantry travel.

[AppStop settled](demo5-appstop-r1-stopped-settled.png) and [held](demo5-appstop-r1-stopped-held.png) both STOPPED4246, Y−1980/XYZ0,−1980,0, all six axis direction commandsFalse, cell_activeFalse/cartonAttachedFalse, same actual partial tool pose. No accepted-scan progress while held. This is actual applicationStop, not physical machineSTOP or ENABLEloss.

[Run-alone restores](demo5-appstop-r1-run-alone-restores.png),4261, shows RUNNING, actualpickupTrue/Y−2040. [Run-alone progress](demo5-appstop-r1-run-alone-progress.png),4321, shows carton_attachedTrue, actual carried carton/tool raised, X+ZpositiveTrue and counts160/160. [Later](demo5-appstop-r1-run-alone-layer-observe-2.png),4895, shows placed1, actual first pallet carton supported and next carton attached/carrying with X2160/Z1680. These demonstrate resumed mechanism/sequence with Run alone, not only changed header text. No fresh machineSTART follows applicationStop in the input plan.

## Source distinction

`VirtualControllerRuntime.Stop():257–281` stops scheduling/edge inputs, clears **Output-role** BOOL/numeric/text images, and resets nonretentive timers (retentive timers retained). It does not clear all Memory-role values. Authored Demo5 machine_run and sequence_step are Memory; their retention explains why a subsequent scan can resume. In contrast, ENABLEFalse is an input consumed by ordinary running LAD, which drops the seal; restoration alone then requires freshStart. These two observed restart policies are consistent and must not be conflated. Native recovery does not directly inspect private memory retention.

## Current acceptance boundary

ApplicationStop partial-pose hold and Run-alone sequence resumption are supported. Full supported four-carton/home endpoint is pending this report stage; no final closure yet. No safety-rated performance, carrying-load interruption or all-phase recovery claim is made.

## Final supported endpoint — closure

Five further original captures rehash-match action records (14matched originals total). Actual [final top](demo5-appstop-r1-run-alone-final-top.png),14968, [final lower](demo5-appstop-r1-run-alone-final-lower.png),15001, and [final held](demo5-appstop-r1-run-alone-final-held.png),15088, show gantry_homeTrue, allXYZencoder counts0, all six axis directionsFalse, cartonAttachedFalse/cell_activeFalse/cycle_in_progressFalse. Actual four-carton layer rests supported on pallet; tool is backhome. The lower-named frame did not scroll to lower rows and is not credited as direct step100 evidence.

Correctly scrolled [complete Signals](demo5-appstop-r1-run-alone-complete-signals.png),16408, and [complete held](demo5-appstop-r1-run-alone-complete-held.png),16461, directly show placed_cartons4/layer_completeTrue/**sequence_step_value100**, vacuum_pickFalse, pick_completeTrue, system_enableTrue/palletvalidTrue/tripFalse/palletizer_faultFalse. Private machine_run is not visible in these rows; endpointcellinactive and offaxis/vacuum commands are directly observed instead.

| Original | SHA256 |
|---|---|
| final-top | 02684b1f658646303d79507d6da4ce6ceed87b838d6a6e43a733598fda1f69bd |
| final-lower | 21a25bf16021a1f33d44f91c761ba85de139f9beeb2c8fc18dcfb1b2a39b9ee8 |
| final-held | 363a7492c669cd780002e1d75dbbc2fabebb235d2ea638104f0312598d7ac2f8 |
| complete-signals | b0f55dad1c100bb6d9e12c7aaff07391d22c94cf15dd733b1726a3439577a49e |
| complete-held | 217a945e967b931e7d6d996512f0302be9327a47b44fb372b9c300dbd7f52718 |

No physical machineStart occurs between applicationStop and this endpoint: input plans use AppRun only followed by passive waits/Signals scrolling. The later checkpointAppStop is after completion and is not the qualifying interruption.

**The genuine Demo5 applicationStop/held/Run-alone recovery case is now closed for the observed unloaded moving-axis interruption through a supported complete four-carton layer/home.** This supersedes the initial pending-endpoint boundary, without broadening to every phase or carrying-load interruption. It remains distinct from ENABLEloss requiring freshStart. No product failure is demonstrated.
