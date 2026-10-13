# RungProof realism review pack — V24c

The authorized presentation update covers all77 scenes/labs. This is a bounded visual/control review pack, not release qualification or proof of every program state.

Start with all77-human-scene-review-checklist-v24c.csv. Each row has the exact scene ID, expected equipment count and actual full/split V22b images. Mark your result in the HumanResult column and record notes. The V24c closure ledger keeps original candidate hashes for earlier geometry, materials and focused verification; it does not relabel historical images as current.

## What to review

1. Confirm the whole plant and field sensors/actuators are present, with plausible bench/cabinet support and useful room context.
2. In the app, use ordinary right-drag orbit, middle-drag pan and wheel zoom to inspect each control group. Dense9.3 has readable manual/numeric/results/status group views; showing all13labels at detail size simultaneously is not supported by the tested framing.
3. Check the displayed scene I/O point before and after an authored control action, then Reset. Tag-only exercises may have no ladder solution loaded; clicking an input does not imply PLC outputs must move. Do not treat missing authored lesson logic as a visual mounting failure.
4. Record scene/build, view, loaded program or blank tag-only state, exact action, expected/actual result, point values and screenshot. Include a saved project when relevant. Preserve the original file before diagnostic edits.

## Known limits

Some overview words need zoom. Dark shadows, subtle broad-surface textures, cropped contextual plaques and minor tabletop grain aliasing are retained presentation limits. Physical keyboard/mouse acceptance, general discoverability/DPI and live PLC behavior were not tested here.

An intermittent managed/native Handle error remains unresolved. A historical controlled baseline logged161errors at load529; two other arms logged0. Those workload observations do not establish a general crash rate or harmlessness. Save learner work before scene switching; after a failure, relaunch and reopen the last saved project. Recovery of unsaved work is not established.

RUN-RUNGPROOF-NEXT.cmd is the existing developer launcher; it builds/imports on each launch and therefore is not an immutable packaged candidate. This pack does not claim a tested distributable or installer. No publishing, paid assets, live PLC or OS input takeover was performed.

## Evidence

- environment-all77-authoritative-closure-ledger-v24c.json: all77 source/versioned evidence associations.
- material-capture-integrity-v22b.json and material-render-review-v22b-part1/2/3.md:154 current material views individually reviewed.
- environment-dot-numeric-cycle-review-v21bc.md:12 numeric input routes and Reset.
- environment-dot-retained-head-review-v23.md and environment-dot-crossing-collective-review-v23bc.md: retained heads, crossing, service door and pickup.
- dense-group-evidence-v24c.json:8 ordinary-camera dense station views, all exact image hashes and native checks pass; root individually inspected all8.
- environment-dot-dense-readability-criterion-review-v24.md: why no arbitrary bench redesign was required.

Final integrated Dot disposition: bounded studio APPROVAL in environment-dot-integrated-realism-disposition-v24c.md. Dense logical groups accepted in environment-dot-dense-logical-group-review-v24c.md. Human final approval is not implied by studio verification.
