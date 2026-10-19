using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using RungProof.Next.App;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static int _passed;
    private static int _failed;
    private static int _selected;
    private static string? _testFilter;

    private static string FindProjectRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "RungProof.Next.csproj"))) return directory.FullName;
        throw new DirectoryNotFoundException("RungProof project root was not found above the test output.");
    }

    private static int Main(string[] args)
    {
        var filterIndex = Array.IndexOf(args, "--filter");
        if (filterIndex >= 0)
        {
            if (filterIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(args[filterIndex + 1]))
            {
                Console.Error.WriteLine("--filter requires a test-name substring.");
                return 2;
            }
            _testFilter = args[filterIndex + 1];
        }
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
        Test("legacy edges without explicit memory suppress startup and restart pulses", TestEdgeLifecycle);
        Test("explicit operator pulse primes legacy edge state without held startup edges", TestFirstScanOperatorPulse);
        Test("edge memory is isolated by stable instruction ID", TestEdgeIsolation);
        Test("edge contacts survive JSON and editor round-trip", TestEdgeRoundTrip);
        Test("TIA P/N edge memory is a persistent writable BOOL operand", TestExplicitEdgeMemory);
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
        Test("TIA block types and declarations persist", TestBlockTypesAndInterfaces);
        Test("editor project loader rejects unreadable structure", TestEditableProjectMalformed);
        Test("editor project rejects null elements at every collection boundary", TestProjectNullEntries);
        Test("scan coordinator rejects nonfinite time without poisoning future scans", TestSessionInvalidTime);
        Test("scan coordinator bounds batches without dropping pending time", TestSessionBudget);
        Test("compiler rejects unsupported sub-millisecond scan periods", TestMinimumScanPeriod);
        Test("new project reset clears all prior engineering state", TestNewProjectReset);
        Test("typed tag initial values parse persist and initialize runtime", TestTagInitialValues);
        Test("S7-1500 PLC tag addresses validate type, role, range, and overlap", TestSiemensPlcTagAddresses);
        Test("watch table persists and follows tag lifecycle", TestWatchTablePersistence);
        Test("watch table validates symbols and participates in undo", TestWatchTableValidationAndUndo);
        Test("named watch tables persist independently and follow tag lifecycle", TestNamedWatchTables);
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
        Test("symbolic TIME tags persist and drive timer presets at scan time", TestSymbolicTimePreset);
        Test("invalid timer instance and preset are rejected", TestInvalidTimer);
        Test("invalid timer reset instance is rejected", TestInvalidTimerReset);
        Test("shared timer instruction instances are rejected", TestSharedTimerInstance);
        Test("set coil latches across false scans", TestSetCoil);
        Test("reset coil clears a latched value", TestResetCoil);
        Test("later reset coil wins in scan order", TestLatchScanOrder);
        Test("latched coil modes survive JSON and editor round-trip", TestLatchRoundTrip);
        Test("unknown coil mode is rejected", TestInvalidCoilMode);
        Test("CTU counts only false-to-true transitions", TestCounterRisingEdge);
        Test("TIA CTU R pin is editable, monitored, and overrides CU while reset is active", TestCounterResetPin);
        Test("counter load and CTD decrement on rising edges", TestCounterDown);
        Test("CTD done becomes true at zero", TestCounterDownDone);
        Test("counter edge memory is isolated per instruction", TestCounterEdgeIsolation);
        Test("CTD and counter load survive JSON and editor round-trip", TestCounterDownRoundTrip);
        Test("CTU done member drives a later coil", TestCounterDoneContact);
        Test("counter reset instruction clears accumulated and done", TestCounterReset);
        Test("Stop preserves counter while Reset clears it", TestCounterLifecycle);
        Test("counter program JSON and editor round-trip", TestCounterRoundTrip);
        Test("invalid counter instance and preset are rejected", TestInvalidCounter);
        Test("TIA counter data-type choices preserve Siemens order and untyped state", TestCounterTypePresentation);
        Test("all numeric comparison operators execute", TestComparisonOperators);
        Test("TIA REAL comparators return false for invalid values", TestComparisonInvalidReal);
        Test("numeric input samples participate in comparisons", TestNumericInputSampling);
        Test("counter ACC and CV are numeric operands", TestCounterNumericOperand);
        Test("timer ET and PT are numeric operands", TestTimerNumericOperand);
        Test("comparison JSON and editor round-trip", TestComparisonRoundTrip);
        Test("invalid comparison operand is rejected", TestInvalidComparisonOperand);
        Test("MOV writes only while rung is true", TestMoveInstruction);
        Test("MOVE rejects implicit numeric type conversions", TestMoveRequiresMatchingType);
        Test("ADD SUB MUL DIV and MOD execute deterministically", TestMathInstructions);
        Test("TIA ADD data type selection persists and drives typed overflow behavior", TestAddDataTypeSelection);
        Test("TIA SUB MUL DIV MOD data type selection drives the supported typed arithmetic subset", TestBinaryArithmeticDataTypeSelection);
        Test("TIA ABS and NEG type selectors validate and execute the supported typed subset", TestUnaryArithmeticDataTypeSelection);
        Test("TIA SQRT type selector and invalid REAL result follow Siemens behavior", TestSquareRootDataTypeSelection);
        Test("TIA EXPT enforces REAL base/result and selectable exponent type", TestExponentiateDataTypeSelection);
        Test("TIA LN and trigonometry use REAL selectors, radians, and invalid-result semantics", TestScientificDataTypeSelection);
        Test("TIA TRUNC keeps a REAL input and independently selected output type", TestTruncateDataTypeSelection);
        Test("TIA CONVERT keeps independent input and output types through compile, execution, and persistence", TestConvertIndependentDataTypes);
        Test("ABS NEG and SQRT execute deterministically", TestUnaryMathInstructions);
        Test("scientific math and TRUNC execute deterministically", TestScientificMathInstructions);
        Test("TIA NORM_X and SCALE_X preserve independent input and output type sides", TestScalingDataTypeSelection);
        Test("NORM_X and SCALE_X execute deterministic three-operand scaling", TestScalingInstructions);
        Test("TIA ROUND CEIL FLOOR use REAL input and independently selected output types", TestRoundingConversionDataTypes);
        Test("numeric conversion instructions execute deterministic vendor-compatible semantics", TestConversionInstructions);
        Test("INT numeric destination rounds nearest-even", TestIntegerDestination);
        Test("DINT output rounds clamps publishes and resets safely", TestDIntOutputLifecycle);
        Test("DIV by zero follows STEP 7 V21 type-specific results", TestDivideByZeroResults);
        Test("literal DIV by zero compiles and executes", TestLiteralDivideByZero);
        Test("MOD zero writes its defined error value and SQRT domain writes invalid REAL", TestAdvancedMathDiagnostics);
        Test("literal MOD zero executes while invalid SQRT is rejected before load", TestInvalidAdvancedMathLiteral);
        Test("scientific unary domain faults write invalid REAL results", TestScientificMathDiagnostics);
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
        Test("tag assignment uses canonical tag-table casing", TestTagTableNameCanonicalization);
        Test("PLC tag and block names are unique across the CPU namespace", TestCpuSymbolUniqueness);
        Test("editor block document identity survives rename, delete, undo, and redo", TestEditorBlockDocumentIdentity);
        Test("compiler rejects case-insensitive tag and block name collisions", TestCompilerCpuSymbolUniqueness);
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
        Test("TIA evidence matrix covers every catalog key and state gate", TestTiaEvidenceMatrixCoverage);
        Test("TIA evidence registry separates local and vendor state for every catalog key", TestTiaEvidenceRegistryCoverage);
        Test("TIA editor-control registry binds exact structured evidence for all 16 non-catalog gates", TestTiaEditorControlEvidenceRegistry);
        Test("TIA local run evidence binds exact local gate coverage", TestTiaLocalRunEvidenceRecord);
        Test("TIA renderer visual review is hash-bound and covers every Siemens-facing key", TestTiaRendererVisualReviewRecord);
        Test("TIA unselected renderer visual review is hash-bound and covers every Siemens-facing key", TestTiaRendererUnselectedVisualReviewRecord);
        Test("instruction help covers every comparison and numeric operation", TestInstructionHelpOperationCoverage);
        Test("instruction help presents distinct TIA and Logix terminology", TestInstructionHelpVendorTerms);
        Test("instruction help rejects unknown keys", TestUnknownInstructionHelp);
        Test("true RETURN exits called block and resumes caller", TestReturnExecution);
        Test("false RETURN continues called block", TestReturnSkipped);
        Test("TIA RET return values drive the caller ENO", TestReturnValueEno);
        Test("TIA RET Value requires a declared BOOL operand", TestReturnValueValidation);
        Test("TIA RET property transactions preserve values through undo and redo", TestReturnValueTransaction);
        Test("RETURN in task entry ends only that due task", TestTaskEntryReturn);
        Test("RETURN JSON and editor round-trip", TestReturnRoundTrip);
        Test("true JMP skips to block-local LBL and false JMP falls through", TestJumpAndLabelExecution);
        Test("JMP LBL validation rejects missing and duplicate labels", TestJumpLabelValidation);
        Test("TIA jump label grammar accepts letters then digits only", TestTiaJumpLabelGrammar);
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
        if (_testFilter is not null && _selected == 0)
        {
            Console.Error.WriteLine($"No tests matched filter '{_testFilter}'.");
            return 2;
        }
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
        False(cell.Tags.Any(tag => tag.Name == "gantry_cycle"));
        var axis = cell.Blocks.Single(block => block.Name == "FB_AxisDrive");
        var gripper = cell.Blocks.Single(block => block.Name == "FB_VacuumGripper");
        Equal(3, cell.Instances.Count(instance => instance.BlockId == axis.Id));
        Equal(1, cell.Instances.Count(instance => instance.BlockId == gripper.Id));
        Equal(4, cell.Instances.Select(instance => instance.InstanceDataBlock!.Number).Distinct().Count());
        True(cell.Instances.All(instance => instance.InstanceDataBlock is { Number: >= 101 and <= 104 }));
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
        var inputs = Inputs(("system_enable", true), ("gantry_home", true), ("pallet_position_valid", true),
            ("carton_at_pick", true), ("start_command", false));
        VirtualControllerSnapshot Scan() => runtime.Scan(inputs,
            NumericInputs(("placed_cartons", 0), ("gantry_x_encoder_counts", 0),
                ("gantry_y_encoder_counts", 0), ("gantry_z_encoder_counts", 0)));
        bool AnyAxis() => new[] { "gantry_x_positive", "gantry_x_negative", "gantry_y_positive",
            "gantry_y_negative", "gantry_z_positive", "gantry_z_negative" }.Any(name => runtime.Snapshot.Outputs[name]);
        Scan(); False(AnyAxis());
        inputs["start_command"] = true; Scan(); True(runtime.Snapshot.Outputs["gantry_y_negative"]);
        inputs["start_command"] = false; inputs["stop_command"] = true; Scan(); False(AnyAxis());
        inputs["stop_command"] = false; Scan(); False(AnyAxis()); // Stop requires a fresh machine Start.
        inputs["start_command"] = true; Scan(); True(runtime.Snapshot.Outputs["gantry_y_negative"]);
        inputs["start_command"] = false; inputs["safety_trip"] = true; Scan(); False(AnyAxis());
        inputs["safety_trip"] = false; Scan(); False(AnyAxis()); // Clearing a trip cannot auto-restart.
        inputs["reset_command"] = true; var reset = Scan(); True(reset.Outputs["palletizer_reset"]);
        Equal(0.0, reset.NumericVariables["sequence_step"]); False(AnyAxis());
    }
    private static void TestPalletizerLayerCount()
    {
        var runtime = PalletizerRuntime();
        var inputs = Inputs(("system_enable", true), ("gantry_home", true), ("pallet_position_valid", true),
            ("carton_at_pick", true));
        var numeric = NumericInputs(("placed_cartons", 0), ("gantry_x_encoder_counts", 0),
            ("gantry_y_encoder_counts", 0), ("gantry_z_encoder_counts", 0));
        VirtualControllerSnapshot Scan() => runtime.Scan(inputs, numeric);
        inputs["start_command"] = true; Scan(); inputs["start_command"] = false;
        for (var pick = 0; pick < 4; pick++)
        {
            inputs["gantry_home"] = false; inputs["at_pickup"] = true;
            numeric["gantry_y_encoder_counts"] = -2050; Scan();
            for (var scan = 0; scan < 38; scan++) Scan();
            True(runtime.Snapshot.Outputs["vacuum_pick"]);
            inputs["carton_attached"] = true; inputs["at_pickup"] = false; Scan();
            numeric["gantry_y_encoder_counts"] = 0; Scan();
            var x = pick % 2 == 0 ? 6900 : 12100;
            var z = pick < 2 ? 1700 : 6300;
            numeric["gantry_x_encoder_counts"] = x; numeric["gantry_z_encoder_counts"] = z; Scan();
            inputs["at_place"] = true; numeric["gantry_y_encoder_counts"] = -8095; Scan();
            inputs["carton_attached"] = false; numeric["placed_cartons"] = pick + 1; Scan();
            inputs["at_place"] = false; numeric["gantry_y_encoder_counts"] = 0; Scan();
            numeric["gantry_x_encoder_counts"] = 0; numeric["gantry_z_encoder_counts"] = 0;
            inputs["gantry_home"] = true; inputs["pick_complete"] = true; Scan();
            Equal((long)pick + 1, runtime.Snapshot.Counters["cycle_count"].Accumulated);
            inputs["pick_complete"] = false; inputs["gantry_home"] = false;
        }
        True(runtime.Snapshot.Outputs["layer_complete"]);
        Equal(1.0, runtime.Snapshot.NumericVariables["layer_count"]);
        Equal(4L, runtime.Snapshot.Counters["cycle_count"].Accumulated);
        False(runtime.Snapshot.Outputs["cell_active"]); False(runtime.Snapshot.Outputs["vacuum_pick"]);
        inputs["reset_command"] = true; var reset = Scan();
        True(reset.Outputs["palletizer_reset"]); Equal(0L, reset.Counters["cycle_count"].Accumulated);
        Equal(0.0, reset.NumericVariables["sequence_step"]);
    }

    private static void Test(string name, Action body)
    {
        if (_testFilter is not null && !name.Contains(_testFilter, StringComparison.OrdinalIgnoreCase)) return;
        _selected++;
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
        True(string.Equals(edge.EdgeMemoryOperand, loadedNode.EdgeMemoryOperand, StringComparison.Ordinal));
        True(edge.EdgeMemoryOperand is not null && editor.CountTagReferences(edge.EdgeMemoryOperand) == 1);
        var editorLoaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(editorLoaded.IsReadable && editorLoaded.Document is not null);
        True(string.Equals(edge.EdgeMemoryOperand, editorLoaded.Document!.Rungs[0].Branches[0].Contacts[0].EdgeMemoryOperand, StringComparison.Ordinal));
        var restored = new LadderEditorDocument();
        restored.ReplaceFromProgram(loaded.Program);
        Equal(LadderEdgeMode.Rising, restored.Rungs[0].Branches[0].Contacts[0].EdgeMode);
        Equal(edge.Id, restored.Rungs[0].Branches[0].Contacts[0].Id);
        True(string.Equals(edge.EdgeMemoryOperand, restored.Rungs[0].Branches[0].Contacts[0].EdgeMemoryOperand, StringComparison.Ordinal));
    }

    private static void TestExplicitEdgeMemory()
    {
        var input = new PlcVariable("input", PlcVariableType.Bool, PlcVariableRole.Input, false);
        var memory = new PlcVariable("edge_memory", PlcVariableType.Bool, PlcVariableRole.Memory, false);
        var output = new PlcVariable("output", PlcVariableType.Bool, PlcVariableRole.Output, false);
        var node = new LadderNode("edge", LadderNodeKind.Contact, "input", EdgeMode: LadderEdgeMode.Rising)
        { EdgeMemoryOperand = "edge_memory" };
        var program = new LadderProgram(1, "explicit-edge", "ExplicitEdge", "LD", TimeSpan.FromMilliseconds(20),
            [input, memory, output], [new LadderNetwork("network", "Network", node, new LadderCoil("coil", "output"))]);
        True(LadderCompiler.Compile(program).IsValid);
        var runtime = Runtime(program);
        runtime.Run();
        False(runtime.Scan(Inputs(("input", false))).Outputs["output"]);
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        True(runtime.Snapshot.Variables["edge_memory"]);
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        True(runtime.Snapshot.Variables["edge_memory"]);
        runtime.Stop();
        runtime.Run();
        False(runtime.Scan(Inputs(("input", true))).Outputs["output"]);
        runtime.Reset();
        runtime.Run();
        True(runtime.Scan(Inputs(("input", true))).Outputs["output"]);

        var firstScanRising = Runtime(program);
        firstScanRising.Run();
        True(firstScanRising.Scan(Inputs(("input", true))).Outputs["output"]);

        var initializedHigh = Runtime(program with
        {
            Variables = [input, memory with { InitialValue = true }, output],
        });
        initializedHigh.Run();
        False(initializedHigh.Scan(Inputs(("input", true))).Outputs["output"]);
        False(initializedHigh.Scan(Inputs(("input", false))).Outputs["output"]);
        True(initializedHigh.Scan(Inputs(("input", true))).Outputs["output"]);

        var falling = Runtime(program with
        {
            Variables = [input, memory with { InitialValue = true }, output],
            Networks = [program.Networks[0] with
            {
                Logic = node with { EdgeMode = LadderEdgeMode.Falling },
            }],
        });
        falling.Run();
        True(falling.Scan(Inputs(("input", false))).Outputs["output"]);
        False(falling.Scan(Inputs(("input", false))).Outputs["output"]);

        var missing = program with { Networks = [program.Networks[0] with { Logic = node with { EdgeMemoryOperand = null } }] };
        var wrongType = program with
        {
            Variables = [input, new("edge_memory", PlcVariableType.DInt, PlcVariableRole.Memory, 0), output],
        };
        var inputOnly = program with { Variables = [input, new("edge_memory", PlcVariableType.Bool, PlcVariableRole.Input, false), output] };
        var duplicate = program with
        {
            Networks =
            [
                program.Networks[0],
                new("network-2", "Network 2", node with { Id = "edge-2" }, new LadderCoil("coil-2", "output")),
            ],
        };
        var addressAlias = program with
        {
            Variables = [input, memory with { Address = "%M0.0" },
                new("edge_memory_alias", PlcVariableType.Bool, PlcVariableRole.Memory, false, Address: "M0.0"), output],
            Networks =
            [
                program.Networks[0],
                new("network-2", "Network 2", node with { Id = "edge-2", EdgeMemoryOperand = "edge_memory_alias" },
                    new LadderCoil("coil-2", "output")),
            ],
        };
        var dbMemory = program with { Variables = [input, memory with { Address = "%DB3.DBX0.2" }, output] };
        var outputAreaMemory = program with { Variables = [input, memory with { Address = "%Q0.2" }, output] };
        True(LadderCompiler.Compile(missing).IsValid); // Legacy imports remain readable; new editor insertion always assigns storage.
        True(LadderCompiler.Validate(wrongType).Any(issue => issue.Path.EndsWith("edgeMemory", StringComparison.Ordinal)));
        True(LadderCompiler.Validate(inputOnly).Any(issue => issue.Code == "VC005"));
        True(LadderCompiler.Validate(duplicate).Any(issue => issue.Code == "VC004" && issue.Path.EndsWith("edgeMemory", StringComparison.Ordinal)));
        True(LadderCompiler.Validate(addressAlias).Any(issue => issue.Code == "VC004" && issue.Message.Contains("address", StringComparison.OrdinalIgnoreCase)));
        True(LadderCompiler.Compile(dbMemory).IsValid);
        True(LadderCompiler.Validate(outputAreaMemory).Any(issue => issue.Code == "VC100" && issue.Path.EndsWith("edgeMemory", StringComparison.Ordinal)));

        var editor = new LadderEditorDocument { Id = "edge-edit", Name = "EdgeEdit" };
        editor.AddTag("sensor", PlcVariableRole.Input);
        editor.AddTag("output", PlcVariableRole.Output, "output");
        editor.AddRung("Edge", "output");
        var edge = editor.InsertEdgeContact(0, 0, 0, "sensor", LadderEdgeMode.Rising);
        var originalMemory = edge.EdgeMemoryOperand!;
        var pasted = editor.PasteContact(edge, 0, 0, 1);
        True(pasted.EdgeMemoryOperand != originalMemory);
        Equal(1, editor.CountTagReferences(originalMemory));
        Equal(1, editor.CountTagReferences(pasted.EdgeMemoryOperand!));
        var renamed = editor.UpdateTag(originalMemory, "renamed_edge_memory", PlcVariableType.Bool, PlcVariableRole.Memory);
        Equal("renamed_edge_memory", renamed.Name);
        True(string.Equals("renamed_edge_memory", editor.Rungs[0].Branches[0].Contacts[0].EdgeMemoryOperand, StringComparison.Ordinal));
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

    private static void TestSiemensPlcTagAddresses()
    {
        static void Valid(string address, PlcVariableType type, PlcVariableRole role)
        {
            True(SiemensPlcTagAddress.TryValidate(address, type, role, [], null, out var error),
                $"Expected valid S7-1500 tag address '{address}' for {type}/{role}; {error}");
        }

        static void Invalid(string address, PlcVariableType type, PlcVariableRole role)
        {
            False(SiemensPlcTagAddress.TryValidate(address, type, role, [], null, out _),
                $"Expected invalid S7-1500 tag address '{address}' for {type}/{role}.");
        }

        Valid("%I0.0", PlcVariableType.Bool, PlcVariableRole.Input);
        Valid("Q32767.7", PlcVariableType.Bool, PlcVariableRole.Output);
        Valid("%M16383.7", PlcVariableType.Bool, PlcVariableRole.Memory);
        Valid("%DB12.DBX0.7", PlcVariableType.Bool, PlcVariableRole.Memory);
        Valid("%IW32766", PlcVariableType.Int, PlcVariableRole.Input);
        Valid("%MW16382", PlcVariableType.Int, PlcVariableRole.Memory);
        Valid("%QD32764", PlcVariableType.DInt, PlcVariableRole.Output);
        Valid("%MD16380", PlcVariableType.Real, PlcVariableRole.Memory);
        Valid("%DB12.DBD16", PlcVariableType.Time, PlcVariableRole.Memory);
        Valid(string.Empty, PlcVariableType.Array, PlcVariableRole.Memory);

        Invalid("%IW1", PlcVariableType.Bool, PlcVariableRole.Input);
        Invalid("%I0.0", PlcVariableType.Bool, PlcVariableRole.Memory);
        Invalid("%I0.8", PlcVariableType.Bool, PlcVariableRole.Input);
        Invalid("%ID32765", PlcVariableType.DInt, PlcVariableRole.Input);
        Invalid("%MW16383", PlcVariableType.Int, PlcVariableRole.Memory);
        Invalid("%DB0.DBX0.0", PlcVariableType.Bool, PlcVariableRole.Memory);
        Invalid("%M0.0", PlcVariableType.String, PlcVariableRole.Memory);

        var occupied = new PlcVariable("existing", PlcVariableType.Int, PlcVariableRole.Memory,
            0L, Address: "MW0");
        False(SiemensPlcTagAddress.TryValidate("%M1.0", PlcVariableType.Bool,
                PlcVariableRole.Memory, [occupied], null, out var overlapError),
            "A BOOL bit must not overlap a declared INT word address.");
        True(overlapError.Contains("overlaps", StringComparison.Ordinal),
            "Overlapping address rejection should identify the conflict.");
        True(SiemensPlcTagAddress.TryValidate("%MW0", PlcVariableType.Int,
                PlcVariableRole.Memory, [occupied], "existing", out _),
            "Editing a declaration must not conflict with its own existing address.");
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

    private static void TestNamedWatchTables()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        editor.WatchVariables.Clear();
        editor.WatchVariables.Add("start_command");
        editor.AddWatchTable("Commissioning");
        editor.WatchVariables.Add("conveyor_running");
        Equal("Commissioning", editor.ActiveWatchTable.Name);
        Equal("conveyor_running", editor.WatchVariables.Single());
        var duplicateRejected = false;
        try
        {
            editor.AddWatchTable("commissioning");
        }
        catch (InvalidOperationException)
        {
            duplicateRejected = true;
        }
        True(duplicateRejected);

        editor.SelectWatchTable(0);
        Equal("start_command", editor.WatchVariables.Single());
        editor.UpdateTag("start_command", "cycle_start", PlcVariableType.Bool, PlcVariableRole.Input, "operator.start");
        Equal("cycle_start", editor.WatchTables[0].Variables.Single());
        editor.SelectWatchTable(1);
        Equal("conveyor_running", editor.WatchVariables.Single());

        var saved = LadderEditorProjectJson.Save(editor);
        var duplicateNameJson = JsonNode.Parse(saved)!.AsObject();
        duplicateNameJson["watchTables"]![1]!["name"] = "WATCH TABLE 1";
        False(LadderEditorProjectJson.Load(duplicateNameJson.ToJsonString()).IsReadable);
        var reopened = LadderEditorProjectJson.Load(saved);
        True(reopened.IsReadable);
        Equal("Watch table 1,Commissioning", string.Join(',', reopened.Document!.WatchTables.Select(table => table.Name)));
        Equal(1, reopened.Document.ActiveWatchTableIndex);
        Equal("cycle_start", reopened.Document.WatchTables[0].Variables.Single());
        Equal("conveyor_running", reopened.Document.WatchTables[1].Variables.Single());
        Equal("cycle_start,conveyor_running", string.Join(',', reopened.Document.BuildProgram().WatchVariables!));
        reopened.Document.SelectWatchTable(0);
        False(LadderEditorProjectJson.HasUnsavedChanges(reopened.Document, saved));
        reopened.Document.SelectWatchTable(1);

        var history = new LadderEditorHistory();
        history.Execute(reopened.Document, "Remove commissioning watch table", () => True(reopened.Document.RemoveActiveWatchTable()));
        Equal(1, reopened.Document.WatchTables.Count);
        True(history.Undo(reopened.Document, out _));
        Equal(2, reopened.Document.WatchTables.Count);
        Equal("Commissioning", reopened.Document.ActiveWatchTable.Name);
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

    private static void TestSymbolicTimePreset()
    {
        True(LadderEditorDocument.TryParseInitialValue(PlcVariableType.Time, "T#100ms", out var initial, out _));
        Equal(TimeSpan.FromMilliseconds(100), (TimeSpan)initial);
        False(LadderEditorDocument.TryParseInitialValue(PlcVariableType.Time, "T#2147483648ms", out _, out _));

        var template = TimerProgram(TimeSpan.FromMilliseconds(100));
        var source = template with
        {
            Variables = [.. template.Variables,
                new("preset", PlcVariableType.Time, PlcVariableRole.Input, TimeSpan.FromMilliseconds(100))],
            Networks = [template.Networks[0] with
            {
                Timer = template.Networks[0].Timer! with { PresetOperand = "preset" },
            }],
        };
        var saved = LadderProgramJson.Save(source);
        var loaded = LadderProgramJson.Load(saved);
        True(loaded.IsValid && loaded.Program is not null);
        Equal(PlcVariableType.Time, loaded.Program!.Variables.Single(item => item.Name == "preset").Type);
        Equal(TimeSpan.FromMilliseconds(100), (TimeSpan)loaded.Program.Variables.Single(item => item.Name == "preset").InitialValue);

        var runtime = Runtime(loaded.Program);
        runtime.Run();
        var first = runtime.Scan(Inputs(("input", true)), NumericInputs(("preset", 60)));
        Equal(TimeSpan.FromMilliseconds(60), first.Timers["delay"].Preset);
        Equal(TimeSpan.FromMilliseconds(20), first.Timers["delay"].Accumulated);
        var second = runtime.Scan(Inputs(("input", true)), NumericInputs(("preset", 40)));
        Equal(TimeSpan.FromMilliseconds(40), second.Timers["delay"].Preset);
        True(second.Timers["delay"].Done);

        var wrongType = loaded.Program with
        {
            Variables = loaded.Program.Variables.Select(item => item.Name == "preset"
                ? item with { Type = PlcVariableType.Real, InitialValue = 100.0 }
                : item).ToArray(),
        };
        HasIssue(wrongType, "VC002");

        var editor = new LadderEditorDocument { Id = "symbolic-time", Name = "SymbolicTime" };
        editor.AddTag("enable", PlcVariableRole.Input);
        editor.AddTag("preset", PlcVariableRole.Input, type: PlcVariableType.Time, initialValue: TimeSpan.FromMilliseconds(100));
        editor.AddTag("delay", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        var rung = editor.AddTimerRung("TON with TIME tag", "delay", TimeSpan.FromMilliseconds(100));
        rung.TimerPresetOperand = "preset";
        editor.AddContact(0, 0, "enable", false);
        var editorJson = LadderEditorProjectJson.Save(editor);
        var restored = LadderEditorProjectJson.Load(editorJson);
        True(restored.IsReadable && restored.Document is not null);
        Equal(TimeSpan.FromMilliseconds(100), (TimeSpan)restored.Document!.Tags.Single(item => item.Name == "preset").InitialValue);
        Equal("preset", restored.Document.Rungs[0].TimerPresetOperand);
        True(LadderCompiler.Compile(restored.Document.BuildProgram()).IsValid);
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
        var unassignedNetwork = program.Networks[0] with
        {
            Timer = program.Networks[0].Timer! with { PresetAssigned = false },
        };
        HasIssue(program with { Networks = [unassignedNetwork] }, "VC007");
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

    private static void TestCounterResetPin()
    {
        var original = CounterProgram(2);
        var networks = original.Networks.Where(network => network.CounterReset is null)
            .Select(network => network.Counter is { } counter
                ? network with { Counter = counter with { ResetInput = "reset_input" } }
                : network).ToArray();
        var program = original with { Networks = networks };
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(program));
        True(loaded.IsValid && loaded.Program is not null);
        Equal("reset_input", loaded.Program!.Networks[0].Counter!.ResetInput);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        Equal("reset_input", editor.Rungs[0].CounterResetInput);
        True(LadderCompiler.Compile(editor.BuildProgram()).IsValid);

        var runtime = Runtime(loaded.Program);
        runtime.Run();
        runtime.Scan(Inputs(("count_input", true), ("reset_input", false)));
        Equal(1L, runtime.Snapshot.Counters["parts"].Accumulated);
        var reset = runtime.Scan(Inputs(("count_input", true), ("reset_input", true)));
        Equal(0L, reset.Counters["parts"].Accumulated);
        False(reset.Counters["parts"].Done);
        var observation = LadderInstructionPresentation.PresentSnapshot("ctu", "ctu", reset);
        True(observation.Rows.Single(row => row.Label == "Consumed R").Value.StartsWith("TRUE", StringComparison.Ordinal));

        runtime.Scan(Inputs(("count_input", true), ("reset_input", false)));
        Equal(0L, runtime.Snapshot.Counters["parts"].Accumulated);
        runtime.Scan(Inputs(("count_input", false), ("reset_input", false)));
        var freshEdge = runtime.Scan(Inputs(("count_input", true), ("reset_input", false)));
        Equal(1L, freshEdge.Counters["parts"].Accumulated);
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
        Equal(LadderCounterKind.CountDown, loaded.Program.Networks[0].Counter!.Kind);
        Equal("load_input", loaded.Program.Networks[0].Counter!.LoadInput);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].IsCounter);
        Equal("load_input", editor.Rungs[0].CounterLoadInput);
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
        var untyped = program.Networks[0] with
        {
            Counter = program.Networks[0].Counter! with { ValueType = LadderCounterValueType.Unspecified },
        };
        HasIssue(program with { Networks = [untyped, .. program.Networks.Skip(1)] }, "VC007");
    }

    private static void TestCounterTypePresentation()
    {
        var types = Enum.GetValues<LadderCounterValueType>();
        Equal("Unspecified,Int,SInt,DInt,USInt,UInt,UDInt,LInt,ULInt", string.Join(',', types));
        Equal("???,Int,SInt,DInt,USInt,UInt,UDInt,LInt,ULInt",
            string.Join(',', types.Select(LadderCounterValueTypePresentation.TiaLabel)));

        var editor = new LadderEditorDocument();
        editor.AddTag("enable", PlcVariableRole.Input);
        editor.AddTag("counter", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        editor.AddCounterRung("Unassigned CTU type", "counter", 3, valueType: LadderCounterValueType.Unspecified);
        editor.AddContact(0, 0, "enable", false);
        var loaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(loaded.IsReadable && loaded.Document is not null);
        Equal(LadderCounterValueType.Unspecified, loaded.Document!.Rungs[0].CounterValueType);
        HasIssue(loaded.Document.BuildProgram(), "VC007");
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

    private static void TestComparisonInvalidReal()
    {
        foreach (var comparison in Enum.GetValues<LadderCompareOperator>()
                     .Where(item => item != LadderCompareOperator.ContainsCode))
        {
            var numeric = NumericProgram(LadderNumericOperationKind.SquareRoot);
            var variables = numeric.Variables.Append(
                new PlcVariable("comparison_true", PlcVariableType.Bool, PlcVariableRole.Output, false)).ToArray();
            var compare = new LadderNetwork(
                $"invalid-real-{comparison}",
                "Invalid REAL comparison returns false",
                new LadderNode($"invalid-real-node-{comparison}", LadderNodeKind.Compare, "real_result",
                    CompareOperator: comparison, RightOperand: "0"),
                new LadderCoil($"invalid-real-coil-{comparison}", "comparison_true"));
            var program = numeric with { Variables = variables, Networks = [.. numeric.Networks, compare] };
            var runtime = Runtime(program);
            runtime.Run();
            var snapshot = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", -1)));
            True(double.IsNaN(snapshot.NumericVariables["real_result"]));
            False(snapshot.Outputs["comparison_true"]);
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

    private static void TestMoveRequiresMatchingType()
    {
        // The existing fixture has a REAL source. Writing it to INT would be
        // an implicit conversion through MOVE, which is outside the simulator's
        // IEC-checked S7-1500 subset; use CONVERT for that operation.
        HasIssue(NumericProgram(LadderNumericOperationKind.Move, destination: "int_result"), "VC002");

        var compatible = NumericProgram(LadderNumericOperationKind.Move,
            destination: "int_result", sourceA: "int_source") with
        {
            Variables = NumericProgram(LadderNumericOperationKind.Move).Variables.Concat(
                [new PlcVariable("int_source", PlcVariableType.Int, PlcVariableRole.Input, 7L)]).ToArray(),
        };
        True(LadderCompiler.Compile(compatible).IsValid);
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

    private static void AssertIntArithmeticOverflow(
        VirtualControllerSnapshot snapshot,
        LadderNumericOperationKind kind,
        string sourceAIdentity,
        double sourceAValue,
        string sourceBIdentity,
        double sourceBValue)
    {
        var diagnosticCode = kind switch
        {
            LadderNumericOperationKind.Add => "VC_RUNTIME_ADD_OVERFLOW",
            LadderNumericOperationKind.Subtract => "VC_RUNTIME_SUB_OVERFLOW",
            LadderNumericOperationKind.Multiply => "VC_RUNTIME_MUL_OVERFLOW",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
                "This assertion covers ADD, SUB, and MUL overflow only."),
        };
        Equal(0.0, snapshot.NumericVariables["int_result"]);
        Equal(1, snapshot.Diagnostics.Count);
        True(snapshot.Diagnostics[0].StartsWith(diagnosticCode, StringComparison.Ordinal));

        var observed = snapshot.Observations!.GlobalLast["numeric-operation"];
        True(observed.Evaluated && observed.NetworkEnable == true && observed.LocalResult);
        Equal("int_result", observed.DestinationIdentity);
        Equal(0.0, Convert.ToDouble(observed.DestinationBefore));
        True(observed.CalculatedResult is null);
        Equal(0.0, Convert.ToDouble(observed.StoredAfter));
        Equal(InstructionWriteDisposition.FailedNoWrite, observed.WriteDisposition);
        False(observed.Rounded || observed.Clamped);
        Equal(1, observed.Diagnostics.Count);
        True(observed.Diagnostics[0].Code.StartsWith(diagnosticCode, StringComparison.Ordinal));

        var sourceA = observed.Operands.Single(operand => operand.Role == "A");
        var sourceB = observed.Operands.Single(operand => operand.Role == "B");
        Equal(sourceAIdentity, sourceA.Identity);
        Equal(sourceAValue, Convert.ToDouble(sourceA.Value));
        Equal(ObservationUnavailableReason.None, sourceA.Reason);
        Equal(sourceBIdentity, sourceB.Identity);
        Equal(sourceBValue, Convert.ToDouble(sourceB.Value));
        Equal(ObservationUnavailableReason.None, sourceB.Reason);
    }

    private static void TestAddDataTypeSelection()
    {
        var source = NumericProgram(LadderNumericOperationKind.Add, destination: "int_result", sourceA: "32767", sourceB: "1");
        var selectedOperation = source.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Int };
        var selected = source with { Networks = [source.Networks[0] with { NumericOperation = selectedOperation }] };
        True(LadderCompiler.Compile(selected).IsValid);
        var runtime = Runtime(selected);
        runtime.Run();
        var overflow = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", 32767), ("right", 1)));
        AssertIntArithmeticOverflow(overflow, LadderNumericOperationKind.Add, "32767", 32767, "1", 1);

        var realProgram = NumericProgram(LadderNumericOperationKind.Add);
        var realOperation = realProgram.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Real };
        var typedReal = realProgram with { Networks = [realProgram.Networks[0] with { NumericOperation = realOperation }] };
        var realRuntime = Runtime(typedReal);
        realRuntime.Run();
        var realResult = realRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", 0.1), ("right", 0.2)));
        Equal((double)((float)0.1 + (float)0.2), realResult.NumericVariables["real_result"]);

        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(selected));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(LadderNumericDataType.Int, loaded.Program!.Networks[0].NumericOperation!.DataType);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        Equal(LadderNumericDataType.Int, editor.Rungs[0].NumericDataType);
        var editorJson = LadderEditorProjectJson.Save(editor);
        var reloadedEditor = LadderEditorProjectJson.Load(editorJson);
        True(reloadedEditor.IsReadable && reloadedEditor.Document is not null);
        Equal(LadderNumericDataType.Int, reloadedEditor.Document!.Rungs[0].NumericDataType);
    }

    private static void TestBinaryArithmeticDataTypeSelection()
    {
        var cases = new (LadderNumericOperationKind Kind, string Left, string Right, double Expected)[]
        {
            (LadderNumericOperationKind.Subtract, "8", "3", 5),
            (LadderNumericOperationKind.Multiply, "7", "6", 42),
            // TIA's integer DIV produces an integer quotient.
            (LadderNumericOperationKind.Divide, "7", "2", 3),
            // MOD is integer-only and returns the remainder.
            (LadderNumericOperationKind.Modulo, "8", "3", 2),
        };

        foreach (var item in cases)
        {
            var source = NumericProgram(item.Kind, destination: "int_result", sourceA: item.Left, sourceB: item.Right);
            var operation = source.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Int };
            var typed = source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };
            True(LadderCompiler.Compile(typed).IsValid);
            var loaded = LadderProgramJson.Load(LadderProgramJson.Save(typed));
            True(loaded.IsValid && loaded.Program is not null);
            Equal(LadderNumericDataType.Int, loaded.Program!.Networks[0].NumericOperation!.DataType);
            var runtime = Runtime(typed);
            runtime.Run();
            var result = runtime.Scan(Inputs(("enable", true))).NumericVariables["int_result"];
            Equal(item.Expected, result);
        }

        foreach (var kind in new[]
        {
            LadderNumericOperationKind.Subtract,
            LadderNumericOperationKind.Multiply,
            LadderNumericOperationKind.Divide,
        })
        {
            var source = NumericProgram(kind, sourceA: "0.1", sourceB: "0.2");
            var operation = source.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Real };
            var typed = source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };
            True(LadderCompiler.Compile(typed).IsValid);
            var runtime = Runtime(typed);
            runtime.Run();
            var snapshot = runtime.Scan(Inputs(("enable", true)));
            var a = (float)0.1;
            var b = (float)0.2;
            var expected = kind switch
            {
                LadderNumericOperationKind.Subtract => (double)(a - b),
                LadderNumericOperationKind.Multiply => (double)(a * b),
                _ => (double)(a / b),
            };
            Equal(expected, snapshot.NumericVariables["real_result"]);
        }

        var overflowSource = NumericProgram(LadderNumericOperationKind.Subtract,
            destination: "int_result", sourceA: "-32768", sourceB: "1");
        var overflowOperation = overflowSource.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Int };
        var overflowProgram = overflowSource with
        {
            Networks = [overflowSource.Networks[0] with { NumericOperation = overflowOperation }],
        };
        var overflowRuntime = Runtime(overflowProgram);
        overflowRuntime.Run();
        var overflow = overflowRuntime.Scan(Inputs(("enable", true)));
        AssertIntArithmeticOverflow(overflow, LadderNumericOperationKind.Subtract,
            "-32768", -32768, "1", 1);

        var multiplyOverflowSource = NumericProgram(LadderNumericOperationKind.Multiply,
            destination: "int_result", sourceA: "32767", sourceB: "2");
        var multiplyOverflowOperation = multiplyOverflowSource.Networks[0].NumericOperation! with
        {
            DataType = LadderNumericDataType.Int,
        };
        var multiplyOverflowProgram = multiplyOverflowSource with
        {
            Networks = [multiplyOverflowSource.Networks[0] with { NumericOperation = multiplyOverflowOperation }],
        };
        var multiplyOverflowRuntime = Runtime(multiplyOverflowProgram);
        multiplyOverflowRuntime.Run();
        var multiplyOverflow = multiplyOverflowRuntime.Scan(Inputs(("enable", true)));
        AssertIntArithmeticOverflow(multiplyOverflow, LadderNumericOperationKind.Multiply,
            "32767", 32767, "2", 2);

        var autoSource = NumericProgram(LadderNumericOperationKind.Multiply,
            destination: "int_result", sourceA: "200000", sourceB: "1000");
        var autoOperation = autoSource.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Auto };
        var autoProgram = autoSource with
        {
            Variables = autoSource.Variables.Select(variable => variable.Name == "int_result"
                ? variable with { Type = PlcVariableType.DInt, InitialValue = 0L }
                : variable).ToArray(),
            Networks = [autoSource.Networks[0] with { NumericOperation = autoOperation }],
        };
        True(LadderCompiler.Compile(autoProgram).IsValid);
        var autoRuntime = Runtime(autoProgram);
        autoRuntime.Run();
        Equal(200_000_000.0, autoRuntime.Scan(Inputs(("enable", true))).NumericVariables["int_result"]);

        var mismatchedTag = NumericProgram(LadderNumericOperationKind.Subtract, destination: "int_result");
        var mismatchedOperation = mismatchedTag.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Int };
        var mismatchedProgram = mismatchedTag with
        {
            Networks = [mismatchedTag.Networks[0] with { NumericOperation = mismatchedOperation }],
        };
        HasIssue(mismatchedProgram, "VC002");

        var moduloChoices = LadderNumericDataTypePresentation.TiaSelectableTypesFor(LadderNumericOperationKind.Modulo);
        True(moduloChoices.SequenceEqual(new[]
        {
            LadderNumericDataType.Unspecified,
            LadderNumericDataType.Int,
            LadderNumericDataType.DInt,
            LadderNumericDataType.Auto,
        }));
        False(LadderNumericOperationRules.SupportsTiaDataType(
            LadderNumericOperationKind.Modulo, LadderNumericDataType.Real));

        var realModulo = NumericProgram(LadderNumericOperationKind.Modulo);
        var realModuloOperation = realModulo.Networks[0].NumericOperation! with
        {
            DataType = LadderNumericDataType.Real,
        };
        HasIssue(realModulo with
        {
            Networks = [realModulo.Networks[0] with { NumericOperation = realModuloOperation }],
        }, "VC002");
        var realAutoModuloOperation = realModulo.Networks[0].NumericOperation! with
        {
            DataType = LadderNumericDataType.Auto,
        };
        HasIssue(realModulo with
        {
            Networks = [realModulo.Networks[0] with { NumericOperation = realAutoModuloOperation }],
        }, "VC002");

        var autoModulo = NumericProgram(LadderNumericOperationKind.Modulo,
            destination: "int_result", sourceA: "8", sourceB: "3");
        var autoModuloOperation = autoModulo.Networks[0].NumericOperation! with
        {
            DataType = LadderNumericDataType.Auto,
        };
        autoModulo = autoModulo with
        {
            Networks = [autoModulo.Networks[0] with { NumericOperation = autoModuloOperation }],
        };
        True(LadderCompiler.Compile(autoModulo).IsValid);
        var autoModuloRuntime = Runtime(autoModulo);
        autoModuloRuntime.Run();
        Equal(2.0, autoModuloRuntime.Scan(Inputs(("enable", true))).NumericVariables["int_result"]);

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

    private static void TestUnaryArithmeticDataTypeSelection()
    {
        static LadderProgram WithDataType(LadderProgram program, LadderNumericDataType dataType)
        {
            var network = program.Networks[0];
            return program with
            {
                Networks = [network with
                {
                    NumericOperation = network.NumericOperation! with { DataType = dataType },
                }],
            };
        }

        var supportedChoices = new[]
        {
            LadderNumericDataType.Unspecified,
            LadderNumericDataType.Int,
            LadderNumericDataType.DInt,
            LadderNumericDataType.Real,
        };
        foreach (var kind in new[] { LadderNumericOperationKind.Absolute, LadderNumericOperationKind.Negate })
        {
            True(LadderNumericOperationRules.HasTiaDataTypeSelector(kind));
            True(LadderNumericDataTypePresentation.TiaSelectableTypesFor(kind).SequenceEqual(supportedChoices));
            False(LadderNumericOperationRules.SupportsTiaDataType(kind, LadderNumericDataType.Auto));

            var selected = WithDataType(
                NumericProgram(kind, destination: "int_result", sourceA: "-8"), LadderNumericDataType.Int);
            True(LadderCompiler.Compile(selected).IsValid);
            var runtime = Runtime(selected);
            runtime.Run();
            var result = runtime.Scan(Inputs(("enable", true))).NumericVariables["int_result"];
            Equal(8.0, result);

            var roundTrip = LadderProgramJson.Load(LadderProgramJson.Save(selected));
            True(roundTrip.IsValid && roundTrip.Program is not null);
            Equal(LadderNumericDataType.Int, roundTrip.Program!.Networks[0].NumericOperation!.DataType);
            var editor = new LadderEditorDocument();
            editor.ReplaceFromProgram(selected);
            var editorRoundTrip = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
            True(editorRoundTrip.IsReadable && editorRoundTrip.Document is not null);
            Equal(LadderNumericDataType.Int, editorRoundTrip.Document!.Rungs[0].NumericDataType);

            var overflowProgram = WithDataType(
                NumericProgram(kind, destination: "int_result", sourceA: "-32768"), LadderNumericDataType.Int);
            var overflowRuntime = Runtime(overflowProgram);
            overflowRuntime.Run();
            var overflow = overflowRuntime.Scan(Inputs(("enable", true)));
            Equal(0.0, overflow.NumericVariables["int_result"]);
            True(overflow.Diagnostics.Any(message => message.StartsWith(
                $"VC_RUNTIME_{(kind == LadderNumericOperationKind.Absolute ? "ABS" : "NEG")}_OVERFLOW",
                StringComparison.Ordinal)));

            var inferredOverflow = NumericProgram(kind, destination: "int_result", sourceA: "-32768");
            True(LadderCompiler.Compile(inferredOverflow).IsValid);
            var inferredRuntime = Runtime(inferredOverflow);
            inferredRuntime.Run();
            var inferred = inferredRuntime.Scan(Inputs(("enable", true)));
            Equal(0.0, inferred.NumericVariables["int_result"]);
            True(inferred.Diagnostics.Any(message => message.StartsWith(
                $"VC_RUNTIME_{(kind == LadderNumericOperationKind.Absolute ? "ABS" : "NEG")}_OVERFLOW",
                StringComparison.Ordinal)));
        }

        var real = WithDataType(
            NumericProgram(LadderNumericOperationKind.Negate, sourceA: "0.25"), LadderNumericDataType.Real);
        True(LadderCompiler.Compile(real).IsValid);
        var realRuntime = Runtime(real);
        realRuntime.Run();
        Equal(-0.25, realRuntime.Scan(Inputs(("enable", true))).NumericVariables["real_result"]);

        foreach (var kind in new[] { LadderNumericOperationKind.Absolute, LadderNumericOperationKind.Negate })
        {
            var dInt = WithDataType(NumericProgram(kind, destination: "dint_result", sourceA: "-8"),
                LadderNumericDataType.DInt);
            True(LadderCompiler.Compile(dInt).IsValid);
            var dIntRuntime = Runtime(dInt);
            dIntRuntime.Run();
            Equal(8.0, dIntRuntime.Scan(Inputs(("enable", true))).NumericVariables["dint_result"]);

            var dIntOverflow = WithDataType(NumericProgram(kind, destination: "dint_result", sourceA: "-2147483648"),
                LadderNumericDataType.DInt);
            var dIntOverflowRuntime = Runtime(dIntOverflow);
            dIntOverflowRuntime.Run();
            var overflow = dIntOverflowRuntime.Scan(Inputs(("enable", true)));
            Equal(0.0, overflow.NumericVariables["dint_result"]);
            True(overflow.Diagnostics.Any(message => message.StartsWith(
                $"VC_RUNTIME_{(kind == LadderNumericOperationKind.Absolute ? "ABS" : "NEG")}_OVERFLOW",
                StringComparison.Ordinal)));
        }

        var mismatched = WithDataType(
            NumericProgram(LadderNumericOperationKind.Absolute, destination: "int_result", sourceA: "left"),
            LadderNumericDataType.Int);
        HasIssue(mismatched, "VC002");
    }

    private static void TestConvertIndependentDataTypes()
    {
        var baseProgram = NumericProgram(LadderNumericOperationKind.Convert,
            destination: "dint_result", sourceA: "42");
        var convert = baseProgram.Networks[0].NumericOperation! with
        {
            InputDataType = LadderNumericDataType.Int,
            OutputDataType = LadderNumericDataType.DInt,
        };
        var typed = baseProgram with
        {
            Networks = [baseProgram.Networks[0] with { NumericOperation = convert }],
        };
        True(LadderCompiler.Compile(typed).IsValid);
        var runtime = Runtime(typed);
        runtime.Run();
        var converted = runtime.Scan(Inputs(("enable", true)));
        Equal(42.0, converted.NumericVariables["dint_result"]);
        var convertedObservation = converted.Observations!.GlobalLast["numeric-operation"];
        True(convertedObservation.Evaluated && convertedObservation.NetworkEnable == true
            && convertedObservation.LocalResult);
        Equal("dint_result", convertedObservation.DestinationIdentity);
        Equal(0.0, Convert.ToDouble(convertedObservation.DestinationBefore));
        Equal(42.0, Convert.ToDouble(convertedObservation.CalculatedResult));
        Equal(42.0, Convert.ToDouble(convertedObservation.StoredAfter));
        Equal(InstructionWriteDisposition.WrittenChanged, convertedObservation.WriteDisposition);
        False(convertedObservation.Rounded || convertedObservation.Clamped);
        Equal(0, convertedObservation.Diagnostics.Count);
        var convertedSource = convertedObservation.Operands.Single(operand => operand.Role == "A");
        Equal("42", convertedSource.Identity);
        Equal(42.0, Convert.ToDouble(convertedSource.Value));
        Equal(ObservationUnavailableReason.None, convertedSource.Reason);

        var programRoundTrip = LadderProgramJson.Load(LadderProgramJson.Save(typed));
        True(programRoundTrip.IsValid && programRoundTrip.Program is not null);
        Equal(LadderNumericDataType.Int, programRoundTrip.Program!.Networks[0].NumericOperation!.InputDataType);
        Equal(LadderNumericDataType.DInt, programRoundTrip.Program.Networks[0].NumericOperation!.OutputDataType);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(programRoundTrip.Program);
        Equal(LadderNumericDataType.Int, editor.Rungs[0].NumericInputDataType);
        Equal(LadderNumericDataType.DInt, editor.Rungs[0].NumericOutputDataType);
        var editorRoundTrip = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(editorRoundTrip.IsReadable && editorRoundTrip.Document is not null);
        Equal(LadderNumericDataType.Int, editorRoundTrip.Document!.Rungs[0].NumericInputDataType);
        Equal(LadderNumericDataType.DInt, editorRoundTrip.Document.Rungs[0].NumericOutputDataType);

        var wrongOutputType = typed.Networks[0].NumericOperation! with
        {
            OutputDataType = LadderNumericDataType.Int,
        };
        False(LadderCompiler.Compile(typed with
        {
            Networks = [typed.Networks[0] with { NumericOperation = wrongOutputType }],
        }).IsValid);

        var overflowProgram = NumericProgram(LadderNumericOperationKind.Convert,
            destination: "int_result", sourceA: "40000.0");
        var overflowOperation = overflowProgram.Networks[0].NumericOperation! with
        {
            InputDataType = LadderNumericDataType.Real,
            OutputDataType = LadderNumericDataType.Int,
        };
        overflowProgram = overflowProgram with
        {
            Networks = [overflowProgram.Networks[0] with { NumericOperation = overflowOperation }],
        };
        True(LadderCompiler.Compile(overflowProgram).IsValid);
        var overflowRuntime = Runtime(overflowProgram);
        overflowRuntime.Run();
        var overflow = overflowRuntime.Scan(Inputs(("enable", true)));
        Equal(0.0, overflow.NumericVariables["int_result"]);
        True(overflow.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_CONVERT_OVERFLOW", StringComparison.Ordinal)));
        var overflowObservation = overflow.Observations!.GlobalLast["numeric-operation"];
        True(overflowObservation.Evaluated && overflowObservation.NetworkEnable == true
            && overflowObservation.LocalResult);
        Equal("int_result", overflowObservation.DestinationIdentity);
        Equal(0.0, Convert.ToDouble(overflowObservation.DestinationBefore));
        True(overflowObservation.CalculatedResult is null);
        Equal(0.0, Convert.ToDouble(overflowObservation.StoredAfter));
        Equal(InstructionWriteDisposition.FailedNoWrite, overflowObservation.WriteDisposition);
        False(overflowObservation.Rounded || overflowObservation.Clamped);
        Equal(1, overflowObservation.Diagnostics.Count);
        True(overflowObservation.Diagnostics[0].Code.StartsWith("VC_RUNTIME_CONVERT_OVERFLOW", StringComparison.Ordinal));
        var overflowSource = overflowObservation.Operands.Single(operand => operand.Role == "A");
        Equal("40000.0", overflowSource.Identity);
        Equal(40000.0, Convert.ToDouble(overflowSource.Value));
        Equal(ObservationUnavailableReason.None, overflowSource.Reason);
    }

    private static void TestSquareRootDataTypeSelection()
    {
        var choices = new[] { LadderNumericDataType.Unspecified, LadderNumericDataType.Real };
        True(LadderNumericOperationRules.HasTiaDataTypeSelector(LadderNumericOperationKind.SquareRoot));
        True(LadderNumericDataTypePresentation.TiaSelectableTypesFor(LadderNumericOperationKind.SquareRoot).SequenceEqual(choices));
        False(LadderNumericOperationRules.SupportsTiaDataType(LadderNumericOperationKind.SquareRoot, LadderNumericDataType.Int));
        False(LadderNumericOperationRules.SupportsTiaDataType(LadderNumericOperationKind.SquareRoot, LadderNumericDataType.DInt));
        False(LadderNumericOperationRules.SupportsTiaDataType(LadderNumericOperationKind.SquareRoot, LadderNumericDataType.Auto));

        static LadderProgram WithType(LadderProgram program, LadderNumericDataType type)
        {
            var network = program.Networks[0];
            return program with { Networks = [network with
            {
                NumericOperation = network.NumericOperation! with { DataType = type },
            }] };
        }

        var typed = WithType(NumericProgram(LadderNumericOperationKind.SquareRoot), LadderNumericDataType.Real);
        True(LadderCompiler.Compile(typed).IsValid);
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(typed));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(LadderNumericDataType.Real, loaded.Program!.Networks[0].NumericOperation!.DataType);
        var editor = new LadderEditorDocument(); editor.ReplaceFromProgram(typed);
        var editorRoundTrip = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(editorRoundTrip.IsReadable && editorRoundTrip.Document is not null);
        Equal(LadderNumericDataType.Real, editorRoundTrip.Document!.Rungs[0].NumericDataType);

        var positiveRuntime = Runtime(typed); positiveRuntime.Run();
        var positive = positiveRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", 25)));
        Equal(5.0, positive.NumericVariables["real_result"]);
        True(positive.Elements["numeric-operation"].Energized);

        var negative = typed with
        {
            Variables = typed.Variables.Select(variable => variable.Name == "real_result"
                ? variable with { InitialValue = 12.5 }
                : variable).ToArray(),
        };
        var negativeRuntime = Runtime(negative); negativeRuntime.Run();
        var invalid = negativeRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", -1)));
        True(double.IsNaN(invalid.NumericVariables["real_result"]));
        // Element.Energized records power at the instruction input (EN), not
        // the instruction's Boolean result (ENO). A domain fault must leave
        // EN energized while the captured fault makes the rendered ENO false.
        True(invalid.Elements["numeric-operation"].Energized);
        True(invalid.Observations!.GlobalLast["numeric-operation"].NetworkEnable == true);
        Equal(InstructionWriteDisposition.FailedInvalidValueWritten,
            invalid.Observations.GlobalLast["numeric-operation"].WriteDisposition);
        True(invalid.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_SQRT_DOMAIN", StringComparison.Ordinal)));

        var mismatch = typed with
        {
            Variables = typed.Variables.Select(variable => variable.Name == "left"
                ? variable with { Type = PlcVariableType.Int, InitialValue = 25L }
                : variable).ToArray(),
        };
        HasIssue(mismatch, "VC002");
    }

    private static void TestExponentiateDataTypeSelection()
    {
        Equal(LadderNumericDataType.Real, LadderNumericOperationRules.SelectedDataTypeForSource(
            LadderNumericOperationKind.Exponentiate, 0, LadderNumericDataType.DInt,
            LadderNumericDataType.Unspecified, LadderNumericDataType.Unspecified));
        Equal(LadderNumericDataType.DInt, LadderNumericOperationRules.SelectedDataTypeForSource(
            LadderNumericOperationKind.Exponentiate, 1, LadderNumericDataType.DInt,
            LadderNumericDataType.Unspecified, LadderNumericDataType.Unspecified));
        var choices = new[]
        {
            LadderNumericDataType.Unspecified,
            LadderNumericDataType.Int,
            LadderNumericDataType.DInt,
            LadderNumericDataType.Real,
        };
        True(LadderNumericOperationRules.HasTiaDataTypeSelector(LadderNumericOperationKind.Exponentiate));
        True(LadderNumericDataTypePresentation.TiaSelectableTypesFor(LadderNumericOperationKind.Exponentiate)
            .SequenceEqual(choices));
        False(LadderNumericOperationRules.SupportsTiaDataType(
            LadderNumericOperationKind.Exponentiate, LadderNumericDataType.Auto));
        static LadderProgram WithOperation(LadderProgram program, LadderNumericOperation operation) =>
            program with { Networks = [program.Networks[0] with { NumericOperation = operation }] };

        var source = NumericProgram(LadderNumericOperationKind.Exponentiate);
        var typed = WithOperation(source, source.Networks[0].NumericOperation! with
        {
            DataType = LadderNumericDataType.Real,
        });
        True(LadderCompiler.Compile(typed).IsValid);
        var runtime = Runtime(typed); runtime.Run();
        Equal(256.0, runtime.Scan(Inputs(("enable", true)),
            NumericInputs(("left", 2), ("right", 8))).NumericVariables["real_result"]);
        var realPrecisionRuntime = Runtime(typed); realPrecisionRuntime.Run();
        Equal((double)(float)Math.Pow((float)1.0000001, (float)3.0),
            realPrecisionRuntime.Scan(Inputs(("enable", true)),
                NumericInputs(("left", 1.0000001), ("right", 3.0))).NumericVariables["real_result"]);

        var overflowProgram = typed with
        {
            Variables = typed.Variables.Select(variable => variable.Name == "real_result"
                ? variable with { InitialValue = 12.0 }
                : variable).ToArray(),
        };
        var overflowRuntime = Runtime(overflowProgram); overflowRuntime.Run();
        var overflow = overflowRuntime.Scan(Inputs(("enable", true)),
            NumericInputs(("left", 10.0), ("right", 50.0)));
        Equal(12.0, overflow.NumericVariables["real_result"]);
        Equal(1, overflow.Diagnostics.Count);
        True(overflow.Diagnostics[0].StartsWith("VC_RUNTIME_EXPT_OVERFLOW", StringComparison.Ordinal));
        var overflowObserved = overflow.Observations!.GlobalLast["numeric-operation"];
        True(overflowObserved.Evaluated && overflowObserved.NetworkEnable == true
            && overflowObserved.LocalResult);
        Equal("real_result", overflowObserved.DestinationIdentity);
        Equal(12.0, Convert.ToDouble(overflowObserved.DestinationBefore));
        True(overflowObserved.CalculatedResult is null);
        Equal(12.0, Convert.ToDouble(overflowObserved.StoredAfter));
        Equal(InstructionWriteDisposition.FailedNoWrite, overflowObserved.WriteDisposition);
        False(overflowObserved.Rounded || overflowObserved.Clamped);
        Equal(1, overflowObserved.Diagnostics.Count);
        True(overflowObserved.Diagnostics[0].Code.StartsWith(
            "VC_RUNTIME_EXPT_OVERFLOW", StringComparison.Ordinal));
        var overflowBase = overflowObserved.Operands.Single(operand => operand.Role == "A");
        var overflowExponent = overflowObserved.Operands.Single(operand => operand.Role == "B");
        Equal("left", overflowBase.Identity);
        Equal(PlcVariableType.Real, overflowBase.DeclaredType!.Value);
        Equal(10.0, Convert.ToDouble(overflowBase.Value));
        Equal(ObservationUnavailableReason.None, overflowBase.Reason);
        Equal("right", overflowExponent.Identity);
        Equal(PlcVariableType.Real, overflowExponent.DeclaredType!.Value);
        Equal(50.0, Convert.ToDouble(overflowExponent.Value));
        Equal(ObservationUnavailableReason.None, overflowExponent.Reason);

        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(typed));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(LadderNumericDataType.Real, loaded.Program!.Networks[0].NumericOperation!.DataType);
        var editor = new LadderEditorDocument(); editor.ReplaceFromProgram(typed);
        var editorRoundTrip = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(editorRoundTrip.IsReadable && editorRoundTrip.Document is not null);
        Equal(LadderNumericDataType.Real, editorRoundTrip.Document!.Rungs[0].NumericDataType);

        var intExponent = typed with
        {
            Variables = typed.Variables.Append(
                new PlcVariable("exponent_int", PlcVariableType.Int, PlcVariableRole.Memory, 1L)).ToArray(),
        };
        intExponent = WithOperation(intExponent, intExponent.Networks[0].NumericOperation! with
        {
            SourceB = "exponent_int",
            DataType = LadderNumericDataType.Int,
        });
        True(LadderCompiler.Compile(intExponent).IsValid);
        var intExponentRuntime = Runtime(intExponent); intExponentRuntime.Run();
        var intExponentSnapshot = intExponentRuntime.Scan(Inputs(("enable", true)),
            NumericInputs(("left", 16777217.0)));
        Equal(16777216.0, intExponentSnapshot.NumericVariables["left"]);
        Equal(16777216.0, intExponentSnapshot.NumericVariables["real_result"]);
        Equal(16777216.0, Convert.ToDouble(intExponentSnapshot.Observations!.GlobalLast["numeric-operation"]
            .Operands.Single(operand => operand.Role == "A").Value));

        var wrongBase = typed with
        {
            Variables = typed.Variables.Select(variable => variable.Name == "left"
                ? variable with { Type = PlcVariableType.Int, InitialValue = 2L }
                : variable).ToArray(),
        };
        HasIssue(wrongBase, "VC002");

        HasIssue(WithOperation(typed, typed.Networks[0].NumericOperation! with
        {
            Destination = "int_result",
        }), "VC002");
        HasIssue(WithOperation(typed, typed.Networks[0].NumericOperation! with
        {
            DataType = LadderNumericDataType.Int,
        }), "VC002");
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

    private static void TestScientificDataTypeSelection()
    {
        var kinds = new[]
        {
            LadderNumericOperationKind.NaturalLog,
            LadderNumericOperationKind.Sine,
            LadderNumericOperationKind.Cosine,
            LadderNumericOperationKind.Tangent,
            LadderNumericOperationKind.ArcSine,
            LadderNumericOperationKind.ArcCosine,
            LadderNumericOperationKind.ArcTangent,
        };
        var choices = new[] { LadderNumericDataType.Unspecified, LadderNumericDataType.Real };
        foreach (var kind in kinds)
        {
            True(LadderNumericOperationRules.HasTiaDataTypeSelector(kind));
            True(LadderNumericDataTypePresentation.TiaSelectableTypesFor(kind).SequenceEqual(choices));
            True(LadderNumericOperationRules.SupportsTiaDataType(kind, LadderNumericDataType.Real));
            False(LadderNumericOperationRules.SupportsTiaDataType(kind, LadderNumericDataType.Int));
            False(LadderNumericOperationRules.SupportsTiaDataType(kind, LadderNumericDataType.DInt));
            False(LadderNumericOperationRules.SupportsTiaDataType(kind, LadderNumericDataType.Auto));

            var source = NumericProgram(kind);
            var operation = source.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Real };
            var typed = source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };
            True(LadderCompiler.Compile(typed).IsValid);
            var invalidRuntime = Runtime(typed); invalidRuntime.Run();
            var invalid = invalidRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", double.NaN)));
            True(double.IsNaN(invalid.NumericVariables["real_result"]));
            Equal(InstructionWriteDisposition.FailedInvalidValueWritten,
                invalid.Observations!.GlobalLast["numeric-operation"].WriteDisposition);
            True(invalid.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_DOMAIN", StringComparison.Ordinal)));

            var wrongSource = typed with
            {
                Variables = typed.Variables.Select(variable => variable.Name == "left"
                    ? variable with { Type = PlcVariableType.Int, InitialValue = 1L }
                    : variable).ToArray(),
            };
            HasIssue(wrongSource, "VC002");
        }

        var roundTripSource = NumericProgram(LadderNumericOperationKind.Sine);
        roundTripSource = roundTripSource with
        {
            Networks = [roundTripSource.Networks[0] with
            {
                NumericOperation = roundTripSource.Networks[0].NumericOperation! with
                {
                    DataType = LadderNumericDataType.Real,
                },
            }],
        };
        var programRoundTrip = LadderProgramJson.Load(LadderProgramJson.Save(roundTripSource));
        True(programRoundTrip.IsValid && programRoundTrip.Program is not null);
        Equal(LadderNumericDataType.Real, programRoundTrip.Program!.Networks[0].NumericOperation!.DataType);
        var editor = new LadderEditorDocument(); editor.ReplaceFromProgram(roundTripSource);
        var editorRoundTrip = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(editorRoundTrip.IsReadable && editorRoundTrip.Document is not null);
        Equal(LadderNumericDataType.Real, editorRoundTrip.Document!.Rungs[0].NumericDataType);

        static double Execute(LadderNumericOperationKind kind, double input)
        {
            var source = NumericProgram(kind, sourceA: input.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            var operation = source.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Real };
            source = source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };
            var runtime = Runtime(source); runtime.Run();
            return runtime.Scan(Inputs(("enable", true))).NumericVariables["real_result"];
        }

        True(Math.Abs(Execute(LadderNumericOperationKind.Sine, Math.PI / 2) - 1.0) < 0.000001);
        True(Math.Abs(Execute(LadderNumericOperationKind.Cosine, Math.PI) + 1.0) < 0.000001);
        True(Math.Abs(Execute(LadderNumericOperationKind.Tangent, Math.PI / 4) - 1.0) < 0.000001);
        True(Math.Abs(Execute(LadderNumericOperationKind.ArcSine, 1.0) - Math.PI / 2) < 0.000001);
        True(Math.Abs(Execute(LadderNumericOperationKind.ArcCosine, -1.0) - Math.PI) < 0.000001);
        True(Math.Abs(Execute(LadderNumericOperationKind.ArcTangent, 1.0) - Math.PI / 4) < 0.000001);
        True(double.IsNegativeInfinity(Execute(LadderNumericOperationKind.NaturalLog, 0.0)));
    }

    private static void TestTruncateDataTypeSelection()
    {
        var inputChoices = new[]
        {
            LadderNumericDataType.Unspecified,
            LadderNumericDataType.Real,
        };
        var outputChoices = new[]
        {
            LadderNumericDataType.Unspecified,
            LadderNumericDataType.Int,
            LadderNumericDataType.DInt,
            LadderNumericDataType.Real,
        };
        True(LadderNumericOperationRules.HasTiaDataTypeSelector(LadderNumericOperationKind.Truncate));
        True(LadderNumericOperationRules.HasTiaDualDataTypeSelectors(LadderNumericOperationKind.Truncate));
        True(LadderNumericDataTypePresentation.TiaInputTypesFor(LadderNumericOperationKind.Truncate)
            .SequenceEqual(inputChoices));
        True(LadderNumericDataTypePresentation.TiaOutputTypesFor(LadderNumericOperationKind.Truncate)
            .SequenceEqual(outputChoices));
        False(LadderNumericOperationRules.SupportsTiaDataType(
            LadderNumericOperationKind.Truncate, LadderNumericDataType.Auto));

        static LadderProgram WithOperation(LadderProgram source, LadderNumericOperation operation) =>
            source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };

        var source = NumericProgram(LadderNumericOperationKind.Truncate, destination: "int_result");
        var typed = WithOperation(source, source.Networks[0].NumericOperation! with
        {
            InputDataType = LadderNumericDataType.Real,
            OutputDataType = LadderNumericDataType.Int,
        });
        True(LadderCompiler.Compile(typed).IsValid);
        var runtime = Runtime(typed); runtime.Run();
        var truncated = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", -7.9)));
        Equal((double)(float)-7.9, truncated.NumericVariables["left"]);
        Equal(-7.0, truncated.NumericVariables["int_result"]);
        Equal((double)(float)-7.9, Convert.ToDouble(truncated.Observations!.GlobalLast["numeric-operation"]
            .Operands.Single(operand => operand.Role == "A").Value));

        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(typed));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(LadderNumericDataType.Real, loaded.Program!.Networks[0].NumericOperation!.InputDataType);
        Equal(LadderNumericDataType.Int, loaded.Program.Networks[0].NumericOperation!.OutputDataType);
        var editor = new LadderEditorDocument(); editor.ReplaceFromProgram(typed);
        var editorRoundTrip = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(editorRoundTrip.IsReadable && editorRoundTrip.Document is not null);
        Equal(LadderNumericDataType.Real, editorRoundTrip.Document!.Rungs[0].NumericInputDataType);
        Equal(LadderNumericDataType.Int, editorRoundTrip.Document.Rungs[0].NumericOutputDataType);

        var wrongSource = typed with
        {
            Variables = typed.Variables.Select(variable => variable.Name == "left"
                ? variable with { Type = PlcVariableType.Int, InitialValue = -7L }
                : variable).ToArray(),
        };
        HasIssue(wrongSource, "VC002");
        HasIssue(WithOperation(typed, typed.Networks[0].NumericOperation! with
        {
            InputDataType = LadderNumericDataType.Int,
        }), "VC007");
        HasIssue(WithOperation(typed, typed.Networks[0].NumericOperation! with
        {
            OutputDataType = LadderNumericDataType.DInt,
        }), "VC002");

        var overflowRuntime = Runtime(typed); overflowRuntime.Run();
        var overflow = overflowRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", 40000.9)));
        Equal(0.0, overflow.NumericVariables["int_result"]);
        True(overflow.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_TRUNC_OVERFLOW", StringComparison.Ordinal)));

        var realSource = NumericProgram(LadderNumericOperationKind.Truncate);
        var realTyped = WithOperation(realSource, realSource.Networks[0].NumericOperation! with
        {
            InputDataType = LadderNumericDataType.Real,
            OutputDataType = LadderNumericDataType.Real,
        });
        var invalidRuntime = Runtime(realTyped); invalidRuntime.Run();
        var invalid = invalidRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", double.NaN)));
        True(double.IsNaN(invalid.NumericVariables["real_result"]));
        Equal(InstructionWriteDisposition.FailedInvalidValueWritten,
            invalid.Observations!.GlobalLast["numeric-operation"].WriteDisposition);
        True(invalid.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_TRUNC_OVERFLOW", StringComparison.Ordinal)));

        // Existing saved programs used DataType as the TRUNC output selector.
        // Preserve that interpretation while new documents store both visible
        // Siemens type fields explicitly.
        var legacy = WithOperation(source, source.Networks[0].NumericOperation! with
        {
            DataType = LadderNumericDataType.Int,
        });
        True(LadderCompiler.Compile(legacy).IsValid);
        var legacyRuntime = Runtime(legacy); legacyRuntime.Run();
        Equal(-7.0, legacyRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", -7.9)))
            .NumericVariables["int_result"]);
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

    private static void TestScalingDataTypeSelection()
    {
        True(LadderNumericOperationRules.HasTiaDualDataTypeSelectors(LadderNumericOperationKind.Normalize));
        True(LadderNumericOperationRules.HasTiaDualDataTypeSelectors(LadderNumericOperationKind.Scale));
        True(LadderNumericDataTypePresentation.TiaInputTypesFor(LadderNumericOperationKind.Normalize)
            .SequenceEqual(new[] { LadderNumericDataType.Unspecified, LadderNumericDataType.Int,
                LadderNumericDataType.DInt, LadderNumericDataType.Real }));
        True(LadderNumericDataTypePresentation.TiaOutputTypesFor(LadderNumericOperationKind.Normalize)
            .SequenceEqual(new[] { LadderNumericDataType.Unspecified, LadderNumericDataType.Real }));
        True(LadderNumericDataTypePresentation.TiaInputTypesFor(LadderNumericOperationKind.Scale)
            .SequenceEqual(new[] { LadderNumericDataType.Unspecified, LadderNumericDataType.Real }));
        True(LadderNumericDataTypePresentation.TiaOutputTypesFor(LadderNumericOperationKind.Scale)
            .SequenceEqual(new[] { LadderNumericDataType.Unspecified, LadderNumericDataType.Int,
                LadderNumericDataType.DInt, LadderNumericDataType.Real }));
        Equal(LadderNumericDataType.DInt, LadderNumericOperationRules.SelectedDataTypeForSource(
            LadderNumericOperationKind.Normalize, 1, LadderNumericDataType.Unspecified,
            LadderNumericDataType.DInt, LadderNumericDataType.Real));
        Equal(LadderNumericDataType.DInt, LadderNumericOperationRules.SelectedDataTypeForSource(
            LadderNumericOperationKind.Scale, 0, LadderNumericDataType.Unspecified,
            LadderNumericDataType.Real, LadderNumericDataType.DInt));
        Equal(LadderNumericDataType.Real, LadderNumericOperationRules.SelectedDataTypeForSource(
            LadderNumericOperationKind.Scale, 1, LadderNumericDataType.Unspecified,
            LadderNumericDataType.Real, LadderNumericDataType.DInt));
        Equal(LadderNumericDataType.DInt, LadderNumericOperationRules.SelectedDataTypeForSource(
            LadderNumericOperationKind.Scale, 2, LadderNumericDataType.Unspecified,
            LadderNumericDataType.Real, LadderNumericDataType.DInt));

        static LadderProgram ProgramFor(LadderNumericOperationKind kind, PlcVariableType rangeType,
            PlcVariableType destinationType, double value)
        {
            static object Initial(PlcVariableType type, long integer)
            {
                if (type == PlcVariableType.Real) return (double)integer;
                return integer;
            }
            static LadderNumericDataType DataType(PlcVariableType type) => type switch
            {
                PlcVariableType.Int => LadderNumericDataType.Int,
                PlcVariableType.DInt => LadderNumericDataType.DInt,
                PlcVariableType.Real => LadderNumericDataType.Real,
                _ => throw new ArgumentOutOfRangeException(nameof(type)),
            };
            var variables = new PlcVariable[]
            {
                new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
                new("minimum", rangeType, PlcVariableRole.Input, Initial(rangeType, 10)),
                new("value", kind == LadderNumericOperationKind.Normalize ? rangeType : PlcVariableType.Real,
                    PlcVariableRole.Input, kind == LadderNumericOperationKind.Normalize
                        ? rangeType == PlcVariableType.Real ? (object)value : Convert.ToInt64(value)
                        : value),
                new("maximum", rangeType, PlcVariableRole.Input, Initial(rangeType, 30)),
                new("result", destinationType, PlcVariableRole.Memory, Initial(destinationType, 12)),
            };
            var operation = new LadderNumericOperation("scaling", kind, "minimum", "value", "result", "maximum")
            {
                InputDataType = kind == LadderNumericOperationKind.Normalize
                    ? DataType(rangeType) : LadderNumericDataType.Real,
                OutputDataType = kind == LadderNumericOperationKind.Normalize
                    ? LadderNumericDataType.Real : DataType(destinationType),
            };
            return new LadderProgram(1, "scaling-types", "ScalingTypes", "LD", TimeSpan.FromMilliseconds(20),
                variables, [new("scaling-net", "Typed scaling", new("enable-contact", LadderNodeKind.Contact, "enable"),
                    NumericOperation: operation)]);
        }

        var norm = ProgramFor(LadderNumericOperationKind.Normalize, PlcVariableType.DInt, PlcVariableType.Real, 40);
        var normCompile = LadderCompiler.Compile(norm);
        if (!normCompile.IsValid)
            throw new InvalidOperationException(string.Join(" | ", normCompile.Issues.Select(issue => $"{issue.Code} {issue.Path}: {issue.Message}")));
        var normRuntime = Runtime(norm); normRuntime.Run();
        var normSnapshot = normRuntime.Scan(Inputs(("enable", true)));
        Equal(1.5, normSnapshot.NumericVariables["result"]);
        AssertScalingObservation(normSnapshot.Observations!.GlobalLast["scaling"], 12.0, 1.5,
            PlcVariableType.DInt, 40.0);

        var scale = ProgramFor(LadderNumericOperationKind.Scale, PlcVariableType.DInt, PlcVariableType.DInt, 1.5);
        True(LadderCompiler.Compile(scale).IsValid);
        var scaleRuntime = Runtime(scale); scaleRuntime.Run();
        var scaleSnapshot = scaleRuntime.Scan(Inputs(("enable", true)));
        Equal(40.0, scaleSnapshot.NumericVariables["result"]);
        AssertScalingObservation(scaleSnapshot.Observations!.GlobalLast["scaling"], 12.0, 40.0,
            PlcVariableType.Real, 1.5);

        var scaleOverflow = scale with
        {
            Variables = scale.Variables.Select(variable => variable.Name switch
            {
                "minimum" => variable with { InitialValue = 0L },
                "value" => variable with { InitialValue = 2.0 },
                "maximum" => variable with { InitialValue = 2_000_000_000L },
                "result" => variable with { InitialValue = 12L },
                _ => variable,
            }).ToArray(),
        };
        True(LadderCompiler.Compile(scaleOverflow).IsValid);
        var overflowRuntime = Runtime(scaleOverflow); overflowRuntime.Run();
        var overflow = overflowRuntime.Scan(Inputs(("enable", true)));
        Equal(12.0, overflow.NumericVariables["result"]);
        Equal(1, overflow.Diagnostics.Count);
        True(overflow.Diagnostics[0].StartsWith("VC_RUNTIME_SCALE_X_OVERFLOW", StringComparison.Ordinal));
        var overflowObserved = overflow.Observations!.GlobalLast["scaling"];
        True(overflowObserved.Evaluated && overflowObserved.NetworkEnable == true
            && overflowObserved.LocalResult);
        Equal("result", overflowObserved.DestinationIdentity);
        Equal(12.0, Convert.ToDouble(overflowObserved.DestinationBefore));
        True(overflowObserved.CalculatedResult is null);
        Equal(12.0, Convert.ToDouble(overflowObserved.StoredAfter));
        Equal(InstructionWriteDisposition.FailedNoWrite, overflowObserved.WriteDisposition);
        False(overflowObserved.Rounded || overflowObserved.Clamped);
        Equal(1, overflowObserved.Diagnostics.Count);
        True(overflowObserved.Diagnostics[0].Code.StartsWith(
            "VC_RUNTIME_SCALE_X_OVERFLOW", StringComparison.Ordinal));
        var overflowMinimum = overflowObserved.Operands.Single(operand => operand.Role == "A");
        var overflowValue = overflowObserved.Operands.Single(operand => operand.Role == "B");
        var overflowMaximum = overflowObserved.Operands.Single(operand => operand.Role == "C");
        Equal("minimum", overflowMinimum.Identity);
        Equal(PlcVariableType.DInt, overflowMinimum.DeclaredType!.Value);
        Equal(0.0, Convert.ToDouble(overflowMinimum.Value));
        Equal(ObservationUnavailableReason.None, overflowMinimum.Reason);
        Equal("value", overflowValue.Identity);
        Equal(PlcVariableType.Real, overflowValue.DeclaredType!.Value);
        Equal(2.0, Convert.ToDouble(overflowValue.Value));
        Equal(ObservationUnavailableReason.None, overflowValue.Reason);
        Equal("maximum", overflowMaximum.Identity);
        Equal(PlcVariableType.DInt, overflowMaximum.DeclaredType!.Value);
        Equal(2_000_000_000.0, Convert.ToDouble(overflowMaximum.Value));
        Equal(ObservationUnavailableReason.None, overflowMaximum.Reason);

        var invalidCases = new (LadderProgram Program, string[] Operands)[]
        {
            (ProgramFor(LadderNumericOperationKind.Normalize, PlcVariableType.Real,
                PlcVariableType.Real, 20.0), ["minimum", "value", "maximum"]),
            (scale, ["value"]),
            (ProgramFor(LadderNumericOperationKind.Scale, PlcVariableType.Real,
                PlcVariableType.Real, 0.5), ["minimum", "value", "maximum"]),
        };
        foreach (var invalidCase in invalidCases)
        {
            var invalidProgram = invalidCase.Program;
            var kind = invalidProgram.Networks[0].NumericOperation!.Kind;
            var expectedCode = kind == LadderNumericOperationKind.Normalize
                ? "VC_RUNTIME_NORM_X_INVALID" : "VC_RUNTIME_SCALE_X_INVALID";
            foreach (var operand in invalidCase.Operands)
            {
                foreach (var invalidInput in new[]
                         {
                             double.NaN,
                             double.PositiveInfinity,
                             double.NegativeInfinity,
                             (double)float.Epsilon,
                         })
                {
                    var invalidRuntime = Runtime(invalidProgram); invalidRuntime.Run();
                    var invalid = invalidRuntime.Scan(Inputs(("enable", true)),
                        NumericInputs((operand, invalidInput)));
                    Equal(12.0, invalid.NumericVariables["result"]);
                    Equal(1, invalid.Diagnostics.Count);
                    True(invalid.Diagnostics[0].StartsWith(expectedCode, StringComparison.Ordinal));
                    var invalidObserved = invalid.Observations!.GlobalLast["scaling"];
                    True(invalidObserved.Evaluated && invalidObserved.NetworkEnable == true
                        && invalidObserved.LocalResult);
                    Equal("result", invalidObserved.DestinationIdentity);
                    Equal(12.0, Convert.ToDouble(invalidObserved.DestinationBefore));
                    True(invalidObserved.CalculatedResult is null);
                    Equal(12.0, Convert.ToDouble(invalidObserved.StoredAfter));
                    Equal(InstructionWriteDisposition.FailedNoWrite, invalidObserved.WriteDisposition);
                    False(invalidObserved.Rounded || invalidObserved.Clamped);
                    Equal(1, invalidObserved.Diagnostics.Count);
                    True(invalidObserved.Diagnostics[0].Code.StartsWith(expectedCode, StringComparison.Ordinal));
                    var expectedRole = operand switch
                    {
                        "minimum" => "A",
                        "value" => "B",
                        "maximum" => "C",
                        _ => throw new InvalidOperationException("Unexpected scaling operand."),
                    };
                    var invalidValue = invalidObserved.Operands.Single(sample => sample.Role == expectedRole);
                    Equal(operand, invalidValue.Identity);
                    Equal(PlcVariableType.Real, invalidValue.DeclaredType!.Value);
                    True(invalidValue.Value is double sampled
                        && (double.IsNaN(invalidInput) ? double.IsNaN(sampled) : sampled.Equals(invalidInput)));
                    Equal(ObservationUnavailableReason.None, invalidValue.Reason);
                }
            }
        }

        var roundTrip = LadderProgramJson.Load(LadderProgramJson.Save(scale));
        True(roundTrip.IsValid && roundTrip.Program is not null);
        Equal(LadderNumericDataType.Real, roundTrip.Program!.Networks[0].NumericOperation!.InputDataType);
        Equal(LadderNumericDataType.DInt, roundTrip.Program.Networks[0].NumericOperation!.OutputDataType);

        var mismatched = scale with
        {
            Variables = scale.Variables.Select(variable => variable.Name == "value"
                ? variable with { Type = PlcVariableType.DInt, InitialValue = 1L }
                : variable).ToArray(),
        };
        HasIssue(mismatched, "VC002");

        static void AssertScalingObservation(InstructionObservation observed, double before, double after,
            PlcVariableType valueType, double value)
        {
            True(observed.Evaluated && observed.NetworkEnable == true && observed.LocalResult);
            Equal("result", observed.DestinationIdentity);
            Equal(before, Convert.ToDouble(observed.DestinationBefore));
            Equal(after, Convert.ToDouble(observed.CalculatedResult));
            Equal(after, Convert.ToDouble(observed.StoredAfter));
            Equal(InstructionWriteDisposition.WrittenChanged, observed.WriteDisposition);
            False(observed.Rounded || observed.Clamped);
            Equal(0, observed.Diagnostics.Count);
            var minimum = observed.Operands.Single(operand => operand.Role == "A");
            var sampledValue = observed.Operands.Single(operand => operand.Role == "B");
            var maximum = observed.Operands.Single(operand => operand.Role == "C");
            Equal("minimum", minimum.Identity);
            Equal(PlcVariableType.DInt, minimum.DeclaredType!.Value);
            Equal(10.0, Convert.ToDouble(minimum.Value));
            Equal(ObservationUnavailableReason.None, minimum.Reason);
            Equal("value", sampledValue.Identity);
            Equal(valueType, sampledValue.DeclaredType!.Value);
            Equal(value, Convert.ToDouble(sampledValue.Value));
            Equal(ObservationUnavailableReason.None, sampledValue.Reason);
            Equal("maximum", maximum.Identity);
            Equal(PlcVariableType.DInt, maximum.DeclaredType!.Value);
            Equal(30.0, Convert.ToDouble(maximum.Value));
            Equal(ObservationUnavailableReason.None, maximum.Reason);
        }
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
            var program = NumericProgram(kind, destination: destination, sourceA: source,
                sourceB: "missing_but_unused");
            if (kind == LadderNumericOperationKind.Convert
                || LadderNumericOperationRules.IsRoundingConversion(kind))
            {
                var dataType = destination switch
                {
                    "int_result" => LadderNumericDataType.Int,
                    "dint_result" => LadderNumericDataType.DInt,
                    _ => LadderNumericDataType.Real,
                };
                var operation = program.Networks[0].NumericOperation! with
                {
                    InputDataType = LadderNumericDataType.Real,
                    OutputDataType = dataType,
                };
                program = program with
                {
                    Networks = [program.Networks[0] with { NumericOperation = operation }],
                };
            }
            var runtime = Runtime(program);
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

    private static void TestRoundingConversionDataTypes()
    {
        var kinds = new[]
        {
            LadderNumericOperationKind.Round,
            LadderNumericOperationKind.Ceiling,
            LadderNumericOperationKind.Floor,
        };
        var inputChoices = new[] { LadderNumericDataType.Unspecified, LadderNumericDataType.Real };
        var outputChoices = new[]
        {
            LadderNumericDataType.Unspecified,
            LadderNumericDataType.Int,
            LadderNumericDataType.DInt,
            LadderNumericDataType.Real,
        };

        static LadderProgram Typed(LadderNumericOperationKind kind, string destination,
            LadderNumericDataType outputType, string source = "left")
        {
            var program = NumericProgram(kind, destination: destination, sourceA: source);
            return program with
            {
                Networks = [program.Networks[0] with
                {
                    NumericOperation = program.Networks[0].NumericOperation! with
                    {
                        InputDataType = LadderNumericDataType.Real,
                        OutputDataType = outputType,
                    },
                }],
            };
        }

        foreach (var kind in kinds)
        {
            True(LadderNumericOperationRules.HasTiaDataTypeSelector(kind));
            True(LadderNumericOperationRules.HasTiaDualDataTypeSelectors(kind));
            True(LadderNumericDataTypePresentation.TiaInputTypesFor(kind).SequenceEqual(inputChoices));
            True(LadderNumericDataTypePresentation.TiaOutputTypesFor(kind).SequenceEqual(outputChoices));

            var valid = Typed(kind, "dint_result", LadderNumericDataType.DInt);
            True(LadderCompiler.Compile(valid).IsValid);

            var wrongSource = valid with
            {
                Variables = valid.Variables.Select(variable => variable.Name == "left"
                    ? variable with { Type = PlcVariableType.Int, InitialValue = 0L }
                    : variable).ToArray(),
            };
            HasIssue(wrongSource, "VC002");
            var wrongOutput = valid with
            {
                Networks = [valid.Networks[0] with
                {
                    NumericOperation = valid.Networks[0].NumericOperation! with
                    {
                        OutputDataType = LadderNumericDataType.Int,
                    },
                }],
            };
            HasIssue(wrongOutput, "VC002");

            var overflowProgram = Typed(kind, "int_result", LadderNumericDataType.Int, "40000.25");
            var overflowRuntime = Runtime(overflowProgram); overflowRuntime.Run();
            var overflow = overflowRuntime.Scan(Inputs(("enable", true)));
            Equal(0.0, overflow.NumericVariables["int_result"]);
            var overflowCode = $"VC_RUNTIME_{LadderNumericOperationRules.RoundingMnemonic(kind)}_OVERFLOW";
            True(overflow.Diagnostics.Any(message => message.StartsWith(overflowCode, StringComparison.Ordinal)));
            var overflowObserved = overflow.Observations!.GlobalLast["numeric-operation"];
            True(overflowObserved.Evaluated && overflowObserved.NetworkEnable == true
                && overflowObserved.LocalResult);
            Equal("int_result", overflowObserved.DestinationIdentity);
            Equal(0.0, Convert.ToDouble(overflowObserved.DestinationBefore));
            True(overflowObserved.CalculatedResult is null);
            Equal(0.0, Convert.ToDouble(overflowObserved.StoredAfter));
            Equal(InstructionWriteDisposition.FailedNoWrite, overflowObserved.WriteDisposition);
            False(overflowObserved.Rounded || overflowObserved.Clamped);
            Equal(1, overflowObserved.Diagnostics.Count);
            True(overflowObserved.Diagnostics[0].Code.StartsWith(overflowCode, StringComparison.Ordinal));
            var overflowSource = overflowObserved.Operands.Single(operand => operand.Role == "A");
            Equal("40000.25", overflowSource.Identity);
            Equal(40000.25, Convert.ToDouble(overflowSource.Value));
            Equal(ObservationUnavailableReason.None, overflowSource.Reason);

            var invalidProgram = Typed(kind, "real_result", LadderNumericDataType.Real, "left");
            var invalidRuntime = Runtime(invalidProgram); invalidRuntime.Run();
            var invalid = invalidRuntime.Scan(Inputs(("enable", true)), NumericInputs(("left", double.NaN)));
            True(double.IsNaN(invalid.NumericVariables["real_result"]));
            var invalidObserved = invalid.Observations!.GlobalLast["numeric-operation"];
            True(invalidObserved.Evaluated && invalidObserved.NetworkEnable == true
                && invalidObserved.LocalResult);
            Equal("real_result", invalidObserved.DestinationIdentity);
            Equal(0.0, Convert.ToDouble(invalidObserved.DestinationBefore));
            True(invalidObserved.CalculatedResult is null);
            True(double.IsNaN(Convert.ToDouble(invalidObserved.StoredAfter)));
            Equal(InstructionWriteDisposition.FailedInvalidValueWritten, invalidObserved.WriteDisposition);
            False(invalidObserved.Rounded || invalidObserved.Clamped);
            Equal(1, invalidObserved.Diagnostics.Count);
            True(invalidObserved.Diagnostics[0].Code.StartsWith(overflowCode, StringComparison.Ordinal));
            var invalidSource = invalidObserved.Operands.Single(operand => operand.Role == "A");
            Equal("left", invalidSource.Identity);
            Equal(PlcVariableType.Real, invalidSource.DeclaredType!.Value);
            True(double.IsNaN(Convert.ToDouble(invalidSource.Value)));
            Equal(ObservationUnavailableReason.None, invalidSource.Reason);
        }

        static double Execute(LadderProgram program, double input)
        {
            var runtime = Runtime(program); runtime.Run();
            return runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", input)))
                .NumericVariables[program.Networks[0].NumericOperation!.Destination];
        }
        Equal(2.0, Execute(Typed(LadderNumericOperationKind.Round, "int_result", LadderNumericDataType.Int), 2.5));
        Equal(4.0, Execute(Typed(LadderNumericOperationKind.Round, "dint_result", LadderNumericDataType.DInt), 3.5));
        Equal(-2.0, Execute(Typed(LadderNumericOperationKind.Round, "real_result", LadderNumericDataType.Real), -1.5));
        var ceilZero = Execute(Typed(LadderNumericOperationKind.Ceiling, "dint_result", LadderNumericDataType.DInt), -0.5);
        Equal(0.0, ceilZero);
        True(BitConverter.DoubleToInt64Bits(ceilZero) == BitConverter.DoubleToInt64Bits(0.0));
        Equal(-1.0, Execute(Typed(LadderNumericOperationKind.Floor, "dint_result", LadderNumericDataType.DInt), -0.5));

        var persisted = Typed(LadderNumericOperationKind.Round, "dint_result", LadderNumericDataType.DInt);
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(persisted));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(LadderNumericDataType.Real, loaded.Program!.Networks[0].NumericOperation!.InputDataType);
        Equal(LadderNumericDataType.DInt, loaded.Program.Networks[0].NumericOperation!.OutputDataType);
        var editor = new LadderEditorDocument(); editor.ReplaceFromProgram(persisted);
        var editorLoaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(editorLoaded.IsReadable && editorLoaded.Document is not null);
        Equal(LadderNumericDataType.Real, editorLoaded.Document!.Rungs[0].NumericInputDataType);
        Equal(LadderNumericDataType.DInt, editorLoaded.Document.Rungs[0].NumericOutputDataType);
    }

    private static void TestDIntOutputLifecycle()
    {
        var source = NumericProgram(LadderNumericOperationKind.Move);
        var variables = source.Variables.Concat(
        [
            new PlcVariable("dint_result", PlcVariableType.DInt, PlcVariableRole.Output, 0L),
        ]).ToArray();
        // REAL-to-DINT is an explicit CONVERT operation with IEC checking;
        // MOVE cannot stand in for a type conversion.
        var operation = source.Networks[0].NumericOperation! with
        {
            Kind = LadderNumericOperationKind.Convert,
            Destination = "dint_result",
        };
        var program = source with
        {
            Variables = variables,
            Networks = [source.Networks[0] with { NumericOperation = operation }],
        };
        var runtime = Runtime(program);
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", 3_000_000_000.75)));
        Equal(0.0, snapshot.NumericVariables["dint_result"]);
        True(snapshot.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_CONVERT_OVERFLOW", StringComparison.Ordinal)));
        Equal(0.0, snapshot.NumericOutputs["dint_result"]);
        Equal(0.0, runtime.Stop().NumericOutputs["dint_result"]);
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(program));
        True(loaded.IsValid && loaded.Program is not null);
        Equal(PlcVariableType.DInt, loaded.Program!.Variables.Single(item => item.Name == "dint_result").Type);
    }

    private static void TestDivideByZeroResults()
    {
        static VirtualControllerSnapshot Execute(LadderNumericDataType dataType, string destination, double dividend)
        {
            var source = NumericProgram(LadderNumericOperationKind.Divide, destination: destination);
            var variableType = dataType switch
            {
                LadderNumericDataType.Int => PlcVariableType.Int,
                LadderNumericDataType.DInt => PlcVariableType.DInt,
                _ => PlcVariableType.Real,
            };
            var operation = source.Networks[0].NumericOperation! with { DataType = dataType };
            var program = source with
            {
                Variables = source.Variables.Select(variable => variable.Name is "left" or "right"
                    ? variable with { Type = variableType, InitialValue = 0L }
                    : variable).ToArray(),
                Networks = [source.Networks[0] with { NumericOperation = operation }],
            };
            True(LadderCompiler.Compile(program).IsValid);
            var runtime = Runtime(program);
            runtime.Run();
            return runtime.Scan(Inputs(("enable", true)), NumericInputs(("left", dividend), ("right", 0)));
        }

        foreach (var item in new[]
                 {
                     (Type: LadderNumericDataType.Int, Destination: "int_result"),
                     (Type: LadderNumericDataType.DInt, Destination: "dint_result"),
                 })
        {
            var snapshot = Execute(item.Type, item.Destination, 7);
            Equal(0.0, snapshot.NumericVariables[item.Destination]);
            Equal(0, snapshot.Diagnostics.Count);
            var observed = snapshot.Observations!.GlobalLast["numeric-operation"];
            Equal(true, observed.LocalResult);
            Equal(0.0, Convert.ToDouble(observed.CalculatedResult));
            Equal(InstructionWriteDisposition.WrittenSameValue, observed.WriteDisposition);
        }

        var real = Execute(LadderNumericDataType.Real, "real_result", 7);
        Equal(2143289344.0, real.NumericVariables["real_result"]);
        Equal(0, real.Diagnostics.Count);
        Equal(true, real.Observations!.GlobalLast["numeric-operation"].LocalResult);
        Equal(InstructionWriteDisposition.WrittenChanged,
            real.Observations.GlobalLast["numeric-operation"].WriteDisposition);

        var zeroOverZero = Execute(LadderNumericDataType.Real, "real_result", 0);
        True(double.IsNaN(zeroOverZero.NumericVariables["real_result"]));
        Equal(0, zeroOverZero.Diagnostics.Count);
        var zeroObserved = zeroOverZero.Observations!.GlobalLast["numeric-operation"];
        Equal(true, zeroObserved.LocalResult);
        True(zeroObserved.CalculatedResult is double calculated && double.IsNaN(calculated));
        Equal(InstructionWriteDisposition.WrittenChanged, zeroObserved.WriteDisposition);
    }

    private static void TestLiteralDivideByZero()
    {
        var source = NumericProgram(LadderNumericOperationKind.Divide,
            destination: "int_result", sourceA: "7", sourceB: "0");
        var operation = source.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Int };
        var program = source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };
        True(LadderCompiler.Compile(program).IsValid);
        var runtime = Runtime(program);
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("enable", true)));
        Equal(0.0, snapshot.NumericVariables["int_result"]);
        Equal(0, snapshot.Diagnostics.Count);
    }

    private static void TestAdvancedMathDiagnostics()
    {
        foreach (var item in new[]
                 {
                     (Type: LadderNumericDataType.Int, VariableType: PlcVariableType.Int, Destination: "int_result"),
                     (Type: LadderNumericDataType.DInt, VariableType: PlcVariableType.DInt, Destination: "dint_result"),
                 })
        {
            var source = NumericProgram(LadderNumericOperationKind.Modulo, destination: item.Destination);
            var operation = source.Networks[0].NumericOperation! with { DataType = item.Type };
            var program = source with
            {
                Variables = source.Variables.Select(variable => variable.Name is "left" or "right"
                    ? variable with { Type = item.VariableType, InitialValue = 0L }
                    : variable.Name == item.Destination
                        ? variable with { InitialValue = 12L }
                        : variable).ToArray(),
                Networks = [source.Networks[0] with { NumericOperation = operation }],
            };
            True(LadderCompiler.Compile(program).IsValid);
            var modulo = Runtime(program);
            modulo.Run();
            var snapshot = modulo.Scan(Inputs(("enable", true)), NumericInputs(("left", 7), ("right", 0)));
            Equal(0.0, snapshot.NumericVariables[item.Destination]);
            Equal(1, snapshot.Diagnostics.Count);
            True(snapshot.Diagnostics[0].StartsWith("VC_RUNTIME_MOD_ZERO", StringComparison.Ordinal));
            var observed = snapshot.Observations!.GlobalLast["numeric-operation"];
            Equal(true, observed.LocalResult);
            Equal(12.0, Convert.ToDouble(observed.DestinationBefore));
            Equal(0.0, Convert.ToDouble(observed.CalculatedResult));
            Equal(0.0, Convert.ToDouble(observed.StoredAfter));
            Equal(InstructionWriteDisposition.FailedErrorValueWritten, observed.WriteDisposition);
        }

        var squareRoot = Runtime(NumericProgram(LadderNumericOperationKind.SquareRoot));
        squareRoot.Run();
        var sqrtSnapshot = squareRoot.Scan(Inputs(("enable", true)), NumericInputs(("left", -1)));
        True(double.IsNaN(sqrtSnapshot.NumericVariables["real_result"]));
        True(sqrtSnapshot.Diagnostics.Any(item => item.StartsWith("VC_RUNTIME_SQRT_DOMAIN", StringComparison.Ordinal)));
    }

    private static void TestInvalidAdvancedMathLiteral()
    {
        var source = NumericProgram(LadderNumericOperationKind.Modulo,
            destination: "int_result", sourceA: "7", sourceB: "0");
        var operation = source.Networks[0].NumericOperation! with { DataType = LadderNumericDataType.Int };
        var program = source with { Networks = [source.Networks[0] with { NumericOperation = operation }] };
        True(LadderCompiler.Compile(program).IsValid);
        var runtime = Runtime(program);
        runtime.Run();
        var snapshot = runtime.Scan(Inputs(("enable", true)));
        Equal(0.0, snapshot.NumericVariables["int_result"]);
        Equal(InstructionWriteDisposition.FailedErrorValueWritten,
            snapshot.Observations!.GlobalLast["numeric-operation"].WriteDisposition);
        HasIssue(NumericProgram(LadderNumericOperationKind.SquareRoot, sourceA: "-1"), "VC009");
    }

    private static void TestScientificMathDiagnostics()
    {
        foreach (var item in new[]
                 {
                     (LadderNumericOperationKind.NaturalLog, "-1", "0"),
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
            if (item.Item1 == LadderNumericOperationKind.Exponentiate)
                Equal(0.0, snapshot.NumericVariables["real_result"]);
            else
            {
                True(double.IsNaN(snapshot.NumericVariables["real_result"]));
                Equal(InstructionWriteDisposition.FailedInvalidValueWritten,
                    snapshot.Observations!.GlobalLast["numeric-operation"].WriteDisposition);
            }
            True(snapshot.Diagnostics.Any(message => message.StartsWith("VC_RUNTIME_DOMAIN", StringComparison.Ordinal)));
        }
    }

    private static void TestInvalidScientificMathLiteral()
    {
        True(LadderCompiler.Compile(NumericProgram(LadderNumericOperationKind.NaturalLog, sourceA: "0")).IsValid);
        HasIssue(NumericProgram(LadderNumericOperationKind.NaturalLog, sourceA: "-0.01"), "VC009");
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
            var observed = snapshot.Observations!.GlobalLast["numeric-operation"];
            True(observed.Evaluated && observed.NetworkEnable == true && observed.LocalResult);
            Equal("real_result", observed.DestinationIdentity);
            Equal(0.0, Convert.ToDouble(observed.DestinationBefore));
            True(observed.CalculatedResult is null);
            Equal(0.0, Convert.ToDouble(observed.StoredAfter));
            Equal(InstructionWriteDisposition.FailedNoWrite, observed.WriteDisposition);
            False(observed.Rounded || observed.Clamped);
            Equal(1, observed.Diagnostics.Count);
            True(observed.Diagnostics[0].Code.StartsWith("VC_RUNTIME_RANGE", StringComparison.Ordinal));
            var minimum = observed.Operands.Single(operand => operand.Role == "A");
            var value = observed.Operands.Single(operand => operand.Role == "B");
            var maximum = observed.Operands.Single(operand => operand.Role == "C");
            Equal("left", minimum.Identity);
            Equal(30.0, Convert.ToDouble(minimum.Value));
            Equal("right", value.Identity);
            Equal(20.0, Convert.ToDouble(value.Value));
            Equal("third", maximum.Identity);
            Equal(10.0, Convert.ToDouble(maximum.Value));
            True(new[] { minimum, value, maximum }.All(operand =>
                operand.Reason == ObservationUnavailableReason.None));
        }
    }

    private static void TestMathInstanceOperands()
    {
        var counter = CounterProgram(2);
        var variables = counter.Variables.Concat(
        [
            new PlcVariable("numeric_result", PlcVariableType.DInt, PlcVariableRole.Memory, 0L),
        ]).ToArray();
        var operation = new LadderNetwork("copy-count", "Copy count", Contact("copy-enable", "count_input"),
            NumericOperation: new LadderNumericOperation("copy", LadderNumericOperationKind.Move,
                "parts.ACC", "0", "numeric_result"));
        var program = counter with { Variables = variables, Networks = [counter.Networks[0], operation] };
        var runtime = Runtime(program);
        runtime.Run();
        Equal(1.0, runtime.Scan(Inputs(("count_input", true))).NumericVariables["numeric_result"]);

        var mismatched = program with
        {
            Variables = variables.Select(variable => variable.Name == "numeric_result"
                ? variable with { Type = PlcVariableType.Real, InitialValue = 0.0 }
                : variable).ToArray(),
        };
        HasIssue(mismatched, "VC002");
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
        editor.AddInterfaceParameter(db.Id, "Speed", PlcVariableType.Real, LadderInterfaceSection.Static, 12.5,
            retain: true, comment: "Retained line-speed setpoint");
        var duplicateGlobalMemberRejected = false;
        try { editor.AddInterfaceParameter(db.Id, "speed", PlcVariableType.Real, LadderInterfaceSection.Static, 1.0); }
        catch (InvalidOperationException) { duplicateGlobalMemberRejected = true; }
        True(duplicateGlobalMemberRejected);
        editor.SelectBlock(editor.Blocks.IndexOf(db));
        var dbNetworkRejected = false;
        try { editor.AddRung("Invalid DB network", string.Empty); }
        catch (InvalidOperationException) { dbNetworkRejected = true; }
        True(dbNetworkRejected);
        var dbInterfaceSectionRejected = false;
        try { editor.AddInterfaceParameter(db.Id, "Command", PlcVariableType.Bool, LadderInterfaceSection.Input); }
        catch (InvalidOperationException) { dbInterfaceSectionRejected = true; }
        True(dbInterfaceSectionRejected);
        var dbTaskRejected = false;
        try { editor.AddTask("InvalidDbTask", LadderTaskKind.Continuous, editor.ScanPeriod, 10, db.Id); }
        catch (InvalidOperationException) { dbTaskRejected = true; }
        True(dbTaskRejected);
        var loaded = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        if (!loaded.IsReadable || loaded.Document is null)
            throw new InvalidOperationException("TIA block project did not reload.");
        Equal(LadderBlockType.OrganizationBlock, loaded.Document.Blocks[0].BlockType);
        Equal(LadderBlockType.FunctionBlock, loaded.Document.Blocks[1].BlockType);
        Equal(LadderInterfaceSection.Input, loaded.Document.Blocks[1].Interface[0].Section);
        Equal("Running", loaded.Document.Blocks[1].Interface[1].Name);
        Equal(LadderBlockType.DataBlock, loaded.Document.Blocks[2].BlockType);
        Equal(12.5, Convert.ToDouble(loaded.Document.Blocks[2].Interface[0].InitialValue));
        True(loaded.Document.Blocks[2].Interface[0].Retain);
        Equal("Retained line-speed setpoint", loaded.Document.Blocks[2].Interface[0].Comment);
        var duplicateDbJson = JsonNode.Parse(LadderEditorProjectJson.Save(editor))!.AsObject();
        var duplicateDbMembers = duplicateDbJson["blocks"]![2]!["interface"]!.AsArray();
        duplicateDbMembers.Add(duplicateDbMembers[0]!.DeepClone());
        duplicateDbMembers[1]!["name"] = "speed";
        var duplicateDbLoad = LadderEditorProjectJson.Load(duplicateDbJson.ToJsonString());
        False(duplicateDbLoad.IsReadable);
        True(duplicateDbLoad.Issues.Any(issue => issue.Message.Contains("Global DB declarations", StringComparison.Ordinal)));
        var invalidDbSectionJson = JsonNode.Parse(LadderEditorProjectJson.Save(editor))!.AsObject();
        invalidDbSectionJson["blocks"]![2]!["interface"]![0]!["section"] = LadderInterfaceSection.Input.ToString();
        var invalidDbSectionLoad = LadderEditorProjectJson.Load(invalidDbSectionJson.ToJsonString());
        False(invalidDbSectionLoad.IsReadable);
        True(invalidDbSectionLoad.Issues.Any(issue => issue.Message.Contains("Global DB declarations", StringComparison.Ordinal)
            && issue.Message.Contains("Static section", StringComparison.Ordinal)));
        foreach (var unsupportedType in new[] { PlcVariableType.Struct, PlcVariableType.Array, PlcVariableType.Time })
        {
            var unsupportedDbTypeJson = JsonNode.Parse(LadderEditorProjectJson.Save(editor))!.AsObject();
            unsupportedDbTypeJson["blocks"]![2]!["interface"]![0]!["type"] = unsupportedType.ToString();
            var unsupportedDbTypeLoad = LadderEditorProjectJson.Load(unsupportedDbTypeJson.ToJsonString());
            False(unsupportedDbTypeLoad.IsReadable);
            True(unsupportedDbTypeLoad.Issues.Any(issue => issue.Message.Contains("unsupported " + unsupportedType.ToString().ToUpperInvariant(), StringComparison.Ordinal)));
        }
        var invalidDbEnumJson = JsonNode.Parse(LadderEditorProjectJson.Save(editor))!.AsObject();
        var enumMemberJson = invalidDbEnumJson["blocks"]![2]!["interface"]![0]!;
        enumMemberJson["type"] = PlcVariableType.Enum.ToString();
        enumMemberJson["initial"] = "UnknownState";
        enumMemberJson["enumMembers"] = new JsonArray("Stopped", "Running");
        var invalidDbEnumLoad = LadderEditorProjectJson.Load(invalidDbEnumJson.ToJsonString());
        False(invalidDbEnumLoad.IsReadable);
        True(invalidDbEnumLoad.Issues.Any(issue => issue.Message.Contains("ENUM member", StringComparison.Ordinal)));
        enumMemberJson["initial"] = "Running";
        var validDbEnumLoad = LadderEditorProjectJson.Load(invalidDbEnumJson.ToJsonString());
        True(validDbEnumLoad.IsReadable);
        Equal("Running", validDbEnumLoad.Document!.Blocks[2].Interface[0].InitialValue);
        True(validDbEnumLoad.Document.Blocks[2].Interface[0].EnumMembers!.SequenceEqual(new[] { "Stopped", "Running" }));
        Equal(0, loaded.Document.Blocks[2].Rungs.Count);
        Equal(LadderBlockType.FunctionBlock, loaded.Document.BuildProgram().Blocks![1].BlockType);
        True(LadderCompiler.Compile(loaded.Document.BuildProgram()).IsValid);
        var validProgram = loaded.Document.BuildProgram();
        var validBlocks = validProgram.Blocks!;
        var illegalDbNetwork = new LadderNetwork(
            "illegal-db-network", "DBs do not execute LAD",
            new LadderNode("illegal-db-root", LadderNodeKind.Parallel, Children: Array.Empty<LadderNode>()));
        var invalidDbBlocks = validBlocks.Select(block => block.BlockType == LadderBlockType.DataBlock
            ? block with { Networks = [illegalDbNetwork] }
            : block).ToArray();
        HasIssue(validProgram with { Blocks = invalidDbBlocks }, "VC_DB_STRUCTURE");
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

    private static void TestCpuSymbolUniqueness()
    {
        var editor = new LadderEditorDocument { Id = "cpu-name-scope", Name = "NameScope" };
        var block = editor.AddBlock("OB1", LadderBlockType.OrganizationBlock);
        editor.AddTagTable("Auxiliary");
        editor.AddTag("Ready", PlcVariableRole.Memory);

        var duplicateAcrossTablesRejected = false;
        try { editor.AddTag("READY", PlcVariableRole.Memory, tagTable: "Auxiliary"); }
        catch (InvalidOperationException) { duplicateAcrossTablesRejected = true; }
        True(duplicateAcrossTablesRejected);
        Equal(1, editor.Tags.Count);

        var tagAgainstBlockRejected = false;
        try { editor.AddTag("ob1", PlcVariableRole.Memory); }
        catch (InvalidOperationException) { tagAgainstBlockRejected = true; }
        True(tagAgainstBlockRejected);

        var blockAgainstTagRejected = false;
        try { editor.AddBlock("ready"); }
        catch (InvalidOperationException) { blockAgainstTagRejected = true; }
        True(blockAgainstTagRejected);

        var blockRenameRejected = false;
        try { editor.RenameBlock(block.Id, "READY"); }
        catch (InvalidOperationException) { blockRenameRejected = true; }
        True(blockRenameRejected);
        Equal("OB1", block.Name);

        var tagRenameRejected = false;
        try
        {
            editor.UpdateTag("Ready", "oB1", PlcVariableType.Bool, PlcVariableRole.Memory);
        }
        catch (InvalidOperationException) { tagRenameRejected = true; }
        True(tagRenameRejected);
        Equal("Ready", editor.Tags.Single().Name);
    }

    private static void TestTagTableNameCanonicalization()
    {
        var document = new LadderEditorDocument();

        var scalar = document.AddTag("Ready", PlcVariableRole.Memory, tagTable: "default TAG table");
        Equal("Default tag table", scalar.TagTable);
        Equal(1, document.TagTables.Count);

        var aggregate = document.AddAggregateTag(
            "SampleArray",
            PlcVariableRole.Memory,
            PlcVariableType.Array,
            new PlcAggregateSchema(ElementType: PlcVariableType.Bool, Length: 1),
            new object[] { false },
            tagTable: "DEFAULT TAG TABLE");
        Equal("Default tag table", aggregate.TagTable);
        Equal(1, document.TagTables.Count);
        True(document.Tags.All(tag => document.TagTables.Contains(tag.TagTable, StringComparer.Ordinal)));
    }

    private static void TestEditorBlockDocumentIdentity()
    {
        var editor = LadderEditorDocument.CreateConveyorExample();
        var earlier = editor.AddBlock("Earlier_Unused", LadderBlockType.Function);
        var survivor = editor.AddBlock("Survivor", LadderBlockType.Function);
        var key = EditorDocumentIdentity.ForBlock(survivor);

        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var originalIndex));
        Equal(survivor.Id, editor.Blocks[originalIndex].Id);

        var history = new LadderEditorHistory();
        history.Execute(editor, "Rename survivor", () => editor.RenameBlock(survivor.Id, "Survivor_Renamed"));
        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var renamedIndex));
        Equal(survivor.Id, editor.Blocks[renamedIndex].Id);
        Equal("Survivor_Renamed", editor.Blocks[renamedIndex].Name);
        True(history.Undo(editor, out _));
        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var undoRenameIndex));
        Equal(survivor.Id, editor.Blocks[undoRenameIndex].Id);
        True(history.Redo(editor, out _));
        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var redoRenameIndex));
        Equal(survivor.Id, editor.Blocks[redoRenameIndex].Id);

        history.Execute(editor, "Delete earlier block", () => True(editor.TryRemoveBlock(earlier.Id, out _)));
        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var shiftedIndex));
        Equal(survivor.Id, editor.Blocks[shiftedIndex].Id);
        Equal(originalIndex - 1, shiftedIndex);
        True(history.Undo(editor, out _));
        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var undoDeleteIndex));
        Equal(survivor.Id, editor.Blocks[undoDeleteIndex].Id);
        Equal(originalIndex, undoDeleteIndex);
        True(history.Redo(editor, out _));
        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var redoDeleteIndex));
        Equal(survivor.Id, editor.Blocks[redoDeleteIndex].Id);
        Equal(originalIndex - 1, redoDeleteIndex);

        history.Execute(editor, "Delete survivor", () => True(editor.TryRemoveBlock(survivor.Id, out _)));
        False(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out _));
        True(history.Undo(editor, out _));
        True(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out var restoredIndex));
        Equal(survivor.Id, editor.Blocks[restoredIndex].Id);
        True(history.Redo(editor, out _));
        False(EditorDocumentIdentity.TryResolveBlockIndex(editor, key, out _));
    }

    private static void TestCompilerCpuSymbolUniqueness()
    {
        var program = LadderEditorDocument.CreateConveyorExample().BuildProgram();
        var firstTag = program.Variables[0];
        var duplicateTag = program with
        {
            Variables = program.Variables.Append(firstTag with { Name = firstTag.Name.ToUpperInvariant() }).ToArray(),
        };
        True(LadderCompiler.Validate(duplicateTag).Any(issue =>
            issue.Code == "VC004" && issue.Path.StartsWith("$.variables[", StringComparison.Ordinal)));

        var collidingBlock = program.Blocks![0] with { Name = firstTag.Name.ToUpperInvariant() };
        var blockCollision = program with
        {
            Blocks = program.Blocks.Select((block, index) => index == 0 ? collidingBlock : block).ToArray(),
        };
        True(LadderCompiler.Validate(blockCollision).Any(issue =>
            issue.Code == "VC004" && issue.Path.StartsWith("$.variables[", StringComparison.Ordinal)
            && issue.Message.Contains("conflicts with a block/routine", StringComparison.Ordinal)));
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

    private static void TestTiaEvidenceMatrixCoverage()
    {
        var matrixPath = Path.Combine(FindProjectRoot(), "docs", "TIA_INSTRUCTION_EVIDENCE_MATRIX.md");
        var lines = File.ReadAllLines(matrixPath);
        True(lines.Contains("| Key | TIA-facing catalog name | Claim boundary / per-key visual acceptance | Offline | Selected | Edit | Monitor |",
            StringComparer.Ordinal));
        var rows = lines
            .Where(line => line.StartsWith("| `", StringComparison.Ordinal))
            .Select(ParseEvidenceMatrixRow)
            .ToArray();
        var catalogKeys = LadderInstructionCatalog.Entries.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
        var expectedKeys = new HashSet<string>(catalogKeys, StringComparer.Ordinal)
        {
            "empty-instruction-placeholder",
        };

        Equal(expectedKeys.Count, rows.Length);
        Equal(rows.Length, rows.Select(row => row.Key).Distinct(StringComparer.Ordinal).Count());
        True(rows.Select(row => row.Key).ToHashSet(StringComparer.Ordinal).SetEquals(expectedKeys));

        foreach (var row in rows)
        {
            if (row.Cells.Length != 7)
                throw new InvalidDataException($"TIA evidence row '{row.Key}' has {row.Cells.Length} cells; expected 7.");
            True(row.Cells[1].Length > 0);
            True(row.Cells[2].Length > 0);
            for (var state = 3; state <= 6; state++)
                True(EvidenceGateStatus(row.Cells[state]) is "PASS" or "PARTIAL" or "OPEN" or "N/A");
        }

        var localKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "counter-load", "counter-reset", "compare-6", "numeric-24",
        };
        foreach (var key in localKeys)
        {
            var row = rows.Single(item => item.Key == key);
            True(row.Cells[2].Contains("local", StringComparison.OrdinalIgnoreCase));
            True(row.Cells.Skip(3).All(cell => EvidenceGateStatus(cell) == "N/A"));
        }

        var nativeRows = rows.Where(item => catalogKeys.Contains(item.Key) && !localKeys.Contains(item.Key)).ToArray();
        var missingVendorBoundary = nativeRows.Where(row =>
                !row.Cells[2].Contains("Siemens", StringComparison.OrdinalIgnoreCase)
                && !row.Cells[2].Contains("TIA", StringComparison.OrdinalIgnoreCase))
            .Select(row => row.Key).ToArray();
        if (missingVendorBoundary.Length > 0)
            throw new InvalidDataException("TIA evidence rows lack a Siemens/TIA claim boundary: "
                + string.Join(", ", missingVendorBoundary));
        foreach (var row in nativeRows)
        {
            True(row.Cells.Skip(3).Any(cell => EvidenceGateStatus(cell) is "PASS" or "PARTIAL"));
        }

        var emptyBox = rows.Single(row => row.Key == "empty-instruction-placeholder");
        Equal("N/A", EvidenceGateStatus(emptyBox.Cells[6]));
    }

    private static (string Key, string[] Cells) ParseEvidenceMatrixRow(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var inCode = false;
        for (var index = 1; index < line.Length - 1; index++)
        {
            var character = line[index];
            if (character == '`') inCode = !inCode;
            if (character == '|' && !inCode && (index == 0 || line[index - 1] != '\\'))
            {
                cells.Add(current.ToString().Trim());
                current.Clear();
            }
            else current.Append(character);
        }
        cells.Add(current.ToString().Trim());
        var values = cells.ToArray();
        if (values.Length == 0 || values[0].Length < 3 || values[0][0] != '`' || values[0][^1] != '`')
            throw new InvalidDataException("Malformed TIA evidence row: " + line);
        return (values[0][1..^1], values);
    }

    private static void TestTiaEvidenceRegistryCoverage()
    {
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }

        static string RequiredString(JsonElement element, string property, string context)
        {
            Require(element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()),
                $"{context} requires a non-empty '{property}' string.");
            return value.GetString()!;
        }

        var projectRoot = FindProjectRoot();
        var registryPath = Path.Combine(projectRoot, "docs", "tia", "evidence-registry.json");
        using var registry = JsonDocument.Parse(File.ReadAllText(registryPath));
        var root = registry.RootElement;
        Require(root.GetProperty("schemaVersion").GetInt32() == 1,
            "TIA evidence registry schemaVersion must be 1.");

        var target = root.GetProperty("target");
        Require(RequiredString(target, "vendor", "TIA evidence target") == "Siemens",
            "TIA evidence target vendor must be Siemens.");
        RequiredString(target, "product", "TIA evidence target");
        RequiredString(target, "preferredVersion", "TIA evidence target");
        RequiredString(target, "claimBoundary", "TIA evidence target");
        Require(target.GetProperty("cpuFamilies").ValueKind == JsonValueKind.Array
            && target.GetProperty("cpuFamilies").GetArrayLength() > 0,
            "TIA evidence target requires at least one CPU family.");

        var entries = root.GetProperty("entries").EnumerateArray().ToArray();
        var catalogKeys = LadderInstructionCatalog.Entries.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
        var registryKeys = entries.Select(entry => RequiredString(entry, "key", "TIA evidence entry")).ToArray();
        Require(registryKeys.Length == registryKeys.Distinct(StringComparer.Ordinal).Count(),
            "TIA evidence registry contains duplicate catalog keys.");
        Require(registryKeys.ToHashSet(StringComparer.Ordinal).SetEquals(catalogKeys),
            "TIA evidence registry keys do not exactly match LadderInstructionCatalog.");

        var localKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "counter-load", "counter-reset", "compare-6", "numeric-24",
        };
        var supportClasses = new HashSet<string>(["siemens-subset", "rungproof-local"], StringComparer.Ordinal);
        var localStatuses = new HashSet<string>(
            ["not-assessed", "automated-pass", "visual-inspected", "automated-and-visual", "not-applicable"],
            StringComparer.Ordinal);
        var vendorStatuses = new HashSet<string>(
            ["not-assessed", "source-only", "reference-capture-only", "matched", "not-required"],
            StringComparer.Ordinal);
        var gateStates = new HashSet<string>(["offline", "selected", "edit", "monitor"], StringComparer.Ordinal);
        var artifactKinds = new HashSet<string>(["image", "log", "json"], StringComparer.Ordinal);
        var durabilityValues = new HashSet<string>(["ephemeral-local", "checked-in", "external-immutable"], StringComparer.Ordinal);
        var sourceScopes = new HashSet<string>(["exact-instruction", "exact-structure", "family-reference"], StringComparer.Ordinal);
        var structuralSourceKeys = new HashSet<string>(["branch", "rung"], StringComparer.Ordinal);
        var familySourceKeys = new HashSet<string>(["compare-2", "compare-3", "compare-4", "compare-5"], StringComparer.Ordinal);
        var unresolvedSourceKeys = new HashSet<string>(StringComparer.Ordinal);
        var exactInstructionUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gateIds = new HashSet<string>(StringComparer.Ordinal);
        var globalArtifactIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            var key = RequiredString(entry, "key", "TIA evidence entry");
            var supportClass = RequiredString(entry, "supportClass", $"TIA evidence entry '{key}'");
            Require(supportClasses.Contains(supportClass),
                $"TIA evidence entry '{key}' has unsupported supportClass '{supportClass}'.");
            Require((supportClass == "rungproof-local") == localKeys.Contains(key),
                $"TIA evidence entry '{key}' has an incorrect supportClass.");

            Require(entry.TryGetProperty("source", out var sources) && sources.ValueKind == JsonValueKind.Array,
                $"TIA evidence entry '{key}' requires a source array.");
            var expectedSourceScope = structuralSourceKeys.Contains(key) ? "exact-structure"
                : familySourceKeys.Contains(key) ? "family-reference"
                : "exact-instruction";
            if (supportClass == "rungproof-local")
                Require(sources.GetArrayLength() == 0,
                    $"RungProof-local evidence entry '{key}' must not carry Siemens parity provenance.");
            else if (unresolvedSourceKeys.Contains(key))
                Require(sources.GetArrayLength() == 0,
                    $"Unresolved evidence entry '{key}' must remain source-empty until an exact matrix source is recorded.");
            else
                Require(sources.GetArrayLength() > 0,
                    $"Siemens-facing evidence entry '{key}' requires an official source.");
            foreach (var source in sources.EnumerateArray())
            {
                RequiredString(source, "title", $"TIA evidence source for '{key}'");
                var url = RequiredString(source, "url", $"TIA evidence source for '{key}'");
                Require(Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps
                    && uri.Host.Equals("docs.tia.siemens.cloud", StringComparison.OrdinalIgnoreCase),
                    $"Siemens-facing evidence source for '{key}' must use official HTTPS documentation.");
                RequiredString(source, "version", $"TIA evidence source for '{key}'");
                var sourceScope = RequiredString(source, "scope", $"TIA evidence source for '{key}'");
                Require(sourceScopes.Contains(sourceScope) && sourceScope == expectedSourceScope,
                    $"TIA evidence source for '{key}' must use explicit scope '{expectedSourceScope}', not '{sourceScope}'.");
                if (sourceScope == "exact-instruction")
                    Require(exactInstructionUrls.Add(url),
                        $"Exact instruction source '{url}' is reused; use family-reference rather than implying per-key provenance.");
            }

            if (familySourceKeys.Contains(key))
                Require(entry.GetProperty("limitations").EnumerateArray().Any(item =>
                        item.GetString()?.Contains("No per-key exact official URL", StringComparison.Ordinal) == true),
                    $"Family-only source for '{key}' requires an explicit missing per-key provenance limitation.");
            if (unresolvedSourceKeys.Contains(key))
                Require(entry.GetProperty("limitations").EnumerateArray().Any(item =>
                        item.GetString()?.Contains("No per-key exact official Siemens URL", StringComparison.Ordinal) == true),
                    $"Unresolved source for '{key}' requires an explicit provenance limitation.");

            Require(entry.TryGetProperty("artifacts", out var artifacts) && artifacts.ValueKind == JsonValueKind.Array,
                $"TIA evidence entry '{key}' requires an artifacts array.");
            var entryArtifactIds = new HashSet<string>(StringComparer.Ordinal);
            var entryArtifacts = new Dictionary<string, (string Path, string Durability, string? Sha256)>(StringComparer.Ordinal);
            foreach (var artifact in artifacts.EnumerateArray())
            {
                var artifactId = RequiredString(artifact, "id", $"TIA evidence artifact for '{key}'");
                Require(entryArtifactIds.Add(artifactId) && globalArtifactIds.Add(artifactId),
                    $"TIA evidence artifact id '{artifactId}' is not globally unique.");
                var path = RequiredString(artifact, "path", $"TIA evidence artifact '{artifactId}'");
                Require(!Path.IsPathRooted(path)
                    && !path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal),
                    $"TIA evidence artifact '{artifactId}' path must be repository-relative.");
                Require(artifactKinds.Contains(RequiredString(artifact, "kind", $"TIA evidence artifact '{artifactId}'")),
                    $"TIA evidence artifact '{artifactId}' has an unsupported kind.");
                Require(durabilityValues.Contains(RequiredString(artifact, "durability", $"TIA evidence artifact '{artifactId}'")),
                    $"TIA evidence artifact '{artifactId}' has an unsupported durability.");
                Require(artifact.TryGetProperty("sha256", out var sha256)
                    && (sha256.ValueKind == JsonValueKind.Null
                        || sha256.ValueKind == JsonValueKind.String
                        && sha256.GetString() is { Length: 64 } hash
                        && hash.All(Uri.IsHexDigit)),
                    $"TIA evidence artifact '{artifactId}' sha256 must be null or 64 hexadecimal characters.");
                var durability = artifact.GetProperty("durability").GetString()!;
                var hashValue = sha256.ValueKind == JsonValueKind.String ? sha256.GetString() : null;
                entryArtifacts.Add(artifactId, (path, durability, hashValue));
                if (path.StartsWith("docs/", StringComparison.Ordinal))
                {
                    var artifactPath = Path.Combine(projectRoot, path.Replace('/', Path.DirectorySeparatorChar));
                    Require(File.Exists(artifactPath),
                        $"TIA evidence artifact '{artifactId}' does not exist at '{path}'.");
                    Require(hashValue is not null,
                        $"Source-tree TIA evidence artifact '{artifactId}' requires a SHA-256 hash.");
                    var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(artifactPath)));
                    Require(actualHash.Equals(hashValue, StringComparison.OrdinalIgnoreCase),
                        $"TIA evidence artifact '{artifactId}' hash does not match '{path}'.");
                }
            }

            Require(entry.TryGetProperty("gates", out var gates) && gates.ValueKind == JsonValueKind.Array,
                $"TIA evidence entry '{key}' requires a gates array.");
            var entryStates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var gate in gates.EnumerateArray())
            {
                var state = RequiredString(gate, "state", $"TIA evidence gate for '{key}'");
                Require(gateStates.Contains(state) && entryStates.Add(state),
                    $"TIA evidence entry '{key}' has a duplicate or unsupported state '{state}'.");
                var gateId = RequiredString(gate, "id", $"TIA evidence gate '{key}/{state}'");
                Require(gateId == $"tia.{key}.{state}" && gateIds.Add(gateId),
                    $"TIA evidence gate id '{gateId}' is unstable or duplicated.");
                var localStatus = RequiredString(gate, "localStatus", $"TIA evidence gate '{gateId}'");
                Require(localStatuses.Contains(localStatus),
                    $"TIA evidence gate '{gateId}' has an unsupported localStatus.");
                var vendorStatus = RequiredString(gate, "vendorStatus", $"TIA evidence gate '{gateId}'");
                Require(vendorStatuses.Contains(vendorStatus),
                    $"TIA evidence gate '{gateId}' has an unsupported vendorStatus.");
                Require((supportClass == "rungproof-local") == (vendorStatus == "not-required"),
                    $"TIA evidence gate '{gateId}' has a vendorStatus inconsistent with supportClass.");
                if (unresolvedSourceKeys.Contains(key))
                    Require(vendorStatus == "not-assessed",
                        $"TIA evidence gate '{gateId}' cannot claim source evidence while its per-key source is unresolved.");
                Require(gate.TryGetProperty("artifactIds", out var artifactReferences)
                    && artifactReferences.ValueKind == JsonValueKind.Array,
                    $"TIA evidence gate '{gateId}' requires an artifactIds array.");
                var referencedArtifacts = artifactReferences.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString()! : string.Empty)
                    .ToArray();
                Require(referencedArtifacts.Length == referencedArtifacts.Distinct(StringComparer.Ordinal).Count(),
                    $"TIA evidence gate '{gateId}' contains duplicate artifact references.");
                foreach (var artifactReference in artifactReferences.EnumerateArray())
                {
                    Require(artifactReference.ValueKind == JsonValueKind.String
                        && entryArtifactIds.Contains(artifactReference.GetString()!),
                        $"TIA evidence gate '{gateId}' references an unknown artifact.");
                }

                if (supportClass == "siemens-subset")
                {
                    var localArtifactId = $"artifact.{key}.local-run.v1";
                    var reviewArtifactId = $"artifact.{key}.selected-review.v1";
                    var offlineReviewArtifactId = $"artifact.{key}.offline-review.v1";
                    var expectedStatus = state switch
                    {
                        "offline" => "automated-and-visual",
                        "selected" => "automated-and-visual",
                        "edit" => "automated-pass",
                        "monitor" => "automated-pass",
                        _ => "not-assessed",
                    };
                    var expectedArtifacts = state switch
                    {
                        "offline" => new[] { localArtifactId, offlineReviewArtifactId },
                        "selected" => new[] { localArtifactId, reviewArtifactId },
                        "edit" => new[] { localArtifactId },
                        "monitor" => new[] { localArtifactId },
                        _ => Array.Empty<string>(),
                    };
                    Require(localStatus == expectedStatus,
                        $"TIA evidence gate '{gateId}' must use evidence-backed localStatus '{expectedStatus}'.");
                    Require(referencedArtifacts.SequenceEqual(expectedArtifacts, StringComparer.Ordinal),
                        $"TIA evidence gate '{gateId}' does not reference the exact evidence for state '{state}'.");
                    var expectedVendorStatus = unresolvedSourceKeys.Contains(key) ? "not-assessed" : "source-only";
                    Require(vendorStatus == expectedVendorStatus,
                        $"TIA evidence gate '{gateId}' must preserve vendorStatus '{expectedVendorStatus}'.");
                }
                else
                {
                    Require(localStatus == "automated-pass" && referencedArtifacts.Length == 0,
                        $"RungProof-local gate '{gateId}' must remain an artifact-free automated-pass.");
                }
            }
            Require(entryStates.SetEquals(gateStates),
                $"TIA evidence entry '{key}' must contain exactly offline, selected, edit, and monitor gates.");

            if (supportClass == "siemens-subset")
            {
                var localArtifactId = $"artifact.{key}.local-run.v1";
                var reviewArtifactId = $"artifact.{key}.selected-review.v1";
                var offlineReviewArtifactId = $"artifact.{key}.offline-review.v1";
                Require(entryArtifacts.Count == 3
                    && entryArtifacts.ContainsKey(localArtifactId)
                    && entryArtifacts.ContainsKey(reviewArtifactId)
                    && entryArtifacts.ContainsKey(offlineReviewArtifactId),
                    $"Siemens-facing entry '{key}' must contain exactly the local-run, selected-review, and offline-review artifacts.");
                var localArtifact = entryArtifacts[localArtifactId];
                var reviewArtifact = entryArtifacts[reviewArtifactId];
                var offlineReviewArtifact = entryArtifacts[offlineReviewArtifactId];
                Require(localArtifact.Path == "docs/tia/tia-local-run-evidence.json"
                    && localArtifact.Durability == "ephemeral-local"
                    && localArtifact.Sha256 is not null,
                    $"Siemens-facing entry '{key}' has an invalid local-run artifact binding.");
                Require(reviewArtifact.Path == "docs/tia/tia-renderer-visual-review.json"
                    && reviewArtifact.Durability == "ephemeral-local"
                    && reviewArtifact.Sha256 is not null,
                    $"Siemens-facing entry '{key}' has an invalid selected-review artifact binding.");
                Require(offlineReviewArtifact.Path == "docs/tia/tia-renderer-unselected-visual-review.json"
                    && offlineReviewArtifact.Durability == "ephemeral-local"
                    && offlineReviewArtifact.Sha256 is not null,
                    $"Siemens-facing entry '{key}' has an invalid offline-review artifact binding.");
                Require(entryArtifacts.Values.All(artifact => !artifact.Path.StartsWith(".tools/", StringComparison.Ordinal)),
                    $"Siemens-facing entry '{key}' must not bind gates directly to volatile .tools output.");
            }
            else
            {
                Require(entryArtifacts.Count == 0,
                    $"RungProof-local entry '{key}' must remain artifact-free.");
            }

            Require(entry.TryGetProperty("limitations", out var limitations)
                && limitations.ValueKind == JsonValueKind.Array
                && limitations.GetArrayLength() > 0
                && limitations.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(item.GetString())),
                $"TIA evidence entry '{key}' requires at least one explicit limitation.");
        }
    }

    private static void TestTiaEditorControlEvidenceRegistry()
    {
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }

        var projectRoot = FindProjectRoot();
        var path = Path.Combine(projectRoot, "docs", "tia", "editor-control-evidence-registry.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Require(root.GetProperty("schemaVersion").GetInt32() == 1
            && root.GetProperty("resultKind").GetString() == "tia-editor-control-evidence-registry",
            "TIA editor-control evidence registry identity is invalid.");

        var expectedEditor = new HashSet<string>(StringComparer.Ordinal)
        {
            "tia.editor.contact-shortcuts", "tia.editor.operand-shortcuts",
            "tia.editor.transaction-resolution", "tia.editor.command-integrity",
            "tia.editor.view-controls", "tia.editor.block-view-state",
            "tia.editor.empty-placeholder",
        };
        var expectedProjectTree = new HashSet<string>(StringComparer.Ordinal)
        {
            "tia.project-tree.activation-semantics", "tia.project-tree.visible-form-cancel-actions",
            "tia.project-tree.show-all-tags",
            "tia.project-tree.create-block", "tia.project-tree.create-tag-table",
            "tia.project-tree.create-watch-table", "tia.project-tree.global-db-contract",
            "tia.project-tree.instance-db-contract", "tia.project-tree.call-drag-drop",
            "tia.project-tree.watch-table-delete", "tia.project-tree.watch-symbol-remove",
            "tia.project-tree.watch-stale-symbol-visible",
        };
        var gates = root.GetProperty("gates").EnumerateArray().ToArray();
        var ids = gates.Select(gate => gate.GetProperty("id").GetString()!).ToArray();
        Require(gates.Length == 19 && ids.Distinct(StringComparer.Ordinal).Count() == 19,
            "TIA editor-control registry must contain exactly 19 unique gates.");
        Require(gates.Where(gate => gate.GetProperty("group").GetString() == "editor")
                .Select(gate => gate.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedEditor)
            && gates.Where(gate => gate.GetProperty("group").GetString() == "project-tree")
                .Select(gate => gate.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedProjectTree),
            "TIA editor-control gate IDs do not match the exact editor/project-tree contract.");
        Require(gates.All(gate => gate.GetProperty("localStatus").GetString() == "automated-pass"
                && gate.GetProperty("evidenceArtifactIds").GetArrayLength() == 1
                && gate.GetProperty("requiredAssertions").GetArrayLength() > 0
                && gate.GetProperty("limitations").GetArrayLength() > 0),
            "TIA editor-control gates must bind exactly one passing structured artifact.");
        var tagTableGate = gates.Single(gate => gate.GetProperty("id").GetString() == "tia.project-tree.create-tag-table");
        var tagTableRequirements = tagTableGate.GetProperty("requiredAssertions").EnumerateArray()
            .Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
        Require(tagTableRequirements.IsSupersetOf([
            "the tag properties editor assigns a new tag to the selected table",
            "renaming through the table editor preserves membership and remains consistent through Undo/Redo and Save/Load",
            "the table editor deletes only an empty table and Undo/Redo restores exact state",
        ]), "TIA tag-table evidence contract omits membership or table lifecycle behavior.");
        foreach (var gate in gates)
        foreach (var property in new[] { "implementationPaths", "testPaths", "documentationPaths" })
        foreach (var item in gate.GetProperty(property).EnumerateArray())
            Require(File.Exists(Path.Combine(projectRoot,
                    item.GetString()!.Replace('/', Path.DirectorySeparatorChar))),
                $"TIA editor-control gate '{gate.GetProperty("id").GetString()}' references a missing {property} path.");

        var contracts = root.GetProperty("structuredSweepContracts").EnumerateArray().ToArray();
        Require(contracts.Length == 2 && contracts.All(contract => contract.GetProperty("implemented").GetBoolean()
                && !string.IsNullOrWhiteSpace(contract.GetProperty("latestArtifactId").GetString())),
            "TIA editor-control structured sweeps must be implemented and bind their latest artifacts.");
        Require(contracts.Single(contract => contract.GetProperty("commandId").GetString() == "tia-editor-control-gate-sweep")
                .GetProperty("gateIds").EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedEditor)
            && contracts.Single(contract => contract.GetProperty("commandId").GetString() == "tia-project-tree-gate-sweep")
                .GetProperty("gateIds").EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal)
                .SetEquals(expectedProjectTree),
            "TIA editor-control structured sweep contracts do not exactly partition the 19 gates.");

        var artifacts = root.GetProperty("evidenceArtifacts").EnumerateArray().ToArray();
        Require(artifacts.Length == 2
            && artifacts.Select(artifact => artifact.GetProperty("id").GetString()).ToHashSet(StringComparer.Ordinal).SetEquals([
                "artifact.tia-editor-control-gate-sweep.v1", "artifact.tia-project-tree-gate-sweep.v1",
            ])
            && artifacts.All(artifact => artifact.GetProperty("status").GetString() == "passed"
                && artifact.GetProperty("externalTia").ValueKind == JsonValueKind.False
                && artifact.GetProperty("livePlc").ValueKind == JsonValueKind.False
                && artifact.GetProperty("sha256").GetString() is { Length: 64 } hash && hash.All(Uri.IsHexDigit)),
            "TIA editor-control evidence artifacts are incomplete or have invalid execution boundaries.");
        var editorArtifactId = artifacts.Single(artifact => artifact.GetProperty("commandId").GetString()
            == "tia-editor-control-gate-sweep").GetProperty("id").GetString();
        var treeArtifactId = artifacts.Single(artifact => artifact.GetProperty("commandId").GetString()
            == "tia-project-tree-gate-sweep").GetProperty("id").GetString();
        Require(gates.Where(gate => gate.GetProperty("group").GetString() == "editor")
                .All(gate => gate.GetProperty("evidenceArtifactIds")[0].GetString() == editorArtifactId)
            && gates.Where(gate => gate.GetProperty("group").GetString() == "project-tree")
                .All(gate => gate.GetProperty("evidenceArtifactIds")[0].GetString() == treeArtifactId),
            "TIA editor-control gates are not bound to their group-specific structured artifacts.");

        var unsupported = root.GetProperty("unsupportedDocumented").EnumerateArray().ToArray();
        Require(unsupported.Length == 2
            && unsupported.All(item => item.GetProperty("status").GetString() == "documented-unsupported")
            && unsupported.Select(item => item.GetProperty("id").GetString()).ToHashSet(StringComparer.Ordinal).SetEquals([
                "tia.unsupported.watch-table-rename", "tia.unsupported.project-tree-watch-table-delete",
            ]),
            "TIA editor-control unsupported-operation boundary is incomplete.");
    }

    private static void TestTiaLocalRunEvidenceRecord()
    {
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }

        static string RequiredString(JsonElement element, string property, string context)
        {
            Require(element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()),
                $"{context} requires a non-empty '{property}' string.");
            return value.GetString()!;
        }

        static void RequireRepositoryRelative(string path, string context)
        {
            Require(!Path.IsPathRooted(path)
                && !path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal),
                $"{context} must be repository-relative.");
        }

        static void RequireSha256(string hash, string context)
        {
            Require(hash.Length == 64 && hash.All(Uri.IsHexDigit),
                $"{context} must be a 64-character hexadecimal SHA-256 hash.");
        }

        var projectRoot = FindProjectRoot();
        var evidencePath = Path.Combine(projectRoot, "docs", "tia", "tia-local-run-evidence.json");
        using var evidence = JsonDocument.Parse(File.ReadAllText(evidencePath));
        var root = evidence.RootElement;
        Require(root.GetProperty("schemaVersion").GetInt32() == 1,
            "TIA local run evidence schemaVersion must be 1.");
        Require(RequiredString(root, "resultKind", "TIA local run evidence") == "tia-local-run-evidence"
            && RequiredString(root, "status", "TIA local run evidence") == "historical-pass"
            && RequiredString(root, "profile", "TIA local run evidence") == "full"
            && !root.GetProperty("currentSourceVerified").GetBoolean()
            && !string.IsNullOrWhiteSpace(RequiredString(root, "supersededReason", "TIA local run evidence")),
            "TIA full-profile evidence must be clearly marked as historical when it does not prove the current source.");

        var sourceRun = root.GetProperty("sourceRun");
        var sourceRunPath = RequiredString(sourceRun, "path", "TIA local run source");
        RequireRepositoryRelative(sourceRunPath, "TIA local run source path");
        Require(sourceRunPath.StartsWith(".tools/", StringComparison.Ordinal)
            && RequiredString(sourceRun, "durability", "TIA local run source") == "ephemeral-local",
            "TIA local run source must preserve its volatile .tools boundary.");
        RequireSha256(RequiredString(sourceRun, "sha256", "TIA local run source"), "TIA local run source hash");

        var boundary = root.GetProperty("evidenceBoundary");
        Require(boundary.GetProperty("offlineOnly").GetBoolean()
            && !boundary.GetProperty("installedTiaExecuted").GetBoolean()
            && !boundary.GetProperty("livePlcExecuted").GetBoolean(),
            "TIA local run evidence must remain explicitly offline-only.");

        var commands = root.GetProperty("commands").EnumerateArray().ToArray();
        Require(commands.Length == 5,
            "TIA local run evidence must bind the renderer, edit, monitor, editor-control, and project-tree commands.");
        var rendererCommand = commands.Single(command =>
            RequiredString(command, "id", "TIA local run command") == "tia-renderer-contact-sheets");
        var monitorCommand = commands.Single(command =>
            RequiredString(command, "id", "TIA local run command") == "tia-monitor-gate-sweep");
        var editCommand = commands.Single(command =>
            RequiredString(command, "id", "TIA local run command") == "tia-edit-gate-sweep");
        var editorControlCommand = commands.Single(command =>
            RequiredString(command, "id", "TIA local run command") == "tia-editor-control-gate-sweep");
        var projectTreeCommand = commands.Single(command =>
            RequiredString(command, "id", "TIA local run command") == "tia-project-tree-gate-sweep");
        Require(RequiredString(rendererCommand, "status", "TIA renderer command") == "passed"
            && RequiredString(monitorCommand, "status", "TIA monitor command") == "passed"
            && RequiredString(editCommand, "status", "TIA edit command") == "passed"
            && RequiredString(editorControlCommand, "status", "TIA editor-control command") == "passed"
            && RequiredString(projectTreeCommand, "status", "TIA project-tree command") == "passed",
            "TIA local run evidence commands must all be passing.");
        var assertions = rendererCommand.GetProperty("stableAssertions");
        Require(assertions.GetProperty("catalogKeys").GetInt32() == 50
            && assertions.GetProperty("rendererCases").GetInt32() == 900
            && assertions.GetProperty("displayModesPerStateAndZoom").GetInt32() == 3
            && assertions.GetProperty("selectedDetails").GetInt32() == 150
            && assertions.GetProperty("contactSheets").GetInt32() == 24,
            "TIA local run renderer counts do not match the reviewed catalog sweep.");
        Require(assertions.GetProperty("selectionStates").EnumerateArray().Select(item => item.GetString()).ToHashSet()
                .SetEquals(["unselected", "selected"])
            && assertions.GetProperty("zoomLevels").EnumerateArray().Select(item => item.GetInt32()).ToHashSet()
                .SetEquals([75, 100, 150]),
            "TIA local run renderer states or zoom levels are incomplete.");
        var monitorAssertions = monitorCommand.GetProperty("stableAssertions");
        Require(monitorAssertions.GetProperty("catalogKeys").GetInt32() == 50
            && monitorAssertions.GetProperty("monitorCases").GetInt32() == 200
            && monitorAssertions.GetProperty("gateIds").GetInt32() == 50,
            "TIA local run monitor counts do not match the exact catalog sweep.");
        Require(monitorAssertions.GetProperty("states").EnumerateArray()
                .Select(item => item.GetString()).ToHashSet()
                .SetEquals(["evaluated-false", "evaluated-true", "unknown-earlier-cycle", "never-executed"]),
            "TIA local run monitor states are incomplete.");
        var editAssertions = editCommand.GetProperty("stableAssertions");
        Require(editAssertions.GetProperty("catalogKeys").GetInt32() == 50
            && editAssertions.GetProperty("editCases").GetInt32() == 50
            && editAssertions.GetProperty("gateIds").GetInt32() == 50
            && editAssertions.GetProperty("validatedForms").GetInt32() == 49,
            "TIA local run edit counts do not match the exact catalog sweep.");
        Require(editAssertions.GetProperty("structuralCasesWithoutMalformedOperand").EnumerateArray()
                .Select(item => item.GetString()).SequenceEqual(["rung"]),
            "TIA local run edit evidence must identify the one structural case without a malformed operand.");
        var editorControlAssertions = editorControlCommand.GetProperty("stableAssertions");
        var projectTreeAssertions = projectTreeCommand.GetProperty("stableAssertions");
        Require(editorControlAssertions.GetProperty("gateIds").GetInt32() == 7
            && editorControlAssertions.GetProperty("assertions").GetInt32() == 36
            && editorControlAssertions.GetProperty("structuredPerGateResults").GetBoolean()
            && !editorControlAssertions.GetProperty("externalTia").GetBoolean()
            && !editorControlAssertions.GetProperty("livePlc").GetBoolean()
            && projectTreeAssertions.GetProperty("gateIds").GetInt32() == 11
            && projectTreeAssertions.GetProperty("assertions").GetInt32() == 75
            && projectTreeAssertions.GetProperty("structuredPerGateResults").GetBoolean()
            && projectTreeAssertions.GetProperty("nativePointerGeometry").GetBoolean()
            && !projectTreeAssertions.GetProperty("externalTia").GetBoolean()
            && !projectTreeAssertions.GetProperty("livePlc").GetBoolean(),
            "TIA local run editor/project-tree command counts or boundaries are incomplete.");

        var localKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "counter-load", "counter-reset", "compare-6", "numeric-24",
        };
        var expectedKeys = LadderInstructionCatalog.Entries.Select(entry => entry.Key)
            .Where(key => !localKeys.Contains(key)).ToHashSet(StringComparer.Ordinal);
        var coverage = root.GetProperty("gateCoverage");
        Require(RequiredString(coverage, "keyPattern", "TIA local run gate coverage") == "tia.{key}.{state}",
            "TIA local run gate identity pattern is unstable.");
        var recordedKeys = coverage.GetProperty("keys").EnumerateArray().Select(item => item.GetString()!).ToArray();
        Require(recordedKeys.Length == 50
            && recordedKeys.Length == recordedKeys.Distinct(StringComparer.Ordinal).Count()
            && recordedKeys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedKeys),
            "TIA local run gate keys must exactly match the 50 Siemens-facing catalog keys.");
        var states = coverage.GetProperty("states");
        Require(states.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
                .SetEquals(["offline", "selected", "edit", "monitor"]),
            "TIA local run gate coverage must contain all four local gate states.");
        var offline = states.GetProperty("offline");
        var selected = states.GetProperty("selected");
        var edit = states.GetProperty("edit");
        var monitor = states.GetProperty("monitor");
        Require(RequiredString(offline, "localStatus", "TIA offline coverage") == "automated-and-visual"
            && offline.GetProperty("visualReview").GetBoolean(),
            "TIA offline gate coverage must carry the unselected visual-review boundary.");
        Require(RequiredString(selected, "localStatus", "TIA selected coverage") == "automated-and-visual"
            && selected.GetProperty("visualReview").GetBoolean(),
            "TIA selected gate coverage must carry the visual-review boundary.");
        Require(RequiredString(monitor, "localStatus", "TIA monitor coverage") == "automated-pass"
            && !monitor.GetProperty("visualReview").GetBoolean(),
            "TIA monitor gate coverage must remain automated-only.");
        Require(RequiredString(edit, "localStatus", "TIA edit coverage") == "automated-pass"
            && !edit.GetProperty("visualReview").GetBoolean(),
            "TIA edit gate coverage must remain automated-only.");
        Require(RequiredString(offline, "commandId", "TIA offline coverage") == "tia-renderer-contact-sheets"
            && RequiredString(selected, "commandId", "TIA selected coverage") == "tia-renderer-contact-sheets"
            && RequiredString(edit, "commandId", "TIA edit coverage") == "tia-edit-gate-sweep"
            && RequiredString(monitor, "commandId", "TIA monitor coverage") == "tia-monitor-gate-sweep",
            "TIA local run gate states must bind their exact renderer or monitor command.");

        var registryPath = Path.Combine(projectRoot, "docs", "tia", "evidence-registry.json");
        using var registry = JsonDocument.Parse(File.ReadAllText(registryPath));
        var registryGateIds = registry.RootElement.GetProperty("entries").EnumerateArray()
            .SelectMany(entry => entry.GetProperty("gates").EnumerateArray())
            .Select(gate => gate.GetProperty("id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var impliedGateIds = recordedKeys.SelectMany(key => new[]
        {
            $"tia.{key}.offline", $"tia.{key}.selected", $"tia.{key}.edit", $"tia.{key}.monitor",
        }).ToArray();
        Require(impliedGateIds.Length == 200 && impliedGateIds.All(registryGateIds.Contains),
            "TIA local run evidence does not resolve to exactly 200 registry gates.");

        var monitorArtifact = root.GetProperty("monitorArtifact");
        var monitorArtifactPath = RequiredString(monitorArtifact, "path", "TIA monitor artifact");
        RequireRepositoryRelative(monitorArtifactPath, "TIA monitor artifact path");
        Require(monitorArtifactPath.StartsWith(".tools/", StringComparison.Ordinal)
            && RequiredString(monitorArtifact, "durability", "TIA monitor artifact") == "ephemeral-local"
            && RequiredString(monitorArtifact, "status", "TIA monitor artifact") == "passed",
            "TIA monitor artifact must preserve its passing, volatile local boundary.");
        var monitorArtifactHash = RequiredString(monitorArtifact, "sha256", "TIA monitor artifact");
        RequireSha256(monitorArtifactHash, "TIA monitor artifact hash");
        var actualMonitorArtifactPath = Path.Combine(
            projectRoot,
            monitorArtifactPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(actualMonitorArtifactPath))
        {
            Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(actualMonitorArtifactPath)))
                    .Equals(monitorArtifactHash, StringComparison.OrdinalIgnoreCase),
                "TIA monitor artifact hash does not match the recorded local artifact.");
            using var monitorEvidence = JsonDocument.Parse(File.ReadAllText(actualMonitorArtifactPath));
            var monitorRoot = monitorEvidence.RootElement;
            Require(RequiredString(monitorRoot, "status", "TIA raw monitor evidence") == "passed"
                && monitorRoot.GetProperty("offlineOnly").GetBoolean()
                && !monitorRoot.GetProperty("installedTiaExecuted").GetBoolean()
                && !monitorRoot.GetProperty("livePlcExecuted").GetBoolean(),
                "TIA raw monitor evidence must remain a passing offline-only run.");
            Require(monitorRoot.GetProperty("keys").GetArrayLength() == 50
                && monitorRoot.GetProperty("caseCount").GetInt32() == 200
                && monitorRoot.GetProperty("states").EnumerateArray()
                    .Select(item => item.GetString()).ToHashSet()
                    .SetEquals(["evaluated-false", "evaluated-true", "unknown-earlier-cycle", "never-executed"]),
                "TIA raw monitor evidence counts or states are incomplete.");
            var rawCases = monitorRoot.GetProperty("cases").EnumerateArray().ToArray();
            Require(rawCases.Length == 200
                && rawCases.Select(item => $"{item.GetProperty("key").GetString()}|{item.GetProperty("state").GetString()}")
                    .Distinct(StringComparer.Ordinal).Count() == 200,
                "TIA raw monitor evidence must contain 200 unique key/state pairs.");
        }

        var editArtifact = root.GetProperty("editArtifact");
        var editArtifactPath = RequiredString(editArtifact, "path", "TIA edit artifact");
        RequireRepositoryRelative(editArtifactPath, "TIA edit artifact path");
        Require(editArtifactPath.StartsWith(".tools/", StringComparison.Ordinal)
            && RequiredString(editArtifact, "durability", "TIA edit artifact") == "ephemeral-local"
            && RequiredString(editArtifact, "status", "TIA edit artifact") == "passed",
            "TIA edit artifact must preserve its passing, volatile local boundary.");
        var editArtifactHash = RequiredString(editArtifact, "sha256", "TIA edit artifact");
        RequireSha256(editArtifactHash, "TIA edit artifact hash");
        var actualEditArtifactPath = Path.Combine(projectRoot, editArtifactPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(actualEditArtifactPath))
        {
            Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(actualEditArtifactPath)))
                    .Equals(editArtifactHash, StringComparison.OrdinalIgnoreCase),
                "TIA edit artifact hash does not match the recorded local artifact.");
            using var editEvidence = JsonDocument.Parse(File.ReadAllText(actualEditArtifactPath));
            var editRoot = editEvidence.RootElement;
            Require(RequiredString(editRoot, "status", "TIA raw edit evidence") == "passed"
                && editRoot.GetProperty("offlineOnly").GetBoolean()
                && !editRoot.GetProperty("installedTiaExecuted").GetBoolean()
                && !editRoot.GetProperty("livePlcExecuted").GetBoolean(),
                "TIA raw edit evidence must remain a passing offline-only run.");
            var editCases = editRoot.GetProperty("cases").EnumerateArray().ToArray();
            Require(editRoot.GetProperty("keyCount").GetInt32() == 50
                && editRoot.GetProperty("caseCount").GetInt32() == 50
                && editCases.Length == 50
                && editCases.Select(item => item.GetProperty("key").GetString()).Distinct(StringComparer.Ordinal).Count() == 50
                && editCases.Select(item => item.GetProperty("gateId").GetString()).Distinct(StringComparer.Ordinal).Count() == 50,
                "TIA raw edit evidence must contain 50 unique key/gate records.");
            Require(editCases.All(item => item.GetProperty("status").GetString() == "passed"
                    && item.GetProperty("opened").GetBoolean()
                    && item.GetProperty("applied").GetBoolean()
                    && item.GetProperty("stableIdentity").GetBoolean()
                    && item.GetProperty("compiled").GetBoolean()
                    && item.GetProperty("saveLoadSaveExact").GetBoolean()
                    && item.GetProperty("undoExact").GetBoolean()
                    && item.GetProperty("redoExact").GetBoolean()
                    && item.GetProperty("reopened").GetBoolean()
                    && item.GetProperty("cleanupExact").GetBoolean()),
                "TIA raw edit evidence has an incomplete lifecycle case.");
            Require(editCases.Count(item => item.GetProperty("invalidMode").GetString() == "validated-form"
                    && item.GetProperty("invalidApplicable").GetBoolean()
                    && item.GetProperty("invalidRejected").GetBoolean()) == 49
                && editCases.Count(item => item.GetProperty("invalidMode").GetString() == "not-representable"
                    && !item.GetProperty("invalidApplicable").GetBoolean()
                    && item.GetProperty("invalidRejected").ValueKind == JsonValueKind.Null) == 1,
                "TIA raw edit evidence must preserve the exact invalid-input applicability boundary.");
            Require(editRoot.GetProperty("physicalInputRoutes").EnumerateArray()
                    .Select(item => item.GetString()).ToHashSet(StringComparer.Ordinal)
                    .SetEquals(["Viewport.PushInput", "Window.PushInput:Enter"]),
                "TIA raw edit evidence must enumerate both production viewport and detached-window input routes.");
        }

        foreach (var expected in new[]
        {
            (Property: "editorControlArtifact", Kind: "tia-editor-control-gate-sweep", Gates: 7, Assertions: 36,
                Status: "historical-pass", CurrentSource: false),
            (Property: "projectTreeArtifact", Kind: "tia-project-tree-gate-sweep", Gates: 12, Assertions: 84,
                Status: "passed", CurrentSource: true),
        })
        {
            var artifact = root.GetProperty(expected.Property);
            var artifactPath = RequiredString(artifact, "path", expected.Kind);
            RequireRepositoryRelative(artifactPath, expected.Kind + " path");
            var artifactHash = RequiredString(artifact, "sha256", expected.Kind);
            RequireSha256(artifactHash, expected.Kind + " hash");
            Require(artifactPath.StartsWith(".tools/", StringComparison.Ordinal)
                && RequiredString(artifact, "durability", expected.Kind) == "ephemeral-local"
                && RequiredString(artifact, "status", expected.Kind) == expected.Status
                && artifact.GetProperty("gateCount").GetInt32() == expected.Gates
                && artifact.GetProperty("assertionCount").GetInt32() == expected.Assertions
                && artifact.GetProperty("currentSource").GetBoolean() == expected.CurrentSource,
                $"{expected.Kind} summary is incomplete.");
            var actualArtifactPath = Path.Combine(projectRoot, artifactPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(actualArtifactPath)) continue;
            Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(actualArtifactPath)))
                    .Equals(artifactHash, StringComparison.OrdinalIgnoreCase),
                $"{expected.Kind} hash does not match its recorded local artifact.");
            using var rawArtifact = JsonDocument.Parse(File.ReadAllText(actualArtifactPath));
            var raw = rawArtifact.RootElement;
            var rawGates = raw.GetProperty("gates").EnumerateArray().ToArray();
            Require(RequiredString(raw, "resultKind", expected.Kind) == expected.Kind
                && RequiredString(raw, "status", expected.Kind) == "passed"
                && !raw.GetProperty("externalTia").GetBoolean()
                && !raw.GetProperty("livePlc").GetBoolean()
                && rawGates.Length == expected.Gates
                && rawGates.Sum(gate => gate.GetProperty("assertions").GetArrayLength()) == expected.Assertions
                && rawGates.All(gate => gate.GetProperty("status").GetString() == "passed"
                    && !gate.GetProperty("externalTia").GetBoolean()
                    && !gate.GetProperty("livePlc").GetBoolean()
                    && gate.GetProperty("assertions").EnumerateArray().All(assertion =>
                        assertion.GetProperty("status").GetString() == "passed"
                        && assertion.GetProperty("passed").GetBoolean())),
                $"{expected.Kind} raw artifact has incomplete or failed per-gate evidence.");
        }

        var review = root.GetProperty("selectedVisualReview");
        var reviewPath = RequiredString(review, "path", "TIA selected visual review");
        RequireRepositoryRelative(reviewPath, "TIA selected visual review path");
        var reviewHash = RequiredString(review, "sha256", "TIA selected visual review");
        RequireSha256(reviewHash, "TIA selected visual review hash");
        var actualReviewPath = Path.Combine(projectRoot, reviewPath.Replace('/', Path.DirectorySeparatorChar));
        Require(File.Exists(actualReviewPath), "TIA selected visual review file is missing.");
        Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(actualReviewPath)))
                .Equals(reviewHash, StringComparison.OrdinalIgnoreCase)
            && RequiredString(review, "status", "TIA selected visual review") == "visual-pass",
            "TIA selected visual review hash or status is invalid.");

        var artifactSet = root.GetProperty("selectedArtifactSet");
        Require(artifactSet.GetProperty("entryCount").GetInt32() == 174,
            "TIA selected visual artifact set must contain 174 reviewed images.");
        RequireSha256(RequiredString(artifactSet, "sha256", "TIA selected artifact set"),
            "TIA selected artifact-set hash");
        RequiredString(artifactSet, "canonicalization", "TIA selected artifact set");

        var offlineReview = root.GetProperty("unselectedVisualReview");
        var offlineReviewPath = RequiredString(offlineReview, "path", "TIA unselected visual review");
        RequireRepositoryRelative(offlineReviewPath, "TIA unselected visual review path");
        var offlineReviewHash = RequiredString(offlineReview, "sha256", "TIA unselected visual review");
        RequireSha256(offlineReviewHash, "TIA unselected visual review hash");
        var actualOfflineReviewPath = Path.Combine(projectRoot,
            offlineReviewPath.Replace('/', Path.DirectorySeparatorChar));
        Require(File.Exists(actualOfflineReviewPath)
            && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(actualOfflineReviewPath)))
                .Equals(offlineReviewHash, StringComparison.OrdinalIgnoreCase)
            && RequiredString(offlineReview, "status", "TIA unselected visual review") == "visual-pass",
            "TIA unselected visual review hash or status is invalid.");

        var offlineArtifactSet = root.GetProperty("unselectedArtifactSet");
        Require(offlineArtifactSet.GetProperty("entryCount").GetInt32() == 174,
            "TIA unselected visual artifact set must contain 174 reviewed images.");
        RequireSha256(RequiredString(offlineArtifactSet, "sha256", "TIA unselected artifact set"),
            "TIA unselected artifact-set hash");
        RequiredString(offlineArtifactSet, "canonicalization", "TIA unselected artifact set");

        var unprovedStates = root.GetProperty("unprovedGateStates").EnumerateArray()
            .Select(item => item.GetString()).ToHashSet();
        Require(unprovedStates.Count == 0,
            "TIA local run evidence must have no unproved local state after the exact edit and monitor sweeps.");
        Require(root.GetProperty("limitations").EnumerateArray().Any(item =>
                item.GetString()?.Contains("No installed TIA Portal", StringComparison.OrdinalIgnoreCase) == true),
            "TIA local run evidence must explicitly deny installed-TIA execution.");
    }

    private static void TestTiaRendererVisualReviewRecord()
    {
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }

        static string RequiredString(JsonElement element, string property, string context)
        {
            Require(element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()),
                $"{context} requires a non-empty '{property}' string.");
            return value.GetString()!;
        }

        var reviewPath = Path.Combine(FindProjectRoot(), "docs", "tia", "tia-renderer-visual-review.json");
        using var review = JsonDocument.Parse(File.ReadAllText(reviewPath));
        var root = review.RootElement;
        Require(root.GetProperty("schemaVersion").GetInt32() == 1,
            "TIA renderer visual review schemaVersion must be 1.");
        Require(RequiredString(root, "resultKind", "TIA renderer visual review") == "tia-renderer-visual-review",
            "TIA renderer visual review has an unexpected resultKind.");
        Require(RequiredString(root, "reviewStatus", "TIA renderer visual review") == "visual-pass",
            "TIA renderer visual review must fail closed until every declared artifact is inspected.");
        Require(DateTimeOffset.TryParse(RequiredString(root, "reviewedUtc", "TIA renderer visual review"), out _),
            "TIA renderer visual review reviewedUtc is invalid.");

        var source = root.GetProperty("sourceManifest");
        var sourcePath = RequiredString(source, "path", "TIA renderer visual review source manifest");
        Require(!Path.IsPathRooted(sourcePath)
            && !sourcePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal),
            "TIA renderer visual review source manifest path must be repository-relative.");
        var sourceHash = RequiredString(source, "sha256", "TIA renderer visual review source manifest");
        Require(sourceHash.Length == 64 && sourceHash.All(Uri.IsHexDigit),
            "TIA renderer visual review source manifest sha256 must be 64 hexadecimal characters.");
        Require(RequiredString(source, "artifactDurability", "TIA renderer visual review source manifest") == "ephemeral-local",
            "Ignored .tools renderer pixels must remain explicitly ephemeral-local.");
        Require(RequiredString(source, "generatedHumanReviewStatus", "TIA renderer visual review source manifest") == "pending",
            "The generated manifest must remain fail-safe pending; the separate review record carries the human verdict.");
        Require(source.GetProperty("keyCount").GetInt32() == 50
            && source.GetProperty("detailCount").GetInt32() == 150
            && source.GetProperty("sheetCount").GetInt32() == 24,
            "TIA renderer visual review source counts must be 50 keys, 150 details, and 24 sheets.");

        var scope = root.GetProperty("scope");
        Require(scope.GetProperty("selected").GetBoolean(),
            "TIA renderer visual review must cover selected-state captures.");
        Require(RequiredString(scope, "operandRepresentation", "TIA renderer visual review scope") == "SymbolicAndAbsolute"
            && RequiredString(scope, "tagInformationDisplay", "TIA renderer visual review scope") == "Show"
            && RequiredString(scope, "tagInformationPosition", "TIA renderer visual review scope") == "OnOperands",
            "TIA renderer visual review scope does not match the generated capture state.");
        var zooms = scope.GetProperty("zoomLevels").EnumerateArray().Select(item => item.GetInt32()).ToHashSet();
        Require(zooms.SetEquals([75, 100, 150]),
            "TIA renderer visual review must cover exactly 75, 100, and 150 percent zoom.");

        var localKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "counter-load", "counter-reset", "compare-6", "numeric-24",
        };
        var expectedKeys = LadderInstructionCatalog.Entries.Select(entry => entry.Key)
            .Where(key => !localKeys.Contains(key)).ToHashSet(StringComparer.Ordinal);
        var reviewedKeys = root.GetProperty("reviewedKeys").EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty).ToArray();
        Require(reviewedKeys.Length == reviewedKeys.Distinct(StringComparer.Ordinal).Count()
            && reviewedKeys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedKeys),
            "TIA renderer visual review keys must exactly match the 50 Siemens-facing catalog keys.");

        var details = root.GetProperty("detailReview");
        Require(details.GetProperty("count").GetInt32() == 150
            && RequiredString(details, "status", "TIA renderer detail review") == "visual-pass",
            "TIA renderer visual review must record 150 passing detail inspections.");
        var sheets = root.GetProperty("sheetReview");
        Require(sheets.GetProperty("count").GetInt32() == 24
            && RequiredString(sheets, "status", "TIA renderer sheet review") == "visual-pass",
            "TIA renderer visual review must record 24 passing contact-sheet inspections.");

        var boundary = root.GetProperty("evidenceBoundary");
        Require(boundary.GetProperty("offlineOnly").GetBoolean()
            && !boundary.GetProperty("installedTiaExecuted").GetBoolean()
            && !boundary.GetProperty("livePlcExecuted").GetBoolean(),
            "TIA renderer visual review must preserve its offline-only evidence boundary.");
        Require(root.GetProperty("limitations").EnumerateArray().Any(item =>
                item.GetString()?.Contains("installed TIA", StringComparison.OrdinalIgnoreCase) == true),
            "TIA renderer visual review must explicitly deny installed-TIA equivalence.");
    }

    private static void TestTiaRendererUnselectedVisualReviewRecord()
    {
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }

        static string RequiredString(JsonElement element, string property, string context)
        {
            Require(element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()),
                $"{context} requires a non-empty '{property}' string.");
            return value.GetString()!;
        }

        var projectRoot = FindProjectRoot();
        var reviewPath = Path.Combine(projectRoot, "docs", "tia", "tia-renderer-unselected-visual-review.json");
        using var review = JsonDocument.Parse(File.ReadAllText(reviewPath));
        var root = review.RootElement;
        Require(root.GetProperty("schemaVersion").GetInt32() == 1
            && RequiredString(root, "resultKind", "TIA unselected renderer visual review")
                == "tia-renderer-unselected-visual-review"
            && RequiredString(root, "reviewStatus", "TIA unselected renderer visual review") == "visual-pass",
            "TIA unselected renderer visual review must be a passing schema-1 record.");
        Require(DateTimeOffset.TryParse(RequiredString(root, "reviewedUtc", "TIA unselected renderer visual review"), out _),
            "TIA unselected renderer visual review reviewedUtc is invalid.");

        var source = root.GetProperty("sourceManifest");
        var sourcePath = RequiredString(source, "path", "TIA unselected renderer source manifest");
        Require(!Path.IsPathRooted(sourcePath)
            && !sourcePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).Contains("..", StringComparer.Ordinal)
            && sourcePath.StartsWith(".tools/", StringComparison.Ordinal),
            "TIA unselected renderer source manifest must be a repository-relative ephemeral path.");
        var sourceHash = RequiredString(source, "sha256", "TIA unselected renderer source manifest");
        Require(sourceHash.Length == 64 && sourceHash.All(Uri.IsHexDigit)
            && RequiredString(source, "artifactDurability", "TIA unselected renderer source manifest") == "ephemeral-local"
            && RequiredString(source, "generatedHumanReviewStatus", "TIA unselected renderer source manifest") == "pending",
            "TIA unselected renderer source manifest provenance is invalid.");
        Require(source.GetProperty("keyCount").GetInt32() == 50
            && source.GetProperty("detailCount").GetInt32() == 150
            && source.GetProperty("sheetCount").GetInt32() == 24,
            "TIA unselected renderer source counts must be 50 keys, 150 details, and 24 sheets.");

        var scope = root.GetProperty("scope");
        Require(!scope.GetProperty("selected").GetBoolean()
            && RequiredString(scope, "operandRepresentation", "TIA unselected renderer scope") == "SymbolicAndAbsolute"
            && RequiredString(scope, "tagInformationDisplay", "TIA unselected renderer scope") == "Show"
            && RequiredString(scope, "tagInformationPosition", "TIA unselected renderer scope") == "OnOperands"
            && scope.GetProperty("zoomLevels").EnumerateArray().Select(item => item.GetInt32()).ToHashSet()
                .SetEquals([75, 100, 150]),
            "TIA unselected renderer visual review scope is incomplete or selected-state contaminated.");

        var localKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "counter-load", "counter-reset", "compare-6", "numeric-24",
        };
        var expectedKeys = LadderInstructionCatalog.Entries.Select(entry => entry.Key)
            .Where(key => !localKeys.Contains(key)).ToHashSet(StringComparer.Ordinal);
        var reviewedKeys = root.GetProperty("reviewedKeys").EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty).ToArray();
        Require(reviewedKeys.Length == 50
            && reviewedKeys.Length == reviewedKeys.Distinct(StringComparer.Ordinal).Count()
            && reviewedKeys.ToHashSet(StringComparer.Ordinal).SetEquals(expectedKeys),
            "TIA unselected renderer visual review keys must exactly match the Siemens-facing catalog.");

        Require(root.GetProperty("detailReview").GetProperty("count").GetInt32() == 150
            && RequiredString(root.GetProperty("detailReview"), "status", "TIA unselected detail review") == "visual-pass"
            && root.GetProperty("sheetReview").GetProperty("count").GetInt32() == 24
            && RequiredString(root.GetProperty("sheetReview"), "status", "TIA unselected sheet review") == "visual-pass"
            && root.GetProperty("integrityReview").GetProperty("artifactCount").GetInt32() == 174
            && root.GetProperty("integrityReview").GetProperty("sha256Mismatches").GetInt32() == 0,
            "TIA unselected renderer review counts or integrity verdict are incomplete.");

        var artifactSet = root.GetProperty("artifactSet");
        var artifactSetHash = RequiredString(artifactSet, "sha256", "TIA unselected artifact set");
        Require(artifactSet.GetProperty("entryCount").GetInt32() == 174
            && artifactSetHash.Length == 64 && artifactSetHash.All(Uri.IsHexDigit),
            "TIA unselected renderer artifact-set record is invalid.");

        var actualManifestPath = Path.Combine(projectRoot, sourcePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(actualManifestPath))
        {
            Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(actualManifestPath)))
                    .Equals(sourceHash, StringComparison.OrdinalIgnoreCase),
                "TIA unselected renderer manifest hash does not match the reviewed source.");
            using var manifestDocument = JsonDocument.Parse(File.ReadAllText(actualManifestPath));
            var manifest = manifestDocument.RootElement;
            Require(!manifest.GetProperty("selected").GetBoolean()
                && RequiredString(manifest, "humanReviewStatus", "TIA unselected generated manifest") == "pending",
                "The reviewed unselected manifest has an invalid selection or generated-review state.");

            var artifactRoot = Path.GetDirectoryName(actualManifestPath)!;
            var lines = new List<string>
            {
                "tia-renderer-artifact-set-v1",
                "selected=false",
                "operandRepresentation=" + RequiredString(manifest, "operandRepresentation", "TIA unselected manifest"),
                "tagInformationDisplay=" + RequiredString(manifest, "tagInformationDisplay", "TIA unselected manifest"),
                "tagInformationPosition=" + RequiredString(manifest, "tagInformationPosition", "TIA unselected manifest"),
                "zoomLevels=" + string.Join(',', manifest.GetProperty("zoomLevels").EnumerateArray()
                    .Select(item => item.GetInt32()).Order()),
            };
            foreach (var detail in manifest.GetProperty("details").EnumerateArray()
                .OrderBy(item => item.GetProperty("file").GetString(), StringComparer.Ordinal))
            {
                var file = RequiredString(detail, "file", "TIA unselected detail");
                var hash = RequiredString(detail, "sha256", "TIA unselected detail");
                Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(artifactRoot,
                            file.Replace('/', Path.DirectorySeparatorChar)))))
                        .Equals(hash, StringComparison.OrdinalIgnoreCase),
                    $"TIA unselected renderer detail hash mismatch: {file}.");
                lines.Add($"detail|{file}|{hash}");
            }
            foreach (var sheet in manifest.GetProperty("sheets").EnumerateArray()
                .OrderBy(item => item.GetProperty("file").GetString(), StringComparer.Ordinal))
            {
                var file = RequiredString(sheet, "file", "TIA unselected sheet");
                var hash = RequiredString(sheet, "sha256", "TIA unselected sheet");
                Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(artifactRoot,
                            file.Replace('/', Path.DirectorySeparatorChar)))))
                        .Equals(hash, StringComparison.OrdinalIgnoreCase),
                    $"TIA unselected renderer sheet hash mismatch: {file}.");
                lines.Add($"sheet|{file}|{hash}");
            }
            var canonical = string.Join('\n', lines) + "\n";
            var actualSetHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
            Require(actualSetHash.Equals(artifactSetHash, StringComparison.OrdinalIgnoreCase),
                "TIA unselected renderer artifact-set hash does not match its canonical manifest entries.");
        }

        var boundary = root.GetProperty("evidenceBoundary");
        Require(boundary.GetProperty("offlineOnly").GetBoolean()
            && !boundary.GetProperty("installedTiaExecuted").GetBoolean()
            && !boundary.GetProperty("livePlcExecuted").GetBoolean()
            && root.GetProperty("limitations").EnumerateArray().Any(item =>
                item.GetString()?.Contains("installed TIA", StringComparison.OrdinalIgnoreCase) == true),
            "TIA unselected renderer review must preserve the offline-only vendor boundary.");
    }

    private static string EvidenceGateStatus(string cell)
    {
        var separator = cell.IndexOf(':');
        return (separator < 0 ? cell : cell[..separator]).Trim();
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
        var assignment = LadderInstructionCatalog.Get("coil");
        Equal("Assignment coil —( )—", assignment.TiaName);
        False(assignment.TiaName.Contains("=", StringComparison.Ordinal));
        var retentiveTimer = LadderInstructionCatalog.Get("rto");
        True(retentiveTimer.TiaName.Contains("TONR", StringComparison.Ordinal));
        True(retentiveTimer.LogixName.Contains("RTO", StringComparison.Ordinal));
        True(retentiveTimer.Parameters.Contains("R BOOL", StringComparison.Ordinal));
        True(retentiveTimer.Execution.Contains("R is true", StringComparison.Ordinal));
        var ctu = LadderInstructionCatalog.Get("ctu");
        True(ctu.Parameters.Contains("IEC counter DB", StringComparison.Ordinal));
        True(ctu.Parameters.Contains("CU BOOL, R BOOL, PV", StringComparison.Ordinal));
        True(ctu.Parameters.Contains("???", StringComparison.Ordinal));
        True(ctu.Execution.Contains("positive edge", StringComparison.Ordinal));
        True(ctu.Execution.Contains("R has priority", StringComparison.Ordinal));
        var timerReset = LadderInstructionCatalog.Get("timer-reset");
        True(timerReset.TiaName.Contains("RT", StringComparison.Ordinal));
        True(timerReset.LogixName.Contains("RES", StringComparison.Ordinal));
        var jump = LadderInstructionCatalog.Get("jump");
        True(jump.TiaName.Contains("JMP", StringComparison.Ordinal));
        True(jump.LogixName.Contains("JMP", StringComparison.Ordinal));
        var label = LadderInstructionCatalog.Get("label");
        True(label.TiaName.Contains("LABEL", StringComparison.Ordinal));
        True(label.LogixName.Contains("LBL", StringComparison.Ordinal));
        Equal("A negative or invalid source writes an invalid REAL value and publishes VC_RUNTIME_DOMAIN; zero writes negative infinity.",
            LadderInstructionCatalog.Get("numeric-10").Restrictions);
        Equal("A source outside -1 through 1, or an invalid source, writes an invalid REAL value and publishes VC_RUNTIME_DOMAIN.",
            LadderInstructionCatalog.Get("numeric-14").Restrictions);
        Equal(LadderInstructionCatalog.Get("numeric-14").Restrictions,
            LadderInstructionCatalog.Get("numeric-15").Restrictions);
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

    private static void TestReturnValueEno()
    {
        static VirtualControllerSnapshot Execute(LadderReturnValueKind kind, bool operand = false)
        {
            var runtime = Runtime(ReturnProgram(kind,
                kind == LadderReturnValueKind.Operand ? "return_value" : string.Empty));
            runtime.Run();
            return runtime.Scan(Inputs(
                ("call_enable", true),
                ("return_enable", true),
                ("return_value", operand)));
        }

        var rlo = Execute(LadderReturnValueKind.Rlo);
        True(rlo.Elements["call-return"].Energized);
        True(rlo.CallInvocations.Single().EnableOutput);

        var trueConstant = Execute(LadderReturnValueKind.True);
        True(trueConstant.Elements["call-return"].Energized);
        True(trueConstant.CallInvocations.Single().EnableOutput);

        var falseConstant = Execute(LadderReturnValueKind.False);
        False(falseConstant.Elements["call-return"].Energized);
        False(falseConstant.CallInvocations.Single().EnableOutput);
        True(falseConstant.Observations!.GlobalLast["call-return"].LocalResult == false);

        var falseOperand = Execute(LadderReturnValueKind.Operand, false);
        False(falseOperand.Elements["call-return"].Energized);
        False(falseOperand.CallInvocations.Single().EnableOutput);

        var trueOperand = Execute(LadderReturnValueKind.Operand, true);
        True(trueOperand.Elements["call-return"].Energized);
        True(trueOperand.CallInvocations.Single().EnableOutput);
        True(trueOperand.Observations!.GlobalLast["call-return"].LocalResult == true);
    }

    private static void TestReturnValueValidation()
    {
        var empty = ReturnProgram(LadderReturnValueKind.Operand, string.Empty);
        True(LadderCompiler.Validate(empty).Any(issue => issue.Path.EndsWith(".return.operand", StringComparison.Ordinal)));

        var wrongType = empty with
        {
            Variables = empty.Variables.Append(
                new PlcVariable("not_bool", PlcVariableType.DInt, PlcVariableRole.Memory, 0L)).ToArray(),
            Blocks = empty.Blocks!.Select(block => block.Id != "sub-return" ? block : block with
            {
                Networks = block.Networks.Select(network => network.Return is null ? network : network with
                {
                    Return = network.Return with { Operand = "not_bool" },
                }).ToArray(),
            }).ToArray(),
        };
        True(LadderCompiler.Validate(wrongType).Any(issue => issue.Code == "VC002"
            && issue.Path.EndsWith(".return.operand", StringComparison.Ordinal)));
    }

    private static void TestReturnValueTransaction()
    {
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(ReturnProgram());
        editor.SelectBlock(1);
        var history = new LadderEditorHistory();
        var transaction = new LadderEditTransaction(editor, "Edit RET return value");
        transaction.Draft.SelectBlock(1);
        var draftReturn = transaction.Draft.Rungs.Single(rung => rung.IsReturn);
        var terminalId = draftReturn.TerminalActionId;
        draftReturn.ReturnValueKind = LadderReturnValueKind.Operand;
        draftReturn.ReturnValueOperand = "return_value";
        True(transaction.TryCommit(history, false, out var error), error);
        var applied = editor.Rungs.Single(rung => rung.IsReturn);
        Equal(LadderReturnValueKind.Operand, applied.ReturnValueKind);
        Equal("return_value", applied.ReturnValueOperand);
        Equal(terminalId, applied.TerminalActionId);

        True(history.Undo(editor, out _));
        var undone = editor.Rungs.Single(rung => rung.IsReturn);
        Equal(LadderReturnValueKind.Rlo, undone.ReturnValueKind);
        Equal(string.Empty, undone.ReturnValueOperand);
        Equal(terminalId, undone.TerminalActionId);

        True(history.Redo(editor, out _));
        var redone = editor.Rungs.Single(rung => rung.IsReturn);
        Equal(LadderReturnValueKind.Operand, redone.ReturnValueKind);
        Equal("return_value", redone.ReturnValueOperand);
        Equal(terminalId, redone.TerminalActionId);
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
        var original = ReturnProgram(LadderReturnValueKind.Operand, "return_value");
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        if (!loaded.IsValid || loaded.Program is null)
            throw new InvalidOperationException("RETURN program did not reload.");
        var loadedReturn = loaded.Program.Blocks![1].Networks[1].Return;
        True(loadedReturn is not null);
        Equal(LadderReturnValueKind.Operand, loadedReturn!.ValueKind);
        Equal("return_value", loadedReturn.Operand);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        editor.SelectBlock(1);
        True(editor.Rungs[1].IsReturn);
        Equal(LadderReturnValueKind.Operand, editor.Rungs[1].ReturnValueKind);
        Equal("return_value", editor.Rungs[1].ReturnValueOperand);
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
        Equal("coil", jumped.Observations!.GlobalLast["after-coil"].CatalogKey);
        Equal("label", jumped.Observations.GlobalLast["label-instruction"].CatalogKey);
        Equal("Visited", jumped.Observations.GlobalLast["label-instruction"].ControlOutcome);
        True(jumped.Observations.GlobalLast["label-instruction"].LocalResult);

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

    private static void TestTiaJumpLabelGrammar()
    {
        foreach (var valid in new[] { "A", "finish", "CAS1", "Network123" })
            True(TiaProgramControlLabel.IsValid(valid));
        foreach (var invalid in new string?[] { null, "", "1CAS", "CAS_1", "CAS-1", "CAS 1", "CAS1A", "Ä" })
            False(TiaProgramControlLabel.IsValid(invalid));
    }

    private static void TestJumpLabelRoundTrip()
    {
        var original = JumpProgram();
        var loaded = LadderProgramJson.Load(LadderProgramJson.Save(original));
        True(loaded.IsValid && loaded.Program is not null);
        Equal("after", loaded.Program!.Blocks![0].Networks[0].Jump!.TargetLabel);
        Equal("after", loaded.Program.Blocks[0].Networks[2].LabelInstruction!.Name);
        Equal("after_output", loaded.Program.Blocks[0].Networks[2].Coil!.Variable);
        var editor = new LadderEditorDocument();
        editor.ReplaceFromProgram(loaded.Program);
        True(editor.Rungs[0].IsJump);
        True(editor.Rungs[2].IsLabel);
        True(editor.Rungs[2].HasTerminalAction);
        Equal("after", editor.Rungs[2].NetworkLabelName);
        Equal("after_output", editor.Rungs[2].CoilVariable);
        Equal("after", editor.Rungs[0].ProgramControlLabel);
        var rebuilt = editor.BuildProgram();
        True(LadderCompiler.Compile(rebuilt).IsValid);
        True(rebuilt.Blocks![0].Networks[2].LabelInstruction is not null
            && rebuilt.Blocks[0].Networks[2].Coil is not null);
        var project = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(editor));
        True(project.IsReadable && project.Document is not null);
        var projectNetwork = project.Document!.BuildProgram().Blocks![0].Networks[2];
        Equal("after", projectNetwork.LabelInstruction!.Name);
        Equal("after_output", projectNetwork.Coil!.Variable);
        var copied = editor.CaptureRung(2);
        var pasted = editor.PasteRung(copied, 3);
        False(pasted.NetworkLabelId == editor.Rungs[2].NetworkLabelId);
        False(pasted.TerminalActionId == editor.Rungs[2].TerminalActionId);

        // Projects saved before LABEL became independent network metadata used
        // the label ID as a terminal action ID. Loading must not invent an
        // empty coil or lose that stable label identity.
        var legacyEditor = new LadderEditorDocument();
        legacyEditor.AddBlock("Main");
        legacyEditor.AddLabelRung("Legacy label", "Legacy1");
        var legacyRoot = JsonNode.Parse(LadderEditorProjectJson.Save(legacyEditor))!.AsObject();
        var legacyRung = legacyRoot["blocks"]!.AsArray()[0]!["rungs"]!.AsArray()[0]!.AsObject();
        legacyRung.Remove("networkLabelId");
        legacyRung.Remove("networkLabelName");
        legacyRung["terminalActionId"] = "legacy-label-id";
        legacyRung["hasTerminalAction"] = true;
        var migrated = LadderEditorProjectJson.Load(legacyRoot.ToJsonString());
        True(migrated.IsReadable && migrated.Document is not null);
        var migratedRung = migrated.Document!.Rungs[0];
        False(migratedRung.HasTerminalAction);
        Equal("legacy-label-id", migratedRung.NetworkLabelId);
        var migratedNetwork = migrated.Document.BuildProgram().Networks[0];
        True(migratedNetwork.Coil is null && migratedNetwork.LabelInstruction?.Id == "legacy-label-id");
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
                new LadderCoil("after-coil", "after_output"),
                LabelInstruction: new LadderLabel("label-instruction", "after")),
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
            new("count-down-network", "Count remaining parts", Contact("count-down-contact", "count_input"),
                Counter: new LadderCounter("ctd", "parts", preset, LadderCounterKind.CountDown, LoadInput: "load_input")),
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
        var variables = new List<PlcVariable>
        {
            new("enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("left", PlcVariableType.Real, PlcVariableRole.Input, 8.0),
            new("right", PlcVariableType.Real, PlcVariableRole.Input, 2.0),
            new("third", PlcVariableType.Real, PlcVariableRole.Input, 30.0),
            new("int_result", PlcVariableType.Int, PlcVariableRole.Memory, 0L),
            new("real_result", PlcVariableType.Real, PlcVariableRole.Output, 0.0),
        };
        if (destination == "dint_result")
            variables.Add(new("dint_result", PlcVariableType.DInt, PlcVariableRole.Memory, 0L));
        var operation = new LadderNumericOperation("numeric-operation", kind, sourceA, sourceB, destination, sourceC);
        return new LadderProgram(1, "numeric-test", "NumericTest", "LD", TimeSpan.FromMilliseconds(20), variables,
            [new LadderNetwork("numeric-network", "Numeric operation", Contact("enable-contact", "enable"), NumericOperation: operation)]);
    }

    private static LadderProgram ReturnProgram(
        LadderReturnValueKind valueKind = LadderReturnValueKind.Rlo,
        string operand = "")
    {
        var variables = new PlcVariable[]
        {
            new("call_enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("return_enable", PlcVariableType.Bool, PlcVariableRole.Input, false),
            new("return_value", PlcVariableType.Bool, PlcVariableRole.Input, false),
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
                Return: new LadderReturn("return-sub", valueKind, operand)),
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

    private static void True(bool value, [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        if (!value) throw new InvalidOperationException($"Expected TRUE: {expression}");
    }

    private static void False(bool value, [CallerArgumentExpression(nameof(value))] string? expression = null)
    {
        if (value) throw new InvalidOperationException($"Expected FALSE: {expression}");
    }

    private static void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Expected {expected}; actual {actual}.");
    }
}


