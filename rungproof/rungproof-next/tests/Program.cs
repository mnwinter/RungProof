using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static int _passed;
    private static int _failed;

    private static string FindProjectRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RungProof.Next.csproj"))) return directory.FullName;
        throw new DirectoryNotFoundException("RungProof project root was not found above the test output.");
    }

    private static int Main(string[] args)
    {
        // Stack exhaustion terminates a process rather than throwing a catchable
        // exception. Keep the adversarial flat CALL chain in its own process.
        if (args.Contains("--deep-call-probe", StringComparer.Ordinal))
        {
            TestDeepCallChain();
            return 0;
        }
        RunEditorCommandTests();
        RegisterInstructionObservationTests();
        RegisterCompactObservationRegressionTests();
        RegisterObservationLifecycleTests();
        RegisterRecursiveEditorTests();
        RegisterEditorMigrationTests();
        RegisterParameterizedCallTests();
        RegisterParameterizedCallAcceptanceTests();
        RegisterParameterizedCallContractRegressionTests();
        RegisterEditorTransactionTests();
        RegisterCallEditorTests();
        RegisterInstructionPresentationTests();
        Test("flat imported CALL chains compile without process stack exhaustion", TestDeepCallChainProcess);
        Test("STRING and ENUM are declared controller types", TestTextTypeSupport);
        Test("scene text initial data is validated before construction", TestSceneTextBoundary);
        Test("malformed enum member JSON returns a diagnostic", TestEnumMalformedJson);
        Test("text MOVE preserves data and lifecycle through JSON and scene mapping", TestTextMoveAndLifecycle);
        Test("text samples reject unknown and oversized values atomically", TestTextInputValidation);
        Test("FORMAT_TEXT clears stale results on malformed templates", TestTextFormattingFailures);
        Test("ENUM validates members and rejects incompatible domains", TestEnumDomains);
        Test("drive matching executes editable STRING logic", TestDriveStringLesson);
        Test("chicken formatting executes editable STRING logic", TestChickenStringLesson);
        Test("motor transitions execute editable ENUM logic", TestMotorEnumLesson);
        Test("typed aggregate declarations execute and persist", TestAggregateExecution);
        Test("aggregate malformed declarations and operands fail closed", TestAggregateRejection);
        Test("NO contact follows false/true", TestNormallyOpen);
        Test("NC contact inverts false/true", TestNormallyClosed);
        Test("rising edge contact pulses for exactly one scan", TestRisingEdgeContact);
        Test("falling edge contact pulses for exactly one scan", TestFallingEdgeContact);
        Test("edge contacts suppress startup and restart pulses", TestEdgeLifecycle);
        Test("explicit operator pulse before first scan fires once without held startup edges", TestFirstScanOperatorPulse);
        Test("edge memory is isolated by stable instruction ID", TestEdgeIsolation);
        Test("edge contacts survive JSON and editor round-trip", TestEdgeRoundTrip);
        Test("series logic is AND", TestSeries);
        Test("parallel branches are OR", TestParallel);
        Test("coil assigns rung result", TestCoilAssignment);
        Test("seal-in persists across scans", TestSealIn);
        Test("Stop breaks seal-in", TestStopBreaksSeal);
        Test("photoeye stops conveyor", TestPhotoeyeStops);
        Test("restart is blocked while photoeye active", TestBlockedRestart);
        Test("repeated runs are deterministic", TestDeterminism);
        Test("missing variable is rejected", TestMissingVariable);
        Test("invalid type usage is rejected", TestInvalidType);
        Test("duplicate element ID is rejected", TestDuplicateId);
        Test("input coil target is rejected", TestInvalidOutputTarget);
        Test("malformed parallel branch is rejected", TestMalformedBranch);
        Test("unknown contact type is rejected", TestUnknownContactType);
        Test("missing program identity is rejected", TestMissingProgramIdentity);
        Test("Reset restores controller memory", TestReset);
        Test("fixed scan order is sample-execute-commit-plant-publish", TestScanOrder);
        Test("Stop cancels an unscanned momentary command", TestStopCancelsPulse);
        Test("typed numeric scene I/O follows deterministic scan order", TestNumericScanOrder);
        Test("editor document builds arbitrary rungs", TestEditorDocumentBuild);
        Test("editor inserts instructions at an exact series position", TestEditorExactInsertion);
        Test("editor parallel branches execute as OR", TestEditorParallelBranches);
        Test("editor document remains compiler validated", TestEditorValidation);
        Test("editor program JSON save/load round-trips", TestEditorRoundTrip);
        Test("editor project save/load preserves invalid work in progress", TestEditableProjectRoundTrip);
        Test("TIA block types and FB interfaces persist", TestBlockTypesAndInterfaces);
        Test("editor project loader rejects unreadable structure", TestEditableProjectMalformed);
        Test("editor project rejects null elements at every collection boundary", TestProjectNullEntries);
        Test("scan coordinator rejects nonfinite time without poisoning future scans", TestSessionInvalidTime);
        Test("scan coordinator bounds batches without dropping pending time", TestSessionBudget);
        Test("compiler rejects unsupported sub-millisecond scan periods", TestMinimumScanPeriod);
        Test("new project reset clears all prior engineering state", TestNewProjectReset);
        Test("typed tag initial values parse persist and initialize runtime", TestTagInitialValues);
        Test("watch table persists and follows tag lifecycle", TestWatchTablePersistence);
        Test("watch table validates symbols and participates in undo", TestWatchTableValidationAndUndo);
        Test("scene I/O bindings enforce direction type and uniqueness", TestSceneIoBindings);
        Test("TON remains timing before preset", TestTonBeforePreset);
        Test("TON completes exactly at preset", TestTonAtPreset);
        Test("TON resets when rung becomes false", TestTonResets);
        Test("TON done member drives a later coil", TestTonDoneContact);
        Test("Stop and Reset clear TON state", TestTonStopAndReset);
        Test("TOF holds done through the off delay", TestTofDelay);
        Test("TP pulses once per rising edge", TestTpPulse);
        Test("TONR RTO pauses retains resumes and survives Stop", TestRetentiveTimer);
        Test("timer reset clears retentive accumulated and done state", TestRetentiveTimerReset);
        Test("timer program JSON save/load round-trips", TestTimerRoundTrip);
        Test("invalid timer instance and preset are rejected", TestInvalidTimer);
        Test("invalid timer reset instance is rejected", TestInvalidTimerReset);
        Test("shared timer instruction instances are rejected", TestSharedTimerInstance);
        Test("set coil latches across false scans", TestSetCoil);
        Test("reset coil clears a latched value", TestResetCoil);
        Test("later reset coil wins in scan order", TestLatchScanOrder);
        Test("latched coil modes survive JSON and editor round-trip", TestLatchRoundTrip);
        Test("unknown coil mode is rejected", TestInvalidCoilMode);
        Test("CTU counts only false-to-true transitions", TestCounterRisingEdge);
        Test("counter load and CTD decrement on rising edges", TestCounterDown);
        Test("CTD done becomes true at zero", TestCounterDownDone);
        Test("counter edge memory is isolated per instruction", TestCounterEdgeIsolation);
        Test("CTD and counter load survive JSON and editor round-trip", TestCounterDownRoundTrip);
        Test("CTU done member drives a later coil", TestCounterDoneContact);
        Test("counter reset instruction clears accumulated and done", TestCounterReset);
        Test("Stop preserves counter while Reset clears it", TestCounterLifecycle);
        Test("counter program JSON and editor round-trip", TestCounterRoundTrip);
        Test("invalid counter instance and preset are rejected", TestInvalidCounter);
        Test("all numeric comparison operators execute", TestComparisonOperators);
        Test("numeric input samples participate in comparisons", TestNumericInputSampling);
        Test("counter ACC and CV are numeric operands", TestCounterNumericOperand);
        Test("timer ET and PT are numeric operands", TestTimerNumericOperand);
        Test("comparison JSON and editor round-trip", TestComparisonRoundTrip);
        Test("invalid comparison operand is rejected", TestInvalidComparisonOperand);
        Test("MOV writes only while rung is true", TestMoveInstruction);
        Test("ADD SUB MUL DIV and MOD execute deterministically", TestMathInstructions);
        Test("ABS NEG and SQRT execute deterministically", TestUnaryMathInstructions);
        Test("scientific math and TRUNC execute deterministically", TestScientificMathInstructions);
        Test("NORM_X and SCALE_X execute deterministic three-operand scaling", TestScalingInstructions);
        Test("numeric conversion instructions execute deterministic vendor-compatible semantics", TestConversionInstructions);
        Test("INT numeric destination rounds nearest-even", TestIntegerDestination);
        Test("DINT output rounds clamps publishes and resets safely", TestDIntOutputLifecycle);
        Test("dynamic divide by zero reports and preserves destination", TestDivideByZeroDiagnostic);
        Test("literal divide by zero is rejected before load", TestLiteralDivideByZero);
        Test("MOD zero and SQRT domain faults preserve destination", TestAdvancedMathDiagnostics);
        Test("invalid literal MOD and SQRT domains are rejected before load", TestInvalidAdvancedMathLiteral);
        Test("scientific math domain faults preserve destination", TestScientificMathDiagnostics);
        Test("invalid literal scientific domains are rejected before load", TestInvalidScientificMathLiteral);
        Test("scaling range faults preserve destination and reject invalid literals", TestScalingDiagnostics);
        Test("math accepts counter and timer numeric members", TestMathInstanceOperands);
        Test("numeric operation JSON and editor round-trip", TestNumericOperationRoundTrip);
        Test("called block executes inline before later main logic", TestBlockCallExecution);
        Test("false CALL rung skips target block", TestBlockCallSkipped);
        Test("missing block call target is rejected", TestMissingCallTarget);
        Test("recursive block call cycle is rejected", TestRecursiveCallRejected);
        Test("multi-block program JSON and editor round-trip", TestMultiBlockRoundTrip);
        Test("continuous task executes every base scan", TestContinuousTaskSchedule);
        Test("periodic task executes on configured base-scan multiple", TestPeriodicTaskSchedule);
        Test("task priority determines deterministic write order", TestTaskPriorityOrder);
        Test("invalid task schedule and target are rejected", TestInvalidTask);
        Test("task configuration JSON and editor round-trip", TestTaskRoundTrip);
        Test("block and routine lifecycle preserves stable references and undo", TestBlockLifecycle);
        Test("referenced block deletion is blocked and unused deletion succeeds", TestBlockDeletionProtection);
        Test("task and OB lifecycle supports rename delete and undo", TestTaskLifecycle);
        Test("editor undo restores exact rung state and stable IDs", TestEditorUndo);
        Test("editor redo reapplies changes and exposes descriptions", TestEditorRedo);
        Test("new editor mutation clears redo history", TestEditorRedoInvalidation);
        Test("selected contact edits preserve stable identity and undo", TestSelectedContactEdit);
        Test("selected contacts move to an exact series position", TestSelectedContactMove);
        Test("rung clipboard paste regenerates every stable identity and undoes", TestRungClipboardPaste);
        Test("instruction clipboard paste preserves operands at an exact slot", TestInstructionClipboardPaste);
        Test("parallel branch removal preserves one required logic path", TestParallelBranchRemoval);
        Test("tag rename updates every symbolic reference atomically", TestTagRename);
        Test("referenced tag deletion is blocked and unused deletion succeeds", TestTagDeletion);
        Test("simulator input force overrides sampled BOOL input", TestInputForce);
        Test("input force release restores unforced image without new samples", TestInputForceRelease);
        Test("forced input retains latest sampled held and pulsed operator image", TestInputForceRawUpdates);
        Test("simulator output force overrides logic until removed", TestOutputForce);
        Test("Stop deenergizes forced output and Reset clears forces", TestForceLifecycle);
        Test("force rejects memory and non-BOOL targets", TestInvalidForceTarget);
        Test("session publishes force state changes", TestForceSessionPublication);
        Test("cross-reference indexes tag declarations reads and writes", TestTagCrossReference);
        Test("cross-reference resolves timer and counter members to root tags", TestMemberCrossReference);
        Test("cross-reference indexes calls and scheduled entry blocks", TestBlockCrossReference);
        Test("cross-reference indexes JMP targets and LBL declarations", TestJumpLabelCrossReference);
        Test("project search is case-insensitive across rung labels and details", TestProjectSearch);
        Test("instruction help catalog has unique complete entries", TestInstructionHelpCatalog);
        Test("instruction help covers every comparison and numeric operation", TestInstructionHelpOperationCoverage);
        Test("instruction help presents distinct TIA and Logix terminology", TestInstructionHelpVendorTerms);
        Test("instruction help rejects unknown keys", TestUnknownInstructionHelp);
        Test("true RETURN exits called block and resumes caller", TestReturnExecution);
        Test("false RETURN continues called block", TestReturnSkipped);
        Test("RETURN in task entry ends only that due task", TestTaskEntryReturn);
        Test("RETURN JSON and editor round-trip", TestReturnRoundTrip);
        Test("true JMP skips to block-local LBL and false JMP falls through", TestJumpAndLabelExecution);
        Test("JMP LBL validation rejects missing and duplicate labels", TestJumpLabelValidation);
        Test("JMP LBL JSON editor round-trip preserves targets", TestJumpLabelRoundTrip);
        Test("backward JMP loop watchdog stops and drives outputs safe", TestJumpLoopWatchdog);
        Test("offline engineering workflow persists validates loads exchanges monitors and forces", TestOfflineEngineeringWorkflow);
        Test("authored palletizer enforces phase-specific permissives and explicit resume", TestPalletizerPermissives);
        Test("authored palletizer counts one layer per completion edge", TestPalletizerLayerCount);
        Test("authored demos 1 through 4 execute their documented behavior", TestOtherAuthoredDemos);
        Test("authored batch publishes count through numeric scene I/O and lifecycle", TestBatchCountNumericOutput);
        Test("scene exercise projects bind declared I/O across the catalog", TestSceneExerciseProjects);
        Test("bottle shuttle reference owns reversal and retains its step across playback Stop", TestBottleShuttleReference);
        Test("ladder dirty state ignores block browsing but retains project edits", TestBlockBrowsingDirtyState);
        Test("authored block demos include their promised executable instruction mix", TestAuthoredDemoStructure);

        Console.WriteLine($"VIRTUAL_CONTROLLER_TESTS_PASS {_passed}");
        Console.WriteLine($"VIRTUAL_CONTROLLER_TESTS_FAIL {_failed}");
        Console.WriteLine("REAL_PLC_TRANSPORT_CONSTRUCTED FALSE");
        Console.WriteLine("REAL_PLC_CONNECTION_ATTEMPTED FALSE");
        return _failed == 0 ? 0 : 1;
    }

    private static void TestBlockBrowsingDirtyState()
    {
        True(AuthoredDemoLadderPrograms.TryCreate("lab-11-13-xy-palletizing", out var document));
        var baseline = LadderEditorProjectJson.Save(document);
        for (var index = 0; index < document.Blocks.Count; index++)
        {
            document.SelectBlock(index);
            False(LadderEditorProjectJson.HasUnsavedChanges(document, baseline));
        }
        var opened = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(document));
        True(opened.IsReadable);
        Equal(document.ActiveBlockIndex, opened.Document!.ActiveBlockIndex);
        document.SelectBlock(0);
        document.Rungs[0].Label += " edited";
        True(LadderEditorProjectJson.HasUnsavedChanges(document, baseline));
        True(LadderEditorProjectJson.HasUnsavedChanges(document, null));
    }

    private static void TestBottleShuttleReference()
    {
        var root = FindProjectRoot();
        var loaded = LadderEditorProjectJson.Load(File.ReadAllText(Path.Combine(root,
            "programs", "examples", "02-bottle-shuttle-reference.rpproj.json")));
        True(loaded.IsReadable);
        var program = loaded.Document!.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        True(compiled.IsValid);
        var runtime = new VirtualControllerRuntime(compiled.Program!);
        runtime.Run();
        VirtualControllerSnapshot Scan(bool start = false, bool left = false, bool right = false, bool stop = false)
            => runtime.Scan(Inputs(("start_command", start), ("left_sensor_active", left),
                ("right_sensor_active", right), ("stop_command", stop)));
        void Output(VirtualControllerSnapshot snapshot, bool run, double direction)
        {
            Equal(run, snapshot.Outputs["motor_run"]);
            Equal(direction, snapshot.NumericOutputs["motor_direction"]);
        }
        Output(Scan(left: true), false, 0); // Run alone is not machine Start.
        Output(Scan(start: true), false, 0); // Start away from home is rejected.
        Output(Scan(start: true, left: true), true, 1);
        Output(Scan(left: true), true, 1); // Initial left beam must not stop the outward step.
        Output(Scan(start: true), true, 1); // Repeated Start cannot reset a running cycle.
        Output(runtime.Stop(), false, 0);
        runtime.Run(); Output(Scan(), true, 1);
        Output(Scan(right: true), true, -1);
        Output(runtime.Stop(), false, 0);
        runtime.Run(); Output(Scan(), true, -1);
        Output(Scan(left: true), false, 0);
        Output(Scan(left: true), false, 0); // No automatic second cycle.
        Output(Scan(start: true, left: true), true, 1);
        Output(Scan(stop: true), false, 0); // Machine Stop cancels, distinct from playback pause.
        Output(Scan(), false, 0);
        runtime.Reset(); runtime.Run(); Output(Scan(left: true), false, 0);
        var restored = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(loaded.Document));
        True(restored.IsReadable && LadderCompiler.Compile(restored.Document!.BuildProgram()).IsValid);
    }

    private static void TestAuthoredDemoStructure()
    {
        True(AuthoredDemoLadderPrograms.TryCreate("lab-9-11-pallet-counting", out var batch));
        True(batch.Blocks.Any(block => block.BlockType == LadderBlockType.FunctionBlock));
        True(batch.Blocks.Any(block => block.BlockType == LadderBlockType.Function));
        True(LadderCompiler.Compile(batch.BuildProgram()).IsValid);
        True(AuthoredDemoLadderPrograms.TryCreate("lab-11-13-xy-palletizing", out var cell));
        True(cell.Blocks.Count(block => block.BlockType == LadderBlockType.FunctionBlock) >= 2);
        True(cell.Blocks.Count(block => block.BlockType == LadderBlockType.Function) >= 2);
        True(cell.Blocks.Count(block => block.BlockType == LadderBlockType.DataBlock && block.Interface.Count > 0) >= 2);
        var rungs = cell.Blocks.SelectMany(block => block.Rungs).ToArray();
        True(rungs.Any(rung => rung.IsTimer));
        True(rungs.Any(rung => rung.IsCounter));
        True(rungs.Any(rung => rung.IsNumericOperation));
        True(rungs.Any(rung => rung.Branches.Any(branch => branch.Contacts.Any(contact => contact.IsComparison))));
        True(rungs.Any(rung => rung.Branches.Count > 1));
        True(LadderCompiler.Compile(cell.BuildProgram()).IsValid);
    }

    private static VirtualControllerRuntime PalletizerRuntime()
    {
        True(AuthoredDemoLadderPrograms.TryCreate("lab-11-13-xy-palletizing", out var document));
        var runtime = Runtime(document.BuildProgram());
        runtime.Run();
        return runtime;
    }

    private static VirtualControllerRuntime AuthoredRuntime(string sceneId)
    {
        True(AuthoredDemoLadderPrograms.TryCreate(sceneId, out var document));
        var runtime = Runtime(document.BuildProgram());
        runtime.Run();
        return runtime;
    }

    private static void TestSceneExerciseProjects()
    {
        var root = FindProjectRoot();
        var files = Directory.GetFiles(Path.Combine(root, "scenes", "migrated"), "*.scene.json");
        Equal(77, files.Length);
        var unsupportedCount = 0;
        foreach (var path in files)
        {
            using var scene = JsonDocument.Parse(File.ReadAllText(path));
            SceneAggregateContract.Validate(scene.RootElement.GetProperty("simulation"));
            var id = scene.RootElement.GetProperty("id").GetString()!;
            var points = new List<SceneIoPoint>();
            var initial = new Dictionary<string, object?>();
            if (scene.RootElement.GetProperty("simulation").TryGetProperty("points", out var scenePoints))
            foreach (var point in scenePoints.EnumerateArray())
            {
                var name = point.GetProperty("name").GetString()!;
                var type = point.GetProperty("type").GetString()!;
                var owner = point.GetProperty("owner").GetString()!;
                points.Add(new(name, type, owner, string.Empty, string.Empty, point.TryGetProperty("aggregate", out var metadata) ? PlcAggregates.ParseSchema(metadata) : null,
                    point.TryGetProperty("enumMembers", out var members) ? members.EnumerateArray().Select(member => member.GetString()!).ToArray() : null));
                if (!point.TryGetProperty("initial", out var value)) continue;
                initial[name] = value.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    // Match the plant's JSON scalar reader: integer tokens
                    // remain long even when their declared scene type is REAL.
                    JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
                    JsonValueKind.Number => value.GetDouble(),
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Object or JsonValueKind.Array => value.Clone(),
                    _ => null,
                };
            }
            var document = SceneLadderProject.Create(id, "Exercise", points, initial, out var unsupported);
            unsupportedCount += unsupported.Count;
            Equal(id, document.SourceSceneId);
            True(document.Blocks.All(block => block.Rungs.Count == 0));
            var program = document.BuildProgram();
            True(LadderCompiler.Compile(program).IsValid);
            Equal(0, SceneIoBindingValidator.Validate(program, points).Count);
            foreach (var tag in document.Tags)
            {
                var point = points.Single(point => point.Name == tag.Binding);
                Equal(point.Owner == "PC" ? PlcVariableRole.Input : PlcVariableRole.Output, tag.Role);
                True(SceneIoBindingValidator.TypesCompatible(tag.Type, point.Type));
            }
            foreach (var point in unsupported) False(document.Tags.Any(tag => tag.Binding == point.Name));
        }
        Console.WriteLine($"SCENE_EXERCISE_CATALOG scenes={files.Length} unsupportedIOTypes={unsupportedCount}");
    }

    private static void TestOtherAuthoredDemos()
    {
        var press = AuthoredRuntime("lab-4-01-press-count-lamp");
        for (var count = 1; count <= 3; count++)
        {
            press.Scan(Inputs(("pulse_received", false)));
            var snapshot = press.Scan(Inputs(("pulse_received", true)));
            Equal((long)count, snapshot.Counters["press_count"].Accumulated);
            Equal(count == 3, snapshot.Outputs["threshold_lamp"]);
        }
        False(press.Reset().Outputs["threshold_lamp"]);

        var timer = AuthoredRuntime("lab-5-01-delayed-lamp");
        for (var scan = 0; scan < 99; scan++)
            False(timer.Scan(Inputs(("timer_request", true))).Outputs["delayed_lamp"]);
        True(timer.Scan(Inputs(("timer_request", true))).Outputs["delayed_lamp"]);
        False(timer.Scan(Inputs(("timer_request", false))).Outputs["delayed_lamp"]);

        var conveyor = AuthoredRuntime("scene-1-conveyor-stop");
        True(conveyor.Scan(Inputs(("start_command", true))).Outputs["conveyor_running"]);
        True(conveyor.Scan(Inputs(("start_command", false))).Outputs["conveyor_running"]);
        False(conveyor.Scan(Inputs(("stop_command", true))).Outputs["conveyor_running"]);
        False(conveyor.Scan(Inputs(("start_command", true), ("simulated_photoeye", true))).Outputs["conveyor_running"]);

        var batch = AuthoredRuntime("lab-9-11-pallet-counting");
        // Reject invalid pallet events, including ones received before the
        // count request. Later permissives must not validate those old events.
        foreach (var (validType, request) in new[] { (false, false), (false, true), (true, false) })
        for (var count = 0; count < 5; count++)
        {
            batch.Scan(Inputs(("pallet_detected", false), ("pallet_type_valid", validType), ("count_request", request)));
            batch.Scan(Inputs(("pallet_detected", true), ("pallet_type_valid", validType), ("count_request", request)));
        }
        Equal(0L, batch.Snapshot.Counters["batch_count"].Accumulated);
        False(batch.Scan(Inputs(("pallet_detected", true), ("pallet_type_valid", true),
            ("count_request", true))).Outputs["pallet_count_valid"]);
        Equal(0L, batch.Snapshot.Counters["batch_count"].Accumulated);
        for (var count = 1; count <= 5; count++)
        {
            batch.Scan(Inputs(("pallet_detected", false), ("pallet_type_valid", true), ("count_request", true)));
            batch.Scan(Inputs(("pallet_detected", true), ("pallet_type_valid", true), ("count_request", true)));
            Equal((long)count, batch.Snapshot.Counters["batch_count"].Accumulated);
            Equal(count == 5, batch.Snapshot.Outputs["pallet_count_valid"]);
        }
        False(batch.Scan(Inputs(("pallet_type_valid", false), ("count_request", true))).Outputs["pallet_count_valid"]);
    }

    private static void TestBatchCountNumericOutput()
    {
        True(AuthoredDemoLadderPrograms.TryCreate("lab-9-11-pallet-counting", out var document));
        var program = document.BuildProgram();
        var output = program.Variables.Single(item => item.Name == "pallet_count");
        Equal(PlcVariableType.DInt, output.Type);
        Equal(PlcVariableRole.Output, output.Role);
        Equal("pallet_count", output.Binding);
        var runtime = AuthoredRuntime("lab-9-11-pallet-counting");
        var invalid = Inputs(("pallet_detected", false), ("pallet_type_valid", false), ("count_request", true));
        runtime.Scan(invalid);
        invalid["pallet_detected"] = true;
        Equal(0.0, runtime.Scan(invalid).NumericOutputs["pallet_count"]);
        for (var count = 1; count <= 5; count++)
        {
            runtime.Scan(Inputs(("pallet_detected", false), ("pallet_type_valid", true), ("count_request", true)));
            var snapshot = runtime.Scan(Inputs(("pallet_detected", true), ("pallet_type_valid", true), ("count_request", true)));
            Equal((double)count, SceneIoImageMapper.CommitNumericOutputs(program, snapshot.NumericOutputs)["pallet_count"]);
            Equal(count == 5, snapshot.Outputs["pallet_count_valid"]);
        }
        // Preserve the existing controller policy: Stop clears numeric output
        // images, while the counter memory remains available when Run resumes.
        Equal(0.0, runtime.Stop().NumericOutputs["pallet_count"]);
        Equal(5L, runtime.Snapshot.Counters["batch_count"].Accumulated);
        runtime.Run();
        Equal(5.0, runtime.Scan(Inputs(("pallet_detected", true), ("pallet_type_valid", true), ("count_request", true))).NumericOutputs["pallet_count"]);
        Equal(0.0, runtime.Reset().NumericOutputs["pallet_count"]);
        Equal(0L, runtime.Snapshot.Counters["batch_count"].Accumulated);
    }

    private static void TestPalletizerPermissives()
    {
        var runtime = PalletizerRuntime();
        var inputs = Inputs(("gantry_home", true), ("pallet_position_valid", true), ("carton_at_pick", true), ("start_command", false));
        False(runtime.Scan(inputs).Outputs["gantry_cycle"]);
        inputs["start_command"] = true; True(runtime.Scan(inputs).Outputs["gantry_cycle"]);
        inputs["start_command"] = false; inputs["gantry_home"] = false; inputs["carton_at_pick"] = false;
        True(runtime.Scan(inputs).Outputs["gantry_cycle"]); // Home must go false during travel.
        for (var scan = 0; scan < 100; scan++) runtime.Scan(inputs);
        Equal(0L, runtime.Snapshot.Counters["cycle_count"].Accumulated); // Time alone cannot count.
        inputs["pallet_position_valid"] = false; False(runtime.Scan(inputs).Outputs["gantry_cycle"]);
        inputs["pallet_position_valid"] = true; False(runtime.Scan(inputs).Outputs["gantry_cycle"]);
        inputs["cycle_in_progress"] = true; inputs["carton_attached"] = true; inputs["start_command"] = true;
        True(runtime.Scan(inputs).Outputs["gantry_cycle"]); True(runtime.Snapshot.Outputs["vacuum_pick"]);
        inputs["at_place"] = true; False(runtime.Scan(inputs).Outputs["vacuum_pick"]);
        inputs["palletizer_fault"] = true; False(runtime.Scan(inputs).Outputs["gantry_cycle"]);
    }
    private static void TestPalletizerLayerCount()
    {
        var runtime = PalletizerRuntime();
        var inputs = Inputs(("gantry_home", true), ("pallet_position_valid", true), ("carton_at_pick", true));
        for (var pick = 0; pick < 4; pick++)
        {
            inputs["pick_complete"] = false; inputs["start_command"] = true; runtime.Scan(inputs);
            inputs["start_command"] = false; inputs["pick_complete"] = true; runtime.Scan(inputs);
            for (var scan = 0; scan < 40; scan++) runtime.Scan(inputs);
            Equal((long)pick + 1, runtime.Snapshot.Counters["cycle_count"].Accumulated);
        }
        True(runtime.Snapshot.Outputs["layer_complete"]);
        Equal(1.0, runtime.Snapshot.NumericVariables["layer_count"]);
        inputs["pick_complete"] = false; inputs["start_command"] = true;
        for (var scan = 0; scan < 100; scan++) runtime.Scan(inputs);
        Equal(4L, runtime.Snapshot.Counters["cycle_count"].Accumulated);
        False(runtime.Snapshot.Outputs["gantry_cycle"]); False(runtime.Snapshot.Outputs["vacuum_pick"]);
        runtime.Reset(); runtime.Run(); inputs["start_command"] = false;
        False(runtime.Scan(inputs).Outputs["gantry_cycle"]);
        inputs["start_command"] = true; True(runtime.Scan(inputs).Outputs["gantry_cycle"]);
        Equal(0L, runtime.Snapshot.Counters["cycle_count"].Accumulated);
    }

    private static void Test(string name, Action body)
    {
        try
        {
            body();
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception exception)
        {
            _failed++;
            Console.WriteLine($"FAIL {name}: {exception.Message}");
        }
    }

    private static void TestNormallyOpen()
    {
        var runtime = Runtime(SimpleProgram(Contact("contact", "input")));
        runtime.Run();
        False(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
    }

    private static void TestNormallyClosed()
    {
        var runtime = Runtime(SimpleProgram(Contact("contact", "input", closed: true)));
        runtime.Run();
        True(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
    }

    private static void TestRisingEdgeContact()
    {
        var runtime = Runtime(SimpleProgram(new LadderNode(
            "rising", LadderNodeKind.Contact, "input", EdgeMode: LadderEdgeMode.Rising)));
        runtime.Run();
        False(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        False(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
    }

    private static void TestFallingEdgeContact()
    {
        var runtime = Runtime(SimpleProgram(new LadderNode(
            "falling", LadderNodeKind.Contact, "input", EdgeMode: LadderEdgeMode.Falling)));
        runtime.Run();
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        False(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
    }

    private static void TestEdgeLifecycle()
    {
        var runtime = Runtime(SimpleProgram(new LadderNode(
            "edge", LadderNodeKind.Contact, "input", EdgeMode: LadderEdgeMode.Rising)));
        runtime.Run();
        False(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        runtime.Stop();
        runtime.Run();
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        runtime.Reset();
        runtime.Run();
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
    }

    private static void TestFirstScanOperatorPulse()
    {
        var runtime = Runtime(SimpleProgram(new LadderNode("edge", LadderNodeKind.Contact, "input", EdgeMode: LadderEdgeMode.Rising)));
        var session = new VirtualControllerSession(runtime);
        session.Run();
        session.PulseInput("input");
        session.Advance(.020, () => Inputs(("input", false)), outputs => True(outputs["output"]), _ => { });
        session.Advance(.020, () => Inputs(("input", false)), outputs => False(outputs["output"]), _ => { });
        session.Reset(); session.Run();
        session.Advance(.020, () => Inputs(("input", true)), outputs => False(outputs["output"]), _ => { });
        session.Reset(); session.Run();
        session.SetBoolForce("input", true); session.PulseInput("input");
        session.Advance(.020, () => Inputs(("input", false)), outputs => False(outputs["output"]), _ => { });
        session.Reset(); session.Run(); session.PulseInput("input"); session.Stop(); session.Run();
        session.Advance(.020, () => Inputs(("input", false)), outputs => False(outputs["output"]), _ => { });
    }

    private static void TestEdgeIsolation()
    {
        var program = new LadderProgram(
            1, "edge-isolation", "EdgeIsolation", "LD", TimeSpan.FromMilliseconds(20),
            [
                new("input", PlcVariableType.Bool, PlcVariableRole.Input, false),
                new("output_a", PlcVariableType.Bool, PlcVariableRole.Output, false),
                new("output_b", PlcVariableType.Bool, PlcVariableRole.Output, false),
            ],
            [
                new("network-a", "A", new LadderNode("edge-a", LadderNodeKind.Contact, "input", EdgeMode: LadderEdgeMode.Rising), new LadderCoil("coil-a", "output_a")),
                new("network-b", "B", new LadderNode("edge-b", LadderNodeKind.Contact, "input", EdgeMode: LadderEdgeMode.Rising), new LadderCoil("coil-b", "output_b")),
            ]);
        var runtime = Runtime(program);
        runtime.Run();
        runtime.Scan(Inputs(("input", false)));
        var pulse = runtime.Scan(Inputs(("input", true)));
        True(pulse.Outputs["output_a"]);
        True(pulse.Outputs["output_b"]);
    }

    private static void TestEdgeRoundTrip()
    {
        var editor = new LadderEditorDocument { Id = "edge-test", Name = "EdgeMain" };
        editor.AddTag("sensor", PlcVariableRole.Input);
        editor.AddTag("pulse", PlcVariableRole.Output);
        editor.AddRung("Detect sensor edge", "pulse");
        var edge = editor.InsertEdgeContact(0, 0, 0, "sensor", LadderEdgeMode.Rising);
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(editor.BuildProgram()));
        True(loaded.IsValid && loaded.Program is not null);
        var loadedNode = loaded.Program!.Networks[0].Logic.Children![0];
        Equal(LadderEdgeMode.Rising, loadedNode.EdgeMode);
        Equal(edge.Id, loadedNode.Id);
        var restored = new LadderEditorDocument();
        restored.ReplaceFromProgram(loaded.Program);
        Equal(LadderEdgeMode.Rising, restored.Rungs[0].Branches[0].Contacts[0].EdgeMode);
        Equal(edge.Id, restored.Rungs[0].Branches[0].Contacts[0].Id);
    }

    private static void TestSeries()
    {
        var logic = new LadderNode("series", LadderNodeKind.Series, Children:
            [Contact("a", "input"), Contact("b", "input2")]);
        var runtime = Runtime(SimpleProgram(logic, secondInput: true));
        runtime.Run();
        False(runtime.Scan(Inputs(("input", true), ("input2", false))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true), ("input2", true))).Outputs["output"]);
    }

    private static void TestParallel()
    {
        var logic = new LadderNode("parallel", LadderNodeKind.Parallel, Children:
            [Contact("a", "input"), Contact("b", "input2")]);
        var runtime = Runtime(SimpleProgram(logic, secondInput: true));
        runtime.Run();
        True(runtime.Scan(Inputs(("input", false), ("input2", true))).Outputs["output"]);
        False(runtime.Scan(Inputs(("input", false), ("input2", false))).Outputs["output"]);
    }

    private static void TestCoilAssignment()
    {
        var runtime = Runtime(SimpleProgram(Contact("contact", "input")));
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("input", true)));
        True(snapshot.Variables["output"]);
        True(snapshot.Elements["coil"].Energized);
    }

    private static void TestSealIn()
    {
        var runtime = DemoRuntime();
        runtime.Run();
        True(runtime.Scan(Inputs(("start_command", true))).Outputs["conveyor_running"]);
        True(runtime.Scan(Inputs(("start_command", false))).Outputs["conveyor_running"]);
    }

    private static void TestStopBreaksSeal()
    {
        var runtime = DemoRuntime();
        runtime.Run();
        runtime.Scan(Inputs(("start_command", true)));
        var snapshot = runtime.Scan(Inputs(("start_command", false), ("stop_command", true)));
        False(snapshot.Variables["seal_in"]);
        False(snapshot.Outputs["conveyor_running"]);
    }

    private static void TestPhotoeyeStops()
    {
        var runtime = DemoRuntime();
        runtime.Run();
        runtime.Scan(Inputs(("start_command", true)));
        var snapshot = runtime.Scan(Inputs(("start_command", false), ("simulated_photoeye", true)));
        False(snapshot.Variables["seal_in"]);
        False(snapshot.Outputs["conveyor_running"]);
    }

    private static void TestBlockedRestart()
    {
        var runtime = DemoRuntime();
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("start_command", true), ("simulated_photoeye", true)));
        False(snapshot.Variables["seal_in"]);
        False(snapshot.Outputs["conveyor_running"]);
    }

    private static void TestDeterminism()
    {
        static string Run()
        {
            var runtime = DemoRuntime();
            runtime.Run();
            var states = new List<string>();
            foreach (var inputs in new[]
                     {
                         Inputs(("start_command", true)),
                         Inputs(("start_command", false)),
                         Inputs(("simulated_photoeye", true)),
                         Inputs(("simulated_photoeye", false)),
                     })
            {
                var snapshot = runtime.Scan(inputs);
                states.Add(string.Join(",", snapshot.Variables.OrderBy(item => item.Key)
                    .Select(item => $"{item.Key}={item.Value}")));
            }
            return string.Join("|", states);
        }
        Equal(Run(), Run());
    }

    private static void TestMissingVariable() =>
        HasIssue(SimpleProgram(Contact("bad", "missing")), "VC001");

    private static void TestInvalidType()
    {
        var program = SimpleProgram(Contact("contact", "input"));
        var variables = program.Variables.Select(item => item.Name == "input"
            ? item with { Type = PlcVariableType.Int, InitialValue = 0 }
            : item).ToArray();
        HasIssue(program with { Variables = variables }, "VC002");
    }

    private static void TestDuplicateId()
    {
        var program = SimpleProgram(Contact("network", "input"));
        HasIssue(program, "VC004");
    }

    private static void TestInvalidOutputTarget()
    {
        var program = SimpleProgram(Contact("contact", "input"));
        var network = program.Networks[0] with { Coil = new LadderCoil("coil", "input") };
        HasIssue(program with { Networks = [network] }, "VC005");
    }

    private static void TestMalformedBranch()
    {
        var logic = new LadderNode("parallel", LadderNodeKind.Parallel, Children: [Contact("only", "input")]);
        HasIssue(SimpleProgram(logic), "VC003");
    }

    private static void TestUnknownContactType()
    {
        var root = FindProjectRoot();
        var json = File.ReadAllText(Path.Combine(root, "programs", "demo-conveyor.ld.json"))
            .Replace("normallyOpen", "invalidContact", StringComparison.Ordinal);
        var loaded = LadderProgramJson.Load(json);
        if (loaded.IsValid || !loaded.Issues.Any(item => item.Code == "VC100"))
            throw new InvalidOperationException("Expected invalid contact validation issue VC100.");
    }

    private static void TestMissingProgramIdentity()
    {
        var program = SimpleProgram(Contact("contact", "input")) with { Id = string.Empty };
        HasIssue(program, "VC100");
    }

    private static void TestReset()
    {
        var runtime = DemoRuntime();
        runtime.Run();
        runtime.Scan(Inputs(("start_command", true)));
        var snapshot = runtime.Reset();
        False(snapshot.Variables["seal_in"]);
        False(snapshot.Outputs["conveyor_running"]);
        Equal(0L, snapshot.ScanNumber);
    }

    private static void TestScanOrder()
    {
        var runtime = DemoRuntime();
        var session = new VirtualControllerSession(runtime);
        var order = new List<string>();
        session.SnapshotPublished += _ => order.Add("publish");
        session.PulseInput("start_command");
        session.Run();
        order.Clear();
        var scans = session.Advance(0.020, () =>
        {
            order.Add("sample");
            return Inputs(("simulated_photoeye", false));
        }, outputs =>
        {
            order.Add("commit");
            True(outputs["conveyor_running"]);
        }, elapsed =>
        {
            order.Add("plant");
            Equal(0.020, elapsed);
        });
        Equal(1, scans);
        Equal("sample,commit,plant,publish", string.Join(",", order));
    }

    private static void TestStopCancelsPulse()
    {
        var session = new VirtualControllerSession(DemoRuntime());
        session.Run();
        session.PulseInput("start_command");
        session.Stop();
        session.Run();
        session.Advance(0.020, () => Inputs(("simulated_photoeye", false)),
            outputs => False(outputs["conveyor_running"]), _ => { });
        session.PulseInput("start_command");
        session.Advance(0.020, () => Inputs(("simulated_photoeye", false)),
            outputs => True(outputs["conveyor_running"]), _ => { });
    }

    private static void TestNumericScanOrder()
    {
        var runtime = Runtime(NumericProgram(LadderNumericOperationKind.Move));
        var session = new VirtualControllerSession(runtime);
        var order = new List<string>();
        session.SnapshotPublished += _ => order.Add("publish");
        session.Run();
        order.Clear();
        var scans = session.Advance(0.020,
            () =>
            {
                order.Add("sample-bool");
                return Inputs(("enable", true));
            },
            () =>
            {
                order.Add("sample-numeric");
                return NumericInputs(("left", 12.5), ("right", 2.0));
            },
            _ => order.Add("commit-bool"),
            outputs =>
            {
                order.Add("commit-numeric");
                Equal(12.5, outputs["real_result"]);
            },
            elapsed =>
            {
                order.Add("plant");
                Equal(0.020, elapsed);
            });
        Equal(1, scans);
        Equal("sample-bool,sample-numeric,commit-bool,commit-numeric,plant,publish", string.Join(",", order));
    }

    private static void TestEditorDocumentBuild()
    {
        var document = new LadderEditorDocument { Id = "editor-test", Name = "MainRoutine" };
        document.AddTag("sensor", PlcVariableRole.Input, "scene_sensor");
        document.AddTag("motor", PlcVariableRole.Output, "scene_motor");
        document.AddRung("Motor command", "motor");
        document.AddContact(0, 0, "sensor", false);
        var program = document.BuildProgram();
        Equal("MainRoutine", program.Name);
        Equal(1, program.Networks.Count);
        Equal("scene_motor", program.Variables.Single(item => item.Name == "motor").Binding);
        True(LadderCompiler.Compile(program).IsValid);
    }

    private static void TestEditorExactInsertion()
    {
        var document = new LadderEditorDocument { Id = "insert-test", Name = "Main" };
        document.AddTag("first", PlcVariableRole.Input);
        document.AddTag("middle", PlcVariableRole.Input);
        document.AddTag("last", PlcVariableRole.Input);
        document.AddTag("limit", PlcVariableRole.Memory, type: PlcVariableType.Int);
        document.AddTag("motor", PlcVariableRole.Output);
        document.AddRung("Ordered path", "motor");
        var first = document.AddContact(0, 0, "first", false);
        var last = document.AddContact(0, 0, "last", false);
        var middle = document.InsertContact(0, 0, 1, "middle", true);
        var comparison = document.InsertComparison(0, 0, 2, "limit", LadderCompareOperator.GreaterOrEqual, "10");
        var contacts = document.Rungs[0].Branches[0].Contacts;
        Equal("first,middle,limit,last", string.Join(",", contacts.Select(contact => contact.Variable)));
        Equal(first.Id, contacts[0].Id);
        Equal(middle.Id, contacts[1].Id);
        Equal(comparison.Id, contacts[2].Id);
        Equal(last.Id, contacts[3].Id);
        True(contacts[1].NormallyClosed);
        True(contacts[2].IsComparison);
        True(LadderCompiler.Compile(document.BuildProgram()).IsValid);
    }

    private static void TestEditorParallelBranches()
    {
        var document = new LadderEditorDocument { Id = "parallel-test", Name = "OB1" };
        document.AddTag("left", PlcVariableRole.Input);
        document.AddTag("right", PlcVariableRole.Input);
        document.AddTag("output", PlcVariableRole.Output);
        document.AddRung("Either input", "output");
        document.AddContact(0, 0, "left", false);
        document.AddParallelBranch(0);
        document.AddContact(0, 1, "right", false);
        var runtime = Runtime(document.BuildProgram());
        runtime.Run();
        True(runtime.Scan(Inputs(("left", false), ("right", true))).Outputs["output"]);
    }

    private static void TestEditorValidation()
    {
        var document = new LadderEditorDocument { Id = "invalid-editor", Name = "Main" };
        document.AddTag("input", PlcVariableRole.Input);
        document.AddRung("Invalid input coil", "input");
        document.AddContact(0, 0, "input", false);
        HasIssue(document.BuildProgram(), "VC005");
    }

    private static void TestEditorRoundTrip()
    {
        var original = LadderEditorDocument.CreateConveyorExample().BuildProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("Saved editor program did not reload.");
        Equal(original.Variables.Count, loaded.Program.Variables.Count);
        Equal(original.Networks.Count, loaded.Program.Networks.Count);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestEditableProjectRoundTrip()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        editor.SourceSceneId = "lab-4-01-press-count-lamp";
        var draft = editor.AddBlock("DraftSequence");
        editor.SelectBlock(editor.Blocks.Count - 1);
        editor.AddJumpRung("Unfinished jump", "missing_destination");
        editor.AddContact(0, 0, "not_declared_yet", false);
        editor.WatchVariables.Add("future_tag");
        var before = editor.CaptureSnapshot();
        var json = LadderEditorProjectJson.Save(editor);
        var loaded = LadderEditorProjectJson.Load(json);
        True(loaded.IsReadable && loaded.Document is not null);
        Equal(json, LadderEditorProjectJson.Save(loaded.Document!));
        Equal(before.ActiveBlockIndex, loaded.Document!.ActiveBlockIndex);
        Equal(before.EntryBlockId, loaded.Document.EntryBlockId);
        Equal(before.SourceSceneId, loaded.Document.SourceSceneId);
        Equal(draft.Id, loaded.Document.Blocks[^1].Id);
        Equal(editor.Rungs[0].Id, loaded.Document.Rungs[0].Id);
        Equal(editor.Rungs[0].Branches[0].Contacts[0].Id,
            loaded.Document.Rungs[0].Branches[0].Contacts[0].Id);
        False(LadderCompiler.Compile(loaded.Document.BuildProgram()).IsValid);
        var originalNext = editor.AddRung("Next original", "conveyor_running").Id;
        var loadedNext = loaded.Document.AddRung("Next loaded", "conveyor_running").Id;
        Equal(originalNext, loadedNext);

        var staleCounterJson = json.Replace(
            $"\"nextId\": {before.NextId}", "\"nextId\": 1", StringComparison.Ordinal);
        var staleCounterLoad = LadderEditorProjectJson.Load(staleCounterJson);
        True(staleCounterLoad.IsReadable && staleCounterLoad.Document is not null);
        var repairedNext = staleCounterLoad.Document!.AddRung("After stale counter", "conveyor_running").Id;
        False(before.Blocks.SelectMany(block => block.Rungs).Any(rung => rung.Id == repairedNext));
    }

    private static void TestEditableProjectMalformed()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var json = LadderEditorProjectJson.Save(editor)
            .Replace("\"coilMode\": \"Assign\"", "\"coilMode\": \"not-a-coil-mode\"", StringComparison.Ordinal);
        var loaded = LadderEditorProjectJson.Load(json);
        False(loaded.IsReadable);
        True(loaded.Issues.Any(issue => issue.Code == "EPJ001"));
        False(LadderEditorProjectJson.Load("{ broken").IsReadable);
        var nullTags = LadderEditorProjectJson.Save(editor)
            .Replace("\"tags\": [", "\"tags\": null,\n  \"discardedTags\": [", StringComparison.Ordinal);
        False(LadderEditorProjectJson.Load(nullTags).IsReadable);
    }

    private static void TestProjectNullEntries()
    {
        var original = LadderEditorProjectJson.Save(LadderEditorDocument.CreateConveyorExample());
        foreach (var key in new[] { "blocks", "tags", "tasks", "rungs", "branches", "contacts", "interface", "watchVariables" })
        {
            var root = JsonNode.Parse(original)!;
            var block = root["blocks"]![0]!;
            var rung = block["rungs"]![0]!;
            // v2 uses a recursive root; retain a deliberately malformed legacy
            // collection too so neither format can hide null collection entries.
            if (rung["branches"]!.AsArray().Count == 0)
                rung["branches"]!.AsArray().Add(new JsonObject { ["id"] = "legacy-probe", ["contacts"] = new JsonArray() });
            var branch = rung["branches"]![0]!;
            var target = key switch { "rungs" or "interface" => block, "branches" => rung, "contacts" => branch, _ => root };
            target[key] = new JsonArray((JsonNode?)null);
            var result = LadderEditorProjectJson.Load(root.ToJsonString());
            False(result.IsReadable);
            True(result.Document is null && result.Issues.Count > 0);
        }
        var malformedTree = JsonNode.Parse(original)!;
        malformedTree["blocks"]![0]!["rungs"]![0]!["root"]!["children"] = new JsonArray((JsonNode?)null);
        False(LadderEditorProjectJson.Load(malformedTree.ToJsonString()).IsReadable);
        True(LadderEditorProjectJson.Load(original).IsReadable);
    }

    private static void TestSessionInvalidTime()
    {
        var session = new VirtualControllerSession(DemoRuntime());
        session.Run();
        foreach (var delta in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var rejected = false;
            try { session.Advance(delta, () => Inputs(), _ => { }, _ => { }); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            True(rejected);
        }
        Equal(1, session.Advance(.020, () => Inputs(), _ => { }, _ => { }));
    }

    private static void TestSessionBudget()
    {
        var session = new VirtualControllerSession(DemoRuntime());
        session.Run();
        Equal(1000, session.Advance(20.04, () => Inputs(), _ => { }, _ => { }));
        Equal(2, session.Advance(0, () => Inputs(), _ => { }, _ => { }));
        Equal(0, session.Advance(0, () => Inputs(), _ => { }, _ => { }));
        session.Advance(20.04, () => Inputs(), _ => { }, _ => { });
        session.Stop();
        var stoppedScan = session.Snapshot.ScanNumber;
        var stoppedCallbacks = 0;
        session.SnapshotPublished += _ => stoppedCallbacks++;
        session.ScanCompleted += _ => stoppedCallbacks++;
        Equal(0, session.Advance(20.04,
            () => { stoppedCallbacks++; return Inputs(); },
            _ => stoppedCallbacks++, _ => stoppedCallbacks++));
        Equal(0, stoppedCallbacks);
        Equal(stoppedScan, session.Snapshot.ScanNumber);
        session.Run();
        Equal(0, session.Advance(0, () => Inputs(), _ => { }, _ => { }));
        var rejectedCommits = 0;
        var rejectedPlantTicks = 0;
        Equal(0, session.Advance(.020,
            () => { session.Stop(); return Inputs(); },
            _ => rejectedCommits++, _ => rejectedPlantTicks++));
        Equal(0, rejectedCommits);
        Equal(0, rejectedPlantTicks);
        Equal(stoppedScan, session.Snapshot.ScanNumber);
    }

    private static void TestMinimumScanPeriod()
    {
        var program = new LadderProgram(1, "period-review", "Period review", "LD", TimeSpan.FromTicks(1), [], []);
        False(LadderCompiler.Compile(program).IsValid);
        True(LadderCompiler.Compile(program with { ScanPeriod = TimeSpan.FromMilliseconds(1) }).IsValid);
    }

    private static void TestNewProjectReset()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        editor.ScanPeriod = TimeSpan.FromMilliseconds(100);
        editor.AddBlock("OldSequence");
        editor.AddTask("OldPeriodic", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(200), 20,
            editor.Blocks[^1].Id);
        editor.WatchVariables.Add("duplicate_watch_metadata");
        _ = editor.AddRung("Consume stable IDs", "conveyor_running");

        editor.ResetProject("new-offline-project", "MainRoutine", TimeSpan.FromMilliseconds(20));

        Equal("new-offline-project", editor.Id);
        Equal("MainRoutine", editor.Name);
        Equal(TimeSpan.FromMilliseconds(20), editor.ScanPeriod);
        Equal(0, editor.ActiveBlockIndex);
        Equal(0, editor.Tags.Count);
        Equal(0, editor.WatchVariables.Count);
        Equal(1, editor.Blocks.Count);
        Equal("block-1", editor.Blocks[0].Id);
        Equal(editor.Blocks[0].Id, editor.EntryBlockId);
        Equal(1, editor.Tasks.Count);
        Equal("task-2", editor.Tasks[0].Id);
        Equal(editor.EntryBlockId, editor.Tasks[0].EntryBlock);
        Equal(0, editor.Rungs.Count);
    }

    private static void TestTagInitialValues()
    {
        True(LadderEditorDocument.TryParseInitialValue(PlcVariableType.Bool, "TRUE", out var boolInitial, out _));
        True(LadderEditorDocument.TryParseInitialValue(PlcVariableType.Int, "-123", out var intInitial, out _));
        True(LadderEditorDocument.TryParseInitialValue(PlcVariableType.DInt, "200000", out var dIntInitial, out _));
        True(LadderEditorDocument.TryParseInitialValue(PlcVariableType.Real, "12.5", out var realInitial, out _));
        False(LadderEditorDocument.TryParseInitialValue(PlcVariableType.Int, "40000", out _, out _));
        False(LadderEditorDocument.TryParseInitialValue(PlcVariableType.Real, "NaN", out _, out _));

        var document = new LadderEditorDocument { Id = "initial-values", Name = "Main" };
        document.AddTag("bool_memory", PlcVariableRole.Memory, type: PlcVariableType.Bool, initialValue: boolInitial);
        document.AddTag("int_memory", PlcVariableRole.Memory, type: PlcVariableType.Int, initialValue: intInitial);
        document.AddTag("dint_memory", PlcVariableRole.Memory, type: PlcVariableType.DInt, initialValue: dIntInitial);
        document.AddTag("real_memory", PlcVariableRole.Memory, type: PlcVariableType.Real, initialValue: realInitial);
        document.AddTag("output", PlcVariableRole.Output);
        document.AddRung("Valid program", "output");
        document.AddContact(0, 0, "bool_memory", false);

        var saved = LadderProgramJson.Save(document.BuildProgram());
        var loaded = LadderProgramJson.Load(saved);
        True(loaded.IsValid && loaded.Program is not null);
        Equal(true, loaded.Program!.Variables.Single(item => item.Name == "bool_memory").InitialValue);
        Equal(-123L, loaded.Program.Variables.Single(item => item.Name == "int_memory").InitialValue);
        Equal(200000L, loaded.Program.Variables.Single(item => item.Name == "dint_memory").InitialValue);
        Equal(12.5, loaded.Program.Variables.Single(item => item.Name == "real_memory").InitialValue);

        var runtime = Runtime(loaded.Program);
        True(runtime.Snapshot.Variables["bool_memory"]);
        Equal(-123.0, runtime.Snapshot.NumericVariables["int_memory"]);
        Equal(200000.0, runtime.Snapshot.NumericVariables["dint_memory"]);
        Equal(12.5, runtime.Snapshot.NumericVariables["real_memory"]);

        var outOfRange = loaded.Program with
        {
            Variables = loaded.Program.Variables.Select(item => item.Name == "int_memory"
                ? item with { InitialValue = 40000L }
                : item).ToArray(),
        };
        HasIssue(outOfRange, "VC002");
    }

    private static void TestWatchTablePersistence()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        editor.WatchVariables.Clear();
        editor.WatchVariables.Add("start_command");
        editor.AddTag("unused_spare", PlcVariableRole.Memory);
        editor.WatchVariables.Add("unused_spare");

        editor.UpdateTag(
            "start_command",
            "cycle_start",
            PlcVariableType.Bool,
            PlcVariableRole.Input,
            "operator.start");
        Equal("cycle_start", editor.WatchVariables[0]);

        True(editor.TryRemoveTag("unused_spare", out var references));
        Equal(0, references);
        False(editor.WatchVariables.Contains("unused_spare", StringComparer.Ordinal));

        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(editor.BuildProgram()));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(1, loaded.Program!.WatchVariables!.Count);
        Equal("cycle_start", loaded.Program.WatchVariables[0]);

        var restored = new LadderEditorDocument();
        restored.ReplaceFromProgram(loaded.Program);
        Equal("cycle_start", restored.WatchVariables[0]);
    }

    private static void TestWatchTableValidationAndUndo()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        editor.WatchVariables.Clear();
        var history = new LadderEditorHistory();
        history.Execute(editor, "Add watched symbol", () => editor.WatchVariables.Add("start_command"));
        Equal("start_command", editor.WatchVariables[0]);
        True(history.Undo(editor, out var description));
        Equal("Add watched symbol", description);
        Equal(0, editor.WatchVariables.Count);
        True(history.Redo(editor, out _));
        Equal("start_command", editor.WatchVariables[0]);

        HasIssue(editor.BuildProgram() with { WatchVariables = ["missing_symbol"] }, "VC001");
        HasIssue(editor.BuildProgram() with { WatchVariables = ["start_command", "start_command"] }, "VC004");
    }

    private static void TestSceneIoBindings()
    {
        var points = new[]
        {
            new SceneIoPoint("photoeye", "BOOL", "PC", "input", "Simulated sensor"),
            new SceneIoPoint("motor_run", "BOOL", "PLC", "output", "Motor command"),
            new SceneIoPoint("level", "REAL", "PC", "input", "Analog feedback"),
            new SceneIoPoint("selector", "DINT", "PC", "input", "Selector position"),
            new SceneIoPoint("speed_command", "DINT", "PLC", "output", "Speed command"),
        };
        var valid = SimpleProgram(Contact("input-contact", "sensor")) with
        {
            Variables = [
                new("sensor", PlcVariableType.Bool, PlcVariableRole.Input, false, "photoeye"),
                new("output", PlcVariableType.Bool, PlcVariableRole.Output, false, "motor_run"),
                new("level_value", PlcVariableType.Real, PlcVariableRole.Input, 0.0, "level"),
                new("selector_value", PlcVariableType.DInt, PlcVariableRole.Input, 0L, "selector"),
                new("speed_value", PlcVariableType.DInt, PlcVariableRole.Output, 0L, "speed_command")
            ]
        };
        Equal(0, SceneIoBindingValidator.Validate(valid, points).Count);
        var invalid = valid with
        {
            Variables = [
                new("sensor", PlcVariableType.Bool, PlcVariableRole.Input, false, "motor_run"),
                new("output", PlcVariableType.Bool, PlcVariableRole.Output, false, "photoeye"),
                new("duplicate", PlcVariableType.Bool, PlcVariableRole.Output, false, "photoeye"),
                new("numeric", PlcVariableType.Real, PlcVariableRole.Input, 0.0, "selector"),
                new("missing", PlcVariableType.Bool, PlcVariableRole.Input, false, "not_declared")
            ]
        };
        var issues = SceneIoBindingValidator.Validate(invalid, points);
        True(issues.Any(issue => issue.Code == "IO001"));
        True(issues.Any(issue => issue.Code == "IO002"));
        True(issues.Count(issue => issue.Code == "IO003") >= 2);
        var duplicateOutputs = valid with
        {
            Variables = [
                new("sensor", PlcVariableType.Bool, PlcVariableRole.Input, false, "photoeye"),
                new("output", PlcVariableType.Bool, PlcVariableRole.Output, false, "motor_run"),
                new("output2", PlcVariableType.Bool, PlcVariableRole.Output, false, "motor_run")
            ]
        };
        True(SceneIoBindingValidator.Validate(duplicateOutputs, points).Any(issue => issue.Code == "IO004"));
    }

    private static void TestTonBeforePreset()
    {
        var runtime = Runtime(TimerProgram(TimeSpan.FromMilliseconds(60)));
        runtime.Run();
        var first = runtime.Scan(Inputs(("input", true)));
        var second = runtime.Scan(Inputs(("input", true)));
        Equal(TimeSpan.FromMilliseconds(40), second.Timers["delay"].Accumulated);
        True(first.Timers["delay"].Timing);
        True(second.Timers["delay"].Timing);
        False(second.Timers["delay"].Done);
    }

    private static void TestTonAtPreset()
    {
        var runtime = Runtime(TimerProgram(TimeSpan.FromMilliseconds(40)));
        runtime.Run();
        runtime.Scan(Inputs(("input", true)));
        var snapshot = runtime.Scan(Inputs(("input", true)));
        Equal(TimeSpan.FromMilliseconds(40), snapshot.Timers["delay"].Accumulated);
        False(snapshot.Timers["delay"].Timing);
        True(snapshot.Timers["delay"].Done);
    }

    private static void TestTonResets()
    {
        var runtime = Runtime(TimerProgram(TimeSpan.FromMilliseconds(60)));
        runtime.Run();
        runtime.Scan(Inputs(("input", true)));
        var snapshot = runtime.Scan(Inputs(("input", false)));
        Equal(TimeSpan.Zero, snapshot.Timers["delay"].Accumulated);
        False(snapshot.Timers["delay"].Timing);
        False(snapshot.Timers["delay"].Done);
    }

    private static void TestTonDoneContact()
    {
        var runtime = Runtime(TimerProgram(TimeSpan.FromMilliseconds(40), includeDoneCoil: true));
        runtime.Run();
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
    }

    private static void TestTonStopAndReset()
    {
        var runtime = Runtime(TimerProgram(TimeSpan.FromMilliseconds(20)));
        runtime.Run();
        True(runtime.Scan(Inputs(("input", true))).Timers["delay"].Done);
        False(runtime.Stop().Timers["delay"].Done);
        runtime.Run();
        runtime.Scan(Inputs(("input", true)));
        var reset = runtime.Reset();
        Equal(TimeSpan.Zero, reset.Timers["delay"].Accumulated);
        False(reset.Timers["delay"].Done);
    }

    private static void TestTofDelay()
    {
        var runtime = Runtime(TimerProgram(
            TimeSpan.FromMilliseconds(40), kind: LadderTimerKind.OffDelay));
        runtime.Run();
        False(runtime.Scan(Inputs(("input", false))).Timers["delay"].Done);
        var on = runtime.Scan(Inputs(("input", true))).Timers["delay"];
        True(on.Done);
        False(on.Timing);
        var delaying = runtime.Scan(Inputs(("input", false))).Timers["delay"];
        Equal(TimeSpan.FromMilliseconds(20), delaying.Accumulated);
        True(delaying.Done);
        True(delaying.Timing);
        var expired = runtime.Scan(Inputs(("input", false))).Timers["delay"];
        Equal(TimeSpan.FromMilliseconds(40), expired.Accumulated);
        False(expired.Done);
        False(expired.Timing);
    }

    private static void TestTpPulse()
    {
        var runtime = Runtime(TimerProgram(
            TimeSpan.FromMilliseconds(60), kind: LadderTimerKind.Pulse));
        runtime.Run();
        runtime.Scan(Inputs(("input", false)));
        var started = runtime.Scan(Inputs(("input", true))).Timers["delay"];
        True(started.Done);
        True(started.Timing);
        True(runtime.Scan(Inputs(("input", true))).Timers["delay"].Done);
        var expired = runtime.Scan(Inputs(("input", true))).Timers["delay"];
        False(expired.Done);
        False(expired.Timing);
        False(runtime.Scan(Inputs(("input", true))).Timers["delay"].Done);
        runtime.Scan(Inputs(("input", false)));
        True(runtime.Scan(Inputs(("input", true))).Timers["delay"].Done);
    }

    private static void TestRetentiveTimer()
    {
        var runtime = Runtime(RetentiveTimerProgram());
        runtime.Run();
        Equal(TimeSpan.FromMilliseconds(20),
            runtime.Scan(Inputs(("input", true), ("reset", false))).Timers["delay"].Accumulated);
        var paused = runtime.Scan(Inputs(("input", false), ("reset", false))).Timers["delay"];
        Equal(TimeSpan.FromMilliseconds(20), paused.Accumulated);
        False(paused.Timing);
        False(paused.Done);

        var stopped = runtime.Stop().Timers["delay"];
        Equal(TimeSpan.FromMilliseconds(20), stopped.Accumulated);
        False(stopped.Timing);
        runtime.Run();
        Equal(TimeSpan.FromMilliseconds(40),
            runtime.Scan(Inputs(("input", true), ("reset", false))).Timers["delay"].Accumulated);
        var done = runtime.Scan(Inputs(("input", true), ("reset", false))).Timers["delay"];
        Equal(TimeSpan.FromMilliseconds(60), done.Accumulated);
        True(done.Done);
        var retainedDone = runtime.Scan(Inputs(("input", false), ("reset", false))).Timers["delay"];
        True(retainedDone.Done);
        Equal(TimeSpan.FromMilliseconds(60), retainedDone.Accumulated);
    }

    private static void TestRetentiveTimerReset()
    {
        var runtime = Runtime(RetentiveTimerProgram());
        runtime.Run();
        runtime.Scan(Inputs(("input", true), ("reset", false)));
        runtime.Scan(Inputs(("input", true), ("reset", false)));
        var cleared = runtime.Scan(Inputs(("input", false), ("reset", true))).Timers["delay"];
        Equal(TimeSpan.Zero, cleared.Accumulated);
        False(cleared.Timing);
        False(cleared.Done);
        False(cleared.Input);
    }

    private static void TestTimerRoundTrip()
    {
        var original = TimerProgram(
            TimeSpan.FromMilliseconds(125), includeDoneCoil: true, kind: LadderTimerKind.OffDelay);
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("Saved timer program did not reload.");
        Equal(TimeSpan.FromMilliseconds(125), loaded.Program.Networks[0].Timer!.Preset);
        Equal(LadderTimerKind.OffDelay, loaded.Program.Networks[0].Timer!.Kind);
        Equal(PlcVariableType.Timer, loaded.Program.Variables.Single(item => item.Name == "delay").Type);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].IsTimer);
        Equal(LadderTimerKind.OffDelay, editor.Rungs[0].TimerKind);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);

        var retentive = RetentiveTimerProgram();
        var retentiveLoaded = LadderProgramJson.Load(LadderProgramJson.Save(retentive));
        True(retentiveLoaded.IsValid && retentiveLoaded.Program is not null);
        Equal(LadderTimerKind.RetentiveOnDelay, retentiveLoaded.Program!.Networks[0].Timer!.Kind);
        Equal("delay", retentiveLoaded.Program.Networks[1].TimerReset!.Variable);
        editor.ReplaceFromProgram(retentiveLoaded.Program);
        True(editor.Rungs[0].IsTimer);
        True(editor.Rungs[1].IsTimerReset);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestInvalidTimer()
    {
        var program = TimerProgram(TimeSpan.FromMilliseconds(100));
        var variables = program.Variables.Select(item => item.Name == "delay"
            ? item with { Type = PlcVariableType.Bool }
            : item).ToArray();
        HasIssue(program with { Variables = variables }, "VC002");
        var invalidNetwork = program.Networks[0] with
        {
            Timer = program.Networks[0].Timer! with { Preset = TimeSpan.Zero },
        };
        HasIssue(program with { Networks = [invalidNetwork] }, "VC007");
    }

    private static void TestInvalidTimerReset()
    {
        var source = RetentiveTimerProgram();
        var invalid = source with
        {
            Networks =
            [
                source.Networks[0],
                source.Networks[1] with { TimerReset = new LadderTimerReset("bad-reset", "output") },
            ],
        };
        HasIssue(invalid, "VC002");
    }

    private static void TestSharedTimerInstance()
    {
        var source = TimerProgram(TimeSpan.FromMilliseconds(100));
        var duplicate = new LadderNetwork("second-timer-network", "Invalid shared timer",
            Contact("second-timer-contact", "input"),
            Timer: new LadderTimer("second-timer", "delay", TimeSpan.FromMilliseconds(200),
                LadderTimerKind.RetentiveOnDelay));
        HasIssue(source with { Networks = [source.Networks[0], duplicate] }, "VC013");
    }

    private static void TestSetCoil()
    {
        var runtime = Runtime(LatchProgram());
        runtime.Run();
        True(runtime.Scan(Inputs(("set_input", true), ("reset_input", false))).Outputs["latched_output"]);
        True(runtime.Scan(Inputs(("set_input", false), ("reset_input", false))).Outputs["latched_output"]);
    }

    private static void TestResetCoil()
    {
        var runtime = Runtime(LatchProgram());
        runtime.Run();
        runtime.Scan(Inputs(("set_input", true), ("reset_input", false)));
        False(runtime.Scan(Inputs(("set_input", false), ("reset_input", true))).Outputs["latched_output"]);
    }

    private static void TestLatchScanOrder()
    {
        var runtime = Runtime(LatchProgram());
        runtime.Run();
        False(runtime.Scan(Inputs(("set_input", true), ("reset_input", true))).Outputs["latched_output"]);
    }

    private static void TestLatchRoundTrip()
    {
        var original = LatchProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("Saved latch program did not reload.");
        Equal(LadderCoilMode.Set, loaded.Program.Networks[0].Coil!.Mode);
        Equal(LadderCoilMode.Reset, loaded.Program.Networks[1].Coil!.Mode);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        Equal(LadderCoilMode.Set, editor.Rungs[0].CoilMode);
        Equal(LadderCoilMode.Reset, editor.Rungs[1].CoilMode);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestInvalidCoilMode()
    {
        var json = LadderProgramJson.Save(LatchProgram()).Replace("\"mode\": \"set\"", "\"mode\": \"invalid\"", StringComparison.Ordinal);
        var loaded = LadderProgramJson.Load(json);
        if (loaded.IsValid || !loaded.Issues.Any(item => item.Code == "VC100"))
            throw new InvalidOperationException("Expected invalid coil mode issue VC100.");
    }

    private static void TestCounterRisingEdge()
    {
        var runtime = Runtime(CounterProgram(3));
        runtime.Run();
        Equal(1L, runtime.Scan(Inputs(("count_input", true))).Counters["parts"].Accumulated);
        Equal(1L, runtime.Scan(Inputs(("count_input", true))).Counters["parts"].Accumulated);
        Equal(1L, runtime.Scan(Inputs(("count_input", false))).Counters["parts"].Accumulated);
        Equal(2L, runtime.Scan(Inputs(("count_input", true))).Counters["parts"].Accumulated);
    }

    private static void TestCounterDown()
    {
        var runtime = Runtime(CounterDownProgram(3));
        runtime.Run();
        var loaded = runtime.Scan(Inputs(("load_input", true), ("count_input", false), ("reset_input", false)));
        Equal(3L, loaded.Counters["parts"].Accumulated);
        runtime.Scan(Inputs(("load_input", false), ("count_input", false), ("reset_input", false)));
        var first = runtime.Scan(Inputs(("load_input", false), ("count_input", true), ("reset_input", false)));
        Equal(2L, first.Counters["parts"].Accumulated);
        var held = runtime.Scan(Inputs(("load_input", false), ("count_input", true), ("reset_input", false)));
        Equal(2L, held.Counters["parts"].Accumulated);
        runtime.Scan(Inputs(("load_input", false), ("count_input", false), ("reset_input", false)));
        var second = runtime.Scan(Inputs(("load_input", false), ("count_input", true), ("reset_input", false)));
        Equal(1L, second.Counters["parts"].Accumulated);
    }

    private static void TestCounterDownDone()
    {
        var runtime = Runtime(CounterDownProgram(2));
        runtime.Run();
        runtime.Scan(Inputs(("load_input", true), ("count_input", false), ("reset_input", false)));
        runtime.Scan(Inputs(("load_input", false), ("count_input", true), ("reset_input", false)));
        runtime.Scan(Inputs(("load_input", false), ("count_input", false), ("reset_input", false)));
        var zero = runtime.Scan(Inputs(("load_input", false), ("count_input", true), ("reset_input", false)));
        Equal(0L, zero.Counters["parts"].Accumulated);
        True(zero.Counters["parts"].Done);
        runtime.Scan(Inputs(("load_input", false), ("count_input", false), ("reset_input", true)));
        False(runtime.Snapshot.Counters["parts"].Done);
    }

    private static void TestCounterEdgeIsolation()
    {
        var program = CounterDownProgram(3);
        var variables = program.Variables.Append(
            new PlcVariable("count_input_2", PlcVariableType.Bool, PlcVariableRole.Input, false)).ToArray();
        var networks = program.Networks.ToList();
        networks.Insert(2, new LadderNetwork(
            "count-down-network-2", "Count second input",
            Contact("count-down-contact-2", "count_input_2"),
            Counter: new LadderCounter("ctd-2", "parts", 3, LadderCounterKind.CountDown)));
        var runtime = Runtime(program with { Variables = variables, Networks = networks });
        runtime.Run();
        runtime.Scan(Inputs(("load_input", true), ("count_input", false), ("count_input_2", false), ("reset_input", false)));
        var first = runtime.Scan(Inputs(("load_input", false), ("count_input", true), ("count_input_2", false), ("reset_input", false)));
        Equal(2L, first.Counters["parts"].Accumulated);
        var second = runtime.Scan(Inputs(("load_input", false), ("count_input", true), ("count_input_2", true), ("reset_input", false)));
        Equal(1L, second.Counters["parts"].Accumulated);
    }

    private static void TestCounterDownRoundTrip()
    {
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(CounterDownProgram(7)));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("Saved CTD program did not reload.");
        Equal(LadderCounterKind.CountDown, loaded.Program.Networks[1].Counter!.Kind);
        Equal(7L, loaded.Program.Networks[0].CounterLoad!.Preset);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].IsCounterLoad);
        Equal(LadderCounterKind.CountDown, editor.Rungs[1].CounterKind);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestCounterDoneContact()
    {
        var runtime = Runtime(CounterProgram(2));
        runtime.Run();
        False(runtime.Scan(Inputs(("count_input", true))).Outputs["count_done"]);
        runtime.Scan(Inputs(("count_input", false)));
        var done = runtime.Scan(Inputs(("count_input", true)));
        True(done.Counters["parts"].Done);
        True(done.Outputs["count_done"]);
    }

    private static void TestCounterReset()
    {
        var runtime = Runtime(CounterProgram(1));
        runtime.Run();
        runtime.Scan(Inputs(("count_input", true)));
        var reset = runtime.Scan(Inputs(("count_input", false), ("reset_input", true)));
        Equal(0L, reset.Counters["parts"].Accumulated);
        False(reset.Counters["parts"].Done);
        False(reset.Outputs["count_done"]);
    }

    private static void TestCounterLifecycle()
    {
        var runtime = Runtime(CounterProgram(1));
        runtime.Run();
        runtime.Scan(Inputs(("count_input", true)));
        Equal(1L, runtime.Stop().Counters["parts"].Accumulated);
        Equal(0L, runtime.Reset().Counters["parts"].Accumulated);
    }

    private static void TestCounterRoundTrip()
    {
        var original = CounterProgram(12);
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("Saved counter program did not reload.");
        Equal(12L, loaded.Program.Networks[0].Counter!.Preset);
        Equal("parts", loaded.Program.Networks[1].CounterReset!.Variable);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].IsCounter);
        True(editor.Rungs[1].IsCounterReset);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestInvalidCounter()
    {
        var program = CounterProgram(2);
        var variables = program.Variables.Select(item => item.Name == "parts"
            ? item with { Type = PlcVariableType.Bool }
            : item).ToArray();
        HasIssue(program with { Variables = variables }, "VC002");
        var invalid = program.Networks[0] with
        {
            Counter = program.Networks[0].Counter! with { Preset = 0 },
        };
        HasIssue(program with { Networks = [invalid, .. program.Networks.Skip(1)] }, "VC008");
    }

    private static void TestComparisonOperators()
    {
        var cases = new (LadderCompareOperator Operator, string Right, bool Expected)[]
        {
            (LadderCompareOperator.Equal, "10", true),
            (LadderCompareOperator.NotEqual, "9", true),
            (LadderCompareOperator.GreaterThan, "9", true),
            (LadderCompareOperator.GreaterOrEqual, "10", true),
            (LadderCompareOperator.LessThan, "11", true),
            (LadderCompareOperator.LessOrEqual, "10", true),
        };
        foreach (var item in cases)
        {
            var runtime = Runtime(ComparisonProgram(item.Operator, item.Right));
            runtime.Run();
            Equal(item.Expected, runtime.Scan(Inputs()).Outputs["comparison_true"]);
        }
    }

    private static void TestNumericInputSampling()
    {
        var runtime = Runtime(ComparisonProgram(LadderCompareOperator.GreaterThan, "threshold"));
        runtime.Run();
        False(runtime.Scan(Inputs(), NumericInputs(("measured", 9.5))).Outputs["comparison_true"]);
        True(runtime.Scan(Inputs(), NumericInputs(("measured", 10.5))).Outputs["comparison_true"]);
    }

    private static void TestCounterNumericOperand()
    {
        var program = CounterProgram(2);
        var compare = new LadderNetwork("acc-compare", "Accumulated is one",
            new LadderNode("acc-node", LadderNodeKind.Compare, "parts.ACC",
                CompareOperator: LadderCompareOperator.Equal, RightOperand: "1"),
            new LadderCoil("acc-coil", "count_done"));
        program = program with { Networks = [program.Networks[0], compare] };
        var runtime = Runtime(program);
        runtime.Run();
        True(runtime.Scan(Inputs(("count_input", true))).Outputs["count_done"]);
    }

    private static void TestTimerNumericOperand()
    {
        var program = TimerProgram(TimeSpan.FromMilliseconds(40), includeDoneCoil: false);
        var compare = new LadderNetwork("et-compare", "Elapsed check",
            new LadderNode("et-node", LadderNodeKind.Compare, "delay.ET",
                CompareOperator: LadderCompareOperator.GreaterOrEqual, RightOperand: "delay.PT"),
            new LadderCoil("et-coil", "output"));
        program = program with { Networks = [.. program.Networks, compare] };
        var runtime = Runtime(program);
        runtime.Run();
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
    }

    private static void TestComparisonRoundTrip()
    {
        var original = ComparisonProgram(LadderCompareOperator.LessOrEqual, "threshold");
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("Saved comparison program did not reload.");
        var node = loaded.Program.Networks[0].Logic;
        Equal(LadderNodeKind.Compare, node.Kind);
        Equal(LadderCompareOperator.LessOrEqual, node.CompareOperator);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].Branches[0].Contacts[0].IsComparison);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestInvalidComparisonOperand()
    {
        var program = ComparisonProgram(LadderCompareOperator.Equal, "missing_numeric");
        HasIssue(program, "VC001");
    }

    private static void TestMoveInstruction()
    {
        var runtime = Runtime(NumericProgram(LadderNumericOperationKind.Move));
        runtime.Run();
        Equal(0.0, runtime.Scan(Inputs(("enable", false)), NumericInputs(("left", 12.5))).NumericVariables["real_result"]);
        Equal(12.5, runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", 12.5))).NumericVariables["real_result"]);
    }

    private static void TestMathInstructions()
    {
        var cases = new (LadderNumericOperationKind Kind, double Expected)[]
        {
            (LadderNumericOperationKind.Add, 10),
            (LadderNumericOperationKind.Subtract, 6),
            (LadderNumericOperationKind.Multiply, 16),
            (LadderNumericOperationKind.Divide, 4),
            (LadderNumericOperationKind.Modulo, 0),
        };
        foreach (var item in cases)
        {
            var runtime = Runtime(NumericProgram(item.Kind));
            runtime.Run();
            var snapshot = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", 8), ("right", 2)));
            Equal(item.Expected, snapshot.NumericVariables["real_result"]);
        }
    }

    private static void TestUnaryMathInstructions()
    {
        var cases = new (LadderNumericOperationKind Kind, string Source, double Expected)[]
        {
            (LadderNumericOperationKind.Absolute, "-8.25", 8.25),
            (LadderNumericOperationKind.Negate, "8.25", -8.25),
            (LadderNumericOperationKind.SquareRoot, "81", 9),
        };
        foreach (var item in cases)
        {
            var program = NumericProgram(item.Kind, sourceA: item.Source, sourceB: "missing_but_unused");
            True(LadderCompiler.Compile(program).IsValid);
            var runtime = Runtime(program);
            runtime.Run();
            Equal(item.Expected, runtime.Scan(Inputs(("enable", true))).NumericVariables["real_result"]);
        }
    }

    private static void TestScientificMathInstructions()
    {
        var cases = new (LadderNumericOperationKind Kind, string SourceA, string SourceB, double Expected)[]
        {
            (LadderNumericOperationKind.Exponentiate, "2", "8", 256),
            (LadderNumericOperationKind.NaturalLog, "1", "missing_but_unused", 0),
            (LadderNumericOperationKind.Sine, "0", "missing_but_unused", 0),
            (LadderNumericOperationKind.Cosine, "0", "missing_but_unused", 1),
            (LadderNumericOperationKind.Tangent, "0", "missing_but_unused", 0),
            (LadderNumericOperationKind.ArcSine, "0", "missing_but_unused", 0),
            (LadderNumericOperationKind.ArcCosine, "1", "missing_but_unused", 0),
            (LadderNumericOperationKind.ArcTangent, "0", "missing_but_unused", 0),
            (LadderNumericOperationKind.Truncate, "-7.9", "missing_but_unused", -7),
        };
        foreach (var item in cases)
        {
            var program = NumericProgram(item.Kind, sourceA: item.SourceA, sourceB: item.SourceB);
            True(LadderCompiler.Compile(program).IsValid);
            var runtime = Runtime(program);
            runtime.Run();
            Equal(item.Expected, runtime.Scan(Inputs(("enable", true))).NumericVariables["real_result"]);
        }
    }

    private static void TestScalingInstructions()
    {
        var normalize = Runtime(NumericProgram(LadderNumericOperationKind.Normalize,
            sourceA: "10", sourceB: "20", sourceC: "30"));
        normalize.Run();
        Equal(0.5, normalize.Scan(Inputs(("enable", true))).NumericVariables["real_result"]);

        var scale = Runtime(NumericProgram(LadderNumericOperationKind.Scale,
            sourceA: "10", sourceB: "0.5", sourceC: "30"));
        scale.Run();
        Equal(20.0, scale.Scan(Inputs(("enable", true))).NumericVariables["real_result"]);
    }

    private static void TestIntegerDestination()
    {
        var runtime = Runtime(NumericProgram(LadderNumericOperationKind.Divide, destination: "int_result"));
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", 7), ("right", 2)));
        Equal(4.0, snapshot.NumericVariables["int_result"]);
    }

    private static void TestConversionInstructions()
    {
        static double Execute(LadderNumericOperationKind kind, string source, string destination = "real_result")
        {
            var runtime = Runtime(NumericProgram(kind, destination: destination, sourceA: source,
                sourceB: "missing_but_unused"));
            runtime.Run();
            return runtime.Scan(Inputs(("enable", true))).NumericVariables[destination];
        }

        // IEC/TIA conversion and Logix mixed-type destination conversion both use
        // nearest-even rounding for a finite REAL value written to an integer tag.
        Equal(2.0, Execute(LadderNumericOperationKind.Convert, "2.5", "int_result"));
        Equal(4.0, Execute(LadderNumericOperationKind.Convert, "3.5", "int_result"));
        Equal(-2.0, Execute(LadderNumericOperationKind.Convert, "-1.5", "int_result"));

        Equal(2.0, Execute(LadderNumericOperationKind.Round, "2.5"));
        Equal(4.0, Execute(LadderNumericOperationKind.Round, "3.5"));
        Equal(-2.0, Execute(LadderNumericOperationKind.Round, "-1.5"));
        Equal(3.0, Execute(LadderNumericOperationKind.Ceiling, "2.1"));
        Equal(-2.0, Execute(LadderNumericOperationKind.Ceiling, "-2.1"));
        Equal(2.0, Execute(LadderNumericOperationKind.Floor, "2.9"));
        Equal(-3.0, Execute(LadderNumericOperationKind.Floor, "-2.1"));

        // TRUNC remains distinct: it discards the fractional part before the
        // destination type conversion is applied.
        Equal(-3.0, Execute(LadderNumericOperationKind.Truncate, "-3.9", "int_result"));
    }

    private static void TestDIntOutputLifecycle()
    {
        var source = NumericProgram(LadderNumericOperationKind.Move);
        var variables = source.Variables.Concat(
        [
            new PlcVariable("dint_result", PlcVariableType.DInt, PlcVariableRole.Output, 0L),
        ]).ToArray();
        var operation = source.Networks[0].NumericOperation! with { Destination = "dint_result" };
        var program = source with
        {
            Variables = variables,
            Networks = [source.Networks[0] with { NumericOperation = operation }],
        };
        var runtime = Runtime(program);
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", 3_000_000_000.75)));
        Equal((double)int.MaxValue, snapshot.NumericVariables["dint_result"]);
        Equal((double)int.MaxValue, snapshot.NumericOutputs["dint_result"]);
        Equal(0.0, runtime.Stop().NumericOutputs["dint_result"]);
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(program));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(PlcVariableType.DInt, loaded.Program!.Variables.Single(item => item.Name == "dint_result").Type);
    }

    private static void TestDivideByZeroDiagnostic()
    {
        var runtime = Runtime(NumericProgram(LadderNumericOperationKind.Divide));
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", 7), ("right", 0)));
        Equal(0.0, snapshot.NumericVariables["real_result"]);
        True(snapshot.Diagnostics.Any(item => item.StartsWith("VC_RUNTIME_DIV_ZERO", StringComparison.Ordinal)));
    }

    private static void TestLiteralDivideByZero() =>
        HasIssue(NumericProgram(LadderNumericOperationKind.Divide, sourceB: "0"), "VC009");

    private static void TestAdvancedMathDiagnostics()
    {
        var modulo = Runtime(NumericProgram(LadderNumericOperationKind.Modulo));
        modulo.Run();
        var moduloSnapshot = modulo.Scan(Inputs(("enable", true)), NumericInputs(("left", 7), ("right", 0)));
        Equal(0.0, moduloSnapshot.NumericVariables["real_result"]);
        True(moduloSnapshot.Diagnostics.Any(item => item.StartsWith("VC_RUNTIME_MOD_ZERO", StringComparison.Ordinal)));

        var squareRoot = Runtime(NumericProgram(LadderNumericOperationKind.SquareRoot));
        squareRoot.Run();
        var sqrtSnapshot = squareRoot.Scan(Inputs(("enable", true)), NumericInputs(("left", -1)));
        Equal(0.0, sqrtSnapshot.NumericVariables["real_result"]);
        True(sqrtSnapshot.Diagnostics.Any(item => item.StartsWith("VC_RUNTIME_DOMAIN", StringComparison.Ordinal)));
    }

    private static void TestInvalidAdvancedMathLiteral()
    {
        HasIssue(NumericProgram(LadderNumericOperationKind.Modulo, sourceB: "0"), "VC009");
        HasIssue(NumericProgram(LadderNumericOperationKind.SquareRoot, sourceA: "-1"), "VC009");
    }

    private static void TestScientificMathDiagnostics()
    {
        foreach (var item in new[]
                 {
                     (LadderNumericOperationKind.NaturalLog, "0", "0"),
                     (LadderNumericOperationKind.ArcSine, "2", "0"),
                     (LadderNumericOperationKind.ArcCosine, "-2", "0"),
                     (LadderNumericOperationKind.Exponentiate, "-2", "0.5"),
                 })
        {
            var source = NumericProgram(item.Item1);
            var operation = source.Networks[0].NumericOperation! with
            {
                SourceA = "left",
                SourceB = item.Item1 == LadderNumericOperationKind.Exponentiate ? "right" : "missing_but_unused",
            };
            var program = source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };
            var runtime = Runtime(program);
            runtime.Run();
            var snapshot = runtime.Scan(Inputs(("enable", true)),
                NumericInputs(("left", double.Parse(item.Item2, System.Globalization.CultureInfo.InvariantCulture)),
                    ("right", double.Parse(item.Item3, System.Globalization.CultureInfo.InvariantCulture))));
            Equal(0.0, snapshot.NumericVariables["real_result"]);
            True(snapshot.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_DOMAIN", StringComparison.Ordinal)));
        }
    }

    private static void TestInvalidScientificMathLiteral()
    {
        HasIssue(NumericProgram(LadderNumericOperationKind.NaturalLog, sourceA: "0"), "VC009");
        HasIssue(NumericProgram(LadderNumericOperationKind.ArcSine, sourceA: "1.01"), "VC009");
        HasIssue(NumericProgram(LadderNumericOperationKind.ArcCosine, sourceA: "-1.01"), "VC009");
        HasIssue(NumericProgram(LadderNumericOperationKind.Exponentiate, sourceA: "-2", sourceB: "0.5"), "VC009");
        HasIssue(NumericProgram(LadderNumericOperationKind.Exponentiate, sourceA: "0", sourceB: "-1"), "VC009");
    }

    private static void TestScalingDiagnostics()
    {
        HasIssue(NumericProgram(LadderNumericOperationKind.Normalize,
            sourceA: "30", sourceB: "20", sourceC: "10"), "VC009");
        HasIssue(NumericProgram(LadderNumericOperationKind.Scale,
            sourceA: "10", sourceB: "0.5", sourceC: "10"), "VC009");

        foreach (var kind in new[] { LadderNumericOperationKind.Normalize, LadderNumericOperationKind.Scale })
        {
            var runtime = Runtime(NumericProgram(kind, sourceA: "left", sourceB: "right", sourceC: "third"));
            runtime.Run();
            var snapshot = runtime.Scan(Inputs(("enable", true)),
                NumericInputs(("left", 30), ("right", 20), ("third", 10)));
            Equal(0.0, snapshot.NumericVariables["real_result"]);
            True(snapshot.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_RANGE", StringComparison.Ordinal)));
        }
    }

    private static void TestMathInstanceOperands()
    {
        var counter = CounterProgram(2);
        var variables = counter.Variables.Concat(
        [
            new PlcVariable("numeric_result", PlcVariableType.Real, PlcVariableRole.Memory, 0.0),
        ]).ToArray();
        var operation = new LadderNetwork("copy-count", "Copy count", Contact("copy-enable", "count_input"),
            NumericOperation: new LadderNumericOperation("copy", LadderNumericOperationKind.Move,
                "parts.ACC", "0", "numeric_result"));
        var program = counter with { Variables = variables, Networks = [counter.Networks[0], operation] };
        var runtime = Runtime(program);
        runtime.Run();
        Equal(1.0, runtime.Scan(Inputs(("count_input", true))).NumericVariables["numeric_result"]);
    }

    private static void TestNumericOperationRoundTrip()
    {
        var original = NumericProgram(LadderNumericOperationKind.Normalize,
            sourceA: "0", sourceB: "50", sourceC: "100");
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("Saved numeric operation did not reload.");
        Equal(LadderNumericOperationKind.Normalize, loaded.Program.Networks[0].NumericOperation!.Kind);
        Equal("100", loaded.Program.Networks[0].NumericOperation!.SourceC);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].IsNumericOperation);
        Equal("100", editor.Rungs[0].NumericSourceC);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestBlockCallExecution()
    {
        var runtime = Runtime(MultiBlockProgram());
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("call_enable", true), ("sub_input", true)));
        True(snapshot.Variables["shared_command"]);
        True(snapshot.Outputs["final_output"]);
        True(snapshot.Elements["call-sub"].Energized);
        True(snapshot.Elements["sub-coil"].Energized);
    }

    private static void TestBlockCallSkipped()
    {
        var runtime = Runtime(MultiBlockProgram());
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("call_enable", false), ("sub_input", true)));
        False(snapshot.Variables["shared_command"]);
        False(snapshot.Outputs["final_output"]);
        False(snapshot.Elements["call-sub"].Energized);
        False(snapshot.Elements.ContainsKey("sub-coil"));
    }

    private static void TestMissingCallTarget()
    {
        var program = MultiBlockProgram();
        var main = program.Blocks![0];
        var badCall = main.Networks[0] with { Call = new LadderCall("call-sub", "missing") };
        HasIssue(program with { Blocks = [main with { Networks = [badCall, main.Networks[1]] }, program.Blocks[1]] }, "VC010");
    }

    private static void TestDeepCallChainProcess()
    {
        var start = new System.Diagnostics.ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet")
            start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--deep-call-probe");
        using var child = System.Diagnostics.Process.Start(start)!;
        var output = child.StandardOutput.ReadToEndAsync();
        var errors = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(45000))
        {
            child.Kill(entireProcessTree: true);
            throw new Exception("Deep CALL chain exceeded 45-second test budget.");
        }
        var stderr = errors.GetAwaiter().GetResult();
        if (child.ExitCode != 0)
            throw new Exception($"CALL chain child exited {child.ExitCode}: {stderr[..Math.Min(300, stderr.Length)]}");
        True(output.GetAwaiter().GetResult().Contains("DEEP_CALL_CHAIN_PASS", StringComparison.Ordinal));
    }

    private static void TestDeepCallChain()
    {
        const int count = 20000;
        var blocks = Enumerable.Range(0, count).Select(index => new LadderBlock(
            $"deep-{index}", $"Block {index}", [new LadderNetwork($"network-{index}", "Call chain",
                Contact($"contact-{index}", "enabled"),
                Call: index + 1 < count ? new LadderCall($"call-{index}", $"deep-{index + 1}") : null,
                Return: index + 1 == count ? new LadderReturn($"return-{index}") : null)],
            index == 0 ? LadderBlockType.OrganizationBlock : LadderBlockType.Function)).ToArray();
        var program = new LadderProgram(1, "deep", "Deep chain", "LD", TimeSpan.FromMilliseconds(20),
            [new PlcVariable("enabled", PlcVariableType.Bool, PlcVariableRole.Memory, true)], [], blocks, blocks[0].Id);
        True(LadderCompiler.Compile(program).IsValid);
        // A back edge at the far end must still be rejected, without recursion.
        var last = blocks[^1];
        blocks[^1] = last with { Networks = [new LadderNetwork("back-network", "Back edge",
            Contact("back-contact", "enabled"), Call: new LadderCall("back-call", blocks[1].Id))] };
        HasIssue(program, "VC011");
        Console.WriteLine("DEEP_CALL_CHAIN_PASS");
    }

    private static void TestRecursiveCallRejected()
    {
        var program = MultiBlockProgram();
        var sub = program.Blocks![1];
        var recursive = new LadderNetwork("recursive-network", "Call main",
            Contact("recursive-contact", "call_enable"), Call: new LadderCall("recursive-call", "main"));
        HasIssue(program with { Blocks = [program.Blocks[0], sub with { Networks = [.. sub.Networks, recursive] }] }, "VC011");
    }

    private static void TestMultiBlockRoundTrip()
    {
        var original = MultiBlockProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program?.Blocks is not { Count: 2 })
            throw new InvalidOperationException("Saved multi-block program did not reload.");
        Equal("main", loaded.Program.EntryBlock);
        Equal("sub", loaded.Program.Blocks[0].Networks[0].Call!.TargetBlock);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        Equal(2, editor.Blocks.Count);
        True(editor.Blocks[0].Rungs[0].IsCall);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestBlockTypesAndInterfaces()
    {
        var editor = new LadderEditorDocument();
        editor.AddBlock("OB_Main", LadderBlockType.OrganizationBlock);
        var fb = editor.AddBlock("FB_Motor", LadderBlockType.FunctionBlock);
        editor.AddInterfaceParameter(fb.Id, "Start", PlcVariableType.Bool, LadderInterfaceSection.Input);
        editor.AddInterfaceParameter(fb.Id, "Running", PlcVariableType.Bool, LadderInterfaceSection.Output);
        var db = editor.AddBlock("DB_Motor", LadderBlockType.DataBlock);
        editor.AddInterfaceParameter(db.Id, "Speed", PlcVariableType.Real, LadderInterfaceSection.Static, 12.5);
        var loaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        if (!loaded.IsReadable || loaded.Document is null)
            throw new InvalidOperationException("TIA block project did not reload.");
        Equal(LadderBlockType.OrganizationBlock, loaded.Document.Blocks[0].BlockType);
        Equal(LadderBlockType.FunctionBlock, loaded.Document.Blocks[1].BlockType);
        Equal(LadderInterfaceSection.Input, loaded.Document.Blocks[1].Interface[0].Section);
        Equal("Running", loaded.Document.Blocks[1].Interface[1].Name);
        Equal(LadderBlockType.DataBlock, loaded.Document.Blocks[2].BlockType);
        Equal(12.5, Convert.ToDouble(loaded.Document.Blocks[2].Interface[0].InitialValue));
        Equal(LadderBlockType.FunctionBlock, loaded.Document.BuildProgram().Blocks![1].BlockType);
        True(LadderCompiler.Compile(loaded.Document.BuildProgram()).IsValid);
    }

    private static void TestContinuousTaskSchedule()
    {
        var runtime = Runtime(TaskProgram());
        runtime.Run();
        var first = runtime.Scan(Inputs(("command", true)));
        var second = runtime.Scan(Inputs(("command", true)));
        Equal(1L, first.Tasks["continuous"].ExecutionCount);
        Equal(2L, second.Tasks["continuous"].ExecutionCount);
        True(second.Tasks["continuous"].Due);
    }

    private static void TestPeriodicTaskSchedule()
    {
        var runtime = Runtime(TaskProgram());
        runtime.Run();
        VirtualControllerSnapshot snapshot = runtime.Snapshot;
        for (var scan = 1; scan <= 4; scan++)
        {
            snapshot = runtime.Scan(Inputs(("command", true)));
            Equal(0L, snapshot.Tasks["periodic"].ExecutionCount);
            False(snapshot.Tasks["periodic"].Due);
        }
        snapshot = runtime.Scan(Inputs(("command", true)));
        Equal(1L, snapshot.Tasks["periodic"].ExecutionCount);
        True(snapshot.Tasks["periodic"].Due);
    }

    private static void TestTaskPriorityOrder()
    {
        var program = TaskProgram();
        var bothContinuous = program.Tasks!.Select(task => task with { Kind = LadderTaskKind.Continuous }).ToArray();
        var runtime = Runtime(program with { Tasks = bothContinuous });
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("command", true)));
        False(snapshot.Outputs["scheduled_output"]);
        var reversed = bothContinuous.Select(task => task.Id == "continuous"
            ? task with { Priority = 20 }
            : task with { Priority = 5 }).ToArray();
        runtime = Runtime(program with { Tasks = reversed });
        runtime.Run();
        True(runtime.Scan(Inputs(("command", true))).Outputs["scheduled_output"]);
    }

    private static void TestInvalidTask()
    {
        var program = TaskProgram();
        var invalidPeriod = program.Tasks![1] with { Period = TimeSpan.FromMilliseconds(30) };
        HasIssue(program with { Tasks = [program.Tasks[0], invalidPeriod] }, "VC012");
        var invalidTarget = program.Tasks[0] with { EntryBlock = "missing" };
        HasIssue(program with { Tasks = [invalidTarget, program.Tasks[1]] }, "VC012");
    }

    private static void TestTaskRoundTrip()
    {
        var original = TaskProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program?.Tasks is not { Count: 2 })
            throw new InvalidOperationException("Saved task configuration did not reload.");
        Equal(LadderTaskKind.Periodic, loaded.Program.Tasks[1].Kind);
        Equal(TimeSpan.FromMilliseconds(100), loaded.Program.Tasks[1].Period);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        Equal(2, editor.Tasks.Count);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestBlockLifecycle()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        var block = editor.AddBlock("Sequence");
        var entryBlock = editor.EntryBlockId;
        var taskEntry = editor.Tasks[0].EntryBlock;
        editor.SelectBlock(1);
        editor.AddRung("Sequence output", "conveyor_running");
        editor.AddContact(0, 0, "start_command", false);
        editor.SelectBlock(0);
        editor.AddCallRung("Run sequence", block.Id);
        editor.AddContact(editor.Rungs.Count - 1, 0, "start_command", false);

        history.Execute(editor, "Rename routine", () => editor.RenameBlock(block.Id, "MotionSequence"));
        Equal("MotionSequence", editor.Blocks[1].Name);
        Equal(block.Id, editor.Rungs[^1].CallTarget);
        Equal(entryBlock, editor.EntryBlockId);
        Equal(taskEntry, editor.Tasks[0].EntryBlock);
        True(history.Undo(editor, out var description));
        Equal("Rename routine", description);
        Equal("Sequence", editor.Blocks[1].Name);
        True(history.Redo(editor, out _));
        Equal("MotionSequence", editor.Blocks[1].Name);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestBlockDeletionProtection()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var main = editor.Blocks[0];
        var sub = editor.AddBlock("Unassigned");
        False(editor.TryRemoveBlock(main.Id, out var entryReason));
        True(entryReason.Contains("entry block", StringComparison.Ordinal));

        editor.SelectBlock(0);
        editor.AddCallRung("Call unassigned", sub.Id);
        False(editor.TryRemoveBlock(sub.Id, out var callReason));
        True(callReason.Contains("CALL/JSR", StringComparison.Ordinal));
        editor.RemoveRung(editor.Rungs.Count - 1);

        var task = editor.AddTask("Periodic", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(100), 20, sub.Id);
        False(editor.TryRemoveBlock(sub.Id, out var taskReason));
        True(taskReason.Contains("Reassign", StringComparison.Ordinal));
        True(editor.TryRemoveTask(task.Id, out _));
        True(editor.TryRemoveBlock(sub.Id, out var removalReason));
        Equal(string.Empty, removalReason);
        Equal(1, editor.Blocks.Count);
        False(editor.TryRemoveBlock(main.Id, out var lastReason));
        True(lastReason.Contains("at least one", StringComparison.Ordinal));
    }

    private static void TestTaskLifecycle()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        var task = editor.AddTask("Periodic", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(100), 20, editor.EntryBlockId);

        history.Execute(editor, "Rename task", () => editor.RenameTask(task.Id, "FastPeriodic"));
        Equal("FastPeriodic", editor.Tasks[1].Name);
        Equal(task.Id, editor.Tasks[1].Id);
        True(history.Undo(editor, out _));
        Equal("Periodic", editor.Tasks[1].Name);
        True(history.Redo(editor, out _));
        Equal("FastPeriodic", editor.Tasks[1].Name);

        history.Execute(editor, "Delete task", () => True(editor.TryRemoveTask(task.Id, out _)));
        Equal(1, editor.Tasks.Count);
        True(history.Undo(editor, out _));
        Equal(2, editor.Tasks.Count);
        True(editor.TryRemoveTask(task.Id, out _));
        False(editor.TryRemoveTask(editor.Tasks[0].Id, out var reason));
        True(reason.Contains("at least one", StringComparison.Ordinal));
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestEditorUndo()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        var rungId = editor.Rungs[0].Id;
        var contactCount = editor.Rungs[0].Branches[0].Contacts.Count;
        history.Execute(editor, "Add contact", () => editor.AddContact(0, 0, "start_command", false));
        Equal(contactCount + 1, editor.Rungs[0].Branches[0].Contacts.Count);
        True(history.Undo(editor, out var description));
        Equal("Add contact", description);
        Equal(contactCount, editor.Rungs[0].Branches[0].Contacts.Count);
        Equal(rungId, editor.Rungs[0].Id);
        True(history.CanRedo);
    }

    private static void TestEditorRedo()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        history.Execute(editor, "Add routine", () =>
        {
            editor.AddBlock("Sequence");
            editor.SelectBlock(1);
            editor.AddRung("Sequence rung", "conveyor_running");
        });
        var blockId = editor.Blocks[1].Id;
        True(history.Undo(editor, out _));
        Equal(1, editor.Blocks.Count);
        True(history.Redo(editor, out var description));
        Equal("Add routine", description);
        Equal(2, editor.Blocks.Count);
        Equal(blockId, editor.Blocks[1].Id);
        Equal(1, editor.ActiveBlockIndex);
        Equal("Add routine", history.UndoDescription);
    }

    private static void TestEditorRedoInvalidation()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        history.Execute(editor, "First edit", () => editor.Rungs[0].Label = "Changed");
        True(history.Undo(editor, out _));
        True(history.CanRedo);
        history.Execute(editor, "Replacement edit", () => editor.Rungs[0].Label = "Replacement");
        False(history.CanRedo);
        Equal("Replacement edit", history.UndoDescription);
    }

    private static void TestSelectedContactEdit()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        var original = editor.Rungs[0].Branches[0].Contacts[0];
        history.Execute(editor, "Edit selected contact", () =>
            editor.ReplaceContact(0, 0, 0, original with
            {
                Variable = "stop_command",
                NormallyClosed = true,
            }));
        var edited = editor.Rungs[0].Branches[0].Contacts[0];
        Equal(original.Id, edited.Id);
        Equal("stop_command", edited.Variable);
        True(edited.NormallyClosed);
        True(history.Undo(editor, out _));
        Equal(original, editor.Rungs[0].Branches[0].Contacts[0]);
    }

    private static void TestSelectedContactMove()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var contacts = editor.Rungs[0].Branches[0].Contacts;
        var firstId = contacts[0].Id;
        var secondId = contacts[1].Id;
        Equal(1, editor.MoveContact(0, 0, 0, 1));
        Equal(secondId, contacts[0].Id);
        Equal(firstId, contacts[1].Id);
        var program = editor.BuildProgram();
        var series = program.Networks[0].Logic.Children![0];
        Equal(secondId, series.Children![0].Id);
        Equal(firstId, series.Children[1].Id);
        True(LadderCompiler.Compile(program).IsValid);
    }

    private static void TestRungClipboardPaste()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var history = new LadderEditorHistory();
        var template = editor.CaptureRung(0);
        var original = editor.Rungs[0];
        var originalBranchIds = original.Branches.Select(branch => branch.Id).ToHashSet(StringComparer.Ordinal);
        var originalContactIds = original.Branches.SelectMany(branch => branch.Contacts)
            .Select(contact => contact.Id).ToHashSet(StringComparer.Ordinal);
        EditableRung? pasted = null;
        history.Execute(editor, "Paste rung", () => pasted = editor.PasteRung(template, 1));
        True(pasted is not null);
        Equal(3, editor.Rungs.Count);
        Equal(original.Label, pasted!.Label);
        Equal(original.CoilVariable, pasted.CoilVariable);
        Equal(original.Branches.Count, pasted.Branches.Count);
        False(original.Id.Equals(pasted.Id, StringComparison.Ordinal));
        True(pasted.Branches.All(branch => !originalBranchIds.Contains(branch.Id)));
        True(pasted.Branches.SelectMany(branch => branch.Contacts)
            .All(contact => !originalContactIds.Contains(contact.Id)));
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
        True(history.Undo(editor, out var description));
        Equal("Paste rung", description);
        Equal(2, editor.Rungs.Count);
    }

    private static void TestInstructionClipboardPaste()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var source = editor.Rungs[0].Branches[0].Contacts[1];
        var inserted = editor.PasteContact(source, 1, 0, 0);
        Equal(source.Variable, inserted.Variable);
        Equal(source.NormallyClosed, inserted.NormallyClosed);
        Equal(source.EdgeMode, inserted.EdgeMode);
        False(source.Id.Equals(inserted.Id, StringComparison.Ordinal));
        Equal(inserted.Id, editor.Rungs[1].Branches[0].Contacts[0].Id);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestParallelBranchRemoval()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        Equal(2, editor.Rungs[0].Branches.Count);
        var retainedBranchId = editor.Rungs[0].Branches[0].Id;
        editor.RemoveParallelBranch(0, 1);
        Equal(1, editor.Rungs[0].Branches.Count);
        Equal(retainedBranchId, editor.Rungs[0].Branches[0].Id);
        var rejected = false;
        try
        {
            editor.RemoveParallelBranch(0, 0);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        True(rejected);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestTagRename()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var references = editor.CountTagReferences("start_command");
        True(references > 0);
        var updated = editor.UpdateTag(
            "start_command",
            "cycle_start",
            PlcVariableType.Bool,
            PlcVariableRole.Input,
            "operator.start");
        Equal("cycle_start", updated.Name);
        Equal(0, editor.CountTagReferences("start_command"));
        Equal(references, editor.CountTagReferences("cycle_start"));
        True(editor.Rungs.SelectMany(rung => rung.Branches)
            .SelectMany(branch => branch.Contacts)
            .Any(contact => contact.Variable == "cycle_start"));
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestTagDeletion()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        False(editor.TryRemoveTag("seal_in", out var referenced));
        True(referenced > 0);
        editor.AddTag("unused_spare", PlcVariableRole.Memory);
        True(editor.TryRemoveTag("unused_spare", out var unusedReferences));
        Equal(0, unusedReferences);
        False(editor.Tags.Any(tag => tag.Name == "unused_spare"));
    }

    private static void TestInputForce()
    {
        var runtime = Runtime(SimpleProgram(Contact("input-contact", "input")));
        runtime.Run();
        runtime.SetBoolForce("input", true);
        var snapshot = runtime.Scan(Inputs(("input", false)));
        True(snapshot.Variables["input"]);
        True(snapshot.Outputs["output"]);
        True(snapshot.Forces["input"].Value);
        Equal(PlcVariableRole.Input, snapshot.Forces["input"].Role);
    }

    private static void TestInputForceRelease()
    {
        foreach (var clearAll in new[] { false, true })
        {
            var runtime = Runtime(SimpleProgram(Contact("input-contact", "input")));
            // Reproduces a stopped operator command: no plant sample owns it.
            runtime.SetBoolForce("input", true);
            var released = clearAll ? runtime.ClearForces() : runtime.RemoveForce("input");
            Equal(0, released.Forces.Count);
            False(released.Variables["input"]);
            runtime.Run();
            False(runtime.Scan(Inputs()).Outputs["output"]);
            False(runtime.Scan(Inputs()).Variables["input"]);
        }
    }

    private static void TestInputForceRawUpdates()
    {
        var runtime = Runtime(SimpleProgram(Contact("input-contact", "input")));
        runtime.Run();
        runtime.SetBoolForce("input", false);
        False(runtime.Scan(Inputs(("input", true))).Variables["input"]);
        // A held TRUE raw input must survive removing a FALSE force.
        True(runtime.RemoveForce("input").Variables["input"]);
        True(runtime.Scan(Inputs()).Outputs["output"]);
        runtime.SetBoolForce("input", true);
        True(runtime.Scan(Inputs(("input", false))).Variables["input"]);
        False(runtime.ClearForces().Variables["input"]);
        runtime.Reset();
        runtime.SetBoolForce("input", true);
        False(runtime.ClearForces().Variables["input"]);

        var session = new VirtualControllerSession(runtime);
        session.Run();
        session.SetBoolForce("input", false);
        session.SetInput("input", true);
        session.Advance(.020, () => Inputs(), _ => { }, _ => { });
        True(session.RemoveForce("input").Variables["input"]);
        session.SetInput("input", false);
        session.SetBoolForce("input", false);
        session.PulseInput("input");
        session.Advance(.020, () => Inputs(), _ => { }, _ => { });
        session.Advance(.020, () => Inputs(), _ => { }, _ => { });
        False(session.ClearForces().Variables["input"]);
        session.Advance(.020, () => Inputs(), _ => { }, _ => { });
        False(session.Snapshot.Outputs["output"]);
    }

    private static void TestOutputForce()
    {
        var runtime = Runtime(SimpleProgram(Contact("input-contact", "input")));
        runtime.Run();
        runtime.SetBoolForce("output", false);
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        runtime.RemoveForce("output");
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
    }

    private static void TestForceLifecycle()
    {
        var runtime = Runtime(SimpleProgram(Contact("input-contact", "input")));
        runtime.Run();
        runtime.SetBoolForce("output", true);
        True(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        var stopped = runtime.Stop();
        False(stopped.Outputs["output"]);
        True(stopped.Forces.ContainsKey("output"));
        runtime.Run();
        True(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        var reset = runtime.Reset();
        False(reset.Outputs["output"]);
        Equal(0, reset.Forces.Count);
    }

    private static void TestInvalidForceTarget()
    {
        var runtime = DemoRuntime();
        var rejectedMemory = false;
        try
        {
            runtime.SetBoolForce("seal_in", true);
        }
        catch (InvalidOperationException)
        {
            rejectedMemory = true;
        }
        True(rejectedMemory);

        var numericRuntime = Runtime(NumericProgram(LadderNumericOperationKind.Move));
        var rejectedNumeric = false;
        try
        {
            numericRuntime.SetBoolForce("left", true);
        }
        catch (InvalidOperationException)
        {
            rejectedNumeric = true;
        }
        True(rejectedNumeric);
    }

    private static void TestForceSessionPublication()
    {
        var session = new VirtualControllerSession(Runtime(SimpleProgram(Contact("input-contact", "input"))));
        var publications = 0;
        session.SnapshotPublished += _ => publications++;
        session.SetBoolForce("input", true);
        session.RemoveForce("input");
        session.SetBoolForce("output", false);
        var cleared = session.ClearForces();
        Equal(4, publications);
        Equal(0, cleared.Forces.Count);
    }

    private static void TestTagCrossReference()
    {
        var index = LadderProjectIndex.Build(LadderEditorDocument.CreateConveyorExample().BuildProgram());
        var references = index.CrossReference("seal_in");
        True(references.Any(item => item.Kind == LadderReferenceKind.Declaration));
        True(references.Any(item => item.Kind == LadderReferenceKind.Write && item.Detail.Contains("coil", StringComparison.OrdinalIgnoreCase)));
        True(references.Count(item => item.Kind == LadderReferenceKind.Read) >= 2);
        True(references.Where(item => item.HasNetwork).All(item => item.BlockId.Length > 0 && item.NetworkId.Length > 0));
    }

    private static void TestMemberCrossReference()
    {
        var index = LadderProjectIndex.Build(CounterProgram(preset: 2));
        var references = index.CrossReference("parts");
        True(references.Any(item => item.Kind == LadderReferenceKind.Write && item.Symbol == "parts"));
        True(references.Any(item => item.Kind == LadderReferenceKind.Read && item.Symbol == "parts.Q"));
        True(references.All(item => item.RootSymbol == "parts"));

        var timerReferences = LadderProjectIndex.Build(RetentiveTimerProgram()).CrossReference("delay");
        True(timerReferences.Any(item => item.Kind == LadderReferenceKind.Write
            && item.Detail.Contains("RetentiveOnDelay", StringComparison.Ordinal)));
        True(timerReferences.Any(item => item.Kind == LadderReferenceKind.Write
            && item.Detail.Contains("Timer reset", StringComparison.Ordinal)));
        True(timerReferences.All(item => item.RootSymbol == "delay"));
    }

    private static void TestBlockCrossReference()
    {
        var program = MultiBlockProgram();
        var tasks = new[]
        {
            new LadderTask("main-task", "MainTask", LadderTaskKind.Continuous, program.ScanPeriod, 10, "main"),
            new LadderTask("sub-task", "SubTask", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(100), 20, "sub"),
        };
        var index = LadderProjectIndex.Build(program with { Tasks = tasks });
        var references = index.CrossReference("sub");
        True(references.Any(item => item.Kind == LadderReferenceKind.Call && item.Detail.Contains("Subroutine", StringComparison.Ordinal)));
        True(references.Any(item => item.Kind == LadderReferenceKind.ScheduledEntry && item.ElementId == "sub-task"));
    }

    private static void TestJumpLabelCrossReference()
    {
        var index = LadderProjectIndex.Build(JumpProgram());
        var references = index.CrossReference("after");
        Equal(2, references.Count);
        True(references.All(item => item.Kind == LadderReferenceKind.ProgramControl));
        True(references.Any(item => item.Detail.Contains("JMP target", StringComparison.Ordinal)));
        True(references.Any(item => item.Detail.Contains("LBL / LABEL declaration", StringComparison.Ordinal)));
    }

    private static void TestProjectSearch()
    {
        var index = LadderProjectIndex.Build(LadderEditorDocument.CreateConveyorExample().BuildProgram());
        var results = index.Search("PHOTOEYE PERMISSIVE");
        True(results.Any(item => item.NetworkLabel.Contains("photoeye permissive", StringComparison.OrdinalIgnoreCase)));
        var instructionResults = index.Search("xio contact");
        True(instructionResults.Any(item => item.Kind == LadderReferenceKind.Read));
    }

    private static void TestInstructionHelpCatalog()
    {
        var entries = LadderInstructionCatalog.Entries;
        Equal(entries.Count, entries.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count());
        True(entries.Count >= 20);
        True(entries.All(item => item.Key.Length > 0
            && item.Category.Length > 0
            && item.Summary.Length > 0
            && item.Parameters.Length > 0
            && item.Execution.Length > 0
            && item.Restrictions.Length > 0
            && item.Example.Length > 0));
        foreach (var key in new[] { "no", "nc", "edge-rising", "edge-falling", "coil", "set", "reset", "branch", "ton", "tof", "tp", "rto", "timer-reset", "ctu", "counter-reset", "rung", "call", "return", "jump", "label" })
            Equal(key, LadderInstructionCatalog.Get(key).Key);
    }

    private static void TestInstructionHelpOperationCoverage()
    {
        for (var index = 0; index < Enum.GetValues<LadderCompareOperator>().Length; index++)
            True(LadderInstructionCatalog.TryGet($"compare-{index}", out _));
        for (var index = 0; index < Enum.GetValues<LadderNumericOperationKind>().Length; index++)
            True(LadderInstructionCatalog.TryGet($"numeric-{index}", out _));
    }

    private static void TestInstructionHelpVendorTerms()
    {
        var contact = LadderInstructionCatalog.Get("no");
        True(contact.TiaName.Contains("Normally open", StringComparison.Ordinal));
        True(contact.LogixName.Contains("XIC", StringComparison.Ordinal));
        var latch = LadderInstructionCatalog.Get("set");
        True(latch.TiaName.Contains("Set", StringComparison.Ordinal));
        True(latch.LogixName.Contains("OTL", StringComparison.Ordinal));
        var retentiveTimer = LadderInstructionCatalog.Get("rto");
        True(retentiveTimer.TiaName.Contains("TONR", StringComparison.Ordinal));
        True(retentiveTimer.LogixName.Contains("RTO", StringComparison.Ordinal));
        var timerReset = LadderInstructionCatalog.Get("timer-reset");
        True(timerReset.TiaName.Contains("RT", StringComparison.Ordinal));
        True(timerReset.LogixName.Contains("RES", StringComparison.Ordinal));
        var jump = LadderInstructionCatalog.Get("jump");
        True(jump.TiaName.Contains("JMP", StringComparison.Ordinal));
        True(jump.LogixName.Contains("JMP", StringComparison.Ordinal));
        var label = LadderInstructionCatalog.Get("label");
        True(label.TiaName.Contains("LABEL", StringComparison.Ordinal));
        True(label.LogixName.Contains("LBL", StringComparison.Ordinal));
    }

    private static void TestUnknownInstructionHelp()
    {
        False(LadderInstructionCatalog.TryGet("not-an-instruction", out _));
        var rejected = false;
        try
        {
            LadderInstructionCatalog.Get("not-an-instruction");
        }
        catch (KeyNotFoundException)
        {
            rejected = true;
        }
        True(rejected);
    }

    private static void TestReturnExecution()
    {
        var runtime = Runtime(ReturnProgram());
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("call_enable", true), ("return_enable", true)));
        True(snapshot.Outputs["before_return"]);
        False(snapshot.Outputs["after_return"]);
        True(snapshot.Outputs["caller_after"]);
        True(snapshot.Elements["return-sub"].Energized);
    }

    private static void TestReturnSkipped()
    {
        var runtime = Runtime(ReturnProgram());
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("call_enable", true), ("return_enable", false)));
        True(snapshot.Outputs["before_return"]);
        True(snapshot.Outputs["after_return"]);
        True(snapshot.Outputs["caller_after"]);
        False(snapshot.Elements["return-sub"].Energized);
    }

    private static void TestTaskEntryReturn()
    {
        var source = ReturnProgram();
        var returnFirst = new LadderBlock("return-main", "ReturnMain", new LadderNetwork[]
        {
            new("return-main-network", "Exit task", Contact("return-main-contact", "return_enable"),
                Return: new LadderReturn("return-main-instruction")),
            new("skipped-main-network", "Skipped output", Contact("skipped-main-contact", "call_enable"),
                new LadderCoil("skipped-main-coil", "before_return")),
        });
        var other = new LadderBlock("other-main", "OtherMain", new LadderNetwork[]
        {
            new("other-network", "Other task output", Contact("other-contact", "call_enable"),
                new LadderCoil("other-coil", "caller_after")),
        });
        var tasks = new LadderTask[]
        {
            new("return-task", "ReturnTask", LadderTaskKind.Continuous, source.ScanPeriod, 5, returnFirst.Id),
            new("other-task", "OtherTask", LadderTaskKind.Continuous, source.ScanPeriod, 10, other.Id),
        };
        var program = source with { Networks = returnFirst.Networks, Blocks = [returnFirst, other], EntryBlock = returnFirst.Id, Tasks = tasks };
        var runtime = Runtime(program);
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("call_enable", true), ("return_enable", true)));
        False(snapshot.Outputs["before_return"]);
        True(snapshot.Outputs["caller_after"]);
    }

    private static void TestReturnRoundTrip()
    {
        var original = ReturnProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("RETURN program did not reload.");
        True(loaded.Program.Blocks![1].Networks[1].Return is not null);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        editor.SelectBlock(1);
        True(editor.Rungs[1].IsReturn);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestJumpAndLabelExecution()
    {
        var runtime = Runtime(JumpProgram());
        runtime.Run();
        var jumped = runtime.Scan(Inputs(("enable", true), ("jump_enable", true)));
        False(jumped.Outputs["skipped_output"]);
        True(jumped.Outputs["after_output"]);
        True(jumped.Elements["jump-instruction"].Energized);
        True(jumped.Elements["label-instruction"].Energized);

        runtime.Reset();
        runtime.Run();
        var fellThrough = runtime.Scan(Inputs(("enable", true), ("jump_enable", false)));
        True(fellThrough.Outputs["skipped_output"]);
        True(fellThrough.Outputs["after_output"]);
        False(fellThrough.Elements["jump-instruction"].Energized);
    }

    private static void TestJumpLabelValidation()
    {
        var source = JumpProgram();
        var block = source.Blocks![0];
        var missing = block with
        {
            Networks = block.Networks.Select(network => network.Jump is null
                ? network
                : network with { Jump = network.Jump with { TargetLabel = "missing" } }).ToArray(),
        };
        HasIssue(source with { Networks = missing.Networks, Blocks = [missing] }, "VC014");

        var duplicateNetwork = block.Networks[2] with
        {
            Id = "duplicate-label-network",
            LabelInstruction = new LadderLabel("duplicate-label-instruction", "after"),
        };
        var duplicate = block with { Networks = block.Networks.Append(duplicateNetwork).ToArray() };
        HasIssue(source with { Networks = duplicate.Networks, Blocks = [duplicate] }, "VC014");
    }

    private static void TestJumpLabelRoundTrip()
    {
        var original = JumpProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        True(loaded.IsValid && loaded.Program is not null);
        Equal("after", loaded.Program!.Blocks![0].Networks[0].Jump!.TargetLabel);
        Equal("after", loaded.Program.Blocks[0].Networks[2].LabelInstruction!.Name);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].IsJump);
        True(editor.Rungs[2].IsLabel);
        Equal("after", editor.Rungs[0].ProgramControlLabel);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);
    }

    private static void TestJumpLoopWatchdog()
    {
        var variables = new PlcVariable[]
        {
            new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("safe_output", PlcVariableType.Bool, PlcVariableRole.Output, true),
        };
        var networks = new LadderNetwork[]
        {
            new("loop-label-network", "Loop label", Contact("loop-label-contact", "enable"),
                LabelInstruction: new LadderLabel("loop-label", "loop")),
            new("loop-jump-network", "Loop forever", Contact("loop-jump-contact", "enable"),
                Jump: new LadderJump("loop-jump", "loop")),
        };
        var block = new LadderBlock("main", "Main", networks);
        var program = new LadderProgram(1, "jump-loop", "JumpLoop", "LD", TimeSpan.FromMilliseconds(20),
            variables, networks, [block], block.Id,
            [new LadderTask("main-task", "MainTask", LadderTaskKind.Continuous, TimeSpan.FromMilliseconds(20), 10, block.Id)]);
        var runtime = Runtime(program);
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("enable", true)));
        Equal(VirtualControllerState.Stopped, snapshot.State);
        False(snapshot.Outputs["safe_output"]);
        True(snapshot.Diagnostics.Any(message => message.Contains("VC_RUNTIME_JUMP_LIMIT", StringComparison.Ordinal)));
    }

    private static void TestOfflineEngineeringWorkflow()
    {
        // Author and persist the mutable engineering document. This is the
        // same format used by File > Save Project, including watch metadata.
        var authored = LadderEditorDocument.CreateConveyorExample();
        var savedProject = LadderEditorProjectJson.Save(authored);
        var opened = LadderEditorProjectJson.Load(savedProject);
        True(opened.IsReadable && opened.Document is not null);
        Equal(authored.WatchVariables.Count, opened.Document!.WatchVariables.Count);

        // Verify bindings and compile a fresh immutable executable snapshot.
        // Merely opening a project never creates or starts this runtime.
        var program = opened.Document.BuildProgram();
        var scenePoints = new[]
        {
            new SceneIoPoint("simulated_photoeye", "BOOL", "PC", "input", "Plant photoeye"),
            new SceneIoPoint("conveyor_running", "BOOL", "PLC", "output", "Conveyor command"),
        };
        Equal(0, SceneIoBindingValidator.Validate(program, scenePoints).Count);
        var compilation = LadderCompiler.Compile(program);
        True(compilation.IsValid && compilation.Program is not null);

        var executable = compilation.Program!;
        var session = new VirtualControllerSession(new VirtualControllerRuntime(executable));
        var published = new List<VirtualControllerSnapshot>();
        session.SnapshotPublished += published.Add;
        var sceneInputs = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["simulated_photoeye"] = false,
        };
        var sceneOutputs = new Dictionary<string, bool>(StringComparer.Ordinal);
        void Commit(IReadOnlyDictionary<string, bool> outputs)
        {
            foreach (var (point, value) in SceneIoImageMapper.CommitBoolOutputs(program, outputs))
                sceneOutputs[point] = value;
        }

        // Run a real deterministic scan through the symbolic scene-I/O map.
        session.PulseInput("start_command");
        session.Run();
        Equal(1, session.Advance(
            session.ScanPeriod.TotalSeconds,
            () => SceneIoImageMapper.SampleBoolInputs(program, sceneInputs),
            Commit,
            _ => { }));
        True(sceneOutputs["conveyor_running"]);
        True(session.Snapshot.Variables["seal_in"]);
        True(session.Snapshot.Elements.Count > 0);

        // Plant feedback is sampled on the next scan and opens the permissive.
        sceneInputs["simulated_photoeye"] = true;
        session.Advance(
            session.ScanPeriod.TotalSeconds,
            () => SceneIoImageMapper.SampleBoolInputs(program, sceneInputs),
            Commit,
            _ => { });
        False(sceneOutputs["conveyor_running"]);
        False(session.Snapshot.Variables["seal_in"]);

        // Simulator-only output forcing is visible in the monitored snapshot;
        // Stop still commits a safe output and Reset removes the armed force.
        var forced = session.SetBoolForce("conveyor_running", true);
        Commit(forced.Outputs);
        True(sceneOutputs["conveyor_running"]);
        True(forced.Forces.ContainsKey("conveyor_running"));
        var stopped = session.Stop();
        Commit(stopped.Outputs);
        False(sceneOutputs["conveyor_running"]);
        True(stopped.Forces.ContainsKey("conveyor_running"));
        var reset = session.Reset();
        Commit(reset.Outputs);
        False(sceneOutputs["conveyor_running"]);
        Equal(0, reset.Forces.Count);
        True(published.Count >= 6);
    }

    private static LadderProgram JumpProgram()
    {
        var variables = new PlcVariable[]
        {
            new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("jump_enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("skipped_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
            new("after_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var networks = new LadderNetwork[]
        {
            new("jump-network", "Skip middle rung", Contact("jump-contact", "jump_enable"),
                Jump: new LadderJump("jump-instruction", "after")),
            new("skipped-network", "Skipped when jump is true", Contact("skipped-contact", "enable"),
                new LadderCoil("skipped-coil", "skipped_output")),
            new("label-network", "Jump destination", Contact("label-contact", "enable"),
                LabelInstruction: new LadderLabel("label-instruction", "after")),
            new("after-network", "Runs after label", Contact("after-contact", "enable"),
                new LadderCoil("after-coil", "after_output")),
        };
        var block = new LadderBlock("main", "Main", networks);
        return new LadderProgram(1, "jump-test", "JumpTest", "LD", TimeSpan.FromMilliseconds(20),
            variables, networks, [block], block.Id,
            [new LadderTask("main-task", "MainTask", LadderTaskKind.Continuous, TimeSpan.FromMilliseconds(20), 10, block.Id)]);
    }

    private static VirtualControllerRuntime DemoRuntime()
    {
        var root = FindProjectRoot();
        var json = File.ReadAllText(Path.Combine(root, "programs", "demo-conveyor.ld.json"));
        var loaded = LadderProgramJson.Load(json);
        if (!loaded.IsValid) throw new InvalidOperationException(string.Join("; ", loaded.Issues.Select(item => item.Message)));
        return Runtime(loaded.Program!);
    }

    private static LadderProgram SimpleProgram(LadderNode logic, bool secondInput = false)
    {
        var variables = new List<PlcVariable>
        {
            new("input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("output", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        if (secondInput) variables.Insert(1, new("input2", PlcVariableType.Bool, PlcVariableRole.Input, false));
        return new LadderProgram(1, "test", "Test", "LD", TimeSpan.FromMilliseconds(20), variables,
            [new LadderNetwork("network", "Network", logic, new LadderCoil("coil", "output"))]);
    }

    private static LadderProgram TimerProgram(
        TimeSpan preset,
        bool includeDoneCoil = false,
        LadderTimerKind kind = LadderTimerKind.OnDelay)
    {
        var variables = new List<PlcVariable>
        {
            new("input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("delay", PlcVariableType.Timer, PlcVariableRole.Memory, TimeSpan.Zero),
            new("output", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var networks = new List<LadderNetwork>
        {
            new("timer-network", "Delay input", Contact("timer-contact", "input"),
                Timer: new LadderTimer("timer", "delay", preset, kind)),
        };
        if (includeDoneCoil)
            networks.Add(new LadderNetwork("done-network", "Timer done", Contact("done-contact", "delay.Q"),
                new LadderCoil("done-coil", "output")));
        return new LadderProgram(1, "timer-test", "TimerTest", "LD", TimeSpan.FromMilliseconds(20), variables, networks);
    }

    private static LadderProgram RetentiveTimerProgram()
    {
        var variables = new PlcVariable[]
        {
            new("input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("reset", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("delay", PlcVariableType.Timer, PlcVariableRole.Memory, TimeSpan.Zero),
            new("output", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var networks = new LadderNetwork[]
        {
            new("rto-network", "Accumulate enabled time", Contact("rto-contact", "input"),
                Timer: new LadderTimer("rto", "delay", TimeSpan.FromMilliseconds(60),
                    LadderTimerKind.RetentiveOnDelay)),
            new("reset-network", "Reset accumulated time", Contact("reset-contact", "reset"),
                TimerReset: new LadderTimerReset("timer-reset", "delay")),
        };
        return new LadderProgram(1, "retentive-timer-test", "RetentiveTimerTest", "LD",
            TimeSpan.FromMilliseconds(20), variables, networks);
    }

    private static LadderProgram LatchProgram()
    {
        var variables = new PlcVariable[]
        {
            new("set_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("reset_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("latched_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var networks = new LadderNetwork[]
        {
            new("set-network", "Latch output", Contact("set-contact", "set_input"),
                new LadderCoil("set-coil", "latched_output", LadderCoilMode.Set)),
            new("reset-network", "Unlatch output", Contact("reset-contact", "reset_input"),
                new LadderCoil("reset-coil", "latched_output", LadderCoilMode.Reset)),
        };
        return new LadderProgram(1, "latch-test", "LatchTest", "LD", TimeSpan.FromMilliseconds(20), variables, networks);
    }

    private static LadderProgram CounterProgram(long preset)
    {
        var variables = new PlcVariable[]
        {
            new("count_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("reset_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("parts", PlcVariableType.Counter, PlcVariableRole.Memory, 0L),
            new("count_done", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var networks = new LadderNetwork[]
        {
            new("count-network", "Count parts", Contact("count-contact", "count_input"),
                Counter: new LadderCounter("ctu", "parts", preset)),
            new("counter-reset-network", "Reset count", Contact("reset-counter-contact", "reset_input"),
                CounterReset: new LadderCounterReset("counter-reset", "parts")),
            new("done-network", "Count complete", Contact("counter-done-contact", "parts.Q"),
                new LadderCoil("counter-done-coil", "count_done")),
        };
        return new LadderProgram(1, "counter-test", "CounterTest", "LD", TimeSpan.FromMilliseconds(20), variables, networks);
    }

    private static LadderProgram CounterDownProgram(long preset)
    {
        var variables = new PlcVariable[]
        {
            new("load_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("count_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("reset_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("parts", PlcVariableType.Counter, PlcVariableRole.Memory, 0L),
            new("count_done", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var networks = new LadderNetwork[]
        {
            new("counter-load-network", "Load remaining count", Contact("counter-load-contact", "load_input"),
                CounterLoad: new LadderCounterLoad("counter-load", "parts", preset)),
            new("count-down-network", "Count remaining parts", Contact("count-down-contact", "count_input"),
                Counter: new LadderCounter("ctd", "parts", preset, LadderCounterKind.CountDown)),
            new("count-down-done-network", "Count down complete", Contact("count-down-done-contact", "parts.Q"),
                new LadderCoil("count-down-done-coil", "count_done")),
            new("count-down-reset-network", "Reset remaining count", Contact("count-down-reset-contact", "reset_input"),
                CounterReset: new LadderCounterReset("count-down-reset", "parts")),
        };
        return new LadderProgram(
            1, "counter-down-test", "CounterDownTest", "LD",
            TimeSpan.FromMilliseconds(20), variables, networks);
    }

    private static LadderProgram ComparisonProgram(LadderCompareOperator comparison, string right)
    {
        var variables = new PlcVariable[]
        {
            new("measured", PlcVariableType.Real, PlcVariableRole.Input, 10.0),
            new("threshold", PlcVariableType.Real, PlcVariableRole.Memory, 10.0),
            new("comparison_true", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var logic = new LadderNode("compare", LadderNodeKind.Compare, "measured",
            CompareOperator: comparison, RightOperand: right);
        return new LadderProgram(1, "compare-test", "CompareTest", "LD", TimeSpan.FromMilliseconds(20), variables,
            [new LadderNetwork("compare-network", "Compare values", logic, new LadderCoil("compare-coil", "comparison_true"))]);
    }

    private static LadderProgram NumericProgram(
        LadderNumericOperationKind kind,
        string destination = "real_result",
        string sourceA = "left",
        string sourceB = "right",
        string sourceC = "0")
    {
        var variables = new PlcVariable[]
        {
            new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("left", PlcVariableType.Real, PlcVariableRole.Input, 8.0),
            new("right", PlcVariableType.Real, PlcVariableRole.Input, 2.0),
            new("third", PlcVariableType.Real, PlcVariableRole.Input, 30.0),
            new("int_result", PlcVariableType.Int, PlcVariableRole.Memory, 0L),
            new("real_result", PlcVariableType.Real, PlcVariableRole.Output, 0.0),
        };
        var operation = new LadderNumericOperation("numeric-operation", kind, sourceA, sourceB, destination, sourceC);
        return new LadderProgram(1, "numeric-test", "NumericTest", "LD", TimeSpan.FromMilliseconds(20), variables,
            [new LadderNetwork("numeric-network", "Numeric operation", Contact("enable-contact", "enable"), NumericOperation: operation)]);
    }

    private static LadderProgram ReturnProgram()
    {
        var variables = new PlcVariable[]
        {
            new("call_enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("return_enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("before_return", PlcVariableType.Bool, PlcVariableRole.Output, false),
            new("after_return", PlcVariableType.Bool, PlcVariableRole.Output, false),
            new("caller_after", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var mainNetworks = new LadderNetwork[]
        {
            new("call-return-network", "Call return routine", Contact("call-return-contact", "call_enable"),
                Call: new LadderCall("call-return", "sub-return")),
            new("caller-after-network", "Caller resumes", Contact("caller-after-contact", "call_enable"),
                new LadderCoil("caller-after-coil", "caller_after")),
        };
        var subNetworks = new LadderNetwork[]
        {
            new("before-return-network", "Write before return", Contact("before-return-contact", "call_enable"),
                new LadderCoil("before-return-coil", "before_return")),
            new("return-network", "Conditional return", Contact("return-contact", "return_enable"),
                Return: new LadderReturn("return-sub")),
            new("after-return-network", "Write after return", Contact("after-return-contact", "call_enable"),
                new LadderCoil("after-return-coil", "after_return")),
        };
        var blocks = new LadderBlock[]
        {
            new("main-return", "Main", mainNetworks),
            new("sub-return", "ReturnRoutine", subNetworks),
        };
        return new LadderProgram(1, "return-test", "ReturnTest", "LD", TimeSpan.FromMilliseconds(20),
            variables, mainNetworks, blocks, "main-return",
            [new LadderTask("return-task", "MainTask", LadderTaskKind.Continuous, TimeSpan.FromMilliseconds(20), 10, "main-return")]);
    }

    private static LadderProgram MultiBlockProgram()
    {
        var variables = new PlcVariable[]
        {
            new("call_enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("sub_input", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("shared_command", PlcVariableType.Bool, PlcVariableRole.Memory, false),
            new("final_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var mainNetworks = new LadderNetwork[]
        {
            new("call-network", "Execute subroutine", Contact("call-contact", "call_enable"),
                Call: new LadderCall("call-sub", "sub")),
            new("main-output-network", "Use subroutine result", Contact("shared-contact", "shared_command"),
                new LadderCoil("final-coil", "final_output")),
        };
        var subNetworks = new LadderNetwork[]
        {
            new("sub-network", "Build shared command", Contact("sub-contact", "sub_input"),
                new LadderCoil("sub-coil", "shared_command")),
        };
        var blocks = new LadderBlock[]
        {
            new("main", "Main", mainNetworks),
            new("sub", "Subroutine", subNetworks),
        };
        return new LadderProgram(1, "multi-block", "MultiBlock", "LD", TimeSpan.FromMilliseconds(20),
            variables, mainNetworks, blocks, "main");
    }

    private static LadderProgram TaskProgram()
    {
        var variables = new PlcVariable[]
        {
            new("command", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("scheduled_output", PlcVariableType.Bool, PlcVariableRole.Output, false),
        };
        var continuousNetworks = new LadderNetwork[]
        {
            new("continuous-network", "Continuous writes true", Contact("continuous-contact", "command"),
                new LadderCoil("continuous-coil", "scheduled_output")),
        };
        var periodicNetworks = new LadderNetwork[]
        {
            new("periodic-network", "Periodic writes inverse", Contact("periodic-contact", "command", closed: true),
                new LadderCoil("periodic-coil", "scheduled_output")),
        };
        var blocks = new LadderBlock[]
        {
            new("continuous-block", "ContinuousLogic", continuousNetworks),
            new("periodic-block", "PeriodicLogic", periodicNetworks),
        };
        var tasks = new LadderTask[]
        {
            new("continuous", "MainTask", LadderTaskKind.Continuous, TimeSpan.FromMilliseconds(20), 5, "continuous-block"),
            new("periodic", "Periodic100ms", LadderTaskKind.Periodic, TimeSpan.FromMilliseconds(100), 10, "periodic-block"),
        };
        return new LadderProgram(1, "task-test", "TaskTest", "LD", TimeSpan.FromMilliseconds(20),
            variables, continuousNetworks, blocks, "continuous-block", tasks);
    }

    private static LadderNode Contact(string id, string variable, bool closed = false) =>
        new(id, LadderNodeKind.Contact, variable, closed);

    private static VirtualControllerRuntime Runtime(LadderProgram program)
    {
        var result = LadderCompiler.Compile(program);
        if (!result.IsValid) throw new InvalidOperationException(string.Join("; ", result.Issues.Select(item => item.Message)));
        return new VirtualControllerRuntime(result.Program!);
    }

    private static Dictionary<string, bool> Inputs(params (string Name, bool Value)[] values) =>
        values.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

    private static Dictionary<string, double> NumericInputs(params (string Name, double Value)[] values) =>
        values.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);

    private static void HasIssue(LadderProgram program, string code)
    {
        if (!LadderCompiler.Validate(program).Any(item => item.Code == code))
            throw new InvalidOperationException($"Expected validation issue {code}.");
    }

    private static void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Expected TRUE.");
    }

    private static void False(bool value)
    {
        if (value) throw new InvalidOperationException("Expected FALSE.");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}; actual {actual}.");
    }
}


