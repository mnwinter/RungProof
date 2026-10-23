# Small desktop verification continuation

Native test surface restored at1021x660 on1024x768 desktop. QA-only screen_bridge_v10_small.py preserves v9 pinned foreground, per-monitor DPI, unoccluded screen-DC capture, input and clipping rejection. Current bridge session25762; original appPID44956/B5 unchanged. Small ui_small.py and wait_small_visible.py send native inputs/read actual pixels only. Original1600coordinates/helpers are excluded at this size.

CNC: r2freshReset/Start active infeedStop freezes actual blank and outputs; r3Run progresses and actualsmall-cnc-r3-completion-1.png matches cycle_completeTrue. r4freshReset/repeat small-cnc-r4-cut-5.png captures cnc_runTrue with actual accessClosed/clampClosed/cutPermitted/robotClearTrue; repeat completion captured small-cnc-r4-repeat-completion-1.png. General wrong-command/interlock tests remain unproven.

Parcel: newV3project loaded via actualscene/FileOpen/Run. Firstparcel physically routes; nativeSignals atscan4330 show count1/route.5/tableLoadedFalse/tableClearFalse/tableAlignedFalse with infeed/transferOff. Laterheldat7608 same pose/state confirmsstall. Cause remains separated: likely QA rung-order admission-output race interactingwithmodelroute-ownership. V4QA-onlyfixture is beingprepared to isolate this. No application change.

Parcel freshReset/activeStart/Stop r2nativepictures show actualtransportfreezes with allcommandsOff/count0. Full normal three-parcel route remains unverified. Original backup41188c70 is unchanged; new evidence/log appends remain local.
