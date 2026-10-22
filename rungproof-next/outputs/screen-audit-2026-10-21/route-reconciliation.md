# Route reconciliation

Current scope is 77 unique scene identities: five demo aliases, six USER CREATED entries, and 66 LABS entries. The five demo-associated scene IDs are not five additional standalone scene entries. Previous 82-workflow bookkeeping overcounted identities and imposed hypothetical duplicate routes.

## Exposed routes and common behavior

- Scenario menu: SimulatorShell.cs:3455-3498 adds each _orderedScenes entry once, in groups 2/0/1; demo group labels are authored aliases and tooltips carry actual IDs.
- Scenario Browser: SimulatorShell.cs:7915-7935 groups the same IDs 2/0/1. Demo labels include curriculum/canonical lab labels; there is no duplicate lab row for these five IDs. ItemSelected at 5497-5502 calls RequestSceneChange.
- Viewer Hierarchy: SimulatorShell.cs:8013-8031 puts authored IDs under FIVE-DEMO CURRICULUM, then explicitly excludes authored IDs from OTHER SCENES. SelectViewerItem at 8057-8065 also calls RequestSceneChange. It is another navigation surface, not another scene instance.
- Hidden operator list: SimulatorShell.cs:4472 creates _operatorSceneList with Visible=false; 7938-7942 populates names but no selection handler/exposed opening route was found. It does not establish a visible duplicate route.
- RequestSceneChange at 4231-4251 routes every surface through the same scene ID; an authored-demo ID requests normal Run/validate/load automatically regardless of surface. showOperator changes view only. Opening a saved project is a project-loading workflow, not a new catalog identity.

No visible separate standalone variant of the five demo-associated IDs was found in the inspected selection routes. Source routing establishes identity; it does not prove that all alternate menu/browser/viewer clicks work. Keep those navigation checks open. Retain existing demo case evidence only for its stated scope.

## Screen anchors and ordinal limits

Independent visual inspection: scene-menu-keyboard-calibration-v2.png visibly selects Water Tank — Radar Level beneath five demo and six user-created rows; lab-2-01-entry.png shows Lab2.1 selected; lab202-ready.png shows Lab2.2 selected. Root operator reports validated Home+13Down -> radar, +14Down -> Lab2.1, +15Down -> Lab2.2.

Down counts below use those observed calibration anchors: count = zero-based menuScenes ordinal + 3. Other rows are source-derived predictions, not individually validated keyboard behavior. Disabled headings/separators and keyboard focus must be checked if an ordinal lands differently. Do not substitute these predictions for screen verification.

