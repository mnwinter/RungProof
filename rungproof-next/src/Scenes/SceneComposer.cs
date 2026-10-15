using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public sealed record SceneComposition(
    Node3D Root,
    IReadOnlyList<string> RenderedEquipmentIds,
    IReadOnlyList<string> DeferredEquipmentIds
);

public static partial class SceneComposer
{
    public static SceneComposition Compose(
        SceneDefinition scene,
        AssetCatalogDocument candidates,
        bool runCommands
    )
    {
        if(Text(scene.Simulation,"type","")=="workspaceFiniteBelt")return ComposeWorkspaceBeltPreset(scene,candidates);
        if(Text(scene.Simulation,"type","")=="workspaceFiniteRoller")return ComposeWorkspaceRollerPreset(scene,candidates);
        var root = new Node3D { Name = $"Scene_{SafeNodeName(scene.Id)}" };
        var rendered = new List<string>();
        var deferred = new List<string>();

        foreach (var equipment in scene.Equipment)
        {
            var node = equipment.Type switch
            {
                "conveyor" when Text(equipment.Config, "installation", string.Empty) == "chainLiftFeed" => CreateChainLiftFeed(equipment, candidates),
                "conveyor" => CreateConveyor(equipment, candidates, runCommands),
                "box" when Text(equipment.Config, "installation", "") == "palletizerCarton" => CreatePalletizerCarton(),
                "box" => CreateSceneLoad(equipment, candidates),
                "palletLoad" => CreateMappedAsset(equipment, candidates,
                    "loads.palletized-cases.gma-48x40.v1"),
                "toteFiller" when Text(equipment.Config, "installation", string.Empty) == "conveyorJug" =>
                    CreateConveyorJugFiller(equipment, candidates, runCommands),
                "toteFiller" or "toteCapper" or "toteLabeler" or "toteVision"
                    when Text(equipment.Config, "installation", string.Empty) == "toteFinishingLine" =>
                    CreateToteFinishingStation(equipment, candidates, runCommands),
                "toteFiller" => CreateControlledAsset(equipment, candidates,
                    "process.packaging.filler.tote-volumetric.v1", runCommands,
                    EquipmentMotionController.MotionKind.LinearY, "KIN_fill_nozzle",
                    travelM: -0.12f, travelTimeSeconds: 0.35f),
                "toteCapper" => CreateControlledAsset(equipment, candidates,
                    "process.packaging.capper.tote-inline.v1", runCommands,
                    EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_capper_spindle", speedRpm: 180.0f),
                "toteLabeler" => CreateControlledAsset(equipment, candidates,
                    "process.packaging.labeler.tote-pressure-sensitive.v1", runCommands,
                    EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_label_roll", speedRpm: 55.0f),
                "toteVision" => CreateControlledAsset(equipment, candidates,
                    "inspection.vision.tote-multicamera.v1", runCommands,
                    EquipmentMotionController.MotionKind.OscillatingRotation, "KIN_vision_lens", travelDegrees: 8.0f),
                "meteringSkid" => CreateControlledAsset(equipment, candidates,
                    "process.dosing.skid.liquid-metering.v1", runCommands,
                    EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_metering_pump_shaft",
                    speedRpm: 1750.0f, rotationAxis: Vector3.Right),
                "containerReceiver" => CreateContainerReceiver(equipment, candidates),
                "photoeye" => CreatePhotoeyeAsset(equipment, candidates),
                "sizeSensorBank" => CreateSizeSensorBank(equipment, candidates),
                "switch" => CreateSwitchAsset(scene.Id, equipment, candidates),
                "indicator" => CreateIndicatorAsset(equipment, candidates),
                "pusher" => CreatePusherAsset(equipment, candidates, runCommands,
                    (float)Number(scene.Simulation, "pusherStrokeTimeS", 0.8)),
                "motor" => CreateControlledAsset(equipment, candidates, "drives.motor.ac-induction.v1", runCommands,
                    EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_motor_", speedRpm: 1450.0f,
                    rotationAxis: Vector3.Right),
                "pipe" => CreateMappedAsset(equipment, candidates, "process.pipe.flanged-spool.v1"),
                "tank" when Text(equipment.Config, "installation", string.Empty) == "barrelSupply" => CreateBarrelFillPart("barrelSupply"),
                "tank" when Text(equipment.Config, "installation", "") == "sumpPiping" => CreateOpenSump(),
                "tank" => CreateTankAsset(equipment, candidates),
                "pump" => CreateControlledAsset(equipment, candidates, "process.pump.centrifugal-skid.v1", runCommands,
                    EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_pump_shaft", speedRpm: 1450.0f,
                    rotationAxis: Vector3.Right),
                "valve" when Text(equipment.Config, "installation", string.Empty) == "barrelValve" => CreateBarrelFillPart("barrelValve"),
                "valve" when Text(equipment.Config, "installation", string.Empty) == "wastewaterOutlet" => CreateWastewaterOutletValve(equipment, candidates),
                "valve" => CreateControlledAsset(equipment, candidates, "process.valve.actuated-ball.v1", runCommands,
                    EquipmentMotionController.MotionKind.PositionRotation, "KIN_valve_stem", travelDegrees: 90.0f, travelTimeSeconds: 1.2f),
                "levelSensor" => CreateLevelSensorAsset(equipment, candidates),
                "radarLevelSensor" => CreateMappedAsset(equipment, candidates, "sensing.level.radar.v1"),
                "rotarySwitch" => CreateSelectorAsset(equipment, candidates),
                "fan" => CreateControlledAsset(equipment, candidates, "air-handling.fan.axial-1900.v1", runCommands,
                    EquipmentMotionController.MotionKind.FanRotor, "KIN_", speedRpm: 720.0f),
                "liftTable" when Text(equipment.Config, "installation", string.Empty) == "guidedChainLift" => CreateGuidedChainLift(),
                "liftTable" when Text(equipment.Config, "installation", string.Empty) == "serviceElevator" => CreateServiceElevator(),
                "liftTable" => CreateLiftAsset(equipment, candidates, runCommands),
                "drillPress" when Text(equipment.Config, "installation", string.Empty) == "fixtureDrill" =>
                    CreateControlledAsset(equipment, candidates, "machining.drill-press.pedestal.v1", runCommands,
                        EquipmentMotionController.MotionKind.SpindleFeed, "KIN_spindle", speedRpm: 900.0f,
                        travelM: -0.245f),
                "drillPress" when Text(equipment.Config, "installation", string.Empty) == "dualSpindleHead" =>
                    CreateDualSpindleHead(equipment, candidates, runCommands),
                "drillPress" => CreateControlledAsset(equipment, candidates, "machining.drill-press.pedestal.v1", runCommands,
                    EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_spindle", speedRpm: 900.0f),
                "robotArm" when Text(equipment.Config, "installation", string.Empty) == "palletHandlingRobot" =>
                    CreatePalletHandlingRobot(equipment, candidates),
                "robotArm" when Text(equipment.Config, "installation", string.Empty) == "cncTendingRobot" =>
                    CreateCncTendingRobot(equipment, candidates),
                "robotArm" => CreateControlledAsset(equipment, candidates, "robotics.robot.six-axis-medium.v1", runCommands,
                    EquipmentMotionController.MotionKind.OscillatingRotation, "KIN_axis_1", travelDegrees: 55.0f),
                "rotaryTable" => CreateRotaryTable(equipment, candidates, runCommands),
                "rollerShutter" => CreateShutterAsset(equipment, candidates, runCommands),
                "machine" when Text(equipment.Config, "installation", string.Empty) == "cookieSealer" => CreateCookiePackagingPart("cookieSealer"),
                "machine" when Text(equipment.Config, "installation", "") == "cableFeed" => CreateCableCutPart("cableFeed"),
                "machine" when Text(equipment.Config, "installation", "") == "repeatCycleCnc" => CreateRepeatCycleMachine(equipment, candidates),
                "machine" => equipment.Label.Contains("Hand-Dryer", StringComparison.OrdinalIgnoreCase)
                    ? CreateHandDryerAsset()
                    : CreateControlledAsset(equipment, candidates, "machining.machine.enclosed-center.v1", runCommands,
                        EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_spindle", speedRpm: 3200.0f),
                "sceneInstallation" when Text(equipment.Config, "installation", string.Empty) is "wastewaterCollector" or "multiConveyorHandoffs" => new Node3D(),
                "sceneInstallation" => CreateChainLiftAccessory(Text(equipment.Config, "installation", string.Empty)),
                "trainingAccessory" => CreateTrainingAccessory(equipment, candidates),
                _ => null,
            };

            if (node is null)
            {
                deferred.Add(equipment.Id);
                continue;
            }

            node.Name = SafeNodeName(equipment.Id);
            node.Position = Vector(equipment.Position);
            if (equipment.Rotation is { Length: >= 3 })
            {
                node.RotationDegrees = Vector(equipment.Rotation);
            }
            if (equipment.Scale is { Length: >= 3 })
            {
                node.Scale = Vector(equipment.Scale);
            }
            root.AddChild(node);
            rendered.Add(equipment.Id);
        }

        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "multiConveyorHandoffs"))
            ConfigureMultiConveyorHandoffs(root);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "visionSorterHandoffBridge"))
            ConfigureVisionSorterSplices(root);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "wastewaterCollector"))
            ConfigureWastewaterPiping(root);
        if (scene.Simulation.TryGetProperty("controllerToteTransfer", out var toteProcess)
            && toteProcess.ValueKind == JsonValueKind.True)
            ConfigureToteFinishingProcess(root);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "sumpPiping"))
            ConfigureSumpPiping(root);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "fixtureDrill"))
            ConfigureFixtureDrill(root);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "tankSwitchMounts"))
            if (scene.Id == "lab-2-14-sump-pump") ConfigureSumpFloats(root, scene);
            else ConfigureTankSwitchMounts(root, scene);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "roofAnalogProbe"))
            ConfigureTankAnalogMount(root, scene);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "tankPiping"))
            ConfigureTankPiping(root, scene);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "palletOutbound"))
            ConfigurePalletOutbound(root);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "dualSpindleSlide"))
            root.GetNode<Node3D>("plate_transfer").FindChildren("*", "", true, false)
                .OfType<EquipmentMotionController>().Single().PositionFollower = root.GetNode<Node3D>("metal_plate");
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "cncTendingStock"))
            ConfigureCncTendingStock(root, scene);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "robotRestartGuard"))
            ConfigureRobotRestartInterlock(root);
        if (scene.Equipment.Any(equipment => Text(equipment.Config, "installation", string.Empty) == "guidedChainLift") && !scene.Simulation.TryGetProperty("chainLiftPlant", out _))
            root.GetNode<Node3D>("liftTable_1").GetNode<ChainLiftDriveVisual>("ChainLiftMotion").PositionFollower = root.GetNode<Node3D>("box_2");
        if (scene.Simulation.TryGetProperty("chainLiftPlant", out _)) ConfigureChainLiftReceiver(root);
        if (scene.Simulation.TryGetProperty("cookiePackagingPlant", out _)) ConfigureCookieConveyor(root);
        if (scene.Simulation.TryGetProperty("barrelFillPlant", out _)) ConfigureBarrelConveyor(root);
        if (scene.Id == "lab-5-10-coating-line") ConfigureCoatingInstallation(root);
        if (scene.Id == "lab-6-08-hand-dryer") ConfigureHandDryerInstallation(root);
        if (scene.Id == "lab-6-07-luggage-weight-sort") ConfigureLuggageInstallation(root);
        // Establish supported operator installations before Ready, runtime
        // initial-pose capture, and ray-pick/feedback bindings are constructed.
        ConfigureContextInstallation(root, scene);
        ConfigureMotorTestFixtures(root, scene);
        ConfigureMotorConveyorZones(root, scene);
        ConfigureControlInstallation(root, scene);
        ConfigureAccessorySupports(root, scene);
        ApplyTemplateOverride(root,scene,scene.TemplateOverride);
        return new SceneComposition(root, rendered, deferred);
    }

    private static Node3D CreateConveyor(
        SceneEquipment equipment,
        AssetCatalogDocument candidates,
        bool runCommand
    )
    {
        const string assetId = "material-handling.belt-conveyor.600x6000.v1";
        var asset = candidates.Assets.FirstOrDefault(item => item.Id == assetId)
            ?? throw new InvalidOperationException($"Required scene asset is missing: {assetId}");
        var packed = ResourceLoader.Load<PackedScene>(asset.Model.DeliveryGltf)
            ?? throw new InvalidOperationException($"Unable to load scene asset {asset.Model.DeliveryGltf}.");
        var model = packed.Instantiate<Node3D>();

        var requestedLength = Number(equipment.Config, "length", 6.0);
        var requestedWidth = Number(equipment.Config, "width", 0.6);
        var requestedHeight = Number(equipment.Config, "deckHeight", 1.055);
        if (!double.IsFinite(requestedLength) || !double.IsFinite(requestedWidth)
            || !double.IsFinite(requestedHeight) || requestedLength <= 0 || requestedWidth <= 0 || requestedHeight <= 0)
            throw new InvalidOperationException($"Conveyor '{equipment.Id}' requires positive finite dimensions.");
        // Width describes the carrying belt, not the overall motor/leg envelope.
        // The authored foot bottoms are at 0.04 m and the belt top at 1.055 m.
        // Size above the feet, then ground the model; keep equipment.Scale separate.
        var heightScale = (float)(requestedHeight / (1.055 - 0.04));
        model.Scale = new Vector3((float)(requestedLength / 6.0), heightScale, (float)(requestedWidth / 0.6));
        model.Position = new Vector3(0, -0.04f * heightScale, 0);
        model.AddChild(new ConveyorController
        {
            Name = "ConveyorController",
            RunCommand = runCommand,
            EstopOk = true,
            SpeedSetpointMps = 0.65f,
        });
        var root = new Node3D();
        root.AddChild(model);
        return root;
    }

    private static Node3D CreateRotaryTable(SceneEquipment equipment, AssetCatalogDocument candidates, bool runCommand)
    {
        if (!equipment.Config.TryGetProperty("parcelTransfer", out var variant) || variant.ValueKind != JsonValueKind.True)
            return CreateControlledAsset(equipment, candidates, "material-handling.table.powered-rotary.v1", runCommand,
                EquipmentMotionController.MotionKind.ContinuousRotation, "KIN_table", speedRpm: 8.0f);

        var model = CreateMappedAsset(equipment, candidates, "material-handling.table.powered-rotary.v1");
        // This illustrative parcel variant omits the modeled machining fixture:
        // its load path needs a flat surface without jaws or raised markers.
        foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            var name = mesh.Name.ToString();
            if (name.StartsWith("TABLE_fixture_", StringComparison.Ordinal)
                || name.StartsWith("TABLE_index_", StringComparison.Ordinal)
                || name == "TABLE_center_register") mesh.Visible = false;
            if (name.StartsWith("TABLE_radial_slot_", StringComparison.Ordinal))
            {
                mesh.Scale = new Vector3(mesh.Scale.X, mesh.Scale.Y * 0.1f, mesh.Scale.Z);
                // These meshes are children of the platen pivot; lower their
                // local height by 17 mm rather than assigning a world height.
                mesh.Position -= Vector3.Up * 0.017f;
            }
        }
        var radius = Number(equipment.Config, "radius", 1.28);
        var deckHeight = Number(equipment.Config, "deckHeight", 0.93);
        if (!double.IsFinite(radius) || !double.IsFinite(deckHeight) || radius <= 0 || deckHeight <= 0)
            throw new InvalidOperationException($"Parcel table '{equipment.Id}' requires positive finite radius/deckHeight.");
        model.Scale = new Vector3((float)(radius / 1.28), (float)(deckHeight / 0.93), (float)(radius / 1.28));
        model.AddChild(new EquipmentMotionController
        {
            Name = "ParcelTableMotion", Kind = EquipmentMotionController.MotionKind.PositionRotation,
            TargetPrefix = "KIN_table", TravelDegrees = 90, RunCommand = runCommand,
            PositionInputSlewSeconds = 1.2f,
        });
        var root = new Node3D();
        root.AddChild(model);
        return root;
    }

    private static Node3D CreateSizeSensorBank(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "sensing.dimensioning.parcel-three-height.v1");
        var clearance = Number(equipment.Config, "portalClearHeight", 2.09);
        if (!double.IsFinite(clearance) || clearance <= 0.12)
            throw new InvalidOperationException($"Sensor portal '{equipment.Id}' requires positive clearance above its base.");
        var rise = (float)(clearance - 2.09);
        var authoredHeights = new[] { 0.95f, 1.30f, 1.65f };
        var heights = equipment.Config.TryGetProperty("beamHeightsM", out var configured)
            ? configured.EnumerateArray().Select(value => value.GetSingle()).ToArray() : authoredHeights;
        if (heights.Length != 3 || heights.Any(height => !float.IsFinite(height) || height <= 0.12f || height + 0.11f >= clearance)
            || heights[0] >= heights[1] || heights[1] >= heights[2])
            throw new InvalidOperationException($"Sensor portal '{equipment.Id}' requires three ascending beam heights below its header.");
        foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            var name = mesh.Name.ToString();
            if (name.StartsWith("PORTAL_POST_", StringComparison.Ordinal))
            {
                mesh.Scale = new Vector3(mesh.Scale.X, mesh.Scale.Y * (2.12f + rise) / 2.12f, mesh.Scale.Z);
                mesh.Position += Vector3.Up * rise / 2;
            }
            else if (name == "PORTAL_HEADER" || name.StartsWith("HEIGHT_", StringComparison.Ordinal))
                mesh.Position += Vector3.Up * rise;
            for (var channel = 1; channel <= 3; channel++)
                if (name.StartsWith($"SENSOR_HOUSING_{channel}_", StringComparison.Ordinal)
                    || name.StartsWith($"LENS_{channel}_", StringComparison.Ordinal) || name == $"KIN_beam_size_{channel}")
                    mesh.Position += Vector3.Up * (heights[channel - 1] - authoredHeights[channel - 1]);
        }
        return model;
    }

    private static Node3D CreateMappedAsset(
        SceneEquipment equipment,
        AssetCatalogDocument candidates,
        string assetId
    )
    {
        var asset = candidates.Assets.FirstOrDefault(item => item.Id == assetId)
            ?? throw new InvalidOperationException($"Required scene asset is missing: {assetId}");
        var packed = ResourceLoader.Load<PackedScene>(asset.Model.DeliveryGltf)
            ?? throw new InvalidOperationException($"Unable to load scene asset {asset.Model.DeliveryGltf}.");
        var model = packed.Instantiate<Node3D>();
        return model;
    }

    // These installations have their own scene-local geometry constructors;
    // they do not require an imported catalog asset. Keep this explicit so a
    // misspelled or unsupported installation still produces a diagnostic.
    public static bool IsProceduralTrainingAccessory(SceneEquipment equipment)
        => equipment.Type == "trainingAccessory" && Text(equipment.Config, "installation", string.Empty) is
            "visionSorterHandoffBridge" or "luggageTransferBridge" or
            "groupingLine" or "groupingStop" or
            "parkingVehicle" or "parkingBarrier" or "parkingPad" or
            "palletizerPickTable" or
            "cablePayoff" or "cableGuide" or "cableEncoder" or "cableCutter" or "cableReceiver" or "cableStrand" or
            "barrelLoad" or "barrelMeter" or "barrelNozzle" or
            "cookieTray" or "cookieCounter" or "robotRestartGuard" or "robotRestartPanel";

    private static Node3D? CreateTrainingAccessory(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        if (Text(equipment.Config, "installation", "") == "visionSorterHandoffBridge") return CreateVisionSorterBridge(equipment);
        if (Text(equipment.Config, "installation", "") == "luggageTransferBridge") return CreateLuggageTransferBridge();
        if (Text(equipment.Config, "installation", "") is "groupingLine" or "groupingStop") return CreatePackageGroupingPart(Text(equipment.Config, "installation", ""));
        if (Text(equipment.Config, "installation", "") is "parkingVehicle" or "parkingBarrier" or "parkingPad") return CreateParkingEntryPart(Text(equipment.Config, "installation", ""));
        if (Text(equipment.Config, "installation", "") == "palletizerPickTable") return CreatePalletizerPickTable();
        if (Text(equipment.Config, "installation", "") is "cablePayoff" or "cableGuide" or "cableEncoder" or "cableCutter" or "cableReceiver" or "cableStrand") return CreateCableCutPart(Text(equipment.Config, "installation", ""));
        if (Text(equipment.Config, "installation", string.Empty) is "barrelLoad" or "barrelMeter" or "barrelNozzle") return CreateBarrelFillPart(Text(equipment.Config, "installation", string.Empty));
        if (Text(equipment.Config, "installation", string.Empty) is "cookieTray" or "cookieCounter") return CreateCookiePackagingPart(Text(equipment.Config, "installation", string.Empty));
        if (Text(equipment.Config, "installation", string.Empty) == "robotRestartGuard") return CreateRobotRestartGuard();
        if (Text(equipment.Config, "installation", string.Empty) == "robotRestartPanel") return CreateRobotRestartPanel();
        if (Text(equipment.Config, "installation", string.Empty) == "palletTransferBridge") return new Node3D();
        var model = CreateOptionalMappedAsset(equipment, candidates, Text(equipment.Config, "catalogAssetId", string.Empty));
        var installation = Text(equipment.Config, "installation", string.Empty);
        if (model is not null && installation == "mobileTrafficSignal") return CreateMobileTrafficSignalBase(model);
        if (model is not null && installation is "drawbridgeDeck" or "drawbridgeGate" or "drawbridgeLimits")
            return AddDrawbridgePivot(model, installation);
        if (model is not null && installation == "drawbridgeSignal") return CreateDrawbridgeSignal(model);
        var display = Text(equipment.Config, "display", string.Empty);
        if (model is not null && display is "palletCount" or "numeric" or "lengthNumeric" or "remainingSeconds" or "weightKg" or "energyKwh" or "staticText")
        {
            // Only explicitly configured equipment opts into a live readout. The reusable
            // asset retains its honest NO LIVE VALUE legend when used alone.
            var legend = model.FindChild(display == "energyKwh" ? "METER_static_legend" : display is "remainingSeconds" or "weightKg" ? "STATIC_READOUT_static_legend" : display == "lengthNumeric" ? "LENGTH_DISPLAY_static_legend" : "COUNT_DISPLAY_static_legend", true, false) as MeshInstance3D
                ?? throw new InvalidOperationException($"Count display '{equipment.Id}' is missing its authored legend.");
            legend.Visible = false;
            model.AddChild(new Label3D
            {
                Name = display == "staticText" ? "StaticReadout" : "NumericReadout",
                Text = display == "staticText" ? Text(equipment.Config, "displayText", "NO DATA")
                    : Text(equipment.Config, "displayLabel", "COUNT") + "\n0",
                Position = display == "energyKwh" ? new Vector3(0,1.42f,.135f) : new Vector3(0, 1.51f, 0.09f),
                FontSize = display == "energyKwh" ? 30 : 48, PixelSize = display == "energyKwh" ? .001f : .0014f, OutlineSize = 0, Modulate = Colors.White,
            });
        }
        if (model is not null && Text(equipment.Config, "motion", string.Empty) == "gantryCommandSweep")
            model.AddChild(new EquipmentMotionController
            {
                Name = "GantryCommandMotion",
                Kind = EquipmentMotionController.MotionKind.CartesianGantry,
                TravelM = 0.7f,
                TravelTimeSeconds = 1.5f,
            });
        return model;
    }

    private static Node3D? CreateOptionalMappedAsset(
        SceneEquipment equipment,
        AssetCatalogDocument candidates,
        string assetId)
    {
        var asset = candidates.Assets.FirstOrDefault(item => item.Id == assetId);
        if (asset is null) return null;
        if (ResourceLoader.Exists(asset.Model.DeliveryGltf))
            return CreateMappedAsset(equipment, candidates, assetId);

        // Restored candidate packages may already have a valid Godot import in
        // .godot/imported while their generated sidecar is intentionally
        // ignored. Use that editor cache for source-worktree review; packaged
        // builds continue to resolve the normal res:// GLB import path.
        var fileName = System.IO.Path.GetFileName(asset.Model.DeliveryGltf);
        var imported = DirAccess.GetFilesAt("res://.godot/imported")
            .FirstOrDefault(name => name.StartsWith(fileName + "-", StringComparison.Ordinal)
                && name.EndsWith(".scn", StringComparison.Ordinal));
        if (imported is null) return null;
        var packed = ResourceLoader.Load<PackedScene>($"res://.godot/imported/{imported}");
        return packed?.Instantiate<Node3D>();
    }

    private static Node3D CreateHandDryerAsset()
    {
        var root = new Node3D { Name = "HandDryerAssembly" };
        var shell = Material(new Color("d8e0e4"), 0.58f, 0.24f);
        var dark = Material(new Color("17242b"), 0.18f, 0.30f);
        var sensor = Material(new Color("36d7ef"), 0.12f, 0.18f, false, 1.0f,
            new Color("36d7ef"), 2.2f);
        var heat = Material(new Color("f28b32"), 0.08f, 0.30f, false, 1.0f,
            new Color("f28b32"), 1.4f);
        AddBox(root, new Vector3(1.25f, 1.65f, 0.55f), new Vector3(0, 1.35f, 0), shell);
        AddBox(root, new Vector3(0.82f, 0.22f, 0.12f), new Vector3(0, 0.70f, 0.30f), dark);
        for (var index = -3; index <= 3; index++)
            AddBox(root, new Vector3(0.065f, 0.16f, 0.035f),
                new Vector3(index * 0.105f, 0.70f, 0.37f), heat, false);
        AddBox(root, new Vector3(0.34f, 0.12f, 0.045f), new Vector3(0, 1.45f, 0.31f), sensor, false);
        AddBox(root, new Vector3(0.72f, 0.09f, 0.04f), new Vector3(0, 1.82f, 0.30f), dark, false);
        return root;
    }

    private static Node3D CreateLevelSensorAsset(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var discrete = Text(equipment.Config, "sensorType", "analog") == "discrete";
        var model = CreateMappedAsset(equipment, candidates, discrete
            ? "sensing.level.tuning-fork.v1" : "sensing.level.analog-4-20ma.v1");
        if (equipment.Config.ValueKind != JsonValueKind.Object
            || !equipment.Config.TryGetProperty("displayStand", out var stand) || stand.ValueKind != JsonValueKind.True)
            return model;

        // Catalog sensor origins are process-fitting datums, not floor datums.
        // Only a standalone gallery display opts into this illustrative stand;
        // tank-mounted scenes retain their authored mounting transforms.
        var fittingHeight = discrete ? 1.225f : 2.78f; // Authored probe tip + 150 mm floor clearance.
        model.Position = new Vector3(0, fittingHeight, 0);
        var root = new Node3D();
        root.AddChild(model);
        var steel = Material(new Color("53646e"), 0.55f, 0.28f);
        var clampHeight = fittingHeight + (discrete ? 0.16f : 0.10f);
        AddBox(root, new Vector3(1.1f, 0.08f, 1.2f), new Vector3(0, 0.04f, 0.24f), steel).Name = "DISPLAY_STAND_base";
        AddBox(root, new Vector3(0.10f, clampHeight - 0.08f, 0.10f),
            new Vector3(0, (clampHeight + 0.08f) / 2, 0.48f), steel).Name = "DISPLAY_STAND_mast";
        var railX = discrete ? 0.145f : 0.24f;
        foreach (var side in new[] { -1, 1 })
            AddBox(root, new Vector3(0.06f, 0.04f, 0.54f),
                new Vector3(side * railX, clampHeight, 0.24f), steel).Name = $"DISPLAY_STAND_fitting_rail_{side}";
        AddBox(root, new Vector3(railX * 2 + 0.06f, 0.04f, 0.06f),
            new Vector3(0, clampHeight, 0.48f), steel).Name = "DISPLAY_STAND_crossbar";
        return root;
    }

    private static Node3D CreateTankAsset(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "process.tank.vertical-3000x5000.v1");
        var shell = model.FindChild("TANK_shell", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("Tank model has no cylindrical shell for sizing.");
        var shellSize = shell.GetAabb().Size;
        // Diameter/height describe the cylindrical process shell. The model's
        // roof, platform, legs and instrumentation remain in proportion.
        var diameter = Number(equipment.Config, "diameter", shellSize.X);
        var height = Number(equipment.Config, "height", shellSize.Y);
        if (!double.IsFinite(diameter) || !double.IsFinite(height) || diameter <= 0 || height <= 0)
            throw new InvalidOperationException($"Tank '{equipment.Id}' requires positive finite diameter/height.");
        model.Scale = new Vector3((float)(diameter / shellSize.X),
            (float)(height / shellSize.Y), (float)(diameter / shellSize.Z));
        // Keep configuration sizing separate from an authored equipment scale.
        var root = new Node3D();
        root.AddChild(model);
        return root;
    }

    private static Node3D CreatePhotoeyeAsset(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "sensing.photoelectric.through-beam.v1");
        // This dimension is measured from finished floor to the optical
        // centerline. It prevents a valid-looking sensor pair from putting
        // its beam through a conveyor rail or below the product envelope.
        var centerlineM = (float)Number(equipment.Config, "beamCenterHeightM", 0.94);
        // Span is the distance between stand centerlines. Translate the heads,
        // posts, feet and pigtails together; do not stretch the sensor housings.
        var spanM = (float)Number(equipment.Config, "span", 1.44);
        var positiveStandM = (float)Number(equipment.Config, "positiveStandPositionM", spanM / 2);
        if (!float.IsFinite(centerlineM) || centerlineM < 0.3f || !float.IsFinite(spanM) || spanM <= 0.3f
            || !float.IsFinite(positiveStandM) || positiveStandM <= 0.15f)
            throw new InvalidOperationException($"Photoeye '{equipment.Id}' requires finite positive stand span and optical height.");
        const float authoredCenterlineM = 0.94f;
        var liftM = centerlineM - authoredCenterlineM;
        var standOffset = (spanM - 1.44f) / 2;
        var installedSpan = positiveStandM + spanM / 2;
        var beamMidpoint = (positiveStandM - spanM / 2) / 2;

        foreach (var item in model.FindChildren("*", string.Empty, true, false).OfType<Node3D>())
        {
            var name = item.Name.ToString();
            if (name.StartsWith("TX_", StringComparison.Ordinal) || name.StartsWith("RX_", StringComparison.Ordinal))
                // Cable vertices are baked around a zero object origin. The
                // named stand, rather than origin sign, owns every part's side.
                item.Position += new Vector3(0, 0, name.StartsWith("TX_", StringComparison.Ordinal)
                    ? positiveStandM - 0.72f : -standOffset);
            if (name.StartsWith("KIN_beam", StringComparison.Ordinal))
            {
                item.Position = new Vector3(item.Position.X, item.Position.Y, item.Position.Z * installedSpan / 1.44f + beamMidpoint);
                item.Scale = new Vector3(item.Scale.X, item.Scale.Y, item.Scale.Z * installedSpan / 1.44f);
            }
            if (name.StartsWith("KIN_beam", StringComparison.Ordinal)
                || name.Contains("adjust_bracket", StringComparison.OrdinalIgnoreCase)
                || name.Contains("housing", StringComparison.OrdinalIgnoreCase)
                || name.Contains("lens", StringComparison.OrdinalIgnoreCase)
                || name.Contains("status_led", StringComparison.OrdinalIgnoreCase)
                || name.Contains("mount_bolt", StringComparison.OrdinalIgnoreCase))
            {
                item.Position += Vector3.Up * liftM;
            }
            else if (name.EndsWith("_post", StringComparison.OrdinalIgnoreCase))
            {
                // Grow from its existing floor-mounted base rather than
                // translating the entire sensor stand into the air.
                item.Position += Vector3.Up * (liftM * 0.5f);
                item.Scale = new Vector3(item.Scale.X, item.Scale.Y * (0.92f + liftM) / 0.92f, item.Scale.Z);
            }
            else if (name.EndsWith("_cable", StringComparison.OrdinalIgnoreCase) && item is MeshInstance3D cable)
            {
                // Stretch the head's tail in model vertical coordinates while
                // keeping its lower M12 connection fixed. Multiplying the basis
                // also works for imported meshes with an authored rotation.
                var bounds = cable.Transform * cable.GetAabb();
                var stretchY = (bounds.Size.Y + liftM) / bounds.Size.Y;
                var stretch = Basis.FromScale(new Vector3(1, stretchY, 1));
                cable.Transform = new Transform3D(stretch * cable.Basis,
                    stretch * cable.Position + Vector3.Up * bounds.Position.Y * (1 - stretchY));
            }
        }
        var positiveHeightM = (float)Number(equipment.Config, "positiveBeamCenterHeightM", centerlineM);
        if (!float.IsFinite(positiveHeightM) || positiveHeightM < 0.3f)
            throw new InvalidOperationException($"Photoeye '{equipment.Id}' requires a finite optical height >= 0.3 m.");
        if (MathF.Abs(positiveHeightM - centerlineM) > 1e-6f)
            ConfigureInclinedPhotoeye(model, centerlineM, positiveHeightM, -spanM / 2, positiveStandM);
        return model;
    }

    // This delivered valve pointer is parallel to the process bore at authored zero (OPEN).
    // Opt in only for this command-driven installation; other lessons keep their existing contract.
    private static Node3D CreateWastewaterOutletValve(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateControlledAsset(equipment, candidates, "process.valve.actuated-ball.v1", false,
            EquipmentMotionController.MotionKind.PositionRotation, "KIN_valve_stem", travelDegrees: 90,
            positionInputInverted: true);
        model.GetNode<EquipmentMotionController>("PositionRotationController").AutonomousPositionTravel = false;
        return model;
    }

    private static Node3D CreateControlledAsset(
        SceneEquipment equipment,
        AssetCatalogDocument candidates,
        string assetId,
        bool runCommand,
        EquipmentMotionController.MotionKind kind,
        string targetPrefix,
        float speedRpm = 90.0f,
        float travelM = 0.35f,
        float travelDegrees = 90.0f,
        float travelTimeSeconds = 1.0f,
        bool positionInputInverted = false,
        Vector3 rotationAxis = default
    )
    {
        var model = CreateMappedAsset(equipment, candidates, assetId);
        model.AddChild(new EquipmentMotionController
        {
            Name = $"{kind}Controller",
            Kind = kind,
            TargetPrefix = targetPrefix,
            RunCommand = runCommand,
            SpeedRpm = speedRpm,
            TravelM = travelM,
            TravelDegrees = travelDegrees,
            TravelTimeSeconds = travelTimeSeconds,
            PositionInputInverted = positionInputInverted,
            RotationAxis = rotationAxis == default ? Vector3.Up : rotationAxis,
        });
        return model;
    }

    private static Node3D CreateShutterAsset(
        SceneEquipment equipment,
        AssetCatalogDocument candidates,
        bool runCommand
    )
    {
        var model = CreateControlledAsset(equipment, candidates,
            "access-control.door.roller-shutter.v1", runCommand,
            EquipmentMotionController.MotionKind.RollerShutter, "KIN_", travelM: 2.8f,
            travelTimeSeconds: 2.5f, positionInputInverted: true);
        if (equipment.Config.TryGetProperty("sequencePositionOnly", out var sequenceOnly) && sequenceOnly.ValueKind == JsonValueKind.True)
            model.GetNode<EquipmentMotionController>("RollerShutterController").AutonomousPositionTravel = false;
        return model;
    }

    private static Node3D CreateLiftAsset(
        SceneEquipment equipment,
        AssetCatalogDocument candidates,
        bool runCommand
    )
    {
        return CreateControlledAsset(equipment, candidates,
            "material-handling.lift.scissor-table.v1", runCommand,
            EquipmentMotionController.MotionKind.ScissorLift, "KIN_",
            travelM: (float)Number(equipment.Config, "travel", 0.8),
            travelTimeSeconds: 2.0f);
    }

    private static Node3D CreateSelectorAsset(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var model = CreateMappedAsset(equipment, candidates, "controls.operator-station.selector.v1");
        var countValue = Number(equipment.Config, "positionCount", 4);
        var initialValue = Number(equipment.Config, "initialPosition", 0);
        if (!double.IsFinite(countValue) || countValue != Math.Truncate(countValue) || countValue < 2 || countValue > 4
            || !double.IsFinite(initialValue) || initialValue != Math.Truncate(initialValue) || initialValue < 0 || initialValue >= countValue)
            throw new InvalidOperationException($"Selector '{equipment.Id}' requires 2-4 positions and a valid initial ordinal.");
        var count = (int)countValue;
        string[]? positionLabels = null;
        if (equipment.Config.TryGetProperty("positionLabels", out var labels))
        {
            if (labels.ValueKind != JsonValueKind.Array || labels.GetArrayLength() != count
                || labels.EnumerateArray().Any(label => label.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(label.GetString())))
                throw new InvalidOperationException($"Selector '{equipment.Id}' needs one nonempty label per detent.");
            positionLabels = labels.EnumerateArray().Select(label => label.GetString()!).ToArray();
        }
        var handle = model.FindChild("KIN_selector_handle", true, false) as Node3D
            ?? throw new InvalidOperationException("Selector asset is missing its handle pivot.");
        // The delivered master has three fixed marks. Keep the master intact;
        // its composed variant must show only the detents this scene can select.
        foreach (var mesh in model.FindChildren("POSITION_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            mesh.Visible = false;
            mesh.Name = $"Authored_{mesh.Name}";
        }
        for (var ordinal = 0; ordinal < count; ordinal++)
        {
            var angle = Mathf.DegToRad(SelectorSwitchController.DetentDegrees(ordinal, count));
            var radial = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle), 0);
            model.AddChild(new MeshInstance3D
            {
                Name = $"POSITION_tick_{ordinal}",
                Mesh = new BoxMesh { Size = new Vector3(0.008f, 0.024f, 0.004f) },
                Position = new Vector3(0, 1.15f, 0.209f) + radial * 0.090f,
                Rotation = new Vector3(0, 0, angle),
                MaterialOverride = Material(Colors.Black, 0, 0.8f),
            });
            model.AddChild(new Label3D
            {
                Name = $"SelectorOrdinal_{ordinal}", Text = positionLabels?[ordinal] ?? ordinal.ToString(CultureInfo.InvariantCulture),
                // Put numbers beyond the dial rim so a projecting handle does
                // not cover its selected number in an oblique operator view.
                Position = new Vector3(0, 1.15f, 0.198f) + radial * 0.153f,
                FontSize = 32, PixelSize = 0.00085f, OutlineSize = 0, Modulate = Colors.White,
            });
        }
        var faceLabel = Text(equipment.Config, "faceLabel", string.Empty);
        if (faceLabel.Length > 0)
            model.AddChild(new Label3D
            {
                Name = "OperatorFaceLabel", Text = faceLabel, Position = new Vector3(0, 1.39f, .205f),
                FontSize = 40, PixelSize = .001f, OutlineSize = 0, Modulate = Colors.White,
            });
        var controller = new SelectorSwitchController { Name = "SelectorSwitchController" };
        model.AddChild(controller);
        controller.Configure(handle, count, (int)initialValue);
        return model;
    }

    private static Node3D CreateSwitchAsset(string sceneId, SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var emergency = Text(equipment.Config, "style", "pushbutton") == "emergency";
        var model = CreateMappedAsset(equipment, candidates, emergency
            ? "controls.operator-station.emergency-stop.v1"
            : "controls.operator-station.single-pushbutton.v1");
        if (!emergency && model.FindChild("KIN_pushbutton", true, false) is MeshInstance3D button)
        {
            button.MaterialOverride = Material(
                ColorFromInteger(equipment.Config, "color", 0x21a366), 0.05f, 0.25f);
        }
        // Retain the authored default plate unless the scene supplies its own
        // operator function. A validity/reset input must not say START.
        var faceLabel = ResolveOperatorFaceLabel(sceneId, equipment);
        if (!emergency && faceLabel.Length > 0
            && model.FindChild("BUTTON_start_label", true, false) is MeshInstance3D plateText)
        {
            plateText.Visible = false;
            model.AddChild(new Label3D
            {
                Name = "OperatorFaceLabel", Text = faceLabel,
                Position = plateText.Position + new Vector3(0, 0, 0.003f),
                FontSize = 32, PixelSize = 0.0006f, OutlineSize = 0,
                Modulate = Colors.White,
            });
        }
        return model;
    }

    private static Node3D CreateIndicatorAsset(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        var colors = StringArray(equipment.Config, "colors", new[] { "green" });
        if (colors.Length > 1)
        {
            var tower = CreateMappedAsset(equipment, candidates, "controls.stack-light.3-tier.v1");
            if (Text(equipment.Config, "installation", string.Empty) == "fourColorSequenceTower")
                AddSequenceTowerBlueTier(tower);
            return tower;
        }
        var model = CreateMappedAsset(equipment, candidates, "controls.beacon.single-tier.v1");
        if (model.FindChild("LENS_single", true, false) is MeshInstance3D lens)
        {
            var color = NamedSignalColor(colors[0]);
            var active = Text(equipment.Config, "active", string.Empty) == colors[0];
            lens.MaterialOverride = Material(color, 0.03f, 0.16f, false, 1.0f,
                color, active ? 5.0f : 0.03f);
        }
        return model;
    }

    // Extend the delivered three-tier assembly for this lesson only. Reuse
    // its lens/bezel geometry and lift the complete cap/sounder assembly so
    // the new tier occupies a real supported slot rather than overlaps a cap.
    private static void AddSequenceTowerBlueTier(Node3D tower)
    {
        var parts = tower.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToArray();
        var lens = parts.Single(part => part.Name == "LENS_red");
        var bezel = parts.Single(part => part.Name == "BEZEL_red_lower");
        void Raise(MeshInstance3D part)
        {
            var parentTransform = Transform3D.Identity;
            for (var parent = part.GetParent(); parent != tower; parent = parent.GetParent())
                if (parent is Node3D spatial) parentTransform = spatial.Transform * parentTransform;
            part.Position += parentTransform.Basis.Inverse() * new Vector3(0, 0.235f, 0);
        }
        foreach (var part in parts.Where(part => part.Name == "BEZEL_red_upper" || part.Name == "CAP"
                     || part.Name.ToString().StartsWith("BUZZER", StringComparison.Ordinal))) Raise(part);
        var blueLens = (MeshInstance3D)lens.Duplicate();
        blueLens.Name = "LENS_blue";
        lens.GetParent().AddChild(blueLens);
        Raise(blueLens);
        blueLens.MaterialOverride = Material(NamedSignalColor("blue").Darkened(0.72f), 0.03f, 0.18f);
        var blueBezel = (MeshInstance3D)bezel.Duplicate();
        blueBezel.Name = "BEZEL_blue_lower";
        bezel.GetParent().AddChild(blueBezel);
        Raise(blueBezel);
    }

    private static Node3D CreateSceneLoad(SceneEquipment equipment, AssetCatalogDocument candidates)
    {
        // The drill delivery already contains its clamped stock. The installation
        // adapter moves that mesh into this equipment root, preserving point/UI
        // identity without importing a second complete fixture through the table.
        if (Text(equipment.Config, "installation", string.Empty) == "drillStock") return new Node3D();
        if (Text(equipment.Config, "installation", string.Empty) == "cncTendingStock") return new Node3D();
        var size = NumberArray(equipment.Config, "size", new[] { 0.8, 0.7, 0.7 });
        var assetId = equipment.Id switch
        {
            "inbound_tote" or "container_a" or "container_b" => "loads.tote.reusable-plastic.v1",
            "coolant_jug" => "loads.container.jerry-can-hdpe.v1",
            "lift_fixture" => "tooling.fixture.modular-two-clamp.v1",
            "robot_pallet" => "loads.pallet.gma-48x40.v1",
            "shuttle_bottle" => "loads.container.process-bottle.v1",
            "finishing_tote" => Text(equipment.Config, "catalogAssetId", "loads.ibc.1000l.v1"),
            "drill_workpiece" or "metal_plate" => "tooling.fixture.clamped-plate.v1",
            "cnc_workpiece" => "tooling.workholding.machine-vise-stock.v1",
            _ => "loads.carton.corrugated-rsc.v1",
        };
        if (assetId.Length == 0) return CreatePrimitiveLoad(equipment, size);

        var asset = candidates.Assets.FirstOrDefault(item => item.Id == assetId)
            ?? throw new InvalidOperationException($"Required scene asset is missing: {assetId}");
        var model = CreateMappedAsset(equipment, candidates, assetId);
        if (equipment.Id == "finishing_tote" && equipment.Config.TryGetProperty("openForFill", out var openTote)
            && openTote.ValueKind == JsonValueKind.True)
        {
            var cap = model.FindChild("IBC_fill_cap", true, false) as MeshInstance3D
                ?? throw new InvalidOperationException("Open finishing tote requires a separate delivered fill cap.");
            cap.Visible = false;
            if (model.FindChild("IBC_fill_witness", true, false) is MeshInstance3D witness) witness.Visible = false;
        }
        if (equipment.Id == "coolant_jug" && equipment.Config.TryGetProperty("openForFill", out var open)
            && open.ValueKind == JsonValueKind.True)
            OpenJugForFilling(model);
        model.Scale = new Vector3(
            (float)(size[0] / asset.Bounds.WidthM),
            (float)(size[1] / asset.Bounds.HeightM),
            (float)(size[2] / asset.Bounds.DepthM)
        );
        if (equipment.Id == "finishing_tote" && equipment.Config.TryGetProperty("preserveCircularFillPort", out var circular)
            && circular.ValueKind == JsonValueKind.True)
            PreserveFinishingCircularPorts(model);
        return model;
    }

    private static Node3D CreatePrimitiveLoad(SceneEquipment equipment, double[] size)
    {
        var root = new Node3D();
        var material = Material(ColorFromInteger(equipment.Config, "color", 0x5e6b73), 0.55f, 0.26f);
        AddBox(root, new Vector3((float)size[0], (float)size[1], (float)size[2]),
            new Vector3(0, (float)size[1] / 2.0f, 0), material);
        return root;
    }

    // The following are purpose-built process stations for the tote finishing
    // line. They are intentionally not routed through the generic CNC-machine
    // mapping: a filler, capper, labeler, and inspection camera have different
    // physical envelopes and must be recognizable without their scene labels.
    private static Node3D CreateToteFiller()
    {
        var root = new Node3D();
        var steel = Material(new Color("52636b"), 0.45f, 0.32f);
        var pipe = Material(new Color("b7c4c8"), 0.7f, 0.20f);
        var blue = Material(new Color("177fb8"), 0.28f, 0.28f);
        AddPortal(root, steel, 2.55f);
        AddCylinder(root, "FillManifold", new Vector3(0, 2.32f, 0), 0.10f, 1.75f, pipe, new Vector3(0, 0, 90));
        AddCylinder(root, "KIN_fill_nozzle", new Vector3(0, 1.72f, 0), 0.07f, 0.72f, pipe);
        AddCylinder(root, "FillValveActuator", new Vector3(0, 2.32f, 0), 0.18f, 0.22f, blue, new Vector3(0, 0, 90));
        root.AddChild(new EquipmentMotionController { Name = "FillNozzleController", Kind = EquipmentMotionController.MotionKind.LinearY, TargetPrefix = "KIN_fill_nozzle", TravelM = -0.12f, TravelTimeSeconds = 0.35f });
        return root;
    }

    private static Node3D CreateToteCapper()
    {
        var root = new Node3D();
        var steel = Material(new Color("485860"), 0.42f, 0.30f);
        var blue = Material(new Color("177fb8"), 0.30f, 0.24f);
        var yellow = Material(new Color("d9a900"), 0.15f, 0.32f);
        AddPortal(root, steel, 2.55f);
        AddBox(root, new Vector3(0.72f, 0.30f, 0.72f), new Vector3(0, 2.14f, 0), blue);
        AddCylinder(root, "KIN_capper_spindle", new Vector3(0, 1.74f, 0), 0.10f, 0.62f, steel);
        AddCylinder(root, "CapChuck", new Vector3(0, 1.39f, 0), 0.20f, 0.12f, yellow);
        root.AddChild(new EquipmentMotionController { Name = "CapperController", Kind = EquipmentMotionController.MotionKind.ContinuousRotation, TargetPrefix = "KIN_capper_spindle", SpeedRpm = 180.0f });
        return root;
    }

    private static Node3D CreateToteLabeler()
    {
        var root = new Node3D();
        var steel = Material(new Color("4b5c64"), 0.42f, 0.32f);
        var blue = Material(new Color("177fb8"), 0.28f, 0.25f);
        var white = Material(new Color("e5e8e6"), 0.0f, 0.70f);
        AddBox(root, new Vector3(0.42f, 1.52f, 0.52f), new Vector3(0, 1.15f, -1.02f), blue);
        AddBox(root, new Vector3(0.85f, 0.09f, 0.16f), new Vector3(0, 1.58f, -0.55f), steel);
        AddCylinder(root, "KIN_label_roll", new Vector3(0, 1.46f, -0.82f), 0.20f, 0.34f, white, new Vector3(90, 0, 0));
        AddBox(root, new Vector3(0.20f, 0.55f, 0.06f), new Vector3(0, 1.35f, -0.28f), white, false);
        root.AddChild(new EquipmentMotionController { Name = "LabelRollController", Kind = EquipmentMotionController.MotionKind.ContinuousRotation, TargetPrefix = "KIN_label_roll", SpeedRpm = 55.0f });
        return root;
    }

    private static Node3D CreateToteVisionStation()
    {
        var root = new Node3D();
        var steel = Material(new Color("4b5c64"), 0.42f, 0.30f);
        var black = Material(new Color("161d21"), 0.18f, 0.22f);
        var cyan = Material(new Color("38d8ed"), 0.1f, 0.20f, false, 1.0f, new Color("38d8ed"), 1.8f);
        AddPortal(root, steel, 2.45f);
        AddBox(root, new Vector3(0.46f, 0.28f, 0.34f), new Vector3(0, 2.04f, 0), black);
        AddCylinder(root, "KIN_vision_lens", new Vector3(0, 1.86f, 0), 0.12f, 0.08f, cyan);
        AddBox(root, new Vector3(1.20f, 0.07f, 0.10f), new Vector3(0, 1.62f, 0), cyan, false);
        root.AddChild(new EquipmentMotionController { Name = "VisionController", Kind = EquipmentMotionController.MotionKind.OscillatingRotation, TargetPrefix = "KIN_vision_lens", TravelDegrees = 8.0f });
        return root;
    }

    private static void AddPortal(Node3D root, Material material, float height)
    {
        AddBox(root, new Vector3(0.12f, height, 0.12f), new Vector3(0, height / 2.0f, -0.92f), material);
        AddBox(root, new Vector3(0.12f, height, 0.12f), new Vector3(0, height / 2.0f, 0.92f), material);
        AddBox(root, new Vector3(0.18f, 0.14f, 1.96f), new Vector3(0, height, 0), material);
    }

    private static void AddCylinder(Node3D parent, string name, Vector3 position, float radius, float height, Material material, Vector3? rotationDegrees = null)
    {
        var instance = new MeshInstance3D
        {
            Name = name,
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, Material = material },
            Position = position,
            RotationDegrees = rotationDegrees ?? Vector3.Zero,
        };
        parent.AddChild(instance);
    }

    private static MeshInstance3D AddBox(Node3D parent, Vector3 size, Vector3 position, Material material, bool shadows = true)
    {
        var instance = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size, Material = material },
            Position = position,
            CastShadow = shadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off,
        };
        parent.AddChild(instance);
        return instance;
    }

    private static StandardMaterial3D Material(Color color, float metallic, float roughness,
        bool transparent = false, float alpha = 1.0f, Color? emission = null, float emissionEnergy = 0.0f)
    {
        color.A = alpha;
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Metallic = metallic,
            Roughness = roughness,
            Transparency = transparent ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
            EmissionEnabled = emission is not null,
            Emission = emission ?? Colors.Black,
            EmissionEnergyMultiplier = emissionEnergy,
        };
    }

    private static Vector3 Vector(double[] value) => new((float)value[0], (float)value[1], (float)value[2]);
    private static string SafeNodeName(string value) => value.Replace("-", "_", StringComparison.Ordinal);

    private static double Number(JsonElement config, string name, double fallback) =>
        config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value) && value.TryGetDouble(out var number)
            ? number : fallback;

    private static double[] NumberArray(JsonElement config, string name, double[] fallback) =>
        config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.GetDouble()).ToArray() : fallback;

    private static string Text(JsonElement config, string name, string fallback) =>
        config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback : fallback;

    private static string[] StringArray(JsonElement config, string name, string[] fallback) =>
        config.ValueKind == JsonValueKind.Object && config.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray() : fallback;

    private static Color ColorFromInteger(JsonElement config, string name, int fallback)
    {
        var value = (int)Number(config, name, fallback);
        return Color.FromHtml(value.ToString("x6", CultureInfo.InvariantCulture));
    }

    private static Color NamedSignalColor(string name) => name.ToLowerInvariant() switch
    {
        "red" => new Color("e03c31"),
        "amber" => new Color("f2a900"),
        "blue" => new Color("2e8bd1"),
        "white" => new Color("e9f2f5"),
        _ => new Color("21a366"),
    };

}
