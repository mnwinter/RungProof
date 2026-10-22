# Lab3.2 independent native evidence review

Reviewed26 completed frames:16 normal/recovery and10 lifecycle, sixSignals, physical gate/robot and two stackindicators. Four lab32-review sheets derive nativepixels; baseline confirms correctscene. No reviewer UI input/backend tests. Explicit reference scope.

Start with openGate/notReady rejects; closingGate and resetwithoutRobotReady do notarm. RobotReadyTrue alone and StartwithoutnewReset remain unarmed. FreshReset withbothpermissives armscell_readyTrue only(rightstackgreen); freshStart setsrobot_enableTrue(leftstackgreen too). Distinct robot arm poses at fresh-start-motion vs motion-later show actual modeled movement.

GateFalse clearsbothoutputs/lamps; gateTrue alone retainsunarmed, same robotpose across trip/restored/reset checkpoints. FreshReset rearmswithoutmotion, then freshStart enables/moves. RobotReadyloss similarly clearsboth; restorationalone doesnotrestart; Reset thenStart recovers. Physical door clearlyopen at baseline and AppReset vs closed after qualification; detailed gate-trip frame doorangle is not independently resolved, so symbolicgatechange is the exact evidence for that case.

AppStop retainsgate/readyTrue but outputsFalse/lampsdark atSTOPPED6509. AppRun6534 and StartwithoutReset6594 stayunarmed withsame robotpose. Reset6654 rearms; Start6714 enables. AppReset clearsall6pointsFalse, STOPPEDscan0, visiblyopensgate. Run23 staysclear. Newpermissives102 do notarm; Reset162 rearms, Start222 enablesagain. Settled motion_request/reset_complete areFalse throughout captures; exact accepted pulsewidth is notobserved.

No newdemonstratedissue. Sampled robotpose changes establish bounded motion, not continuousspeed/trajectory/fullproductioncycle or exactstoplatency. Door animation details, simultaneouscommandpolicy and physicalsafetyfunction unproven. No whole-scenePASS.
