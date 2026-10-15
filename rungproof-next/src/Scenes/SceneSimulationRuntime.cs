using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.App;
using RungProof.Next.VirtualController;

namespace RungProof.Next.Scenes;

/// <summary>
/// Executes the simulation contract embedded in a migrated scene. This layer
/// intentionally deals only in symbolic point names. PLC transport and address
/// mapping belong in a separate adapter and must never be invented here.
/// </summary>
public partial class SceneSimulationRuntime : Node
{
    private readonly JsonElement _definition;
    private readonly Node3D _sceneRoot;
    private readonly Dictionary<string, object?> _initialPoints = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _points = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _previousPoints = new(StringComparer.Ordinal);
    private readonly HashSet<string> _outputPoints = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _pointOwners = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _pointTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vector3> _initialEquipmentPositions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vector3> _initialEquipmentRotations = new(StringComparer.Ordinal);
    private readonly HashSet<string> _sequenceRotatedEquipment = new(StringComparer.Ordinal);
    private readonly Dictionary<Node3D, (Vector3 Scale, Vector3 Position, float Height)> _liquidAuthored = [];
    private JsonElement _activeSteps;
    private int _activeStepIndex = -1;
    private double _stepElapsed;
    private bool _commandSequenceStarted;
    private readonly Dictionary<string, float> _positionMotionStarts = new(StringComparer.Ordinal);
    // Tank scenes are continuous process simulations, not timed sequences. Keep
    // their running state independently so a stopped tank cannot keep changing
    // level merely because a pump command remains authored in the scene.
    private bool _tankSimulationRunning;
    private bool _controllerPlaybackRunning;
    private bool _controllerClockInitialized;
    // Only the standalone combinational reference preview uses this latch.
    // Its rules otherwise reassert outputs immediately after StopControllers.
    private bool _booleanPreviewStopped;
    private readonly ConveyorPlantModel? _conveyorPlant;
    // A released carton is still a visible load on the receiving surface.
    // Keep its visual pose separately from the canonical infeed leading edge.
    // This scene renders one recycled carton, not a receiver accumulation queue.
    private Vector3? _releasedCartonPosition;
    private readonly HashSet<string> _pulsePoints = new(StringComparer.Ordinal);

    public event Action? StateChanged;

    public string RuntimeType => Text(_definition, "type", "none");
    public IReadOnlyDictionary<string, object?> Points => _points;
    /// <summary>Main-thread native observation; presence cannot be inferred from the zero discharge projection.</summary>
    public RungProof.Next.Challenges.ChallengePlantState CaptureConveyorChallengeState()
    {
        if (_conveyorPlant is null)
            throw new InvalidOperationException("Conveyor challenge state is unavailable for this non-conveyor scene.");
        return new(
            new Dictionary<string, bool> { ["simulated_photoeye"] = AsBool(_points.GetValueOrDefault("simulated_photoeye")) },
            new Dictionary<string, double> { ["object_position"] = Convert.ToDouble(_points.GetValueOrDefault("object_position") ?? 0, CultureInfo.InvariantCulture) },
            new Dictionary<string, string> { ["component_state"] = Convert.ToString(_points.GetValueOrDefault("component_state"), CultureInfo.InvariantCulture) ?? string.Empty },
            _conveyorPlant.ObjectPresent, _conveyorPlant.Completed);
    }
    public bool IsRunning => _bottleShuttleReferenceActive || _shippingPalletReferenceActive || _tankSimulationRunning || _activeStepIndex >= 0 || Controllers().Any(IsControllerRunning);
    private bool _usesExternalClock;
    public bool UsesExternalClock
    {
        get => _usesExternalClock;
        set
        {
            if (_usesExternalClock == value) return;
            _usesExternalClock = value;
            // Source changes must update already-composed equipment callbacks,
            // even when the controller's running/stopped state is unchanged.
            RefreshEquipmentClock();
        }
    }
    private bool _externalPlaybackSelected;
    private bool _externalPlaybackRunning;

    public void SetExternalPlayback(bool selected, bool running)
    {
        if(selected && running) _motorZonesPaused=false;
        if (_externalPlaybackSelected == selected && _externalPlaybackRunning == running) return;
        _externalPlaybackSelected = selected;
        _externalPlaybackRunning = running;
        // Equipment animations have their own physics callbacks. Freeze those
        // as well as the plant model, without rewriting the PLC command image.
        RefreshEquipmentClock();
        if (selected && !running) PauseBottleShuttleClock();
        if (selected && !running) PauseChainLiftClock();
        if (selected && !running) PauseCookieClock();
        if (selected && !running) PauseBarrelClock();
        if (selected && !running) PauseCableClock();
        if (selected && !running) PausePalletizerClock();
        if (selected && !running) PauseBagIndexClock();
        if (selected && !running) PauseCoatingClock();
        if (selected && !running) PauseHandDryer();
        if (selected && !running) PauseGuardedTransfer();
        if (selected && !running) PauseToteTransfer();
        if (selected && !running) PauseLuggagePlant();
        if(selected && !running && HasEvPlant) PauseEvPlant();
        if (selected && !running) PauseWastewaterPlant();
        if (selected && !running) StopWorkspaceRollerPreset();
        if (selected && running) ApplyBindings();
    }

    public void ResetExternalPlant()
    {
        var controllerImage = _points.Where(item => IsPlcOwnedPoint(item.Key))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        ResetSimulation();
        foreach (var (name, value) in controllerImage) _points[name] = value;
        ApplyBindings();
        StateChanged?.Invoke();
    }

    public void SetControllerPlaybackRunning(bool running)
    {
        if(running) _motorZonesPaused=false;
        if (_controllerClockInitialized && _controllerPlaybackRunning == running) return;
        _controllerClockInitialized = true;
        _controllerPlaybackRunning = running;
        RefreshEquipmentClock();
        if (!running) PauseBottleShuttleClock();
        if (!running) PauseChainLiftClock();
        if (!running) PauseCookieClock();
        if (!running) PauseBarrelClock();
        if (!running) PauseCableClock();
        if (!running) PausePalletizerClock();
        if (!running) PauseBagIndexClock();
        if (!running) PauseCoatingClock();
        if (!running) PauseHandDryer();
        if (!running) PauseGuardedTransfer();
        if (!running) PauseToteTransfer();
        if (!running) PauseLuggagePlant();
        if(!running && HasEvPlant) PauseEvPlant();
        if (!running) PauseWastewaterPlant();
        if (!running) StopWorkspaceRollerPreset();
        if (RuntimeType == "tank") ProjectTankState();
    }

    private bool PlantPlaybackRunning => !UsesExternalClock ? _tankSimulationRunning
        : _externalPlaybackSelected ? _externalPlaybackRunning : _controllerPlaybackRunning;

    private void RefreshEquipmentClock()
    {
        foreach (var controller in Controllers())
            controller.SetPhysicsProcess(!UsesExternalClock || (_externalPlaybackSelected ? _externalPlaybackRunning : _controllerPlaybackRunning));
        // Bottle travel and belt animation share the same prescribed clock.
        // The general conveyor callback must not integrate a second motion.
        _bottleShuttleConveyor?.SetPhysicsProcess(false);
        FreezeDualSpindleAdapters();
        FreezeChainLiftAdapters();
        FreezeCookieAdapters();
        FreezeBarrelAdapters();
        FreezeRepeatCycleAdapters();
        FreezeBagIndexAdapter();
        FreezeCoatingAdapters();
        FreezeHandDryerAdapter();
        _guardedDrive?.SetPhysicsProcess(false);
        FreezeToteTransferAdapter();
        FreezeLuggageAdapters();
        FreezeVisionSorterAdapters();
        FreezeMotorConveyorAdapters();
        FreezeWorkspaceBeltAdapter();
        FreezeWorkspaceRollerAdapter();
    }

    public void ConsumeExternalInputPulses(IReadOnlyDictionary<string, object?> sampledPoints)
    {
        // Clear only pulses captured by this accepted exchange. Paused playback
        // must still send momentary PC feedback once; physics timing cannot
        // clear it before the slower PLC exchange samples it.
        var consumed = _pulsePoints.Where(name => sampledPoints.TryGetValue(name, out var value) && value is true).ToArray();
        foreach (var name in consumed)
        {
            SetPoint(name, false);
            _pulsePoints.Remove(name);
        }
        if (consumed.Length > 0) { EvaluateRules(); StateChanged?.Invoke(); }
    }

