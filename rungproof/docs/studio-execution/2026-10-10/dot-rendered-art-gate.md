# Dot rendered art gate

Verdict: **PASS for the sampled five-demo 1600-pixel scene-view presentation; broader usability qualification remains open.**

Dot independently viewed these retained actual PNG files using filesystem image inspection, without activating the app or sending shared input:

- `.tools/studio-verification/scenes/lab-4-01-press-count-lamp-after-final.png`
- `.tools/studio-verification/scenes/lab-5-01-delayed-lamp-after-final.png`
- `.tools/studio-verification/scenes/scene-1-conveyor-stop-after-final.png`
- `.tools/studio-verification/scenes/lab-9-11-pallet-counting-after-final.png`
- `.tools/studio-verification/scenes/lab-11-13-xy-palletizing-after-final.png`

All paths are relative to the isolated `rungproof/rungproof-next` project. Root reports matched requested scene IDs, clean capture logs and corresponding before frames; Dot inspected the five after frames directly, not the complete before-frame set.

Industrial context is visibly present: coherent service bays for the first two lessons, a dispatch recess behind the conveyor, staging context behind the pallet lesson and a low service wall/cabinet behind the XY cell. Bounded slabs, restrained steel/concrete colors and warm fixture lighting support process silhouettes. Background walls are plain; the earlier repeated diagonal shadow bands are absent in these sampled frames. New setting geometry does not obscure the visible photoeye, conveyor/carton or principal machine silhouette in the sampled views.

The XY cell's lower front control/gripper edge remains behind the existing I/O dashboard. Root identifies the same clipping in before and after frames. This gate does not certify full-machine visibility, interaction reach or continuous travel clearance. Existing tiny decorative bay titles are not relied on for instructions.

Decorative ventilation is independently clocked and gated by reduced motion/playback in reviewed source. Static frames do not prove its animation timing or every lifecycle transition. No PLC/output behavior, live transport, safety or commissioning claim follows.

Dot also independently inspected `challenge-starter-v4.png` and `challenge-starter-1280-v4.png`: the corrected challenge identity, ordinary palette, initial network and copied font16 LIVE/OFFLINE screen-space states are readable. The earlier tiny world text is removed. At 1280, the initial output coil requires horizontal scrolling. Actual keyboard access, dialogs/focus, expanded hints/results, supported logical-size/DPI matrix and assistive technology remain separate gates.

Earlier wrong-scene, placeholder, script-error, crashed-starter and unreadable-label captures remain rejected; their presence is not acceptance evidence. This sampled art pass is not a public-release or distribution approval.
