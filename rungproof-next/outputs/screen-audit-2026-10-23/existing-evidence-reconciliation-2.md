# Existing-evidence reconciliation after checkpoint824f9b56

No UI input, application edits or Git/D writes. Seven controls P cells were bookkeeping-only: grouping ENABLE/CLEAR/LOAD; cable START; chain-lift START; coating START/readiness; traffic READY/clears/queues; inbound-tote START; bag START/PAUSE. Exact native reviews already show their physical responses. Current scoped cells now cite those observations; historical P remains under supersedes.

No remaining READY phase case was silently closed. Specifically:

- `fix-plant211-r1-scanner.png` shows the tote across the scanner region, but does not display tote_at_scannerTrue or an accepted stopped-identification observation. The clearing frame is a later changed pose. Q12 stays open.
- `lab509-r1-start-forward.png` directly reads pause_clearFalse/conveyor_runTrue/reverseFalse/speed0.5, proving forward travel does not require CLEAR. `clear-alone` shows pauseTrue and no restart at EXIT. The actual midforward CLEAR transition requested in Q05 is not captured; it remains distinct and must not be called a missing safety interlock.
- Q18 pickup-end rejection and Q21 activeWALK noqueue remain explicit documented guard cases absent from old accepted-action/checkpoint evidence.
- Q09 old machineSTOP filename still shows the running pilot lit; accepted machineStop is not established. Current queue steps require actual machineStop acknowledgement/response before AppStop/fullrepeat; machineReset0 is already directly visible.
- Q02 release Stop, Q03 knife Stop, Q08 occupied-crossing readiness loss and Q19 vertical lift Stop remain distinct named actuator/interruption cases. No default or shared code path closes them.

Current queue remains12 READY,6 scoped closures and Q23 Stop/recovery CLOSED with Reset/repeat FAIL. Control-only reconciliation reduces the matrix to27 non-SCOPED_PASS cells; those include route questions and known failures, not27 extra test executions.
