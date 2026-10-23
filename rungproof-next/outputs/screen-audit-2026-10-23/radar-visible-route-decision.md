# Radar inspection: a visible presentation command

Source/design review after the 77-entry compilation. No application or scene changes, UI input, engine execution or PLC connection. This refines the existing QA-UX-CANDIDATE-003 access finding; it does not create another issue or prove a renderer failure. See [route trace](radar-inspection-route-analysis.md).

## Recommended narrow route

Expose **View > Tank inspection view (ghost shell)** for the existing `tank-radar` scene/action. A viewing aid belongs beside other view commands. A physical tank hitbox or PLC-looking pushbutton would give it the wrong meaning and could obstruct existing pump/drain targets.

`SimulatorShell` already owns the View popup, current scene/runtime, and `SceneActionRequested` event. `Main.cs:637` already routes that event through `ExecuteSelectedControllerAction`. A new scene-specific menu item can use this existing event with `toggle-inspection`; no new transport, controller variable, direct point assignment or new general action framework is required. These are proposed additions, not existing APIs.

Keep the item conditional on the exact scene and declared action/BOOL SIM point. Reserve its menu ID after checking the existing View IDs and interface-scale range. Read the checked state from the actual runtime `tank_inspection_view` value; never retain a second optimistic UI Boolean. Refresh visibility/check state when scene/runtime bindings or relevant state change, including Reset. Sending a request is not proof the dispatcher accepted it.

Do not reveal the entire hidden `_operatorActions` parent. That bank contains other scene commands, lifecycle aliases and generic tooltips that describe toggles as PLC inputs. Making all of it visible would create a wider UI/ownership change. Do not call `ProjectRadarInspectionView` directly from the menu: the existing scene action remains the owner of its SIM state and material projection.

## Mode and lifecycle choices

- The ordinary local scene, including a stopped controller, should use the existing action dispatcher. The action is a view aid; an App Run/PLC machine Start is not its prerequisite. Local **scan-inspection Pause** is a different state: the current generic dispatcher rejects plant changes while paused. Preserve that existing gate for the smallest route repair unless paused presentation inspection is explicitly included in scope. If included, add a narrow presentation-only exception with a non-plant contract; do not exempt all SIM toggles.
- Preserve the current Stop/Reset semantics. Stop may retain the viewing selection while stopping process commands; Reset restores the declared initial view. Do not clear pump/drain commands, reset the controller, move the tank or alter material geometry merely to refresh a menu checkmark.
- Scene change must remove/hide the item and discard its old runtime reference. Returning to the scene must derive state from that new instance. An always-visible stale checkmark is not harmless when the action targets another scene.
- Do not add PLC mappings or external transport. Reuse the existing owner checks. Any external-mode verification should remain an offline adapter exercise unless separately authorized; none was performed here.

## Finite acceptance

1. In a freshly identified local radar scene, show the ordinary opaque tank and visible View item. Use the actual menu once: ghost shell/roof and internal surface become visibly useful. Use it again: original opacity returns. The checkmark follows the actual result each time.
2. Keep process conditions steady during the pair. The level/measurement values, controller execution identity and plant transforms must remain unchanged by the viewing request. Existing renderer tests are supplemental; hidden `ExecuteAction` calls do not prove the menu works.
3. Reset while inspection is selected, then change away and return. Check initial view and menu visibility/state; one neighboring scene has no misleading radar item. Verify existing physical pump/drain targeting still works.
4. If Pause support was deliberately included, prove only the view changes while scans/plant remain held. Otherwise document the existing blocked outcome honestly; do not claim Pause acceptance from stopped-mode success.

Correct the help to name the chosen visible route. Removing an advertised feature is an alternative product decision, but exposing this already implemented viewing aid best preserves the existing lesson intent. The exact material result remains a future native acceptance gate.
