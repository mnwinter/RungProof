# Tote fill and cap review fixture

Open `tote-fill-cap-review.rpproj.json` in the Chemical Tote Finishing Line's Logic Editor, then use Online > Verify + load offline. Run alone remains idle; Start Tote Finishing seals the QA cycle. The four rungs transfer to fill, wait for normalized fill completion, transfer to cap, wait for cap application and chuck home, then discharge to the supported exit. Labeling and inspection are not commanded by this fixture.

The authored belt speed is 0.75 m/s. This fixture uses a 10 ms controller scan, giving 7.5 mm travel per scan. Its cap transfer comparison stops at the first sample at or beyond -5 mm, which lands about 2.5 mm before the 0 m cap datum from the authored home. This stays within the delivered cap's existing radial eligibility. The original 20 ms fixture stopped 5 mm beyond the datum and correctly received cap inhibition. A 10 ms scan alone with a zero threshold still overshoots; both the period and threshold matter. This is a bounded fixture for the current scene dimensions, not a general servo positioning algorithm.

Expected: filled tote acquires its visible cap, chuck returns home, and tote stops at x=6.4 m with travel off. Stop clears commands and retains quantity/cap/pose; Reset returns an empty open tote to the infeed and restores chuck home. If it stops under the capper, inspect position, fill_complete, cap_applied, cap_home and cap_inhibited before changing eligibility or geometry.

The integrated focused verifier runs this saved ladder through the virtual controller and actual scene. Native intermediate filling/capping poses still require inspection from multiple angles. No calibrated litres, real threads, torque, sealing, complete finishing sequence or physical PLC operation is established.
