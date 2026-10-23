# Post-backup native verification checkpoint

Application source and Git backup remain at41188c70. Only local QA logs/new evidence changed after backup.

- B5 jug: normal transport/fill100/high/completed discharge, active Stop holds1.81818% fill, Run resumes completion, Reset clears fill/high/completion, fresh repeat reaches100/completion. See postbackup-jug-r1..r4 and independent postbackup-jug-review.md.
- B5 CNC: actual physical START runs pickup/robot/stock movement to outfeed endpoint; cycle_complete, machining_complete, part_at_exit True; infeed/outfeed/cnc/robot/grip commands Off verified in separately scrolled native Signals. Exact active cut/door/clamp chronology and lifecycle remain unproven. See postbackup-cnc-r1 and independent review.
- CNC lifecycle r2 did NOT start: require_canvas rejected before any input. Native desktop changed to1024x768; original client became1021x600, then restoring1600x900 made it larger than virtual screen. Actual Win32 observation virtual[0,0,1024,768], appPID44956, current frame[-6,20,1609,957]. No wrong-coordinate inputs sent by rejected helper. Escape focus probe accepted separately; no simulator state change.
- Current v9 bridgePID37496/session9220 remains live, pinned44956. Input/capture reject offscreen1600client. Future UI work must establish a fresh fully visible native client and new grounded coordinates/QA canvas; do not reuse1600coords or synthesize/render hidden screenshots.
- Full77-scene goal remains incomplete. Backup commit was verified pushed before deadline; postbackup evidence is local and uncommitted.
