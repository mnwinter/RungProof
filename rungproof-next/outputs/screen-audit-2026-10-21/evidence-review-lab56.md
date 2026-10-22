# Lab 5.6 independent native review

Reviewed all 17 native case frames in lab56-cases.json using lab56-review-1/2.jpg, retaining actual physical stack, five Signals and runtime. Loaded reviewer ladder scope only; no reviewer UI/backend operations.

Disabled STEP leaves the stack dark. ENABLE lights only red (scan3141). STEP held down1.2 seconds leaves only amber (3234), and mouse release retains amber (3280). Fresh STEP lights green (3341), then fresh STEP red (3404); idle1.2 seconds retains red (3493). All physical colors agree with native tower_red/amber/green, one at a time. Disabling clears every color; disabled STEP stays dark; re-enabling starts red (3679), followed by amber and green on fresh steps. This supports no deferred disabled step in that captured sequence.

AppStop (STOPPED3855) retains tower_enable=True but clears all physical colors and output points. AppRun (3879) republishes green from the fixture's retained phase. This is the loaded fixture policy, not a universal restart specification. AppReset (STOPPED0) clears enable and colors; Run25 remains dark, then fresh ENABLE88 starts red. Settled step_pulse=False in every frame does not establish exact pulse width or scan-level event timing.

No new issue. Simultaneous initial ENABLE+STEP, long sequence stress, different fixture restart policies and exact pulse timing remain unproven. No whole-scene PASS.
