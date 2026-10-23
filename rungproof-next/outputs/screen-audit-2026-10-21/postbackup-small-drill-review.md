# Post-backup Lab 2.16 native drill review

Independent review of selected native screenshots, using derived contact/Signals crops. No application/source or tracked-report changes. Runtime evidence belongs to the loaded V3 QA ladder, not an industrial safety validation.

| Case | Actual native evidence | Scoped result |
|---|---|---|
| Active App Stop | small-drill-r1-active.png, -stopped.png, -held.png, -stop-signals.png | Drill head is partway down; stopped/held head and workpiece poses agree. Both limits False, both hands True, spindle/feed/retract False, cycle False. Active motion stops without losing hand inputs. |
| Run recovery | small-drill-r2-resumed-complete.png | Actual top True, bottom False, cycle True, spindle/feed/retract False. Head is back at its upper pose; amber completion indication agrees with cycle_color amber. |
| Reset and repeat | small-drill-r2-reset.png, small-drill-r3-repeat-signals.png | Reset restores top, clears hands/cycle and commands, red status. Fresh repeated cycle ends top/cycle True and all three commands False, amber status. |
| Hand withdrawal/restoration | small-drill-r4-hand-loss.png, -loss-held.png, -hand-restored.png, -restored-held.png, -restored-signals.png | Midstroke pose remains held after withdrawal and restoration. Settled restored Signals: both hands True, both limits False, cycle False and all motion commands False. Restoring a hand alone does not restart. |
| Midstroke Start and recovery | small-drill-r5-midstroke-start.png, -recovery-reset.png, -recovery-repeat.png, -recovery-signals.png | Fresh Start at midstroke remains inactive with both limits False. Reset restores top and clears hands; fresh qualified cycle completes at top with cycle True and commands Off. |

## Fixture policy, separate from observations

The actual ixture-new-binding-lab-2-16-v3.rpproj.json resets its qa_cycle latch on hand/workpiece qualification loss. Start requires top feedback. Therefore restoration alone and Start at midstroke remain inactive; App Reset followed by hands and fresh Start is the tested recovery. App Stop retains the cycle state and can resume on Run with retained qualifications, which is a distinct path. Retract is a ladder-owned output, not an exposed operator retract button.

## Disposition and limits

The tested Lab 2.16 subcase of QA-PLANT-001 now has screen evidence for command-driven descent/return, endpoints, active App Stop/Run, hand-loss arrest/no automatic restart, Reset and repeat. The physical Start route is also exercised. No new defect observed in these cases. Exact dwell/scan timing, spindle rotation rate, simultaneous input ordering, material cutting and safety-rated performance are unproven.

Derived evidence: postbackup-small-drill-contact.jpg, postbackup-small-drill-points.jpg. Originals remain authoritative.
