# Explicit learner contracts: drill and robot CNC

Read-only proposal. No source/UI/build changes. These are proposed simulation interfaces, not undocumented hardware behavior or safety functions. Current help explicitly promises a normal down/dwell/retract drill cycle, but CNC help explicitly describes an offline reference and unverified normal controller operation. Do not silently convert reference scripts into PLC solutions.

## Lab2.16: separate spindle and feed directions

Preserve PLC `drill_run` as spindle enable. Add PLC BOOL `drill_feed_down` and `drill_retract_up`, initialFALSE. Preserve PC `start_drill_request`, both hand requests, workpiece_present, drill_at_top/bottom. New PC BOOL `drill_motion_inhibited` reports a conflicting direction or a requested descent without actual workpiece/two-hand/spindle permission. SIM feed position is derived, not a controller input. Existing245mm geometry stroke and1.4s full-travel may be documented model parameters; no automatic spring return.

Plant rule: actual position integrates DOWN only if down AND NOT up AND both hands AND workpiece AND spindle; UP only if up AND NOT down (explicit motorized retract, not inferred from spindleOFF). Both commands/none hold. Native Stop freezes all positions and spindle. Actual endpoint switches derive from position, not phase timers. Workpiece disappearance/hand release inhibits descent immediately; it must never fabricate bottom/top/completion or reverse automatically. A command inhibit does not rewrite a PLC output.

Learner ladder phases: START only at actualtop with required inputs -> spindle spinup TON0.35s (explicit QA timing) -> feed until actualbottom -> dwell TON0.35s -> retract until actualtop -> spindleOFF and cycle reset. Start request never calls a scene sequence. Loss of hand/workpiece cancels active cycle and drops commands; no silent restart. For recovery, Reset returns authored safe home in this educational simulator; a later dedicated RECOVER/RETRACT station would require a separately documented operator contract. Do not fake a recovery route through hidden tag edits.

Migration: retain all old bindings and standalone reference unchanged; old saved controller gets new output tags through EnsureSceneIoTable but, unless it drives them, spindle may rotate with no feed. That is deliberate explicit compatibility, not an invented auto-cycle fallback. Help must state old one-output programs need learner logic updates for motion. New fixturev3 consumes actual limits and documents dwell parameters. Add tests for down/up/no/conflict, hand loss, exact stroke/limits, Stop midstroke, reset, and no autonomous retract.

## Lab2.24: explicit motion and process interface

Do not define robot_run as run-the-reference-sequence. Retain it only as robot servo/motion enable. Retain infeed_run/outfeed_run and cnc_run. Add the following explicit simulation commands and feedback:

|Owner|Proposed point(s)|Semantics|
|---|---|---|
|PLC REAL|robot_target_x/y/z|World-metre tool targets within declared simulated workspace; defaults actualpark. No waypoint auto-advance.|
|PLC BOOL|robot_grip_close|Maintained gripper target. Opening releases only at actual supported belt/vise location; unsupported release reports fault and does not teleport stock.|
|PLC BOOL|cnc_access_open|Maintained access-door/tool-clearance request; existing delivered sliding doors and tool clearance adapters follow actual normalized position.|
|PLC BOOL|cnc_clamp_close|Explicit educational vise clamp target; renderer and feedback follow actual clamp. Requires separately authored movable jaw geometry; no fake clamp-confirmed flag.|
|PC REAL|robot_actual_x/y/z|Actual joint-solved tool position.|
|PC BOOL|robot_at_target, robot_grip_confirmed|Tolerance on actual tool target; actual gripper/load attachment.|
|PC BOOL|cnc_access_is_open, cnc_access_is_closed, cnc_tool_clear, cnc_clamp_closed|Actual mechanism endpoints.|
|PC BOOL|stock_on_vise, stock_on_outfeed, robot_clear_of_enclosure|Geometry-derived support/clearance, not PLC phase aliases.|
|PC BOOL|cell_motion_inhibited|Blocked unsafe geometry/motion request; never rewrites command image.|

Existing blank_at_pickup derives from the actual moved blank intersecting pickup beam. cnc_ready becomes actual closed access+clearrobot+clampedstock+noactivecut readiness; its initialTRUE must be reconsidered because empty home is not ready-to-cut (introduce separate cell_ready if needed rather than overloading semantics). machining_complete derives from actual process exposure under cnc_run with required actual interlocks;2s is an explicitly labeled simulation cut-duration parameter, not hardware machining proof. A new machine cycle resets that process feedback only under an explicit cycle contract, not arbitrary timer aliases.

LAD owns every target transition: feed until realpickup; requestaccess and await actualopen/toolclear; command corridor waypoint then aboveblank/downblank; closegrip and await actualattachment; lift/corridor/frontopening/abovevise/lower; opengrip only at supportedvise; withdraw/park; clamp and awaitactualclosed; closeaccess and awaitactualclosed/robotclear; cnc_run until realprocesscomplete; openaccess/unclamp; explicit pickup and reverse corridor targets; release supportedoutfeed; withdraw/park/closeaccess; outfeed until realexit. Retained reference coordinates describe simulated scene waypoints only, never robot hardware positions.

This is a larger interface than a one-BOOL repair, but it exposes decisions to ladder and makes genuine feedback possible. Minimal staged delivery is first honest infeed-to-pickup, then actual access/clamp mechanisms and robot single-target moves, then full learner ladder. Each stage must remain honestly incomplete until native evidence exists. Do not call full tending fixed after only infeed movement.

Backcompat: preserve existing point names/owners, old saved projects still load, new points auto-add without networks. Keep standalone reference preview visibly labeled reference-only. Opt-in loaded-controller plant must never call StartSequence/controllerSequenceCommand or apply reference step output presets. Old robot_run-only programs enable servo but cannot choose target/grip/door/clamp and therefore remain at safehome. Additive declarations plus updated help/fixture are required. Runtime guards, pure physics tests and real native geometric/support/interlock checks are separate acceptance evidence.
