using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Catalog;
using RungProof.Next.Connections;
using RungProof.Next.Diagnostics;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class Main : Node3D
{
    private string? _capturePath;
    private int _captureFramesRemaining;
    private bool _captureWithoutEnvironment;
    private bool _captureRunAuthoredDemo;
    private string _captureState = "running";
    private string? _candidateId;
    private Node3D? _candidatePreviewRoot;
    private string? _sceneId;
    private string? _sceneAction;
    private bool _verifySceneContract;
    private bool _verifyAppShell;
    private bool _verifyExternalDialog;
    private bool _verifyExternalPlayback;
    private bool _verifyPlantMotion;
    private string? _offlinePlaybackModeFile;
    private bool _verifyWorkspace;
    private bool _verifyHud;
    private bool _verifyCameraInput;
    private bool _verifySceneControls;
    private bool _verifyGalleryClock;
    private bool _verifyTypedLessons;
    private bool _verifyVirtualController;
    private bool _verifyNumericSceneIo;
    private bool _verifyLadderEditor;
    private bool _verifyProjectTreePointer;
    private bool _verifySplitView;
    private bool _verifyUiDensity;
    private bool _virtualControllerDemo;
    private string? _mcpProjectPath;
    private string? _mcpSceneId;
    private bool _appShellRequested;
    private string _shellView = string.Empty;
    private string _shellScene = string.Empty;
    private string _shellSearch = string.Empty;
    private string _shellPlaceAsset = string.Empty;
    private bool _shellDemoConnections;
    private string _shellTransformMode = string.Empty;
    private string _shellTransformSpace = string.Empty;
    private bool _shellSelectAll;
    private bool _shellGroupSelection;
    private bool _shellNestedGroups;
    private bool _shellDemoLocalAxes;
    private bool _shellDemoMarquee;
    private bool _shellDemoArrangeMenu;
    private bool _shellDemoGroupRename;
    private bool _shellDemoGroupPivot;
    private Func<float>? _previewSpeedReader;
    private Node3D? _captureWitness;
    private SceneSimulationRuntime? _sceneRuntime;
    private SceneControlInteractor? _sceneControlInteractor;
    private Camera3D? _sceneCamera;
    private Node3D? _sceneCompositionRoot;
    private DemoEnvironmentPresentation? _demoEnvironment;
    private Node3D? _assetPreviewRoot;
    private Vector3 _sceneCameraDirection;
    private SimulatorShell? _simulatorShell;
    private ExternalPlcRuntimeClient? _externalConnection;
    private readonly ExternalScenePlayback _externalPlayback = new();
    private double _externalCycleElapsed;
    private int _externalCadenceGeneration = -1;
    private int _placedAssetCount;
    private string? _currentSceneId;
    private bool _frameWholeScene;
    private AssetCatalogDocument? _candidateCatalog;
    private SceneCatalogDocument? _sceneCatalog;
    private Camera3D? _mainCamera;
    private SceneCameraController? _cameraController;
    private Rect2? _lastFramedAperture;
    private SceneDefinition? _activeSceneDefinition;
    private readonly Dictionary<string, (string AssetId, Node3D Node)> _workspaceNodes =
        new(StringComparer.Ordinal);
    private readonly List<WorkspaceDocument> _undoHistory = [];
    private readonly List<WorkspaceDocument> _redoHistory = [];
    private string _workspaceSavedSnapshotJson = string.Empty;
    private string? _selectedPlacementId;
    private readonly HashSet<string> _selectedPlacementIds = new(StringComparer.Ordinal);
    private readonly List<WorkspaceConnectorLink> _connectorLinks = [];
    private readonly List<WorkspaceSignalLink> _signalLinks = [];
    private readonly List<WorkspaceGroup> _workspaceGroups = [];
    private VirtualControllerSession? _virtualController;
    private LadderProgram? _virtualProgram;
    private VirtualControllerSnapshot? _virtualSnapshot;
    private Node3D? _connectionVisuals;
    private bool _snapEnabled;
    private float _positionSnapM = 0.10f;
    private float _rotationSnapDegrees = 15.0f;
    private WorkspaceTransformSpace _transformSpace = WorkspaceTransformSpace.World;
    private WorkspaceTransformGizmo? _transformGizmo;
    private WorkspaceDocument? _gizmoStartSnapshot;
    private readonly Dictionary<string, (Vector3 Position, Vector3 Rotation, Vector3 Scale)> _dragStartTransforms =
        new(StringComparer.Ordinal);
    private Vector3 _dragPivot;
    private WorkspaceDocument? _workspaceClipboard;
    private bool _marqueePending;
    private bool _marqueeActive;
    private bool _marqueeAdditive;
    private Vector2 _marqueeStart;

    public override void _Ready()
    {
        // Keep editor dialogs and context property panels inside the application
        // viewport so they scale with the workbench and remain part of captures.
        GetTree().Root.GuiEmbedSubwindows = true;
        if (OS.HasFeature("offline_demo") && !RuntimeBuildPolicy.IsOfflineDemo)
        {
            GD.PushError("Offline demo export lacks the required compiled policy. Build with tools/build_offline_demo.ps1.");
            GetTree().Quit(1);
            return;
        }
        var camera = GetNode<Camera3D>("Camera");
        camera.Fov = 42.0f;
        // glTF converts Blender Z-up coordinates to Godot Y-up:
        // Blender (x, y, z) becomes Godot (x, z, -y).
        camera.Position = new Vector3(4.4f, 2.6f, 4.1f);
        camera.LookAt(new Vector3(0.0f, 0.72f, 0.0f), Vector3.Up);

        var userArguments = OS.GetCmdlineUserArgs();
        if (userArguments.Contains("--verify-offline-build-policy", StringComparer.Ordinal))
        {
            var factoryCalls = 0;
            var rejected = false;
            try
            {
                using var client = new ExternalPlcRuntimeClient(() =>
                {
                    factoryCalls++;
                    throw new InvalidOperationException("Policy check must not create a bridge channel");
                });
            }
            catch (NotSupportedException) { rejected = true; }
            var passed = RuntimeBuildPolicy.IsOfflineDemo && rejected && factoryCalls == 0;
            GD.Print($"OFFLINE_BUILD_POLICY {(passed ? "PASS" : "FAIL")} constructorRejected={rejected} bridgeFactoryCalls={factoryCalls}");
            GetTree().Quit(passed ? 0 : 1);
            return;
        }
        _capturePath = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--capture=", StringComparison.Ordinal))?
            .Substring("--capture=".Length);
        if(userArguments.Contains("--editor-zoom-review", StringComparer.Ordinal))
        {
            SetPhysicsProcess(false); SetProcess(false);
            CaptureEditorZoomReview(userArguments);
            return;
        }
        if(userArguments.Contains("--verify-tia-renderer-sweep", StringComparer.Ordinal))
        {
            SetPhysicsProcess(false); SetProcess(false);
            VerifyTiaUniversalRendererSweep();
            return;
        }
        if(userArguments.Contains("--verify-tia-monitor-sweep", StringComparer.Ordinal))
        {
            SetPhysicsProcess(false); SetProcess(false);
            VerifyTiaUniversalMonitorSweep();
            return;
        }
        if(userArguments.Contains("--recursive-editor-review", StringComparer.Ordinal))
        {
            SetPhysicsProcess(false); SetProcess(false);
            CaptureRecursiveEditorReview(userArguments);
            return;
        }
        if(userArguments.Contains("--capture-editor-monitoring", StringComparer.Ordinal))
        {
            SetPhysicsProcess(false); SetProcess(false);
            CaptureEditorMonitoringReview(userArguments);
            return;
        }
        _captureWithoutEnvironment = _capturePath is not null && userArguments.Contains("--capture-no-environment", StringComparer.Ordinal);
        _captureRunAuthoredDemo = _capturePath is not null && userArguments.Contains("--capture-run-demo", StringComparer.Ordinal);
        InitializeTenZoneCaptureOptions(userArguments);
        _templateCapturePending=userArguments.Contains("--template-edit-capture",StringComparer.Ordinal);
        _indexedTraceCaptureState=userArguments.FirstOrDefault(a=>a.StartsWith("--capture-indexed-device-trace=",StringComparison.Ordinal))?.Split('=',2)[1];
        _indexedTraceCapturePending=_indexedTraceCaptureState is not null;
        _deviceTraceCapturePending=userArguments.Contains("--device-trace-capture",StringComparer.Ordinal);
        _workspaceBeltCaptureState=userArguments.FirstOrDefault(a=>a.StartsWith("--capture-workspace-belt-state=",StringComparison.Ordinal))?.Split('=',2)[1];
        _workspaceBeltCapturePending=_workspaceBeltCaptureState is not null;
        _workspaceRollerCaptureState=userArguments.FirstOrDefault(a=>a.StartsWith("--capture-workspace-roller-state=",StringComparison.Ordinal))?.Split('=',2)[1];
        _workspaceRollerCapturePending=_workspaceRollerCaptureState is not null;
        _demo5LearnerCaptureState=userArguments.FirstOrDefault(a=>a.StartsWith("--capture-demo5-learner-state=",StringComparison.Ordinal))?.Split('=',2)[1];
        _demo5LearnerCapturePending=_demo5LearnerCaptureState is not null;
        _verifyChallengeUi = userArguments.Contains("--verify-challenge-ui", StringComparer.Ordinal);
        _verifyConveyorChallenge = userArguments.Contains("--verify-conveyor-challenge", StringComparer.Ordinal) || userArguments.Contains("--verify-challenge-conveyor", StringComparer.Ordinal);
        _captureEnterChallenge = _capturePath is not null && userArguments.Contains("--capture-challenge", StringComparer.Ordinal);
        _captureState = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--capture-state=", StringComparison.Ordinal))?
            .Substring("--capture-state=".Length)
            ?? "running";
        _candidateId = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--candidate-id=", StringComparison.Ordinal))?
            .Substring("--candidate-id=".Length);
        _sceneId = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--scene-id=", StringComparison.Ordinal))?
            .Substring("--scene-id=".Length);
        _sceneAction = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--scene-action=", StringComparison.Ordinal))?
            .Substring("--scene-action=".Length);
        _verifySceneContract = userArguments.Contains("--verify-scene-contract", StringComparer.Ordinal);
        _verifyAppShell = userArguments.Contains("--verify-app-shell", StringComparer.Ordinal);
        _verifyExternalDialog = userArguments.Contains("--verify-external-dialog", StringComparer.Ordinal);
        _verifyExternalPlayback = userArguments.Contains("--verify-external-playback", StringComparer.Ordinal);
        _verifyDemoSequence = userArguments.Contains("--verify-demo-sequence", StringComparer.Ordinal);
        _verifyPlantMotion = userArguments.Contains("--verify-plant-motion", StringComparer.Ordinal);
        _verifySceneGeometry = userArguments.Contains("--verify-scene-geometry", StringComparer.Ordinal)
            || userArguments.Contains("--verify-wastewater-installation", StringComparer.Ordinal)
            || userArguments.Contains("--verify-tank-switch-mounts", StringComparer.Ordinal)
            || userArguments.Contains("--verify-pallet-robot-controller", StringComparer.Ordinal)
            || userArguments.Contains("--verify-service-elevator-installation", StringComparer.Ordinal);
        _verifyToteFinishing = userArguments.Contains("--verify-tote-finishing", StringComparer.Ordinal);
        _auditDualSpindle = userArguments.Contains("--audit-dual-spindle", StringComparer.Ordinal);
        _auditRobotCnc = userArguments.Contains("--audit-robot-cnc", StringComparer.Ordinal);
        _auditRobotRestart = userArguments.Contains("--audit-robot-restart", StringComparer.Ordinal);
        _auditSequenceTower = userArguments.Contains("--audit-sequence-tower", StringComparer.Ordinal);
        _auditPalletizer = userArguments.Contains("--audit-palletizer", StringComparer.Ordinal);
        _auditCableCut = userArguments.Contains("--audit-cable-cut", StringComparer.Ordinal);
        _verifyRepeatCycleLearner = userArguments.Contains("--verify-repeat-cycle-learner", StringComparer.Ordinal);
        _repeatLearnerCaptureState=userArguments.FirstOrDefault(a=>a.StartsWith("--capture-repeat-learner-state=",StringComparison.Ordinal))?.Split('=',2)[1];
        _repeatLearnerCapturePending=_repeatLearnerCaptureState is not null;
        _verifyRepeatCycleLifecycle = userArguments.Contains("--verify-repeat-cycle-lifecycle", StringComparer.Ordinal);
        _auditRepeatCycle = userArguments.Contains("--audit-repeat-cycle", StringComparer.Ordinal);
        _auditButtonCounters = userArguments.Contains("--audit-button-counters", StringComparer.Ordinal);
        _auditParkingEntry = userArguments.Contains("--audit-parking-entry", StringComparer.Ordinal);
        _auditFlashPair = userArguments.Contains("--audit-flash-pair", StringComparer.Ordinal);
        _auditDrawbridge = userArguments.Contains("--audit-drawbridge", StringComparer.Ordinal);
        _auditGuardedLayout = userArguments.Contains("--audit-guarded-layout", StringComparer.Ordinal);
        _auditRadarLayout = userArguments.Contains("--audit-radar-layout", StringComparer.Ordinal);
        _auditEvLayout = userArguments.Contains("--audit-ev-layout", StringComparer.Ordinal);
        _auditWastewaterProcess = userArguments.Contains("--verify-wastewater-process", StringComparer.Ordinal);
        _auditMobileTraffic = userArguments.Contains("--verify-mobile-traffic", StringComparer.Ordinal);
        _auditMotorAggregates = userArguments.Contains("--verify-motor-aggregates", StringComparer.Ordinal);
        _verifyMotorConveyorZones = userArguments.Contains("--verify-motor-conveyor-zones", StringComparer.Ordinal);
        _verifyWorkspaceBelt = userArguments.Contains("--verify-workspace-belt",StringComparer.Ordinal);
        _verifyWorkspaceRoller = userArguments.Contains("--verify-workspace-roller", StringComparer.Ordinal);
        _verifyWorkspaceMappingDetach = userArguments.Contains("--verify-workspace-mapping-detach", StringComparer.Ordinal);
        _verifyFiveDemoLearner = userArguments.Contains("--verify-five-demo-learner", StringComparer.Ordinal);
        _verifyScanInspection = userArguments.Contains("--verify-scan-inspection", StringComparer.Ordinal);
        _verifyScanInspectionDemo5 = userArguments.Contains("--verify-scan-inspection-demo5-equivalence", StringComparer.Ordinal);
        _scanInspectionCaptureState = userArguments.FirstOrDefault(a => a.StartsWith("--capture-scan-inspection-state=", StringComparison.Ordinal))?.Split('=', 2)[1];
        _scanInspectionCapturePending = _scanInspectionCaptureState is not null;
        _verifyWorkspaceRollerGeometry = userArguments.Contains("--verify-workspace-roller-geometry", StringComparer.Ordinal);
        _verifyWorkspaceRollerExternalGeometry = userArguments.Contains("--verify-workspace-roller-external-geometry", StringComparer.Ordinal);
        _workspaceRollerGeometryCapture = userArguments.FirstOrDefault(argument => argument.StartsWith("--workspace-roller-geometry-capture=", StringComparison.Ordinal))?.Substring("--workspace-roller-geometry-capture=".Length);
        _workspaceRollerGeometryCapturePending = _workspaceRollerGeometryCapture is not null;
        if (_workspaceRollerGeometryCapturePending && !_verifyWorkspaceRollerGeometry && !_verifyWorkspaceRollerExternalGeometry)
        {
            GD.PushError("Roller geometry capture requires a roller geometry verification mode.");
            GetTree().Quit(1);
            return;
        }
        _auditServiceElevatorAccess = userArguments.Contains("--verify-service-elevator-access", StringComparer.Ordinal);
        _auditToteFinishingProcess = userArguments.Contains("--verify-tote-finishing-process", StringComparer.Ordinal);
        _auditLuggageLayout = userArguments.Contains("--audit-luggage-layout", StringComparer.Ordinal);
        _auditHandDryer = userArguments.Contains("--audit-hand-dryer", StringComparer.Ordinal);
        _auditCoating = userArguments.Contains("--audit-coating", StringComparer.Ordinal);
        _auditBagIndex = userArguments.Contains("--audit-bag-index", StringComparer.Ordinal);
        _auditPedestrianCrossing = userArguments.Contains("--audit-pedestrian-crossing", StringComparer.Ordinal);
        _auditRunningTower = userArguments.Contains("--audit-running-tower", StringComparer.Ordinal);
        _auditRotaryFlasher = userArguments.Contains("--audit-rotary-flasher", StringComparer.Ordinal);
        _auditTimerLessons = userArguments.Contains("--audit-timer-lessons", StringComparer.Ordinal);
        _auditPackageGrouping = userArguments.Contains("--audit-package-grouping", StringComparer.Ordinal);
        _auditBarrelFill = userArguments.Contains("--audit-barrel-fill", StringComparer.Ordinal);
        _auditCookiePackaging = userArguments.Contains("--audit-cookie-packaging", StringComparer.Ordinal);
        _auditChainLiftInstallation = userArguments.Contains("--audit-chain-lift-installation", StringComparer.Ordinal);
        _verifyCartonStaticRoutes = userArguments.Contains("--verify-carton-static-routes", StringComparer.Ordinal);
        _reportSceneGeometry = userArguments.Contains("--report-scene-geometry", StringComparer.Ordinal);
        _visualPlantReview = userArguments.Contains("--visual-plant-review", StringComparer.Ordinal);
        _visualSceneReview = _visualPlantReview || userArguments.Contains("--visual-scene-review", StringComparer.Ordinal);
        _verifyWorkspaceDriveRepositoryOnly = userArguments.Contains("--verify-workspace-drive-repository", StringComparer.Ordinal);
        _verifyWorkspaceDriveRetirement = userArguments.Contains("--verify-workspace-drive-retirement", StringComparer.Ordinal);
        _verifyWorkspaceDriveAuthoring = userArguments.Contains("--verify-workspace-drive-authoring", StringComparer.Ordinal);
        _verifyWorkspaceDrive = userArguments.Contains("--verify-workspace-drive", StringComparer.Ordinal);
        _verifyWorkspace = _verifyWorkspaceDriveRepositoryOnly || userArguments.Contains("--verify-workspace", StringComparer.Ordinal);
        _verifyHud = userArguments.Contains("--verify-hud", StringComparer.Ordinal);
        _verifyCameraInput = userArguments.Contains("--verify-camera-input", StringComparer.Ordinal);
        _verifySceneControls = userArguments.Contains("--verify-scene-controls", StringComparer.Ordinal);
        _verifyGalleryClock = userArguments.Contains("--verify-gallery-clock", StringComparer.Ordinal);
        _verifyTypedLessons = userArguments.Contains("--verify-typed-lessons", StringComparer.Ordinal);
        _verifyTypedLearnerWorkflow = userArguments.Contains("--verify-typed-learner-workflow", StringComparer.Ordinal);
        _verifyTypedHistoryPresentation = userArguments.Contains("--verify-typed-history-presentation", StringComparer.Ordinal);
        _typedLearnerCaptureState=userArguments.FirstOrDefault(a=>a.StartsWith("--capture-typed-learner-state=",StringComparison.Ordinal))?.Split('=',2)[1];
        _typedLearnerCapturePending=_typedLearnerCaptureState is not null;
        _workspaceDriveCaptureState=userArguments.FirstOrDefault(a=>a.StartsWith("--capture-workspace-drive-state=",StringComparison.Ordinal))?.Split('=',2)[1];
        _workspaceDriveCapturePending=_workspaceDriveCaptureState is not null;
        _verifyVirtualController = userArguments.Contains("--verify-virtual-controller", StringComparer.Ordinal);
        _verifyNumericSceneIo = userArguments.Contains("--verify-numeric-scene-io", StringComparer.Ordinal);
        _verifyLadderEditor = userArguments.Contains("--verify-ladder-editor", StringComparer.Ordinal);
        _verifyProjectTreePointer = userArguments.Contains("--verify-project-tree-pointer", StringComparer.Ordinal);
        _verifySplitView = userArguments.Contains("--verify-split-view", StringComparer.Ordinal);
        _verifyUiDensity = userArguments.Contains("--verify-ui-density", StringComparer.Ordinal);
        _virtualControllerDemo = userArguments.Contains("--virtual-controller-demo", StringComparer.Ordinal);
        _mcpProjectPath = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--mcp-project=", StringComparison.Ordinal))?
            .Substring("--mcp-project=".Length);
        _mcpSceneId = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--mcp-scene=", StringComparison.Ordinal))?
            .Substring("--mcp-scene=".Length);
        _appShellRequested = _verifyWorkspaceBelt || _verifyChallengeUi || _verifyConveyorChallenge || _verifyDemoSequence || _auditToteFinishingProcess || _auditServiceElevatorAccess || _auditMotorAggregates || _verifyMotorConveyorZones || _auditMobileTraffic || _auditWastewaterProcess || _reportSceneGeometry || _verifySceneGeometry || _verifyToteFinishing || _auditDualSpindle || _auditRobotCnc || _auditRobotRestart || _auditSequenceTower || _auditChainLiftInstallation || _auditCookiePackaging || _auditBarrelFill || _auditCableCut || _auditPalletizer || _auditRepeatCycle || _auditButtonCounters || _auditParkingEntry || _auditPackageGrouping || _auditTimerLessons || _auditRotaryFlasher || _auditFlashPair || _auditRunningTower || _auditPedestrianCrossing || _auditDrawbridge || _auditBagIndex || _auditCoating || _auditHandDryer || _auditLuggageLayout || _auditEvLayout || _auditRadarLayout || _auditGuardedLayout || _verifyCartonStaticRoutes || (_visualSceneReview && !_visualPlantReview) || _verifyAppShell || _verifyExternalDialog || _verifyExternalPlayback || _verifyPlantMotion || _verifyWorkspace || _verifyHud || _verifyCameraInput || _verifySceneControls
            || _verifyTypedLessons || _verifyGalleryClock || _verifyVirtualController || _verifyNumericSceneIo || _verifyLadderEditor || _verifyProjectTreePointer || _verifySplitView || _verifyUiDensity || _virtualControllerDemo
            || userArguments.Contains("--app-shell", StringComparer.Ordinal);
        _appShellRequested |= _verifyWorkspaceRoller || _verifyWorkspaceRollerGeometry || _verifyWorkspaceRollerExternalGeometry;
        _appShellRequested |= _verifyWorkspaceMappingDetach || _verifyWorkspaceDrive || _verifyWorkspaceDriveAuthoring || _verifyWorkspaceDriveRetirement;
        _appShellRequested |= _verifyFiveDemoLearner || _verifyTypedLearnerWorkflow || _verifyTypedHistoryPresentation;
        _appShellRequested |= _verifyScanInspection;
        _appShellRequested |= _verifyScanInspectionDemo5;
        if (_visualPlantReview && (_sceneId is null || _appShellRequested || _verifySceneContract))
        {
            GD.PushError("--visual-plant-review requires --scene-id and cannot be combined with app-shell or verification modes.");
            GetTree().Quit(1);
            return;
        }
        _shellView = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--shell-view=", StringComparison.Ordinal))?
            .Substring("--shell-view=".Length) ?? string.Empty;
        _shellScene = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--shell-scene=", StringComparison.Ordinal))?
            .Substring("--shell-scene=".Length) ?? string.Empty;
        if (!ConfigureNativeMotionMovie(userArguments)) return;
        _shellSearch = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--shell-search=", StringComparison.Ordinal))?
            .Substring("--shell-search=".Length) ?? string.Empty;
        _shellPlaceAsset = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--shell-place-asset=", StringComparison.Ordinal))?
            .Substring("--shell-place-asset=".Length) ?? string.Empty;
        _shellDemoConnections = userArguments.Contains("--shell-demo-connections", StringComparer.Ordinal);
        _shellTransformMode = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--shell-transform-mode=", StringComparison.Ordinal))?
            .Substring("--shell-transform-mode=".Length) ?? string.Empty;
        _shellTransformSpace = userArguments
            .FirstOrDefault(argument => argument.StartsWith("--shell-transform-space=", StringComparison.Ordinal))?
            .Substring("--shell-transform-space=".Length) ?? string.Empty;
        _shellSelectAll = userArguments.Contains("--shell-select-all", StringComparer.Ordinal);
        _shellGroupSelection = userArguments.Contains("--shell-group-selection", StringComparer.Ordinal);
        _shellNestedGroups = userArguments.Contains("--shell-nested-groups", StringComparer.Ordinal);
        _shellDemoLocalAxes = userArguments.Contains("--shell-demo-local-axes", StringComparer.Ordinal);
        _shellDemoMarquee = userArguments.Contains("--shell-demo-marquee", StringComparer.Ordinal);
        _shellDemoArrangeMenu = userArguments.Contains("--shell-demo-arrange-menu", StringComparer.Ordinal);
        _shellDemoGroupRename = userArguments.Contains("--shell-demo-group-rename", StringComparer.Ordinal);
        _shellDemoGroupPivot = userArguments.Contains("--shell-demo-group-pivot", StringComparer.Ordinal);
        _captureFramesRemaining = _capturePath is null
            ? 0
            : _captureState.Equals("stopped", StringComparison.OrdinalIgnoreCase) ? 45 : 120;

        var production = AssetCatalogLoader.LoadFromProject(
            "res://assets/catalog/production.catalog.json"
        );
        var candidates = AssetCatalogLoader.LoadFromProject(
            "res://assets/catalog/candidates.catalog.json",
            requireApproval: false
        );
        var assets = AssetCatalogLoader.Merge(production, candidates);

        GD.Print($"RungProof Next ready. Production assets: {production.Assets.Count}.");
        GD.Print($"Candidate assets: {candidates.Assets.Count}.");
        GD.Print("Legacy primitive assets intentionally excluded.");

        if (_mcpProjectPath is not null)
        {
            VerifyMcpProject(_mcpProjectPath, _mcpSceneId);
            return;
        }

        if (_sceneId is not null)
        {
            if (_visualPlantReview)
            {
                _mainCamera = camera;
                _candidateCatalog = assets;
                _sceneCatalog = SceneCatalogLoader.LoadCatalog("res://scenes/catalog/original-scenes.catalog.json");
            }
            AddMigratedScene(_sceneId, assets, camera, autoRun: !_visualPlantReview);
            if (_visualPlantReview) AddVisualSceneReviewControls();
        }
        else if (_candidateId is not null || (_capturePath is not null && !_appShellRequested))
        {
            var selectedAsset = _candidateId is null
                ? candidates.Assets[0]
                : candidates.Assets.FirstOrDefault(asset => asset.Id == _candidateId)
                    ?? throw new InvalidOperationException($"Unknown candidate asset '{_candidateId}'.");
            AddCandidatePreview(selectedAsset);
            if (_capturePath is not null && selectedAsset.Kinematics.Count > 0)
            {
                AddCaptureWitness(selectedAsset);
            }
            if (_candidatePreviewRoot is not null)
            {
                _sceneCamera = camera;
                _sceneCompositionRoot = _candidatePreviewRoot;
                _sceneCameraDirection = camera.Position;
                FrameComposition(camera, _candidatePreviewRoot, _sceneCameraDirection);
            }
        }
        else
        {
            AddSimulatorShell(assets, camera);
        }
    }

    public override void _ExitTree()
    {
        if (_nativeMotionTraceConnected)
        {
            RenderingServer.FramePostDraw -= TraceNativeMotionFrame;
            _nativeMotionTraceConnected = false;
        }
        _nativeMotionTrace?.Dispose();
        _nativeMotionTrace = null;
        _challengeCoordinator?.Dispose();
        _externalConnection?.Dispose();
        if (_offlinePlaybackModeFile is not null) System.IO.File.Delete(_offlinePlaybackModeFile);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && _simulatorShell is not null)
            _simulatorShell.RequestWindowClose();
    }

    /// <summary>
    /// Headless verification entry point for the local RungProof MCP server.
    /// It loads only editor JSON and scene-declared symbolic points, compiles
    /// the program, and validates the offline I/O seam.  It deliberately never
    /// creates a connection client or any physical PLC transport.
    /// </summary>
    private void VerifyMcpProject(string projectPath, string? requestedSceneId)
    {
        var issues = new List<LadderValidationIssue>();
        string projectName = string.Empty;
        string sceneId = requestedSceneId ?? string.Empty;
        try
        {
            var loaded = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(projectPath));
            issues.AddRange(loaded.Issues);
            if (!loaded.IsReadable || loaded.Document is null)
            {
                PrintMcpVerification(false, projectName, sceneId, issues);
                GetTree().Quit(1);
                return;
            }

            projectName = loaded.Document.Name;
            var program = loaded.Document.BuildProgram();
            issues.AddRange(LadderCompiler.Compile(program).Issues);
            sceneId = string.IsNullOrWhiteSpace(requestedSceneId)
                ? loaded.Document.SourceSceneId
                : requestedSceneId;
            if (string.IsNullOrWhiteSpace(sceneId))
            {
                issues.Add(new LadderValidationIssue("MCP001", "$.sourceSceneId",
                    "A sourceSceneId or --mcp-scene is required for scene I/O validation."));
            }
            else
            {
                var catalog = SceneCatalogLoader.LoadCatalog("res://scenes/catalog/original-scenes.catalog.json");
                var entry = catalog.Scenes.FirstOrDefault(candidate => candidate.Id == sceneId);
                if (entry is null)
                {
                    issues.Add(new LadderValidationIssue("MCP002", "$.sourceSceneId",
                        $"Scene '{sceneId}' is not in the migrated scene catalog."));
                }
                else
                {
                    var scene = SceneCatalogLoader.LoadScene(entry);
                    issues.AddRange(SceneIoBindingValidator.Validate(program, ReadScenePoints(scene)));
                }
            }
        }
        catch (Exception exception)
        {
            issues.Add(new LadderValidationIssue("MCP003", "$.project", exception.Message));
        }

        var passed = issues.Count == 0;
        PrintMcpVerification(passed, projectName, sceneId, issues);
        GetTree().Quit(passed ? 0 : 1);
    }

    private static IReadOnlyList<SceneIoPoint> ReadScenePoints(SceneDefinition scene)
    {
        var points = new List<SceneIoPoint>();
        if (scene.Simulation.ValueKind != JsonValueKind.Object
            || !scene.Simulation.TryGetProperty("points", out var elements)
            || elements.ValueKind != JsonValueKind.Array) return points;
        foreach (var element in elements.EnumerateArray())
        {
            var name = JsonText(element, "name");
            if (name.Length == 0) continue;
            points.Add(new SceneIoPoint(name, JsonText(element, "type"), JsonText(element, "owner"),
                JsonText(element, "role"), JsonText(element, "purpose"),
                element.TryGetProperty("aggregate", out var schema) ? PlcAggregates.ParseSchema(schema) : null));
        }
        return points;
    }

    private static string JsonText(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static void PrintMcpVerification(
        bool passed,
        string projectName,
        string sceneId,
        IReadOnlyList<LadderValidationIssue> issues)
    {
        var payload = JsonSerializer.Serialize(new
        {
            passed,
            projectName,
            sceneId,
            issueCount = issues.Count,
            issues = issues.Select(issue => new { issue.Code, issue.Path, issue.Message }),
            safety = "offline-symbolic-only; physical-plc-transport-unavailable",
        });
        GD.Print($"MCP_PROJECT_VALIDATION {payload}");
    }

    private void AddSimulatorShell(AssetCatalogDocument candidates, Camera3D camera)
    {
        var sceneCatalog = SceneCatalogLoader.LoadCatalog(
            "res://scenes/catalog/original-scenes.catalog.json"
        );
        _candidateCatalog = candidates;
        _sceneCatalog = sceneCatalog;
        _mainCamera = camera;
        var scenes = sceneCatalog.Scenes.Select(SceneCatalogLoader.LoadScene).ToArray();
        var diagnostics = ProjectValidator.Validate(candidates, sceneCatalog, scenes);
        IGuardedRuntimeClient connection;
        if (RuntimeBuildPolicy.IsOfflineDemo)
        {
            connection = new DisconnectedRuntimeClient();
            GD.Print("RUNGPROOF_BUILD_POLICY offline-demo external-transport-disabled");
        }
        else
        {
            if (_verifyExternalPlayback)
            {
                _offlinePlaybackModeFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"rungproof-playback-{Guid.NewGuid():N}.txt");
                System.IO.File.WriteAllText(_offlinePlaybackModeFile, "ready");
                _externalConnection = new ExternalPlcRuntimeClient(() =>
                {
                    var info = new System.Diagnostics.ProcessStartInfo(ProjectSettings.GlobalizePath("res://../build/.venv-rungproof/Scripts/python.exe"))
                    {
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                    };
                    info.ArgumentList.Add(ProjectSettings.GlobalizePath("res://tests/Connections/fake_bridge.py"));
                    info.ArgumentList.Add(_offlinePlaybackModeFile);
                    return new ProcessBridgeChannel(info);
                });
            }
            else _externalConnection = new ExternalPlcRuntimeClient(ProjectSettings.GlobalizePath("res://.."));
            connection = _externalConnection;
            _externalConnection.StateChanged += SynchronizeExternalPlayback;
        }
        _simulatorShell = new SimulatorShell(candidates, sceneCatalog, diagnostics, connection);
        GetTree().AutoAcceptQuit = false;
        _simulatorShell.SceneRequested += sceneId =>
        {
            try
            {
                AddMigratedScene(sceneId, candidates, camera, showRuntimeControls: false, autoRun: false);
                _simulatorShell.SetWorkspaceStatus($"SCENARIO LOADED · {_activeSceneDefinition?.Name ?? sceneId}");
            }
            catch (Exception exception)
            {
                _simulatorShell.SetWorkspaceStatus($"SCENARIO LOAD FAILED · {exception.Message}", isError: true);
                GD.PushError($"SCENARIO_LOAD_FAILED id={sceneId} error={exception}");
            }
        };
        _simulatorShell.AssetPlacementRequested += asset => PlaceAsset(asset);
        _simulatorShell.AssetPreviewRequested += OpenAssetPreview;
        _simulatorShell.AssetPreviewClosed += CloseAssetPreview;
        _simulatorShell.LoadWorkspaceRequested += LoadWorkspace;
        _simulatorShell.SaveWorkspaceToPathRequested += SaveWorkspaceToPath;
        _simulatorShell.LoadWorkspaceFromPathRequested += LoadWorkspaceFromPath;
        _simulatorShell.WorkspaceDriveConnectionRequested += ConnectSelectedWorkspaceDrive;
        _simulatorShell.CreateFiniteBeltWorkspaceRequested += CreateFiniteBeltWorkspace;
        _simulatorShell.CreateFiniteRollerWorkspaceRequested += CreateFiniteRollerWorkspace;
        _simulatorShell.WorkspaceReplacementGuard = GuardWorkspaceReplacement;
        _simulatorShell.IntegratedDraftChanged += RefreshWorkspaceDirty;
        _simulatorShell.IntegratedDraftChanged += WorkspaceTrialDraftChanged;
        _simulatorShell.IntegratedDraftChanged += RepeatCycleDraftChanged;
        _simulatorShell.IntegratedDraftChanged += WorkspaceRollerTrialDraftChanged;
        _simulatorShell.IntegratedDraftChanged += PublishScanInspectionState;
        _simulatorShell.WorkspaceDiscardRequested += CompletePendingWorkspaceAction;
        _simulatorShell.WorkspaceReplacementCancelled += CancelPendingWorkspaceAction;
        _simulatorShell.PlacementSelectionSetRequested += SetSelection;
        _simulatorShell.PlacementTransformRequested += TransformPlacement;
        _simulatorShell.UndoRequested += UndoWorkspace;
        _simulatorShell.RedoRequested += RedoWorkspace;
        _simulatorShell.DuplicateSelectionRequested += DuplicateSelection;
        _simulatorShell.CopySelectionRequested += CopySelection;
        _simulatorShell.PasteSelectionRequested += PasteSelection;
        _simulatorShell.DeleteSelectionRequested += DeleteSelection;
        _simulatorShell.GroupSelectionRequested += GroupSelection;
        _simulatorShell.UngroupSelectionRequested += UngroupSelection;
        _simulatorShell.RenameGroupRequested += RenameGroup;
        _simulatorShell.GroupPivotRequested += SetGroupPivot;
        _simulatorShell.FocusPlacementRequested += FocusPlacement;
        _simulatorShell.FrameSceneRequested += wholeScene =>
        {
            _frameWholeScene = wholeScene;
            if (_mainCamera is not null && _sceneCompositionRoot is not null)
                FrameComposition(_mainCamera, _sceneCompositionRoot, _sceneCameraDirection);
        };
        _simulatorShell.ResetPlacementTransformRequested += ResetPlacementTransform;
        _simulatorShell.SnapSettingsChanged += SetSnapSettings;
        _simulatorShell.TransformModeRequested += SetTransformMode;
        _simulatorShell.TransformSpaceRequested += SetTransformSpace;
        _simulatorShell.ArrangeSelectionRequested += ArrangeSelection;
        _simulatorShell.ConnectorLinkRequested += CreateConnectorLink;
        _simulatorShell.SignalLinkRequested += CreateSignalLink;
        _simulatorShell.ConnectorLinkDeleteRequested += DeleteConnectorLink;
        _simulatorShell.SignalLinkDeleteRequested += DeleteSignalLink;
        _simulatorShell.RunRequested += RunEditorDraftController;
        _simulatorShell.StopRequested += StopActiveController;
        _simulatorShell.ResetRequested += ResetActiveController;
        _simulatorShell.PauseRequested += PauseActiveController;
        _simulatorShell.StepScanRequested += StepActiveController;
        _simulatorShell.SceneActionRequested += action => ExecuteSelectedControllerAction(action);
        _simulatorShell.ControllerModeChanged += external =>
        {
            ReleaseGantryReviewClock();
            RefreshGantryReviewClockControls();
            _externalPlayback.Pause();
            if (_virtualController is not null) DisableVirtualController();
            if (_sceneRuntime is not null)
            {
                // Gallery animation is SIM-owned; PLC lessons still require a
                // selected controller and cannot execute preview PLC rules.
                _sceneRuntime.UsesExternalClock = SceneRequiresControllerClock;
                if (!external) _sceneRuntime.ResetSimulation();
            }
            SynchronizeExternalPlayback();
            ReconcileWorkspaceDriveMappings();
            RefreshWorkspaceUi();
        };
        _simulatorShell.VirtualControllerDemoRequested += EnableVirtualControllerDemo;
        _simulatorShell.VirtualControllerProgramRequested += EnableVirtualControllerProgram;
        _simulatorShell.VirtualControllerDisabled += DisableVirtualController;
        _simulatorShell.VirtualForceRequested += ApplyVirtualForce;
        _simulatorShell.VirtualForceRemoveRequested += RemoveVirtualForce;
        _simulatorShell.VirtualForceClearRequested += ClearVirtualForces;
        _simulatorShell.VirtualWatchModifyRequested += ModifyVirtualWatchValue;
        _simulatorShell.VirtualStartRequested += () => PulseVirtualInput("operator.start", "start_command");
        _simulatorShell.VirtualStopRequested += () => PulseVirtualInput("operator.stop", "stop_command");
        AddChild(_simulatorShell);
        InitializeChallengeShell();
        _cameraController = new SceneCameraController(camera);
        _cameraController.ViewportRectProvider = () => _simulatorShell?.SceneViewportRect() ?? new Rect2();
        _simulatorShell.ProductViewChanged += _ => _cameraController.CancelPointerCapture();
        AddChild(_cameraController);
        _transformGizmo = new WorkspaceTransformGizmo();
        AddChild(_transformGizmo);

        if (sceneCatalog.Scenes.Count > 0)
        {
            var initialSceneId = _shellScene.Length > 0 ? _shellScene : sceneCatalog.Scenes[0].Id;
            var unavailableRequestedScene = !sceneCatalog.Scenes.Any(scene => scene.Id.Equals(initialSceneId, StringComparison.Ordinal));
            if (unavailableRequestedScene)
            {
                // A stale shortcut/review argument must not abort _Ready and
                // leave a half-built shell displaying "Loading project".
                GD.PushWarning($"Startup scene '{initialSceneId}' is unavailable; loading the default scene.");
                initialSceneId = sceneCatalog.Scenes[0].Id;
            }
            AddMigratedScene(
                initialSceneId,
                candidates,
                camera,
                showRuntimeControls: false,
                autoRun: false
            );
            if (unavailableRequestedScene)
                _simulatorShell.SetWorkspaceStatus($"Startup scene '{_shellScene}' unavailable · default scene loaded");
        }
        if (_visualSceneReview) AddVisualSceneReviewControls();
        if (_nativeMotionMovie) CallDeferred(nameof(StartNativeMotionMovie));
        if (_reportSceneGeometry)
        {
            CallDeferred(nameof(ReportSceneGeometry));
        }
        else if (_verifyCartonStaticRoutes)
        {
            CallDeferred(nameof(VerifyCartonStaticRoutesOnly));
        }
        else if (_verifyToteFinishing)
        {
            CallDeferred(nameof(VerifyToteFinishingOnly));
        }
        else if (_auditDualSpindle)
        {
            CallDeferred(nameof(AuditDualSpindle));
        }
        else if (_auditRobotCnc)
        {
            CallDeferred(nameof(AuditRobotCnc));
        }
        else if (_auditRobotRestart)
        {
            CallDeferred(nameof(AuditRobotRestart));
        }
        else if (_auditSequenceTower)
        {
            CallDeferred(nameof(AuditSequenceTower));
        }
        else if (_auditPalletizer)
        {
            CallDeferred(nameof(AuditPalletizer));
        }
        else if (_auditGuardedLayout)
        {
            CallDeferred(nameof(AuditGuardedLayout));
        }
        else if (_auditRadarLayout)
        {
            CallDeferred(nameof(AuditRadarLayout));
        }
        else if (_auditWastewaterProcess)
        {
            CallDeferred(nameof(AuditWastewaterProcess));
        }
        else if (_auditMobileTraffic)
        {
            CallDeferred(nameof(AuditMobileTraffic));
        }
        else if (_verifyScanInspectionDemo5)
        {
            CallDeferred(nameof(VerifyDemo5ScanInspectionEquivalence));
        }
        else if (_verifyScanInspection)
        {
            CallDeferred(nameof(VerifyScanInspection));
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-editor-view-state", StringComparer.Ordinal))
        {
            Callable.From((Action)(async () =>
            {
                try
                {
                    _simulatorShell!.SetReviewState("ladder", "");
                    var passed = await _simulatorShell.VerifyLadderBlockViewStateAsync();
                    GD.Print($"EDITOR_VIEW_STATE_VERIFY {(passed ? "PASS" : "FAIL")}"); GetTree().Quit(passed ? 0 : 1);
                }
                catch (Exception error) { GD.PushError("EDITOR_VIEW_STATE_VERIFY FAIL " + error); GetTree().Quit(1); }
            })).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-operand-placement", StringComparer.Ordinal))
        {
            Callable.From((Action)(async () =>
            {
                try
                {
                    var result = await _simulatorShell!.VerifyOperandEditorPlacementAsync();
                    GD.Print($"OPERAND_PLACEMENT_VERIFY {(result.Passed ? "PASS" : "FAIL")} {result.Detail}");
                    GetTree().Quit(result.Passed ? 0 : 1);
                }
                catch (Exception error) { GD.PushError("OPERAND_PLACEMENT_VERIFY FAIL " + error); GetTree().Quit(1); }
            })).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-tia-edit-sweep", StringComparer.Ordinal))
        {
            Callable.From((Action)(async () =>
            {
                try
                {
                    const string evidenceArgumentPrefix = "--edit-evidence=";
                    var evidencePath = OS.GetCmdlineUserArgs()
                        .FirstOrDefault(argument => argument.StartsWith(evidenceArgumentPrefix, StringComparison.Ordinal))?
                        .Substring(evidenceArgumentPrefix.Length);
                    var result = await _simulatorShell!.VerifyTiaUniversalEditSweepAsync(evidencePath);
                    GD.Print(result.Detail);
                    GetTree().Quit(result.Passed ? 0 : 1);
                }
                catch (Exception error)
                {
                    GD.PushError("TIA_EDIT_GATE_SWEEP_FAIL " + error);
                    GetTree().Quit(1);
                }
            })).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-tia-project-tree-gates", StringComparer.Ordinal))
        {
            Callable.From((Action)(async () =>
            {
                try
                {
                    _simulatorShell!.SetReviewState("ladder", string.Empty);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    const string evidenceArgumentPrefix = "--project-tree-evidence=";
                    var evidencePath = OS.GetCmdlineUserArgs()
                        .FirstOrDefault(argument => argument.StartsWith(evidenceArgumentPrefix, StringComparison.Ordinal))?
                        .Substring(evidenceArgumentPrefix.Length)
                        ?? ".tools/tia-project-tree-gates.json";
                    var result = await _simulatorShell.VerifyTiaProjectTreeGateSweepAsync(evidencePath);
                    GD.Print(result.Detail);
                    GetTree().Quit(result.Passed ? 0 : 1);
                }
                catch (Exception error)
                {
                    GD.PushError("TIA_PROJECT_TREE_GATE_SWEEP_FAIL " + error);
                    GetTree().Quit(1);
                }
            })).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-tia-editor-control-gates", StringComparer.Ordinal))
        {
            Callable.From((Action)(async () =>
            {
                try
                {
                    const string evidenceArgumentPrefix = "--editor-control-evidence=";
                    var evidencePath = OS.GetCmdlineUserArgs()
                        .FirstOrDefault(argument => argument.StartsWith(evidenceArgumentPrefix, StringComparison.Ordinal))?
                        .Substring(evidenceArgumentPrefix.Length);
                    var result = await _simulatorShell!.VerifyTiaEditorControlGatesAsync(evidencePath);
                    GD.Print(result.Detail);
                    GetTree().Quit(result.Passed ? 0 : 1);
                }
                catch (Exception error)
                {
                    GD.PushError("TIA_EDITOR_CONTROL_GATE_SWEEP_FAIL " + error);
                    GetTree().Quit(1);
                }
            })).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-editor-transactions", StringComparer.Ordinal))
        {
            Callable.From(() =>
            {
                var passed = _simulatorShell!.VerifyEditorTransactions(out var detail);
                GD.Print($"EDITOR_TRANSACTIONS_VERIFY {(passed ? "PASS" : "FAIL")} {detail}");
                GetTree().Quit(passed ? 0 : 1);
            }).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--review-call-authoring", StringComparer.Ordinal))
        {
            Callable.From(() => _simulatorShell!.SetupCallAuthoringReview(
                !OS.GetCmdlineUserArgs().Contains("--review-call-closed", StringComparer.Ordinal))).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--review-tia-empty-box", StringComparer.Ordinal))
        {
            Callable.From(() => _simulatorShell!.SetupEmptyInstructionReview(
                OS.GetCmdlineUserArgs().Contains("--review-tia-empty-box-chooser", StringComparer.Ordinal))).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-tia-empty-box", StringComparer.Ordinal))
        {
            Callable.From(() =>
            {
                var passed = _simulatorShell!.VerifyEmptyInstructionReplacement();
                GD.Print($"TIA_EMPTY_INSTRUCTION_BOX_VERIFY {(passed ? "PASS" : "FAIL")}");
                GetTree().Quit(passed ? 0 : 1);
            }).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-call-authoring", StringComparer.Ordinal))
        {
            Callable.From(() =>
            {
                try
                {
                    var passed = _simulatorShell!.VerifyCallAuthoring();
                    GD.Print($"CALL_AUTHORING_VERIFY {(passed ? "PASS" : "FAIL")}");
                    GetTree().Quit(passed ? 0 : 1);
                }
                catch (Exception error) { GD.PushError("CALL_AUTHORING_VERIFY FAIL " + error); GetTree().Quit(1); }
            }).CallDeferred();
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-editor-monitoring", StringComparer.Ordinal))
        {
            CallDeferred(nameof(VerifyEditorMonitoring));
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-counter-monitor", StringComparer.Ordinal))
        {
            CallDeferred(nameof(VerifyCounterMonitor));
        }
        else if (_verifyFiveDemoLearner)
        {
            CallDeferred(nameof(VerifyFiveDemoLearner));
        }
        else if (_verifyWorkspaceMappingDetach)
        {
            CallDeferred(nameof(VerifyWorkspaceMappingDetach));
        }
        else if (_verifyRepeatCycleLearner)
        {
            CallDeferred(nameof(VerifyRepeatCycleLearner));
        }
        else if (_verifyRepeatCycleLifecycle)
        {
            CallDeferred(nameof(VerifyRepeatCycleLifecycle));
        }
        else if (_verifyWorkspaceRoller)
        {
            CallDeferred(nameof(VerifyWorkspaceRoller));
        }
        else if (_verifyWorkspaceRollerExternalGeometry)
        {
            CallDeferred(nameof(VerifyWorkspaceRollerExternalGeometry));
        }
        else if (_verifyWorkspaceRollerGeometry)
        {
            CallDeferred(nameof(VerifyWorkspaceRollerGeometry));
        }
        else if (_verifyWorkspaceDriveRetirement)
        {
            CallDeferred(nameof(VerifyWorkspaceDriveRetirement));
        }
        else if (_verifyWorkspaceDriveAuthoring)
        {
            CallDeferred(nameof(VerifyWorkspaceDriveAuthoring));
        }
        else if (_verifyWorkspaceDrive)
        {
            CallDeferred(nameof(VerifyWorkspaceDrive));
        }
        else if (_verifyWorkspaceBelt)
        {
            CallDeferred(nameof(VerifyWorkspaceBelt));
        }
        else if (_verifyMotorConveyorZones)
        {
            CallDeferred(nameof(VerifyMotorConveyorZones));
        }
        else if (_auditMotorAggregates)
        {
            CallDeferred(nameof(AuditMotorAggregates));
        }
        else if (_auditServiceElevatorAccess)
        {
            CallDeferred(nameof(AuditServiceElevatorAccess));
        }
        else if (_auditToteFinishingProcess)
        {
            CallDeferred(nameof(AuditToteFinishingProcess));
        }
        else if (_auditEvLayout)
        {
            CallDeferred(nameof(AuditEvLayout));
        }
        else if (_auditLuggageLayout)
        {
            CallDeferred(nameof(AuditLuggageLayout));
        }
        else if (_auditHandDryer)
        {
            CallDeferred(nameof(AuditHandDryer));
        }
        else if (_auditCoating)
        {
            CallDeferred(nameof(AuditCoating));
        }
        else if (_auditBagIndex)
        {
            CallDeferred(nameof(AuditBagIndex));
        }
        else if (_auditDrawbridge)
        {
            CallDeferred(nameof(AuditDrawbridge));
        }
        else if (_auditPedestrianCrossing)
        {
            CallDeferred(nameof(AuditPedestrianCrossing));
        }
        else if (_auditRunningTower)
        {
            CallDeferred(nameof(AuditRunningTower));
        }
        else if (_auditFlashPair)
        {
            CallDeferred(nameof(AuditFlashPair));
        }
        else if (_auditRotaryFlasher)
        {
            CallDeferred(nameof(AuditRotaryFlasher));
        }
        else if (_auditTimerLessons)
        {
            CallDeferred(nameof(AuditTimerLessons));
        }
        else if (_auditPackageGrouping)
        {
            CallDeferred(nameof(AuditPackageGrouping));
        }
        else if (_auditParkingEntry)
        {
            CallDeferred(nameof(AuditParkingEntry));
        }
        else if (_auditButtonCounters)
        {
            CallDeferred(nameof(AuditButtonCounters));
        }
        else if (_auditRepeatCycle)
        {
            CallDeferred(nameof(AuditRepeatCycle));
        }
        else if (_auditCableCut)
        {
            CallDeferred(nameof(AuditCableCut));
        }
        else if (_auditBarrelFill)
        {
            CallDeferred(nameof(AuditBarrelFill));
        }
        else if (_auditCookiePackaging)
        {
            CallDeferred(nameof(AuditCookiePackaging));
        }
        else if (_auditChainLiftInstallation)
        {
            CallDeferred(nameof(AuditChainLiftInstallation));
        }
        else if (_verifySceneGeometry)
        {
            CallDeferred(nameof(VerifySceneGeometry));
        }
        else if (_verifyHud)
        {
            CreateDemoConnections(candidates);
            _simulatorShell.SetReviewState("connections", string.Empty);
            CallDeferred(nameof(VerifyHud));
        }
        else if (_verifyWorkspace)
        {
            CallDeferred(nameof(VerifyWorkspace));
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-industrial-mounts", StringComparer.Ordinal))
        {
            CallDeferred(nameof(VerifyIndustrialMounts));
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-control-installations", StringComparer.Ordinal))
        {
            CallDeferred(nameof(VerifyControlInstallations));
        }
        else if (OS.GetCmdlineUserArgs().Contains("--verify-scene-environments", StringComparer.Ordinal))
        {
            CallDeferred(nameof(VerifyCatalogEnvironments));
        }
        else if (_verifyAppShell)
        {
            CallDeferred(nameof(VerifyAppShell));
        }
        else if (_verifyPlantMotion)
        {
            CallDeferred(nameof(VerifyPlantMotion));
        }
        else if (_verifyExternalPlayback)
        {
            CallDeferred(nameof(VerifyExternalPlayback));
        }
        else if (_verifyExternalDialog)
        {
            CallDeferred(nameof(VerifyExternalDialog));
        }
        else if (_verifyCameraInput)
        {
            CallDeferred(nameof(VerifyCameraInput));
        }
        else if (_verifySceneControls)
        {
            CallDeferred(nameof(VerifySceneControls));
        }
        else if (_verifyGalleryClock)
        {
            CallDeferred(nameof(VerifyGalleryClock));
        }
        else if (_verifyTypedHistoryPresentation)
        {
            CallDeferred(nameof(VerifyTypedHistoryPresentation));
        }
        else if (_verifyTypedLearnerWorkflow)
        {
            CallDeferred(nameof(VerifyTypedLearnerWorkflow));
        }
        else if (_verifyTypedLessons)
        {
            CallDeferred(nameof(VerifyTypedLessons));
        }
        else if (_verifyDemoSequence)
        {
            CallDeferred(nameof(VerifyDemoSequence));
        }
        else if (_verifyVirtualController)
        {
            CallDeferred(nameof(VerifyVirtualController));
        }
        else if (_verifyNumericSceneIo)
        {
            CallDeferred(nameof(VerifyNumericSceneIo));
        }
        else if (_verifyLadderEditor)
        {
            CallDeferred(nameof(VerifyLadderEditor));
        }
        else if (_verifyProjectTreePointer)
        {
            CallDeferred(nameof(VerifyProjectTreePointer));
        }
        else if (_verifySplitView)
        {
            CallDeferred(nameof(VerifySplitView));
        }
        else if (_verifyUiDensity)
        {
            CallDeferred(nameof(VerifyUiDensity));
        }
        else if (_virtualControllerDemo)
        {
            EnableVirtualControllerDemo();
            _virtualController?.PulseInput("start_command");
            RunActiveController();
            if (_shellView.Length > 0) _simulatorShell.SetReviewState(_shellView, _shellSearch);
        }
        else if (_shellView.Length > 0 || _shellSearch.Length > 0)
        {
            if (_shellDemoConnections)
            {
                CreateDemoConnections(candidates);
            }
            else if (_shellPlaceAsset.Length > 0)
            {
                var asset = candidates.Assets.FirstOrDefault(item => item.Id == _shellPlaceAsset)
                    ?? throw new InvalidOperationException($"Unknown shell review asset '{_shellPlaceAsset}'.");
                PlaceAsset(asset);
            }
            if (Enum.TryParse<WorkspaceTransformMode>(_shellTransformMode, true, out var reviewMode))
                SetTransformMode(reviewMode);
            if (Enum.TryParse<WorkspaceTransformSpace>(_shellTransformSpace, true, out var reviewSpace))
                SetTransformSpace(reviewSpace);
            if (_shellSelectAll) SetSelection(_workspaceNodes.Keys.ToArray());
            if (_shellGroupSelection) GroupSelection();
            if (_shellNestedGroups) CreateNestedGroupReviewAssembly(candidates);
            if (_shellDemoLocalAxes && _selectedPlacementId is not null
                && _workspaceNodes.TryGetValue(_selectedPlacementId, out var reviewPlacement))
            {
                reviewPlacement.Node.RotationDegrees = new Vector3(0, 35, 0);
                SetTransformSpace(WorkspaceTransformSpace.Local);
                RefreshWorkspaceUi();
            }
            if (_shellDemoMarquee) CallDeferred(nameof(ShowReviewMarquee));
            if (_shellDemoArrangeMenu) _simulatorShell.CallDeferred(nameof(SimulatorShell.ShowArrangeMenu));
            if (_shellDemoGroupRename) _simulatorShell.CallDeferred(nameof(SimulatorShell.ShowRenameGroupDialog));
            if (_shellDemoGroupPivot) _simulatorShell.CallDeferred(nameof(SimulatorShell.ShowPivotDialog));
            _simulatorShell.SetReviewState(_shellView, _shellSearch);
        }
        else
        {
            // Startup acknowledgement is interactive-only; it must not cover
            // deterministic verification or capture output.
            if (_capturePath is null && !_nativeMotionMovie && !_verifyChallengeUi && !_verifyConveyorChallenge)
                _simulatorShell.CallDeferred(nameof(SimulatorShell.ShowSimulatorModeNotice));
        }
        if (_verifyConveyorChallenge) CallDeferred(nameof(VerifyConveyorChallenge));
        if (_verifyChallengeUi) CallDeferred(nameof(VerifyChallengeUi));
        if (_captureEnterChallenge) EnterConveyorChallenge();
        if (_captureRunAuthoredDemo)
        {
            RunActiveController();
            PulseVirtualInput("operator.start", "start_command");
        }
        if (_capturePath is not null)
        {
            // Optional supplemental evidence uses the same declared runtime
            // action as a user click. Never inject PLC outputs or persist this
            // fixture state into the authored scene or ordinary startup.
            foreach (var argument in OS.GetCmdlineUserArgs())
            {
                const string prefix = "--capture-action=";
                if (!argument.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var actionId = argument[prefix.Length..];
                if (_sceneRuntime is null || !_sceneRuntime.ExecuteAction(actionId))
                {
                    GD.PushError("RUNTIME_CAPTURE_ACTION_REJECTED " + actionId);
                    GetTree().Quit(2);
                    return;
                }
                GD.Print("RUNTIME_CAPTURE_ACTION_APPLIED " + actionId);
            }
        }
        if (_capturePath is not null && _shellView.Length > 0) _simulatorShell.SetReviewState(_shellView, _shellSearch);
        if (_capturePath is not null && OS.GetCmdlineUserArgs().Contains("--capture-block-creation-dialog", StringComparer.Ordinal))
            _simulatorShell.CallDeferred(nameof(SimulatorShell.PrepareBlockCreationDialogCapture));
        if (_tenZoneCaptureState is not null) CallDeferred(nameof(PrepareTenZoneCaptureReview));
        if (_templateCapturePending) CallDeferred(nameof(PrepareTemplateCaptureReview));
        if (_indexedTraceCapturePending) CallDeferred(nameof(PrepareIndexedDeviceTraceCapture));
        if (_deviceTraceCapturePending) CallDeferred(nameof(PrepareDeviceTraceCaptureReview));
        if (_workspaceBeltCapturePending) CallDeferred(nameof(PrepareWorkspaceBeltCapture));
        if (_workspaceRollerCapturePending) CallDeferred(nameof(PrepareWorkspaceRollerCapture));
        if (_demo5LearnerCapturePending) CallDeferred(nameof(PrepareDemo5LearnerCapture));
        if (_scanInspectionCapturePending) CallDeferred(nameof(PrepareScanInspectionCapture));
        if (_repeatLearnerCapturePending) CallDeferred(nameof(PrepareRepeatLearnerCapture));
        if (_typedLearnerCapturePending) CallDeferred(nameof(PrepareTypedLearnerCapture));
        if (_workspaceDriveCapturePending) CallDeferred(nameof(PrepareWorkspaceDriveCapture));
    }

    private void CreateDemoConnections(AssetCatalogDocument candidates)
    {
        var conveyor = candidates.Assets.First(item => item.Id == "material-handling.belt-conveyor.600x6000.v1");
        var photoeye = candidates.Assets.First(item => item.Id == "sensing.photoelectric.through-beam.v1");
        PlaceAsset(conveyor);
        var conveyorInstance = _selectedPlacementId!;
        PlaceAsset(photoeye);
        var photoeyeInstance = _selectedPlacementId!;
        CreateConnectorLink(conveyorInstance, "material_out", photoeyeInstance, "beam_center");
        CreateSignalLink(conveyorInstance, "run_command", "conveyor_run");
    }

    private void EnableVirtualControllerDemo()
    {
        if (_candidateCatalog is null || _mainCamera is null || _simulatorShell is null) return;
        var path = ProjectSettings.GlobalizePath("res://programs/demo-conveyor.ld.json");
        var loaded = LadderProgramJson.Load(System.IO.File.ReadAllText(path));
        if (!loaded.IsValid || loaded.Program is null)
        {
            _simulatorShell.SetVirtualControllerValidation(loaded.Issues);
            return;
        }
        if (_currentSceneId != DemoPrograms.ConveyorSceneId)
        {
            AddMigratedScene(DemoPrograms.ConveyorSceneId, _candidateCatalog, _mainCamera,
                showRuntimeControls: false, autoRun: false);
        }
        EnableVirtualControllerProgram(loaded.Program);
    }

    private void EnableVirtualControllerProgram(LadderProgram program) => EnableVirtualControllerProgram(program, resetScene: true);

    private void EnableVirtualControllerProgram(LadderProgram program, bool resetScene)
    {
        if (_candidateCatalog is null || _mainCamera is null || _simulatorShell is null) return;
        if (_simulatorShell.IsExternalMode)
        {
            _simulatorShell.SetWorkspaceStatus("Select Built-in Simulator before loading a local controller.", isError: true);
            return;
        }
        var repeatCycleResetHandoff = resetScene ? null : CaptureRepeatCycleResetHandoff(program);
        if (_virtualController is not null) DisableVirtualController();
        if (_sceneRuntime is null) return;

        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid || compiled.Program is null)
        {
            _simulatorShell.SetVirtualControllerValidation(compiled.Issues);
            return;
        }

        _virtualProgram = program;
        _virtualController = new VirtualControllerSession(new VirtualControllerRuntime(compiled.Program));
        _scanInspectionEpoch++;
        _virtualController.SnapshotPublished += OnVirtualSnapshot;
        _virtualController.PublicationPublished += OnInstructionPublication;
        _virtualController.ScanCompleted += OnChallengeLiveScan;
        AttachWorkspaceTrialObserver(_virtualController);
        _sceneRuntime.UsesExternalClock = true;
        if (resetScene) _sceneRuntime.ResetSimulation();
        _virtualSnapshot = _virtualController.Reset();
        CommitVirtualControllerSnapshot(_virtualSnapshot);
        _simulatorShell.AttachVirtualController(_virtualProgram, _virtualSnapshot);
        _simulatorShell.UpdateInstructionPublication(_virtualController.Publication);
        if (resetScene) ResetRepeatCycleTrial();
        else { RestoreRepeatCycleResetHandoff(repeatCycleResetHandoff); BindRepeatCycleTrial(); }
        if (resetScene) ResetWorkspaceObservedTrial("Controller load reset");
        else BeginWorkspaceObservedTrial("Current draft loaded");
        if (resetScene) ResetWorkspaceRollerObservedTrial("Controller load reset");
        else BeginWorkspaceRollerObservedTrial("Current draft loaded");
        GD.Print($"VIRTUAL_CONTROLLER_ENABLED mode=offline scene={_currentSceneId} transport=none");
    }

    private bool SceneRequiresControllerClock => _simulatorShell is not null
        && (_simulatorShell.IsExternalMode || _sceneRuntime?.RuntimeType != "gallery");

    private void DisableVirtualController()
    {
        StopWorkspaceDrive();
        ReleaseScanPauseModes();
        RetireRepeatCycleTrial("Controller detached or replaced; Reset is required.");
        DetachWorkspaceTrialObserver();
        if (_workspaceObservedTrial is not null) _workspaceObservedTrial = _workspaceObservedTrial.Invalidate("Controller detached; observation is historical.");
        if (_workspaceRollerObservedTrial is not null) _workspaceRollerObservedTrial = _workspaceRollerObservedTrial.Invalidate("Controller detached; observation is historical.");
        if (_virtualController is not null) { _virtualController.SnapshotPublished -= OnVirtualSnapshot; _virtualController.PublicationPublished -= OnInstructionPublication; _virtualController.ScanCompleted -= OnChallengeLiveScan; }
        _challengeLiveScan = null;
        _virtualController = null;
        _simulatorShell?.UpdateInstructionPublication(null);
        _virtualProgram = null;
        _virtualSnapshot = null;
        if (_sceneRuntime is not null)
        {
            _sceneRuntime.SetControllerPlaybackRunning(false);
            _sceneRuntime.UsesExternalClock = SceneRequiresControllerClock;
            _sceneRuntime.StopSimulation();
        }
        _simulatorShell?.DetachVirtualController();
        PublishScanInspectionState();
        GD.Print("VIRTUAL_CONTROLLER_DISABLED");
    }

    private void OnInstructionPublication(ControllerPublication publication)
        => _simulatorShell?.UpdateInstructionPublication(publication);

    private void OnVirtualSnapshot(VirtualControllerSnapshot snapshot)
    {
        _virtualSnapshot = snapshot;
        _sceneRuntime?.SetControllerPlaybackRunning(snapshot.State == VirtualControllerState.Running);
        _simulatorShell?.UpdateVirtualController(snapshot);
        UpdateGantryReviewClockLabel();
        PublishScanInspectionState();
    }

    private void PulseVirtualInput(string binding, string fallbackName)
    {
        if (_virtualController is null) return;
        var variable = _virtualProgram?.Variables.FirstOrDefault(item =>
            item.Role == PlcVariableRole.Input
            && item.Binding.Equals(binding, StringComparison.Ordinal));
        if (_virtualController.IsPaused && (!ScanInspectionCurrentDraftMatches() || variable?.Type != PlcVariableType.Bool))
        {
            _simulatorShell?.SetWorkspaceStatus("Input blocked · paused draft must match a declared BOOL input binding", true);
            return;
        }
        _virtualController.PulseInput(variable?.Name ?? fallbackName);
    }

    private void ApplyVirtualForce(string variableName, bool value)
    {
        if (BlockPausedForceChange()) return;
        if (_virtualController is null || _simulatorShell is null) return;
        try
        {
            var snapshot = _virtualController.SetBoolForce(variableName, value);
            RetireRepeatCycleTrial("Force used; Reset is required even after the force is cleared.");
            CommitVirtualControllerSnapshot(snapshot);
            GD.Print($"VIRTUAL_FORCE_SET variable={variableName} value={value} scope=offline-simulator transport=none");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _simulatorShell.SetVirtualControllerForceError(exception.Message);
        }
    }

    private bool ModifyVirtualWatchValue(IReadOnlyList<(string VariableName, object Value)> edits)
    {
        if (_virtualController is null || _simulatorShell is null) return false;
        if (_virtualController.IsPaused)
        {
            _simulatorShell.SetWorkspaceStatus("SIM MODIFY blocked · resume or stop the offline runtime before changing a value", true);
            return false;
        }
        try
        {
            var snapshot = _virtualController.ModifyVariables(edits);
            RetireRepeatCycleTrial("Watch-table value modified; Reset is required before repeating the cycle.");
            CommitVirtualControllerSnapshot(snapshot);
            GD.Print($"VIRTUAL_WATCH_MODIFY count={edits.Count} scope=offline-simulator transport=none");
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _simulatorShell.SetWorkspaceStatus($"SIM MODIFY blocked · {exception.Message}", true);
            return false;
        }
    }

    private void RemoveVirtualForce(string variableName)
    {
        if (BlockPausedForceChange()) return;
        if (_virtualController is null) return;
        var snapshot = _virtualController.RemoveForce(variableName);
        CommitVirtualControllerSnapshot(snapshot);
        GD.Print($"VIRTUAL_FORCE_REMOVED variable={variableName} scope=offline-simulator transport=none");
    }

    private void ClearVirtualForces()
    {
        if (BlockPausedForceChange()) return;
        if (_virtualController is null) return;
        var snapshot = _virtualController.ClearForces();
        CommitVirtualControllerSnapshot(snapshot);
        GD.Print("VIRTUAL_FORCES_CLEARED scope=offline-simulator transport=none");
    }

    private IReadOnlyDictionary<string, bool> SampleVirtualControllerInputs()
    {
        var sceneInputs = _sceneRuntime?.SampleVirtualControllerInputs()
            ?? new Dictionary<string, bool>();
        if (_virtualProgram is null) return sceneInputs;
        return SceneIoImageMapper.SampleBoolInputs(_virtualProgram, sceneInputs);
    }

    private IReadOnlyDictionary<string, double> SampleVirtualControllerNumericInputs()
    {
        var sceneInputs = _sceneRuntime?.SampleVirtualControllerNumericInputs()
            ?? new Dictionary<string, double>();
        if (_virtualProgram is null) return sceneInputs;
        return SceneIoImageMapper.SampleNumericInputs(_virtualProgram, sceneInputs);
    }

    private IReadOnlyDictionary<string, string> SampleVirtualControllerTextInputs()
    {
        var inputs = _sceneRuntime?.SampleVirtualControllerTextInputs() ?? new Dictionary<string, string>();
        return _virtualProgram is null ? inputs : SceneIoImageMapper.SampleTextInputs(_virtualProgram, inputs);
    }

    private void CommitVirtualControllerTextOutputs(IReadOnlyDictionary<string, string> outputs)
    {
        if (_sceneRuntime is not null && _virtualProgram is not null)
            _sceneRuntime.CommitVirtualControllerTextOutputs(SceneIoImageMapper.CommitTextOutputs(_virtualProgram, outputs));
    }

    private void CommitVirtualControllerOutputs(IReadOnlyDictionary<string, bool> outputs)
    {
        if (_sceneRuntime is null || _virtualProgram is null) return;
        _sceneRuntime.CommitVirtualControllerOutputs(
            SceneIoImageMapper.CommitBoolOutputs(_virtualProgram, outputs));
    }

    private void CommitVirtualControllerNumericOutputs(IReadOnlyDictionary<string, double> outputs)
    {
        if (_sceneRuntime is null || _virtualProgram is null) return;
        _sceneRuntime.CommitVirtualControllerNumericOutputs(
            SceneIoImageMapper.CommitNumericOutputs(_virtualProgram, outputs));
    }

    private void CommitVirtualControllerSnapshot(VirtualControllerSnapshot snapshot)
    {
        CommitVirtualControllerOutputs(snapshot.Outputs);
        CommitVirtualControllerNumericOutputs(snapshot.NumericOutputs);
        CommitVirtualControllerTextOutputs(snapshot.TextOutputs);
        // SnapshotPublished precedes output projection. Held inspection has no
        // later physics callback to refresh the label after Stop clears outputs.
        UpdateGantryReviewClockLabel();
    }

    private async void VerifyExternalPlayback()
    {
        try
        {
            if (_externalConnection is null || _simulatorShell is null || _candidateCatalog is null || _mainCamera is null)
                throw new InvalidOperationException("Offline playback verifier requires the application shell.");
            AddMigratedScene("scene-1-conveyor-stop", _candidateCatalog, _mainCamera, false, false);
            var runtime = _sceneRuntime!;
            var menu = _simulatorShell.GetNode<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlcMenu").GetPopup();
            menu.EmitSignal(PopupMenu.SignalName.IdPressed, 11);
            var descriptor = JsonSerializer.SerializeToElement(new
            {
                id = "offline.json", sceneId = _currentSceneId, cycleMs = 20, heartbeatTimeoutMs = 2000,
                pcPointScope = new[] { new { name = "simulated_photoeye", dataType = "BOOL" } },
                plcPointScope = new[] { new { name = "conveyor_running", dataType = "BOOL" } },
            });
            ExternalSceneContract.Validate(_activeSceneDefinition!.Simulation, descriptor);
            _externalConnection.Connect("offline.json", _currentSceneId!, [], _ => { }, ReportExternalExchangeFailure, 100, descriptor);
            async System.Threading.Tasks.Task WaitFor(Func<bool> condition)
            {
                var deadline = System.Diagnostics.Stopwatch.StartNew();
                while (deadline.Elapsed < TimeSpan.FromSeconds(8))
                {
                    if (condition()) return;
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                throw new InvalidOperationException("Offline exchange did not reach its expected state.");
            }
            long Cycle() => _externalConnection.LatestCycle?.GetProperty("cycle").GetInt64() ?? 0;
            double Position() => Convert.ToDouble(runtime.Points["object_position"]);
            void Check(bool condition, string label)
            {
                GD.Print($"EXTERNAL_PLAYBACK_CHECK {label}={condition}");
                if (!condition) throw new InvalidOperationException(label);
            }
            await WaitFor(() => Cycle() >= 4);
            Check(Position() == 0, "connect_exchanges_without_playback");
            RunActiveController();
            await WaitFor(() => Position() > 0.03);
            StopActiveController();
            var pausedPosition = Position();
            var pausedCycle = Cycle();
            var drum = _sceneCompositionRoot!.FindChildren("KIN_drive_drum*", "Node3D", true, false).OfType<Node3D>().First();
            var drumPose = drum.Transform;
            await WaitFor(() => Cycle() >= pausedCycle + 4);
            Check(Position() == pausedPosition && drum.Transform == drumPose, "stop_freezes_plant_and_equipment_but_keeps_exchange");
            ResetActiveController();
            var resetCycle = Cycle();
            await WaitFor(() => Cycle() >= resetCycle + 3);
            Check(Position() == 0 && runtime.Points["conveyor_running"] is true, "reset_stays_paused_preserves_controller_image_and_exchange");
            RunActiveController();
            await WaitFor(() => Position() > 0.02);
            System.IO.File.WriteAllText(_offlinePlaybackModeFile!, "disabled");
            await WaitFor(() => _externalConnection.LatestCycle?.GetProperty("plcStatus").GetProperty("simulation_enable").GetBoolean() == false);
            var heldPosition = Position();
            var heldCycle = Cycle();
            await WaitFor(() => Cycle() >= heldCycle + 3);
            Check(Position() == heldPosition && runtime.Points["conveyor_running"] is true,
                "readiness_loss_holds_plant_and_rejects_unready_output_image");
            ResetActiveController();
            RunActiveController();
            var blockedCycle = Cycle();
            await WaitFor(() => Cycle() >= blockedCycle + 3);
            Check(Position() == 0, "run_blocked_while_not_ready");
            System.IO.File.WriteAllText(_offlinePlaybackModeFile!, "ready");
            await WaitFor(() => _externalConnection.LatestCycle?.GetProperty("plcStatus").GetProperty("simulation_enable").GetBoolean() == true);
            var recoveredCycle = Cycle();
            await WaitFor(() => Cycle() >= recoveredCycle + 3);
            Check(Position() == 0, "readiness_recovery_requires_new_run");
            RunActiveController();
            await WaitFor(() => Position() > 0.02);
            _externalConnection.Disconnect();
            var disconnectedPosition = Position();
            await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
            Check(Position() == disconnectedPosition && _externalConnection.State == ConnectionState.Disconnected,
                "disconnect_holds_playback");
            using var pulseDefinition = JsonDocument.Parse("""{"type":"fixture","points":[{"name":"feedback","owner":"PC","type":"BOOL","initial":false}],"actions":[{"id":"pulse","type":"pulse","point":"feedback"}]}""");
            var pulseRoot = new Node3D();
            var pulseRuntime = new SceneSimulationRuntime(pulseDefinition.RootElement, pulseRoot) { UsesExternalClock = true };
            AddChild(pulseRoot);
            AddChild(pulseRuntime);
            pulseRuntime.SetExternalPlayback(true, false);
            pulseRuntime.ExecuteAction("pulse");
            pulseRuntime.AdvanceSimulation(0.1);
            Check(pulseRuntime.Points["feedback"] is true, "paused_pulse_waits_for_exchange");
            pulseRuntime.ConsumeExternalInputPulses(new Dictionary<string, object?>());
            Check(pulseRuntime.Points["feedback"] is true, "unmapped_pulse_not_consumed");
            pulseRuntime.ConsumeExternalInputPulses(new Dictionary<string, object?> { ["feedback"] = true });
            Check(pulseRuntime.Points["feedback"] is false, "sampled_pulse_clears_without_playback");
            pulseRuntime.QueueFree();
            pulseRoot.QueueFree();
            GD.Print("EXTERNAL_PLAYBACK_VERIFY PASS offline-only; no PLC adapter or network");
            if (OS.GetCmdlineUserArgs().Contains("--keep-playback-review-open", StringComparer.Ordinal))
            {
                await WaitFor(() => !_externalConnection.IsBusy);
                _externalConnection.Connect("offline.json", _currentSceneId!, [], _ => { }, ReportExternalExchangeFailure, 100, descriptor);
                await WaitFor(() => Cycle() >= 3);
                _simulatorShell.SetWorkspaceStatus("OFFLINE PLAYBACK REVIEW · fake bridge only · paused; use Run, Stop and Reset");
                return;
            }
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"EXTERNAL_PLAYBACK_VERIFY FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    // Human Run always executes the current editor draft. Diagnostic fixtures
    // deliberately use RunActiveController after explicitly loading a program.
    private void RunEditorDraftController()
    {
        if (!ValidateWorkspaceDriveRun()) return;
        if (_simulatorShell is null || _simulatorShell.IsExternalMode)
        {
            RunActiveController();
            return;
        }
        if (_simulatorShell.IsChallengeDraftActive)
        {
            RunActiveController(); // Challenge branch validates its own draft.
            return;
        }
        if (!_simulatorShell.TryBuildCurrentLadderProgram(out var draft, out var issues))
        {
            StopActiveController();
            _simulatorShell.SetVirtualControllerValidation(issues);
            _simulatorShell.SetWorkspaceStatus("RUN BLOCKED · current ladder draft is invalid", isError: true);
            return;
        }
        if (_sceneRuntime?.HasLatchedEmergencyStop == true)
        {
            RunActiveController(); // Retain the existing explicit reset gate.
            return;
        }
        ReleaseScanPauseModes();
        // Watch metadata does not alter execution. Match the monitor's existing
        // execution identity so a watch-only edit cannot reset program memory.
        if (_virtualProgram is null || LadderProgramJson.Save(_virtualProgram with { WatchVariables = null }) != LadderProgramJson.Save(draft with { WatchVariables = null }))
            EnableVirtualControllerProgram(draft, resetScene: false);
        RunActiveController();
    }

    private void RunActiveController()
    {
        if (!ValidateWorkspaceDriveRun()) return;
        ReleaseScanPauseModes();
        if (_simulatorShell?.IsChallengeDraftActive == true)
        {
            if (!_simulatorShell.TryBuildChallengeDraft(out var learnerProgram, out var challengeIssues))
            { StopActiveController(); _simulatorShell.SetChallengeValidationIssues(challengeIssues); _simulatorShell.SetWorkspaceStatus("Invalid challenge draft: " + string.Join("; ", challengeIssues), true); return; }
            _simulatorShell.SetChallengeValidationIssues(System.Array.Empty<string>());
            if (_virtualProgram is null || LadderProgramJson.Save(_virtualProgram) != LadderProgramJson.Save(learnerProgram))
                EnableVirtualControllerProgram(learnerProgram, resetScene: false);
            _virtualController?.Run();
            return;
        }
        if (_simulatorShell?.IsExternalMode == true)
        {
            SynchronizeExternalPlayback();
            if (!_externalPlayback.TryRun())
            {
                _simulatorShell.SetWorkspaceStatus(
                    $"RUN BLOCKED · External PLC readiness: {_externalPlayback.ReadinessLabel}",
                    isError: true);
                return;
            }
            SynchronizeExternalPlayback();
            _simulatorShell.SetWorkspaceStatus("Scene playback running · PLC exchange continues; PLC commands remain controller-owned");
            return;
        }
        if (_sceneRuntime?.HasLatchedEmergencyStop == true)
        {
            _simulatorShell?.SetWorkspaceStatus("RUN BLOCKED · explicitly reset the simulated E-stop first", isError: true);
            return;
        }
        if (_virtualController is null && _simulatorShell is not null)
        {
            if (!_simulatorShell.TryBuildCurrentLadderProgram(out var currentProgram, out var validationIssues))
            {
                _simulatorShell.SetVirtualControllerValidation(validationIssues);
                return;
            }
            // First Run loads the authored controller without discarding the
            // feedback the operator just established. Reset remains explicit.
            EnableVirtualControllerProgram(currentProgram, resetScene: false);
        }
        if (_virtualController is not null)
        {
            _virtualController.Run();
            return;
        }
        _sceneRuntime?.RunDefault();
    }

    private void StopActiveController()
    {
        StopWorkspaceDrive();
        ReleaseScanPauseModes();
        _challengeCoordinator?.Cancel();
        _demoEnvironment?.SetAmbientMotionEnabled(false);
        if (_simulatorShell?.IsExternalMode == true)
        {
            _externalPlayback.Pause();
            SynchronizeExternalPlayback();
            _simulatorShell.SetWorkspaceStatus("Scene playback paused · PLC connection and heartbeat exchange remain active");
            return;
        }
        _sceneRuntime?.DiscardPendingLocalInputPulses();
        StopRepeatCycleTrial();
        StopWorkspaceObservedTrial();
        StopWorkspaceRollerObservedTrial();
        if (_virtualController is not null && _sceneRuntime is not null)
        {
            var snapshot = _virtualController.Stop();
            CommitVirtualControllerSnapshot(snapshot);
            PublishWorkspaceObservedTrial();
            PublishWorkspaceRollerObservedTrial();
            return;
        }
        _sceneRuntime?.StopSimulation();
    }

    private void ResetActiveController()
    {
        ResetWorkspaceDrive();
        ReleaseScanPauseModes();
        _scanInspectionEpoch++;
        if (_challengeCoordinator?.State.IsWorkerOutstanding == true) { _challengeCoordinator.RequestAction(RungProof.Next.Challenges.ChallengePendingAction.Reset); return; }
        _demoEnvironment?.SetAmbientMotionEnabled(false);
        if (_simulatorShell?.IsExternalMode == true)
        {
            _externalPlayback.Pause();
            SynchronizeExternalPlayback();
            _sceneRuntime?.ResetExternalPlant();
            ReframeResetReviewFocus();
            _simulatorShell.SetWorkspaceStatus("Scene reset and paused · PLC connection and heartbeat exchange remain active");
            return;
        }
        if (_virtualController is not null && _sceneRuntime is not null)
        {
            _sceneRuntime.ResetSimulation();
            var snapshot = _virtualController.Reset();
            CommitVirtualControllerSnapshot(snapshot);
            ResetRepeatCycleTrial();
            ResetWorkspaceObservedTrial("Reset");
            ResetWorkspaceRollerObservedTrial("Reset");
            _simulatorShell?.SetWorkspaceStatus("Built-in controller reset · stopped · new machine Start required");
            ReframeResetReviewFocus();
            return;
        }
        _sceneRuntime?.ResetSimulation();
        ResetRepeatCycleTrial();
            ResetWorkspaceObservedTrial("Reset");
        ResetWorkspaceRollerObservedTrial("Reset");
        ReframeResetReviewFocus();
    }

    private void VerifyVirtualController()
    {
        EnableVirtualControllerDemo();
        if (_virtualController is null || _sceneRuntime is null || _simulatorShell is null)
        {
            GD.PushError("VIRTUAL_CONTROLLER_UI_VERIFY FAIL controller unavailable");
            GetTree().Quit(1);
            return;
        }
        _virtualController.PulseInput("start_command");
        _virtualController.Run();
        var operatorActions = _simulatorShell.GetNode<VBoxContainer>(
            "Workspace/LeftDock/LeftTabs/Operator/OperatorContent/OperatorActions");
        var originalButtons = operatorActions.GetChildren().OfType<Button>().ToArray();
        for (var index = 0; index < 90; index++)
        {
            _virtualController.Advance(
                _virtualController.ScanPeriod.TotalSeconds,
                SampleVirtualControllerInputs,
                SampleVirtualControllerNumericInputs,
                CommitVirtualControllerOutputs,
                CommitVirtualControllerNumericOutputs,
                AdvanceAcceptedScenePlant, SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs);
        }
        var photoeye = _sceneRuntime.Points.TryGetValue("simulated_photoeye", out var sensor) && sensor is true;
        var actionsStable = originalButtons.Length > 0
            && originalButtons.SequenceEqual(operatorActions.GetChildren().OfType<Button>())
            && originalButtons.All(button => !button.IsQueuedForDeletion());
        var stopped = _virtualSnapshot is not null
            && !_virtualSnapshot.Outputs.GetValueOrDefault("conveyor_running")
            && !_virtualSnapshot.Variables.GetValueOrDefault("seal_in");
        _virtualController.PulseInput("start_command");
        _virtualController.Advance(
            _virtualController.ScanPeriod.TotalSeconds,
            SampleVirtualControllerInputs,
            SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs,
            CommitVirtualControllerNumericOutputs,
            AdvanceAcceptedScenePlant, SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs,
            CaptureScanInspectionPlantInputs);
        var restartBlocked = _virtualSnapshot is not null
            && !_virtualSnapshot.Outputs.GetValueOrDefault("conveyor_running");
        var forcedSnapshot = _virtualController.SetBoolForce("conveyor_running", true);
        CommitVirtualControllerSnapshot(forcedSnapshot);
        var forcedOutput = forcedSnapshot.Outputs.GetValueOrDefault("conveyor_running")
            && forcedSnapshot.Forces.TryGetValue("conveyor_running", out var activeForce)
            && activeForce.Value
            && activeForce.Role == PlcVariableRole.Output;
        var ui = _simulatorShell.VerifyVirtualControllerUi(out var uiResult);
        var forceStoppedSnapshot = _virtualController.Stop();
        CommitVirtualControllerSnapshot(forceStoppedSnapshot);
        var forceSafeStop = !forceStoppedSnapshot.Outputs.GetValueOrDefault("conveyor_running")
            && forceStoppedSnapshot.Forces.ContainsKey("conveyor_running");
        ResetActiveController();
        var reset = _virtualSnapshot is
        {
            ScanNumber: 0,
            State: VirtualControllerState.Stopped,
        }
            && !_virtualSnapshot.Variables.GetValueOrDefault("seal_in")
            && !_virtualSnapshot.Outputs.GetValueOrDefault("conveyor_running")
            && _sceneRuntime.Points.TryGetValue("object_position", out var position)
            && Math.Abs(Convert.ToDouble(position ?? 0.0)) < 1e-9
            && _sceneRuntime.Points.TryGetValue("simulated_photoeye", out var resetSensor)
            && resetSensor is false
            && _virtualSnapshot.Forces.Count == 0;
        var passed = actionsStable && photoeye && stopped && restartBlocked && forcedOutput && forceSafeStop && reset && ui;
        GD.Print($"VIRTUAL_CONTROLLER_UI_VERIFY {(passed ? "PASS" : "FAIL")} actionsStable={actionsStable} photoeye={photoeye} stopped={stopped} restartBlocked={restartBlocked} forcedOutput={forcedOutput} forceSafeStop={forceSafeStop} reset={reset} {uiResult}");
        GD.Print("VIRTUAL_CONTROLLER_REAL_PLC_CONNECTION_ATTEMPTED FALSE");
        GetTree().Quit(passed ? 0 : 1);
    }

    private void VerifyNumericSceneIo()
    {
        if (_candidateCatalog is null || _mainCamera is null || _simulatorShell is null)
        {
            GD.PushError("NUMERIC_SCENE_IO_VERIFY FAIL shell unavailable");
            GetTree().Quit(1);
            return;
        }

        AddMigratedScene("lab-2-15-fume-extractor", _candidateCatalog, _mainCamera,
            showRuntimeControls: false, autoRun: false);
        if (_sceneRuntime is null)
        {
            GD.PushError("NUMERIC_SCENE_IO_VERIFY FAIL scene runtime unavailable");
            GetTree().Quit(1);
            return;
        }

        var variables = new PlcVariable[]
        {
            new("execute", PlcVariableType.Bool, PlcVariableRole.Memory, true),
            new("selector", PlcVariableType.DInt, PlcVariableRole.Input, 0L, "fan_selector_position"),
            new("speed", PlcVariableType.DInt, PlcVariableRole.Output, 0L, "fan_speed_percent"),
        };
        var program = new LadderProgram(
            1,
            "numeric-scene-io-verify",
            "NumericSceneIoVerify",
            "LD",
            TimeSpan.FromMilliseconds(20),
            variables,
            [new LadderNetwork(
                "copy-selector",
                "Copy selector to fan speed",
                new LadderNode("execute-contact", LadderNodeKind.Contact, "execute"),
                NumericOperation: new LadderNumericOperation(
                    "move-selector", LadderNumericOperationKind.Move, "selector", "0", "speed"))]);
        var points = new SceneIoPoint[]
        {
            new("fan_selector_position", "DINT", "PC", "input", "Fan selector position"),
            new("fan_speed_percent", "DINT", "PLC", "output", "Fan speed command"),
        };
        var bindingIssues = SceneIoBindingValidator.Validate(program, points);
        var compiled = LadderCompiler.Compile(program);
        if (bindingIssues.Count > 0 || !compiled.IsValid || compiled.Program is null)
        {
            GD.PushError($"NUMERIC_SCENE_IO_VERIFY FAIL validation={bindingIssues.Count} compile={compiled.Issues.Count}");
            GetTree().Quit(1);
            return;
        }

        EnableVirtualControllerProgram(program);
        var actionWorked = _sceneRuntime.ExecuteAction("next-speed");
        _virtualController?.Run();
        _virtualController?.Advance(
            _virtualController.ScanPeriod.TotalSeconds,
            SampleVirtualControllerInputs,
            SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs,
            CommitVirtualControllerNumericOutputs,
            AdvanceAcceptedScenePlant, SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs);
        var sampled = _virtualSnapshot?.NumericVariables.GetValueOrDefault("selector") == 1.0;
        var published = _virtualSnapshot?.NumericOutputs.GetValueOrDefault("speed") == 1.0
            && Convert.ToDouble(_sceneRuntime.Points.GetValueOrDefault("fan_speed_percent") ?? -1.0) == 1.0;
        StopActiveController();
        var safeStop = _virtualSnapshot?.NumericOutputs.GetValueOrDefault("speed") == 0.0
            && Convert.ToDouble(_sceneRuntime.Points.GetValueOrDefault("fan_speed_percent") ?? -1.0) == 0.0;
        var passed = actionWorked && sampled && published && safeStop;
        GD.Print($"NUMERIC_SCENE_IO_VERIFY {(passed ? "PASS" : "FAIL")} action={actionWorked} sampled={sampled} published={published} safeStop={safeStop} scene={_currentSceneId}");
        GD.Print("NUMERIC_SCENE_IO_REAL_PLC_CONNECTION_ATTEMPTED FALSE");
        GetTree().Quit(passed ? 0 : 1);
    }

    private void VerifyLadderEditor()
    {
        if (_simulatorShell is null)
        {
            GD.PushError("LADDER_EDITOR_INTERACTION_VERIFY FAIL shell unavailable");
            GetTree().Quit(1);
            return;
        }
        try
        {
            if (_simulatorShell.VerifyLadderEditorInteractions(out var result))
            {
                GD.Print($"LADDER_EDITOR_INTERACTION_VERIFY PASS {result}");
                var commandsPassed = _simulatorShell.VerifyEditorCommandRoutes(out var commandResult);
                GD.Print($"EDITOR_COMMAND_ROUTES_VERIFY {(commandsPassed ? "PASS" : "FAIL")} {commandResult}");
                GetTree().Quit(commandsPassed ? 0 : 1);
                return;
            }
            GD.PushError($"LADDER_EDITOR_INTERACTION_VERIFY FAIL {result}");
        }
        catch(Exception error) { GD.PushError("LADDER_EDITOR_INTERACTION_VERIFY FAIL " + error); }
        GetTree().Quit(1);
    }

    private string CreateNestedGroupReviewAssembly(AssetCatalogDocument candidates)
    {
        var firstAssembly = _workspaceNodes.Keys.ToArray();
        if (firstAssembly.Length != 2)
            throw new InvalidOperationException("Nested group review requires exactly two seed placements.");

        SetSelection(firstAssembly);
        GroupSelection();
        var conveyor = candidates.Assets.First(item => item.Id == "material-handling.belt-conveyor.600x6000.v1");
        var photoeye = candidates.Assets.First(item => item.Id == "sensing.photoelectric.through-beam.v1");
        PlaceAsset(conveyor);
        var secondConveyor = _selectedPlacementId!;
        PlaceAsset(photoeye);
        var secondPhotoeye = _selectedPlacementId!;
        SetSelection([secondConveyor, secondPhotoeye]);
        GroupSelection();
        SetSelection(_workspaceNodes.Keys.ToArray());
        GroupSelection();
        var root = ExactSelectedGroup()
            ?? throw new InvalidOperationException("Nested group review did not create a parent group.");
        _simulatorShell?.SetWorkspaceStatus("Nested cell · parent group contains two reusable subassemblies");
        return root.Id;
    }

    private async void VerifyExternalDialog()
    {
        if (_simulatorShell is null || _externalConnection is null) { GetTree().Quit(1); return; }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _simulatorShell.ShowExternalPlcDialog();
        var deadline = Time.GetTicksMsec() + 5000;
        do { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        while (_externalConnection.IsBusy && Time.GetTicksMsec() < deadline);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var passed = _simulatorShell.VerifyExternalDialogBounds(out var result);
        GD.Print($"EXTERNAL_DIALOG_VERIFY {(passed ? "PASS" : "FAIL")} {result}");
        GetTree().Quit(passed ? 0 : 1);
    }

    private bool VerifyRendererBindingModes()
    {
        var scenes = _sceneCatalog!.Scenes.Select(SceneCatalogLoader.LoadScene).ToArray();
        foreach (var issue in ProjectValidator.Validate(_candidateCatalog!, _sceneCatalog, scenes))
            GD.Print($"CATALOG_DIAGNOSTIC {issue.Code}: {issue.Message}");
        var actualSupported = !ProjectValidator.Validate(_candidateCatalog!, _sceneCatalog, scenes)
            .Any(issue => issue.Severity == DiagnosticSeverity.Error);
        // The sorter bridge is constructed by SceneComposer rather than loaded
        // from the asset catalog. It must not appear as a missing asset in the
        // normal scenario browser's diagnostics.
        var proceduralBridgeRecognized = !ProjectValidator.Validate(_candidateCatalog!, _sceneCatalog, scenes)
            .Any(issue => issue.Code == "SCN-ACCESSORY-MISSING"
                && issue.Message.Contains("'sorter_handoff_bridge'", StringComparison.Ordinal));
        var sorter = scenes.Single(scene => scene.Id == "lab-10-03-vision-package-sorter");
        var bridge = sorter.Equipment.Single(item => item.Id == "sorter_handoff_bridge");
        using var unknownInstallation = JsonDocument.Parse("{\"installation\":\"unsupported-review-probe\"}");
        using var badAsset = JsonDocument.Parse("{\"installation\":\"visionSorterHandoffBridge\",\"catalogAssetId\":\"missing-review-probe\"}");
        bool RejectsAccessory(JsonElement config) => ProjectValidator.Validate(_candidateCatalog!, _sceneCatalog,
            new[] { sorter with { Equipment = new[] { bridge with { Config = config } } } })
            .Any(issue => issue.Code == "SCN-ACCESSORY-MISSING");
        var invalidAccessoryRejected = RejectsAccessory(unknownInstallation.RootElement) && RejectsAccessory(badAsset.RootElement);
        var palletScene = scenes.Single(scene => scene.Id == "lab-2-17-pallet-robot");
        var palletBridge = palletScene.Equipment.Single(item => item.Id == "robot_pallet_transfer_bridge");
        var orphanBridgeRejected = ProjectValidator.Validate(_candidateCatalog!, _sceneCatalog,
            new[] { palletScene with { Equipment = new[] { palletBridge } } })
            .Any(issue => issue.Code == "SCN-ACCESSORY-MISSING");
        var allAccessoriesResolved = !ProjectValidator.Validate(_candidateCatalog!, _sceneCatalog, scenes)
            .Any(issue => issue.Code is "SCN-ACCESSORY-MISSING" or "SCN-ASSET-UNRESOLVED");
        // Keep unknown modes rejected while exercising the actual catalog modes.
        // This fixture changes no loaded scene and constructs no PLC transport.
        using var fixture = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            points = new[] { new { name = "probe", type = "REAL", owner = "PLC", initial = 0.0 } },
            pointBindings = new[]
            {
                new { point = "probe", equipmentId = scenes[0].Equipment[0].Id, mode = "unsupported-review-probe" }
            }
        }));
        var unknownScene = scenes[0] with { Simulation = fixture.RootElement };
        var unknownRejected = ProjectValidator.Validate(_candidateCatalog!, _sceneCatalog, new[] { unknownScene })
            .Count(issue => issue.Code == "SCN-BINDING-MODE") == 1;
        GD.Print($"RENDER_BINDING_VALIDATION_VERIFY {(actualSupported && unknownRejected ? "PASS" : "FAIL")} actualSupported={actualSupported} unknownRejected={unknownRejected}");
        GD.Print($"PROCEDURAL_ACCESSORY_VALIDATION_VERIFY {(proceduralBridgeRecognized ? "PASS" : "FAIL")} sorterBridgeRecognized={proceduralBridgeRecognized}");
        GD.Print($"PROCEDURAL_ACCESSORY_REJECTION_VERIFY {(invalidAccessoryRejected ? "PASS" : "FAIL")}");
        GD.Print($"PROCEDURAL_ACCESSORY_CATALOG_VERIFY {(allAccessoriesResolved && orphanBridgeRejected ? "PASS" : "FAIL")} allResolved={allAccessoriesResolved} orphanBridgeRejected={orphanBridgeRejected}");
        return actualSupported && unknownRejected && proceduralBridgeRecognized && invalidAccessoryRejected
            && allAccessoriesResolved && orphanBridgeRejected;
    }

    private void VerifyAppShell()
    {
        var rendererModesPassed = VerifyRendererBindingModes();
        var result = "shell unavailable";
        var menuResult = "shell unavailable";
        var plcResult = "shell unavailable";
        var scenarioResult = "shell unavailable";
        var projectResult = "shell unavailable";
        var guardResult = "shell unavailable";
        var draftResult = "shell unavailable";
        var structurePassed = _simulatorShell is not null
            && _simulatorShell.VerifyStructure(out result);
        var menuPassed = _simulatorShell is not null
            && _simulatorShell.VerifyWorkspaceLoadMenuBinding(out menuResult);
        var plcPassed = _simulatorShell is not null
            && _simulatorShell.VerifyExternalPlcMenu(out plcResult);
        var scenarioPassed = _simulatorShell is not null
            && _simulatorShell.VerifyScenarioMenuSelection(out scenarioResult);
        var projectPassed = _simulatorShell is not null
            && _simulatorShell.VerifyCrossSceneProjectOpen(out projectResult);
        var guardPassed = _simulatorShell is not null
            && _simulatorShell.VerifyExternalProfileGuards(out guardResult);
        var draftPassed = _simulatorShell is not null
            && _simulatorShell.VerifySceneDraftPersistence(out draftResult);
        GD.Print($"SCENE_DRAFT_VERIFY {(draftPassed ? "PASS" : "FAIL")} {draftResult}");
        var unsavedResult = "shell unavailable";
        var unsavedPassed = _simulatorShell is not null && _simulatorShell.VerifyUnsavedWorkGuard(out unsavedResult);
        GD.Print($"UNSAVED_WORK_VERIFY {(unsavedPassed ? "PASS" : "FAIL")} {unsavedResult}");
        GD.Print($"EXTERNAL_PROFILE_GUARD_VERIFY {(guardPassed ? "PASS" : "FAIL")} {guardResult}");
        if (rendererModesPassed && structurePassed && menuPassed && plcPassed && scenarioPassed && projectPassed && guardPassed && draftPassed && unsavedPassed)
        {
            GD.Print($"APP_SHELL_VERIFY PASS {result} workspaceMenu={menuResult} plcMenu={plcResult} scenarioMenu={scenarioResult} crossSceneProject={projectResult}");
            if (OS.GetCmdlineUserArgs().Contains("--trace-orphans", StringComparer.Ordinal))
            {
                TraceOrphansAndQuit();
                return;
            }
            GetTree().Quit(0);
            return;
        }
        var failure = "shell unavailable";
        if (_simulatorShell is not null)
        {
            _simulatorShell.VerifyStructure(out failure);
            if (!menuPassed) failure += $"; workspaceMenu={menuResult}";
            if (!plcPassed) failure += $"; plcMenu={plcResult}";
            if (!scenarioPassed) failure += $"; scenarioMenu={scenarioResult}";
            if (!projectPassed) failure += $"; crossSceneProject={projectResult}";
            if (!guardPassed) failure += $"; externalProfileGuards={guardResult}";
            if (!draftPassed) failure += $"; sceneDraft={draftResult}";
            if (!rendererModesPassed) failure += "; renderer binding validation failed";
        }
        GD.PushError($"APP_SHELL_VERIFY FAIL {failure}");
        GetTree().Quit(1);
    }

    private async void TraceOrphansAndQuit()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Node.PrintOrphanNodes();
        GetTree().Quit();
    }

    private void VerifyUiDensity()
    {
        var window = GetWindow();
        var physicalSize = DisplayServer.WindowGetSize();
        var viewportSize = window.GetVisibleRect().Size;
        var mode = window.ContentScaleMode;
        var oneToOne = Math.Abs(viewportSize.X - physicalSize.X) < 1.0f
            && Math.Abs(viewportSize.Y - physicalSize.Y) < 1.0f;
        var passed = mode == Window.ContentScaleModeEnum.Disabled
            && Math.Abs(window.ContentScaleFactor - 1.0f) < 0.001f
            && oneToOne;
        var result = $"mode={mode} factor={window.ContentScaleFactor:0.###} "
            + $"window={physicalSize.X}x{physicalSize.Y} viewport={viewportSize.X:0}x{viewportSize.Y:0} oneToOne={oneToOne}";
        if (passed)
        {
            GD.Print($"UI_DENSITY_VERIFY PASS {result}");
            GetTree().Quit(0);
            return;
        }
        GD.PushError($"UI_DENSITY_VERIFY FAIL {result}");
        GetTree().Quit(1);
    }

    private void VerifyHud()
    {
        if (_simulatorShell is not null && _simulatorShell.VerifyLayout(out var result))
        {
            GD.Print($"APP_HUD_VERIFY PASS {result}");
            GetTree().Quit(0);
            return;
        }
        var failure = "shell unavailable";
        if (_simulatorShell is not null) _simulatorShell.VerifyLayout(out failure);
        GD.PushError($"APP_HUD_VERIFY FAIL {failure}");
        GetTree().Quit(1);
    }

    private async void VerifySplitView()
    {
        _simulatorShell?.SetReviewState("ladder", string.Empty);
        var freshDrawersFit = true;
        if (_simulatorShell is not null)
        {
            var vendors = _simulatorShell.GetNode<TabContainer>(
                "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs");
            for (var vendor = 0; vendor < vendors.GetTabCount(); vendor++)
            {
                vendors.CurrentTab = vendor;
                var environment = vendors.GetCurrentTabControl();
                var tabs = environment.GetNode<TabContainer>(
                    "Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags");
                foreach (var tab in new[] { 0, 1, 2 })
                {
                    tabs.CurrentTab = tab;
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var dockTop = _simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock").GetGlobalRect().Position.Y;
                    foreach (var path in new[] { "Workbench", "Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTitle",
                        "Workbench/EditorAndTasks/EditorAndInspector/BottomDockHost" })
                    {
                        var control = environment.GetNode<Control>(path);
                        freshDrawersFit &= control.GetGlobalRect().End.Y <= dockTop - 4;
                        GD.Print($"EDITOR_DRAWER_BOUNDS vendor={vendor} tab={tab} node={path} end={control.GetGlobalRect().End.Y} dock={dockTop} minimum={control.GetCombinedMinimumSize()}");
                    }
                }
                tabs.CurrentTab = 0;
            }
            vendors.CurrentTab = 0;
        }
        _simulatorShell?.SetReviewState("split", string.Empty);
        // Container minimum sizes and deferred toolbar clearance settle after
        // the view switch. Measure the rendered layout, not the previous view.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var result = "shell unavailable";
        if (_simulatorShell is not null && _simulatorShell.VerifySplitLayout(out result))
        {
            var aperture = ChallengeSceneAperture(_simulatorShell.SceneViewportRect());
            var targetCentered = _mainCamera is not null && _cameraController is not null
                && _mainCamera.UnprojectPosition(_cameraController.ViewTarget).DistanceTo(aperture.GetCenter()) < 1.0f;
            const string routinePath = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView";
            var routine = _simulatorShell.GetNode<Control>(routinePath);
            var initialHeight = routine.Size.Y;
            var toggle = _simulatorShell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle");
            var tables = _simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables");
            var inputTable = tables.GetNode<Control>("SimulatorToPlcPoints").GetGlobalRect();
            var columnsAligned = Math.Abs(inputTable.End.X - (aperture.End.X + 4.0f)) < 8.0f;
            toggle.EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var collapseReclaimsSpace = !tables.Visible && routine.Size.Y > initialHeight + 60.0f;
            toggle.EmitSignal(BaseButton.SignalName.Pressed);
            _simulatorShell.SetReviewState("ladder", string.Empty);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var dock = _simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock");
            var ladder = _simulatorShell.GetNode<Control>("Workspace/LadderWorkspace");
            var standalonePoints = tables.Visible && dock.Visible
                && ladder.GetGlobalRect().End.Y <= dock.GetGlobalRect().Position.Y;
            var projectPagesFit = true;
            var environments = _simulatorShell.GetNode<TabContainer>(
                "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs");
            for (var vendor = 0; vendor < environments.GetTabCount(); vendor++)
            {
                environments.CurrentTab = vendor;
                var projectTabs = environments.GetCurrentTabControl().GetNode<TabContainer>(
                    "Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags");
                foreach (var tab in new[] { 1, 2 })
                {
                    projectTabs.CurrentTab = tab;
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var page = projectTabs.GetCurrentTabControl();
                    projectPagesFit &= page.GetGlobalRect().End.Y <= ladder.GetGlobalRect().End.Y + 1.0f
                        && ladder.GetGlobalRect().End.Y <= dock.GetGlobalRect().Position.Y
                        && projectTabs.GetGlobalRect().End.Y <= ladder.GetGlobalRect().End.Y + 1.0f;
                }
                projectTabs.CurrentTab = 0;
            }
            environments.CurrentTab = 0;
            var projectPanel = _simulatorShell.GetNode<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization");
            var wasProjectVisible = projectPanel.Visible;
            _simulatorShell.SetReviewState("operator", string.Empty);
            _simulatorShell.SetReviewState("split", string.Empty);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var drawerStatePreserved = projectPanel.Visible == wasProjectVisible;
            var passed = freshDrawersFit && targetCentered && columnsAligned && collapseReclaimsSpace && standalonePoints && drawerStatePreserved && projectPagesFit;
            GD.Print($"SPLIT_VIEW_VERIFY {(passed ? "PASS" : "FAIL")} {result} targetCentered={targetCentered} columnsAligned={columnsAligned} collapseReclaimsSpace={collapseReclaimsSpace} standalonePoints={standalonePoints} drawerStatePreserved={drawerStatePreserved} projectPagesFit={projectPagesFit} freshDrawersFit={freshDrawersFit}");
            GetTree().Quit(passed ? 0 : 1);
            return;
        }
        var failure = _simulatorShell is null ? "shell unavailable" : result;
        GD.PushError($"SPLIT_VIEW_VERIFY FAIL {failure}");
        GetTree().Quit(1);
    }

    private void VerifyCameraInput()
    {
        if (_simulatorShell is null || _mainCamera is null)
        {
            GD.PushError("CAMERA_INPUT_VERIFY FAIL shell or camera unavailable");
            GetTree().Quit(1);
            return;
        }
        var viewport = _simulatorShell.SceneViewportRect();
        var center = viewport.GetCenter();
        var before = _mainCamera.GlobalPosition;
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Right, Pressed = true, Position = center,
        }, true);
        GetViewport().PushInput(new InputEventMouseMotion
        {
            Position = center + new Vector2(48, 0), Relative = new Vector2(48, 0),
        }, true);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Right, Pressed = false, Position = center + new Vector2(48, 0),
        }, true);
        var changed = !_mainCamera.GlobalPosition.IsEqualApprox(before);
        if (!changed)
        {
            GD.PushError($"CAMERA_INPUT_VERIFY FAIL viewport={viewport} moved=false");
            GetTree().Quit(1);
            return;
        }

        // Regression: a view switch must cancel a button gesture even if the
        // native release occurs while the ladder editor owns the pointer.
        var transitionBefore = _mainCamera.GlobalPosition;
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Right, Pressed = true, Position = viewport.GetCenter(),
        }, true);
        _simulatorShell.SetReviewState("ladder", string.Empty);
        _simulatorShell.SetReviewState("operator", string.Empty);
        GetViewport().PushInput(new InputEventMouseMotion
        {
            Position = viewport.GetCenter() + new Vector2(64, 0), Relative = new Vector2(64, 0),
        }, true);
        var releasedByViewSwitch = _mainCamera.GlobalPosition.IsEqualApprox(transitionBefore);
        var passed = changed && releasedByViewSwitch;
        GD.Print($"CAMERA_INPUT_VERIFY {(passed ? "PASS" : "FAIL")} viewport={viewport} moved={changed} view_switch_released={releasedByViewSwitch}");
        GetTree().Quit(passed ? 0 : 1);
    }

    private void VerifySceneControls()
    {
        if (_sceneControlInteractor is null || _sceneRuntime is null || _mainCamera is null
            || _sceneControlInteractor.Bindings.Count == 0)
        {
            GD.PushError("SCENE_CONTROL_VERIFY FAIL no composed scene control is available");
            GetTree().Quit(1);
            return;
        }
        var control = _sceneControlInteractor.Bindings
            .FirstOrDefault(binding => binding.ActionId == "conveyor_run");
        if (control is null)
        {
            GD.PushError("SCENE_CONTROL_VERIFY FAIL default Start control is unavailable");
            GetTree().Quit(1);
            return;
        }
        var mesh = control.Node as MeshInstance3D ?? control.Node.FindChildren("*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>()
            .FirstOrDefault();
        if (mesh is null)
        {
            GD.PushError($"SCENE_CONTROL_VERIFY FAIL control={control.EquipmentId} has no mesh");
            GetTree().Quit(1);
            return;
        }
        var feedbackTarget = control.Node.FindChild("KIN_pushbutton", true, false) as Node3D
            ?? control.Node.FindChild("KIN_estop", true, false) as Node3D
            ?? control.Node;
        var authoredPosition = feedbackTarget.Transform.Origin;
        var operatorDirection = (_mainCamera.GlobalPosition - control.Node.GlobalPosition).Normalized();
        var controlFaceDirection = (control.Node.GlobalBasis * Vector3.Back).Normalized();
        var facesOperator = controlFaceDirection.Dot(operatorDirection) > 0.25f;
        var pointer = _mainCamera.UnprojectPosition(mesh.GlobalPosition);
        RunActiveController();
        void ScanOnce() => _virtualController?.Advance(0.020,
            SampleVirtualControllerInputs, SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs, CommitVirtualControllerNumericOutputs,
            AdvanceAcceptedScenePlant, SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs);
        var reviewBarBlocked = true;
        var reviewPopupBlocked = true;
        var reviewClockBlocked = true;
        if (_visualReviewBar is not null && _visualReviewLabel is not null && _visualReviewFocus is not null)
        {
            // Put the noninteractive coverage label directly over the real Start
            // mesh. The same raw-input route must leave the PLC image unchanged.
            var barPosition = _visualReviewBar.Position;
            _visualReviewBar.Position += pointer - _visualReviewLabel.GetGlobalRect().GetCenter();
            GD.Print($"REVIEW_OVERLAY_FIXTURE visible={_visualReviewBar.IsVisibleInTree()} pointer={pointer} bar={_visualReviewBar.GetGlobalRect()} label={_visualReviewLabel.GetGlobalRect()}");
            GetViewport().PushInput(new InputEventMouseButton
            { ButtonIndex = MouseButton.Left, Pressed = true, Position = pointer }, true);
            ScanOnce();
            reviewBarBlocked = _sceneRuntime.Points.GetValueOrDefault("conveyor_run") is false;
            _visualReviewBar.Position = barPosition;
            ResetActiveController();
            RunActiveController();
            _visualReviewFocus.GetPopup().Popup();
            _Input(new InputEventMouseButton
            { ButtonIndex = MouseButton.Left, Pressed = true, Position = pointer });
            ScanOnce();
            reviewPopupBlocked = _sceneRuntime.Points.GetValueOrDefault("conveyor_run") is false;
            _visualReviewFocus.GetPopup().Hide();
            ResetActiveController();
            RunActiveController();
            if (_gantryReviewClockBar is not null)
            {
                var clockPosition = _gantryReviewClockBar.Position;
                var clockVisible = _gantryReviewClockBar.Visible;
                _gantryReviewClockBar.Visible = true;
                _gantryReviewClockBar.Position = pointer - new Vector2(20, 15);
                _Input(new InputEventMouseButton
                { ButtonIndex = MouseButton.Left, Pressed = true, Position = pointer });
                ScanOnce();
                reviewClockBlocked = _sceneRuntime.Points.GetValueOrDefault("conveyor_run") is false;
                _gantryReviewClockBar.Position = clockPosition;
                _gantryReviewClockBar.Visible = clockVisible;
                ResetActiveController();
                RunActiveController();
            }
        }
        // Exercise the same GUI input route used by an operator. Calling the
        // command method directly would miss a CanvasLayer interception bug.
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = pointer,
        }, true);
        ScanOnce();
        _sceneControlInteractor.AdvanceFeedback(0.08);
        var depressed = feedbackTarget.Transform.Origin.DistanceTo(authoredPosition) > 0.001f;
        _sceneControlInteractor.AdvanceFeedback(0.30);
        var restored = feedbackTarget.Transform.Origin.DistanceTo(authoredPosition) < 0.0001f;
        var running = _sceneRuntime.Points.TryGetValue("conveyor_run", out var value) && value is true;
        var estop = _sceneControlInteractor.Bindings
            .FirstOrDefault(binding => binding.ActionId == "conveyor-estop");
        if (estop is null)
        {
            GD.PushError("SCENE_CONTROL_VERIFY FAIL default E-stop control is unavailable");
            GetTree().Quit(1);
            return;
        }
        var estopPointer = _mainCamera.UnprojectPosition(estop.Node.GlobalPosition);
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = estopPointer,
        }, true);
        var estopLatched = _sceneRuntime.Points.TryGetValue("estop_ok", out var estopValue) && estopValue is false;
        var stopped = _sceneRuntime.Points.TryGetValue("conveyor_run", out var stoppedValue) && stoppedValue is false;
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = pointer,
        }, true);
        var restartBlocked = _sceneRuntime.Points.TryGetValue("conveyor_run", out var blockedValue) && blockedValue is false;
        ResetActiveController();
        RunActiveController();
        _sceneControlInteractor.SynchronizeFeedback(_sceneRuntime.Points);
        var resetReleased = _sceneRuntime.Points.TryGetValue("estop_ok", out var resetValue) && resetValue is true;
        GetViewport().PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = pointer,
        }, true);
        ScanOnce();
        var restarted = _sceneRuntime.Points.TryGetValue("conveyor_run", out var restartedValue) && restartedValue is true;
        // Repeat at the controller seam; scene-only tests miss scan overwrites.
        ResetActiveController();
        RunActiveController();
        bool OutputOn() => _sceneRuntime.Points.GetValueOrDefault("conveyor_run") is true;
        var virtualReady = _virtualController is not null;
        GetViewport().PushInput(new InputEventMouseButton
        { ButtonIndex = MouseButton.Left, Pressed = true, Position = pointer }, true);
        ScanOnce();
        var virtualStart = OutputOn();
        ExecuteSelectedControllerAction("conveyor_run");
        ScanOnce();
        var virtualStopCommand = !OutputOn();
        ExecuteSelectedControllerAction("conveyor_run");
        ScanOnce();
        ExecuteSelectedControllerAction("conveyor-estop");
        var immediateEstop = !OutputOn() && _virtualController?.Snapshot.State == VirtualControllerState.Stopped;
        RunActiveController();
        var virtualRestartBlocked = _virtualController?.Snapshot.State == VirtualControllerState.Stopped;
        ExecuteSelectedControllerAction("conveyor-estop-reset");
        RunActiveController();
        ScanOnce();
        var resetDoesNotStart = !OutputOn();
        ExecuteSelectedControllerAction("conveyor_run");
        ScanOnce();
        var virtualRestarted = OutputOn();
        var plcMenu = _simulatorShell?.GetNode<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlcMenu").GetPopup();
        plcMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 11);
        var exclusiveExternal = _virtualController is null && _sceneRuntime.UsesExternalClock;
        var externalActionBlocked = !ExecuteSelectedControllerAction("conveyor_run");
        _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["conveyor_run"] = true });
        var invalidOutputRejected = false;
        try { _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["conveyor_run"] = "False" }); }
        catch (InvalidOperationException) { invalidOutputRejected = OutputOn(); }
        var atomicImageRejected = false;
        try { _sceneRuntime.CommitExternalPlcOutputs(new Dictionary<string, object?> { ["conveyor_run"] = false, ["estop_ok"] = true }); }
        catch (InvalidOperationException) { atomicImageRejected = OutputOn(); }
        StopActiveController();
        ResetActiveController();
        var externalImagePreserved = OutputOn();
        plcMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 10);
        var awaitingController = _sceneRuntime.UsesExternalClock && _virtualController is null
            && !ExecuteSelectedControllerAction("conveyor_run") && !OutputOn();
        // A feedback action under controller ownership must not execute the
        // fallback PLC rule, even if its condition becomes true.
        using var fixtureJson = System.Text.Json.JsonDocument.Parse("""
            {"type":"booleanPanel","points":[
              {"name":"feedback","type":"BOOL","owner":"PC","initial":false},
              {"name":"motor","type":"BOOL","owner":"PLC","initial":false,"role":"output"}],
             "actions":[{"id":"feedback-toggle","type":"toggle","point":"feedback"}],
             "rules":[{"when":{"feedback":true},"set":{"motor":true}}]}
            """);
        var fixtureRoot = new Node3D();
        var fixtureRuntime = new SceneSimulationRuntime(fixtureJson.RootElement, fixtureRoot);
        fixtureRuntime.ResetSimulation();
        fixtureRuntime.UsesExternalClock = true;
        fixtureRuntime.ExecuteAction("feedback-toggle");
        var noFallbackOverwrite = fixtureRuntime.Points.GetValueOrDefault("motor") is false;
        fixtureRuntime.UsesExternalClock = false;
        fixtureRuntime.ExecuteAction("feedback-toggle");
        fixtureRuntime.ExecuteAction("feedback-toggle");
        var fallbackRulesWork = fixtureRuntime.Points.GetValueOrDefault("motor") is true;
        fixtureRuntime.Free();
        fixtureRoot.Free();
        var pickingPassed = VerifySceneControlPicking();
        var feedbackPassed = VerifySceneControlFeedback();
        var passed = pickingPassed && feedbackPassed && facesOperator && running && depressed && restored && estopLatched && stopped && restartBlocked
            && resetReleased && restarted && virtualReady && virtualStart && virtualStopCommand && immediateEstop
            && virtualRestartBlocked && resetDoesNotStart && virtualRestarted && exclusiveExternal && externalActionBlocked
            && externalImagePreserved && awaitingController && noFallbackOverwrite && fallbackRulesWork
            && invalidOutputRejected && atomicImageRejected && reviewBarBlocked && reviewPopupBlocked && reviewClockBlocked;
        GD.Print($"REVIEW_OVERLAY_INPUT_VERIFY {(reviewBarBlocked && reviewPopupBlocked && reviewClockBlocked ? "PASS" : "FAIL")} enabled={_visualReviewBar is not null} bar={reviewBarBlocked} popup={reviewPopupBlocked} clock={reviewClockBlocked}");
        GD.Print($"EXTERNAL_IMAGE_VERIFY {(invalidOutputRejected && atomicImageRejected ? "PASS" : "FAIL")} typed={invalidOutputRejected} atomic={atomicImageRejected}");
        GD.Print($"SCENE_CONTROL_VERIFY {(passed ? "PASS" : "FAIL")} facesOperator={facesOperator} start={running} depressed={depressed} restored={restored} estopLatched={estopLatched} stopped={stopped} restartBlocked={restartBlocked} resetReleased={resetReleased} restarted={restarted} virtualReady={virtualReady} virtualStart={virtualStart} virtualStopCommand={virtualStopCommand} immediateEstop={immediateEstop} virtualRestartBlocked={virtualRestartBlocked} resetDoesNotStart={resetDoesNotStart} virtualRestarted={virtualRestarted} exclusiveExternal={exclusiveExternal} externalActionBlocked={externalActionBlocked} externalImagePreserved={externalImagePreserved} awaitingController={awaitingController} noFallbackOverwrite={noFallbackOverwrite} fallbackRulesWork={fallbackRulesWork}");
        GetTree().Quit(passed ? 0 : 1);
    }

    private async void VerifyGalleryClock()
    {
        // Exercise the normal shell action and actual engine callbacks. Calling
        // the rotor's _PhysicsProcess directly would miss the disabled clock.
        AddMigratedScene("equipment-gallery", _candidateCatalog!, _mainCamera!, false, false);
        var fan = _sceneCompositionRoot!.GetNode<Node3D>("gallery_fan");
        var rotor = fan.FindChildren("KIN_*", string.Empty, true, false).OfType<Node3D>().First();
        var drive = fan.FindChildren("*", string.Empty, true, false).OfType<EquipmentMotionController>().Single();
        var home = rotor.Transform;
        var startAccepted = ExecuteSelectedControllerAction("toggle-gallery");
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var localMotion = startAccepted && _virtualController is null
            && _sceneRuntime!.Points.GetValueOrDefault("gallery_animation") is true
            && drive.RunCommand && rotor.Transform != home;

        StopActiveController();
        var stopped = rotor.Transform;
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var stopHolds = !drive.RunCommand && rotor.Transform == stopped
            && _sceneRuntime!.Points.GetValueOrDefault("gallery_animation") is false;
        ResetActiveController();
        var resetRestores = rotor.Transform == home && !drive.RunCommand;

        // A loaded controller still owns gallery timing. This valid, memory-only
        // fixture supplies no scene outputs and is not a gallery lesson program.
        var clockFixture = new LadderEditorDocument();
        clockFixture.ResetProject("gallery-clock-fixture", "Gallery clock fixture", TimeSpan.FromMilliseconds(20));
        clockFixture.AddTag("fixture", PlcVariableRole.Memory, initialValue: true);
        clockFixture.AddRung("Clock ownership fixture", "fixture");
        clockFixture.AddContact(0, 0, "fixture", false);
        EnableVirtualControllerProgram(clockFixture.BuildProgram());
        ExecuteSelectedControllerAction("toggle-gallery");
        var controllerHeld = rotor.Transform;
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var loadedStopped = _sceneRuntime!.UsesExternalClock && drive.RunCommand && rotor.Transform == controllerHeld;
        RunActiveController();
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var loadedRuns = rotor.Transform != controllerHeld;
        StopActiveController();
        controllerHeld = rotor.Transform;
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var loadedStopHolds = rotor.Transform == controllerHeld;
        DisableVirtualController();
        ResetActiveController();
        ExecuteSelectedControllerAction("toggle-gallery");
        var unloaded = rotor.Transform;
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var unloadRestoresLocal = !_sceneRuntime!.UsesExternalClock && rotor.Transform != unloaded;
        StopActiveController();
        ResetActiveController();

        var plcMenu = _simulatorShell!.GetNode<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlcMenu").GetPopup();
        plcMenu.EmitSignal(PopupMenu.SignalName.IdPressed, 11);
        ExecuteSelectedControllerAction("toggle-gallery");
        var externalHeld = rotor.Transform;
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var externalPaused = _sceneRuntime!.UsesExternalClock && rotor.Transform == externalHeld;
        plcMenu.EmitSignal(PopupMenu.SignalName.IdPressed, 10);
        ExecuteSelectedControllerAction("toggle-gallery");
        var resumed = rotor.Transform;
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var localResumes = _virtualController is null && rotor.Transform != resumed;
        StopActiveController();
        ResetActiveController();

        // The exception must not activate fake PLC rules in a blank exercise.
        AddMigratedScene("lab-2-01-workstation-call", _candidateCatalog!, _mainCamera!, false, false);
        RunActiveController();
        ExecuteSelectedControllerAction("toggle-material-call");
        await ToSignal(GetTree().CreateTimer(0.13), SceneTreeTimer.SignalName.Timeout);
        var blankFailClosed = _virtualController is null && _sceneRuntime!.UsesExternalClock
            && _sceneRuntime.Points.GetValueOrDefault("material_call_pressed") is true
            && _sceneRuntime.Points.GetValueOrDefault("material_call_on") is false;
        ResetActiveController();
        var passed = localMotion && stopHolds && resetRestores && loadedStopped && loadedRuns && loadedStopHolds && unloadRestoresLocal
            && externalPaused && localResumes && blankFailClosed;
        GD.Print($"GALLERY_CLOCK_VERIFY {(passed ? "PASS" : "FAIL")} localMotion={localMotion} stopHolds={stopHolds} resetRestores={resetRestores} loadedStopped={loadedStopped} loadedRuns={loadedRuns} loadedStopHolds={loadedStopHolds} unloadRestoresLocal={unloadRestoresLocal} externalPaused={externalPaused} localResumes={localResumes} blankFailClosed={blankFailClosed}");
        GetTree().Quit(passed ? 0 : 1);
    }

    private bool VerifySceneControlFeedback()
    {
        AddMigratedScene("lab-9-10-box-volume", _candidateCatalog!, _mainCamera!, false, false);
        var interactor = _sceneControlInteractor!;
        var readout = interactor.Bindings.Single(binding => binding.EquipmentId == "numeric_display_0");
        var nodes = readout.Node.FindChildren("*", string.Empty, true, false).OfType<Node3D>()
            .Append(readout.Node).ToArray();
        var poses = nodes.ToDictionary(node => node, node => node.Transform);
        bool FixedReadout() => nodes.All(node => node.Transform.IsEqualApprox(poses[node]));
        interactor.PlayAcceptedFeedback(readout);
        interactor.AdvanceFeedback(0.08);
        var standFixed = FixedReadout();
        interactor.PlayAcceptedFeedback(readout);
        interactor.AdvanceFeedback(0.08);
        var repeatedReadoutFixed = FixedReadout();
        interactor.AdvanceFeedback(0.30);
        var readoutRestored = FixedReadout();

        var button = interactor.Bindings.Single(binding => binding.ActionId == "toggle-length_valid");
        var cap = (Node3D)button.Node.FindChild("KIN_pushbutton", true, false);
        var authoredCap = cap.Transform;
        var authoredStand = button.Node.Transform;
        interactor.PlayAcceptedFeedback(button);
        interactor.AdvanceFeedback(0.08);
        var capPressed = cap.Position.DistanceTo(authoredCap.Origin) > 0.001f;
        var standUnmoved = button.Node.Transform.IsEqualApprox(authoredStand);
        var firstPress = cap.Transform;
        interactor.PlayAcceptedFeedback(button);
        interactor.AdvanceFeedback(0.08);
        var repeatHasNoExtraTravel = cap.Transform.IsEqualApprox(firstPress);
        interactor.AdvanceFeedback(0.30);
        var repeatReturnsHome = cap.Transform.IsEqualApprox(authoredCap);
        var passed = standFixed && repeatedReadoutFixed && readoutRestored && capPressed
            && standUnmoved && repeatHasNoExtraTravel && repeatReturnsHome;
        GD.Print($"CONTROL_FEEDBACK_VERIFY {(passed ? "PASS" : "FAIL")} standFixed={standFixed} repeatedReadoutFixed={repeatedReadoutFixed} readoutRestored={readoutRestored} capPressed={capPressed} standUnmoved={standUnmoved} repeatHasNoExtraTravel={repeatHasNoExtraTravel} repeatReturnsHome={repeatReturnsHome}");
        return passed;
    }

    private bool VerifySceneControlPicking()
    {
        // A supported display leaves air between its head and foot. That air
        // must not steal a click from a visible button behind the assembly.
        using var config = System.Text.Json.JsonDocument.Parse("""{"action":"fixture-action"}""");
        var fixture = new Node3D { Name = "ControlPickingFixture" };
        AddChild(fixture);
        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 10 };
        fixture.AddChild(camera);
        camera.Position = new Vector3(0, 1.5f, 10);
        camera.LookAt(new Vector3(0, 1.5f, 0));
        var display = new Node3D { Name = "fixture_display" };
        var button = new Node3D { Name = "fixture_button" };
        var rotated = new Node3D { Name = "fixture_rotated", Position = new Vector3(3, 1.5f, 0),
            RotationDegrees = new Vector3(0, 0, 45), Scale = new Vector3(1, 2, 3) };
        fixture.AddChild(display); fixture.AddChild(button); fixture.AddChild(rotated);
        MeshInstance3D Box(Node3D parent, Vector3 position, Vector3 size)
        {
            var mesh = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = position };
            parent.AddChild(mesh);
            return mesh;
        }
        Box(display, new Vector3(0, 3, 0), new Vector3(2, 0.4f, 0.2f));
        Box(display, new Vector3(0, 0, 0), new Vector3(2, 0.2f, 1));
        Box(display, new Vector3(-0.9f, 1.5f, 0), new Vector3(0.1f, 3, 0.1f));
        Box(button, new Vector3(0.5f, 1.5f, -1), new Vector3(0.3f, 0.3f, 0.3f));
        Box(rotated, Vector3.Zero, new Vector3(2, 0.1f, 0.1f));
        var definition = _activeSceneDefinition! with
        {
            Equipment = new[] { display, button, rotated }.Select(node => new SceneEquipment(
                node.Name.ToString(), "fixture", node.Name.ToString(), new double[3], null, null, config.RootElement)).ToArray()
        };
        var picker = new SceneControlInteractor(definition, fixture);
        string? Pick(Vector3 point) => picker.TryPick(camera, camera.UnprojectPosition(point), out var hit)
            ? hit?.EquipmentId : null;
        try
        {
            var gap = new Vector3(0.5f, 1.5f, 0);
            var empty = new Vector3(0.5f, 2.1f, 0);
            var gapButton = Pick(gap) == "fixture_button";
            var emptySpace = Pick(empty) is null;
            var head = Pick(new Vector3(0, 3, 0)) == "fixture_display";
            var hidden = Box(display, empty, new Vector3(0.3f, 0.3f, 0.3f));
            hidden.Visible = false;
            var hiddenIgnored = Pick(empty) is null;
            var rotatedGap = Pick(new Vector3(3.55f, 0.95f, 0)) is null;
            var rotatedFace = Pick(rotated.GlobalPosition) == "fixture_rotated";
            // The same ray now hits an actual foreground part, not an empty
            // aggregate box. Nonuniform scale must preserve world hit ordering.
            var front = Box(display, gap + Vector3.Back, new Vector3(0.3f, 0.3f, 0.3f));
            front.Scale = new Vector3(2, 0.5f, 3);
            var nearest = Pick(gap) == "fixture_display";
            front.Visible = false;
            var behind = Pick(gap) == "fixture_button";
            var passed = gapButton && emptySpace && head && hiddenIgnored && rotatedGap && rotatedFace && nearest && behind;
            GD.Print($"CONTROL_PICKING_VERIFY {(passed ? "PASS" : "FAIL")} gapButton={gapButton} emptySpace={emptySpace} head={head} hiddenIgnored={hiddenIgnored} rotatedGap={rotatedGap} rotatedFace={rotatedFace} nearest={nearest} behind={behind}");
            return passed;
        }
        finally { fixture.Free(); }
    }

    private void VerifyWorkspace()
    {
        if (_candidateCatalog is null || _sceneCatalog is null || _currentSceneId is null)
        {
            GD.PushError("WORKSPACE_VERIFY FAIL project not initialized");
            GetTree().Quit(1);
            return;
        }
        var path = ProjectSettings.GlobalizePath(WorkspaceRepository.VerificationWorkspacePath);
        try
        {
            VerifyWorkspaceDriveRepository();
            if (_verifyWorkspaceDriveRepositoryOnly) { GetTree().Quit(0); return; }
            VerifyWorkspaceDirtyUndo();
            VerifyWorkspaceReplacementGuards();
            var conveyor = _candidateCatalog.Assets.First(
                item => item.Id == "material-handling.belt-conveyor.600x6000.v1"
            );
            PlaceAsset(conveyor);
            var instanceId = _workspaceNodes.Keys.Single();
            var original = _workspaceNodes[instanceId].Node.Position;
            var gizmoPassed = VerifyGizmoInteraction(instanceId);
            var edited = original + new Vector3(1.25f, 0.4f, -0.75f);
            TransformPlacement(instanceId, edited, new Vector3(0, 35, 0), Vector3.One);
            UndoWorkspace();
            var undoPassed = _workspaceNodes[instanceId].Node.Position.IsEqualApprox(original);
            RedoWorkspace();
            var redoPassed = _workspaceNodes[instanceId].Node.Position.IsEqualApprox(edited);
            SetSnapSettings(true, 0.25, 15.0);
            TransformPlacement(instanceId, new Vector3(1.38f, 0.37f, -0.62f),
                new Vector3(0, 22, 0), Vector3.One);
            var snappedPosition = new Vector3(1.5f, 0.25f, -0.5f);
            var snapPassed = _workspaceNodes[instanceId].Node.Position.IsEqualApprox(snappedPosition)
                && _workspaceNodes[instanceId].Node.RotationDegrees.IsEqualApprox(
                    new Vector3(0, 15, 0));
            DuplicatePlacement(instanceId);
            var duplicatePassed = _workspaceNodes.Count == 2;
            UndoWorkspace();
            var duplicateUndoPassed = _workspaceNodes.Count == 1;
            SetSnapSettings(false, 0.10, 15.0);
            DeletePlacement(instanceId);
            var deletePassed = _workspaceNodes.Count == 0;
            UndoWorkspace();
            var restorePassed = _workspaceNodes.TryGetValue(instanceId, out var restored)
                && restored.Node.Position.IsEqualApprox(snappedPosition);
            var photoeye = _candidateCatalog.Assets.First(item => item.Id == "sensing.photoelectric.through-beam.v1");
            PlaceAsset(photoeye);
            var photoeyeInstance = _workspaceNodes.Keys.First(key => key != instanceId);
            var arrangePassed = VerifyArrangeSelection(instanceId, photoeyeInstance, conveyor);
            SetSelection([]);
            var marqueePassed = SelectPlacementsInScreenRect(
                ScreenRectForWorkspacePlacements([instanceId, photoeyeInstance], 12.0f), additive: false) == 2;
            CreateConnectorLink(instanceId, "material_out", photoeyeInstance, "beam_center");
            CreateSignalLink(instanceId, "run_command", "conveyor_run");
            var linksPassed = _connectorLinks.Count == 1 && _signalLinks.Count == 1;
            SetSelection([instanceId, photoeyeInstance]);
            GroupSelection();
            var groupPassed = _workspaceGroups.Count == 1
                && _workspaceGroups[0].MemberInstanceIds.Count == 2;
            var hierarchyGroupSelectionPassed = _simulatorShell?.SelectFirstWorkspaceHierarchyGroupForVerification() == true
                && _selectedPlacementIds.Count == 2;
            var groupId = _workspaceGroups[0].Id;
            RenameGroup(groupId, "Conveyor Sensor Cell");
            var renamePassed = _workspaceGroups[0].Name == "Conveyor Sensor Cell";
            UndoWorkspace();
            var renameUndoPassed = _workspaceGroups[0].Name == "Group 001";
            RedoWorkspace();
            var renameRedoPassed = _workspaceGroups[0].Name == "Conveyor Sensor Cell";
            RenameGroup(groupId, "   ");
            var blankRenameRejected = _workspaceGroups[0].Name == "Conveyor Sensor Cell";
            var customPivot = Array(_workspaceNodes[instanceId].Node.Position);
            SetGroupPivot(groupId, customPivot);
            var pivotSetPassed = _workspaceGroups[0].Pivot?.SequenceEqual(customPivot) == true
                && VerifyCustomGroupPivot(instanceId, photoeyeInstance);
            SetSelection([]);
            var groupMarqueePassed = SelectPlacementsInScreenRect(
                ScreenRectForWorkspacePlacements([instanceId], 5.0f), additive: false) == 2;
            SelectPlacement(instanceId);
            var groupSelectionPassed = _selectedPlacementIds.Count == 2;
            var groupGizmoPassed = VerifyGroupGizmo(instanceId, photoeyeInstance);
            _sceneRuntime?.RunDefault();
            var mappingStatus = ApplySignalMappings().GetValueOrDefault(_signalLinks[0].Id);
            var mappedController = _workspaceNodes[instanceId].Node
                .GetNodeOrNull<ConveyorController>("WorkspaceRuntimeAdapter");
            var mappingLive = mappingStatus?.State == SignalMappingRuntimeState.LiveInput
                && mappedController is not null
                && mappedController.RunCommand;
            _sceneRuntime?.StopSimulation();
            mappingLive &= mappedController is not null && !mappedController.RunCommand;
            SetSelection([instanceId, photoeyeInstance]);
            CopySelection();
            PasteSelection();
            var clipboardPassed = _workspaceNodes.Count == 4
                && _connectorLinks.Count == 2
                && _signalLinks.Count == 2
                && _selectedPlacementIds.Count == 2
                && _workspaceGroups.Count == 2
                && _workspaceGroups.Any(group => group.Pivot is not null);
            var copiedGroup = _workspaceGroups.First(group => group.Id != groupId);
            var copiedName = copiedGroup.Name;
            RenameGroup(copiedGroup.Id, "Conveyor Sensor Cell");
            var duplicateRenameRejected = _workspaceGroups.First(group => group.Id == copiedGroup.Id).Name == copiedName;
            UndoWorkspace();
            var clipboardUndoPassed = _workspaceNodes.Count == 2
                && _connectorLinks.Count == 1
                && _signalLinks.Count == 1
                && _workspaceGroups.Count == 1;
            SelectPlacement(instanceId);
            UngroupSelection();
            var ungroupPassed = _workspaceGroups.Count == 0;
            UndoWorkspace();
            var ungroupUndoPassed = _workspaceGroups.Count == 1;
            SetSelection([instanceId, photoeyeInstance]);
            DeleteSelection();
            var batchDeletePassed = _workspaceNodes.Count == 0;
            UndoWorkspace();
            var batchDeleteUndoPassed = _workspaceNodes.Count == 2
                && _connectorLinks.Count == 1
                && _signalLinks.Count == 1
                && _workspaceGroups.Count == 1;
            var nestedRootId = CreateNestedGroupReviewAssembly(_candidateCatalog);
            var nestedRoot = _workspaceGroups.Single(group => group.Id == nestedRootId);
            var nestedChildren = _workspaceGroups.Where(group => group.ParentGroupId == nestedRootId).ToArray();
            var nestedGroupsPassed = _workspaceGroups.Count == 3
                && nestedRoot.MemberInstanceIds.Count == 4
                && nestedChildren.Length == 2
                && nestedChildren.All(group => group.MemberInstanceIds.Count == 2);
            var nestedHierarchySelectionPassed = _simulatorShell?.SelectFirstWorkspaceHierarchyGroupForVerification() == true
                && _selectedPlacementIds.Count == 4;
            UngroupSelection();
            var nestedUngroupPassed = _workspaceGroups.Count == 2
                && _workspaceGroups.All(group => group.ParentGroupId is null)
                && _workspaceGroups.All(group => group.MemberInstanceIds.Count == 2);
            UndoWorkspace();
            var nestedUngroupUndoPassed = _workspaceGroups.Count == 3
                && _workspaceGroups.Count(group => group.ParentGroupId == nestedRootId) == 2;
            var expected = CreateWorkspaceDocument();
            VerifyWorkspaceWriteFailure(expected);
            var invalidSibling = new WorkspaceGroup("group-overlap", "Overlapping sibling",
                [expected.Placements[0].InstanceId, expected.Placements[1].InstanceId]);
            var siblingOverlapRejected = false;
            try
            {
                WorkspaceRepository.Save(expected with { Groups = [.. expected.Groups!, invalidSibling] },
                    WorkspaceRepository.VerificationWorkspacePath);
            }
            catch (InvalidOperationException)
            {
                siblingOverlapRejected = true;
            }
            WorkspaceRepository.Save(expected, WorkspaceRepository.VerificationWorkspacePath);
            // A descendant listed before a cyclic pair must also reject promptly.
            // Checking only whether a parent returns to the starting group hangs here.
            var cycleMembers = expected.Placements.Take(2).Select(item => item.InstanceId).ToArray();
            var cycleRejected = false;
            GD.Print("WORKSPACE_CYCLE_VERIFY starting descendant-first malformed hierarchy");
            try
            {
                WorkspaceRepository.Save(expected with { Groups = [
                    new WorkspaceGroup("descendant", "Descendant", cycleMembers, ParentGroupId: "cycle-a"),
                    new WorkspaceGroup("cycle-a", "Cycle A", cycleMembers, ParentGroupId: "cycle-b"),
                    new WorkspaceGroup("cycle-b", "Cycle B", cycleMembers, ParentGroupId: "cycle-a"),
                ] }, WorkspaceRepository.VerificationWorkspacePath);
            }
            catch (InvalidOperationException exception)
            {
                cycleRejected = exception.Message.Contains("parent cycle", StringComparison.Ordinal);
            }
            GD.Print($"WORKSPACE_CYCLE_VERIFY rejected={cycleRejected}");
            var loaded = WorkspaceRepository.Load(
                _candidateCatalog,
                _sceneCatalog,
                WorkspaceRepository.VerificationWorkspacePath
            );
            var passed = loaded.SourceSceneId == expected.SourceSceneId
                && loaded.Placements.Count == 4
                && loaded.Placements[0].AssetId == expected.Placements[0].AssetId
                && loaded.ConnectorLinks?.Count == 1
                && loaded.SignalLinks?.Count == 1
                && loaded.Groups?.Count == 3
                && loaded.Groups.Any(group => group.Pivot?.SequenceEqual(customPivot) == true)
                && loaded.Groups.Count(group => group.ParentGroupId is not null) == 2
                && undoPassed && redoPassed && deletePassed && restorePassed && linksPassed
                && mappingLive && snapPassed && duplicatePassed && duplicateUndoPassed
                && gizmoPassed && arrangePassed && clipboardPassed && clipboardUndoPassed
                && batchDeletePassed && batchDeleteUndoPassed && groupPassed
                && hierarchyGroupSelectionPassed && marqueePassed && groupMarqueePassed && groupSelectionPassed
                && renamePassed && renameUndoPassed && renameRedoPassed
                && blankRenameRejected && duplicateRenameRejected && pivotSetPassed
                && groupGizmoPassed && ungroupPassed && ungroupUndoPassed
                && nestedGroupsPassed && nestedHierarchySelectionPassed
                && nestedUngroupPassed && nestedUngroupUndoPassed && siblingOverlapRejected && cycleRejected;
            GD.Print($"WORKSPACE_VERIFY {(passed ? "PASS" : "FAIL")} scene={loaded.SourceSceneId} placements={loaded.Placements.Count} gizmo={gizmoPassed} marquee={marqueePassed} arrange={arrangePassed} groupMarquee={groupMarqueePassed} group={groupPassed} hierarchyGroupSelect={hierarchyGroupSelectionPassed} rename={renamePassed} renameUndo={renameUndoPassed} renameRedo={renameRedoPassed} blankRenameReject={blankRenameRejected} duplicateRenameReject={duplicateRenameRejected} pivot={pivotSetPassed} groupSelect={groupSelectionPassed} groupGizmo={groupGizmoPassed} ungroup={ungroupPassed} ungroupUndo={ungroupUndoPassed} nested={nestedGroupsPassed} nestedHierarchySelect={nestedHierarchySelectionPassed} nestedUngroup={nestedUngroupPassed} nestedUngroupUndo={nestedUngroupUndoPassed} siblingOverlapReject={siblingOverlapRejected} undo={undoPassed} redo={redoPassed} snap={snapPassed} duplicate={duplicatePassed} duplicateUndo={duplicateUndoPassed} clipboard={clipboardPassed} clipboardUndo={clipboardUndoPassed} batchDelete={batchDeletePassed} batchDeleteUndo={batchDeleteUndoPassed} delete={deletePassed} restore={restorePassed} links={linksPassed} mappingLive={mappingLive}");
            DirAccess.RemoveAbsolute(path);
            GetTree().Quit(passed ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"WORKSPACE_VERIFY FAIL {exception.Message}");
            if (FileAccess.FileExists(WorkspaceRepository.VerificationWorkspacePath))
                DirAccess.RemoveAbsolute(path);
            GetTree().Quit(1);
        }
    }

    private void PlaceAsset(
        AssetDefinition asset,
        WorkspacePlacement? restored = null,
        bool recordUndo = true
    )
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        if (_sceneCompositionRoot is null)
        {
            GD.PushWarning("Asset placement requires an active scene.");
            return;
        }
        var packed = ResourceLoader.Load<PackedScene>(asset.Model.DeliveryGltf)
            ?? throw new InvalidOperationException($"Unable to load asset {asset.Model.DeliveryGltf}.");
        if (recordUndo && restored is null) RecordUndo();
        var model = packed.Instantiate<Node3D>();
        var instanceId = restored?.InstanceId ?? NextPlacementInstanceId(asset.Id);
        model.Name = $"UserPlacement_{SafeNodeName(instanceId)}";
        model.Position = restored is null
            ? FindOpenPlacementPosition(asset)
            : Vector(restored.Position);
        if (restored is not null)
        {
            model.RotationDegrees = Vector(restored.Rotation);
            model.Scale = Vector(restored.Scale);
        }
        _sceneCompositionRoot.AddChild(model);
        WorkspaceSignalRuntimeAdapter.AttachSupportedController(asset, model);
        model.AddChild(CreateSelectionOutline(asset));
        _workspaceNodes[instanceId] = (asset.Id, model);
        SelectPlacement(instanceId);
        RefreshWorkspaceUi();
        if (restored is null && _mainCamera is not null)
        {
            FrameComposition(_mainCamera, _sceneCompositionRoot, _sceneCameraDirection);
        }
        _simulatorShell?.SetWorkspaceStatus($"Workspace modified · {_workspaceNodes.Count} placed assets");
        if (recordUndo) RefreshWorkspaceDirty();
        GD.Print($"ASSET_PLACED id={asset.Id} node={model.Name}");
    }

    /// <summary>
    /// Shows one catalog model in the center viewport without adding it to the
    /// workspace. The authored scene remains loaded but hidden, so Return is
    /// instant and has no effect on scene state, mappings, or PLC transport.
    /// </summary>
    private void OpenAssetPreview(AssetDefinition asset)
    {
        if (_mainCamera is null || _sceneCompositionRoot is null) return;
        CloseAssetPreview();
        var packed = ResourceLoader.Load<PackedScene>(asset.Model.DeliveryGltf)
            ?? throw new InvalidOperationException($"Unable to load asset preview {asset.Model.DeliveryGltf}.");
        _assetPreviewRoot = new Node3D { Name = "AssetPreviewRoot" };
        var model = packed.Instantiate<Node3D>();
        model.Name = $"AssetPreview_{SafeNodeName(asset.Id)}";
        _assetPreviewRoot.AddChild(model);
        AddChild(_assetPreviewRoot);
        _sceneCompositionRoot.Visible = false;
        if (_connectionVisuals is not null) _connectionVisuals.Visible = false;
        if (_transformGizmo is not null) _transformGizmo.Visible = false;
        FrameComposition(_mainCamera, _assetPreviewRoot, _sceneCameraDirection);
        _simulatorShell?.SetAssetPreviewState(asset);
        GD.Print($"ASSET_PREVIEW_OPEN id={asset.Id}");
    }

    private void CloseAssetPreview()
    {
        if (_assetPreviewRoot is null) return;
        RemoveChild(_assetPreviewRoot);
        _assetPreviewRoot.QueueFree();
        _assetPreviewRoot = null;
        if (_sceneCompositionRoot is not null) _sceneCompositionRoot.Visible = true;
        if (_connectionVisuals is not null) _connectionVisuals.Visible = true;
        if (_transformGizmo is not null) _transformGizmo.Visible = true;
        if (_mainCamera is not null && _sceneCompositionRoot is not null)
        {
            FrameComposition(_mainCamera, _sceneCompositionRoot, _sceneCameraDirection);
        }
        _simulatorShell?.SetAssetPreviewState(null);
        GD.Print("ASSET_PREVIEW_CLOSE");
    }

    private void SelectPlacement(string? instanceId) => SelectPlacement(instanceId, additive: false);

    private void SelectPlacement(string? instanceId, bool additive)
    {
        if(SelectTemplateAssembly(instanceId))return;
        if (!additive) _selectedPlacementIds.Clear();
        if (instanceId is not null && _workspaceNodes.ContainsKey(instanceId))
        {
            var group = GroupFor(instanceId);
            IReadOnlyList<string> targets = group?.MemberInstanceIds ?? [instanceId];
            var allSelected = targets.All(_selectedPlacementIds.Contains);
            if (additive && allSelected)
                foreach (var target in targets) _selectedPlacementIds.Remove(target);
            else
                foreach (var target in targets) _selectedPlacementIds.Add(target);
        }
        _selectedPlacementId = instanceId is not null && _selectedPlacementIds.Contains(instanceId)
            ? instanceId
            : _selectedPlacementIds.LastOrDefault();
        foreach (var item in _workspaceNodes)
        {
            if (item.Value.Node.GetNodeOrNull<Node3D>("EditorSelectionOutline") is { } outline)
                outline.Visible = _selectedPlacementIds.Contains(item.Key);
        }
        UpdateTransformGizmo();
        RefreshWorkspaceUi();
    }

    private void SetSelection(IReadOnlyList<string> instanceIds)
    {
        if(instanceIds.Contains(WorkspaceTemplatePolicy.SelectionId)&&SelectTemplateAssembly(WorkspaceTemplatePolicy.SelectionId))return;
        _selectedPlacementIds.Clear();
        foreach (var id in instanceIds)
        {
            if (!_workspaceNodes.ContainsKey(id)) continue;
            var group = GroupFor(id);
            if (group is null) _selectedPlacementIds.Add(id);
            else foreach (var member in group.MemberInstanceIds) _selectedPlacementIds.Add(member);
        }
        _selectedPlacementId = instanceIds.LastOrDefault(_workspaceNodes.ContainsKey)
            ?? _selectedPlacementIds.LastOrDefault();
        foreach (var item in _workspaceNodes)
            if (item.Value.Node.GetNodeOrNull<Node3D>("EditorSelectionOutline") is { } outline)
                outline.Visible = _selectedPlacementIds.Contains(item.Key);
        UpdateTransformGizmo();
        RefreshWorkspaceUi();
    }

    private WorkspaceGroup? GroupFor(string instanceId) => _workspaceGroups
        .Where(group => group.ParentGroupId is null && group.MemberInstanceIds.Contains(instanceId, StringComparer.Ordinal))
        .FirstOrDefault();

    private WorkspaceGroup? ExactSelectedGroup()
    {
        var selected = _selectedPlacementIds.ToHashSet(StringComparer.Ordinal);
        return _workspaceGroups.FirstOrDefault(group => group.MemberInstanceIds.Count == selected.Count
            && group.MemberInstanceIds.All(selected.Contains));
    }

    private Vector3 SharedLocalPivot()
    {
        var group = ExactSelectedGroup();
        if (group?.Pivot is { Length: 3 } pivot) return Vector(pivot);
        var transforms = _dragStartTransforms.Count > 0
            ? _dragStartTransforms.Values.Select(transform => transform.Position).ToArray()
            : _selectedPlacementIds.Where(_workspaceNodes.ContainsKey)
                .Select(id => _workspaceNodes[id].Node.Position).ToArray();
        return transforms.Length == 0 ? Vector3.Zero
            : transforms.Aggregate(Vector3.Zero, (sum, position) => sum + position) / transforms.Length;
    }

    private void SetGroupPivot(string groupId, double[]? pivot)
    {
        var index = _workspaceGroups.FindIndex(group => group.Id == groupId);
        if (index < 0)
        {
            _simulatorShell?.SetWorkspaceStatus("Pivot update failed · group no longer exists", isError: true);
            return;
        }
        if (pivot is not null && (pivot.Length != 3 || pivot.Any(value => !double.IsFinite(value))))
        {
            _simulatorShell?.SetWorkspaceStatus("Pivot rejected · coordinates must be finite X/Y/Z values", isError: true);
            return;
        }
        var group = _workspaceGroups[index];
        var normalized = pivot?.ToArray();
        if ((group.Pivot is null && normalized is null)
            || (group.Pivot is not null && normalized is not null && group.Pivot.SequenceEqual(normalized))) return;
        RecordUndo();
        _workspaceGroups[index] = new WorkspaceGroup(group.Id, group.Name, group.MemberInstanceIds, normalized, group.ParentGroupId);
        UpdateTransformGizmo();
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus(normalized is null
            ? "Group pivot reset · member centroid"
            : $"Group pivot set · X {normalized[0]:0.###} / Y {normalized[1]:0.###} / Z {normalized[2]:0.###}");
        RefreshWorkspaceDirty();
    }

    private void ShowReviewMarquee()
    {
        if (_simulatorShell is null) return;
        var viewport = _simulatorShell.SceneViewportRect();
        _simulatorShell.SetSelectionMarquee(
            viewport.Position + viewport.Size * new Vector2(0.16f, 0.18f),
            viewport.Position + viewport.Size * new Vector2(0.78f, 0.78f),
            true);
        _simulatorShell.SetWorkspaceStatus("Marquee selection · Ctrl/Shift adds · Esc cancels");
    }

    private int SelectPlacementsInScreenRect(Rect2 rect, bool additive)
    {
        if (_mainCamera is null) return 0;
        var selected = new HashSet<string>(StringComparer.Ordinal);
        if (additive) selected.UnionWith(_selectedPlacementIds);
        foreach (var item in _workspaceNodes)
        {
            var asset = _candidateCatalog?.Assets.FirstOrDefault(candidate => candidate.Id == item.Value.AssetId);
            var height = asset is null ? 0.5f : (float)asset.Bounds.HeightM * 0.5f;
            var center = item.Value.Node.GlobalPosition + Vector3.Up * height;
            if (_mainCamera.IsPositionBehind(center)) continue;
            if (rect.HasPoint(_mainCamera.UnprojectPosition(center))) selected.Add(item.Key);
        }
        SetSelection(selected.ToArray());
        return _selectedPlacementIds.Count;
    }

    private void SelectAtScreenPoint(Vector2 point, bool additive)
    {
        if (_mainCamera is null) return;
        string? selected = null;
        var closest = 72.0f;
        foreach (var item in _workspaceNodes)
        {
            var asset = _candidateCatalog?.Assets.FirstOrDefault(candidate => candidate.Id == item.Value.AssetId);
            var height = asset is null ? 0.5f : (float)asset.Bounds.HeightM * 0.5f;
            var center = item.Value.Node.GlobalPosition + Vector3.Up * height;
            if (_mainCamera.IsPositionBehind(center)) continue;
            var distance = _mainCamera.UnprojectPosition(center).DistanceTo(point);
            if (distance < closest)
            {
                closest = distance;
                selected = item.Key;
            }
        }
        SelectPlacement(selected, additive);
    }

    private Rect2 ScreenRectForWorkspacePlacements(IEnumerable<string> instanceIds, float padding)
    {
        if (_mainCamera is null) return new Rect2();
        var points = instanceIds.Where(_workspaceNodes.ContainsKey).Select(id =>
        {
            var placement = _workspaceNodes[id];
            var asset = _candidateCatalog?.Assets.FirstOrDefault(candidate => candidate.Id == placement.AssetId);
            var height = asset is null ? 0.5f : (float)asset.Bounds.HeightM * 0.5f;
            return _mainCamera.UnprojectPosition(placement.Node.GlobalPosition + Vector3.Up * height);
        }).ToArray();
        if (points.Length == 0) return new Rect2();
        var min = points.Aggregate(points[0], (value, point) => new Vector2(
            MathF.Min(value.X, point.X), MathF.Min(value.Y, point.Y)));
        var max = points.Aggregate(points[0], (value, point) => new Vector2(
            MathF.Max(value.X, point.X), MathF.Max(value.Y, point.Y)));
        return new Rect2(min - Vector2.One * padding, max - min + Vector2.One * padding * 2.0f);
    }

    private static Vector2 ClampToRect(Vector2 point, Rect2 rect) => new(
        Mathf.Clamp(point.X, rect.Position.X, rect.End.X),
        Mathf.Clamp(point.Y, rect.Position.Y, rect.End.Y));

    private void GroupSelection()
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        var members = _selectedPlacementIds.Where(_workspaceNodes.ContainsKey).ToArray();
        if (members.Length < 2)
        {
            _simulatorShell?.SetWorkspaceStatus("Group rejected · select at least two placements", isError: true);
            return;
        }
        if (ExactSelectedGroup() is not null)
        {
            _simulatorShell?.SetWorkspaceStatus("Group unchanged · selection already matches a group");
            return;
        }
        RecordUndo();
        var id = NextId("group", _workspaceGroups.Select(group => group.Id));
        var ordinal = _workspaceGroups.Count + 1;
        _workspaceGroups.Add(new WorkspaceGroup(id, $"Group {ordinal:000}", members));
        foreach (var child in _workspaceGroups.Where(group => group.Id != id && group.ParentGroupId is null
                     && group.MemberInstanceIds.All(members.Contains)).ToArray())
        {
            var childIndex = _workspaceGroups.FindIndex(group => group.Id == child.Id);
            _workspaceGroups[childIndex] = child with { ParentGroupId = id };
        }
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus($"Created Group {ordinal:000} · {members.Length} placements");
        RefreshWorkspaceDirty();
    }

    private void UngroupSelection()
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        var groups = _workspaceGroups.Where(group => group.ParentGroupId is null &&
            group.MemberInstanceIds.Any(_selectedPlacementIds.Contains)).ToArray();
        if (groups.Length == 0)
        {
            _simulatorShell?.SetWorkspaceStatus("Ungroup skipped · selection is not grouped", isError: true);
            return;
        }
        RecordUndo();
        foreach (var group in groups)
        {
            foreach (var child in _workspaceGroups.Where(item => item.ParentGroupId == group.Id).ToArray())
            {
                var childIndex = _workspaceGroups.FindIndex(item => item.Id == child.Id);
                _workspaceGroups[childIndex] = child with { ParentGroupId = null };
            }
            _workspaceGroups.Remove(group);
        }
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus($"Removed {groups.Length} group{(groups.Length == 1 ? "" : "s")}");
        RefreshWorkspaceDirty();
    }

    private void RenameGroup(string groupId, string requestedName)
    {
        var index = _workspaceGroups.FindIndex(group => group.Id == groupId);
        if (index < 0)
        {
            _simulatorShell?.SetWorkspaceStatus("Rename failed · group no longer exists", isError: true);
            return;
        }
        var name = requestedName.Trim();
        if (name.Length == 0)
        {
            _simulatorShell?.SetWorkspaceStatus("Rename rejected · group name cannot be blank", isError: true);
            return;
        }
        if (_workspaceGroups.Any(group => group.Id != groupId
            && group.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            _simulatorShell?.SetWorkspaceStatus($"Rename rejected · group name '{name}' already exists", isError: true);
            return;
        }
        if (_workspaceGroups[index].Name == name) return;
        RecordUndo();
        var group = _workspaceGroups[index];
        _workspaceGroups[index] = new WorkspaceGroup(group.Id, name, group.MemberInstanceIds, group.Pivot, group.ParentGroupId);
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus($"Renamed group · {name}");
        RefreshWorkspaceDirty();
    }

    private List<string[]> SelectedArrangementUnits()
    {
        var selected = _selectedPlacementIds.Where(_workspaceNodes.ContainsKey)
            .ToHashSet(StringComparer.Ordinal);
        var consumed = new HashSet<string>(StringComparer.Ordinal);
        var units = new List<string[]>();
        foreach (var id in selected.OrderBy(value => value, StringComparer.Ordinal))
        {
            if (!consumed.Add(id)) continue;
            var group = GroupFor(id);
            var members = group is null
                ? [id]
                : group.MemberInstanceIds.Where(selected.Contains).ToArray();
            foreach (var member in members) consumed.Add(member);
            units.Add(members);
        }
        return units;
    }

    private void ArrangeSelection(WorkspaceArrangeOperation operation)
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        var units = SelectedArrangementUnits();
        var distribute = operation is WorkspaceArrangeOperation.DistributeX
            or WorkspaceArrangeOperation.DistributeY or WorkspaceArrangeOperation.DistributeZ;
        var minimum = distribute ? 3 : 2;
        if (units.Count < minimum)
        {
            _simulatorShell?.SetWorkspaceStatus(
                $"Arrange rejected · select at least {minimum} independent objects or groups", isError: true);
            return;
        }
        var axis = operation is WorkspaceArrangeOperation.AlignX or WorkspaceArrangeOperation.DistributeX ? 0
            : operation is WorkspaceArrangeOperation.AlignY or WorkspaceArrangeOperation.DistributeY ? 1 : 2;
        Vector3 Pivot(string[] unit) => unit.Aggregate(Vector3.Zero,
            (sum, id) => sum + _workspaceNodes[id].Node.Position) / unit.Length;
        float Component(Vector3 value) => axis == 0 ? value.X : axis == 1 ? value.Y : value.Z;
        Vector3 AxisVector() => axis == 0 ? Vector3.Right : axis == 1 ? Vector3.Up : Vector3.Back;
        void Translate(string[] unit, float delta)
        {
            foreach (var id in unit) _workspaceNodes[id].Node.Position += AxisVector() * delta;
        }

        RecordUndo();
        if (!distribute)
        {
            var primary = units.FirstOrDefault(unit => _selectedPlacementId is not null
                && unit.Contains(_selectedPlacementId, StringComparer.Ordinal)) ?? units[0];
            var target = Component(Pivot(primary));
            foreach (var unit in units)
                Translate(unit, target - Component(Pivot(unit)));
        }
        else
        {
            var ordered = units.OrderBy(unit => Component(Pivot(unit))).ToArray();
            var first = Component(Pivot(ordered[0]));
            var last = Component(Pivot(ordered[^1]));
            var spacing = (last - first) / (ordered.Length - 1);
            for (var index = 1; index < ordered.Length - 1; index++)
                Translate(ordered[index], first + spacing * index - Component(Pivot(ordered[index])));
        }
        UpdateTransformGizmo();
        RefreshWorkspaceUi();
        RefreshConnectionVisuals();
        _simulatorShell?.SetWorkspaceStatus($"Selection arranged · {operation}");
        RefreshWorkspaceDirty();
    }

    private void TransformPlacement(string instanceId, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        if(TryTransformFiniteBeltWorkspace(instanceId,position,rotation,scale))return;
        if(TryTransformFiniteRollerWorkspace(instanceId,position,rotation,scale))return;
        if(TransformTemplateAssembly(instanceId,position,rotation,scale))return;
        if (!_workspaceNodes.TryGetValue(instanceId, out var placement)) return;
        if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale))
        {
            _simulatorShell?.SetWorkspaceStatus("Transform rejected · values must be finite", isError: true);
            RefreshWorkspaceUi();
            return;
        }
        if (_snapEnabled)
        {
            position = Snap(position, _positionSnapM);
            rotation = Snap(rotation, _rotationSnapDegrees);
        }
        if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0)
        {
            _simulatorShell?.SetWorkspaceStatus("Transform rejected · scale must be positive", isError: true);
            RefreshWorkspaceUi();
            return;
        }
        if (placement.Node.Position.IsEqualApprox(position)
            && placement.Node.RotationDegrees.IsEqualApprox(rotation)
            && placement.Node.Scale.IsEqualApprox(scale)) return;
        RecordUndo();
        placement.Node.Position = position;
        placement.Node.RotationDegrees = rotation;
        placement.Node.Scale = scale;
        SelectPlacement(instanceId);
        _simulatorShell?.SetWorkspaceStatus($"Updated transform · {instanceId}");
        RefreshWorkspaceDirty();
    }

    private void DuplicatePlacement(string instanceId)
    {
        if (!_workspaceNodes.TryGetValue(instanceId, out var source)
            || _candidateCatalog?.Assets.FirstOrDefault(item => item.Id == source.AssetId) is not { } asset)
            return;
        RecordUndo();
        var duplicateId = NextPlacementInstanceId(asset.Id);
        var offset = MathF.Max((float)asset.Bounds.WidthM * 0.65f, 0.50f);
        var position = source.Node.Position + Vector3.Right * offset;
        if (_snapEnabled) position = Snap(position, _positionSnapM);
        var document = new WorkspacePlacement(
            duplicateId,
            asset.Id,
            Array(position),
            Array(source.Node.RotationDegrees),
            Array(source.Node.Scale)
        );
        PlaceAsset(asset, document, recordUndo: false);
        _simulatorShell?.SetWorkspaceStatus($"Duplicated placement · {duplicateId}");
        RefreshWorkspaceDirty();
    }

    private void DuplicateSelection()
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        if (_selectedPlacementIds.Count == 0) return;
        CopySelection();
        PasteSelection();
    }

    private void CopySelection()
    {
        if (_currentSceneId is null || _selectedPlacementIds.Count == 0)
        {
            _simulatorShell?.SetWorkspaceStatus("Copy skipped · no workspace selection", isError: true);
            return;
        }
        var selected = _selectedPlacementIds.ToHashSet(StringComparer.Ordinal);
        var placements = CreateWorkspaceDocument().Placements
            .Where(item => selected.Contains(item.InstanceId)).ToArray();
        var connectors = _connectorLinks.Where(link =>
            selected.Contains(link.FromInstanceId) && selected.Contains(link.ToInstanceId)).ToArray();
        var signals = _signalLinks.Where(link => selected.Contains(link.InstanceId)).ToArray();
        var groups = _workspaceGroups.Where(group =>
            group.MemberInstanceIds.All(selected.Contains)).ToArray();
        _workspaceClipboard = new WorkspaceDocument(5, _currentSceneId, placements, connectors, signals, groups);
        _simulatorShell?.SetWorkspaceStatus($"Copied {placements.Length} placement{(placements.Length == 1 ? "" : "s")}");
    }

    private void PasteSelection()
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        if (_workspaceClipboard is null || _candidateCatalog is null || _currentSceneId is null)
        {
            _simulatorShell?.SetWorkspaceStatus("Paste skipped · clipboard is empty", isError: true);
            return;
        }
        RecordUndo();
        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var pastedIds = new List<string>();
        foreach (var source in _workspaceClipboard.Placements)
        {
            var asset = _candidateCatalog.Assets.FirstOrDefault(item => item.Id == source.AssetId);
            if (asset is null) continue;
            var newId = NextPlacementInstanceId(asset.Id);
            idMap[source.InstanceId] = newId;
            var position = Vector(source.Position) + new Vector3(0.5f, 0.0f, 0.5f);
            if (_snapEnabled) position = Snap(position, _positionSnapM);
            PlaceAsset(asset, new WorkspacePlacement(
                newId,
                source.AssetId,
                Array(position),
                source.Rotation.ToArray(),
                source.Scale.ToArray()
            ), recordUndo: false);
            pastedIds.Add(newId);
        }
        foreach (var link in _workspaceClipboard.ConnectorLinks ?? [])
        {
            if (!idMap.TryGetValue(link.FromInstanceId, out var from)
                || !idMap.TryGetValue(link.ToInstanceId, out var to)) continue;
            _connectorLinks.Add(new WorkspaceConnectorLink(
                NextId("connector-link", _connectorLinks.Select(item => item.Id)),
                from, link.FromConnectorId, to, link.ToConnectorId));
        }
        var skippedSignalMappings = 0;
        foreach (var link in _workspaceClipboard.SignalLinks ?? [])
        {
            if (!idMap.TryGetValue(link.InstanceId, out var instance)
                || !_workspaceNodes.TryGetValue(instance, out var placement)
                || _candidateCatalog.Assets.FirstOrDefault(item => item.Id == placement.AssetId) is not { } asset
                || asset.Signals.FirstOrDefault(item => item.Id == link.SignalId) is not { } signal
                || ScenePointType(link.PointName) is not { } pointType
                || !TypesCompatible(signal.DataType, pointType))
            {
                skippedSignalMappings++;
                continue;
            }
            _signalLinks.Add(new WorkspaceSignalLink(
                NextId("signal-link", _signalLinks.Select(item => item.Id)),
                instance, link.SignalId, link.PointName));
        }
        var copiedGroups = _workspaceClipboard.Groups ?? [];
        var allocatedGroupIds = _workspaceGroups.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var groupIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var group in copiedGroups)
        {
            var next = NextId("group", allocatedGroupIds);
            allocatedGroupIds.Add(next);
            groupIds.Add(group.Id, next);
        }
        foreach (var group in copiedGroups)
        {
            var members = group.MemberInstanceIds
                .Where(idMap.ContainsKey)
                .Select(member => idMap[member]).ToArray();
            if (members.Length < 2) continue;
            var id = groupIds[group.Id];
            var pivot = group.Pivot is null ? null : Array(Vector(group.Pivot) + new Vector3(0.5f, 0.0f, 0.5f));
            _workspaceGroups.Add(new WorkspaceGroup(id,
                $"{group.Name} Copy", members, pivot,
                group.ParentGroupId is null ? null : groupIds.GetValueOrDefault(group.ParentGroupId)));
        }
        SetSelection(pastedIds);
        RefreshConnectionVisuals();
        _simulatorShell?.SetWorkspaceStatus(
            $"Pasted {pastedIds.Count} placement{(pastedIds.Count == 1 ? "" : "s")}" +
            (skippedSignalMappings > 0
                ? $" · skipped {skippedSignalMappings} scene-incompatible signal mapping{(skippedSignalMappings == 1 ? "" : "s")}"
                : string.Empty),
            isError: skippedSignalMappings > 0);
        RefreshWorkspaceDirty();
    }

    private string NextPlacementInstanceId(string assetId)
    {
        string candidate;
        do
        {
            _placedAssetCount++;
            candidate = $"placement-{_placedAssetCount:000}-{SafeNodeName(assetId)}";
        } while (_workspaceNodes.ContainsKey(candidate));
        return candidate;
    }

    private void FocusPlacement(string instanceId)
    {
        if (!_workspaceNodes.TryGetValue(instanceId, out var placement)
            || _candidateCatalog?.Assets.FirstOrDefault(item => item.Id == placement.AssetId) is not { } asset)
            return;
        var center = placement.Node.GlobalPosition + Vector3.Up * ((float)asset.Bounds.HeightM * 0.5f);
        var radius = MathF.Max((float)Math.Max(asset.Bounds.WidthM,
            Math.Max(asset.Bounds.HeightM, asset.Bounds.DepthM)), 0.25f);
        _cameraController?.FocusOn(center, radius);
        _simulatorShell?.SetWorkspaceStatus($"Focused selection · {instanceId}");
    }

    private void ResetPlacementTransform(string instanceId)
    {
        if(instanceId==WorkspaceTemplatePolicy.SelectionId&&_sceneCompositionRoot?.GetNodeOrNull<Node3D>("ControlInstallation") is { } support)
        {
            var original=support.GetMeta("templateOriginalTransform").AsTransform3D();
            TransformTemplateAssembly(instanceId,original.Origin,original.Basis.GetEuler()*180/Mathf.Pi,Vector3.One);
            return;
        }
        if (!_workspaceNodes.TryGetValue(instanceId, out var placement)) return;
        TransformPlacement(instanceId, placement.Node.Position, Vector3.Zero, Vector3.One);
    }

    private void SetSnapSettings(bool enabled, double positionM, double rotationDegrees)
    {
        _snapEnabled = enabled;
        _positionSnapM = MathF.Max((float)positionM, 0.001f);
        _rotationSnapDegrees = MathF.Max((float)rotationDegrees, 0.1f);
        _simulatorShell?.SetWorkspaceStatus(enabled
            ? $"Snapping enabled · {_positionSnapM:0.###} m / {_rotationSnapDegrees:0.#}°"
            : "Snapping disabled");
    }

    private void SetTransformMode(WorkspaceTransformMode mode)
    {
        _transformGizmo?.SetMode(mode);
        _simulatorShell?.SetTransformMode(mode);
        _simulatorShell?.SetWorkspaceStatus($"Transform mode · {mode.ToString().ToUpperInvariant()}  (drag colored handles)");
    }

    private void SetTransformSpace(WorkspaceTransformSpace space)
    {
        _transformSpace = space;
        _transformGizmo?.SetSpace(space);
        _simulatorShell?.SetTransformSpace(space);
        UpdateTransformGizmo();
        _simulatorShell?.SetWorkspaceStatus($"Transform axes · {space.ToString().ToUpperInvariant()}");
    }

    private void UpdateTransformGizmo()
    {
        if (_transformGizmo is null) return;
        if(_workspaceFiniteBeltDocument is not null || _workspaceFiniteRollerDocument is not null){_transformGizmo.Detach();return;}
        var selected = _selectedPlacementIds
            .Where(_workspaceNodes.ContainsKey)
            .Select(id => _workspaceNodes[id].Node.GlobalPosition)
            .ToArray();
        if (selected.Length == 0)
        {
            _transformGizmo.Detach();
            return;
        }
        var orientation = Basis.Identity;
        if (_transformSpace == WorkspaceTransformSpace.Local
            && _selectedPlacementId is not null
            && _workspaceNodes.TryGetValue(_selectedPlacementId, out var primary))
            orientation = primary.Node.GlobalBasis.Orthonormalized();
        var pivot = SharedLocalPivot();
        var primaryParent = _selectedPlacementId is not null && _workspaceNodes.TryGetValue(_selectedPlacementId, out var pivotPrimary)
            ? pivotPrimary.Node.GetParent() as Node3D
            : null;
        var globalPivot = primaryParent is null ? pivot : primaryParent.ToGlobal(pivot);
        _transformGizmo.AttachTo(globalPivot, orientation);
    }

    private void ApplyGizmoDrag(float amount)
    {
        if (_transformGizmo is null || _dragStartTransforms.Count == 0) return;
        var worldAxis = _transformGizmo.ActiveAxisWorld();
        var axisIndex = _transformGizmo.ActiveAxis;
        var localComponent = WorkspaceTransformGizmo.Axis(axisIndex).Abs();
        foreach (var item in _dragStartTransforms)
        {
            if (!_workspaceNodes.TryGetValue(item.Key, out var placement)) continue;
            var start = item.Value;
            var parent = placement.Node.GetParent() as Node3D;
            var axis = parent is null
                ? worldAxis
                : (parent.GlobalBasis.Inverse() * worldAxis).Normalized();
            switch (_transformGizmo.Mode)
            {
                case WorkspaceTransformMode.Move:
                    var position = start.Position + axis * amount;
                    placement.Node.Position = _snapEnabled ? Snap(position, _positionSnapM) : position;
                    break;
                case WorkspaceTransformMode.Rotate:
                    var rotationAmount = _snapEnabled
                        ? MathF.Round(amount / _rotationSnapDegrees) * _rotationSnapDegrees
                        : amount;
                    var basis = new Basis(axis, Mathf.DegToRad(rotationAmount));
                    placement.Node.Position = _dragPivot + basis * (start.Position - _dragPivot);
                    var startBasis = Basis.FromEuler(start.Rotation * (Mathf.Pi / 180.0f));
                    placement.Node.Rotation = (basis * startBasis).GetEuler();
                    break;
                case WorkspaceTransformMode.Scale:
                    var factor = MathF.Max(1.0f + amount, 0.01f);
                    var offset = start.Position - _dragPivot;
                    placement.Node.Position = _dragPivot
                        + offset + axis * (offset.Dot(axis) * (factor - 1.0f));
                    var scale = start.Scale * (Vector3.One + localComponent * (factor - 1.0f));
                    placement.Node.Scale = new Vector3(
                        MathF.Max(scale.X, 0.01f),
                        MathF.Max(scale.Y, 0.01f),
                        MathF.Max(scale.Z, 0.01f));
                    break;
            }
        }
        UpdateTransformGizmo();
    }

    private void CaptureDragStartTransforms()
    {
        _dragStartTransforms.Clear();
        foreach (var id in _selectedPlacementIds)
            if (_workspaceNodes.TryGetValue(id, out var placement))
                _dragStartTransforms[id] = (
                    placement.Node.Position,
                    placement.Node.RotationDegrees,
                    placement.Node.Scale);
        _dragPivot = SharedLocalPivot();
    }

    private bool VerifyGizmoInteraction(string instanceId)
    {
        if (_transformGizmo is null || _mainCamera is null
            || !_workspaceNodes.TryGetValue(instanceId, out var placement)) return false;
        SelectPlacement(instanceId);

        bool Drag(WorkspaceTransformMode mode, Vector3 worldHandle, Vector2 screenDelta)
        {
            SetTransformMode(mode);
            var pointer = _mainCamera.UnprojectPosition(
                _transformGizmo.GlobalPosition + worldHandle);
            if (!_transformGizmo.BeginDrag(_mainCamera, pointer)) return false;
            CaptureDragStartTransforms();
            var amount = _transformGizmo.DragAmount(_mainCamera, pointer + screenDelta);
            ApplyGizmoDrag(amount);
            _transformGizmo.EndDrag();
            return MathF.Abs(amount) > 0.001f;
        }

        SetTransformMode(WorkspaceTransformMode.Move);
        var original = placement.Node.Position;
        var originScreen = _mainCamera.UnprojectPosition(_transformGizmo.GlobalPosition);
        var axisScreen = _mainCamera.UnprojectPosition(
            _transformGizmo.GlobalPosition + Vector3.Right * 0.65f);
        var direction = (axisScreen - originScreen).Normalized();
        if (direction.LengthSquared() < 0.5f
            || !_transformGizmo.BeginDrag(_mainCamera, axisScreen)) return false;
        CaptureDragStartTransforms();
        var amount = _transformGizmo.DragAmount(_mainCamera, axisScreen + direction * 24.0f);
        ApplyGizmoDrag(amount);
        _transformGizmo.EndDrag();
        var movePassed = placement.Node.Position.X > original.X + 0.01f;
        placement.Node.Position = original;

        var originalRotation = placement.Node.RotationDegrees;
        var rotatePassed = Drag(WorkspaceTransformMode.Rotate, Vector3.Right * 0.85f,
            new Vector2(0, 28));
        rotatePassed &= !placement.Node.RotationDegrees.IsEqualApprox(originalRotation);
        placement.Node.RotationDegrees = originalRotation;

        var originalScale = placement.Node.Scale;
        var scaleOrigin = _mainCamera.UnprojectPosition(_transformGizmo.GlobalPosition);
        var scaleHandle = _mainCamera.UnprojectPosition(
            _transformGizmo.GlobalPosition + Vector3.Right * 0.65f);
        var scaleDirection = (scaleHandle - scaleOrigin).Normalized();
        var scalePassed = Drag(WorkspaceTransformMode.Scale, Vector3.Right * 0.65f,
            scaleDirection * 24.0f);
        scalePassed &= !placement.Node.Scale.IsEqualApprox(originalScale);
        placement.Node.Scale = originalScale;

        SetTransformMode(WorkspaceTransformMode.Move);
        UpdateTransformGizmo();
        return movePassed && rotatePassed && scalePassed;
    }

    private bool VerifyGroupGizmo(string firstId, string secondId)
    {
        if (_transformGizmo is null || _mainCamera is null
            || !_workspaceNodes.TryGetValue(firstId, out var first)
            || !_workspaceNodes.TryGetValue(secondId, out var second)) return false;
        SetSelection([secondId, firstId]);
        SetTransformMode(WorkspaceTransformMode.Move);
        var firstStart = first.Node.Position;
        var secondStart = second.Node.Position;
        var originScreen = _mainCamera.UnprojectPosition(_transformGizmo.GlobalPosition);
        var axisScreen = _mainCamera.UnprojectPosition(
            _transformGizmo.GlobalPosition + Vector3.Right * 0.65f);
        var direction = (axisScreen - originScreen).Normalized();
        if (direction.LengthSquared() < 0.5f
            || !_transformGizmo.BeginDrag(_mainCamera, axisScreen)) return false;
        CaptureDragStartTransforms();
        var amount = _transformGizmo.DragAmount(_mainCamera, axisScreen + direction * 24.0f);
        ApplyGizmoDrag(amount);
        _transformGizmo.EndDrag();
        var firstDelta = first.Node.Position - firstStart;
        var secondDelta = second.Node.Position - secondStart;
        var passed = firstDelta.X > 0.01f && firstDelta.IsEqualApprox(secondDelta);
        first.Node.Position = firstStart;
        second.Node.Position = secondStart;
        _dragStartTransforms.Clear();

        var firstRotationStart = first.Node.RotationDegrees;
        var secondRotationStart = second.Node.RotationDegrees;
        var originalSeparation = firstStart.DistanceTo(secondStart);
        SetTransformMode(WorkspaceTransformMode.Rotate);
        UpdateTransformGizmo();
        var rotatePointer = _mainCamera.UnprojectPosition(
            _transformGizmo.GlobalPosition + Vector3.Right * 0.85f);
        var rotatePassed = _transformGizmo.BeginDrag(_mainCamera, rotatePointer);
        if (rotatePassed)
        {
            CaptureDragStartTransforms();
            amount = _transformGizmo.DragAmount(_mainCamera, rotatePointer + new Vector2(0, 28));
            ApplyGizmoDrag(amount);
            _transformGizmo.EndDrag();
            rotatePassed = MathF.Abs(amount) > 0.1f
                && MathF.Abs(first.Node.Position.DistanceTo(second.Node.Position) - originalSeparation) < 0.001f
                && (!first.Node.Position.IsEqualApprox(firstStart)
                    || !second.Node.Position.IsEqualApprox(secondStart));
        }
        first.Node.Position = firstStart;
        second.Node.Position = secondStart;
        first.Node.RotationDegrees = firstRotationStart;
        second.Node.RotationDegrees = secondRotationStart;
        _dragStartTransforms.Clear();

        var firstScaleStart = first.Node.Scale;
        var secondScaleStart = second.Node.Scale;
        SetTransformMode(WorkspaceTransformMode.Scale);
        UpdateTransformGizmo();
        originScreen = _mainCamera.UnprojectPosition(_transformGizmo.GlobalPosition);
        axisScreen = _mainCamera.UnprojectPosition(
            _transformGizmo.GlobalPosition + Vector3.Right * 0.65f);
        direction = (axisScreen - originScreen).Normalized();
        var scalePassed = direction.LengthSquared() >= 0.5f
            && _transformGizmo.BeginDrag(_mainCamera, axisScreen);
        if (scalePassed)
        {
            CaptureDragStartTransforms();
            amount = _transformGizmo.DragAmount(_mainCamera, axisScreen + direction * 24.0f);
            ApplyGizmoDrag(amount);
            _transformGizmo.EndDrag();
            scalePassed = !first.Node.Scale.IsEqualApprox(firstScaleStart)
                && !second.Node.Scale.IsEqualApprox(secondScaleStart)
                && MathF.Abs((first.Node.Position - second.Node.Position).X)
                    > MathF.Abs((firstStart - secondStart).X);
        }
        first.Node.Position = firstStart;
        second.Node.Position = secondStart;
        first.Node.Scale = firstScaleStart;
        second.Node.Scale = secondScaleStart;
        _dragStartTransforms.Clear();

        var firstRotation = first.Node.RotationDegrees;
        first.Node.RotationDegrees = new Vector3(0, 45, 0);
        SetTransformMode(WorkspaceTransformMode.Move);
        SetTransformSpace(WorkspaceTransformSpace.Local);
        UpdateTransformGizmo();
        var localAxis = (first.Node.GlobalBasis * Vector3.Right).Normalized();
        originScreen = _mainCamera.UnprojectPosition(_transformGizmo.GlobalPosition);
        axisScreen = _mainCamera.UnprojectPosition(
            _transformGizmo.GlobalPosition + localAxis * 0.65f);
        direction = (axisScreen - originScreen).Normalized();
        var localPassed = direction.LengthSquared() >= 0.5f
            && _transformGizmo.BeginDrag(_mainCamera, axisScreen);
        if (localPassed)
        {
            CaptureDragStartTransforms();
            amount = _transformGizmo.DragAmount(_mainCamera, axisScreen + direction * 24.0f);
            ApplyGizmoDrag(amount);
            _transformGizmo.EndDrag();
            firstDelta = first.Node.Position - firstStart;
            secondDelta = second.Node.Position - secondStart;
            localPassed = firstDelta.Length() > 0.01f
                && firstDelta.Normalized().Dot(localAxis) > 0.98f
                && firstDelta.IsEqualApprox(secondDelta);
        }
        first.Node.Position = firstStart;
        second.Node.Position = secondStart;
        first.Node.RotationDegrees = firstRotation;
        _dragStartTransforms.Clear();
        SetTransformSpace(WorkspaceTransformSpace.World);
        UpdateTransformGizmo();
        GD.Print($"GROUP_GIZMO_VERIFY move={passed} rotate={rotatePassed} scale={scalePassed} local={localPassed} firstDelta={firstDelta} secondDelta={secondDelta} localAxis={localAxis}");
        return passed && rotatePassed && scalePassed && localPassed;
    }

    private bool VerifyCustomGroupPivot(string firstId, string secondId)
    {
        if (_transformGizmo is null || _mainCamera is null
            || !_workspaceNodes.TryGetValue(firstId, out var first)
            || !_workspaceNodes.TryGetValue(secondId, out var second)) return false;
        SetSelection([firstId]);
        var firstStart = first.Node.Position;
        var secondStart = second.Node.Position;
        var firstRotation = first.Node.RotationDegrees;
        var secondRotation = second.Node.RotationDegrees;
        var visualPivotPassed = _transformGizmo.GlobalPosition.IsEqualApprox(first.Node.GlobalPosition);
        SetTransformMode(WorkspaceTransformMode.Rotate);
        UpdateTransformGizmo();
        var pointer = _mainCamera.UnprojectPosition(_transformGizmo.GlobalPosition + Vector3.Right * 0.85f);
        var rotated = _transformGizmo.BeginDrag(_mainCamera, pointer);
        if (rotated)
        {
            CaptureDragStartTransforms();
            var amount = _transformGizmo.DragAmount(_mainCamera, pointer + new Vector2(0, 28));
            ApplyGizmoDrag(amount);
            _transformGizmo.EndDrag();
            rotated = MathF.Abs(amount) > 0.1f
                && first.Node.Position.IsEqualApprox(firstStart)
                && !second.Node.Position.IsEqualApprox(secondStart);
        }
        first.Node.Position = firstStart;
        second.Node.Position = secondStart;
        first.Node.RotationDegrees = firstRotation;
        second.Node.RotationDegrees = secondRotation;
        _dragStartTransforms.Clear();
        SetTransformMode(WorkspaceTransformMode.Move);
        UpdateTransformGizmo();
        return visualPivotPassed && rotated;
    }

    private bool VerifyArrangeSelection(string firstId, string secondId, AssetDefinition thirdAsset)
    {
        var snapshot = CreateWorkspaceDocument();
        var undoCount = _undoHistory.Count;
        _workspaceNodes[firstId].Node.Position = new Vector3(0, 1, 0);
        _workspaceNodes[secondId].Node.Position = new Vector3(4, 3, 2);
        SetSelection([firstId, secondId]);
        ArrangeSelection(WorkspaceArrangeOperation.AlignY);
        var alignPassed = MathF.Abs(_workspaceNodes[firstId].Node.Position.Y - 3.0f) < 0.001f
            && MathF.Abs(_workspaceNodes[firstId].Node.Position.X) < 0.001f;

        PlaceAsset(thirdAsset, recordUndo: false);
        var thirdId = _workspaceNodes.Keys.First(id => id != firstId && id != secondId);
        _workspaceNodes[firstId].Node.Position = new Vector3(0, 0, 0);
        _workspaceNodes[secondId].Node.Position = new Vector3(17, 0, 0);
        _workspaceNodes[thirdId].Node.Position = new Vector3(20, 0, 0);
        SetSelection([firstId, secondId, thirdId]);
        ArrangeSelection(WorkspaceArrangeOperation.DistributeX);
        var distribution = _workspaceNodes.Values.Select(item => item.Node.Position.X)
            .OrderBy(value => value).ToArray();
        var distributePassed = distribution.Length == 3
            && MathF.Abs(distribution[0]) < 0.001f
            && MathF.Abs(distribution[1] - 10.0f) < 0.001f
            && MathF.Abs(distribution[2] - 20.0f) < 0.001f;
        ApplyWorkspaceSnapshot(snapshot);
        while (_undoHistory.Count > undoCount) _undoHistory.RemoveAt(_undoHistory.Count - 1);
        return alignPassed && distributePassed;
    }

    private static Vector3 Snap(Vector3 value, float increment) => new(
        MathF.Round(value.X / increment) * increment,
        MathF.Round(value.Y / increment) * increment,
        MathF.Round(value.Z / increment) * increment
    );

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    private void DeletePlacement(string instanceId)
    {
        if (!_workspaceNodes.TryGetValue(instanceId, out var placement)) return;
        var retiringDrive = _workspaceDriveRelation?.InstanceId == instanceId;
        RecordUndo();
        if (retiringDrive) StopActiveController();
        placement.Node.GetParent()?.RemoveChild(placement.Node);
        placement.Node.QueueFree();
        _workspaceNodes.Remove(instanceId);
        _connectorLinks.RemoveAll(link => link.FromInstanceId == instanceId || link.ToInstanceId == instanceId);
        _signalLinks.RemoveAll(link => link.InstanceId == instanceId);
        RemoveGroupMembers([instanceId]);
        _selectedPlacementIds.Remove(instanceId);
        _selectedPlacementId = _selectedPlacementIds.LastOrDefault();
        if (retiringDrive) RebuildTemplateWorkspace(CreateWorkspaceDocument() with { Version = 5, DriveConnection = null });
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus($"Deleted placement · {_workspaceNodes.Count} remain");
        RefreshWorkspaceDirty();
        GD.Print($"ASSET_PLACEMENT_DELETED {instanceId}");
    }

    private void DeleteSelection()
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        var selected = _selectedPlacementIds.Where(_workspaceNodes.ContainsKey).ToArray();
        if (selected.Length == 0) return;
        RecordUndo();
        var selectedSet = selected.ToHashSet(StringComparer.Ordinal);
        var retiringDrive = _workspaceDriveRelation is { } drive && selectedSet.Contains(drive.InstanceId);
        if (retiringDrive) StopActiveController();
        foreach (var id in selected)
        {
            var placement = _workspaceNodes[id];
            placement.Node.GetParent()?.RemoveChild(placement.Node);
            placement.Node.QueueFree();
            _workspaceNodes.Remove(id);
        }
        _connectorLinks.RemoveAll(link => selectedSet.Contains(link.FromInstanceId)
            || selectedSet.Contains(link.ToInstanceId));
        _signalLinks.RemoveAll(link => selectedSet.Contains(link.InstanceId));
        RemoveGroupMembers(selectedSet);
        _selectedPlacementIds.Clear();
        _selectedPlacementId = null;
        if (retiringDrive) RebuildTemplateWorkspace(CreateWorkspaceDocument() with { Version = 5, DriveConnection = null });
        UpdateTransformGizmo();
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus($"Deleted {selected.Length} selected placements · {_workspaceNodes.Count} remain");
        RefreshWorkspaceDirty();
    }

    private void RemoveGroupMembers(IEnumerable<string> removedIds)
    {
        var removed = removedIds.ToHashSet(StringComparer.Ordinal);
        for (var index = _workspaceGroups.Count - 1; index >= 0; index--)
        {
            var group = _workspaceGroups[index];
            var remaining = group.MemberInstanceIds.Where(member => !removed.Contains(member)).ToArray();
            if (remaining.Length < 2) _workspaceGroups.RemoveAt(index);
            else if (remaining.Length != group.MemberInstanceIds.Count)
                _workspaceGroups[index] = group with { MemberInstanceIds = remaining };
        }
    }

    private void RecordUndo()
    {
        RetirePausedWorkspaceForEdit();
        if (_currentSceneId is null) return;
        _undoHistory.Add(CreateWorkspaceDocument());
        if (_undoHistory.Count > 100) _undoHistory.RemoveAt(0);
        _redoHistory.Clear();
    }

    private void UndoWorkspace()
    {
        if (_undoHistory.Count == 0) return;
        _redoHistory.Add(CreateWorkspaceDocument());
        var snapshot = _undoHistory[^1];
        _undoHistory.RemoveAt(_undoHistory.Count - 1);
        ApplyWorkspaceSnapshot(snapshot);
        _simulatorShell?.SetWorkspaceStatus("Undo applied");
        RefreshWorkspaceDirty();
    }

    private void RedoWorkspace()
    {
        if (_redoHistory.Count == 0) return;
        _undoHistory.Add(CreateWorkspaceDocument());
        var snapshot = _redoHistory[^1];
        _redoHistory.RemoveAt(_redoHistory.Count - 1);
        ApplyWorkspaceSnapshot(snapshot);
        _simulatorShell?.SetWorkspaceStatus("Redo applied");
        RefreshWorkspaceDirty();
    }

    private void ApplyWorkspaceSnapshot(WorkspaceDocument snapshot)
    {
        RetirePausedWorkspaceForEdit();
        if(snapshot.FiniteBeltPreset is not null || _workspaceFiniteBeltDocument is not null
            || snapshot.FiniteRollerPreset is not null || _workspaceFiniteRollerDocument is not null
            || snapshot.DriveConnection is not null || _workspaceDriveRelation is not null)
        { WorkspaceRepository.ValidateDocument(snapshot,_candidateCatalog!,_sceneCatalog!);RebuildTemplateWorkspace(snapshot);return; }
        if (snapshot.SourceSceneId != _currentSceneId)
            throw new InvalidOperationException("Undo snapshot belongs to another scene.");
        if(JsonSerializer.Serialize(snapshot.TemplateOverride)!=JsonSerializer.Serialize(_templateOverride))
        { RebuildTemplateWorkspace(snapshot);return; }
        foreach (var placement in _workspaceNodes.Values)
        {
            placement.Node.GetParent()?.RemoveChild(placement.Node);
            placement.Node.QueueFree();
        }
        _workspaceNodes.Clear();
        _connectorLinks.Clear();
        _signalLinks.Clear();
        _workspaceGroups.Clear();
        _selectedPlacementId = null;
        _selectedPlacementIds.Clear();
        foreach (var placement in snapshot.Placements)
        {
            var asset = _candidateCatalog?.Assets.FirstOrDefault(item => item.Id == placement.AssetId)
                ?? throw new InvalidOperationException($"Undo asset '{placement.AssetId}' is unavailable.");
            PlaceAsset(asset, placement, recordUndo: false);
        }
        _connectorLinks.AddRange(snapshot.ConnectorLinks ?? []);
        _signalLinks.AddRange(snapshot.SignalLinks ?? []);
        _workspaceGroups.AddRange(snapshot.Groups ?? []);
        UpdateTransformGizmo();
        RefreshWorkspaceUi();
    }

    private void RefreshWorkspaceUi()
    {
        if (_simulatorShell is null || _currentSceneId is null) return;
        _simulatorShell.UpdateWorkspace(
            CreateWorkspaceDocument().Placements.Concat(TemplateWorkspaceRows()).ToArray(),
            _selectedPlacementId,
            _undoHistory.Count > 0,
            _redoHistory.Count > 0,
            _connectorLinks,
            _signalLinks,
            ApplySignalMappings(),
            _selectedPlacementIds,
            _workspaceGroups
        );
        _simulatorShell.SetWorkspaceDriveConnectionAvailability(CanConnectWorkspaceDrive(_selectedPlacementId), _workspaceDriveRelation is not null);
        RefreshConnectionVisuals();
    }

    private void ApplyWorkspaceSignalMappings()
    {
        ApplySignalMappings();
    }

    private IReadOnlyDictionary<string, SignalMappingRuntimeStatus> ApplySignalMappings()
    {
        var statuses = new Dictionary<string, SignalMappingRuntimeStatus>(StringComparer.Ordinal);
        ReconcileWorkspaceDriveMappings();
        ClearFormerGenericLiveInputs();
        if (_sceneRuntime is null || _candidateCatalog is null) return statuses;

        foreach (var link in _signalLinks)
        {
            if (!_workspaceNodes.TryGetValue(link.InstanceId, out var placement)
                || _candidateCatalog.Assets.FirstOrDefault(item => item.Id == placement.AssetId) is not { } asset
                || asset.Signals.FirstOrDefault(item => item.Id == link.SignalId) is not { } signal
                || !_sceneRuntime.Points.TryGetValue(link.PointName, out var pointValue))
            {
                statuses[link.Id] = new SignalMappingRuntimeStatus(
                    SignalMappingRuntimeState.AuthoredOnly,
                    "AUTHORED ONLY",
                    "The saved mapping references an unavailable runtime endpoint."
                );
                continue;
            }

            var boundedStatus = WorkspaceDriveMappingStatus(link.InstanceId,link.SignalId)
                ?? FiniteBeltMappingStatus(link.InstanceId,link.SignalId)
                ?? FiniteRollerMappingStatus(link.InstanceId,link.SignalId);
            statuses[link.Id] = boundedStatus ?? WorkspaceSignalRuntimeAdapter.Apply(
                asset,
                placement.Node,
                signal,
                pointValue
            );
            if (boundedStatus is null) OwnGenericLiveInput(placement.Node,link.SignalId,statuses[link.Id]);
        }
        return statuses;
    }

    private void RefreshConnectionVisuals()
    {
        if (_connectionVisuals is not null)
        {
            _connectionVisuals.GetParent()?.RemoveChild(_connectionVisuals);
            _connectionVisuals.QueueFree();
        }
        _connectionVisuals = new Node3D { Name = "WorkspaceConnectionVisuals" };
        _sceneCompositionRoot?.AddChild(_connectionVisuals);
        foreach (var link in _connectorLinks)
        {
            if (!_workspaceNodes.TryGetValue(link.FromInstanceId, out var fromPlacement)
                || !_workspaceNodes.TryGetValue(link.ToInstanceId, out var toPlacement)
                || !TryResolveConnector(link.FromInstanceId, link.FromConnectorId, out var from)
                || !TryResolveConnector(link.ToInstanceId, link.ToConnectorId, out var to)) continue;
            var start = fromPlacement.Node.Transform * ConnectorPosition(from);
            var end = toPlacement.Node.Transform * ConnectorPosition(to);
            var color = ConnectorColor(from.Kind, to.Kind);
            var material = new StandardMaterial3D
            {
                AlbedoColor = color,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                NoDepthTest = true,
                EmissionEnabled = true,
                Emission = color,
                EmissionEnergyMultiplier = 2.0f,
            };
            var lines = new ImmediateMesh();
            lines.SurfaceBegin(Mesh.PrimitiveType.LineStrip, material);
            var midpointX = (start.X + end.X) * 0.5f;
            lines.SurfaceAddVertex(start);
            lines.SurfaceAddVertex(new Vector3(midpointX, start.Y, start.Z));
            lines.SurfaceAddVertex(new Vector3(midpointX, end.Y, end.Z));
            lines.SurfaceAddVertex(end);
            lines.SurfaceEnd();
            _connectionVisuals.AddChild(new MeshInstance3D
            {
                Name = $"Link_{SafeNodeName(link.Id)}",
                Mesh = lines,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
            AddConnectorMarker(_connectionVisuals, start, color, $"From_{SafeNodeName(link.Id)}");
            AddConnectorMarker(_connectionVisuals, end, color, $"To_{SafeNodeName(link.Id)}");
        }
    }

    private static Vector3 ConnectorPosition(AssetConnector connector) => new(
        (float)connector.PositionM[0],
        (float)connector.PositionM[2],
        -(float)connector.PositionM[1]
    );

    private static Color ConnectorColor(string fromKind, string toKind)
    {
        var combined = $"{fromKind} {toKind}";
        if (combined.Contains("electrical", StringComparison.OrdinalIgnoreCase)) return new Color("f3c64f");
        if (combined.Contains("process", StringComparison.OrdinalIgnoreCase)) return new Color("4aa5ff");
        if (combined.Contains("pneumatic", StringComparison.OrdinalIgnoreCase)) return new Color("b48cff");
        return new Color("42d3ff");
    }

    private static void AddConnectorMarker(Node3D parent, Vector3 position, Color color, string name)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            EmissionEnabled = true,
            Emission = color,
            EmissionEnergyMultiplier = 2.2f,
        };
        parent.AddChild(new MeshInstance3D
        {
            Name = name,
            Position = position,
            Mesh = new SphereMesh { Radius = 0.065f, Height = 0.13f, Material = material },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
    }

    private void CreateConnectorLink(
        string fromInstance,
        string fromConnectorId,
        string toInstance,
        string toConnectorId
    )
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        if (!TryResolveConnector(fromInstance, fromConnectorId, out var from)
            || !TryResolveConnector(toInstance, toConnectorId, out var to))
        {
            _simulatorShell?.SetWorkspaceStatus("Connection rejected · endpoint is unavailable", isError: true);
            return;
        }
        if (fromInstance == toInstance && fromConnectorId == toConnectorId)
        {
            _simulatorShell?.SetWorkspaceStatus("Connection rejected · endpoint cannot connect to itself", isError: true);
            return;
        }
        if (!from.CompatibleKinds.Contains(to.Kind, StringComparer.Ordinal)
            && !to.CompatibleKinds.Contains(from.Kind, StringComparer.Ordinal))
        {
            _simulatorShell?.SetWorkspaceStatus($"Connection rejected · {from.Kind} is incompatible with {to.Kind}", isError: true);
            return;
        }
        var duplicate = _connectorLinks.Any(link =>
            (link.FromInstanceId == fromInstance && link.FromConnectorId == fromConnectorId
             && link.ToInstanceId == toInstance && link.ToConnectorId == toConnectorId)
            || (link.FromInstanceId == toInstance && link.FromConnectorId == toConnectorId
                && link.ToInstanceId == fromInstance && link.ToConnectorId == fromConnectorId));
        if (duplicate)
        {
            _simulatorShell?.SetWorkspaceStatus("Connection rejected · duplicate link", isError: true);
            return;
        }
        RecordUndo();
        _connectorLinks.Add(new WorkspaceConnectorLink(
            NextId("connector-link", _connectorLinks.Select(link => link.Id)),
            fromInstance,
            fromConnectorId,
            toInstance,
            toConnectorId
        ));
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus($"Connector link created · {from.Kind} ↔ {to.Kind}");
        RefreshWorkspaceDirty();
    }

    private void CreateSignalLink(string instanceId, string signalId, string pointName)
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        if (!_workspaceNodes.TryGetValue(instanceId, out var placement)
            || _candidateCatalog?.Assets.FirstOrDefault(item => item.Id == placement.AssetId) is not { } asset
            || asset.Signals.FirstOrDefault(item => item.Id == signalId) is not { } signal)
        {
            _simulatorShell?.SetWorkspaceStatus("Signal mapping rejected · asset signal is unavailable", isError: true);
            return;
        }
        var pointType = ScenePointType(pointName);
        if (pointType is null || !TypesCompatible(signal.DataType, pointType))
        {
            _simulatorShell?.SetWorkspaceStatus($"Signal mapping rejected · {signal.DataType} is incompatible with {pointType ?? "missing point"}", isError: true);
            return;
        }
        if (_signalLinks.Any(link => link.InstanceId == instanceId && link.SignalId == signalId))
        {
            _simulatorShell?.SetWorkspaceStatus("Signal mapping rejected · asset signal is already mapped", isError: true);
            return;
        }
        RecordUndo();
        _signalLinks.Add(new WorkspaceSignalLink(
            NextId("signal-link", _signalLinks.Select(link => link.Id)),
            instanceId,
            signalId,
            pointName
        ));
        ApplySignalMappings();
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus($"Signal mapped · {signalId} ↔ {pointName}");
        RefreshWorkspaceDirty();
    }

    private void DeleteConnectorLink(string id)
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        if (!_connectorLinks.Any(link => link.Id == id)) return;
        RecordUndo();
        _connectorLinks.RemoveAll(link => link.Id == id);
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus("Connector link deleted");
        RefreshWorkspaceDirty();
    }

    private void DeleteSignalLink(string id)
    {
        if(RejectBoundedWorkspaceGraphEdit())return;
        if (!_signalLinks.Any(link => link.Id == id)) return;
        if (_workspaceDriveRelation is { } drive && _signalLinks.Any(link => link.Id == id && link.InstanceId == drive.InstanceId && link.SignalId == "run_command")
            && !ScanInspectionCurrentDraftMatches())
        { StopActiveController(); _virtualController?.ClearHistory(); _scanInspectionEpoch++; }
        RecordUndo();
        _signalLinks.RemoveAll(link => link.Id == id);
        RefreshWorkspaceUi();
        _simulatorShell?.SetWorkspaceStatus("Signal mapping deleted");
        RefreshWorkspaceDirty();
    }

    private bool TryResolveConnector(string instanceId, string connectorId, out AssetConnector connector)
    {
        connector = null!;
        if (!_workspaceNodes.TryGetValue(instanceId, out var placement)) return false;
        var asset = _candidateCatalog?.Assets.FirstOrDefault(item => item.Id == placement.AssetId);
        connector = asset?.Connectors.FirstOrDefault(item => item.Id == connectorId)!;
        return connector is not null;
    }

    private string? ScenePointType(string pointName)
    {
        if (_activeSceneDefinition?.Simulation.ValueKind != System.Text.Json.JsonValueKind.Object
            || !_activeSceneDefinition.Simulation.TryGetProperty("points", out var points)
            || points.ValueKind != System.Text.Json.JsonValueKind.Array) return null;
        foreach (var point in points.EnumerateArray())
        {
            if (point.TryGetProperty("name", out var name)
                && name.GetString() == pointName
                && point.TryGetProperty("type", out var type)) return type.GetString();
        }
        return null;
    }

    private static bool TypesCompatible(string signalType, string pointType) =>
        (signalType.ToLowerInvariant(), pointType.ToUpperInvariant()) switch
        {
            ("bool", "BOOL") => true,
            ("float32", "REAL") => true,
            ("int32", "DINT") => true,
            ("int32", "INT") => true,
            _ => false,
        };

    private static string NextId(string prefix, IEnumerable<string> existing)
    {
        var used = existing.ToHashSet(StringComparer.Ordinal);
        for (var index = 1; ; index++)
        {
            var candidate = $"{prefix}-{index:000}";
            if (!used.Contains(candidate)) return candidate;
        }
    }

    private Vector3 FindOpenPlacementPosition(AssetDefinition asset)
    {
        if (_sceneCompositionRoot is null) return Vector3.Zero;
        var points = new List<Vector3>();
        foreach (Node child in _sceneCompositionRoot.GetChildren())
        {
            if (child.Name.ToString().StartsWith("UserPlacement_", StringComparison.Ordinal)) continue;
            foreach (var node in child.FindChildren("*", "MeshInstance3D", true, false))
            {
                if (node is not MeshInstance3D mesh) continue;
                var bounds = mesh.GetAabb();
                for (var mask = 0; mask < 8; mask++)
                {
                    var corner = bounds.Position + new Vector3(
                        (mask & 1) == 0 ? 0 : bounds.Size.X,
                        (mask & 2) == 0 ? 0 : bounds.Size.Y,
                        (mask & 4) == 0 ? 0 : bounds.Size.Z
                    );
                    points.Add(mesh.GlobalTransform * corner);
                }
            }
        }
        if (points.Count == 0) return Vector3.Zero;
        var minimum = points[0];
        var maximum = points[0];
        foreach (var point in points)
        {
            minimum = new Vector3(MathF.Min(minimum.X, point.X), MathF.Min(minimum.Y, point.Y), MathF.Min(minimum.Z, point.Z));
            maximum = new Vector3(MathF.Max(maximum.X, point.X), MathF.Max(maximum.Y, point.Y), MathF.Max(maximum.Z, point.Z));
        }
        var column = (_placedAssetCount - 1) % 4;
        var row = (_placedAssetCount - 1) / 4;
        var width = Math.Max((float)asset.Bounds.WidthM, 0.5f);
        var depth = Math.Max((float)asset.Bounds.DepthM, 0.5f);
        return new Vector3(
            minimum.X + width * 0.5f + column * (width + 0.75f),
            0.0f,
            minimum.Z - depth * 0.5f - 1.0f - row * (depth + 0.75f)
        );
    }

    private void SaveWorkspace()
        => SaveWorkspaceToPath(WorkspaceRepository.LastWorkspacePath);

    private void SaveWorkspaceToPath(string path)
    {
        if (_simulatorShell is null) { SaveResolvedWorkspaceToPath(path); return; }
        _simulatorShell.ResolveIntegratedProjectSave(() => SaveResolvedWorkspaceToPath(path), CancelPendingWorkspaceAction);
    }

    private void SaveResolvedWorkspaceToPath(string path)
    {
        if (_currentSceneId is null)
        {
            const string message = "Save failed: no active scene is loaded.";
            _simulatorShell?.SetWorkspaceStatus(message, isError: true);
            _simulatorShell?.ShowWorkspaceFeedback(message, isError: true);
            return;
        }
        try
        {
            var bundle=new WorkspaceProjectBundle(1,WorkspaceProjectBundleRepository.Kind,
                CreateWorkspaceDocument(),_simulatorShell!.CaptureIntegratedLadderDraft());
            var savedPath = WorkspaceProjectBundleRepository.Save(bundle,path,_candidateCatalog!,_sceneCatalog!);
            _simulatorShell.MarkIntegratedLadderDraftSaved();
            _simulatorShell.SetWorkspaceStatus($"Saved integrated project · scene + {_workspaceNodes.Count} placements + actual ladder draft · {savedPath}");
            MarkWorkspaceSaved();
            GD.Print($"WORKSPACE_SAVED {savedPath} placements={_workspaceNodes.Count}");
        }
        catch (Exception exception)
        {
            var message = $"Save failed: {exception.Message}";
            _simulatorShell?.SetWorkspaceStatus(message, isError: true);
            if (_pendingWorkspaceAction is not null)
                _simulatorShell?.ShowUnsavedWorkspaceDialog(message + "\n\n");
            else _simulatorShell?.ShowWorkspaceFeedback(message, isError: true);
            GD.PushError($"WORKSPACE_SAVE_FAILED {exception.Message}");
            return;
        }
        CompletePendingWorkspaceAction();
    }

    private WorkspaceDocument CreateWorkspaceDocument()
    {
        if (_currentSceneId is null) throw new InvalidOperationException("No active scene.");
        if(_workspaceFiniteBeltDocument is not null)return _workspaceFiniteBeltDocument;
        if(_workspaceFiniteRollerDocument is not null)return _workspaceFiniteRollerDocument;
        var placements = _workspaceNodes.Select(item => new WorkspacePlacement(
            item.Key,
            item.Value.AssetId,
            Array(item.Value.Node.Position),
            Array(item.Value.Node.RotationDegrees),
            Array(item.Value.Node.Scale)
        )).ToArray();
        return new WorkspaceDocument(_workspaceDriveRelation is not null?9:_templateOverride is null?5:6, _currentSceneId, placements,
            _connectorLinks.ToArray(), _signalLinks.ToArray(), _workspaceGroups.ToArray(),_templateOverride,
            DriveConnection:_workspaceDriveRelation);
    }

    private void RefreshWorkspaceDirty()
    {
        if (_currentSceneId is null) return;
        _simulatorShell?.SetWorkspaceDirty(!string.Equals(_workspaceSavedSnapshotJson,
            JsonSerializer.Serialize(CreateWorkspaceDocument()), StringComparison.Ordinal)
            ||_workspaceSavedLadderJson is not null && _workspaceSavedLadderJson!=_simulatorShell?.CaptureIntegratedLadderDraft());
    }

    private void MarkWorkspaceSaved()
    {
        _workspaceSavedSnapshotJson = JsonSerializer.Serialize(CreateWorkspaceDocument());
        _workspaceSavedLadderJson=_simulatorShell?.CaptureIntegratedLadderDraft();
        RefreshWorkspaceDirty();
    }

    private void LoadWorkspace()
        => LoadWorkspaceFromPath(WorkspaceRepository.LastWorkspacePath);

    private void LoadWorkspaceFromPath(string path)
    {
        if (_simulatorShell is null)
        {
            LoadWorkspaceFromPathCore(path);
            return;
        }
        _simulatorShell.ResolveIntegratedProjectReplacement(() => LoadWorkspaceFromPathCore(path));
    }

    private void LoadWorkspaceFromPathCore(string path)
    {
        if (_candidateCatalog is null || _sceneCatalog is null || _mainCamera is null)
        {
            const string message = "Load failed: project catalogs are unavailable.";
            _simulatorShell?.SetWorkspaceStatus(message, isError: true);
            _simulatorShell?.ShowWorkspaceFeedback(message, isError: true);
            return;
        }
        try
        {
            // Validate both document graphs and source identity before any scene
            // or editor replacement. Legacy standalone workspace files remain readable.
            var bundle=WorkspaceProjectBundleRepository.LoadIfBundle(path,_candidateCatalog,_sceneCatalog);
            var document = bundle?.Workspace??WorkspaceRepository.Load(_candidateCatalog, _sceneCatalog, path);
            if (!GuardWorkspaceReplacement(() => LoadWorkspaceFromPathCore(path))) return;
            var rollback=CaptureWorkspaceReplacementRollback();
            try
            {
            AddMigratedScene(document.SourceSceneId, _candidateCatalog, _mainCamera,
                showRuntimeControls: false, autoRun: false,templateOverride:document.TemplateOverride,
                finiteBeltWorkspace:document.FiniteBeltPreset is null?null:document,
                finiteRollerWorkspace:document.FiniteRollerPreset is null?null:document,
                driveWorkspace:document.DriveConnection is null?null:document);
            if(_injectWorkspaceReplacementFailure=="after-compose")
            { _injectWorkspaceReplacementFailure=null;throw new InvalidOperationException("Injected native replacement failure after actual scene compose."); }
            foreach (var placement in document.Placements)
            {
                if(document.FiniteBeltPreset is not null || document.FiniteRollerPreset is not null)continue;
                var asset = _candidateCatalog.Assets.First(item => item.Id == placement.AssetId);
                PlaceAsset(asset, placement, recordUndo: false);
            }
            _connectorLinks.AddRange(document.ConnectorLinks ?? []);
            if(document.FiniteBeltPreset is null && document.FiniteRollerPreset is null)_signalLinks.AddRange(document.SignalLinks ?? []);
            _workspaceGroups.AddRange(document.Groups ?? []);
            if(document.DriveConnection is not null)RegisterWorkspaceDrive(document);
            _undoHistory.Clear();
            _redoHistory.Clear();
            DisableVirtualController();
            if(bundle is not null)_simulatorShell!.RestoreIntegratedLadderDraft(bundle.LadderProjectJson);
            if(_injectWorkspaceReplacementFailure=="after-draft")
            { _injectWorkspaceReplacementFailure=null;throw new InvalidOperationException("Injected native replacement failure after actual draft application."); }
            _simulatorShell!.SetIntegratedSceneIdentity();
            // Joint Open has composed a new measured-home scene and restored its exact
            // authored layout/draft. Stage its epoch after the loader's final detach,
            // which intentionally retires ordinary controller replacements.
            ResetRepeatCycleTrial();
            RefreshWorkspaceUi();
            var message = $"Loaded {document.Placements.Count} placements from {System.IO.Path.GetFileName(path)}.\n\nScene: {document.SourceSceneId}\n"+
                (bundle is null?"LEGACY WORKSPACE · no ladder draft contained":"INTEGRATED PROJECT · saved ladder draft restored · stopped");
            _simulatorShell?.SetWorkspaceStatus(message.Replace("\n\n", " · "), isError: false);
            MarkWorkspaceSaved();
            _simulatorShell?.ShowWorkspaceFeedback(message);
            GD.Print($"WORKSPACE_LOADED scene={document.SourceSceneId} placements={document.Placements.Count}");
            }
            catch
            {
                rollback();
                throw;
            }
        }
        catch (Exception exception)
        {
            var message = $"Load failed: {exception.Message}\n\nFor a Ladder agent project, use File → Open Ladder Agent Project….\nFor a scene workspace, use File → Save Workspace As… first.";
            _simulatorShell?.SetWorkspaceStatus(message.Replace("\n\n", " · "), isError: true);
            _simulatorShell?.ShowWorkspaceFeedback(message, isError: true);
            GD.PushWarning($"WORKSPACE_LOAD_FAILED {exception.Message}");
        }
    }

    private void AddMigratedScene(
        string sceneId,
        AssetCatalogDocument candidates,
        Camera3D camera,
        bool showRuntimeControls = true,
        bool autoRun = true,
        WorkspaceTemplateOverride? templateOverride = null,
        WorkspaceDocument? finiteBeltWorkspace = null,
        WorkspaceDocument? finiteRollerWorkspace = null,
        WorkspaceDocument? driveWorkspace = null
    )
    {
        if (!GuardChallengeSceneReplacement(() => AddMigratedScene(sceneId, candidates, camera, showRuntimeControls, autoRun,templateOverride,finiteBeltWorkspace,finiteRollerWorkspace,driveWorkspace))) return;
        ClearChallengeLabels();
        ReleaseGantryReviewClock();
        if (_externalConnection is not null && (_externalConnection.State != ConnectionState.Disconnected || _externalConnection.IsBusy))
            _externalConnection.Disconnect();
        if (_virtualController is not null) DisableVirtualController();
        CloseAssetPreview();
        var catalog = SceneCatalogLoader.LoadCatalog(
            "res://scenes/catalog/original-scenes.catalog.json"
        );
        var entry = catalog.Scenes.FirstOrDefault(scene => scene.Id == sceneId)
            ?? throw new InvalidOperationException($"Unknown migrated scene '{sceneId}'.");
        var scene = SceneCatalogLoader.LoadScene(entry);
        if(templateOverride is not null)WorkspaceTemplatePolicy.Validate(scene,templateOverride);
        scene=scene with { TemplateOverride=templateOverride };
        if(finiteBeltWorkspace is not null)scene=WorkspaceFiniteBeltPolicy.ReplaceSource(scene,finiteBeltWorkspace);
        if(finiteRollerWorkspace is not null)scene=WorkspaceFiniteRollerPolicy.ReplaceSource(scene,finiteRollerWorkspace);
        if(driveWorkspace is not null)scene=WorkspaceDriveConnectionPolicy.AugmentSource(scene,driveWorkspace,candidates);
        _templateOverride=templateOverride;
        // Reject malformed typed scene contracts before composing Godot nodes.
        SceneAggregateContract.Validate(scene.Simulation);
        _activeSceneDefinition = scene;
        ClearWorkspaceDriveRegistration();
        _frameWholeScene = false;
        var composition = SceneComposer.Compose(
            scene,
            candidates,
            false
        );
        _workspaceFiniteBeltDocument=null;
        _workspaceFiniteRollerDocument=null;
        _workspaceNodes.Clear();
        _connectorLinks.Clear();
        _signalLinks.Clear();
        _workspaceGroups.Clear();
        _selectedPlacementId = null;
        _selectedPlacementIds.Clear();
        _undoHistory.Clear();
        _redoHistory.Clear();
        _transformGizmo?.Detach();
        if (_sceneRuntime is not null)
        {
            _sceneRuntime.StateChanged -= ApplyWorkspaceSignalMappings;
            RemoveChild(_sceneRuntime);
            _sceneRuntime.QueueFree();
            _sceneRuntime = null;
        }
        _sceneControlInteractor = null;
        RemoveDemoEnvironment();
        if (_sceneCompositionRoot is not null)
        {
            RemoveChild(_sceneCompositionRoot);
            _sceneCompositionRoot.QueueFree();
            _sceneCompositionRoot = null;
        }
        AddChild(composition.Root);
        _sceneRuntime = new SceneSimulationRuntime(scene.Simulation, composition.Root);
        _sceneRuntime.UsesExternalClock = SceneRequiresControllerClock;
        _sceneControlInteractor = new SceneControlInteractor(scene, composition.Root);
        _sceneRuntime.StateChanged += ApplyWorkspaceSignalMappings;
        _sceneRuntime.StateChanged += UpdateGantryReviewClockLabel;
        AddChild(_sceneRuntime);
        _sceneRuntime.SetControllerPlaybackRunning(false);
        SynchronizeExternalPlayback();
        if (_verifySceneContract)
        {
            var passed = _sceneRuntime.VerifyDeclaredCases(scene.Verification);
            GetTree().Quit(passed ? 0 : 1);
        }
        else if (autoRun
            && _simulatorShell?.IsExternalMode != true
            && !_captureState.Equals("stopped", StringComparison.OrdinalIgnoreCase))
        {
            if (_sceneAction is null)
            {
                _sceneRuntime.RunDefault();
            }
            else
            {
                foreach (var actionId in _sceneAction.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!_sceneRuntime.ExecuteAction(actionId))
                    {
                        throw new InvalidOperationException($"Scene action '{actionId}' could not execute.");
                    }
                }
            }
        }
        if (showRuntimeControls && _capturePath is null && !_verifySceneContract)
        {
            AddRuntimeControls(scene, _sceneRuntime);
        }
        var sourceCameraPosition = new Vector3(
            (float)scene.Camera.Position[0],
            (float)scene.Camera.Position[1],
            (float)scene.Camera.Position[2]
        );
        var authoredTarget = new Vector3(
            (float)scene.Camera.Target[0],
            (float)scene.Camera.Target[1],
            (float)scene.Camera.Target[2]
        );
        camera.Fov = (float)scene.Camera.Fov;
        FrameComposition(camera, composition.Root, sourceCameraPosition - authoredTarget);
        _sceneCamera = camera;
        _sceneCompositionRoot = composition.Root;
        _sceneCameraDirection = sourceCameraPosition - authoredTarget;
        AttachDemoEnvironment(scene.Id, composition.Root, _sceneCameraDirection);
        _placedAssetCount = 0;
        _currentSceneId = scene.Id;
        if(finiteBeltWorkspace is not null)RegisterFiniteBeltWorkspace(finiteBeltWorkspace);
        if(finiteRollerWorkspace is not null)RegisterFiniteRollerWorkspace(finiteRollerWorkspace);
        if (_visualSceneReview) UpdateVisualReviewLabel();
        _simulatorShell?.AttachScene(scene, _sceneRuntime, composition);
        _simulatorShell?.ResetWorkspaceTrialHint();
        _simulatorShell?.ResetWorkspaceRollerTrialHint();
        ResetRepeatCycleTrial();
        ResetWorkspaceObservedTrial("Scene attached");
        ResetWorkspaceRollerObservedTrial("Scene attached");
        MarkWorkspaceSaved();
        RefreshWorkspaceUi();
        GD.Print($"MIGRATED_SCENE_LOADED {scene.Id} rendered={composition.RenderedEquipmentIds.Count} deferred={composition.DeferredEquipmentIds.Count}");
        if (composition.DeferredEquipmentIds.Count > 0)
        {
            GD.Print($"MIGRATED_SCENE_DEFERRED {string.Join(',', composition.DeferredEquipmentIds)}");
        }
    }

    private void AddRuntimeControls(SceneDefinition scene, SceneSimulationRuntime runtime)
    {
        var layer = new CanvasLayer { Name = "RuntimeControls" };
        var panel = new PanelContainer
        {
            Name = "ControlPanel",
            OffsetLeft = 18,
            OffsetTop = 18,
            OffsetRight = 378,
            OffsetBottom = 230,
        };
        var content = new VBoxContainer();
        content.AddChild(new Label { Text = scene.Name });
        content.AddChild(new Label { Text = $"Runtime: {runtime.RuntimeType}" });
        var status = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        // Continuous pallet feedback must not push Run/Stop/Reset around as
        // numbers change width. This reserves four lines in its preview HUD.
        if (scene.Id == "lab-2-18-pallet-pickup") status.CustomMinimumSize = new Vector2(0, 108);
        content.AddChild(status);

        var controls = new HBoxContainer();
        var run = new Button { Text = "Run" };
        var stop = new Button { Text = "Stop" };
        var reset = new Button { Text = "Reset" };
        run.Pressed += () => runtime.RunDefault();
        stop.Pressed += runtime.StopSimulation;
        reset.Pressed += runtime.ResetSimulation;
        controls.AddChild(run);
        controls.AddChild(stop);
        controls.AddChild(reset);
        content.AddChild(controls);
        if (scene.Id == "lab-2-17-pallet-robot")
            content.AddChild(new Label
            {
                Text = "Reset before repeating or restarting a stopped cycle.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
        panel.AddChild(content);
        layer.AddChild(panel);
        AddChild(layer);

        void RefreshStatus()
        {
            var values = runtime.Points.Take(8)
                .Select(point => $"{point.Key}: {(point.Value is double number ? number.ToString("G6", System.Globalization.CultureInfo.InvariantCulture) : point.Value)}");
            status.Text = string.Join("   |   ", values);
        }
        runtime.StateChanged += RefreshStatus;
        RefreshStatus();
    }

    private void FrameComposition(Camera3D camera, Node3D root, Vector3 authoredDirection, Node3D? companion = null)
    {
        var points = new List<Vector3>();
        IEnumerable<Node> frameMeshes = root.FindChildren("*", "MeshInstance3D", true, false);
        if (_simulatorShell is not null && !_visualSceneReview && !_frameWholeScene
            && companion is null && !ReferenceEquals(root, _assetPreviewRoot)
            && _activeSceneDefinition?.Id == "lab-11-13-xy-palletizing")
        {
            // The demonstration teaches pickup/gantry/placement, not the
            // separated reference robot and accessory samples. Fit the whole
            // active process (including placed cartons), retaining the gantry
            // structure as the travel envelope. Whole-scene framing remains
            // explicit in View and the choice survives aperture resizes.
            var processIds = new HashSet<string>(StringComparer.Ordinal)
            { "training_accessory_4", "training_accessory_6", "training_accessory_8", "box_2" };
            var processMeshes = root.GetChildren().OfType<Node3D>()
                .Where(node => processIds.Contains(node.Name.ToString())
                    || node.Name == "ControlInstallation" || node.HasMeta("installationKind")
                    || node.Name.ToString().StartsWith("PlacedCarton_", StringComparison.Ordinal))
                .SelectMany(node => node.FindChildren("*", "MeshInstance3D", true, false)).ToArray();
            if (processMeshes.Length > 0) frameMeshes = processMeshes;
        }
        foreach (var node in frameMeshes
            .Concat(companion?.FindChildren("*", "MeshInstance3D", true, false) ?? new Godot.Collections.Array<Node>()))
        {
            if (node is not MeshInstance3D mesh || !mesh.IsVisibleInTree())
            {
                continue;
            }
            var bounds = mesh.GetAabb();
            for (var mask = 0; mask < 8; mask++)
            {
                var corner = bounds.Position + new Vector3(
                    (mask & 1) == 0 ? 0 : bounds.Size.X,
                    (mask & 2) == 0 ? 0 : bounds.Size.Y,
                    (mask & 4) == 0 ? 0 : bounds.Size.Z
                );
                points.Add(mesh.GlobalTransform * corner);
            }
        }
        if (points.Count == 0)
        {
            return;
        }

        // Frame the remaining vertical lift travel at load time. A camera fitted
        // only to the lowered deck clips the platform/fixture as it rises. Use a
        // conservative translated envelope without changing any equipment pose.
        if (_currentSceneId == "lab-5-08-drawbridge-control") AddDrawbridgeFrameEnvelope(root, points);
        AddWastewaterReadoutFrameEnvelope(root, authoredDirection, points);
        var currentPoints = points.ToArray();
        var sorterCartonFocus = _visualSceneReview && _currentSceneId == "lab-10-03-vision-package-sorter"
            && _visualReviewFocusId == "box_1" && ReferenceEquals(root, _sceneCompositionRoot?.GetNodeOrNull<Node3D>("box_1"));
        if (sorterCartonFocus && MathF.Abs(authoredDirection.Normalized().Y) > 0.99f)
        {
            // Fit every possible table yaw up front. Following translates this
            // camera without changing zoom; fitting only the home rectangle
            // lets its rotated corner grow behind the review toolbar.
            var cartonCenter = ReviewBounds(root).GetCenter();
            var radius = currentPoints.Max(point => new Vector2(point.X - cartonCenter.X, point.Z - cartonCenter.Z).Length());
            var bottom = currentPoints.Min(point => point.Y);
            var top = currentPoints.Max(point => point.Y);
            foreach (var height in new[] { bottom, top })
            foreach (var xSign in new[] { -1, 1 })
            foreach (var zSign in new[] { -1, 1 })
                points.Add(new Vector3(cartonCenter.X + xSign * radius, height, cartonCenter.Z + zSign * radius));
        }
        foreach (var motion in root.FindChildren("*", "", true, false).OfType<EquipmentMotionController>()
            .Where(item => item.Kind == EquipmentMotionController.MotionKind.ScissorLift))
        {
            var remaining = motion.TravelM * (1.0f - motion.PositionPercent * 0.01f);
            var offset = motion.GetParent<Node3D>().GlobalBasis.Y * remaining;
            if (offset.LengthSquared() > 0.000001f)
                points.AddRange(currentPoints.Select(point => point + offset));
        }

        var minimum = points[0];
        var maximum = points[0];
        foreach (var point in points)
        {
            minimum = new Vector3(MathF.Min(minimum.X, point.X), MathF.Min(minimum.Y, point.Y), MathF.Min(minimum.Z, point.Z));
            maximum = new Vector3(MathF.Max(maximum.X, point.X), MathF.Max(maximum.Y, point.Y), MathF.Max(maximum.Z, point.Z));
        }
        var center = (minimum + maximum) * 0.5f;
        // The ten-zone line is a transport lesson: a higher view from the open
        // room front keeps the belt tops visible behind their actual drives.
        // Fit the real bounds below; do not move equipment to fake visibility.
        var direction = (root.HasNode("MotorConveyorInstallation")
            ? new Vector3(8, 14, 14) : authoredDirection).Normalized();
        var right = direction.Cross(Vector3.Up).Normalized();
        var viewUp = right.Cross(direction).Normalized();
        var halfWidth = 0.0f;
        var halfHeight = 0.0f;
        var halfDepth = 0.0f;
        foreach (var point in points)
        {
            var offset = point - center;
            halfWidth = MathF.Max(halfWidth, MathF.Abs(offset.Dot(right)));
            halfHeight = MathF.Max(halfHeight, MathF.Abs(offset.Dot(viewUp)));
            halfDepth = MathF.Max(halfDepth, MathF.Abs(offset.Dot(direction)));
        }
        var verticalTangent = MathF.Tan(Mathf.DegToRad(camera.Fov * 0.5f));
        var viewport = camera.GetViewport().GetVisibleRect();
        var aperture = ChallengeSceneAperture(_simulatorShell?.SceneViewportRect() ?? viewport);
        if (aperture.Size.X <= 0 || aperture.Size.Y <= 0) aperture = viewport;
        aperture = WastewaterReadoutReviewAperture(root, aperture);
        camera.KeepAspect = Camera3D.KeepAspectEnum.Height;
        var distance = MathF.Max(halfHeight * viewport.Size.Y / (verticalTangent * aperture.Size.Y),
            halfWidth * viewport.Size.Y / (verticalTangent * aperture.Size.X));
        distance = (distance + halfDepth) * 1.16f;
        // Reserve space below the review clock for barrier tips and sensor heads
        // in nearly vertical full-scene views and the rotating sorter focus.
        if (_visualSceneReview && (ReferenceEquals(root, _sceneCompositionRoot) || sorterCartonFocus) && MathF.Abs(direction.Y) > 0.99f)
            distance *= 1.30f;
        camera.Position = center + direction * MathF.Max(distance, 2.5f);
        camera.LookAt(center, Vector3.Up);
        _cameraController?.CaptureCurrentView(center);
        _lastFramedAperture = _simulatorShell?.SceneViewportRect();
    }

    public override void _Process(double delta)
    {
        UpdateTenZoneCaptureCaptionLayout();
        PollChallenge();
        UpdateChallengeLabels();
        FollowVisualReviewCarton();
        _externalConnection?.Poll();
        if (_simulatorShell is not null && _mainCamera is not null && _sceneCompositionRoot is not null)
        {
            var aperture = ChallengeSceneAperture(_simulatorShell.SceneViewportRect());
            if (aperture.Size.X > 0 && aperture.Size.Y > 0 && aperture != _lastFramedAperture)
            {
                // Keep the wastewater diagnostic focus when its wrapped QA
                // controls change the operator aperture during layout.
                if (_visualSceneReview && _currentSceneId == "lab-11-06-wastewater-collection"
                    && _visualReviewFocusId == "training_accessory_7")
                    SetVisualReviewAngle(_sceneCameraDirection, "readout-aperture-resize");
                else FrameComposition(_mainCamera, _assetPreviewRoot ?? _sceneCompositionRoot, _sceneCameraDirection);
            }
        }
        if (_virtualController?.IsPaused != true) _sceneControlInteractor?.AdvanceFeedback(delta);
        if (_virtualController?.IsPaused != true && _sceneControlInteractor is not null && _sceneRuntime is not null)
            _sceneControlInteractor.SynchronizeFeedback(_sceneRuntime.Points);
        // Decorative ventilation is independent of controller scans. Freeze it
        // with playback and the explicit reduced-motion preference.
        _demoEnvironment?.SetAmbientMotionEnabled(!_demoReducedMotion &&
            (_simulatorShell?.IsExternalMode == true ? _externalPlayback.IsRunning :
                _virtualController is not null ? !_virtualController.IsPaused && _virtualController.Snapshot.State == VirtualControllerState.Running : _sceneRuntime?.IsRunning == true));
        if (_captureWitness is not null && _previewSpeedReader is not null)
        {
            _captureWitness.Position += Vector3.Right
                * _previewSpeedReader()
                * (float)delta;
        }

        if (_capturePath is null)
        {
            return;
        }
        if (_userReadoutCapturePending || _repeatLearnerCapturePending || _workspaceDriveCapturePending || _typedLearnerCapturePending || _scanInspectionCapturePending || _tenZoneCapturePending || _templateCapturePending || _deviceTraceCapturePending || _workspaceBeltCapturePending || _workspaceRollerCapturePending || _demo5LearnerCapturePending || _indexedTraceCapturePending || _workspaceRollerGeometryCapturePending) return;
        _captureFramesRemaining--;
        if (_captureFramesRemaining == 100 && _simulatorShell is not null && _shellView.Length > 0)
        {
            _simulatorShell.SetReviewState(_shellView, _shellSearch);
            if (OS.GetCmdlineUserArgs().Contains("--capture-instance-db-definition", StringComparer.Ordinal))
                _simulatorShell.CallDeferred(nameof(SimulatorShell.PrepareInstanceDbDefinitionCapture));
            if (OS.GetCmdlineUserArgs().Contains("--capture-named-watch-table", StringComparer.Ordinal))
                _simulatorShell.CallDeferred(nameof(SimulatorShell.PrepareNamedWatchTableCapture));
            if (OS.GetCmdlineUserArgs().Contains("--capture-global-db-definition", StringComparer.Ordinal))
                _simulatorShell.CallDeferred(nameof(SimulatorShell.PrepareGlobalDbDefinitionCapture));
            GD.Print("RUNTIME_CAPTURE_VIEW " + _shellView);
        }
        if (_captureFramesRemaining == 1 && _sceneCamera is not null && _sceneCompositionRoot is not null)
        {
            if (_workspaceRollerGeometryCapture is not null)
            {
                ApplyWorkspaceRollerGeometryCaptureView();
                return;
            }
            FrameComposition(_sceneCamera, _sceneCompositionRoot, _sceneCameraDirection);
            PrepareReadoutPresentationCapture();
            if (_capturePath is null) return;
            if (OS.GetCmdlineUserArgs().Contains("--capture-user-readout-navigation", StringComparer.Ordinal))
            {
                PrepareUserReadoutCapture();
                return;
            }
            ApplyControlInstallationCaptureView();
            ApplyTypedLearnerDetailCaptureView();
            return;
        }
        if (_captureFramesRemaining > 0) return;

        var capturePath = _capturePath;
        _capturePath = null;
        var image = GetViewport().GetTexture().GetImage();
        if (image is null || image.IsEmpty())
        {
            GD.PushError("Runtime capture requires a rendering display; the headless dummy renderer has no viewport image.");
            GetTree().Quit(2);
            return;
        }

        var error = image.SavePng(capturePath);
        if (error != Error.Ok)
        {
            GD.PushError($"Unable to save runtime capture '{capturePath}': {error}.");
            GetTree().Quit(1);
            return;
        }

        GD.Print($"RUNTIME_CAPTURE_SAVED {capturePath}");
        GD.Print(FormattableString.Invariant($"RUNTIME_CAPTURE_PERFORMANCE fps={Performance.GetMonitor(Performance.Monitor.TimeFps):F1} processMs={Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000:F2} drawCalls={Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame):F0} videoMemoryBytes={Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed):F0} scope=single-capped-capture-not-benchmark"));
        // Capture-only probe: drain unreachable managed resource wrappers while
        // Godot's native bindings still exist, before engine teardown begins.
        // The saved image and the normal application's lifetime are unchanged.
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        GD.Print("RUNTIME_CAPTURE_MANAGED_FINALIZERS_DRAINED");
        GetTree().Quit();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_repeatLearnerCaptureHeld || _workspaceDriveCaptureHeld || _typedLearnerCaptureHeld || TenZoneCaptureClockHeld || _deviceTraceCaptureHeld || _workspaceBeltCaptureState is not null || _workspaceRollerCaptureState is not null || Demo5LearnerCaptureClockHeld) return;
        if (GantryReviewClockHeld && !_gantryReviewStepping) return;
        if (_simulatorShell?.IsExternalMode == true)
        {
            if (_externalConnection?.State == ConnectionState.Connected)
            {
                AdvanceExternalPlc(delta);
                if (_externalPlayback.IsRunning) _sceneRuntime?.AdvanceSimulation(delta);
            }
            return;
        }
        if (_virtualController is null || _sceneRuntime is null) return;
        if (!PrepareWorkspaceDriveAcceptedScan()) return;
        _virtualController.Advance(
            delta,
            SampleVirtualControllerInputs,
            SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs,
            CommitVirtualControllerNumericOutputs,
            AdvanceAcceptedScenePlant, SampleVirtualControllerTextInputs, CommitVirtualControllerTextOutputs,
            CaptureScanInspectionPlantInputs);
    }

    private void AdvanceExternalPlc(double delta)
    {
        if (_externalConnection?.ActiveDescriptor is not JsonElement descriptor
            || _sceneRuntime is null
            || _currentSceneId is null)
            return;
        if (_externalCadenceGeneration != _externalConnection.SessionGeneration)
        {
            _externalCadenceGeneration = _externalConnection.SessionGeneration;
            _externalCycleElapsed = descriptor.GetProperty("cycleMs").GetInt32() / 1000.0;
        }
        _externalCycleElapsed += delta;
        var interval = descriptor.GetProperty("cycleMs").GetInt32() / 1000.0;
        if (_externalConnection.IsBusy || _externalCycleElapsed < interval) return;
        // Never queue catch-up writes. Sample the current plant image once when
        // the bridge is free; the configured cadence is a minimum interval.
        _externalCycleElapsed = 0;
        try
        {
            var runtime = _sceneRuntime;
            var sceneId = _currentSceneId;
            var generation = _externalConnection.SessionGeneration;
            var pcPoints = ExternalSceneContract.SamplePcPoints(descriptor, runtime.Points);
            _externalConnection.Cycle(sceneId, pcPoints, result =>
            {
                if (_simulatorShell?.IsExternalMode != true || !ReferenceEquals(runtime, _sceneRuntime)
                    || sceneId != _currentSceneId) return;
                if (result.GetProperty("connected").GetBoolean() && generation != _externalConnection.SessionGeneration) return;
                SynchronizeExternalPlayback();
                // The Python bridge reports readiness but does not gate its
                // output image. Match the existing browser/native plant policy.
                if (_externalPlayback.IsReady)
                    runtime.CommitExternalPlcOutputs(ExternalSceneContract.ReadPlcPoints(descriptor, result.GetProperty("plcPoints")));
            }, ReportExternalExchangeFailure);
            runtime.ConsumeExternalInputPulses(pcPoints);
        }
        catch (Exception exception)
        {
            ReportExternalExchangeFailure(exception);
        }
    }

    private void ReportExternalExchangeFailure(Exception exception)
    {
        _simulatorShell?.SetWorkspaceStatus($"External PLC exchange failed · {exception.Message}", isError: true);
        _externalConnection?.Disconnect();
    }

    private void SynchronizeExternalPlayback()
    {
        if (_externalConnection is null) return;
        _externalPlayback.Update(_externalConnection.State, _externalConnection.SessionGeneration, _externalConnection.LatestCycle);
        var external = _simulatorShell?.IsExternalMode == true;
        _sceneRuntime?.SetExternalPlayback(external, external && _externalPlayback.IsRunning);
        _simulatorShell?.SetExternalPlaybackState(_externalPlayback.IsRunning, _externalPlayback.ReadinessLabel);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (_simulatorShell is null) return;
        if (inputEvent is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.CtrlPressed && key.Keycode == Key.S)
            {
                if (_simulatorShell.IsLadderView) _simulatorShell.ShowLadderSaveDialog();
                else SaveWorkspace();
            }
            else if (key.CtrlPressed && key.Keycode == Key.O)
            {
                if (_simulatorShell.IsLadderView) _simulatorShell.ShowLadderLoadDialog();
                else LoadWorkspace();
            }
            else if (key.CtrlPressed && key.Keycode == Key.Z && key.ShiftPressed)
            {
                if (_simulatorShell.IsLadderView) _simulatorShell.TryRedoLadderEdit(out _);
                else RedoWorkspace();
            }
            else if (key.CtrlPressed && key.Keycode == Key.Z)
            {
                if (_simulatorShell.IsLadderView) _simulatorShell.TryUndoLadderEdit(out _);
                else UndoWorkspace();
            }
            else if (key.CtrlPressed && key.Keycode == Key.Y)
            {
                if (_simulatorShell.IsLadderView) _simulatorShell.TryRedoLadderEdit(out _);
                else RedoWorkspace();
            }
            else if (key.Keycode == Key.F1 && _simulatorShell.IsLadderView)
                _simulatorShell.ShowLadderInstructionHelp();
            else if (key.CtrlPressed && key.ShiftPressed && key.Keycode == Key.G) UngroupSelection();
            else if (key.CtrlPressed && key.Keycode == Key.G) GroupSelection();
            else if (key.CtrlPressed && key.Keycode == Key.C) CopySelection();
            else if (key.CtrlPressed && key.Keycode == Key.V) PasteSelection();
            else if (key.CtrlPressed && key.Keycode == Key.D && _selectedPlacementIds.Count > 0) DuplicateSelection();
            else if (key.Keycode == Key.F && _selectedPlacementId is not null) FocusPlacement(_selectedPlacementId);
            else if (key.Keycode == Key.W) SetTransformMode(WorkspaceTransformMode.Move);
            else if (key.Keycode == Key.E) SetTransformMode(WorkspaceTransformMode.Rotate);
            else if (key.Keycode == Key.R) SetTransformMode(WorkspaceTransformMode.Scale);
            else if (key.Keycode == Key.Q) SetTransformSpace(
                _transformSpace == WorkspaceTransformSpace.World
                    ? WorkspaceTransformSpace.Local
                    : WorkspaceTransformSpace.World);
            else if (key.Keycode == Key.Delete && _selectedPlacementIds.Count > 0) DeleteSelection();
            else if (key.Keycode == Key.Escape && (_marqueePending || _marqueeActive))
            {
                _marqueePending = false;
                _marqueeActive = false;
                _simulatorShell.SetSelectionMarquee(Vector2.Zero, Vector2.Zero, false);
                _simulatorShell.SetWorkspaceStatus("Marquee selection cancelled");
            }
            else if (key.Keycode == Key.Escape && _transformGizmo?.IsDragging == true)
            {
                foreach (var item in _dragStartTransforms)
                    if (_workspaceNodes.TryGetValue(item.Key, out var placement))
                    {
                        placement.Node.Position = item.Value.Position;
                        placement.Node.RotationDegrees = item.Value.Rotation;
                        placement.Node.Scale = item.Value.Scale;
                    }
                _transformGizmo.EndDrag();
                _gizmoStartSnapshot = null;
                _dragStartTransforms.Clear();
                UpdateTransformGizmo();
                RefreshWorkspaceUi();
            }
            else if (key.Keycode == Key.Escape) SelectPlacement(null);
            else return;
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_mainCamera is null) return;
        if (inputEvent is InputEventMouseMotion motion && _transformGizmo?.IsDragging == true)
        {
            ApplyGizmoDrag(_transformGizmo.DragAmount(_mainCamera, motion.Position));
            GetViewport().SetInputAsHandled();
            return;
        }
        if (inputEvent is InputEventMouseMotion marqueeMotion && _marqueePending)
        {
            var marqueePoint = ClampToRect(marqueeMotion.Position, _simulatorShell.SceneViewportRect());
            if (!_marqueeActive && marqueePoint.DistanceTo(_marqueeStart) >= 6.0f)
                _marqueeActive = true;
            if (_marqueeActive)
                _simulatorShell.SetSelectionMarquee(_marqueeStart, marqueePoint, true);
            GetViewport().SetInputAsHandled();
            return;
        }
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }
            && _transformGizmo?.IsDragging == true)
        {
            _transformGizmo.EndDrag();
            if (_gizmoStartSnapshot is not null)
            {
                _undoHistory.Add(_gizmoStartSnapshot);
                if (_undoHistory.Count > 100) _undoHistory.RemoveAt(0);
                _redoHistory.Clear();
                _gizmoStartSnapshot = null;
            }
            _dragStartTransforms.Clear();
            RefreshWorkspaceUi();
            RefreshConnectionVisuals();
            _simulatorShell.SetWorkspaceStatus("Viewport transform applied");
            RefreshWorkspaceDirty();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } marqueeRelease
            && _marqueePending)
        {
            var wasActive = _marqueeActive;
            _marqueePending = false;
            _marqueeActive = false;
            _simulatorShell.SetSelectionMarquee(Vector2.Zero, Vector2.Zero, false);
            if (wasActive)
            {
                var releasePoint = ClampToRect(marqueeRelease.Position, _simulatorShell.SceneViewportRect());
                var position = new Vector2(
                    MathF.Min(_marqueeStart.X, releasePoint.X),
                    MathF.Min(_marqueeStart.Y, releasePoint.Y));
                var rect = new Rect2(position, new Vector2(
                    MathF.Abs(releasePoint.X - _marqueeStart.X),
                    MathF.Abs(releasePoint.Y - _marqueeStart.Y)));
                var count = SelectPlacementsInScreenRect(rect, _marqueeAdditive);
                _simulatorShell.SetWorkspaceStatus($"Marquee selected {count} placement{(count == 1 ? "" : "s")}");
            }
            else
            {
                SelectAtScreenPoint(marqueeRelease.Position, _marqueeAdditive);
            }
            GetViewport().SetInputAsHandled();
            return;
        }
        if (inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
            return;
        if (_simulatorShell.SceneViewportRect().HasPoint(click.Position)
            && TryExecuteSceneControl(click.Position))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_selectedPlacementId is not null && _transformGizmo?.BeginDrag(_mainCamera, click.Position) == true
            && _workspaceNodes.ContainsKey(_selectedPlacementId))
        {
            _gizmoStartSnapshot = CreateWorkspaceDocument();
            CaptureDragStartTransforms();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_simulatorShell.SceneViewportRect().HasPoint(click.Position))
        {
            _marqueePending = true;
            _marqueeActive = false;
            _marqueeAdditive = click.CtrlPressed || click.ShiftPressed;
            _marqueeStart = click.Position;
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        // The full-screen CanvasLayer consumes pointer events before
        // _UnhandledInput. Scene controls therefore need to be considered at
        // the raw input stage, but only inside the unobstructed 3D aperture.
        if (_simulatorShell is null
            || inputEvent is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click
            || !_simulatorShell.SceneViewportRect().HasPoint(click.Position)
            || !TryExecuteSceneControl(click.Position))
        {
            return;
        }
        GetViewport().SetInputAsHandled();
    }

    private bool TryExecuteSceneControl(Vector2 screenPosition)
    {
        // Review controls sit inside the aperture. Both raw and unhandled input
        // must defer to this overlay; labels/gaps can pass through GUI dispatch.
        if (_visualReviewBar?.IsVisibleInTree() == true
            && (_visualReviewBar.GetGlobalRect().HasPoint(screenPosition)
                || _visualReviewFocus?.GetPopup().Visible == true)) return false;
        if (_gantryReviewClockBar?.IsVisibleInTree() == true
            && _gantryReviewClockBar.GetGlobalRect().HasPoint(screenPosition)) return false;
        if (_sceneControlInteractor is null || _sceneRuntime is null || _mainCamera is null
            || !_sceneControlInteractor.TryPick(_mainCamera, screenPosition, out var control)
            || control is null)
        {
            return false;
        }

        // 3D controls and sidebar controls share the selected execution source.
        var executed = ExecuteSelectedControllerAction(control.ActionId);
        if (executed) _sceneControlInteractor.PlayAcceptedFeedback(control);
        return true;
    }

    private bool ExecuteSelectedControllerAction(string actionId)
    {
        var executed = TryExecuteSelectedControllerAction(actionId);
        if (executed) _simulatorShell?.SetWorkspaceStatus($"Scene action accepted · {actionId}");
        return executed;
    }

    private bool TryExecuteSelectedControllerAction(string actionId)
    {
        if (_sceneRuntime is null || !_sceneRuntime.TryGetAction(actionId, out var action)) return false;
        var type = action.TryGetProperty("type", out var typeElement)
            && typeElement.ValueKind == JsonValueKind.String ? typeElement.GetString() ?? string.Empty : string.Empty;
        var point = action.TryGetProperty("point", out var pointElement) ? pointElement.GetString() ?? string.Empty : string.Empty;
        bool Block(string reason)
        {
            _simulatorShell?.SetWorkspaceStatus($"Action blocked · {reason}", isError: true);
            return false;
        }
        if (!_sceneRuntime.CanExecuteAction(action)) return Block(type switch
        {
            "toggle" when _sceneRuntime.RuntimeType == "evCharging" => "Unplug before vehicle removal; insert only in an occupied bay, with the charge command and allocation off",
            "luggageLoad" => "Finish the current bag at an exit and remove both travel commands before loading the next bag",
            "cycle" when _sceneRuntime.RuntimeType == "luggageWeightSort" && point == "fixture_mass_kg" => "Select fixture mass with travel commands off, while the bag is at home or has finished",
            "palletizerLoad" => "Load requires home, an empty pickup station, and space in the layer; Reset after four cartons or a fault",
            "parkingEnter" => "Entry needs an idle lane and a free bay; use EXIT when both spaces are occupied",
            "parkingExit" => "Exit needs an idle lane and a parked vehicle; REMOVE or re-enter any departed vehicle first",
            "parkingClearDeparted" => "REMOVE applies only after a vehicle has fully departed and the lane is idle",
            "groupingLoad" => "LOAD requires an idle feed and a free group position; wait for the carton to stop, or Reset after the three-carton transfer",
            _ => "scene permissives are not satisfied; reset before restarting",
        });
        if (_simulatorShell?.IsExternalMode == true)
        {
            // Only declared simulator feedback may change here. There is no
            // implicit mapping from a scene motor toggle to a PLC command.
            if (type is not ("toggle" or "togglePoint" or "pulse" or "cycle")
                || _sceneRuntime.IsPlcOwnedPoint(point))
                return Block("External PLC owns this command; no local output was changed");
            return _sceneRuntime.ExecuteAction(actionId);
        }
        if (_virtualController is null)
        {
            if (type is "start" or "run" || _sceneRuntime.IsPlcOwnedPoint(point))
                return Block("No ladder controller is loaded; add or load scene logic and Run first");
            return _sceneRuntime.ExecuteAction(actionId);
        }
        if (type == "emergencyStop")
        {
            _sceneRuntime.ExecuteAction(actionId);
            StopActiveController();
            return true;
        }
        if (type == "reset") { ResetActiveController(); return true; }
        if (type == "stop") { StopActiveController(); return true; }
        if (type == "run") { RunEditorDraftController(); return true; }
        if (_virtualController.IsPaused)
        {
            if (!ScanInspectionCurrentDraftMatches()) return Block("current draft changed; Run to load it before queuing input");
            if (TryQueuePausedSceneAction(action, type, point)) return true;
            return Block("plant changes are held while paused; Resume before loading or moving equipment");
        }
        if (_sceneRuntime.IsPlcOwnedPoint(point) || type == "start")
        {
            if (_sceneRuntime.HasLatchedEmergencyStop) return Block("explicitly reset the simulated E-stop first");
            if (_virtualController.Snapshot.State != VirtualControllerState.Running)
                return Block("Run the controller before issuing a machine command");
            if (point.Length > 0 && _virtualProgram?.Variables.Any(item =>
                item.Role == PlcVariableRole.Output && item.Binding.Equals(point, StringComparison.Ordinal)) != true)
                return Block($"the loaded ladder has no output mapped to {point}");
            // These bindings are authored in the scene, never inferred from a
            // PLC output name. A missing binding is reported instead of silently
            // forcing an output or queuing an invalid input for a later Run.
            var stopping = _sceneRuntime.Points.TryGetValue(point, out var current) && current is true;
            var bindingKey = stopping ? "controllerStopBinding" : "controllerStartBinding";
            if (!action.TryGetProperty(bindingKey, out var bindingElement))
                return Block("this scene action has no command binding in the loaded ladder program");
            var binding = bindingElement.GetString();
            var variable = _virtualProgram?.Variables.FirstOrDefault(item =>
                item.Role == PlcVariableRole.Input && item.Type == PlcVariableType.Bool
                && item.Binding.Equals(binding, StringComparison.Ordinal));
            if (variable is null) return Block($"the loaded ladder program has no BOOL input for {binding}");
            _virtualController.PulseInput(variable.Name);
            return true;
        }
        // A scene pulse is an explicit press, unlike an input already held at
        // startup. Capture its released baseline before the first scan.
        if (type == "pulse" && _virtualController.Snapshot.State == VirtualControllerState.Running
            && _sceneRuntime.Points.GetValueOrDefault(point) is false)
        {
            var pulseVariable = _virtualProgram?.Variables.FirstOrDefault(item =>
                item.Role == PlcVariableRole.Input && item.Type == PlcVariableType.Bool && item.Binding == point);
            if (pulseVariable is not null) _virtualController.PrepareExplicitInputPulse(pulseVariable.Name);
        }
        return _sceneRuntime.ExecuteAction(actionId);
    }

    private void AddCandidatePreview(AssetDefinition asset)
    {
        var packedScene = ResourceLoader.Load<PackedScene>(asset.Model.DeliveryGltf)
            ?? throw new System.InvalidOperationException(
                $"Unable to load candidate model {asset.Model.DeliveryGltf}."
            );
        var model = packedScene.Instantiate<Node3D>();
        model.Name = "CandidatePreview";
        AddChild(model);
        _candidatePreviewRoot = model;

        if (asset.Id.Contains("pallet-roller-conveyor", StringComparison.Ordinal))
        {
            var controller = new RollerConveyorController
            {
                Name = "RollerConveyorController",
                RunCommand = !_captureState.Equals("stopped", StringComparison.OrdinalIgnoreCase),
                EstopOk = true,
                SpeedSetpointMps = 0.45f,
            };
            model.AddChild(controller);
            _previewSpeedReader = () => controller.ActualSpeedMps;
        }
        else
        {
            var controller = new ConveyorController
            {
                Name = "ConveyorController",
                RunCommand = !_captureState.Equals("stopped", StringComparison.OrdinalIgnoreCase),
                EstopOk = true,
                SpeedSetpointMps = 0.65f,
            };
            model.AddChild(controller);
            _previewSpeedReader = () => controller.ActualSpeedMps;
        }
    }

    private void AddCaptureWitness(AssetDefinition asset)
    {
        if (asset.Id.Contains("pallet-roller-conveyor", StringComparison.Ordinal))
        {
            AddPalletWitness();
            return;
        }

        var box = new BoxMesh { Size = new Vector3(0.42f, 0.28f, 0.42f) };
        box.Material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.92f, 0.56f, 0.03f),
            Metallic = 0.04f,
            Roughness = 0.64f,
        };
        _captureWitness = new MeshInstance3D
        {
            Name = "ReviewMotionWitness",
            Mesh = box,
            Position = new Vector3(-1.60f, 1.205f, 0.0f),
        };
        AddChild(_captureWitness);
    }

    private void AddPalletWitness()
    {
        var root = new Node3D
        {
            Name = "ReviewPalletWitness",
            Position = new Vector3(-1.25f, 1.095f, 0.0f),
        };
        var wood = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.43f, 0.24f, 0.09f),
            Metallic = 0.02f,
            Roughness = 0.72f,
        };
        foreach (var z in new[] { -0.40f, 0.0f, 0.40f })
        {
            AddWitnessBox(root, new Vector3(1.20f, 0.10f, 0.10f), new Vector3(0, 0.05f, z), wood);
        }
        foreach (var x in new[] { -0.53f, -0.35f, -0.17f, 0.0f, 0.17f, 0.35f, 0.53f })
        {
            AddWitnessBox(root, new Vector3(0.13f, 0.045f, 1.0f), new Vector3(x, 0.12f, 0), wood);
        }
        _captureWitness = root;
        AddChild(root);
    }

    private static void AddWitnessBox(Node parent, Vector3 size, Vector3 position, Material material)
    {
        var mesh = new BoxMesh { Size = size, Material = material };
        parent.AddChild(new MeshInstance3D { Mesh = mesh, Position = position });
    }

    private static Node3D CreateSelectionOutline(AssetDefinition asset)
    {
        var width = Math.Max((float)asset.Bounds.WidthM, 0.08f);
        var height = Math.Max((float)asset.Bounds.HeightM, 0.08f);
        var depth = Math.Max((float)asset.Bounds.DepthM, 0.08f);
        var minimum = new Vector3(-width * 0.5f, 0.0f, -depth * 0.5f);
        var maximum = new Vector3(width * 0.5f, height, depth * 0.5f);
        var corners = new[]
        {
            new Vector3(minimum.X, minimum.Y, minimum.Z),
            new Vector3(maximum.X, minimum.Y, minimum.Z),
            new Vector3(maximum.X, minimum.Y, maximum.Z),
            new Vector3(minimum.X, minimum.Y, maximum.Z),
            new Vector3(minimum.X, maximum.Y, minimum.Z),
            new Vector3(maximum.X, maximum.Y, minimum.Z),
            new Vector3(maximum.X, maximum.Y, maximum.Z),
            new Vector3(minimum.X, maximum.Y, maximum.Z),
        };
        var edges = new (int Start, int End)[]
        {
            (0, 1), (1, 2), (2, 3), (3, 0),
            (4, 5), (5, 6), (6, 7), (7, 4),
            (0, 4), (1, 5), (2, 6), (3, 7),
        };
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color("42d3ff"),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            NoDepthTest = true,
            EmissionEnabled = true,
            Emission = new Color("42d3ff"),
            EmissionEnergyMultiplier = 2.0f,
        };
        var lines = new ImmediateMesh();
        lines.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
        foreach (var edge in edges)
        {
            lines.SurfaceAddVertex(corners[edge.Start]);
            lines.SurfaceAddVertex(corners[edge.End]);
        }
        lines.SurfaceEnd();
        return new MeshInstance3D
        {
            Name = "EditorSelectionOutline",
            Mesh = lines,
            Visible = false,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
    }

    private static string SafeNodeName(string value) => value
        .Replace("-", "_", StringComparison.Ordinal)
        .Replace(".", "_", StringComparison.Ordinal)
        .Replace("/", "_", StringComparison.Ordinal);

    private static Vector3 Vector(double[] values) => new(
        (float)values[0], (float)values[1], (float)values[2]
    );

    private static double[] Array(Vector3 value) =>
        [value.X, value.Y, value.Z];
}