    public void DiscardPendingLocalInputPulses()
    {
        // A momentary scene action accepted before local Stop but not sampled
        // must not become a delayed command on Run. Held inputs are separate;
        // external PLC pulses remain owned by the accepted exchange path.
        if (_externalPlaybackSelected || _pulsePoints.Count == 0) return;
        foreach (var name in _pulsePoints) SetPoint(name, false);
        _pulsePoints.Clear();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    public bool TryGetAction(string id, out JsonElement action)
    {
        action = Actions().FirstOrDefault(item => Text(item, "id", string.Empty) == id);
        return action.ValueKind == JsonValueKind.Object;
    }

    public bool CanExecuteAction(JsonElement action) => RequirementsSatisfied(action) && EvActionAvailable(action)
        && (Text(action, "type", "") != "start" || !Controllers().OfType<PalletRobotMotion>().Any(robot => robot.ReferenceNeedsReset))
        && (Text(action, "type", "") != "palletizerLoad" || _palletizer?.CanLoadCarton == true)
        && (Text(action, "type", "") != "groupingLoad" || PackageGroupingPlant?.CanLoad == true)
        && (!HasLuggagePlant || (Text(action,"type","") != "luggageLoad" || (LuggageFinished && LuggageCommandsOff)))
        && (!HasLuggagePlant || Text(action,"point","") != "fixture_mass_kg" || (LuggageFixtureAvailable && LuggageCommandsOff))
        && ParkingEntryActionAvailable(Text(action, "type", ""))
        && MobileTrafficActionAvailable(Text(action, "type", ""));
    public bool IsPlcOwnedPoint(string point) =>
        string.Equals(_pointOwners.GetValueOrDefault(point), "PLC", StringComparison.OrdinalIgnoreCase);
    public bool IsPcOwnedPoint(string point) =>
        string.Equals(_pointOwners.GetValueOrDefault(point), "PC", StringComparison.OrdinalIgnoreCase);

    // A scene-local permissive belongs only to the offline simulator. External
    // PLC safety and reset logic remain in that PLC and are never synthesized here.
    public bool HasLatchedEmergencyStop => Actions().Any(action =>
        Text(action, "type", string.Empty) == "emergencyStop"
        && !AsBool(_points.GetValueOrDefault(Text(action, "point", "estop_ok"))));

    public SceneSimulationRuntime(JsonElement definition, Node3D sceneRoot)
    {
        _definition = definition.Clone();
        _sceneRoot = sceneRoot;
        Name = "SceneSimulationRuntime";
        LoadInitialPoints();
        InitializeAggregatePoints();
        if (RuntimeType is "conveyorStop" or "conveyorPusher")
            _conveyorPlant = new ConveyorPlantModel(Number(_definition, "lengthM", 1), Number(_definition, "speedMps", 0.5),
                Number(_definition, "objectLengthM", 0.2), Number(_definition, "photoeyePositionM", 0.5),
                Number(_definition, "minimumPhotoeyeOnS", 0.1), Number(_definition, "pusherStrokeTimeS", 0.3),
                Number(_definition, "transferPositionFraction", 0.8), RuntimeType == "conveyorPusher" ? Number(_definition, "repeatLoadSeconds", 2.5) : null);
        foreach (Node child in sceneRoot.GetChildren())
        {
            if (child is Node3D equipment)
            {
                _initialEquipmentPositions[equipment.Name.ToString()] = equipment.Position;
                _initialEquipmentRotations[equipment.Name.ToString()] = equipment.RotationDegrees;
            }
        }
    }

    public override void _Ready()
    {
        ResetSimulation();
        GD.Print($"SCENE_RUNTIME_READY type={RuntimeType} points={_points.Count} actions={Actions().Length}");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!UsesExternalClock&&!HasWorkspaceRollerPreset) AdvanceSimulation(delta);
    }

