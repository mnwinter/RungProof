# Post-backup Lab 11.11 native elevator review

Independent review of original small-client screenshots; no source, tracked reports, application state or Git changes.

## V2 normal roundtrip

- Frames small-elevator-r1-upper-1.png through -5.png show actual car ascent, position about 33.67 to 96.67 percent, up command True/down False, upper and lower feedback False.
- Frame -6.png (scan 2007) directly proves actual upper feedback: at_upper_landing True, at_lower_landing False, car_position_pct 99.9999, both direction commands False. The car is visibly at the upper landing.
- Frames -7.png through -13.png show descent, down command True/up False and changing physical car height. Frame -10.png is 55 percent; -13.png is about 11.33 percent.
- Frame -14.png and subsequent held frames show lower True/upper False, both commands False and position 4.98258E-06 percent. The car is visibly at the lower landing. This floating residual is not an endpoint failure.
- All sampled Signals frames show direction_conflict False. No simultaneous direction command appears in these samples.

The V2 QA ladder adds a one-second upper dwell after actual upper feedback; this is a test parameter, not a general scene timing requirement. Failure of the original OCR helper to recognize a small label does not negate the readable original upper feedback frame.

Normal ascent, observed upper dwell, descent and lower hold have native proof. Exact dwell duration, unsampled instantaneous direction conflicts and descending interruptions are not yet credited here. Earlier ascending interruption evidence remains separate.

Derived compact evidence: postbackup-small-elevator-upper-contact.jpg. Originals remain authoritative.

## R3 active descending door loss

The actual before-loss Signals frame shows car_position_pct 81.9999, down True/up False, both landing feedback False and doors_closed True. After physical door input loss, settled small-elevator-r3-loss-signals.png shows doors_closed False, position 56.6666, both commands False and both endpoints False. This proves interruption during actual descent, not a scheduled assumption about phase. Door-loss/held floor images change framing, so their absolute pixel coordinates alone are not credited as a measured freeze. Restoration/resumption and a second settled position observation remain pending.

## R3 settled hold and R4 restoration

Independent pixels in small-elevator-r3-settled-held-signals.png retain position 56.6666 after the additional held interval, doors False, both direction commands False and neither landing feedback. Car pose agrees with the settled loss frame. small-elevator-r4-lower-signals.png after restoration shows all three qualifications True, lower True/upper False, both commands False, direction_conflict False and position 4.98258E-06; car is physically at the bottom. Thus the upper-feedback and active descending interruption/held/restored-return gaps are closed for this V2 fixture. The earlier pending statements describe intermediate review state and are superseded here. All phased combinations of qualification losses, exact timing and industrial elevator safety performance remain outside this evidence.

Additional compact evidence: postbackup-small-elevator-held-return.jpg.
