# Post-backup Lab 10.6 array transport review

Independent selected original-pixel review only; application/source and tracked reports unchanged.

Actual V2 array-command baseline shows occupied zone1. Native small-array-r2-zone4-6.png shows zone4 True; zone5-5.png shows zone5 True; zone6-5.png shows zone6 True; zone7-4.png shows zone7 True. Corresponding carton poses advance along the supported ten-conveyor line. Zone7-4 also directly shows motor_commands[0] through [9] True while legacy motor_array_run and visible legacy motor6..9 outputs remain False. Thus movement is associated with the array command route, rather than legacy aggregate output.

The earlier r1 timed end caption is not endpoint proof; it actually showed zone5. Likewise r2-zone8-18.png has a carton near the downstream end but all visible occupancy rows False, so it is not credited as zone8. Those two particular timed captures receive no zone8/9 credit; the matching original frames identified below establish the current result. Earlier zone0..3 and interruption evidence is separate.

No built-in sequential startup is claimed or required by this test. Exact transport timing, load-contact physics and unsampled occupancy edge durations are outside the evidence. Derived compact image: postbackup-small-array-progress-contact.jpg.

## Actual zone8/9 and finite downstream hold

Review of all 76 original r2-zone8 captures found the missed native pulses: small-array-r2-zone8-6.png directly shows zone_occupied_8 True (zone9 False); small-array-r2-zone8-10.png directly shows zone_occupied_9 True (zone8 False). The helper OCR timeout is not a sensor failure. These actual frames supersede the earlier pending zone8/9 statement. Combined with the earlier zone0..3 review and the actual zone4..7 frames above, all ten zone feedback positions have observed occupancy evidence across the recorded runs.

small-array-r3-current-signals.png shows the carton downstream with all occupancy rows False after it passed the last sensing region; this is not a completion signal. The same-camera small-array-r3-end-candidate.png and small-array-r3-end-held-candidate.png show the carton held on the finite final conveyor support while the runtime continues advancing (root capture interval five seconds). Credit is limited to supported downstream travel and bounded held pose, without inventing a batch/cycle completion point or claiming automatic command shutdown.

Additional derived evidence: postbackup-array-zone8-all.jpg, postbackup-array-zone89-proof.jpg and postbackup-array-end-contact.jpg.
