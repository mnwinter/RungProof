# QA-VIS-002 fix verification

Independent saved-pixel review of the fresh built app's `fix-fume-r1-{baseline,low,medium,high,off}.png`, combined in `fix-fume-r1-review-compact.jpg` using only physical stack and Signals crops.

| State | Actual PLC image | Actual SIM indication | Physical stack |
|---|---|---|---|
| Baseline/off | fan_run False, percent 0, selector 0 | All three False | All dark |
| Low | True, 35, selector 1 | low True only | Green only |
| Medium | True, 65, selector 2 | medium True only | Amber only |
| High | True, 100, selector 3 | high True only | Red only |
| Return off | False, 0, selector 0 | All three False | All dark |

The demonstrated normal selector route now resolves the original dark-stack failure, with no residual previous-speed lamp. Hood light is False throughout these five frames. Light independence and App Stop/Run/Reset remain pending their separate native evidence; this review does not claim measured fan speed or physical shaft-motion verification. No UI input was performed by the reviewer.

## Lifecycle and independent light follow-up

Reviewed seven actual `fix-fume-r2-{light-on,low,stop,run,medium,high,reset}.png` via `fix-fume-r2-review-compact.jpg`. Light request/output are True with selector off, and remain True with green/amber/red speed indications at 35/65/100 respectively. App Stop retains selector1/requestTrue but clears fan_run, percent, all three SIM flags and the physical stack; hood_light_on is False. App Run republishes 35/green only and hood_light_on True. Reset returns selector0/requestFalse/outputFalse/percent0/all flagsFalse and physically dark stack. Thus the speed indication defect is resolved across the 12 reviewed normal/light/lifecycle checkpoints in this built app. This closure concerns commanded-speed display; hood-light emitted illumination, shaft speed, and unsampled transients are not claimed.
