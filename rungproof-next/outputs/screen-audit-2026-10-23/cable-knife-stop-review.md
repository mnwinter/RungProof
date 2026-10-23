# Cable knife outbound AppStop / explicit-resume review

Independent original-pixel review; no input/state queries. AppPID12768/native1021×660, actual Lab4.12 Cable Cut-Length Cell/local runtime/PLC disconnected. Eight cited originals rehash-match native-ui-actions/helper result manifest. No application defect inferred from OCR.

[Controls/home](cable-r1-controls-and-knife.png) shows actual knife raised, cutter_homeTrue/position0, stock10/length0. [Qualified observe033](cable-knife-stop-r1-1792735298190059300/observe-033.png) shows RUNNING11305, cutter_fireTrue/position0.15/homeFalse/doneFalse, cycle_activeTrue, feedFalse, length3/stock7. A single application Stop follows; immediate prior-render frame is not accepted-Stop proof.

[Settled Stop](cable-knife-stop-r1-1792735298190059300/stop-settled.png) and [held](cable-knife-stop-r1-1792735298190059300/stop-held.png) show STOPPED11326, **actual position0.675/homeFalse/doneFalse**, fire/feed/activeFalse, speed0, measured_length3 and stock_remaining7 (visible original despite OCR missing its name). Actual blade hangs below its raised home and remains identically positioned in the held pair.

For independent geometry comparison, [blade comparison](cable-independent-blade-comparison.png) contains exact original crops(470,305)..(630,345), enlarged5× in rows: home, observe033, settledStop, heldStop, finalhome. It clearly shows outbound downward blade displacement and fixed partial stroke in the two Stop rows; the final blade returns to original raised pose. No synthetic renderer or state API was used.

[Run alone](cable-r1-run-alone.png), scan11356, and [Run held](cable-r1-run-alone-held.png), scan11481, show actual playbackRUNNING but blade remains0.675, fire/feed/activeFalse and completionFalse. Thus restoring application scans does not automatically resume this fixture's latched cycle.

[Fresh physical Start](cable-r1-fresh-start.png), scan11531, follows logged cabinet Start(641,420): accepted momentary START/RESUME footer; cycle_activeTrue, blade0.875, cut_doneTrue while homeFalse. cutter_fire alreadyFalse in this sample: the stroke/return model can continue after the firing command has ended. Do not confuse an OFF actuator command with completed/home status.

[Resumed complete](cable-r1-resumed-complete.png), scan11657: cutter_homeTrue, cut_doneTrue/cut_completeTrue/cycle_completeTrue, cut_faultFalse; length3/stock7 retained; feed/fire/activeFalse and speed0. The final cut-length material remains on the visible output support path; this wide view does not provide a precise geometric length measurement. The tiny cutter_position1.46758E−15 residual is consistent with numerical zero and homeTrue, not a separately established precision fault.

**Q03 closes for actual outbound knife qualification→Stop/held→Run-alone inhibited→fresh physical Start recovery.** Exact input chronology uses one Stop, then Run only, then a fresh Start; no Reset/backend manipulation. This is the fixture's documented explicit-resume policy, not a requirement that every lesson auto-resume. No universal phase/safety acceptance claimed.
