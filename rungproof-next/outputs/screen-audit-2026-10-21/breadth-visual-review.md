# Breadth visual review — bounded snapshot

Snapshot: 77 completed manifest rows; SHA256 809b820b571f697f181e0a8caf95a71f33b6c800f63b6061ef581674fbbda06b. Frozen as breadth-visual-review-snapshot.jsonl. Reviewed 2026-10-21.

## Method and limits

Inspected seven derived 12-entry contact sheets of actual native entry PNGs (400x225 thumbnails with scene IDs); originals preserved. Reviewed OCR text for entry/Run/reset phases of all rows, with full-resolution inspection of the Lab 2.1 diagnostics and Equipment Gallery Run anomalies. No UI input, app changes, runtime scripting, or remediation planning by this reviewer. Contact sheets establish entry appearance only; they do not establish responsiveness, motion, exact I/O behavior, accessible hit targets, or completion.

All 77 entry snapshots contain distinct nonblank scene geometry and normal app chrome. No missing-render/blank scene or crash dialog was found in this snapshot. Wide default views make physical control labels small in several industrial scenes; zoom/navigation and actual usability have not been tested here, so this is not logged as a defect.

OCR Run classification: 10 entries indicate runtime RUNNING; 67 show the EDIT001/no-ladder-networks gate. Missing authored ladder is expected for learner exercises and is not a functional failure. Gallery is a separate pending UX question because its help describes SIM-only motion; its dedicated physical control remains untested. Lab 2.1 returning with a previously authored controller is a pending Discard/cache question, not a default-solution pass.

Derived evidence: breadth-review-sheet-01.jpg through breadth-review-sheet-07.jpg. Per-row native window title and hashes remain in the frozen snapshot. No unexpected exception/crash/missing-resource error text was found by the bounded OCR screen. OCR is fallible and this does not prove the absence of errors.

## Per-entry review disposition

Every row below: entry render visually inspected; Run classification is OCR evidence only. All normal-flow, fault/recovery, and individual control cases remain open except where separate targeted demo/lab reports provide stronger evidence.

