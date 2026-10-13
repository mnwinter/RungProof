# Dot cabinet legend source review V10

2026-10-11. APPROVED FOR BUILD AND RENDER VERIFICATION; no blocking finding in the bounded legend patch.

Compared the exact mount-legends-v9 before copy against current SceneComposer.ControlInstallations.cs and read mount-legend-visual-fix-v10.md. The sole patch block concerns existing OperatorFaceLabel presentation for non-bench selected cabinet controls.

The plate's accumulated transform is composed into the legend parent's coordinate frame before calculating bounds. The existing Label3D is centred horizontally/vertically on that measured plate, and its position is moved 3 mm beyond the plate's positive-Z face. Existing authored cabinet heads use this front convention; later whole-device yaw/scale applies equally to label and plate. The imported text's left anchor no longer controls the centred live label position.

Actual text/font size is measured with the selected font or fallback. Maximum line width and summed line heights set a conservative pixel scale within plate width/height inset margins, capped by the prior presentation size. Empty text remains finite through minimum dimensions; text itself is never shortened. Multiline line spacing/outline and projected camera readability remain actual render checks, not guaranteed by Font.GetStringSize alone. Current authored labels are single-line and OutlineSize=0 in their builders.

The same named Label3D, physical plate, equipment root, configured action, controller children and binding paths remain. The patch changes no collision or input target creation, simulation values, source tags/config or runtime behavior. It does move a child label and can slightly alter retained mesh-free visual bounds only through its position; meaningful label readability requires new exact-candidate renders.

Require new Chicken LOAD & WEIGH/PRINTER READY/DATA VALID and drive/wastewater operator frames confirming all edge characters fit and no plate/head interference. Existing generic START and unlabeled feedback semantics are deliberately unchanged and must remain explicit limitations. Primary screen/input remain reserved; this review used files only.
