# Lab 9.2 independent screen evidence review

Reviewed all27 uniquely indexed saved frames from lab902-monitored.json; actual physical A/B/PRODUCT digits, validity lamp, seven Signals and runtime. Full fixture-lab-9-02-product-function-running.png confirms identity/control layout; four lab902-review-compact-*.jpg sheets and source-hash manifest retain derived provenance. Reviewer used no UI input/backend checks.

At A2/B5, all eight A-valid/B-valid/CALCULATE combinations appear. Only111 sets product_result_validTrue and lights green. Before first valid calculation PRODUCT0; after computing10, subsequent invalid combinations retain numeric10 while validityFalse/lampdark. Re-enabled111 restores validgreen10. Numeric retention is an observation, not a failed invalidity check.

All-permissive arithmetic agrees in physical displays and Signals: A5/B5=25; A10/B5=50; A0/B5=0; A1/B5=5; A2/B5=10. Then A2/B10=20; B0=0; B1=2; B2=4; B5=10. Green remains lit even when the valid product is zero, correctly distinguishing value from validity. These uniquely indexed captures support the A and B0/1/2/5/10 manual cycle wraps at the recorded checkpoints.

App Stop retains factor2/5 and three permissivesTrue, but product0/validFalse/dark at STOPPEDscan6100. Run recomputes10/validTrue/green at6179. Active App Reset clears factors0/0, all three inputsFalse, product0/validFalse/dark at STOPPEDscan0. Subsequent Run stayszero/false/dark at89.

No new demonstrated issue. Explicitly GUI-loaded reference scope, not empty exercise behavior. Unproven: negative/arbitrary factors outside selector set, overflow, simultaneous changes and exact response latency. No whole-scene PASS claim.