    public void AdvanceSimulation(double delta)
    {
        if (_externalPlaybackSelected && !_externalPlaybackRunning) return;
        // A stopped controller has not accepted a plant tick. Preserve its
        // pending scene inputs as well as geometry until an accepted scan.
        if (UsesExternalClock && !PlantPlaybackRunning) return;
        if (!_externalPlaybackSelected && _pulsePoints.Count > 0)
        {
            foreach (var point in _pulsePoints)
            {
                SetPoint(point, false);
            }
            _pulsePoints.Clear();
            EvaluateRules();
        }

        // An explicitly configured PLC enable starts this plant routine once
        // per Reset. Scene step output presets never replace that PLC image.
        var sequenceCommand = Text(_definition, "controllerSequenceCommand", "");
        if (UsesExternalClock && sequenceCommand.Length > 0 && !_commandSequenceStarted
            && AsBool(_points.GetValueOrDefault(sequenceCommand)))
        {
            if (_pointOwners.GetValueOrDefault(sequenceCommand) != "PLC"
                || _pointTypes.GetValueOrDefault(sequenceCommand) != "BOOL")
                throw new InvalidOperationException("Sequence command must be a declared PLC BOOL.");
            var sequence = Text(_definition, "defaultSequence", "");
            var action = Actions().FirstOrDefault(a => Text(a, "type", "") == "start"
                && Text(a, "sequence", "") == sequence);
            if (action.ValueKind == JsonValueKind.Object && CanExecuteAction(action)
                && StartSequence(sequence)) _commandSequenceStarted = true;
        }

        if (HasWorkspaceBeltPreset) { AdvanceWorkspaceBeltPreset(delta); return; }
        if (HasWorkspaceRollerPreset) { AdvanceWorkspaceRollerPreset(delta); return; }
        if (HasChickenLabelPlant) { AdvanceChickenLabelPlant(delta); return; }
        if (HasMotorConveyorZones) { AdvanceMotorConveyorZones(delta); return; }
        if (HasVisionSorter) { AdvanceVisionSorter(delta); return; }
        if (HasChainLiftPlant) { AdvanceChainLiftPlant(delta); return; }
        if (HasCookiePackagingPlant) { AdvanceCookiePackagingPlant(delta); return; }
        if (HasBarrelFillPlant) { AdvanceBarrelFillPlant(delta); return; }
        if (HasCableCutPlant) { AdvanceCableCutPlant(delta); return; }
        if (HasPalletizerPlant) { AdvancePalletizerPlant(delta); return; }
        if (HasRepeatCyclePlant) { AdvanceRepeatCyclePlant(delta); return; }
        if (HasDrawbridgePlant) { AdvanceDrawbridgePlant(delta); return; }
        if (HasBagIndexPlant) { AdvanceBagIndexPlant(delta); return; }
        if (HasCoatingPlant) { AdvanceCoatingPlant(delta); return; }
        if (HasHandDryer) { AdvanceHandDryer(delta); return; }
        if (HasGuardedTransfer) { AdvanceGuardedTransfer(delta); return; }
        if (HasServiceElevator) { AdvanceServiceElevator(delta); return; }
        if (HasWastewaterPlant) { AdvanceWastewaterPlant(delta); return; }
        if (HasMobileTrafficPlant) { AdvanceMobileTrafficPlant(delta); return; }
        if (HasDualSpindlePlant && UsesExternalClock) { AdvanceDualSpindlePlant(delta); return; }
        if (HasToteTransfer && UsesExternalClock) { AdvanceToteTransfer(delta); return; }
        if (HasLuggagePlant) { AdvanceLuggagePlant(delta); return; }
        if (HasEvPlant) { AdvanceEvPlant(delta); return; }
        if (HasParkingEntryPlant) { AdvanceParkingEntryPlant(delta); return; }
        if (HasPackageGroupingPlant) { AdvancePackageGroupingPlant(delta); return; }

        if (HasBottleShuttleReference)
        {
            AdvanceBottleShuttleReference(delta);
            return;
        }

        if (HasMultiConveyorRoute) { AdvanceMultiConveyorRoute(delta); return; }
        if (HasShippingPalletReference && UsesExternalClock)
        {
            AdvanceControllerShippingPallet(delta);
            return;
        }
        if (HasShippingPalletReference && !UsesExternalClock && !_externalPlaybackSelected)
        {
            AdvanceShippingPalletReference(delta);
            return;
        }

        if (RuntimeType == "tank" && PlantPlaybackRunning)
        {
            AdvanceTank(delta);
        }

        if (RuntimeType is "conveyorStop" or "conveyorPusher")
        {
            AdvanceConveyorStop(delta);
        }

        if (RuntimeType == "conveyor")
        {
            AdvanceConveyorCell(delta);
        }

        if (_activeStepIndex < 0 || _activeSteps.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var steps = _activeSteps.EnumerateArray().ToArray();
        if (_activeStepIndex >= steps.Length)
        {
            _activeStepIndex = -1;
            StopControllers();
            StateChanged?.Invoke();
            GD.Print("SCENE_SEQUENCE_COMPLETE");
            return;
        }

        var step = steps[_activeStepIndex];
        // Explicitly gated sequence routes hold their elapsed time and pose
        // until the PLC supplies the configured command.
        if (step.TryGetProperty("motions", out var gatedMotions))
        foreach (var motion in gatedMotions.EnumerateArray())
        {
            var command = Text(motion, "requiresCommand", "");
            if (command.Length == 0) continue;
            if (_pointOwners.GetValueOrDefault(command) != "PLC" || _pointTypes.GetValueOrDefault(command) != "BOOL")
                throw new InvalidOperationException($"Motion requires a declared PLC BOOL command '{command}'.");
            if (!AsBool(_points.GetValueOrDefault(command))) return;
        }
        var duration = Math.Max(Number(step, "durationS", 0.0), 0.0001);
        _stepElapsed += delta;
        var progress = (float)Math.Clamp(_stepElapsed / duration, 0.0, 1.0);
        if (!ApplyMotions(step, progress)) { StopSimulation(); return; }
        if (_stepElapsed + 1e-9 < duration)
        {
            return;
        }

        if (!ApplyMotions(step, 1.0f)) { StopSimulation(); return; }
        _activeStepIndex++;
        _stepElapsed = 0.0;
        if (_activeStepIndex < steps.Length)
        {
            EnterStep(steps[_activeStepIndex]);
        }
    }

    public bool VerifyDeclaredCases(JsonElement verification)
    {
        if (verification.ValueKind != JsonValueKind.Object
            || !verification.TryGetProperty("cases", out var cases)
            || cases.ValueKind != JsonValueKind.Array)
        {
            GD.Print("SCENE_CONTRACT_NO_CASES");
            return true;
        }

        var passed = true;
        foreach (var testCase in cases.EnumerateArray())
        {
            ResetSimulation();
            var caseName = Text(testCase, "name", "unnamed");
            if (testCase.TryGetProperty("actions", out var immediateActions)
                && immediateActions.ValueKind == JsonValueKind.Array)
            {
                foreach (var action in immediateActions.EnumerateArray())
                {
                    ExecuteAction(action.GetString() ?? string.Empty);
                    // Give momentary actions one simulator scan so rising-edge
                    // rules execute and the input can return to false.
                    AdvanceSimulation(1.0 / 120.0);
                }
            }
            if (testCase.TryGetProperty("phases", out var phases) && phases.ValueKind == JsonValueKind.Array)
            {
                foreach (var phase in phases.EnumerateArray())
                {
                    var action = Text(phase, "action", string.Empty);
                    if (action.Length > 0) ExecuteAction(action);
                    var remaining = Math.Max(Number(phase, "runForS", 0.0), 0.0);
                    if (remaining <= 1e-9) AdvanceSimulation(1.0 / 120.0);
                    while (remaining > 1e-9)
                    {
                        var step = Math.Min(remaining, 1.0 / 120.0);
                        AdvanceSimulation(step);
                        remaining -= step;
                    }
                }
            }

            var casePassed = true;
            if (testCase.TryGetProperty("expect", out var expected) && expected.ValueKind == JsonValueKind.Object)
            {
                foreach (var expectation in expected.EnumerateObject())
                {
                    if (!_points.TryGetValue(expectation.Name, out var actual)
                        || !Equivalent(actual, Value(expectation.Value)))
                    {
                        casePassed = false;
                        GD.PushError($"SCENE_CONTRACT_MISMATCH case={caseName} point={expectation.Name} expected={Value(expectation.Value)} actual={actual}");
                    }
                }
            }
            passed &= casePassed;
            GD.Print($"SCENE_CONTRACT_CASE {caseName} {(casePassed ? "PASS" : "FAIL")}");
        }
        GD.Print($"SCENE_CONTRACT_RESULT {(passed ? "PASS" : "FAIL")}");
        return passed;
    }

    public bool RunDefault()
    {
        _motorZonesPaused=false;
        // Run quantities without supplying PLC commands or starting adapters.
        if (HasWastewaterPlant)
        {
            _tankSimulationRunning = true;
            ApplyBindings();
            StateChanged?.Invoke();
            return true;
        }
        if (_definition.ValueKind == JsonValueKind.Object
            && _definition.TryGetProperty("defaultSequence", out var sequence)
            && sequence.ValueKind == JsonValueKind.String)
        {
            var sequenceName = sequence.GetString() ?? string.Empty;
            // Run and the named Start action must enter the same contract.
            // Starting the sequence directly bypassed its declared permissives.
            var startAction = Actions().FirstOrDefault(action => Text(action, "type", string.Empty) == "start"
                && Text(action, "sequence", string.Empty) == sequenceName);
            return startAction.ValueKind == JsonValueKind.Object
                ? ExecuteAction(Text(startAction, "id", string.Empty))
                : StartSequence(sequenceName);
        }

        if (RuntimeType == "booleanPanel")
        {
            _booleanPreviewStopped = false;
            EvaluateRules();
            GD.Print("SCENE_RUNTIME_BOOLEAN_SCAN");
            return true;
        }

        foreach (var controller in Controllers())
        {
            SetControllerRunning(controller, true);
        }
        if (RuntimeType == "conveyor")
        {
            SetPoint("conveyor_run", true);
            ApplyBindings();
        }
        if (RuntimeType == "tank")
        {
            _tankSimulationRunning = true;
            ProjectTankState();
        }
        StateChanged?.Invoke();
        GD.Print("SCENE_RUNTIME_RUN_DEFAULT");
        return true;
    }

    public void StopSimulation()
    {
        _motorZonesPaused=true;
        DiscardPendingLocalInputPulses();
        _bottleShuttleReferenceActive = false;
        _bottleShuttleConveyor?.ApplyPlantTravel(0, 0);
        PauseBagIndexClock();
        PauseCoatingClock();
        PauseHandDryer();
        PauseGuardedTransfer();
        PauseToteTransfer();
        PauseLuggagePlant();
        if(HasEvPlant) PauseEvPlant();
        PauseWastewaterPlant();
        _shippingPalletReferenceActive = false;
        if (!UsesExternalClock && RuntimeType == "booleanPanel") _booleanPreviewStopped = true;
        _activeStepIndex = -1;
        _tankSimulationRunning = false;
        if (RuntimeType == "tank")
        {
            // These are symbolic simulator points only. Clearing them here does
            // not write a PLC and mirrors the source simulator's safe stop.
            SetPoint("inlet_pump_run", false);
            SetPoint("drain_valve_open", false);
            ProjectTankState();
        }
        StopControllers();
        StopWorkspaceBeltPreset();
        StopWorkspaceRollerPreset();
        if (_definition.ValueKind == JsonValueKind.Object
            && _definition.TryGetProperty("safeState", out var safeState)
            && safeState.ValueKind == JsonValueKind.Object)
        {
            ApplySet(safeState);
        }
        EvaluateRules();
        StateChanged?.Invoke();
        GD.Print("SCENE_RUNTIME_STOP");
    }

    public void ResetSimulation()
    {
        _booleanPreviewStopped = false;
        _commandSequenceStarted = false;
        _activeStepIndex = -1;
        _stepElapsed = 0.0;
        _tankSimulationRunning = false;
        _pulsePoints.Clear();
        _releasedCartonPosition = null;
        _conveyorPlant?.Reset();
        _points.Clear();
        foreach (var (name, value) in _initialPoints)
        {
            _points[name] = value;
            _previousPoints[name] = value;
        }
        foreach (var (name, position) in _initialEquipmentPositions)
        {
            if (_sceneRoot.GetNodeOrNull<Node3D>(name) is { } equipment)
            {
                equipment.Position = position;
                // Restore only roots rotated by the declared plant sequence;
                // leave unrelated workspace rotations/scales under editor ownership.
                if (_sequenceRotatedEquipment.Contains(name)) equipment.RotationDegrees = _initialEquipmentRotations[name];
            }
        }
        foreach (var (liquid, authored) in _liquidAuthored)
        {
            liquid.Scale = authored.Scale;
            liquid.Position = authored.Position;
        }
        foreach (var controller in Controllers())
        {
            switch (controller)
            {
                case ChainLiftDriveVisual lift:
                    lift.ResetMotion();
                    break;
                case ChainFeedMotion feed:
                    feed.ResetMotion();
                    break;
                case EquipmentMotionController motion:
                    motion.ResetMotion();
                    break;
                case PalletRobotMotion robot:
                    robot.ResetMotion();
                    break;
                case ConveyorController conveyor:
                    conveyor.RunCommand = false;
                    conveyor.ResetPlantTravel();
                    break;
                case RollerConveyorController rollers:
                    rollers.RunCommand = false;
                    break;
            }
        }
        ApplyInitialTankLevels();
        ResetShippingPalletReference();
        ResetMotorConveyorZones();
        ResetWorkspaceBeltPreset();
        ResetWorkspaceRollerPreset();
        ResetMultiConveyorRoute();
        ResetBottleShuttleReference();
        ResetChainLiftPlant();
        ResetCookiePackagingPlant();
        ResetBarrelFillPlant();
        ResetCableCutPlant();
        ResetPalletizerPlant();
        ResetRepeatCyclePlant();
        ResetDrawbridgePlant();
        ResetBagIndexPlant();
        ResetCoatingPlant();
        ResetHandDryer();
        ResetGuardedTransfer();
        ResetServiceElevator();
        ResetDualSpindlePlant();
        ResetToteTransfer();
        ResetLuggagePlant();
        ResetEvPlant();
        ResetWastewaterPlant();
        ResetMobileTrafficPlant();
        ResetParkingEntryPlant();
        ResetPackageGroupingPlant();
        ResetVisionSorter();
        ResetChickenLabelPlant();
        EvaluateRules();
        ApplyBindings();
        if (RuntimeType == "tank") ProjectTankState();
        ProjectConveyorPlant();
        StateChanged?.Invoke();
        GD.Print("SCENE_RUNTIME_RESET");
    }

    public bool ExecuteAction(string actionId)
    {
        var action = Actions().FirstOrDefault(item => Text(item, "id", string.Empty) == actionId);
        if (action.ValueKind != JsonValueKind.Object)
        {
            GD.PushWarning($"Unknown scene action '{actionId}'.");
            return false;
        }
        if (!RequirementsSatisfied(action) || ((HasLuggagePlant || HasEvPlant) && !CanExecuteAction(action)))
        {
            GD.Print($"SCENE_ACTION_BLOCKED {actionId} {Text(action, "blockedMessage", "requirements not met")}");
            return false;
        }

        var type = Text(action, "type", string.Empty);
        var point = Text(action, "point", string.Empty);
        var result = type switch
        {
            "chickenLoad" => LoadChickenProduct(),
            "palletizerLoad" => LoadPalletizerCarton(),
            "groupingLoad" => LoadGroupingCarton(),
            "parkingEnter" or "parkingExit" or "parkingClearDeparted" => ParkingEntryAction(type),
            "start" => StartSequence(Text(action, "sequence", string.Empty)),
            "run" => RunDefault(),
            "stop" => StopAction(),
            "emergencyStop" => EmergencyStopAction(action),
            "reset" => ResetAction(),
            "toggle" or "togglePoint" => TogglePoint(point),
            "cycle" => CyclePoint(point, action),
            "luggageLoad" => LoadNextLuggage(),
            "trafficRequestA" or "trafficRequestB" => MobileTrafficAction(type),
            "pulse" => PulsePoint(point),
            _ => false,
        };
        if (result)
        {
            if (RuntimeType == "tank")
            {
                ProjectTankState();
            }
            GD.Print($"SCENE_ACTION_EXECUTED {actionId}");
        }
        return result;
    }

    public IReadOnlyList<(string Id, string Label)> GetActions() => Actions()
        .Select(action => (Text(action, "id", string.Empty), Text(action, "label", Text(action, "id", "Action"))))
        .Where(action => action.Item1.Length > 0)
        .ToArray();

    /// <summary>
    /// Samples declared simulator-owned BOOL points for a virtual controller.
    /// This is symbolic data only and has no PLC endpoint or address path.
    /// </summary>
    public IReadOnlyDictionary<string, string> SampleVirtualControllerTextInputs() =>
        _points.Where(point => _pointOwners.GetValueOrDefault(point.Key) == "PC"
            && _pointTypes.GetValueOrDefault(point.Key) is "STRING" or "ENUM" && point.Value is string)
            .ToDictionary(point => point.Key, point => (string)point.Value!, StringComparer.Ordinal);

    public void CommitVirtualControllerTextOutputs(IReadOnlyDictionary<string, string> outputs)
    {
        foreach (var (name, value) in outputs)
        {
            var metadata = _definition.GetProperty("points").EnumerateArray().FirstOrDefault(point => Text(point, "name", "") == name);
            var members = metadata.ValueKind == JsonValueKind.Object && metadata.TryGetProperty("enumMembers", out var domain)
                ? domain.EnumerateArray().Select(member => member.GetString()!).ToArray() : null;
            var type = _pointTypes.GetValueOrDefault(name);
            var declaration = new PlcVariable(name, type == "ENUM" ? PlcVariableType.Enum : PlcVariableType.String,
                PlcVariableRole.Output, value, EnumMembers: members);
            if (_pointOwners.GetValueOrDefault(name) != "PLC" || type is not ("STRING" or "ENUM") || !PlcTextValues.IsValid(declaration, value))
                throw new InvalidOperationException($"Invalid PLC-owned text scene output '{name}'.");
        }
        foreach (var (name, value) in outputs) _points[name] = value;
        ApplyBindings();
        StateChanged?.Invoke();
    }

    public IReadOnlyDictionary<string, bool> SampleVirtualControllerInputs()
    {
        ProjectRobotParkFeedback();
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var (name, owner) in _pointOwners)
        {
            if (!owner.Equals("PC", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_pointTypes.GetValueOrDefault(name), "BOOL", StringComparison.OrdinalIgnoreCase))
                continue;
            result[name] = AsBool(_points.GetValueOrDefault(name));
        }
        return result;
    }

