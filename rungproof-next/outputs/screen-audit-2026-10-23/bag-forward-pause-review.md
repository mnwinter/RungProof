# Bag indexing forward PAUSE withdrawal and normal return

Independent actual-pixel review; no UI input or runtime queries. PID12768/native1021×660, actual Lab5.9 Bag Indexing Conveyor/local runtime/PLC disconnected. Eight cited original SHA256 values match native-ui-actions.jsonl. Existing fixture's pause_clear policy gates return eligibility/return travel, not the current forward leg; no universal stop-on-pause assumption is imposed.

| Original | Actual public state and geometry |
|---|---|
| [Accepted clear](bag-r1-pause-clear-accepted.png) | scan6797; explicit accepted maintained pause_clear=TRUE footer, actual SignalsTrue; entryTrue/position−2, runFalse. Bag rests on entry belt. |
| [Before withdrawal](bag-r1-forward-before-block.png) | scan8029; pause_clear**True** is directly readable in original despite OCR omission. position−1.02/speed+0.5/runTrue/reverseFalse; actual bag advanced along same-camera belt. |
| [After withdrawal](bag-r1-forward-after-block.png) | scan8083; accepted maintained pause_clear=FALSE footer; position−0.48/speed+0.5/runTrue/reverseFalse. Forward motion continues. |
| [Blocked moving](bag-r1-forward-blocked-moving.png) | scan8209; pauseFalse, position+0.78/speed+0.5/runTrue; actual bag farther right, still belt-supported. |
| [Forward exit](bag-r1-forward-exit.png) | scan8485; actualexitTrue/entryFalse, position1.28/speed0/runFalse/reverseFalse; bag supported at exit. |
| [Clear for return](bag-r1-clear-for-return.png) | scan10804; pauseTrue restored and accepted, but still stopped at exit1.28; selector restoration alone does not start return. |
| [Fresh return Start](bag-r1-return-start.png) | scan10879; accepted physical START/RETURNRESTART, reverseTrue/runTrue/speed−0.5, position1.04; actual bag moving back left. |
| [Returned entry](bag-r1-return-entry.png) | scan11305; actualentryTrue/exitFalse, position−1.58/speed0/runFalse/reverseFalse; bag visibly supported at entry. |

Action log/grounded controls identify actual START(547,369) and PAUSE selector(556,370); the wrong earlier(566,379) tap did not change pauseFalse and is QA target calibration, not an application failure. The accepted clear frame and immediate prewithdrawal original establish maintainedTrue before the transition. Actual bag motion is visible against unchanged belt/sensors, not inferred from empty-belt texture.

**Q05 closes for forward-leg pause-clear withdrawal, continued forward endpoint stop, restoration-alone idle and fresh-Start normal return.** This exact behavior matches the frozen learner fixture's asymmetric forward/return policy. No application defect or general safety-stop claim demonstrated. Return-leg pause withdrawal was separate prior coverage; no new arbitrary permutation gate is introduced.
