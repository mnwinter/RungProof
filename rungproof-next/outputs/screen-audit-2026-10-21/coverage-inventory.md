# Screen-control audit coverage inventory

Date: 2026-10-21 (America/Los_Angeles). Repository: mnwinter/RungProof; current local source inventory only.

Screen-control execution is blocked: this session exposes no native apps or browser tabs, and native computer APIs are disabled. No entry below has been function tested. Source/catalog enumeration establishes scope only. No behavioral scripts, automated backend tests, or repairs were performed for this inventory.

Coverage: 5 authored demo project workflows; 77 unique catalog scenes (70 labs and 7 other scenes). Demo-associated scenes overlap the catalog and are not additional unique scenes.

Catalog source: rungproof-next/scenes/catalog/original-scenes.catalog.json. Main.cs loads this catalog at lines 385 and 536-542; current in-app visibility remains unverified.

## Authored demo workflows

| Project ID | Project name | Associated scene | File | Status |
|---|---|---|---|---|
| demo-1-press-count | Press_Count_Main | lab-4-01-press-count-lamp | rungproof-next/programs/demos/01-press-count-starter.rpproj.json | NOT TESTED - screen control unavailable |
| demo-2-delayed-lamp | Delayed_Lamp_Main | lab-5-01-delayed-lamp | rungproof-next/programs/demos/02-delayed-lamp-timer.rpproj.json | NOT TESTED - screen control unavailable |
| demo-3-height-sorter | Height_Sorting_Cell_Main | scene-2-conveyor-pusher | rungproof-next/programs/demos/03-conveyor-sequence.rpproj.json | NOT TESTED - screen control unavailable |
| demo-4-batch-process | Batch_Process_Main | lab-9-11-pallet-counting | rungproof-next/programs/demos/04-batch-process-blocks.rpproj.json | NOT TESTED - screen control unavailable |
| demo-5-integrated-palletizer | Palletizing_Cell_Main | lab-11-13-xy-palletizing | rungproof-next/programs/demos/05-integrated-cell-multi-fb-fc.rpproj.json | NOT TESTED - screen control unavailable |

## Labs

