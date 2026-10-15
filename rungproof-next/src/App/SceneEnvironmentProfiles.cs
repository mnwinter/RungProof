using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RungProof.Next.App;

public enum SceneEnvironmentFamily
{
    TrainingWorkshop, DispatchWarehouse, MachineShop, ProcessUtility,
    OutdoorTraffic, OutdoorUtility, PublicInterior, EquipmentGallery,
}

/// <summary>
/// Presentation only. MotionMargin is additional conservative decoration spacing,
/// not a collision model or a continuous-clearance/safety certificate. ReplaceFloor
/// is false where an authored pit, waterway or elevated installation needs its ground.
/// Room axes follow the process world axes; camera direction never defines room yaw.
/// </summary>
public sealed record SceneEnvironmentProfile(SceneEnvironmentFamily Family,
    string Variant, string AccentHex, float MotionMargin = 0, bool ReplaceFloor = true);

/// <summary>
/// Explicit ledger of the 77 authored catalog scenes. Unknown scenes intentionally
/// have no environment instead of receiving an inappropriate generic warehouse.
/// Accent colors belong to decorative furniture, never functional lamps or controls.
/// </summary>
public static class SceneEnvironmentProfiles
{
    public static IReadOnlyDictionary<string, SceneEnvironmentProfile> Profiles { get; } =
        new ReadOnlyDictionary<string, SceneEnvironmentProfile>(
            new Dictionary<string, SceneEnvironmentProfile>(StringComparer.Ordinal)
            {
                ["conveyor-cell"] = P(SceneEnvironmentFamily.DispatchWarehouse, "inspection", "735C43"),
                ["equipment-gallery"] = P(SceneEnvironmentFamily.EquipmentGallery, "gallery", "65584E"),
                ["lab-10-01-drive-alarm-code-string"] = P(SceneEnvironmentFamily.MachineShop, "drives", "53684E"),
                ["lab-10-02-chicken-label-print"] = P(SceneEnvironmentFamily.DispatchWarehouse, "food", "768477"),
                ["lab-10-03-vision-package-sorter"] = P(SceneEnvironmentFamily.DispatchWarehouse, "inspection", "7D654D", 0.8f),
                ["lab-10-04-motor-enum-state"] = P(SceneEnvironmentFamily.MachineShop, "drives", "6C5B49"),
                ["lab-10-05-motor-struct-data"] = P(SceneEnvironmentFamily.MachineShop, "drives", "59694F"),
                ["lab-10-06-ten-motor-array-startup"] = P(SceneEnvironmentFamily.MachineShop, "drives", "805E48"),
                ["lab-11-06-wastewater-collection"] = P(SceneEnvironmentFamily.OutdoorUtility, "wastewater", "69774D", 0.8f, false),
                ["lab-11-07-multi-conveyor-pallet-route"] = P(SceneEnvironmentFamily.DispatchWarehouse, "receiving", "796044", 0.8f),
                ["lab-11-11-service-elevator"] = P(SceneEnvironmentFamily.PublicInterior, "elevator", "79634C", 1.0f, false),
                ["lab-11-12-mobile-traffic-lights"] = P(SceneEnvironmentFamily.OutdoorTraffic, "mobile", "777148", 1.5f),
                ["lab-11-13-xy-palletizing"] = P(SceneEnvironmentFamily.DispatchWarehouse, "palletizing", "8A6B35", 1.5f),
                ["lab-11-19-powder-batch-mixer"] = P(SceneEnvironmentFamily.ProcessUtility, "powder", "73694D", 0.5f),
                ["lab-2-01-workstation-call"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "754C46"),
                ["lab-2-02-dual-confirmation"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "65704E"),
                ["lab-2-03-service-marker-inhibit"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "78664C"),
                ["lab-2-04-two-station-call"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "765A48"),
                ["lab-2-05-bay-light-selector"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "67734F"),
                ["lab-2-06-ready-attention"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "795748"),
                ["lab-2-07-dual-contact-permissive"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "5E704F"),
                ["lab-2-08-inspection-vote"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "856448"),
                ["lab-2-09-maintenance-beacon"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "754F48"),
                ["lab-2-10-dust-collector-seal-in"] = P(SceneEnvironmentFamily.MachineShop, "extraction", "68714D"),
                ["lab-2-11-inbound-tote-stop"] = P(SceneEnvironmentFamily.DispatchWarehouse, "receiving", "7C6447"),
                ["lab-2-12-assembly-lift"] = P(SceneEnvironmentFamily.MachineShop, "assembly", "806443", 0.5f),
                ["lab-2-13-coolant-jug-fill"] = P(SceneEnvironmentFamily.DispatchWarehouse, "chemical", "62704E"),
                ["lab-2-14-sump-pump"] = P(SceneEnvironmentFamily.ProcessUtility, "water", "6E7853", 0.5f, false),
                ["lab-2-15-fume-extractor"] = P(SceneEnvironmentFamily.MachineShop, "extraction", "7A604A"),
                ["lab-2-16-safe-drill"] = P(SceneEnvironmentFamily.MachineShop, "machining", "64704E", 0.5f),
                ["lab-2-17-pallet-robot"] = P(SceneEnvironmentFamily.DispatchWarehouse, "palletizing", "8A6941", 1.5f),
                ["lab-2-18-pallet-pickup"] = P(SceneEnvironmentFamily.DispatchWarehouse, "dispatch", "795B43", 0.8f),
                ["lab-2-19-service-door"] = P(SceneEnvironmentFamily.PublicInterior, "access", "746448", 0.5f),
                ["lab-2-20-bottle-shuttle"] = P(SceneEnvironmentFamily.DispatchWarehouse, "packaging", "6F7958", 0.8f),
                ["lab-2-21-tote-finishing"] = P(SceneEnvironmentFamily.DispatchWarehouse, "chemical", "5D7250", 0.8f),
                ["lab-2-22-dual-spindle"] = P(SceneEnvironmentFamily.MachineShop, "machining", "7C6047", 0.8f),
                ["lab-2-23-parcel-sorter"] = P(SceneEnvironmentFamily.DispatchWarehouse, "dispatch", "806343", 0.8f),
                ["lab-2-24-robot-cnc"] = P(SceneEnvironmentFamily.MachineShop, "machining", "64704D", 1.5f),
                ["lab-2-25-inspection-toggle"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "78554A"),
                ["lab-3-01-guarded-pallet-transfer"] = P(SceneEnvironmentFamily.DispatchWarehouse, "receiving", "80704C", 0.8f),
                ["lab-3-02-robot-cell-safe-restart"] = P(SceneEnvironmentFamily.MachineShop, "machining", "68764F", 1.5f),
                ["lab-4-01-press-count-lamp"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "804C43"),
                ["lab-4-02-counter-reset-lamp"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "766346"),
                ["lab-4-03-repeat-cycle-counter"] = P(SceneEnvironmentFamily.MachineShop, "assembly", "6C754F", 0.5f),
                ["lab-4-04-sequence-light-tower"] = P(SceneEnvironmentFamily.MachineShop, "assembly", "816445"),
                ["lab-4-05-dual-input-count-window"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "77544A"),
                ["lab-4-06-multi-press-confirmation"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "65724E"),
                ["lab-4-07-parking-garage-entry"] = P(SceneEnvironmentFamily.OutdoorTraffic, "parking", "82734C", 1.5f),
                ["lab-4-08-package-grouping"] = P(SceneEnvironmentFamily.DispatchWarehouse, "packaging", "806241", 0.8f),
                ["lab-4-09-chain-drive-lift"] = P(SceneEnvironmentFamily.PublicInterior, "elevator", "7B6347", 1.0f, false),
                ["lab-4-10-cookie-packaging"] = P(SceneEnvironmentFamily.DispatchWarehouse, "food", "798373", 0.5f),
                ["lab-4-11-barrel-fill-station"] = P(SceneEnvironmentFamily.DispatchWarehouse, "chemical", "69754E", 0.5f),
                ["lab-4-12-cable-cut-length"] = P(SceneEnvironmentFamily.MachineShop, "assembly", "806346", 0.8f),
                ["lab-5-01-delayed-lamp"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "62764F"),
                ["lab-5-02-timed-lamp-off"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "78554A"),
                ["lab-5-03-rotary-flasher"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "7C6949"),
                ["lab-5-04-alternating-lamps"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "66754F"),
                ["lab-5-05-variable-flash-rate"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "77574A"),
                ["lab-5-06-running-light-tower"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "806B49"),
                ["lab-5-07-pedestrian-crossing"] = P(SceneEnvironmentFamily.OutdoorTraffic, "crossing", "7C7550", 1.0f),
                ["lab-5-08-drawbridge-control"] = P(SceneEnvironmentFamily.OutdoorTraffic, "waterfront", "697650", 1.5f, false),
                ["lab-5-09-bag-indexing-conveyor"] = P(SceneEnvironmentFamily.DispatchWarehouse, "packaging", "7F6545", 0.8f),
                ["lab-5-10-coating-line"] = P(SceneEnvironmentFamily.MachineShop, "coating", "747D59", 0.5f),
                ["lab-6-07-luggage-weight-sort"] = P(SceneEnvironmentFamily.PublicInterior, "baggage", "7B6D55", 0.8f),
                ["lab-6-08-hand-dryer"] = P(SceneEnvironmentFamily.PublicInterior, "washroom", "858575"),
                ["lab-9-01-sum-function"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "7C584A"),
                ["lab-9-02-product-function"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "68754F"),
                ["lab-9-03-sum-and-counter-function"] = P(SceneEnvironmentFamily.TrainingWorkshop, "controls", "816C49"),
                ["lab-9-04-function-selector"] = P(SceneEnvironmentFamily.TrainingWorkshop, "commissioning", "7A5449"),
                ["lab-9-10-box-volume"] = P(SceneEnvironmentFamily.DispatchWarehouse, "inspection", "786448"),
                ["lab-9-11-pallet-counting"] = P(SceneEnvironmentFamily.DispatchWarehouse, "receiving", "63764F", 0.5f),
                ["lab-9-12-ev-charging-manager"] = P(SceneEnvironmentFamily.OutdoorTraffic, "charging", "6B7955", 1.0f),
                ["scene-1-conveyor-stop"] = P(SceneEnvironmentFamily.DispatchWarehouse, "dispatch", "66764F"),
                ["scene-2-conveyor-pusher"] = P(SceneEnvironmentFamily.DispatchWarehouse, "dispatch", "826449", 0.8f),
                ["tank-high-low"] = P(SceneEnvironmentFamily.ProcessUtility, "water", "75815B", 0.5f),
                ["tank-level"] = P(SceneEnvironmentFamily.ProcessUtility, "water", "796C4D", 0.5f),
                ["tank-radar"] = P(SceneEnvironmentFamily.ProcessUtility, "water", "6C7953", 0.5f),
            });

    public static bool TryGet(string sceneId, out SceneEnvironmentProfile profile) =>
        Profiles.TryGetValue(sceneId, out profile!);

    private static SceneEnvironmentProfile P(SceneEnvironmentFamily family, string variant,
        string accentHex, float margin = 0, bool replaceFloor = true) =>
        new(family, variant, accentHex, margin, replaceFloor);
}
