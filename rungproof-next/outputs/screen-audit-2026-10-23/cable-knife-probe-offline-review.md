# Cable knife Stop observer: offline validation

`cable_knife_stop_probe.py` SHA256 `e58360794ef2c3d42b9136a92a151a6ec5017d3a171ede9853a8529c69dff326` is frozen for root review/invocation. No live UI or bridge calls were made by this agent.

Root must first ground the cable scene/unchanged reference, physically START, and leave the actual App Playback menu open during feed. The helper performs no setup, menu opening, Start, Run or Reset. It verifies the visible Stop menu, then acts on one actual same-image RUNNING witness: cutter_fireTrue, cut_doneFalse, cutter_homeFalse,0<cutter_position<1,length_reachedTrue,feed_runFalse,cut_faultFalse. Exact normalized row names and values come only from the fixed2x native Signals crop. Numeric OCR corrections are confined to numeric fields; decimal punctuation is retained. Missing/conflicting values remain unproven.

Before the one Stop request it rechecks Stop menu text from that target image and image age≤0.5s after pinned-session health. It retains immediate,0.5s-settled and an additional1s-held native originals/crops/OCR/hashes/action witnesses. It checks stopped scan retention and whether cut_done remainedFalse, but leaves physical pose/phase acceptance to root. A target can advance before the OS input lands; a stopped completed cut cannot be credited as the outbound interruption.

Default operation budget is25s with3s reserved for post-Stop observations, bounded OCR cleanup and the11:00UTC cutoff. A missed/ambiguous/expired target remains a pending QA case. It is not an application failure. An uncertain Stop request is never retried automatically.

Offline evidence:

- `cable-probe-offline-native-home-1792735259930407600/result.json`: actual `cable-r1-engineering-home.png` reads all seven required fields correctly, position0, fireFalse/homeTrue/lengthReachedFalse, RUNNINGscan204; targetFalse and menuFalse (the saved menu is closed). OCR worker exits0. Optional stock_remaining_m name was unreadable and remainsnull; the helper does not infer stock.
- `cable-probe-offline-guards-1792735259927636600/result.json`: synthetic outbound0.4 positive; return/bottom/home/feed/fault/zero/one/negative-position, each missing required field, conflicting position and cutoff negatives pass. These are parser/guard checks, not native outbound proof.

After native Stop review, existing reference policy still requires separate Run-alone held observation and a fresh physical START for retained cut/home completion. The helper does not operate those controls.