| Scene ID | Name | File | Status |
|---|---|---|---|
| lab-10-01-drive-alarm-code-string | Lab 10.1 - Drive Alarm-Code String | rungproof-next/scenes/migrated/lab-10-01-drive-alarm-code-string.scene.json | NOT TESTED - screen control unavailable |
| lab-10-02-chicken-label-print | Lab 10.2 - Chicken Label Print | rungproof-next/scenes/migrated/lab-10-02-chicken-label-print.scene.json | NOT TESTED - screen control unavailable |
| lab-10-03-vision-package-sorter | Lab 10.3 - Vision Package Sorter | rungproof-next/scenes/migrated/lab-10-03-vision-package-sorter.scene.json | NOT TESTED - screen control unavailable |
| lab-10-04-motor-enum-state | Lab 10.4 - Motor Operating-State Enum | rungproof-next/scenes/migrated/lab-10-04-motor-enum-state.scene.json | NOT TESTED - screen control unavailable |
| lab-10-05-motor-struct-data | Lab 10.5 - Motor STRUCT Data | rungproof-next/scenes/migrated/lab-10-05-motor-struct-data.scene.json | NOT TESTED - screen control unavailable |
| lab-10-06-ten-motor-array-startup | Lab 10.6 - Ten-Motor Array Startup | rungproof-next/scenes/migrated/lab-10-06-ten-motor-array-startup.scene.json | NOT TESTED - screen control unavailable |
| lab-11-06-wastewater-collection | Lab 11.6 - Wastewater Collection | rungproof-next/scenes/migrated/lab-11-06-wastewater-collection.scene.json | NOT TESTED - screen control unavailable |
| lab-11-07-multi-conveyor-pallet-route | Lab 11.7 - Multi-Conveyor Pallet Route | rungproof-next/scenes/migrated/lab-11-07-multi-conveyor-pallet-route.scene.json | NOT TESTED - screen control unavailable |
| lab-11-11-service-elevator | Lab 11.11 - Service Elevator | rungproof-next/scenes/migrated/lab-11-11-service-elevator.scene.json | NOT TESTED - screen control unavailable |
| lab-11-12-mobile-traffic-lights | Lab 11.12 - Mobile Traffic Lights | rungproof-next/scenes/migrated/lab-11-12-mobile-traffic-lights.scene.json | NOT TESTED - screen control unavailable |
| lab-11-13-xy-palletizing | Lab 11.13 - XY Palletizing Cell | rungproof-next/scenes/migrated/lab-11-13-xy-palletizing.scene.json | NOT TESTED - screen control unavailable |
| lab-11-19-powder-batch-mixer | Lab 11.19 - Powder Batch Mixer | rungproof-next/scenes/migrated/lab-11-19-powder-batch-mixer.scene.json | NOT TESTED - screen control unavailable |
| lab-2-01-workstation-call | Lab 2.1 - Workstation Call Lamp | rungproof-next/scenes/migrated/lab-2-01-workstation-call.scene.json | NOT TESTED - screen control unavailable |
| lab-2-02-dual-confirmation | Lab 2.2 - Dual Confirmation Lamp | rungproof-next/scenes/migrated/lab-2-02-dual-confirmation.scene.json | NOT TESTED - screen control unavailable |
| lab-2-03-service-marker-inhibit | Lab 2.3 - Service Marker Inhibit | rungproof-next/scenes/migrated/lab-2-03-service-marker-inhibit.scene.json | NOT TESTED - screen control unavailable |
| lab-2-04-two-station-call | Lab 2.4 - Two-Station Call Beacon | rungproof-next/scenes/migrated/lab-2-04-two-station-call.scene.json | NOT TESTED - screen control unavailable |
| lab-2-05-bay-light-selector | Lab 2.5 - Bay Light Selector | rungproof-next/scenes/migrated/lab-2-05-bay-light-selector.scene.json | NOT TESTED - screen control unavailable |
| lab-2-06-ready-attention | Lab 2.6 - Ready / Attention Button | rungproof-next/scenes/migrated/lab-2-06-ready-attention.scene.json | NOT TESTED - screen control unavailable |
| lab-2-07-dual-contact-permissive | Lab 2.7 - Dual-Contact Permissive | rungproof-next/scenes/migrated/lab-2-07-dual-contact-permissive.scene.json | NOT TESTED - screen control unavailable |
| lab-2-08-inspection-vote | Lab 2.8 - Inspection Vote Stacklight | rungproof-next/scenes/migrated/lab-2-08-inspection-vote.scene.json | NOT TESTED - screen control unavailable |
| lab-2-09-maintenance-beacon | Lab 2.9 - Maintenance Beacon Selector | rungproof-next/scenes/migrated/lab-2-09-maintenance-beacon.scene.json | NOT TESTED - screen control unavailable |
| lab-2-10-dust-collector-seal-in | Lab 2.10 - Dust Collector Seal-In | rungproof-next/scenes/migrated/lab-2-10-dust-collector-seal-in.scene.json | NOT TESTED - screen control unavailable |
| lab-2-11-inbound-tote-stop | Lab 2.11 - Inbound Tote Stop | rungproof-next/scenes/migrated/lab-2-11-inbound-tote-stop.scene.json | NOT TESTED - screen control unavailable |
| lab-2-12-assembly-lift | Lab 2.12 - Ergonomic Assembly Lift | rungproof-next/scenes/migrated/lab-2-12-assembly-lift.scene.json | NOT TESTED - screen control unavailable |
| lab-2-13-coolant-jug-fill | Lab 2.13 - Coolant Jug Filling Cell | rungproof-next/scenes/migrated/lab-2-13-coolant-jug-fill.scene.json | NOT TESTED - screen control unavailable |
| lab-2-14-sump-pump | Lab 2.14 - Sump Dewatering Pump | rungproof-next/scenes/migrated/lab-2-14-sump-pump.scene.json | NOT TESTED - screen control unavailable |
| lab-2-15-fume-extractor | Lab 2.15 - Weld Fume Extractor | rungproof-next/scenes/migrated/lab-2-15-fume-extractor.scene.json | NOT TESTED - screen control unavailable |
| lab-2-16-safe-drill | Lab 2.16 - Fixture-Safe Drill Station | rungproof-next/scenes/migrated/lab-2-16-safe-drill.scene.json | NOT TESTED - screen control unavailable |
| lab-2-17-pallet-robot | Lab 2.17 - Twin-Container Pallet Cell | rungproof-next/scenes/migrated/lab-2-17-pallet-robot.scene.json | NOT TESTED - screen control unavailable |
| lab-2-18-pallet-pickup | Lab 2.18 - Shipping Pallet Accumulation | rungproof-next/scenes/migrated/lab-2-18-pallet-pickup.scene.json | NOT TESTED - screen control unavailable |
| lab-2-19-service-door | Lab 2.19 - Service Door Shutter | rungproof-next/scenes/migrated/lab-2-19-service-door.scene.json | NOT TESTED - screen control unavailable |
| lab-2-20-bottle-shuttle | Lab 2.20 - Bottle Shuttle Conveyor | rungproof-next/scenes/migrated/lab-2-20-bottle-shuttle.scene.json | NOT TESTED - screen control unavailable |
| lab-2-21-tote-finishing | Lab 2.21 - Chemical Tote Finishing Line | rungproof-next/scenes/migrated/lab-2-21-tote-finishing.scene.json | NOT TESTED - screen control unavailable |
| lab-2-22-dual-spindle | Lab 2.22 - Dual-Spindle Plate Cell | rungproof-next/scenes/migrated/lab-2-22-dual-spindle.scene.json | NOT TESTED - screen control unavailable |
| lab-2-23-parcel-sorter | Lab 2.23 - Parcel Size Sorter | rungproof-next/scenes/migrated/lab-2-23-parcel-sorter.scene.json | NOT TESTED - screen control unavailable |
| lab-2-24-robot-cnc | Lab 2.24 - Robot CNC Tending Cell | rungproof-next/scenes/migrated/lab-2-24-robot-cnc.scene.json | NOT TESTED - screen control unavailable |
| lab-2-25-inspection-toggle | Lab 2.25 - Inspection Light Toggle | rungproof-next/scenes/migrated/lab-2-25-inspection-toggle.scene.json | NOT TESTED - screen control unavailable |
| lab-3-01-guarded-pallet-transfer | Lab 3.1 - Guarded Pallet Transfer | rungproof-next/scenes/migrated/lab-3-01-guarded-pallet-transfer.scene.json | NOT TESTED - screen control unavailable |
| lab-3-02-robot-cell-safe-restart | Lab 3.2 - Robot Cell Safe Restart | rungproof-next/scenes/migrated/lab-3-02-robot-cell-safe-restart.scene.json | NOT TESTED - screen control unavailable |
| lab-4-01-press-count-lamp | Lab 4.1 - Press-Count Lamp | rungproof-next/scenes/migrated/lab-4-01-press-count-lamp.scene.json | NOT TESTED - screen control unavailable |
| lab-4-02-counter-reset-lamp | Lab 4.2 - Counter Reset Lamp | rungproof-next/scenes/migrated/lab-4-02-counter-reset-lamp.scene.json | NOT TESTED - screen control unavailable |
| lab-4-03-repeat-cycle-counter | Lab 4.3 - Repeat-Cycle Counter | rungproof-next/scenes/migrated/lab-4-03-repeat-cycle-counter.scene.json | NOT TESTED - screen control unavailable |
| lab-4-04-sequence-light-tower | Lab 4.4 - Sequence Light Tower | rungproof-next/scenes/migrated/lab-4-04-sequence-light-tower.scene.json | NOT TESTED - screen control unavailable |
| lab-4-05-dual-input-count-window | Lab 4.5 - Dual-Input Count Window | rungproof-next/scenes/migrated/lab-4-05-dual-input-count-window.scene.json | NOT TESTED - screen control unavailable |
| lab-4-06-multi-press-confirmation | Lab 4.6 - Multi-Press Confirmation | rungproof-next/scenes/migrated/lab-4-06-multi-press-confirmation.scene.json | NOT TESTED - screen control unavailable |
| lab-4-07-parking-garage-entry | Lab 4.7 - Parking Garage Entry | rungproof-next/scenes/migrated/lab-4-07-parking-garage-entry.scene.json | NOT TESTED - screen control unavailable |
| lab-4-08-package-grouping | Lab 4.8 - Package Grouping Station | rungproof-next/scenes/migrated/lab-4-08-package-grouping.scene.json | NOT TESTED - screen control unavailable |
| lab-4-09-chain-drive-lift | Lab 4.9 - Chain-Drive Lift | rungproof-next/scenes/migrated/lab-4-09-chain-drive-lift.scene.json | NOT TESTED - screen control unavailable |
| lab-4-10-cookie-packaging | Lab 4.10 - Cookie Packaging Cell | rungproof-next/scenes/migrated/lab-4-10-cookie-packaging.scene.json | NOT TESTED - screen control unavailable |
| lab-4-11-barrel-fill-station | Lab 4.11 - Barrel Fill Station | rungproof-next/scenes/migrated/lab-4-11-barrel-fill-station.scene.json | NOT TESTED - screen control unavailable |
| lab-4-12-cable-cut-length | Lab 4.12 - Cable Cut-Length Cell | rungproof-next/scenes/migrated/lab-4-12-cable-cut-length.scene.json | NOT TESTED - screen control unavailable |
| lab-5-01-delayed-lamp | Lab 5.1 - Delayed Lamp | rungproof-next/scenes/migrated/lab-5-01-delayed-lamp.scene.json | NOT TESTED - screen control unavailable |
| lab-5-02-timed-lamp-off | Lab 5.2 - Timed Lamp-Off | rungproof-next/scenes/migrated/lab-5-02-timed-lamp-off.scene.json | NOT TESTED - screen control unavailable |
| lab-5-03-rotary-flasher | Lab 5.3 - Rotary Flasher | rungproof-next/scenes/migrated/lab-5-03-rotary-flasher.scene.json | NOT TESTED - screen control unavailable |
| lab-5-04-alternating-lamps | Lab 5.4 - Alternating Lamps | rungproof-next/scenes/migrated/lab-5-04-alternating-lamps.scene.json | NOT TESTED - screen control unavailable |
| lab-5-05-variable-flash-rate | Lab 5.5 - Variable Flash Rate | rungproof-next/scenes/migrated/lab-5-05-variable-flash-rate.scene.json | NOT TESTED - screen control unavailable |
| lab-5-06-running-light-tower | Lab 5.6 - Running-Light Tower | rungproof-next/scenes/migrated/lab-5-06-running-light-tower.scene.json | NOT TESTED - screen control unavailable |
| lab-5-07-pedestrian-crossing | Lab 5.7 - Pedestrian Crossing | rungproof-next/scenes/migrated/lab-5-07-pedestrian-crossing.scene.json | NOT TESTED - screen control unavailable |
| lab-5-08-drawbridge-control | Lab 5.8 - Drawbridge Control | rungproof-next/scenes/migrated/lab-5-08-drawbridge-control.scene.json | NOT TESTED - screen control unavailable |
| lab-5-09-bag-indexing-conveyor | Lab 5.9 - Bag Indexing Conveyor | rungproof-next/scenes/migrated/lab-5-09-bag-indexing-conveyor.scene.json | NOT TESTED - screen control unavailable |
| lab-5-10-coating-line | Lab 5.10 - Coating Line | rungproof-next/scenes/migrated/lab-5-10-coating-line.scene.json | NOT TESTED - screen control unavailable |
| lab-6-07-luggage-weight-sort | Lab 6.7 - Luggage Weight Sort | rungproof-next/scenes/migrated/lab-6-07-luggage-weight-sort.scene.json | NOT TESTED - screen control unavailable |
| lab-6-08-hand-dryer | Lab 6.8 - Timed Hand-Dryer | rungproof-next/scenes/migrated/lab-6-08-hand-dryer.scene.json | NOT TESTED - screen control unavailable |
| lab-9-01-sum-function | Lab 9.1 - Sum Function Block | rungproof-next/scenes/migrated/lab-9-01-sum-function.scene.json | NOT TESTED - screen control unavailable |
| lab-9-02-product-function | Lab 9.2 - Product Function Block | rungproof-next/scenes/migrated/lab-9-02-product-function.scene.json | NOT TESTED - screen control unavailable |
| lab-9-03-sum-and-counter-function | Lab 9.3 - Sum and Counter Function | rungproof-next/scenes/migrated/lab-9-03-sum-and-counter-function.scene.json | NOT TESTED - screen control unavailable |
| lab-9-04-function-selector | Lab 9.4 - Function Selector | rungproof-next/scenes/migrated/lab-9-04-function-selector.scene.json | NOT TESTED - screen control unavailable |
| lab-9-10-box-volume | Lab 9.10 - Box Volume Calculation | rungproof-next/scenes/migrated/lab-9-10-box-volume.scene.json | NOT TESTED - screen control unavailable |
| lab-9-11-pallet-counting | Lab 9.11 - Pallet Count Function | rungproof-next/scenes/migrated/lab-9-11-pallet-counting.scene.json | NOT TESTED - screen control unavailable |
| lab-9-12-ev-charging-manager | Lab 9.12 - EV Charging Manager | rungproof-next/scenes/migrated/lab-9-12-ev-charging-manager.scene.json | NOT TESTED - screen control unavailable |

