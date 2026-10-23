# Post-backup parcel V4 isolation result

ActualB5 scene/FileOpen V4/Reset/Run/physicalStart completed normally as UI operations. V4validation is parser/compiler evidence only. After50s and actualscan4187, native small-parcel-v4-final-signals.png shows sorted_count2, all_parcels_receivedFalse, cycle_completeFalse, route_position0.5, primary_table_alignedTrue, primary_table_loadedFalse/clearFalse, table_transfer_runTrue, infeed_runFalse, transfer_inhibitedTrue, misroute_detectedFalse.

Physical frames show the second parcel on its receiving path and the third blocked near the primary table. This is progress beyondV3's one-parcel stall, not full normal acceptance. V4only removes the old admission-stage extra belt pulse when measuredtableloaded. New remaining transition requires isolation between QA ladder staging, return-to-home alignment, and physical support/admission model. Cause unconfirmed; no application-source change made.

OriginalV3 failure andV4partialprogress are retained separately. Do not mark QA-PLANT-001parcel closed or infer a fixed count/size route from timing filenames. Git backup41188c70 is unchanged. Additional QA fixtures/native evidence remain local.
