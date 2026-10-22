# QA-PLANT-001: jug and service door native instances

Selected native `newbinding213-219-gap-review.jpg` compares actual controls/before,1s and later6–7s response frames plus current Signals.

Lab2.19 shutter remains fully closed in the same frame/track relationship through the OPEN command interval. Actual Signals shows motor_openTrue/motor_closeFalse, open_limit_ncTrue/closed_limit_ncFalse and door_position100. This is absent mechanical response, not failed request delivery. Door position's existing100=closed convention must be retained by its plant adapter. Bridge agent owns authorized correction.

Lab2.13 jug remains at the same far-infeed pose while actual conveyor_runTrue/jug_presentFalse/highFalse/valveFalse/skidFalse/fill0. Thus missing jug transport is independently confirmed, not a speculative fill-process issue. Correct Lab2.13 scene now composes/loads in B2, closing the demonstrated QA-LOAD-0012.13 load rejection. Other table-risk scenes2.23/24 need their own compose evidence.

Authorized jug correction implemented: command-driven belt/transport and liquid accumulation, geometry-derived leading-edge sensor placement to align delivered open neck with nozzle, filling only while belt stopped/both valve+skid commanded/tip physically inside neck, high feedback at full and retained percent until Reset. No automatic valve/motor sequence; PLC owns all commands. Stream reflects physical filling eligibility. Pure model regression added; native post-fix proof pending. Explicit sensor/index contract and request I/O/help inventory updated.