| Scene ordinal (1-based) | Home + Down count | Group | Menu label | Actual scene ID |
|---|---|---|---|---|
| 1 | 3 | DEMOS | Demo 1 - Press Count Starter | lab-4-01-press-count-lamp |
| 2 | 4 | DEMOS | Demo 2 - Delayed Lamp Timer | lab-5-01-delayed-lamp |
| 3 | 5 | DEMOS | Demo 3 - Height Sorting Pusher Cell | scene-2-conveyor-pusher |
| 4 | 6 | DEMOS | Demo 4 - Batch Process Blocks | lab-9-11-pallet-counting |
| 5 | 7 | DEMOS | Demo 5 - Integrated Palletizer | lab-11-13-xy-palletizing |
| 6 | 8 | USER CREATED | Conveyor Inspection Cell | conveyor-cell |
| 7 | 9 | USER CREATED | Conveyor Stop | scene-1-conveyor-stop |
| 8 | 10 | USER CREATED | Reusable Equipment Gallery | equipment-gallery |
| 9 | 11 | USER CREATED | Tank Level / 4–20 mA | tank-level |
| 10 | 12 | USER CREATED | Water Tank — High/Low Switches | tank-high-low |
| 11 | 13 | USER CREATED | Water Tank — Radar Level | tank-radar |
| 12 | 14 | LABS | Lab 2.1 - Workstation Call Lamp | lab-2-01-workstation-call |
| 13 | 15 | LABS | Lab 2.2 - Dual Confirmation Lamp | lab-2-02-dual-confirmation |
| 14 | 16 | LABS | Lab 2.3 - Service Marker Inhibit | lab-2-03-service-marker-inhibit |
| 15 | 17 | LABS | Lab 2.4 - Two-Station Call Beacon | lab-2-04-two-station-call |
| 16 | 18 | LABS | Lab 2.5 - Bay Light Selector | lab-2-05-bay-light-selector |
| 17 | 19 | LABS | Lab 2.6 - Ready / Attention Button | lab-2-06-ready-attention |
| 18 | 20 | LABS | Lab 2.7 - Dual-Contact Permissive | lab-2-07-dual-contact-permissive |
| 19 | 21 | LABS | Lab 2.8 - Inspection Vote Stacklight | lab-2-08-inspection-vote |
| 20 | 22 | LABS | Lab 2.9 - Maintenance Beacon Selector | lab-2-09-maintenance-beacon |
| 21 | 23 | LABS | Lab 2.10 - Dust Collector Seal-In | lab-2-10-dust-collector-seal-in |
| 22 | 24 | LABS | Lab 2.11 - Inbound Tote Stop | lab-2-11-inbound-tote-stop |
| 23 | 25 | LABS | Lab 2.12 - Ergonomic Assembly Lift | lab-2-12-assembly-lift |
| 24 | 26 | LABS | Lab 2.13 - Coolant Jug Filling Cell | lab-2-13-coolant-jug-fill |
| 25 | 27 | LABS | Lab 2.14 - Sump Dewatering Pump | lab-2-14-sump-pump |
| 26 | 28 | LABS | Lab 2.15 - Weld Fume Extractor | lab-2-15-fume-extractor |
| 27 | 29 | LABS | Lab 2.16 - Fixture-Safe Drill Station | lab-2-16-safe-drill |
| 28 | 30 | LABS | Lab 2.17 - Twin-Container Pallet Cell | lab-2-17-pallet-robot |
| 29 | 31 | LABS | Lab 2.18 - Shipping Pallet Accumulation | lab-2-18-pallet-pickup |
| 30 | 32 | LABS | Lab 2.19 - Service Door Shutter | lab-2-19-service-door |
| 31 | 33 | LABS | Lab 2.20 - Bottle Shuttle Conveyor | lab-2-20-bottle-shuttle |
| 32 | 34 | LABS | Lab 2.21 - Chemical Tote Finishing Line | lab-2-21-tote-finishing |
| 33 | 35 | LABS | Lab 2.22 - Dual-Spindle Plate Cell | lab-2-22-dual-spindle |
| 34 | 36 | LABS | Lab 2.23 - Parcel Size Sorter | lab-2-23-parcel-sorter |
| 35 | 37 | LABS | Lab 2.24 - Robot CNC Tending Cell | lab-2-24-robot-cnc |
| 36 | 38 | LABS | Lab 2.25 - Inspection Light Toggle | lab-2-25-inspection-toggle |
| 37 | 39 | LABS | Lab 3.1 - Guarded Pallet Transfer | lab-3-01-guarded-pallet-transfer |
| 38 | 40 | LABS | Lab 3.2 - Robot Cell Safe Restart | lab-3-02-robot-cell-safe-restart |
| 39 | 41 | LABS | Lab 4.2 - Counter Reset Lamp | lab-4-02-counter-reset-lamp |
| 40 | 42 | LABS | Lab 4.3 - Repeat-Cycle Counter | lab-4-03-repeat-cycle-counter |
| 41 | 43 | LABS | Lab 4.4 - Sequence Light Tower | lab-4-04-sequence-light-tower |
| 42 | 44 | LABS | Lab 4.5 - Dual-Input Count Window | lab-4-05-dual-input-count-window |
| 43 | 45 | LABS | Lab 4.6 - Multi-Press Confirmation | lab-4-06-multi-press-confirmation |
| 44 | 46 | LABS | Lab 4.7 - Parking Garage Entry | lab-4-07-parking-garage-entry |
| 45 | 47 | LABS | Lab 4.8 - Package Grouping Station | lab-4-08-package-grouping |
| 46 | 48 | LABS | Lab 4.9 - Chain-Drive Lift | lab-4-09-chain-drive-lift |
| 47 | 49 | LABS | Lab 4.10 - Cookie Packaging Cell | lab-4-10-cookie-packaging |
| 48 | 50 | LABS | Lab 4.11 - Barrel Fill Station | lab-4-11-barrel-fill-station |
| 49 | 51 | LABS | Lab 4.12 - Cable Cut-Length Cell | lab-4-12-cable-cut-length |
| 50 | 52 | LABS | Lab 5.2 - Timed Lamp-Off | lab-5-02-timed-lamp-off |
| 51 | 53 | LABS | Lab 5.3 - Rotary Flasher | lab-5-03-rotary-flasher |
| 52 | 54 | LABS | Lab 5.4 - Alternating Lamps | lab-5-04-alternating-lamps |
| 53 | 55 | LABS | Lab 5.5 - Variable Flash Rate | lab-5-05-variable-flash-rate |
| 54 | 56 | LABS | Lab 5.6 - Running-Light Tower | lab-5-06-running-light-tower |
| 55 | 57 | LABS | Lab 5.7 - Pedestrian Crossing | lab-5-07-pedestrian-crossing |
| 56 | 58 | LABS | Lab 5.8 - Drawbridge Control | lab-5-08-drawbridge-control |
| 57 | 59 | LABS | Lab 5.9 - Bag Indexing Conveyor | lab-5-09-bag-indexing-conveyor |
| 58 | 60 | LABS | Lab 5.10 - Coating Line | lab-5-10-coating-line |
| 59 | 61 | LABS | Lab 6.7 - Luggage Weight Sort | lab-6-07-luggage-weight-sort |
| 60 | 62 | LABS | Lab 6.8 - Timed Hand-Dryer | lab-6-08-hand-dryer |
| 61 | 63 | LABS | Lab 9.1 - Sum Function Block | lab-9-01-sum-function |
| 62 | 64 | LABS | Lab 9.2 - Product Function Block | lab-9-02-product-function |
| 63 | 65 | LABS | Lab 9.3 - Sum and Counter Function | lab-9-03-sum-and-counter-function |
| 64 | 66 | LABS | Lab 9.4 - Function Selector | lab-9-04-function-selector |
| 65 | 67 | LABS | Lab 9.10 - Box Volume Calculation | lab-9-10-box-volume |
| 66 | 68 | LABS | Lab 9.12 - EV Charging Manager | lab-9-12-ev-charging-manager |
| 67 | 69 | LABS | Lab 10.1 - Drive Alarm-Code String | lab-10-01-drive-alarm-code-string |
| 68 | 70 | LABS | Lab 10.2 - Chicken Label Print | lab-10-02-chicken-label-print |
| 69 | 71 | LABS | Lab 10.3 - Vision Package Sorter | lab-10-03-vision-package-sorter |
| 70 | 72 | LABS | Lab 10.4 - Motor Operating-State Enum | lab-10-04-motor-enum-state |
| 71 | 73 | LABS | Lab 10.5 - Motor STRUCT Data | lab-10-05-motor-struct-data |
| 72 | 74 | LABS | Lab 10.6 - Ten-Motor Array Startup | lab-10-06-ten-motor-array-startup |
| 73 | 75 | LABS | Lab 11.6 - Wastewater Collection | lab-11-06-wastewater-collection |
| 74 | 76 | LABS | Lab 11.7 - Multi-Conveyor Pallet Route | lab-11-07-multi-conveyor-pallet-route |
| 75 | 77 | LABS | Lab 11.11 - Service Elevator | lab-11-11-service-elevator |
| 76 | 78 | LABS | Lab 11.12 - Mobile Traffic Lights | lab-11-12-mobile-traffic-lights |
| 77 | 79 | LABS | Lab 11.19 - Powder Batch Mixer | lab-11-19-powder-batch-mixer |
