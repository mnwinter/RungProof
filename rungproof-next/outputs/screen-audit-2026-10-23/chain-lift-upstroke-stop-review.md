# Chain lift upstroke application Stop / explicit recovery

Independent original-pixel/action review, no UI input. PID12768/native1021×660; Lab4.9 Chain-Drive Lift/local runtime/PLC disconnected. Thirteen cited originals rehash-match native action/helper manifest records.

## Qualified upstroke and hold

[observe015](chainlift-upstroke-stop-r1-1792736383529056200/observe-015.png), scan15579, and [observe016](chainlift-upstroke-stop-r1-1792736383529056200/observe-016.png), scan15594, show carton_on_liftTrue, lift_enableTrue, cycle_activeTrue, both lift_home/upperFalse, chain_run/lowerFalse. Actual supported carton/tray rises against fixed gantry members between these frames. The single Stop is applied while this upstroke is active.

[Settled](chainlift-upstroke-stop-r1-1792736383529056200/stop-settled.png) and [held](chainlift-upstroke-stop-r1-1792736383529056200/stop-held.png) show STOPPED15615, same mid-height tray/carton pose, carton_on_liftTrue, both endpointsFalse; chain/lift_enable/lower/activeFalse and completionFalse. Thus real supported intermediate hold is observed, not an idle endpoint assertion.

[Run alone](chainlift-r1-run-alone.png), scan15646, and [Run held](chainlift-r1-run-alone-held.png), scan15772, remain inactive with the same pose/endpoints. Restoring scans alone does not reseal this controller's cycle. [Fresh physical Start](chainlift-r1-fresh-start.png), scan15826, acknowledges momentary Start and restores lift_enable/cycle_activeTrue; actual tray resumes rising. No Reset was needed.

## Measured upper, transfer and home result

[Upper](chainlift-r1-recovery-upper.png), scan16002: actualupperTrue/homeFalse, carton_on_liftTrue, chain_runTrue, lift_enable/lowerFalse; tray physically raised to receiver height. [Transfer](chainlift-r1-recovery-transfer.png),16280, shows carton moving onto the upper receiver, beamBlockedTrue/receiverOccupiedTrue, carton_on_liftFalse. [Descent-named](chainlift-r1-recovery-descent.png),16406, and [Final-named](chainlift-r1-recovery-final.png),16632, still show upperTrue/lowerFalse, carton progressing along supported upper receiver; they **do not prove descent or completion** despite filenames.

[Later](chainlift-r1-recovery-later.png),19520, and [Later held](chainlift-r1-recovery-later-held.png),19646, show carton_at_receiverTrue/receiverOccupiedTrue, carton_on_liftFalse, actualhomeTrue/upperFalse, cycle_completeTrue, transfer_faultFalse; all chain/lift/lower/active commandsFalse. Actual carton rests on the upper receiver belt while tray is visibly back at lower home. No native frame samples lowerTrue or an intermediate moving downstroke; observed geometry proves the raised→home endpoint change, not continuous descent timing.

**Q19 closes for qualified upstroke Stop/held, Run-alone inactivity, fresh physical Start recovery through supported upper transfer to finalhome/completion.** The supplied finite scope is met without adding a new sampled-downstroke gate. No failure demonstrated. No safety-rated behavior, all-phase acceptance, or filename-based descent proof is claimed.
