# Post-backup representative Viewer navigation

Independent native screenshots only; no application/source/Git changes.

- small-navigation-r2-viewer.png shows the Reusable Equipment Gallery title, matching gallery Inspector and rendered gallery in Viewer layout.
- small-navigation-r3-viewer-demo1-settled.png shows Demo 1 - Press Count Starter selected in the Viewer list, the Demo 1 header, rendered press/count/reset scene and matching underlying Inspector Lab 4.1 - Press-Count Lamp. Runtime is running with a loaded ladder. The Demo title/underlying lab name are distinct labels, not evidence of a wrong scene.
- small-navigation-r4-viewer-lab21-settled.png shows Lab 2.1 - Workstation Call Lamp selected, matching header/Inspector identity and rendered workstation. Runtime is stopped with ladder loaded.

These settled images support representative Viewer navigation gallery→authored demo→non-demo lab, including scene content/Inspector identity alignment. They do not prove every Viewer item, state retention policy or every menu path. The bounded Browser and draft-choice evidence added below is separate. No new defect observed in this bounded sequence.

Derived compact evidence: postbackup-small-navigation-contact.jpg. Original settled frames are authoritative.

## Representative Browser demo route and unsaved-workspace choices

At 75 percent interface scale, r8-scale75 exposes the Browser Demo 1 row. The r9 Browser request opens an Unsaved scene workspace prompt. Actual r10-cancel-preserves-count45.png retains Lab 4.5, A2/B5, window_ready False and running controller after Cancel. A second Browser request opens the same prompt with Discard changes/Cancel/Save choices (r10-reprompt.png). After actual Discard, r10-browser-demo1-settled.png shows Demo 1 selected, press-count starter geometry, count0/pulseFalse/resetFalse/thresholdFalse and local runtime RUNNING on the demo ladder.

This supports this specific Browser→Demo1 route plus Cancel preservation and Discard continuation for the QA-owned loaded draft. It is not a broad navigation pass; ordinary Browser lab selection and Save/reopen were still pending at that intermediate review; the new evidence below resolves the representative cases. No new defect is observed in these choices.

## Ordinary Browser lab and Save/reopen

The new Browser Lab2.1 request opens the unsaved-workspace prompt (r14-browser-lab-save-prompt.png). Save uses the unique QA-owned postbackup-navigation-count46-owned.rungproof.json target rather than overwriting an existing fixture. The resulting r15-after-save-navigation.png shows the requested Lab2.1 workstation, matching title and STOPPED runtime. This establishes a representative ordinary-lab Browser route after Save.

Actual r18-restored-workspace.png shows reopened Lab4.6 plus the Workspace dialog naming that exact saved file, lab-4-06-multi-press-confirmation and saved ladder draft restored/stopped. r19-restored-ladder-after-ok.png independently shows QA_Lab406 Main with the count CTU/reset networks in the offline editor. After Run, r21-restored-a2-b3-signals.png directly shows A2/B3, confirmation_valid True, both button inputs False and the physical green segment illuminated. Thus the saved integrated scene/ladder reopens stopped and can execute the same bounded functional check after explicit Run.

Representative Viewer demo/lab, Browser demo/lab, Cancel, Discard and Save/reopen now have native evidence. This is not every item, every navigation order, preservation of live counter values across reopen or a global navigation pass. No new defect observed in these tested routes.