    /// <summary>
    /// Samples declared simulator-owned INT, DINT, and REAL points. Values are
    /// returned by symbolic name only; there is no physical PLC address path.
    /// </summary>
    public IReadOnlyDictionary<string, double> SampleVirtualControllerNumericInputs()
    {
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var (name, owner) in _pointOwners)
        {
            var type = _pointTypes.GetValueOrDefault(name);
            if (!owner.Equals("PC", StringComparison.OrdinalIgnoreCase)
                || type is null
                || !IsNumericPointType(type)
                || !TryAsDouble(_points.GetValueOrDefault(name), out var value))
                continue;
            result[name] = value;
        }
        return result;
    }

    /// <summary>
    /// Commits a virtual scan's BOOL outputs only to declared PLC-owned scene
    /// points. Unknown, non-BOOL, or simulator-owned targets fail closed.
    /// </summary>
    public void CommitVirtualControllerOutputs(IReadOnlyDictionary<string, bool> outputs)
    {
        foreach (var (name, value) in outputs)
        {
            if (!_pointOwners.TryGetValue(name, out var owner)
                || !owner.Equals("PLC", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(_pointTypes.GetValueOrDefault(name), "BOOL", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Virtual controller output '{name}' is not a declared PLC-owned BOOL scene point.");
            }
            _points[name] = value;
        }
        ApplyBindings();
        ProjectWorkspaceBeltCommittedCommand();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Commits a virtual scan's finite numeric outputs only to declared
    /// PLC-owned INT, DINT, or REAL scene points. Integer points are truncated
    /// and range-clamped according to their declared scene type.
    /// </summary>
    public void CommitVirtualControllerNumericOutputs(IReadOnlyDictionary<string, double> outputs)
    {
        foreach (var (name, value) in outputs)
        {
            var type = _pointTypes.GetValueOrDefault(name);
            if (!_pointOwners.TryGetValue(name, out var owner)
                || !owner.Equals("PLC", StringComparison.OrdinalIgnoreCase)
                || type is null
                || !IsNumericPointType(type)
                || !double.IsFinite(value))
            {
                throw new InvalidOperationException(
                    $"Virtual controller output '{name}' is not a declared PLC-owned finite numeric scene point.");
            }
            _points[name] = type.ToUpperInvariant() switch
            {
                // Box integer branches before the REAL branch can promote the
                // whole switch expression to double and erase the scene type.
                "INT" => (object)(long)Math.Clamp(Math.Truncate(value), short.MinValue, short.MaxValue),
                "DINT" => (object)(long)Math.Clamp(Math.Truncate(value), int.MinValue, int.MaxValue),
                _ => value,
            };
        }
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private bool StartSequence(string name)
    {
        if (HasBottleShuttleReference) return StartBottleShuttleReference(name);
        if (HasShippingPalletReference) return StartShippingPalletReference(name);
        if (_definition.TryGetProperty("requireHomeForReferenceStart", out var requireHome)
            && requireHome.ValueKind == JsonValueKind.True
            && ((_activeSteps.ValueKind == JsonValueKind.Array && _activeStepIndex >= 0 && _activeStepIndex < _activeSteps.GetArrayLength())
                || Controllers().OfType<EquipmentMotionController>().Any(motion => motion.PositionPercent > 0.001f)))
        {
            GD.Print("SCENE_REFERENCE_START_REJECT homeOrResetRequired=true");
            return false;
        }
        if (Controllers().OfType<PalletRobotMotion>().Any(robot => robot.ReferenceNeedsReset))
        {
            // Restarting authored pickup coordinates with a held or deposited
            // tote would teleport it. A stopped/completed robot reference
            // requires the operator's existing Reset action before Run.
            GD.Print("SCENE_ROBOT_START_REJECT resetRequired=true");
            return false;
        }
        if (name.Length == 0
            || !_definition.TryGetProperty("sequences", out var sequences)
            || sequences.ValueKind != JsonValueKind.Object
            || !sequences.TryGetProperty(name, out var steps)
            || steps.ValueKind != JsonValueKind.Array
            || steps.GetArrayLength() == 0)
        {
            return false;
        }
        _activeSteps = steps.Clone();
        _activeStepIndex = 0;
        _stepElapsed = 0.0;
        EnterStep(_activeSteps[0]);
        StateChanged?.Invoke();
        GD.Print($"SCENE_SEQUENCE_STARTED {name}");
        return true;
    }

    private void EnterStep(JsonElement step)
    {
        // Opt-in position sequences capture their starting pose once, rather
        // than restarting from an authored endpoint after Stop or reversal.
        _positionMotionStarts.Clear();
        if (step.TryGetProperty("motions", out var startingMotions))
        foreach (var motion in startingMotions.EnumerateArray())
        {
            if (Text(motion, "type", string.Empty) != "position"
                || !motion.TryGetProperty("fromCurrent", out var current) || current.ValueKind != JsonValueKind.True) continue;
            var id = SafeNodeName(Text(motion, "equipmentId", string.Empty));
            if (_sceneRoot.GetNodeOrNull<Node3D>(id) is { } equipment)
                _positionMotionStarts[id] = equipment.FindChildren("*", string.Empty, true, false)
                    .OfType<EquipmentMotionController>().Single().InputPositionNormalized;
        }
        if (step.TryGetProperty("set", out var values) && values.ValueKind == JsonValueKind.Object)
        {
            ApplySet(values);
        }
        EvaluateRules();
        ApplyBindings();
        GD.Print($"SCENE_SEQUENCE_STEP {Text(step, "name", _activeStepIndex.ToString(CultureInfo.InvariantCulture))}");
        // Opt-in level trajectories own their current feedback from the start
        // of the step; authored endpoint presets must not appear as live data.
        if (HasSequenceLevelFeedback) ApplyMotions(step, 0);
    }

    private bool ApplyMotions(JsonElement step, float progress)
    {
        if (!step.TryGetProperty("motions", out var motions) || motions.ValueKind != JsonValueKind.Array)
        {
            return true;
        }
        foreach (var motion in motions.EnumerateArray())
        {
            var kind = Text(motion, "type", string.Empty);
            var robotMotion = kind is "robotApproach" or "robotGrip" or "robotTransfer" or "robotRelease" or "robotWithdraw";
            var equipmentId = SafeNodeName(Text(motion, "equipmentId", string.Empty));
            if (_sceneRoot.GetNodeOrNull<Node3D>(equipmentId) is not { } equipment)
            {
                if (robotMotion) { GD.Print($"SCENE_ROBOT_MOTION_REJECT missingEquipment={equipmentId}"); return false; }
                continue;
            }
            var from = (float)Number(motion, "from", 0.0);
            if (_positionMotionStarts.TryGetValue(equipmentId, out var currentFrom)
                && Text(motion, "type", string.Empty) == "position") from = currentFrom;
            var to = (float)Number(motion, "to", 0.0);
            var value = Mathf.Lerp(from, to, progress);
            switch (Text(motion, "type", string.Empty))
            {
                case "robotApproach":
                case "robotGrip":
                case "robotTransfer":
                case "robotRelease":
                case "robotWithdraw":
                    var robot = equipment.FindChild("PalletRobotMotion", true, false) as PalletRobotMotion;
                    var loadId = Text(motion, "loadId", string.Empty);
                    var load = loadId.Length > 0 ? _sceneRoot.GetNodeOrNull<Node3D>(SafeNodeName(loadId)) : null;
                    bool Position(string property, bool required, out Vector3 position)
                    {
                        position = Vector3.Zero;
                        if (!motion.TryGetProperty(property, out var value)) return !required;
                        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 3) return false;
                        for (var axis = 0; axis < 3; axis++)
                        {
                            if (value[axis].ValueKind != JsonValueKind.Number || !value[axis].TryGetSingle(out var coordinate)
                                || !float.IsFinite(coordinate)) return false;
                            position[axis] = coordinate;
                        }
                        return true;
                    }
                    var needsRoute = kind is "robotTransfer" or "robotWithdraw";
                    // Only an approach without a load is an intentional park.
                    // A misspelled load or malformed vector must stop before
                    // the timed sequence can increment its placement count.
                    if (robot is null || (loadId.Length > 0 && load is null) || (kind != "robotApproach" && load is null)
                        || !Position("fromPosition", needsRoute, out var routeFrom)
                        || !Position("toPosition", needsRoute, out var routeTo)
                        || !robot.ApplyReference(Text(step, "name", string.Empty), kind, load, routeFrom, routeTo, progress))
                    {
                        GD.Print($"SCENE_ROBOT_MOTION_REJECT step={Text(step, "name", string.Empty)} load={loadId}");
                        return false;
                    }
                    break;
                case "translate":
                    var position = equipment.Position;
                    position[AxisIndex(Text(motion, "axis", "x"))] = value;
                    equipment.Position = position;
                    break;
                case "position":
                    foreach (var controller in equipment.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>())
                    {
                        controller.SetPositionNormalized(value);
                    }
                    ProjectReferencePositionFeedback(motion, value);
                    break;
                case "rotate":
                    _sequenceRotatedEquipment.Add(equipmentId);
                    var rotation = equipment.RotationDegrees;
                    rotation[AxisIndex(Text(motion, "axis", "y"))] = value;
                    equipment.RotationDegrees = rotation;
                    break;
                case "tankLevel":
                    ApplyTankLevel(equipment, value);
                    if (HasSequenceLevelFeedback) { ApplyBindings(); StateChanged?.Invoke(); }
                    break;
            }
        }
        return true;
    }

    private void ProjectRobotParkFeedback()
    {
        if (!_definition.TryGetProperty("robotParkFeedback", out var robotId)) return;
        if (_pointOwners.GetValueOrDefault("robot_at_park") != "PC" || _pointTypes.GetValueOrDefault("robot_at_park") != "BOOL")
            throw new InvalidOperationException("Robot park feedback requires PC-owned BOOL robot_at_park.");
        var equipment = _sceneRoot.GetNode<Node3D>(SafeNodeName(robotId.GetString() ?? ""));
        var robot = equipment.FindChild("PalletRobotMotion", true, false) as PalletRobotMotion
            ?? throw new InvalidOperationException("Robot park feedback requires the actual robot motion adapter.");
        _points["robot_at_park"] = robot.AtPark;
    }

    private void ApplyTankLevel(Node3D equipment, float normalized)
    {
        if (equipment.FindChild("KIN_liquid", true, false) is not Node3D liquid)
        {
            return;
        }
        if (!_liquidAuthored.TryGetValue(liquid, out var authored))
        {
            var height = liquid is MeshInstance3D mesh ? mesh.GetAabb().Size.Y * liquid.Scale.Y : 1.0f;
            authored = (liquid.Scale, liquid.Position, height);
            _liquidAuthored[liquid] = authored;
        }
        normalized = Mathf.Clamp(normalized, 0.001f, 1.0f);
        var scale = authored.Scale;
        scale.Y = authored.Scale.Y * normalized;
        liquid.Scale = scale;
        var position = authored.Position;
        var bottom = authored.Position.Y - authored.Height * 0.5f;
        position.Y = bottom + authored.Height * normalized * 0.5f;
        liquid.Position = position;
        if (HasSequenceLevelFeedback && equipment.Name.ToString() == SafeNodeName(Text(_definition, "tankId", "")))
        {
            // Sump contract: the plant supplies its measured level and two
            // threshold inputs; this projection never supplies pump_run.
            foreach (var (name, owner, type) in new[] {
                ("sump_level", "SIM", "REAL"), ("low_float_active", "PC", "BOOL"), ("high_float_active", "PC", "BOOL"),
            })
                if (_pointOwners.GetValueOrDefault(name) != owner || _pointTypes.GetValueOrDefault(name) != type)
                    throw new InvalidOperationException($"Sequence level feedback requires {owner} {type} {name}.");
            // Visual captive floats move independently of the PLC command.
            // Their limited stroke illustrates the existing threshold inputs;
            // it is not a physical buoyancy/contact simulation.
            var surfaceY = (equipment.GlobalTransform * new Vector3(0, bottom + authored.Height * normalized, 0)).Y;
            foreach (var id in new[] { "low_float", "high_float" })
            {
                if (_sceneRoot.GetNodeOrNull<Node3D>(id) is not { } sensor
                    || !sensor.HasMeta("sump_float_visual_travel_m")
                    || sensor.GetNodeOrNull<MeshInstance3D>("SUMP_FLOAT_body") is not { } body) continue;
                var travel = sensor.GetMeta("sump_float_visual_travel_m").AsSingle();
                var pose = body.Position;
                pose.Y = Mathf.Clamp(surfaceY - sensor.GlobalPosition.Y, -travel, travel);
                body.Position = pose;
            }
            SetPoint("sump_level", (double)normalized * 100);
            SetPoint("low_float_active", normalized <= Number(_definition, "lowThreshold", .2)+.000001);
            SetPoint("high_float_active", normalized >= Number(_definition, "highThreshold", .78)-.000001);
        }
    }

    private bool HasSequenceLevelFeedback => _definition.TryGetProperty("sequenceLevelFeedback", out var enabled)
        && enabled.ValueKind == JsonValueKind.True;

    /// <summary>
    /// Mirrors the source tank simulation using only scene-owned symbolic
    /// points. PLC transport remains outside this class and no address is
    /// accepted or emitted here.
    /// </summary>
    private void AdvanceTank(double delta)
    {
        var level = TankLevelFraction();
        var inletRate = Number(_definition, "inletRatePerSecond", 0.055);
        var outletRate = Number(_definition, "outletRatePerSecond", 0.035);
        var change = (AsBool(_points.GetValueOrDefault("inlet_pump_run")) ? inletRate : 0.0)
            - (AsBool(_points.GetValueOrDefault("drain_valve_open")) ? outletRate : 0.0);
        level = Math.Clamp(level + change * delta, 0.0, 1.0);
        SetPoint("tank_level", level * 100.0);
        ProjectTankState();
    }

    private void AdvanceConveyorStop(double delta)
    {
        _conveyorPlant!.Step(delta, AsBool(_points.GetValueOrDefault("conveyor_running")),
            AsBool(_points.GetValueOrDefault("pusher_extend")), RuntimeType == "conveyorPusher");
        ProjectConveyorPlant();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void ProjectConveyorPlant()
    {
        if (_conveyorPlant is null) return;
        SetExistingPoint("object_position", _conveyorPlant.LeadingEdge ?? 0);
        SetExistingPoint("simulated_photoeye", _conveyorPlant.PhotoeyeBlocked);
        SetExistingPoint("part_at_pusher", _conveyorPlant.PhotoeyeBlocked);
        SetExistingPoint("pusher_position", _conveyorPlant.PusherPosition * 100);
        SetExistingPoint("pusher_extended", _conveyorPlant.PusherExtended);
        SetExistingPoint("pusher_retracted", _conveyorPlant.PusherRetracted);
        SetExistingPoint("parts_completed", _conveyorPlant.Completed);
        SetExistingPoint("component_state", _conveyorPlant.State);
        var productId = SafeNodeName(Text(_definition, "productId", string.Empty));
        var photoeyeId = SafeNodeName(Text(_definition, "photoeyeId", string.Empty));
        var pusherId = SafeNodeName(Text(_definition, "pusherId", string.Empty));
        var stroke = 0.0f;
        if (_sceneRoot.GetNodeOrNull<Node3D>(pusherId) is { } pusher)
            foreach (var controller in Controllers(pusher).OfType<EquipmentMotionController>())
            {
                controller.SetPositionNormalized((float)_conveyorPlant.PusherPosition);
                stroke = controller.TravelM;
            }
        if (_sceneRoot.GetNodeOrNull<Node3D>(productId) is { } product)
        {
            if (_conveyorPlant.ObjectPresent) _releasedCartonPosition = null;
            else if (RuntimeType == "conveyorPusher" && _conveyorPlant.ObjectTransferred)
                _releasedCartonPosition = product.Position;
            product.Visible = _conveyorPlant.ObjectPresent || _releasedCartonPosition.HasValue;
            if (_initialEquipmentPositions.TryGetValue(productId, out var initial))
            {
                var target = initial;
                if (_sceneRoot.GetNodeOrNull<Node3D>(photoeyeId) is { } photoeye)
                {
                    var sensorPosition = Number(_definition, "photoeyePositionM", 0.5);
                    // A through-beam can detect the trailing portion of a
                    // carton while its centre is aligned with the pusher.
                    // Keep this optional visual station datum separate from
                    // the canonical plant's symbolic sensor position.
                    var stationX = Number(_definition, "stationCenterXM", photoeye.Position.X);
                    if (!double.IsFinite(stationX))
                        throw new InvalidOperationException("Conveyor visual stationCenterXM must be finite.");
                    var scale = sensorPosition > 1e-9 ? (stationX - initial.X) / sensorPosition : 1;
                    target.X += (float)((_conveyorPlant.LeadingEdge ?? 0) * scale);
                    foreach (var beam in photoeye.FindChildren("KIN_beam*", string.Empty, true, false).OfType<Node3D>())
                        beam.Visible = !_conveyorPlant.PhotoeyeBlocked;
                }
                // Only the package at the transfer station can follow the
                // pusher. Previously an off-station stroke also dragged the
                // infeed package sideways, although the plant correctly
                // withheld transfer feedback/counting for that package.
                if (RuntimeType == "conveyorPusher" && _conveyorPlant.PhotoeyeBlocked)
                    target.Z += (float)_conveyorPlant.PusherPosition * stroke;
                if (_releasedCartonPosition is { } released)
                {
                    // The plant releases the infeed at its configured threshold,
                    // before the last part of the cylinder stroke. Continue that
                    // visual stroke, then leave the carton on the receiver when
                    // the plate retracts. Recycle only on the actual plant reload.
                    released.Z = MathF.Max(released.Z, initial.Z + (float)_conveyorPlant.PusherPosition * stroke);
                    _releasedCartonPosition = released;
                    target = released;
                }
                product.Position = target;
            }
        }
    }

    /// <summary>
    /// Applies typed PLC-owned outputs returned by the existing guarded
    /// external PLC session. Physical addresses remain outside the scene
    /// runtime; this method only accepts the declared symbolic scene contract.
    /// </summary>
    public void CommitExternalPlcOutputs(IReadOnlyDictionary<string, object?> outputs)
    {
        var validated = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (name, value) in outputs)
        {
            if (!_pointOwners.TryGetValue(name, out var owner)
                || !owner.Equals("PLC", StringComparison.OrdinalIgnoreCase)
                || value is null)
            {
                throw new InvalidOperationException(
                    $"External PLC output '{name}' is not a declared non-null PLC-owned scene point.");
            }
            validated.Add(name, RungProof.Next.Connections.ExternalSceneContract.TypedValue(_pointTypes[name], value, name));
        }
        // Reject the entire image before mutating anything if one point fails.
        foreach (var (name, value) in validated) _points[name] = value;
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void AdvanceConveyorCell(double delta)
    {
        var conveyorRunning = AsBool(_points.GetValueOrDefault("conveyor_run"))
            && AsBool(_points.GetValueOrDefault("estop_ok"));
        var speed = Math.Max(0.0, Number(_definition, "speedMps", 0.5));
        var conveyorId = SafeNodeName(Text(_definition, "conveyorId", string.Empty));
        var conveyor = _sceneRoot.GetNodeOrNull<Node3D>(conveyorId);
        var length = Number(_definition, "lengthM", 8.0);
        var startX = (conveyor?.Position.X ?? 0.0f) - (float)length * 0.5f + 0.45f;
        var endX = (conveyor?.Position.X ?? 0.0f) + (float)length * 0.5f - 0.45f;
        var sensorId = SafeNodeName(Text(_definition, "photoeyeId", string.Empty));
        var sensor = _sceneRoot.GetNodeOrNull<Node3D>(sensorId);
        var sensorX = sensor?.Position.X ?? 1.25f;
        var blocked = false;
        var completed = Convert.ToInt32(_points.GetValueOrDefault("parts_completed") ?? 0, CultureInfo.InvariantCulture);

        if (_definition.TryGetProperty("productIds", out var productIds)
            && productIds.ValueKind == JsonValueKind.Array)
        {
            foreach (var productElement in productIds.EnumerateArray())
            {
                var productId = SafeNodeName(productElement.GetString() ?? string.Empty);
                if (_sceneRoot.GetNodeOrNull<Node3D>(productId) is not { } product)
                    continue;

                var position = product.Position;
                if (conveyorRunning)
                {
                    position.X += (float)(speed * Math.Max(0.0, delta));
                    if (position.X > endX)
                    {
                        position.X = startX;
                        completed++;
                    }
                    product.Position = position;
                }

                if (Math.Abs(position.X - sensorX) <= 0.48f)
                    blocked = true;
            }
        }

        SetExistingPoint("conveyor_speed", conveyorRunning ? speed : 0.0);
        SetExistingPoint("photoeye_blocked", blocked);
        SetExistingPoint("parts_completed", completed);
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private double TankLevelFraction()
    {
        if (!_points.TryGetValue("tank_level", out var value))
        {
            return Number(_definition, "initialLevel", 0.0);
        }
        try
        {
            return Math.Clamp(Convert.ToDouble(value, CultureInfo.InvariantCulture) / 100.0, 0.0, 1.0);
        }
        catch (FormatException)
        {
            return Number(_definition, "initialLevel", 0.0);
        }
    }

    private void ProjectTankState()
    {
        var level = TankLevelFraction();
        var lowActive = level <= Number(_definition, "lowThreshold", 0.2);
        var highActive = level >= Number(_definition, "highThreshold", 0.8);
        var pumpRunning = PlantPlaybackRunning && AsBool(_points.GetValueOrDefault("inlet_pump_run"));

        var tankId = SafeNodeName(Text(_definition, "tankId", string.Empty));
        var tank = _sceneRoot.GetNodeOrNull<Node3D>(tankId);
        if (tank is not null) ApplyTankLevel(tank, (float)level);
        ProjectRadarInspectionView(tank);

        var pumpId = SafeNodeName(Text(_definition, "pumpId", string.Empty));
        if (_sceneRoot.GetNodeOrNull<Node3D>(pumpId) is { } pump)
        {
            foreach (var controller in Controllers(pump)) SetControllerRunning(controller, pumpRunning);
        }

        SetExistingPoint("low_level_switch", lowActive);
        SetExistingPoint("high_level_switch", highActive);
        SetExistingPoint("level_transmitter", 4.0 + 16.0 * level);
        SetExistingPoint("radar_level", level * 100.0);
        SetExistingPoint("radar_signal", 4.0 + 16.0 * level);
        SetExistingPoint("radar_echo_ok", true);
        SetExistingPoint("radar_distance", RadarDistance(tank));

        var lowId = SafeNodeName(Text(_definition, "lowSensorId", string.Empty));
        if (_sceneRoot.GetNodeOrNull<Node3D>(lowId) is { } lowSensor) SetLevelSensor(lowSensor, lowActive);
        var highId = SafeNodeName(Text(_definition, "highSensorId", string.Empty));
        if (_sceneRoot.GetNodeOrNull<Node3D>(highId) is { } highSensor) SetLevelSensor(highSensor, highActive);

        var indicatorId = SafeNodeName(Text(_definition, "indicatorId", string.Empty));
        if (_sceneRoot.GetNodeOrNull<Node3D>(indicatorId) is { } indicator)
        {
            SetIndicator(indicator, PlantPlaybackRunning, !PlantPlaybackRunning ? "amber" : (lowActive || highActive ? "red" : "green"));
        }
    }

    private double RadarDistance(Node3D? tank)
    {
        var transmitterId = SafeNodeName(Text(_definition, "transmitterId", string.Empty));
        if (tank?.FindChild("KIN_liquid", true, false) is not Node3D liquid
            || _sceneRoot.GetNodeOrNull<Node3D>(transmitterId) is not Node3D transmitter)
        {
            return _points.TryGetValue("radar_distance", out var existing)
                ? Convert.ToDouble(existing, CultureInfo.InvariantCulture)
                : 0.08;
        }
        // Both datums include configured sizing and authored parent transforms.
        var liquidSurfaceY = liquid is MeshInstance3D mesh ? WorldVerticalRange(mesh).Top : liquid.GlobalPosition.Y;
        // Range starts at the delivered antenna lens, not the equipment origin
        // (which is an installation datum above the horn).
        var antennaY = transmitter.FindChild("ANTENNA_dielectric_lens", true, false) is MeshInstance3D lens
            ? WorldVerticalRange(lens).Bottom : transmitter.GlobalPosition.Y;
        if (transmitter.FindChild("KIN_radar_beam", true, false) is MeshInstance3D beam)
        {
            var parent = beam.GetParent<Node3D>();
            var localTop = parent.ToLocal(new Vector3(transmitter.GlobalPosition.X, antennaY, transmitter.GlobalPosition.Z));
            var localBottom = parent.ToLocal(new Vector3(transmitter.GlobalPosition.X, liquidSurfaceY, transmitter.GlobalPosition.Z));
            var bounds = beam.GetAabb();
            var scale = beam.Scale;
            scale.Y = MathF.Max(0.001f, localTop.Y - localBottom.Y) / bounds.Size.Y;
            beam.Scale = scale;
            var position = beam.Position;
            position.Y = localTop.Y - bounds.End.Y * scale.Y;
            beam.Position = position;
        }
        return Math.Max(0.08, antennaY - liquidSurfaceY);
    }

    private static (float Bottom, float Top) WorldVerticalRange(MeshInstance3D mesh)
    {
        var bounds = mesh.GetAabb();
        var bottom = float.PositiveInfinity;
        var top = float.NegativeInfinity;
        for (var corner = 0; corner < 8; corner++)
        {
            var local = bounds.Position + bounds.Size * new Vector3(
                (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1);
            var y = (mesh.GlobalTransform * local).Y;
            bottom = MathF.Min(bottom, y);
            top = MathF.Max(top, y);
        }
        return (bottom, top);
    }

    private void EvaluateRules()
    {
        if(HasLuggagePlant) ProjectLuggagePlant();
        if(HasEvPlant) ProjectEvGeometry();
        if (HasHandDryer) { ProjectDryerHands(); if (_dryerFan is not null) ProjectDryerCommands(PlantPlaybackRunning); }
        if (UsesExternalClock)
        {
            // The selected controller produces the output image. Scene rules
            // are the fallback fake PLC, and must not compete with that image.
            foreach (var (name, value) in _points) _previousPoints[name] = value;
            ApplyBindings();
            StateChanged?.Invoke();
            return;
        }
        if (RuntimeType == "booleanPanel")
        {
            // Match the original fake-PLC scan: combinational outputs return
            // to their declared initial state before matching rules execute.
            foreach (var point in _outputPoints) _points[point] = _initialPoints[point];
        }
        var previewStopped = RuntimeType == "booleanPanel" && _booleanPreviewStopped;
        if (!previewStopped && _definition.TryGetProperty("edgeRules", out var edgeRules) && edgeRules.ValueKind == JsonValueKind.Array)
        {
            foreach (var rule in edgeRules.EnumerateArray())
            {
                var point = Text(rule, "rising", string.Empty);
                if (point.Length > 0 && !AsBool(_previousPoints.GetValueOrDefault(point)) && AsBool(_points.GetValueOrDefault(point)))
                {
                    if (rule.TryGetProperty("set", out var values)) ApplySet(values);
                    if (rule.TryGetProperty("toggle", out var toggles) && toggles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var toggle in toggles.EnumerateArray()) TogglePoint(toggle.GetString() ?? string.Empty, evaluate: false);
                    }
                }
            }
        }
        if (!previewStopped && _definition.TryGetProperty("rules", out var rules) && rules.ValueKind == JsonValueKind.Array)
        {
            foreach (var rule in rules.EnumerateArray())
            {
                if (rule.TryGetProperty("when", out var when) && ConditionsMatch(when)
                    && rule.TryGetProperty("set", out var values))
                {
                    ApplySet(values);
                }
            }
        }
        foreach (var (name, value) in _points) _previousPoints[name] = value;
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void ApplyBindings()
    {
        ProjectAggregatePoints();
        // Count the effective command image from typed ARRAY and legacy BOOL
        // sources. This display does not represent measured shaft feedback.
        if (_definition.TryGetProperty("motorCommandDisplays", out var motorDisplays)
            && motorDisplays.ValueKind == JsonValueKind.True)
        {
            var group = AsBool(_points.GetValueOrDefault("motor_array_run"));
            var commanded = Enumerable.Range(0, 10).Count(i => group
                || AsBool(_points.GetValueOrDefault($"motor_{i}_run"))
                || AsBool(_points.GetValueOrDefault($"motor_commands[{i}]")));
            if (_sceneRoot.GetNodeOrNull<Node3D>("training_accessory_7")?.FindChild("StaticReadout", true, false) is Label3D groupDisplay)
                groupDisplay.Text = $"COMMANDS ON\n{commanded}/10";
            if (_sceneRoot.GetNodeOrNull<Node3D>("training_accessory_8")?.FindChild("StaticReadout", true, false) is Label3D sequenceDisplay)
                sequenceDisplay.Text = "SEQUENCE CMD\n" + (AsBool(_points.GetValueOrDefault("startup_sequence_active")) ? "ACTIVE" : "OFF");
        }
        // Refresh display feedback even when Stop prevents further motion ticks.
        // The selected controller retains ownership of conveyor_run.
        if (UsesExternalClock && HasShippingPalletReference)
            SetPoint("status_color", !PlantPlaybackRunning ? "red"
                : AsBool(_points.GetValueOrDefault("conveyor_run")) ? "green" : "amber");
        // The sump tower reports the current controller image, rather than
        // retaining a sequence-entry color after Stop clears the pump output.
        // This is display-only feedback; it never produces a PLC command.
        if (UsesExternalClock && HasSequenceLevelFeedback
            && _pointOwners.GetValueOrDefault("status_color") == "SIM"
            && _pointTypes.GetValueOrDefault("status_color") == "STRING")
            SetPoint("status_color", !PlantPlaybackRunning ? "red"
                : AsBool(_points.GetValueOrDefault("pump_run")) ? "green" : "amber");
        ProjectRobotParkFeedback();
        if (!_definition.TryGetProperty("pointBindings", out var bindings) || bindings.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        var indicatorStates = new Dictionary<Node3D, (bool Active, string Color)>();
        // Opt-in compatibility: a legacy group command and an individual
        // command may each request the same motor. Resolve once per image,
        // rather than letting JSON binding order overwrite an active command.
        var combineRunning = _definition.TryGetProperty("combineRunningCommands", out var combine)
            && combine.ValueKind == JsonValueKind.True;
        var runningStates = new Dictionary<Node3D, bool>();
        foreach (var binding in bindings.EnumerateArray())
        {
            var point = Text(binding, "point", string.Empty);
            var equipmentId = SafeNodeName(Text(binding, "equipmentId", string.Empty));
            if (!_points.TryGetValue(point, out var value) || _sceneRoot.GetNodeOrNull<Node3D>(equipmentId) is not { } equipment)
            {
                continue;
            }
            switch (Text(binding, "mode", string.Empty))
            {
                case "workspaceMomentary":
                    if(HasWorkspaceRollerPreset)ProjectWorkspaceRollerMomentary(equipment,AsBool(value));
                    else ProjectWorkspaceMomentary(equipment,AsBool(value));
                    break;
                case "running":
                    if (combineRunning)
                        runningStates[equipment] = runningStates.GetValueOrDefault(equipment) || AsBool(value);
                    else
                        foreach (var controller in Controllers(equipment)) SetControllerRunning(controller, AsBool(value));
                    break;
                case "speedPercent":
                    // Project the declared command image without manufacturing
                    // a run command or writing any controller/transport state.
                    var speedPercent = Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    foreach (var controller in Controllers(equipment).OfType<EquipmentMotionController>())
                        controller.SetSpeedPercent(speedPercent);
                    break;
                case "estopPermissive":
                    foreach (var controller in Controllers(equipment).OfType<ConveyorController>())
                        controller.EstopOk = AsBool(value);
                    foreach (var controller in Controllers(equipment).OfType<RollerConveyorController>())
                        controller.EstopOk = AsBool(value);
                    break;
                case "switch":
                    foreach (var controller in equipment.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>())
                        controller.SetPositionNormalized(AsBool(value) ? 1.0f : 0.0f);
                    break;
                case "selector":
                    if (equipment.FindChild("SelectorSwitchController", true, false) is SelectorSwitchController selector)
                        selector.SetPosition(Convert.ToSingle(value, CultureInfo.InvariantCulture));
                    break;
                case "textDisplay":
                    if (value is string displayed)
                    {
                        var textReadout = equipment.FindChild("StaticReadout", true, false) as Label3D;
                        if (textReadout is null)
                        {
                            foreach (var legend in equipment.FindChildren("*static_legend*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()) legend.Visible = false;
                            if (equipment.FindChild("LABEL_DISPLAY_static_preview", true, false) is MeshInstance3D staticPreview) staticPreview.Visible = false;
                            textReadout = new Label3D { Name = "StaticReadout", FontSize = 32, PixelSize = .0016f, OutlineSize = 0,
                                Position = new Vector3(0,1.49f,.17f), Modulate = Text(binding, "textColor", "white") == "black" ? Colors.Black : Colors.White };
                            equipment.AddChild(textReadout);
                        }
                        textReadout.Text = Text(binding, "label", "") + "\n" + (displayed.Length == 0 ? "NO DATA" : displayed);
                    }
                    break;
                case "numericDisplay":
                    if (equipment.GetNodeOrNull<Label3D>("NumericReadout") is { } readout
                        && IsNumericPointType(_pointTypes.GetValueOrDefault(point) ?? string.Empty))
                    {
                        var format = Text(binding, "format", "G");
                        if (format is not ("G" or "F0" or "F1" or "F2" or "F3"))
                            throw new InvalidOperationException($"Numeric display '{equipmentId}' requires G or F0 through F3 format.");
                        var validPoint=Text(binding,"validPoint",string.Empty);
                        readout.Text = Text(binding, "label", string.Empty) + "\n"
                            + (validPoint.Length>0 && !AsBool(_points.GetValueOrDefault(validPoint)) ? "—" : Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString(format, CultureInfo.InvariantCulture));
                    }
                    break;
                case "indicator":
                    var color = value is string text ? text : Text(binding, "activeColor", "green");
                    var active = value is string || AsBool(value);
                    if (!indicatorStates.ContainsKey(equipment)) indicatorStates[equipment] = (false, color);
                    if (active) indicatorStates[equipment] = (true, color);
                    break;
                case "indicatorChannel":
                    // Independent PLC-owned lamp channels must remain visible
                    // together. Do not collapse conflicting commands to the
                    // last active binding as an exclusive indicator does.
                    SetIndicator(equipment, AsBool(value), Text(binding, "activeColor", "green"), channelOnly: true);
                    break;
                case "photoeye":
                    foreach (var beam in equipment.FindChildren("KIN_beam*", string.Empty, true, false).OfType<Node3D>())
                        beam.Visible = !AsBool(value);
                    break;
                case "levelSensor":
                    SetLevelSensor(equipment, AsBool(value));
                    break;
                case "position":
                    var position = value is bool boolean ? (boolean ? 1.0f : 0.0f) : Convert.ToSingle(value, CultureInfo.InvariantCulture);
                    if (_externalPlaybackSelected && !_externalPlaybackRunning && IsPlcOwnedPoint(point)) break;
                    if (position > 1.0f) position /= 100.0f;
                    foreach (var controller in equipment.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>())
                        controller.SetPositionNormalized(position);
                    break;
            }
        }
        foreach (var (equipment, state) in indicatorStates) SetIndicator(equipment, state.Active, state.Color);
        foreach (var (equipment, running) in runningStates)
            foreach (var controller in Controllers(equipment)) SetControllerRunning(controller, running);
        ProjectToteFillVisibility();
        ProjectChainLiftCommands();
        ProjectWastewaterValveFeedback();
    }

    private void ApplyInitialTankLevels()
    {
        var applied = new HashSet<string>(StringComparer.Ordinal);
        if (_definition.ValueKind == JsonValueKind.Object
            && _definition.TryGetProperty("initialLevel", out var initialLevel)
            && initialLevel.TryGetSingle(out var level)
            && _definition.TryGetProperty("tankId", out var tankId)
            && tankId.ValueKind == JsonValueKind.String)
        {
            var id = SafeNodeName(tankId.GetString() ?? string.Empty);
            if (_sceneRoot.GetNodeOrNull<Node3D>(id) is { } tank)
            {
                ApplyTankLevel(tank, level);
                applied.Add(id);
            }
        }
        if (!_definition.TryGetProperty("sequences", out var sequences) || sequences.ValueKind != JsonValueKind.Object) return;
        foreach (var sequence in sequences.EnumerateObject())
        foreach (var step in sequence.Value.EnumerateArray())
        {
            if (!step.TryGetProperty("motions", out var motions) || motions.ValueKind != JsonValueKind.Array) continue;
            foreach (var motion in motions.EnumerateArray())
            {
                if (Text(motion, "type", string.Empty) != "tankLevel") continue;
                var id = SafeNodeName(Text(motion, "equipmentId", string.Empty));
                if (applied.Add(id) && _sceneRoot.GetNodeOrNull<Node3D>(id) is { } tank)
                    ApplyTankLevel(tank, (float)Number(motion, "from", 0.0));
            }
        }
    }

    private static void SetIndicator(Node3D equipment, bool active, string colorName, bool channelOnly = false)
    {
        static Color SignalColor(string name) => name.ToLowerInvariant() switch
        {
            "red" => new Color("e03c31"), "amber" => new Color("f2a900"),
            "blue" => new Color("2e8bd1"), "white" => new Color("e9f2f5"),
            "orange" => new Color("ff6400"),
            _ => new Color("21a366"),
        };
        var lenses = equipment.FindChildren("*", string.Empty, true, false).OfType<MeshInstance3D>()
            .Where(item => item.Name.ToString().Contains("LENS", StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var mesh in lenses)
        {
            var name = mesh.Name.ToString().ToLowerInvariant();
            var nativeColor = new[] { "red", "amber", "green", "blue", "white", "orange" }
                .FirstOrDefault(name.Contains) ?? colorName;
            if (channelOnly && !nativeColor.Equals(colorName, StringComparison.OrdinalIgnoreCase)) continue;
            var color = SignalColor(nativeColor);
            var lensActive = active && (lenses.Length == 1 || nativeColor.Equals(colorName, StringComparison.OrdinalIgnoreCase));
            mesh.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = lensActive ? color : color.Darkened(0.72f),
                Roughness = 0.18f,
                EmissionEnabled = lensActive,
                Emission = color,
                // Both exclusive and independent lamps must retain their hue.
                // High emission washed a stopped red tower lens out to white
                // in the native compatibility renderer.
                EmissionEnergyMultiplier = lensActive ? 0.65f : 0.0f,
            };
        }
    }

    private static void SetLevelSensor(Node3D equipment, bool active)
    {
        var activeColor = new Color("f2a900");
        foreach (var mesh in equipment.FindChildren("*", string.Empty, true, false).OfType<MeshInstance3D>()
                     .Where(item => item.Name.ToString().Contains("head", StringComparison.OrdinalIgnoreCase)
                         || item.Name.ToString().Contains("tine", StringComparison.OrdinalIgnoreCase)))
        {
            mesh.MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = active ? activeColor : new Color("536772"),
                Metallic = 0.12f,
                Roughness = 0.42f,
                EmissionEnabled = active,
                Emission = activeColor,
                EmissionEnergyMultiplier = active ? 2.2f : 0.0f,
            };
        }
    }

    private bool RequirementsSatisfied(JsonElement action) =>
        !action.TryGetProperty("requires", out var requires) || ConditionsMatch(requires);

    private bool ConditionsMatch(JsonElement conditions)
    {
        if (conditions.ValueKind != JsonValueKind.Object) return true;
        foreach (var condition in conditions.EnumerateObject())
        {
            if (!_points.TryGetValue(condition.Name, out var current) || !Equivalent(current, Value(condition.Value)))
                return false;
        }
        return true;
    }

    private void ApplySet(JsonElement values)
    {
        if (values.ValueKind != JsonValueKind.Object) return;
        foreach (var property in values.EnumerateObject())
        {
            if (HasSequenceLevelFeedback && property.Name is "sump_level" or "low_float_active" or "high_float_active") continue;
            if (UsesExternalClock && Text(_definition, "controllerSequenceCommand", "").Length > 0
                && IsPlcOwnedPoint(property.Name)) continue;
            SetPoint(property.Name, Value(property.Value));
        }
    }

    private void SetPoint(string name, object? value)
    {
        if (name.Length > 0) _points[name] = value;
    }

    private void SetExistingPoint(string name, object? value)
    {
        if (_points.ContainsKey(name)) _points[name] = value;
    }

    private bool TogglePoint(string point, bool evaluate = true)
    {
        if (point.Length == 0) return false;
        SetPoint(point, !AsBool(_points.GetValueOrDefault(point)));
        if (evaluate) EvaluateRules();
        return true;
    }

    private bool CyclePoint(string point, JsonElement action)
    {
        if (point.Length == 0 || !action.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array) return false;
        var available = values.EnumerateArray().Select(Value).ToArray();
        if (available.Length == 0) return false;
        var current = _points.GetValueOrDefault(point);
        var index = Array.FindIndex(available, item => Equivalent(item, current));
        SetPoint(point, available[(index + 1) % available.Length]);
        EvaluateRules();
        return true;
    }

    private bool PulsePoint(string point)
    {
        if (point.Length == 0) return false;
        SetPoint(point, true);
        EvaluateRules();
        _pulsePoints.Add(point);
        return true;
    }

    private bool StopAction() { StopSimulation(); return true; }
    private bool EmergencyStopAction(JsonElement action)
    {
        // This is a scene-local symbolic E-stop model, not safety-rated logic
        // and never a physical I/O command. The action latches its declared
        // permissive false, applies the scene safe state, and requires the
        // normal simulator Reset path before a Start action can run again.
        StopSimulation();
        var point = Text(action, "point", "estop_ok");
        SetExistingPoint(point, false);
        EvaluateRules();
        return true;
    }
    private bool ResetAction() { ResetSimulation(); return true; }

    private void LoadInitialPoints()
    {
        if (_definition.ValueKind != JsonValueKind.Object
            || !_definition.TryGetProperty("points", out var points)
            || points.ValueKind != JsonValueKind.Array) return;
        foreach (var point in points.EnumerateArray())
        {
            var name = Text(point, "name", string.Empty);
            if (name.Length > 0 && point.TryGetProperty("initial", out var initial))
            {
                _initialPoints[name] = Value(initial);
                _pointOwners[name] = Text(point, "owner", string.Empty).ToUpperInvariant();
                _pointTypes[name] = Text(point, "type", string.Empty).ToUpperInvariant();
                if (Text(point, "role", string.Empty) == "output") _outputPoints.Add(name);
            }
        }
    }

    private JsonElement[] Actions() => _definition.ValueKind == JsonValueKind.Object
        && _definition.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array
            ? actions.EnumerateArray().Select(item => item.Clone()).ToArray() : [];

    private IEnumerable<Node> Controllers(Node? root = null)
    {
        root ??= _sceneRoot;
        return root.FindChildren("*", string.Empty, true, false).Where(node =>
            node is EquipmentMotionController or ConveyorController or RollerConveyorController or PalletRobotMotion);
    }

    private static bool IsControllerRunning(Node controller) => controller switch
    {
        EquipmentMotionController motion => motion.Running,
        PalletRobotMotion robot => robot.RunCommand,
        ConveyorController conveyor => conveyor.RunCommand,
        RollerConveyorController rollers => rollers.RunCommand,
        _ => false,
    };

    private static void SetControllerRunning(Node controller, bool running)
    {
        switch (controller)
        {
            case EquipmentMotionController motion: if (running) motion.Run(); else motion.Stop(); break;
            case PalletRobotMotion robot: robot.RunCommand = running; break;
            case ConveyorController conveyor: conveyor.RunCommand = running; break;
            case RollerConveyorController rollers: rollers.RunCommand = running; break;
        }
    }

    private void StopControllers() { foreach (var controller in Controllers()) SetControllerRunning(controller, false); }
    private static int AxisIndex(string axis) => axis.ToLowerInvariant() switch { "y" => 1, "z" => 2, _ => 0 };
    private static string SafeNodeName(string value) => value.Replace("-", "_", StringComparison.Ordinal);
    private static string Text(JsonElement element, string name, string fallback) => element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
    private static double Number(JsonElement element, string name, double fallback) => element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value) && value.TryGetDouble(out var number) ? number : fallback;
    private static object? Value(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => true, JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => value.GetDouble(), JsonValueKind.String => value.GetString(),
        JsonValueKind.Null => null, _ => value.GetRawText(),
    };
    private static bool AsBool(object? value) => value switch
    {
        bool boolean => boolean, long integer => integer != 0, double number => Math.Abs(number) > 1e-9,
        string text when bool.TryParse(text, out var boolean) => boolean, _ => false,
    };
    private static bool IsNumericPointType(string type) =>
        type.Equals("INT", StringComparison.OrdinalIgnoreCase)
        || type.Equals("DINT", StringComparison.OrdinalIgnoreCase)
        || type.Equals("REAL", StringComparison.OrdinalIgnoreCase);
    private static bool TryAsDouble(object? value, out double result)
    {
        if (value is not (sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal))
        {
            result = 0.0;
            return false;
        }
        try
        {
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return double.IsFinite(result);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            result = 0.0;
            return false;
        }
    }
    private static bool Equivalent(object? left, object? right)
    {
        if (left is null || right is null) return left is null && right is null;
        if (left is IConvertible && right is IConvertible && left is not string && right is not string)
        {
            try { return Math.Abs(Convert.ToDouble(left, CultureInfo.InvariantCulture) - Convert.ToDouble(right, CultureInfo.InvariantCulture)) < 1e-9; }
            catch (FormatException) { }
        }
        return Equals(left, right);
    }
}

