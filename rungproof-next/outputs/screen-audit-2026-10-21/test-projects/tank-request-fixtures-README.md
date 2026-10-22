# Maintained tank request learner fixtures

Normal UI input projects; static JSON preparation only. Open, Verify/Load, Run. Pump station toggles pump_request; drain toggles drain_request. Drain request always inhibits inlet, even below low. Pump fills only below actual high; drain opens only while actual low switch is wet. No PC/SIM writes or fabricated feedback.

Test request OFF/OFF -> fill ON -> high cutoff; drain ON with pump still requested -> pump OFF and level falls -> low cutoff. Toggle both OFF; Stop removes commands. Maintained requests remain stateful, so Run may resume commands according to current real switch levels. Reset then Run gives initial level with both requests false. Tank-level transmitter should follow actual level as 4+16*(level percent/100) mA. No hardware/safety proof.
