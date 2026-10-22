# Lab 5.4 independent native review

Reviewed 48 cycle/recovery plan capture actions plus final reset check (49 frames) using four lab54-review sheets. Actual physical amber A/green B, three native Signals and scan/runtime retained. Loaded reviewer ladder scope only; no reviewer UI/backend operations.

First enable6174 and cycle00/6188 light amber A only. Subsequent cycle samples alternate: B6200/6211; A6223/6235; B6248/6260; A6273/6285; B6297/6310; A6322/6335; B6346/6359; A6372/6385; B6398/6410; A6422. In every enabled running sample exactly one physical lamp is lit and exactly one lamp_a/lamp_b is True. This is captured mutual exclusivity, not scan-continuous proof or an exact500ms half-period measurement.

Disable6440 and held6500 clear enable and both physical lamps. Fresh enable6514 starts A; repeat samples again alternate A/B through6677. AppStop6696 clears both lamps while enable remainsTrue; lateStop6696 stays dark across1.1 seconds. The immediate filename apprun-resumed still displays STOPPED6696 and is not credited as a running state. Subsequent actual RUNNING6709 shows B,6722/6736 A,6749/6762 B,6776/6790 A and6803 B, proving resumed alternation. AppReset and independent reset check are STOPPED0, all three pointsFalse and both lamps dark.

No new issue. Exact timer boundaries, long-duration drift, overlap between captures and simultaneous control changes remain unproven. No whole-scene PASS.