## Other catalog scenes

| Scene ID | Name | File | Status |
|---|---|---|---|
| conveyor-cell | Conveyor Inspection Cell | rungproof-next/scenes/migrated/conveyor-cell.scene.json | NOT TESTED - screen control unavailable |
| equipment-gallery | Reusable Equipment Gallery | rungproof-next/scenes/migrated/equipment-gallery.scene.json | NOT TESTED - screen control unavailable |
| scene-1-conveyor-stop | Conveyor Stop | rungproof-next/scenes/migrated/scene-1-conveyor-stop.scene.json | NOT TESTED - screen control unavailable |
| scene-2-conveyor-pusher | Conveyor Pusher | rungproof-next/scenes/migrated/scene-2-conveyor-pusher.scene.json | NOT TESTED - screen control unavailable |
| tank-high-low | Water Tank — High/Low Switches | rungproof-next/scenes/migrated/tank-high-low.scene.json | NOT TESTED - screen control unavailable |
| tank-level | Tank Level / 4–20 mA | rungproof-next/scenes/migrated/tank-level.scene.json | NOT TESTED - screen control unavailable |
| tank-radar | Water Tank — Radar Level | rungproof-next/scenes/migrated/tank-radar.scene.json | NOT TESTED - screen control unavailable |

## Findings status

No application defects are established by this inventory. The screen-control access failure is an audit blocker, not a demonstrated app defect. A complete per-scene issue list and remediation plan remain pending actual screen interaction across the full inventory.
