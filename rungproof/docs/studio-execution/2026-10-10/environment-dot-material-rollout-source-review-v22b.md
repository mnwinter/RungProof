# Dot scoped material rollout source review V22b

Human approved current two-room direction; remaining75 rollout permitted, whole77 acceptance not granted. No source edits/build/engine by reviewer.

Compared17 environment partials against docs/material-rollout-v22-before: each is exactly original text with StudioSceneMaterials.Create/CreateDetailed calls replaced by EnvironmentSurface/EnvironmentDetailedSurface. No geometry/IDs/pose/layout/hex/runtime edits in those files. ReferenceMaterials wrapper applies approved recipes only to paint/plaster/concrete/resin; baremetal/wood/cardboard/rubber/glass retain exact prior Create versus CreateDetailed distinction. Global factory/device/pilot semantics untouched.

Wrapper preserveCatalogColor:true uses newColor(hex).SrgbToLinear, matching original StudioSceneMaterials.Create/CreateDetailed albedo convention. Existing exact-two ReferenceRoleSurface calls defaultfalse preserve their approved raw-sRGB recipes. Cache key includes preserveCatalogColor to prevent cross-contamination. This is necessary because V22 direct swatch treatment visibly washed out old calibrated palettes; preserve failed previews rather than claiming them approved.

Bounded source scope approved. Actual per-scene material/color/texture/readability review and native unchanged geometry/runtime/restore evidence required before rollout acceptance. No automatic scene recolor to pilothex, global lighting/exposure change or patterned background authorized. Full75 material visual delta coverage justified by broad material change, while historic geometry/input evidence remains separately attributed. Handle OPEN/all77 ACTIVE.

## Three actual V22b samples

Individually opened pedestrian-crossing, Chicken-label-print and sum-and-counter-function scene PNGs. Crossing road/white crosswalk distinct against muted siteground; rust hut color retained. Chicken sage floor and pale yellow/cream walls retain color contrast without blank-white washout; existing cupboard/scale/printer/cabinet visible. Sum/count wood floor remains visibly grained, cream/plum wall roles retained, bench/heads complete. No new visual texture or support blocker in these samples. Texture relief stays subtle at overview distance; physical legends tiny as expected and prior operator evidence remains separate. These three are sampling approval to continue broad authorized capture review, not75scene artistic completion or direct comparison against unviewed V22 failure frames.