| # | Scene ID | Run checkpoint indication | Entry evidence |
|---|---|---|---|
| 1 | lab-4-01-press-count-lamp | RUNNING (not full functional pass) | breadth-lab-4-01-press-count-lamp-entry.png |
| 2 | lab-5-01-delayed-lamp | RUNNING (not full functional pass) | breadth-lab-5-01-delayed-lamp-entry.png |
| 3 | scene-2-conveyor-pusher | RUNNING (not full functional pass) | breadth-scene-2-conveyor-pusher-entry.png |
| 4 | lab-9-11-pallet-counting | RUNNING (not full functional pass) | breadth-lab-9-11-pallet-counting-entry.png |
| 5 | lab-11-13-xy-palletizing | RUNNING (not full functional pass) | breadth-lab-11-13-xy-palletizing-entry.png |
| 6 | conveyor-cell | RUNNING (not full functional pass) | breadth-conveyor-cell-entry.png |
| 7 | scene-1-conveyor-stop | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-scene-1-conveyor-stop-entry.png |
| 8 | equipment-gallery | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-equipment-gallery-entry.png |
| 9 | tank-level | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-tank-level-entry.png |
| 10 | tank-high-low | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-tank-high-low-entry.png |
| 11 | tank-radar | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-tank-radar-entry.png |
| 12 | lab-2-01-workstation-call | RUNNING (not full functional pass) | breadth-lab-2-01-workstation-call-entry.png |
| 13 | lab-2-02-dual-confirmation | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-02-dual-confirmation-entry.png |
| 14 | lab-2-03-service-marker-inhibit | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-03-service-marker-inhibit-entry.png |
| 15 | lab-2-04-two-station-call | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-04-two-station-call-entry.png |
| 16 | lab-2-05-bay-light-selector | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-05-bay-light-selector-entry.png |
| 17 | lab-2-06-ready-attention | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-06-ready-attention-entry.png |
| 18 | lab-2-07-dual-contact-permissive | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-07-dual-contact-permissive-entry.png |
| 19 | lab-2-08-inspection-vote | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-08-inspection-vote-entry.png |
| 20 | lab-2-09-maintenance-beacon | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-09-maintenance-beacon-entry.png |
| 21 | lab-2-10-dust-collector-seal-in | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-10-dust-collector-seal-in-entry.png |
| 22 | lab-2-11-inbound-tote-stop | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-11-inbound-tote-stop-entry.png |
| 23 | lab-2-12-assembly-lift | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-12-assembly-lift-entry.png |
| 24 | lab-2-13-coolant-jug-fill | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-13-coolant-jug-fill-entry.png |
| 25 | lab-2-14-sump-pump | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-14-sump-pump-entry.png |
| 26 | lab-2-15-fume-extractor | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-15-fume-extractor-entry.png |
| 27 | lab-2-16-safe-drill | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-16-safe-drill-entry.png |
| 28 | lab-2-17-pallet-robot | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-17-pallet-robot-entry.png |
| 29 | lab-2-18-pallet-pickup | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-18-pallet-pickup-entry.png |
| 30 | lab-2-19-service-door | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-19-service-door-entry.png |
| 31 | lab-2-20-bottle-shuttle | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-20-bottle-shuttle-entry.png |
| 32 | lab-2-21-tote-finishing | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-21-tote-finishing-entry.png |
| 33 | lab-2-22-dual-spindle | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-22-dual-spindle-entry.png |
| 34 | lab-2-23-parcel-sorter | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-23-parcel-sorter-entry.png |
| 35 | lab-2-24-robot-cnc | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-24-robot-cnc-entry.png |
| 36 | lab-2-25-inspection-toggle | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-2-25-inspection-toggle-entry.png |
| 37 | lab-3-01-guarded-pallet-transfer | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-3-01-guarded-pallet-transfer-entry.png |
| 38 | lab-3-02-robot-cell-safe-restart | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-3-02-robot-cell-safe-restart-entry.png |
| 39 | lab-4-02-counter-reset-lamp | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-02-counter-reset-lamp-entry.png |
| 40 | lab-4-03-repeat-cycle-counter | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-03-repeat-cycle-counter-entry.png |
| 41 | lab-4-04-sequence-light-tower | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-04-sequence-light-tower-entry.png |
| 42 | lab-4-05-dual-input-count-window | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-05-dual-input-count-window-entry.png |
| 43 | lab-4-06-multi-press-confirmation | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-06-multi-press-confirmation-entry.png |
| 44 | lab-4-07-parking-garage-entry | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-07-parking-garage-entry-entry.png |
| 45 | lab-4-08-package-grouping | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-08-package-grouping-entry.png |
| 46 | lab-4-09-chain-drive-lift | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-09-chain-drive-lift-entry.png |
| 47 | lab-4-10-cookie-packaging | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-10-cookie-packaging-entry.png |
| 48 | lab-4-11-barrel-fill-station | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-11-barrel-fill-station-entry.png |
| 49 | lab-4-12-cable-cut-length | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-4-12-cable-cut-length-entry.png |
| 50 | lab-5-02-timed-lamp-off | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-02-timed-lamp-off-entry.png |
| 51 | lab-5-03-rotary-flasher | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-03-rotary-flasher-entry.png |
| 52 | lab-5-04-alternating-lamps | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-04-alternating-lamps-entry.png |
| 53 | lab-5-05-variable-flash-rate | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-05-variable-flash-rate-entry.png |
| 54 | lab-5-06-running-light-tower | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-06-running-light-tower-entry.png |
| 55 | lab-5-07-pedestrian-crossing | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-07-pedestrian-crossing-entry.png |
| 56 | lab-5-08-drawbridge-control | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-08-drawbridge-control-entry.png |
| 57 | lab-5-09-bag-indexing-conveyor | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-09-bag-indexing-conveyor-entry.png |
| 58 | lab-5-10-coating-line | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-5-10-coating-line-entry.png |
| 59 | lab-6-07-luggage-weight-sort | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-6-07-luggage-weight-sort-entry.png |
| 60 | lab-6-08-hand-dryer | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-6-08-hand-dryer-entry.png |
| 61 | lab-9-01-sum-function | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-9-01-sum-function-entry.png |
| 62 | lab-9-02-product-function | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-9-02-product-function-entry.png |
| 63 | lab-9-03-sum-and-counter-function | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-9-03-sum-and-counter-function-entry.png |
| 64 | lab-9-04-function-selector | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-9-04-function-selector-entry.png |
| 65 | lab-9-10-box-volume | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-9-10-box-volume-entry.png |
| 66 | lab-9-12-ev-charging-manager | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-9-12-ev-charging-manager-entry.png |
| 67 | lab-10-01-drive-alarm-code-string | RUNNING (not full functional pass) | breadth-lab-10-01-drive-alarm-code-string-entry.png |
| 68 | lab-10-02-chicken-label-print | RUNNING (not full functional pass) | breadth-lab-10-02-chicken-label-print-entry.png |
| 69 | lab-10-03-vision-package-sorter | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-10-03-vision-package-sorter-entry.png |
| 70 | lab-10-04-motor-enum-state | RUNNING (not full functional pass) | breadth-lab-10-04-motor-enum-state-entry.png |
| 71 | lab-10-05-motor-struct-data | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-10-05-motor-struct-data-entry.png |
| 72 | lab-10-06-ten-motor-array-startup | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-10-06-ten-motor-array-startup-entry.png |
| 73 | lab-11-06-wastewater-collection | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-11-06-wastewater-collection-entry.png |
| 74 | lab-11-07-multi-conveyor-pallet-route | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-11-07-multi-conveyor-pallet-route-entry.png |
| 75 | lab-11-11-service-elevator | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-11-11-service-elevator-entry.png |
| 76 | lab-11-12-mobile-traffic-lights | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-11-12-mobile-traffic-lights-entry.png |
| 77 | lab-11-19-powder-batch-mixer | No-ladder gate (expected for learner exercise; Gallery pending) | breadth-lab-11-19-powder-batch-mixer-entry.png |
