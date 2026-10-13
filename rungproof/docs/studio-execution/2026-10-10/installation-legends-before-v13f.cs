using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Presentation contracts only. These do not become equipment or I/O.
    private sealed record FaceContract(string Action, string ActionType, string Point, string Text);
    private static readonly IReadOnlyDictionary<string, FaceContract> InstallationFaceContracts =
        new Dictionary<string, FaceContract>
        {
            ["lab-10-03-vision-package-sorter/switch_9"] = new("toggle-package_present", "toggle", "package_present", "PACKAGE\nPRESENT"),
            ["lab-10-03-vision-package-sorter/switch_10"] = new("toggle-vision_result_valid", "toggle", "vision_result_valid", "VISION\nVALID"),
            ["lab-10-03-vision-package-sorter/switch_11"] = new("toggle-destination_clear", "toggle", "destination_clear", "DEST\nCLEAR"),
            ["lab-11-07-multi-conveyor-pallet-route/switch_9"] = new("toggle-zone_1_clear", "toggle", "zone_1_clear", "ZONE 1\nCLEAR"),
            ["lab-11-07-multi-conveyor-pallet-route/switch_10"] = new("toggle-zone_2_clear", "toggle", "zone_2_clear", "ZONE 2\nCLEAR"),
            ["lab-11-07-multi-conveyor-pallet-route/switch_11"] = new("toggle-zone_3_clear", "toggle", "zone_3_clear", "ZONE 3\nCLEAR"),
            ["lab-11-11-service-elevator/switch_2"] = new("toggle-call_valid", "toggle", "call_valid", "CALL\nVALID"),
            ["lab-11-11-service-elevator/switch_8"] = new("toggle-doors_closed", "toggle", "doors_closed", "DOORS\nCLOSED"),
            ["lab-11-11-service-elevator/switch_9"] = new("toggle-landing_clear", "toggle", "landing_clear", "LANDING\nCLEAR"),
            ["lab-11-19-powder-batch-mixer/switch_11"] = new("toggle-recipe_valid", "toggle", "recipe_valid", "RECIPE\nVALID"),
            ["lab-11-19-powder-batch-mixer/switch_12"] = new("toggle-dose_complete", "toggle", "dose_complete", "DOSE\nDONE"),
            ["lab-11-19-powder-batch-mixer/switch_13"] = new("toggle-mixer_ready", "toggle", "mixer_ready", "MIXER\nREADY"),
            ["lab-2-18-pallet-pickup/manual_jog"] = new("manual-jog", "start", "", "JOG"),
            ["lab-3-01-guarded-pallet-transfer/switch_4"] = new("toggle-guard_closed", "toggle", "guard_closed", "GUARD\nCLOSED"),
            ["lab-3-01-guarded-pallet-transfer/switch_9"] = new("toggle-entry_clear", "toggle", "entry_clear", "ENTRY\nCLEAR"),
            ["lab-3-01-guarded-pallet-transfer/switch_10"] = new("toggle-exit_clear", "toggle", "exit_clear", "EXIT\nCLEAR"),
            ["lab-6-07-luggage-weight-sort/switch_8"] = new("load-next-bag", "luggageLoad", "", "LOAD\nBAG"),
            ["lab-6-07-luggage-weight-sort/switch_9"] = new("toggle-scale_ready", "toggle", "scale_ready", "SCALE\nREADY"),
            ["lab-6-07-luggage-weight-sort/switch_10"] = new("cycle-fixture_mass_kg", "cycle", "fixture_mass_kg", "NEXT\nMASS"),
            ["lab-6-08-hand-dryer/switch_7"] = new("toggle-hands_inserted", "toggle", "hands_inserted", "HANDS\nIN"),
            ["lab-9-11-pallet-counting/switch_7"] = new("toggle-pallet_detected", "toggle", "pallet_detected", "PALLET\nPRESENT"),
            ["lab-9-11-pallet-counting/switch_8"] = new("toggle-pallet_type_valid", "toggle", "pallet_type_valid", "TYPE\nVALID"),
            ["lab-9-11-pallet-counting/switch_9"] = new("toggle-count_request", "toggle", "count_request", "COUNT\nREQUEST"),
            ["lab-9-12-ev-charging-manager/switch_1"] = new("toggle-bay_occupied", "toggle", "bay_occupied", "A BAY\nOCCUPIED"),
            ["lab-9-12-ev-charging-manager/switch_8"] = new("toggle-customer_authorized", "toggle", "customer_authorized", "A AUTH\nVALID"),
            ["lab-9-12-ev-charging-manager/switch_9"] = new("toggle-charger_ready", "toggle", "charger_ready", "A CHARGE\nREADY"),
            ["lab-9-12-ev-charging-manager/fixture_connector_inserted"] = new("toggle-connector_inserted", "toggle", "connector_inserted", "A PLUG\nIN"),
            ["lab-9-12-ev-charging-manager/fixture_bay_b_occupied"] = new("toggle-bay_b_occupied", "toggle", "bay_b_occupied", "B BAY\nOCCUPIED"),
            ["lab-9-12-ev-charging-manager/fixture_bay_b_customer_authorized"] = new("toggle-bay_b_customer_authorized", "toggle", "bay_b_customer_authorized", "B AUTH\nVALID"),
            ["lab-9-12-ev-charging-manager/fixture_bay_b_charger_ready"] = new("toggle-bay_b_charger_ready", "toggle", "bay_b_charger_ready", "B CHARGE\nREADY"),
            ["lab-9-12-ev-charging-manager/fixture_bay_b_connector_inserted"] = new("toggle-bay_b_connector_inserted", "toggle", "bay_b_connector_inserted", "B PLUG\nIN"),
            ["tank-high-low/hl_drain_station"] = new("toggle-drain", "toggle", "drain_valve_open", "DRAIN"),
            ["tank-level/drain_station"] = new("toggle-drain", "toggle", "drain_valve_open", "DRAIN"),
            ["tank-radar/radar_pump_station"] = new("toggle-pump", "toggle", "cycle_request", "AUTO\nCYCLE"),
            ["tank-radar/radar_drain_station"] = new("toggle-drain", "toggle", "manual_drain_request", "DRAIN\nREQUEST"),
        };

    private static string ResolveOperatorFaceLabel(string sceneId, SceneEquipment equipment)
    {
        var explicitFace = Text(equipment.Config, "faceLabel", "");
        if (explicitFace.Length > 0 || Text(equipment.Config, "style", "") == "emergency") return explicitFace;
        if (!InstallationFaceContracts.TryGetValue(sceneId + "/" + equipment.Id, out var contract)) return "";
        if (equipment.Type != "switch" || Text(equipment.Config, "action", "") != contract.Action)
            throw new InvalidOperationException("Operator face action drift: " + sceneId + "/" + equipment.Id);
        return contract.Text;
    }

    private sealed record IdentificationContract(string Type, string Label, string Face, string Action,
        string BindingSignature, string Text);
    private static readonly IReadOnlyDictionary<string, IdentificationContract> InstallationIdentificationContracts =
        new Dictionary<string, IdentificationContract>
        {
            ["equipment-gallery/gallery_indicator"] = new("indicator", "Three-color stack light", "", "", "", "DEMO STACK LIGHT\nDEMO"),
            ["lab-2-01-workstation-call/material_call_button"] = new("switch", "Material call button", "MATERIAL CALL", "toggle-material-call", "material_call_pressed|switch", "MATERIAL CALL\nTOGGLE"),
            ["lab-2-01-workstation-call/material_call_lamp"] = new("indicator", "Material request lamp", "", "", "material_call_on|indicator", "MATERIAL REQUEST LAMP\nOUTPUT"),
            ["lab-2-02-dual-confirmation/operator_confirm_button"] = new("switch", "Operator confirmation", "OPERATOR OK", "toggle-operator-confirm", "operator_confirmed|switch", "OPERATOR OK\nTOGGLE"),
            ["lab-2-02-dual-confirmation/quality_confirm_button"] = new("switch", "Quality confirmation", "QUALITY OK", "toggle-quality-confirm", "quality_confirmed|switch", "QUALITY OK\nTOGGLE"),
            ["lab-2-02-dual-confirmation/handoff_ready_lamp"] = new("indicator", "Handoff ready lamp", "", "", "handoff_ready|indicator", "HANDOFF READY LAMP\nOUTPUT"),
            ["lab-2-03-service-marker-inhibit/marker_inhibit_button"] = new("switch", "Marker inhibit button", "INHIBIT", "toggle-marker-inhibit", "marker_inhibit_pressed|switch", "INHIBIT\nTOGGLE"),
            ["lab-2-03-service-marker-inhibit/service_marker_lamp"] = new("indicator", "Service marker lamp", "", "", "service_marker_on|indicator", "SERVICE MARKER LAMP\nOUTPUT"),
            ["lab-2-04-two-station-call/north_call_button"] = new("switch", "North station call", "NORTH CALL", "toggle-north-call", "north_call_pressed|switch", "NORTH CALL\nTOGGLE"),
            ["lab-2-04-two-station-call/south_call_button"] = new("switch", "South station call", "SOUTH CALL", "toggle-south-call", "south_call_pressed|switch", "SOUTH CALL\nTOGGLE"),
            ["lab-2-04-two-station-call/assistance_beacon"] = new("indicator", "Assistance call beacon", "", "", "assistance_call_on|indicator", "ASSISTANCE CALL BEACON\nOUTPUT"),
            ["lab-2-05-bay-light-selector/mode_selector"] = new("rotarySwitch", "Maintenance selector", "", "next-mode", "selector_position|selector", "BAY SELECTOR\n0 / 1\nSELECT"),
            ["lab-2-05-bay-light-selector/bay_light_a"] = new("indicator", "Bay A LED light", "", "", "bay_a_command|indicator", "BAY A LED LIGHT\nOUTPUT"),
            ["lab-2-05-bay-light-selector/bay_light_b"] = new("indicator", "Bay B LED light", "", "", "bay_b_command|indicator", "BAY B LED LIGHT\nOUTPUT"),
            ["lab-2-06-ready-attention/request_button"] = new("switch", "Attention request button", "ATTENTION", "toggle-request", "request_held|switch", "ATTENTION\nTOGGLE"),
            ["lab-2-06-ready-attention/ready_lamp"] = new("indicator", "Ready lamp", "", "", "ready_light|indicator", "READY LAMP\nOUTPUT"),
            ["lab-2-06-ready-attention/attention_lamp"] = new("indicator", "Attention lamp", "", "", "attention_light|indicator", "ATTENTION LAMP\nOUTPUT"),
            ["lab-2-07-dual-contact-permissive/reset_button"] = new("switch", "NO reset request", "RESET NO", "toggle-reset", "reset_request|switch", "RESET NO\nTOGGLE"),
            ["lab-2-07-dual-contact-permissive/nc_button"] = new("switch", "NC test contact", "TEST NC", "toggle-nc", "", "TEST NC\nTOGGLE"),
            ["lab-2-07-dual-contact-permissive/permit_lamp"] = new("indicator", "Permit lamp", "", "", "permit_output|indicator", "PERMIT LAMP\nOUTPUT"),
            ["lab-2-08-inspection-vote/left_vote"] = new("switch", "Inspector A vote", "VOTE A", "toggle-left", "vote_a|switch", "VOTE A\nTOGGLE"),
            ["lab-2-08-inspection-vote/right_vote"] = new("switch", "Inspector B vote", "VOTE B", "toggle-right", "vote_b|switch", "VOTE B\nTOGGLE"),
            ["lab-2-08-inspection-vote/vote_stacklight"] = new("indicator", "Inspection result stacklight", "", "", "conflict_indication|indicator;hold_indication|indicator;pass_indication|indicator", "INSPECTION RESULT\nOUTPUT"),
            ["lab-2-09-maintenance-beacon/beacon_selector"] = new("rotarySwitch", "Maintenance state selector", "", "next-beacon-state", "beacon_position|selector", "BEACON SELECTOR\n0 / 1 / 2 / 3"),
            ["lab-2-09-maintenance-beacon/maintenance_beacon"] = new("indicator", "Maintenance beacon", "", "", "amber_beacon|indicator;green_beacon|indicator;red_beacon|indicator", "MAINTENANCE BEACON\nOUTPUT"),
            ["lab-2-25-inspection-toggle/inspection_toggle_button"] = new("switch", "Inspection light toggle", "TOGGLE", "toggle-inspection-light", "toggle_button_pressed|switch", "TOGGLE\nPULSE"),
            ["lab-2-25-inspection-toggle/inspection_light"] = new("indicator", "Machine inspection light", "", "", "inspection_light_on|indicator", "INSPECTION LIGHT\nOUTPUT"),
            ["lab-4-01-press-count-lamp/switch_0"] = new("switch", "Press-Count Lamp switch", "PULSE", "toggle-pulse_received", "pulse_received|switch", "PULSE\nPULSE"),
            ["lab-4-01-press-count-lamp/indicator_1"] = new("indicator", "Press-Count Lamp indicator", "", "", "threshold_lamp|indicator", "COUNT REACHED\nOUTPUT"),
            ["lab-4-02-counter-reset-lamp/switch_0"] = new("switch", "Count pulse button", "COUNT", "pulse-count", "pulse_received|switch", "COUNT\nPULSE"),
            ["lab-4-02-counter-reset-lamp/indicator_1"] = new("indicator", "Counter Reset Lamp indicator", "", "", "counter_lamp|indicator", "COUNT REACHED\nOUTPUT"),
            ["lab-4-02-counter-reset-lamp/switch_2"] = new("switch", "Counter reset button", "RESET", "pulse-reset", "reset_pressed|switch", "RESET\nPULSE"),
            ["lab-4-05-dual-input-count-window/switch_0"] = new("switch", "Channel A pulse button", "A", "pulse-a", "channel_a_pulse|switch", "A\nPULSE"),
            ["lab-4-05-dual-input-count-window/switch_1"] = new("switch", "Channel B pulse button", "B", "pulse-b", "channel_b_pulse|switch", "B\nPULSE"),
            ["lab-4-05-dual-input-count-window/indicator_2"] = new("indicator", "PLC count comparison indication", "", "", "window_ready|indicator", "COUNT COMPARE\nOUTPUT"),
            ["lab-4-05-dual-input-count-window/reset_button"] = new("switch", "Reset both PLC counters", "RESET", "pulse-reset", "reset_pressed|switch", "RESET\nPULSE"),
            ["lab-4-05-dual-input-count-window/count_a_display"] = new("trainingAccessory", "PLC channel A accumulated count", "", "", "channel_a_count|numericDisplay", "A COUNT\nOUTPUT"),
            ["lab-4-05-dual-input-count-window/count_b_display"] = new("trainingAccessory", "PLC channel B accumulated count", "", "", "channel_b_count|numericDisplay", "B COUNT\nOUTPUT"),
            ["lab-4-06-multi-press-confirmation/switch_0"] = new("switch", "Channel A pulse button", "A", "pulse-a", "button_a_pressed|switch", "A\nPULSE"),
            ["lab-4-06-multi-press-confirmation/switch_1"] = new("switch", "Channel B pulse button", "B", "pulse-b", "button_b_pressed|switch", "B\nPULSE"),
            ["lab-4-06-multi-press-confirmation/indicator_2"] = new("indicator", "PLC count comparison indication", "", "", "confirmation_valid|indicator", "COUNT COMPARE\nOUTPUT"),
            ["lab-4-06-multi-press-confirmation/reset_button"] = new("switch", "Reset both PLC counters", "RESET", "pulse-reset", "reset_pressed|switch", "RESET\nPULSE"),
            ["lab-4-06-multi-press-confirmation/count_a_display"] = new("trainingAccessory", "PLC channel A accumulated count", "", "", "button_a_count|numericDisplay", "A COUNT\nOUTPUT"),
            ["lab-4-06-multi-press-confirmation/count_b_display"] = new("trainingAccessory", "PLC channel B accumulated count", "", "", "button_b_count|numericDisplay", "B COUNT\nOUTPUT"),
            ["lab-5-01-delayed-lamp/switch_0"] = new("rotarySwitch", "Timer REQUEST selector OFF / ON", "REQUEST", "toggle-timer_request", "timer_request|selector", "REQUEST\nOFF / ON\nSELECT"),
            ["lab-5-01-delayed-lamp/indicator_1"] = new("indicator", "PLC timed lamp", "", "", "delayed_lamp|indicator", "PLC TIMED LAMP\nOUTPUT"),
            ["lab-5-02-timed-lamp-off/switch_0"] = new("switch", "START momentary timer trigger", "START", "press-start", "start_pulse|switch", "START\nPULSE"),
            ["lab-5-02-timed-lamp-off/indicator_1"] = new("indicator", "PLC timed lamp", "", "", "timed_lamp|indicator", "PLC TIMED LAMP\nOUTPUT"),
            ["lab-5-03-rotary-flasher/switch_0"] = new("rotarySwitch", "Mode selector OFF / FLASH", "MODE", "toggle-flash_mode_selected", "flash_mode_selected|selector", "MODE\nOFF / FLASH\nSELECT"),
            ["lab-5-03-rotary-flasher/indicator_1"] = new("indicator", "PLC flashing lamp", "", "", "flash_lamp|indicator", "PLC FLASHING LAMP\nOUTPUT"),
            ["lab-5-04-alternating-lamps/switch_0"] = new("rotarySwitch", "ENABLE maintained selector OFF / RUN", "ENABLE", "toggle-alternate_enable", "alternate_enable|selector", "ENABLE\nOFF / RUN\nSELECT"),
            ["lab-5-04-alternating-lamps/indicator_1"] = new("indicator", "Lamp A - amber", "", "", "lamp_a|indicator", "LAMP A - AMBER\nOUTPUT"),
            ["lab-5-04-alternating-lamps/indicator_2"] = new("indicator", "Lamp B - green", "", "", "lamp_b|indicator", "LAMP B - GREEN\nOUTPUT"),
            ["lab-5-05-variable-flash-rate/switch_0"] = new("rotarySwitch", "ENABLE maintained selector OFF / RUN", "ENABLE", "toggle-flash_enable", "flash_enable|selector", "ENABLE\nOFF / RUN\nSELECT"),
            ["lab-5-05-variable-flash-rate/switch_1"] = new("rotarySwitch", "FAST maintained selector OFF / FAST", "FAST", "toggle-fast_rate_selected", "fast_rate_selected|selector", "FAST\nOFF / FAST\nSELECT"),
            ["lab-5-05-variable-flash-rate/indicator_2"] = new("indicator", "PLC rate lamp - green", "", "", "rate_lamp|indicator", "PLC RATE LAMP - GREEN\nOUTPUT"),
            ["lab-5-05-variable-flash-rate/switch_3"] = new("rotarySwitch", "SLOW maintained selector OFF / SLOW", "SLOW", "toggle-slow_rate_selected", "slow_rate_selected|selector", "SLOW\nOFF / SLOW\nSELECT"),
            ["lab-5-06-running-light-tower/switch_0"] = new("rotarySwitch", "ENABLE maintained selector OFF / RUN", "ENABLE", "toggle-tower_enable", "tower_enable|selector", "ENABLE\nOFF / RUN\nSELECT"),
            ["lab-5-06-running-light-tower/indicator_1"] = new("indicator", "PLC three-color running-light tower", "", "", "tower_amber|indicatorChannel;tower_green|indicatorChannel;tower_red|indicatorChannel", "RUNNING SEQUENCE\nOUTPUT"),
            ["lab-5-06-running-light-tower/switch_2"] = new("switch", "Momentary sequence Step", "STEP", "toggle-step_pulse", "step_pulse|switch", "STEP\nPULSE"),
            ["lab-9-01-sum-function/switch_1"] = new("switch", "A VALID manual input", "A VALID", "toggle-operand_a_valid", "operand_a_valid|switch", "A VALID\nTOGGLE"),
            ["lab-9-01-sum-function/indicator_2"] = new("indicator", "Sum Function Block indicator", "", "", "sum_result_valid|indicator", "RESULT VALID\nOUTPUT"),
            ["lab-9-01-sum-function/training_accessory_4"] = new("trainingAccessory", "SUM live numeric result", "", "", "sum_result|numericDisplay", "SUM\nOUTPUT"),
            ["lab-9-01-sum-function/switch_5"] = new("switch", "B VALID manual input", "B VALID", "toggle-operand_b_valid", "operand_b_valid|switch", "B VALID\nTOGGLE"),
            ["lab-9-01-sum-function/switch_6"] = new("switch", "CALCULATE manual input", "CALCULATE", "toggle-calculate_request", "calculate_request|switch", "CALCULATE\nTOGGLE"),
            ["lab-9-01-sum-function/numeric_display_0"] = new("trainingAccessory", "A NEXT live numeric readout", "", "cycle-operand_a", "operand_a|numericDisplay", "A NEXT\nNEXT"),
            ["lab-9-01-sum-function/numeric_display_1"] = new("trainingAccessory", "B NEXT live numeric readout", "", "cycle-operand_b", "operand_b|numericDisplay", "B NEXT\nNEXT"),
            ["lab-9-02-product-function/switch_1"] = new("switch", "A VALID manual input", "A VALID", "toggle-factor_a_valid", "factor_a_valid|switch", "A VALID\nTOGGLE"),
            ["lab-9-02-product-function/indicator_2"] = new("indicator", "Product Function Block indicator", "", "", "product_result_valid|indicator", "RESULT VALID\nOUTPUT"),
            ["lab-9-02-product-function/training_accessory_4"] = new("trainingAccessory", "PRODUCT live numeric result", "", "", "product_result|numericDisplay", "PRODUCT\nOUTPUT"),
            ["lab-9-02-product-function/switch_5"] = new("switch", "B VALID manual input", "B VALID", "toggle-factor_b_valid", "factor_b_valid|switch", "B VALID\nTOGGLE"),
            ["lab-9-02-product-function/switch_6"] = new("switch", "CALCULATE manual input", "CALCULATE", "toggle-calculate_request", "calculate_request|switch", "CALCULATE\nTOGGLE"),
            ["lab-9-02-product-function/numeric_display_0"] = new("trainingAccessory", "A NEXT live numeric readout", "", "cycle-factor_a", "factor_a|numericDisplay", "A NEXT\nNEXT"),
            ["lab-9-02-product-function/numeric_display_1"] = new("trainingAccessory", "B NEXT live numeric readout", "", "cycle-factor_b", "factor_b|numericDisplay", "B NEXT\nNEXT"),
            ["lab-9-03-sum-and-counter-function/switch_1"] = new("switch", "Manual inputs valid input", "INPUTS VALID", "toggle-inputs_valid", "inputs_valid|switch", "INPUTS VALID\nTOGGLE"),
            ["lab-9-03-sum-and-counter-function/indicator_2"] = new("indicator", "PLC result valid indication", "", "", "result_valid|indicator", "RESULT VALID\nOUTPUT"),
            ["lab-9-03-sum-and-counter-function/switch_3"] = new("switch", "Manual calculate input", "CALCULATE", "toggle-calculate_request", "calculate_request|switch", "CALCULATE\nTOGGLE"),
            ["lab-9-03-sum-and-counter-function/switch_4"] = new("switch", "Manual call complete input", "CALL COMPLETE", "toggle-call_complete", "call_complete|switch", "CALL COMPLETE\nTOGGLE"),
            ["lab-9-03-sum-and-counter-function/indicator_5"] = new("indicator", "PLC event counted indication", "", "", "event_counted|indicator", "EVENT COUNTED\nOUTPUT"),
            ["lab-9-03-sum-and-counter-function/numeric_display_0"] = new("trainingAccessory", "A NEXT live numeric readout", "", "cycle-operand_a", "operand_a|numericDisplay", "A NEXT\nNEXT"),
            ["lab-9-03-sum-and-counter-function/numeric_display_1"] = new("trainingAccessory", "B NEXT live numeric readout", "", "cycle-operand_b", "operand_b|numericDisplay", "B NEXT\nNEXT"),
            ["lab-9-03-sum-and-counter-function/numeric_display_2"] = new("trainingAccessory", "SUM live numeric readout", "", "", "sum_result|numericDisplay", "SUM\nOUTPUT"),
            ["lab-9-03-sum-and-counter-function/numeric_display_3"] = new("trainingAccessory", "COUNT live numeric readout", "", "", "event_count|numericDisplay", "COUNT\nOUTPUT"),
            ["lab-9-04-function-selector/switch_0"] = new("switch", "OPERANDS manual input", "OPERANDS", "toggle-operand_set_valid", "operand_set_valid|switch", "OPERANDS\nTOGGLE"),
            ["lab-9-04-function-selector/indicator_2"] = new("indicator", "Function Selector indicator", "", "", "selected_result_valid|indicator", "RESULT VALID\nOUTPUT"),
            ["lab-9-04-function-selector/switch_5"] = new("switch", "FUNC VALID manual input", "FUNC VALID", "toggle-function_select_valid", "function_select_valid|switch", "FUNC VALID\nTOGGLE"),
            ["lab-9-04-function-selector/switch_6"] = new("switch", "CALCULATE manual input", "CALCULATE", "toggle-calculate_request", "calculate_request|switch", "CALCULATE\nTOGGLE"),
            ["lab-9-04-function-selector/numeric_display_0"] = new("trainingAccessory", "A NEXT live numeric readout", "", "cycle-operand_a", "operand_a|numericDisplay", "A NEXT\nNEXT"),
            ["lab-9-04-function-selector/numeric_display_1"] = new("trainingAccessory", "B NEXT live numeric readout", "", "cycle-operand_b", "operand_b|numericDisplay", "B NEXT\nNEXT"),
            ["lab-9-04-function-selector/numeric_display_2"] = new("trainingAccessory", "FUNC NEXT live numeric readout", "", "cycle-function_choice", "function_choice|numericDisplay", "FUNC NEXT\nNEXT"),
            ["lab-9-04-function-selector/numeric_display_3"] = new("trainingAccessory", "RESULT live numeric readout", "", "", "selected_result|numericDisplay", "RESULT\nOUTPUT"),
        };

    private static readonly IReadOnlyDictionary<string, FaceContract> BenchActionContracts =
        new Dictionary<string, FaceContract>
        {
            ["lab-2-01-workstation-call/material_call_button"] = new("toggle-material-call", "toggle", "material_call_pressed", ""),
            ["lab-2-02-dual-confirmation/operator_confirm_button"] = new("toggle-operator-confirm", "toggle", "operator_confirmed", ""),
            ["lab-2-02-dual-confirmation/quality_confirm_button"] = new("toggle-quality-confirm", "toggle", "quality_confirmed", ""),
            ["lab-2-03-service-marker-inhibit/marker_inhibit_button"] = new("toggle-marker-inhibit", "toggle", "marker_inhibit_pressed", ""),
            ["lab-2-04-two-station-call/north_call_button"] = new("toggle-north-call", "toggle", "north_call_pressed", ""),
            ["lab-2-04-two-station-call/south_call_button"] = new("toggle-south-call", "toggle", "south_call_pressed", ""),
            ["lab-2-05-bay-light-selector/mode_selector"] = new("next-mode", "cycle", "selector_position", ""),
            ["lab-2-06-ready-attention/request_button"] = new("toggle-request", "toggle", "request_held", ""),
            ["lab-2-07-dual-contact-permissive/reset_button"] = new("toggle-reset", "toggle", "reset_request", ""),
            ["lab-2-07-dual-contact-permissive/nc_button"] = new("toggle-nc", "toggle", "stop_contact_nc", ""),
            ["lab-2-08-inspection-vote/left_vote"] = new("toggle-left", "toggle", "vote_a", ""),
            ["lab-2-08-inspection-vote/right_vote"] = new("toggle-right", "toggle", "vote_b", ""),
            ["lab-2-09-maintenance-beacon/beacon_selector"] = new("next-beacon-state", "cycle", "beacon_position", ""),
            ["lab-2-25-inspection-toggle/inspection_toggle_button"] = new("toggle-inspection-light", "pulse", "toggle_button_pressed", ""),
            ["lab-4-01-press-count-lamp/switch_0"] = new("toggle-pulse_received", "pulse", "pulse_received", ""),
            ["lab-4-02-counter-reset-lamp/switch_0"] = new("pulse-count", "pulse", "pulse_received", ""),
            ["lab-4-02-counter-reset-lamp/switch_2"] = new("pulse-reset", "pulse", "reset_pressed", ""),
            ["lab-4-05-dual-input-count-window/switch_0"] = new("pulse-a", "pulse", "channel_a_pulse", ""),
            ["lab-4-05-dual-input-count-window/switch_1"] = new("pulse-b", "pulse", "channel_b_pulse", ""),
            ["lab-4-05-dual-input-count-window/reset_button"] = new("pulse-reset", "pulse", "reset_pressed", ""),
            ["lab-4-06-multi-press-confirmation/switch_0"] = new("pulse-a", "pulse", "button_a_pressed", ""),
            ["lab-4-06-multi-press-confirmation/switch_1"] = new("pulse-b", "pulse", "button_b_pressed", ""),
            ["lab-4-06-multi-press-confirmation/reset_button"] = new("pulse-reset", "pulse", "reset_pressed", ""),
            ["lab-5-01-delayed-lamp/switch_0"] = new("toggle-timer_request", "toggle", "timer_request", ""),
            ["lab-5-02-timed-lamp-off/switch_0"] = new("press-start", "pulse", "start_pulse", ""),
            ["lab-5-03-rotary-flasher/switch_0"] = new("toggle-flash_mode_selected", "toggle", "flash_mode_selected", ""),
            ["lab-5-04-alternating-lamps/switch_0"] = new("toggle-alternate_enable", "toggle", "alternate_enable", ""),
            ["lab-5-05-variable-flash-rate/switch_0"] = new("toggle-flash_enable", "toggle", "flash_enable", ""),
            ["lab-5-05-variable-flash-rate/switch_1"] = new("toggle-fast_rate_selected", "toggle", "fast_rate_selected", ""),
            ["lab-5-05-variable-flash-rate/switch_3"] = new("toggle-slow_rate_selected", "toggle", "slow_rate_selected", ""),
            ["lab-5-06-running-light-tower/switch_0"] = new("toggle-tower_enable", "toggle", "tower_enable", ""),
            ["lab-5-06-running-light-tower/switch_2"] = new("toggle-step_pulse", "pulse", "step_pulse", ""),
            ["lab-9-01-sum-function/switch_1"] = new("toggle-operand_a_valid", "toggle", "operand_a_valid", ""),
            ["lab-9-01-sum-function/switch_5"] = new("toggle-operand_b_valid", "toggle", "operand_b_valid", ""),
            ["lab-9-01-sum-function/switch_6"] = new("toggle-calculate_request", "toggle", "calculate_request", ""),
            ["lab-9-01-sum-function/numeric_display_0"] = new("cycle-operand_a", "cycle", "operand_a", ""),
            ["lab-9-01-sum-function/numeric_display_1"] = new("cycle-operand_b", "cycle", "operand_b", ""),
            ["lab-9-02-product-function/switch_1"] = new("toggle-factor_a_valid", "toggle", "factor_a_valid", ""),
            ["lab-9-02-product-function/switch_5"] = new("toggle-factor_b_valid", "toggle", "factor_b_valid", ""),
            ["lab-9-02-product-function/switch_6"] = new("toggle-calculate_request", "toggle", "calculate_request", ""),
            ["lab-9-02-product-function/numeric_display_0"] = new("cycle-factor_a", "cycle", "factor_a", ""),
            ["lab-9-02-product-function/numeric_display_1"] = new("cycle-factor_b", "cycle", "factor_b", ""),
            ["lab-9-03-sum-and-counter-function/switch_1"] = new("toggle-inputs_valid", "toggle", "inputs_valid", ""),
            ["lab-9-03-sum-and-counter-function/switch_3"] = new("toggle-calculate_request", "toggle", "calculate_request", ""),
            ["lab-9-03-sum-and-counter-function/switch_4"] = new("toggle-call_complete", "toggle", "call_complete", ""),
            ["lab-9-03-sum-and-counter-function/numeric_display_0"] = new("cycle-operand_a", "cycle", "operand_a", ""),
            ["lab-9-03-sum-and-counter-function/numeric_display_1"] = new("cycle-operand_b", "cycle", "operand_b", ""),
            ["lab-9-04-function-selector/switch_0"] = new("toggle-operand_set_valid", "toggle", "operand_set_valid", ""),
            ["lab-9-04-function-selector/switch_5"] = new("toggle-function_select_valid", "toggle", "function_select_valid", ""),
            ["lab-9-04-function-selector/switch_6"] = new("toggle-calculate_request", "toggle", "calculate_request", ""),
            ["lab-9-04-function-selector/numeric_display_0"] = new("cycle-operand_a", "cycle", "operand_a", ""),
            ["lab-9-04-function-selector/numeric_display_1"] = new("cycle-operand_b", "cycle", "operand_b", ""),
            ["lab-9-04-function-selector/numeric_display_2"] = new("cycle-function_choice", "cycle", "function_choice", ""),
        };

    private static void ValidateFaceAction(SceneDefinition scene, SceneEquipment equipment, FaceContract contract)
    {
        var actions = scene.Simulation.GetProperty("actions").EnumerateArray()
            .Where(a => Text(a, "id", "") == contract.Action).ToArray();
        if (actions.Length != 1 || Text(actions[0], "type", "") != contract.ActionType
            || Text(actions[0], "point", "") != contract.Point)
            throw new InvalidOperationException("Operator face simulation drift: " + scene.Id + "/" + equipment.Id);
    }

    // Root calls this AFTER installation packing, before template transforms.
    // All labels belong to the support, never to the picked field device.
    private static void ConfigureInstallationIdentification(Node3D root, SceneDefinition scene, Node3D installation)
    {
        foreach (var item in scene.Equipment)
            if (InstallationFaceContracts.TryGetValue(scene.Id + "/" + item.Id, out var face))
                ValidateFaceAction(scene, item, face);
        if (!ControlInstallationPlans.TryGetValue(scene.Id, out var plan)) return;
        ConfigureMappedOperatorStrips(root, scene, installation, plan);
        if (plan.Bench)
        {
            foreach (var id in plan.EquipmentIds)
            {
                var item = scene.Equipment.Single(e => e.Id == id);
                if (!InstallationIdentificationContracts.TryGetValue(scene.Id + "/" + id, out var c))
                    throw new InvalidOperationException("Missing bench identification: " + scene.Id + "/" + id);
                var bindings = scene.Simulation.GetProperty("pointBindings").EnumerateArray()
                    .Where(b => Text(b, "equipmentId", "") == id).ToArray();
                var signature = string.Join(";", bindings.Select(b => Text(b, "point", "") + "|" + Text(b, "mode", ""))
                    .OrderBy(s => s, StringComparer.Ordinal));
                if (item.Type != c.Type || item.Label != c.Label || Text(item.Config, "faceLabel", "") != c.Face
                    || Text(item.Config, "action", "") != c.Action || signature != c.BindingSignature)
                    throw new InvalidOperationException("Bench identification source drift: " + scene.Id + "/" + id);
                if (BenchActionContracts.TryGetValue(scene.Id + "/" + id, out var benchAction))
                    ValidateFaceAction(scene, item, benchAction);
                var device = root.GetNode<Node3D>(SafeNodeName(id));
                var deviceBounds = InstallationBounds(device);
                // Transform measured head to the support frame, including camera-facing rotation.
                var relative = installation.Transform.AffineInverse();
                var head = relative * deviceBounds;
                // Upper shelf is authored as a repeated mesh; select any measured supporting shelf.
                var shelves = installation.GetChildren().OfType<MeshInstance3D>()
                    .Where(m => m.Mesh is BoxMesh && (m.Name.ToString().StartsWith("StudentBench_Worktop", StringComparison.Ordinal)
                        || m.Name.ToString().StartsWith("InstrumentTier", StringComparison.Ordinal)
                        || m.Name.ToString().StartsWith("UpperTierShelf", StringComparison.Ordinal)))
                    .Select(m => m.Transform * m.GetAabb()).ToArray();
                var bearing = shelves.Where(s => MathF.Abs(s.End.Y - head.Position.Y) < .12f)
                    .OrderBy(s => MathF.Abs(s.End.Y - head.Position.Y)).FirstOrDefault();
                if (bearing.Size == Vector3.Zero)
                    throw new InvalidOperationException("Bench identification needs measured shelf: " + scene.Id + "/" + id);
                const float pitch = .70f; // Exact existing bench packing pitch; no guessed column ordering.
                var width = MathF.Min(pitch - .025f, .72f);
                // Upper fascias must not extend down across the lower heads.
                // Their explicit contracts are concise two-line output identities.
                var stripHeight = bearing.End.Y > 1.1f ? .12f : .24f;
                AddIdentificationStrip(installation, device, id, c.Text,
                    new Vector3(head.GetCenter().X, bearing.End.Y - stripHeight / 2 - .005f, bearing.End.Z - .006f), width, stripHeight,
                    c.Action, c.BindingSignature);
            }
        }
        foreach (var (id, point, text) in scene.Id switch
        {
            "lab-2-10-dust-collector-seal-in" => new[] { ("collector_run_lamp", "collector_run", "RUNNING") },
            "lab-2-12-assembly-lift" => new[] { ("top_limit_lamp", "top_limit", "TOP LIMIT"), ("bottom_limit_lamp", "bottom_limit", "BOTTOM LIMIT") },
            _ => System.Array.Empty<(string, string, string)>(),
        })
        {
            var binding = scene.Simulation.GetProperty("pointBindings").EnumerateArray()
                .Any(b => Text(b, "equipmentId", "") == id && Text(b, "point", "") == point && Text(b, "mode", "") == "indicator");
            if (!binding) throw new InvalidOperationException("Pilot identification binding drift: " + scene.Id + "/" + id);
            var device = root.GetNode<Node3D>(SafeNodeName(id));
            var head = installation.Transform.AffineInverse() * InstallationBounds(device);
            AddIdentificationStrip(installation, device, id, text, new Vector3(head.GetCenter().X, head.Position.Y - .10f, -.110f),
                MathF.Min(.48f, (installation.HasMeta("measuredHeadPitch") ? installation.GetMeta("measuredHeadPitch").AsSingle() : .38f) - .025f), .16f, "", point + "|indicator");
        }
    }

    private static Aabb IdentificationMeshBounds(Node3D equipment, MeshInstance3D mesh, Node3D support)
    {
        var pose = mesh.Transform;
        for (var parent = mesh.GetParent(); parent != equipment; parent = parent.GetParent())
            if (parent is Node3D spatial) pose = spatial.Transform * pose;
        return support.Transform.AffineInverse() * equipment.Transform * pose * mesh.GetAabb();
    }

    private static void ConfigureMappedOperatorStrips(Node3D root, SceneDefinition scene,
        Node3D installation, InstallationPlan plan)
    {
        foreach (var item in scene.Equipment)
        {
            if (!InstallationFaceContracts.TryGetValue(scene.Id + "/" + item.Id, out var contract)
                || Text(item.Config, "faceLabel", "").Length > 0) continue;
            // This explicit inventory contains panel inputs only. An authored
            // installation change must be reviewed, not silently guessed.
            if (plan.Bench || !plan.EquipmentIds.Contains(item.Id))
                throw new InvalidOperationException("Mapped operator strip has no panel association: " + scene.Id + "/" + item.Id);
            if (ResolveOperatorFaceLabel(scene.Id, item) != contract.Text)
                throw new InvalidOperationException("Mapped operator strip face drift: " + item.Id);
            ValidateFaceAction(scene, item, contract);
            var device = root.GetNode<Node3D>(SafeNodeName(item.Id));
            var ring = device.FindChild("BUTTON_retaining_ring", true, false) as MeshInstance3D
                ?? throw new InvalidOperationException("Mapped operator strip requires retained button ring: " + item.Id);
            var oldPlate = device.FindChild("BUTTON_legend_plate", true, false) as MeshInstance3D
                ?? throw new InvalidOperationException("Mapped operator strip requires original plate: " + item.Id);
            var oldInk = device.FindChild("OperatorFaceLabel", true, false) as Label3D
                ?? throw new InvalidOperationException("Mapped operator strip requires resolved ink: " + item.Id);
            var door = installation.GetNodeOrNull<MeshInstance3D>("OperatorCabinet_Door")
                ?? throw new InvalidOperationException("Mapped operator strip requires cabinet fascia: " + item.Id);
            var ringBounds = IdentificationMeshBounds(device, ring, installation);
            var doorBounds = door.Transform * door.GetAabb();
            var pitch = installation.HasMeta("measuredHeadPitch")
                ? installation.GetMeta("measuredHeadPitch").AsSingle()
                : plan.EquipmentIds.Any(id => scene.Equipment.Single(e => e.Id == id).Type == "trainingAccessory") ? .70f : .38f;
            var width = MathF.Min(.48f, pitch - .025f);
            const float height = .16f;
            const float ringGap = .02f;
            // The rear plate face bears exactly on the measured door front.
            // Keep all geometry in support coordinates: the cabinet rotation
            // handles the opposite camera side without moving picked devices.
            var at = new Vector3(ringBounds.GetCenter().X,
                ringBounds.End.Y + ringGap + height / 2, doorBounds.End.Z + .006f);
            if (at.X - width / 2 < doorBounds.Position.X || at.X + width / 2 > doorBounds.End.X
                || at.Y - height / 2 < doorBounds.Position.Y || at.Y + height / 2 > doorBounds.End.Y)
                throw new InvalidOperationException("Mapped operator strip exceeds cabinet fascia: " + scene.Id + "/" + item.Id);
            var sourceBindings = scene.Simulation.GetProperty("pointBindings").EnumerateArray()
                .Where(b => Text(b, "equipmentId", "") == item.Id)
                .Select(b => Text(b, "point", "") + "|" + Text(b, "mode", ""))
                .OrderBy(s => s, StringComparer.Ordinal);
            AddIdentificationStrip(installation, device, item.Id, contract.Text, at, width, height,
                contract.Action, string.Join(";", sourceBindings));
            var plate = installation.GetNode<MeshInstance3D>("InstallationIdentification_" + SafeNodeName(item.Id));
            plate.SetMeta("installationLegendKind", "mappedOperatorFace");
            plate.SetMeta("sourceActionType", contract.ActionType);
            plate.SetMeta("sourcePoint", contract.Point);
            plate.SetMeta("measuredRingGap", ringGap);
            plate.SetMeta("measuredDoorFront", doorBounds.End.Z);
            // Publish replacement first; only then retire the exact tiny face.
            // Lens/controller paths and original device transforms stay intact.
            oldInk.Visible = false;
            oldPlate.Visible = false;
        }
    }

    private static void AddIdentificationStrip(Node3D support, Node3D device, string id, string text,
        Vector3 at, float width, float height, string action, string binding)
    {
        var name = "InstallationIdentification_" + SafeNodeName(id);
        if (support.GetNodeOrNull<Node3D>(name) is not null)
            throw new InvalidOperationException("Duplicate identification: " + id);
        var plate = AddBox(support, new Vector3(width, height, .012f), at, Material(new Color("17242b"), .15f, .35f));
        plate.Name = name;
        plate.SetMeta("equipmentId", id);
        plate.SetMeta("sourceAction", action);
        plate.SetMeta("sourceBindings", binding);
        plate.SetMeta("passivePresentation", true);
        // Use measured ink width, not character count, at the fixed readable
        // glyph scale. Upper fascias retain their .12m clearance envelope:
        // compose the function and INPUT/OUTPUT role on one line when it fits.
        // Two lines measured .1144m natively, exceeding their .100m ink area.
        const int identificationFontSize = 64;
        const float identificationPixelSize = .00065f;
        var font = ThemeDB.FallbackFont;
        var lines = new List<string>();
        var paragraphs = height <= .12f ? new[] { text.Replace('\n', ' ') } : text.Split('\n');
        foreach (var paragraph in paragraphs)
        {
            var current = "";
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (current.Length > 0 && font.GetStringSize(candidate, HorizontalAlignment.Left, -1,
                        identificationFontSize).X * identificationPixelSize > width - .02f)
                { lines.Add(current); current = ""; }
                current += (current.Length == 0 ? "" : " ") + word;
            }
            if (current.Length > 0) lines.Add(current);
        }
        var label = new Label3D { Name = name + "_Ink", Text = string.Join("\n", lines),
            Position = at + new Vector3(0, 0, .009f), FontSize = identificationFontSize, PixelSize = identificationPixelSize,
            OutlineSize = 0, Modulate = Colors.White, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center };
        var sizes = lines.Select(l => font.GetStringSize(l, HorizontalAlignment.Left, -1, label.FontSize)).ToArray();
        var inkWidth = sizes.Max(s => s.X) * label.PixelSize;
        var inkHeight = font.GetStringSize("Mg", HorizontalAlignment.Left, -1, label.FontSize).Y * lines.Count * label.PixelSize;
        if (inkWidth > width - .02f || inkHeight > height - .02f)
            throw new InvalidOperationException($"Identification strip needs wider readable layout: {id}; lines={lines.Count}; ink=({inkWidth:F6},{inkHeight:F6}); available=({width - .02f:F6},{height - .02f:F6}); font={label.FontSize}; pixelSize={label.PixelSize:F6}; text={label.Text.Replace('\n', '|')}");
        label.SetMeta("equipmentId", id);
        label.SetMeta("passivePresentation", true);
        support.AddChild(label);
        device.SetMeta("installationIdentification", plate.Name.ToString());
    }
}

