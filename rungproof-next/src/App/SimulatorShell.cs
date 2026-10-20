using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using RungProof.Next.Catalog;
using RungProof.Next.Challenges;
using RungProof.Next.Connections;
using RungProof.Next.Diagnostics;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

/// <summary>
/// Main operator/authoring shell. It presents project data and delegates all
/// plant behavior to SceneSimulationRuntime; it never owns PLC transport.
/// </summary>
public partial class SimulatorShell : CanvasLayer
{
    private static TreeItem? FindIndexedTreeRow(Tree tree, int index)
    {
        var root = tree.GetRoot();
        for (var row = root?.GetFirstChild(); row is not null; row = row.GetNext())
        {
            if (row.GetMetadata(0).AsString() == $"tag:{index}") return row;
        }
        return null;
    }

    private static void SelectIndexedTreeRow(Tree tree, int index)
    {
        FindIndexedTreeRow(tree, index)?.Select(0);
    }

    private static int SelectedTreeRowIndex(Tree tree)
    {
        var metadata = tree.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
        return metadata.StartsWith("tag:", StringComparison.Ordinal)
            && int.TryParse(metadata.AsSpan(4), out var index) ? index : -1;
    }

    private readonly AssetCatalogDocument _assets;
    private readonly SceneCatalogDocument _sceneCatalog;
    private readonly IReadOnlyList<SceneCatalogEntry> _orderedScenes;
    private readonly List<SceneCatalogEntry?> _scenarioBrowserRows = [];
    private static readonly (string Label, string SceneId)[] AuthoredDemos =
    [
        ("Demo 1 - Press Count Starter", "lab-4-01-press-count-lamp"),
        ("Demo 2 - Delayed Lamp Timer", "lab-5-01-delayed-lamp"),
        ("Demo 3 - Conveyor Sequence", "scene-1-conveyor-stop"),
        ("Demo 4 - Batch Process Blocks", "lab-9-11-pallet-counting"),
        ("Demo 5 - Integrated Palletizing Cell Multi-FB/FC", "lab-11-13-xy-palletizing"),
    ];
    private readonly IReadOnlyList<DiagnosticIssue> _diagnostics;
    private readonly IGuardedRuntimeClient _connection;
    private readonly List<AssetDefinition> _filteredAssets = [];
    private SceneSimulationRuntime? _runtime;
    private SceneDefinition? _activeScene;
    private readonly List<string> _operatorEvents = [];
    private RichTextLabel? _operatorEventHistory;
    private AssetDefinition? _selectedAsset;

    private ItemList _sceneList = null!;
    private ItemList _assetList = null!;
    private LineEdit _assetSearch = null!;
    private RichTextLabel _inspector = null!;
    private RichTextLabel _ioInspector = null!;
    private RichTextLabel _connectionInspector = null!;
    private OptionButton _fromInstance = null!;
    private OptionButton _fromConnector = null!;
    private OptionButton _toInstance = null!;
    private OptionButton _toConnector = null!;
    private OptionButton _signalInstance = null!;
    private OptionButton _signal = null!;
    private OptionButton _scenePoint = null!;
    private ItemList _connectorLinkList = null!;
    private ItemList _signalLinkList = null!;
    private Label _signalMappingSummary = null!;
    private PanelContainer _leftDock = null!;
    private PanelContainer _rightDock = null!;
    private PanelContainer _diagnosticsDock = null!;
    private PanelContainer _transportDock = null!;
    private bool _diagnosticsExpanded;
    private bool _pointsCollapsed;
    private bool _pointsCollapsedBeforeEditor;
    private bool _compactDockDefaultsApplied;
    private int _tiaProjectDockSplitOffset = 280;
    private int _studio5000ProjectDockSplitOffset = 280;
    private string _activeSceneName = "Loading project…";
    private ItemList _diagnosticList = null!;
    private Label _sceneTitle = null!;
    private Label _runtimeStatus = null!;
    private Label _connectionStatus = null!;
    private Label _cycleStatus = null!;
    private Label _diagnosticSummary = null!;
    private Label _workspaceStatus = null!;
    private Label _transportScene = null!;
    private Label _transportScope = null!;
    private Label _operatorInputHeading = null!;
    private Label _operatorOutputHeading = null!;
    private RichTextLabel _operatorSceneSummary = null!;
    private RichTextLabel _operatorRuntimeSummary = null!;
    private string _sceneInspectorText = string.Empty;
    private RichTextLabel _operatorEquipment = null!;
    private RichTextLabel _operatorInputPoints = null!;
    private RichTextLabel _operatorOutputPoints = null!;
    private Control _operatorPointTables = null!;
    private PanelContainer _tagWatchPanel = null!;
    private ItemList _tagWatchList = null!;
    private readonly List<string> _watchablePointNames = [];
    private Button _placeAsset = null!;
    private Button _viewAsset = null!;
    private Button _assetHelp = null!;
    private Button _returnFromAssetPreview = null!;
    private AcceptDialog _assetHelpDialog = null!;
    private RichTextLabel _assetHelpContent = null!;
    private TabContainer _leftTabs = null!;
    private TabContainer _inspectorTabs = null!;
    private ItemList _operatorSceneList = null!;
    private LineEdit _viewerSearch = null!;
    private ItemList _viewerHierarchy = null!;
    private readonly List<(string Kind, string Id)> _viewerItems = [];
    private Button _viewerReturn = null!;
    private VBoxContainer _operatorActions = null!;
    private RichTextLabel _operatorHealth = null!;
    private readonly Dictionary<string, Button> _productViewButtons = [];
    private Button? _sceneQuickTab;
    private Button? _logicQuickTab;
    private string _productView = "classic";
    private ItemList _workspaceList = null!;
    private readonly List<IReadOnlyList<string>> _workspaceListSelectionTargets = [];
    private Button _deletePlacement = null!;
    private Button _undo = null!;
    private Button _redo = null!;
    private CheckButton _snapEnabled = null!;
    private SpinBox _positionSnap = null!;
    private SpinBox _rotationSnap = null!;
    private readonly SpinBox[] _positionInputs = new SpinBox[3];
    private readonly SpinBox[] _rotationInputs = new SpinBox[3];
    private readonly SpinBox[] _scaleInputs = new SpinBox[3];
    private IReadOnlyList<WorkspacePlacement> _workspacePlacements = [];
    private string? _selectedPlacementId;
    private readonly HashSet<string> _selectedPlacementIds = new(StringComparer.Ordinal);
    private bool _updatingTransformInputs;
    private IReadOnlyList<WorkspaceConnectorLink> _connectorLinks = [];
    private IReadOnlyList<WorkspaceSignalLink> _signalLinks = [];
    private IReadOnlyDictionary<string, SignalMappingRuntimeStatus> _signalStatuses =
        new Dictionary<string, SignalMappingRuntimeStatus>();
    private readonly Dictionary<WorkspaceTransformMode, Button> _transformModeButtons = [];
    private readonly Dictionary<WorkspaceTransformSpace, Button> _transformSpaceButtons = [];
    private IReadOnlyList<WorkspaceGroup> _workspaceGroups = [];
    private Panel _selectionMarquee = null!;
    private MenuButton _arrangeMenu = null!;
    private ConfirmationDialog _renameGroupDialog = null!;
    private LineEdit _renameGroupInput = null!;
    private string? _renameGroupId;
    private ConfirmationDialog _pivotDialog = null!;
    private readonly SpinBox[] _pivotInputs = new SpinBox[3];
    private CheckBox _centroidPivot = null!;
    private string? _pivotGroupId;
    private AcceptDialog _simulatorModeDialog = null!;
    private AcceptDialog _workspaceFeedbackDialog = null!;
    private Label _virtualControllerStatus = null!;
    private RichTextLabel _virtualControllerLadder = null!;
    private RichTextLabel _virtualControllerVariables = null!;
    private OptionButton _virtualForceVariable = null!;
    private OptionButton _virtualForceValue = null!;
    private Label _virtualForceStatus = null!;
    private LadderProgram? _virtualProgram;
    private VirtualControllerSnapshot? _virtualSnapshot;
    private PanelContainer _ladderWorkspace = null!;
    private Panel _splitDivider = null!;
    private float _splitDividerRatio = 0.43f;
    private bool _draggingSplitDivider;
    private bool _draggingInstruction;
    private string _draggedInstructionKind = string.Empty;
    private LadderEditorCanvas? _draggedInstructionCanvas;
    private TabContainer _ladderEnvironmentTabs = null!;
    private Label _ladderWorkspaceStatus = null!;
    private readonly LadderEditorDocument _ladderDocument = LadderEditorDocument.CreateConveyorExample();
    private LadderEditorHistory _ladderHistory = new(150);
    // Scene changes replace the plant, not the user's offline project. Keep the
    // shared document object (both editor views reference it) and restore each
    // scene's exact draft, including invalid work and its own Undo/Redo history.
    private sealed record SceneLadderDraft(LadderEditorSnapshot Document,
        string? SavedJson, string? ProjectPath, LadderEditorHistory History);
    private readonly Dictionary<string, SceneLadderDraft> _sceneLadderDrafts = new(StringComparer.Ordinal);
    private readonly List<LadderEditorCanvas> _ladderCanvases = [];
    private TiaOperandRepresentation _tiaOperandRepresentation = TiaOperandRepresentation.Symbolic;
    private TiaTagInformationDisplay _tiaTagInformationDisplay = TiaTagInformationDisplay.Hidden;
    private TiaTagInformationPosition _tiaTagInformationPosition = TiaTagInformationPosition.OnOperands;
    private EditableRung? _ladderRungClipboard;
    private EditableContact? _ladderContactClipboard;
    private EditorClipboard? _ladderCommandClipboard;
    private bool _ladderMonitorMatchesLoadedProgram;
    private readonly List<Action> _ladderEditorRefreshers = [];
    private Action? _restoreActiveWatchTableTreeRow;
    private Func<string>? _getActiveTiaEditorDocumentKey;
    private Action<string, string>? _ensureTiaEditorDocumentTab;
    private Action<IReadOnlyList<string>>? _orderTiaEditorDocumentTabs;
    private Action<string>? _activateTiaEditorDocument;
    private readonly List<Action> _ladderEditorProjectReplacementResetters = [];
    private readonly List<Action> _ladderSceneDiagnosticResetters = [];
    private readonly List<Action<VirtualControllerSnapshot?>> _ladderWatchRefreshers = [];
    private readonly List<Action<IReadOnlyList<LadderValidationIssue>>> _ladderValidationRefreshers = [];
    private FileDialog _ladderSaveDialog = null!;
    private FileDialog _ladderLoadDialog = null!;
    private ConfirmationDialog _unsavedLadderDialog = null!;
    private FileDialog _pendingDraftSaveDialog = null!;
    private Action? _pendingLadderAction;
    private readonly Queue<(string SceneId, SceneLadderDraft Draft)> _pendingDraftSaves = [];
    private FileDialog _workspaceSaveDialog = null!;
    private FileDialog _workspaceLoadDialog = null!;
    private ConfirmationDialog _unsavedWorkspaceDialog = null!;
    private string? _ladderSavedProjectJson;
    private string? _ladderProjectPath;
    private AcceptDialog _applicationSettingsDialog = null!;
    private AcceptDialog _externalPlcDialog = null!;
    private OptionButton _externalProfileSelector = null!;
    private JsonElement? _verifiedExternalDescriptor;
    private int _externalSelectionGeneration;
    private RichTextLabel _externalProfileDetails = null!;
    private CheckButton _externalAuthorization = null!;
    private bool _externalMode;
    private CheckButton _mcpUiEnabled = null!;
    private CheckButton _showSafetyNotices = null!;
    private PopupMenu _toolsPopup = null!;
    private const int McpToggleMenuId = 100;
    private const int ApplicationSettingsMenuId = 101;
    private const string SettingsPath = "user://rungproof-settings.json";

    public event Action<string>? SceneRequested;
    public event Action<AssetDefinition>? AssetPlacementRequested;
    /// <summary>
    /// Requests a read-only, isolated render of the selected catalog model.
    /// This does not place an object or alter the authored workspace.
    /// </summary>
    public event Action<AssetDefinition>? AssetPreviewRequested;
    public event Action? AssetPreviewClosed;
    public event Action? SaveWorkspaceRequested;
    public event Action? LoadWorkspaceRequested;
    public event Action? CreateFiniteBeltWorkspaceRequested;
    public event Action? CreateFiniteRollerWorkspaceRequested;
    public event Action<string>? SaveWorkspaceToPathRequested;
    public event Action<string>? LoadWorkspaceFromPathRequested;
    public Func<Action, bool>? WorkspaceReplacementGuard { get; set; }
    public event Action? WorkspaceDiscardRequested;
    public event Action? WorkspaceReplacementCancelled;
    public event Action<IReadOnlyList<string>>? PlacementSelectionSetRequested;
    public event Action<string, Vector3, Vector3, Vector3>? PlacementTransformRequested;
    public event Action? UndoRequested;
    public event Action? RedoRequested;
    public event Action? DuplicateSelectionRequested;
    public event Action? CopySelectionRequested;
    public event Action? PasteSelectionRequested;
    public event Action? DeleteSelectionRequested;
    public event Action? GroupSelectionRequested;
    public event Action? UngroupSelectionRequested;
    public event Action<string, string>? RenameGroupRequested;
    public event Action<string, double[]?>? GroupPivotRequested;
    public event Action<string>? FocusPlacementRequested;
    public event Action<bool>? FrameSceneRequested;
    public event Action<string>? ResetPlacementTransformRequested;
    public event Action<bool, double, double>? SnapSettingsChanged;
    public event Action<WorkspaceTransformMode>? TransformModeRequested;
    public event Action<WorkspaceTransformSpace>? TransformSpaceRequested;
    public event Action<WorkspaceArrangeOperation>? ArrangeSelectionRequested;
    public event Action<string, string, string, string>? ConnectorLinkRequested;
    public event Action<string, string, string>? SignalLinkRequested;
    public event Action<string>? ConnectorLinkDeleteRequested;
    public event Action<string>? SignalLinkDeleteRequested;
    public event Action? RunRequested;
    public event Action? StopRequested;
    public event Action? ResetRequested;
    public event Action<string>? SceneActionRequested;
    public event Action<bool>? ControllerModeChanged;
    public event Action? VirtualControllerDemoRequested;
    public event Action<LadderProgram>? VirtualControllerProgramRequested;
    public event Action? VirtualControllerDisabled;
    public event Action<string, bool>? VirtualForceRequested;
    public event Action<string>? VirtualForceRemoveRequested;
    public event Action? VirtualForceClearRequested;
    public event Func<IReadOnlyList<(string VariableName, object Value)>, bool>? VirtualWatchModifyRequested;
    public event Action? VirtualStartRequested;
    public event Action? VirtualStopRequested;
    public event Action<string>? ProductViewChanged;

    public SimulatorShell(
        AssetCatalogDocument assets,
        SceneCatalogDocument sceneCatalog,
        IReadOnlyList<DiagnosticIssue> diagnostics,
        IGuardedRuntimeClient connection
    )
    {
        _assets = assets;
        _sceneCatalog = sceneCatalog;
        _orderedScenes = sceneCatalog.Scenes
            .OrderBy(ScenarioSortGroup)
            .ThenBy(scene => ScenarioSortGroup(scene) == 2
                ? ScenarioDemoIndex(scene)
                : 0)
            .ThenBy(ScenarioSortMajor)
            .ThenBy(ScenarioSortMinor)
            .ThenBy(scene => scene.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        _diagnostics = diagnostics;
        _connection = connection;
        Name = "SimulatorShell";
    }

    public override void _Ready()
    {
        BuildInterface();
        PopulateScenes();
        FilterAssets(string.Empty);
        FilterViewerHierarchy(string.Empty);
        PopulateDiagnostics();
        RefreshConnection();
        _connection.StateChanged += RefreshConnection;
        GetViewport().SizeChanged += RefreshViewportLayout;
    }

    public override void _ExitTree()
    {
        _connection.StateChanged -= RefreshConnection;
        GetViewport().SizeChanged -= RefreshViewportLayout;
        if (_runtime is not null)
        {
            _runtime.StateChanged -= RefreshRuntimeState;
        }
    }

    public override void _Input(InputEvent input)
    {
        if (_draggingInstruction && _draggedInstructionCanvas is not null)
        {
            var canvas = _draggedInstructionCanvas;
            if (input is InputEventMouseMotion dragMotion)
            {
                var local = canvas.GetGlobalTransformWithCanvas().AffineInverse() * dragMotion.Position;
                canvas.PreviewInsertionAt(local);
                GetViewport().SetInputAsHandled();
                return;
            }
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release)
            {
                var local = canvas.GetGlobalTransformWithCanvas().AffineInverse() * release.Position;
                canvas.DropInstructionAt(_draggedInstructionKind, local);
                _draggingInstruction = false;
                _draggedInstructionKind = string.Empty;
                _draggedInstructionCanvas = null;
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (_productView != "split" || _splitDivider is null) return;

        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed && _splitDivider.GetGlobalRect().HasPoint(button.Position))
            {
                _draggingSplitDivider = true;
                GetViewport().SetInputAsHandled();
            }
            else if (!button.Pressed && _draggingSplitDivider)
            {
                _draggingSplitDivider = false;
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (input is InputEventMouseMotion motion && _draggingSplitDivider)
        {
            var viewport = GetViewport().GetVisibleRect();
            var width = viewport.Size.X;
            if (width > 0.0f)
            {
                MoveSplitDivider(motion.Position.X);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void AttachScene(
        SceneDefinition scene,
        SceneSimulationRuntime runtime,
        SceneComposition composition
    )
    {
        InvalidateExternalApproval();
        var changingScene = _activeScene is null||SceneDraftCacheKey(_activeScene)!=SceneDraftCacheKey(scene);
        if (changingScene && _activeScene is not null)
            _sceneLadderDrafts[SceneDraftCacheKey(_activeScene)] = CaptureSceneLadderDraft();
        if (_runtime is not null)
        {
            _runtime.StateChanged -= RefreshRuntimeState;
        }
        _activeScene = scene;
        _deviceTraceScene=string.Empty; // Same source ID can attach a different explicit workspace preset.
        SelectActiveScenarioRow();
        _activeSceneName = ScenarioDisplayName(scene);
        _runtime = runtime;
        _runtime.StateChanged += RefreshRuntimeState;
        RebuildOperatorActions();
        if (changingScene) LoadSceneLadderDraft(scene);
        _sceneTitle.Text = _activeSceneName;
        _sceneTitle.TooltipText = _activeSceneName;
        GetWindow().Title = $"RungProof · {_activeSceneName}";
        RecordOperatorEvent($"Loaded {_activeSceneName}");
        _selectedAsset = null;
        _placeAsset.Disabled = true;
        RefreshSceneInspector(composition);
        RefreshIoInspector();
        RefreshRuntimeState();
        RefreshOperatorPanels();
        RefreshOperatorPointTables();
        UpdateWorkspace([], null, canUndo: false, canRedo: false, [], []);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
    }

    private static bool IsFiniteBeltWorkspace(SceneDefinition scene)=>scene.Simulation.ValueKind==JsonValueKind.Object
        &&scene.Simulation.TryGetProperty("type",out var runtimeType)
        &&runtimeType.ValueKind==JsonValueKind.String&&runtimeType.GetString()=="workspaceFiniteBelt";
    private static bool IsFiniteRollerWorkspace(SceneDefinition scene)=>scene.Simulation.ValueKind==JsonValueKind.Object
        &&scene.Simulation.TryGetProperty("type",out var runtimeType)
        &&runtimeType.ValueKind==JsonValueKind.String&&runtimeType.GetString()=="workspaceFiniteRoller";
    private static string SceneDraftCacheKey(SceneDefinition scene)=>scene.Id+(IsFiniteBeltWorkspace(scene)?":workspaceFiniteBelt":IsFiniteRollerWorkspace(scene)?":workspaceFiniteRoller":"");
    private SceneLadderDraft CaptureSceneLadderDraft() => new(
        _ladderDocument.CaptureSnapshot(), _ladderSavedProjectJson, _ladderProjectPath, _ladderHistory);

    private void LoadSceneLadderDraft(SceneDefinition scene)
    {
        // Verification belongs to the previous scene's controller, not the
        // restored or fresh draft. Both editor modes must show this boundary.
        foreach (var reset in _ladderSceneDiagnosticResetters) reset();
        if (_sceneLadderDrafts.TryGetValue(SceneDraftCacheKey(scene), out var draft))
        {
            _ladderDocument.RestoreSnapshot(draft.Document);
            _ladderSavedProjectJson = draft.SavedJson;
            _ladderProjectPath = draft.ProjectPath;
            _ladderHistory = draft.History;
            _ladderWorkspaceStatus.Text = "SCENE DRAFT RESTORED · VERIFY + LOAD BEFORE RUN";
            _ladderWorkspaceStatus.TooltipText = "The scene's exact offline draft and edit history were restored. Save to retain it after exit.";
            RefreshLadderMonitorMatch();
            return;
        }

        LadderEditorDocument starter=null!;
        var authored = !IsFiniteBeltWorkspace(scene)&&!IsFiniteRollerWorkspace(scene)&&(AuthoredDemoLadderPrograms.TryCreate(scene.Id, out starter)
            || AuthoredLessonLadderPrograms.TryCreate(scene.Id, out starter));
        IReadOnlyList<SceneIoPoint> unsupported = [];
        if (!authored)
            starter = scene.Id == "conveyor-cell" ? LadderEditorDocument.CreateConveyorExample()
                : SceneLadderProject.Create(scene.Id, scene.Name, ScenePoints(),
                    _runtime?.Points ?? new Dictionary<string, object?>(), out unsupported);
        _ladderDocument.RestoreSnapshot(starter.CaptureSnapshot());
        _ladderDocument.SourceSceneId = scene.Id;
        // Align only a fresh template. Restoring a draft must never rewrite
        // bindings the user has deliberately edited.
        if (scene.Id == "conveyor-cell") AlignDefaultConveyorBindings(scene);
        _ladderSavedProjectJson = LadderEditorProjectJson.Save(_ladderDocument);
        _ladderProjectPath = null;
        _ladderHistory = new LadderEditorHistory(150);
        _ladderRungClipboard = null;
        _ladderContactClipboard = null; _ladderCommandClipboard = null;
        if (_ladderWorkspaceStatus is not null)
        {
            var blockSummary = string.Join(", ", _ladderDocument.Blocks
                .Select(block => block.BlockType switch
                {
                    LadderBlockType.OrganizationBlock => "OB",
                    LadderBlockType.FunctionBlock => "FB",
                    LadderBlockType.Function => "FC",
                    LadderBlockType.DataBlock => "DB",
                    _ => "BLOCK",
                }));
            _ladderWorkspaceStatus.Text = authored
                ? $"AUTHORED LADDER LOADED · {blockSummary}"
                : scene.Id == "conveyor-cell" ? "CONVEYOR LADDER READY · VERIFY + LOAD"
                : unsupported.Count > 0 ? $"EXERCISE · {unsupported.Count} UNSUPPORTED I/O TYPES · LADDER REQUIRED"
                : "EXERCISE · SCENE TAGS READY · ADD LADDER NETWORKS BEFORE RUN";
            _ladderWorkspaceStatus.TooltipText = unsupported.Count > 0
                ? "Offline ladder cannot bind: " + string.Join(", ", unsupported.Select(point => $"{point.Name} [{point.Type}]"))
                : "Demos contain reference programs. Other labs are exercises with their declared scene tags.";
            _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("65d49a"));
        }
    }

    private void AlignDefaultConveyorBindings(SceneDefinition scene)
    {
        // The editor keeps stable ladder symbols for training, while bindings
        // must match the symbolic points declared by the active scene.
        var photoeyeBinding = scene.Id == "conveyor-cell"
            ? "photoeye_blocked"
            : "simulated_photoeye";
        var conveyorBinding = scene.Id == "conveyor-cell"
            ? "conveyor_run"
            : "conveyor_running";

        var photoeye = _ladderDocument.Tags.FirstOrDefault(tag => tag.Name == "simulated_photoeye");
        if (photoeye is not null && !photoeye.Binding.Equals(photoeyeBinding, StringComparison.Ordinal))
        {
            _ladderDocument.UpdateTag(photoeye.Name, photoeye.Name, photoeye.Type, photoeye.Role,
                photoeyeBinding, photoeye.InitialValue);
        }

        var conveyor = _ladderDocument.Tags.FirstOrDefault(tag => tag.Name == "conveyor_running");
        if (conveyor is not null && !conveyor.Binding.Equals(conveyorBinding, StringComparison.Ordinal))
        {
            _ladderDocument.UpdateTag(conveyor.Name, conveyor.Name, conveyor.Type, conveyor.Role,
                conveyorBinding, conveyor.InitialValue);
        }
    }

    public bool VerifyStructure(out string result)
    {
        var required = new[]
        {
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/FileMenu",
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/ViewMenu",
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlaybackMenu",
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/SceneMenu",
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/ToolsMenu",
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlcMenu",
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/HelpMenu",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette/Bit Logic/AddNormallyOpen",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette/Bit Logic/AddEmptyInstructionBox",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette/Bit Logic/AddNormallyClosed",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette/Bit Logic/AddBranch",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView/GraphicalLadderCanvas",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree",
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView/GraphicalLadderCanvas",
            "Workspace/Transport/TransportMargin/TransportRow/RunButton",
            "Workspace/Transport/TransportMargin/TransportRow/StopButton",
            "Workspace/Transport/TransportMargin/TransportRow/ResetButton",
            "Workspace/Transport/TransportMargin/TransportRow/SaveButton",
            "Workspace/Transport/TransportMargin/TransportRow/LoadButton",
            "Workspace/LeftDock/LeftTabs/Scenes/SceneList",
             "Workspace/LeftDock/LeftTabs/Assets/AssetList",
             "Workspace/LeftDock/LeftTabs/Assets/ViewAssetButton",
             "Workspace/LeftDock/LeftTabs/Assets/AssetHelpButton",
             "Workspace/LeftDock/LeftTabs/Assets/ReturnFromAssetPreviewButton",
             "AssetHelpDialog",
             "AssetHelpDialog/AssetHelpScroll/AssetHelpContent",
            "Workspace/LeftDock/LeftTabs/Workspace/WorkspaceList",
            "Workspace/LeftDock/LeftTabs/Workspace/EditorButtons/UndoButton",
            "Workspace/LeftDock/LeftTabs/Workspace/EditorButtons/RedoButton",
            "Workspace/LeftDock/LeftTabs/Workspace/EditorButtons/ArrangeMenu",
            "Workspace/LeftDock/LeftTabs/Workspace/DeletePlacementButton",
            "Workspace/LeftDock/LeftTabs/Workspace/ObjectButtons/DuplicatePlacementButton",
            "Workspace/LeftDock/LeftTabs/Workspace/ObjectButtons/FocusPlacementButton",
            "Workspace/LeftDock/LeftTabs/Workspace/SnapSettings/SnapEnabled",
            "Workspace/LeftDock/LeftTabs/Workspace/TransformModes/MoveModeButton",
            "Workspace/LeftDock/LeftTabs/Workspace/TransformModes/RotateModeButton",
            "Workspace/LeftDock/LeftTabs/Workspace/TransformModes/ScaleModeButton",
            "Workspace/LeftDock/LeftTabs/Workspace/ClipboardButtons/CopySelectionButton",
            "Workspace/LeftDock/LeftTabs/Workspace/ClipboardButtons/PasteSelectionButton",
            "Workspace/LeftDock/LeftTabs/Workspace/GroupButtons/GroupSelectionButton",
            "Workspace/LeftDock/LeftTabs/Workspace/GroupButtons/UngroupSelectionButton",
            "Workspace/LeftDock/LeftTabs/Workspace/GroupButtons/RenameGroupButton",
            "Workspace/LeftDock/LeftTabs/Workspace/GroupButtons/PivotGroupButton",
            "Workspace/LeftDock/LeftTabs/Workspace/TransformSpace/WorldSpaceButton",
            "Workspace/LeftDock/LeftTabs/Workspace/TransformSpace/LocalSpaceButton",
            "Workspace/RightDock/InspectorTabs/Inspector",
            "Workspace/RightDock/InspectorTabs/Signals",
            "Workspace/RightDock/InspectorTabs/Connections",
            "Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/SignalMappingSummary",
            "Workspace/RightDock/InspectorTabs/PLC",
            "Workspace/RightDock/InspectorTabs/Virtual Controller",
            "Workspace/LeftDock/LeftTabs/Operator/OperatorContent/OperatorSceneSummary",
            "Workspace/LeftDock/LeftTabs/Operator/OperatorContent/OperatorRuntimeSummary",
            "Workspace/RightDock/InspectorTabs/Health/HealthContent/OperatorEquipment",
            "Workspace/RightDock/InspectorTabs/Health/HealthContent/OperatorHealth",
            "Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticList",
            "Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle",
            "Workspace/SelectionMarquee",
        };
        var missing = required.Where(path => GetNodeOrNull(path) is null).ToArray();
        var errors = new List<string>();
        if (missing.Length > 0) errors.Add($"missing nodes: {string.Join(", ", missing)}");
        var tiaProjectTree = GetNodeOrNull<Tree>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree");
        static bool HasTreeMetadata(TreeItem? first, string metadata)
        {
            for (var item = first; item is not null; item = item.GetNext())
                if (item.GetMetadata(0).AsString() == metadata || HasTreeMetadata(item.GetFirstChild(), metadata)) return true;
            return false;
        }
        if (tiaProjectTree is not null
            && (!HasTreeMetadata(tiaProjectTree.GetRoot(), "add-block")
                || !HasTreeMetadata(tiaProjectTree.GetRoot(), "add-tag-table")
                || !HasTreeMetadata(tiaProjectTree.GetRoot(), "tag-table:")
                || !HasTreeMetadata(tiaProjectTree.GetRoot(), "tag-table:Default tag table")))
            errors.Add("TIA project tree is missing Add new block, Show all tags, Add new tag table, or the default tag table row.");
        var scenarioRows = _scenarioBrowserRows.Count(scene => scene is not null);
        if (scenarioRows != _sceneCatalog.Scenes.Count)
            errors.Add($"scene list {scenarioRows}/{_sceneCatalog.Scenes.Count}");
        if (_assetList.ItemCount != _assets.Assets.Count)
            errors.Add($"asset list {_assetList.ItemCount}/{_assets.Assets.Count}");
        var lab21Index = _orderedScenes.ToList().FindIndex(scene => scene.Name.StartsWith("Lab 2.1 ", StringComparison.Ordinal));
        var lab101Index = _orderedScenes.ToList().FindIndex(scene => scene.Name.StartsWith("Lab 10.1 ", StringComparison.Ordinal));
        if (lab21Index < 0 || lab101Index < 0 || lab21Index >= lab101Index)
            errors.Add($"scenario numeric order missing: lab2.1={lab21Index} lab10.1={lab101Index}");
        var scenarioMenu = GetNodeOrNull<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/SceneMenu");
        var scenarioPopup = scenarioMenu?.GetPopup();
        var demoMenuItems = scenarioPopup is null
            ? 0
            : Enumerable.Range(0, scenarioPopup.GetItemCount())
                .Count(index => scenarioPopup.GetItemText(index).StartsWith("Demo ", StringComparison.Ordinal));
        var demoMenuOrder = scenarioPopup is null
            ? []
            : Enumerable.Range(0, scenarioPopup.GetItemCount())
                .Select(index => scenarioPopup.GetItemText(index))
                .Where(label => label.StartsWith("Demo ", StringComparison.Ordinal))
                .ToArray();
        var expectedDemoOrder = AuthoredDemos.Select(demo => demo.Label).ToArray();
        var groupMenuItems = scenarioPopup is null
            ? 0
            : new[] { "USER CREATED", "LABS", "DEMOS" }
                .Count(group => Enumerable.Range(0, scenarioPopup.GetItemCount())
                    .Any(index => scenarioPopup.GetItemText(index) == group));
        if (scenarioMenu is null
            || scenarioMenu.Text != "Scenario"
            || scenarioPopup is null
            || scenarioPopup.GetItemText(0) != "Scenario Browser"
            || demoMenuItems != AuthoredDemos.Length
            || !demoMenuOrder.SequenceEqual(expectedDemoOrder, StringComparer.Ordinal)
            || groupMenuItems != 3
            || _sceneQuickTab?.Text != "SCENARIO"
            || _leftTabs.GetTabTitle(1) != "Scenarios")
            errors.Add($"scenario menu contract missing: menu={scenarioMenu?.Text ?? "<missing>"} demos={demoMenuItems}/{AuthoredDemos.Length} order={string.Join(" | ", demoMenuOrder)} groups={groupMenuItems}/3");
        var toolbarRow = GetNodeOrNull<HBoxContainer>("Workspace/Toolbar/ToolbarMargin/ToolbarRow");
        var titleIndex = _sceneTitle?.GetIndex() ?? -1;
        var sceneTabIndex = _sceneQuickTab?.GetIndex() ?? -1;
        var logicTabIndex = _logicQuickTab?.GetIndex() ?? -1;
        if (toolbarRow is null
            || titleIndex < 0
            || sceneTabIndex <= titleIndex
            || logicTabIndex <= sceneTabIndex)
            errors.Add($"view tabs are not ordered after scene title: title={titleIndex} scenario={sceneTabIndex} logic={logicTabIndex}");
        if (!_sceneList.HasThemeColorOverride("font_selected_color")
            || !_sceneList.HasThemeStyleboxOverride("selected")
            || !_assetList.HasThemeColorOverride("font_selected_color")
            || !_workspaceList.HasThemeStyleboxOverride("selected"))
            errors.Add("browser selection contrast contract is missing");
        if (_connection.State != ConnectionState.Disconnected)
            errors.Add($"unsafe default connection state: {_connection.State}");
        if (_activeScene is null || _runtime is null) errors.Add("no active scene/runtime");
        if (_activeScene?.Id == "lab-11-13-xy-palletizing")
        {
            if (_sceneTitle?.Text != AuthoredDemos[4].Label)
                errors.Add($"starting scenario title is stale: {_sceneTitle?.Text ?? "<missing>"}");
            var cartonBinding = _ladderDocument.Tags.FirstOrDefault(tag => tag.Name == "carton_at_pick")?.Binding;
            var vacuumBinding = _ladderDocument.Tags.FirstOrDefault(tag => tag.Name == "vacuum_pick")?.Binding;
            var hasFunctionBlock = _ladderDocument.Blocks.Any(block => block.BlockType == LadderBlockType.FunctionBlock);
            var hasFunction = _ladderDocument.Blocks.Any(block => block.BlockType == LadderBlockType.Function);
            if (cartonBinding != "carton_at_pick" || vacuumBinding != "vacuum_pick")
                errors.Add($"starting-scene bindings are stale: carton={cartonBinding ?? "<missing>"} vacuum={vacuumBinding ?? "<missing>"}");
            if (!hasFunctionBlock || !hasFunction || _ladderDocument.Blocks.Count < 6)
                errors.Add($"Demo 5 ladder complexity missing: blocks={_ladderDocument.Blocks.Count} FB={hasFunctionBlock} FC={hasFunction}");
        }
        foreach (var demo in AuthoredDemos)
        {
            if (!AuthoredDemoLadderPrograms.TryCreate(demo.SceneId, out var demoDocument))
            {
                errors.Add($"authored ladder missing: {demo.Label}");
                continue;
            }
            if (demoDocument.SourceSceneId != demo.SceneId)
                errors.Add($"authored ladder scene mismatch: {demo.Label} -> {demoDocument.SourceSceneId}");
            var compile = LadderCompiler.Compile(demoDocument.BuildProgram());
            if (!compile.IsValid)
                errors.Add($"authored ladder invalid: {demo.Label} ({string.Join(", ", compile.Issues.Select(issue => issue.Code))})");
            var savedPath = AuthoredDemoLadderPrograms.ProjectFiles.First(item => item.SceneId == demo.SceneId).RelativePath;
            var saved = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://" + savedPath));
            if (!saved.IsReadable || saved.Document is null
                || LadderEditorProjectJson.Save(saved.Document) != LadderEditorProjectJson.Save(demoDocument))
                errors.Add($"saved demo project differs from Demo menu: {savedPath}");
        }
        if (_productView != "operator" || !_leftDock.Visible || !_rightDock.Visible
            || !_diagnosticsDock.Visible || !_transportDock.Visible)
            errors.Add("original operator-console layout is not the visible default");
        result = errors.Count == 0
            ? $"scenes={scenarioRows} groups=3 demos={AuthoredDemos.Length} assets={_assetList.ItemCount} diagnostics={_diagnostics.Count} connection={_connection.State.ToString().ToUpperInvariant()}"
            : string.Join("; ", errors);
        return errors.Count == 0;
    }

    public bool VerifyLayout(out string result)
    {
        var errors = new List<string>();
        var viewport = GetViewport().GetVisibleRect();
        var toolbar = GetNode<Control>("Workspace/Toolbar").GetGlobalRect();
        var left = _leftDock.GetGlobalRect();
        var right = _rightDock.GetGlobalRect();
        var diagnostics = _diagnosticsDock.GetGlobalRect();
        var centerWidth = right.Position.X - left.End.X;
        var centerHeight = diagnostics.Position.Y - toolbar.End.Y;
        if (viewport.Size.X < 1200 || viewport.Size.Y < 675)
            errors.Add($"logical viewport too small: {viewport.Size.X:0}x{viewport.Size.Y:0}");
        if (toolbar.End.Y > left.Position.Y + 4.0f || toolbar.End.Y > right.Position.Y + 4.0f)
            errors.Add($"toolbar overlaps side docks: toolbarEnd={toolbar.End.Y:0.##} sideTop={left.Position.Y:0.##}");
        if (left.End.Y > diagnostics.Position.Y + 4.0f || right.End.Y > diagnostics.Position.Y + 4.0f)
            errors.Add($"side dock overlaps diagnostics: sideEnd={left.End.Y:0.##} diagnosticsTop={diagnostics.Position.Y:0.##}");
        if (left.End.X > right.Position.X)
            errors.Add("left and right docks overlap");
        if (centerWidth < 480 || centerHeight < 420)
            errors.Add($"3D viewport aperture is too small: {centerWidth:0}x{centerHeight:0}");
        foreach (var path in new[]
                 {
                     "Workspace/Transport/TransportMargin/TransportRow/RunButton",
                     "Workspace/Transport/TransportMargin/TransportRow/StopButton",
                     "Workspace/Transport/TransportMargin/TransportRow/ResetButton",
                     "Workspace/Transport/TransportMargin/TransportRow/SaveButton",
                     "Workspace/Transport/TransportMargin/TransportRow/LoadButton",
                 })
        {
            var rect = GetNode<Control>(path).GetGlobalRect();
            if (rect.Position.X < viewport.Position.X || rect.End.X > viewport.End.X
                || rect.Position.Y < viewport.Position.Y || rect.End.Y > viewport.End.Y)
                errors.Add($"transport control outside viewport: {path}");
        }
        var connectionPanel = GetNode<Control>("Workspace/RightDock/InspectorTabs/Connections").GetGlobalRect();
        foreach (var path in new[]
                 {
                     "Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/CreateConnectorLink",
                     "Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/DeleteConnectorLink",
                     "Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/CreateSignalLink",
                     "Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/SignalLinks",
                     "Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/SignalMappingSummary",
                     "Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/DeleteSignalLink",
                 })
        {
            var rect = GetNode<Control>(path).GetGlobalRect();
            if (rect.Position.X < connectionPanel.Position.X - 2.0f
                || rect.End.X > connectionPanel.End.X + 2.0f
                || rect.Position.Y < connectionPanel.Position.Y - 2.0f
                || rect.End.Y > connectionPanel.End.Y + 2.0f)
                errors.Add($"connection control clipped or requires scrolling: {path}");
        }
        _leftTabs.CurrentTab = 3;
        var workspacePanel = GetNode<Control>("Workspace/LeftDock/LeftTabs/Workspace").GetGlobalRect();
        foreach (var path in new[]
                 {
                     "Workspace/LeftDock/LeftTabs/Workspace/TransformModes/MoveModeButton",
                     "Workspace/LeftDock/LeftTabs/Workspace/ObjectButtons/DuplicatePlacementButton",
                     "Workspace/LeftDock/LeftTabs/Workspace/ClipboardButtons/PasteSelectionButton",
                     "Workspace/LeftDock/LeftTabs/Workspace/SnapSettings/SnapEnabled",
                     "Workspace/LeftDock/LeftTabs/Workspace/DeletePlacementButton",
                 })
        {
            var rect = GetNode<Control>(path).GetGlobalRect();
            if (rect.Position.X < workspacePanel.Position.X - 2.0f
                || rect.End.X > workspacePanel.End.X + 2.0f
                || rect.Position.Y < workspacePanel.Position.Y - 2.0f
                || rect.End.Y > workspacePanel.End.Y + 2.0f)
                errors.Add($"workspace control clipped: {path}");
        }
        result = errors.Count == 0
            ? $"logical={viewport.Size.X:0}x{viewport.Size.Y:0} toolbarEnd={toolbar.End.Y:0.##} sideTop={left.Position.Y:0.##} aperture={centerWidth:0}x{centerHeight:0} diagnostics={diagnostics.Size.Y:0}"
            : string.Join("; ", errors);
        return errors.Count == 0;
    }

    public bool VerifySplitLayout(out string result)
    {
        SetProductView("split");
        var errors = new List<string>();
        var viewport = GetViewport().GetVisibleRect();
        var toolbar = GetNode<Control>("Workspace/Toolbar").GetGlobalRect();
        var ladder = _ladderWorkspace.GetGlobalRect();
        var scene = SceneViewportRect();
        var ladderCanvas = GetNodeOrNull<Control>(
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView/GraphicalLadderCanvas");
        if (!_ladderWorkspace.Visible) errors.Add("ladder split panel is hidden");
        if (ladder.Position.X <= viewport.Position.X + 100.0f)
            errors.Add($"ladder pane is not on the right: x={ladder.Position.X:0.##}");
        if (ladder.Position.Y < toolbar.End.Y - 1.0f)
            errors.Add($"ladder pane overlaps toolbar: toolbarEnd={toolbar.End.Y:0.##} paneTop={ladder.Position.Y:0.##}");
        if (ladder.End.X > viewport.End.X + 1.0f || ladder.End.Y > viewport.End.Y + 1.0f)
            errors.Add($"ladder pane exceeds viewport: pane={ladder} viewport={viewport}");
        var localModel = _diagnosticsDock.GetGlobalRect();
        if (ladder.End.Y > localModel.Position.Y + 1.0f)
            errors.Add($"ladder pane overlaps local model points: ladderBottom={ladder.End.Y:0.##} pointsTop={localModel.Position.Y:0.##}");
        if (scene.Size.X < 400.0f || scene.Size.Y < 300.0f)
            errors.Add($"scene pane is too small: {scene.Size.X:0.##}x{scene.Size.Y:0.##}");
        if (ladderCanvas is null || !ladderCanvas.Visible)
            errors.Add("ladder canvas is not visible");
        if (!_diagnosticsDock.Visible || !_operatorPointTables.Visible)
            errors.Add("local model points are missing from split view");
        var routineView = ladderCanvas?.GetParent() as Control;
        if (routineView is not null && routineView.Size.Y < 150.0f)
            errors.Add($"usable ladder viewport is too short: {routineView.Size.Y:0.##}");
        result = errors.Count == 0
            ? $"scene={scene.Size.X:0}x{scene.Size.Y:0} ladder={ladder.Size.X:0}x{ladder.Size.Y:0}"
            : string.Join("; ", errors);
        return errors.Count == 0;
    }

    public Rect2 SceneViewportRect()
    {
        var toolbar = GetNode<Control>("Workspace/Toolbar").GetGlobalRect();
        if (_productView == "ladder") return new Rect2();
        if (IsChallengeDraftActive && _challengePresenter is { Visible: true })
        {
            var viewport = GetViewport().GetVisibleRect();
            var top = _challengePresenter.GetGlobalRect().End.Y + 8;
            var width = _productView == "split" ? _ladderWorkspace.GetGlobalRect().Position.X - viewport.Position.X - 8 : viewport.Size.X;
            return new Rect2(new Vector2(viewport.Position.X, top), new Vector2(Math.Max(0, width), Math.Max(0, viewport.End.Y - 58 - top)));
        }
        if (_productView == "floor")
        {
            var floorViewport = GetViewport().GetVisibleRect();
            return new Rect2(new Vector2(floorViewport.Position.X, toolbar.End.Y),
                new Vector2(floorViewport.Size.X, floorViewport.End.Y - toolbar.End.Y));
        }
        if (_productView == "split")
        {
            var viewport = GetViewport().GetVisibleRect();
            var ladder = _ladderWorkspace.GetGlobalRect();
            return new Rect2(
                new Vector2(viewport.Position.X, toolbar.End.Y),
                new Vector2(ladder.Position.X - viewport.Position.X - 8.0f,
                    _diagnosticsDock.GetGlobalRect().Position.Y - toolbar.End.Y));
        }
        if (_productView == "operator")
        {
            var operatorLeft = _leftDock.GetGlobalRect();
            var operatorRight = _rightDock.GetGlobalRect();
            var operatorDiagnostics = _diagnosticsDock.GetGlobalRect();
            return new Rect2(
                new Vector2(operatorLeft.End.X, toolbar.End.Y),
                new Vector2(operatorRight.Position.X - operatorLeft.End.X,
                    operatorDiagnostics.Position.Y - toolbar.End.Y)
            );
        }
        var left = _leftDock.GetGlobalRect();
        var right = _rightDock.GetGlobalRect();
        var diagnostics = _diagnosticsDock.GetGlobalRect();
        return new Rect2(
            new Vector2(left.End.X, toolbar.End.Y),
            new Vector2(right.Position.X - left.End.X, diagnostics.Position.Y - toolbar.End.Y));
    }

    public void SetSelectionMarquee(Vector2 start, Vector2 end, bool visible)
    {
        _selectionMarquee.Visible = visible;
        if (!visible) return;
        var position = new Vector2(MathF.Min(start.X, end.X), MathF.Min(start.Y, end.Y));
        _selectionMarquee.Position = position;
        _selectionMarquee.Size = new Vector2(MathF.Abs(end.X - start.X), MathF.Abs(end.Y - start.Y));
    }

    public void ShowArrangeMenu() => _arrangeMenu.ShowPopup();

    public void PrepareBlockCreationDialogCapture()
    {
        var addBlock = FindChild("AddBlock", true, false) as Button
            ?? throw new InvalidOperationException("The ladder editor Add Block control is unavailable.");
        addBlock.EmitSignal(BaseButton.SignalName.Pressed);
    }

    public void PrepareInstanceDbDefinitionCapture()
    {
        // Capture-only project data: show an FB-generated instance DB with
        // interface sections and defaults through the same tree activation route
        // used by an operator. The authored project and saved baseline are not written.
        var axis = _ladderDocument.AddBlock("FB_AxisDrive", LadderBlockType.FunctionBlock);
        _ladderDocument.AddInterfaceParameter(axis.Id, "Enable", PlcVariableType.Bool,
            LadderInterfaceSection.Input, false, comment: "Axis permission");
        _ladderDocument.AddInterfaceParameter(axis.Id, "PositiveRequest", PlcVariableType.Bool,
            LadderInterfaceSection.Input, false, comment: "Positive direction request");
        _ladderDocument.AddInterfaceParameter(axis.Id, "NegativeRequest", PlcVariableType.Bool,
            LadderInterfaceSection.Input, false, comment: "Negative direction request");
        _ladderDocument.AddInterfaceParameter(axis.Id, "PositiveCommand", PlcVariableType.Bool,
            LadderInterfaceSection.Output, false, comment: "Positive drive command");
        _ladderDocument.AddInterfaceParameter(axis.Id, "NegativeCommand", PlcVariableType.Bool,
            LadderInterfaceSection.Output, false, comment: "Negative drive command");
        _ladderDocument.AddInterfaceParameter(axis.Id, "ActualPosition", PlcVariableType.Real,
            LadderInterfaceSection.Static, 0.0, comment: "Measured position");
        _ladderDocument.Instances.Add(new LadderInstance("db_layout_axis", "X_Axis", axis.Id,
            new LadderInstanceDataBlock(101, "X_Axis_DB")));
        foreach (var refresh in _ladderEditorRefreshers) refresh();

        const string treePath = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree";
        var projectTree = GetNodeOrNull<Tree>(treePath)
            ?? throw new InvalidOperationException("The TIA project tree is unavailable for instance-DB capture.");
        TreeItem? FindInstanceDb(TreeItem? item)
        {
            for (var current = item; current is not null; current = current.GetNext())
            {
                if (current.GetMetadata(0).AsString() == "instance-db:101") return current;
                if (FindInstanceDb(current.GetFirstChild()) is { } child) return child;
            }
            return null;
        }
        var instanceDb = FindInstanceDb(projectTree.GetRoot())
            ?? throw new InvalidOperationException("The capture IEC instance DB was not added to the TIA project tree.");
        instanceDb.Select(0);
        projectTree.EmitSignal(Tree.SignalName.ItemActivated);
    }

    public void PrepareNamedWatchTableCapture()
    {
        var tableIndex = _ladderDocument.WatchTables.FindIndex(table => table.Name == "Commissioning");
        if (tableIndex < 0)
        {
            var table = _ladderDocument.AddWatchTable("Commissioning");
            foreach (var symbol in new[] { "start_command", "stop_command", "simulated_photoeye", "seal_in", "conveyor_running" })
                if (_ladderDocument.Tags.Any(tag => tag.Name == symbol)) table.Variables.Add(symbol);
            tableIndex = _ladderDocument.ActiveWatchTableIndex;
        }
        foreach (var refresh in _ladderEditorRefreshers) refresh();

        const string treePath = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree";
        var projectTree = GetNodeOrNull<Tree>(treePath)
            ?? throw new InvalidOperationException("The TIA project tree is unavailable for watch-table capture.");
        TreeItem? FindWatchTable(TreeItem? item)
        {
            for (var current = item; current is not null; current = current.GetNext())
            {
                if (current.GetMetadata(0).AsString() == $"watch-table:{tableIndex}") return current;
                if (FindWatchTable(current.GetFirstChild()) is { } child) return child;
            }
            return null;
        }
        var row = FindWatchTable(projectTree.GetRoot())
            ?? throw new InvalidOperationException("The named watch table was not added to the TIA project tree.");
        row.GetParent()?.SetCollapsed(false);
        row.Select(0);
        projectTree.EmitSignal(Tree.SignalName.ItemActivated);

        var bottomPanel = FindChild("InspectorOutputDock", true, false) as Control;
        var reopenBottom = FindChild("ReopenBottomDock", true, false) as Control;
        var tabs = FindChild("InspectorOutputTabs", true, false) as TabContainer;
        if (bottomPanel is null || reopenBottom is null || tabs is null)
            throw new InvalidOperationException("The watch-table bottom workspace is unavailable for capture.");
        bottomPanel.Show();
        reopenBottom.Hide();
        tabs.CurrentTab = 2;
        if (bottomPanel.GetParent() is SplitContainer split)
            split.SplitOffsets = [260];
    }

    public void PrepareGlobalDbDefinitionCapture()
    {
        var blockIndex = _ladderDocument.Blocks.FindIndex(block => block.Name == "Machine_Setpoints"
            && block.BlockType == LadderBlockType.DataBlock);
        if (blockIndex < 0)
        {
            var db = _ladderDocument.AddBlock("Machine_Setpoints", LadderBlockType.DataBlock);
            _ladderDocument.AddInterfaceParameter(db.Id, "SystemEnabled", PlcVariableType.Bool, LadderInterfaceSection.Static, false,
                comment: "Master operating permission");
            _ladderDocument.AddInterfaceParameter(db.Id, "BatchTarget", PlcVariableType.Int, LadderInterfaceSection.Static, 4L,
                retain: true, comment: "Batches after power cycle");
            _ladderDocument.AddInterfaceParameter(db.Id, "ConveyorSpeed", PlcVariableType.Real, LadderInterfaceSection.Static, 1250.0,
                comment: "Line speed setpoint");
            blockIndex = _ladderDocument.Blocks.IndexOf(db);
        }
        _ladderDocument.SelectBlock(blockIndex);
        foreach (var refresh in _ladderEditorRefreshers) refresh();

        const string treePath = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree";
        var projectTree = GetNodeOrNull<Tree>(treePath)
            ?? throw new InvalidOperationException("The TIA project tree is unavailable for global-DB capture.");
        TreeItem? FindBlock(TreeItem? item)
        {
            for (var current = item; current is not null; current = current.GetNext())
            {
                if (current.GetMetadata(0).AsString() == $"program:{blockIndex}") return current;
                if (FindBlock(current.GetFirstChild()) is { } child) return child;
            }
            return null;
        }
        var row = FindBlock(projectTree.GetRoot())
            ?? throw new InvalidOperationException("The capture global DB was not added to the project tree.");
        row.GetParent()?.SetCollapsed(false);
        row.Select(0);
        projectTree.EmitSignal(Tree.SignalName.ItemActivated);
    }

    public void SetReviewState(string view, string search)
    {
        if (search.Length > 0)
        {
            _assetSearch.Text = search;
            FilterAssets(search);
        }
        switch (view.ToLowerInvariant())
        {
            case "scene":
            case "operator":
                SetProductView("operator");
                break;
            case "split":
                ShowClassicTool("split");
                break;
            case "assets":
                ShowClassicTool("assets");
                if (_filteredAssets.Count > 0)
                {
                    _assetList.Select(0);
                    SelectAsset(0);
                }
                break;
            case "signals":
                ShowClassicTool("signals");
                break;
            case "connection":
                ShowClassicTool("connection");
                break;
            case "connections":
                ShowClassicTool("connections");
                break;
            case "workspace":
                ShowClassicTool("workspace");
                break;
            case "ladder":
                ShowClassicTool("ladder");
                break;
            case "ladder-local":
            {
                ShowClassicTool("ladder");
                const string toolbarRoot = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar";
                var category = GetNodeOrNull<OptionButton>($"{toolbarRoot}/InstructionCategory");
                var palette = GetNodeOrNull<TabContainer>($"{toolbarRoot}/InstructionPalette");
                if (category is null || palette is null) break;
                var localIndex = Enumerable.Range(0, category.ItemCount).FirstOrDefault(index =>
                    category.GetItemText(index) == "RungProof local (not Siemens)", -1);
                if (localIndex >= 0)
                {
                    category.Select(localIndex);
                    palette.CurrentTab = localIndex;
                }
                break;
            }
            case "ladder-edit":
                ShowClassicTool("ladder");
                GetNodeOrNull<LadderEditorCanvas>(
                    "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView/GraphicalLadderCanvas")
                    ?.SelectElement(0, 0, 1);
                break;
            case "ladder-insertion":
                ShowClassicTool("ladder");
                GetNodeOrNull<LadderEditorCanvas>(
                    "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView/GraphicalLadderCanvas")
                    ?.SelectInsertionPoint(0, 0, 1);
                break;
            case "ladder-errors":
                ShowClassicTool("ladder");
                _ladderDocument.Rungs[0].CoilVariable = "start_command";
                GetNodeOrNull<Button>(
                    "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/PLC tags/TagContent/ValidateAndLoadButton")
                    ?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            case "ladder-tags":
            case "ladder-tag-table":
            {
                ShowClassicTool("ladder");
                var organization = GetNodeOrNull<Tree>(
                    "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree");
                var desiredMetadata = view == "ladder-tag-table" ? "tag-table:Default tag table" : "tag-table:";
                TreeItem? FindTable(TreeItem? first)
                {
                    for (var item = first; item is not null; item = item.GetNext())
                    {
                        if (item.GetMetadata(0).AsString() == desiredMetadata) return item;
                        var nested = FindTable(item.GetFirstChild());
                        if (nested is not null) return nested;
                    }
                    return null;
                }
                var table = FindTable(organization?.GetRoot());
                if (organization is not null && table is not null)
                {
                    table.Select(0);
                    organization.EmitSignal(Tree.SignalName.ItemActivated);
                }
                break;
            }
            case "ladder-bindings":
            case "ladder-bindings-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var outputPoint = ScenePoints().FirstOrDefault(point =>
                    point.Type.Equals("BOOL", StringComparison.OrdinalIgnoreCase)
                    && point.Owner.Equals("PLC", StringComparison.OrdinalIgnoreCase));
                var tagIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == "conveyor_running");
                if (outputPoint is not null && tagIndex >= 0)
                    _ladderDocument.Tags[tagIndex] = _ladderDocument.Tags[tagIndex] with { Binding = outputPoint.Name };
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var bindingRoot =
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags";
                GetNodeOrNull<TabContainer>(bindingRoot)?.Set("current_tab", 1);
                var tagPageName = logix ? "Controller Tags" : "PLC tags";
                var tagList = GetNodeOrNull<Tree>($"{bindingRoot}/{tagPageName}/TagTable");
                if (tagList is not null && tagIndex >= 0)
                {
                    SelectIndexedTreeRow(tagList, tagIndex);
                    tagList.EmitSignal(Tree.SignalName.ItemSelected);
                }
                break;
            }
            case "ladder-numeric-bindings":
            case "ladder-numeric-bindings-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var mathPalette = GetNodeOrNull<TabContainer>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette");
                if (mathPalette is not null) mathPalette.CurrentTab = 4;
                var inputPoint = ScenePoints().FirstOrDefault(point =>
                    point.Owner.Equals("PC", StringComparison.OrdinalIgnoreCase)
                    && point.Type is "REAL" or "DINT" or "INT");
                if (inputPoint is null) break;
                var type = inputPoint.Type.ToUpperInvariant() switch
                {
                    "REAL" => PlcVariableType.Real,
                    "DINT" => PlcVariableType.DInt,
                    _ => PlcVariableType.Int,
                };
                const string tagName = "scene_numeric_input";
                var tagIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == tagName);
                if (tagIndex < 0)
                {
                    _ladderDocument.AddTag(tagName, PlcVariableRole.Input, inputPoint.Name, type);
                    tagIndex = _ladderDocument.Tags.Count - 1;
                }
                else
                {
                    _ladderDocument.Tags[tagIndex] = _ladderDocument.Tags[tagIndex] with
                    {
                        Type = type,
                        Role = PlcVariableRole.Input,
                        Binding = inputPoint.Name,
                    };
                }
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var environment = logix ? "Studio 5000" : "TIA Portal";
                var bindingRoot =
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environment}/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags";
                var tagPageName = logix ? "Controller Tags" : "PLC tags";
                GetNodeOrNull<TabContainer>(bindingRoot)?.Set("current_tab", 1);
                var tagList = GetNodeOrNull<Tree>($"{bindingRoot}/{tagPageName}/TagTable");
                if (tagList is not null)
                {
                    SelectIndexedTreeRow(tagList, tagIndex);
                    tagList.EmitSignal(Tree.SignalName.ItemSelected);
                }
                break;
            }
            case "ladder-monitor":
                ShowClassicTool("ladder");
                var monitorProgram = _ladderDocument.BuildProgram();
                var monitorCompilation = LadderCompiler.Compile(monitorProgram);
                if (monitorCompilation.IsValid && monitorCompilation.Program is not null)
                {
                    var monitorRuntime = new VirtualControllerRuntime(monitorCompilation.Program);
                    monitorRuntime.Run();
                    var monitorSnapshot = monitorRuntime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(monitorProgram, monitorSnapshot);
                }
                break;
            case "ladder-watch":
            case "ladder-watch-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var watchProgram = _ladderDocument.BuildProgram();
                var watchCompilation = LadderCompiler.Compile(watchProgram);
                if (watchCompilation.IsValid && watchCompilation.Program is not null)
                {
                    var watchRuntime = new VirtualControllerRuntime(watchCompilation.Program);
                    watchRuntime.Run();
                    var watchSnapshot = watchRuntime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(watchProgram, watchSnapshot);
                }
                var environment = logix ? "Studio 5000" : "TIA Portal";
                var workbenchRoot = $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environment}/Workbench/EditorAndTasks/EditorAndInspector";
                GetNodeOrNull<TabContainer>($"{workbenchRoot}/BottomDockHost/InspectorOutputDock/InspectorOutputBody/InspectorOutputTabs")
                    ?.Set("current_tab", 2);
                var split = GetNodeOrNull<VSplitContainer>(workbenchRoot);
                if (split is not null) split.SplitOffsets = [540];
                break;
            }
            case "ladder-edges":
            case "ladder-edges-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var rising = _ladderDocument.Rungs[0].Branches[0].Contacts[0];
                _ladderDocument.ReplaceContact(0, 0, 0,
                    rising with { NormallyClosed = false, EdgeMode = LadderEdgeMode.Rising });
                var falling = _ladderDocument.Rungs[1].Branches[0].Contacts[0];
                _ladderDocument.ReplaceContact(1, 0, 0,
                    falling with { NormallyClosed = false, EdgeMode = LadderEdgeMode.Falling });
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var edgeProgram = _ladderDocument.BuildProgram();
                var edgeCompilation = LadderCompiler.Compile(edgeProgram);
                if (edgeCompilation.IsValid && edgeCompilation.Program is not null)
                {
                    var edgeRuntime = new VirtualControllerRuntime(edgeCompilation.Program);
                    edgeRuntime.Run();
                    edgeRuntime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = false,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    var edgeSnapshot = edgeRuntime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(edgeProgram, edgeSnapshot);
                }
                break;
            }
            case "ladder-math":
            case "ladder-math-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var mathPalette = GetNodeOrNull<TabContainer>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette");
                if (mathPalette is not null) mathPalette.CurrentTab = 4;
                if (_ladderDocument.Tags.All(tag => tag.Name != "sqrt_result"))
                    _ladderDocument.AddTag("sqrt_result", PlcVariableRole.Memory, type: PlcVariableType.Real);
                if (_ladderDocument.Tags.All(tag => tag.Name != "remainder"))
                    _ladderDocument.AddTag("remainder", PlcVariableRole.Memory, type: PlcVariableType.DInt);
                void ConfigureMath(EditableRung rung, LadderNumericOperationKind kind,
                    string sourceA, string sourceB, string destination)
                {
                    rung.IsTimer = false;
                    rung.IsTimerReset = false;
                    rung.IsCounter = false;
                    rung.IsCounterReset = false;
                    rung.IsCounterLoad = false;
                    rung.IsCall = false;
                    rung.IsReturn = false;
                    rung.IsJump = false;
                    rung.IsLabel = false;
                    rung.IsNumericOperation = true;
                    rung.NumericOperationKind = kind;
                    rung.NumericSourceA = sourceA;
                    rung.NumericSourceB = sourceB;
                    rung.NumericDestination = destination;
                }
                ConfigureMath(_ladderDocument.Rungs[0], LadderNumericOperationKind.SquareRoot,
                    "81", "0", "sqrt_result");
                ConfigureMath(_ladderDocument.Rungs[1], LadderNumericOperationKind.Modulo,
                    "17", "5", "remainder");
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var mathProgram = _ladderDocument.BuildProgram();
                var mathCompilation = LadderCompiler.Compile(mathProgram);
                if (mathCompilation.IsValid && mathCompilation.Program is not null)
                {
                    var mathRuntime = new VirtualControllerRuntime(mathCompilation.Program);
                    mathRuntime.Run();
                    var mathSnapshot = mathRuntime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(mathProgram, mathSnapshot);
                }
                break;
            }
            case "ladder-scientific":
            case "ladder-scientific-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var scientificPalette = GetNodeOrNull<TabContainer>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette");
                if (scientificPalette is not null) scientificPalette.CurrentTab = 5;
                if (_ladderDocument.Tags.All(tag => tag.Name != "power_result"))
                    _ladderDocument.AddTag("power_result", PlcVariableRole.Memory, type: PlcVariableType.Real);
                if (_ladderDocument.Tags.All(tag => tag.Name != "log_result"))
                    _ladderDocument.AddTag("log_result", PlcVariableRole.Memory, type: PlcVariableType.Real);
                void ConfigureScientific(EditableRung rung, LadderNumericOperationKind kind,
                    string sourceA, string sourceB, string destination)
                {
                    rung.IsTimer = false;
                    rung.IsTimerReset = false;
                    rung.IsCounter = false;
                    rung.IsCounterReset = false;
                    rung.IsCounterLoad = false;
                    rung.IsCall = false;
                    rung.IsReturn = false;
                    rung.IsJump = false;
                    rung.IsLabel = false;
                    rung.IsNumericOperation = true;
                    rung.NumericOperationKind = kind;
                    rung.NumericSourceA = sourceA;
                    rung.NumericSourceB = sourceB;
                    rung.NumericDestination = destination;
                }
                ConfigureScientific(_ladderDocument.Rungs[0], LadderNumericOperationKind.Exponentiate,
                    "2", "8", "power_result");
                ConfigureScientific(_ladderDocument.Rungs[1], LadderNumericOperationKind.NaturalLog,
                    "1", "0", "log_result");
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var program = _ladderDocument.BuildProgram();
                var compilation = LadderCompiler.Compile(program);
                if (compilation.IsValid && compilation.Program is not null)
                {
                    var runtime = new VirtualControllerRuntime(compilation.Program);
                    runtime.Run();
                    var snapshot = runtime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(program, snapshot);
                }
                break;
            }
            case "ladder-scaling":
            case "ladder-scaling-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var conversionPalette = GetNodeOrNull<TabContainer>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette");
                if (conversionPalette is not null) conversionPalette.CurrentTab = 6;
                if (_ladderDocument.Tags.All(tag => tag.Name != "normalized_value"))
                    _ladderDocument.AddTag("normalized_value", PlcVariableRole.Memory, type: PlcVariableType.Real);
                if (_ladderDocument.Tags.All(tag => tag.Name != "engineering_value"))
                    _ladderDocument.AddTag("engineering_value", PlcVariableRole.Memory, type: PlcVariableType.Real);
                void ConfigureScaling(EditableRung rung, LadderNumericOperationKind kind,
                    string minimum, string value, string maximum, string destination)
                {
                    rung.IsTimer = false;
                    rung.IsTimerReset = false;
                    rung.IsCounter = false;
                    rung.IsCounterReset = false;
                    rung.IsCounterLoad = false;
                    rung.IsCall = false;
                    rung.IsReturn = false;
                    rung.IsJump = false;
                    rung.IsLabel = false;
                    rung.IsNumericOperation = true;
                    rung.NumericOperationKind = kind;
                    rung.NumericSourceA = minimum;
                    rung.NumericSourceB = value;
                    rung.NumericSourceC = maximum;
                    rung.NumericDestination = destination;
                }
                ConfigureScaling(_ladderDocument.Rungs[0], LadderNumericOperationKind.Normalize,
                    "0", "13824", "27648", "normalized_value");
                ConfigureScaling(_ladderDocument.Rungs[1], LadderNumericOperationKind.Scale,
                    "0", "0.5", "100", "engineering_value");
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var program = _ladderDocument.BuildProgram();
                var compilation = LadderCompiler.Compile(program);
                if (compilation.IsValid && compilation.Program is not null)
                {
                    var runtime = new VirtualControllerRuntime(compilation.Program);
                    runtime.Run();
                    var snapshot = runtime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(program, snapshot);
                }
                break;
            }
            case "ladder-conversion":
            case "ladder-conversion-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var conversionPalette = GetNodeOrNull<TabContainer>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette");
                if (conversionPalette is not null) conversionPalette.CurrentTab = 6;
                if (_ladderDocument.Tags.All(tag => tag.Name != "rounded_count"))
                    _ladderDocument.AddTag("rounded_count", PlcVariableRole.Memory, type: PlcVariableType.DInt);
                if (_ladderDocument.Tags.All(tag => tag.Name != "truncated_count"))
                    _ladderDocument.AddTag("truncated_count", PlcVariableRole.Memory, type: PlcVariableType.DInt);
                void ConfigureConversion(EditableRung rung, LadderNumericOperationKind kind,
                    string source, string destination)
                {
                    rung.IsTimer = false;
                    rung.IsTimerReset = false;
                    rung.IsCounter = false;
                    rung.IsCounterReset = false;
                    rung.IsCounterLoad = false;
                    rung.IsCall = false;
                    rung.IsReturn = false;
                    rung.IsJump = false;
                    rung.IsLabel = false;
                    rung.IsNumericOperation = true;
                    rung.NumericOperationKind = kind;
                    rung.NumericSourceA = source;
                    rung.NumericSourceB = "0";
                    rung.NumericSourceC = "0";
                    rung.NumericDestination = destination;
                }
                ConfigureConversion(_ladderDocument.Rungs[0], LadderNumericOperationKind.Round,
                    "3.5", "rounded_count");
                ConfigureConversion(_ladderDocument.Rungs[1], LadderNumericOperationKind.Truncate,
                    "-3.9", "truncated_count");
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var program = _ladderDocument.BuildProgram();
                var compilation = LadderCompiler.Compile(program);
                if (compilation.IsValid && compilation.Program is not null)
                {
                    var runtime = new VirtualControllerRuntime(compilation.Program);
                    runtime.Run();
                    var snapshot = runtime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(program, snapshot);
                }
                break;
            }
            case "ladder-retentive-timer":
            case "ladder-retentive-timer-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var timerPalette = GetNodeOrNull<TabContainer>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette");
                if (timerPalette is not null) timerPalette.CurrentTab = 1;
                if (_ladderDocument.Tags.All(tag => tag.Name != "process_time"))
                    _ladderDocument.AddTag("process_time", PlcVariableRole.Memory, type: PlcVariableType.Timer);
                void ClearOtherOutputs(EditableRung rung)
                {
                    rung.IsCounter = false;
                    rung.IsCounterReset = false;
                    rung.IsCounterLoad = false;
                    rung.IsNumericOperation = false;
                    rung.IsCall = false;
                    rung.IsReturn = false;
                    rung.IsJump = false;
                    rung.IsLabel = false;
                }
                var timerRung = _ladderDocument.Rungs[0];
                ClearOtherOutputs(timerRung);
                timerRung.IsTimer = true;
                timerRung.IsTimerReset = false;
                timerRung.TimerKind = LadderTimerKind.RetentiveOnDelay;
                timerRung.TimerVariable = "process_time";
                timerRung.TimerPreset = TimeSpan.FromMilliseconds(1000);
                var resetRung = _ladderDocument.Rungs[1];
                ClearOtherOutputs(resetRung);
                resetRung.IsTimer = false;
                resetRung.IsTimerReset = true;
                resetRung.TimerVariable = "process_time";
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var program = _ladderDocument.BuildProgram();
                var compilation = LadderCompiler.Compile(program);
                if (compilation.IsValid && compilation.Program is not null)
                {
                    var runtime = new VirtualControllerRuntime(compilation.Program);
                    runtime.Run();
                    var snapshot = runtime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = true,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    });
                    AttachVirtualController(program, snapshot);
                }
                break;
            }
            case "ladder-project":
            case "ladder-project-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var block = _ladderDocument.Blocks.FirstOrDefault(candidate => candidate.Name == "MotionSequence");
                if (block is null)
                {
                    block = _ladderDocument.AddBlock("MotionSequence");
                    _ladderDocument.SelectBlock(_ladderDocument.Blocks.Count - 1);
                    _ladderDocument.AddRung(logix ? "Start motion sequence" : "Start motion sequence", "conveyor_running");
                    _ladderDocument.AddContact(0, 0, "start_command", false);
                }
                else
                {
                    _ladderDocument.SelectBlock(_ladderDocument.Blocks.IndexOf(block));
                }
                if (_ladderDocument.Tasks.All(task => task.Name != "Periodic100ms"))
                    _ladderDocument.AddTask("Periodic100ms", LadderTaskKind.Periodic,
                        TimeSpan.FromMilliseconds(100), 20, block.Id);
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var projectTabs = GetNodeOrNull<TabContainer>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags");
                if (projectTabs is not null && projectTabs.GetTabCount() > 2) projectTabs.CurrentTab = 2;
                break;
            }
            case "ladder-new-project":
            case "ladder-new-project-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                GetNodeOrNull<MenuButton>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/VendorChrome/VendorMenuBar/LadderProjectMenu")
                    ?.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
                break;
            }
            case "ladder-properties":
            case "ladder-properties-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var ladderCanvas = GetNodeOrNull<LadderEditorCanvas>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView/GraphicalLadderCanvas");
                if (ladderCanvas is not null)
                {
                    ladderCanvas._GuiInput(new InputEventMouseButton
                    {
                        ButtonIndex = MouseButton.Left,
                        Pressed = true,
                        DoubleClick = true,
                        Position = ladderCanvas.GetElementPosition(0, 0, 0),
                    });
                }
                break;
            }
            case "ladder-ab":
                ShowClassicTool("ladder");
                _ladderEnvironmentTabs.CurrentTab = 1;
                break;
            case "ladder-return":
                ShowClassicTool("ladder");
                GetNodeOrNull<Button>(
                    "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette/Program Control/AddReturn")
                    ?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            case "ladder-return-ab":
                ShowClassicTool("ladder");
                _ladderEnvironmentTabs.CurrentTab = 1;
                GetNodeOrNull<Button>(
                    "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette/Program Control/AddReturn")
                    ?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            case "ladder-jump":
            case "ladder-jump-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var block = _ladderDocument.Blocks.FirstOrDefault(candidate => candidate.Name == "JumpSequence");
                if (block is null)
                {
                    block = _ladderDocument.AddBlock("JumpSequence");
                    _ladderDocument.SelectBlock(_ladderDocument.Blocks.Count - 1);
                    var jumpRung = _ladderDocument.AddJumpRung("Skip optional processing", "finish");
                    _ladderDocument.AddContact(0, 0, "stop_command", false);
                    _ladderDocument.AddRung("Intervening logic skipped while condition is true", "seal_in");
                    _ladderDocument.AddContact(1, 0, "start_command", false);
                    _ladderDocument.AddLabelRung("Block-local jump destination", "finish");
                }
                else
                {
                    _ladderDocument.SelectBlock(_ladderDocument.Blocks.IndexOf(block));
                }
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                var reviewEditorRoot =
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/Workbench/EditorAndTasks/EditorAndInspector";
                var reviewPalette = GetNodeOrNull<TabContainer>(
                    $"{reviewEditorRoot}/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette");
                if (reviewPalette is not null)
                    reviewPalette.CurrentTab = reviewPalette.GetTabCount() - 1;
                GetNodeOrNull<Button>(
                    $"{reviewEditorRoot}/BottomDockHost/InspectorOutputDock/InspectorOutputBody/InspectorOutputTitle/CollapseBottomDock")
                    ?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            }
            case "ladder-clipboard":
            case "ladder-clipboard-ab":
            {
                ShowClassicTool("ladder");
                var logix = view.EndsWith("-ab", StringComparison.OrdinalIgnoreCase);
                if (logix) _ladderEnvironmentTabs.CurrentTab = 1;
                var environmentName = logix ? "Studio 5000" : "TIA Portal";
                GetNodeOrNull<MenuButton>(
                    $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environmentName}/VendorChrome/VendorMenuBar/LadderEditMenu")
                    ?.CallDeferred("show_popup");
                break;
            }
            case "ladder-timers":
                ShowClassicTool("ladder");
                GetNodeOrNull<Button>(
                    "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/InstructionToolbarChrome/InstructionToolbar/InstructionPalette/Timers/AddTimerOffDelay")
                    ?.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            case "ladder-search":
                ShowClassicTool("ladder");
                const string searchRoot = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/ToolDockHost/InstructionAndTags/ToolBody/ToolTabsHost/ToolTabs";
                var tabs = GetNodeOrNull<TabContainer>(searchRoot);
                var query = GetNodeOrNull<LineEdit>($"{searchRoot}/Find and cross-reference/ProjectSearchQuery");
                var xref = GetNodeOrNull<Button>($"{searchRoot}/Find and cross-reference/SearchActions/CrossReferenceButton");
                if (tabs is not null) tabs.CurrentTab = 1;
                if (query is not null && search.Length > 0)
                {
                    query.Text = search;
                    xref?.EmitSignal(BaseButton.SignalName.Pressed);
                }
                break;
            case "ladder-help":
                ShowClassicTool("ladder");
                ShowLadderInstructionHelp();
                if (search.Length > 0)
                {
                    const string helpRoot = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/ToolDockHost/InstructionAndTags/ToolBody/ToolTabsHost/ToolTabs/Instruction help";
                    var selector = GetNodeOrNull<OptionButton>($"{helpRoot}/HelpInstructionSelector");
                    if (selector is not null)
                    {
                        var item = Enumerable.Range(0, selector.ItemCount)
                            .FirstOrDefault(index => selector.GetItemMetadata(index).AsString().Equals(search, StringComparison.OrdinalIgnoreCase), -1);
                        if (item >= 0)
                        {
                            selector.Select(item);
                            selector.EmitSignal(OptionButton.SignalName.ItemSelected, item);
                        }
                    }
                }
                break;
            case "virtual-controller":
                ShowClassicTool("virtual-controller");
                break;
        }
    }

    private void BuildInterface()
    {
        var workspace = new Control { Name = "Workspace" };
        workspace.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(workspace);

        var toolbar = PanelContainer("Toolbar", new Color("101a22e8"), new Color("294252"));
        toolbar.AnchorRight = 1.0f;
        toolbar.OffsetLeft = 0;
        toolbar.OffsetTop = 0;
        toolbar.OffsetRight = 0;
        // The brand mark plus row/margins produces an 84 px minimum toolbar.
        // Use that rendered boundary for every dock below it.
        toolbar.OffsetBottom = 84;
        workspace.AddChild(toolbar);
        var toolbarMargin = Margin("ToolbarMargin", 14, 14, 8, 8);
        toolbar.AddChild(toolbarMargin);
        var toolbarRow = new HBoxContainer { Name = "ToolbarRow" };
        toolbarRow.AddThemeConstantOverride("separation", 10);
        toolbarMargin.AddChild(toolbarRow);

        var brandMark = new RungProofMark
        {
            Name = "BrandMark",
            CustomMinimumSize = new Vector2(36, 36),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        toolbarRow.AddChild(brandMark);

        var brand = new HBoxContainer { Name = "BrandWordmark" };
        brand.AddThemeConstantOverride("separation", 0);
        brand.AddChild(Heading("Rung", 20, new Color("e9f4f8")));
        brand.AddChild(Heading("Proof", 20, new Color("16a34a")));
        toolbarRow.AddChild(brand);
        var subtitle = Heading("RUNGPROOF ENGINEERING WORKBENCH", 9, new Color("7fa7ba"));
        subtitle.Name = "BrandSubtitle";
        toolbarRow.AddChild(subtitle);
        toolbarRow.AddChild(VRule());
        toolbarRow.AddChild(VRule());
        var file = TopMenu("FileMenu", "File");
        file.GetPopup().AddItem("Save Workspace As…", 0);
        file.GetPopup().AddItem("Load Last Workspace", 1);
        file.GetPopup().AddSeparator();
        file.GetPopup().AddItem("Open Ladder Agent Project…", 2);
        file.GetPopup().AddItem("Open RungProof Workspace…", 3);
        file.GetPopup().AddItem("New Finite Belt Workspace", 4);
        file.GetPopup().AddItem("New finite pallet roller workspace", 5);
        file.GetPopup().IdPressed += id =>
        {
            if (id == 0) SaveWorkspaceRequested?.Invoke();
            if (id == 1) LoadWorkspaceRequested?.Invoke();
            if (id == 2)
            {
                ShowClassicTool("ladder");
                CallDeferred(nameof(ShowLadderLoadDialog));
            }
            if (id == 3) CallDeferred(nameof(ShowWorkspaceLoadDialog));
            if (id == 4) CreateFiniteBeltWorkspaceRequested?.Invoke();
            if (id == 5) CreateFiniteRollerWorkspaceRequested?.Invoke();
        };
        toolbarRow.AddChild(file);

        var view = TopMenu("ViewMenu", "View");
        var viewItems = new (string Label, string View)[]
        {
            ("Operator Console", "operator"),
            ("Viewer Hierarchy", "viewer"),
            ("Engineering Layout", "engineering"),
            ("Immersive Scenario", "floor"),
            ("Scenario + Ladder Split", "split"),
        };
        for (var index = 0; index < viewItems.Length; index++) view.GetPopup().AddItem(viewItems[index].Label, index);
        view.GetPopup().AddSeparator();
        view.GetPopup().AddItem("Frame Process", 10);
        view.GetPopup().AddItem("Frame Whole Scene", 11);
        view.GetPopup().IdPressed += id =>
        {
            if (id >= 0 && id < viewItems.Length) ShowClassicTool(viewItems[id].View);
            if (id is 10 or 11) FrameSceneRequested?.Invoke(id == 11);
        };
        toolbarRow.AddChild(view);

        var quickTabGroup = new ButtonGroup { AllowUnpress = false };
        _sceneQuickTab = ToolbarButton("SceneQuickTab", "SCENARIO", new Color("355d73"), 92);
        _sceneQuickTab.ToggleMode = true;
        _sceneQuickTab.ButtonGroup = quickTabGroup;
        _sceneQuickTab.TooltipText = "Switch to the scenario/operator view";
        _sceneQuickTab.Pressed += () => SetProductView("operator");
        _logicQuickTab = ToolbarButton("LogicEditorQuickTab", "LOGIC EDITOR", new Color("355d73"), 116);
        _logicQuickTab.ToggleMode = true;
        _logicQuickTab.ButtonGroup = quickTabGroup;
        _logicQuickTab.TooltipText = "Switch to the ladder logic editor";
        _logicQuickTab.Pressed += () => SetProductView("ladder");

        var playback = TopMenu("PlaybackMenu", "Playback");
        playback.GetPopup().AddItem("Run", 0);
        playback.GetPopup().AddItem("Stop", 1);
        playback.GetPopup().AddItem("Reset", 2);
        playback.GetPopup().IdPressed += id =>
        {
            if (id == 0) RunRequested?.Invoke();
            if (id == 1) StopRequested?.Invoke();
            if (id == 2) ResetRequested?.Invoke();
        };
        toolbarRow.AddChild(playback);

        var sceneMenu = TopMenu("SceneMenu", "Scenario");
        sceneMenu.TooltipText = "Browse and open demonstration scenarios";
        sceneMenu.GetPopup().AddItem("Scenario Browser", 0);
        sceneMenu.GetPopup().AddItem("Operator Console", 1);
        sceneMenu.GetPopup().AddSeparator();
        const int scenarioMenuBase = 1000;
        var menuScenes = new List<SceneCatalogEntry>();
        foreach (var group in new[] { 2, 0, 1 })
        {
            if (menuScenes.Count > 0) sceneMenu.GetPopup().AddSeparator();
            sceneMenu.GetPopup().AddItem(ScenarioGroupLabel(group), 900 + group);
            sceneMenu.GetPopup().SetItemDisabled(sceneMenu.GetPopup().GetItemIndex(900 + group), true);
            foreach (var scenario in _orderedScenes.Where(scene => ScenarioSortGroup(scene) == group))
            {
                var menuIndex = menuScenes.Count;
                menuScenes.Add(scenario);
                var menuId = scenarioMenuBase + menuIndex;
                var label = group == 2
                    ? AuthoredDemos[ScenarioDemoIndex(scenario)].Label
                    : scenario.Name;
                sceneMenu.GetPopup().AddItem(label, menuId);
                sceneMenu.GetPopup().SetItemTooltip(
                    sceneMenu.GetPopup().GetItemIndex(menuId),
                    $"Open {scenario.Name} ({scenario.Id})");
            }
        }
        sceneMenu.GetPopup().IdPressed += id =>
        {
            if (id == 0)
            {
                ShowClassicTool("scenes");
                return;
            }
            if (id == 1)
            {
                ShowClassicTool("operator");
                return;
            }
            var scenarioIndex = (int)id - scenarioMenuBase;
            if (scenarioIndex >= 0 && scenarioIndex < menuScenes.Count)
            {
                SetProductView("operator");
                SetWorkspaceStatus($"OPENING SCENARIO · {menuScenes[scenarioIndex].Name}");
                RequestSceneChange(menuScenes[scenarioIndex].Id);
            }
        };
        toolbarRow.AddChild(sceneMenu);

        var tools = TopMenu("ToolsMenu", "Tools");
        _toolsPopup = tools.GetPopup();
        tools.TooltipText = "Open engineering, Ladder, and inspection tools.";
        var toolItems = new (string Label, string Tool)[]
        {
            ("Ladder Logic", "ladder"),
            ("Asset Library", "assets"),
            ("Workspace Editor", "workspace"),
            ("Inspector", "inspector"),
            ("Symbolic Signals", "signals"),
            ("Connections", "connections"),
            ("Virtual Controller Monitor", "virtual-controller"),
            ("Diagnostics", "diagnostics"),
        };
        for (var index = 0; index < toolItems.Length; index++)
            tools.GetPopup().AddItem(toolItems[index].Label, index);
        tools.GetPopup().AddSeparator();
        tools.GetPopup().AddItem("MCP Integration: Enabled", McpToggleMenuId);
        tools.GetPopup().AddItem("MCP & Application Settings…", ApplicationSettingsMenuId);
        tools.GetPopup().IdPressed += id =>
        {
            if (id == McpToggleMenuId)
                SetMcpUiEnabled(!_mcpUiEnabled.ButtonPressed);
            else if (id == ApplicationSettingsMenuId)
                _applicationSettingsDialog.PopupCenteredRatio(0.55f);
            else if (id >= 0 && id < toolItems.Length)
                ShowClassicTool(toolItems[id].Tool);
        };
        toolbarRow.AddChild(tools);

        var plc = TopMenu("PlcMenu", "PLC");
        plc.GetPopup().AddItem("PLC Status", 0);
        plc.GetPopup().AddItem("Virtual Controller Monitor", 1);
        plc.GetPopup().AddSeparator();
        plc.GetPopup().AddCheckItem("Built-in Simulator", 10);
        plc.GetPopup().AddCheckItem("External PLC", 11);
        plc.GetPopup().AddItem("External PLC Settings / Tests…", 12);
        plc.GetPopup().SetItemChecked(plc.GetPopup().GetItemIndex(10), true);
        if (RuntimeBuildPolicy.IsOfflineDemo)
        {
            plc.GetPopup().SetItemDisabled(plc.GetPopup().GetItemIndex(11), true);
            plc.GetPopup().SetItemDisabled(plc.GetPopup().GetItemIndex(12), true);
        }
        plc.GetPopup().IdPressed += id =>
        {
            if (id == 0) ShowClassicTool("connection");
            else if (id == 1) ShowClassicTool("virtual-controller");
            else if (id == 10) SetExternalMode(false);
            else if (id == 11) SetExternalMode(true);
            else if (id == 12) ShowExternalPlcDialog();
        };
        toolbarRow.AddChild(plc);

        var help = TopMenu("HelpMenu", "Help");
        help.GetPopup().AddItem("Simulator Mode and Safety Boundary", 0);
        help.GetPopup().IdPressed += _ =>
        {
            if (_showSafetyNotices.ButtonPressed) ShowSimulatorModeNotice();
        };
        toolbarRow.AddChild(help);
        toolbarRow.AddChild(VRule());
        _sceneTitle = Heading("Loading project…", 17, new Color("bcd3df"));
        _sceneTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _sceneTitle.ClipText = true;
        _sceneTitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        toolbarRow.AddChild(_sceneTitle);
        // Keep the two primary view switches beside the current scene title.
        // The Scenario menu remains the browser; these are direct view tabs.
        toolbarRow.AddChild(_sceneQuickTab);
        toolbarRow.AddChild(_logicQuickTab);

        var statuses = new VBoxContainer { CustomMinimumSize = new Vector2(190, 0) };
        _runtimeStatus = SmallStatus("LOCAL RUNTIME  •  STOPPED", new Color("8aa5b4"));
        _connectionStatus = SmallStatus("PLC  •  DISCONNECTED", new Color("f1aa5b"));
        _cycleStatus = SmallStatus("CYCLE  —  ·  SCAN  —", new Color("7fa7ba"));
        statuses.AddChild(_runtimeStatus);
        statuses.AddChild(_connectionStatus);
        statuses.AddChild(_cycleStatus);
        toolbarRow.AddChild(statuses);

        BuildApplicationSettingsDialog();
        BuildChallengeInterface();
        BuildExternalPlcDialog();

        _workspaceStatus = new Label
        {
            Name = "WorkspaceStatus",
            Text = "RMB orbit · MMB pan · wheel zoom",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _workspaceStatus.AddThemeFontSizeOverride("font_size", 11);
        _workspaceStatus.AddThemeColorOverride("font_color", new Color("7fa7ba"));

        _leftDock = PanelContainer("LeftDock", new Color("101a22e8"), new Color("294252"));
        _leftDock.AnchorBottom = 1.0f;
        _leftDock.OffsetLeft = 0;
        _leftDock.OffsetTop = 70;
        _leftDock.OffsetRight = 212;
        _leftDock.OffsetBottom = -296;
        workspace.AddChild(_leftDock);
        _leftTabs = new TabContainer { Name = "LeftTabs" };
        _leftTabs.AddThemeFontSizeOverride("font_size", 15);
        _leftDock.AddChild(_leftTabs);
        _leftTabs.AddChild(BuildOperatorConsole());
        _leftTabs.AddChild(BuildSceneBrowser());
        _leftTabs.SetTabTitle(0, "Operator");
        _leftTabs.SetTabTitle(1, "Scenarios");
        _leftTabs.AddChild(BuildAssetBrowser());
        _leftTabs.AddChild(BuildWorkspaceEditor());
        _leftTabs.AddChild(BuildViewerBrowser());

        _rightDock = PanelContainer("RightDock", new Color("101a22e8"), new Color("294252"));
        _rightDock.AnchorLeft = 1.0f;
        _rightDock.AnchorRight = 1.0f;
        _rightDock.AnchorBottom = 1.0f;
        _rightDock.OffsetLeft = -216;
        _rightDock.OffsetTop = 70;
        _rightDock.OffsetRight = 0;
        _rightDock.OffsetBottom = -296;
        workspace.AddChild(_rightDock);
        _inspectorTabs = new TabContainer { Name = "InspectorTabs" };
        _inspectorTabs.AddThemeFontSizeOverride("font_size", 15);
        _rightDock.AddChild(_inspectorTabs);
        _inspectorTabs.AddChild(BuildOperatorHealth());
        _inspector = Inspector("Inspector");
        _ioInspector = Inspector("Signals");
        var linkInspector = BuildConnectionEditor();
        _connectionInspector = Inspector("PLC");
        _inspectorTabs.AddChild(_inspector);
        _inspectorTabs.AddChild(BuildDeviceSignalsInspector());
        _inspectorTabs.AddChild(linkInspector);
        _inspectorTabs.AddChild(_connectionInspector);
        _inspectorTabs.AddChild(BuildVirtualControllerPanel());

        _diagnosticsDock = PanelContainer("DiagnosticsDock", new Color("0c141beF"), new Color("294252"));
        _diagnosticsDock.AnchorTop = 1.0f;
        _diagnosticsDock.AnchorRight = 1.0f;
        _diagnosticsDock.AnchorBottom = 1.0f;
        _diagnosticsDock.OffsetLeft = 0;
        _diagnosticsDock.OffsetTop = -288;
        _diagnosticsDock.OffsetRight = 0;
        _diagnosticsDock.OffsetBottom = -50;
        workspace.AddChild(_diagnosticsDock);
        var diagnosticsBody = new VBoxContainer { Name = "DiagnosticsBody" };
        _diagnosticsDock.AddChild(diagnosticsBody);
        var diagnosticsHeader = new HBoxContainer { Name = "DiagnosticsHeader" };
        diagnosticsBody.AddChild(diagnosticsHeader);
        _diagnosticSummary = Heading("PROJECT DIAGNOSTICS", 14, new Color("bcd3df"));
        _diagnosticSummary.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        diagnosticsHeader.AddChild(_diagnosticSummary);
        diagnosticsHeader.AddChild(_workspaceStatus);
        var diagnosticsToggle = new Button { Name = "DiagnosticsToggle", Text = "COLLAPSE", Flat = true };
        diagnosticsToggle.Pressed += () =>
        {
            if (_productView is "operator" or "ladder" or "split")
                _pointsCollapsed = !_pointsCollapsed;
            else _diagnosticsExpanded = !_diagnosticsExpanded;
            ApplyDiagnosticsLayout();
        };
        diagnosticsHeader.AddChild(diagnosticsToggle);
        _operatorPointTables = new HBoxContainer { Name = "OperatorPointTables" };
        _operatorPointTables.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _operatorPointTables.AddThemeConstantOverride("separation", 8);
        _tagWatchPanel = PanelContainer("TagWatch", new Color("0b171deF"), new Color("294252"));
        _tagWatchPanel.CustomMinimumSize = new Vector2(230, 0);
        _tagWatchPanel.Visible = false;
        var watchBody = new VBoxContainer();
        _tagWatchPanel.AddChild(watchBody);
        watchBody.AddChild(SectionLabel("TAG WATCH · CTRL-CLICK"));
        _tagWatchList = new ItemList
        {
            Name = "TagWatchList",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SelectMode = ItemList.SelectModeEnum.Multi,
            AllowReselect = true,
            TooltipText = "Select one or more declared symbolic tags to focus the tables. With none selected, all declared tags remain visible.",
        };
        _tagWatchList.MultiSelected += (_, _) => RefreshOperatorPointTables();
        watchBody.AddChild(_tagWatchList);
        _operatorPointTables.AddChild(_tagWatchPanel);
        var inputPanel = PanelContainer("SimulatorToPlcPoints", new Color("0b171deF"), new Color("294252"));
        inputPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var inputBody = new VBoxContainer();
        inputPanel.AddChild(inputBody);
        _operatorInputHeading = SectionLabel("SIMULATOR → PLC · LOCAL MODEL");
        inputBody.AddChild(_operatorInputHeading);
        _operatorInputPoints = Inspector("SimulatorToPlcPointTable");
        _operatorInputPoints.CustomMinimumSize = new Vector2(0, 32);
        _operatorInputPoints.FitContent = false;
        _operatorInputPoints.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        inputBody.AddChild(_operatorInputPoints);
        var outputPanel = PanelContainer("PlcToSimulatorPoints", new Color("0b171deF"), new Color("294252"));
        outputPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var outputBody = new VBoxContainer();
        outputPanel.AddChild(outputBody);
        _operatorOutputHeading = SectionLabel("PLC → SIMULATOR · LOCAL MODEL");
        outputBody.AddChild(_operatorOutputHeading);
        _operatorOutputPoints = Inspector("PlcToSimulatorPointTable");
        _operatorOutputPoints.CustomMinimumSize = new Vector2(0, 32);
        _operatorOutputPoints.FitContent = false;
        _operatorOutputPoints.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        outputBody.AddChild(_operatorOutputPoints);
        _operatorPointTables.AddChild(inputPanel);
        _operatorPointTables.AddChild(outputPanel);
        diagnosticsBody.AddChild(_operatorPointTables);
        _diagnosticList = new ItemList
        {
            Name = "DiagnosticList",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
        };
        diagnosticsBody.AddChild(_diagnosticList);

        _transportDock = PanelContainer("Transport", new Color("101a22f2"), new Color("294252"));
        _transportDock.AnchorTop = 1.0f;
        _transportDock.AnchorRight = 1.0f;
        _transportDock.AnchorBottom = 1.0f;
        _transportDock.OffsetLeft = 0;
        _transportDock.OffsetTop = -50;
        _transportDock.OffsetRight = 0;
        _transportDock.OffsetBottom = 0;
        workspace.AddChild(_transportDock);
        var transportMargin = Margin("TransportMargin", 10, 10, 6, 6);
        _transportDock.AddChild(transportMargin);
        var transportRow = new HBoxContainer { Name = "TransportRow" };
        transportRow.AddThemeConstantOverride("separation", 7);
        transportMargin.AddChild(transportRow);
        var run = ToolbarButton("RunButton", "RUN", new Color("1f8a58"), 62);
        var stop = ToolbarButton("StopButton", "STOP", new Color("355d73"), 62);
        var reset = ToolbarButton("ResetButton", "RESET", new Color("355d73"), 68);
        run.Pressed += () => RunRequested?.Invoke();
        stop.Pressed += () => StopRequested?.Invoke();
        reset.Pressed += () => ResetRequested?.Invoke();
        transportRow.AddChild(run);
        transportRow.AddChild(stop);
        transportRow.AddChild(reset);
        AppendScanInspectionControls(transportRow);
        var save = ToolbarButton("SaveButton", "SAVE", new Color("3d5868"), 60);
        var load = ToolbarButton("LoadButton", "LOAD", new Color("3d5868"), 60);
        save.Pressed += () =>
        {
            if (IsLadderView) _ladderSaveDialog.PopupCenteredRatio(0.72f);
            else ShowWorkspaceSaveDialog();
        };
        load.Pressed += () =>
        {
            if (IsLadderView) ShowLadderLoadDialog();
            else ShowWorkspaceLoadDialog();
        };
        transportRow.AddChild(save);
        transportRow.AddChild(load);
        transportRow.AddChild(VRule());
        var nowPlaying = Heading("NOW PLAYING", 10, new Color("7fa7ba"));
        transportRow.AddChild(nowPlaying);
        _transportScene = Heading("LOCAL MODEL · STOPPED", 12, new Color("bcd3df"));
        _transportScene.Name = "TransportScene";
        _transportScene.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        transportRow.AddChild(_transportScene);
        _transportScope = SmallStatus("LOCAL ONLY", new Color("f1aa5b"));
        transportRow.AddChild(_transportScope);

        _ladderWorkspace = BuildLadderWorkspace();
        workspace.AddChild(_ladderWorkspace);

        _splitDivider = new Panel
        {
            Name = "SplitViewDivider",
            MouseFilter = Control.MouseFilterEnum.Stop,
            ZIndex = 40,
            Visible = false,
            TooltipText = "Drag to resize the operator and ladder panes",
        };
        _splitDivider.AddThemeStyleboxOverride("panel", BoxStyle(new Color("4f879d"), new Color("9ee8c0")));
        _splitDivider.GuiInput += input =>
        {
            if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
            {
                _draggingSplitDivider = button.Pressed;
                if (_draggingSplitDivider) _splitDivider.AcceptEvent();
            }
            else if (input is InputEventMouseMotion motion && _draggingSplitDivider)
            {
                var width = GetViewport().GetVisibleRect().Size.X;
                if (width > 0.0f)
                {
                    MoveSplitDivider(motion.GlobalPosition.X);
                    _splitDivider.AcceptEvent();
                }
            }
        };
        workspace.AddChild(_splitDivider);

        _selectionMarquee = new Panel
        {
            Name = "SelectionMarquee",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            ZIndex = 100,
        };
        var marqueeStyle = new StyleBoxFlat
        {
            BgColor = new Color("32bfe128"),
            BorderColor = new Color("50d8f4"),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
        };
        _selectionMarquee.AddThemeStyleboxOverride("panel", marqueeStyle);
        workspace.AddChild(_selectionMarquee);

        // The operator console mirrors the established RungProof hierarchy:
        // scene/routine rail, central machine, health/equipment rail, points
        // below, and compact transport. Authoring remains deliberate.
        SetProductView("operator");

        _renameGroupDialog = new ConfirmationDialog
        {
            Name = "RenameGroupDialog",
            Title = "Rename equipment group",
            Exclusive = true,
            MinSize = new Vector2I(420, 150),
        };
        var renameBody = new VBoxContainer
        {
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            OffsetLeft = 16,
            OffsetTop = 12,
            OffsetRight = -16,
            OffsetBottom = -48,
        };
        renameBody.AddChild(Heading("GROUP NAME", 13, new Color("9db4c0")));
        _renameGroupInput = new LineEdit
        {
            Name = "GroupNameInput",
            MaxLength = 64,
            PlaceholderText = "Enter a unique group name",
        };
        renameBody.AddChild(_renameGroupInput);
        _renameGroupDialog.AddChild(renameBody);
        _renameGroupDialog.Confirmed += () =>
        {
            if (_renameGroupId is not null)
                RenameGroupRequested?.Invoke(_renameGroupId, _renameGroupInput.Text);
        };
        AddChild(_renameGroupDialog);

        _pivotDialog = new ConfirmationDialog
        {
            Name = "GroupPivotDialog",
            Title = "Edit group pivot",
            Exclusive = true,
            MinSize = new Vector2I(420, 230),
        };
        var pivotBody = new VBoxContainer
        {
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            OffsetLeft = 16,
            OffsetTop = 12,
            OffsetRight = -16,
            OffsetBottom = -48,
        };
        _centroidPivot = new CheckBox { Name = "CentroidPivot", Text = "Use member centroid" };
        pivotBody.AddChild(_centroidPivot);
        var pivotGrid = new GridContainer { Columns = 2 };
        foreach (var (axis, index) in new[] { ("X", 0), ("Y", 1), ("Z", 2) })
        {
            pivotGrid.AddChild(Heading(axis, 13, new Color("9db4c0")));
            var input = new SpinBox
            {
                Name = $"Pivot{axis}",
                MinValue = -1000,
                MaxValue = 1000,
                Step = 0.01,
                AllowGreater = true,
                AllowLesser = true,
            };
            _pivotInputs[index] = input;
            pivotGrid.AddChild(input);
        }
        _centroidPivot.Toggled += useCentroid =>
        {
            foreach (var input in _pivotInputs) input.Editable = !useCentroid;
        };
        pivotBody.AddChild(pivotGrid);
        _pivotDialog.AddChild(pivotBody);
        _pivotDialog.Confirmed += () =>
        {
            if (_pivotGroupId is not null)
                GroupPivotRequested?.Invoke(_pivotGroupId, _centroidPivot.ButtonPressed
                    ? null
                    : _pivotInputs.Select(input => input.Value).ToArray());
        };
        AddChild(_pivotDialog);

        _simulatorModeDialog = new AcceptDialog
        {
            Name = "SimulatorModeDialog",
            Title = "Simulator mode",
            DialogText = "RungProof starts in local simulation mode.\n\n" +
                         "Start Demo 1, select Run, then press its count button three times: the lamp turns on. Stop removes outputs; Reset restores the initial scene.\n\n" +
                         "The five demos include authored logic. Other exercises may start empty: open Logic Editor, add networks, then use Online > Verify + load offline before Run.\n\n" +
                         (RuntimeBuildPolicy.IsOfflineDemo
                             ? "This offline demo build cannot connect to a physical PLC. Other catalog scenes remain experimental."
                             : "The physical PLC stays disconnected until you configure a guarded connection."),
            Exclusive = true,
            DialogAutowrap = true,
            MinSize = new Vector2I(500, 210),
        };
        _simulatorModeDialog.OkButtonText = "CONTINUE";
        _simulatorModeDialog.AddButton("START DEMO 1", true, "demo-1");
        _simulatorModeDialog.CustomAction += action =>
        {
            if (action != "demo-1") return;
            _simulatorModeDialog.Hide();
            RequestSceneChange(AuthoredDemos[0].SceneId);
        };
        AddChild(_simulatorModeDialog);

        _workspaceFeedbackDialog = new AcceptDialog
        {
            Name = "WorkspaceFeedbackDialog",
            Title = "Workspace",
            Exclusive = true,
            MinSize = new Vector2I(560, 180),
        };
        _workspaceFeedbackDialog.OkButtonText = "OK";
        AddChild(_workspaceFeedbackDialog);

        _workspaceSaveDialog = new FileDialog
        {
            Name = "SaveWorkspaceDialog",
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Title = "Save RungProof workspace",
            CurrentDir = ProjectSettings.GlobalizePath("user://workspaces"),
            CurrentFile = "scene-workspace.rungproof.json",
            Filters = ["*.rungproof.json ; RungProof scene workspace"],
        };
        _workspaceSaveDialog.FileSelected += path =>
            SaveWorkspaceToPathRequested?.Invoke(EnsureWorkspaceExtension(path));
        _workspaceSaveDialog.Canceled += () => WorkspaceReplacementCancelled?.Invoke();
        AddChild(_workspaceSaveDialog);

        _unsavedWorkspaceDialog = new ConfirmationDialog
        {
            Name = "UnsavedWorkspaceDialog", Title = "Unsaved scene workspace",
            OkButtonText = "Discard changes", CancelButtonText = "Cancel",
        };
        _unsavedWorkspaceDialog.AddButton("Save…", true, "save");
        _unsavedWorkspaceDialog.GetLabel().AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _unsavedWorkspaceDialog.GetLabel().CustomMaximumSize = new Vector2(600, -1);
        _unsavedWorkspaceDialog.Confirmed += () => WorkspaceDiscardRequested?.Invoke();
        _unsavedWorkspaceDialog.Canceled += () => WorkspaceReplacementCancelled?.Invoke();
        _unsavedWorkspaceDialog.CustomAction += action =>
        {
            if (action != "save") return;
            _unsavedWorkspaceDialog.Hide();
            ShowWorkspaceSaveDialog();
        };
        AddChild(_unsavedWorkspaceDialog);

        _workspaceLoadDialog = new FileDialog
        {
            Name = "OpenWorkspaceDialog",
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Title = "Open RungProof workspace",
            CurrentDir = ProjectSettings.GlobalizePath("user://workspaces"),
            Filters = ["*.rungproof.json ; RungProof scene workspace"],
        };
        _workspaceLoadDialog.FileSelected += path =>
            LoadWorkspaceFromPathRequested?.Invoke(path);
        AddChild(_workspaceLoadDialog);

        _assetHelpDialog = new AcceptDialog
        {
            Name = "AssetHelpDialog",
            Title = "ASSET HELP",
            Exclusive = false,
            MinSize = new Vector2I(760, 620),
        };
        var helpScroll = new ScrollContainer
        {
            Name = "AssetHelpScroll",
            CustomMinimumSize = new Vector2(720, 520),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        _assetHelpContent = new RichTextLabel
        {
            Name = "AssetHelpContent",
            BbcodeEnabled = false,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(700, 520),
            Text = "Select an asset and choose OPEN ASSET HELP.",
        };
        helpScroll.AddChild(_assetHelpContent);
        _assetHelpDialog.AddChild(helpScroll);
        AddChild(_assetHelpDialog);
    }

    private WorkspaceGroup? SelectedExactGroup()
    {
        var selected = _selectedPlacementIds.ToHashSet(StringComparer.Ordinal);
        return _workspaceGroups.FirstOrDefault(group => group.MemberInstanceIds.Count == selected.Count
            && group.MemberInstanceIds.All(selected.Contains));
    }

    private void OpenRenameGroupDialog()
    {
        var group = SelectedExactGroup();
        if (group is null)
        {
            SetWorkspaceStatus("Rename rejected · select exactly one complete group", isError: true);
            return;
        }
        _renameGroupId = group.Id;
        _renameGroupInput.Text = group.Name;
        _renameGroupDialog.PopupCentered(new Vector2I(420, 150));
        _renameGroupInput.GrabFocus();
        _renameGroupInput.SelectAll();
    }

    public void ShowRenameGroupDialog() => OpenRenameGroupDialog();

    private void OpenPivotDialog()
    {
        var group = SelectedExactGroup();
        if (group is null)
        {
            SetWorkspaceStatus("Pivot rejected · select exactly one complete group", isError: true);
            return;
        }
        _pivotGroupId = group.Id;
        var centroid = group.MemberInstanceIds.Select(id => _workspacePlacements
            .First(item => item.InstanceId == id).Position).Aggregate(new double[3], (sum, position) => new[]
            { sum[0] + position[0], sum[1] + position[1], sum[2] + position[2] });
        centroid = centroid.Select(value => value / group.MemberInstanceIds.Count).ToArray();
        var values = group.Pivot ?? centroid;
        _centroidPivot.ButtonPressed = group.Pivot is null;
        for (var index = 0; index < 3; index++) _pivotInputs[index].Value = values[index];
        foreach (var input in _pivotInputs) input.Editable = !_centroidPivot.ButtonPressed;
        _pivotDialog.PopupCentered(new Vector2I(420, 230));
    }

    public void ShowPivotDialog() => OpenPivotDialog();

    /// <summary>
    /// Announces the local-simulator boundary once at an interactive launch.
    /// Inspector panels remain focused on working scene data after dismissal.
    /// </summary>
    public void ShowSimulatorModeNotice()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        _simulatorModeDialog.PopupCentered(new Vector2I((int)Math.Min(620, viewport.X - 48), (int)Math.Min(360, viewport.Y - 48)));
    }

    public void ShowWorkspaceFeedback(string message, bool isError = false)
    {
        _workspaceFeedbackDialog.Title = isError ? "Workspace load failed" : "Workspace";
        _workspaceFeedbackDialog.DialogText = message;
        _workspaceFeedbackDialog.PopupCentered(new Vector2I(560, 180));
    }

    public void ShowWorkspaceSaveDialog() => _workspaceSaveDialog.PopupCenteredRatio(0.72f);

    public void ShowUnsavedWorkspaceDialog(string error = "")
    {
        _workspaceSaveDialog.Hide();
        _unsavedWorkspaceDialog.DialogText = error +
            "This integrated project has unsaved scene or ladder draft edits.\n\n" +
            "Save the scene layout, links, groups and actual ladder draft together before continuing, discard these edits, or cancel.";
        _unsavedWorkspaceDialog.PopupCentered(new Vector2I(650, 250));
        _unsavedWorkspaceDialog.GetCancelButton().GrabFocus();
    }

    public void HideWorkspaceReplacementDialogs()
    {
        _unsavedWorkspaceDialog.Hide();
        _workspaceSaveDialog.Hide();
    }

    public void RequestSceneChange(string sceneId)
    {
        Action apply = () => SceneRequested?.Invoke(sceneId);
        if (WorkspaceReplacementGuard?.Invoke(apply) != false) apply();
        else SelectActiveScenarioRow();
    }

    private void SelectActiveScenarioRow()
    {
        var index = _scenarioBrowserRows.FindIndex(scene => scene?.Id == _activeScene?.Id);
        _sceneList.DeselectAll();
        if (index >= 0) _sceneList.Select(index);
    }

    public void ShowWorkspaceLoadDialog() => _workspaceLoadDialog.PopupCenteredRatio(0.72f);

    private static string EnsureWorkspaceExtension(string path)
    {
        return path.EndsWith(".rungproof.json", StringComparison.OrdinalIgnoreCase)
            ? path
            : System.IO.Path.ChangeExtension(path, null) + ".rungproof.json";
    }

    /// <summary>
    /// The operator console is the primary RungProof run surface. Scene
    /// selection and authoring are tools; the current scenario and its safe
    /// local-model state stay readable without opening an editor dock.
    /// </summary>
    private Control BuildOperatorConsole()
    {
        var scroll = new ScrollContainer
        {
            Name = "Operator", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        var body = new VBoxContainer { Name = "OperatorContent", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(body);
        body.AddThemeConstantOverride("separation", 8);
        body.AddChild(SectionLabel("SCENARIO"));
        _operatorSceneSummary = Inspector("OperatorSceneSummary");
        _operatorSceneSummary.CustomMinimumSize = new Vector2(0, 118);
        _operatorSceneSummary.FitContent = true;
        _operatorSceneSummary.ScrollActive = false;
        body.AddChild(_operatorSceneSummary);
        body.AddChild(BuildRepeatCycleObservedTrialCard());
        body.AddChild(BuildWorkspaceObservedTrialCard());
        body.AddChild(BuildWorkspaceRollerObservedTrialCard());
        body.AddChild(SectionLabel("DECLARED MACHINE ACTIONS"));
        _operatorActions = new VBoxContainer { Name = "OperatorActions" };
        body.AddChild(_operatorActions);
        body.AddChild(SectionLabel("RUNTIME"));
        _operatorRuntimeSummary = Inspector("OperatorRuntimeSummary");
        _operatorRuntimeSummary.FitContent = true;
        _operatorRuntimeSummary.ScrollActive = false;
        body.AddChild(_operatorRuntimeSummary);
        var ladder = ToolbarButton("OpenLadderButton", "BASIC LOGIC / LADDER", new Color("276b89"));
        ladder.Pressed += () => SetProductView("ladder");
        body.AddChild(ladder);
        var engineering = ToolbarButton("OpenEngineeringButton", "OPEN ENGINEERING VIEW", new Color("355d73"));
        engineering.Pressed += () => SetProductView("engineering");
        body.AddChild(engineering);

        // Scene browsing remains available under TOOLS. This hidden list keeps
        // the original population/verification contract without replacing the
        // current-scene operator rail with a catalog browser.
        _operatorSceneList = new ItemList { Name = "OperatorSceneList", Visible = false };
        body.AddChild(_operatorSceneList);
        return scroll;
    }

    private Control BuildViewerBrowser()
    {
        var body = new VBoxContainer { Name = "Viewer" };
        body.AddThemeConstantOverride("separation", 5);
        body.AddChild(SectionLabel("SITE / SCENE HIERARCHY"));
        _viewerSearch = new LineEdit
        {
            Name = "ViewerSearch",
            PlaceholderText = "Search scenes and assets…",
            ClearButtonEnabled = true,
        };
        _viewerSearch.TextChanged += FilterViewerHierarchy;
        body.AddChild(_viewerSearch);
        _viewerHierarchy = new ItemList
        {
            Name = "ViewerHierarchy",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
        };
        ConfigureSelectableList(_viewerHierarchy);
        _viewerHierarchy.ItemSelected += index => SelectViewerItem((int)index);
        body.AddChild(_viewerHierarchy);
        _viewerReturn = ToolbarButton("ViewerReturnButton", "RETURN TO SCENE", new Color("355d73"));
        _viewerReturn.Visible = false;
        _viewerReturn.Pressed += () => AssetPreviewClosed?.Invoke();
        body.AddChild(_viewerReturn);

        return body;
    }

    private Control BuildOperatorHealth()
    {
        var scroll = new ScrollContainer
        {
            Name = "Health", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        var body = new VBoxContainer { Name = "HealthContent", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(body);
        body.AddThemeConstantOverride("separation", 7);
        body.AddChild(SectionLabel("EVENT HISTORY"));
        _operatorEventHistory = Inspector("OperatorEventHistory");
        _operatorEventHistory.CustomMinimumSize = new Vector2(0, 80);
        _operatorEventHistory.AddThemeFontSizeOverride("normal_font_size", 11);
        body.AddChild(_operatorEventHistory);
        body.AddChild(SectionLabel("SCENE EQUIPMENT"));
        _operatorEquipment = Inspector("OperatorEquipment");
        _operatorEquipment.CustomMinimumSize = new Vector2(0, 136);
        _operatorEquipment.FitContent = true;
        _operatorEquipment.ScrollActive = false;
        body.AddChild(_operatorEquipment);
        body.AddChild(SectionLabel("PLC HEALTH"));
        _operatorHealth = Inspector("OperatorHealth");
        _operatorHealth.FitContent = true;
        _operatorHealth.ScrollActive = false;
        body.AddChild(_operatorHealth);
        return scroll;
    }

    private void SetProductView(string view)
    {
        var enteringEditor = view is "ladder" or "split" && _productView is not ("ladder" or "split");
        var leavingEditor = _productView is "ladder" or "split" && view is not ("ladder" or "split");
        if (enteringEditor)
        {
            _pointsCollapsedBeforeEditor = _pointsCollapsed;
            _pointsCollapsed = true; // Give the working ladder room; I/O remains one click away.
        }
        else if (leavingEditor) _pointsCollapsed = _pointsCollapsedBeforeEditor;
        _productView = view;
        var engineering = view == "engineering";
        var floor = view == "floor";
        var operatorConsole = view == "operator";
        var ladder = view == "ladder";
        var split = view == "split";
        _leftDock.Visible = engineering || operatorConsole;
        _rightDock.Visible = engineering || operatorConsole;
        _diagnosticsDock.Visible = engineering || operatorConsole || split || ladder;
        // Ladder workspace is elevated above the scene in split mode. Raise
        // the shared local-model dock with it so the point tables remain a
        // real bottom region instead of being painted underneath the ladder.
        _diagnosticsDock.ZIndex = split || ladder ? 30 : 0;
        _transportDock.Visible = operatorConsole || ladder || split;
        _ladderWorkspace.Visible = ladder || split;
        // The ladder editor has a substantial minimum width from its existing
        // workbench controls. Give it enough room to stay inside a 1600 px
        // viewport while preserving a useful scene pane on the left.
        _ladderWorkspace.AnchorLeft = split ? _splitDividerRatio : 0.0f;
        _ladderWorkspace.AnchorRight = split ? 1.0f : 1.0f;
        _ladderWorkspace.OffsetLeft = split ? 8.0f : 0.0f;
        _ladderWorkspace.OffsetRight = split ? -8.0f : 0.0f;
        _leftTabs.TabsVisible = engineering;
        _inspectorTabs.TabsVisible = engineering;
        if (engineering)
        {
            _leftDock.AnchorLeft = 0.0f;
            _leftDock.AnchorRight = 0.0f;
            _rightDock.AnchorLeft = 1.0f;
            _rightDock.AnchorRight = 1.0f;
            _leftDock.OffsetTop = 84;
            _leftDock.OffsetRight = 350;
            _leftDock.OffsetBottom = -126;
            _rightDock.OffsetLeft = -428;
            _rightDock.OffsetTop = 84;
            _rightDock.OffsetBottom = -126;
            _diagnosticsDock.OffsetTop = -118;
            _diagnosticsDock.OffsetBottom = -8;
            _leftTabs.CurrentTab = 1;
            _inspectorTabs.CurrentTab = 1;
        }
        else if (operatorConsole)
        {
            _leftDock.AnchorLeft = 0.0f;
            _leftDock.AnchorRight = 0.0f;
            _rightDock.AnchorLeft = 1.0f;
            _rightDock.AnchorRight = 1.0f;
            // Keep both operator docks below the toolbar's actual 84 px
            // minimum height; 70 px cuts into the toolbar row at 125% scale.
            _leftDock.OffsetTop = 84;
            _leftDock.OffsetRight = 290;
            _leftDock.OffsetBottom = -296;
            _rightDock.OffsetLeft = -250;
            _rightDock.OffsetTop = 84;
            _rightDock.OffsetBottom = -296;
            _diagnosticsDock.OffsetTop = -288;
            _diagnosticsDock.OffsetBottom = -58;
            _leftTabs.CurrentTab = 0;
            _inspectorTabs.CurrentTab = 0;
        }
        else if (split)
        {
            // The operator console lives entirely in the left pane. Keep the
            // two information rails inside that pane instead of letting the
            // right rail cover the Ladder workbench.
            _leftDock.AnchorLeft = 0.0f;
            _leftDock.AnchorRight = _splitDividerRatio;
            _leftDock.OffsetLeft = 0;
            _leftDock.OffsetTop = 84;
            _leftDock.OffsetRight = -196;
            _leftDock.OffsetBottom = -296;
            _rightDock.AnchorLeft = _splitDividerRatio;
            _rightDock.AnchorRight = _splitDividerRatio;
            _rightDock.OffsetLeft = -188;
            _rightDock.OffsetTop = 84;
            _rightDock.OffsetRight = -8;
            _rightDock.OffsetBottom = -296;
            _leftTabs.TabsVisible = false;
            _inspectorTabs.TabsVisible = false;
            _leftTabs.CurrentTab = 0;
            _inspectorTabs.CurrentTab = 0;
        }
        ApplySplitLayout();
        _operatorPointTables.Visible = (operatorConsole || split || ladder) && !_pointsCollapsed;
        _diagnosticList.Visible = engineering;
        _diagnosticSummary.Text = operatorConsole || split || ladder
            ? "SCENE I/O POINTS"
            : DiagnosticSummaryText();
        foreach (var item in _productViewButtons)
        {
            item.Value.Modulate = item.Key == view ? new Color("9ee8c0") : Colors.White;
        }
        _sceneQuickTab?.SetPressedNoSignal(!IsLadderView);
        _logicQuickTab?.SetPressedNoSignal(IsLadderView);
        _workspaceStatus.Text = floor
            ? "IMMERSIVE FLOOR · RMB orbit · MMB pan · wheel zoom"
            : engineering
                ? "ENGINEERING SPLIT · authoring and connection review"
                : split
                    ? "SCENE + LADDER SPLIT · live offline logic beside the scene"
                : operatorConsole
                    ? "LOCAL MODEL · symbolic points only · no PLC transport"
                : ladder
                    ? "LOGIC EDITOR · local model points below · offline ladder execution"
                : "CLASSIC VIEW · use TOOLS to open scenes, assets, or inspection";
        _workspaceStatus.AddThemeColorOverride("font_color", new Color("7fa7ba"));
        ProductViewChanged?.Invoke(view);
        ApplyDiagnosticsLayout();
        ApplyChallengeLayout();
        CallDeferred(nameof(ApplyToolbarClearance));
    }

    private float ToolbarClearanceOffset()
    {
        var workspace = GetNodeOrNull<Control>("Workspace");
        var toolbar = GetNodeOrNull<Control>("Workspace/Toolbar");
        if (workspace is null || toolbar is null || toolbar.Size.Y <= 0.0f) return 84.0f;
        return Mathf.Max(84.0f, toolbar.GetGlobalRect().End.Y - workspace.GetGlobalRect().Position.Y + 4.0f);
    }

    private void ApplyToolbarClearance()
    {
        var compact = GetViewport().GetVisibleRect().Size.X < 1400;
        var row = GetNode<HBoxContainer>("Workspace/Toolbar/ToolbarMargin/ToolbarRow");
        row.GetNode<Control>("BrandWordmark").Visible = !compact;
        row.GetNode<Control>("BrandSubtitle").Visible = !compact;
        row.AddThemeConstantOverride("separation", compact ? 6 : 10);
        _sceneTitle.CustomMinimumSize = new Vector2(compact ? 160 : 0, 0);
        var top = ToolbarClearanceOffset();
        _leftDock.OffsetTop = top;
        _rightDock.OffsetTop = top;
        _ladderWorkspace.OffsetTop = top;
        _splitDivider.OffsetTop = top;
        ApplyChallengeLayout();
    }

    private void RefreshViewportLayout()
    {
        ApplySplitLayout();
        ApplyDiagnosticsLayout();
        ApplyChallengeLayout();
        CallDeferred(nameof(ApplyToolbarClearance));
    }

    private void MoveSplitDivider(float x)
    {
        var viewport = GetViewport().GetVisibleRect();
        var minimumRatio = 400.0f / viewport.Size.X;
        var maximumRatio = 1.0f - (_ladderWorkspace.GetCombinedMinimumSize().X + 16.0f) / viewport.Size.X;
        _splitDividerRatio = Mathf.Clamp((x - viewport.Position.X) / viewport.Size.X,
            minimumRatio, Mathf.Max(minimumRatio, maximumRatio));
        ApplySplitLayout();
    }

    private void ApplySplitLayout()
    {
        var split = _productView == "split";
        if (_operatorPointTables.GetNodeOrNull<Control>("SimulatorToPlcPoints") is { } inputPoints)
            inputPoints.SizeFlagsStretchRatio = split ? _splitDividerRatio : 1.0f;
        if (_operatorPointTables.GetNodeOrNull<Control>("PlcToSimulatorPoints") is { } outputPoints)
            outputPoints.SizeFlagsStretchRatio = split ? 1.0f - _splitDividerRatio : 1.0f;
        _splitDivider.Visible = split;
        foreach (var environment in new[] { "TIA Portal", "Studio 5000" })
        {
            var root = $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environment}/Workbench";
            var projectPanel = GetNodeOrNull<Control>($"{root}/ProjectDockHost/ProjectOrganization");
            var toolPanel = GetNodeOrNull<Control>($"{root}/EditorAndTasks/ToolDockHost/InstructionAndTags");
            var compact = split && GetViewport().GetVisibleRect().Size.X < 1900.0f;
            var inspectorRoot = $"{root}/EditorAndTasks/EditorAndInspector";
            if (compact && !_compactDockDefaultsApplied)
            {
                if (projectPanel is not null) projectPanel.Visible = false;
                if (toolPanel is not null) toolPanel.Visible = false;
                if (GetNodeOrNull<HSplitContainer>(root) is { } workSplit) workSplit.SplitOffsets = [30];
                if (GetNodeOrNull<HSplitContainer>($"{root}/EditorAndTasks") is { } taskSplit) taskSplit.SplitOffsets = [600];
                if (GetNodeOrNull<Control>($"{inspectorRoot}/BottomDockHost/InspectorOutputDock") is { } bottomPanel)
                    bottomPanel.Visible = false;
            }
            if (GetNodeOrNull<Control>($"{root}/ProjectDockHost/ReopenProjectDock") is { } projectHandle)
                projectHandle.Visible = projectPanel?.Visible != true;
            if (GetNodeOrNull<Control>($"{root}/EditorAndTasks/ToolDockHost/ReopenToolDock") is { } toolHandle)
                toolHandle.Visible = toolPanel?.Visible != true;
            if (GetNodeOrNull<Control>($"{inspectorRoot}/BottomDockHost/ReopenBottomDock") is { } bottomHandle)
                bottomHandle.Visible = GetNodeOrNull<Control>($"{inspectorRoot}/BottomDockHost/InspectorOutputDock")?.Visible != true;
            var programRoot = $"{inspectorRoot}/ProgramEditor";
            var narrowHeight = IsLadderView && GetViewport().GetVisibleRect().Size.Y < 760;
            // The selection row is redundant with instruction Properties and
            // consumes the routine's last usable rows in a 900px window.
            // Compact it by height as well as by split view, before the
            // container's minimum size can extend below Local Model Points.
            var compactPalette = split || GetViewport().GetVisibleRect().Size.Y < 1000;
            if (narrowHeight && GetNodeOrNull<Control>($"{root}/EditorAndTasks/ToolDockHost") is { Visible: false } toolHost)
            {
                toolHost.Visible = true;
                if (toolPanel is not null) toolPanel.Visible = false;
                if (GetNodeOrNull<Control>($"{root}/EditorAndTasks/ToolDockHost/ReopenToolDock") is { } narrowHandle)
                    narrowHandle.Visible = true;
            }
            if (GetNodeOrNull<Control>($"{programRoot}/InstructionToolbarChrome") is { } palette)
            {
                palette.Visible = !narrowHeight;
                // At a 900 px viewport, two wrapped command rows need 64 px;
                // the category row, spacing, and frame bring the full palette
                // to about 108 px. The old 144 px minimum left too little room
                // for the ladder routine while adding blank space below tools.
                palette.CustomMinimumSize = new Vector2(0, compactPalette ? 112 : 180);
            }
            if (GetNodeOrNull<Control>($"{programRoot}/InstructionToolbarChrome/InstructionToolbar/Selection") is { } selection)
                selection.Visible = !compactPalette;
        }
        if (split && GetViewport().GetVisibleRect().Size.X < 1900.0f) _compactDockDefaultsApplied = true;
        if (GetNodeOrNull<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderHeader") is { } header)
            header.Visible = !(IsLadderView && GetViewport().GetVisibleRect().Size.Y < 760);
        if (!split) return;

        _ladderWorkspace.AnchorLeft = _splitDividerRatio;
        // Keep the operator scene rail narrow. Anchoring its right edge to
        // the divider makes it expand across the entire left pane and hides
        // the 3D scene; the divider belongs only to the ladder pane.
        _leftDock.AnchorRight = 0.0f;
        _leftDock.OffsetRight = 196;
        _rightDock.AnchorLeft = _splitDividerRatio;
        _rightDock.AnchorRight = _splitDividerRatio;
        _splitDivider.AnchorLeft = _splitDividerRatio;
        _splitDivider.AnchorRight = _splitDividerRatio;
        _splitDivider.AnchorTop = 0.0f;
        _splitDivider.AnchorBottom = 1.0f;
        _splitDivider.OffsetLeft = -5;
        _splitDivider.OffsetRight = 5;
        _splitDivider.OffsetTop = ToolbarClearanceOffset();
        _splitDivider.OffsetBottom = -50;

        // Local Model Points is the bottom dock in split mode. Keep the
        // ladder workbench in the space above it instead of letting the
        // workbench continue underneath the dock's opaque panel.
        _ladderWorkspace.OffsetTop = ToolbarClearanceOffset();
        _ladderWorkspace.OffsetBottom = _diagnosticsDock.OffsetTop - 8.0f;

        // The full workbench is deliberately wide in the standalone Logic
        // Editor. In split mode its three internal dividers must start at
        // compact positions so their preferred widths do not force the whole
        // right pane outside the application window.
        foreach (var environment in new[] { "TIA Portal", "Studio 5000" })
        {
            var root = $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environment}/Workbench";
            if (GetNodeOrNull<Control>($"{root}/ProjectDockHost/ProjectOrganization") is { } projectPanel)
                projectPanel.CustomMinimumSize = new Vector2(150, 0);
            if (GetNodeOrNull<Control>($"{root}/EditorAndTasks/ToolDockHost/InstructionAndTags") is { } toolPanel)
                toolPanel.CustomMinimumSize = new Vector2(300, 0);
        }
    }

    /// <summary>
    /// Restores the older RungProof interaction pattern: tools are drawers,
    /// not a permanent HUD. Exactly one tool region is visible at a time.
    /// </summary>
    private void ShowClassicTool(string tool)
    {
        if (tool == "operator" || tool == "engineering" || tool == "floor" || tool == "ladder" || tool == "split")
        {
            SetProductView(tool);
            return;
        }
        SetProductView("engineering");
        switch (tool)
        {
            case "scenes":
                _leftTabs.CurrentTab = 1;
                break;
            case "assets":
                _leftTabs.CurrentTab = 2;
                break;
            case "workspace":
                _leftTabs.CurrentTab = 3;
                break;
            case "viewer":
                _leftTabs.CurrentTab = 4;
                break;
            case "inspector":
                _inspectorTabs.CurrentTab = 1;
                break;
            case "signals":
                _inspectorTabs.CurrentTab = 2;
                break;
            case "connections":
                _inspectorTabs.CurrentTab = 3;
                break;
            case "connection":
                _inspectorTabs.CurrentTab = 4;
                break;
            case "virtual-controller":
                _inspectorTabs.CurrentTab = 5;
                break;
            case "diagnostics":
                _diagnosticList.Visible = true;
                _operatorPointTables.Visible = false;
                break;
            default:
                SetProductView("operator");
                return;
        }
        _workspaceStatus.Text = $"ENGINEERING TOOL · {tool.ToUpperInvariant()} · return with TOOLS > Operator console";
        _workspaceStatus.AddThemeColorOverride("font_color", new Color("7fa7ba"));
    }

    private void RebuildOperatorActions()
    {
        if (_runtime is null || _operatorActions is null) return;
        // Controls must survive between mouse-down and mouse-up. A controller
        // publishes several updates per scan; rebuilding here on every update
        // discarded the pressed button before it could emit Pressed.
        foreach (var child in _operatorActions.GetChildren())
        {
            _operatorActions.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var action in _runtime.GetActions())
        {
            var button = ToolbarButton($"Action_{action.Id.Replace('-', '_')}", action.Label.ToUpperInvariant(), new Color("276b89"));
            button.Pressed += () => SceneActionRequested?.Invoke(action.Id);
            _operatorActions.AddChild(button);
        }
        if (_operatorActions.GetChildCount() == 0)
            _operatorActions.AddChild(SectionLabel("No scene-specific actions declared."));
    }

    private bool _externalPlaybackRunning;
    private string _externalReadinessLabel = "DISCONNECTED";

    public void SetExternalPlaybackState(bool running, string readinessLabel)
    {
        if (_externalPlaybackRunning == running && _externalReadinessLabel == readinessLabel) return;
        _externalPlaybackRunning = running;
        _externalReadinessLabel = readinessLabel;
        if (IsInsideTree()) RefreshRuntimeState();
    }

    private bool ActiveRuntimeRunning => _externalMode
        ? _externalPlaybackRunning
        : _virtualSnapshot is not null
            ? _virtualSnapshot.State == VirtualControllerState.Running
            : _runtime?.IsRunning == true;

    private void RefreshOperatorPanels()
    {
        if (_runtime is null || _activeScene is null || _operatorActions is null) return;
        _transportScene.Text = $"{Escape(IsChallengeDraftActive ? ChallengeDisplayName : _activeSceneName)} · {ScanInspectionRuntimeLabel}";
        _operatorSceneSummary.Text =
            $"[font_size=18][b]{Escape(_activeScene.Name)}[/b][/font_size]\n" +
            $"[color=#9db4c0]{Escape(_activeScene.Description)}[/color]\n\n" +
            $"[color=#7fa7ba]{_activeScene.Equipment.Count} declared equipment items · symbolic local model[/color]";
        var external = _externalMode;
        _operatorRuntimeSummary.Text =
            $"[b]Player[/b]  {(external ? "EXTERNAL PLC" : "BUILT-IN SIMULATOR")}\n" +
            $"[b]State[/b]  {ScanInspectionRuntimeLabel}\n" +
            $"[b]Scene[/b]  {Escape(_activeScene.Id)}\n" +
            $"[b]Points[/b]  {_runtime.Points.Count}\n" +
            $"[b]PLC exchange[/b]  {(external ? _connection.State.ToString().ToUpperInvariant() : "LOCAL MODEL")}";
        var equipment = new StringBuilder();
        equipment.AppendLine($"[b]{Escape(_activeScene.Name)}[/b]");
        foreach (var item in _activeScene.Equipment.Take(8))
            equipment.AppendLine($"• {Escape(item.Label)}");
        if (_activeScene.Equipment.Count > 8)
            equipment.AppendLine($"[color=#7fa7ba]+ {_activeScene.Equipment.Count - 8} more[/color]");
        _operatorEquipment.Text = equipment.ToString();
        var health = new StringBuilder();
        health.AppendLine($"[b]Health[/b]  {_connection.State.ToString().ToUpperInvariant()}");
        if (external)
        {
            health.AppendLine($"Readiness  {Escape(_externalReadinessLabel)}");
            if (_connection is ExternalPlcRuntimeClient { LatestCycle: JsonElement cycle })
            {
                health.AppendLine($"Cycle health  {Escape(cycle.GetProperty("health").GetString() ?? "unknown")} · cycle {cycle.GetProperty("cycle")}");
                var heartbeat = cycle.GetProperty("heartbeat");
                health.AppendLine($"Heartbeat  {Escape(heartbeat.GetProperty("reason").GetString() ?? "unknown")}");
                health.AppendLine($"Echo  {heartbeat.GetProperty("last_echo")} · age {heartbeat.GetProperty("age_ms")} ms");
                var status = cycle.GetProperty("plcStatus");
                foreach (var name in new[] { "simulation_enable", "simulation_comm_ok", "simulation_timeout" })
                    health.AppendLine($"{name}  {(status.TryGetProperty(name, out var value) ? Escape(value.ToString()) : "unavailable")}");
            }
            else
            {
                health.AppendLine(_connection.State == ConnectionState.Connected
                    ? "Heartbeat  awaiting first readback" : "Heartbeat  unavailable (disconnected)");
                health.AppendLine("Echo  unavailable");
            }
            health.AppendLine($"Connection  {_connection.EndpointDescription}");
            health.AppendLine("Timeout  profile watchdog");
        }
        else
        {
            health.AppendLine("Heartbeat  local runtime only");
            health.AppendLine("Echo  none");
            health.AppendLine("Connection  disabled");
            health.AppendLine("Timeout  —");
        }
        _operatorHealth.Text = health.ToString();
        if (_productView == "operator") _diagnosticSummary.Text = "SCENE I/O POINTS";
        RefreshOperatorPointTables();
    }

    private void RefreshOperatorPointTables()
    {
        if (_activeScene is null || _runtime is null) return;
        RefreshTagWatchList();
        var watched = _watchablePointNames
            .Where((_, index) => _tagWatchList.IsSelected(index))
            .ToHashSet(StringComparer.Ordinal);
        var simulatorRows = new List<(string Point, string Type, string Value, string Owner)>();
        var plcRows = new List<(string Point, string Type, string Value, string Owner)>();
        var hasSimulatorPoints = false;
        var hasPlcPoints = false;
        if (_activeScene.Simulation.ValueKind == JsonValueKind.Object
            && _activeScene.Simulation.TryGetProperty("points", out var points)
            && points.ValueKind == JsonValueKind.Array)
        {
            foreach (var point in points.EnumerateArray())
            {
                var name = Text(point, "name");
                var type = Text(point, "type");
                var owner = Text(point, "owner");
                if (name.Length == 0) continue;
                if (watched.Count > 0 && !watched.Contains(name)) continue;
                _runtime.Points.TryGetValue(name, out var value);
                var row = (name, type, DisplayPointValue(value), owner);
                if (owner.Equals("PLC", StringComparison.OrdinalIgnoreCase))
                {
                    plcRows.Add(row);
                    hasPlcPoints = true;
                }
                else
                {
                    simulatorRows.Add(row);
                    hasSimulatorPoints = true;
                }
            }
        }
        _operatorInputPoints.Text = BuildPointTable(simulatorRows, hasSimulatorPoints, "simulator-owned");
        _operatorOutputPoints.Text = BuildPointTable(plcRows, hasPlcPoints, "PLC-owned");
    }

    private static string DisplayPointValue(object? value) => value switch
    {
        IReadOnlyDictionary<string, object> fields => "{" + string.Join("; ", fields.Select(field => field.Key + "=" + DisplayPointValue(field.Value))) + "}",
        // The value contains ordered elements, but no schema lower bound. Avoid
        // inventing indices; the typed leaf rows carry their declared indices.
        IReadOnlyList<object> elements => "[" + string.Join(", ", elements.Select(DisplayPointValue)) + "]",
        double number => number.ToString("G6", System.Globalization.CultureInfo.InvariantCulture),
        float number => number.ToString("G6", System.Globalization.CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "—",
    };

    private static string BuildPointTable(
        IReadOnlyList<(string Point, string Type, string Value, string Owner)> rows,
        bool hasRows,
        string ownerDescription)
    {
        if (!hasRows) return $"[color=#7fa7ba]No declared {ownerDescription} points.[/color]";

        var table = new StringBuilder("[table=4]");
        const string cell = "[cell padding=0,0,14,2]";
        table.Append(cell).Append("[b]POINT[/b][/cell]");
        table.Append(cell).Append("[b]TYPE[/b][/cell]");
        table.Append(cell).Append("[b]VALUE[/b][/cell]");
        table.Append(cell).Append("[b]OWNER[/b][/cell]");
        foreach (var row in rows)
        {
            table.Append(cell).Append(Escape(row.Point)).Append("[/cell]");
            table.Append(cell).Append(Escape(row.Type)).Append("[/cell]");
            table.Append(cell).Append(Escape(row.Value)).Append("[/cell]");
            table.Append(cell).Append(Escape(row.Owner)).Append("[/cell]");
        }
        table.Append("[/table]");
        return table.ToString();
    }

    private void RefreshTagWatchList()
    {
        if (_activeScene?.Simulation.ValueKind != JsonValueKind.Object
            || !_activeScene.Simulation.TryGetProperty("points", out var points)
            || points.ValueKind != JsonValueKind.Array) return;
        var names = points.EnumerateArray()
            .Select(point => Text(point, "name"))
            .Where(name => name.Length > 0)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (names.SequenceEqual(_watchablePointNames, StringComparer.Ordinal)) return;
        var selected = _watchablePointNames
            .Where((_, index) => _tagWatchList.IsSelected(index))
            .ToHashSet(StringComparer.Ordinal);
        _watchablePointNames.Clear();
        _tagWatchList.Clear();
        foreach (var name in names)
        {
            var index = _tagWatchList.ItemCount;
            _tagWatchList.AddItem(name);
            if (selected.Contains(name)) _tagWatchList.Select(index, false);
            _watchablePointNames.Add(name);
        }
    }

    private Control BuildSceneBrowser()
    {
        var body = new VBoxContainer { Name = "Scenes" };
        body.AddThemeConstantOverride("separation", 7);
        body.AddChild(SectionLabel("SCENARIO BROWSER"));
        _sceneList = new ItemList
        {
            Name = "SceneList",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
        };
        ConfigureSelectableList(_sceneList);
        _sceneList.ItemSelected += index =>
        {
            if (index >= 0 && index < _scenarioBrowserRows.Count
                && _scenarioBrowserRows[(int)index] is { } scenario)
                RequestSceneChange(scenario.Id);
        };
        body.AddChild(_sceneList);
        return body;
    }

    private Control BuildAssetBrowser()
    {
        var body = new VBoxContainer { Name = "Assets" };
        body.AddThemeConstantOverride("separation", 7);
        body.AddChild(SectionLabel("ASSET CATALOG"));
        _assetSearch = new LineEdit
        {
            Name = "AssetSearch",
            PlaceholderText = "Search name, category, or tag…",
            ClearButtonEnabled = true,
        };
        _assetSearch.TextChanged += FilterAssets;
        body.AddChild(_assetSearch);
        _assetList = new ItemList
        {
            Name = "AssetList",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
        };
        ConfigureSelectableList(_assetList);
        _assetList.ItemSelected += index => SelectAsset((int)index);
        body.AddChild(_assetList);
        _placeAsset = ToolbarButton("PlaceAssetButton", "+  PLACE SELECTED", new Color("276b89"));
        _placeAsset.Disabled = true;
        _placeAsset.Pressed += () =>
        {
            if (_selectedAsset is not null) AssetPlacementRequested?.Invoke(_selectedAsset);
        };
        body.AddChild(_placeAsset);
        _viewAsset = ToolbarButton("ViewAssetButton", "VIEW 3D PREVIEW", new Color("355d73"));
        _viewAsset.TooltipText = "Isolate the selected catalog GLB for inspection. It does not place or run the asset.";
        _viewAsset.Disabled = true;
        _viewAsset.Pressed += () =>
        {
            if (_selectedAsset is not null) AssetPreviewRequested?.Invoke(_selectedAsset);
        };
        body.AddChild(_viewAsset);
        _assetHelp = ToolbarButton("AssetHelpButton", "OPEN ASSET HELP", new Color("4f6f82"));
        _assetHelp.TooltipText = "Open the generated usage, tag, motion, provenance, and verification guide for the selected asset.";
        _assetHelp.Disabled = true;
        _assetHelp.Pressed += ShowSelectedAssetHelp;
        body.AddChild(_assetHelp);
        _returnFromAssetPreview = ToolbarButton("ReturnFromAssetPreviewButton", "RETURN TO SCENE", new Color("3d5868"));
        _returnFromAssetPreview.TooltipText = "Return to the authored scene without modifying its workspace.";
        _returnFromAssetPreview.Visible = false;
        _returnFromAssetPreview.Pressed += () => AssetPreviewClosed?.Invoke();
        body.AddChild(_returnFromAssetPreview);
        return body;
    }

    private Control BuildWorkspaceEditor()
    {
        var body = new VBoxContainer { Name = "Workspace" };
        body.AddThemeConstantOverride("separation", 6);
        body.AddChild(SectionLabel("WORKSPACE HIERARCHY"));
        _workspaceList = new ItemList
        {
            Name = "WorkspaceList",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
            SelectMode = ItemList.SelectModeEnum.Multi,
        };
        ConfigureSelectableList(_workspaceList);
        _workspaceList.MultiSelected += (_, _) => EmitWorkspaceSelectionSet();
        body.AddChild(_workspaceList);

        var history = new HBoxContainer { Name = "EditorButtons" };
        history.AddThemeConstantOverride("separation", 6);
        _undo = ToolbarButton("UndoButton", "UNDO", new Color("3d5868"), 92);
        _redo = ToolbarButton("RedoButton", "REDO", new Color("3d5868"), 92);
        _undo.Pressed += () => UndoRequested?.Invoke();
        _redo.Pressed += () => RedoRequested?.Invoke();
        history.AddChild(_undo);
        history.AddChild(_redo);
        _arrangeMenu = new MenuButton
        {
            Name = "ArrangeMenu",
            Text = "ARRANGE ▾",
            CustomMinimumSize = new Vector2(120, 36),
        };
        var arrangePopup = _arrangeMenu.GetPopup();
        foreach (var operation in Enum.GetValues<WorkspaceArrangeOperation>())
            arrangePopup.AddItem(operation switch
            {
                WorkspaceArrangeOperation.AlignX => "ALIGN X TO PRIMARY",
                WorkspaceArrangeOperation.AlignY => "ALIGN Y TO PRIMARY",
                WorkspaceArrangeOperation.AlignZ => "ALIGN Z TO PRIMARY",
                WorkspaceArrangeOperation.DistributeX => "DISTRIBUTE X EVENLY",
                WorkspaceArrangeOperation.DistributeY => "DISTRIBUTE Y EVENLY",
                _ => "DISTRIBUTE Z EVENLY",
            }, (int)operation);
        arrangePopup.IdPressed += id => ArrangeSelectionRequested?.Invoke((WorkspaceArrangeOperation)id);
        history.AddChild(_arrangeMenu);
        body.AddChild(history);

        var transformModes = new HBoxContainer { Name = "TransformModes" };
        transformModes.AddThemeConstantOverride("separation", 6);
        var modeGroup = new ButtonGroup();
        foreach (var (name, text, mode) in new[]
                 {
                     ("MoveModeButton", "W  MOVE", WorkspaceTransformMode.Move),
                     ("RotateModeButton", "E  ROTATE", WorkspaceTransformMode.Rotate),
                     ("ScaleModeButton", "R  SCALE", WorkspaceTransformMode.Scale),
                 })
        {
            var button = ToolbarButton(name, text, new Color("355d73"), 96);
            button.ToggleMode = true;
            button.ButtonGroup = modeGroup;
            button.ButtonPressed = mode == WorkspaceTransformMode.Move;
            button.Pressed += () => TransformModeRequested?.Invoke(mode);
            _transformModeButtons[mode] = button;
            transformModes.AddChild(button);
        }
        body.AddChild(transformModes);

        var transformSpace = new HBoxContainer { Name = "TransformSpace" };
        transformSpace.AddThemeConstantOverride("separation", 6);
        var spaceGroup = new ButtonGroup();
        foreach (var (name, text, space) in new[]
                 {
                     ("WorldSpaceButton", "WORLD AXES", WorkspaceTransformSpace.World),
                     ("LocalSpaceButton", "LOCAL AXES", WorkspaceTransformSpace.Local),
                 })
        {
            var button = ToolbarButton(name, text, new Color("314d61"), 145);
            button.ToggleMode = true;
            button.ButtonGroup = spaceGroup;
            button.ButtonPressed = space == WorkspaceTransformSpace.World;
            button.Pressed += () => TransformSpaceRequested?.Invoke(space);
            _transformSpaceButtons[space] = button;
            transformSpace.AddChild(button);
        }
        body.AddChild(transformSpace);

        var objectButtons = new HBoxContainer { Name = "ObjectButtons" };
        objectButtons.AddThemeConstantOverride("separation", 6);
        var duplicate = ToolbarButton("DuplicatePlacementButton", "DUPLICATE", new Color("3d5868"), 92);
        var focus = ToolbarButton("FocusPlacementButton", "FOCUS", new Color("3d5868"), 92);
        var resetTransform = ToolbarButton("ResetPlacementTransformButton", "RESET XFORM", new Color("3d5868"), 112);
        duplicate.Pressed += () =>
        {
            if (_selectedPlacementIds.Count > 0) DuplicateSelectionRequested?.Invoke();
        };
        focus.Pressed += () =>
        {
            if (_selectedPlacementId is not null) FocusPlacementRequested?.Invoke(_selectedPlacementId);
        };
        resetTransform.Pressed += () =>
        {
            if (_selectedPlacementId is not null) ResetPlacementTransformRequested?.Invoke(_selectedPlacementId);
        };
        objectButtons.AddChild(duplicate);
        objectButtons.AddChild(focus);
        objectButtons.AddChild(resetTransform);
        body.AddChild(objectButtons);

        var clipboardButtons = new HBoxContainer { Name = "ClipboardButtons" };
        clipboardButtons.AddThemeConstantOverride("separation", 6);
        var copy = ToolbarButton("CopySelectionButton", "CTRL+C  COPY", new Color("3d5868"), 145);
        var paste = ToolbarButton("PasteSelectionButton", "CTRL+V  PASTE", new Color("3d5868"), 145);
        copy.Pressed += () => CopySelectionRequested?.Invoke();
        paste.Pressed += () => PasteSelectionRequested?.Invoke();
        clipboardButtons.AddChild(copy);
        clipboardButtons.AddChild(paste);
        body.AddChild(clipboardButtons);

        var groupButtons = new HBoxContainer { Name = "GroupButtons" };
        groupButtons.AddThemeConstantOverride("separation", 6);
        var group = ToolbarButton("GroupSelectionButton", "GROUP", new Color("3d5868"), 72);
        var rename = ToolbarButton("RenameGroupButton", "RENAME", new Color("3d5868"), 66);
        var pivot = ToolbarButton("PivotGroupButton", "PIVOT", new Color("314d61"), 82);
        var ungroup = ToolbarButton("UngroupSelectionButton", "UNGROUP", new Color("3d5868"), 88);
        group.Pressed += () => GroupSelectionRequested?.Invoke();
        rename.Pressed += OpenRenameGroupDialog;
        pivot.Pressed += OpenPivotDialog;
        ungroup.Pressed += () => UngroupSelectionRequested?.Invoke();
        groupButtons.AddChild(group);
        groupButtons.AddChild(rename);
        groupButtons.AddChild(pivot);
        groupButtons.AddChild(ungroup);
        body.AddChild(groupButtons);

        var snapSettings = new HBoxContainer { Name = "SnapSettings" };
        snapSettings.AddThemeConstantOverride("separation", 5);
        _snapEnabled = new CheckButton { Name = "SnapEnabled", Text = "SNAP" };
        _positionSnap = CompactSpinBox("PositionSnap", 0.01, 10.0, 0.01, 0.10, " m");
        _rotationSnap = CompactSpinBox("RotationSnap", 1.0, 180.0, 1.0, 15.0, "°");
        _snapEnabled.Toggled += _ => EmitSnapSettings();
        _positionSnap.ValueChanged += _ => EmitSnapSettings();
        _rotationSnap.ValueChanged += _ => EmitSnapSettings();
        snapSettings.AddChild(_snapEnabled);
        snapSettings.AddChild(_positionSnap);
        snapSettings.AddChild(_rotationSnap);
        body.AddChild(snapSettings);

        var grid = new GridContainer { Name = "TransformGrid", Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 5);
        grid.AddThemeConstantOverride("v_separation", 4);
        grid.AddChild(new Label());
        foreach (var axis in new[] { "X", "Y", "Z" }) grid.AddChild(Heading(axis, 12, new Color("7fa7ba")));
        AddTransformRow(grid, "Position", _positionInputs, -1000, 1000, 0.01);
        AddTransformRow(grid, "Rotation", _rotationInputs, -3600, 3600, 1.0);
        AddTransformRow(grid, "Scale", _scaleInputs, 0.01, 100, 0.01);
        body.AddChild(grid);

        _deletePlacement = ToolbarButton("DeletePlacementButton", "DELETE SELECTED", new Color("8c3c3c"));
        _deletePlacement.Pressed += () =>
        {
            if (_selectedPlacementIds.Count > 0) DeleteSelectionRequested?.Invoke();
        };
        body.AddChild(_deletePlacement);
        return body;
    }

    private Control BuildConnectionEditor()
    {
        var scroll = new ScrollContainer
        {
            Name = "Connections",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        var body = new VBoxContainer
        {
            Name = "ConnectionEditorBody",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        body.AddThemeConstantOverride("separation", 5);
        scroll.AddChild(body);
        body.AddChild(SectionLabel("ASSET CONNECTORS"));
        _fromInstance = ConnectionOption("FromInstance");
        _fromConnector = ConnectionOption("FromConnector");
        _toInstance = ConnectionOption("ToInstance");
        _toConnector = ConnectionOption("ToConnector");
        _fromInstance.ItemSelected += _ => RefreshConnectorOptions(_fromInstance, _fromConnector);
        _toInstance.ItemSelected += _ => RefreshConnectorOptions(_toInstance, _toConnector);
        body.AddChild(Field("From object", _fromInstance));
        body.AddChild(Field("From connector", _fromConnector));
        body.AddChild(Field("To object", _toInstance));
        body.AddChild(Field("To connector", _toConnector));
        var createConnector = ToolbarButton("CreateConnectorLink", "CREATE CONNECTOR LINK", new Color("276b89"));
        createConnector.Pressed += RequestConnectorLink;
        body.AddChild(createConnector);
        _connectorLinkList = new ItemList { Name = "ConnectorLinks", CustomMinimumSize = new Vector2(0, 52) };
        _connectorLinkList.ItemSelected += index => ShowConnectorLink((int)index);
        body.AddChild(_connectorLinkList);
        var deleteConnector = ToolbarButton("DeleteConnectorLink", "DELETE LINK", new Color("714141"));
        deleteConnector.Pressed += () =>
        {
            var selected = _connectorLinkList.GetSelectedItems();
            if (selected.Length > 0 && selected[0] < _connectorLinks.Count)
                ConnectorLinkDeleteRequested?.Invoke(_connectorLinks[selected[0]].Id);
        };
        body.AddChild(deleteConnector);

        body.AddChild(SectionLabel("SYMBOLIC SIGNAL MAPPING"));
        AddWorkspaceDriveConnectionButton(body);
        _signalInstance = ConnectionOption("SignalInstance");
        _signal = ConnectionOption("AssetSignal");
        _scenePoint = ConnectionOption("ScenePoint");
        _signalInstance.ItemSelected += _ => RefreshSignalOptions();
        body.AddChild(Field("Object", _signalInstance));
        body.AddChild(Field("Asset signal", _signal));
        body.AddChild(Field("Scene point", _scenePoint));
        var createSignal = ToolbarButton("CreateSignalLink", "CREATE SIGNAL MAPPING", new Color("276b89"));
        createSignal.Pressed += RequestSignalLink;
        body.AddChild(createSignal);
        _signalLinkList = new ItemList { Name = "SignalLinks", CustomMinimumSize = new Vector2(0, 52) };
        _signalLinkList.ItemSelected += index => ShowSignalLink((int)index);
        body.AddChild(_signalLinkList);
        _signalMappingSummary = Heading("0 LIVE · 0 AUTHORED ONLY", 11, new Color("7fa7ba"));
        _signalMappingSummary.Name = "SignalMappingSummary";
        _signalMappingSummary.TooltipText =
            "LIVE INPUT means the scene point currently drives a verified asset controller. " +
            "AUTHORED ONLY means the mapping is saved but has no verified runtime behavior yet.";
        body.AddChild(_signalMappingSummary);
        var deleteSignal = ToolbarButton("DeleteSignalLink", "DELETE MAPPING", new Color("714141"));
        deleteSignal.Pressed += () =>
        {
            var selected = _signalLinkList.GetSelectedItems();
            if (selected.Length > 0 && selected[0] < _signalLinks.Count)
                SignalLinkDeleteRequested?.Invoke(_signalLinks[selected[0]].Id);
        };
        body.AddChild(deleteSignal);
        return scroll;
    }

    private static OptionButton ConnectionOption(string name) => new()
    {
        Name = name,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        FitToLongestItem = false,
        ClipText = true,
    };

    private static SpinBox CompactSpinBox(
        string name,
        double minimum,
        double maximum,
        double step,
        double value,
        string suffix
    ) => new()
    {
        Name = name,
        MinValue = minimum,
        MaxValue = maximum,
        Step = step,
        Value = value,
        Suffix = suffix,
        CustomMinimumSize = new Vector2(84, 30),
    };

    private void EmitSnapSettings()
    {
        if (_snapEnabled is null || _positionSnap is null || _rotationSnap is null) return;
        SnapSettingsChanged?.Invoke(_snapEnabled.ButtonPressed, _positionSnap.Value, _rotationSnap.Value);
    }

    private static Control Field(string label, Control input)
    {
        var row = new HBoxContainer();
        var caption = Heading(label, 12, new Color("9db4c0"));
        caption.CustomMinimumSize = new Vector2(106, 0);
        row.AddChild(caption);
        row.AddChild(input);
        return row;
    }

    private void RequestConnectorLink()
    {
        var fromInstance = SelectedPlacement(_fromInstance);
        var toInstance = SelectedPlacement(_toInstance);
        var from = SelectedConnector(_fromInstance, _fromConnector);
        var to = SelectedConnector(_toInstance, _toConnector);
        if (fromInstance is not null && toInstance is not null && from is not null && to is not null)
            ConnectorLinkRequested?.Invoke(fromInstance, from.Id, toInstance, to.Id);
    }

    private void RequestSignalLink()
    {
        var instance = SelectedPlacement(_signalInstance);
        var signals = SignalsFor(_signalInstance);
        var points = ScenePoints();
        if (instance is null || _signal.Selected < 0 || _signal.Selected >= signals.Count
            || _scenePoint.Selected < 0 || _scenePoint.Selected >= points.Count) return;
        SignalLinkRequested?.Invoke(instance, signals[_signal.Selected].Id, points[_scenePoint.Selected].Name);
    }

    private void AddTransformRow(
        GridContainer grid,
        string label,
        SpinBox[] inputs,
        double minimum,
        double maximum,
        double step
    )
    {
        grid.AddChild(Heading(label, 12, new Color("bcd3df")));
        for (var index = 0; index < 3; index++)
        {
            var input = new SpinBox
            {
                MinValue = minimum,
                MaxValue = maximum,
                Step = step,
                CustomMinimumSize = new Vector2(69, 30),
                AllowGreater = true,
                AllowLesser = true,
            };
            input.ValueChanged += _ => EmitTransformEdit();
            inputs[index] = input;
            grid.AddChild(input);
        }
    }

    public void UpdateWorkspace(
        IReadOnlyList<WorkspacePlacement> placements,
        string? selectedId,
        bool canUndo,
        bool canRedo,
        IReadOnlyList<WorkspaceConnectorLink> connectorLinks,
        IReadOnlyList<WorkspaceSignalLink> signalLinks,
        IReadOnlyDictionary<string, SignalMappingRuntimeStatus>? signalStatuses = null,
        IReadOnlyCollection<string>? selectedIds = null,
        IReadOnlyList<WorkspaceGroup>? groups = null
    )
    {
        var hadWorkspaceSelection = _selectedPlacementIds.Count > 0;
        _workspacePlacements = placements;
        _selectedPlacementId = selectedId;
        _workspaceGroups = groups ?? [];
        _selectedPlacementIds.Clear();
        if (selectedIds is not null)
            _selectedPlacementIds.UnionWith(selectedIds);
        else if (selectedId is not null)
            _selectedPlacementIds.Add(selectedId);
        _workspaceList.Clear();
        _workspaceListSelectionTargets.Clear();
        var groupedIds = new HashSet<string>(_workspaceGroups.SelectMany(group => group.MemberInstanceIds),
            StringComparer.Ordinal);
        void AddGroup(WorkspaceGroup group, int depth)
        {
            var members = group.MemberInstanceIds.Where(id => placements.Any(item => item.InstanceId == id)).ToArray();
            if (members.Length == 0) return;
            var prefix = string.Concat(Enumerable.Repeat("│  ", depth));
            var headerIndex = _workspaceList.ItemCount;
            _workspaceList.AddItem($"{prefix}▾ GROUP  ·  {group.Name}  ·  {members.Length} assets");
            _workspaceListSelectionTargets.Add(members);
            if (members.All(_selectedPlacementIds.Contains)) _workspaceList.Select(headerIndex, false);
            var children = _workspaceGroups.Where(item => item.ParentGroupId == group.Id)
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var child in children) AddGroup(child, depth + 1);
            var childMembers = children.SelectMany(child => child.MemberInstanceIds).ToHashSet(StringComparer.Ordinal);
            foreach (var memberId in members.Where(id => !childMembers.Contains(id)))
            {
                var placement = placements.First(item => item.InstanceId == memberId);
                var asset = _assets.Assets.FirstOrDefault(item => item.Id == placement.AssetId);
                var itemIndex = _workspaceList.ItemCount;
                _workspaceList.AddItem($"{prefix}│  └  {asset?.DisplayName ?? placement.AssetId}");
                _workspaceListSelectionTargets.Add([memberId]);
                if (_selectedPlacementIds.Contains(memberId) && !members.All(_selectedPlacementIds.Contains))
                    _workspaceList.Select(itemIndex, false);
            }
        }
        foreach (var group in _workspaceGroups.Where(item => item.ParentGroupId is null)
                     .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)) AddGroup(group, 0);
        foreach (var placement in placements.Where(item => !groupedIds.Contains(item.InstanceId)))
        {
            var asset = _assets.Assets.FirstOrDefault(item => item.Id == placement.AssetId);
            var itemIndex = _workspaceList.ItemCount;
            _workspaceList.AddItem(asset?.DisplayName ?? placement.AssetId);
            _workspaceListSelectionTargets.Add([placement.InstanceId]);
            if (_selectedPlacementIds.Contains(placement.InstanceId)) _workspaceList.Select(itemIndex, false);
        }
        _undo.Disabled = !canUndo;
        _redo.Disabled = !canRedo;
        _deletePlacement.Disabled = _selectedPlacementIds.Count == 0 || _selectedPlacementIds.Any(id=>id==WorkspaceTemplatePolicy.SelectionId);
        _arrangeMenu.Disabled = _selectedPlacementIds.Count < 2;
        _connectorLinks = connectorLinks;
        _signalLinks = signalLinks;
        _signalStatuses = signalStatuses
            ?? new Dictionary<string, SignalMappingRuntimeStatus>();
        RefreshTransformInputs();
        RefreshConnectionEditor();
        if (_selectedPlacementIds.Count > 1)
        {
            _inspector.Text =
                $"[font_size=20][b]{_selectedPlacementIds.Count} OBJECTS SELECTED[/b][/font_size]\n\n" +
                "[color=#8fc6a5]Batch selection[/color]\n" +
                GroupSelectionSummary() + "\n" +
                "Duplicate, copy, paste, group, or delete the selection. The transform gizmo moves the selection around its shared pivot.";
        }
        else if (selectedId is not null)
        {
            var placement = placements.FirstOrDefault(item => item.InstanceId == selectedId);
            if (placement is not null) RefreshPlacementInspector(placement);
            else _inspector.Text = _sceneInspectorText;
        }
        else if (hadWorkspaceSelection) _inspector.Text = _sceneInspectorText;
    }

    private string GroupSelectionSummary()
    {
        var selected = _selectedPlacementIds.ToHashSet(StringComparer.Ordinal);
        var group = _workspaceGroups.FirstOrDefault(item =>
            item.MemberInstanceIds.Count == selected.Count
            && item.MemberInstanceIds.All(selected.Contains));
        return group is null
            ? "[b]Group[/b]  Not grouped"
            : $"[b]Group[/b]  {Escape(group.Name)}";
    }

    private void EmitWorkspaceSelectionSet()
    {
        var selected = _workspaceList.GetSelectedItems()
            .Where(index => index >= 0 && index < _workspaceListSelectionTargets.Count)
            .SelectMany(index => _workspaceListSelectionTargets[index])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        _selectedPlacementIds.Clear();
        _selectedPlacementIds.UnionWith(selected);
        _selectedPlacementId = selected.LastOrDefault();
        RefreshTransformInputs();
        PlacementSelectionSetRequested?.Invoke(selected);
    }

    public bool SelectFirstWorkspaceHierarchyGroupForVerification()
    {
        if (_workspaceListSelectionTargets.Count == 0 || _workspaceList.ItemCount == 0) return false;
        var expected = _workspaceListSelectionTargets[0].ToHashSet(StringComparer.Ordinal);
        if (expected.Count < 2) return false;
        _workspaceList.DeselectAll();
        _workspaceList.Select(0, false);
        EmitWorkspaceSelectionSet();
        return _selectedPlacementIds.SetEquals(expected);
    }

    private void RefreshTransformInputs()
    {
        _updatingTransformInputs = true;
        var placement = _workspacePlacements.FirstOrDefault(item => item.InstanceId == _selectedPlacementId);
        var enabled = placement is not null;
        foreach (var input in _positionInputs.Concat(_rotationInputs).Concat(_scaleInputs))
            input.Editable = enabled;
        if (placement is not null)
        {
            for (var index = 0; index < 3; index++)
            {
                _positionInputs[index].Value = placement.Position[index];
                _rotationInputs[index].Value = placement.Rotation[index];
                _scaleInputs[index].Value = placement.Scale[index];
            }
        }
        if(placement?.InstanceId==WorkspaceTemplatePolicy.SelectionId)
        {
            _positionInputs[1].Editable=false;
            _rotationInputs[0].Editable=false;_rotationInputs[2].Editable=false;
            foreach(var input in _scaleInputs)input.Editable=false;
        }
        _updatingTransformInputs = false;
    }

    private void EmitTransformEdit()
    {
        if (_updatingTransformInputs || _selectedPlacementId is null) return;
        PlacementTransformRequested?.Invoke(
            _selectedPlacementId,
            Values(_positionInputs),
            Values(_rotationInputs),
            Values(_scaleInputs)
        );
    }

    private void RefreshPlacementInspector(WorkspacePlacement placement)
    {
        if(placement.InstanceId==WorkspaceTemplatePolicy.SelectionId)
        {
            _inspector.Text="[font_size=20][b]EDITABLE TEMPLATE ASSEMBLY[/b][/font_size]\n\n"+
                "Operator bench and its original mounted controls move together.\nEdit X/Z position and yaw in the Workspace tab. Height, tilt and scale are locked.\n\n"+
                "Editing rebuilds and stops the symbolic simulation. Original device IDs and I/O bindings remain.\nIndividual heads and process equipment are locked in this first slice.";
            return;
        }
        var asset = _assets.Assets.FirstOrDefault(item => item.Id == placement.AssetId);
        var name = asset?.DisplayName ?? placement.AssetId;
        _inspector.Text =
            $"[font_size=20][b]{Escape(name)}[/b][/font_size]\n" +
            $"[color=#7fa7ba]{Escape(placement.InstanceId)}[/color]\n\n" +
            $"[b]Asset ID[/b]  {Escape(placement.AssetId)}\n" +
            $"[b]Position[/b]  {placement.Position[0]:0.###}, {placement.Position[1]:0.###}, {placement.Position[2]:0.###} m\n" +
            $"[b]Rotation[/b]  {placement.Rotation[0]:0.#}, {placement.Rotation[1]:0.#}, {placement.Rotation[2]:0.#}°\n" +
            $"[b]Scale[/b]  {placement.Scale[0]:0.###}, {placement.Scale[1]:0.###}, {placement.Scale[2]:0.###}\n\n" +
            "[color=#8fc6a5]Selected workspace object[/color]\n" +
            "Edit its transform in the Workspace tab or click another placed object in the viewport.";
    }

    private static Vector3 Values(SpinBox[] inputs) => new(
        (float)inputs[0].Value,
        (float)inputs[1].Value,
        (float)inputs[2].Value
    );

    private void RefreshConnectionEditor()
    {
        if (_fromInstance is null) return;
        foreach (var option in new[] { _fromInstance, _toInstance, _signalInstance })
        {
            var previous = option.Selected;
            option.Clear();
            foreach (var placement in _workspacePlacements)
            {
                var asset = _assets.Assets.FirstOrDefault(item => item.Id == placement.AssetId);
                option.AddItem(asset?.DisplayName??"Template assembly (no new connections)");
            }
            if (option.ItemCount > 0) option.Select(Math.Clamp(previous, 0, option.ItemCount - 1));
        }
        RefreshConnectorOptions(_fromInstance, _fromConnector);
        RefreshConnectorOptions(_toInstance, _toConnector);
        RefreshSignalOptions();
        _scenePoint.Clear();
        foreach (var point in ScenePoints()) _scenePoint.AddItem($"{point.Name}  [{point.Type}]");

        _connectorLinkList.Clear();
        foreach (var link in _connectorLinks)
            _connectorLinkList.AddItem($"{InstanceName(link.FromInstanceId)}.{link.FromConnectorId}  →  {InstanceName(link.ToInstanceId)}.{link.ToConnectorId}");
        _signalLinkList.Clear();
        foreach (var link in _signalLinks)
        {
            var status = _signalStatuses.GetValueOrDefault(link.Id);
            var label = status?.Label ?? "AUTHORED ONLY";
            _signalLinkList.AddItem($"[{label}]  {InstanceName(link.InstanceId)}.{link.SignalId}  ↔  {link.PointName}");
            _signalLinkList.SetItemTooltip(_signalLinkList.ItemCount - 1,
                status?.Detail ?? "Mapping is persisted but no verified runtime adapter is active.");
        }
        var live = _signalStatuses.Values.Count(status => status.State == SignalMappingRuntimeState.LiveInput);
        var feedback = _signalStatuses.Values.Count(status => status.State == SignalMappingRuntimeState.LiveFeedback);
        var authoredOnly = _signalLinks.Count - live - feedback;
        _signalMappingSummary.Text = feedback > 0
            ? $"{live} LIVE INPUT · {feedback} LIVE FEEDBACK · {authoredOnly} AUTHORED ONLY"
            : $"{live} LIVE INPUT · {authoredOnly} AUTHORED ONLY";
        _signalMappingSummary.AddThemeColorOverride("font_color",
            authoredOnly > 0 ? new Color("f1aa5b") : new Color("65d49a"));
        if (_connectorLinks.Count > 0)
        {
            _connectorLinkList.Select(_connectorLinks.Count - 1);
            ShowConnectorLink(_connectorLinks.Count - 1);
        }
        if (_signalLinks.Count > 0)
        {
            _signalLinkList.Select(_signalLinks.Count - 1);
            ShowSignalLink(_signalLinks.Count - 1);
        }
    }

    private void ShowConnectorLink(int index)
    {
        if (index < 0 || index >= _connectorLinks.Count) return;
        var link = _connectorLinks[index];
        SelectInstance(_fromInstance, link.FromInstanceId);
        RefreshConnectorOptions(_fromInstance, _fromConnector);
        SelectConnector(_fromInstance, _fromConnector, link.FromConnectorId);
        SelectInstance(_toInstance, link.ToInstanceId);
        RefreshConnectorOptions(_toInstance, _toConnector);
        SelectConnector(_toInstance, _toConnector, link.ToConnectorId);
    }

    private void ShowSignalLink(int index)
    {
        if (index < 0 || index >= _signalLinks.Count) return;
        var link = _signalLinks[index];
        SelectInstance(_signalInstance, link.InstanceId);
        RefreshSignalOptions();
        var signals = SignalsFor(_signalInstance);
        for (var signalIndex = 0; signalIndex < signals.Count; signalIndex++)
            if (signals[signalIndex].Id == link.SignalId) _signal.Select(signalIndex);
        var points = ScenePoints();
        for (var pointIndex = 0; pointIndex < points.Count; pointIndex++)
            if (points[pointIndex].Name == link.PointName) _scenePoint.Select(pointIndex);
    }

    private void SelectInstance(OptionButton option, string instanceId)
    {
        for (var index = 0; index < _workspacePlacements.Count; index++)
            if (_workspacePlacements[index].InstanceId == instanceId) option.Select(index);
    }

    private void SelectConnector(OptionButton instance, OptionButton connector, string connectorId)
    {
        var connectors = ConnectorsFor(instance);
        for (var index = 0; index < connectors.Count; index++)
            if (connectors[index].Id == connectorId) connector.Select(index);
    }

    private void RefreshConnectorOptions(OptionButton instanceOption, OptionButton connectorOption)
    {
        connectorOption.Clear();
        foreach (var connector in ConnectorsFor(instanceOption))
            connectorOption.AddItem($"{connector.Id}  [{connector.Kind}]");
    }

    private void RefreshSignalOptions()
    {
        if (_signal is null) return;
        _signal.Clear();
        foreach (var signal in SignalsFor(_signalInstance))
            _signal.AddItem($"{signal.Id}  [{signal.DataType} · {signal.Direction}]");
    }

    private string? SelectedPlacement(OptionButton option) =>
        option.Selected >= 0 && option.Selected < _workspacePlacements.Count
            ? _workspacePlacements[option.Selected].InstanceId
            : null;

    private IReadOnlyList<AssetConnector> ConnectorsFor(OptionButton instanceOption)
    {
        var instance = SelectedPlacement(instanceOption);
        var placement = _workspacePlacements.FirstOrDefault(item => item.InstanceId == instance);
        return placement is null
            ? []
            : _assets.Assets.FirstOrDefault(item => item.Id == placement.AssetId)?.Connectors??[];
    }

    private AssetConnector? SelectedConnector(OptionButton instanceOption, OptionButton connectorOption)
    {
        var connectors = ConnectorsFor(instanceOption);
        return connectorOption.Selected >= 0 && connectorOption.Selected < connectors.Count
            ? connectors[connectorOption.Selected]
            : null;
    }

    private IReadOnlyList<SignalDefinition> SignalsFor(OptionButton instanceOption)
    {
        var instance = SelectedPlacement(instanceOption);
        var placement = _workspacePlacements.FirstOrDefault(item => item.InstanceId == instance);
        return placement is null
            ? []
            : _assets.Assets.FirstOrDefault(item => item.Id == placement.AssetId)?.Signals??[];
    }

    private IReadOnlyList<SceneIoPoint> ScenePoints()
    {
        var result = new List<SceneIoPoint>();
        if (_activeScene?.Simulation.ValueKind != JsonValueKind.Object
            || !_activeScene.Simulation.TryGetProperty("points", out var points)
            || points.ValueKind != JsonValueKind.Array) return result;
        foreach (var point in points.EnumerateArray())
        {
            var name = Text(point, "name");
            var type = Text(point, "type");
            if (name.Length > 0)
                result.Add(new SceneIoPoint(
                    name,
                    type,
                    Text(point, "owner"),
                    Text(point, "role"),
                    Text(point, "purpose"),
                    point.TryGetProperty("aggregate", out var schema) ? PlcAggregates.ParseSchema(schema) : null,
                    point.TryGetProperty("enumMembers", out var members) && members.ValueKind == JsonValueKind.Array
                        ? members.EnumerateArray().Select(member => member.GetString() ?? string.Empty).ToArray() : null));
        }
        return result;
    }

    private string InstanceName(string instanceId)
    {
        var placement = _workspacePlacements.FirstOrDefault(item => item.InstanceId == instanceId);
        if (placement is null) return instanceId;
        var name = _assets.Assets.First(item => item.Id == placement.AssetId).DisplayName;
        var qualifier = name.IndexOf(" - ", StringComparison.Ordinal);
        if (qualifier > 0) name = name[..qualifier];
        return name.Length <= 22 ? name : name[..21] + "…";
    }

    private void ApplyDiagnosticsLayout()
    {
        var modelPointsView = _productView is "operator" or "ladder" or "split";
        var toggle = GetNodeOrNull<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle");
        if (modelPointsView)
        {
            _operatorPointTables.Visible = !_pointsCollapsed;
            if (toggle is not null) toggle.Text = _pointsCollapsed ? "EXPAND" : "COLLAPSE";
            var operatorHeight = _pointsCollapsed ? 38.0f
                : GetViewport().GetVisibleRect().Size.Y < 760 ? 146.0f : 180.0f;
            var fontSize = GetViewport().GetVisibleRect().Size.Y < 760 ? 12 : 14;
            foreach (var table in new[] { _operatorInputPoints, _operatorOutputPoints })
            {
                var fontChanged = table.GetThemeFontSize("normal_font_size") != fontSize;
                table.AddThemeFontSizeOverride("normal_font_size", fontSize);
                table.AddThemeFontSizeOverride("bold_font_size", fontSize);
                if (fontChanged)
                {
                    // Godot retains BBCode table column measurements when only
                    // the theme changes. Reparse so a resized window measures
                    // its columns with the new font instead of wrapping values.
                    var contents = table.Text;
                    table.Text = "";
                    table.Text = contents;
                }
            }
            _diagnosticsDock.OffsetTop = -(operatorHeight + 58.0f);
            _diagnosticsDock.OffsetBottom = -58;
            _leftDock.OffsetBottom = -(operatorHeight + 66.0f);
            _rightDock.OffsetBottom = -(operatorHeight + 66.0f);
            // Stop the ladder pane just above the diagnostics/Scene I/O dock.
            // OffsetTop already accounts for the dock height and its bottom
            // margin; reserving another 54 px leaves a large unused band and
            // unnecessarily shortens the routine viewport.
            _ladderWorkspace.OffsetBottom = _diagnosticsDock.OffsetTop - 8.0f;
            return;
        }
        if (toggle is not null) toggle.Text = _diagnosticsExpanded ? "COLLAPSE" : "EXPAND";
        var engineeringHeight = _diagnosticsExpanded ? 270.0f : 118.0f;
        _diagnosticsDock.OffsetTop = -engineeringHeight;
        _diagnosticsDock.OffsetBottom = -8;
        _leftDock.OffsetBottom = -(engineeringHeight + 8.0f);
        _rightDock.OffsetBottom = -(engineeringHeight + 8.0f);
    }

    private static int ScenarioSortGroup(SceneCatalogEntry scene) =>
        IsAuthoredDemo(scene) ? 2
        : scene.Name.StartsWith("Lab ", StringComparison.OrdinalIgnoreCase) ? 1
        : 0;

    private static string ScenarioGroupLabel(int group) => group switch
    {
        0 => "USER CREATED",
        1 => "LABS",
        _ => "DEMOS",
    };

    private static bool IsAuthoredDemo(SceneCatalogEntry scene) =>
        AuthoredDemos.Any(demo => demo.SceneId.Equals(scene.Id, StringComparison.Ordinal));

    private static string ScenarioDisplayName(SceneDefinition scene)
    {
        var demo = AuthoredDemos.FirstOrDefault(item =>
            item.SceneId.Equals(scene.Id, StringComparison.Ordinal));
        return demo.SceneId is null ? scene.Name : demo.Label;
    }

    private static string ScenarioBrowserDemoLabel(SceneCatalogEntry scene)
    {
        var alias=AuthoredDemos[ScenarioDemoIndex(scene)].Label.Split(" - ",2,StringSplitOptions.None);
        var canonical=scene.Name.Split(" - ",2,StringSplitOptions.None)[0];
        return alias[0]+" / "+canonical+(alias.Length>1?" · "+alias[1]:"");
    }
    public bool VerifyActiveScenarioIdentity()
    {
        var selected=_sceneList.GetSelectedItems();
        if(selected.Length!=1||_activeScene is null)return false;
        var row=selected[0];
        var entry=_scenarioBrowserRows[row];
        if(entry is null||_sceneList.GetItemMetadata(row).AsString()!=_activeScene.Id)return false;
        var expected=IsAuthoredDemo(entry)?ScenarioBrowserDemoLabel(entry):entry.Name;
        return _sceneList.GetItemText(row)==expected;
    }    private static int ScenarioDemoIndex(SceneCatalogEntry scene)
    {
        var index = Array.FindIndex(AuthoredDemos, demo => demo.SceneId.Equals(scene.Id, StringComparison.Ordinal));
        return index >= 0 ? index : int.MaxValue;
    }

    private static int ScenarioSortMajor(SceneCatalogEntry scene) => ParseScenarioNumber(scene).Major;

    private static int ScenarioSortMinor(SceneCatalogEntry scene) => ParseScenarioNumber(scene).Minor;

    private static (int Major, int Minor) ParseScenarioNumber(SceneCatalogEntry scene)
    {
        if (!scene.Name.StartsWith("Lab ", StringComparison.OrdinalIgnoreCase)) return (int.MaxValue, int.MaxValue);
        var label = scene.Name[4..];
        var end = label.IndexOf(' ');
        if (end < 0) end = label.IndexOf('-');
        if (end >= 0) label = label[..end];
        var parts = label.Split('.', 2);
        if (!int.TryParse(parts[0], out var major)) return (int.MaxValue, int.MaxValue);
        if (parts.Length == 1) return (major, 0);
        return int.TryParse(parts[1], out var minor)
            ? (major, minor)
            : (int.MaxValue, int.MaxValue);
    }

    private void PopulateScenes()
    {
        _sceneList.Clear();
        _operatorSceneList.Clear();
        _scenarioBrowserRows.Clear();
        foreach (var group in new[] { 2, 0, 1 })
        {
            _sceneList.AddItem(ScenarioGroupLabel(group));
            _sceneList.SetItemDisabled(_sceneList.ItemCount - 1, true);
            _scenarioBrowserRows.Add(null);
            foreach (var scene in _orderedScenes.Where(candidate => ScenarioSortGroup(candidate) == group))
            {
                var label = group == 2
                    ? ScenarioBrowserDemoLabel(scene)
                    : scene.Name;
                _sceneList.AddItem(label);
                _sceneList.SetItemMetadata(_sceneList.ItemCount - 1,scene.Id);
                _sceneList.SetItemTooltip(_sceneList.ItemCount - 1, $"{scene.Name}\n{scene.Id}");
                _scenarioBrowserRows.Add(scene);
            }
        }
        foreach (var scene in _orderedScenes)
        {
            _operatorSceneList.AddItem(scene.Name);
            _operatorSceneList.SetItemTooltip(_operatorSceneList.ItemCount - 1, $"{scene.Name}\n{scene.Id}");
        }
    }

    private static void ConfigureSelectableList(Control list)
    {
        // Godot's Windows compatibility renderer can omit ItemList's default
        // selected background.  Every simulator browser uses an explicit high-
        // contrast selection contract so selecting a row never makes it vanish.
        list.AddThemeColorOverride("font_selected_color", new Color("ffffff"));
        list.AddThemeStyleboxOverride("selected", BoxStyle(new Color("276b89"), new Color("174f68")));
        list.AddThemeStyleboxOverride("selected_focus", BoxStyle(new Color("276b89"), new Color("174f68")));
        if (list is Tree)
        {
            // Tree draws separate styles while the pointer is over a row.
            // Keep selection readable with and without keyboard focus.
            list.AddThemeColorOverride("font_hovered_color", new Color("263943"));
            list.AddThemeColorOverride("font_hovered_dimmed_color", new Color("263943"));
            list.AddThemeColorOverride("font_hovered_selected_color", new Color("ffffff"));
            list.AddThemeStyleboxOverride("hovered", BoxStyle(new Color("d9e9ef"), new Color("b6cbd4")));
            list.AddThemeStyleboxOverride("hovered_dimmed", BoxStyle(new Color("d9e9ef"), new Color("b6cbd4")));
            list.AddThemeStyleboxOverride("hovered_selected", BoxStyle(new Color("276b89"), new Color("174f68")));
            list.AddThemeStyleboxOverride("hovered_selected_focus", BoxStyle(new Color("276b89"), new Color("174f68")));
        }
    }

    private void FilterAssets(string query)
    {
        if (_assetList is null) return;
        _filteredAssets.Clear();
        var search = query.Trim();
        foreach (var asset in _assets.Assets.Where(asset => Matches(asset, search))
                     .OrderBy(asset => asset.Category, StringComparer.Ordinal)
                     .ThenBy(asset => asset.DisplayName, StringComparer.Ordinal))
        {
            _filteredAssets.Add(asset);
        }
        _assetList.Clear();
        foreach (var asset in _filteredAssets)
        {
            _assetList.AddItem($"{asset.DisplayName}  —  {asset.Category}");
        }
        _selectedAsset = null;
        _placeAsset.Disabled = true;
        _viewAsset.Disabled = true;
        _assetHelp.Disabled = true;
    }

    /// <summary>
    /// The default viewer rail is deliberately flat and filterable, like an
    /// industrial camera/site browser. It exposes both scene destinations and
    /// reusable catalog assets without opening the engineering editor.
    /// </summary>
    private void FilterViewerHierarchy(string query)
    {
        if (_viewerHierarchy is null) return;
        var search = query.Trim();
        _viewerHierarchy.Clear();
        _viewerItems.Clear();
        AddViewerHeading("SCENES");
        foreach (var scene in _sceneCatalog.Scenes
                     .Where(scene => search.Length == 0
                         || scene.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                         || scene.Id.Contains(search, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(scene => scene.Name, StringComparer.OrdinalIgnoreCase))
        {
            AddViewerItem($"◆  {scene.Name}", "scene", scene.Id);
        }
        AddViewerHeading("ASSET LIBRARY");
        foreach (var asset in _assets.Assets
                     .Where(asset => Matches(asset, search))
                     .OrderBy(asset => asset.Category, StringComparer.Ordinal)
                     .ThenBy(asset => asset.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            AddViewerItem($"◇  {asset.DisplayName}", "asset", asset.Id);
        }
    }

    private void AddViewerHeading(string text)
    {
        var index = _viewerHierarchy.ItemCount;
        _viewerHierarchy.AddItem(text);
        _viewerHierarchy.SetItemDisabled(index, true);
        _viewerItems.Add(("heading", string.Empty));
    }

    private void AddViewerItem(string text, string kind, string id)
    {
        _viewerHierarchy.AddItem(text);
        _viewerItems.Add((kind, id));
    }

    private void SelectViewerItem(int index)
    {
        if (index < 0 || index >= _viewerItems.Count) return;
        var item = _viewerItems[index];
        if (item.Kind == "scene")
        {
            AssetPreviewClosed?.Invoke();
            RequestSceneChange(item.Id);
            return;
        }
        if (item.Kind != "asset") return;
        var asset = _assets.Assets.FirstOrDefault(candidate => candidate.Id == item.Id);
        if (asset is null) return;
        _selectedAsset = asset;
        AssetPreviewRequested?.Invoke(asset);
    }

    private static bool Matches(AssetDefinition asset, string query)
    {
        if (query.Length == 0) return true;
        return asset.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || asset.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || asset.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void SelectAsset(int index)
    {
        if (index < 0 || index >= _filteredAssets.Count) return;
        _selectedAsset = _filteredAssets[index];
        _placeAsset.Disabled = false;
        _viewAsset.Disabled = false;
        _assetHelp.Disabled = false;
        var asset = _selectedAsset;
        var text = new StringBuilder();
        text.AppendLine($"[font_size=20][b]{Escape(asset.DisplayName)}[/b][/font_size]");
        text.AppendLine($"[color=#7fa7ba]{Escape(asset.Category)}[/color]");
        text.AppendLine();
        text.AppendLine($"[b]Catalog ID[/b]  {Escape(asset.Id)}");
        text.AppendLine($"[b]Envelope[/b]  {asset.Bounds.WidthM:0.###} × {asset.Bounds.DepthM:0.###} × {asset.Bounds.HeightM:0.###} m");
        text.AppendLine($"[b]Quality gate[/b]  {Escape(asset.Quality.Status)}");
        text.AppendLine($"[b]Recognition[/b]  {(asset.Quality.RecognitionConfidence ?? 0):P0}");
        text.AppendLine();
        text.AppendLine($"[b]Connectors ({asset.Connectors.Count})[/b]");
        foreach (var connector in asset.Connectors)
            text.AppendLine($"  • {Escape(connector.Id)}  [color=#7fa7ba]{Escape(connector.Kind)}[/color]");
        text.AppendLine();
        text.AppendLine($"[b]Signals ({asset.Signals.Count})[/b]");
        foreach (var signal in asset.Signals)
            text.AppendLine($"  • {Escape(signal.Id)}  [color=#7fa7ba]{Escape(signal.DataType)} · {Escape(signal.Direction)}[/color]");
        _inspector.Text = text.ToString();
    }

    private void ShowSelectedAssetHelp()
    {
        if (_selectedAsset is null) return;

        var path = $"res://docs/help/assets/{_selectedAsset.Id}.md";
        _assetHelpDialog.Title = $"ASSET HELP · {_selectedAsset.DisplayName}";
        if (!FileAccess.FileExists(path))
        {
            _assetHelpContent.Text =
                $"Help is not packaged for this asset yet.\n\nExpected document:\n{path}";
        }
        else
        {
            // Keep the generated Markdown visible as plain text. This avoids
            // interpreting catalog descriptions or source URLs as UI markup.
            _assetHelpContent.Text = FileAccess.GetFileAsString(path);
        }
        _assetHelpDialog.PopupCenteredRatio(0.82f);
    }

    /// <summary>
    /// Keeps the catalog controls honest about whether the center viewport is
    /// showing the authored scene or an isolated, read-only model preview.
    /// </summary>
    public void SetAssetPreviewState(AssetDefinition? asset)
    {
        var previewing = asset is not null;
        _returnFromAssetPreview.Visible = previewing;
        _viewerReturn.Visible = previewing;
        _placeAsset.Disabled = previewing || _selectedAsset is null;
        _viewAsset.Disabled = previewing || _selectedAsset is null;
        _assetHelp.Disabled = previewing || _selectedAsset is null;
        if (previewing)
        {
            _workspaceStatus.Text = $"ASSET PREVIEW · {asset!.DisplayName} · read-only model inspection";
            _workspaceStatus.AddThemeColorOverride("font_color", new Color("8fc6a5"));
        }
        else
        {
            _workspaceStatus.Text = "VIEWER · searchable hierarchy · symbolic local model";
            _workspaceStatus.AddThemeColorOverride("font_color", new Color("7fa7ba"));
        }
    }

    private void RefreshSceneInspector(SceneComposition composition)
    {
        if (_activeScene is null) return;
        var scene = _activeScene;
        var text = new StringBuilder();
        text.AppendLine($"[font_size=20][b]{Escape(scene.Name)}[/b][/font_size]");
        text.AppendLine($"[color=#7fa7ba]{Escape(scene.Id)}[/color]");
        text.AppendLine();
        text.AppendLine(Escape(scene.Description));
        text.AppendLine();
        text.AppendLine($"[b]Equipment[/b]  {scene.Equipment.Count}");
        text.AppendLine($"[b]Rendered[/b]  {composition.RenderedEquipmentIds.Count}");
        text.AppendLine($"[b]Deferred[/b]  {composition.DeferredEquipmentIds.Count}");
        text.AppendLine($"[b]Source[/b]  {Escape(scene.Migration.SourceFile)}");
        text.AppendLine($"[b]Visual mapping[/b]  {Escape(scene.Migration.VisualStatus)}");
        if (composition.DeferredEquipmentIds.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"[color=#ef9f55][b]Deferred objects[/b][/color]");
            foreach (var id in composition.DeferredEquipmentIds) text.AppendLine($"  • {Escape(id)}");
        }
        _sceneInspectorText = text.ToString();
        _inspector.Text = _sceneInspectorText;
    }

    private void RefreshIoInspector()
    {
        if (_activeScene is null || _runtime is null) return;
        var text = new StringBuilder();
        text.AppendLine("[font_size=18][b]SYMBOLIC POINTS[/b][/font_size]");
        text.AppendLine();
        foreach (var point in _runtime.Points.OrderBy(point => point.Key, StringComparer.Ordinal))
            text.AppendLine($"• {Escape(point.Key)}  [color=#8fc6a5]{Escape(DisplayPointValue(point.Value))}[/color]");
        text.AppendLine();
        text.AppendLine("[font_size=16][b]RENDERER BINDINGS[/b][/font_size]");
        if (_activeScene.Simulation.ValueKind == JsonValueKind.Object
            && _activeScene.Simulation.TryGetProperty("pointBindings", out var bindings)
            && bindings.ValueKind == JsonValueKind.Array)
        {
            foreach (var binding in bindings.EnumerateArray())
            {
                text.AppendLine($"• {Escape(Text(binding, "point"))} → {Escape(Text(binding, "equipmentId"))}  [color=#7fa7ba]{Escape(Text(binding, "mode"))}[/color]");
            }
        }
        AppendDeviceTrace(text);
        _ioInspector.Text = text.ToString();
    }

    private void RefreshRuntimeState()
    {
        if (_runtime is null) return;
        _transportScope.Text = _externalMode ? "EXTERNAL PLC SOURCE" : "LOCAL ONLY";
        _operatorInputHeading.Text = _externalMode ? "SIMULATOR → PLC · GUARDED EXCHANGE" : "SIMULATOR → PLC · LOCAL MODEL";
        _operatorOutputHeading.Text = _externalMode ? "PLC → SIMULATOR · GUARDED EXCHANGE" : "PLC → SIMULATOR · LOCAL MODEL";
        if (_externalMode)
        {
            _cycleStatus.Text = _connection is ExternalPlcRuntimeClient { LatestCycle: JsonElement cycle }
                ? $"EXCHANGE  {cycle.GetProperty("cycle")}  ·  {cycle.GetProperty("health").GetString()?.ToUpperInvariant()}"
                : "EXCHANGE  —  ·  AWAITING READBACK";
            _cycleStatus.AddThemeColorOverride("font_color", new Color("7fa7ba"));
        }
        _runtimeStatus.Text = _externalMode
            ? $"EXTERNAL PLC  •  {_connection.State.ToString().ToUpperInvariant()}  •  {(ActiveRuntimeRunning ? "PLAYING" : "PAUSED")}"
            : ActiveRuntimeRunning
            ? (_scanInspectionPaused ? "LOCAL RUNTIME  •  PAUSED" : "LOCAL RUNTIME  •  RUNNING")
            : "LOCAL RUNTIME  •  STOPPED";
        _runtimeStatus.AddThemeColorOverride("font_color",
            ActiveRuntimeRunning ? new Color("65d49a") : new Color("8aa5b4"));
        RefreshIoInspector();
        RefreshOperatorPanels();
        RefreshOperatorPointTables();
    }

    private PanelContainer BuildLadderWorkspace()
    {
        _ladderSavedProjectJson ??= LadderEditorProjectJson.Save(_ladderDocument);
        var panel = PanelContainer("LadderWorkspace", new Color("0b141bf8"), new Color("365666"));
        panel.AnchorRight = 1.0f;
        panel.AnchorBottom = 1.0f;
        // Match the toolbar's full logical height so the ladder workspace also
        // starts below the toolbar when it is shown.
        panel.OffsetTop = 84;
        panel.OffsetBottom = -50;
        panel.Visible = false;
        panel.ZIndex = 20;

        var margin = Margin("LadderMargin", 14, 14, 10, 10);
        panel.AddChild(margin);
        var body = new VBoxContainer { Name = "LadderBody" };
        body.AddThemeConstantOverride("separation", 8);
        margin.AddChild(body);

        var header = new HBoxContainer { Name = "LadderHeader" };
        var heading = Heading("LADDER LOGIC PROGRAMMING", 18, new Color("e9f4f8"));
        heading.ClipText = true;
        heading.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        heading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(heading);
        _ladderWorkspaceStatus = Heading(
            "OFFLINE LD RUNTIME · VALIDATED BEFORE LOAD · NO PHYSICAL PLC",
            11,
            new Color("f1aa5b"));
        _ladderWorkspaceStatus.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _ladderWorkspaceStatus.ClipText = true;
        _ladderWorkspaceStatus.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _ladderWorkspaceStatus.HorizontalAlignment = HorizontalAlignment.Center;
        header.AddChild(_ladderWorkspaceStatus);
        var close = ToolbarButton("CloseLadderButton", "RETURN TO SCENE", new Color("355d73"), 148);
        close.Pressed += () => SetProductView("operator");
        header.AddChild(close);
        body.AddChild(header);

        _ladderEnvironmentTabs = new TabContainer
        {
            Name = "LadderEnvironmentTabs",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _ladderEnvironmentTabs.AddThemeFontSizeOverride("font_size", 15);
        _ladderEnvironmentTabs.AddChild(BuildLadderEnvironment("TIA Portal", true));
        _ladderEnvironmentTabs.AddChild(BuildLadderEnvironment("Studio 5000", false));
        _ladderEnvironmentTabs.TabChanged += _ =>
        {
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        };
        body.AddChild(_ladderEnvironmentTabs);

        _ladderSaveDialog = new FileDialog
        {
            Name = "SaveLadderProjectDialog",
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Title = "Save offline Ladder project",
            Filters = ["*.rpproj.json ; RungProof editable Ladder project"],
            CurrentFile = "offline-controller.rpproj.json",
        };
        _ladderSaveDialog.FileSelected += path =>
        {
            if (TrySaveLadderProject(path, out var message))
            {
                _ladderWorkspaceStatus.Text = message;
                _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("65d49a"));
            }
            else
            {
                _ladderWorkspaceStatus.Text = message;
                _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("ef7777"));
            }
        };
        panel.AddChild(_ladderSaveDialog);

        _ladderLoadDialog = new FileDialog
        {
            Name = "OpenLadderProjectDialog",
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Title = "Open offline Ladder project",
            CurrentDir = ProjectSettings.GlobalizePath("res://artifacts/agent-evidence"),
            Filters = ["*.rpproj.json ; RungProof editable Ladder project"],
        };
        _ladderLoadDialog.FileSelected += path =>
        {
            if (TryOpenLadderProject(path, out var message))
            {
                _ladderWorkspaceStatus.Text = message;
                _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("f1aa5b"));
            }
            else
            {
                _ladderWorkspaceStatus.Text = message;
                _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("ef7777"));
            }
        };
        panel.AddChild(_ladderLoadDialog);
        _unsavedLadderDialog = new ConfirmationDialog
        {
            Name = "UnsavedLadderDialog", Title = "Unsaved ladder work",
            OkButtonText = "Discard changes", CancelButtonText = "Cancel",
        };
        _unsavedLadderDialog.AddButton("Save…", true, "save");
        _unsavedLadderDialog.GetLabel().AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _unsavedLadderDialog.GetLabel().CustomMaximumSize = new Vector2(600, -1);
        _unsavedLadderDialog.Confirmed += CompletePendingLadderAction;
        _unsavedLadderDialog.Canceled += CancelPendingLadderAction;
        _unsavedLadderDialog.CustomAction += action =>
        {
            if (action != "save") return;
            _unsavedLadderDialog.Hide();
            ShowNextPendingDraftSave();
        };
        panel.AddChild(_unsavedLadderDialog);
        _pendingDraftSaveDialog = new FileDialog
        {
            Name = "SavePendingDraftDialog", FileMode = FileDialog.FileModeEnum.SaveFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = ["*.rpproj.json ; RungProof editable Ladder project"],
        };
        _pendingDraftSaveDialog.FileSelected += SaveNextPendingDraft;
        _pendingDraftSaveDialog.Canceled += CancelPendingLadderAction;
        panel.AddChild(_pendingDraftSaveDialog);
        return panel;
    }

    private static string DraftJson(SceneLadderDraft draft)
    {
        var document = new LadderEditorDocument();
        document.RestoreSnapshot(draft.Document);
        return LadderEditorProjectJson.Save(document);
    }

    private static bool DraftHasUnsavedChanges(SceneLadderDraft draft)
    {
        var document = new LadderEditorDocument();
        document.RestoreSnapshot(draft.Document);
        return LadderEditorProjectJson.HasUnsavedChanges(document, draft.SavedJson);
    }

    public void RequestWindowClose()
    {
        Action close = () => RequestLadderAction(() => GetTree().Quit(), allScenes: true);
        if (ChallengeCleanupGuard?.Invoke(close) == false) return;
        if (WorkspaceReplacementGuard?.Invoke(close) != false) close();
    }

    private void RequestLadderAction(Action action, bool allScenes = false)
        => ResolvePendingEditorForms(() => RequestResolvedLadderAction(action, allScenes));

    private void RequestResolvedLadderAction(Action action, bool allScenes)
    {
        if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
        if (_pendingLadderAction is not null) return;
        var currentId = _activeScene?.Id ?? string.Empty;
        var current = CaptureSceneLadderDraft();
        if (DraftHasUnsavedChanges(current)) _pendingDraftSaves.Enqueue((currentId, current));
        if (allScenes)
            foreach (var entry in _sceneLadderDrafts)
                if (entry.Key != currentId && DraftHasUnsavedChanges(entry.Value))
                    _pendingDraftSaves.Enqueue((entry.Key, entry.Value));
        if (_pendingDraftSaves.Count == 0) { action(); return; }
        _pendingLadderAction = action;
        ShowUnsavedLadderDialog();
    }

    private void ShowUnsavedLadderDialog(string error = "")
    {
        _unsavedLadderDialog.DialogText = error +
            $"{_pendingDraftSaves.Count} unsaved ladder project(s).\n" +
            string.Join("\n", _pendingDraftSaves.Take(5).Select(target => target.SceneId)) +
            (_pendingDraftSaves.Count > 5 ? $"\n… and {_pendingDraftSaves.Count - 5} more" : string.Empty) +
            "\n\nSave each project before continuing, discard these changes, or cancel.\n" +
            "Saving preserves work in progress even when ladder verification fails.";
        // The headless display has no desktop bounds to center against.
        // Keep the actual dialog visible for guard verification without asking
        // Godot to spawn it at a negative desktop position. Interactive windows
        // retain the normal centered dialog and the same Save/Discard/Cancel path.
        if (DisplayServer.GetName() == "headless")
            _unsavedLadderDialog.Popup(new Rect2I(0, 0, 650, 320));
        else
            _unsavedLadderDialog.PopupCentered(new Vector2I(650, 320));
        _unsavedLadderDialog.GetCancelButton().GrabFocus();
    }

    private void CancelPendingLadderAction()
    {
        _pendingLadderAction = null;
        _pendingDraftSaves.Clear();
        _unsavedLadderDialog.Hide();
        _pendingDraftSaveDialog.Hide();
    }

    private void CompletePendingLadderAction()
    {
        var action = _pendingLadderAction;
        CancelPendingLadderAction();
        action?.Invoke();
    }

    private void ShowNextPendingDraftSave()
    {
        if (_pendingDraftSaves.Count == 0) { CompletePendingLadderAction(); return; }
        var target = _pendingDraftSaves.Peek();
        _pendingDraftSaveDialog.Title = $"Save unsaved ladder - {target.SceneId} ({_pendingDraftSaves.Count} remaining)";
        if (target.Draft.ProjectPath is not null) _pendingDraftSaveDialog.CurrentPath = target.Draft.ProjectPath;
        else _pendingDraftSaveDialog.CurrentFile =
            string.Concat(target.SceneId.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '_'))
            + ".rpproj.json";
        _pendingDraftSaveDialog.PopupCenteredRatio(0.72f);
    }

    private void SaveNextPendingDraft(string path)
    {
        if (_pendingLadderAction is null || _pendingDraftSaves.Count == 0) return;
        _pendingDraftSaveDialog.Hide();
        var target = _pendingDraftSaves.Peek();
        try
        {
            // Save the queued snapshot directly. Saving a cached scene must
            // neither switch the machine nor load its controller program.
            var document = new LadderEditorDocument();
            document.RestoreSnapshot(target.Draft.Document);
            document.SourceSceneId = target.SceneId;
            var json = LadderEditorProjectJson.Save(document);
            var fullPath = System.IO.Path.GetFullPath(path);
            WriteLadderProjectFile(fullPath, json);
            if (target.SceneId == _activeScene?.Id)
            {
                _ladderDocument.SourceSceneId = target.SceneId;
                _ladderSavedProjectJson = json;
                _ladderProjectPath = fullPath;
                foreach (var refresh in _ladderEditorRefreshers) refresh();
            }
            else _sceneLadderDrafts[target.SceneId] = target.Draft with
            { Document = document.CaptureSnapshot(), SavedJson = json, ProjectPath = fullPath };
            _pendingDraftSaves.Dequeue();
            ShowNextPendingDraftSave();
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException or ArgumentException)
        {
            // A failed Save cannot authorize the destructive action.
            ShowUnsavedLadderDialog("Save failed: " + exception.Message + "\n\n");
        }
    }

    private bool TrySaveLadderProject(string path, out string message)
    {
        try
        {
            _ladderDocument.SourceSceneId = _activeScene?.Id ?? string.Empty;
            var projectJson = LadderEditorProjectJson.Save(_ladderDocument);
            WriteLadderProjectFile(path, projectJson);
            _ladderProjectPath = System.IO.Path.GetFullPath(path);
            _ladderSavedProjectJson = projectJson;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            message = $"PROJECT SAVED · {System.IO.Path.GetFileName(path)} · WORK IN PROGRESS PRESERVED";
            return true;
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException)
        {
            message = "PROJECT SAVE FAILED · " + exception.Message.ToUpperInvariant();
            return false;
        }
    }

    private static void WriteLadderProjectFile(string path, string json)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var preserveOriginal = false;
        if (System.IO.File.Exists(fullPath))
        {
            var existing = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(fullPath));
            if (!existing.IsReadable)
                throw new System.IO.InvalidDataException("Existing project cannot be safely read; choose a new Save As path.");
            preserveOriginal = existing.SourceSchemaVersion < LadderEditorProjectJson.SchemaVersion;
        }
        LadderProjectFileStore.SaveValidated(fullPath, json, content =>
        {
            if (!LadderEditorProjectJson.Load(content).IsReadable)
                throw new System.IO.InvalidDataException("Project validation failed; original file was preserved.");
        }, preserveOriginal);
    }

    public bool VerifyUnsavedWorkGuard(out string result)
    {
        var original = CaptureSceneLadderDraft();
        var originalDrafts = _sceneLadderDrafts.ToArray();
        var originalScene = _activeScene?.Id ?? string.Empty;
        var pathA = System.IO.Path.Combine(OS.GetUserDataDir(), $"guard-current-{Guid.NewGuid():N}.rpproj.json");
        var pathB = System.IO.Path.Combine(OS.GetUserDataDir(), $"guard-cached-{Guid.NewGuid():N}.rpproj.json");
        try
        {
            _sceneLadderDrafts.Clear();
            _ladderDocument.Rungs[0].Label = "Current unsaved guard review";
            var cached = LadderEditorDocument.CreateConveyorExample();
            cached.SourceSceneId = "lab-2-01-workstation-call";
            var cachedBaseline = LadderEditorProjectJson.Save(cached);
            cached.Rungs[0].Label = "Cached unsaved guard review";
            _sceneLadderDrafts[cached.SourceSceneId] = new(cached.CaptureSnapshot(), cachedBaseline, null, new LadderEditorHistory(150));
            var currentJson = LadderEditorProjectJson.Save(_ladderDocument);
            var actions = 0;
            RequestLadderAction(() => actions++, allScenes: true);
            var detectsAll = _unsavedLadderDialog.Visible && _pendingDraftSaves.Count == 2;
            _unsavedLadderDialog.EmitSignal(AcceptDialog.SignalName.Canceled);
            var cancelPreserves = actions == 0 && _pendingLadderAction is null
                && LadderEditorProjectJson.Save(_ladderDocument) == currentJson;
            RequestLadderAction(() => actions++, allScenes: true);
            _unsavedLadderDialog.EmitSignal(AcceptDialog.SignalName.CustomAction, "save");
            SaveNextPendingDraft(OS.GetUserDataDir()); // Directory target must fail without proceeding.
            var failureBlocks = actions == 0 && _pendingDraftSaves.Count == 2 && _unsavedLadderDialog.Visible;
            _unsavedLadderDialog.Hide();
            _pendingDraftSaveDialog.Hide();
            SaveNextPendingDraft(pathA);
            var waitsForAll = actions == 0 && _pendingDraftSaves.Count == 1;
            _pendingDraftSaveDialog.Hide();
            SaveNextPendingDraft(pathB);
            var savedCurrent = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(pathA));
            var savedCached = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(pathB));
            var savesAll = actions == 1 && _pendingLadderAction is null
                && _activeScene?.Id == originalScene
                && savedCurrent.Document?.Rungs[0].Label == "Current unsaved guard review"
                && savedCurrent.Document.SourceSceneId == originalScene
                && savedCached.Document?.Rungs[0].Label == "Cached unsaved guard review"
                && savedCached.Document.SourceSceneId == cached.SourceSceneId
                && _ladderSavedProjectJson == LadderEditorProjectJson.Save(_ladderDocument)
                && _sceneLadderDrafts[cached.SourceSceneId].SavedJson == DraftJson(_sceneLadderDrafts[cached.SourceSceneId]);
            _ladderDocument.Rungs[0].Label += " discard";
            RequestLadderAction(() => actions++);
            _unsavedLadderDialog.EmitSignal(AcceptDialog.SignalName.Confirmed);
            var discardProceeds = actions == 2 && _pendingLadderAction is null;
            result = $"allScenes={detectsAll} cancel={cancelPreserves} failureBlocks={failureBlocks} waitsForAll={waitsForAll} savesAll={savesAll} discard={discardProceeds}";
            return detectsAll && cancelPreserves && failureBlocks && waitsForAll && savesAll && discardProceeds;
        }
        finally
        {
            CancelPendingLadderAction();
            _ladderDocument.RestoreSnapshot(original.Document);
            _ladderSavedProjectJson = original.SavedJson;
            _ladderProjectPath = original.ProjectPath;
            _ladderHistory = original.History;
            _sceneLadderDrafts.Clear();
            foreach (var entry in originalDrafts) _sceneLadderDrafts.Add(entry.Key, entry.Value);
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            System.IO.File.Delete(pathA);
            System.IO.File.Delete(pathB);
        }
    }

    private bool TryOpenLadderProject(string path, out string message)
    {
        if (ChallengeMutationLocked) { message = "TEST CLEANUP PENDING · document replacement locked"; return false; }
        LadderEditorProjectLoadResult loaded;
        try
        {
            loaded = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(path));
        }
        catch (Exception exception) when (exception is System.IO.IOException or UnauthorizedAccessException)
        {
            message = "PROJECT OPEN FAILED · " + exception.Message.ToUpperInvariant();
            return false;
        }
        if (!loaded.IsReadable || loaded.Document is null)
        {
            message = "PROJECT REJECTED · " + loaded.Issues[0].Message.ToUpperInvariant();
            return false;
        }
        var sourceSceneId = loaded.Document.SourceSceneId.Trim();
        if (sourceSceneId.Length > 0)
        {
            if (!_sceneCatalog.Scenes.Any(scene => scene.Id.Equals(sourceSceneId, StringComparison.Ordinal)))
            {
                message = $"PROJECT OPEN REJECTED · UNKNOWN SOURCE SCENE '{sourceSceneId}'";
                return false;
            }
            if (!string.Equals(_activeScene?.Id, sourceSceneId, StringComparison.Ordinal))
            {
                message = $"PROJECT OPEN BLOCKED · SOURCE SCENE MISMATCH · SELECT '{sourceSceneId}' THEN OPEN AGAIN";
                return false;
            }
        }
        // Scene IDs are serialized metadata. Normalize the value that is
        // restored so the editor cannot retain whitespace-padded identities.
        loaded.Document.SourceSceneId = sourceSceneId;
        ChallengeDraftChanged?.Invoke();
        _ladderDocument.RestoreSnapshot(loaded.Document.CaptureSnapshot());
        _ladderProjectPath = System.IO.Path.GetFullPath(path);
        _ladderSavedProjectJson = LadderEditorProjectJson.Save(_ladderDocument);
        _ladderHistory.Clear();
        _ladderRungClipboard = null;
        _ladderContactClipboard = null; _ladderCommandClipboard = null;
        foreach (var resetDocuments in _ladderEditorProjectReplacementResetters) resetDocuments();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        RefreshLadderMonitorMatch();
        if (!TryBuildCurrentLadderProgram(out var program, out var issues))
        {
            SetVirtualControllerValidation(issues);
            var retained = _virtualProgram is null ? "NO CONTROLLER LOADED" : "PREVIOUS CONTROLLER RETAINED";
            message = $"PROJECT OPENED · {System.IO.Path.GetFileName(path)} · VERIFY + LOAD REQUIRED · {issues.Count} issue(s) · {retained}";
            return true;
        }
        foreach (var refresh in _ladderValidationRefreshers) refresh(issues);
        var validDraftControllerState = _virtualProgram is null
            ? "NO CONTROLLER LOADED"
            : "PREVIOUS CONTROLLER RETAINED";
        _ladderWorkspaceStatus.Text = $"VALID DRAFT · PROJECT OPENED · NOT LOADED · VERIFY + LOAD REQUIRED · {validDraftControllerState}";
        _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("65d49a"));
        message = $"PROJECT OPENED · {System.IO.Path.GetFileName(path)} · VALID DRAFT · NOT LOADED · VERIFY + LOAD REQUIRED · {validDraftControllerState}";
        return true;
    }

    public bool VerifySceneDraftPersistence(out string result)
    {
        var originalScene = _activeScene?.Id ?? string.Empty;
        var original = _ladderDocument.CaptureSnapshot();
        var originalSaved = _ladderSavedProjectJson;
        var originalPath = _ladderProjectPath;
        var originalHistory = _ladderHistory;
        var originalDrafts = _sceneLadderDrafts.ToArray();
        try
        {
            SceneRequested?.Invoke("lab-2-01-workstation-call");
            var labTags = _ladderDocument.Tags.Select(tag => tag.Name).OrderBy(name => name).ToArray();
            var labProject = labTags.SequenceEqual(new[] { "material_call_on", "material_call_pressed" })
                && _ladderDocument.SourceSceneId == "lab-2-01-workstation-call"
                && _ladderDocument.Blocks.All(block => block.Rungs.Count == 0);
            SceneRequested?.Invoke("scene-1-conveyor-stop");
            var baseline = _ladderDocument.CaptureSnapshot();
            _ladderHistory = new LadderEditorHistory(150);
            const string marker = "Unsaved invalid draft - scenario round trip";
            _ladderProjectPath = "draft-regression.rpproj.json";
            _ladderSavedProjectJson = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderHistory.Execute(_ladderDocument, "Add unsaved network",
                () => _ladderDocument.AddRung(marker, "unresolved_output"));
            var draft = LadderEditorProjectJson.Save(_ladderDocument);
            SceneRequested?.Invoke("lab-4-01-press-count-lamp");
            var isolated = !_ladderDocument.Rungs.Any(rung => rung.Label == marker);
            SceneRequested?.Invoke("scene-1-conveyor-stop");
            var retained = LadderEditorProjectJson.Save(_ladderDocument) == draft
                && _ladderProjectPath == "draft-regression.rpproj.json"
                && _ladderSavedProjectJson != draft;
            var undo = TryUndoLadderEdit(out _) && !_ladderDocument.Rungs.Any(rung => rung.Label == marker);
            SceneRequested?.Invoke("lab-4-01-press-count-lamp");
            SceneRequested?.Invoke("scene-1-conveyor-stop");
            var redo = TryRedoLadderEdit(out _) && LadderEditorProjectJson.Save(_ladderDocument) == draft;
            SceneRequested?.Invoke("scene-1-conveyor-stop");
            var sameScene = LadderEditorProjectJson.Save(_ladderDocument) == draft && _ladderHistory.CanUndo;
            result = $"draft={retained} isolated={isolated} undo={undo} redo={redo} sameScene={sameScene} labProject={labProject}";
            _ladderDocument.RestoreSnapshot(baseline);
            return retained && isolated && undo && redo && sameScene && labProject;
        }
        finally
        {
            if (_activeScene?.Id != originalScene) SceneRequested?.Invoke(originalScene);
            _ladderDocument.RestoreSnapshot(original);
            _ladderSavedProjectJson = originalSaved;
            _ladderProjectPath = originalPath;
            _ladderHistory = originalHistory;
            _sceneLadderDrafts.Clear();
            foreach (var entry in originalDrafts) _sceneLadderDrafts.Add(entry.Key, entry.Value);
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
    }

    public bool VerifyCrossSceneProjectOpen(out string result)
    {
        // Exercise the rejected-open boundary without changing scenes. A real
        // scene hop would also replace workspace/runtime state owned by Main.
        var sceneId = _activeScene?.Id ?? string.Empty;
        var original = _ladderDocument.CaptureSnapshot();
        var savedJson = _ladderSavedProjectJson;
        var projectPath = _ladderProjectPath;
        var history = _ladderHistory;
        var rungClipboard = _ladderRungClipboard;
        var contactClipboard = _ladderContactClipboard;
        var commandClipboard = _ladderCommandClipboard;
        var drafts = _sceneLadderDrafts.ToArray();
        var targetSceneId = "lab-11-13-xy-palletizing";
        var path = System.IO.Path.Combine(OS.GetUserDataDir(), $"cross-scene-review-{Guid.NewGuid():N}.rpproj.json");
        var unknownPath = System.IO.Path.Combine(OS.GetUserDataDir(), $"unknown-scene-review-{Guid.NewGuid():N}.rpproj.json");
        try
        {
            var originalDocumentJson = LadderEditorProjectJson.Save(_ladderDocument);
            var loadedProgram = _virtualProgram;
            var loadedScan = _virtualSnapshot?.ScanNumber;
            var loadedState = _virtualSnapshot?.State;
            var mismatchProject = LadderEditorProjectJson.Load(originalDocumentJson);
            var unknownProject = LadderEditorProjectJson.Load(originalDocumentJson);
            if (!mismatchProject.IsReadable || mismatchProject.Document is null
                || !unknownProject.IsReadable || unknownProject.Document is null)
                throw new InvalidOperationException("Cross-scene Open regression needs readable source fixtures.");
            mismatchProject.Document.SourceSceneId = targetSceneId;
            unknownProject.Document.SourceSceneId = "missing-scene-review-id";
            System.IO.File.WriteAllText(path, LadderEditorProjectJson.Save(mismatchProject.Document));
            System.IO.File.WriteAllText(unknownPath, LadderEditorProjectJson.Save(unknownProject.Document));

            var targetBaseline = LadderEditorProjectJson.Load(originalDocumentJson).Document!;
            var targetDraft = LadderEditorProjectJson.Load(originalDocumentJson).Document!;
            targetDraft.SourceSceneId = targetSceneId;
            const string dirtyTargetMarker = "Dirty cached target scene draft";
            if (targetDraft.Rungs.Count > 0)
                targetDraft.Rungs[0].Label = dirtyTargetMarker;
            else
                targetDraft.AddRung(dirtyTargetMarker, string.Empty);
            _sceneLadderDrafts[targetSceneId] = new(
                targetDraft.CaptureSnapshot(), LadderEditorProjectJson.Save(targetBaseline),
                "cached-review.rpproj.json", new LadderEditorHistory(150));
            var targetDraftJson = DraftJson(_sceneLadderDrafts[targetSceneId]);

            var mismatchOpened = TryOpenLadderProject(path, out var mismatchMessage);
            var mismatchRejectedWithoutMutation = !mismatchOpened
                && mismatchMessage.Contains("SOURCE SCENE MISMATCH", StringComparison.Ordinal)
                && _activeScene?.Id == sceneId
                && LadderEditorProjectJson.Save(_ladderDocument) == originalDocumentJson
                && _ladderProjectPath == projectPath
                && ReferenceEquals(_ladderHistory, history)
                && ReferenceEquals(_virtualProgram, loadedProgram)
                && _virtualSnapshot?.ScanNumber == loadedScan
                && _virtualSnapshot?.State == loadedState
                && ReferenceEquals(_ladderRungClipboard, rungClipboard)
                && ReferenceEquals(_ladderContactClipboard, contactClipboard)
                && ReferenceEquals(_ladderCommandClipboard, commandClipboard);

            var unknownOpened = TryOpenLadderProject(unknownPath, out var unknownMessage);
            var unknownSceneRejectedWithoutMutation = !unknownOpened
                && unknownMessage.Contains("UNKNOWN SOURCE SCENE", StringComparison.Ordinal)
                && _activeScene?.Id == sceneId
                && LadderEditorProjectJson.Save(_ladderDocument) == originalDocumentJson
                && _ladderProjectPath == projectPath
                && ReferenceEquals(_ladderHistory, history)
                && ReferenceEquals(_virtualProgram, loadedProgram)
                && _virtualSnapshot?.ScanNumber == loadedScan
                && _virtualSnapshot?.State == loadedState
                && ReferenceEquals(_ladderRungClipboard, rungClipboard)
                && ReferenceEquals(_ladderContactClipboard, contactClipboard)
                && ReferenceEquals(_ladderCommandClipboard, commandClipboard);
            var cachedDraftStayedSafeOnRejectedOpen = _sceneLadderDrafts.TryGetValue(targetSceneId, out var stillCachedTarget)
                && DraftJson(stillCachedTarget) == targetDraftJson
                && DraftHasUnsavedChanges(stillCachedTarget);

            var passed = mismatchRejectedWithoutMutation
                && unknownSceneRejectedWithoutMutation
                && cachedDraftStayedSafeOnRejectedOpen;
            result = $"mismatchRejected={mismatchRejectedWithoutMutation} unknownRejected={unknownSceneRejectedWithoutMutation} cachedTargetPreserved={cachedDraftStayedSafeOnRejectedOpen} sceneTransitions=0";
            return passed;
        }
        finally
        {
            _ladderDocument.RestoreSnapshot(original);
            _ladderSavedProjectJson = savedJson;
            _ladderProjectPath = projectPath;
            _ladderHistory = history;
            _ladderRungClipboard = rungClipboard;
            _ladderContactClipboard = contactClipboard;
            _ladderCommandClipboard = commandClipboard;
            _sceneLadderDrafts.Clear();
            foreach (var entry in drafts) _sceneLadderDrafts.Add(entry.Key, entry.Value);
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            if (System.IO.File.Exists(unknownPath)) System.IO.File.Delete(unknownPath);
        }
    }

    private Control BuildLadderEnvironment(string tabName, bool siemens)
    {
        int GetProjectDockSplitOffset() => siemens
            ? _tiaProjectDockSplitOffset
            : _studio5000ProjectDockSplitOffset;

        void SetProjectDockSplitOffset(int value)
        {
            if (siemens) _tiaProjectDockSplitOffset = value;
            else _studio5000ProjectDockSplitOffset = value;
        }

        var document = _ladderDocument;
        _ = document.Rungs;
        var root = new VBoxContainer { Name = tabName };
        root.AddThemeConstantOverride("separation", 0);
        var accent = siemens ? new Color("0087a8") : new Color("b82e2e");
        MenuButton VendorMenu(string name, string text)
        {
            var menu = TopMenu(name, text);
            if (siemens)
            {
                menu.AddThemeColorOverride("font_color", new Color("344851"));
                menu.AddThemeColorOverride("font_hover_color", new Color("142a36"));
                menu.AddThemeColorOverride("font_pressed_color", new Color("142a36"));
            }
            return menu;
        }
        var chrome = new ScrollContainer { Name = "VendorChrome",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 56) };
        chrome.AddThemeStyleboxOverride("panel", BoxStyle(siemens ? new Color("e4e8ea") : new Color("333840"), accent));
        var chromeRow = new HBoxContainer { Name = "VendorMenuBar" };
        chromeRow.AddThemeConstantOverride("separation", 12);
        chrome.AddChild(chromeRow);
        chromeRow.AddChild(Heading(siemens ? "RungProof TIA WORKBENCH" : "RungProof LOGIX WORKBENCH", 13,
            siemens ? new Color("263943") : new Color("f4f5f6")));
        var projectMenu = VendorMenu("LadderProjectMenu", siemens ? "Project" : "File");
        projectMenu.GetPopup().AddItem("New offline project", 0);
        projectMenu.GetPopup().AddItem("Save project", 1);
        projectMenu.GetPopup().AddItem("Save As…", 3);
        projectMenu.GetPopup().AddItem("Open project…", 2);
        chromeRow.AddChild(projectMenu);
        var editMenu = VendorMenu("LadderEditMenu", "Edit");
        editMenu.GetPopup().AddItem(siemens ? "Add network" : "Add rung", 0);
        editMenu.GetPopup().AddItem(
            siemens ? "Delete selected instruction / network" : "Delete selected instruction / rung", 1);
        editMenu.GetPopup().AddSeparator();
        editMenu.GetPopup().AddItem(
            siemens ? "Copy selected instruction / network" : "Copy selected instruction / rung", 4);
        editMenu.GetPopup().AddItem("Paste after selection", 5);
        editMenu.GetPopup().AddItem("Cut", 7);
        editMenu.GetPopup().AddItem(siemens ? "Copy network" : "Copy rung", 6);
        editMenu.GetPopup().AddSeparator();
        editMenu.GetPopup().AddItem("Undo", 2);
        editMenu.GetPopup().AddItem("Redo", 3);
        chromeRow.AddChild(editMenu);
        var undoEdit = ToolbarButton("LadderUndo", "UNDO", new Color("637985"), 64);
        var redoEdit = ToolbarButton("LadderRedo", "REDO", new Color("637985"), 64);
        chromeRow.AddChild(undoEdit);
        chromeRow.AddChild(redoEdit);
        var onlineMenu = VendorMenu("LadderOnlineMenu", siemens ? "Online" : "Communications");
        onlineMenu.GetPopup().AddItem("Verify + load offline", 0);
        onlineMenu.GetPopup().AddSeparator();
        onlineMenu.GetPopup().AddItem("Run simulator controller", 1);
        onlineMenu.GetPopup().AddItem("Stop simulator controller", 2);
        onlineMenu.GetPopup().AddItem("Reset simulator controller", 3);
        chromeRow.AddChild(onlineMenu);
        var viewMenu = VendorMenu("LadderViewMenu", "View");
        viewMenu.GetPopup().AddItem("Program editor", 0);
        viewMenu.GetPopup().AddItem("PLC tags", 1);
        viewMenu.GetPopup().AddItem("Diagnostics / output", 2);
        viewMenu.GetPopup().AddItem("Find / cross-reference", 3);
        viewMenu.GetPopup().AddItem("Instruction help", 4);
        viewMenu.GetPopup().AddItem(siemens ? "Watch table" : "Watch List", 5);
        viewMenu.GetPopup().AddItem("Instruction browser", 6);
        if (siemens)
        {
            var viewPopup = viewMenu.GetPopup();
            viewPopup.AddSeparator();
            foreach (var (label, itemId) in new[]
            {
                ("Operand representation: Symbolic", 7),
                ("Operand representation: Absolute", 8),
                ("Operand representation: Symbolic and absolute", 9),
                ("Tag information: Hide", 10),
                ("Tag information: Show", 11),
                ("Tag information: Hierarchical comments", 12),
                ("Position of tag information: On operands", 13),
                ("Position of tag information: Below network", 14),
            })
            {
                if (itemId is 10 or 13) viewPopup.AddSeparator();
                viewPopup.AddItem(label, itemId);
                viewPopup.SetItemAsCheckable(viewPopup.GetItemIndex(itemId), true);
                viewPopup.SetItemAsRadioCheckable(viewPopup.GetItemIndex(itemId), true);
            }
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(7), _tiaOperandRepresentation == TiaOperandRepresentation.Symbolic);
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(8), _tiaOperandRepresentation == TiaOperandRepresentation.Absolute);
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(9), _tiaOperandRepresentation == TiaOperandRepresentation.SymbolicAndAbsolute);
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(10), _tiaTagInformationDisplay == TiaTagInformationDisplay.Hidden);
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(11), _tiaTagInformationDisplay == TiaTagInformationDisplay.Show);
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(12), _tiaTagInformationDisplay == TiaTagInformationDisplay.HierarchicalComments);
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(13), _tiaTagInformationPosition == TiaTagInformationPosition.OnOperands);
            viewPopup.SetItemChecked(viewPopup.GetItemIndex(14), _tiaTagInformationPosition == TiaTagInformationPosition.BelowNetwork);
        }
        chromeRow.AddChild(viewMenu);
        var toolsMenu = VendorMenu("LadderToolsMenu", "Tools");
        toolsMenu.GetPopup().AddItem("Add BOOL tag", 0);
        toolsMenu.GetPopup().AddItem("Instruction help (F1)", 1);
        toolsMenu.GetPopup().AddItem(siemens ? "Cross-reference" : "Find All", 2);
        chromeRow.AddChild(toolsMenu);
        var offline = Heading("OFFLINE SIMULATOR", 11, accent);
        offline.HorizontalAlignment = HorizontalAlignment.Right;
        offline.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        chromeRow.AddChild(offline);
        root.AddChild(chrome);

        void ConfigureDockSplit(SplitContainer split)
        {
            split.DraggingEnabled = true;
            split.DraggerVisibility = SplitContainer.DraggerVisibilityEnum.Visible;
            split.DragNestedIntersections = true;
            split.AddThemeConstantOverride("separation", 8);
            split.AddThemeConstantOverride("minimum_grab_thickness", 12);
            split.AddThemeConstantOverride("autohide", 0);
            split.AddThemeStyleboxOverride(
                "split_bar_background",
                BoxStyle(new Color("536b78"), new Color("324955")));
        }

        var work = new HSplitContainer { Name = "Workbench" };
        work.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        work.SplitOffsets = [280];
        ConfigureDockSplit(work);
        root.AddChild(work);

        var projectDockHost = new HBoxContainer { Name = "ProjectDockHost" };
        projectDockHost.AddThemeConstantOverride("separation", 0);
        var reopenProjectDock = ToolbarButton("ReopenProjectDock", "▶", new Color("4d6674"), 30);
        reopenProjectDock.CustomMinimumSize = new Vector2(30, 0);
        reopenProjectDock.TooltipText = siemens ? "Show project tree" : "Show Controller Organizer";
        reopenProjectDock.Visible = false;
        projectDockHost.AddChild(reopenProjectDock);
        work.AddChild(projectDockHost);

        var editorAndTasks = new HSplitContainer { Name = "EditorAndTasks" };
        editorAndTasks.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        editorAndTasks.SplitOffsets = [600];
        ConfigureDockSplit(editorAndTasks);
        var toolDockSplitOffset = 600;
        work.AddChild(editorAndTasks);

        var editorAndInspector = new VSplitContainer { Name = "EditorAndInspector" };
        editorAndInspector.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        editorAndInspector.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        // Keep the ladder visible when Scene I/O is also expanded. Diagnostics
        // opens on demand with its full watch-table minimum height.
        editorAndInspector.SplitOffsets = [10000];
        ConfigureDockSplit(editorAndInspector);
        var bottomDockSplitOffset = 280;
        editorAndTasks.AddChild(editorAndInspector);

        var projectPanel = PanelContainer("ProjectOrganization", new Color("eef0f2"), new Color("9aa7ad"));
        projectPanel.CustomMinimumSize = new Vector2(150, 0);
        projectPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        projectPanel.TooltipText = "Drag the divider at the right edge to resize this dock.";
        var projectBody = new VBoxContainer { Name = "ProjectBody" };
        projectPanel.AddChild(projectBody);
        var projectTabs = new TabContainer
        {
            Name = "ProjectTreeAndTags",
            TabsVisible = !siemens,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        projectTabs.AddThemeFontSizeOverride("font_size", 11);
        // Tag/object pages contain dark engineering labels. Use the same
        // light surface as the project tree instead of the default gray tab.
        projectTabs.AddThemeStyleboxOverride("panel", BoxStyle(new Color("eef0f2"), new Color("9aa7ad")));
        var projectTitle = new HBoxContainer { Name = "ProjectTitle" };
        var projectHeading = Heading(siemens ? "Project tree" : "Controller Organizer", 13, new Color("263943"));
        projectHeading.Name = "ProjectTreeHeading";
        projectHeading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        projectTitle.AddChild(projectHeading);
        var showProjectTree = ToolbarButton("ShowProjectTree", "TREE", new Color("4d6674"), 52);
        showProjectTree.TooltipText = "Return to the project tree.";
        showProjectTree.Visible = false;
        var projectTreeShortcutSlot = new CenterContainer
        {
            Name = "ProjectTreeShortcutSlot",
            CustomMinimumSize = new Vector2(52, 30),
            Visible = siemens,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        projectTreeShortcutSlot.AddChild(showProjectTree);
        projectTitle.AddChild(projectTreeShortcutSlot);
        var addBlockFromTree = ToolbarButton("AddBlockFromProjectTree", "+", accent, 30);
        addBlockFromTree.TooltipText = siemens
            ? "Add OB / FB / FC / DB to Program blocks"
            : "Add a routine to the Controller Organizer";
        var addBlockSlot = new CenterContainer
        {
            Name = "AddBlockFromProjectTreeSlot",
            CustomMinimumSize = new Vector2(30, 30),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        addBlockSlot.AddChild(addBlockFromTree);
        projectTitle.AddChild(addBlockSlot);
        var collapseProjectDock = ToolbarButton("CollapseProjectDock", "◀", new Color("637985"), 30);
        collapseProjectDock.TooltipText = siemens ? "Hide project tree" : "Hide Controller Organizer";
        projectTitle.AddChild(collapseProjectDock);
        // Keep the project header and its TREE shortcut ahead of the expanding
        // tab content. At constrained heights, a fill-first TabContainer can
        // consume the VBox space and push this header outside the viewport.
        projectBody.AddChild(projectTitle);
        projectBody.AddChild(projectTabs);
        var organization = new LadderProgramTree
        {
            Name = "ProjectTree",
            HideRoot = false,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
            SelectMode = Tree.SelectModeEnum.Single,
            TooltipText = siemens
                ? "Drag an FC or FB from Program blocks onto a network's terminal instruction to insert a named block call."
                : "Controller Organizer",
        };
        organization.AddThemeColorOverride("font_color", new Color("263943"));
        organization.AddThemeColorOverride("font_selected_color", new Color("ffffff"));
        ConfigureSelectableList(organization);
        organization.AddThemeColorOverride("guide_color", new Color("9aa7ad"));
        organization.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        var controllerItem = organization.CreateItem();
        controllerItem.SetText(0, "Offline_Controller");
        controllerItem.SetMetadata(0, "controller");
        TreeItem mainItem;
        TreeItem tagItem;
        TreeItem? addNewBlockItem = null;
        TreeItem? showAllTagsItem = null;
        TreeItem? addNewTagTableItem = null;
        TreeItem? addWatchTableItem = null;
        TreeItem watchFolder;
        TreeItem diagnosticsItem;
        TreeItem blockFolder;
        TreeItem? instanceDbFolder = null;
        TreeItem taskFolder;
        TreeItem initialTaskItem;
        if (siemens)
        {
            var plcItem = organization.CreateItem(controllerItem);
            plcItem.SetText(0, "PLC_1 [S7-1500]");
            plcItem.SetMetadata(0, "controller");
            var deviceItem = organization.CreateItem(plcItem);
            deviceItem.SetText(0, "Symbolic I/O");
            deviceItem.SetMetadata(0, "device");
            diagnosticsItem = organization.CreateItem(plcItem);
            diagnosticsItem.SetText(0, "Online & diagnostics");
            diagnosticsItem.SetMetadata(0, "diagnostics");
            taskFolder = organization.CreateItem(plcItem);
            taskFolder.SetText(0, "Organization blocks / tasks");
            taskFolder.SetMetadata(0, "tasks");
            // Siemens exposes OBs as code blocks under Program blocks. The
            // simulator's scheduler tasks remain project metadata, but showing
            // them as a second OB tree duplicates the same authored block and
            // invents a TIA folder that is not part of this supported view.
            taskFolder.Visible = false;
            initialTaskItem = organization.CreateItem(taskFolder);
            initialTaskItem.SetText(0, "Main cycle [OB]");
            initialTaskItem.SetMetadata(0, "task:0");
            initialTaskItem.Visible = false;
            blockFolder = organization.CreateItem(plcItem);
            blockFolder.SetText(0, "Program blocks");
            blockFolder.SetMetadata(0, "blocks");
            addNewBlockItem = organization.CreateItem(blockFolder);
            addNewBlockItem.SetText(0, "Add new block");
            addNewBlockItem.SetMetadata(0, "add-block");
            mainItem = organization.CreateItem(blockFolder);
            mainItem.SetText(0, $"{document.Blocks[0].Name} [OB]");
            mainItem.SetMetadata(0, "program:0");
            // TIA lists instance DBs in Program blocks beside OBs, FBs and FCs;
            // they are not a separate sibling folder in the project tree.
            instanceDbFolder = blockFolder;
            tagItem = organization.CreateItem(plcItem);
            tagItem.SetText(0, "PLC tags");
            tagItem.SetMetadata(0, "tags");
            showAllTagsItem = organization.CreateItem(tagItem);
            showAllTagsItem.SetText(0, "Show all tags");
            showAllTagsItem.SetMetadata(0, "tag-table:");
            addNewTagTableItem = organization.CreateItem(tagItem);
            addNewTagTableItem.SetText(0, "Add new tag table");
            addNewTagTableItem.SetMetadata(0, "add-tag-table");
            var defaultTagTableItem = organization.CreateItem(tagItem);
            defaultTagTableItem.SetText(0, "Default tag table [0]");
            defaultTagTableItem.SetMetadata(0, "tag-table:Default tag table");
            watchFolder = organization.CreateItem(plcItem);
            watchFolder.SetText(0, "Watch tables");
            watchFolder.SetMetadata(0, "watch");
            addWatchTableItem = organization.CreateItem(watchFolder);
            addWatchTableItem.SetText(0, "Add new watch table");
            addWatchTableItem.SetMetadata(0, "add-watch-table");
        }
        else
        {
            tagItem = organization.CreateItem(controllerItem);
            tagItem.SetText(0, "Controller Tags");
            tagItem.SetMetadata(0, "tags");
            watchFolder = organization.CreateItem(controllerItem);
            watchFolder.SetText(0, "Watch Tables");
            watchFolder.SetMetadata(0, "watch");
            taskFolder = organization.CreateItem(controllerItem);
            taskFolder.SetText(0, "Tasks");
            taskFolder.SetMetadata(0, "tasks");
            var mainTask = organization.CreateItem(taskFolder);
            mainTask.SetText(0, "MainTask");
            mainTask.SetMetadata(0, "task:0");
            initialTaskItem = mainTask;
            var mainProgram = organization.CreateItem(mainTask);
            mainProgram.SetText(0, "MainProgram");
            mainProgram.SetMetadata(0, "blocks");
            blockFolder = mainProgram;
            mainItem = organization.CreateItem(mainProgram);
            mainItem.SetText(0, document.Blocks[0].Name);
            mainItem.SetMetadata(0, "program:0");
            diagnosticsItem = organization.CreateItem(controllerItem);
            diagnosticsItem.SetText(0, "Controller Properties / Diagnostics");
            diagnosticsItem.SetMetadata(0, "diagnostics");
            var io = organization.CreateItem(controllerItem);
            io.SetText(0, "I/O Configuration");
            io.SetMetadata(0, "device");
        }
        controllerItem.SetCollapsed(false);
        var blockItems = new List<TreeItem> { mainItem };
        var taskItems = new List<TreeItem> { initialTaskItem };
        var instanceDbItems = new List<TreeItem>();
        var tagTableItems = new List<TreeItem>();
        var watchTableItems = new List<TreeItem>();
        if (siemens && tagItem.GetFirstChild() is { } firstTagTable)
        {
            for (var tableItem = firstTagTable; tableItem is not null; tableItem = tableItem.GetNext())
                if (tableItem.GetMetadata(0).AsString() is var metadata
                    && metadata.StartsWith("tag-table:", StringComparison.Ordinal) && metadata.Length > "tag-table:".Length)
                    tagTableItems.Add(tableItem);
        }
        projectTabs.AddChild(organization);
        LadderProgramTree? tagOperandDetailView = null;
        VBoxContainer? tagOperandDetailPage = null;
        var tagOperandDetailTabIndex = -1;
        if (siemens)
        {
            tagOperandDetailPage = new VBoxContainer { Name = "TagOperandDetailView" };
            var detailPage = tagOperandDetailPage;
            detailPage.AddThemeConstantOverride("separation", 6);
            var detailHeader = new HBoxContainer { Name = "TagOperandDetailHeader" };
            var detailTitle = Heading("PLC tag detail view", 13, new Color("263943"));
            detailTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            detailHeader.AddChild(detailTitle);
            var returnToTree = ToolbarButton("ReturnToProjectTree", "←", new Color("637985"), 30);
            returnToTree.TooltipText = "Return to the project tree.";
            returnToTree.Pressed += () =>
            {
                projectTabs.CurrentTab = 0;
                projectHeading.Text = "Project tree";
                addBlockFromTree.Visible = true;
                organization.GrabFocus();
            };
            detailHeader.AddChild(returnToTree);
            detailPage.AddChild(detailHeader);
            var detailHint = new Label
            {
                Name = "TagOperandDetailHint",
                Text = "Drag a tag onto a compatible operand in the open LAD document.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(0, 34),
            };
            detailHint.AddThemeColorOverride("font_color", new Color("607983"));
            detailPage.AddChild(detailHint);
            tagOperandDetailView = new LadderProgramTree
            {
                Name = "TagOperandDetailTable",
                Columns = 2,
                ColumnTitlesVisible = true,
                HideRoot = true,
                SelectMode = Tree.SelectModeEnum.Single,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                TooltipText = "Drag a declared scalar tag row onto a compatible LAD operand.",
            };
            tagOperandDetailView.SetColumnTitle(0, "TAG");
            tagOperandDetailView.SetColumnTitle(1, "DATA TYPE");
            tagOperandDetailView.SetColumnExpand(0, true);
            tagOperandDetailView.SetColumnExpand(1, true);
            tagOperandDetailView.AddThemeColorOverride("font_color", new Color("263943"));
            tagOperandDetailView.AddThemeColorOverride("font_selected_color", new Color("ffffff"));
            ConfigureSelectableList(tagOperandDetailView);
            tagOperandDetailView.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
            detailPage.AddChild(tagOperandDetailView);
        }
        projectDockHost.AddChild(projectPanel);

        collapseProjectDock.Pressed += () =>
        {
            SetProjectDockSplitOffset(work.SplitOffsets.Length > 0
                ? work.SplitOffsets[0]
                : GetProjectDockSplitOffset());
            projectPanel.Visible = false;
            reopenProjectDock.Visible = true;
            work.SplitOffsets = [(int)reopenProjectDock.CustomMinimumSize.X];
        };
        reopenProjectDock.Pressed += () =>
        {
            reopenProjectDock.Visible = false;
            projectPanel.Visible = true;
            work.SplitOffsets = [GetProjectDockSplitOffset()];
        };

        var center = new VBoxContainer { Name = "ProgramEditor", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        center.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        center.AddThemeConstantOverride("separation", 1);
        editorAndInspector.AddChild(center);
        var editorDocumentTabs = new HBoxContainer { Name = "EditorDocuments", Visible = siemens };
        editorDocumentTabs.AddThemeConstantOverride("separation", 1);
        var editorDocumentScroll = new ScrollContainer
        {
            Name = "EditorDocumentsScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var editorDocumentHost = PanelContainer("EditorDocumentsHost", new Color("e4e8eb"), new Color("aab4ba"));
        editorDocumentHost.CustomMinimumSize = new Vector2(0, 34);
        editorDocumentHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        editorDocumentHost.AddChild(editorDocumentScroll);
        editorDocumentScroll.AddChild(editorDocumentTabs);
        var editorDocumentButtonGroup = new ButtonGroup { AllowUnpress = false };
        var editorDocumentButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        var editorDocumentItems = new Dictionary<string, HBoxContainer>(StringComparer.Ordinal);
        var nextEditorDocumentIndex = 0;
        var editorTagDocuments = new Dictionary<string, string?>(StringComparer.Ordinal);
        var editorInstanceDbDocuments = new Dictionary<string, string>(StringComparer.Ordinal);
        var editorWatchTableDocuments = new Dictionary<string, string>(StringComparer.Ordinal);
        string BlockDocumentKey(string blockId) => EditorDocumentIdentity.ForBlock(blockId);
        bool TryResolveBlockDocument(string key, out int blockIndex) =>
            EditorDocumentIdentity.TryResolveBlockIndex(document, key, out blockIndex);
        var initialBlockIndex = Math.Clamp(document.ActiveBlockIndex, 0, Math.Max(0, document.Blocks.Count - 1));
        var activeEditorDocumentKey = document.Blocks.Count > 0
            ? BlockDocumentKey(document.Blocks[initialBlockIndex].Id)
            : string.Empty;
        void AddEditorDocumentTab(string key, string title)
        {
            if (!siemens) return;
            var tabTitle = title.EndsWith(" ✕", StringComparison.Ordinal) ? title[..^2].TrimEnd() : title;
            if (editorDocumentButtons.TryGetValue(key, out var existing))
            {
                existing.Text = tabTitle;
                existing.TooltipText = "Activate this document.";
                return;
            }
            var itemIndex = nextEditorDocumentIndex++;
            var item = new HBoxContainer
            {
                Name = $"EditorDocumentItem_{itemIndex}",
                CustomMinimumSize = new Vector2(250, 32),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            };
            item.SetMeta("editorDocumentKey", key);
            var tab = new Button
            {
                Name = $"EditorDocument_{itemIndex}",
                Text = tabTitle,
                ToggleMode = true,
                ButtonGroup = editorDocumentButtonGroup,
                FocusMode = Control.FocusModeEnum.All,
                CustomMinimumSize = new Vector2(222, 32),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                TooltipText = "Activate this document.",
            };
            tab.SetMeta("editorDocumentKey", key);
            tab.AddThemeColorOverride("font_color", new Color("263943"));
            tab.AddThemeColorOverride("font_hover_color", new Color("263943"));
            tab.AddThemeColorOverride("font_pressed_color", new Color("1d343e"));
            tab.AddThemeFontSizeOverride("font_size", 11);
            tab.AddThemeStyleboxOverride("normal", BoxStyle(new Color("d8dee2"), new Color("9aa7ad")));
            tab.AddThemeStyleboxOverride("hover", BoxStyle(new Color("eaf0f3"), new Color("708690")));
            tab.AddThemeStyleboxOverride("pressed", BoxStyle(new Color("f8f9fa"), new Color("607983")));
            tab.Pressed += () => ResolvePendingEditorForms(() => ActivateEditorDocument(key));
            var closeTab = new Button
            {
                Name = $"EditorDocumentClose_{itemIndex}",
                Text = "×",
                CustomMinimumSize = new Vector2(28, 32),
                FocusMode = Control.FocusModeEnum.All,
                TooltipText = "Close this editor tab. The project object and saved data are unchanged.",
            };
            closeTab.AddThemeColorOverride("font_color", new Color("263943"));
            closeTab.AddThemeColorOverride("font_hover_color", new Color("d64545"));
            closeTab.AddThemeFontSizeOverride("font_size", 13);
            closeTab.AddThemeStyleboxOverride("normal", BoxStyle(new Color("d8dee2"), new Color("9aa7ad")));
            closeTab.AddThemeStyleboxOverride("hover", BoxStyle(new Color("fdebec"), new Color("d64545")));
            closeTab.Pressed += () => ResolvePendingEditorForms(() => CloseEditorDocument(key));
            item.AddChild(tab);
            item.AddChild(closeTab);
            editorDocumentButtons.Add(key, tab);
            editorDocumentItems.Add(key, item);
            editorDocumentTabs.AddChild(item);
        }
        center.AddChild(editorDocumentHost);
        var editorTab = PanelContainer("EditorTab", new Color("d9dde0"), new Color("9aa7ad"));
        editorTab.Visible = !siemens;
        var editorTabTitle = Heading(string.Empty, 12, new Color("202b31"));
        editorTabTitle.Name = "EditorTabTitle";
        editorTabTitle.ClipText = true;
        editorTabTitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        editorTabTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        editorTab.AddChild(editorTabTitle);
        center.AddChild(editorTab);

        var canvas = new LadderEditorCanvas(siemens ? LadderVendorStyle.SiemensTia : LadderVendorStyle.RockwellLogix, document);
        if (siemens)
            canvas.SetTiaDisplaySettings(_tiaOperandRepresentation, _tiaTagInformationDisplay, _tiaTagInformationPosition);
        var blockViewMemory = new LadderEditorViewMemory();
        canvas.SetMonitorPaused(_scanInspectionPaused);
        _ladderCanvases.Add(canvas);
        var selectedRung = 0;
        var selectedBranch = 0;
        var selectedContact = -1;
        var selectedInsertionIndex = -1;
        var selectedOutput = false;
        Button? moveContactLeft = null;
        Button? moveContactRight = null;
        var tagSelector = new OptionButton { Name = "ContactTagSelector", CustomMinimumSize = new Vector2(165, 32) };
        var contactTypeSelector = new OptionButton { Name = "ContactTypeSelector", CustomMinimumSize = new Vector2(190, 32) };
        var edgeMemorySelector = new OptionButton { Name = "EdgeMemoryOperandSelector", CustomMinimumSize = new Vector2(165, 32) };
        var edgeMemoryLabel = Heading("Edge memory", 11, new Color("344851"));
        edgeMemoryLabel.Visible = false;
        edgeMemorySelector.Visible = false;
        contactTypeSelector.AddItem(siemens ? "Normally open —| |—" : "XIC —] [—");
        contactTypeSelector.AddItem(siemens ? "Normally closed —|/|—" : "XIO —]/[—");
        contactTypeSelector.AddItem(siemens ? "Positive edge —|P|—" : "XIC + ONS");
        contactTypeSelector.AddItem(siemens ? "Negative edge —|N|—" : "XIO + ONS / OSF");
        var branchSelector = new OptionButton { Name = "BranchSelector", CustomMinimumSize = new Vector2(96, 32) };
        var coilSelector = new OptionButton { Name = "CoilTagSelector", CustomMinimumSize = new Vector2(165, 32) };
        var coilModeSelector = new OptionButton { Name = "CoilModeSelector", CustomMinimumSize = new Vector2(105, 32) };
        coilModeSelector.AddItem(siemens ? "Assign (=)" : "OTE");
        coilModeSelector.AddItem(siemens ? "Set (S)" : "OTL");
        coilModeSelector.AddItem(siemens ? "Reset (R)" : "OTU");
        var timerSelector = new OptionButton { Name = "TimerTagSelector", CustomMinimumSize = new Vector2(145, 32) };
        var timerPreset = new SpinBox
        {
            Name = "TimerPresetMs",
            MinValue = 1,
            MaxValue = 86_400_000,
            Step = 1,
            Suffix = " ms",
            CustomMinimumSize = new Vector2(130, 32),
        };
        var timerPresetOperand = new LineEdit
        {
            Name = "TimerPresetOperand",
            PlaceholderText = "TIME literal; blank = ???",
            CustomMinimumSize = new Vector2(180, 32),
        };
        var timerResetOperand = new LineEdit
        {
            Name = "TimerResetOperand",
            PlaceholderText = "BOOL tag or TRUE/FALSE; blank = ???",
            CustomMinimumSize = new Vector2(180, 32),
        };
        var timerDbNumber = new SpinBox { Name = "TimerInstanceDbNumber", MinValue = 1, MaxValue = 65535, Step = 1, Prefix = "%DB", CustomMinimumSize = new Vector2(105, 32) };
        var timerDbName = new LineEdit { Name = "TimerInstanceDbName", PlaceholderText = "IEC_Timer_0_DB", CustomMinimumSize = new Vector2(170, 32) };
        var counterSelector = new OptionButton { Name = "CounterTagSelector", CustomMinimumSize = new Vector2(145, 32) };
        var counterDataTypeSelector = new OptionButton { Name = "CounterDataTypeSelector", CustomMinimumSize = new Vector2(82, 32) };
        foreach (var valueType in Enum.GetValues<LadderCounterValueType>())
            counterDataTypeSelector.AddItem(LadderCounterValueTypePresentation.TiaLabel(valueType));
        var counterPreset = new SpinBox
        {
            Name = "CounterPreset",
            MinValue = 1,
            MaxValue = 2_147_483_647,
            Step = 1,
            CustomMinimumSize = new Vector2(110, 32),
        };
        var counterDbNumber = new SpinBox { Name = "CounterInstanceDbNumber", MinValue = 1, MaxValue = 65535, Step = 1, Prefix = "%DB", CustomMinimumSize = new Vector2(105, 32) };
        var counterDbName = new LineEdit { Name = "CounterInstanceDbName", PlaceholderText = "IEC_Counter_0_DB", CustomMinimumSize = new Vector2(170, 32) };
        var counterLoadInput = new LineEdit { Name = "CounterLoadInput", PlaceholderText = "BOOL tag or TRUE/FALSE", CustomMinimumSize = new Vector2(170, 32) };
        var counterResetInput = new LineEdit { Name = "CounterResetInput", PlaceholderText = "BOOL tag or TRUE/FALSE", CustomMinimumSize = new Vector2(170, 32) };
        var compareLeft = new OptionButton { Name = "CompareLeftOperand", CustomMinimumSize = new Vector2(150, 32) };
        var compareOperator = new OptionButton { Name = "CompareOperator", CustomMinimumSize = new Vector2(85, 32) };
        var compareDataType = new OptionButton { Name = "CompareDataType", CustomMinimumSize = new Vector2(105, 32) };
        foreach (var valueType in LadderComparisonValueTypePresentation.TiaSelectableTypes)
            compareDataType.AddItem(LadderComparisonValueTypePresentation.TiaLabel(valueType));
        compareDataType.Select(LadderComparisonValueTypePresentation.TiaSelectorIndex(LadderComparisonValueType.Unspecified));
        foreach (var text in new[] { "==", "<>", ">", ">=", "<", "<=", "CODE MATCH" }) compareOperator.AddItem(text);
        var compareRight = new LineEdit
        {
            Name = "CompareRightOperand",
            PlaceholderText = "tag or numeric literal",
            Text = "0",
            CustomMinimumSize = new Vector2(170, 32),
        };
        var numericSourceA = new LineEdit
        {
            Name = "NumericSourceA",
            PlaceholderText = "source A tag or literal",
            Text = "0",
            CustomMinimumSize = new Vector2(145, 32),
        };
        var numericSourceB = new LineEdit
        {
            Name = "NumericSourceB",
            PlaceholderText = "tag or literal",
            Text = "0",
            CustomMinimumSize = new Vector2(145, 32),
        };
        var numericSourceC = new LineEdit
        {
            Name = "NumericSourceC",
            PlaceholderText = "source C tag or literal",
            Text = "0",
            CustomMinimumSize = new Vector2(145, 32),
        };
        foreach (var readOnlyCapableField in new[] { numericSourceB, numericSourceC })
        {
            readOnlyCapableField.AddThemeColorOverride("font_uneditable_color", new Color("344851"));
            readOnlyCapableField.AddThemeStyleboxOverride("read_only", BoxStyle(new Color("e2e6e8"), new Color("9aa7ad")));
        }
        var numericDestination = new OptionButton { Name = "NumericDestination", CustomMinimumSize = new Vector2(145, 32) };
        var callTarget = new OptionButton { Name = "CallTarget", CustomMinimumSize = new Vector2(180, 32) };
        var programControlLabel = new LineEdit
        {
            Name = "ProgramControlLabel",
            PlaceholderText = "block_local_label",
            CustomMinimumSize = new Vector2(220, 32),
        };
        var blockName = new LineEdit
        {
            Name = "BlockName",
            PlaceholderText = siemens ? "block name" : "routine name",
            CustomMinimumSize = new Vector2(170, 32),
        };
        var blockTypeSelector = new OptionButton
        {
            Name = "BlockTypeSelector",
            CustomMinimumSize = new Vector2(190, 32),
        };
        if (siemens)
        {
            blockTypeSelector.AddItem("Organization block (OB)");
            blockTypeSelector.AddItem("Function block (FB)");
            blockTypeSelector.AddItem("Function (FC)");
            blockTypeSelector.AddItem("Data block (DB)");
        }
        else
        {
            blockTypeSelector.AddItem("Routine");
        }
        blockTypeSelector.Select(siemens ? 2 : 0);
        blockTypeSelector.Disabled = siemens;
        blockTypeSelector.TooltipText = siemens
            ? "Block type is selected when the block is created. Create a different block type instead of converting an existing block."
            : "Routine";
        var interfaceName = new LineEdit
        {
            Name = "InterfaceName",
            PlaceholderText = "parameter name",
            CustomMinimumSize = new Vector2(135, 30),
        };
        var interfaceType = new OptionButton { Name = "InterfaceType", CustomMinimumSize = new Vector2(115, 30) };
        foreach (var type in new[] { PlcVariableType.Bool, PlcVariableType.Int, PlcVariableType.DInt, PlcVariableType.Real, PlcVariableType.Timer, PlcVariableType.Counter, PlcVariableType.String, PlcVariableType.Enum })
            interfaceType.AddItem(type.ToString().ToUpperInvariant());
        var interfaceSection = new OptionButton { Name = "InterfaceSection", CustomMinimumSize = new Vector2(100, 30) };
        foreach (var section in Enum.GetValues<LadderInterfaceSection>()) interfaceSection.AddItem(section.ToString().ToUpperInvariant());
        interfaceSection.Select(0);
        var interfaceInitial = new LineEdit
        {
            Name = "InterfaceInitial",
            Text = "0",
            PlaceholderText = "initial",
            CustomMinimumSize = new Vector2(100, 30),
        };
        var interfaceEnumMembers = new LineEdit { Name = "InterfaceEnumMembers", PlaceholderText = "ENUM members, comma separated", CustomMinimumSize = new Vector2(180, 30) };
        var interfaceRetain = new CheckBox { Name = "InterfaceRetain", Text = "RETAIN", TooltipText = "Stores the RETAIN declaration attribute for this optimized-access Global DB member. The offline runtime does not model power-cycle retention." };
        var interfaceComment = new LineEdit { Name = "InterfaceComment", PlaceholderText = "comment", CustomMinimumSize = new Vector2(180, 30) };
        var addInterface = ToolbarButton("AddInterface", "+ PARAMETER", new Color("4d6674"), 130);
        var newInterface = ToolbarButton("NewInterface", "NEW", new Color("536771"), 105);
        newInterface.TooltipText = "Start a new declaration. Pending form changes must be applied, discarded, or canceled first.";
        var cancelInterface = ToolbarButton("CancelInterface", "CANCEL", new Color("536771"), 100);
        cancelInterface.TooltipText = "Discard the current member form draft and return to a blank declaration form.";
        cancelInterface.Disabled = true;
        var removeInterface = ToolbarButton("RemoveInterface", "REMOVE", new Color("8a4d55"), 90);
        var interfaceTable = new Tree
        {
            Name = "BlockInterface",
            Columns = 6,
            HideRoot = true,
            CustomMinimumSize = new Vector2(0, 120),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SelectMode = Tree.SelectModeEnum.Single,
        };
        interfaceTable.SetColumnTitle(0, "NAME");
        interfaceTable.SetColumnTitle(1, "SECTION");
        interfaceTable.SetColumnTitle(2, "TYPE");
        interfaceTable.SetColumnTitle(3, "INITIAL");
        interfaceTable.SetColumnTitle(4, "RETAIN");
        interfaceTable.SetColumnTitle(5, "COMMENT");
        interfaceTable.AddThemeColorOverride("font_color", new Color("263943"));
        ConfigureSelectableList(interfaceTable);
        interfaceTable.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        var taskTarget = new OptionButton { Name = "TaskTarget", CustomMinimumSize = new Vector2(180, 32) };
        var taskName = new LineEdit
        {
            Name = "TaskName",
            PlaceholderText = siemens ? "OB schedule name" : "task name",
            CustomMinimumSize = new Vector2(155, 32),
        };
        var taskKind = new OptionButton { Name = "TaskKind", CustomMinimumSize = new Vector2(110, 32) };
        taskKind.AddItem("Continuous");
        taskKind.AddItem("Periodic");
        taskKind.Select(1);
        var taskPeriod = new SpinBox
        {
            Name = "TaskPeriodMs",
            MinValue = 1,
            MaxValue = 86_400_000,
            Step = 1,
            Value = 100,
            Suffix = " ms",
            CustomMinimumSize = new Vector2(130, 32),
        };
        taskPeriod.GetLineEdit().AddThemeColorOverride("font_uneditable_color", new Color("344851"));
        taskPeriod.GetLineEdit().AddThemeStyleboxOverride("read_only", BoxStyle(new Color("e2e6e8"), new Color("9aa7ad")));
        var taskPriority = new SpinBox
        {
            Name = "TaskPriority",
            MinValue = 0,
            MaxValue = 1000,
            Step = 1,
            Value = 10,
            Prefix = "P",
            CustomMinimumSize = new Vector2(90, 32),
        };
        var selectedTaskIndex = 0;
        var selectedTagIndex = -1;
        var selectedInterfaceIndex = -1;
        var interfaceInitialValueEdited = false;
        string? interfaceFormOwnerBlockId = null;
        string? selectedInterfaceBlockId = null;
        string? selectedInterfaceParameterId = null;
        string? selectedInterfaceParameterName = null;
        string? activeTagTableName = "Default tag table";
        Button? applyTagEdit = null;
        Button? deleteTag = null;
        var rungLabel = new LineEdit { Name = "RungLabel", CustomMinimumSize = new Vector2(230, 32) };
        var tagList = new LadderProgramTree
        {
            Name = "TagTable",
            Columns = siemens ? 7 : 5,
            ColumnTitlesVisible = true,
            HideRoot = true,
            AllowReselect = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 150),
        };
        var tagColumns = siemens
            ? new[] { (0, "NAME"), (1, "ADDRESS"), (2, "COMMENT"), (3, "TYPE"), (4, "ROLE"), (5, "INITIAL"), (6, "BINDING") }
            : new[] { (0, "NAME"), (1, "TYPE"), (2, "ROLE"), (3, "INITIAL"), (4, "BINDING"), (5, ""), (6, "") };
        foreach (var (column, title) in tagColumns.Take(siemens ? 7 : 5))
            tagList.SetColumnTitle(column, title);
        var tagNameColumn = 0;
        tagList.SetColumnExpand(tagNameColumn, false);
        tagList.SetColumnCustomMinimumWidth(tagNameColumn, 118);
        if (siemens)
        {
            tagList.SetColumnExpand(1, false); tagList.SetColumnCustomMinimumWidth(1, 90);
            tagList.SetColumnExpand(2, true); tagList.SetColumnCustomMinimumWidth(2, 120);
            tagList.SetColumnExpand(3, false); tagList.SetColumnCustomMinimumWidth(3, 60);
            tagList.SetColumnExpand(4, false); tagList.SetColumnCustomMinimumWidth(4, 60);
            tagList.SetColumnExpand(5, false); tagList.SetColumnCustomMinimumWidth(5, 72);
            tagList.SetColumnExpand(6, true); tagList.SetColumnCustomMinimumWidth(6, 110);
        }
        else
        {
            tagList.SetColumnExpand(1, false); tagList.SetColumnCustomMinimumWidth(1, 60);
            tagList.SetColumnExpand(2, false); tagList.SetColumnCustomMinimumWidth(2, 60);
            tagList.SetColumnExpand(3, false); tagList.SetColumnCustomMinimumWidth(3, 72);
            tagList.SetColumnExpand(4, true);
        }
        tagList.AddThemeColorOverride("font_color", new Color("263943"));
        ConfigureSelectableList(tagList);
        tagList.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        var tagName = new LineEdit { Name = "NewTagName", PlaceholderText = "new_bool_tag" };
        var tagTableMembership = new OptionButton
        {
            Name = "TagTableMembership",
            FitToLongestItem = false,
            ClipText = true,
            CustomMinimumSize = new Vector2(0, 32),
        };
        var tagAddress = new LineEdit { Name = "TagAddress", PlaceholderText = siemens ? "%I0.0 or %DB1.DBX0.0" : "optional controller address" };
        var tagAddressValidation = new Label
        {
            Name = "TagAddressValidation",
            Visible = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        tagAddressValidation.AddThemeColorOverride("font_color", new Color("b42318"));
        if (siemens)
            tagAddress.TooltipText = "Optional S7-1500 address metadata. Supported editor subset: BOOL I/Q/M bit or DBX; INT IW/MW/DBW; DINT, REAL, or TIME ID/MD/DBD. This does not map addresses to hardware or enable PLC I/O.";
        var tagComment = new LineEdit { Name = "TagComment", PlaceholderText = "Tag comment" };
        var tagType = new OptionButton { Name = "NewTagType" };
        tagType.AddItem("BOOL");
        tagType.AddItem("TIMER");
        tagType.AddItem("COUNTER");
        tagType.AddItem("INT");
        tagType.AddItem("DINT");
        tagType.AddItem("REAL");
        tagType.AddItem("STRUCT");
        tagType.AddItem("ARRAY");
        tagType.AddItem("STRING");
        tagType.AddItem("ENUM");
        tagType.AddItem("TIME");
        var tagEnumMembers = new LineEdit
        {
            Name = "TagEnumMembers", Text = "Stopped, Running, Fault",
            PlaceholderText = "Stopped, Running, Fault",
            TooltipText = "Comma-separated case-sensitive enum members; initial value must be a declared member.",
            CustomMinimumSize = new Vector2(0, 32),
        };
        var aggregateTagEditor = new AggregateTagEditor();
        var tagRole = new OptionButton { Name = "NewTagRole" };
        tagRole.AddItem("Input");
        tagRole.AddItem("Memory");
        tagRole.AddItem("Output");
        var tagInitialValue = new LineEdit
        {
            Name = "TagInitialValue",
            PlaceholderText = "FALSE",
            TooltipText = "Startup value applied by offline controller Reset. BOOL: TRUE/FALSE; INT/DINT: integer; REAL: finite decimal; STRING: raw text; ENUM: declared member.",
            CustomMinimumSize = new Vector2(0, 32),
        };
        var tagBinding = new OptionButton
        {
            Name = "NewTagBinding",
            FitToLongestItem = false,
            ClipText = true,
            CustomMinimumSize = new Vector2(0, 32),
        };
        var tagBindingStatus = Heading("UNBOUND · no scene I/O exchange", 11, new Color("4f6874"));
        tagBindingStatus.Name = "TagBindingStatus";
        tagBindingStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        tagBindingStatus.CustomMinimumSize = new Vector2(0, 40);
        var output = Inspector("OutputWindow");
        output.AddThemeColorOverride("default_color", new Color("344851"));
        output.CustomMinimumSize = new Vector2(0, 66);
        output.Text = "[color=#18864b]Ready.[/color] Click a wire segment to place an insertion cursor, choose an instruction, then Verify + Load.";
        var refreshingEditor = false;
        ItemList? validationIssueList = null;
        Label? validationSummary = null;

        // Instruction operands belong to the selected graphical instruction, not
        // to a generic editor-wide form. The popup is opened by double-click or
        // the ladder element's right-click Properties command.
        var instructionPropertiesLayer = new CanvasLayer { Name = "InstructionPropertiesLayer", Layer = 100 };
        var instructionPropertiesPopup = new Control
        {
            Name = "InstructionPropertiesPopup",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        instructionPropertiesPopup.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var instructionPropertiesBackdrop = new ColorRect
        {
            Name = "InstructionPropertiesBackdrop",
            Color = new Color(0, 0, 0, 0.35f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        instructionPropertiesBackdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        instructionPropertiesPopup.AddChild(instructionPropertiesBackdrop);
        var instructionPropertiesFrame = PanelContainer(
            "InstructionPropertiesFrame", new Color("f5f6f7"), accent);
        instructionPropertiesFrame.SetAnchorsPreset(Control.LayoutPreset.Center);
        instructionPropertiesFrame.OffsetLeft = -380;
        instructionPropertiesFrame.OffsetTop = -95;
        instructionPropertiesFrame.OffsetRight = 380;
        instructionPropertiesFrame.OffsetBottom = 95;
        var instructionPropertiesBody = new VBoxContainer { Name = "InstructionPropertiesBody" };
        instructionPropertiesBody.AddThemeConstantOverride("separation", 8);
        instructionPropertiesFrame.AddChild(instructionPropertiesBody);
        instructionPropertiesPopup.AddChild(instructionPropertiesFrame);
        var instructionPropertiesTitleRow = new HBoxContainer { Name = "InstructionPropertiesTitleRow" };
        var instructionPropertiesTitle = Heading(
            siemens ? "Instruction properties" : "Instruction Properties",
            14, new Color("263943"));
        instructionPropertiesTitle.Name = "InstructionPropertiesTitle";
        instructionPropertiesTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        instructionPropertiesTitleRow.AddChild(instructionPropertiesTitle);
        var closeInstructionProperties = ToolbarButton(
            "CloseInstructionProperties", "CLOSE", new Color("637985"), 70);
        instructionPropertiesTitleRow.AddChild(closeInstructionProperties);
        instructionPropertiesBody.AddChild(instructionPropertiesTitleRow);
        var instructionPropertyScope = Heading("", 11, new Color("4f6874"));
        instructionPropertyScope.Name = "InstructionPropertyScope";
        instructionPropertiesBody.AddChild(instructionPropertyScope);

        HBoxContainer PropertyRow(string name, params Control[] controls)
        {
            var row = new HBoxContainer { Name = name };
            row.AddThemeConstantOverride("separation", 6);
            foreach (var control in controls) row.AddChild(control);
            return row;
        }

        var rungPropertyRow = PropertyRow(
            "RungPropertyRow",
            Heading(siemens ? "Network title" : "Rung comment", 11, new Color("344851")),
            rungLabel);
        var contactPropertyRow = PropertyRow(
            "ContactPropertyRow",
            Heading(siemens ? "Contact type" : "Instruction", 11, new Color("344851")),
            contactTypeSelector,
            Heading("Operand", 11, new Color("344851")),
            tagSelector,
            edgeMemoryLabel,
            edgeMemorySelector);
        var coilPropertyRow = PropertyRow(
            "CoilPropertyRow",
            Heading(siemens ? "Coil" : "Output instruction", 11, new Color("344851")),
            coilModeSelector,
            Heading("Operand", 11, new Color("344851")),
            coilSelector);
        var timerPresetLabel = Heading(siemens ? "PT" : "Preset", 11, new Color("344851"));
        var timerResetLabel = Heading("R", 11, new Color("344851"));
        var timerPropertyRow = PropertyRow(
            "TimerPropertyRow",
            Heading("Timer instance", 11, new Color("344851")),
            timerSelector,
            timerPresetLabel,
            timerPreset,
            timerPresetOperand,
            timerResetLabel,
            timerResetOperand);
        var counterPresetLabel = Heading("Preset", 11, new Color("344851"));
        var counterPropertyRow = PropertyRow(
            "CounterPropertyRow",
            Heading("Counter instance", 11, new Color("344851")),
            counterSelector,
            Heading("Type", 11, new Color("344851")),
            counterDataTypeSelector,
            counterPresetLabel,
            counterPreset);
        var timerDbPropertyRow = PropertyRow("TimerInstanceDbPropertyRow",
            Heading("Single instance DB", 11, new Color("344851")), timerDbNumber, timerDbName);
        var counterDbPropertyRow = PropertyRow("CounterInstanceDbPropertyRow",
            Heading("Single instance DB", 11, new Color("344851")), counterDbNumber, counterDbName);
        var counterLoadInputRow = PropertyRow("CounterLoadInputPropertyRow",
            Heading("LD input", 11, new Color("344851")), counterLoadInput);
        var counterResetInputRow = PropertyRow("CounterResetInputPropertyRow",
            Heading("R input", 11, new Color("344851")), counterResetInput);
        var compareDataTypeLabel = Heading("Type", 11, new Color("344851"));
        compareDataTypeLabel.Visible = siemens;
        compareDataType.Visible = siemens;
        var comparePropertyRow = PropertyRow(
            "ComparePropertyRow",
            Heading("Source", 11, new Color("344851")),
            compareLeft,
            compareOperator,
            compareDataTypeLabel,
            compareDataType,
            Heading("Operand", 11, new Color("344851")),
            compareRight);
        var numericPropertyRow = PropertyRow(
            "NumericPropertyRow",
            Heading("Source A", 11, new Color("344851")),
            numericSourceA,
            Heading("Source B", 11, new Color("344851")),
            numericSourceB,
            Heading("Source C", 11, new Color("344851")),
            numericSourceC,
            Heading("Destination", 11, new Color("344851")),
            numericDestination);
        var callPropertyRow = PropertyRow(
            "CallPropertyRow",
            Heading(siemens ? "Called block" : "Target routine", 11, new Color("344851")),
            callTarget);
        var returnValueLabel = Heading("Return value", 11, new Color("344851"));
        var returnValueSelector = new OptionButton
        {
            Name = "ReturnValueSelector",
            CustomMinimumSize = new Vector2(120, 32),
        };
        returnValueSelector.AddItem("RLO");
        returnValueSelector.AddItem("TRUE");
        returnValueSelector.AddItem("FALSE");
        returnValueSelector.AddItem("Value");
        var returnOperandLabel = Heading("BOOL operand", 11, new Color("344851"));
        var returnValueOperand = new LineEdit
        {
            Name = "ReturnValueOperand",
            PlaceholderText = "BOOL tag",
            CustomMinimumSize = new Vector2(230, 32),
        };
        var logixReturnMessage = Heading("RET has no editable operands.", 11, new Color("344851"));
        returnValueLabel.Visible = returnValueSelector.Visible = returnOperandLabel.Visible = returnValueOperand.Visible = siemens;
        logixReturnMessage.Visible = !siemens;
        var returnPropertyRow = PropertyRow(
            "ReturnPropertyRow",
            returnValueLabel, returnValueSelector, returnOperandLabel, returnValueOperand,
            logixReturnMessage);
        var jumpLabelPropertyRow = PropertyRow(
            "JumpLabelPropertyRow",
            Heading(siemens ? "Block-local label" : "Label name", 11, new Color("344851")),
            programControlLabel);
        foreach (var row in new[]
                 {
                     rungPropertyRow, contactPropertyRow, coilPropertyRow, timerPropertyRow,
                     counterPropertyRow, timerDbPropertyRow, counterDbPropertyRow, counterLoadInputRow, counterResetInputRow, comparePropertyRow, numericPropertyRow,
                     callPropertyRow, returnPropertyRow, jumpLabelPropertyRow,
                 })
            instructionPropertiesBody.AddChild(row);
        closeInstructionProperties.Pressed += () => { instructionPropertiesPopup.Hide(); canvas.GrabFocus(); };
        instructionPropertiesBackdrop.GuiInput += input =>
        {
            if (input is InputEventMouseButton { Pressed: true }) instructionPropertiesBackdrop.AcceptEvent();
        };
        instructionPropertiesLayer.AddChild(instructionPropertiesPopup);
        root.AddChild(instructionPropertiesLayer);

        EditorTarget? popupTarget = null;
        bool propertyNetworkLabel = false;
        var instructionContextMenu = new PopupMenu { Name = "InstructionContextMenu" };
        instructionContextMenu.AddItem(siemens ? "Properties…" : "Instruction Properties…", 0);
        instructionContextMenu.AddSeparator();
        instructionContextMenu.AddItem("Copy", 3);
        instructionContextMenu.AddItem("Cut", 7);
        instructionContextMenu.AddItem("Paste after selection", 4);
        instructionContextMenu.AddSeparator();
        instructionContextMenu.AddItem(siemens ? "Delete instruction / network" : "Delete instruction / rung", 1);
        instructionContextMenu.AddItem("Instruction help", 2);
        instructionContextMenu.AddItem(siemens ? "Copy network" : "Copy rung", 6);
        root.AddChild(instructionContextMenu);
        var emptyInstructionChooser = new PopupMenu
        {
            Name = "EmptyInstructionChooser",
            // Keep the supported subset browsable in a TIA-style dropdown; an
            // unbounded PopupMenu grows below the editor on normal displays.
            MaxSize = new Vector2I(420, 320),
        };
        // Siemens' empty box chooses a box instruction. Coils (SET/RESET),
        // timer-reset coil RT, and CTD's LD input are separate LAD elements or
        // pins, not standalone empty-box entries.
        foreach (var choice in TiaEmptyBoxChoices)
            emptyInstructionChooser.AddItem(choice.Label);
        root.AddChild(emptyInstructionChooser);

        void UpdateInstructionPropertyVisibility()
        {
            foreach (var row in new[]
                     {
                         rungPropertyRow, contactPropertyRow, coilPropertyRow, timerPropertyRow,
                         counterPropertyRow, timerDbPropertyRow, counterDbPropertyRow, counterLoadInputRow, counterResetInputRow, comparePropertyRow, numericPropertyRow,
                         callPropertyRow, returnPropertyRow, jumpLabelPropertyRow,
                     })
                row.Visible = false;
            if (document.Rungs.Count == 0) return;
            var rung = document.Rungs[Math.Clamp(selectedRung, 0, document.Rungs.Count - 1)];
            var activeBlockName = document.Blocks[document.ActiveBlockIndex].Name;
            instructionPropertyScope.Text = siemens
                ? $"{activeBlockName}  ›  Network {selectedRung + 1}"
                : $"MainProgram  ›  {activeBlockName}  ›  Rung {selectedRung}";
            if (selectedContact >= 0 && selectedBranch >= 0
                && selectedBranch < rung.Branches.Count
                && selectedContact < rung.Branches[selectedBranch].Contacts.Count)
            {
                var contact = rung.Branches[selectedBranch].Contacts[selectedContact];
                instructionPropertiesTitle.Text = contact.IsComparison
                    ? (siemens ? "Compare instruction properties" : "Compare Instruction Properties")
                    : (siemens ? "Contact properties" : "Contact Instruction Properties");
                (contact.IsComparison ? comparePropertyRow : contactPropertyRow).Visible = true;
                var edgeMemoryVisible = !contact.IsComparison && contact.EdgeMode != LadderEdgeMode.None;
                edgeMemoryLabel.Visible = edgeMemoryVisible;
                edgeMemorySelector.Visible = edgeMemoryVisible;
                return;
            }
            if (selectedOutput)
            {
                if (propertyNetworkLabel && rung.IsLabel)
                {
                    instructionPropertiesTitle.Text = siemens ? "LABEL properties" : "LBL Instruction Properties";
                    jumpLabelPropertyRow.Visible = true;
                }
                else if (rung.IsTimer || rung.IsTimerReset)
                {
                    instructionPropertiesTitle.Text = rung.IsTimerReset
                        ? (siemens ? "RT properties" : "RES Timer Properties")
                        : $"{(siemens ? rung.TimerKind.ToString() : rung.TimerKind.ToString())} properties";
                    timerPresetLabel.Visible = rung.IsTimer;
                    timerPreset.Visible = rung.IsTimer && !siemens;
                    timerPresetOperand.Visible = rung.IsTimer && siemens;
                    timerResetLabel.Visible = rung.IsTimer && siemens
                        && rung.TimerKind == LadderTimerKind.RetentiveOnDelay;
                    timerResetOperand.Visible = rung.IsTimer && siemens
                        && rung.TimerKind == LadderTimerKind.RetentiveOnDelay;
                    timerPropertyRow.Visible = true;
                    timerDbPropertyRow.Visible = siemens;
                    timerDbNumber.Editable = rung.IsTimer;
                    timerDbName.Editable = rung.IsTimer;
                }
                else if (rung.IsCounter || rung.IsCounterReset || rung.IsCounterLoad)
                {
                    instructionPropertiesTitle.Text = siemens ? "Counter instruction properties" : "Counter Instruction Properties";
                    var usesPreset = !rung.IsCounterReset;
                    counterPresetLabel.Visible = usesPreset;
                    counterPreset.Visible = usesPreset;
                    counterDataTypeSelector.Visible = rung.IsCounter || rung.IsCounterLoad;
                    counterPropertyRow.Visible = true;
                    counterDbPropertyRow.Visible = siemens;
                    counterLoadInputRow.Visible = siemens && rung.IsCounter && rung.CounterKind == LadderCounterKind.CountDown;
                    counterLoadInput.Editable = counterLoadInputRow.Visible;
                    counterResetInputRow.Visible = siemens && rung.IsCounter && rung.CounterKind == LadderCounterKind.CountUp;
                    counterResetInput.Editable = counterResetInputRow.Visible;
                    counterDbNumber.Editable = rung.IsCounter || rung.IsCounterLoad;
                    counterDbName.Editable = rung.IsCounter || rung.IsCounterLoad;
                }
                else if (rung.IsNumericOperation)
                {
                    instructionPropertiesTitle.Text = $"{rung.NumericOperationKind} properties";
                    numericPropertyRow.Visible = true;
                }
                else if (rung.IsCall)
                {
                    instructionPropertiesTitle.Text = siemens ? "CALL properties" : "JSR Properties";
                    callPropertyRow.Visible = true;
                }
                else if (rung.IsReturn)
                {
                    instructionPropertiesTitle.Text = siemens ? "RETURN properties" : "RET Properties";
                    returnPropertyRow.Visible = true;
                    returnOperandLabel.Visible = returnValueOperand.Visible = siemens
                        && returnValueSelector.Selected == (int)LadderReturnValueKind.Operand;
                }
                else if (rung.IsJump || !rung.HasTerminalAction && rung.IsLabel)
                {
                    instructionPropertiesTitle.Text = rung.IsJump
                        ? (siemens ? "JMP properties" : "JMP Instruction Properties")
                        : (siemens ? "LABEL properties" : "LBL Instruction Properties");
                    jumpLabelPropertyRow.Visible = true;
                }
                else
                {
                    instructionPropertiesTitle.Text = siemens ? "Coil properties" : "Output Instruction Properties";
                    coilPropertyRow.Visible = true;
                }
                return;
            }
            instructionPropertiesTitle.Text = siemens ? "Network properties" : "Rung Properties";
            rungPropertyRow.Visible = true;
        }

        LadderEditTransaction? propertyEdit = null;
        bool propertyDirty = false;
        bool tagFormDirty = false, userConstantFormDirty = false, blockFormDirty = false, taskFormDirty = false, interfaceFormDirty = false, loadingFormControls = false;
        long tagFormRevision = 0, userConstantFormRevision = 0, blockFormRevision = 0, taskFormRevision = 0, interfaceFormRevision = 0;
        string? selectedUserConstantId = null;
        Tree? globalDbDeclarations = null;
        void UpdateGlobalDbDeclarationGridEditability()
        {
            var firstMember = globalDbDeclarations?.GetRoot()?.GetFirstChild();
            if (firstMember is null) return;

            var canEdit = !(propertyDirty || tagFormDirty || userConstantFormDirty || blockFormDirty || taskFormDirty || interfaceFormDirty);
            for (var member = firstMember; member is not null; member = member.GetNext())
            {
                var startValueEditable = Enum.TryParse<PlcVariableType>(member.GetText(1), true, out var type)
                    && type is not (PlcVariableType.Timer or PlcVariableType.Counter);
                for (var column = 0; column < globalDbDeclarations!.Columns; column++)
                    member.SetEditable(column, canEdit && (column != 2 || startValueEditable));
            }
        }
        bool CurrentForm(long revision) { if (revision == document.Revision) return true; output.Text = "[color=#d17a00]This form is stale.[/color] Discard it and reopen the target before applying."; return false; }
        string propertyNetworkId = "", propertyNodeId = "";
        bool propertyOutput = false;
        var propertyActions = new HBoxContainer { Name = "InstructionPropertyActions" };
        var applyProperties = new Button { Name = "ApplyInstructionProperties", Text = "Apply" };
        var cancelProperties = new Button { Name = "CancelInstructionProperties", Text = "Cancel" };
        var propertyStatus = new Label { Name = "InstructionPropertyStatus", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        propertyActions.AddChild(applyProperties); propertyActions.AddChild(cancelProperties);
        instructionPropertiesBody.AddChild(propertyStatus); instructionPropertiesBody.AddChild(propertyActions);
        void CancelProperties()
        {
            propertyEdit?.Cancel(); propertyEdit = null; propertyDirty = false;
            instructionPropertiesPopup.Hide(); RefreshEditor(); canvas.GrabFocus();
        }
        string SelectedText(OptionButton selector) => selector.Selected >= 0 ? selector.GetItemText(selector.Selected) : "";
        bool ApplyProperties()
        {
            if (propertyEdit is null) return true;
            if (!propertyDirty) { CancelProperties(); return true; }
            try
            {
                var draft = propertyEdit.Draft;
                var rungIndex = draft.Rungs.FindIndex(r => r.Id == propertyNetworkId);
                if (rungIndex < 0) throw new InvalidOperationException("Instruction target is unavailable.");
                var rung = draft.Rungs[rungIndex];
                if (propertyNodeId.Length > 0 && !propertyOutput)
                {
                    var node = EditableLadderTree.Find(rung.Root, propertyNodeId) ?? throw new InvalidOperationException("Operand target is unavailable.");
                    node = node.Kind == LadderNodeKind.Compare
                        ? node with { Variable = SelectedText(compareLeft), CompareOperator = (LadderCompareOperator)compareOperator.Selected,
                            ComparisonValueType = LadderComparisonValueTypePresentation.FromTiaSelector(compareDataType.Selected), RightOperand = compareRight.Text.Trim() }
                        : node with { Variable = SelectedText(tagSelector), NormallyClosed = contactTypeSelector.Selected == 1,
                            EdgeMode = contactTypeSelector.Selected == 2 ? LadderEdgeMode.Rising : contactTypeSelector.Selected == 3 ? LadderEdgeMode.Falling : LadderEdgeMode.None,
                            EdgeMemoryOperand = contactTypeSelector.Selected is 2 or 3 && edgeMemorySelector.Selected > 0
                                ? SelectedText(edgeMemorySelector) : null };
                    rung.SetRoot(EditableLadderTree.Replace(rung.Root, propertyNodeId, node));
                }
                else if (!propertyOutput) rung.Label = rungLabel.Text.Trim();
                else if (propertyNetworkLabel)
                {
                    var labelName = programControlLabel.Text.Trim();
                    if (siemens && !TiaProgramControlLabel.IsValid(labelName))
                    {
                        propertyStatus.Text = TiaProgramControlLabel.ValidationMessage;
                        return false;
                    }
                    rung.NetworkLabelName = labelName;
                    if (!rung.IsJump) rung.ProgramControlLabel = labelName;
                }
                else if (rung.IsTimer || rung.IsTimerReset)
                {
                    rung.TimerVariable = SelectedText(timerSelector);
                    if (!rung.IsTimerReset)
                    {
                        if (siemens)
                        {
                            rung.TimerPresetOperand = timerPresetOperand.Text.Trim();
                            rung.TimerPresetAssigned = rung.TimerPresetOperand.Length > 0;
                            if (LadderTimeLiteral.TryParse(rung.TimerPresetOperand, out var parsedPreset))
                                rung.TimerPreset = parsedPreset;
                            if (rung.TimerKind == LadderTimerKind.RetentiveOnDelay)
                                rung.TimerResetOperand = timerResetOperand.Text.Trim();
                        }
                        else
                        {
                            rung.TimerPreset = TimeSpan.FromMilliseconds(timerPreset.Value);
                            rung.TimerPresetAssigned = true;
                            rung.TimerPresetOperand = LadderTimeLiteral.Format(rung.TimerPreset);
                        }
                        if (siemens) rung.TimerInstanceDataBlock = new LadderInstanceDataBlock((int)timerDbNumber.Value, timerDbName.Text.Trim());
                    }
                }
                else if (rung.IsCounter || rung.IsCounterLoad || rung.IsCounterReset)
                { rung.CounterVariable = SelectedText(counterSelector); if (!rung.IsCounterReset) rung.CounterPreset = (long)counterPreset.Value;
                    if (rung.IsCounter && rung.CounterKind == LadderCounterKind.CountDown) rung.CounterLoadInput = counterLoadInput.Text.Trim();
                    if (rung.IsCounter && rung.CounterKind == LadderCounterKind.CountUp) rung.CounterResetInput = counterResetInput.Text.Trim();
                    if (rung.IsCounter || rung.IsCounterLoad)
                    {
                        rung.CounterValueType = (LadderCounterValueType)counterDataTypeSelector.Selected;
                        if (siemens) rung.CounterInstanceDataBlock = new LadderInstanceDataBlock((int)counterDbNumber.Value, counterDbName.Text.Trim());
                    } }
                else if (rung.IsNumericOperation)
                { rung.NumericSourceA = numericSourceA.Text.Trim(); rung.NumericSourceB = numericSourceB.Text.Trim(); rung.NumericSourceC = numericSourceC.Text.Trim(); rung.NumericDestination = SelectedText(numericDestination); }
                else if (rung.IsCall) rung.CallTarget = callTarget.Selected >= 0 ? callTarget.GetItemMetadata(callTarget.Selected).AsString() : "";
                else if (rung.IsReturn && siemens)
                {
                    rung.ReturnValueKind = (LadderReturnValueKind)returnValueSelector.Selected;
                    rung.ReturnValueOperand = rung.ReturnValueKind == LadderReturnValueKind.Operand
                        ? returnValueOperand.Text.Trim() : string.Empty;
                }
                else if (rung.IsJump || !rung.HasTerminalAction && rung.IsLabel)
                {
                    var labelName = programControlLabel.Text.Trim();
                    if (siemens && !TiaProgramControlLabel.IsValid(labelName))
                    {
                        propertyStatus.Text = TiaProgramControlLabel.ValidationMessage;
                        return false;
                    }
                    rung.ProgramControlLabel = labelName;
                }
                else if (!rung.IsReturn) { rung.CoilVariable = SelectedText(coilSelector); rung.CoilMode = (LadderCoilMode)coilModeSelector.Selected; }
                var targetNodeId = propertyNetworkLabel ? rung.NetworkLabelId
                    : propertyOutput ? rung.TerminalActionId : propertyNodeId;
                draft.EnsureInstanceDataBlocks();
                var validationIssues = LadderCompiler.Validate(draft.BuildProgram());
                // Compiler provenance paths use stable block/network IDs, while
                // EditorDiagnosticTarget handles the legacy positional paths.
                // Route DB assignment errors directly to the edited instruction.
                var issue = validationIssues.FirstOrDefault(i => i.Code == "VC016"
                    && i.Path.Contains($".networks[{rung.Id}].", StringComparison.Ordinal))
                    ?? (targetNodeId.Length == 0 ? null : validationIssues.FirstOrDefault(i =>
                        EditorDiagnosticTarget.Capture(draft, i.Path)?.Target is { } target
                        && target.NetworkId == rung.Id && target.NodeId == targetNodeId))
                    ?? (!propertyOutput ? null : validationIssues.FirstOrDefault(i =>
                        EditorDiagnosticTarget.Capture(draft, i.Path)?.Target is { } target
                        && target.NetworkId == rung.Id && target.Role == EditorOperandRole.Output));
                if (issue is not null) { propertyStatus.Text = issue.Message; return false; }
                if (!propertyEdit.TryCommit(_ladderHistory, ChallengeMutationLocked, out var error)) { propertyStatus.Text = error; return false; }
                propertyEdit = null; propertyDirty = false;
                BeginEdit("Apply instruction properties", recordHistory: false);
                instructionPropertiesPopup.Hide(); RefreshEditor(); canvas.GrabFocus(); return true;
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException)
            { propertyStatus.Text = error.Message; return false; }
        }
        void StageProperty()
        {
            if (refreshingEditor || propertyEdit is null) return;
            propertyDirty = true; propertyStatus.Text = "Pending edit · Apply commits all fields; Cancel discards";
            UpdateGlobalDbDeclarationGridEditability();
        }
        void OpenInstructionProperties()
        {
            ResolvePendingEditorForms(() =>
            {
                propertyNetworkLabel = selectedOutput && canvas.NetworkLabelSelected;
                RefreshEditor(); UpdateInstructionPropertyVisibility();
                if (document.Rungs.Count == 0) return;
                if (selectedOutput && !canvas.NetworkLabelSelected && document.Rungs[selectedRung].IsCall)
                {
                    OpenCallAuthoring(document, document.Blocks[document.ActiveBlockIndex].Id, document.Rungs[selectedRung].Id,
                        () => { BeginEdit("Apply CALL", recordHistory: false); RefreshEditor(); canvas.GrabFocus(); });
                    return;
                }
                propertyEdit = new LadderEditTransaction(document, "Edit instruction properties"); propertyDirty = false;
                propertyNetworkId = document.Rungs[selectedRung].Id; propertyOutput = selectedOutput;
                propertyNodeId = selectedOutput ? "" : canvas.SelectedNodeId;
                if (propertyNodeId.Length == 0 && HasSelectedContact()) propertyNodeId = document.Rungs[selectedRung].Branches[selectedBranch].Contacts[selectedContact].Id;
                propertyStatus.Text = "Changes stay pending until Apply";
                instructionPropertiesPopup.Show(); FocusFirstChallengeEditorControl(instructionPropertiesPopup);
            });
        }
        PlcVariableType SelectedTagType() => tagType.Selected switch
        {
            1 => PlcVariableType.Timer,
            2 => PlcVariableType.Counter,
            3 => PlcVariableType.Int,
            4 => PlcVariableType.DInt,
            5 => PlcVariableType.Real,
            6 => PlcVariableType.Struct,
            7 => PlcVariableType.Array,
            8 => PlcVariableType.String,
            9 => PlcVariableType.Enum,
            10 => PlcVariableType.Time,
            _ => PlcVariableType.Bool,
        };

        PlcVariableRole SelectedTagRole(PlcVariableType type) =>
            type is PlcVariableType.Timer or PlcVariableType.Counter
                ? PlcVariableRole.Memory
                : (PlcVariableRole)tagRole.Selected;

        static string FormatTagInitialValue(PlcVariable variable) => variable.Type switch
        {
            PlcVariableType.Struct => $"{variable.Aggregate?.Fields?.Count ?? 0} typed fields",
            PlcVariableType.Array => $"[{variable.Aggregate?.LowerBound}..{(long)(variable.Aggregate?.LowerBound ?? 0) + (variable.Aggregate?.Length ?? 0) - 1}] {variable.Aggregate?.ElementType}",
            PlcVariableType.String or PlcVariableType.Enum => (string)variable.InitialValue,
            PlcVariableType.Bool => ((bool)variable.InitialValue).ToString().ToUpperInvariant(),
            PlcVariableType.Real => Convert.ToDouble(variable.InitialValue,
                System.Globalization.CultureInfo.InvariantCulture).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            PlcVariableType.Int or PlcVariableType.DInt => Convert.ToInt64(variable.InitialValue,
                System.Globalization.CultureInfo.InvariantCulture).ToString(System.Globalization.CultureInfo.InvariantCulture),
            PlcVariableType.Time => LadderTimeLiteral.Format((TimeSpan)variable.InitialValue),
            _ => "0",
        };

        string[]? SelectedEnumMembers(PlcVariableType type) => type == PlcVariableType.Enum
            ? tagEnumMembers.Text.Split(',').Select(member => member.Trim()).ToArray() : null;

        bool TryReadTagInitialValue(PlcVariableType type, out object initialValue)
        {
            if (type is PlcVariableType.String or PlcVariableType.Enum)
            {
                initialValue = tagInitialValue.Text;
                // Document preflight validates enum members and initial membership before history changes.
                return true;
            }
            if (PlcAggregates.IsAggregate(type))
            {
                if (aggregateTagEditor.TryRead(type, out _, out initialValue, out var aggregateError)) return true;
                output.Text = $"[color=#d64545]Tag definition rejected.[/color] {Escape(aggregateError)}"; return false;
            }
            if (LadderEditorDocument.TryParseInitialValue(type, tagInitialValue.Text, out initialValue, out var error))
                return true;
            output.Text = $"[color=#d64545]Tag definition rejected.[/color] {Escape(error)}";
            return false;
        }

        bool PreflightTagEdit(Func<LadderEditorDocument, PlcVariable> edit)
        {
            var preview = new LadderEditorDocument(); preview.RestoreSnapshot(document.CaptureSnapshot());
            try { edit(preview); return true; }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException)
            {
                output.Text = $"[color=#d64545]Tag definition rejected.[/color] {Escape(error.Message)}";
                return false;
            }
        }

        bool ValidateSiemensTagAddress(string? excludedTagName, PlcVariableType type, PlcVariableRole role)
        {
            if (!siemens || SiemensPlcTagAddress.TryValidate(tagAddress.Text, type, role,
                    document.Tags, excludedTagName, out var addressError))
            {
                tagAddressValidation.Text = string.Empty;
                tagAddressValidation.Visible = false;
                return true;
            }
            tagAddressValidation.Text = $"ADDRESS REJECTED · {addressError}";
            tagAddressValidation.Visible = true;
            output.Text = $"[color=#d64545]Tag definition rejected.[/color] {Escape(addressError)}";
            tagAddress.GrabFocus();
            return false;
        }

        string SelectedTagBinding() => tagBinding.Selected >= 0
            ? tagBinding.GetItemMetadata(tagBinding.Selected).AsString()
            : string.Empty;

        void RefreshTagBindingOptions(PlcVariableType type, PlcVariableRole role, string currentBinding)
        {
            tagBinding.Clear();
            tagBinding.AddItem("<unbound>  ·  no scene I/O exchange");
            tagBinding.SetItemMetadata(0, string.Empty);
            if (type == PlcVariableType.Bool && role == PlcVariableRole.Input)
            {
                foreach (var operatorBinding in new[] { "operator.start", "operator.stop" })
                {
                    tagBinding.AddItem($"{operatorBinding}  [SIMULATOR PULSE]");
                    tagBinding.SetItemMetadata(tagBinding.ItemCount - 1, operatorBinding);
                }
            }
            if (role is PlcVariableRole.Input or PlcVariableRole.Output
                && type is PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.Struct or PlcVariableType.Array or PlcVariableType.String or PlcVariableType.Enum)
            {
                foreach (var scenePoint in ScenePoints().Where(candidate =>
                             SceneIoBindingValidator.TypesCompatible(type, candidate.Type)
                             && candidate.Owner.Equals(
                                 role == PlcVariableRole.Input ? "PC" : "PLC",
                                 StringComparison.OrdinalIgnoreCase)))
                {
                    var direction = role == PlcVariableRole.Input ? "PC → PLC" : "PLC → SCENE";
                    tagBinding.AddItem($"{scenePoint.Name}  [{scenePoint.Type} · {direction}]");
                    tagBinding.SetItemMetadata(tagBinding.ItemCount - 1, scenePoint.Name);
                    tagBinding.SetItemTooltip(tagBinding.ItemCount - 1, scenePoint.Purpose);
                }
            }
            var selected = -1;
            for (var index = 0; index < tagBinding.ItemCount; index++)
            {
                if (tagBinding.GetItemMetadata(index).AsString() == currentBinding)
                {
                    selected = index;
                    break;
                }
            }
            if (selected < 0 && currentBinding.Length > 0)
            {
                tagBinding.AddItem($"⚠ {currentBinding}  [UNAVAILABLE / INCOMPATIBLE]");
                tagBinding.SetItemMetadata(tagBinding.ItemCount - 1, currentBinding);
                selected = tagBinding.ItemCount - 1;
            }
            tagBinding.Select(Math.Max(0, selected));
            var bindableType = type is PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.Struct or PlcVariableType.Array or PlcVariableType.String or PlcVariableType.Enum;
            tagBinding.Disabled = !bindableType || role == PlcVariableRole.Memory;
            var binding = SelectedTagBinding();
            var selectedPoint = ScenePoints().FirstOrDefault(candidate => candidate.Name == binding);
            if (binding.Length == 0)
            {
                tagBindingStatus.Text = bindableType && role != PlcVariableRole.Memory
                    ? "UNBOUND · tag is internal to the offline controller"
                    : "NO SCENE BINDING · this tag type/role is controller-internal";
                tagBindingStatus.AddThemeColorOverride("font_color", new Color("4f6874"));
            }
            else if (binding.StartsWith("operator.", StringComparison.Ordinal))
            {
                tagBindingStatus.Text = $"SIMULATOR OPERATOR INPUT · {binding} · momentary pulse only";
                tagBindingStatus.AddThemeColorOverride("font_color", new Color("075985"));
            }
            else if (selectedPoint is not null)
            {
                tagBindingStatus.Text = $"ACTIVE SCENE · {selectedPoint.Name} · {selectedPoint.Type} · owner {selectedPoint.Owner}\nPURPOSE · {selectedPoint.Purpose}";
                tagBindingStatus.TooltipText = selectedPoint.Purpose;
                tagBindingStatus.AddThemeColorOverride("font_color", new Color("075d35"));
            }
            else
            {
                tagBindingStatus.Text = $"INCOMPATIBLE · '{binding}' is unavailable for {type} {role} in the active scene";
                tagBindingStatus.AddThemeColorOverride("font_color", new Color("8c1d18"));
            }
        }

        bool BeginEdit(string description, bool recordHistory = true)
        {
            if (refreshingEditor)
            {
                // Programmatic field refresh raises the same signals as edits.
                // Ignore it silently; no user operation was attempted here.
                return false;
            }
            var request = new EditorCommandRequest(EditorCommandId.BeginMutation, EditorTarget.CaptureProject(document), Description: description);
            if (!LadderEditorCommands.CanExecute(document, request, ChallengeMutationLocked, out _))
            { ShowChallengeLocked(); return false; }
            if (recordHistory && !LadderEditorCommands.Execute(document, _ladderHistory, request, ChallengeMutationLocked).Applied) return false;
            ChallengeDraftChanged?.Invoke();
            _ladderMonitorMatchesLoadedProgram = false;
            foreach (var ladderCanvas in _ladderCanvases) ladderCanvas.SetMonitorSnapshot(null);
            _ladderWorkspaceStatus.Text = "OFFLINE EDIT CHANGED · VERIFY + LOAD TO MONITOR";
            _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("f1aa5b"));
            validationIssueList?.Clear();
            if (validationSummary is not null)
            {
                validationSummary.Text = "NOT VERIFIED · run Verify + Load Offline";
                validationSummary.AddThemeColorOverride("font_color", new Color("b36c16"));
            }
            return true;
        }

        bool BeginProjectMetadataEdit(string description)
        {
            if (refreshingEditor || ChallengeMutationLocked) return false;
            var request = new EditorCommandRequest(EditorCommandId.BeginMutation, EditorTarget.CaptureProject(document), Description: description);
            if (!LadderEditorCommands.Execute(document, _ladderHistory, request, ChallengeMutationLocked).Applied) return false;
            ChallengeDraftChanged?.Invoke();
            return true;
        }

        Label? interfaceHeading = null;
        Label? interfaceExecutionNote = null;
        Label? tagTableContextHeading = null;
        Action? refreshUserConstants = null;
        Action? loadUserConstantForm = null;
        Action? discardUserConstantForm = null;
        Action? refreshEditorAfterInitialization = null;
        Button? renameTagTableAction = null;
        Button? removeTagTableAction = null;
        RichTextLabel? blockInterfaceSummary = null;
        VBoxContainer? globalDbDocument = null;
        RichTextLabel? globalDbHeading = null;
        Action? restoreActiveEditorDocumentBody = null;

        string SelectedTagTableMembership() => tagTableMembership.Selected >= 0
            ? tagTableMembership.GetItemMetadata(tagTableMembership.Selected).AsString()
            : activeTagTableName ?? "Default tag table";

        void RefreshTagTableEditorControls()
        {
            var tableForForm = selectedTagIndex >= 0 && selectedTagIndex < document.Tags.Count
                ? document.Tags[selectedTagIndex].TagTable
                : activeTagTableName ?? "Default tag table";
            var wasLoading = loadingFormControls;
            loadingFormControls = true;
            if (!tagFormDirty)
            {
                tagTableMembership.Clear();
                foreach (var tableName in document.TagTables)
                {
                    tagTableMembership.AddItem(tableName);
                    tagTableMembership.SetItemMetadata(tagTableMembership.ItemCount - 1, tableName);
                }
                var selectedTable = Enumerable.Range(0, tagTableMembership.ItemCount)
                    .FirstOrDefault(index => tagTableMembership.GetItemMetadata(index).AsString()
                        .Equals(tableForForm, StringComparison.OrdinalIgnoreCase), -1);
                if (selectedTable >= 0) tagTableMembership.Select(selectedTable);
            }
            loadingFormControls = wasLoading;
            if (tagTableContextHeading is not null)
                tagTableContextHeading.Text = activeTagTableName is null
                    ? "ALL TAGS"
                    : $"TAG TABLE: {activeTagTableName.ToUpperInvariant()}";
            var isUserTable = activeTagTableName is not null
                && !activeTagTableName.Equals("Default tag table", StringComparison.OrdinalIgnoreCase)
                && document.TagTables.Contains(activeTagTableName, StringComparer.OrdinalIgnoreCase);
            if (renameTagTableAction is not null)
                renameTagTableAction.Disabled = !isUserTable || tagFormDirty || userConstantFormDirty || ChallengeMutationLocked;
            if (removeTagTableAction is not null)
                removeTagTableAction.Disabled = !isUserTable || tagFormDirty || userConstantFormDirty || ChallengeMutationLocked
                    || document.Tags.Any(tag => tag.TagTable.Equals(activeTagTableName, StringComparison.OrdinalIgnoreCase))
                    || document.UserConstants.Any(constant => constant.TagTable.Equals(activeTagTableName, StringComparison.OrdinalIgnoreCase));
        }

        Action? refreshTreeActions = null;
        void ReconcileTagTableEditorState()
        {
            var staleActiveKey = activeTagTableName is not null
                && !document.TagTables.Contains(activeTagTableName, StringComparer.OrdinalIgnoreCase)
                && string.Equals(activeEditorDocumentKey, $"tags:{activeTagTableName}", StringComparison.Ordinal);
            var staleKeys = editorTagDocuments
                .Where(pair => pair.Value is not null
                    && !document.TagTables.Contains(pair.Value, StringComparer.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .ToArray();

            if (activeTagTableName is not null)
            {
                var canonicalName = document.TagTables.FirstOrDefault(name =>
                    name.Equals(activeTagTableName, StringComparison.OrdinalIgnoreCase));
                activeTagTableName = canonicalName
                    ?? document.TagTables.FirstOrDefault(name => name.Equals("Default tag table", StringComparison.OrdinalIgnoreCase))
                    ?? document.TagTables.FirstOrDefault();
            }

            foreach (var key in staleKeys) CloseEditorDocument(key);

            // Undoing a rename can restore the old table after its document tab
            // was closed by the rename action. Reopen the built-in table so the
            // editor never remains attached to a missing table name.
            if (staleActiveKey && activeTagTableName is not null)
            {
                selectedTagIndex = -1;
                OpenTagDocument(activeTagTableName);
            }
        }

        void RefreshEditor()
        {
            UpdateGlobalDbDeclarationGridEditability();
            if (propertyDirty || tagFormDirty || userConstantFormDirty || blockFormDirty || taskFormDirty || interfaceFormDirty) return;
            ReconcileTagTableEditorState();
            refreshingEditor = true;
            RefreshTagTableEditorControls();
            refreshUserConstants?.Invoke();
            document.EnsureInstanceDataBlocks();
            var activeBlockName = document.Blocks.Count > 0
                ? document.Blocks[Math.Clamp(document.ActiveBlockIndex, 0, document.Blocks.Count - 1)].Name
                : "<no block>";
            var projectDirty = LadderEditorProjectJson.HasUnsavedChanges(document, _ladderSavedProjectJson);
            IntegratedDraftChanged?.Invoke();
            var projectFileName = _ladderProjectPath is null
                ? "Untitled.rpproj.json"
                : System.IO.Path.GetFileName(_ladderProjectPath);
            var activeBlockOrdinal = Math.Clamp(document.ActiveBlockIndex, 0, Math.Max(0, document.Blocks.Count - 1));
            var activeBlockType = document.Blocks.Count > 0 ? document.Blocks[activeBlockOrdinal].BlockType : LadderBlockType.Function;
            var activeBlockCode = activeBlockType switch
            {
                LadderBlockType.OrganizationBlock => "OB",
                LadderBlockType.FunctionBlock => "FB",
                LadderBlockType.DataBlock => "DB",
                _ => "FC",
            };
            editorTabTitle.Text = siemens
                ? $"{(projectDirty ? "* " : string.Empty)}{activeBlockName} [{activeBlockCode}]  —  {projectFileName}"
                : $"{(projectDirty ? "* " : string.Empty)}MainProgram — {activeBlockName} [LAD]  —  {projectFileName}";
            editorTabTitle.TooltipText = projectDirty
                ? $"Unsaved project changes · {projectFileName}"
                : $"Project is saved · {projectFileName}";
            if (siemens)
            {
                // Block tabs identify declarations by stable ID. Keep tabs for
                // deleted blocks disabled so Undo can restore the same tab
                // without allowing it to target a shifted ordinal.
                foreach (var (key, tab) in editorDocumentButtons)
                {
                    if (!key.StartsWith("block:", StringComparison.Ordinal)) continue;
                    if (!TryResolveBlockDocument(key, out var tabBlockIndex))
                    {
                        tab.Disabled = true;
                        tab.Text = "[Deleted block]";
                        tab.TooltipText = "This block was deleted. Undo restores this tab's original block identity.";
                        continue;
                    }

                    var tabBlock = document.Blocks[tabBlockIndex];
                    var tabBlockCode = tabBlock.BlockType switch
                    {
                        LadderBlockType.OrganizationBlock => "OB",
                        LadderBlockType.FunctionBlock => "FB",
                        LadderBlockType.DataBlock => "DB",
                        _ => "FC",
                    };
                    tab.Disabled = false;
                    tab.Text = $"{tabBlock.Name} [{tabBlockCode}{tabBlockIndex + 1}]";
                    tab.TooltipText = $"{tabBlock.Name} · {tabBlock.BlockType} · stable block identity";
                }

                foreach (var (key, tableName) in editorTagDocuments)
                {
                    if (!editorDocumentButtons.TryGetValue(key, out var tagTab)) continue;
                    var tagDocumentName = tableName ?? "Show all tags";
                    tagTab.Text = $"{(projectDirty ? "* " : string.Empty)}{tagDocumentName} — {projectFileName}";
                    tagTab.TooltipText = $"{(tableName is null ? "All PLC tags" : $"PLC tag table · {tableName}")} · "
                        + (projectDirty ? "Unsaved project changes. " : "Project is saved. ")
                        + "Use the close button beside this tab to close it.";
                }

                foreach (var (key, watchTableId) in editorWatchTableDocuments)
                {
                    if (!editorDocumentButtons.TryGetValue(key, out var watchTab)) continue;
                    var watchTableIndex = document.WatchTables.FindIndex(table => table.Id.Equals(watchTableId, StringComparison.Ordinal));
                    if (watchTableIndex < 0)
                    {
                        watchTab.Disabled = true;
                        watchTab.Text = "[Deleted watch table]";
                        watchTab.TooltipText = "This watch table was deleted. Undo restores its original identity and tab.";
                        continue;
                    }

                    var watchTable = document.WatchTables[watchTableIndex];
                    watchTab.Disabled = false;
                    watchTab.Text = $"{(projectDirty ? "* " : string.Empty)}{watchTable.Name} — {projectFileName}";
                    watchTab.TooltipText = $"Watch table · {watchTable.Name} · stable table identity. Values are simulator-only.";
                }

                foreach (var (key, ownerLocator) in editorInstanceDbDocuments)
                {
                    if (!editorDocumentButtons.TryGetValue(key, out var instanceDbTab)) continue;
                    var owner = ResolveInstanceDbOwner(ownerLocator);
                    if (owner?.DataBlock is null)
                    {
                        instanceDbTab.Disabled = true;
                        instanceDbTab.Text = "[Deleted instance DB]";
                        instanceDbTab.TooltipText = "This FB or IEC instance no longer exists. The tab will not retarget another DB.";
                        continue;
                    }

                    var instanceDb = owner.Value.DataBlock;
                    instanceDbTab.Disabled = false;
                    instanceDbTab.Text = $"{(projectDirty ? "* " : string.Empty)}{instanceDb.Name} [DB{instanceDb.Number}] — {projectFileName}";
                    instanceDbTab.TooltipText = $"Instance DB · stable owner {ownerLocator} · offline project definition.";
                }

                if (activeEditorDocumentKey.StartsWith("block:", StringComparison.Ordinal)
                    && !TryResolveBlockDocument(activeEditorDocumentKey, out _)
                    && document.Blocks.Count > 0)
                {
                    activeEditorDocumentKey = BlockDocumentKey(document.Blocks[activeBlockOrdinal].Id);
                    SetEditorDocumentBody(activeEditorDocumentKey);
                }

                if (TryResolveBlockDocument(activeEditorDocumentKey, out var activeTabBlockIndex))
                {
                    var activeTabKey = BlockDocumentKey(document.Blocks[activeTabBlockIndex].Id);
                    AddEditorDocumentTab(activeTabKey, editorTabTitle.Text);
                    editorDocumentButtons[activeTabKey].Disabled = false;
                    editorDocumentButtons[activeTabKey].ButtonPressed = true;
                }
                else if (editorInstanceDbDocuments.TryGetValue(activeEditorDocumentKey, out var activeInstanceDbOwner))
                {
                    if (ResolveInstanceDbOwner(activeInstanceDbOwner)?.DataBlock is not null)
                        OpenInstanceDbDocumentByOwner(activeInstanceDbOwner);
                    else if (document.Blocks.Count > 0)
                    {
                        activeEditorDocumentKey = BlockDocumentKey(document.Blocks[activeBlockOrdinal].Id);
                        SetEditorDocumentBody(activeEditorDocumentKey);
                    }
                }
                else if (editorWatchTableDocuments.TryGetValue(activeEditorDocumentKey, out var activeWatchTableId))
                {
                    var activeWatchTableIndex = document.WatchTables.FindIndex(table => table.Id.Equals(activeWatchTableId, StringComparison.Ordinal));
                    if (activeWatchTableIndex >= 0)
                    {
                        var activeWatchTable = document.WatchTables[activeWatchTableIndex];
                        editorDocumentButtons[activeEditorDocumentKey].Disabled = false;
                        editorDocumentButtons[activeEditorDocumentKey].ButtonPressed = true;
                        editorTabTitle.Text = $"{(projectDirty ? "* " : string.Empty)}{activeWatchTable.Name} — {projectFileName}";
                        editorTabTitle.TooltipText = $"Watch table · {activeWatchTable.Name}. Values and simulated force status are offline simulator state.";
                    }
                    else if (FindOpenEditorDocumentFallback(activeEditorDocumentKey) is { } fallbackWatchDocument
                        && editorWatchTableDocuments.TryGetValue(fallbackWatchDocument, out var fallbackWatchTableId)
                        && document.WatchTables.FirstOrDefault(table => table.Id.Equals(fallbackWatchTableId, StringComparison.Ordinal)) is { } fallbackWatchTable)
                    {
                        OpenWatchTableDocument(fallbackWatchTable);
                    }
                    else if (document.Blocks.Count > 0)
                    {
                        activeEditorDocumentKey = BlockDocumentKey(document.Blocks[activeBlockOrdinal].Id);
                        SetEditorDocumentBody(activeEditorDocumentKey);
                    }
                }
            }
            while (blockItems.Count < document.Blocks.Count)
            {
                var index = blockItems.Count;
                var item = organization.CreateItem(blockFolder);
                item.SetMetadata(0, $"program:{index}");
                blockItems.Add(item);
            }
            for (var index = 0; index < document.Blocks.Count; index++)
            {
                var blockCode = document.Blocks[index].BlockType switch
                {
                    LadderBlockType.OrganizationBlock => "OB",
                    LadderBlockType.FunctionBlock => "FB",
                    LadderBlockType.DataBlock => "DB",
                    _ => "FC",
                };
                // Block numbers are not stored in this project model. Do not
                // imply TIA identities by deriving them from tree order.
                var suffix = siemens ? $" [{blockCode}]" : string.Empty;
                blockItems[index].SetText(0, document.Blocks[index].Name + suffix);
                blockItems[index].Visible = true;
            }
            for (var index = document.Blocks.Count; index < blockItems.Count; index++)
                blockItems[index].Visible = false;
            if (siemens && instanceDbFolder is not null)
            {
                var instanceDbs = document.Instances.Select(instance => instance.InstanceDataBlock)
                    .Concat(document.Blocks.SelectMany(block => block.Rungs)
                    .Where(rung => rung.IsTimer || rung.IsTimerReset || rung.IsCounter || rung.IsCounterReset || rung.IsCounterLoad)
                    .Select(rung => rung.IsTimer || rung.IsTimerReset ? rung.TimerInstanceDataBlock : rung.CounterInstanceDataBlock))
                    .Where(db => db is not null).Select(db => db!)
                    .GroupBy(db => db.Number).Select(group => group.First()).OrderBy(db => db.Number).ToArray();
                while (instanceDbItems.Count < instanceDbs.Length)
                {
                    var index = instanceDbItems.Count;
                    var item = organization.CreateItem(instanceDbFolder);
                    item.SetMetadata(0, $"instance-db:{index}");
                    instanceDbItems.Add(item);
                }
                for (var index = 0; index < instanceDbs.Length; index++)
                {
                    var db = instanceDbs[index];
                    instanceDbItems[index].SetText(0, $"DB{db.Number}  \"{db.Name}\"");
                    instanceDbItems[index].SetMetadata(0, $"instance-db:{db.Number}");
                    instanceDbItems[index].SetTooltipText(0, "FB, IEC timer, or counter instance data block. Edit FB assignments from CALL properties.");
                    instanceDbItems[index].Visible = true;
                }
                for (var index = instanceDbs.Length; index < instanceDbItems.Count; index++)
                    instanceDbItems[index].Visible = false;
            }
            if (siemens && addNewTagTableItem is not null)
            {
                while (tagTableItems.Count < document.TagTables.Count)
                {
                    var item = organization.CreateItem(tagItem);
                    tagTableItems.Add(item);
                }
                for (var index = 0; index < document.TagTables.Count; index++)
                {
                    var tableName = document.TagTables[index];
                    var count = document.Tags.Count(tag => tag.TagTable.Equals(tableName, StringComparison.Ordinal));
                    var constantCount = document.UserConstants.Count(constant => constant.TagTable.Equals(tableName, StringComparison.Ordinal));
                    tagTableItems[index].SetText(0, $"{tableName} [{count} tags · {constantCount} constants]");
                    tagTableItems[index].SetMetadata(0, $"tag-table:{tableName}");
                    tagTableItems[index].SetTooltipText(0, $"{count} PLC tag(s) and {constantCount} user constant(s) in {tableName}.");
                    tagTableItems[index].Visible = true;
                }
                for (var index = document.TagTables.Count; index < tagTableItems.Count; index++)
                    tagTableItems[index].Visible = false;
            }
            if (siemens && addWatchTableItem is not null)
            {
                while (watchTableItems.Count < document.WatchTables.Count)
                    watchTableItems.Add(organization.CreateItem(watchFolder));
                for (var index = 0; index < document.WatchTables.Count; index++)
                {
                    var table = document.WatchTables[index];
                    watchTableItems[index].SetText(0, $"{table.Name} [{table.Variables.Count}]");
                    watchTableItems[index].SetMetadata(0, $"watch-table:{index}");
                    watchTableItems[index].SetTooltipText(0, $"{table.Variables.Count} watched symbol(s). Double-click to open.");
                    watchTableItems[index].Visible = true;
                }
                for (var index = document.WatchTables.Count; index < watchTableItems.Count; index++)
                    watchTableItems[index].Visible = false;
            }
            blockName.Text = document.Blocks.Count > 0
                ? document.Blocks[document.ActiveBlockIndex].Name
                : string.Empty;
            if (document.Blocks.Count > 0)
            {
                var activeBlock = document.Blocks[document.ActiveBlockIndex];
                blockTypeSelector.Select(siemens ? activeBlock.BlockType switch
                {
                    LadderBlockType.OrganizationBlock => 0,
                    LadderBlockType.FunctionBlock => 1,
                    LadderBlockType.Function => 2,
                    LadderBlockType.DataBlock => 3,
                    _ => 2,
                } : 0);
                interfaceSection.Disabled = siemens && activeBlock.BlockType == LadderBlockType.DataBlock;
                interfaceSection.Visible = !siemens || activeBlock.BlockType != LadderBlockType.DataBlock;
                interfaceRetain.Visible = siemens && activeBlock.BlockType == LadderBlockType.DataBlock;
                interfaceComment.Visible = siemens && activeBlock.BlockType == LadderBlockType.DataBlock;
                if (interfaceSection.Disabled)
                    interfaceSection.Select((int)LadderInterfaceSection.Static);
                if (!StringComparer.Ordinal.Equals(interfaceFormOwnerBlockId, activeBlock.Id))
                    ResetInterfaceForm();
                interfaceTable.Clear();
                var interfaceRoot = interfaceTable.CreateItem();
                foreach (var parameter in activeBlock.Interface)
                {
                    var row = interfaceTable.CreateItem(interfaceRoot);
                    row.SetText(0, parameter.Name);
                    row.SetMetadata(0, parameter.Id);
                    row.SetText(1, parameter.Section.ToString().ToUpperInvariant());
                    row.SetText(2, parameter.Type.ToString().ToUpperInvariant());
                    row.SetText(3, FormatTagInitialValue(new PlcVariable(parameter.Name, parameter.Type, PlcVariableRole.Memory, parameter.InitialValue)));
                    row.SetText(4, parameter.Retain ? "YES" : string.Empty);
                    row.SetText(5, parameter.Comment);
                }
                interfaceTable.Visible = siemens || activeBlock.Interface.Count > 0;
                interfaceHeading!.Text = siemens
                    ? activeBlock.BlockType == LadderBlockType.DataBlock
                        ? "GLOBAL DB DECLARATIONS"
                        : $"{activeBlock.BlockType switch { LadderBlockType.OrganizationBlock => "OB", LadderBlockType.FunctionBlock => "FB", LadderBlockType.Function => "FC", _ => "BLOCK" }} INTERFACE"
                    : "ROUTINE PARAMETERS";
                interfaceExecutionNote!.Text = activeBlock.BlockType switch
                {
                    LadderBlockType.DataBlock => "Optimized-access Global DB subset: supported scalar members only.",
                    LadderBlockType.FunctionBlock => "FB Static also supports IEC TIMER/COUNTER; TEMP is invocation-local.",
                    _ => "Supported scalar parameters; typed execution bindings are an offline subset.",
                };
                interfaceExecutionNote.TooltipText = activeBlock.BlockType switch
                {
                    LadderBlockType.DataBlock => "Global DB declarations use static storage. This offline subset does not model standard-access offsets, nested structures, or arrays.",
                    LadderBlockType.FunctionBlock => "FB Static storage belongs to the chosen instance. TEMP locals are invocation-local and are not stored in an instance DB.",
                    _ => "Legacy mode keeps descriptive declarations. Explicit parameterized mode executes typed Input/Output/InOut bindings in the offline simulator.",
                };
                var interfaceText = activeBlock.Interface.Count == 0
                    ? (activeBlock.BlockType == LadderBlockType.DataBlock ? "[b]DB DATA[/b]  ·  no members declared" : "[b]BLOCK INTERFACE[/b]  ·  no parameters declared")
                    : string.Join("    ", activeBlock.Interface.Select(parameter =>
                        $"[b]{parameter.Section.ToString().ToUpperInvariant()}[/b] {Escape(parameter.Name)} : {parameter.Type.ToString().ToUpperInvariant()}"));
                blockInterfaceSummary!.Text = $"[b]{activeBlock.BlockType switch { LadderBlockType.OrganizationBlock => "OB", LadderBlockType.FunctionBlock => "FB", LadderBlockType.Function => "FC", LadderBlockType.DataBlock => "DB", _ => "BLOCK" }} DECLARATIONS[/b]  ·  shared project tags\n{interfaceText}";
                blockInterfaceSummary.Visible = siemens && (activeBlock.Interface.Count > 0 || activeBlock.BlockType == LadderBlockType.DataBlock);
                if (globalDbHeading is not null && globalDbDeclarations is not null
                    && activeBlock.BlockType == LadderBlockType.DataBlock)
                {
                    globalDbHeading.Text = $"[font_size=20][b]{Escape(activeBlock.Name)}[/b][/font_size]\n"
                        + "[color=#8fb0bd]Global data block · optimized-access offline declaration subset[/color]";
                    globalDbDeclarations.Clear();
                    var declarationRoot = globalDbDeclarations.CreateItem();
                    foreach (var declaration in activeBlock.Interface)
                    {
                        var row = globalDbDeclarations.CreateItem(declarationRoot);
                        row.SetText(0, declaration.Name);
                        row.SetMetadata(0, declaration.Id);
                        row.SetMetadata(1, declaration.Name);
                        row.SetMetadata(2, activeBlock.Id);
                        row.SetText(1, declaration.Type.ToString().ToUpperInvariant());
                        row.SetText(2, FormatTagInitialValue(new PlcVariable(
                            declaration.Name, declaration.Type, PlcVariableRole.Memory, declaration.InitialValue)));
                        row.SetCellMode(3, TreeItem.TreeCellMode.Check);
                        row.SetChecked(3, declaration.Retain);
                        row.SetText(4, declaration.Comment);
                        for (var column = 0; column < globalDbDeclarations.Columns; column++)
                            row.SetEditable(column, column != 2
                                || declaration.Type is not (PlcVariableType.Timer or PlcVariableType.Counter));
                    }
                    UpdateGlobalDbDeclarationGridEditability();
                }
            }
            while (taskItems.Count < document.Tasks.Count)
            {
                var index = taskItems.Count;
                var item = organization.CreateItem(taskFolder);
                item.SetMetadata(0, $"task:{index}");
                taskItems.Add(item);
            }
            for (var index = 0; index < document.Tasks.Count; index++)
            {
                var task = document.Tasks[index];
                var schedule = task.Kind == LadderTaskKind.Continuous
                    ? "Continuous"
                    : $"{task.Period.TotalMilliseconds:0} ms";
                var prefix = siemens ? (task.Kind == LadderTaskKind.Continuous ? "OB1" : $"OB{30 + index}") : task.Name;
                taskItems[index].SetText(0, $"{prefix} · {schedule} · P{task.Priority}");
                taskItems[index].Visible = !siemens;
            }
            for (var index = document.Tasks.Count; index < taskItems.Count; index++)
                taskItems[index].Visible = false;
            selectedTaskIndex = Math.Clamp(selectedTaskIndex, 0, Math.Max(0, document.Tasks.Count - 1));
            foreach (var ladderCanvas in _ladderCanvases) ladderCanvas.RefreshDocument();
            tagSelector.Clear();
            coilSelector.Clear();
            timerSelector.Clear();
            counterSelector.Clear();
            compareLeft.Clear();
            numericDestination.Clear();
            edgeMemorySelector.Clear();
            edgeMemorySelector.AddItem("Select edge memory…");
            var parameterizedBlock = document.Blocks[document.ActiveBlockIndex].SemanticMode == LadderSemanticMode.Parameterized;
            if (document.Blocks[document.ActiveBlockIndex].SemanticMode == LadderSemanticMode.Parameterized)
            {
                foreach (var selector in new[] { tagSelector, coilSelector, timerSelector, counterSelector, compareLeft, numericDestination }) selector.Clear();
                var activeRung = document.Rungs.Count > 0 ? document.Rungs[Math.Clamp(selectedRung, 0, document.Rungs.Count - 1)] : null;
                foreach (var symbol in LadderOperandSymbols.ForDocument(document))
                {
                    void Add(OptionButton selector) { selector.AddItem(symbol.Name); selector.SetItemTooltip(selector.ItemCount - 1, symbol.DisplayName + " · " + symbol.Type); }
                    if (symbol.Type == PlcVariableType.Bool) { Add(tagSelector); if (symbol.Writable) Add(coilSelector); }
                    else if (symbol.Type == PlcVariableType.Timer && symbol.Writable) Add(timerSelector);
                    else if (symbol.Type == PlcVariableType.Counter && symbol.Writable) Add(counterSelector);
                    else if (symbol.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.String or PlcVariableType.Enum)
                    {
                        Add(compareLeft);
                        if (symbol.Writable && (symbol.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real
                            || activeRung?.NumericOperationKind == LadderNumericOperationKind.Move
                            || activeRung?.NumericOperationKind == LadderNumericOperationKind.FormatText && symbol.Type == PlcVariableType.String)) Add(numericDestination);
                    }
                }
            }
            callTarget.Clear();
            taskTarget.Clear();
            tagList.Clear();
            var tagRoot = tagList.CreateItem();
            if (tagOperandDetailView is not null)
            {
                tagOperandDetailView.Clear();
                var detailRoot = tagOperandDetailView.CreateItem();
                for (var index = 0; index < document.Tags.Count; index++)
                {
                    var tag = document.Tags[index];
                    var row = tagOperandDetailView.CreateItem(detailRoot);
                    row.SetMetadata(0, $"tag:{index}");
                    row.SetMetadata(1, "tag-id:" + LadderCallBindings.GlobalId(tag));
                    row.SetText(0, tag.Name);
                    row.SetText(1, tag.Type.ToString().ToUpperInvariant());
                    row.SetTooltipText(0, string.IsNullOrWhiteSpace(tag.Comment)
                        ? $"{tag.Name} · {tag.Type} · drag onto a compatible LAD operand"
                        : $"{tag.Name} · {tag.Type} · {tag.Comment} · drag onto a compatible LAD operand");
                    row.SetTooltipText(1, PlcAggregates.IsAggregate(tag.Type)
                        ? "Aggregate operands are not supported by the offline tag-drag editor."
                        : tag.Type.ToString().ToUpperInvariant());
                }
            }
            foreach (var (tag, index) in document.Tags.Select((tag, index) => (tag, index)))
            {
                if (tag.Type == PlcVariableType.Bool && tag.Role == PlcVariableRole.Memory)
                    edgeMemorySelector.AddItem(parameterizedBlock
                        ? "global:" + LadderCallBindings.GlobalId(tag) : tag.Name);
                if (PlcAggregates.IsAggregate(tag.Type))
                {
                    foreach (var leaf in AggregateEditorLeaves(tag))
                    {
                        var context = $"{tag.Name} · {tag.Type} member · {leaf.Type} · {leaf.Role}";
                        if (leaf.Type == PlcVariableType.Bool)
                        {
                            tagSelector.AddItem(leaf.Name); tagSelector.SetItemTooltip(tagSelector.ItemCount - 1, context);
                            if (leaf.Role != PlcVariableRole.Input) { coilSelector.AddItem(leaf.Name); coilSelector.SetItemTooltip(coilSelector.ItemCount - 1, context); }
                        }
                        else
                        {
                            compareLeft.AddItem(leaf.Name); compareLeft.SetItemTooltip(compareLeft.ItemCount - 1, context);
                            if (leaf.Role != PlcVariableRole.Input) { numericDestination.AddItem(leaf.Name); numericDestination.SetItemTooltip(numericDestination.ItemCount - 1, context); }
                        }
                    }
                }
                if (tag.Type == PlcVariableType.Bool)
                {
                    tagSelector.AddItem(tag.Name);
                    if (tag.Role != PlcVariableRole.Input) coilSelector.AddItem(tag.Name);
                }
                else if (tag.Type == PlcVariableType.Timer)
                {
                    timerSelector.AddItem(tag.Name);
                    tagSelector.AddItem($"{tag.Name}.{(siemens ? "Q" : "DN")}");
                    tagSelector.AddItem($"{tag.Name}.TT");
                }
                else if (tag.Type == PlcVariableType.Counter)
                {
                    counterSelector.AddItem(tag.Name);
                    tagSelector.AddItem($"{tag.Name}.{(siemens ? "Q" : "DN")}");
                    compareLeft.AddItem($"{tag.Name}.{(siemens ? "CV" : "ACC")}");
                    compareLeft.AddItem($"{tag.Name}.{(siemens ? "PV" : "PRE")}");
                }
                else if (tag.Type is PlcVariableType.String or PlcVariableType.Enum)
                {
                    compareLeft.AddItem(tag.Name);
                    compareLeft.SetItemTooltip(compareLeft.ItemCount - 1, $"{tag.Type} · {tag.Role}; text equality or CODE MATCH as validated");
                }
                else if (tag.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
                {
                    compareLeft.AddItem(tag.Name);
                    if (tag.Role != PlcVariableRole.Input) numericDestination.AddItem(tag.Name);
                }
                if (tag.Type == PlcVariableType.Timer)
                {
                    compareLeft.AddItem($"{tag.Name}.ET");
                    compareLeft.AddItem($"{tag.Name}.PT");
                }
                if (activeTagTableName is not null
                    && !tag.TagTable.Equals(activeTagTableName, StringComparison.Ordinal)) continue;
                var tagRow = tagList.CreateItem(tagRoot);
                tagRow.SetMetadata(0, $"tag:{index}");
                tagRow.SetMetadata(1, "tag-id:" + LadderCallBindings.GlobalId(tag));
                tagRow.SetText(0, tag.Name);
                var addressColumn = siemens ? 1 : -1;
                var commentColumn = siemens ? 2 : -1;
                var typeColumn = siemens ? 3 : 1;
                var roleColumn = siemens ? 4 : 2;
                var initialColumn = siemens ? 5 : 3;
                var bindingColumn = siemens ? 6 : 4;
                if (addressColumn >= 0) tagRow.SetText(addressColumn, string.IsNullOrWhiteSpace(tag.Address) ? "—" : tag.Address);
                if (commentColumn >= 0) tagRow.SetText(commentColumn, string.IsNullOrWhiteSpace(tag.Comment) ? "—" : tag.Comment);
                tagRow.SetText(typeColumn, tag.Type.ToString().ToUpperInvariant());
                tagRow.SetText(roleColumn, tag.Role.ToString().ToUpperInvariant());
                tagRow.SetText(initialColumn, FormatTagInitialValue(tag));
                tagRow.SetText(bindingColumn, string.IsNullOrWhiteSpace(tag.Binding) ? "—" : tag.Binding);
                tagRow.SetTooltipText(0, tag.Name);
                if (addressColumn >= 0) tagRow.SetTooltipText(addressColumn, string.IsNullOrWhiteSpace(tag.Address) ? "—" : tag.Address);
                if (commentColumn >= 0) tagRow.SetTooltipText(commentColumn, string.IsNullOrWhiteSpace(tag.Comment) ? "—" : tag.Comment);
                tagRow.SetTooltipText(typeColumn, PlcAggregates.IsAggregate(tag.Type) ? string.Join("\n", AggregateEditorLeaves(tag).Select(leaf => $"{leaf.Name}: {leaf.Type}")) : tag.Type.ToString().ToUpperInvariant());
                tagRow.SetTooltipText(roleColumn, tag.Role.ToString().ToUpperInvariant());
                tagRow.SetTooltipText(initialColumn, FormatTagInitialValue(tag));
                tagRow.SetTooltipText(bindingColumn, string.IsNullOrWhiteSpace(tag.Binding) ? "—" : tag.Binding);
            }
            if (parameterizedBlock)
                foreach (var formal in document.Blocks[document.ActiveBlockIndex].Interface
                             .Where(item => item.Section == LadderInterfaceSection.Static && item.Type == PlcVariableType.Bool))
                    edgeMemorySelector.AddItem("local:" + formal.Id);
            if (selectedTagIndex >= document.Tags.Count
                || selectedTagIndex >= 0 && activeTagTableName is not null
                    && !document.Tags[selectedTagIndex].TagTable.Equals(activeTagTableName, StringComparison.Ordinal))
                selectedTagIndex = -1;
            applyTagEdit!.Disabled = selectedTagIndex < 0;
            deleteTag!.Disabled = selectedTagIndex < 0;
            if (selectedTagIndex >= 0) SelectIndexedTreeRow(tagList, selectedTagIndex);
            RefreshTagTableEditorControls();
            for (var index = 0; index < document.Blocks.Count; index++)
            {
                if (document.Blocks[index].BlockType != LadderBlockType.DataBlock)
                {
                    taskTarget.AddItem(document.Blocks[index].Name);
                    taskTarget.SetItemMetadata(taskTarget.ItemCount - 1, document.Blocks[index].Id);
                }
                if (index == document.ActiveBlockIndex) continue;
                if (document.Blocks[index].BlockType is LadderBlockType.Function or LadderBlockType.FunctionBlock)
                {
                    callTarget.AddItem(document.Blocks[index].Name);
                    callTarget.SetItemMetadata(callTarget.ItemCount - 1, document.Blocks[index].Id);
                }
            }
            selectedRung = Math.Clamp(selectedRung, 0, Math.Max(0, document.Rungs.Count - 1));
            branchSelector.Clear();
            if (document.Rungs.Count > 0)
            {
                var rung = document.Rungs[selectedRung];
                for (var i = 0; i < rung.Branches.Count; i++) branchSelector.AddItem($"Branch {i + 1}");
                selectedBranch = ChallengePresenterModel.ClampSelection(rung.Branches.Count, selectedBranch);
                branchSelector.Select(selectedBranch);
                IReadOnlyList<EditableContact> branchContacts = selectedBranch >= 0
                    ? rung.Branches[selectedBranch].Contacts : Array.Empty<EditableContact>();
                if (selectedBranch < 0) selectedInsertionIndex = -1;
                if (selectedContact >= branchContacts.Count) selectedContact = -1;
                if (selectedInsertionIndex > branchContacts.Count)
                    selectedInsertionIndex = branchContacts.Count;
                if (selectedContact >= 0 || selectedOutput) selectedInsertionIndex = -1;
                moveContactLeft!.Disabled = selectedContact <= 0;
                moveContactRight!.Disabled = selectedContact < 0 || selectedContact >= branchContacts.Count - 1;
                if (selectedContact >= 0)
                {
                    var contact = branchContacts[selectedContact];
                    if (contact.IsComparison)
                    {
                        var leftIndex = Enumerable.Range(0, compareLeft.ItemCount)
                            .FirstOrDefault(index => compareLeft.GetItemText(index) == contact.Variable, -1);
                        if (leftIndex >= 0) compareLeft.Select(leftIndex);
                        compareOperator.Select((int)contact.CompareOperator);
                        compareDataType.Select(Math.Max(0, LadderComparisonValueTypePresentation.TiaSelectorIndex(contact.ComparisonValueType)));
                        compareRight.Text = contact.RightOperand;
                    }
                    else
                    {
                        contactTypeSelector.Select(contact.EdgeMode switch
                        {
                            LadderEdgeMode.Rising => 2,
                            LadderEdgeMode.Falling => 3,
                            _ => contact.NormallyClosed ? 1 : 0,
                        });
                        var tagIndex = Enumerable.Range(0, tagSelector.ItemCount)
                            .FirstOrDefault(index => tagSelector.GetItemText(index) == contact.Variable, -1);
                        if (tagIndex >= 0) tagSelector.Select(tagIndex);
                        var edgeMemoryIndex = Enumerable.Range(0, edgeMemorySelector.ItemCount)
                            .FirstOrDefault(index => edgeMemorySelector.GetItemText(index) == contact.EdgeMemoryOperand, -1);
                        edgeMemorySelector.Select(edgeMemoryIndex >= 0 ? edgeMemoryIndex : 0);
                    }
                }
                rungLabel.Text = rung.Label;
                var coilIndex = Enumerable.Range(0, coilSelector.ItemCount).FirstOrDefault(index => coilSelector.GetItemText(index) == rung.CoilVariable, -1);
                if (coilIndex >= 0) coilSelector.Select(coilIndex);
                else
                {
                    // Keep an unassigned/unknown model operand visibly unassigned.
                    // Append the placeholder so real operand indices stay unchanged;
                    // selecting even the sole real operand now emits ItemSelected.
                    coilSelector.AddItem("Select output…", int.MaxValue);
                    coilSelector.Select(coilSelector.ItemCount - 1);
                }
                coilModeSelector.Select((int)rung.CoilMode);
                var timerIndex = document.Tags.Where(tag => tag.Type == PlcVariableType.Timer).ToList()
                    .FindIndex(tag => tag.Name == rung.TimerVariable);
                if (timerIndex >= 0) timerSelector.Select(timerIndex);
                timerPreset.Value = Math.Max(1, rung.TimerPreset.TotalMilliseconds);
                timerPresetOperand.Text = rung.TimerPresetOperand;
                timerPreset.Editable = true;
                timerPresetOperand.Editable = true;
                timerResetOperand.Text = rung.TimerResetOperand;
                timerResetOperand.Editable = true;
                timerDbNumber.Value = rung.TimerInstanceDataBlock?.Number ?? 1;
                timerDbName.Text = rung.TimerInstanceDataBlock?.Name ?? string.Empty;
                var counterIndex = document.Tags.Where(tag => tag.Type == PlcVariableType.Counter).ToList()
                    .FindIndex(tag => tag.Name == rung.CounterVariable);
                if (counterIndex >= 0) counterSelector.Select(counterIndex);
                counterPreset.Value = Math.Max(1, rung.CounterPreset);
                counterDataTypeSelector.Select((int)rung.CounterValueType);
                counterLoadInput.Text = rung.CounterLoadInput;
                counterDbNumber.Value = rung.CounterInstanceDataBlock?.Number ?? 1;
                counterDbName.Text = rung.CounterInstanceDataBlock?.Name ?? string.Empty;
                counterResetInput.Text = rung.CounterResetInput;
                numericSourceA.Text = rung.NumericSourceA;
                if (document.Blocks[document.ActiveBlockIndex].SemanticMode == LadderSemanticMode.Legacy && rung.IsNumericOperation && rung.NumericOperationKind is LadderNumericOperationKind.Move or LadderNumericOperationKind.FormatText)
                    foreach (var textTag in document.Tags.Where(tag => tag.Role != PlcVariableRole.Input
                        && (rung.NumericOperationKind == LadderNumericOperationKind.FormatText
                            ? tag.Type == PlcVariableType.String : tag.Type is PlcVariableType.String or PlcVariableType.Enum)))
                        numericDestination.AddItem(textTag.Name);
                if (document.Blocks[document.ActiveBlockIndex].SemanticMode == LadderSemanticMode.Legacy && rung.IsNumericOperation && rung.NumericOperationKind == LadderNumericOperationKind.FormatText)
                    for (var item = numericDestination.ItemCount - 1; item >= 0; item--)
                        if (!document.Tags.Any(tag => tag.Name == numericDestination.GetItemText(item) && tag.Type == PlcVariableType.String))
                            numericDestination.RemoveItem(item);
                var numericDestinationIndex = Enumerable.Range(0, numericDestination.ItemCount)
                    .FirstOrDefault(index => numericDestination.GetItemText(index) == rung.NumericDestination, -1);
                if (numericDestinationIndex >= 0) numericDestination.Select(numericDestinationIndex);
                if (rung.IsCall)
                {
                    var callTargetIndex = Enumerable.Range(0, callTarget.ItemCount)
                        .FirstOrDefault(index => callTarget.GetItemMetadata(index).AsString() == rung.CallTarget, -1);
                    if (callTargetIndex >= 0) callTarget.Select(callTargetIndex);
                }
                returnValueSelector.Select((int)rung.ReturnValueKind);
                returnValueOperand.Text = rung.ReturnValueOperand;
                returnOperandLabel.Visible = returnValueOperand.Visible = siemens
                    && rung.IsReturn && rung.ReturnValueKind == LadderReturnValueKind.Operand;
                void PreserveOperand(OptionButton selector, string operand)
                {
                    var index = Enumerable.Range(0, selector.ItemCount).FirstOrDefault(i => selector.GetItemText(i) == operand, -1);
                    if (index < 0) { selector.AddItem(operand); index = selector.ItemCount - 1; selector.SetItemTooltip(index, "Configured operand; verify its declaration/type before applying."); }
                    selector.Select(index);
                }
                if (selectedContact >= 0)
                {
                    var contact = branchContacts[selectedContact];
                    PreserveOperand(contact.IsComparison ? compareLeft : tagSelector, contact.Variable);
                }
                PreserveOperand(timerSelector, rung.TimerVariable); PreserveOperand(counterSelector, rung.CounterVariable);
                PreserveOperand(numericDestination, rung.NumericDestination);
                programControlLabel.Text = propertyNetworkLabel && rung.IsLabel
                    ? (string.IsNullOrWhiteSpace(rung.NetworkLabelName) ? rung.ProgramControlLabel : rung.NetworkLabelName)
                    : rung.ProgramControlLabel;
                if (!string.IsNullOrWhiteSpace(rung.NumericSourceB)) numericSourceB.Text = rung.NumericSourceB;
                if (!string.IsNullOrWhiteSpace(rung.NumericSourceC)) numericSourceC.Text = rung.NumericSourceC;
                numericSourceB.Editable = rung.IsNumericOperation
                    && LadderNumericOperationRules.RequiresSourceB(rung.NumericOperationKind);
                numericSourceB.TooltipText = rung.NumericOperationKind == LadderNumericOperationKind.FormatText
                    ? "STRING tag or quoted template, exactly one {0:F0} through {0:F6}; invariant culture. Example: \"CHICKEN {0:F3} kg\"."
                    : "Source B tag or literal.";
                numericSourceB.PlaceholderText = numericSourceB.Editable
                    ? rung.NumericOperationKind == LadderNumericOperationKind.FormatText ? "quoted template or STRING tag" : "source B tag or literal"
                    : "not used by unary instruction";
                numericSourceC.Editable = rung.IsNumericOperation
                    && LadderNumericOperationRules.RequiresSourceC(rung.NumericOperationKind);
                numericSourceC.PlaceholderText = numericSourceC.Editable
                    ? "MAX tag or literal"
                    : "not used by this instruction";
            }
            else
            {
                selectedContact = -1;
                selectedInsertionIndex = -1;
                selectedOutput = false;
                moveContactLeft!.Disabled = true;
                moveContactRight!.Disabled = true;
                numericSourceB.Editable = false;
                numericSourceC.Editable = false;
            }
            if (document.Tasks.Count > 0) LoadTaskControls(selectedTaskIndex);
            undoEdit.Disabled = !_ladderHistory.CanUndo;
            redoEdit.Disabled = !_ladderHistory.CanRedo;
            undoEdit.TooltipText = _ladderHistory.CanUndo ? $"Undo {_ladderHistory.UndoDescription}" : "Nothing to undo";
            redoEdit.TooltipText = _ladderHistory.CanRedo ? $"Redo {_ladderHistory.RedoDescription}" : "Nothing to redo";
            refreshingEditor = false;
            RestoreInterfaceSelection();
            if (instructionPropertiesPopup.Visible) UpdateInstructionPropertyVisibility();
            RefreshWatchTable(_virtualSnapshot);
            refreshTreeActions?.Invoke();
            restoreActiveEditorDocumentBody?.Invoke();
        }

        var instructionChrome = new ScrollContainer
        {
            Name = "InstructionToolbarChrome",
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 140),
        };
        instructionChrome.AddThemeStyleboxOverride("panel", BoxStyle(new Color("eef1f3"), new Color("aeb8bd")));
        var instructions = new VBoxContainer
        {
            Name = "InstructionToolbar",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        instructions.AddThemeConstantOverride("separation", 4);
        instructionChrome.AddChild(instructions);
        var selectionRow = new HFlowContainer { Name = "Selection" };
        selectionRow.AddThemeConstantOverride("separation", 4);
        selectionRow.AddChild(Heading(siemens ? "Instruction palette" : "Language elements", 11, new Color("344851")));
        selectionRow.AddChild(branchSelector);
        moveContactLeft = ToolbarButton("MoveSelectedLeft", "◀ MOVE", new Color("637985"), 78);
        moveContactLeft.TooltipText = "Move the selected contact one position left in its branch";
        moveContactRight = ToolbarButton("MoveSelectedRight", "MOVE ▶", new Color("637985"), 78);
        moveContactRight.TooltipText = "Move the selected contact one position right in its branch";
        moveContactLeft.Disabled = true;
        moveContactRight.Disabled = true;
        selectionRow.AddChild(moveContactLeft);
        selectionRow.AddChild(moveContactRight);
        var openInstructionHelp = ToolbarButton("OpenInstructionHelp", "HELP (F1)", new Color("4d6674"), 92);
        selectionRow.AddChild(openInstructionHelp);
        instructions.AddChild(selectionRow);
        // Category tabs and fixed-width command rows forced the split editor
        // beyond its aperture. Keep the existing command paths, but select
        // categories through a compact control and wrap their buttons.
        var instructionCategory = new OptionButton
        {
            Name = "InstructionCategory",
            CustomMinimumSize = new Vector2(160, 28),
            TooltipText = "Choose an instruction category; buttons wrap to the available editor width",
        };
        instructions.AddChild(instructionCategory);
        var instructionPalette = new TabContainer
        {
            Name = "InstructionPalette",
            TabsVisible = false,
            CustomMinimumSize = new Vector2(0, 72),
        };
        instructionPalette.AddThemeFontSizeOverride("font_size", 11);
        instructionPalette.AddThemeColorOverride("font_selected_color", new Color("263943"));
        instructionPalette.AddThemeColorOverride("font_unselected_color", new Color("4f6874"));
        instructionPalette.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        instructionPalette.AddThemeStyleboxOverride("tab_selected", BoxStyle(new Color("ffffff"), accent));
        instructionPalette.AddThemeStyleboxOverride("tab_unselected", BoxStyle(new Color("d9dfe2"), new Color("aeb8bd")));
        instructions.AddChild(instructionPalette);
        Button Command(string name, string text)
        {
            var button = ToolbarButton(name, text, new Color("637985"), 76);
            button.CustomMinimumSize = new Vector2(76, 32);
            return button;
        }
        var addNo = Command("AddNormallyOpen", siemens ? "—| |—  NO" : "—] [—  XIC");
        var addEmptyInstruction = Command("AddEmptyInstructionBox", "???");
        addEmptyInstruction.Visible = siemens;
        addEmptyInstruction.TooltipText = "Insert an empty instruction box in the selected network, as in TIA Portal. Choose a supported instruction afterward to replace it.";
        var addNc = Command("AddNormallyClosed", siemens ? "—|/|—  NC" : "—]/[—  XIO");
        var addRisingEdge = Command("AddRisingEdge", siemens ? "—|P|— EDGE" : "XIC+ONS");
        var addFallingEdge = Command("AddFallingEdge", siemens ? "—|N|— EDGE" : "XIO+ONS");
        var addTon = Command("AddTimerOnDelay", "TON");
        var addTof = Command("AddTimerOffDelay", "TOF");
        var addTp = Command("AddPulseTimer", "TP");
        var addRto = Command("AddRetentiveTimer", siemens ? "TONR" : "RTO");
        var addTimerReset = Command("AddTimerReset", siemens ? "RT" : "RES TIMER");
        var addAssign = Command("AddAssignCoil", "ASSIGN");
        addAssign.Visible = false;
        var addSet = Command("AddSetLatch", siemens ? "SET" : "OTL");
        var addReset = Command("AddResetUnlatch", siemens ? "RESET" : "OTU");
        var addCounter = Command("AddCountUp", "CTU");
        var addCounterDown = Command("AddCountDown", "CTD");
        var addCounterLoad = Command("AddCounterLoad", siemens ? "LOAD COUNTER" : "LOAD");
        addCounterLoad.TooltipText = "RungProof-local preset load operation. Siemens CTD uses its built-in LD input pin instead.";
        var addCounterReset = Command("AddCounterReset", siemens ? "RESET COUNTER" : "RES");
        addCounterReset.TooltipText = "RungProof-local counter reset operation. Siemens CTU uses its built-in R input pin instead.";
        var addCompare = Command("AddComparison", "CMP");
        var addBranch = Command("AddBranch", "BRANCH");
        var removeBranch = Command("RemoveBranch", "− BRANCH");
        removeBranch.Visible = !siemens;
        var addRung = Command("AddRung", siemens ? "+ NETWORK" : "+ RUNG");
        var remove = Command("RemoveInstruction", "DELETE");
        addNo.TooltipText = siemens
            ? "Insert a normally-open contact at the selected wire position, or change the selected contact to NO"
            : "Insert an Examine If Closed contact at the selected wire position, or change the selected contact to XIC";
        addNc.TooltipText = siemens
            ? "Insert a normally-closed contact at the selected wire position, or change the selected contact to NC"
            : "Insert an Examine If Open contact at the selected wire position, or change the selected contact to XIO";
        addRisingEdge.TooltipText = "Insert a one-scan FALSE-to-TRUE edge contact with stable per-instruction simulator memory";
        addFallingEdge.TooltipText = "Insert a one-scan TRUE-to-FALSE edge contact with stable per-instruction simulator memory";
        addCompare.TooltipText = "Insert a comparison at the selected wire position";

        var bitCommands = new HFlowContainer { Name = "Bit Logic" };
        bitCommands.AddThemeConstantOverride("separation", 4);
        bitCommands.AddChild(addNo);
        bitCommands.AddChild(addEmptyInstruction);
        bitCommands.AddChild(addNc);
        bitCommands.AddChild(addAssign);
        bitCommands.AddChild(addRisingEdge);
        bitCommands.AddChild(addFallingEdge);
        bitCommands.AddChild(addSet);
        bitCommands.AddChild(addReset);
        bitCommands.AddChild(addBranch);
        bitCommands.AddChild(removeBranch);
        bitCommands.AddChild(addRung);
        bitCommands.AddChild(remove);
        instructionPalette.AddChild(bitCommands);

        var timerCommands = new HFlowContainer { Name = "Timers" };
        timerCommands.AddThemeConstantOverride("separation", 4);
        timerCommands.AddChild(addTon);
        timerCommands.AddChild(addTof);
        timerCommands.AddChild(addTp);
        timerCommands.AddChild(addRto);
        timerCommands.AddChild(addTimerReset);
        instructionPalette.AddChild(timerCommands);

        var counterCommands = new HFlowContainer { Name = "Counters" };
        counterCommands.AddThemeConstantOverride("separation", 4);
        counterCommands.AddChild(addCounter);
        counterCommands.AddChild(addCounterDown);
        if (!siemens)
        {
            counterCommands.AddChild(addCounterLoad);
            counterCommands.AddChild(addCounterReset);
        }
        instructionPalette.AddChild(counterCommands);

        var compareCommands = new HFlowContainer { Name = "Compare" };
        compareCommands.AddThemeConstantOverride("separation", 4);
        compareCommands.AddChild(addCompare);
        instructionPalette.AddChild(compareCommands);

        var numericCommands = new HFlowContainer { Name = "Math and Move" };
        numericCommands.AddThemeConstantOverride("separation", 4);
        var addMove = Command("AddMove", "MOV");
        var addFormatText = Command("AddFormatText", "FORMAT_TEXT");
        addFormatText.TooltipText = "Local simulator instruction: numeric source A + quoted template B to STRING; no vendor parity claim.";
        var addAdd = Command("AddAdd", "ADD");
        var addSubtract = Command("AddSubtract", "SUB");
        var addMultiply = Command("AddMultiply", "MUL");
        var addDivide = Command("AddDivide", "DIV");
        var addModulo = Command("AddModulo", "MOD");
        var addAbsolute = Command("AddAbsolute", "ABS");
        var addNegate = Command("AddNegate", "NEG");
        var addSquareRoot = Command("AddSquareRoot", "SQRT");
        numericCommands.AddChild(addMove);
        if (!siemens) numericCommands.AddChild(addFormatText);
        numericCommands.AddChild(addAdd);
        numericCommands.AddChild(addSubtract);
        numericCommands.AddChild(addMultiply);
        numericCommands.AddChild(addDivide);
        numericCommands.AddChild(addModulo);
        numericCommands.AddChild(addAbsolute);
        numericCommands.AddChild(addNegate);
        numericCommands.AddChild(addSquareRoot);
        instructionPalette.AddChild(numericCommands);

        if (siemens)
        {
            // Preserve portable simulator operations without presenting them
            // as native STEP 7 instructions. Siemens CTU/CTD expose R/LD as
            // box inputs, and FORMAT_TEXT is a RungProof-only teaching helper.
            var localCommands = new HFlowContainer { Name = "RungProof local (not Siemens)" };
            localCommands.AddThemeConstantOverride("separation", 4);
            localCommands.AddChild(addCounterLoad);
            localCommands.AddChild(addCounterReset);
            localCommands.AddChild(addFormatText);
            instructionPalette.AddChild(localCommands);
        }

        var scientificCommands = new HFlowContainer { Name = "Scientific Math" };
        scientificCommands.AddThemeConstantOverride("separation", 4);
        var addExponentiate = Command("AddExponentiate", "EXPT");
        var addNaturalLog = Command("AddNaturalLog", "LN");
        var addSine = Command("AddSine", "SIN");
        var addCosine = Command("AddCosine", "COS");
        var addTangent = Command("AddTangent", "TAN");
        var addArcSine = Command("AddArcSine", "ASIN");
        var addArcCosine = Command("AddArcCosine", "ACOS");
        var addArcTangent = Command("AddArcTangent", "ATAN");
        scientificCommands.AddChild(addExponentiate);
        scientificCommands.AddChild(addNaturalLog);
        scientificCommands.AddChild(addSine);
        scientificCommands.AddChild(addCosine);
        scientificCommands.AddChild(addTangent);
        scientificCommands.AddChild(addArcSine);
        scientificCommands.AddChild(addArcCosine);
        scientificCommands.AddChild(addArcTangent);
        instructionPalette.AddChild(scientificCommands);

        var conversionCommands = new HFlowContainer { Name = "Conversion" };
        conversionCommands.AddThemeConstantOverride("separation", 4);
        var addTruncate = Command("AddTruncate", "TRUNC");
        var addNormalize = Command("AddNormalize", siemens ? "NORM_X" : "CPT NORM");
        var addScale = Command("AddScale", siemens ? "SCALE_X" : "CPT SCALE");
        var addConvert = Command("AddConvert", siemens ? "CONVERT" : "MOV CONV");
        var addRound = Command("AddRound", siemens ? "ROUND" : "CPT ROUND");
        var addCeiling = Command("AddCeiling", siemens ? "CEIL" : "CPT CEIL");
        var addFloor = Command("AddFloor", siemens ? "FLOOR" : "CPT FLOOR");
        conversionCommands.AddChild(addTruncate);
        conversionCommands.AddChild(addConvert);
        conversionCommands.AddChild(addRound);
        conversionCommands.AddChild(addCeiling);
        conversionCommands.AddChild(addFloor);
        conversionCommands.AddChild(addNormalize);
        conversionCommands.AddChild(addScale);
        instructionPalette.AddChild(conversionCommands);

        var programCommands = new HFlowContainer { Name = "Program Control" };
        programCommands.AddThemeConstantOverride("separation", 4);
        var addCall = Command("AddCall", siemens ? "FC / FB CALL" : "JSR");
        addCall.TooltipText = siemens
            ? "TIA LAD inserts a named FC/FB from Program blocks into a network; it does not use a generic CALL palette instruction."
            : "Insert a subroutine call.";
        // TIA inserts a specific named FC/FB by dragging it from Program
        // blocks. Keep the internal command for the shared model/verifier, but
        // do not present a generic CALL button in the Siemens-facing palette.
        addCall.Visible = !siemens;
        var addReturn = Command("AddReturn", siemens ? "RETURN" : "RET");
        var addJump = Command("AddJump", "JMP");
        var addLabel = Command("AddLabel", siemens ? "LABEL" : "LBL");
        var addBlock = Command("AddBlock", siemens ? "+ FC" : "+ ROUTINE");
        programCommands.AddChild(addCall);
        programCommands.AddChild(addReturn);
        programCommands.AddChild(addJump);
        programCommands.AddChild(addLabel);
        instructionPalette.AddChild(programCommands);

        for (var category = 0; category < instructionPalette.GetTabCount(); category++)
            instructionCategory.AddItem(instructionPalette.GetTabTitle(category));
        instructionCategory.ItemSelected += category => instructionPalette.CurrentTab = (int)category;
        instructionPalette.TabChanged += category => instructionCategory.Select((int)category);

        var addTask = Command("AddTask", siemens ? "+ OB" : "+ TASK");
        var applyBlock = Command("ApplyBlock", siemens ? "RENAME BLOCK" : "RENAME ROUTINE");
        var cancelBlock = Command("CancelBlockEdit", "CANCEL");
        var removeBlock = Command("RemoveBlock", siemens ? "DELETE BLOCK" : "DELETE ROUTINE");
        var applyTask = Command("ApplyTask", "APPLY");
        var cancelTask = Command("CancelTaskEdit", "CANCEL");
        var removeTask = Command("RemoveTask", siemens ? "DELETE OB" : "DELETE TASK");
        center.AddChild(instructionChrome);

        blockInterfaceSummary = new RichTextLabel
        {
            Name = "BlockInterfaceSummary",
            BbcodeEnabled = true,
            // Bound the summary before its first layout. FitContent measures
            // wrapped text at the initial narrow width and can force the
            // entire workbench beyond the viewport and the points dock.
            FitContent = false,
            CustomMinimumSize = new Vector2(0, 44),
            ScrollActive = true,
            TooltipText = "Inspect interface declarations. Legacy calls use shared project tags; Parameterized FC/FB calls use explicit bindings and FB instance storage.",
        };
        blockInterfaceSummary.AddThemeColorOverride("default_color", new Color("263943"));
        blockInterfaceSummary.AddThemeStyleboxOverride("normal", BoxStyle(new Color("e8eef1"), new Color("9aa7ad")));
        center.AddChild(blockInterfaceSummary);

        var scroll = new ScrollContainer
        {
            Name = "RoutineView",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 88),
        };
        center.AddChild(LadderEditorZoom.CreateControls(canvas, scroll));
        scroll.AddChild(canvas);
        center.AddChild(scroll);

        var bottomDockHost = new VBoxContainer { Name = "BottomDockHost" };
        bottomDockHost.AddThemeConstantOverride("separation", 0);
        var reopenBottomDock = ToolbarButton("ReopenBottomDock", "DIAGNOSTICS  ▲", new Color("4d6674"), 175);
        reopenBottomDock.CustomMinimumSize = new Vector2(0, 26);
        reopenBottomDock.TooltipText = "Show errors, output, and watch data";
        reopenBottomDock.Visible = true;
        bottomDockHost.AddChild(reopenBottomDock);
        var bottomPanel = PanelContainer("InspectorOutputDock", new Color("eef1f3"), new Color("9aa7ad"));
        bottomPanel.Visible = false;
        // Title, tabs and watch controls consume most of a 120 px dock,
        // leaving the actual feedback table below the visible aperture.
        bottomPanel.CustomMinimumSize = new Vector2(0, 180);
        var dockStyle = BoxStyle(new Color("eef1f3"), new Color("9aa7ad"));
        dockStyle.ContentMarginTop = dockStyle.ContentMarginBottom = 2;
        bottomPanel.AddThemeStyleboxOverride("panel", dockStyle);
        bottomPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        bottomPanel.TooltipText = "Drag the divider above this dock to resize Error List, Output, and Watch data.";
        var bottomBody = new VBoxContainer { Name = "InspectorOutputBody" };
        bottomBody.AddThemeConstantOverride("separation", 2);
        bottomPanel.AddChild(bottomBody);
        var bottomTitle = new HBoxContainer { Name = "InspectorOutputTitle" };
        var bottomHeading = Heading(siemens ? "Diagnostics / Watch" : "Output / Errors / Watch", 12, new Color("263943"));
        bottomHeading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        bottomTitle.AddChild(bottomHeading);
        var collapseBottomDock = ToolbarButton("CollapseBottomDock", "▼", new Color("637985"), 32);
        collapseBottomDock.CustomMinimumSize = new Vector2(32, 24);
        foreach (var state in new[] { "normal", "hover", "pressed" })
        {
            var compactStyle = (StyleBoxFlat)collapseBottomDock.GetThemeStylebox(state).Duplicate();
            compactStyle.ContentMarginTop = compactStyle.ContentMarginBottom = 2;
            collapseBottomDock.AddThemeStyleboxOverride(state, compactStyle);
        }
        collapseBottomDock.TooltipText = "Hide errors, output, and watch data";
        bottomTitle.AddChild(collapseBottomDock);
        bottomBody.AddChild(bottomTitle);
        var bottomTabs = new TabContainer { Name = "InspectorOutputTabs", SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        bottomTabs.AddThemeFontSizeOverride("font_size", 11);
        bottomTabs.AddThemeColorOverride("font_selected_color", new Color("263943"));
        bottomTabs.AddThemeColorOverride("font_unselected_color", new Color("4f6874"));
        var dockPageStyle = BoxStyle(new Color("f8f9fa"), new Color("b7c0c5"));
        dockPageStyle.ContentMarginTop = dockPageStyle.ContentMarginBottom = 2;
        bottomTabs.AddThemeStyleboxOverride("panel", dockPageStyle);
        bottomTabs.AddThemeStyleboxOverride("tab_selected", BoxStyle(new Color("ffffff"), accent));
        bottomTabs.AddThemeStyleboxOverride("tab_unselected", BoxStyle(new Color("d9dfe2"), new Color("aeb8bd")));
        bottomBody.AddChild(bottomTabs);

        void AddInspectorPage(VBoxContainer content)
        {
            // A short desktop window must still expose the dock footer and
            // error/watch controls. Scroll content rather than propagating
            // every tab's form height into the entire workbench minimum.
            var page = new ScrollContainer
            {
                Name = content.Name,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            };
            content.Name = "Content";
            content.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            page.AddChild(content);
            bottomTabs.AddChild(page);
        }

        var errorPage = new VBoxContainer { Name = "Error List" };
        validationSummary = Heading("0 ERRORS · program not yet verified", 11, new Color("4f6874"));
        validationSummary.Name = "ValidationSummary";
        errorPage.AddChild(validationSummary);
        validationIssueList = new ItemList
        {
            Name = "ValidationIssues",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
        };
        validationIssueList.AddThemeColorOverride("font_color", new Color("8c2f2f"));
        validationIssueList.AddThemeColorOverride("font_selected_color", new Color("ffffff"));
        ConfigureSelectableList(validationIssueList);
        validationIssueList.AddThemeStyleboxOverride("panel", BoxStyle(new Color("fffafa"), new Color("c9a2a2")));
        errorPage.AddChild(validationIssueList);
        var validationDetail=new Label { Name="SelectedIssueDetail", AutowrapMode=TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal=Control.SizeFlags.ExpandFill };
        validationDetail.AddThemeColorOverride("font_color",new Color("8c2f2f"));
        validationDetail.AddThemeFontSizeOverride("font_size",12);
        var validationDetailScroll=new ScrollContainer { Name="SelectedIssueDetailScroll", SizeFlagsVertical=Control.SizeFlags.ExpandFill, HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled, CustomMinimumSize=new Vector2(0,48) };
        validationDetailScroll.AddChild(validationDetail);
        errorPage.AddChild(validationDetailScroll);
        validationIssueList.ItemSelected += i => validationDetail.Text=validationIssueList.GetItemTooltip((int)i);
        validationIssueList.CustomMinimumSize = new Vector2(0, 28);
        validationIssueList.SizeFlagsVertical=Control.SizeFlags.Fill;
        AddInspectorPage(errorPage);

        var outputPage = new VBoxContainer { Name = "Output" };
        var outputDescription = Heading(
            "Validation, compile, load, search, navigation, and controller-runtime messages appear here.",
            11, new Color("4f6874"));
        // Keep the explanation available without dedicating a fixed-height
        // row to it. The output itself already describes each operation.
        outputDescription.Visible = false;
        output.TooltipText = outputDescription.Text;
        outputPage.AddChild(outputDescription);
        output.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        outputPage.AddChild(output);
        AddInspectorPage(outputPage);

        var watchPage = new VBoxContainer { Name = siemens ? "Watch table 1" : "Watch List" };
        watchPage.AddThemeConstantOverride("separation", 3);
        var watchHeader = new HBoxContainer { Name = "WatchHeader" };
        var watchTableSelector = new OptionButton { Name = "WatchTableSelector", CustomMinimumSize = new Vector2(170, 30) };
        watchHeader.AddChild(watchTableSelector);
        var watchStatus = Heading("OFFLINE · PROGRAM NOT LOADED", 11, new Color("4f6874"));
        watchStatus.Name = "WatchStatus";
        watchStatus.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        watchHeader.AddChild(watchStatus);
        var addWatchTable = ToolbarButton("AddWatchTable", "+ TABLE", accent, 72);
        var removeWatchTable = ToolbarButton("RemoveWatchTable", "− TABLE", new Color("8a4d55"), 72);
        var renameWatchTable = ToolbarButton("RenameWatchTable", "RENAME", new Color("4d6674"), 72);
        var duplicateWatchTable = ToolbarButton("DuplicateWatchTable", "COPY", new Color("4d6674"), 60);
        var moveWatchTableUp = ToolbarButton("MoveWatchTableUp", "UP", new Color("4d6674"), 48);
        var moveWatchTableDown = ToolbarButton("MoveWatchTableDown", "DOWN", new Color("4d6674"), 52);
        renameWatchTable.TooltipText = "Rename the active watch table";
        duplicateWatchTable.TooltipText = "Copy the active watch table and its symbols";
        moveWatchTableUp.TooltipText = "Move the active watch table earlier in the project tree";
        moveWatchTableDown.TooltipText = "Move the active watch table later in the project tree";
        watchHeader.AddChild(addWatchTable);
        watchHeader.AddChild(renameWatchTable);
        watchHeader.AddChild(duplicateWatchTable);
        watchHeader.AddChild(moveWatchTableUp);
        watchHeader.AddChild(moveWatchTableDown);
        watchHeader.AddChild(removeWatchTable);
        watchPage.AddChild(watchHeader);
        var watchSymbolHeader = new HBoxContainer { Name = "WatchSymbolActions" };
        watchSymbolHeader.AddThemeConstantOverride("separation", 4);
        var watchSymbol = new OptionButton { Name = "WatchSymbol", CustomMinimumSize = new Vector2(180, 30) };
        watchSymbolHeader.AddChild(watchSymbol);
        var addWatch = ToolbarButton("AddWatchSymbol", "+ SYMBOL", accent, 82);
        var watchAddress = new LineEdit { Name = "WatchAbsoluteAddress", PlaceholderText = "Absolute address", CustomMinimumSize = new Vector2(145, 30), ClearButtonEnabled = true };
        var addWatchAddress = ToolbarButton("AddWatchAddress", "+ ADDRESS", new Color("4d6674"), 92);
        var removeWatch = ToolbarButton("RemoveWatchSymbol", "REMOVE", new Color("8a4d55"), 72);
        var moveWatchRowUp = ToolbarButton("MoveWatchRowUp", "↑ ROW", new Color("4d6674"), 60);
        var moveWatchRowDown = ToolbarButton("MoveWatchRowDown", "↓ ROW", new Color("4d6674"), 60);
        var addAllWatch = ToolbarButton("AddAllWatchSymbols", "ADD ALL", new Color("4d6674"), 72);
        var clearWatch = ToolbarButton("ClearWatchSymbols", "CLEAR", new Color("637985"), 58);
        var modifyWatch = ToolbarButton("ModifyWatchValues", "SIM MODIFY", new Color("a66b20"), 96);
        modifyWatch.TooltipText = "Apply selected Modify values to the offline simulator snapshot only. Never writes to a physical PLC.";
        watchSymbolHeader.AddChild(addWatch);
        watchAddress.Visible = addWatchAddress.Visible = siemens;
        watchSymbolHeader.AddChild(watchAddress);
        watchSymbolHeader.AddChild(addWatchAddress);
        watchSymbolHeader.AddChild(removeWatch);
        watchSymbolHeader.AddChild(moveWatchRowUp);
        watchSymbolHeader.AddChild(moveWatchRowDown);
        watchSymbolHeader.AddChild(addAllWatch);
        watchSymbolHeader.AddChild(clearWatch);
        watchSymbolHeader.AddChild(modifyWatch);
        watchPage.AddChild(watchSymbolHeader);
        var watchTable = new Tree
        {
            Name = "WatchTable",
            Columns = 8,
            ColumnTitlesVisible = true,
            HideRoot = true,
            AllowReselect = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        foreach (var (column, title) in new[]
                 {
                     (0, "NAME"), (1, "ADDRESS"), (2, "DISPLAY FORMAT"),
                     (3, "MONITOR VALUE"), (4, "MODIFY VALUE"), (5, "MODIFY"),
                     (6, "COMMENT"), (7, "TAG COMMENT"),
                 })
            watchTable.SetColumnTitle(column, title);
        watchTable.SetColumnExpand(0, true);
        watchTable.SetColumnCustomMinimumWidth(0, 115);
        watchTable.SetColumnExpand(1, true);
        watchTable.SetColumnCustomMinimumWidth(1, 100);
        watchTable.SetColumnExpand(2, true);
        watchTable.SetColumnCustomMinimumWidth(2, 100);
        watchTable.SetColumnExpand(3, true);
        watchTable.SetColumnCustomMinimumWidth(3, 95);
        watchTable.SetColumnExpand(4, true);
        watchTable.SetColumnCustomMinimumWidth(4, 95);
        watchTable.SetColumnExpand(5, false);
        watchTable.SetColumnCustomMinimumWidth(5, 56);
        watchTable.SetColumnExpand(6, true);
        watchTable.SetColumnCustomMinimumWidth(6, 110);
        watchTable.SetColumnExpand(7, true);
        watchTable.SetColumnCustomMinimumWidth(7, 110);
        watchTable.SetColumnExpand(5, true);
        watchTable.AddThemeColorOverride("font_color", new Color("263943"));
        ConfigureSelectableList(watchTable);
        watchTable.AddThemeStyleboxOverride("panel", BoxStyle(new Color("ffffff"), new Color("b7c0c5")));
        watchPage.AddChild(watchTable);
        watchTable.CustomMinimumSize = new Vector2(0, 66);
        AddInspectorPage(watchPage);
        VBoxContainer? watchDocument = null;
        Tree? watchDocumentTable = null;
        Label? watchDocumentTitle = null;
        Label? watchDocumentStatus = null;
        var watchDocumentRows = new List<TreeItem>();
        string[] watchDocumentRowNames = [];
        if (siemens)
        {
            watchDocument = new VBoxContainer
            {
                Name = "WatchTableDocument",
                Visible = false,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            };
            watchDocument.AddThemeConstantOverride("separation", 6);
            watchDocumentTitle = new Label
            {
                Name = "WatchTableDocumentTitle",
                Text = "Watch table",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            watchDocumentTitle.AddThemeColorOverride("font_color", new Color("202b31"));
            watchDocumentTitle.AddThemeFontSizeOverride("font_size", 20);
            watchDocument.AddChild(watchDocumentTitle);
            watchDocumentStatus = new Label
            {
                Name = "WatchTableDocumentStatus",
                Text = "Offline simulator values · no PLC connection",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            watchDocumentStatus.AddThemeColorOverride("font_color", new Color("4f6874"));
            watchDocument.AddChild(watchDocumentStatus);
            watchDocumentTable = new Tree
            {
                Name = "WatchTableDocumentGrid",
                Columns = 8,
                ColumnTitlesVisible = true,
                HideRoot = true,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0, 220),
            };
            foreach (var (column, title) in new[]
                     {
                         (0, "NAME"), (1, "ADDRESS"), (2, "DISPLAY FORMAT"),
                         (3, "MONITOR VALUE"), (4, "MODIFY VALUE"), (5, "MODIFY"),
                         (6, "COMMENT"), (7, "TAG COMMENT"),
                     })
                watchDocumentTable.SetColumnTitle(column, title);
            watchDocumentTable.SetColumnExpand(0, true);
            watchDocumentTable.SetColumnCustomMinimumWidth(0, 115);
            watchDocumentTable.SetColumnExpand(1, true);
            watchDocumentTable.SetColumnCustomMinimumWidth(1, 100);
            watchDocumentTable.SetColumnExpand(2, true);
            watchDocumentTable.SetColumnCustomMinimumWidth(2, 100);
            watchDocumentTable.SetColumnExpand(3, true);
            watchDocumentTable.SetColumnCustomMinimumWidth(3, 95);
            watchDocumentTable.SetColumnExpand(4, true);
            watchDocumentTable.SetColumnCustomMinimumWidth(4, 95);
            watchDocumentTable.SetColumnExpand(5, false);
            watchDocumentTable.SetColumnCustomMinimumWidth(5, 56);
            watchDocumentTable.SetColumnExpand(6, true);
            watchDocumentTable.SetColumnCustomMinimumWidth(6, 110);
            watchDocumentTable.SetColumnExpand(7, true);
            watchDocumentTable.SetColumnCustomMinimumWidth(7, 110);
            watchDocumentTable.AddThemeColorOverride("font_color", new Color("263943"));
            ConfigureSelectableList(watchDocumentTable);
            watchDocumentTable.AddThemeStyleboxOverride("panel", BoxStyle(new Color("ffffff"), new Color("b7c0c5")));
            watchDocument.AddChild(watchDocumentTable);
            center.AddChild(watchDocument);
        }
        bottomTabs.CurrentTab = 1;
        bottomTabs.SetDeferred("current_tab", 1);

        string[] watchTagNames = [];
        string[] watchRowNames = [];
        string[] watchTableNames = [];
        var refreshingWatchTableSelector = false;
        var watchRows = new List<TreeItem>();
        var watchModifySelection = new HashSet<string>(StringComparer.Ordinal);
        PlcVariable[] watchVariablesCache = [];
        string WatchModifyKey(EditableWatchRow row) => $"{document.ActiveWatchTable.Id}:{row.Id}";
        Action openWatchTableCreation = () => { };
        Action openWatchTableRename = () => { };

        string WatchValue(PlcVariable variable, VirtualControllerSnapshot snapshot)
        {
            if (PlcAggregates.IsAggregate(variable.Type)) return AggregateWatchValue(variable, snapshot);
            if (variable.Type is PlcVariableType.String or PlcVariableType.Enum)
                return snapshot.TextVariables.TryGetValue(variable.Name, out var text) ? text : "—";
            if (variable.Type == PlcVariableType.Bool)
                return snapshot.Variables.TryGetValue(variable.Name, out var boolean)
                    ? boolean.ToString().ToUpperInvariant() : "—";
            if (variable.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
                return snapshot.NumericVariables.TryGetValue(variable.Name, out var numeric)
                    ? variable.Type == PlcVariableType.Real
                        ? WatchTableDisplayFormat.FormatReal(numeric, "Auto")
                        : numeric.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                    : "—";
            if (variable.Type == PlcVariableType.Time && snapshot.NumericVariables.TryGetValue(variable.Name, out var milliseconds))
                return LadderTimeLiteral.Format(TimeSpan.FromMilliseconds(milliseconds));
            if (variable.Type == PlcVariableType.Timer && snapshot.Timers.TryGetValue(variable.Name, out var timer))
                return $"ET={timer.Accumulated.TotalMilliseconds:0} ms · Q={timer.Done.ToString().ToUpperInvariant()}";
            if (variable.Type == PlcVariableType.Counter && snapshot.Counters.TryGetValue(variable.Name, out var counter))
                return $"CV/ACC={counter.Accumulated} · Q/DN={counter.Done.ToString().ToUpperInvariant()}";
            return "—";
        }

        string NormalizeWatchAddress(string address) =>
            (address ?? string.Empty).Trim().TrimStart('%').ToUpperInvariant();

        PlcVariable? ResolveWatchVariable(EditableWatchRow row)
        {
            if (row.ReferenceKind == WatchReferenceKind.Symbolic)
                return watchVariablesCache.FirstOrDefault(tag => tag.Name.Equals(row.Reference, StringComparison.Ordinal));
            var matches = watchVariablesCache.Where(tag => !string.IsNullOrWhiteSpace(tag.Address)
                && SiemensPlcTagAddress.IsPersistedS7AddressCandidate(tag.Address)
                && NormalizeWatchAddress(tag.Address).Equals(NormalizeWatchAddress(row.Reference), StringComparison.Ordinal))
                .Where(tag => SiemensPlcTagAddress.TryValidateWatchAddress(row.Reference, tag.Type, tag.Role, out _))
                .Take(2).ToArray();
            // Absolute references are only meaningful in this offline model
            // when they exactly identify one declared tag. Never infer a DB
            // member from a byte offset or alias a partial address span.
            return matches.Length == 1 ? matches[0] : null;
        }

        string[] WatchDisplayFormats(PlcVariable? variable) =>
            WatchTableDisplayFormat.ForType(variable?.Type).ToArray();

        string FormatWatchValue(EditableWatchRow row, PlcVariable variable, VirtualControllerSnapshot snapshot)
        {
            var format = row.DisplayFormat.Trim();
            if (format.Equals("Binary", StringComparison.OrdinalIgnoreCase)) format = "Bin";
            if (format.Equals("Signed decimal", StringComparison.OrdinalIgnoreCase)) format = "Dec+/-";
            if (format.Equals("Auto", StringComparison.OrdinalIgnoreCase)) return WatchValue(variable, snapshot);
            if (variable.Type == PlcVariableType.Bool && snapshot.Variables.TryGetValue(variable.Name, out var boolean))
            {
                if (format.Equals("Bool", StringComparison.OrdinalIgnoreCase)) return boolean ? "TRUE" : "FALSE";
                var bit = boolean ? 1 : 0;
                return format.Equals("Hex", StringComparison.OrdinalIgnoreCase) ? $"16#{bit:X}" : bit.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            if (variable.Type is PlcVariableType.Int or PlcVariableType.DInt
                && snapshot.NumericVariables.TryGetValue(variable.Name, out var number))
            {
                var integer = checked((long)Math.Truncate(number));
                return WatchTableDisplayFormat.FormatInteger(integer, variable.Type, format);
            }
            if (variable.Type == PlcVariableType.Real && snapshot.NumericVariables.TryGetValue(variable.Name, out var real))
                return WatchTableDisplayFormat.FormatReal(real, format);
            if (variable.Type == PlcVariableType.Time && snapshot.NumericVariables.TryGetValue(variable.Name, out var milliseconds))
            {
                var value = (long)Math.Truncate(milliseconds);
                return format.ToUpperInvariant() switch
                {
                    "TIMER" => LadderTimeLiteral.Format(TimeSpan.FromMilliseconds(milliseconds)),
                    "HEX" => $"16#{unchecked((uint)value):X8}",
                    "BIN" => Convert.ToString(unchecked((uint)value), 2),
                    "DEC" or "DEC+/-" => value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    _ => WatchValue(variable, snapshot),
                };
            }
            if ((variable.Type == PlcVariableType.String || variable.Type == PlcVariableType.Enum)
                && snapshot.TextVariables.TryGetValue(variable.Name, out var textValue))
                return textValue;
            return WatchValue(variable, snapshot);
        }

        bool TryParseWatchModifyValue(PlcVariable variable, string text, out object value, out string error)
        {
            var source = text;
            text = text.Trim();
            value = false;
            error = string.Empty;
            switch (variable.Type)
            {
                case PlcVariableType.Bool:
                    if (bool.TryParse(text, out var boolean)) { value = boolean; return true; }
                    if (text == "1") { value = true; return true; }
                    if (text == "0") { value = false; return true; }
                    error = "BOOL Modify value must be TRUE/FALSE or 1/0.";
                    return false;
                case PlcVariableType.Int:
                    if (short.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var integer))
                    { value = (int)integer; return true; }
                    error = "INT Modify value must be a signed 16-bit integer.";
                    return false;
                case PlcVariableType.DInt:
                    if (int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var dInteger))
                    { value = dInteger; return true; }
                    error = "DINT Modify value must be a signed 32-bit integer.";
                    return false;
                case PlcVariableType.Real:
                    if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var real)
                        && double.IsFinite(real) && Math.Abs(real) <= float.MaxValue)
                    { value = real; return true; }
                    error = "REAL Modify value must be a finite single-precision number.";
                    return false;
                case PlcVariableType.Time:
                    if (LadderTimeLiteral.TryParse(text, out var duration)) { value = duration; return true; }
                    error = "TIME Modify value must use a supported T# duration literal.";
                    return false;
                case PlcVariableType.String:
                    if (PlcTextValues.IsValid(variable, source))
                    { value = source; return true; }
                    error = $"STRING Modify value must be {PlcTextValues.MaximumLength} characters or fewer.";
                    return false;
                case PlcVariableType.Enum:
                    if (variable.EnumMembers?.Contains(text, StringComparer.Ordinal) == true)
                    { value = text; return true; }
                    error = "ENUM Modify value must match one of the declared members.";
                    return false;
                default:
                    error = $"{variable.Type.ToString().ToUpperInvariant()} Modify values are not supported by the offline watch-table simulator.";
                    return false;
            }
        }

        void RefreshWatchTable(VirtualControllerSnapshot? snapshot)
        {
            // Scan publication changes values, not the controls' identity.
            // Clearing each scan loses selection and disrupts an open symbol
            // menu or a Remove click between mouse-down and mouse-up.
            watchVariablesCache = AggregateWatchVariables(document.Tags).ToArray();
            var watchVariables = watchVariablesCache;
            var currentTableNames = document.WatchTables.Select(table => table.Name).ToArray();
            if (!watchTableNames.SequenceEqual(currentTableNames, StringComparer.Ordinal))
            {
                watchTableSelector.Clear();
                foreach (var tableName in currentTableNames) watchTableSelector.AddItem(tableName);
                watchTableNames = currentTableNames;
            }
            refreshingWatchTableSelector = true;
            watchTableSelector.Select(document.ActiveWatchTableIndex);
            refreshingWatchTableSelector = false;
            modifyWatch.Disabled = snapshot is null || !_ladderMonitorMatchesLoadedProgram;
            removeWatchTable.Disabled = document.WatchTables.Count <= 1;
            moveWatchTableUp.Disabled = document.ActiveWatchTableIndex <= 0;
            moveWatchTableDown.Disabled = document.ActiveWatchTableIndex >= document.WatchTables.Count - 1;
            var tagNames = watchVariables.Select(tag => tag.Name).ToArray();
            if (!watchTagNames.SequenceEqual(tagNames, StringComparer.Ordinal))
            {
                var selectedSymbol = watchSymbol.Selected >= 0 ? watchSymbol.GetItemText(watchSymbol.Selected) : string.Empty;
                watchSymbol.Clear();
                foreach (var name in tagNames) watchSymbol.AddItem(name);
                var selectedIndex = Array.IndexOf(tagNames, selectedSymbol);
                if (selectedIndex >= 0) watchSymbol.Select(selectedIndex);
                watchTagNames = tagNames;
            }
            // Keep unresolved references visible so stale symbols and absolute
            // addresses without an exact declared-tag match can be corrected.
            var rows = document.ActiveWatchTable.Rows
                .Select(row => (Row: row, Variable: ResolveWatchVariable(row)))
                .ToArray();
            var rowNames = rows.Select(entry => entry.Row.Id).ToArray();
            watchModifySelection.IntersectWith(rows.Select(entry => WatchModifyKey(entry.Row)));
            RefreshWatchRowActionState();
            if (watchTable.GetRoot() is null || !watchRowNames.SequenceEqual(rowNames, StringComparer.Ordinal))
            {
                var selectedName = watchTable.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
                watchTable.Clear();
                watchRows.Clear();
                var rootItem = watchTable.CreateItem();
                foreach (var name in rowNames)
                {
                    var item = watchTable.CreateItem(rootItem);
                    item.SetMetadata(0, name);
                    item.SetCellMode(5, TreeItem.TreeCellMode.Check);
                    item.SetEditable(5, true);
                    item.SetEditable(2, true);
                    item.SetEditable(4, true);
                    item.SetEditable(6, true);
                    if (name == selectedName) item.Select(0);
                    watchRows.Add(item);
                }
                watchRowNames = rowNames;
            }
            for (var index = 0; index < rows.Length; index++)
            {
                var (row, variable) = rows[index];
                var item = watchRows[index];
                item.SetText(0, row.ReferenceKind == WatchReferenceKind.Symbolic ? row.Reference : string.Empty);
                item.SetText(1, row.ReferenceKind == WatchReferenceKind.AbsoluteAddress
                    ? row.Reference
                    : variable is null || string.IsNullOrWhiteSpace(variable.Address) ? "—" : variable.Address);
                item.SetText(2, row.DisplayFormat);
                item.SetText(4, row.ModifyValue ?? string.Empty);
                item.SetCellMode(5, TreeItem.TreeCellMode.Check);
                item.SetChecked(5, watchModifySelection.Contains(WatchModifyKey(row)));
                item.SetEditable(5, true);
                item.SetEditable(2, true);
                item.SetEditable(4, true);
                item.SetEditable(6, true);
                item.SetText(6, row.Comment);
                item.SetText(7, variable?.Comment ?? string.Empty);
                if (variable is null)
                {
                    item.SetText(3, "UNRESOLVED");
                    item.SetCustomColor(3, new Color("b36c16"));
                    item.SetTooltipText(1, row.ReferenceKind == WatchReferenceKind.AbsoluteAddress
                        ? "No single declared PLC tag exactly matches this absolute address. Runtime value and simulator modification are unavailable for an unmapped address."
                        : "This saved symbolic watch entry does not match a current PLC tag. Runtime value and simulator modification are unavailable until it is repaired.");
                    item.SetTooltipText(3, "UNRESOLVED · simulator does not infer addresses or partial DB spans.");
                    continue;
                }
                var current = snapshot is not null && _ladderMonitorMatchesLoadedProgram;
                item.SetText(3, current ? FormatWatchValue(row, variable, snapshot!) : "—");
                var force = snapshot?.Forces.GetValueOrDefault(variable.Name);
                item.SetTooltipText(3, !current ? snapshot is null ? "No simulator program loaded." : "Editor differs from the loaded simulator program."
                    : force is not null ? $"SIM FORCE {force.Value.ToString().ToUpperInvariant()} · offline only"
                    : snapshot!.State == VirtualControllerState.Running ? $"SIMULATOR VALUE · SCAN {snapshot.ScanNumber} · offline only"
                    : "SIMULATOR VALUE · STOPPED · offline only");
                item.SetTooltipText(7, string.IsNullOrWhiteSpace(variable.Comment) ? "No PLC tag comment." : variable.Comment);
            }
            if (watchDocument is not null && watchDocumentTable is not null)
            {
                watchDocumentTitle!.Text = document.ActiveWatchTable.Name;
                watchDocumentStatus!.Text = watchStatus.Text;
                if (watchDocumentTable.GetRoot() is null
                    || !watchDocumentRowNames.SequenceEqual(rowNames, StringComparer.Ordinal))
                {
                    var selectedName = watchDocumentTable.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
                    watchDocumentTable.Clear();
                    watchDocumentRows.Clear();
                    var rootItem = watchDocumentTable.CreateItem();
                    foreach (var name in rowNames)
                    {
                        var item = watchDocumentTable.CreateItem(rootItem);
                        item.SetMetadata(0, name);
                        item.SetCellMode(5, TreeItem.TreeCellMode.Check);
                        item.SetEditable(2, true);
                        item.SetEditable(4, true);
                        item.SetEditable(6, true);
                        if (name == selectedName) item.Select(0);
                        watchDocumentRows.Add(item);
                    }
                    watchDocumentRowNames = rowNames;
                }
                for (var index = 0; index < rows.Length; index++)
                    for (var column = 0; column < watchTable.Columns; column++)
                    {
                        watchDocumentRows[index].SetText(column, watchRows[index].GetText(column));
                        if (column == 5)
                        {
                            watchDocumentRows[index].SetCellMode(5, TreeItem.TreeCellMode.Check);
                            watchDocumentRows[index].SetEditable(5, true);
                            watchDocumentRows[index].SetChecked(5, watchRows[index].IsChecked(5));
                        }
                        if (column is 2 or 4 or 6) watchDocumentRows[index].SetEditable(column, true);
                        watchDocumentRows[index].SetTooltipText(column, watchRows[index].GetTooltipText(column));
                    }
            }
            var tablePrefix = $"{document.ActiveWatchTable.Name} · ";
            watchStatus.Text = snapshot is null
                ? $"{tablePrefix}{rows.Length} ROW(S) · PROGRAM NOT LOADED"
                : !_ladderMonitorMatchesLoadedProgram
                    ? $"{tablePrefix}{rows.Length} ROW(S) · EDIT DIFFERS FROM LOADED PROGRAM · SIMULATOR ONLY"
                    : $"{tablePrefix}{rows.Length} ROW(S) · {snapshot.State.ToString().ToUpperInvariant()} · SCAN {snapshot.ScanNumber} · SIMULATOR ONLY";
            watchStatus.AddThemeColorOverride("font_color",
                snapshot is not null && _ladderMonitorMatchesLoadedProgram ? new Color("18864b") : new Color("b36c16"));
            if (watchDocumentStatus is not null) watchDocumentStatus.Text = watchStatus.Text;
        }

        void RefreshWatchMetadataEditors()
        {
            // Watch membership is saved project metadata. Refresh editor titles,
            // selector items, monitor rows, and Undo/Redo without invalidating
            // the executable controller or its monitoring snapshot.
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            foreach (var refresh in _ladderWatchRefreshers) refresh(_virtualSnapshot);
        }

        void SelectActiveWatchTableTreeRow()
        {
            if (!siemens || document.ActiveWatchTableIndex < 0 || document.ActiveWatchTableIndex >= watchTableItems.Count) return;
            var item = watchTableItems[document.ActiveWatchTableIndex];
            item.Select(0);
            organization.ScrollToItem(item);
        }
        if (siemens) _restoreActiveWatchTableTreeRow = SelectActiveWatchTableTreeRow;

        void RefreshWatchTableAfterRejectedEdit(string message)
        {
            output.Text = $"[color=#d64545]Watch-table edit rejected.[/color] {Escape(message)}";
            RefreshWatchMetadataEditors();
        }

        void WatchCellEdited(Tree grid)
        {
            var edited = grid.GetEdited();
            if (edited is null) return;
            var id = edited.GetMetadata(0).AsString();
            var row = document.ActiveWatchTable.Rows.FirstOrDefault(candidate => candidate.Id == id);
            if (row is null) return;
            var column = grid.GetEditedColumn();
            if (column == 5)
            {
                var key = WatchModifyKey(row);
                if (edited.IsChecked(5)) watchModifySelection.Add(key);
                else watchModifySelection.Remove(key);
                return;
            }

            var variable = ResolveWatchVariable(row);
            var updatedFormat = row.DisplayFormat;
            var updatedModify = row.ModifyValue;
            var updatedComment = row.Comment;
            if (column == 2)
            {
                updatedFormat = edited.GetText(2).Trim();
                if (updatedFormat.Equals("Binary", StringComparison.OrdinalIgnoreCase)) updatedFormat = "Bin";
                if (updatedFormat.Equals("Signed decimal", StringComparison.OrdinalIgnoreCase)) updatedFormat = "Dec+/-";
                if (!WatchDisplayFormats(variable).Contains(updatedFormat, StringComparer.OrdinalIgnoreCase))
                {
                    RefreshWatchTableAfterRejectedEdit(variable is null
                        ? "An unresolved reference can only use Auto display format."
                        : $"{updatedFormat} is not a supported format for {variable.Type.ToString().ToUpperInvariant()}.");
                    return;
                }
            }
            else if (column == 4)
            {
                var candidate = edited.GetText(4);
                updatedModify = string.IsNullOrWhiteSpace(candidate) ? null : candidate;
                if (updatedModify is not null && variable is not null
                    && !TryParseWatchModifyValue(variable, updatedModify, out _, out var modifyError))
                {
                    RefreshWatchTableAfterRejectedEdit(modifyError);
                    return;
                }
            }
            else if (column == 6)
                updatedComment = edited.GetText(6);
            else return;

            if (updatedFormat == row.DisplayFormat && updatedModify == row.ModifyValue && updatedComment == row.Comment) return;
            if (!BeginProjectMetadataEdit("Edit watch-table row"))
            {
                RefreshWatchMetadataEditors();
                return;
            }
            row.DisplayFormat = updatedFormat;
            row.ModifyValue = updatedModify;
            row.Comment = updatedComment;
            RefreshWatchMetadataEditors();
        }
        watchTable.ItemEdited += () => WatchCellEdited(watchTable);
        if (watchDocumentTable is not null)
            watchDocumentTable.ItemEdited += () => WatchCellEdited(watchDocumentTable);

        string SelectedWatchRowId()
        {
            var selected = watchDocumentTable?.GetSelected() ?? watchTable.GetSelected();
            return selected?.GetMetadata(0).AsString() ?? string.Empty;
        }

        void RefreshWatchRowActionState()
        {
            var rows = document.ActiveWatchTable.Rows;
            var index = rows.FindIndex(row => row.Id == SelectedWatchRowId());
            removeWatch.Disabled = rows.Count == 0 || index < 0;
            moveWatchRowUp.Disabled = index <= 0;
            moveWatchRowDown.Disabled = index < 0 || index >= rows.Count - 1;
        }

        watchTable.ItemSelected += RefreshWatchRowActionState;
        if (watchDocumentTable is not null) watchDocumentTable.ItemSelected += RefreshWatchRowActionState;

        addWatch.Pressed += () =>
        {
            if (watchSymbol.Selected < 0) return;
            var name = watchSymbol.GetItemText(watchSymbol.Selected);
            if (document.WatchVariables.Contains(name, StringComparer.Ordinal)) return;
            if (!BeginProjectMetadataEdit("Add watch-table symbol")) return;
            document.ActiveWatchTable.Rows.Add(new EditableWatchRow(name));
            RefreshWatchMetadataEditors();
        };
        addWatchAddress.Pressed += () =>
        {
            var address = watchAddress.Text.Trim();
            if (!SiemensPlcTagAddress.TryValidateWatchAddress(address, out var addressError))
            {
                output.Text = $"[color=#d64545]Absolute watch address rejected.[/color] {Escape(addressError)}";
                return;
            }
            if (document.ActiveWatchTable.Rows.Any(row => row.ReferenceKind == WatchReferenceKind.AbsoluteAddress
                    && NormalizeWatchAddress(row.Reference).Equals(NormalizeWatchAddress(address), StringComparison.Ordinal)))
            {
                output.Text = "[color=#d64545]Absolute watch address rejected.[/color] That address is already in the active table.";
                return;
            }
            if (!BeginProjectMetadataEdit("Add absolute watch address")) return;
            document.ActiveWatchTable.Rows.Add(new EditableWatchRow(address, WatchReferenceKind.AbsoluteAddress));
            watchAddress.Clear();
            RefreshWatchMetadataEditors();
        };
        removeWatch.Pressed += () =>
        {
            var id = SelectedWatchRowId();
            var index = document.ActiveWatchTable.Rows.FindIndex(row => row.Id == id);
            if (index < 0 || !BeginProjectMetadataEdit("Remove watch-table row")) return;
            document.ActiveWatchTable.Rows.RemoveAt(index);
            watchModifySelection.Remove(id);
            RefreshWatchMetadataEditors();
        };
        void MoveWatchRow(int offset)
        {
            var id = SelectedWatchRowId();
            var rows = document.ActiveWatchTable.Rows;
            var index = rows.FindIndex(row => row.Id == id);
            var destination = index + offset;
            if (index < 0 || destination < 0 || destination >= rows.Count) return;
            if (!BeginProjectMetadataEdit("Reorder watch-table rows")) return;
            var row = rows[index];
            rows.RemoveAt(index);
            rows.Insert(destination, row);
            RefreshWatchMetadataEditors();
            output.Text = $"[color=#18864b]Watch-table row moved.[/color] {Escape(row.Reference)}";
        }
        moveWatchRowUp.Pressed += () => MoveWatchRow(-1);
        moveWatchRowDown.Pressed += () => MoveWatchRow(1);
        modifyWatch.Pressed += () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            if (_virtualSnapshot is null || !_ladderMonitorMatchesLoadedProgram)
            {
                output.Text = "[color=#d64545]SIM MODIFY unavailable.[/color] Verify + Load the current offline project first.";
                return;
            }
            var selectedRows = document.ActiveWatchTable.Rows
                .Where(row => watchModifySelection.Contains(WatchModifyKey(row))).ToArray();
            if (selectedRows.Length == 0)
            {
                output.Text = "Select at least one row in the MODIFY column.";
                return;
            }
            var modifyRequests = new List<(string Name, object Value)>();
            foreach (var row in selectedRows)
            {
                var variable = ResolveWatchVariable(row);
                if (variable is null)
                {
                    output.Text = $"[color=#d64545]SIM MODIFY unavailable.[/color] '{Escape(row.Reference)}' does not exactly match one declared PLC tag.";
                    return;
                }
                if (_virtualSnapshot!.Forces.ContainsKey(variable.Name))
                {
                    output.Text = $"[color=#d64545]SIM MODIFY rejected.[/color] Clear the active simulator force on {Escape(variable.Name)} first.";
                    return;
                }
                if (variable.Role == PlcVariableRole.Output && _virtualSnapshot.State != VirtualControllerState.Running)
                {
                    output.Text = $"[color=#d64545]SIM MODIFY rejected.[/color] Output {Escape(variable.Name)} can only be changed while the offline simulator is RUNNING.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(row.ModifyValue))
                {
                    output.Text = $"[color=#d64545]SIM MODIFY rejected.[/color] Enter a Modify value for {Escape(variable.Name)}.";
                    return;
                }
                if (!TryParseWatchModifyValue(variable, row.ModifyValue, out var parsedValue, out var modifyError))
                {
                    output.Text = $"[color=#d64545]SIM MODIFY rejected.[/color] {Escape(modifyError)}";
                    return;
                }
                modifyRequests.Add((variable.Name, parsedValue));
            }
            if (VirtualWatchModifyRequested is null)
            {
                output.Text = "[color=#d64545]SIM MODIFY unavailable.[/color] The offline simulator is not attached.";
                return;
            }
            if (!VirtualWatchModifyRequested.Invoke(modifyRequests.Select(request =>
                    (VariableName: request.Name, Value: request.Value)).ToArray()))
            {
                output.Text = "[color=#d64545]SIM MODIFY rejected.[/color] The offline runtime rejected the complete edit batch; no partial edit was applied.";
                return;
            }
            output.Text = "[color=#18864b]SIM MODIFY applied to the offline simulator only.[/color] No physical PLC was written.";
        };
        addAllWatch.Pressed += () =>
        {
            // Match the Add All scope to the symbol picker: aggregate roots
            // and their individually selectable fields/elements are valid
            // watch operands too.
            var missing = AggregateWatchVariables(document.Tags).Select(tag => tag.Name)
                .Where(name => !document.WatchVariables.Contains(name, StringComparer.Ordinal)).ToArray();
            if (missing.Length == 0 || !BeginProjectMetadataEdit("Add all watch-table symbols")) return;
            document.WatchVariables.AddRange(missing);
            RefreshWatchMetadataEditors();
        };
        clearWatch.Pressed += () =>
        {
            if (document.ActiveWatchTable.Rows.Count == 0 || !BeginProjectMetadataEdit("Clear watch table")) return;
            document.ActiveWatchTable.Rows.Clear();
            watchModifySelection.Clear();
            RefreshWatchMetadataEditors();
        };
        addWatchTable.Pressed += () => openWatchTableCreation();
        renameWatchTable.Pressed += () => openWatchTableRename();
        Action<EditableWatchTable> openWatchTableDocumentForMutation = _ =>
            throw new InvalidOperationException("Watch-table editor is not initialized yet.");
        duplicateWatchTable.Pressed += () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            if (!BeginProjectMetadataEdit("Duplicate watch table")) return;
            var duplicate = document.DuplicateActiveWatchTable();
            // Change the active editor identity before refreshing. Otherwise
            // refresh restores the source table tab and silently reselects it.
            openWatchTableDocumentForMutation(duplicate);
            RefreshWatchMetadataEditors();
            SelectActiveWatchTableTreeRow();
            output.Text = $"[color=#18864b]Watch table duplicated.[/color] {Escape(duplicate.Name)}";
        };
        moveWatchTableUp.Pressed += () => MoveWatchTable(-1);
        moveWatchTableDown.Pressed += () => MoveWatchTable(1);
        removeWatchTable.Pressed += () =>
        {
            if (document.WatchTables.Count <= 1 || !BeginProjectMetadataEdit("Delete watch table")) return;
            var removedName = document.ActiveWatchTable.Name;
            document.RemoveActiveWatchTable();
            RefreshWatchMetadataEditors();
            SelectActiveWatchTableTreeRow();
            output.Text = $"[color=#18864b]Watch table deleted.[/color] {Escape(removedName)}";
        };

        void MoveWatchTable(int offset)
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            var destination = document.ActiveWatchTableIndex + offset;
            if (destination < 0 || destination >= document.WatchTables.Count) return;
            if (!BeginProjectMetadataEdit("Reorder watch tables")) return;
            if (!document.MoveActiveWatchTable(offset)) return;
            RefreshWatchMetadataEditors();
            SelectActiveWatchTableTreeRow();
            output.Text = $"[color=#18864b]Watch table order updated.[/color] {Escape(document.ActiveWatchTable.Name)}";
        }
        bottomDockHost.AddChild(bottomPanel);
        editorAndInspector.AddChild(bottomDockHost);

        collapseBottomDock.Pressed += () =>
        {
            bottomDockSplitOffset = editorAndInspector.SplitOffsets.Length > 0
                ? editorAndInspector.SplitOffsets[0]
                : bottomDockSplitOffset;
            bottomPanel.Visible = false;
            reopenBottomDock.Visible = true;
            editorAndInspector.SplitOffsets = [Math.Max(0, (int)editorAndInspector.Size.Y - (int)reopenBottomDock.CustomMinimumSize.Y)];
        };
        reopenBottomDock.Pressed += () =>
        {
            reopenBottomDock.Visible = false;
            bottomPanel.Visible = true;
            editorAndInspector.SplitOffsets = [bottomDockSplitOffset];
        };

        var toolPanel = PanelContainer("InstructionAndTags", new Color("eef0f2"), new Color("9aa7ad"));
        // Give the instruction identity enough room to remain readable at the
        // default desktop layout. The adjacent splitter still lets the user
        // enlarge this dock; full names are also retained as Tree tooltips.
        toolPanel.CustomMinimumSize = new Vector2(300, 0);
        toolPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolPanel.TooltipText = "Drag the divider at the left edge to resize this dock.";
        toolPanel.ClipContents = true;
        var toolBody = new VBoxContainer
        {
            Name = "ToolBody",
            ClipContents = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        toolPanel.AddChild(toolBody);
        var toolTitle = new HBoxContainer { Name = "ToolTitle" };
        var toolHeading = Heading(siemens ? "Instructions / PLC tags" : "Instructions / Controller tags", 13, new Color("263943"));
        toolHeading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolTitle.AddChild(toolHeading);
        var collapseToolDock = ToolbarButton("CollapseToolDock", "▶", new Color("637985"), 30);
        collapseToolDock.TooltipText = siemens ? "Hide Instructions / PLC tags" : "Hide Instruction Toolbox";
        toolTitle.AddChild(collapseToolDock);
        toolBody.AddChild(toolTitle);
        var toolTabs = new TabContainer { Name = "ToolTabs" };
        toolTabs.ClipContents = true;
        toolTabs.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolTabs.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        toolTabs.TabsVisible = false;
        toolTabs.AddThemeFontSizeOverride("font_size", 11);
        var toolTabsHost = new HBoxContainer
        {
            Name = "ToolTabsHost",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        toolTabsHost.AddThemeConstantOverride("separation", 4);
        toolTabsHost.AddChild(toolTabs);
        toolBody.AddChild(toolTabsHost);
        var toolRail = new VBoxContainer
        {
            Name = "ToolTabRail",
            CustomMinimumSize = new Vector2(94, 0),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        toolRail.AddThemeConstantOverride("separation", 3);
        toolTabsHost.AddChild(toolRail);
        var toolRailButtons = new List<Button>();
        toolTabs.TabChanged += tabIndex =>
        {
            var railIndex = tabIndex == 0 ? 0 : tabIndex == 1 ? 3 : 4;
            for (var index = 0; index < toolRailButtons.Count; index++)
                toolRailButtons[index].ButtonPressed = index == railIndex;
        };
        var instructionPage = new VBoxContainer { Name = "Instructions" };
        var instructionTree = new Tree
        {
            Name = "InstructionTree",
            HideRoot = false,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
        };
        instructionTree.AddThemeColorOverride("font_color", new Color("263943"));
        instructionTree.AddThemeColorOverride("font_selected_color", new Color("ffffff"));
        ConfigureSelectableList(instructionTree);
        instructionTree.AddThemeColorOverride("guide_color", new Color("aeb8bd"));
        instructionTree.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        var instructionRoot = instructionTree.CreateItem();
        instructionRoot.SetText(0, "Basic instructions");
        var bitLogic = instructionTree.CreateItem(instructionRoot);
        bitLogic.SetText(0, "Bit logic operations");
        var noInstruction = instructionTree.CreateItem(bitLogic);
        noInstruction.SetText(0, siemens ? "Normally open contact  —| |—" : "Examine On (XIC)  —] [—");
        noInstruction.SetMetadata(0, "no");
        var ncInstruction = instructionTree.CreateItem(bitLogic);
        ncInstruction.SetText(0, siemens ? "Normally closed contact  —|/|—" : "Examine Off (XIO)  —]/[—");
        ncInstruction.SetMetadata(0, "nc");
        var risingEdgeInstruction = instructionTree.CreateItem(bitLogic);
        risingEdgeInstruction.SetText(0, siemens ? "Positive edge contact  —|P|—" : "Rising edge composite  XIC + ONS");
        risingEdgeInstruction.SetMetadata(0, "edge-rising");
        var fallingEdgeInstruction = instructionTree.CreateItem(bitLogic);
        fallingEdgeInstruction.SetText(0, siemens ? "Negative edge contact  —|N|—" : "Falling edge composite  XIO + ONS / OSF");
        fallingEdgeInstruction.SetMetadata(0, "edge-falling");
        var coilInstruction = instructionTree.CreateItem(bitLogic);
        coilInstruction.SetText(0, siemens ? "Assignment coil  —( )—" : "Output Energize (OTE)  —( )—");
        coilInstruction.SetMetadata(0, "coil");
        var setInstruction = instructionTree.CreateItem(bitLogic);
        setInstruction.SetText(0, siemens ? "Set coil  —(S)—" : "Output Latch (OTL)  —(L)—");
        setInstruction.SetMetadata(0, "set");
        var resetInstruction = instructionTree.CreateItem(bitLogic);
        resetInstruction.SetText(0, siemens ? "Reset coil  —(R)—" : "Output Unlatch (OTU)  —(U)—");
        resetInstruction.SetMetadata(0, "reset");
        var branchInstruction = instructionTree.CreateItem(bitLogic);
        branchInstruction.SetText(0, "Open parallel branch");
        branchInstruction.SetMetadata(0, "branch");
        var timerOperations = instructionTree.CreateItem(instructionRoot);
        timerOperations.SetText(0, "Timer operations");
        var tonInstruction = instructionTree.CreateItem(timerOperations);
        tonInstruction.SetText(0, siemens ? "TON · On-delay timer (IEC)" : "TON · Timer On Delay");
        tonInstruction.SetMetadata(0, "ton");
        var tofInstruction = instructionTree.CreateItem(timerOperations);
        tofInstruction.SetText(0, siemens ? "TOF · Off-delay timer (IEC)" : "TOF · Timer Off Delay");
        tofInstruction.SetMetadata(0, "tof");
        var tpInstruction = instructionTree.CreateItem(timerOperations);
        tpInstruction.SetText(0, siemens ? "TP · Pulse timer (IEC)" : "TP · Simulator Pulse Timer");
        tpInstruction.SetMetadata(0, "tp");
        var rtoInstruction = instructionTree.CreateItem(timerOperations);
        rtoInstruction.SetText(0, siemens ? "TONR · Time accumulator" : "RTO · Retentive Timer On");
        rtoInstruction.SetMetadata(0, "rto");
        var timerResetInstruction = instructionTree.CreateItem(timerOperations);
        timerResetInstruction.SetText(0, siemens ? "RT · Reset timer" : "RES · Reset Timer");
        timerResetInstruction.SetMetadata(0, "timer-reset");
        var counterOperations = instructionTree.CreateItem(instructionRoot);
        counterOperations.SetText(0, "Counter operations");
        var ctuInstruction = instructionTree.CreateItem(counterOperations);
        ctuInstruction.SetText(0, "CTU · Count Up");
        ctuInstruction.SetMetadata(0, "ctu");
        var ctdInstruction = instructionTree.CreateItem(counterOperations);
        ctdInstruction.SetText(0, "CTD · Count Down");
        ctdInstruction.SetMetadata(0, "ctd");
        var localOperations = siemens ? instructionTree.CreateItem(instructionRoot) : null;
        if (localOperations is not null) localOperations.SetText(0, "RungProof local (not Siemens)");
        var loadCounterInstruction = instructionTree.CreateItem(localOperations ?? counterOperations);
        loadCounterInstruction.SetText(0, siemens ? "RungProof local · Load counter preset" : "LOAD · Initialize counter ACC");
        loadCounterInstruction.SetMetadata(0, "counter-load");
        var resInstruction = instructionTree.CreateItem(localOperations ?? counterOperations);
        resInstruction.SetText(0, siemens ? "RungProof local · Reset counter" : "RES · Reset Counter");
        resInstruction.SetMetadata(0, "counter-reset");
        var compareOperations = instructionTree.CreateItem(instructionRoot);
        compareOperations.SetText(0, "Compare operations");
        var compareNames = new[]
        {
            ("Equal (EQ)", "compare-0"),
            ("Not equal (NE)", "compare-1"),
            ("Greater than (GT)", "compare-2"),
            ("Greater or equal (GE)", "compare-3"),
            ("Less than (LT)", "compare-4"),
            ("Less or equal (LE)", "compare-5"),
        };
        foreach (var item in compareNames)
        {
            var comparisonItem = instructionTree.CreateItem(compareOperations);
            comparisonItem.SetText(0, item.Item1);
            comparisonItem.SetMetadata(0, item.Item2);
        }
        if (siemens)
        {
            var codeMatchItem = instructionTree.CreateItem(localOperations);
            codeMatchItem.SetText(0, "CODE MATCH · local text instruction");
            codeMatchItem.SetMetadata(0, "compare-6");
        }
        else
        {
            var codeMatchItem = instructionTree.CreateItem(compareOperations);
            codeMatchItem.SetText(0, "CODE MATCH · local text instruction");
            codeMatchItem.SetMetadata(0, "compare-6");
        }
        var mathOperations = instructionTree.CreateItem(instructionRoot);
        mathOperations.SetText(0, "Move and math operations");
        foreach (var item in new[]
                 {
                     ("Move (MOV)", "numeric-0"),
                     ("Add (ADD)", "numeric-1"),
                     ("Subtract (SUB)", "numeric-2"),
                     ("Multiply (MUL)", "numeric-3"),
                     ("Divide (DIV)", "numeric-4"),
                     ("Modulo / remainder (MOD)", "numeric-5"),
                     ("Absolute value (ABS)", "numeric-6"),
                     ("Negate (NEG)", "numeric-7"),
                     ("Square root (SQRT)", "numeric-8"),
                     (siemens ? "Exponentiate (EXPT)" : "Exponentiate (EXPT; XPY before v36)", "numeric-9"),
                     ("Natural logarithm (LN)", "numeric-10"),
                     ("Sine (SIN)", "numeric-11"),
                     ("Cosine (COS)", "numeric-12"),
                     ("Tangent (TAN)", "numeric-13"),
                     (siemens ? "Arc sine (ASIN)" : "Arc sine (ASIN; ASN before v36)", "numeric-14"),
                     (siemens ? "Arc cosine (ACOS)" : "Arc cosine (ACOS; ACS before v36)", "numeric-15"),
                     (siemens ? "Arc tangent (ATAN)" : "Arc tangent (ATAN; ATN before v36)", "numeric-16"),
                     (siemens ? "Truncate (TRUNC)" : "Truncate (TRUNC; TRN before v36)", "numeric-17"),
                     (siemens ? "Normalize (NORM_X)" : "Normalize expression (CPT macro)", "numeric-18"),
                     (siemens ? "Scale (SCALE_X)" : "Scale expression (CPT macro)", "numeric-19"),
                     (siemens ? "Convert (CONVERT)" : "Destination-type conversion (MOV)", "numeric-20"),
                     (siemens ? "Round nearest-even (ROUND)" : "Round expression (CPT macro)", "numeric-21"),
                     (siemens ? "Ceiling (CEIL)" : "Ceiling expression (CPT macro)", "numeric-22"),
                     (siemens ? "Floor (FLOOR)" : "Floor expression (CPT macro)", "numeric-23"),
                 })
        {
            var numericItem = instructionTree.CreateItem(mathOperations);
            numericItem.SetText(0, item.Item1);
            numericItem.SetMetadata(0, item.Item2);
        }
        var formatTextItem = instructionTree.CreateItem(localOperations ?? mathOperations);
        formatTextItem.SetText(0, "FORMAT_TEXT · local text instruction");
        formatTextItem.SetMetadata(0, "numeric-24");
        var programControl = instructionTree.CreateItem(instructionRoot);
        programControl.SetText(0, "Program control");
        var rungInstruction = instructionTree.CreateItem(programControl);
        rungInstruction.SetText(0, siemens ? "Insert network" : "Insert rung");
        rungInstruction.SetMetadata(0, "rung");
        var callInstruction = instructionTree.CreateItem(programControl);
        callInstruction.SetText(0, siemens ? "FC / FB block call" : "Jump to Subroutine (JSR)");
        callInstruction.SetMetadata(0, "call");
        var returnInstruction = instructionTree.CreateItem(programControl);
        returnInstruction.SetText(0, siemens ? "RETURN from block" : "Return from Subroutine (RET)");
        returnInstruction.SetMetadata(0, "return");
        var jumpInstruction = instructionTree.CreateItem(programControl);
        jumpInstruction.SetText(0, "Jump to block-local label (JMP)");
        jumpInstruction.SetMetadata(0, "jump");
        var labelInstruction = instructionTree.CreateItem(programControl);
        labelInstruction.SetText(0, siemens ? "Block-local destination (LABEL)" : "Block-local destination (LBL)");
        labelInstruction.SetMetadata(0, "label");
        static void AddInstructionTooltips(TreeItem item)
        {
            item.SetTooltipText(0, item.GetText(0));
            for (var child = item.GetFirstChild(); child is not null; child = child.GetNext())
                AddInstructionTooltips(child);
        }
        // Keep the palette useful at first open: show the category list, but
        // do not expand every instruction family into a wall of entries.
        instructionRoot.SetCollapsed(false);
        foreach (var category in new[]
                 { bitLogic, timerOperations, counterOperations, compareOperations, mathOperations, programControl })
            category.SetCollapsed(true);
        localOperations?.SetCollapsed(true);
        AddInstructionTooltips(instructionRoot);
        instructionPage.AddChild(instructionTree);
        instructionTree.GuiInput += input =>
        {
            if (input is not InputEventMouseMotion || !Input.IsMouseButtonPressed(MouseButton.Left)) return;
            var kind = instructionTree.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            if (kind.Length == 0) return;
            _draggingInstruction = true;
            _draggedInstructionKind = kind;
            _draggedInstructionCanvas = canvas;
            instructionTree.AcceptEvent();
        };
        toolTabs.AddChild(instructionPage);

        var tagPage = new VBoxContainer
        {
            Name = "TagContent",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        tagPage.AddChild(tagList);
        var xrefSelectedTag = ToolbarButton("CrossReferenceSelectedTag", siemens ? "XREF SELECTED TAG" : "FIND ALL FOR SELECTED TAG", accent, 200);
        tagPage.AddChild(xrefSelectedTag);
        var tagTableActions = new HBoxContainer { Name = "TagTableActions" };
        tagTableActions.AddThemeConstantOverride("separation", 5);
        var tagTableHeading = Heading("TAG TABLE", 11, new Color("344851"));
        tagTableHeading.Name = "TagTableContextHeading";
        tagTableContextHeading = tagTableHeading;
        tagTableHeading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        tagTableActions.AddChild(tagTableHeading);
        var renameTagTable = ToolbarButton("RenameTagTable", "RENAME TABLE", new Color("4d6674"), 130);
        renameTagTableAction = renameTagTable;
        renameTagTable.Disabled = true;
        tagTableActions.AddChild(renameTagTable);
        var removeTagTable = ToolbarButton("RemoveEmptyTagTable", "DELETE EMPTY TABLE", new Color("8a4d55"), 164);
        removeTagTableAction = removeTagTable;
        removeTagTable.Disabled = true;
        tagTableActions.AddChild(removeTagTable);
        tagTableActions.Visible = siemens;
        tagPage.AddChild(tagTableActions);
        HBoxContainer TagFieldRow(string name, string label, Control field)
        {
            var row = new HBoxContainer { Name = name };
            row.AddThemeConstantOverride("separation", 5);
            var caption = Heading(label, 10, new Color("344851"));
            caption.CustomMinimumSize = new Vector2(72, 0);
            row.AddChild(caption);
            field.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(field);
            return row;
        }
        tagPage.AddChild(TagFieldRow("TagNameRow", "NAME", tagName));
        var tagTableMembershipRow = TagFieldRow("TagTableMembershipRow", "TAG TABLE", tagTableMembership);
        tagTableMembershipRow.Visible = siemens;
        tagPage.AddChild(tagTableMembershipRow);
        tagPage.AddChild(TagFieldRow("TagAddressRow", "ADDRESS", tagAddress));
        tagPage.AddChild(tagAddressValidation);
        tagPage.AddChild(TagFieldRow("TagCommentRow", "COMMENT", tagComment));
        tagPage.AddChild(TagFieldRow("TagTypeRow", "TYPE", tagType));
        tagPage.AddChild(TagFieldRow("TagRoleRow", "ROLE", tagRole));
        tagPage.AddChild(TagFieldRow("TagInitialValueRow", "INITIAL", tagInitialValue));
        var tagEnumMembersRow = TagFieldRow("TagEnumMembersRow", "MEMBERS", tagEnumMembers);
        tagEnumMembersRow.Visible = false;
        tagPage.AddChild(tagEnumMembersRow);
        tagPage.AddChild(aggregateTagEditor);
        tagPage.AddChild(TagFieldRow("TagBindingRow", "BINDING", tagBinding));
        tagPage.AddChild(tagBindingStatus);
        var addTag = ToolbarButton("AddTag", "+ ADD TAG", accent, 180);
        tagPage.AddChild(addTag);
        var tagEditActions = new HFlowContainer { Name = "TagEditActions" };
        tagEditActions.AddThemeConstantOverride("h_separation", 5);
        tagEditActions.AddThemeConstantOverride("v_separation", 5);
        applyTagEdit = ToolbarButton("ApplyTagEdit", "APPLY TAG CHANGES", new Color("4d6674"), 165);
        var cancelTagEdit = ToolbarButton("CancelTagEdit", "CANCEL", new Color("4d6674"), 90);
        deleteTag = ToolbarButton("DeleteTag", "DELETE UNUSED TAG", new Color("8a4d55"), 155);
        applyTagEdit.Disabled = true;
        deleteTag.Disabled = true;
        tagEditActions.AddChild(applyTagEdit);
        tagEditActions.AddChild(cancelTagEdit);
        tagEditActions.AddChild(deleteTag);
        tagPage.AddChild(tagEditActions);
        // TIA tag documents expose Tags, User constants, and System constants
        // as separate views. Keep the existing tag form intact as the Tags view.
        var tagDefinitionsView = new VBoxContainer { Name = "TagDefinitionsView", SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        foreach (var existingChild in tagPage.GetChildren().ToArray())
        {
            tagPage.RemoveChild(existingChild);
            tagDefinitionsView.AddChild(existingChild);
        }
        var tagDeclarationTabs = new HBoxContainer { Name = "TagDeclarationTabs" };
        tagDeclarationTabs.AddThemeConstantOverride("separation", 5);
        var tagDefinitionsTab = ToolbarButton("TagDefinitionsTab", "TAGS", new Color("4d6674"), 90);
        var userConstantsTab = ToolbarButton("UserConstantsTab", "USER CONSTANTS", new Color("4d6674"), 150);
        var systemConstantsTab = ToolbarButton("SystemConstantsTab", "SYSTEM CONSTANTS", new Color("4d6674"), 165);
        tagDefinitionsTab.ToggleMode = userConstantsTab.ToggleMode = systemConstantsTab.ToggleMode = true;
        tagDefinitionsTab.ButtonPressed = true;
        tagDeclarationTabs.AddChild(tagDefinitionsTab);
        tagDeclarationTabs.AddChild(userConstantsTab);
        tagDeclarationTabs.AddChild(systemConstantsTab);
        tagPage.AddChild(tagDeclarationTabs);
        tagPage.AddChild(tagDefinitionsView);

        var userConstantsView = new VBoxContainer
        {
            Name = "UserConstantsView",
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        var userConstantsGrid = new Tree
        {
            Name = "UserConstantsGrid",
            Columns = 4,
            HideRoot = true,
            ColumnTitlesVisible = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 180),
        };
        userConstantsGrid.SetColumnTitle(0, "NAME");
        userConstantsGrid.SetColumnTitle(1, "DATA TYPE");
        userConstantsGrid.SetColumnTitle(2, "VALUE");
        userConstantsGrid.SetColumnTitle(3, "COMMENT");
        var userConstantsActions = new HBoxContainer { Name = "UserConstantsActions" };
        var addUserConstant = ToolbarButton("AddUserConstant", "+ CONSTANT", accent, 130);
        var editUserConstant = ToolbarButton("EditUserConstant", "EDIT", new Color("4d6674"), 80);
        var removeUserConstant = ToolbarButton("RemoveUserConstant", "DELETE", new Color("8a4d55"), 90);
        editUserConstant.Disabled = true;
        removeUserConstant.Disabled = true;
        userConstantsActions.AddChild(addUserConstant);
        userConstantsActions.AddChild(editUserConstant);
        userConstantsActions.AddChild(removeUserConstant);
        var userConstantName = new LineEdit { Name = "UserConstantName", PlaceholderText = "Symbolic constant name", ClearButtonEnabled = true };
        var userConstantTable = new OptionButton { Name = "UserConstantTable" };
        var userConstantType = new OptionButton { Name = "UserConstantType" };
        foreach (var (type, label) in new[]
                 {
                     (PlcVariableType.Bool, "Bool"), (PlcVariableType.Int, "Int"), (PlcVariableType.DInt, "DInt"),
                     (PlcVariableType.Real, "Real"), (PlcVariableType.Time, "Time"), (PlcVariableType.String, "String"),
                 })
        {
            userConstantType.AddItem(label);
            userConstantType.SetItemMetadata(userConstantType.ItemCount - 1, (int)type);
        }
        var userConstantValue = new LineEdit { Name = "UserConstantValue", PlaceholderText = "Fixed value", ClearButtonEnabled = true };
        var userConstantComment = new LineEdit { Name = "UserConstantComment", PlaceholderText = "Comment", ClearButtonEnabled = true };
        var userConstantForm = new VBoxContainer { Name = "UserConstantForm", Visible = false };
        var userConstantValidation = new Label
        {
            Name = "UserConstantValidationMessage",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Visible = false,
        };
        userConstantValidation.AddThemeColorOverride("font_color", new Color("d64545"));
        userConstantForm.AddChild(TagFieldRow("UserConstantNameRow", "NAME", userConstantName));
        userConstantForm.AddChild(TagFieldRow("UserConstantTableRow", "TAG TABLE", userConstantTable));
        userConstantForm.AddChild(TagFieldRow("UserConstantTypeRow", "DATA TYPE", userConstantType));
        userConstantForm.AddChild(TagFieldRow("UserConstantValueRow", "VALUE", userConstantValue));
        userConstantForm.AddChild(TagFieldRow("UserConstantCommentRow", "COMMENT", userConstantComment));
        userConstantForm.AddChild(userConstantValidation);
        var userConstantEditActions = new HBoxContainer { Name = "UserConstantEditActions" };
        var applyUserConstant = ToolbarButton("ApplyUserConstant", "APPLY", accent, 95);
        var cancelUserConstant = ToolbarButton("CancelUserConstant", "CANCEL", new Color("4d6674"), 95);
        applyUserConstant.Disabled = true;
        cancelUserConstant.Disabled = true;
        userConstantEditActions.AddChild(applyUserConstant);
        userConstantEditActions.AddChild(cancelUserConstant);
        var userConstantScope = new Label
        {
            Name = "UserConstantScope",
            Text = "User constants are offline declarations. Supported types: BOOL, INT, DINT, REAL, TIME, STRING.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        userConstantScope.AddThemeColorOverride("font_color", new Color("637780"));
        userConstantsView.AddChild(userConstantsGrid);
        userConstantsView.AddChild(userConstantsActions);
        userConstantsView.AddChild(userConstantForm);
        userConstantsView.AddChild(userConstantEditActions);
        userConstantsView.AddChild(userConstantScope);
        tagPage.AddChild(userConstantsView);

        var systemConstantsView = new VBoxContainer
        {
            Name = "SystemConstantsView",
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        var systemConstantsGrid = new Tree
        {
            Name = "SystemConstantsGrid",
            Columns = 3,
            HideRoot = true,
            ColumnTitlesVisible = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        systemConstantsGrid.SetColumnTitle(0, "NAME");
        systemConstantsGrid.SetColumnTitle(1, "VALUE");
        systemConstantsGrid.SetColumnTitle(2, "COMMENT");
        var systemConstantsScope = new Label
        {
            Name = "SystemConstantsScope",
            Text = "No hardware configuration is modeled in this offline project. TIA system constants are generated from configured hardware and are read-only.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        systemConstantsScope.AddThemeColorOverride("font_color", new Color("637780"));
        systemConstantsView.AddChild(systemConstantsGrid);
        systemConstantsView.AddChild(systemConstantsScope);
        tagPage.AddChild(systemConstantsView);
        var userConstantFormEditing = false;
        var refreshingUserConstantsGrid = false;
        void SetTagDeclarationView(int index)
        {
            ResolvePendingEditorForms(() =>
            {
                if (index != 1 && userConstantFormEditing && !userConstantFormDirty)
                    discardUserConstantForm?.Invoke();
                tagDefinitionsView.Visible = index == 0;
                userConstantsView.Visible = index == 1;
                systemConstantsView.Visible = index == 2;
                tagDefinitionsTab.ButtonPressed = index == 0;
                userConstantsTab.ButtonPressed = index == 1;
                systemConstantsTab.ButtonPressed = index == 2;
                if (index == 1) refreshUserConstants?.Invoke();
            });
        }
        void RefreshUserConstantTableOptions()
        {
            var currentTableNames = document.TagTables.ToArray();
            var optionsMatch = userConstantTable.ItemCount == currentTableNames.Length
                && Enumerable.Range(0, userConstantTable.ItemCount)
                    .Select(userConstantTable.GetItemText)
                    .SequenceEqual(currentTableNames, StringComparer.Ordinal);
            if (optionsMatch) return;

            var selectedConstant = selectedUserConstantId is null
                ? null
                : document.UserConstants.FirstOrDefault(item => item.Id == selectedUserConstantId);
            var selectedName = selectedConstant?.TagTable;
            if (selectedName is null && userConstantTable.Selected >= 0)
            {
                var previousName = userConstantTable.GetItemText(userConstantTable.Selected);
                if (currentTableNames.Contains(previousName, StringComparer.OrdinalIgnoreCase))
                    selectedName = previousName;
            }
            if (selectedName is null && activeTagTableName is not null
                && currentTableNames.Contains(activeTagTableName, StringComparer.OrdinalIgnoreCase))
                selectedName = activeTagTableName;
            selectedName ??= currentTableNames.FirstOrDefault(name =>
                name.Equals("Default tag table", StringComparison.OrdinalIgnoreCase));

            var priorLoading = loadingFormControls;
            loadingFormControls = true;
            try
            {
                userConstantTable.Clear();
                foreach (var tableName in currentTableNames) userConstantTable.AddItem(tableName);
                var selectedIndex = Array.FindIndex(currentTableNames, name =>
                    name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
                if (selectedIndex >= 0) userConstantTable.Select(selectedIndex);
            }
            finally { loadingFormControls = priorLoading; }
        }
        refreshUserConstants = () =>
        {
            // Tag-table renames update constant membership in the model. Keep
            // an open form's selector aligned with those canonical names.
            RefreshUserConstantTableOptions();
            var visibleConstants = document.UserConstants
                .Where(constant => activeTagTableName is null
                    || constant.TagTable.Equals(activeTagTableName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            refreshingUserConstantsGrid = true;
            try
            {
            userConstantsGrid.Clear();
            var rootItem = userConstantsGrid.CreateItem();
            foreach (var constant in visibleConstants)
            {
                var row = userConstantsGrid.CreateItem(rootItem);
                row.SetText(0, constant.Name);
                row.SetText(1, constant.Type.ToString().ToUpperInvariant());
                row.SetText(2, constant.Value);
                row.SetText(3, constant.Comment);
                row.SetMetadata(0, constant.Id);
                row.SetTooltipText(0, $"{constant.Name} · {constant.Type} · {constant.Value}\n{constant.Comment}");
                if (constant.Id == selectedUserConstantId) row.Select(0);
            }
            rootItem.Collapsed = false;
            }
            finally { refreshingUserConstantsGrid = false; }
            var hasSelection = selectedUserConstantId is not null
                && visibleConstants.Any(constant => constant.Id == selectedUserConstantId);
            editUserConstant.Disabled = !hasSelection || ChallengeMutationLocked || userConstantFormDirty;
            removeUserConstant.Disabled = !hasSelection || ChallengeMutationLocked || userConstantFormDirty;
            addUserConstant.Disabled = ChallengeMutationLocked || userConstantFormDirty;
            applyUserConstant.Disabled = !userConstantFormEditing || !userConstantFormDirty || ChallengeMutationLocked;
            cancelUserConstant.Disabled = !userConstantFormEditing;
            systemConstantsTab.Disabled = activeTagTableName is not null
                && !activeTagTableName.Equals("Default tag table", StringComparison.OrdinalIgnoreCase);
        };
        loadUserConstantForm = () =>
        {
            var constant = selectedUserConstantId is null
                ? null
                : document.UserConstants.FirstOrDefault(item => item.Id == selectedUserConstantId);
            var priorLoading = loadingFormControls;
            loadingFormControls = true;
            try
            {
                userConstantName.Text = constant?.Name ?? string.Empty;
                var currentTableNames = document.TagTables.ToArray();
                userConstantTable.Clear();
                foreach (var tableName in currentTableNames) userConstantTable.AddItem(tableName);
                var selectedTableName = constant?.TagTable ?? activeTagTableName ?? "Default tag table";
                var selectedTableIndex = Array.FindIndex(currentTableNames, tableName =>
                    tableName.Equals(selectedTableName, StringComparison.OrdinalIgnoreCase));
                userConstantTable.Select(Math.Max(0, selectedTableIndex));
                var selectedType = constant?.Type ?? PlcVariableType.Bool;
                var typeIndex = Enumerable.Range(0, userConstantType.ItemCount)
                    .FirstOrDefault(index => userConstantType.GetItemMetadata(index).AsInt32() == (int)selectedType, -1);
                userConstantType.Select(Math.Max(0, typeIndex));
                userConstantValue.Text = constant?.Value ?? "FALSE";
                userConstantComment.Text = constant?.Comment ?? string.Empty;
                userConstantValidation.Text = string.Empty;
                userConstantValidation.Visible = false;
                userConstantForm.Visible = userConstantFormEditing;
                userConstantFormDirty = false;
                userConstantFormRevision = document.Revision;
            }
            finally { loadingFormControls = priorLoading; }
            refreshUserConstants?.Invoke();
        };
        discardUserConstantForm = () =>
        {
            userConstantFormDirty = false;
            userConstantFormEditing = false;
            userConstantValidation.Text = string.Empty;
            userConstantValidation.Visible = false;
            // Keep an existing row selected after Cancel so Edit/Delete remain
            // enabled without requiring an artificial second row-selection.
            // New drafts already have a null ID and remain unselected.
            userConstantForm.Visible = false;
            refreshUserConstants?.Invoke();
        };
        tagDefinitionsTab.Pressed += () => SetTagDeclarationView(0);
        userConstantsTab.Pressed += () => SetTagDeclarationView(1);
        systemConstantsTab.Pressed += () =>
        {
            if (!systemConstantsTab.Disabled) SetTagDeclarationView(2);
        };
        userConstantsGrid.ItemSelected += () =>
        {
            if (refreshingUserConstantsGrid) return;
            var row = userConstantsGrid.GetSelected();
            if (row is null) return;
            var id = row.GetMetadata(0).AsString();
            ResolvePendingEditorForms(() =>
            {
                selectedUserConstantId = id;
                userConstantFormEditing = false;
                userConstantForm.Visible = false;
                refreshUserConstants?.Invoke();
            });
        };
        addUserConstant.Pressed += () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            selectedUserConstantId = null;
            userConstantFormEditing = true;
            loadUserConstantForm?.Invoke();
            userConstantName.GrabFocus();
        };
        editUserConstant.Pressed += () =>
        {
            var row = userConstantsGrid.GetSelected();
            if (row is null) return;
            selectedUserConstantId = row.GetMetadata(0).AsString();
            userConstantFormEditing = true;
            loadUserConstantForm?.Invoke();
            userConstantName.GrabFocus();
        };
        removeUserConstant.Pressed += () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            if (selectedUserConstantId is null) return;
            var constant = document.UserConstants.FirstOrDefault(item => item.Id == selectedUserConstantId);
            if (constant is null || !BeginProjectMetadataEdit("Delete PLC user constant")) return;
            document.RemoveUserConstant(constant.Id);
            selectedUserConstantId = null;
            userConstantFormEditing = false;
            userConstantFormDirty = false;
            userConstantForm.Visible = false;
            refreshEditorAfterInitialization?.Invoke();
            output.Text = $"[color=#18864b]PLC user constant deleted.[/color] {Escape(constant.Name)}.";
        };
        applyUserConstant.Pressed += () =>
        {
            if (userConstantFormDirty && !CurrentForm(userConstantFormRevision)) return;
            if (!userConstantFormEditing) return;
            userConstantValidation.Text = string.Empty;
            userConstantValidation.Visible = false;
            var name = userConstantName.Text.Trim();
            if (name.Length == 0)
            {
                userConstantValidation.Text = "Enter a symbolic name.";
                userConstantValidation.Visible = true;
                output.Text = "[color=#d64545]User constant rejected.[/color] Enter a symbolic name.";
                userConstantName.GrabFocus();
                return;
            }
            if (userConstantType.Selected < 0) return;
            if (userConstantTable.Selected < 0) return;
            var type = (PlcVariableType)userConstantType.GetItemMetadata(userConstantType.Selected).AsInt32();
            var tableName = userConstantTable.GetItemText(userConstantTable.Selected);
            try
            {
                var preview = new LadderEditorDocument();
                preview.RestoreSnapshot(document.CaptureSnapshot());
                if (selectedUserConstantId is null)
                    preview.AddUserConstant(name, type, userConstantValue.Text, tableName, userConstantComment.Text);
                else
                    preview.UpdateUserConstant(selectedUserConstantId, name, type, userConstantValue.Text, userConstantComment.Text, tableName);
                if (!BeginProjectMetadataEdit(selectedUserConstantId is null ? "Add PLC user constant" : "Update PLC user constant")) return;
                var updated = selectedUserConstantId is null
                    ? document.AddUserConstant(name, type, userConstantValue.Text, tableName, userConstantComment.Text)
                    : document.UpdateUserConstant(selectedUserConstantId, name, type, userConstantValue.Text, userConstantComment.Text, tableName);
                selectedUserConstantId = updated.Id;
                userConstantFormDirty = false;
                userConstantFormEditing = false;
                userConstantForm.Visible = false;
                refreshEditorAfterInitialization?.Invoke();
                output.Text = $"[color=#18864b]PLC user constant saved.[/color] {Escape(updated.Name)} · {updated.Type} · offline declaration.";
            }
            catch (InvalidOperationException error)
            {
                userConstantValidation.Text = error.Message;
                userConstantValidation.Visible = true;
                output.Text = $"[color=#d64545]User constant rejected.[/color] {Escape(error.Message)}";
            }
        };
        cancelUserConstant.Pressed += () =>
        {
            discardUserConstantForm?.Invoke();
            output.Text = "PLC user-constant changes discarded. No project data or history was changed.";
        };
        foreach (var control in new Control[] { userConstantName, userConstantTable, userConstantType, userConstantValue, userConstantComment })
        {
            void MarkUserConstantPending()
            {
                if (refreshingEditor || loadingFormControls || !userConstantFormEditing) return;
                userConstantValidation.Text = string.Empty;
                userConstantValidation.Visible = false;
                if (!userConstantFormDirty) userConstantFormRevision = document.Revision;
                userConstantFormDirty = true;
                refreshUserConstants?.Invoke();
            }
            if (control is LineEdit text) text.TextChanged += _ => MarkUserConstantPending();
            else if (control is OptionButton option) option.ItemSelected += _ => MarkUserConstantPending();
            control.GuiInput += input =>
            {
                if (input is InputEventKey { Pressed: true, Keycode: Key.Escape })
                {
                    discardUserConstantForm?.Invoke();
                    control.AcceptEvent();
                }
            };
        }
        userConstantName.TextSubmitted += _ => applyUserConstant.EmitSignal(BaseButton.SignalName.Pressed);
        userConstantValue.TextSubmitted += _ => applyUserConstant.EmitSignal(BaseButton.SignalName.Pressed);
        var tagScroll = new ScrollContainer
        {
            Name = siemens ? "PLC tags" : "Controller Tags",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        tagScroll.AddChild(tagPage);
        if (siemens)
        {
            tagScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            tagScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
            tagScroll.Visible = false;
            center.AddChild(tagScroll);
            var hiddenTagDock = new Control { Name = "PLC tags", Visible = false };
            projectTabs.AddChild(hiddenTagDock);
        }
        else projectTabs.AddChild(tagScroll);
        var instanceDbDocument = new VBoxContainer
        {
            Name = "InstanceDbDocument",
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 260),
        };
        instanceDbDocument.AddThemeConstantOverride("separation", 8);
        var instanceDbHeading = new Label { Name = "InstanceDbHeading", Text = "Instance data block", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        instanceDbHeading.AddThemeColorOverride("font_color", new Color("dce8ed"));
        instanceDbHeading.AddThemeFontSizeOverride("font_size", 20);
        instanceDbDocument.AddChild(instanceDbHeading);
        var instanceDbSubtitle = new Label { Name = "InstanceDbSubtitle", Text = "Generated from the assigned FB or IEC instruction · offline definition" };
        instanceDbSubtitle.AddThemeColorOverride("font_color", new Color("aebcc2"));
        instanceDbDocument.AddChild(instanceDbSubtitle);
        var instanceDbDeclarations = new Tree
        {
            Name = "InstanceDbDeclarations",
            Columns = 6,
            HideRoot = true,
            ColumnTitlesVisible = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 220),
        };
        instanceDbDeclarations.SetColumnTitle(0, "NAME");
        instanceDbDeclarations.SetColumnTitle(1, "DATA TYPE");
        instanceDbDeclarations.SetColumnTitle(2, "DEFAULT VALUE");
        instanceDbDeclarations.SetColumnTitle(3, "START VALUE");
        instanceDbDeclarations.SetColumnTitle(4, "RETAIN");
        instanceDbDeclarations.SetColumnTitle(5, "COMMENT");
        void ApplyInstanceDbColumnLayout()
        {
            // The expanded project tree leaves less room for the declaration
            // grid on a 1000 px workstation. Compact the six columns only when
            // that pane is narrow; the Comment column takes remaining space.
            // Wider panes keep readable name/comment widths without a scrollbar.
            var compact = instanceDbDeclarations.Size.X < 720;
            var minimumWidths = compact
                ? new[] { 96, 64, 78, 78, 54, 78 }
                : new[] { 155, 80, 100, 100, 65, 120 };
            for (var column = 0; column < minimumWidths.Length; column++)
            {
                instanceDbDeclarations.SetColumnExpand(column, column == 5);
                instanceDbDeclarations.SetColumnCustomMinimumWidth(column, minimumWidths[column]);
            }
            instanceDbDeclarations.AddThemeFontSizeOverride("font_size", compact ? 10 : 12);
        }
        var instanceDbLayoutUpdateQueued = false;
        void QueueInstanceDbColumnLayout()
        {
            if (instanceDbLayoutUpdateQueued) return;
            instanceDbLayoutUpdateQueued = true;
            // The root/splitter resize notifications can precede the child
            // layout pass. Apply after two deferred frames so the decision
            // uses the final declaration-grid width, including when the grid
            // was hidden during a window resize.
            Callable.From(() => Callable.From(() =>
            {
                instanceDbLayoutUpdateQueued = false;
                if (GodotObject.IsInstanceValid(instanceDbDeclarations))
                    ApplyInstanceDbColumnLayout();
            }).CallDeferred()).CallDeferred();
        }
        instanceDbDeclarations.Resized += QueueInstanceDbColumnLayout;
        root.Resized += QueueInstanceDbColumnLayout;
        instanceDbDeclarations.AddThemeColorOverride("font_color", new Color("263943"));
        instanceDbDeclarations.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        instanceDbDocument.AddChild(instanceDbDeclarations);
        Callable.From(ApplyInstanceDbColumnLayout).CallDeferred();
        var instanceDbBoundary = new Label
        {
            Name = "InstanceDbBoundary",
            Text = "Read-only offline instance view. No per-instance start/comment/retain overrides; power-cycle retention, standard-access offsets, monitor/snapshots, HMI attributes or supervision are not modeled.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        instanceDbBoundary.AddThemeColorOverride("font_color", new Color("aebcc2"));
        instanceDbDocument.AddChild(instanceDbBoundary);
        center.AddChild(instanceDbDocument);
        globalDbDocument = new VBoxContainer
        {
            Name = "GlobalDbDocument",
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        globalDbDocument.AddThemeConstantOverride("separation", 8);
        var globalDbEditorPreviousDockWidth = 0;
        var globalDbHeader = new HBoxContainer { Name = "GlobalDbHeader" };
        globalDbHeader.AddThemeConstantOverride("separation", 8);
        globalDbHeading = new RichTextLabel
        {
            Name = "GlobalDbHeading",
            BbcodeEnabled = true,
            FitContent = true,
            CustomMinimumSize = new Vector2(0, 62),
        };
        globalDbHeading.AddThemeColorOverride("default_color", new Color("dce8ed"));
        globalDbHeading.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        globalDbHeader.AddChild(globalDbHeading);
        var globalDbAddMember = ToolbarButton("GlobalDbAddMember", "+ MEMBER", new Color("1f8a58"), 104);
        globalDbAddMember.TooltipText = "Add a declaration to this Global DB.";
        var globalDbEditMember = ToolbarButton("GlobalDbEditMember", "EDIT", new Color("4d6674"), 68);
        globalDbEditMember.TooltipText = "Edit the selected Global DB declaration.";
        var globalDbRemoveMember = ToolbarButton("GlobalDbRemoveMember", "REMOVE", new Color("8a4d55"), 88);
        globalDbRemoveMember.TooltipText = "Remove the selected Global DB declaration.";
        globalDbHeader.AddChild(globalDbAddMember);
        globalDbHeader.AddChild(globalDbEditMember);
        globalDbHeader.AddChild(globalDbRemoveMember);
        var editGlobalDbMembers = ToolbarButton("EditGlobalDbMembers", "EDIT MEMBERS", new Color("4d6674"), 130);
        editGlobalDbMembers.TooltipText = "Open the project member editor or return to the project tree.";
        editGlobalDbMembers.Pressed += () =>
        {
            var showMemberEditor = projectTabs.CurrentTab != 2;
            // Capture the live splitter position before ShowProjectDock can
            // reopen a collapsed dock. The cached value is only authoritative
            // when the dock is hidden; while visible, the user may have dragged
            // the divider since the cache was last updated.
            var currentDockWidth = work.SplitOffsets.Length > 0
                ? work.SplitOffsets[0]
                : GetProjectDockSplitOffset();
            var dockWasVisible = projectPanel.Visible;
            if (showMemberEditor)
            {
                globalDbEditorPreviousDockWidth = dockWasVisible
                    ? currentDockWidth
                    : GetProjectDockSplitOffset();
            }

            ShowProjectDock();
            projectTabs.CurrentTab = showMemberEditor ? 2 : 0;
            if (showMemberEditor)
            {
                var preferredDockWidth = Math.Min(600, Math.Max(560, (int)root.Size.X / 3));
                // The splitter's combined minimum can be stale while the
                // Global DB document and object tab are changing visibility.
                // Request the preferred width, then read the constrained
                // project panel geometry after Godot lays out the splitter.
                SetProjectDockSplitOffset(preferredDockWidth);
            }
            else if (globalDbEditorPreviousDockWidth > 0)
            {
                SetProjectDockSplitOffset(globalDbEditorPreviousDockWidth);
                globalDbEditorPreviousDockWidth = 0;
            }
            work.SplitOffsets = [GetProjectDockSplitOffset()];
            if (showMemberEditor)
            {
                var requestedDockWidth = GetProjectDockSplitOffset();
                Callable.From(() => Callable.From(() =>
                {
                    if (!GodotObject.IsInstanceValid(work)
                        || !GodotObject.IsInstanceValid(projectPanel)
                        || !projectPanel.IsVisibleInTree())
                        return;

                    var layoutClampedWidth = (int)Math.Round(projectPanel.Size.X);
                    SetProjectDockSplitOffset(Math.Min(requestedDockWidth, layoutClampedWidth));
                    work.SplitOffsets = [GetProjectDockSplitOffset()];
                }).CallDeferred()).CallDeferred();
            }
            projectHeading.Text = showMemberEditor ? "Project objects" : "Project tree";
            editGlobalDbMembers.Text = showMemberEditor ? "PROJECT TREE" : "EDIT MEMBERS";
            output.Text = showMemberEditor
                ? "Global DB member editor active. Add, select, apply, and remove declarations in Project objects."
                : "Project tree active. Select a program block or data block.";
        };
        globalDbHeader.AddChild(editGlobalDbMembers);
        globalDbDocument.AddChild(globalDbHeader);
        globalDbDeclarations = new Tree
        {
            Name = "GlobalDbDeclarations",
            Columns = 5,
            HideRoot = true,
            ColumnTitlesVisible = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 220),
        };
        globalDbDeclarations.SetColumnTitle(0, "NAME");
        globalDbDeclarations.SetColumnTitle(1, "DATA TYPE");
        globalDbDeclarations.SetColumnTitle(2, "START VALUE");
        globalDbDeclarations.SetColumnTitle(3, "RETAIN");
        globalDbDeclarations.SetColumnTitle(4, "COMMENT");
        globalDbDeclarations.SetColumnExpand(0, true);
        globalDbDeclarations.SetColumnExpand(1, true);
        globalDbDeclarations.SetColumnExpand(2, true);
        globalDbDeclarations.SetColumnExpand(3, false);
        globalDbDeclarations.SetColumnExpand(4, true);
        globalDbDeclarations.SetColumnCustomMinimumWidth(0, 150);
        globalDbDeclarations.SetColumnCustomMinimumWidth(1, 145);
        globalDbDeclarations.SetColumnCustomMinimumWidth(2, 150);
        globalDbDeclarations.SetColumnCustomMinimumWidth(3, 92);
        globalDbDeclarations.SetColumnCustomMinimumWidth(4, 180);
        globalDbDeclarations.AddThemeColorOverride("font_color", new Color("263943"));
        globalDbDeclarations.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        globalDbDeclarations.ItemEdited += () =>
        {
            if (refreshingEditor || document.Blocks.Count == 0) return;
            var row = globalDbDeclarations.GetEdited();
            if (row is null) return;

            var ownerId = row.GetMetadata(2).AsString();
            var memberId = row.GetMetadata(0).AsString();
            var owner = document.Blocks.FirstOrDefault(block =>
                StringComparer.Ordinal.Equals(block.Id, ownerId) && block.BlockType == LadderBlockType.DataBlock);
            var memberName = row.GetMetadata(1).AsString();
            var memberIndex = owner?.Interface.FindIndex(parameter =>
                !string.IsNullOrWhiteSpace(memberId)
                    ? StringComparer.Ordinal.Equals(parameter.Id, memberId)
                    : StringComparer.Ordinal.Equals(parameter.Name, memberName)) ?? -1;
            if (owner is null || memberIndex < 0)
            {
                output.Text = "[color=#d64545]Global DB edit rejected.[/color] This declaration no longer exists. Refresh the block and try again.";
                RefreshEditor();
                return;
            }

            var existing = owner.Interface[memberIndex];
            var column = globalDbDeclarations.GetEditedColumn();
            var name = column == 0 ? row.GetText(0).Trim() : existing.Name;
            var type = existing.Type;
            if (column == 1 && (!Enum.TryParse(row.GetText(1).Trim(), true, out type)
                || !Enum.IsDefined(type) || type is PlcVariableType.Struct or PlcVariableType.Array or PlcVariableType.Time))
            {
                output.Text = "[color=#d64545]Global DB edit rejected.[/color] Use a supported scalar data type (BOOL, INT, DINT, REAL, TIMER, COUNTER, STRING, or ENUM).";
                RefreshEditor();
                return;
            }

            if (name.Length == 0 || owner.Interface.Where((_, index) => index != memberIndex)
                .Any(parameter => parameter.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                output.Text = "[color=#d64545]Global DB edit rejected.[/color] Declaration names must be non-empty and unique without regard to letter case.";
                RefreshEditor();
                return;
            }

            var typeChanged = column == 1 && type != existing.Type;
            var initialValue = existing.InitialValue;
            if (column == 2 || typeChanged)
            {
                var initialText = column == 2 ? row.GetText(2) : FormatTagInitialValue(new PlcVariable(
                    existing.Name, existing.Type, PlcVariableRole.Memory, existing.InitialValue));
                if (typeChanged && type is PlcVariableType.Timer or PlcVariableType.Counter)
                {
                    initialValue = type == PlcVariableType.Timer ? TimeSpan.Zero : 0L;
                }
                else
                {
                    if (typeChanged
                        && !LadderEditorDocument.TryParseInitialValue(type, initialText, out _, out _))
                    {
                        initialText = InterfaceInitialDefault(type switch
                        {
                            PlcVariableType.Int => 1,
                            PlcVariableType.DInt => 2,
                            PlcVariableType.Real => 3,
                            PlcVariableType.Timer => 4,
                            PlcVariableType.Counter => 5,
                            PlcVariableType.String => 6,
                            PlcVariableType.Enum => 7,
                            _ => 0,
                        });
                    }
                    if (!LadderEditorDocument.TryParseInitialValue(type, initialText, out initialValue, out var initialError))
                    {
                        output.Text = $"[color=#d64545]Global DB edit rejected.[/color] {Escape(initialError)}";
                        RefreshEditor();
                        return;
                    }
                }
            }
            var enumMembers = type == PlcVariableType.Enum ? existing.EnumMembers : null;
            if (type == PlcVariableType.Enum && existing.Type != PlcVariableType.Enum)
            {
                output.Text = "[color=#d64545]Global DB edit rejected.[/color] Create or edit the ENUM member domain in Edit Members before changing this declaration to ENUM.";
                RefreshEditor();
                return;
            }
            if (type == PlcVariableType.Enum
                && (!PlcTextValues.IsValidDomain(enumMembers)
                    || initialValue is not string enumValue
                    || !enumMembers!.Contains(enumValue, StringComparer.Ordinal)))
            {
                output.Text = "[color=#d64545]Global DB edit rejected.[/color] ENUM declarations need valid members; use Edit Members for the enum domain.";
                RefreshEditor();
                return;
            }

            var updated = existing with
            {
                Name = name,
                Type = type,
                InitialValue = initialValue,
                Retain = column == 3 ? row.IsChecked(3) : existing.Retain,
                Comment = column == 4 ? row.GetText(4) : existing.Comment,
            };
            if (!BeginEdit($"Edit Global DB {new[] { "name", "data type", "start value", "retentivity", "comment" }[Math.Clamp(column, 0, 4)]}"))
            {
                RefreshEditor();
                return;
            }
            owner.Interface[memberIndex] = updated;
            RefreshEditor();
            output.Text = $"[color=#18864b]Global DB declaration updated.[/color] {Escape(updated.Name)} · {updated.Type.ToString().ToUpperInvariant()}.";
        };
        globalDbDocument.AddChild(globalDbDeclarations);
        var globalDbBoundary = Inspector("GlobalDbBoundary");
        globalDbBoundary.FitContent = true;
        globalDbBoundary.AddThemeColorOverride("default_color", new Color("607983"));
        globalDbBoundary.Text = "Optimized-access offline subset: Name, Data type, Start value, Retain, Comment. TIA V21 supports additional configurable columns, including Default value, HMI visibility/access/write, Setpoint, and Supervision; Offset requires standard access. Monitor values require an online connection; creating a snapshot requires online access, though a saved snapshot can be viewed offline. These are omitted. Retain is metadata; power-cycle retention is not simulated. ARRAY/system-type DB and manual instance-DB creation are unsupported.";
        globalDbDocument.AddChild(globalDbBoundary);
        center.AddChild(globalDbDocument);
        void SetEditorDocumentBody(string key)
        {
            activeEditorDocumentKey = key;
            var showGlobalDb = false;
            if (siemens && TryResolveBlockDocument(key, out var dbBlockIndex))
                showGlobalDb = document.Blocks[dbBlockIndex].BlockType == LadderBlockType.DataBlock;
            var showWatchTable = siemens && key.StartsWith("watch-table:", StringComparison.Ordinal);
            var showBlockEditor = !siemens || TryResolveBlockDocument(key, out _) && !showGlobalDb;
            foreach (var child in center.GetChildren().OfType<Control>())
            {
                if (child == editorDocumentHost) child.Visible = true;
                else if (child == tagScroll) child.Visible = key.StartsWith("tags:", StringComparison.Ordinal);
                else if (child == instanceDbDocument) child.Visible = key.StartsWith("instance-db:", StringComparison.Ordinal);
                else if (child == globalDbDocument) child.Visible = showGlobalDb;
                else if (child == watchDocument) child.Visible = showWatchTable;
                else child.Visible = showBlockEditor;
            }
        }
        bool IsOpenEditorDocument(string candidate) => editorDocumentButtons.ContainsKey(candidate)
            && (TryResolveBlockDocument(candidate, out _)
                || editorTagDocuments.ContainsKey(candidate)
                || editorInstanceDbDocuments.TryGetValue(candidate, out var ownerLocator)
                    && ResolveInstanceDbOwner(ownerLocator) is not null
                || editorWatchTableDocuments.ContainsKey(candidate)
                    && EditorDocumentIdentity.TryResolveWatchTableIndex(document, candidate, out _));

        string? FindOpenEditorDocumentFallback(string excludedKey)
        {
            var orderedKeys = editorDocumentTabs.GetChildren()
                .OfType<HBoxContainer>()
                .Select(tabItem => tabItem.GetMeta("editorDocumentKey").AsString())
                .Where(candidate => !string.IsNullOrEmpty(candidate))
                .ToArray();
            var tabIndex = Array.IndexOf(orderedKeys, excludedKey);
            for (var index = Math.Min(tabIndex - 1, orderedKeys.Length - 1); index >= 0; index--)
                if (orderedKeys[index] != excludedKey && IsOpenEditorDocument(orderedKeys[index]))
                    return orderedKeys[index];
            for (var index = Math.Max(tabIndex + 1, 0); index < orderedKeys.Length; index++)
                if (orderedKeys[index] != excludedKey && IsOpenEditorDocument(orderedKeys[index]))
                    return orderedKeys[index];
            return null;
        }

        void CloseEditorDocument(string key)
        {
            if (!editorDocumentButtons.Remove(key, out _) || !editorDocumentItems.Remove(key, out var item)) return;
            var wasActive = StringComparer.Ordinal.Equals(activeEditorDocumentKey, key);
            editorTagDocuments.Remove(key);
            editorInstanceDbDocuments.Remove(key);
            editorWatchTableDocuments.Remove(key);
            editorDocumentTabs.RemoveChild(item);
            item.QueueFree();
            if (!wasActive) return;

            var fallbackKey = FindOpenEditorDocumentFallback(key);
            if (fallbackKey is not null) ActivateEditorDocument(fallbackKey);
            else SetEditorDocumentBody(string.Empty);
            output.Text = "Editor tab closed. The project object and its saved data are unchanged.";
        }
        restoreActiveEditorDocumentBody = () => SetEditorDocumentBody(activeEditorDocumentKey);
        string? FindInstanceDbOwnerLocator(int dbNumber)
        {
            var assignedInstance = document.Instances.FirstOrDefault(instance => instance.InstanceDataBlock?.Number == dbNumber);
            if (assignedInstance is not null) return $"fb:{assignedInstance.Id}";

            // Prefer the timer/counter instruction over its reset/load
            // companion so a stable instruction ID owns the editor tab.
            foreach (var block in document.Blocks)
            foreach (var rung in block.Rungs)
            {
                if (rung.IsTimer && rung.TimerInstanceDataBlock?.Number == dbNumber)
                    return $"timer:{rung.Id}";
                if (rung.IsCounter && rung.CounterInstanceDataBlock?.Number == dbNumber)
                    return $"counter:{rung.Id}";
            }
            foreach (var block in document.Blocks)
            foreach (var rung in block.Rungs)
            {
                if ((rung.IsTimerReset || rung.IsTimer) && rung.TimerInstanceDataBlock?.Number == dbNumber)
                    return $"timer:{rung.Id}";
                if ((rung.IsCounterReset || rung.IsCounterLoad || rung.IsCounter) && rung.CounterInstanceDataBlock?.Number == dbNumber)
                    return $"counter:{rung.Id}";
            }
            return null;
        }

        (LadderInstance? Instance, EditableBlock? InstanceBlock, EditableRung? Rung,
            EditableBlock? InstructionBlock, int RungIndex, LadderInstanceDataBlock? DataBlock)?
            ResolveInstanceDbOwner(string ownerLocator)
        {
            var separator = ownerLocator.IndexOf(':');
            if (separator <= 0 || separator >= ownerLocator.Length - 1) return null;
            var kind = ownerLocator[..separator];
            var id = ownerLocator[(separator + 1)..];
            if (kind == "fb")
            {
                var instance = document.Instances.FirstOrDefault(candidate => candidate.Id == id);
                if (instance?.InstanceDataBlock is null) return null;
                return (instance, document.Blocks.FirstOrDefault(block => block.Id == instance.BlockId),
                    null, null, -1, instance.InstanceDataBlock);
            }

            foreach (var block in document.Blocks)
            for (var index = 0; index < block.Rungs.Count; index++)
            {
                var rung = block.Rungs[index];
                var db = kind switch
                {
                    "timer" when rung.Id == id && (rung.IsTimer || rung.IsTimerReset) => rung.TimerInstanceDataBlock,
                    "counter" when rung.Id == id && (rung.IsCounter || rung.IsCounterReset || rung.IsCounterLoad) => rung.CounterInstanceDataBlock,
                    _ => null,
                };
                if (db is not null) return (null, null, rung, block, index, db);
            }
            return null;
        }

        void OpenInstanceDbDocument(int dbNumber)
        {
            if (!siemens) return;
            var ownerLocator = FindInstanceDbOwnerLocator(dbNumber);
            if (ownerLocator is null)
            {
                output.Text = $"[color=#d64545]Instance DB %DB{dbNumber} is no longer present in the current project.[/color]";
                return;
            }
            OpenInstanceDbDocumentByOwner(ownerLocator);
        }

        void OpenInstanceDbDocumentByOwner(string ownerLocator)
        {
            if (!siemens) return;
            var key = EditorDocumentIdentity.ForInstanceDb(ownerLocator);
            var owner = ResolveInstanceDbOwner(ownerLocator);
            if (owner is null)
            {
                if (editorDocumentButtons.TryGetValue(key, out var staleTab))
                {
                    staleTab.Disabled = true;
                    staleTab.Text = "[Deleted instance DB]";
                    staleTab.TooltipText = "This FB or IEC instance no longer exists. The tab will not retarget another DB.";
                }
                output.Text = "[color=#d64545]This instance DB owner is no longer present in the current project.[/color]";
                return;
            }
            var assignedInstance = owner.Value.Instance;
            var instanceBlock = owner.Value.InstanceBlock;
            var instructionRung = owner.Value.Rung;
            var instructionBlock = owner.Value.InstructionBlock;
            var instructionRungIndex = owner.Value.RungIndex;
            var db = owner.Value.DataBlock!;
            var title = $"{db.Name} [DB{db.Number}]  —  Instance data block";
            AddEditorDocumentTab(key, title);
            editorInstanceDbDocuments[key] = ownerLocator;
            instanceDbHeading.Text = $"{db.Name} [DB{db.Number}]";
            instanceDbSubtitle.Text = "Instance data block · offline project definition";
            instanceDbDeclarations.Clear();
            var declarationRoot = instanceDbDeclarations.CreateItem();
            if (assignedInstance is not null)
            {
                instanceDbSubtitle.Text = $"Assigned instance {assignedInstance.Name} · {instanceBlock?.Name ?? assignedInstance.BlockId} [FB] · offline project definition";
                var interfaceMembers = instanceBlock?.Interface ?? [];
                foreach (var section in Enum.GetValues<LadderInterfaceSection>())
                {
                    // TEMP is invocation-local storage and does not belong to an instance DB.
                    if (section == LadderInterfaceSection.Temp) continue;
                    var members = interfaceMembers.Where(parameter => parameter.Section == section).ToArray();
                    if (members.Length == 0) continue;
                    var sectionRow = instanceDbDeclarations.CreateItem(declarationRoot);
                    sectionRow.SetText(0, section.ToString().ToUpperInvariant());
                    sectionRow.SetSelectable(0, false);
                    foreach (var parameter in members)
                    {
                        var memberRow = instanceDbDeclarations.CreateItem(sectionRow);
                        memberRow.SetText(0, parameter.Name);
                        memberRow.SetText(1, string.IsNullOrWhiteSpace(parameter.DataType) ? parameter.Type.ToString().ToUpperInvariant() : parameter.DataType);
                        var inheritedValue = FormatTagInitialValue(new PlcVariable(parameter.Name, parameter.Type, PlcVariableRole.Memory, parameter.InitialValue));
                        memberRow.SetText(2, inheritedValue);
                        // Siemens initializes a newly generated instance DB's start value
                        // from the FB default. Show that inherited value in both columns;
                        // do not imply that RungProof supports a per-instance override.
                        memberRow.SetText(3, inheritedValue);
                        memberRow.SetCustomColor(3, new Color("78878d"));
                        memberRow.SetTooltipText(3, "Inherited FB default. Per-instance start-value overrides are not modeled.");
                        memberRow.SetText(4, parameter.Retain ? "YES" : "NO");
                        memberRow.SetText(5, parameter.Comment);
                        memberRow.SetTooltipText(5, string.IsNullOrWhiteSpace(parameter.Comment)
                            ? "No comment."
                            : parameter.Comment);
                        memberRow.SetSelectable(0, false);
                    }
                    sectionRow.Collapsed = false;
                }
                if (!interfaceMembers.Any(parameter => parameter.Section != LadderInterfaceSection.Temp))
                {
                    var emptyRow = instanceDbDeclarations.CreateItem(declarationRoot);
                    emptyRow.SetText(0, "No declared FB interface members.");
                    emptyRow.SetSelectable(0, false);
                }
            }
            else if (instructionRung is not null && instructionBlock is not null)
            {
                var rung = instructionRung;
                var timer = rung.IsTimer || rung.IsTimerReset;
                instanceDbSubtitle.Text = $"Instruction instance { (timer ? rung.TimerVariable : rung.CounterVariable) } · {instructionBlock.Name} · Network {instructionRungIndex + 1} · offline definition";
                var sectionRow = instanceDbDeclarations.CreateItem(declarationRoot);
                sectionRow.SetText(0, "STATIC");
                sectionRow.SetSelectable(0, false);
                // TIA's IEC_TIMER system type declares PT, ET, IN, and Q.
                // Keep the declaration table schema complete even though this
                // offline model does not expose live monitor values.
                var memberNames = timer
                    ? new[] { ("PT", "TIME"), ("ET", "TIME"), ("IN", "BOOL"), ("Q", "BOOL") }
                    : new[] { ("Q", "BOOL"), ("CV", "DINT") };
                foreach (var (memberName, memberType) in memberNames)
                {
                    var memberRow = instanceDbDeclarations.CreateItem(sectionRow);
                    memberRow.SetText(0, memberName);
                    memberRow.SetText(1, memberType);
                    memberRow.SetText(2, "");
                    memberRow.SetText(3, "");
                    memberRow.SetText(4, "NO");
                    memberRow.SetText(5, "");
                    memberRow.SetSelectable(0, false);
                }
                sectionRow.Collapsed = false;
            }
            declarationRoot.Collapsed = false;
            SetEditorDocumentBody(key);
            QueueInstanceDbColumnLayout();
            editorDocumentButtons[key].ButtonPressed = true;
            editorTabTitle.Text = $"{db.Name} [DB{db.Number}]";
            editorTabTitle.TooltipText = "Read-only instance DB definition generated from the current FB or IEC instruction assignment.";
            output.Text = $"[color=#18864b]Instance DB opened.[/color] %DB{db.Number} \"{Escape(db.Name)}\".";
        }
        void OpenTagDocument(string? tableName, int selectedIndex = -1)
        {
            if (!siemens)
            {
                activeTagTableName = tableName;
                RefreshEditor();
                projectTabs.CurrentTab = 1;
                return;
            }
            // The empty suffix is reserved for the unfiltered view so a user
            // table named "all" keeps a distinct persistent document key.
            var key = tableName is null ? "tags:" : $"tags:{tableName}";
            editorTagDocuments[key] = tableName;
            var projectFileName = _ladderProjectPath is null
                ? "Untitled.rpproj.json"
                : System.IO.Path.GetFileName(_ladderProjectPath);
            var tagTitle = $"{(LadderEditorProjectJson.HasUnsavedChanges(document, _ladderSavedProjectJson) ? "* " : string.Empty)}{tableName ?? "Show all tags"} — {projectFileName}";
            AddEditorDocumentTab(key, tagTitle);
            activeTagTableName = tableName;
            selectedTagIndex = selectedIndex >= 0 && selectedIndex < document.Tags.Count ? selectedIndex : -1;
            tagFormDirty = false;
            loadingFormControls = true;
            tagName.Clear();
            tagType.Select(0);
            tagRole.Select((int)PlcVariableRole.Memory);
            tagInitialValue.Text = "FALSE";
            tagAddress.Clear();
            tagComment.Clear();
            tagEnumMembers.Text = "Stopped, Running, Fault";
            tagBinding.Clear();
            aggregateTagEditor.Load(PlcVariableType.Bool);
            loadingFormControls = false;
            RefreshTagBindingOptions(PlcVariableType.Bool, PlcVariableRole.Memory, string.Empty);
            RefreshEditor();
            SetEditorDocumentBody(key);
            editorDocumentButtons[key].ButtonPressed = true;
            editorTabTitle.Text = tableName is null ? "Show all tags" : tableName;
            editorTabTitle.TooltipText = tableName is null ? "All PLC tags" : $"PLC tag table · {tableName}";
            if (selectedTagIndex >= 0)
            {
                SelectIndexedTreeRow(tagList, selectedTagIndex);
                tagList.EmitSignal(Tree.SignalName.ItemSelected);
            }
            output.Text = tableName is null
                ? $"Showing all {document.Tags.Count} PLC tags."
                : $"[color=#18864b]Tag table open.[/color] {Escape(tableName)} · {document.Tags.Count(tag => tag.TagTable.Equals(tableName, StringComparison.Ordinal))} tag(s).";
        }
        void OpenWatchTableDocument(EditableWatchTable watchTable)
        {
            if (!siemens) return;
            var tableIndex = document.WatchTables.FindIndex(table => table.Id.Equals(watchTable.Id, StringComparison.Ordinal));
            if (tableIndex < 0) return;
            var key = EditorDocumentIdentity.ForWatchTable(watchTable);
            editorWatchTableDocuments[key] = watchTable.Id;
            document.SelectWatchTable(tableIndex);
            var projectFileName = _ladderProjectPath is null
                ? "Untitled.rpproj.json"
                : System.IO.Path.GetFileName(_ladderProjectPath);
            var dirtyPrefix = LadderEditorProjectJson.HasUnsavedChanges(document, _ladderSavedProjectJson) ? "* " : string.Empty;
            AddEditorDocumentTab(key, $"{dirtyPrefix}{watchTable.Name} — {projectFileName}");
            SetEditorDocumentBody(key);
            ShowBottomDock();
            bottomPanel.Visible = true;
            reopenBottomDock.Visible = false;
            bottomTabs.CurrentTab = 2;
            RefreshWatchTable(_virtualSnapshot);
            SelectActiveWatchTableTreeRow();
            editorDocumentButtons[key].ButtonPressed = true;
            editorTabTitle.Text = $"{dirtyPrefix}{watchTable.Name} — {projectFileName}";
            editorTabTitle.TooltipText = $"Watch table · {watchTable.Name}. Values and simulated force status are offline simulator state.";
            output.Text = $"[color=#18864b]Watch table opened.[/color] {Escape(watchTable.Name)} · {watchTable.Variables.Count} symbol(s).";
        }
        openWatchTableDocumentForMutation = OpenWatchTableDocument;
        watchTableSelector.ItemSelected += index =>
        {
            if (refreshingWatchTableSelector || index < 0 || index >= document.WatchTables.Count) return;
            if (!siemens)
            {
                document.SelectWatchTable((int)index);
                RefreshWatchTable(_virtualSnapshot);
                return;
            }
            if (document.ActiveWatchTableIndex == index) return;
            ResolvePendingEditorForms(() =>
            {
                if (index >= document.WatchTables.Count) return;
                OpenWatchTableDocument(document.WatchTables[(int)index]);
                RefreshWatchTable(_virtualSnapshot);
                if (document.ActiveWatchTableIndex < watchTableItems.Count)
                {
                    watchTableItems[document.ActiveWatchTableIndex].Select(0);
                    organization.ScrollToItem(watchTableItems[document.ActiveWatchTableIndex]);
                }
            });
        };
        void ActivateEditorDocument(string key)
        {
            if (!siemens) return;
            if (!IsOpenEditorDocument(key))
            {
                var fallbackKey = FindOpenEditorDocumentFallback(key);
                if (fallbackKey is not null)
                    ActivateEditorDocument(fallbackKey);
                else
                    SetEditorDocumentBody(string.Empty);
                output.Text = "[color=#d64545]This editor document is no longer available. Select another open document.[/color]";
                return;
            }
            if (TryResolveBlockDocument(key, out var blockIndex))
            {
                var alreadyActive = blockIndex == document.ActiveBlockIndex;
                SwitchEditorBlock(blockIndex, restoreView: true);
                SetEditorDocumentBody(key);
                // SwitchEditorBlock intentionally skips a no-op block switch. A
                // non-block document may still own the title in that case, so
                // refresh after changing the active document body.
                if (alreadyActive) RefreshEditor();
                editorDocumentButtons[key].ButtonPressed = true;
                return;
            }
            if (editorTagDocuments.TryGetValue(key, out var tableName))
                OpenTagDocument(tableName);
            else if (editorInstanceDbDocuments.TryGetValue(key, out var ownerLocator))
                OpenInstanceDbDocumentByOwner(ownerLocator);
            else if (editorWatchTableDocuments.ContainsKey(key)
                && EditorDocumentIdentity.TryResolveWatchTableIndex(document, key, out var watchTableIndex))
                OpenWatchTableDocument(document.WatchTables[watchTableIndex]);
        }
        if (siemens)
        {
            _getActiveTiaEditorDocumentKey = () => activeEditorDocumentKey;
            _ensureTiaEditorDocumentTab = (key, title) =>
            {
                AddEditorDocumentTab(key, title);
                if (key.StartsWith("tags:", StringComparison.Ordinal))
                {
                    var tableName = key["tags:".Length..];
                    editorTagDocuments[key] = tableName.Length == 0 ? null : tableName;
                }
                else if (key.StartsWith("instance-db:", StringComparison.Ordinal))
                {
                    var ownerLocator = key["instance-db:".Length..];
                    if (!ownerLocator.Contains(':') && int.TryParse(ownerLocator, out var legacyDbNumber))
                        ownerLocator = FindInstanceDbOwnerLocator(legacyDbNumber) ?? string.Empty;
                    if (!string.IsNullOrEmpty(ownerLocator)) editorInstanceDbDocuments[key] = ownerLocator;
                }
                else if (EditorDocumentIdentity.TryResolveWatchTableIndex(document, key, out var watchTableIndex))
                {
                    editorWatchTableDocuments[key] = document.WatchTables[watchTableIndex].Id;
                }
            };
            _orderTiaEditorDocumentTabs = keys =>
            {
                for (var index = 0; index < keys.Count; index++)
                    if (editorDocumentItems.TryGetValue(keys[index], out var item))
                        editorDocumentTabs.MoveChild(item, index);
            };
            _activateTiaEditorDocument = key =>
            {
                if (editorDocumentButtons.ContainsKey(key)) ActivateEditorDocument(key);
            };
        }
        var apply = ToolbarButton("ValidateAndLoadButton", "VERIFY + LOAD OFFLINE", new Color("1f8a58"), 210);
        tagPage.AddChild(apply);
        var boundary = Inspector("VendorBoundary");
        boundary.FitContent = true;
        boundary.AddThemeColorOverride("default_color", new Color("344851"));
        boundary.Text = siemens
            ? "Simulator-native STEP 7-style workbench. No TIA project file or Siemens connection is created."
            : "Simulator-native Logix-style workbench. No ACD file or Rockwell connection is created.";
        tagPage.AddChild(boundary);

        var projectObjectsTab = new VBoxContainer
        {
            Name = "Project objects",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        var projectPage = new VBoxContainer
        {
            Name = "ObjectContent",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        projectPage.AddThemeConstantOverride("separation", 6);
        projectPage.AddChild(Heading(siemens ? "PROGRAM BLOCK" : "ROUTINE", 11, new Color("344851")));
        projectPage.AddChild(blockName);
        // Logix has only a routine choice, but refresh callbacks still use
        // this selector. Parent it in both environments so its popup/viewport
        // and theme resources are released with the workbench.
        projectPage.AddChild(blockTypeSelector);
        blockTypeSelector.Visible = siemens;
        var blockCommands = new HFlowContainer { Name = "BlockCommands" };
        blockCommands.AddThemeConstantOverride("h_separation", 4);
        blockCommands.AddThemeConstantOverride("v_separation", 4);
        blockCommands.AddChild(addBlock);
        blockCommands.AddChild(applyBlock);
        blockCommands.AddChild(cancelBlock);
        blockCommands.AddChild(removeBlock);
        projectPage.AddChild(blockCommands);
        projectPage.AddChild(CreateBlockSemanticControls(document, () => { BeginEdit("Convert block", recordHistory: false); RefreshEditor(); }));
        interfaceHeading = Heading(siemens ? "FB / FC INTERFACE" : "ROUTINE PARAMETERS", 11, new Color("344851"));
        interfaceHeading.Name = "BlockInterfaceHeading";
        projectPage.AddChild(interfaceHeading);
        interfaceExecutionNote = Heading("Supported scalar parameters; FB Static also TIMER/COUNTER.", 11, new Color("344851"));
        interfaceExecutionNote.Name = "InterfaceExecutionNote";
        interfaceExecutionNote.ClipText = true;
        interfaceExecutionNote.TooltipText = "Legacy mode keeps descriptive declarations. Explicit parameterized mode executes typed Input/Output/InOut bindings; FB Static storage belongs to the chosen instance.";
        projectPage.AddChild(interfaceExecutionNote);
        var interfaceFields = new HFlowContainer { Name = "InterfaceFields" };
        interfaceFields.AddThemeConstantOverride("h_separation", 3);
        interfaceFields.AddThemeConstantOverride("v_separation", 3);
        interfaceFields.AddChild(interfaceName);
        interfaceFields.AddChild(interfaceSection);
        interfaceFields.AddChild(interfaceType);
        interfaceFields.AddChild(interfaceInitial);
        interfaceFields.AddChild(interfaceEnumMembers);
        interfaceFields.AddChild(interfaceRetain);
        interfaceFields.AddChild(interfaceComment);

        projectPage.AddChild(interfaceFields);
        // Keep every member action reachable when the project dock is narrow.
        // The ObjectContent page also supports horizontal scrolling for fields,
        // but the core add/new/cancel/remove actions must remain in the visible dock.
        var interfaceCommands = new HFlowContainer { Name = "InterfaceCommands" };
        interfaceCommands.AddThemeConstantOverride("h_separation", 4);
        interfaceCommands.AddThemeConstantOverride("v_separation", 4);
        interfaceCommands.AddChild(addInterface);
        interfaceCommands.AddChild(newInterface);
        interfaceCommands.AddChild(cancelInterface);
        interfaceCommands.AddChild(removeInterface);
        projectPage.AddChild(interfaceTable);
        projectPage.AddChild(Heading(siemens ? "OB SCHEDULE" : "TASK SCHEDULE", 11, new Color("344851")));
        projectPage.AddChild(taskName);
        projectPage.AddChild(taskTarget);
        projectPage.AddChild(taskKind);
        var taskTiming = new HFlowContainer { Name = "TaskTiming" };
        taskTiming.AddThemeConstantOverride("h_separation", 4);
        taskTiming.AddThemeConstantOverride("v_separation", 4);
        taskTiming.AddChild(taskPeriod);
        taskTiming.AddChild(taskPriority);
        projectPage.AddChild(taskTiming);
        var taskCommands = new HFlowContainer { Name = "TaskCommands" };
        taskCommands.AddThemeConstantOverride("h_separation", 4);
        taskCommands.AddThemeConstantOverride("v_separation", 4);
        taskCommands.AddChild(addTask);
        taskCommands.AddChild(applyTask);
        taskCommands.AddChild(cancelTask);
        taskCommands.AddChild(removeTask);
        projectPage.AddChild(taskCommands);
        var objectsScroll = new ScrollContainer
        {
            Name = "ProjectObjectScroll",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        objectsScroll.AddChild(projectPage);
        // Keep the core member actions in the fixed-width tab viewport. The
        // declaration fields below remain horizontally scrollable, but their
        // preferred minimum width must not push Add/New/Cancel/Remove off dock.
        projectObjectsTab.AddChild(interfaceCommands);
        projectObjectsTab.AddChild(objectsScroll);
        projectTabs.AddChild(projectObjectsTab);

        // Keep the established project-tree, tag-dock, and project-object tab
        // indices stable. Several DB/property routes intentionally target tab 2.
        if (tagOperandDetailPage is not null)
        {
            projectTabs.AddChild(tagOperandDetailPage);
            tagOperandDetailTabIndex = tagOperandDetailPage.GetIndex();
            projectTabs.SetTabTitle(tagOperandDetailTabIndex, "PLC tag detail view");
            projectTabs.CurrentTab = 0;
        }
        projectTabs.TabChanged += tabIndex =>
        {
            showProjectTree.Visible = siemens && tabIndex != 0;
            if (tabIndex == 0)
            {
                projectHeading.Text = siemens ? "Project tree" : "Controller Organizer";
                addBlockFromTree.Visible = true;
            }
            else if (tagOperandDetailPage is not null && tabIndex == tagOperandDetailTabIndex)
            {
                projectHeading.Text = "PLC tag detail view";
                addBlockFromTree.Visible = false;
            }
            else if (siemens && tabIndex == 2)
            {
                projectHeading.Text = "Project objects";
                addBlockFromTree.Visible = false;
            }
        };
        showProjectTree.Pressed += () => ResolvePendingEditorForms(() =>
        {
            ShowProjectDock();
            projectTabs.CurrentTab = 0;
            projectHeading.Text = siemens ? "Project tree" : "Controller Organizer";
            addBlockFromTree.Visible = true;
            organization.GrabFocus();
        });

        var searchPage = new VBoxContainer { Name = siemens ? "Find and cross-reference" : "Find All" };
        var searchQuery = new LineEdit
        {
            Name = "ProjectSearchQuery",
            PlaceholderText = "tag, block, rung, operand, or instruction",
            CustomMinimumSize = new Vector2(0, 34),
        };
        searchPage.AddChild(searchQuery);
        var searchButtons = new HBoxContainer { Name = "SearchActions" };
        searchButtons.AddThemeConstantOverride("separation", 5);
        var findAll = ToolbarButton("FindAllButton", "FIND ALL", accent, 110);
        var crossReference = ToolbarButton("CrossReferenceButton", siemens ? "CROSS-REFERENCE" : "TAG XREF", new Color("4d6674"), 135);
        findAll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        crossReference.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        searchButtons.AddChild(findAll);
        searchButtons.AddChild(crossReference);
        searchPage.AddChild(searchButtons);
        var searchSummary = Heading("Enter a project search term.", 11, new Color("4f6874"));
        searchSummary.Name = "SearchSummary";
        searchPage.AddChild(searchSummary);
        var searchResults = new ItemList
        {
            Name = "SearchResults",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AllowReselect = true,
        };
        searchResults.AddThemeColorOverride("font_color", new Color("263943"));
        searchResults.AddThemeColorOverride("font_selected_color", new Color("ffffff"));
        ConfigureSelectableList(searchResults);
        searchResults.AddThemeStyleboxOverride("panel", BoxStyle(new Color("f8f9fa"), new Color("b7c0c5")));
        searchPage.AddChild(searchResults);
        var visibleSearchResults = new List<LadderProjectReference>();

        string ReferenceText(LadderProjectReference reference)
        {
            var access = reference.Kind switch
            {
                LadderReferenceKind.Declaration => "DECL",
                LadderReferenceKind.BlockDeclaration => siemens ? "BLOCK" : "ROUTINE",
                LadderReferenceKind.NetworkDeclaration => siemens ? "NETWORK" : "RUNG",
                LadderReferenceKind.TaskDeclaration => siemens ? "OB/TASK" : "TASK",
                LadderReferenceKind.Read => "READ",
                LadderReferenceKind.Write => "WRITE",
                LadderReferenceKind.Call => siemens ? "CALL" : "JSR",
                LadderReferenceKind.ProgramControl => siemens ? "RETURN" : "RET",
                LadderReferenceKind.ScheduledEntry => "SCHEDULE",
                _ => reference.Kind.ToString().ToUpperInvariant(),
            };
            var location = reference.HasNetwork
                ? $"{reference.BlockName} · {(siemens ? "N" : "R")}{reference.NetworkIndex + 1} · {reference.NetworkLabel}"
                : string.IsNullOrWhiteSpace(reference.BlockName) ? "Project tags" : reference.BlockName;
            return $"{access} · {reference.Symbol} · {location} · {reference.Detail}";
        }

        void ShowSearchResults(IReadOnlyList<LadderProjectReference> results, string mode)
        {
            visibleSearchResults.Clear();
            visibleSearchResults.AddRange(results);
            searchResults.Clear();
            foreach (var reference in results)
            {
                var index = searchResults.ItemCount;
                searchResults.AddItem(ReferenceText(reference));
                searchResults.SetItemMetadata(index, $"{reference.BlockId}\u001f{reference.NetworkIndex}");
                searchResults.SetItemTooltip(index,
                    $"{reference.Kind}: {reference.Symbol}\nBlock: {reference.BlockName}\nRung/network: {reference.NetworkLabel}\nElement: {reference.ElementId}");
            }
            searchSummary.Text = $"{mode} · {results.Count} match(es) · double-click";
            searchSummary.AddThemeColorOverride("font_color", results.Count > 0 ? new Color("18864b") : new Color("a64b4b"));
        }

        void RunProjectSearch(bool exactCrossReference)
        {
            var query = searchQuery.Text.Trim();
            if (query.Length == 0)
            {
                ShowSearchResults([], exactCrossReference ? "CROSS-REFERENCE" : "FIND ALL");
                return;
            }
            var index = LadderProjectIndex.Build(document.BuildProgram());
            ShowSearchResults(
                exactCrossReference ? index.CrossReference(query) : index.Search(query),
                exactCrossReference ? "CROSS-REFERENCE" : "FIND ALL");
            toolTabs.CurrentTab = 1;
        }

        void NavigateSearchResult(int resultIndex)
        {
            if (resultIndex < 0 || resultIndex >= visibleSearchResults.Count) return;
            NavigateReference(visibleSearchResults[resultIndex]);
        }

        void NavigateReference(LadderProjectReference reference)
        {
            var declaration = document.Tags.FirstOrDefault(tag => tag.Name.Equals(reference.RootSymbol, StringComparison.Ordinal));
            var declarationId = declaration is null ? "" : LadderCallBindings.GlobalId(declaration);
            ResolvePendingEditorForms(() => NavigateResolvedReference(reference, declarationId));
        }

        void NavigateResolvedReference(LadderProjectReference reference, string declarationId)
        {
            if (reference.Kind == LadderReferenceKind.Declaration)
            {
                var tagIndex = document.Tags.FindIndex(tag => LadderCallBindings.GlobalId(tag) == declarationId);
                if (tagIndex >= 0)
                {
                    if (siemens) OpenTagDocument(null, tagIndex);
                    else
                    {
                        activeTagTableName = null;
                        RefreshEditor();
                        projectTabs.CurrentTab = 1;
                        SelectIndexedTreeRow(tagList, tagIndex);
                        tagList.ScrollToItem(tagList.GetSelected());
                    }
                    output.Text = $"[color=#18864b]{Escape(reference.Symbol)} declaration selected.[/color] {Escape(reference.Detail)}";
                }
                return;
            }
            var blockIndex = document.Blocks.FindIndex(block => block.Id == reference.BlockId);
            if (blockIndex < 0) return;
            var referenceNetwork = reference.HasNetwork ? document.Blocks[blockIndex].Rungs.FindIndex(rung => rung.Id == reference.NetworkId) : 0;
            if (reference.HasNetwork && referenceNetwork < 0) { output.Text = "Reference target no longer exists. Refresh the search."; return; }
            SwitchEditorBlock(blockIndex, restoreView: false);
            blockItems[blockIndex].Select(0);
            organization.ScrollToItem(blockItems[blockIndex]);
            if (reference.HasNetwork)
            {
                selectedRung = referenceNetwork;
                selectedBranch = 0;
                selectedContact = -1;
                selectedInsertionIndex = -1;
                selectedOutput = false;
            }
            RefreshEditor();
            if (document.Rungs.Count > 0) canvas.SelectRung(selectedRung);
            _deviceNavigatedBlock=document.Blocks[document.ActiveBlockIndex].Id;
            _deviceNavigatedNetwork=selectedRung;
            if (document.Rungs.Count > 0) scroll.ScrollVertical = Math.Max(0, (int)canvas.GetRungBounds(selectedRung).Position.Y - 10);
            output.Text = reference.HasNetwork
                ? $"[color=#18864b]Opened {Escape(reference.BlockName)} · {(siemens ? "Network" : "Rung")} {reference.NetworkIndex + 1}.[/color] {Escape(reference.Detail)}"
                : $"[color=#18864b]Opened {Escape(reference.BlockName)}.[/color] {Escape(reference.Detail)}";
        }

        _navigateDeviceReference = NavigateReference;
        findAll.Pressed += () => RunProjectSearch(false);
        crossReference.Pressed += () => RunProjectSearch(true);
        searchQuery.TextSubmitted += _ => RunProjectSearch(false);
        searchResults.ItemActivated += index => NavigateSearchResult((int)index);
        void LoadTagControls()
        {
            if (selectedTagIndex < 0 || selectedTagIndex >= document.Tags.Count) return;
            loadingFormControls = true;
            tagAddressValidation.Text = string.Empty;
            tagAddressValidation.Visible = false;
            var tag = document.Tags[selectedTagIndex];
            tagName.Text = tag.Name;
            tagAddress.Text = tag.Address;
            tagComment.Text = tag.Comment;
            tagType.Select(tag.Type switch
            {
                PlcVariableType.Timer => 1,
                PlcVariableType.Counter => 2,
                PlcVariableType.Int => 3,
                PlcVariableType.DInt => 4,
                PlcVariableType.Real => 5,
                PlcVariableType.Struct => 6,
                PlcVariableType.Array => 7,
                PlcVariableType.String => 8,
                PlcVariableType.Enum => 9,
                PlcVariableType.Time => 10,
                _ => 0,
            });
            tagRole.Select((int)tag.Role);
            var isInstance = tag.Type is PlcVariableType.Timer or PlcVariableType.Counter;
            tagRole.Disabled = isInstance;
            tagInitialValue.Text = FormatTagInitialValue(tag);
            tagInitialValue.Editable = !isInstance && !PlcAggregates.IsAggregate(tag.Type);
            aggregateTagEditor.Load(tag.Type, tag.Aggregate, tag.InitialValue);
            tagEnumMembersRow.Visible = tag.Type == PlcVariableType.Enum;
            tagEnumMembers.Text = string.Join(", ", tag.EnumMembers ?? ["Stopped", "Running", "Fault"]);
            RefreshTagBindingOptions(tag.Type, tag.Role, tag.Binding);
            applyTagEdit!.Disabled = false;
            deleteTag!.Disabled = false;
            output.Text = $"[color=#18864b]{Escape(tag.Name)} selected.[/color] Edit its definition or inspect references before deletion.";
            loadingFormControls = false; tagFormDirty = false;
            RefreshTagTableEditorControls();
            UpdateGlobalDbDeclarationGridEditability();
        }
        bool restoringTagSelection = false;
        void RestoreTagSelection()
        {
            restoringTagSelection = true;
            try { if (selectedTagIndex >= 0) SelectIndexedTreeRow(tagList, selectedTagIndex); else tagList.DeselectAll(); }
            finally { restoringTagSelection = false; }
        }
        tagList.ItemSelected += () =>
        {
            if (restoringTagSelection) return;
            var targetTagIndex = SelectedTreeRowIndex(tagList);
            ResolvePendingEditorForms(() => { selectedTagIndex = targetTagIndex; RestoreTagSelection(); LoadTagControls(); }, RestoreTagSelection);
        };

        xrefSelectedTag.Pressed += () =>
        {
            var selected = SelectedTreeRowIndex(tagList);
            if (selected < 0 || selected >= document.Tags.Count) return;
            searchQuery.Text = document.Tags[selected].Name;
            RunProjectSearch(true);
        };
        tagList.ItemActivated += () =>
        {
            var index = SelectedTreeRowIndex(tagList);
            if (index < 0 || index >= document.Tags.Count) return;
            searchQuery.Text = document.Tags[index].Name;
            RunProjectSearch(true);
        };
        applyTagEdit!.Pressed += () =>
        {
            if (tagFormDirty && !CurrentForm(tagFormRevision)) return;
            if (selectedTagIndex < 0 || selectedTagIndex >= document.Tags.Count || string.IsNullOrWhiteSpace(tagName.Text)) return;
            var current = document.Tags[selectedTagIndex];
            var normalizedName = tagName.Text.Trim();
            var type = SelectedTagType();
            var role = SelectedTagRole(type);
            if (!ValidateSiemensTagAddress(current.Name, type, role)) return;
            if (!TryReadTagInitialValue(type, out var initialValue)) return;
            var schema = PlcAggregates.IsAggregate(type) && aggregateTagEditor.TryRead(type, out var editedSchema, out _, out _) ? editedSchema : null;
            var binding = type is PlcVariableType.Timer or PlcVariableType.Counter ? string.Empty : SelectedTagBinding();
            var targetTable = SelectedTagTableMembership();
            PlcVariable UpdateTagDefinition(LadderEditorDocument target)
            {
                var updatedTag = target.UpdateTag(current.Name, normalizedName, type, role, binding, initialValue, schema,
                    enumMembers: SelectedEnumMembers(type), address: tagAddress.Text, comment: tagComment.Text);
                return updatedTag.TagTable.Equals(targetTable, StringComparison.OrdinalIgnoreCase)
                    ? updatedTag
                    : target.MoveTag(LadderCallBindings.GlobalId(updatedTag), targetTable);
            }
            if (!PreflightTagEdit(UpdateTagDefinition)) return;
            if (!BeginEdit("Update tag definition")) return;
            var updated = UpdateTagDefinition(document);
            tagFormDirty = false;
            RefreshEditor();
            output.Text = $"[color=#18864b]Tag updated.[/color] {Escape(updated.Name)} · {updated.Type} · {updated.Role}. All symbolic references were renamed atomically.";
        };
        deleteTag!.Pressed += () =>
        {
            if (selectedTagIndex < 0 || selectedTagIndex >= document.Tags.Count) return;
            var tag = document.Tags[selectedTagIndex];
            var references = document.CountTagReferences(tag.Name);
            if (references > 0)
            {
                output.Text = $"[color=#d17a00]Delete blocked.[/color] {Escape(tag.Name)} still has {references} executable reference(s). Use cross-reference first.";
                return;
            }
            if (!BeginEdit("Delete unused tag")) return;
            if (!document.TryRemoveTag(tag.Name, out _)) return;
            selectedTagIndex = -1; loadingFormControls = true;
            tagName.Clear();
            tagAddress.Clear();
            tagComment.Clear();
            tagInitialValue.Text = "FALSE";
            tagBinding.Clear();
            RefreshTagBindingOptions(SelectedTagType(), SelectedTagRole(SelectedTagType()), string.Empty);
            loadingFormControls = false; tagFormDirty = false; RefreshEditor();
            output.Text = $"[color=#18864b]Unused tag deleted.[/color] {Escape(tag.Name)} had no executable references.";
        };
        toolTabs.AddChild(searchPage);

        var toolDockHost = new HBoxContainer { Name = "ToolDockHost", Visible = false };
        toolDockHost.AddThemeConstantOverride("separation", 0);
        toolDockHost.AddChild(toolPanel);
        var reopenToolDock = ToolbarButton("ReopenToolDock", "◀", new Color("4d6674"), 30);
        reopenToolDock.CustomMinimumSize = new Vector2(30, 0);
        reopenToolDock.TooltipText = siemens ? "Show Instructions / PLC tags" : "Show Instruction Toolbox";
        reopenToolDock.Visible = false;
        toolDockHost.AddChild(reopenToolDock);
        editorAndTasks.AddChild(toolDockHost);

        var helpPage = new VBoxContainer { Name = "Instruction help" };
        var helpSelector = new OptionButton
        {
            Name = "HelpInstructionSelector",
            CustomMinimumSize = new Vector2(0, 34),
        };
        foreach (var entry in LadderInstructionCatalog.Entries)
        {
            helpSelector.AddItem($"{entry.Category} · {(siemens ? entry.TiaName : entry.LogixName)}");
            helpSelector.SetItemMetadata(helpSelector.ItemCount - 1, entry.Key);
        }
        helpPage.AddChild(helpSelector);
        var helpDetails = Inspector("InstructionHelpDetails");
        helpDetails.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        helpDetails.FitContent = false;
        helpPage.AddChild(helpDetails);
        var currentHelpKey = "no";

        void RenderInstructionHelp(string key, bool openPage)
        {
            if (!LadderInstructionCatalog.TryGet(key, out var entry) || entry is null) return;
            currentHelpKey = key;
            var selectorIndex = Enumerable.Range(0, helpSelector.ItemCount)
                .FirstOrDefault(index => helpSelector.GetItemMetadata(index).AsString() == key, -1);
            if (selectorIndex >= 0) helpSelector.Select(selectorIndex);
            var vendorName = siemens ? entry.TiaName : entry.LogixName;
            helpDetails.Text =
                $"[font_size=18][b]{Escape(vendorName)}[/b][/font_size]\n" +
                $"[color=#7fa7ba]{Escape(entry.Category)} · simulator instruction[/color]\n\n" +
                $"[b]Purpose[/b]\n{Escape(entry.Summary)}\n\n" +
                $"[b]Parameters[/b]\n{Escape(entry.Parameters)}\n\n" +
                $"[b]Scan execution[/b]\n{Escape(entry.Execution)}\n\n" +
                $"[color=#ef7777][b]Restrictions / boundary[/b][/color]\n{Escape(entry.Restrictions)}\n\n" +
                $"[b]Example[/b]\n[font_size={(IsChallengeDraftActive ? 16 : 13)}]{Escape(entry.Example)}[/font_size]\n\n" +
                "[color=#f1aa5b]RungProof offline semantics only. No vendor project file, online help database, or physical PLC connection.[/color]";
            if (openPage) ShowToolDock(2);
        }

        _ladderEditorRefreshers.Add(() => RenderInstructionHelp(currentHelpKey, false));
        helpSelector.ItemSelected += index =>
            RenderInstructionHelp(helpSelector.GetItemMetadata((int)index).AsString(), false);
        openInstructionHelp.Pressed += () => RenderInstructionHelp(currentHelpKey, true);
        toolTabs.AddChild(helpPage);
        projectTabs.SetTabTitle(0, siemens ? "Project tree" : "Controller Organizer");
        projectTabs.SetTabTitle(1, siemens ? "PLC tags" : "Controller Tags");
        projectTabs.SetTabTitle(2, siemens ? "Blocks / tasks" : "Routines / tasks");
        toolTabs.SetTabTitle(0, "Instructions");
        toolTabs.SetTabTitle(1, "Find and cross-reference");
        toolTabs.SetTabTitle(2, "Instruction help");
        var toolRailDefinitions = new (string Text, string Tooltip)[]
        {
            ("Instructions", "Browse and drag ladder instructions"),
            (siemens ? "PLC tags" : "Tags", "Create and edit controller tags"),
            (siemens ? "Blocks" : "Routines", "Manage program blocks and interfaces"),
            ("Search", "Find symbols and cross-references"),
            ("Help", "Read instruction behavior and scan semantics"),
        };
        for (var index = 0; index < toolRailDefinitions.Length; index++)
        {
            var railIndex = index;
            var definition = toolRailDefinitions[index];
            var railButton = new Button
            {
                Name = $"ToolTabRailButton{index}",
                Text = definition.Text,
                TooltipText = definition.Tooltip,
                ToggleMode = true,
                CustomMinimumSize = new Vector2(0, 34),
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                FocusMode = Control.FocusModeEnum.All,
            };
            railButton.Pressed += () =>
            {
                if (siemens && railIndex == 1)
                {
                    ResolvePendingEditorForms(() => OpenTagDocument(null));
                }
                else if (siemens && railIndex == 2)
                {
                    blockFolder.Select(0);
                    organization.ScrollToItem(blockFolder);
                    ShowProjectDock();
                    projectTabs.CurrentTab = 0;
                }
                else if (railIndex is 1 or 2)
                {
                    // Tags and blocks belong to ProjectTabs, not the three
                    // instruction/search/help pages in the right tool dock.
                    reopenProjectDock.Visible = false;
                    projectPanel.Visible = true;
                    work.SplitOffsets = [GetProjectDockSplitOffset()];
                    projectTabs.CurrentTab = railIndex;
                }
                else toolTabs.CurrentTab = railIndex == 0 ? 0 : railIndex == 3 ? 1 : 2;
                for (var buttonIndex = 0; buttonIndex < toolRailButtons.Count; buttonIndex++)
                    toolRailButtons[buttonIndex].ButtonPressed = buttonIndex == railIndex;
            };
            toolRail.AddChild(railButton);
            toolRailButtons.Add(railButton);
        }
        toolTabs.CurrentTab = 0;
        RenderInstructionHelp(currentHelpKey, false);
        void ShowToolDock(int tab)
        {
            toolDockHost.Visible = true;
            toolPanel.Visible = true;
            reopenToolDock.Visible = false;
            toolTabs.CurrentTab = tab;
            editorAndTasks.SplitOffsets = [Math.Max(0, (int)editorAndTasks.Size.X - 320)];
        }
        void ShowProjectDock()
        {
            projectDockHost.Show(); projectPanel.Show(); reopenProjectDock.Hide();
        }
        void ShowBottomDock()
        {
            bottomDockHost.Show(); bottomPanel.Show(); reopenBottomDock.Hide();
        }
        string OfflineDiagnosticStatus() => _virtualProgram is null
            ? "[b]Offline diagnostics[/b] · no controller loaded · Verify + Load required"
            : !_ladderMonitorMatchesLoadedProgram
                ? "[b]Offline diagnostics[/b] · draft differs from loaded controller · previous controller retained"
                : $"[b]Offline diagnostics[/b] · draft matches loaded controller · {_virtualSnapshot?.State.ToString() ?? "snapshot unavailable"}";

        collapseToolDock.Pressed += () =>
        {
            toolDockSplitOffset = editorAndTasks.SplitOffsets.Length > 0
                ? editorAndTasks.SplitOffsets[0]
                : toolDockSplitOffset;
            toolPanel.Visible = false;
            reopenToolDock.Visible = true;
            editorAndTasks.SplitOffsets = [Math.Max(0, (int)editorAndTasks.Size.X - (int)reopenToolDock.CustomMinimumSize.X)];
        };
        reopenToolDock.Pressed += () =>
        {
            ShowToolDock(toolTabs.CurrentTab);
            editorAndTasks.SplitOffsets = [toolDockSplitOffset];
        };

        void RestoreEditorSelection()
        {
            if (document.Rungs.Count == 0) return;
            if (propertyEdit is not null && propertyNodeId.Length > 0) canvas.SelectTreeNode(selectedRung, propertyNodeId, notify: false);
            else if (selectedOutput && propertyNetworkLabel) canvas.SelectNetworkLabel(selectedRung, notify: false);
            else if (selectedOutput) canvas.SelectElement(selectedRung, -1, -1, true, notify: false);
            else if (HasSelectedContact()) canvas.SelectElement(selectedRung, selectedBranch, selectedContact, notify: false);
            else if (selectedInsertionIndex >= 0) canvas.SelectInsertionPoint(selectedRung, selectedBranch, selectedInsertionIndex, notify: false);
            else canvas.SelectRung(selectedRung);
        }
        canvas.RungSelected += index => ResolvePendingEditorForms(() =>
        {
            selectedRung = index;
            selectedBranch = 0;
            selectedContact = -1;
            selectedInsertionIndex = -1;
            selectedOutput = false;
            RefreshEditor();
        }, () => RestoreEditorSelection());
        canvas.ElementSelected += (rungIndex, branchIndex, contactIndex, outputSelected) =>
        {
            var networkLabelSelected = canvas.NetworkLabelSelected;
            ResolvePendingEditorForms(() =>
            {
                selectedRung = rungIndex;
                selectedBranch = branchIndex >= 0 ? branchIndex : 0;
                selectedContact = contactIndex;
                selectedInsertionIndex = -1;
                selectedOutput = outputSelected;
                if (networkLabelSelected) canvas.SelectNetworkLabel(rungIndex, notify: false);
                else canvas.SelectElement(rungIndex, branchIndex, contactIndex, outputSelected, notify: false);
                RefreshEditor();
                if (outputSelected)
                {
                    output.Text = networkLabelSelected
                        ? "[color=#18864b]Network LABEL selected.[/color] Double-click it or right-click → Properties to edit the block-local destination name."
                        : "[color=#18864b]Output instruction selected.[/color] Double-click it or right-click → Properties to edit only this instruction.";
                }
                else
                {
                    var contact = document.Rungs[selectedRung].Branches[selectedBranch].Contacts[selectedContact];
                    output.Text = $"[color=#18864b]Instruction selected.[/color] {Escape(contact.Variable)} · double-click or right-click → Properties; use Move or Delete for structure.";
                }
            }, () => RestoreEditorSelection());
        };
        string SelectedInstructionHelpKey()
        {
            if (document.Rungs.Count == 0) return "rung";
            var rung = document.Rungs[selectedRung];
            if (selectedContact >= 0 && selectedBranch >= 0
                && selectedBranch < rung.Branches.Count
                && selectedContact < rung.Branches[selectedBranch].Contacts.Count)
            {
                var contact = rung.Branches[selectedBranch].Contacts[selectedContact];
                if (contact.IsComparison) return $"compare-{(int)contact.CompareOperator}";
                if (contact.EdgeMode == LadderEdgeMode.Rising) return "edge-rising";
                if (contact.EdgeMode == LadderEdgeMode.Falling) return "edge-falling";
                return contact.NormallyClosed ? "nc" : "no";
            }
            if (!selectedOutput) return "rung";
            if (rung.IsTimerReset) return "timer-reset";
            if (rung.IsTimer)
                return rung.TimerKind switch
                {
                    LadderTimerKind.OffDelay => "tof",
                    LadderTimerKind.Pulse => "tp",
                    LadderTimerKind.RetentiveOnDelay => "rto",
                    _ => "ton",
                };
            if (rung.IsCounterReset) return "counter-reset";
            if (rung.IsCounterLoad) return "counter-load";
            if (rung.IsCounter) return rung.CounterKind == LadderCounterKind.CountDown ? "ctd" : "ctu";
            if (rung.IsNumericOperation) return $"numeric-{(int)rung.NumericOperationKind}";
            if (rung.IsCall) return "call";
            if (rung.IsReturn) return "return";
            if (rung.IsJump) return "jump";
            if (canvas.NetworkLabelSelected && rung.IsLabel) return "label";
            return rung.CoilMode switch
            {
                LadderCoilMode.Set => "set",
                LadderCoilMode.Reset => "reset",
                _ => "coil",
            };
        }
        void InsertInstructionKind(string kind)
        {
            if (kind == "no") addNo.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "empty-box") addEmptyInstruction.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "nc") addNc.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "edge-rising") addRisingEdge.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "edge-falling") addFallingEdge.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "branch") addBranch.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "ton") addTon.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "tof") addTof.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "tp") addTp.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "rto") addRto.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "timer-reset") addTimerReset.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "ctu") addCounter.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "ctd") addCounterDown.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "counter-load") addCounterLoad.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "counter-reset") addCounterReset.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "call" && !siemens) addCall.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "return") addReturn.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "jump") addJump.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "label") addLabel.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind.StartsWith("compare-", StringComparison.Ordinal)
                     && int.TryParse(kind.AsSpan("compare-".Length), out var compareIndex))
            {
                compareOperator.Select(compareIndex);
                addCompare.EmitSignal(BaseButton.SignalName.Pressed);
            }
            else if (kind.StartsWith("numeric-", StringComparison.Ordinal)
                     && int.TryParse(kind.AsSpan("numeric-".Length), out var numericIndex))
            {
                var buttons = new[] { addMove, addAdd, addSubtract, addMultiply, addDivide,
                    addModulo, addAbsolute, addNegate, addSquareRoot, addExponentiate,
                    addNaturalLog, addSine, addCosine, addTangent, addArcSine, addArcCosine,
                    addArcTangent, addTruncate, addNormalize, addScale, addConvert, addRound,
                    addCeiling, addFloor, addFormatText };
                if (numericIndex >= 0 && numericIndex < buttons.Length)
                    buttons[numericIndex].EmitSignal(BaseButton.SignalName.Pressed);
            }
            else if (kind == "set") addSet.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "reset") addReset.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "rung") addRung.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "coil") SelectCoilMode(LadderCoilMode.Assign);
        }
        emptyInstructionChooser.IdPressed += id =>
        {
            if ((uint)id >= (uint)TiaEmptyBoxChoices.Length) return;
            InsertInstructionKind(TiaEmptyBoxChoices[(int)id].Kind);
        };
        canvas.SaveDraftRequested += ShowLadderSaveDialog;
        canvas.SaveAsRequested += ShowLadderSaveAsDialog;
        canvas.UndoEditRequested += () => TryUndoLadderEdit(out _);
        canvas.RedoEditRequested += () => TryRedoLadderEdit(out _);
        // TIA edits the operand at its point of use. Keep this field inside
        // the scrolling canvas so it follows the rung instead of opening a
        // large detached editor panel over the network.
        var operandEditor = new LadderOperandEditor(); canvas.AddChild(operandEditor);
        LadderEditTransaction? operandEdit = null;
        void CancelOperand() { operandEdit?.Cancel(); operandEdit = null; canvas.GrabFocus(); }
        _pendingEditorForms.Add(new(() => operandEditor.Visible, () => operandEditor.TryApply(false), () => operandEditor.CancelEdit()));
        canvas.InlineOperandRequested += (initialText, clearOnOpen, requestedTarget) => ResolvePendingEditorForms(() =>
        {
            if (document.Rungs.Count == 0 || ChallengeMutationLocked) return;
            var networkId = document.Rungs[canvas.SelectedRung].Id;
            var nodeId = canvas.SelectedNodeId;
            var isOutput = canvas.OutputSelected;
            var original = document.Rungs[canvas.SelectedRung];
            var leaf = !isOutput && nodeId.Length > 0 ? EditableLadderTree.Find(original.Root, nodeId) : null;
            if (!isOutput && leaf?.IsLeaf != true || isOutput && (original.IsCall || original.IsJump
                    || canvas.NetworkLabelSelected && original.IsLabel || !original.HasTerminalAction && original.IsLabel || original.IsReturn))
            { OpenInstructionProperties(); return; }
            var symbols = LadderOperandSymbols.ForDocument(document);
            var target = requestedTarget ?? (isOutput ? "terminal-output" : leaf!.Kind == LadderNodeKind.Compare ? "compare-left" : "contact");
            var current = target switch
            {
                "terminal-counter-r" => original.CounterResetInput,
                "terminal-counter-load" => original.CounterLoadInput,
                "terminal-counter-pv" => original.CounterPreset.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "terminal-timer-reset" => original.TimerResetOperand,
                "terminal-timer-pt" => original.TimerPresetOperand,
                "terminal-numeric-a" => original.NumericSourceA,
                "terminal-numeric-b" => original.NumericSourceB,
                "terminal-numeric-c" => original.NumericSourceC,
                "compare-right" => leaf?.RightOperand ?? "",
                _ => isOutput ? original.IsTimer || original.IsTimerReset ? original.TimerVariable
                : original.IsCounter || original.IsCounterLoad || original.IsCounterReset ? original.CounterVariable
                : original.IsNumericOperation ? original.NumericDestination : original.CoilVariable : leaf!.Variable,
            };
            PlcVariableType NumericSourceType(int sourceIndex)
            {
                var selected = LadderNumericOperationRules.SelectedDataTypeForSource(
                    original.NumericOperationKind, sourceIndex, original.NumericDataType,
                    original.NumericInputDataType, original.NumericOutputDataType);
                if (selected == LadderNumericDataType.Unspecified)
                {
                    var choices = LadderNumericOperationRules.HasTiaDualDataTypeSelectors(original.NumericOperationKind)
                        ? original.NumericOperationKind == LadderNumericOperationKind.Scale && sourceIndex != 1
                            ? LadderNumericDataTypePresentation.TiaOutputTypesFor(original.NumericOperationKind)
                            : LadderNumericDataTypePresentation.TiaInputTypesFor(original.NumericOperationKind)
                        : LadderNumericDataTypePresentation.TiaSelectableTypesFor(original.NumericOperationKind);
                    var fixedTypes = choices.Where(item => item != LadderNumericDataType.Unspecified).Distinct().ToArray();
                    if (fixedTypes.Length == 1) selected = fixedTypes[0];

                    // Match the compiler's supported SCALE_X/NORM_X inference:
                    // SCALE_X VALUE is REAL, SCALE_X range pins follow OUT, and
                    // NORM_X range/value pins follow MIN. Other source pins can
                    // infer directly from the operand already attached to them.
                    var inferenceOperand = original.NumericOperationKind switch
                    {
                        LadderNumericOperationKind.Scale when sourceIndex == 1 => null,
                        LadderNumericOperationKind.Scale => original.NumericDestination,
                        LadderNumericOperationKind.Normalize => original.NumericSourceA,
                        LadderNumericOperationKind.Move => original.NumericDestination,
                        _ => current,
                    };
                    if (selected == LadderNumericDataType.Unspecified)
                        selected = LadderNumericDataTypePresentation.FromVariableType(
                            symbols.FirstOrDefault(item => item.Name == inferenceOperand)?.Type);
                }
                return selected switch
                {
                    LadderNumericDataType.Int => PlcVariableType.Int,
                    LadderNumericDataType.Real => PlcVariableType.Real,
                    _ => PlcVariableType.DInt,
                };
            }
            var type = target switch
            {
                "terminal-counter-r" or "terminal-counter-load" => PlcVariableType.Bool,
                "terminal-counter-pv" => original.CounterValueType is LadderCounterValueType.DInt or LadderCounterValueType.UDInt ? PlcVariableType.DInt : PlcVariableType.Int,
                "terminal-timer-reset" => PlcVariableType.Bool,
                "terminal-timer-pt" => PlcVariableType.Time,
                "terminal-numeric-a" => NumericSourceType(0),
                "terminal-numeric-b" => NumericSourceType(1),
                "terminal-numeric-c" => NumericSourceType(2),
                "compare-right" => leaf?.ComparisonValueType switch
                {
                    LadderComparisonValueType.Real => PlcVariableType.Real,
                    LadderComparisonValueType.DInt => PlcVariableType.DInt,
                    _ => PlcVariableType.Int,
                },
                _ => isOutput ? original.IsTimer || original.IsTimerReset ? PlcVariableType.Timer
                : original.IsCounter || original.IsCounterLoad || original.IsCounterReset ? PlcVariableType.Counter
                : original.IsNumericOperation ? symbols.FirstOrDefault(item => item.Name == current)?.Type ?? PlcVariableType.DInt : PlcVariableType.Bool
                : leaf!.Kind == LadderNodeKind.Contact ? PlcVariableType.Bool : symbols.FirstOrDefault(item => item.Name == current)?.Type ?? PlcVariableType.DInt,
            };
            operandEdit = new LadderEditTransaction(document, "Edit operand");
            var operandBounds = canvas.GetOperandEditorBounds(current, target);
            var writableOperand = target == "terminal-output";
            operandEditor.Begin(current, symbols.Where(item => item.Type == type && (!writableOperand || item.Writable)), (value, create) =>
            {
                if (operandEdit is null) return "No active edit.";
                var draft = operandEdit.Draft;
                try
                {
                    if (create && target == "terminal-counter-pv") return "PV is a numeric literal on this instruction; enter the value directly.";
                    if (create)
                    {
                        if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_]*$")) return "New tag names must be identifiers; aggregate members must already be declared.";
                        if (draft.Tags.Any(tag => tag.Name == value)) return "Tag already exists. Use Apply to assign it.";
                        var created = draft.AddTag(value, PlcVariableRole.Memory, type: type);
                        if (draft.Blocks[draft.ActiveBlockIndex].SemanticMode == LadderSemanticMode.Parameterized) value = "global:" + LadderCallBindings.GlobalId(created);
                    }
                    var literalAllowed = target is "compare-right" or "terminal-counter-r" or "terminal-counter-load"
                        or "terminal-timer-reset" or "terminal-timer-pt" or "terminal-numeric-a" or "terminal-numeric-b" or "terminal-numeric-c";
                    if (target == "terminal-counter-pv")
                    {
                        if (!long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var preset))
                            return "PV must be an integer literal for the currently supported counter model.";
                        if (preset > LadderCounterValueTypeRange.Maximum(draft.Rungs.First(item => item.Id == networkId).CounterValueType))
                            return "PV exceeds the selected counter data type.";
                    }
                    else
                    {
                        var error = LadderOperandSymbols.Validate(LadderOperandSymbols.ForDocument(draft), value, type,
                            target == "terminal-output",
                            literalAllowed || (!isOutput && leaf?.Kind == LadderNodeKind.Compare));
                        if (error is not null) return error;
                    }
                    var rung = draft.Rungs.First(item => item.Id == networkId);
                    if (target == "contact" || target == "compare-left")
                        rung.SetRoot(EditableLadderTree.Replace(rung.Root, nodeId, leaf! with { Variable = value }));
                    else if (target == "compare-right")
                        rung.SetRoot(EditableLadderTree.Replace(rung.Root, nodeId, leaf! with { RightOperand = value }));
                    else if (target == "terminal-counter-r") rung.CounterResetInput = value;
                    else if (target == "terminal-counter-load") rung.CounterLoadInput = value;
                    else if (target == "terminal-counter-pv") rung.CounterPreset = long.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    else if (target == "terminal-timer-reset") rung.TimerResetOperand = value;
                    else if (target == "terminal-timer-pt")
                    {
                        rung.TimerPresetOperand = value;
                        rung.TimerPresetAssigned = value.Length > 0;
                        if (LadderTimeLiteral.TryParse(value, out var parsedPreset)) rung.TimerPreset = parsedPreset;
                    }
                    else if (target == "terminal-numeric-a") rung.NumericSourceA = value;
                    else if (target == "terminal-numeric-b") rung.NumericSourceB = value;
                    else if (target == "terminal-numeric-c") rung.NumericSourceC = value;
                    else if (rung.IsTimer || rung.IsTimerReset) rung.TimerVariable = value;
                    else if (rung.IsCounter || rung.IsCounterLoad || rung.IsCounterReset) rung.CounterVariable = value;
                    else if (rung.IsNumericOperation) rung.NumericDestination = value;
                    else rung.CoilVariable = value;
                    var terminalTarget = target.StartsWith("terminal-", StringComparison.Ordinal);
                    var editedId = isOutput || terminalTarget ? rung.TerminalActionId : nodeId;
                    var issue = LadderCompiler.Validate(draft.BuildProgram()).FirstOrDefault(i =>
                        EditorDiagnosticTarget.Capture(draft, i.Path)?.Target is { } target && target.NetworkId == networkId && target.NodeId == editedId);
                    if (issue is not null) return issue.Message;
                    if (!operandEdit.TryCommit(_ladderHistory, ChallengeMutationLocked, out var commitError)) return commitError;
                    operandEdit = null; BeginEdit("Edit operand", recordHistory: false); RefreshEditor(); canvas.GrabFocus(); return "";
                }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException) { return error.Message; }
            }, CancelOperand, initialText, clearOnOpen);
            operandEditor.PlaceInline(operandBounds);
        });
        canvas.TagOperandDropRequested += (tagIdentity, target) =>
        {
            if (!LadderTagOperandDragPayload.TryResolveScalarTag(document, tagIdentity, out _, out var operand))
            {
                output.Text = "[color=#d64545]Tag drop rejected.[/color] The source declaration no longer exists or is not a scalar tag.";
                return;
            }
            canvas.RequestInlineOperandEdit(operand, clearOnOpen: false, target);
        };
        canvas.KeyboardInstructionRequested += InsertInstructionKind;
        canvas.KeyboardSelectionChanged += () =>
        {
            if (document.Rungs.Count == 0) return;
            LadderEditorZoom.RevealSelection(canvas, scroll);
        };
        canvas.PropertiesRequested += (_, _, _, _) => OpenInstructionProperties();
        canvas.CounterValueTypeRequested += (rungIndex, valueType) =>
        {
            if (ChallengeMutationLocked || rungIndex < 0 || rungIndex >= document.Rungs.Count) return;
            var rung = document.Rungs[rungIndex];
            if (!rung.IsCounter && !rung.IsCounterLoad) return;
            if (rung.CounterValueType == valueType) return;
            _ladderHistory.Execute(document, "Change counter data type", () => rung.CounterValueType = valueType);
            BeginEdit("Change counter data type", recordHistory: false);
            RefreshEditor();
            canvas.GrabFocus();
        };
        canvas.NumericDataTypeRequested += (rungIndex, dataType) =>
        {
            if (ChallengeMutationLocked || rungIndex < 0 || rungIndex >= document.Rungs.Count) return;
            var rung = document.Rungs[rungIndex];
            if (!rung.IsNumericOperation || !LadderNumericOperationRules.HasTiaDataTypeSelector(rung.NumericOperationKind)
                || rung.NumericDataType == dataType) return;
            var mnemonic = TiaNumericInstructionPorts.Mnemonic(rung.NumericOperationKind);
            _ladderHistory.Execute(document, $"Change {mnemonic} data type", () => rung.NumericDataType = dataType);
            BeginEdit($"Change {mnemonic} data type", recordHistory: false);
            RefreshEditor();
            canvas.GrabFocus();
        };
        canvas.NumericConvertDataTypeRequested += (rungIndex, input, dataType) =>
        {
            if (ChallengeMutationLocked || rungIndex < 0 || rungIndex >= document.Rungs.Count) return;
            var rung = document.Rungs[rungIndex];
            if (!rung.IsNumericOperation || !LadderNumericOperationRules.HasTiaDualDataTypeSelectors(rung.NumericOperationKind)) return;
            var previous = input ? rung.NumericInputDataType : rung.NumericOutputDataType;
            if (previous == dataType) return;
            var side = input ? "input" : "output";
            var mnemonic = TiaNumericInstructionPorts.Mnemonic(rung.NumericOperationKind);
            _ladderHistory.Execute(document, $"Change {mnemonic} {side} data type", () =>
            {
                if (input) rung.NumericInputDataType = dataType;
                else rung.NumericOutputDataType = dataType;
            });
            BeginEdit($"Change {mnemonic} {side} data type", recordHistory: false);
            RefreshEditor();
            canvas.GrabFocus();
        };
        canvas.ComparisonValueTypeRequested += (rungIndex, nodeId, valueType) =>
        {
            if (ChallengeMutationLocked || rungIndex < 0 || rungIndex >= document.Rungs.Count) return;
            var rung = document.Rungs[rungIndex];
            var node = EditableLadderTree.Find(rung.Root, nodeId);
            if (node is not { Kind: LadderNodeKind.Compare } || node.ComparisonValueType == valueType) return;
            _ladderHistory.Execute(document, "Change comparator data type", () =>
                rung.SetRoot(EditableLadderTree.Replace(rung.Root, nodeId,
                    node with { ComparisonValueType = valueType })));
            BeginEdit("Change comparator data type", recordHistory: false);
            RefreshEditor();
            canvas.GrabFocus();
        };
        canvas.ContextMenuRequested += (_, _, _, _) =>
        {
            popupTarget = CaptureSelectionTarget();
            foreach (var pair in new[] { (1, EditorCommandId.Delete), (7, EditorCommandId.Delete), (3, EditorCommandId.Copy), (4, EditorCommandId.Paste), (6, EditorCommandId.CopyNetwork) })
            {
                var permitted = popupTarget is not null && LadderEditorCommands.CanExecute(document,
                    new(pair.Item2, popupTarget, _ladderCommandClipboard), ChallengeMutationLocked, out _);
                instructionContextMenu.SetItemDisabled(instructionContextMenu.GetItemIndex(pair.Item1), !permitted);
            }
            var mouse = canvas.GetGlobalMousePosition();
            instructionContextMenu.Position = new Vector2I((int)mouse.X, (int)mouse.Y);
            instructionContextMenu.Popup();
        };
        canvas.EmptyInstructionChooserRequested += rungIndex =>
        {
            if (!siemens || ChallengeMutationLocked || rungIndex < 0 || rungIndex >= document.Rungs.Count) return;
            selectedRung = rungIndex;
            selectedBranch = selectedContact = selectedInsertionIndex = -1;
            selectedOutput = true;
            // PopupMenu is a native transient window, while the LAD surface is
            // drawn in the editor canvas. The click position is already in the
            // desktop coordinate space expected by PopupMenu.Position.
            emptyInstructionChooser.Position = DisplayServer.MouseGetPosition();
            emptyInstructionChooser.Popup();
        };
        canvas.InsertionPointSelected += (rungIndex, branchIndex, insertionIndex) => ResolvePendingEditorForms(() =>
        {
            selectedRung = rungIndex;
            selectedBranch = branchIndex;
            selectedContact = -1;
            selectedInsertionIndex = insertionIndex;
            selectedOutput = false;
            RefreshEditor();
            canvas.SelectInsertionPoint(selectedRung, selectedBranch, selectedInsertionIndex, notify: false);
            output.Text = $"[color=#18864b]Insertion point selected.[/color] Branch {selectedBranch + 1}, position {selectedInsertionIndex + 1}. Choose a contact or comparison instruction.";
        }, () => RestoreEditorSelection());
        canvas.InstructionDropRequested += (kind, _, _, _) =>
        {
            // A dragged coil is an output instruction. Commit the default
            // assignment coil immediately, matching the drop-to-rung behavior
            // of the vendor editors. Set/reset remain explicit palette items.
            if (kind == "coil") SelectCoilMode(LadderCoilMode.Assign);
            else InsertInstructionKind(kind);
        };
        canvas.BlockCallDropRequested += (rungIndex, targetBlockIndex) =>
        {
            if (!siemens || rungIndex < 0 || rungIndex >= document.Rungs.Count
                || targetBlockIndex < 0 || targetBlockIndex >= document.Blocks.Count)
                return;

            var target = document.Blocks[targetBlockIndex];
            var owner = document.Blocks[document.ActiveBlockIndex];
            if (target.BlockType is not (LadderBlockType.Function or LadderBlockType.FunctionBlock)
                || target.Id == owner.Id)
            {
                output.Text = "[color=#d64545]Block call rejected.[/color] Choose a different FC or FB from Program blocks.";
                return;
            }
            if (target.SemanticMode != LadderSemanticMode.Parameterized)
            {
                output.Text = "[color=#d64545]Block call rejected.[/color] Convert the target FC/FB to parameterized mode first so its formals and FB instance are explicit.";
                return;
            }
            var rung = document.Rungs[rungIndex];
            selectedRung = rungIndex;
            selectedBranch = -1;
            selectedContact = -1;
            selectedInsertionIndex = -1;
            selectedOutput = true;
            canvas.SelectElement(rungIndex, -1, -1, output: true, notify: false);

            // Like TIA's FB call-options step, require an explicit instance
            // and typed actuals before this call can pass offline validation.
            var edit = LadderCallAuthoring.CreateForDroppedBlock(document, owner.Id, rung.Id, target.Id);
            OpenCallAuthoring(edit, () =>
            {
                BeginEdit("Apply dropped FC/FB call", recordHistory: false);
                RefreshEditor();
                canvas.SelectElement(rungIndex, -1, -1, output: true, notify: false);
            });
            output.Text = target.BlockType == LadderBlockType.FunctionBlock
                ? "[color=#18864b]Inserted FB call.[/color] Complete the instance and actual-parameter assignments."
                : "[color=#18864b]Inserted FC call.[/color] Complete the actual-parameter assignments.";
        };
        branchSelector.ItemSelected += index =>
        {
            selectedBranch = (int)index;
            selectedContact = -1;
            selectedInsertionIndex = -1;
            selectedOutput = false;
            canvas.ClearElementSelection();
            RefreshEditor();
        };
        bool HasSelectedBranch() => selectedRung >= 0 && selectedRung < document.Rungs.Count
            && selectedBranch >= 0 && selectedBranch < document.Rungs[selectedRung].Branches.Count;
        bool HasSelectedContact() => HasSelectedBranch() && selectedContact >= 0
            && selectedContact < document.Rungs[selectedRung].Branches[selectedBranch].Contacts.Count;
        void InsertOrChangeContact(bool normallyClosed)
        {
            if (selectedRung >= 0 && selectedRung < document.Rungs.Count && !document.Rungs[selectedRung].IsFlat && selectedContact < 0)
            {
                if (tagSelector.ItemCount == 0 || tagSelector.Selected < 0) return;
                var target = canvas.CaptureTreeTarget();
                DispatchTreeCommand(new(target.Slot >= 0 ? LadderTreeCommandId.InsertAt : LadderTreeCommandId.InsertAfter,
                    target, new("pending-contact", LadderNodeKind.Contact, tagSelector.GetItemText(tagSelector.Selected), normallyClosed)));
                return;
            }
            if (!HasSelectedBranch() || tagSelector.ItemCount == 0 || tagSelector.Selected < 0) return;
            if (selectedContact >= 0)
            {
                var current = document.Rungs[selectedRung].Branches[selectedBranch].Contacts[selectedContact];
                if (current.IsComparison) return;
                if (!BeginEdit(normallyClosed ? "Change to normally-closed contact" : "Change to normally-open contact")) return;
                document.ReplaceContact(selectedRung, selectedBranch, selectedContact,
                    current with { NormallyClosed = normallyClosed, EdgeMode = LadderEdgeMode.None });
            }
            else
            {
                if (!BeginEdit(normallyClosed ? "Add normally-closed contact" : "Add normally-open contact")) return;
                var contacts = document.Rungs[selectedRung].Branches[selectedBranch].Contacts;
                var insertionIndex = selectedInsertionIndex >= 0 ? selectedInsertionIndex : contacts.Count;
                document.InsertContact(
                    selectedRung,
                    selectedBranch,
                    insertionIndex,
                    tagSelector.GetItemText(tagSelector.Selected),
                    normallyClosed);
                selectedContact = insertionIndex;
            }
            selectedInsertionIndex = -1;
            selectedOutput = false;
            RefreshEditor();
            canvas.SelectElement(selectedRung, selectedBranch, selectedContact, notify: false);
        }
        addNo.Pressed += () =>
            InsertOrChangeContact(false);
        addEmptyInstruction!.Pressed += () =>
        {
            if (!siemens || selectedRung < 0 || selectedRung >= document.Rungs.Count
                || !BeginEdit("Insert empty instruction box")) return;
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = false;
            rung.HasEmptyInstructionPlaceholder = true;
            rung.IsTimer = rung.IsTimerReset = rung.IsCounter = rung.IsCounterReset = rung.IsCounterLoad = false;
            rung.IsNumericOperation = rung.IsCall = rung.IsReturn = rung.IsJump = rung.IsLabel = false;
            rung.CallTarget = string.Empty;
            rung.CallBindings = [];
            rung.CallInstanceId = string.Empty;
            selectedBranch = selectedContact = selectedInsertionIndex = -1;
            selectedOutput = true;
            RefreshEditor();
            canvas.SelectElement(selectedRung, -1, -1, output: true, notify: false);
            output.Text = "[color=#18864b]Empty instruction box inserted.[/color] Choose an instruction from the palette to replace the placeholder.";
        };
        addNc.Pressed += () =>
            InsertOrChangeContact(true);
        void InsertOrChangeEdge(LadderEdgeMode edgeMode)
        {
            if (selectedRung >= 0 && selectedRung < document.Rungs.Count && !document.Rungs[selectedRung].IsFlat && selectedContact < 0)
            {
                if (tagSelector.ItemCount == 0 || tagSelector.Selected < 0) return;
                var target = canvas.CaptureTreeTarget();
                DispatchTreeCommand(new(target.Slot >= 0 ? LadderTreeCommandId.InsertAt : LadderTreeCommandId.InsertAfter,
                    target, new("pending-edge", LadderNodeKind.Contact, tagSelector.GetItemText(tagSelector.Selected), EdgeMode: edgeMode)));
                return;
            }
            if (!HasSelectedBranch() || tagSelector.ItemCount == 0 || tagSelector.Selected < 0) return;
            if (selectedContact >= 0)
            {
                var current = document.Rungs[selectedRung].Branches[selectedBranch].Contacts[selectedContact];
                if (current.IsComparison) return;
                if (!BeginEdit(edgeMode == LadderEdgeMode.Rising
                        ? "Change to rising-edge contact" : "Change to falling-edge contact")) return;
                document.ReplaceContact(selectedRung, selectedBranch, selectedContact,
                    current with { NormallyClosed = false, EdgeMode = edgeMode,
                        EdgeMemoryOperand = current.EdgeMode == LadderEdgeMode.None
                            ? document.CreateEdgeMemoryOperand() : current.EdgeMemoryOperand });
            }
            else
            {
                if (!BeginEdit(edgeMode == LadderEdgeMode.Rising
                        ? "Add rising-edge contact" : "Add falling-edge contact")) return;
                var contacts = document.Rungs[selectedRung].Branches[selectedBranch].Contacts;
                var insertionIndex = selectedInsertionIndex >= 0 ? selectedInsertionIndex : contacts.Count;
                document.InsertEdgeContact(selectedRung, selectedBranch, insertionIndex,
                    tagSelector.GetItemText(tagSelector.Selected), edgeMode);
                selectedContact = insertionIndex;
            }
            selectedInsertionIndex = -1;
            selectedOutput = false;
            RefreshEditor();
            canvas.SelectElement(selectedRung, selectedBranch, selectedContact, notify: false);
        }
        addRisingEdge.Pressed += () => InsertOrChangeEdge(LadderEdgeMode.Rising);
        addFallingEdge.Pressed += () => InsertOrChangeEdge(LadderEdgeMode.Falling);
        addBranch.Pressed += () =>
        {
            if (document.Rungs.Count == 0) return;
            var target = canvas.CaptureTreeTarget();
            if (target.Slot >= 0)
            {
                DispatchTreeCommand(new(LadderTreeCommandId.OpenParallelAtSlot, target));
                return;
            }
            if (!document.Rungs[selectedRung].IsFlat)
            { DispatchTreeCommand(new(LadderTreeCommandId.WrapParallel, target)); return; }
            if (!BeginEdit("Add parallel branch")) return;
            document.AddParallelBranch(selectedRung);
            selectedBranch = document.Rungs[selectedRung].Branches.Count - 1;
            selectedContact = -1;
            selectedInsertionIndex = 0;
            selectedOutput = false;
            RefreshEditor();
            canvas.SelectInsertionPoint(selectedRung, selectedBranch, selectedInsertionIndex, notify: false);
        };
        removeBranch.Pressed += () =>
        {
            if (document.Rungs.Count == 0) return;
            if (!document.Rungs[selectedRung].IsFlat)
            {
                var target = canvas.CaptureTreeTarget();
                var parent = EditableLadderTree.Visit(document.Rungs[selectedRung].Root)
                    .FirstOrDefault(n => n.Kind == LadderNodeKind.Parallel && n.Children?.Any(c => c.Id == target.NodeId) == true);
                if (parent is null)
                { output.Text = "[color=#d17a00]Select an exact parallel path boundary before removing a path.[/color]"; return; }
                DispatchTreeCommand(new(LadderTreeCommandId.Delete, target)); return;
            }
            var branches = document.Rungs[selectedRung].Branches;
            if (branches.Count <= 1)
            {
                output.Text = "[color=#d17a00]Branch retained.[/color] A network/rung must keep one logic path.";
                return;
            }
            if (!BeginEdit("Delete parallel branch")) return;
            document.RemoveParallelBranch(selectedRung, selectedBranch);
            selectedBranch = Math.Clamp(selectedBranch, 0, document.Rungs[selectedRung].Branches.Count - 1);
            selectedContact = -1;
            selectedInsertionIndex = -1;
            selectedOutput = false;
            canvas.ClearElementSelection();
            RefreshEditor();
            output.Text = $"[color=#18864b]Parallel branch deleted.[/color] {document.Rungs[selectedRung].Branches.Count} path(s) remain.";
        };
        void SelectTimer(LadderTimerKind kind)
        {
            if (document.Rungs.Count == 0) return;
            var mnemonic = kind switch
            {
                LadderTimerKind.OffDelay => "TOF",
                LadderTimerKind.Pulse => "TP",
                LadderTimerKind.RetentiveOnDelay => siemens ? "TONR" : "RTO",
                _ => "TON",
            };
            if (!BeginEdit($"Insert {mnemonic}")) return;
            var timerTag = document.Tags.FirstOrDefault(tag => tag.Type == PlcVariableType.Timer);
            if (timerTag is null)
            {
                var suffix = 1;
                var name = "timer_1";
                while (document.Tags.Any(tag => tag.Name == name)) name = $"timer_{++suffix}";
                timerTag = document.AddTag(name, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            }
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = true;
            rung.HasEmptyInstructionPlaceholder = false;
            rung.IsTimer = true;
            rung.IsTimerReset = false;
            rung.IsCounter = false;
            rung.IsCounterReset = false;
            rung.IsCounterLoad = false;
            rung.IsNumericOperation = false;
            rung.IsCall = false;
            rung.IsReturn = false;
            rung.IsJump = false;
            rung.IsLabel = false;
            rung.TimerVariable = timerTag.Name;
            rung.TimerKind = kind;
            if (rung.TimerPreset <= TimeSpan.Zero) rung.TimerPreset = TimeSpan.FromSeconds(1);
            document.EnsureInstanceDataBlocks();
            RefreshEditor();
            output.Text = $"[color=#18864b]{mnemonic} inserted.[/color] {Escape(timerTag.Name)} uses a {rung.TimerPreset.TotalMilliseconds:0} ms preset.";
        }
        addTon.Pressed += () => SelectTimer(LadderTimerKind.OnDelay);
        addTof.Pressed += () => SelectTimer(LadderTimerKind.OffDelay);
        addTp.Pressed += () => SelectTimer(LadderTimerKind.Pulse);
        addRto.Pressed += () => SelectTimer(LadderTimerKind.RetentiveOnDelay);
        addTimerReset.Pressed += () =>
        {
            if (document.Rungs.Count == 0) return;
            // The generated timer tag and reset instruction form one edit.
            // Capture history before either mutation so Undo removes both.
            if (!BeginEdit(siemens ? "Insert timer reset" : "Insert timer RES")) return;
            var timerTag = document.Tags.FirstOrDefault(tag => tag.Type == PlcVariableType.Timer);
            if (timerTag is null)
            {
                var suffix = 1;
                var name = "timer_1";
                while (document.Tags.Any(tag => tag.Name == name)) name = $"timer_{++suffix}";
                timerTag = document.AddTag(name, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            }
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = true;
            rung.HasEmptyInstructionPlaceholder = false;
            rung.IsTimer = false;
            rung.IsTimerReset = true;
            rung.IsCounter = false;
            rung.IsCounterReset = false;
            rung.IsCounterLoad = false;
            rung.IsNumericOperation = false;
            rung.IsCall = false;
            rung.IsReturn = false;
            rung.IsJump = false;
            rung.IsLabel = false;
            rung.TimerVariable = timerTag.Name;
            document.EnsureInstanceDataBlocks();
            RefreshEditor();
            output.Text = $"[color=#18864b]{(siemens ? "RT" : "RES")} inserted.[/color] A true rung clears {Escape(timerTag.Name)}.";
        };
        void SelectCoilMode(LadderCoilMode mode)
        {
            if (document.Rungs.Count == 0 || coilSelector.ItemCount == 0) return;
            if (!BeginEdit(mode switch
                {
                    LadderCoilMode.Set => siemens ? "Insert Set coil" : "Insert OTL",
                    LadderCoilMode.Reset => siemens ? "Insert Reset coil" : "Insert OTU",
                    _ => siemens ? "Insert assignment coil" : "Insert OTE",
                })) return;
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = true;
            rung.HasEmptyInstructionPlaceholder = false;
            rung.IsTimer = false;
            rung.IsTimerReset = false;
            rung.IsCounter = false;
            rung.IsCounterReset = false;
            rung.IsCounterLoad = false;
            rung.IsNumericOperation = false;
            rung.IsCall = false;
            rung.IsReturn = false;
            rung.IsJump = false;
            rung.IsLabel = false;
            if (coilSelector.Selected >= 0 && coilSelector.GetItemId(coilSelector.Selected) != int.MaxValue)
                rung.CoilVariable = coilSelector.GetItemText(coilSelector.Selected);
            rung.CoilMode = mode;
            RefreshEditor();
            var name = mode switch
            {
                LadderCoilMode.Set => siemens ? "Set coil" : "OTL",
                LadderCoilMode.Reset => siemens ? "Reset coil" : "OTU",
                _ => siemens ? "Assignment coil" : "OTE",
            };
            output.Text = $"[color=#18864b]{name} selected.[/color] The instruction writes {Escape(rung.CoilVariable)} using scan-state semantics.";
        }
        addAssign.Pressed += () => SelectCoilMode(LadderCoilMode.Assign);
        addSet.Pressed += () => SelectCoilMode(LadderCoilMode.Set);
        addReset.Pressed += () => SelectCoilMode(LadderCoilMode.Reset);
        void SelectCounter(LadderCounterKind kind = LadderCounterKind.CountUp, bool reset = false, bool load = false)
        {
            if (document.Rungs.Count == 0) return;
            var mnemonic = reset ? "RES" : load ? "LOAD" : kind == LadderCounterKind.CountDown ? "CTD" : "CTU";
            if (!BeginEdit($"Insert {mnemonic}")) return;
            var counterTag = document.Tags.FirstOrDefault(tag => tag.Type == PlcVariableType.Counter);
            if (counterTag is null)
            {
                var suffix = 1;
                var name = "counter_1";
                while (document.Tags.Any(tag => tag.Name == name)) name = $"counter_{++suffix}";
                counterTag = document.AddTag(name, PlcVariableRole.Memory, type: PlcVariableType.Counter);
            }
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = true;
            rung.HasEmptyInstructionPlaceholder = false;
            rung.IsTimer = false;
            rung.IsTimerReset = false;
            rung.IsCounter = !reset && !load;
            rung.IsCounterReset = reset;
            rung.IsCounterLoad = load;
            rung.CounterKind = kind;
            rung.IsNumericOperation = false;
            rung.IsCall = false;
            rung.IsReturn = false;
            rung.IsJump = false;
            rung.IsLabel = false;
            rung.CounterVariable = counterTag.Name;
            if (siemens && !reset && !load)
                rung.CounterValueType = LadderCounterValueType.Unspecified;
            if (rung.CounterPreset <= 0) rung.CounterPreset = 10;
            document.EnsureInstanceDataBlocks();
            RefreshEditor();
            output.Text = reset
                ? $"[color=#18864b]Counter reset inserted.[/color] {Escape(counterTag.Name)} clears when the rung is true."
                : load
                    ? $"[color=#18864b]Counter load inserted.[/color] {Escape(counterTag.Name)} loads {rung.CounterPreset} while the rung is true."
                    : $"[color=#18864b]{mnemonic} inserted.[/color] {Escape(counterTag.Name)} changes once per false-to-true transition.";
        }
        addCounter.Pressed += () => SelectCounter();
        addCounterDown.Pressed += () => SelectCounter(LadderCounterKind.CountDown);
        addCounterLoad.Pressed += () => SelectCounter(load: true);
        addCounterReset.Pressed += () => SelectCounter(reset: true);
        addCompare.Pressed += () =>
        {
            if (document.Rungs.Count == 0 || compareLeft.ItemCount == 0 || string.IsNullOrWhiteSpace(compareRight.Text)) return;
            if (compareOperator.Selected == (int)LadderCompareOperator.ContainsCode)
            {
                var source = document.Tags.FirstOrDefault(tag => tag.Type == PlcVariableType.String);
                if (source is null)
                {
                    output.Text = "[color=#d64545]CODE MATCH needs a STRING source tag.[/color] Add a STRING tag first.";
                    return;
                }
                if (!document.Tags.Any(tag => tag.Name == compareLeft.GetItemText(compareLeft.Selected) && tag.Type == PlcVariableType.String))
                    for (var item = 0; item < compareLeft.ItemCount; item++)
                        if (compareLeft.GetItemText(item) == source.Name) { compareLeft.Select(item); break; }
                if (compareRight.Text == "0") compareRight.Text = "\"F003\"";
            }
            if (!document.Rungs[selectedRung].IsFlat)
            {
                var target = canvas.CaptureTreeTarget();
                DispatchTreeCommand(new(target.Slot >= 0 ? LadderTreeCommandId.InsertAt : LadderTreeCommandId.InsertAfter,
                    target, new("pending-comparison", LadderNodeKind.Compare, compareLeft.GetItemText(compareLeft.Selected),
                        CompareOperator: (LadderCompareOperator)compareOperator.Selected, RightOperand: compareRight.Text,
                        ComparisonValueType: LadderComparisonValueTypePresentation.FromTiaSelector(compareDataType.Selected))));
                return;
            }
            if (!HasSelectedBranch()) return;
            if (!BeginEdit("Add comparison")) return;
            var contacts = document.Rungs[selectedRung].Branches[selectedBranch].Contacts;
            var insertionIndex = selectedInsertionIndex >= 0 ? selectedInsertionIndex : contacts.Count;
            document.InsertComparison(
                selectedRung,
                selectedBranch,
                insertionIndex,
                compareLeft.GetItemText(compareLeft.Selected),
                (LadderCompareOperator)compareOperator.Selected,
                compareRight.Text,
                LadderComparisonValueTypePresentation.FromTiaSelector(compareDataType.Selected));
            selectedContact = insertionIndex;
            selectedInsertionIndex = -1;
            selectedOutput = false;
            RefreshEditor();
            canvas.SelectElement(selectedRung, selectedBranch, selectedContact, notify: false);
            output.Text = "[color=#18864b]Comparison inserted.[/color] It participates in rung power flow as a Boolean condition.";
        };
        void SelectNumericOperation(LadderNumericOperationKind kind)
        {
            if (document.Rungs.Count == 0) return;
            // Validate destination availability before history, monitor state or rung fields change.
            // Aggregate numeric leaves participate exactly like scalar numeric tags.
            var scalarTags = document.Tags.SelectMany(tag => PlcAggregates.IsAggregate(tag.Type)
                ? AggregateEditorLeaves(tag) : new[] { tag }).ToArray();
            var hasNumeric = scalarTags.Any(tag => tag.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real);
            var hasWritableNumeric = scalarTags.Any(tag => tag.Role != PlcVariableRole.Input
                && tag.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real);
            var hasWritableText = scalarTags.Any(tag => tag.Role != PlcVariableRole.Input
                && tag.Type is PlcVariableType.String or PlcVariableType.Enum);
            if (kind != LadderNumericOperationKind.FormatText && hasNumeric && !hasWritableNumeric
                && !(kind == LadderNumericOperationKind.Move && hasWritableText))
            {
                output.Text = $"[color=#d64545]{kind} insertion rejected.[/color] No writable destination of the required type. Add a Memory or Output tag first.";
                return;
            }
            if (!BeginEdit($"Insert {kind}")) return;
            if (!hasNumeric && !(kind == LadderNumericOperationKind.Move && hasWritableText))
            {
                document.AddTag("numeric_value", PlcVariableRole.Memory, type: PlcVariableType.Real);
                RefreshEditor();
            }
            if (kind == LadderNumericOperationKind.FormatText
                && !document.Tags.Any(tag => tag.Type == PlcVariableType.String && tag.Role != PlcVariableRole.Input))
            {
                var name = "formatted_text";
                for (var suffix = 2; document.Tags.Any(tag => tag.Name == name); suffix++) name = $"formatted_text_{suffix}";
                document.AddTag(name, PlcVariableRole.Memory, type: PlcVariableType.String, initialValue: "");
            }
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = true;
            rung.HasEmptyInstructionPlaceholder = false;
            rung.IsTimer = false;
            rung.IsTimerReset = false;
            rung.IsCounter = false;
            rung.IsCounterReset = false;
            rung.IsCounterLoad = false;
            rung.IsNumericOperation = true;
            rung.IsCall = false;
            rung.IsReturn = false;
            rung.IsJump = false;
            rung.IsLabel = false;
            rung.NumericOperationKind = kind;
            rung.NumericSourceA = string.IsNullOrWhiteSpace(numericSourceA.Text) ? "0" : numericSourceA.Text.Trim();
            rung.NumericSourceB = kind == LadderNumericOperationKind.FormatText
                ? "\"CHICKEN {0:F3} kg\""
                : string.IsNullOrWhiteSpace(numericSourceB.Text) ? "0" : numericSourceB.Text.Trim();
            rung.NumericSourceC = string.IsNullOrWhiteSpace(numericSourceC.Text) ? "0" : numericSourceC.Text.Trim();
            RefreshEditor();
            // Property selectors retain invalid configured references so an existing draft is
            // never silently rewritten. Palette insertion must choose a real writable declaration.
            var destinations = LadderOperandSymbols.ForDocument(document).Where(symbol => symbol.Writable
                && (kind == LadderNumericOperationKind.FormatText ? symbol.Type == PlcVariableType.String
                    : (symbol.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
                        || kind == LadderNumericOperationKind.Move && (symbol.Type is PlcVariableType.String or PlcVariableType.Enum))).ToArray();
            var preferred = numericDestination.ItemCount == 0 ? "" : numericDestination.GetItemText(Math.Max(0, numericDestination.Selected));
            rung.NumericDestination = destinations.FirstOrDefault(symbol => symbol.Name == preferred)?.Name
                ?? destinations.FirstOrDefault()?.Name ?? "";
            RefreshEditor();
            output.Text = $"[color=#18864b]{kind} inserted.[/color] Typed write executes only while the rung is true.";
        }
        addMove.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Move);
        addFormatText.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.FormatText);
        addAdd.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Add);
        addSubtract.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Subtract);
        addMultiply.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Multiply);
        addDivide.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Divide);
        addModulo.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Modulo);
        addAbsolute.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Absolute);
        addNegate.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Negate);
        addSquareRoot.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.SquareRoot);
        addExponentiate.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Exponentiate);
        addNaturalLog.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.NaturalLog);
        addSine.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Sine);
        addCosine.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Cosine);
        addTangent.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Tangent);
        addArcSine.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.ArcSine);
        addArcCosine.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.ArcCosine);
        addArcTangent.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.ArcTangent);
        addTruncate.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Truncate);
        addNormalize.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Normalize);
        addScale.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Scale);
        addConvert.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Convert);
        addRound.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Round);
        addCeiling.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Ceiling);
        addFloor.Pressed += () => SelectNumericOperation(LadderNumericOperationKind.Floor);
        ConfirmationDialog NamedCreationDialog(string nodeName, string title, Func<string, string> create, out LineEdit nameField, out Label errorField, Control? details = null)
        {
            var dialog = new ConfirmationDialog
            {
                Name = nodeName,
                Title = title,
                DialogHideOnOk = false,
                MinSize = new Vector2I(430, 190),
                MaxSize = new Vector2I(560, details is null ? 190 : 340),
                Size = new Vector2I(430, 190),
                Exclusive = true,
            };
            var body = new VBoxContainer { OffsetLeft = 16, OffsetTop = 12, OffsetRight = -16, OffsetBottom = -48 };
            body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            var name = new LineEdit { Name = "CreationName", PlaceholderText = "Enter a unique name" };
            var error = new Label { Name = "CreationError", AutowrapMode = TextServer.AutowrapMode.WordSmart };
            error.AddThemeColorOverride("font_color", new Color("b3261e"));
            body.AddChild(name);
            if (details is not null) body.AddChild(details);
            body.AddChild(error); dialog.AddChild(body); root.AddChild(dialog);
            void Commit()
            {
                error.Text = create(name.Text);
                if (error.Text.Length == 0) dialog.Hide(); else name.GrabFocus();
            }
            dialog.Confirmed += Commit;
            name.TextSubmitted += _ => Commit();
            nameField = name; errorField = error;
            return dialog;
        }
        string UniqueDefault(string prefix, System.Collections.Generic.IEnumerable<string> existing)
        {
            var names = existing.ToHashSet(StringComparer.Ordinal); var number = 1;
            while (names.Contains(prefix + number)) number++;
            return prefix + number;
        }
        EditorTarget? blockCreationTarget = null;
        LadderBlockType pendingBlockType = LadderBlockType.Function;
        string BlockTypePrefix(LadderBlockType type) => type switch
        {
            LadderBlockType.OrganizationBlock => "OB",
            LadderBlockType.FunctionBlock => "FB",
            LadderBlockType.DataBlock => "DB",
            _ => siemens ? "FC" : "Routine_",
        };
        var blockTypeDetails = new VBoxContainer { Name = "BlockTypeChoices" };
        blockTypeDetails.AddThemeConstantOverride("separation", 4);
        blockTypeDetails.AddChild(Heading(siemens ? "Block type" : "Routine type", 11, new Color("c8d3d8")));
        var blockTypeButtonsRow = new HBoxContainer { Name = "BlockTypeButtons" };
        blockTypeButtonsRow.AddThemeConstantOverride("separation", 5);
        blockTypeDetails.AddChild(blockTypeButtonsRow);
        var dataBlockTypeDetails = new VBoxContainer { Name = "CreateDataBlockTypeDetails", Visible = false };
        dataBlockTypeDetails.AddThemeConstantOverride("separation", 3);
        dataBlockTypeDetails.AddChild(Heading("Data block type", 10, new Color("c8d3d8")));
        var dataBlockType = new OptionButton { Name = "CreateDataBlockType", CustomMinimumSize = new Vector2(220, 30) };
        dataBlockType.AddItem("Global DB");
        dataBlockType.Disabled = true;
        dataBlockType.TooltipText = "RungProof currently supports freely declared global DB metadata only.";
        dataBlockTypeDetails.AddChild(dataBlockType);
        var dataBlockTypeBoundary = Heading("ARRAY DB, PLC/system-type DB, and manual instance DB creation are not supported.", 10, new Color("d8b16a"));
        dataBlockTypeBoundary.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        dataBlockTypeDetails.AddChild(dataBlockTypeBoundary);
        blockTypeDetails.AddChild(dataBlockTypeDetails);
        var blockTypeGroup = new ButtonGroup();
        var blockTypeButtons = new Dictionary<LadderBlockType, Button>();
        var blockTypeChoices = siemens
            ? new[]
            {
                (LadderBlockType.OrganizationBlock, "OB"),
                (LadderBlockType.FunctionBlock, "FB"),
                (LadderBlockType.Function, "FC"),
                (LadderBlockType.DataBlock, "DB"),
            }
            : new[] { (LadderBlockType.Function, "Routine") };
        foreach (var (type, label) in blockTypeChoices)
        {
            var choice = ToolbarButton($"CreateBlockType_{type}", label, new Color("637985"), siemens ? 76 : 110);
            choice.ToggleMode = true;
            choice.ButtonGroup = blockTypeGroup;
            choice.TooltipText = type switch
            {
                LadderBlockType.OrganizationBlock => "Organization block (OB)",
                LadderBlockType.FunctionBlock => "Function block (FB)",
                LadderBlockType.DataBlock => "Global data block (DB)",
                _ => siemens ? "Function (FC)" : "Routine",
            };
            blockTypeButtonsRow.AddChild(choice);
            blockTypeButtons.Add(type, choice);
        }
        if (blockTypeButtons.TryGetValue(pendingBlockType, out var initialBlockTypeButton))
            initialBlockTypeButton.SetPressedNoSignal(true);
        var suggestedBlockName = string.Empty;
        var blockCreation = NamedCreationDialog("CreateBlockDialog", siemens ? "Create program block" : "Create routine", name =>
        {
            if (blockCreationTarget is null) return "Creation target expired. Open the form again.";
            pendingBlockType = blockTypeChoices.First(choice => blockTypeButtons[choice.Item1].ButtonPressed).Item1;
            var request = new EditorCommandRequest(EditorCommandId.CreateBlock, blockCreationTarget, Creation: new(name, pendingBlockType));
            if (!LadderEditorCommands.CanExecute(document, request, ChallengeMutationLocked, out var reason))
                return reason switch
                {
                    "duplicate-name" => "A block with this name already exists.",
                    "block-name-conflicts-with-plc-tag" => "This name is already used by a PLC tag.",
                    _ => reason,
                };
            if (!BeginEdit("Create program block", recordHistory: false)) return "Editing unavailable.";
            blockViewMemory.Remember(document.Id, document.Blocks[document.ActiveBlockIndex].Id,
                canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical));
            var result = LadderEditorCommands.Execute(document, _ladderHistory, request, ChallengeMutationLocked);
            if (!result.Applied) return result.Reason;
            selectedRung = 0; selectedBranch = -1; selectedContact = -1; selectedInsertionIndex = -1; selectedOutput = false;
            activeEditorDocumentKey = BlockDocumentKey(document.Blocks[document.ActiveBlockIndex].Id);
            RefreshEditor(); blockItems[^1].Select(0); organization.ScrollToItem(blockItems[^1]);
            output.Text = $"[color=#18864b]Block created.[/color] {Escape(document.Blocks[^1].Name)} · {pendingBlockType}";
            return string.Empty;
        }, out var blockCreationName, out var blockCreationError, siemens ? blockTypeDetails : null);
        foreach (var (type, button) in blockTypeButtons)
        {
            button.Pressed += () =>
            {
                if (pendingBlockType == type) return;
                var keepGeneratedName = blockCreationName.Text == suggestedBlockName;
                pendingBlockType = type;
                dataBlockTypeDetails.Visible = type == LadderBlockType.DataBlock;
                blockCreation.Size = new Vector2I(430, dataBlockTypeDetails.Visible ? 340 : 190);
                if (blockCreation.Visible) blockCreation.PopupCentered();
                if (keepGeneratedName)
                    blockCreationName.Text = suggestedBlockName = UniqueDefault(BlockTypePrefix(type), document.Blocks.Select(item => item.Name));
            };
        }
        addBlock.Pressed += () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            pendingBlockType = siemens ? LadderBlockType.FunctionBlock : LadderBlockType.Function;
            foreach (var (type, button) in blockTypeButtons) button.SetPressedNoSignal(type == pendingBlockType);
            dataBlockTypeDetails.Visible = false;
            blockCreation.Size = new Vector2I(430, 190);
            blockCreationTarget = EditorTarget.CaptureProject(document);
            blockCreationName.Text = suggestedBlockName = UniqueDefault(BlockTypePrefix(pendingBlockType), document.Blocks.Select(item => item.Name));
            blockCreationError.Text = string.Empty;
            blockCreation.PopupCentered(); blockCreationName.GrabFocus(); blockCreationName.SelectAll();
        };
        addBlockFromTree.Pressed += () => addBlock.EmitSignal(BaseButton.SignalName.Pressed);
        var tagTableCreation = NamedCreationDialog("CreateTagTableDialog", "Add new tag table", name =>
        {
            var normalizedName = name.Trim();
            if (normalizedName.Length == 0) return "Enter a tag table name.";
            if (document.TagTables.Contains(normalizedName, StringComparer.OrdinalIgnoreCase))
                return $"Tag table '{normalizedName}' already exists.";
            if (ChallengeMutationLocked) return "Project changes are locked for this lesson.";
            if (!BeginProjectMetadataEdit("Add tag table")) return "Editing unavailable.";
            try
            {
                activeTagTableName = document.AddTagTable(normalizedName);
                selectedTagIndex = -1;
                RefreshEditor();
                var createdIndex = document.TagTables.FindIndex(table => table.Equals(activeTagTableName, StringComparison.Ordinal));
                if (createdIndex >= 0 && createdIndex < tagTableItems.Count)
                {
                    tagTableItems[createdIndex].Select(0);
                    organization.ScrollToItem(tagTableItems[createdIndex]);
                }
                if (siemens) OpenTagDocument(activeTagTableName);
                else projectTabs.CurrentTab = 1;
                output.Text = $"[color=#18864b]Tag table created.[/color] {Escape(activeTagTableName)} · add tags to this table.";
                return string.Empty;
            }
            catch (InvalidOperationException exception) { return exception.Message; }
        }, out var tagTableCreationName, out var tagTableCreationError);
        var tagTableRenameDialog = NamedCreationDialog("RenameTagTableDialog", "Rename tag table", name =>
        {
            var oldName = activeTagTableName;
            if (oldName is null) return "Open a user-defined tag table first.";
            if (ChallengeMutationLocked) return "Project changes are locked for this lesson.";
            var normalizedName = name.Trim();
            if (normalizedName.Length == 0) return "Enter a tag table name.";
            if (normalizedName.Equals(oldName, StringComparison.Ordinal)) return string.Empty;
            try
            {
                var preview = new LadderEditorDocument();
                preview.RestoreSnapshot(document.CaptureSnapshot());
                preview.RenameTagTable(oldName, normalizedName);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            { return exception.Message; }
            if (!BeginProjectMetadataEdit("Rename tag table")) return "Editing unavailable.";
            var renamed = document.RenameTagTable(oldName, normalizedName);
            foreach (var key in editorTagDocuments
                         .Where(pair => pair.Value?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true)
                         .Select(pair => pair.Key).ToArray())
                CloseEditorDocument(key);
            activeTagTableName = renamed;
            RefreshEditor();
            if (siemens) OpenTagDocument(renamed, selectedTagIndex);
            RefreshTagTableEditorControls();
            output.Text = $"[color=#18864b]Tag table renamed.[/color] {Escape(oldName)} → {Escape(renamed)}. Member tags stayed in the table.";
            return string.Empty;
        }, out var tagTableRenameName, out var tagTableRenameError);
        renameTagTable.Pressed += () =>
        {
            if (activeTagTableName is null || renameTagTable.Disabled) return;
            tagTableRenameName.Text = activeTagTableName;
            tagTableRenameError.Text = string.Empty;
            tagTableRenameDialog.PopupCentered();
            tagTableRenameName.GrabFocus();
            tagTableRenameName.SelectAll();
        };
        removeTagTable.Pressed += () =>
        {
            var oldName = activeTagTableName;
            if (oldName is null || removeTagTable.Disabled) return;
            var preview = new LadderEditorDocument();
            preview.RestoreSnapshot(document.CaptureSnapshot());
            if (!preview.TryRemoveTagTable(oldName, out var rejection))
            { output.Text = $"[color=#d64545]Tag table delete rejected.[/color] {Escape(rejection)}"; return; }
            if (!BeginProjectMetadataEdit("Delete empty tag table")) return;
            if (!document.TryRemoveTagTable(oldName, out rejection))
            { output.Text = $"[color=#d64545]Tag table delete rejected.[/color] {Escape(rejection)}"; return; }
            foreach (var key in editorTagDocuments
                         .Where(pair => pair.Value?.Equals(oldName, StringComparison.OrdinalIgnoreCase) == true)
                         .Select(pair => pair.Key).ToArray())
                CloseEditorDocument(key);
            activeTagTableName = "Default tag table";
            selectedTagIndex = -1;
            RefreshEditor();
            if (siemens) OpenTagDocument(activeTagTableName);
            RefreshTagTableEditorControls();
            output.Text = $"[color=#18864b]Empty tag table deleted.[/color] {Escape(oldName)}. Undo restores it.";
        };
        void OpenTagTableCreation()
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            tagTableCreationName.Text = UniqueDefault("Tag_table_", document.TagTables);
            tagTableCreationError.Text = string.Empty;
            tagTableCreation.PopupCentered();
            tagTableCreationName.GrabFocus();
            tagTableCreationName.SelectAll();
        }
        var watchTableCreation = NamedCreationDialog("CreateWatchTableDialog", "Add new watch table", name =>
        {
            var normalizedName = name.Trim();
            if (normalizedName.Length == 0) return "Enter a watch table name.";
            if (document.WatchTables.Any(table => table.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
                return $"Watch table '{normalizedName}' already exists.";
            if (ChallengeMutationLocked) return "Project changes are locked for this lesson.";
            if (!BeginProjectMetadataEdit("Add watch table")) return "Editing unavailable.";
            try
            {
                var createdWatchTable = document.AddWatchTable(normalizedName);
                RefreshEditor();
                ShowBottomDock();
                bottomPanel.Visible = true;
                reopenBottomDock.Visible = false;
                bottomTabs.CurrentTab = 2;
                OpenWatchTableDocument(createdWatchTable);
                SelectActiveWatchTableTreeRow();
                output.Text = $"[color=#18864b]Watch table created.[/color] {Escape(createdWatchTable.Name)} · add symbols to this table.";
                return string.Empty;
            }
            catch (InvalidOperationException exception) { return exception.Message; }
        }, out var watchTableCreationName, out var watchTableCreationError);
        openWatchTableCreation = () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            watchTableCreationName.Text = UniqueDefault("Watch_table_", document.WatchTables.Select(table => table.Name));
            watchTableCreationError.Text = string.Empty;
            watchTableCreation.PopupCentered();
            watchTableCreationName.GrabFocus();
            watchTableCreationName.SelectAll();
        };
        var watchTableRename = NamedCreationDialog("RenameWatchTableDialog", "Rename watch table", name =>
        {
            var normalizedName = name.Trim();
            if (normalizedName.Length == 0) return "Enter a watch table name.";
            if (document.WatchTables.Where((_, index) => index != document.ActiveWatchTableIndex)
                .Any(table => table.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
                return $"Watch table '{normalizedName}' already exists.";
            if (document.ActiveWatchTable.Name.Equals(normalizedName, StringComparison.Ordinal)) return string.Empty;
            if (ChallengeMutationLocked) return "Project changes are locked for this lesson.";
            if (!BeginProjectMetadataEdit("Rename watch table")) return "Editing unavailable.";
            try
            {
                var oldName = document.ActiveWatchTable.Name;
                document.RenameActiveWatchTable(normalizedName);
                RefreshEditor();
                RefreshWatchMetadataEditors();
                SelectActiveWatchTableTreeRow();
                output.Text = $"[color=#18864b]Watch table renamed.[/color] {Escape(oldName)} → {Escape(normalizedName)}";
                return string.Empty;
            }
            catch (InvalidOperationException exception) { return exception.Message; }
        }, out var watchTableRenameName, out var watchTableRenameError);
        openWatchTableRename = () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            watchTableRenameName.Text = document.ActiveWatchTable.Name;
            watchTableRenameError.Text = string.Empty;
            watchTableRename.PopupCentered();
            watchTableRenameName.GrabFocus();
            watchTableRenameName.SelectAll();
        };
        applyBlock.Pressed += () =>
        {
            if (blockFormDirty && !CurrentForm(blockFormRevision)) return;
            if (document.Blocks.Count == 0) return;
            var normalizedName = blockName.Text.Trim();
            if (normalizedName.Length == 0
                || document.Blocks.Where((_, index) => index != document.ActiveBlockIndex)
                    .Any(block => block.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
            {
                output.Text = $"[color=#d64545]Rename blocked.[/color] Enter a non-empty, unique {(siemens ? "block" : "routine")} name.";
                return;
            }
            var preview = new LadderEditorDocument(); preview.RestoreSnapshot(document.CaptureSnapshot());
            try { preview.RenameBlock(document.Blocks[document.ActiveBlockIndex].Id, normalizedName); }
            catch (InvalidOperationException exception)
            { output.Text = $"[color=#d64545]Rename blocked.[/color] {Escape(exception.Message)}"; return; }
            if (!BeginEdit(siemens ? "Rename program block" : "Rename routine")) return;
            var block = document.RenameBlock(document.Blocks[document.ActiveBlockIndex].Id, normalizedName);
            blockFormDirty = false; RefreshEditor();
            blockItems[document.ActiveBlockIndex].Select(0);
            output.Text = $"[color=#18864b]{(siemens ? "Block" : "Routine")} renamed.[/color] Stable calls and task references still target {Escape(block.Name)}.";
        };
        removeBlock.Pressed += () =>
        {
            if (document.Blocks.Count == 0) return;
            var block = document.Blocks[document.ActiveBlockIndex];
            if (!document.CanRemoveBlock(block.Id, out var reason))
            {
                output.Text = $"[color=#d64545]Delete blocked.[/color] {Escape(reason)}";
                return;
            }
            if (!BeginEdit(siemens ? "Delete program block" : "Delete routine")) return;
            document.TryRemoveBlock(block.Id, out _);
            selectedRung = 0;
            selectedBranch = 0;
            selectedContact = -1;
            selectedInsertionIndex = -1;
            selectedOutput = false;
            RefreshEditor();
            blockItems[document.ActiveBlockIndex].Select(0);
            output.Text = $"[color=#18864b]{(siemens ? "Block" : "Routine")} deleted.[/color] {Escape(block.Name)} had no task or CALL/JSR references.";
        };
        void ResetInterfaceForm()
        {
            selectedInterfaceIndex = -1;
            selectedInterfaceBlockId = null;
            selectedInterfaceParameterId = null;
            selectedInterfaceParameterName = null;
            interfaceFormDirty = false;
            loadingFormControls = true;
            interfaceName.Clear();
            var activeBlock = document.Blocks.Count > 0
                ? document.Blocks[Math.Clamp(document.ActiveBlockIndex, 0, document.Blocks.Count - 1)]
                : null;
            interfaceFormOwnerBlockId = activeBlock?.Id;
            var activeBlockIsGlobalDb = activeBlock?.BlockType == LadderBlockType.DataBlock;
            interfaceSection.Select((int)(activeBlockIsGlobalDb
                ? LadderInterfaceSection.Static
                : LadderInterfaceSection.Input));
            interfaceType.Select(0);
            interfaceInitial.Text = "FALSE";
            interfaceInitialValueEdited = false;
            interfaceEnumMembers.Clear();
            interfaceRetain.ButtonPressed = false;
            interfaceComment.Clear();
            addInterface.Text = activeBlockIsGlobalDb ? "+ MEMBER" : "+ PARAMETER";
            newInterface.Text = activeBlockIsGlobalDb ? "NEW MEMBER" : "NEW";
            cancelInterface.Disabled = true;
            loadingFormControls = false;
            UpdateGlobalDbDeclarationGridEditability();
        }

        static string InterfaceInitialDefault(int typeIndex) => typeIndex switch
        {
            1 or 2 or 3 or 5 => "0",
            4 => "T#0ms",
            6 => string.Empty,
            7 => "Value1",
            _ => "FALSE",
        };

        void LoadInterfaceForm(int index)
        {
            var block = document.Blocks.Count > 0 ? document.Blocks[document.ActiveBlockIndex] : null;
            if (block is null || index < 0 || index >= block.Interface.Count) return;
            var parameter = block.Interface[index];
            loadingFormControls = true;
            selectedInterfaceIndex = index;
            selectedInterfaceBlockId = block.Id;
            selectedInterfaceParameterId = parameter.Id;
            interfaceFormOwnerBlockId = block.Id;
            selectedInterfaceParameterName = parameter.Name;
            interfaceName.Text = parameter.Name;
            interfaceType.Select(parameter.Type switch
            {
                PlcVariableType.Int => 1,
                PlcVariableType.DInt => 2,
                PlcVariableType.Real => 3,
                PlcVariableType.Timer => 4,
                PlcVariableType.Counter => 5,
                PlcVariableType.String => 6,
                PlcVariableType.Enum => 7,
                _ => 0,
            });
            interfaceSection.Select((int)parameter.Section);
            interfaceInitial.Text = FormatTagInitialValue(new PlcVariable(parameter.Name, parameter.Type,
                PlcVariableRole.Memory, parameter.InitialValue));
            interfaceInitialValueEdited = false;
            interfaceEnumMembers.Text = string.Join(",", parameter.EnumMembers ?? []);
            interfaceRetain.ButtonPressed = parameter.Retain;
            interfaceComment.Text = parameter.Comment;
            addInterface.Text = block.BlockType == LadderBlockType.DataBlock ? "APPLY MEMBER" : "APPLY EDIT";
            cancelInterface.Disabled = false;
            interfaceFormRevision = document.Revision;
            interfaceFormDirty = false;
            loadingFormControls = false;
            UpdateGlobalDbDeclarationGridEditability();
        }

        int FindSelectedInterfaceIndex(EditableBlock block)
        {
            // Declaration identity survives member rename and row movement.
            // Old project files without IDs retain the name/index fallback.
            var selectedId = selectedInterfaceParameterId;
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                for (var index = 0; index < block.Interface.Count; index++)
                    if (StringComparer.Ordinal.Equals(block.Interface[index].Id, selectedId)) return index;
                return -1;
            }
            var selectedName = selectedInterfaceParameterName;
            if (!string.IsNullOrWhiteSpace(selectedName))
                for (var index = 0; index < block.Interface.Count; index++)
                    if (StringComparer.Ordinal.Equals(block.Interface[index].Name, selectedName)) return index;
            return selectedInterfaceIndex >= 0 && selectedInterfaceIndex < block.Interface.Count
                ? selectedInterfaceIndex : -1;
        }

        bool restoringInterfaceSelection = false;
        void SelectInterfaceTreeRow(int index)
        {
            var row = interfaceTable.GetRoot()?.GetFirstChild();
            for (var current = 0; row is not null && current < index; current++) row = row.GetNext();
            row?.Select(0);
        }

        void RestoreInterfaceSelection()
        {
            restoringInterfaceSelection = true;
            try
            {
                if (document.Blocks.Count > 0)
                {
                    var activeBlock = document.Blocks[Math.Clamp(document.ActiveBlockIndex, 0, document.Blocks.Count - 1)];
                    if (StringComparer.Ordinal.Equals(selectedInterfaceBlockId, activeBlock.Id))
                        selectedInterfaceIndex = FindSelectedInterfaceIndex(activeBlock);
                }
                if (selectedInterfaceIndex >= 0) SelectInterfaceTreeRow(selectedInterfaceIndex);
                else interfaceTable.DeselectAll();
            }
            finally { restoringInterfaceSelection = false; }
        }

        interfaceTable.ItemSelected += () =>
        {
            if (refreshingEditor || restoringInterfaceSelection) return;
            var targetIndex = interfaceTable.GetSelected()?.GetIndex() ?? -1;
            ResolvePendingEditorForms(() =>
            {
                LoadInterfaceForm(targetIndex);
                RestoreInterfaceSelection();
            }, RestoreInterfaceSelection);
        };

        newInterface.Pressed += () => ResolvePendingEditorForms(() =>
        {
            ResetInterfaceForm();
            interfaceTable.DeselectAll();
        }, RestoreInterfaceSelection);
        cancelInterface.Pressed += () =>
        {
            DiscardInterface();
            interfaceTable.DeselectAll();
            output.Text = "[color=#607983]Member edit canceled.[/color] No declaration changes were applied.";
        };
        interfaceType.ItemSelected += index =>
        {
            if (loadingFormControls) return;
            // Keep the initial value valid when the declaration's type changes.
            // Preserve an operator-entered value; only replace the untouched
            // form default with the new type's valid default.
            loadingFormControls = true;
            if ((int)index == 7)
            {
                var members = interfaceEnumMembers.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (members.Length == 0)
                {
                    var firstMember = interfaceInitialValueEdited && !string.IsNullOrWhiteSpace(interfaceInitial.Text)
                        ? interfaceInitial.Text.Trim()
                        : "Value1";
                    interfaceEnumMembers.Text = firstMember;
                    members = [firstMember];
                }
                if (!interfaceInitialValueEdited) interfaceInitial.Text = members[0];
            }
            else if (!interfaceInitialValueEdited) interfaceInitial.Text = InterfaceInitialDefault((int)index);
            loadingFormControls = false;
        };
        interfaceInitial.TextChanged += _ =>
        {
            if (!loadingFormControls) interfaceInitialValueEdited = true;
        };
        interfaceEnumMembers.TextChanged += _ =>
        {
            if (loadingFormControls || interfaceInitialValueEdited || interfaceType.Selected != 7) return;
            var firstMember = interfaceEnumMembers.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault() ?? string.Empty;
            loadingFormControls = true;
            interfaceInitial.Text = firstMember;
            loadingFormControls = false;
        };

        addInterface.Pressed += () =>
        {
            if (interfaceFormDirty && !CurrentForm(interfaceFormRevision)) return;
            if (!siemens || document.Blocks.Count == 0) return;
            if (string.IsNullOrWhiteSpace(interfaceName.Text))
            {
                output.Text = "[color=#d64545]Interface parameter rejected.[/color] Enter a non-empty declaration name.";
                interfaceName.GrabFocus();
                return;
            }
            var activeBlock = document.Blocks[document.ActiveBlockIndex];
            var isEditing = selectedInterfaceIndex >= 0;
            if (isEditing && (!StringComparer.Ordinal.Equals(selectedInterfaceBlockId, activeBlock.Id)
                || (string.IsNullOrWhiteSpace(selectedInterfaceParameterId)
                    && string.IsNullOrWhiteSpace(selectedInterfaceParameterName))))
            {
                output.Text = "[color=#d64545]Declaration edit rejected.[/color] The selected row belongs to a different block. Select the declaration again.";
                ResetInterfaceForm();
                return;
            }
            var editIndex = isEditing
                ? FindSelectedInterfaceIndex(activeBlock)
                : -1;
            if (isEditing && editIndex < 0)
            {
                output.Text = "[color=#d64545]Declaration edit rejected.[/color] The selected declaration no longer exists. Select the declaration again.";
                ResetInterfaceForm();
                return;
            }
            var existing = editIndex >= 0 && editIndex < activeBlock.Interface.Count
                ? activeBlock.Interface[editIndex]
                : null;
            var parameterName = interfaceName.Text.Trim();
            if (activeBlock.Interface.Where((_, index) => index != editIndex)
                .Any(parameter => parameter.Name.Equals(parameterName,
                    activeBlock.BlockType == LadderBlockType.DataBlock
                        ? StringComparison.OrdinalIgnoreCase
                        : StringComparison.Ordinal)))
            {
                output.Text = $"[color=#d64545]Interface parameter rejected.[/color] {Escape(parameterName)} already exists.";
                return;
            }
            var type = interfaceType.Selected switch
            {
                1 => PlcVariableType.Int,
                2 => PlcVariableType.DInt,
                3 => PlcVariableType.Real,
                4 => PlcVariableType.Timer,
                5 => PlcVariableType.Counter,
                6 => PlcVariableType.String,
                7 => PlcVariableType.Enum,
                _ => PlcVariableType.Bool,
            };
            if (!LadderEditorDocument.TryParseInitialValue(type, type == PlcVariableType.String ? interfaceInitial.Text : interfaceInitial.Text.Trim(), out var initial, out var error))
            {
                output.Text = $"[color=#d64545]Interface parameter rejected.[/color] {Escape(error)}";
                return;
            }
            var section = activeBlock.BlockType == LadderBlockType.DataBlock
                ? LadderInterfaceSection.Static
                : (LadderInterfaceSection)Math.Clamp(interfaceSection.Selected, 0, 4);
            var domain = type == PlcVariableType.Enum ? interfaceEnumMembers.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : null;
            if (type == PlcVariableType.Enum
                && (!PlcTextValues.IsValidDomain(domain)
                    || initial is not string enumInitial
                    || !domain!.Contains(enumInitial, StringComparer.Ordinal)))
            {
                output.Text = "[color=#d64545]Interface parameter rejected.[/color] ENUM members must be 1–32 distinct identifiers, and the initial value must be one of them.";
                return;
            }
            var stagedFormal = new LadderInterfaceParameter(parameterName, type, section, initial,
                DataType: existing?.Type == type ? existing.DataType : string.Empty,
                Id: !string.IsNullOrWhiteSpace(existing?.Id) ? existing.Id : "formal-" + Guid.NewGuid().ToString("N"), EnumMembers: domain,
                Retain: activeBlock.BlockType == LadderBlockType.DataBlock && interfaceRetain.ButtonPressed,
                Comment: activeBlock.BlockType == LadderBlockType.DataBlock ? interfaceComment.Text.Trim() : string.Empty);
            if (activeBlock.SemanticMode == LadderSemanticMode.Parameterized && LadderCallAuthoring.ValidateFormal(activeBlock.BlockType, stagedFormal) is { } invalidFormal)
            { output.Text = "[color=#d64545]Interface parameter rejected.[/color] " + Escape(invalidFormal); return; }
            // Reject form errors before history/monitor state changes.
            if (!BeginEdit(existing is null ? "Add block interface parameter" : "Update block interface parameter")) return;
            try
            {
                if (existing is null) activeBlock.Interface.Add(stagedFormal);
                else activeBlock.Interface[editIndex] = stagedFormal;
                ResetInterfaceForm();
                RefreshEditor();
                output.Text = $"[color=#18864b]Declaration {(existing is null ? "added" : "updated")}.[/color] {Escape(parameterName)} · {type}. {activeBlock.SemanticMode} scope.";
            }
            catch (InvalidOperationException exception)
            {
                output.Text = $"[color=#d64545]Interface parameter rejected.[/color] {Escape(exception.Message)}";
            }
        };
        removeInterface.Pressed += () =>
        {
            if (!siemens || document.Blocks.Count == 0) return;
            var selectedBlock = document.Blocks[document.ActiveBlockIndex];
            if (!StringComparer.Ordinal.Equals(selectedInterfaceBlockId, selectedBlock.Id)
                || (string.IsNullOrWhiteSpace(selectedInterfaceParameterId)
                    && string.IsNullOrWhiteSpace(selectedInterfaceParameterName))) return;
            var selectedIndex = FindSelectedInterfaceIndex(selectedBlock);
            if (selectedIndex < 0) return;
            var targetBlockId = selectedBlock.Id;
            var targetParameterId = selectedBlock.Interface[selectedIndex].Id;

            // Resolve the selected member's draft before removing it. A valid
            // Apply may rename the member, so prefer its stable ID and retain
            // the captured row as a legacy fallback for declarations without IDs.
            ResolvePendingEditorForms(() =>
            {
                var activeBlock = document.Blocks.FirstOrDefault(block => StringComparer.Ordinal.Equals(block.Id, targetBlockId));
                if (activeBlock is null) return;
                var index = !string.IsNullOrWhiteSpace(targetParameterId)
                    ? activeBlock.Interface.FindIndex(parameter => StringComparer.Ordinal.Equals(parameter.Id, targetParameterId))
                    : selectedIndex < activeBlock.Interface.Count ? selectedIndex : -1;
                if (index < 0) return;
                if (!BeginEdit("Remove block interface parameter")) return;
                document.RemoveInterfaceParameter(activeBlock.Id, index);
                ResetInterfaceForm();
                RefreshEditor();
            }, RestoreInterfaceSelection);
        };
        globalDbAddMember.Pressed += () => ResolvePendingEditorForms(() =>
        {
            if (projectTabs.CurrentTab != 2) editGlobalDbMembers.EmitSignal(BaseButton.SignalName.Pressed);
            else ShowProjectDock();
            ResetInterfaceForm();
            interfaceTable.DeselectAll();
            output.Text = "New Global DB declaration. Enter the supported properties, then apply or cancel.";
        }, RestoreInterfaceSelection);
        globalDbEditMember.Pressed += () =>
        {
            var selectedRow = globalDbDeclarations?.GetSelected();
            if (selectedRow is null || document.Blocks.Count == 0)
            {
                output.Text = "Select a Global DB declaration before choosing Edit.";
                return;
            }
            var targetBlockId = document.Blocks[document.ActiveBlockIndex].Id;
            var targetIdentity = selectedRow.GetMetadata(0).AsString();
            var targetName = selectedRow.GetMetadata(1).AsString();
            var targetRowIndex = selectedRow.GetIndex();
            ResolvePendingEditorForms(() =>
            {
                var owner = document.Blocks.FirstOrDefault(block => StringComparer.Ordinal.Equals(block.Id, targetBlockId));
                var index = owner?.Interface.FindIndex(parameter =>
                    !string.IsNullOrWhiteSpace(targetIdentity)
                        ? StringComparer.Ordinal.Equals(parameter.Id, targetIdentity)
                        : StringComparer.Ordinal.Equals(parameter.Name, targetName)) ?? -1;
                if (index < 0 && string.IsNullOrWhiteSpace(targetIdentity)
                    && owner is not null && targetRowIndex < owner.Interface.Count)
                    index = targetRowIndex;
                if (owner is null || owner.BlockType != LadderBlockType.DataBlock || index < 0)
                {
                    output.Text = "The selected declaration is no longer present in this Global DB. Select it again.";
                    return;
                }
                if (projectTabs.CurrentTab != 2) editGlobalDbMembers.EmitSignal(BaseButton.SignalName.Pressed);
                else ShowProjectDock();
                selectedInterfaceBlockId = owner.Id;
                SelectInterfaceTreeRow(index);
                LoadInterfaceForm(index);
                output.Text = $"Editing Global DB member {Escape(owner.Interface[index].Name)}. Apply or cancel the pending change.";
            }, RestoreInterfaceSelection);
        };
        globalDbRemoveMember.Pressed += () =>
        {
            var selectedRow = globalDbDeclarations?.GetSelected();
            if (selectedRow is null || document.Blocks.Count == 0)
            {
                output.Text = "Select a Global DB declaration before choosing Remove.";
                return;
            }
            var targetBlockId = document.Blocks[document.ActiveBlockIndex].Id;
            var targetIdentity = selectedRow.GetMetadata(0).AsString();
            var targetName = selectedRow.GetMetadata(1).AsString();
            var targetRowIndex = selectedRow.GetIndex();
            ResolvePendingEditorForms(() =>
            {
                var owner = document.Blocks.FirstOrDefault(block => StringComparer.Ordinal.Equals(block.Id, targetBlockId));
                var index = owner?.Interface.FindIndex(parameter =>
                    !string.IsNullOrWhiteSpace(targetIdentity)
                        ? StringComparer.Ordinal.Equals(parameter.Id, targetIdentity)
                        : StringComparer.Ordinal.Equals(parameter.Name, targetName)) ?? -1;
                if (index < 0 && string.IsNullOrWhiteSpace(targetIdentity)
                    && owner is not null && targetRowIndex < owner.Interface.Count)
                    index = targetRowIndex;
                if (owner is null || owner.BlockType != LadderBlockType.DataBlock || index < 0)
                {
                    output.Text = "The selected declaration is no longer present in this Global DB. Select it again.";
                    return;
                }
                selectedInterfaceBlockId = owner.Id;
                SelectInterfaceTreeRow(index);
                LoadInterfaceForm(index);
                removeInterface.EmitSignal(BaseButton.SignalName.Pressed);
            }, RestoreInterfaceSelection);
        };
        addCall.Pressed += () =>
        {
            if (document.Rungs.Count == 0 || callTarget.ItemCount == 0) return;
            if (!BeginEdit(siemens ? "Insert FC/FB block call" : "Insert JSR")) return;
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = true;
            rung.HasEmptyInstructionPlaceholder = false;
            rung.IsTimer = false;
            rung.IsTimerReset = false;
            rung.IsCounter = false;
            rung.IsCounterReset = false;
            rung.IsCounterLoad = false;
            rung.IsNumericOperation = false;
            rung.IsCall = false;
            rung.IsReturn = false;
            rung.IsJump = false;
            rung.IsLabel = false;
            rung.IsCall = true;
            rung.CallTarget = callTarget.GetItemMetadata(callTarget.Selected).AsString();
            RefreshEditor();
            output.Text = siemens
                ? "[color=#18864b]FC/FB block call inserted.[/color] The named target executes when the rung is true."
                : "[color=#18864b]JSR inserted.[/color] Target executes when the rung is true.";
        };
        addReturn.Pressed += () =>
        {
            if (document.Rungs.Count == 0) return;
            if (!BeginEdit(siemens ? "Insert RETURN" : "Insert RET")) return;
            var rung = document.Rungs[selectedRung];
            rung.HasTerminalAction = true;
            rung.HasEmptyInstructionPlaceholder = false;
            rung.IsTimer = false;
            rung.IsTimerReset = false;
            rung.IsCounter = false;
            rung.IsCounterReset = false;
            rung.IsCounterLoad = false;
            rung.IsNumericOperation = false;
            rung.IsCall = false;
            rung.IsReturn = false;
            rung.IsJump = false;
            rung.IsLabel = false;
            rung.IsReturn = true;
            RefreshEditor();
            output.Text = $"[color=#18864b]{(siemens ? "RETURN" : "RET")} inserted.[/color] A true rung exits only the current execution frame.";
        };
        string DefaultProgramControlLabel(bool declaration)
        {
            var existing = document.Rungs
                .Where(candidate => candidate.IsLabel && !string.IsNullOrWhiteSpace(candidate.NetworkLabelName))
                .Select(candidate => candidate.NetworkLabelName)
                .FirstOrDefault(candidate => !siemens || TiaProgramControlLabel.IsValid(candidate));
            if (!declaration && !string.IsNullOrWhiteSpace(existing)) return existing;
            var suffix = 1;
            var candidateName = siemens ? "Label1" : "label_1";
            while (document.Rungs.Any(candidate => candidate.IsLabel
                       && candidate.NetworkLabelName.Equals(candidateName, StringComparison.Ordinal)))
                candidateName = siemens ? $"Label{++suffix}" : $"label_{++suffix}";
            return candidateName;
        }
        void SelectJumpOrLabel(bool label)
        {
            if (document.Rungs.Count == 0) return;
            if (!BeginEdit(label
                    ? (siemens ? "Insert LABEL" : "Insert LBL")
                    : "Insert JMP")) return;
            var rung = document.Rungs[selectedRung];
            var controlLabel = DefaultProgramControlLabel(label);
            if (label)
            {
                // TIA LABEL is attached to the network. Do not discard the
                // network's existing terminal action when adding it.
                rung.IsLabel = true;
                if (string.IsNullOrWhiteSpace(rung.NetworkLabelId))
                    rung.NetworkLabelId = $"{rung.Id}-label";
                rung.NetworkLabelName = controlLabel;
                if (!rung.IsJump) rung.ProgramControlLabel = controlLabel; // legacy project compatibility
            }
            else
            {
                rung.HasTerminalAction = true;
                rung.HasEmptyInstructionPlaceholder = false;
                rung.IsTimer = false;
                rung.IsTimerReset = false;
                rung.IsCounter = false;
                rung.IsCounterReset = false;
                rung.IsCounterLoad = false;
                rung.IsNumericOperation = false;
                rung.IsCall = false;
                rung.IsReturn = false;
                rung.IsJump = true;
                rung.ProgramControlLabel = controlLabel;
            }
            selectedContact = -1;
            selectedInsertionIndex = -1;
            selectedOutput = true;
            RefreshEditor();
            canvas.SelectElement(selectedRung, -1, -1, output: true, notify: false);
            output.Text = label
                ? $"[color=#18864b]{(siemens ? "LABEL" : "LBL")} inserted.[/color] {Escape(rung.NetworkLabelName)} marks this network and is local to this {(siemens ? "block" : "routine")}."
                : $"[color=#18864b]JMP inserted.[/color] A true rung continues at {Escape(rung.ProgramControlLabel)} in this {(siemens ? "block" : "routine")}.";
        }
        addJump.Pressed += () => SelectJumpOrLabel(false);
        addLabel.Pressed += () => SelectJumpOrLabel(true);
        void LoadTaskControls(int index)
        {
            if (index < 0 || index >= document.Tasks.Count) return;
            selectedTaskIndex = index;
            var task = document.Tasks[index];
            loadingFormControls = true;
            taskName.Text = task.Name;
            taskKind.Select((int)task.Kind);
            taskPeriod.Value = Math.Max(1, task.Period.TotalMilliseconds);
            taskPeriod.Editable = task.Kind == LadderTaskKind.Periodic;
            taskPriority.Value = task.Priority;
            var targetIndex = document.Blocks.FindIndex(block => block.Id == task.EntryBlock);
            if (targetIndex >= 0) taskTarget.Select(targetIndex);
            loadingFormControls = false; taskFormDirty = false;
            UpdateGlobalDbDeclarationGridEditability();
        }
        EditorTarget? taskCreationTarget = null;
        EditorNamedCreation? pendingTaskCreation = null;
        var taskCreation = NamedCreationDialog("CreateTaskDialog", siemens ? "Create OB schedule" : "Create task", name =>
        {
            if (taskCreationTarget is null || pendingTaskCreation is null) return "Creation target expired. Open the form again.";
            var request = new EditorCommandRequest(EditorCommandId.CreateTask, taskCreationTarget, Creation: pendingTaskCreation with { Name = name });
            if (!LadderEditorCommands.CanExecute(document, request, ChallengeMutationLocked, out var reason)) return reason;
            if (!BeginEdit("Create task", recordHistory: false)) return "Editing unavailable.";
            var result = LadderEditorCommands.Execute(document, _ladderHistory, request, ChallengeMutationLocked);
            if (!result.Applied) return result.Reason;
            selectedTaskIndex = document.Tasks.Count - 1; RefreshEditor();
            taskItems[selectedTaskIndex].Select(0); organization.ScrollToItem(taskItems[selectedTaskIndex]);
            output.Text = $"[color=#18864b]Schedule created.[/color] {Escape(document.Tasks[selectedTaskIndex].Name)}";
            return string.Empty;
        }, out var taskCreationName, out var taskCreationError);
        addTask.Pressed += () =>
        {
            if (ChallengeMutationLocked) { ShowChallengeLocked(); return; }
            if (taskTarget.ItemCount == 0) { output.Text = "Create an entry block before a task."; return; }
            taskCreationTarget = EditorTarget.CaptureProject(document);
            pendingTaskCreation = new(string.Empty, TaskKind: (LadderTaskKind)taskKind.Selected,
                Period: taskKind.Selected == (int)LadderTaskKind.Continuous ? document.ScanPeriod : TimeSpan.FromMilliseconds(taskPeriod.Value),
                Priority: (int)taskPriority.Value, EntryBlock: taskTarget.GetItemMetadata(taskTarget.Selected).AsString());
            taskCreationName.Text = UniqueDefault(siemens ? "CyclicOB_" : "Task_", document.Tasks.Select(item => item.Name));
            taskCreationError.Text = string.Empty;
            taskCreation.PopupCentered(); taskCreationName.GrabFocus(); taskCreationName.SelectAll();
        };
        applyTask.Pressed += () =>
        {
            if (taskFormDirty && !CurrentForm(taskFormRevision)) return;
            if (selectedTaskIndex < 0 || selectedTaskIndex >= document.Tasks.Count || taskTarget.ItemCount == 0) return;
            var normalizedName = taskName.Text.Trim();
            if (normalizedName.Length == 0
                || document.Tasks.Where((_, index) => index != selectedTaskIndex)
                    .Any(task => task.Name.Equals(normalizedName, StringComparison.Ordinal)))
            {
                output.Text = $"[color=#d64545]Update blocked.[/color] Enter a non-empty, unique {(siemens ? "OB schedule" : "task")} name.";
                return;
            }
            if (!BeginEdit(siemens ? "Update OB schedule" : "Update task schedule")) return;
            var task = document.Tasks[selectedTaskIndex];
            document.RenameTask(task.Id, normalizedName);
            task.Kind = (LadderTaskKind)taskKind.Selected;
            task.Period = task.Kind == LadderTaskKind.Continuous
                ? document.ScanPeriod
                : TimeSpan.FromMilliseconds(taskPeriod.Value);
            task.Priority = (int)taskPriority.Value;
            task.EntryBlock = taskTarget.GetItemMetadata(taskTarget.Selected).AsString();
            taskFormDirty = false; RefreshEditor();
            output.Text = $"[color=#18864b]Schedule updated.[/color] {Escape(task.Name)} will be compiler-validated before load.";
        };
        removeTask.Pressed += () =>
        {
            if (selectedTaskIndex < 0 || selectedTaskIndex >= document.Tasks.Count) return;
            var task = document.Tasks[selectedTaskIndex];
            if (!document.CanRemoveTask(task.Id, out var reason))
            {
                output.Text = $"[color=#d64545]Delete blocked.[/color] {Escape(reason)}";
                return;
            }
            if (!BeginEdit(siemens ? "Delete OB schedule" : "Delete task")) return;
            document.TryRemoveTask(task.Id, out _);
            selectedTaskIndex = Math.Clamp(selectedTaskIndex, 0, document.Tasks.Count - 1);
            RefreshEditor();
            taskItems[selectedTaskIndex].Select(0);
            organization.ScrollToItem(taskItems[selectedTaskIndex]);
            output.Text = $"[color=#18864b]{(siemens ? "OB schedule" : "Task")} deleted.[/color] {Escape(task.Name)} was removed from the offline scheduler.";
        };
        taskKind.ItemSelected += index => taskPeriod.Editable = index == (long)LadderTaskKind.Periodic;
        addRung.Pressed += () =>
        {
            if (document.Blocks[document.ActiveBlockIndex].BlockType == LadderBlockType.DataBlock)
            {
                output.Text = "[color=#d64545]Network rejected.[/color] A global DB contains declarations, not LAD networks.";
                return;
            }
            var insertionIndex = selectedRung >= 0 && selectedRung < document.Rungs.Count ? selectedRung + 1 : document.Rungs.Count;
            if (!BeginEdit(siemens ? "Add network" : "Add rung")) return;
            var coil = document.Tags.FirstOrDefault(tag => tag.Type == PlcVariableType.Bool && tag.Role != PlcVariableRole.Input)?.Name ?? string.Empty;
            var added = document.AddRung(siemens ? "New network" : "New rung comment", coil);
            document.Rungs.RemoveAt(document.Rungs.Count - 1);
            document.Rungs.Insert(insertionIndex, added);
            selectedRung = insertionIndex;
            selectedBranch = 0;
            selectedContact = -1;
            selectedInsertionIndex = 0;
            selectedOutput = false;
            RefreshEditor();
            canvas.SelectInsertionPoint(selectedRung, selectedBranch, selectedInsertionIndex, notify: false);
        };
        EditorTarget? CaptureSelectionTarget()
        {
            if (selectedRung < 0 || selectedRung >= document.Rungs.Count) return null;
            var role = selectedOutput ? EditorOperandRole.Output
                : HasSelectedContact() ? EditorOperandRole.Contact
                : selectedInsertionIndex >= 0 && HasSelectedBranch() ? EditorOperandRole.Insertion
                : EditorOperandRole.Network;
            return EditorTarget.Capture(document, selectedRung,
                role is EditorOperandRole.Contact or EditorOperandRole.Insertion ? selectedBranch : -1,
                role == EditorOperandRole.Insertion ? selectedInsertionIndex : selectedContact, role);
        }
        bool DispatchEditorCommand(EditorCommandId command, EditorTarget? exactTarget = null)
        {
            if (exactTarget is null && selectedRung >= 0 && selectedRung < document.Rungs.Count
                && !canvas.OutputSelected && !string.IsNullOrEmpty(canvas.SelectedNodeId)
                && (!document.Rungs[selectedRung].IsFlat || selectedContact < 0 && selectedInsertionIndex < 0))
            {
                var treeCommand = command == EditorCommandId.Delete ? LadderTreeCommandId.Delete
                    : command == EditorCommandId.Copy ? LadderTreeCommandId.Copy
                    : command == EditorCommandId.Paste ? canvas.CaptureTreeTarget().Slot >= 0 ? LadderTreeCommandId.PasteAt : LadderTreeCommandId.PasteAfter
                    : command == EditorCommandId.MoveLeft ? LadderTreeCommandId.MoveLeft : LadderTreeCommandId.MoveRight;
                if (command is EditorCommandId.Delete or EditorCommandId.Copy or EditorCommandId.Paste or EditorCommandId.MoveLeft or EditorCommandId.MoveRight)
                    return DispatchTreeCommand(new(treeCommand, canvas.CaptureTreeTarget(),
                        command == EditorCommandId.Paste ? canvas.TreeClipboard
                            ?? (_ladderCommandClipboard is EditorContactClipboard contactClipboard ? EditableLadderTree.FromContact(contactClipboard.Contact) : null) : null));
            }
            var target = exactTarget ?? CaptureSelectionTarget();
            if (target is null) return false;
            var request = new EditorCommandRequest(command, target, _ladderCommandClipboard);
            if (!LadderEditorCommands.CanExecute(document, request, ChallengeMutationLocked || refreshingEditor, out var reason))
            {
                output.Text = $"[color=#d17a00]Command rejected.[/color] {Escape(reason)}";
                return false;
            }
            var result = LadderEditorCommands.Execute(document, _ladderHistory, request, ChallengeMutationLocked);
            if (!result.Applied) { output.Text = $"[color=#d17a00]Command rejected.[/color] {Escape(result.Reason)}"; return false; }
            if (command is not (EditorCommandId.Copy or EditorCommandId.CopyNetwork)) BeginEdit(command.ToString(), recordHistory: false);
            if (result.Clipboard is not null)
            {
                _ladderCommandClipboard = result.Clipboard;
                // Keep legacy verifier fields synchronized during command migration.
                _ladderContactClipboard = (result.Clipboard as EditorContactClipboard)?.Contact;
                _ladderRungClipboard = (result.Clipboard as EditorNetworkClipboard)?.Network;
                output.Text = result.Clipboard is EditorOutputClipboard
                    ? "[color=#18864b]Output copied.[/color] Paste onto an output to replace only its action."
                    : result.Clipboard is EditorContactClipboard ? "[color=#18864b]Instruction copied.[/color] Paste into a series slot."
                    : "[color=#18864b]Network copied.[/color] Paste onto a network header to insert after it.";
                return true;
            }
            if (result.Selection is not null && LadderEditorCommands.TryResolve(document, result.Selection, out var rung, out var branch, out var node, out _))
            {
                selectedRung = rung; selectedBranch = branch; selectedContact = result.Selection.Role == EditorOperandRole.Contact ? node : -1;
                selectedInsertionIndex = -1; selectedOutput = result.Selection.Role == EditorOperandRole.Output;
            }
            else { selectedRung = 0; selectedBranch = -1; selectedContact = -1; selectedInsertionIndex = -1; selectedOutput = false; }
            RefreshEditor();
            if (selectedOutput) canvas.SelectElement(selectedRung, -1, -1, true, notify: false);
            else if (selectedContact >= 0) canvas.SelectElement(selectedRung, selectedBranch, selectedContact, notify: false);
            else canvas.SelectRung(selectedRung);
            output.Text = $"[color=#18864b]{Escape(command.ToString())} applied.[/color]";
            return true;
        }
        void DispatchMove(int direction)
        {
            if (selectedRung >= 0 && selectedRung < document.Rungs.Count && !document.Rungs[selectedRung].IsFlat)
                DispatchTreeCommand(new(direction < 0 ? LadderTreeCommandId.MoveLeft : LadderTreeCommandId.MoveRight, canvas.CaptureTreeTarget()));
            else DispatchEditorCommand(direction < 0 ? EditorCommandId.MoveLeft : EditorCommandId.MoveRight);
        }
        refreshTreeActions = () =>
        {
            if (selectedRung < 0 || selectedRung >= document.Rungs.Count) { removeBranch.Disabled = true; return; }
            var rung = document.Rungs[selectedRung];
            if (rung.IsFlat) { removeBranch.Disabled = ChallengeMutationLocked || rung.Branches.Count <= 1; return; }
            var nodeId = canvas.SelectedNodeId;
            var parent = EditableLadderTree.Visit(rung.Root).FirstOrDefault(n => n.Children?.Any(c => c.Id == nodeId) == true);
            var index = parent?.Children?.ToList().FindIndex(n => n.Id == nodeId) ?? -1;
            moveContactLeft.Disabled = ChallengeMutationLocked || index <= 0;
            moveContactRight.Disabled = ChallengeMutationLocked || parent?.Children is null || index < 0 || index >= parent.Children.Count - 1;
            removeBranch.Disabled = ChallengeMutationLocked || parent?.Kind != LadderNodeKind.Parallel;
        };
        moveContactLeft.Pressed += () => DispatchMove(-1);
        moveContactRight.Pressed += () => DispatchMove(1);
        remove.Pressed += () => DispatchEditorCommand(EditorCommandId.Delete);
        canvas.DeleteRequested += () => DispatchEditorCommand(EditorCommandId.Delete);
        canvas.MoveRequested += DispatchMove;
        void CutLadderSelection(EditorTarget? exactTarget = null)
        {
            if (ChallengeMutationLocked || refreshingEditor) return;
            if (exactTarget is null && !canvas.OutputSelected && canvas.SelectedNodeId.Length > 0 && !document.Rungs[canvas.SelectedRung].IsFlat)
            { DispatchTreeCommand(new(LadderTreeCommandId.Cut, canvas.CaptureTreeTarget())); return; }
            var target = exactTarget ?? CaptureSelectionTarget(); if (target is null) return;
            var deletion = new EditorCommandRequest(EditorCommandId.Delete, target);
            if (!LadderEditorCommands.CanExecute(document, deletion, ChallengeMutationLocked, out _)) return;
            var copied = LadderEditorCommands.Execute(document, _ladderHistory, new(EditorCommandId.Copy, target), ChallengeMutationLocked);
            if (!copied.Applied) return;
            if (!DispatchEditorCommand(EditorCommandId.Delete, target)) return;
            _ladderCommandClipboard = copied.Clipboard;
            _ladderContactClipboard = (copied.Clipboard as EditorContactClipboard)?.Contact;
            _ladderRungClipboard = (copied.Clipboard as EditorNetworkClipboard)?.Network;
        }
        canvas.CutRequested += () => CutLadderSelection();
        void CopyLadderSelection() => DispatchEditorCommand(EditorCommandId.Copy);
        void PasteLadderClipboard() => DispatchEditorCommand(EditorCommandId.Paste);
        canvas.CopyRequested += CopyLadderSelection;
        canvas.PasteRequested += PasteLadderClipboard;
        bool DispatchTreeCommand(LadderTreeEditorRequest request)
        {
            if (siemens && request.Command == LadderTreeCommandId.Delete)
            {
                var targetRungIndex = document.Rungs.FindIndex(item => item.Id == request.Target.NetworkId);
                if (targetRungIndex >= 0)
                {
                    var root = document.Rungs[targetRungIndex].Root;
                    var selected = EditableLadderTree.Find(root, request.Target.NodeId);
                    var parent = EditableLadderTree.Visit(root)
                        .FirstOrDefault(node => node.Children?.Any(child => child.Id == selected?.Id) == true);
                    var grandParent = parent is null ? null : EditableLadderTree.Visit(root)
                        .FirstOrDefault(node => node.Children?.Any(child => child.Id == parent.Id) == true);
                    // STEP 7 removes the rest of a simultaneous branch when
                    // its final LAD element is deleted. Retarget the atomic
                    // edit to that exact path; never leave an open empty leg.
                    if (selected?.IsLeaf == true && parent?.Kind == LadderNodeKind.Series
                        && parent.Children?.Count == 1 && grandParent?.Kind == LadderNodeKind.Parallel
                        && grandParent.Children is { Count: > 1 })
                        request = request with { Target = request.Target with { NodeId = parent.Id, ParentId = "", Slot = -1 } };
                }
            }
            var result = LadderTreeEditorCommands.Execute(document, _ladderHistory, request, ChallengeMutationLocked || refreshingEditor);
            if (!result.Applied)
            { output.Text = $"[color=#d17a00]Tree command rejected.[/color] {Escape(result.Reason)}"; return false; }
            if (result.Clipboard is not null) canvas.TreeClipboard = result.Clipboard;
            if (request.Command != LadderTreeCommandId.Copy) BeginEdit(request.Command.ToString(), recordHistory: false);
            RefreshEditor();
            var rungIndex = document.Rungs.FindIndex(item => item.Id == request.Target.NetworkId);
            if (rungIndex >= 0 && result.SelectionSlot >= 0)
                canvas.SelectTreeInsertionPoint(rungIndex, result.SelectionParentId, result.SelectionSlot);
            else if (rungIndex >= 0) canvas.SelectTreeNode(rungIndex, result.SelectionId);
            output.Text = $"[color=#18864b]{Escape(request.Command.ToString())} applied.[/color]";
            return true;
        }
        canvas.TreeCommandRequested += request => DispatchTreeCommand(request);
        instructionContextMenu.IdPressed += id =>
        {
            // Even read-only popup actions reject obsolete targets. Never use the
            // currently selected instruction as a fallback after undo or block switch.
            if (popupTarget is null || !LadderEditorCommands.TryResolve(document, popupTarget, out var targetRung, out var targetBranch, out var targetNode, out _))
            { output.Text = "[color=#d17a00]Context target expired.[/color] Open its menu again."; return; }
            selectedRung = targetRung; selectedBranch = targetBranch;
            selectedContact = popupTarget.Role == EditorOperandRole.Contact ? targetNode : -1;
            selectedInsertionIndex = popupTarget.Role == EditorOperandRole.Insertion ? targetNode : -1;
            selectedOutput = popupTarget.Role == EditorOperandRole.Output;
            RefreshEditor();
            if (id == 0) OpenInstructionProperties();
            else if (id == 1) DispatchEditorCommand(EditorCommandId.Delete, popupTarget);
            else if (id == 2) RenderInstructionHelp(SelectedInstructionHelpKey(), true);
            else if (id == 3) DispatchEditorCommand(EditorCommandId.Copy, popupTarget);
            else if (id == 4) DispatchEditorCommand(EditorCommandId.Paste, popupTarget);
            else if (id == 6) DispatchEditorCommand(EditorCommandId.CopyNetwork, popupTarget);
            else if (id == 7) CutLadderSelection(popupTarget);
        };
        foreach (var textField in new[] { rungLabel, compareRight, programControlLabel, returnValueOperand, numericSourceA, numericSourceB, numericSourceC, counterLoadInput, counterResetInput, timerPresetOperand, timerResetOperand })
        {
            textField.TextChanged += _ => StageProperty();
            textField.TextSubmitted += _ => ApplyProperties();
            textField.FocusExited += () => propertyEdit?.FocusLost();
            textField.GuiInput += input =>
            {
                if (input is InputEventKey { Pressed: true, Keycode: Key.Escape }) { CancelProperties(); textField.AcceptEvent(); }
            };
        }
        foreach (var selector in new[] { contactTypeSelector, tagSelector, edgeMemorySelector, compareLeft, compareOperator, compareDataType, coilSelector,
            coilModeSelector, timerSelector, counterSelector, counterDataTypeSelector, callTarget, returnValueSelector, numericDestination })
            selector.ItemSelected += _ => StageProperty();
        returnValueSelector.ItemSelected += index =>
        {
            returnOperandLabel.Visible = returnValueOperand.Visible = siemens
                && index == (long)LadderReturnValueKind.Operand;
            StageProperty();
        };
        contactTypeSelector.ItemSelected += index =>
        {
            var visible = index is 2 or 3;
            edgeMemoryLabel.Visible = visible;
            edgeMemorySelector.Visible = visible;
            StageProperty();
        };
        timerPreset.ValueChanged += _ => StageProperty();
        counterPreset.ValueChanged += _ => StageProperty();
        timerDbNumber.ValueChanged += _ => StageProperty(); counterDbNumber.ValueChanged += _ => StageProperty();
        timerDbName.TextChanged += _ => StageProperty(); counterDbName.TextChanged += _ => StageProperty();
        tagType.ItemSelected += index =>
        {
            tagAddressValidation.Text = string.Empty;
            tagAddressValidation.Visible = false;
            var isInstance = index is 1 or 2;
            if (isInstance) tagRole.Select((int)PlcVariableRole.Memory);
            tagRole.Disabled = isInstance;
            tagName.PlaceholderText = index switch
            {
                1 => "new_timer",
                2 => "new_counter",
                3 => "new_int",
                4 => "new_dint",
                5 => "new_real",
                8 => "new_string",
                9 => "new_enum",
                _ => "new_bool_tag",
            };
            var type = SelectedTagType();
            tagInitialValue.Editable = !isInstance && !PlcAggregates.IsAggregate(type);
            aggregateTagEditor.Load(type);
            tagEnumMembersRow.Visible = type == PlcVariableType.Enum;
            tagInitialValue.PlaceholderText = type switch
            {
                PlcVariableType.Bool => "FALSE",
                PlcVariableType.Int or PlcVariableType.DInt => "0",
                PlcVariableType.Real => "0.0",
                PlcVariableType.String => "raw text (no quotes)",
                PlcVariableType.Enum => "Stopped",
                _ => "0",
            };
            if (selectedTagIndex < 0)
                tagInitialValue.Text = type switch
                { PlcVariableType.Bool => "FALSE", PlcVariableType.String => "", PlcVariableType.Enum => "Stopped", _ => "0" };
            RefreshTagBindingOptions(type, SelectedTagRole(type), string.Empty);
        };
        tagRole.ItemSelected += _ =>
        {
            tagAddressValidation.Text = string.Empty;
            tagAddressValidation.Visible = false;
            var type = SelectedTagType();
            RefreshTagBindingOptions(type, SelectedTagRole(type), string.Empty);
        };
        tagBinding.ItemSelected += _ =>
        {
            var type = SelectedTagType();
            RefreshTagBindingOptions(type, SelectedTagRole(type), SelectedTagBinding());
            if (PlcAggregates.IsAggregate(type) && ScenePoints().FirstOrDefault(p => p.Name == SelectedTagBinding()) is { Aggregate: not null } root)
                aggregateTagEditor.Load(type, root.Aggregate, _runtime?.Points.GetValueOrDefault(root.Name));
            if (type == PlcVariableType.Enum && ScenePoints().FirstOrDefault(p => p.Name == SelectedTagBinding()) is { EnumMembers: not null } enumPoint)
            {
                tagEnumMembers.Text = string.Join(", ", enumPoint.EnumMembers);
                if (!enumPoint.EnumMembers.Contains(tagInitialValue.Text, StringComparer.Ordinal) && enumPoint.EnumMembers.Count > 0)
                    tagInitialValue.Text = enumPoint.EnumMembers[0];
            }
        };
        addTag.Pressed += () =>
        {
            if (string.IsNullOrWhiteSpace(tagName.Text))
            { output.Text = "[color=#d64545]Tag definition rejected.[/color] Enter a non-empty tag name."; tagName.GrabFocus(); return; }
            var type = SelectedTagType();
            var role = SelectedTagRole(type);
            if (!ValidateSiemensTagAddress(null, type, role)) return;
            if (!TryReadTagInitialValue(type, out var initialValue)) return;
            var schema = PlcAggregates.IsAggregate(type) && aggregateTagEditor.TryRead(type, out var newSchema, out _, out _) ? newSchema : null;
            var binding = type is PlcVariableType.Timer or PlcVariableType.Counter ? string.Empty : SelectedTagBinding();
            PlcVariable AddDefinition(LadderEditorDocument target) => schema is not null
                ? target.AddAggregateTag(tagName.Text, role, type, schema, initialValue, binding,
                    SelectedTagTableMembership(), tagAddress.Text, tagComment.Text)
                : target.AddTag(tagName.Text, role, binding, type, initialValue,
                    enumMembers: SelectedEnumMembers(type), tagTable: SelectedTagTableMembership(),
                    address: tagAddress.Text, comment: tagComment.Text);
            if (!PreflightTagEdit(AddDefinition)) return;
            if (!BeginEdit("Add tag")) return;
            AddDefinition(document);
            tagFormDirty = false;
            loadingFormControls = true;
            tagName.Clear();
            tagAddress.Clear();
            tagComment.Clear();
            tagInitialValue.Text = type switch
                { PlcVariableType.Bool => "FALSE", PlcVariableType.String => "", PlcVariableType.Enum => "Stopped", _ => "0" };
            RefreshTagBindingOptions(type, role, string.Empty);
            loadingFormControls = false; tagFormDirty = false; RefreshEditor();
        };
        applyProperties.Pressed += () => ApplyProperties(); cancelProperties.Pressed += () => CancelProperties();
        closeInstructionProperties.Pressed += () => CancelProperties();
        _pendingEditorForms.Add(new(() => propertyDirty, () => ApplyProperties(), () => CancelProperties()));
        instructionPropertiesPopup.GuiInput += input =>
        {
            if (input is InputEventKey { Pressed: true, Keycode: Key.Escape })
            { CancelProperties(); instructionPropertiesPopup.AcceptEvent(); }
        };

        void WatchPending(Control control, Action mark)
        {
            void Changed()
            {
                if (refreshingEditor || loadingFormControls) return;
                mark();
                RefreshTagTableEditorControls();
                UpdateGlobalDbDeclarationGridEditability();
            }
            if (control is LineEdit text) text.TextChanged += _ => Changed();
            else if (control is OptionButton option) option.ItemSelected += _ => Changed();
            else if (control is SpinBox number) number.ValueChanged += _ => Changed();
            else if (control is CheckBox checkBox) checkBox.Toggled += _ => Changed();
        }
        foreach (var field in new Control[] { tagName, tagTableMembership, tagAddress, tagComment, tagInitialValue, tagEnumMembers, tagType, tagRole, tagBinding }) WatchPending(field, () => { if (!tagFormDirty) tagFormRevision = document.Revision; tagFormDirty = true; });
        tagAddress.TextChanged += _ =>
        {
            tagAddressValidation.Text = string.Empty;
            tagAddressValidation.Visible = false;
        };
        aggregateTagEditor.PendingChanged += () =>
        {
            if (refreshingEditor || loadingFormControls) return;
            if (!tagFormDirty) tagFormRevision = document.Revision;
            tagFormDirty = true;
            UpdateGlobalDbDeclarationGridEditability();
        };
        foreach (var field in new Control[] { blockName, blockTypeSelector }) WatchPending(field, () => { if (!blockFormDirty) blockFormRevision = document.Revision; blockFormDirty = true; });
        foreach (var field in new Control[] { taskName, taskKind, taskPeriod, taskPriority, taskTarget }) WatchPending(field, () => { if (!taskFormDirty) taskFormRevision = document.Revision; taskFormDirty = true; });
        foreach (var field in new Control[] { interfaceName, interfaceType, interfaceSection, interfaceInitial, interfaceEnumMembers, interfaceRetain, interfaceComment }) WatchPending(field, () =>
        {
            if (!interfaceFormDirty) interfaceFormRevision = document.Revision;
            interfaceFormDirty = true;
            cancelInterface.Disabled = false;
            UpdateGlobalDbDeclarationGridEditability();
        });
        void DiscardTag()
        {
            tagFormDirty = false;
            tagAddressValidation.Text = string.Empty;
            tagAddressValidation.Visible = false;
            if (selectedTagIndex >= 0) LoadTagControls();
            else OpenTagDocument(activeTagTableName);
        }
        void DiscardBlock()
        {
            // Restore this form independently: a sibling draft may legitimately suppress
            // RefreshEditor. Clearing the flag alone would leave discarded values applicable.
            blockFormDirty = false;
            var wasLoading = loadingFormControls; loadingFormControls = true;
            try
            {
                var block = document.Blocks.Count > 0 ? document.Blocks[document.ActiveBlockIndex] : null;
                blockName.Text = block?.Name ?? string.Empty;
                if (block is not null)
                    blockTypeSelector.Select(siemens ? block.BlockType switch
                    {
                        LadderBlockType.OrganizationBlock => 0,
                        LadderBlockType.FunctionBlock => 1,
                        LadderBlockType.Function => 2,
                        LadderBlockType.DataBlock => 3,
                        _ => 2
                    } : 0);
            }
            finally { loadingFormControls = wasLoading; }
            RefreshEditor();
        }
        void DiscardTask()
        {
            taskFormDirty = false;
            LoadTaskControls(selectedTaskIndex);
            UpdateGlobalDbDeclarationGridEditability();
        }
        void DiscardInterface()
        {
            ResetInterfaceForm();
            interfaceTable.DeselectAll();
        }
        cancelTagEdit.Pressed += () =>
        {
            DiscardTag();
            output.Text = "Tag changes discarded. No project data or history was changed.";
        };
        cancelBlock.Pressed += () =>
        {
            DiscardBlock();
            output.Text = "Block changes discarded. No project data or history was changed.";
        };
        cancelTask.Pressed += () =>
        {
            DiscardTask();
            output.Text = "Schedule changes discarded. No project data or history was changed.";
        };
        _pendingEditorForms.Add(new(() => tagFormDirty, () => { (selectedTagIndex >= 0 ? applyTagEdit! : addTag).EmitSignal(BaseButton.SignalName.Pressed); return !tagFormDirty; }, DiscardTag));
        _pendingEditorForms.Add(new(
            () => userConstantFormDirty,
            () =>
            {
                applyUserConstant.EmitSignal(BaseButton.SignalName.Pressed);
                return !userConstantFormDirty;
            },
            () => discardUserConstantForm?.Invoke()));
        _pendingEditorForms.Add(new(() => blockFormDirty, () => { applyBlock.EmitSignal(BaseButton.SignalName.Pressed); return !blockFormDirty; }, DiscardBlock));
        _pendingEditorForms.Add(new(() => taskFormDirty, () => { applyTask.EmitSignal(BaseButton.SignalName.Pressed); return !taskFormDirty; }, DiscardTask));
        _pendingEditorForms.Add(new(() => interfaceFormDirty, () => { addInterface.EmitSignal(BaseButton.SignalName.Pressed); return !interfaceFormDirty; }, DiscardInterface));
        foreach (var field in new[] { tagName, tagAddress, tagComment, tagInitialValue, tagEnumMembers })
            field.TextSubmitted += _ => (selectedTagIndex >= 0 ? applyTagEdit! : addTag).EmitSignal(BaseButton.SignalName.Pressed);
        blockName.TextSubmitted += _ => applyBlock.EmitSignal(BaseButton.SignalName.Pressed);
        taskName.TextSubmitted += _ => applyTask.EmitSignal(BaseButton.SignalName.Pressed);
        taskPeriod.GetLineEdit().TextSubmitted += _ => applyTask.EmitSignal(BaseButton.SignalName.Pressed);
        taskPriority.GetLineEdit().TextSubmitted += _ => applyTask.EmitSignal(BaseButton.SignalName.Pressed);
        interfaceName.TextSubmitted += _ => addInterface.EmitSignal(BaseButton.SignalName.Pressed);
        interfaceInitial.TextSubmitted += _ => addInterface.EmitSignal(BaseButton.SignalName.Pressed);
        void EscapeForm(Control field, Action discard)
        {
            field.GuiInput += input => { if (input is InputEventKey { Pressed: true, Keycode: Key.Escape }) { discard(); field.AcceptEvent(); } };
        }
        foreach (var field in new Control[] { tagName, tagTableMembership, tagAddress, tagComment, tagInitialValue, tagEnumMembers, tagType, tagRole, tagBinding, aggregateTagEditor }) EscapeForm(field, DiscardTag);
        foreach (var field in new Control[] { blockName, blockTypeSelector }) EscapeForm(field, DiscardBlock);
        foreach (var field in new Control[] { taskName, taskKind, taskPeriod, taskPriority, taskTarget }) EscapeForm(field, DiscardTask);
        foreach (var field in new Control[] { interfaceName, interfaceType, interfaceSection, interfaceInitial, interfaceEnumMembers, interfaceRetain, interfaceComment }) EscapeForm(field, DiscardInterface);
        var visibleValidationIssues = new List<LadderValidationIssue>();
        var visibleDiagnosticTargets = new List<EditorDiagnosticTarget?>();
        long validationRevision = -1;

        void ShowValidationIssues(IReadOnlyList<LadderValidationIssue> issues, bool openDock, bool verified = true)
        {
            validationRevision = document.ContentRevision;
            visibleValidationIssues.Clear();
            visibleValidationIssues.AddRange(issues);
            visibleDiagnosticTargets.Clear();
            visibleDiagnosticTargets.AddRange(issues.Select(issue => EditorDiagnosticTarget.Capture(document, issue.Path)));
            validationIssueList.Clear();
            foreach (var issue in issues)
            {
                var index = validationIssueList.ItemCount;
                validationIssueList.AddItem($"ERROR  {issue.Code}  ·  {issue.Path}  ·  {issue.Message}");
                validationIssueList.SetItemMetadata(index, issue.Path);
                validationIssueList.SetItemTooltip(index,
                    $"{issue.Code}\n{issue.Message}\nDocument path: {issue.Path}\nDouble-click to navigate.");
            }
            validationDetail.Text=issues.Count==0 ? "" : $"{issues[0].Code}: {issues[0].Message}";
            validationSummary.Text = issues.Count == 0
                ? verified ? "0 ERRORS · verification passed" : "NOT VERIFIED · run Verify + Load Offline"
                : $"{issues.Count} ERROR{(issues.Count == 1 ? string.Empty : "S")} · edit not loaded · double-click to navigate";
            validationSummary.AddThemeColorOverride("font_color",
                issues.Count == 0 ? new Color("18864b") : new Color("b3261e"));
            if (openDock)
            {
                ShowBottomDock();
                bottomPanel.Visible = true;
                reopenBottomDock.Visible = false;
                bottomTabs.CurrentTab = issues.Count == 0 ? 1 : 0;
            }
        }

        static int ValidationPathIndex(string path, string collection)
        {
            var marker = $".{collection}[";
            var start = path.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return -1;
            start += marker.Length;
            var end = path.IndexOf(']', start);
            return end > start && int.TryParse(path.AsSpan(start, end - start), out var index) ? index : -1;
        }

        void NavigateValidationIssue(int issueIndex)
        {
            if (issueIndex < 0 || issueIndex >= visibleValidationIssues.Count) return;
            if (validationRevision != document.ContentRevision)
            {
                output.Text = "[color=#d17a00]Diagnostic is stale.[/color] Run Verify + Load again before navigating.";
                return;
            }
            var issue = visibleValidationIssues[issueIndex];
            var blockIndex = ValidationPathIndex(issue.Path, "blocks");
            var networkIndex = ValidationPathIndex(issue.Path, "networks");
            var variableIndex = ValidationPathIndex(issue.Path, "variables");
            var taskIndex = ValidationPathIndex(issue.Path, "tasks");
            // Capture stable destinations before the resolver can apply edits or rebuild controls.
            var targetBlockId = blockIndex >= 0 && blockIndex < document.Blocks.Count ? document.Blocks[blockIndex].Id : "";
            var targetTagId = variableIndex >= 0 && variableIndex < document.Tags.Count ? LadderCallBindings.GlobalId(document.Tags[variableIndex]) : "";
            var targetTaskId = taskIndex >= 0 && taskIndex < document.Tasks.Count ? document.Tasks[taskIndex].Id : "";
            var diagnosticTarget = visibleDiagnosticTargets[issueIndex];
            ResolvePendingEditorForms(() =>
            {
            blockIndex = document.Blocks.FindIndex(block => block.Id == targetBlockId);
            variableIndex = document.Tags.FindIndex(tag => LadderCallBindings.GlobalId(tag) == targetTagId);
            taskIndex = document.Tasks.FindIndex(task => task.Id == targetTaskId);
            if (blockIndex >= 0 && blockIndex < document.Blocks.Count)
            {
                ShowProjectDock();
                if (networkIndex >= 0 && diagnosticTarget is null)
                { output.Text = "Diagnostic target is unavailable. Run Verify + Load again."; return; }
                SwitchEditorBlock(blockIndex, restoreView: false);
                blockItems[blockIndex].Select(0);
                organization.ScrollToItem(blockItems[blockIndex]);
                if (networkIndex >= document.Rungs.Count) return;
                selectedRung = networkIndex >= 0 ? networkIndex : 0;
                selectedBranch = 0;
                selectedContact = -1;
                selectedInsertionIndex = -1;
                selectedOutput = false;
                RefreshEditor();
                if (document.Rungs.Count > 0)
                {
                    if (diagnosticTarget is not null)
                    {
                        // Selecting another block invalidates context popups but
                        // this already-validated diagnostic keeps its captured IDs.
                        var exactTarget = diagnosticTarget.Target with { Revision = document.Revision };
                        if (!LadderEditorCommands.TryResolve(document, exactTarget, out var exactRung, out var exactBranch, out var exactNode, out _))
                        { output.Text = "Diagnostic target is unavailable. Revalidate the draft."; return; }
                        selectedRung = exactRung;
                        if (exactTarget.Role is EditorOperandRole.Contact or EditorOperandRole.Output)
                        {
                            canvas.SelectElement(exactRung, exactBranch, exactNode, exactTarget.Role == EditorOperandRole.Output);
                            OpenInstructionProperties();
                            Control? field = diagnosticTarget.OperandRole switch
                            {
                                "rightOperand" => compareRight,
                                "sourceA" => numericSourceA, "sourceB" => numericSourceB, "sourceC" => numericSourceC,
                                "destination" => numericDestination, "targetBlock" => callTarget,
                                "preset" => document.Rungs[exactRung].IsTimer ? timerPreset : counterPreset,
                                "variable" => exactTarget.Role == EditorOperandRole.Contact ? tagSelector : coilSelector,
                                _ => null
                            };
                            field?.GrabFocus();
                        }
                        else canvas.SelectRung(exactRung);
                    }
                    else canvas.SelectRung(selectedRung);
                    scroll.ScrollVertical = Math.Max(0, (int)canvas.GetRungBounds(selectedRung).Position.Y - 10);
                }
            }
            else if (variableIndex >= 0 && variableIndex < document.Tags.Count)
            {
                if (siemens) OpenTagDocument(null, variableIndex);
                else
                {
                    activeTagTableName = null;
                    RefreshEditor();
                    ShowProjectDock();
                    projectTabs.CurrentTab = 1;
                    SelectIndexedTreeRow(tagList, variableIndex);
                    tagList.ScrollToItem(tagList.GetSelected());
                }
            }
            else if (taskIndex >= 0 && taskIndex < document.Tasks.Count)
            {
                ShowProjectDock();
                LoadTaskControls(taskIndex);
                taskItems[taskIndex].Select(0);
                organization.ScrollToItem(taskItems[taskIndex]);
                if (!siemens) projectTabs.CurrentTab = 2;
            }
            output.Text = $"[color=#d64545]{Escape(issue.Code)}[/color] {Escape(issue.Message)}  [color=#657b86]{Escape(issue.Path)}[/color]";
            });
        }

        validationIssueList.ItemActivated += index => NavigateValidationIssue((int)index);
        apply.Pressed += () => ResolvePendingEditorForms(() =>
        {
            var valid = TryBuildCurrentLadderProgram(out var program, out var issues);
            ShowValidationIssues(issues, openDock: true);
            if (!valid)
            {
                output.Text = $"[color=#d64545]Verification failed.[/color] {issues.Count} error(s). Open Error List and double-click an item to navigate.";
                _ladderWorkspaceStatus.Text = _virtualProgram is null
                    ? "EDIT INVALID · NO CONTROLLER LOADED · SEE ERROR LIST"
                    : "EDIT INVALID · PREVIOUS CONTROLLER RETAINED · SEE ERROR LIST";
                _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("ef7777"));
                return;
            }
            var totalNetworks = program.Blocks?.Sum(block => block.Networks.Count) ?? program.Networks.Count;
            if (_externalMode)
            {
                output.Text = "[color=#d17a00]Verification complete; local controller NOT LOADED.[/color] Select Built-in Simulator to load this project into the offline runtime.";
                _ladderWorkspaceStatus.Text = "VALID DRAFT · EXTERNAL PLC MODE · LOCAL CONTROLLER NOT LOADED";
                _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("f1aa5b"));
                return;
            }
            output.Text = $"[color=#18864b]Verification complete.[/color] {program.Blocks?.Count ?? 1} blocks · {program.Tasks?.Count ?? 1} tasks · {totalNetworks} networks/rungs · {program.Variables.Count} tags · loaded into offline runtime.";
            _ladderWorkspaceStatus.Text = $"VALID · {tabName.ToUpperInvariant()} WORKFLOW · OFFLINE RUNTIME";
            _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("65d49a"));
            VirtualControllerProgramRequested?.Invoke(program);
        });
        instructionTree.ItemSelected += () =>
        {
            var kind = instructionTree.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            RenderInstructionHelp(kind, false);
            output.Text = kind switch
            {
                "no" => "Normally-open/XIC contact selected. Double-click to insert it using the selected tag and branch.",
                "nc" => "Normally-closed/XIO contact selected. Double-click to insert it using the selected tag and branch.",
                "edge-rising" => siemens ? "Positive-edge contact selected. It pulses for one scan after a FALSE-to-TRUE transition." : "XIC + ONS composite selected. It pulses for one scan after a FALSE-to-TRUE transition.",
                "edge-falling" => siemens ? "Negative-edge contact selected. It pulses for one scan after a TRUE-to-FALSE transition." : "XIO + ONS / OSF composite selected. It pulses for one scan after a TRUE-to-FALSE transition.",
                "coil" => "Output coil selected. Choose its writable tag in the Output coil field below the rung.",
                "set" => siemens ? "Set coil selected. Double-click to latch the selected BOOL when the rung becomes true." : "OTL selected. Double-click to latch the selected BOOL when the rung becomes true.",
                "reset" => siemens ? "Reset coil selected. Double-click to clear the selected BOOL when the rung becomes true." : "OTU selected. Double-click to unlatch the selected BOOL when the rung becomes true.",
                "ton" => "TON selected. Double-click to make the selected rung an on-delay timer instruction.",
                "tof" => "TOF selected. Double-click to make the selected rung an off-delay timer instruction.",
                "tp" => "TP selected. Double-click to make the selected rung a rising-edge pulse timer instruction.",
                "rto" => siemens ? "TONR selected. Double-click to accumulate enabled time retentively." : "RTO selected. Double-click to accumulate enabled time retentively.",
                "timer-reset" => siemens ? "RT selected. Double-click to reset the selected timer instance." : "Timer RES selected. Double-click to reset the selected timer instance.",
                "ctu" => "CTU selected. Double-click to count false-to-true transitions on the selected rung.",
                "ctd" => "CTD selected. Double-click to decrement once per false-to-true transition.",
                "counter-load" => "Counter load selected. Double-click to copy the preset into CV/ACC while the rung is true.",
                "counter-reset" => "Counter reset selected. Double-click to clear the selected counter while the rung is true.",
                "call" => siemens ? "FC/FB block call selected. In TIA, drag the named target from Program blocks onto its network." : "JSR selected. Double-click to call the target routine shown in the program toolbar.",
                "return" => siemens ? "RETURN selected. Double-click to exit the current block when the rung is true." : "RET selected. Double-click to return from the current routine when the rung is true.",
                "jump" => "JMP selected. Double-click to jump to a block-local label when the rung is true.",
                "label" => siemens ? "LABEL selected. Double-click to declare a block-local jump destination." : "LBL selected. Double-click to declare a routine-local jump destination.",
                var value when value.StartsWith("compare-", StringComparison.Ordinal) => "Comparison selected. Double-click to insert it using the operands shown below the editor.",
                var value when value.StartsWith("numeric-", StringComparison.Ordinal) => "Typed write instruction selected. Double-click to insert it using the source and destination fields.",
                "branch" => "Parallel branch selected. Double-click to add an OR path to the selected rung.",
                "rung" => siemens ? "Network selected. Double-click to insert a network." : "Rung selected. Double-click to insert a rung.",
                _ => "Choose a supported instruction. Double-click an instruction to insert it.",
            };
        };
        instructionTree.ItemActivated += () =>
        {
            var kind = instructionTree.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            if (kind == "no") addNo.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "nc") addNc.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "edge-rising") addRisingEdge.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "edge-falling") addFallingEdge.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "branch") addBranch.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "ton") addTon.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "tof") addTof.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "tp") addTp.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "rto") addRto.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "timer-reset") addTimerReset.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "ctu") addCounter.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "ctd") addCounterDown.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "counter-load") addCounterLoad.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "counter-reset") addCounterReset.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "call" && !siemens) addCall.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "return") addReturn.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "jump") addJump.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "label") addLabel.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind.StartsWith("compare-", StringComparison.Ordinal)
                     && int.TryParse(kind.AsSpan("compare-".Length), out var compareIndex))
            {
                compareOperator.Select(compareIndex);
                addCompare.EmitSignal(BaseButton.SignalName.Pressed);
            }
            else if (kind.StartsWith("numeric-", StringComparison.Ordinal)
                     && int.TryParse(kind.AsSpan("numeric-".Length), out var numericIndex))
            {
                var buttons = new[] { addMove, addAdd, addSubtract, addMultiply, addDivide,
                    addModulo, addAbsolute, addNegate, addSquareRoot, addExponentiate,
                    addNaturalLog, addSine, addCosine, addTangent, addArcSine, addArcCosine,
                    addArcTangent, addTruncate, addNormalize, addScale, addConvert, addRound,
                    addCeiling, addFloor, addFormatText };
                if (numericIndex >= 0 && numericIndex < buttons.Length)
                    buttons[numericIndex].EmitSignal(BaseButton.SignalName.Pressed);
            }
            else if (kind == "set") addSet.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "reset") addReset.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "rung") addRung.EmitSignal(BaseButton.SignalName.Pressed);
            else if (kind == "coil") SelectCoilMode(LadderCoilMode.Assign);
        };
        void SwitchEditorBlock(int blockIndex, bool restoreView)
        {
            if (blockIndex == document.ActiveBlockIndex) return;
            _blockViewRememberCount++;
            var leavingView = canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical);
            var traceView = OS.GetCmdlineUserArgs().Contains("--verify-editor-view-state", StringComparer.Ordinal);
            if (traceView) GD.Print($"BLOCK_VIEW_NAV remember={document.Blocks[document.ActiveBlockIndex].Id} state={leavingView} size={canvas.Size} page={scroll.Size}");
            blockViewMemory.Remember(document.Id, document.Blocks[document.ActiveBlockIndex].Id, leavingView);
            document.SelectBlock(blockIndex);
            selectedRung = selectedBranch = 0; selectedContact = selectedInsertionIndex = -1; selectedOutput = false;
            canvas.SelectRung(0); RefreshEditor();
            LadderEditorViewState? state = null;
            var restored = restoreView && blockViewMemory.TryRestore(document.Id, document.Blocks[blockIndex].Id, out state)
                && state is not null && canvas.RestoreViewState(state);
            if (restored)
            {
                selectedRung = canvas.SelectedRung; selectedBranch = canvas.SelectedBranch; selectedContact = canvas.SelectedContact;
                selectedInsertionIndex = canvas.SelectedInsertionIndex; selectedOutput = canvas.OutputSelected;
                RefreshEditor();
            }
            var blockId = document.Blocks[blockIndex].Id;
            var restoreRevision = document.Revision;
            var restoreSelection = canvas.CaptureViewState(0, 0);
            if (traceView) GD.Print($"BLOCK_VIEW_NAV restore={blockId} restored={restored} expected={state} actual={canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical)} size={canvas.Size} maxH={scroll.GetHScrollBar().MaxValue} maxV={scroll.GetVScrollBar().MaxValue}");
            Callable.From((Action)(async () =>
            {
                // Container sorting updates scrollbar ranges after the zoomed minimum size.
                // Restoring before that clamps valid saved offsets to the old range.
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!GodotObject.IsInstanceValid(canvas) || !canvas.IsInsideTree() || document.Revision != restoreRevision
                    || document.Blocks[document.ActiveBlockIndex].Id != blockId || canvas.CaptureViewState(0, 0) != restoreSelection) return;
                scroll.ScrollHorizontal = restored ? state!.HorizontalScroll : 0;
                scroll.ScrollVertical = restored ? state!.VerticalScroll : 0;
                if (traceView) GD.Print($"BLOCK_VIEW_NAV deferred={blockId} actual={canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical)} size={canvas.Size} maxH={scroll.GetHScrollBar().MaxValue} maxV={scroll.GetVScrollBar().MaxValue}");
                canvas.QueueRedraw();
            })).CallDeferred();
        }
        var restoringOrganizationSelection = false;
        void RestoreActiveOrganizationRow()
        {
            var expected = "program:" + document.ActiveBlockIndex;
            TreeItem? FindActive(TreeItem? item)
            {
                for (var current = item; current is not null; current = current.GetNext())
                {
                    if (current.GetMetadata(0).AsString() == expected) return current;
                    var nested = FindActive(current.GetFirstChild());
                    if (nested is not null) return nested;
                }
                return null;
            }
            var row = FindActive(organization.GetRoot());
            if (row is null) return;
            restoringOrganizationSelection = true;
            try { row.Select(0); }
            finally { restoringOrganizationSelection = false; }
        }
        organization.ItemSelected += () =>
        {
            if (restoringOrganizationSelection) return;
            var kind = organization.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            ResolvePendingEditorForms(() =>
            {
            if (kind.StartsWith("tag-table:", StringComparison.Ordinal))
            {
                var tableName = kind["tag-table:".Length..];
                if (siemens)
                {
                    output.Text = tableName.Length == 0
                        ? "Show all tags selected. Double-click to open the table in the editor."
                        : $"{Escape(tableName)} selected. Double-click to open the table in the editor.";
                    return;
                }
                activeTagTableName = tableName.Length == 0 ? null : tableName;
                selectedTagIndex = -1;
                tagFormDirty = false;
                loadingFormControls = true;
                tagName.Clear();
                tagType.Select(0);
                tagRole.Select((int)PlcVariableRole.Memory);
                tagInitialValue.Text = "FALSE";
                tagBinding.Clear();
                loadingFormControls = false;
                RefreshTagBindingOptions(PlcVariableType.Bool, PlcVariableRole.Memory, string.Empty);
                RefreshEditor();
                ShowProjectDock();
                if (!siemens) projectTabs.CurrentTab = 1;
                output.Text = activeTagTableName is null
                    ? $"Showing all {document.Tags.Count} PLC tags."
                    : $"[color=#18864b]Tag table selected.[/color] {Escape(activeTagTableName)} · {document.Tags.Count(tag => tag.TagTable.Equals(activeTagTableName, StringComparison.Ordinal))} tag(s).";
                return;
            }
            if (kind.StartsWith("program:", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan("program:".Length), out var blockIndex)
                && blockIndex >= 0 && blockIndex < document.Blocks.Count)
            {
                if (siemens)
                {
                    output.Text = $"{Escape(document.Blocks[blockIndex].Name)} selected. Double-click to open it in the program editor.";
                    return;
                }
                SwitchEditorBlock(blockIndex, restoreView: true);
                if (document.Blocks[blockIndex].BlockType == LadderBlockType.DataBlock)
                    output.Text = $"[color=#18864b]{Escape(document.Blocks[blockIndex].Name)} active.[/color] Global DB declarations are shown in the central document; edit members in the Project inspector.";
                else
                {
                    output.Text = $"[color=#18864b]{Escape(document.Blocks[blockIndex].Name)} active.[/color] Select a rung and use the instruction toolbar.";
                    canvas.GrabFocus();
                }
                return;
            }
            if (kind.StartsWith("task:", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan("task:".Length), out var taskIndex)
                && taskIndex >= 0 && taskIndex < document.Tasks.Count)
            {
                LoadTaskControls(taskIndex);
                var task = document.Tasks[taskIndex];
                if (!siemens) projectTabs.CurrentTab = 2;
                output.Text = $"[color=#18864b]{Escape(task.Name)} selected.[/color] Edit target, type, period, or priority in the schedule toolbar.";
                return;
            }
            if (kind.StartsWith("instance-db:", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan("instance-db:".Length), out var dbNumber))
            {
                output.Text = $"Instance DB %DB{dbNumber} selected. Double-click to open its definition.";
                return;
            }
            if (kind.StartsWith("watch-table:", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan("watch-table:".Length), out var watchIndex)
                && watchIndex >= 0 && watchIndex < document.WatchTables.Count)
            {
                output.Text = $"{Escape(document.WatchTables[watchIndex].Name)} selected. Double-click to open it in the watch-table workspace.";
                return;
            }
            switch (kind)
            {
                case "instance-dbs":
                    var dbCount = document.Blocks.SelectMany(block => block.Rungs)
                        .Where(rung => rung.IsTimer || rung.IsTimerReset || rung.IsCounter || rung.IsCounterReset || rung.IsCounterLoad)
                        .Select(rung => rung.IsTimer || rung.IsTimerReset ? rung.TimerInstanceDataBlock : rung.CounterInstanceDataBlock)
                        .Where(db => db is not null).Select(db => db!.Number).Distinct().Count();
                    output.Text = $"{dbCount} IEC instruction instance(s) use symbolic instance DB assignments. Select a DB to locate its timer/counter instruction.";
                    break;
                case "tags":
                    if (siemens)
                    {
                        if (tagOperandDetailView is not null)
                        {
                            ShowProjectDock();
                            projectTabs.CurrentTab = tagOperandDetailTabIndex;
                            projectHeading.Text = "PLC tag detail view";
                            addBlockFromTree.Visible = false;
                            output.Text = "PLC tag detail view opened. Drag a tag onto a compatible operand in the open LAD document.";
                        }
                        else
                        {
                            output.Text = "PLC tags folder selected. Double-click a tag table to open it in the program editor.";
                        }
                        break;
                    }
                    activeTagTableName = null;
                    selectedTagIndex = -1;
                    RefreshEditor();
                    ShowProjectDock();
                    output.Text = "All PLC tags selected. Choose a table in the project tree or create a tag in the active view.";
                    projectTabs.CurrentTab = 1;
                    tagName.GrabFocus();
                    break;
                case "diagnostics":
                    output.Text = OfflineDiagnosticStatus();
                    ShowBottomDock();
                    bottomPanel.Visible = true;
                    reopenBottomDock.Visible = false;
                    bottomTabs.CurrentTab = 1;
                    break;
                case "device":
                    ShowProjectDock();
                    if (!siemens) projectTabs.CurrentTab = 1;
                    tagBinding.GrabFocus();
                    output.Text = "[b]Simulator I/O configuration[/b] · symbolic scene bindings only; no hardware rack or physical addresses.";
                    break;
                case "watch":
                    ShowBottomDock();
                    output.Text = "[b]Watch table active.[/b] Add symbols, Verify + Load, then Run to monitor typed simulator values by scan.";
                    bottomPanel.Visible = true;
                    reopenBottomDock.Visible = false;
                    bottomTabs.CurrentTab = 2;
                    RefreshWatchTable(_virtualSnapshot);
                    break;
                case "blocks":
                    if (!siemens) projectTabs.CurrentTab = 2;
                    output.Text = $"Program organization contains {document.Blocks.Count} blocks/routines and {document.Blocks.Sum(block => block.Rungs.Count)} total networks/rungs.";
                    break;
                case "tasks":
                    if (!siemens) projectTabs.CurrentTab = 2;
                    output.Text = $"Scheduler contains {document.Tasks.Count} configured tasks/OBs. Lower numeric priority executes first.";
                    break;
                default:
                    output.Text = "Offline controller project selected.";
                    break;
            }
            }, RestoreActiveOrganizationRow);
        };
        void UndoLadderEdit()
        {
            if (!TryUndoLadderEdit(out var description)) return;
            output.Text = $"[color=#18864b]Undo complete.[/color] {Escape(description)}";
        }
        void RedoLadderEdit()
        {
            if (!TryRedoLadderEdit(out var description)) return;
            output.Text = $"[color=#18864b]Redo complete.[/color] {Escape(description)}";
        }
        undoEdit.Pressed += UndoLadderEdit;
        redoEdit.Pressed += RedoLadderEdit;
        projectMenu.GetPopup().IdPressed += id =>
        {
            if (id == 0)
            {
                RequestLadderAction(() =>
                {
                if (ChallengeMutationLocked) return;
                ChallengeDraftChanged?.Invoke();
                document.ResetProject(
                    "offline-controller",
                    siemens ? "Main" : "MainRoutine",
                    TimeSpan.FromMilliseconds(20));
                document.AddTag("input_1", PlcVariableRole.Input);
                document.AddTag("output_1", PlcVariableRole.Output);
                document.AddRung(siemens ? "New network" : "New rung", "output_1");
                _ladderProjectPath = null;
                _ladderSavedProjectJson = null;
                _ladderHistory.Clear();
                _ladderRungClipboard = null;
                _ladderContactClipboard = null; _ladderCommandClipboard = null;
                selectedRung = 0;
                selectedBranch = 0;
                selectedContact = -1;
                selectedInsertionIndex = 0;
                selectedOutput = false;
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                RefreshLadderMonitorMatch();
                _ladderWorkspaceStatus.Text = "NEW PROJECT · NOT VERIFIED · VERIFY + LOAD REQUIRED";
                _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("f1aa5b"));
                output.Text = "[color=#18864b]New offline Ladder project created.[/color] Add logic, then Verify + Load.";
                });
            }
            else if (id == 1) ShowLadderSaveDialog();
            else if (id == 3) ShowLadderSaveAsDialog();
            else if (id == 2) RequestLadderAction(() => _ladderLoadDialog.PopupCenteredRatio(0.72f));
        };
        editMenu.GetPopup().IdPressed += id =>
        {
            if (id == 0) addRung.EmitSignal(BaseButton.SignalName.Pressed);
            else if (id == 1) remove.EmitSignal(BaseButton.SignalName.Pressed);
            else if (id == 2) UndoLadderEdit();
            else if (id == 3) RedoLadderEdit();
            else if (id == 4) CopyLadderSelection();
            else if (id == 5) PasteLadderClipboard();
            else if (id == 6) DispatchEditorCommand(EditorCommandId.CopyNetwork);
            else if (id == 7) CutLadderSelection();
        };
        onlineMenu.GetPopup().IdPressed += id =>
        {
            if (id == 0) apply.EmitSignal(BaseButton.SignalName.Pressed);
            else if (id == 1) RunRequested?.Invoke();
            else if (id == 2) StopRequested?.Invoke();
            else if (id == 3) ResetRequested?.Invoke();
        };
        viewMenu.GetPopup().IdPressed += id =>
        {
            if (siemens && id is >= 7 and <= 14)
            {
                if (id is >= 7 and <= 9) _tiaOperandRepresentation = (TiaOperandRepresentation)(id - 7);
                else if (id is >= 10 and <= 12) _tiaTagInformationDisplay = (TiaTagInformationDisplay)(id - 10);
                else _tiaTagInformationPosition = id == 13
                    ? TiaTagInformationPosition.OnOperands : TiaTagInformationPosition.BelowNetwork;
                foreach (var ladderCanvas in _ladderCanvases)
                    ladderCanvas.SetTiaDisplaySettings(_tiaOperandRepresentation, _tiaTagInformationDisplay, _tiaTagInformationPosition);
                var popup = viewMenu.GetPopup();
                var selectedRepresentationId = 7 + (int)_tiaOperandRepresentation;
                var selectedInformationId = 10 + (int)_tiaTagInformationDisplay;
                var selectedPositionId = _tiaTagInformationPosition == TiaTagInformationPosition.OnOperands ? 13 : 14;
                foreach (var itemId in new[] { 7, 8, 9, 10, 11, 12, 13, 14 })
                {
                    var index = popup.GetItemIndex(itemId);
                    popup.SetItemChecked(index, itemId == selectedRepresentationId || itemId == selectedInformationId || itemId == selectedPositionId);
                }
                output.Text = $"[b]TIA view settings applied.[/b] Operand: {_tiaOperandRepresentation}; tag information: {_tiaTagInformationDisplay}; position: {_tiaTagInformationPosition}.";
                return;
            }
            if (id is 0 or 1) ShowProjectDock();
            if (id is 2 or 5) ShowBottomDock();
            if (id == 0)
            {
                mainItem.Select(0);
                organization.ScrollToItem(mainItem);
                projectTabs.CurrentTab = 0;
                output.Text = "Program editor active.";
            }
            else if (id == 1)
            {
                ResolvePendingEditorForms(() =>
                {
                    tagItem.Select(0);
                    organization.ScrollToItem(tagItem);
                    if (siemens) OpenTagDocument(null);
                    else
                    {
                        projectTabs.CurrentTab = 1;
                        tagName.GrabFocus();
                    }
                });
            }
            else if (id == 2)
            {
                diagnosticsItem.Select(0);
                organization.ScrollToItem(diagnosticsItem);
                bottomPanel.Visible = true;
                reopenBottomDock.Visible = false;
                bottomTabs.CurrentTab = 1;
                output.Text = OfflineDiagnosticStatus();
            }
            else if (id == 3)
            {
                ShowToolDock(1);
                searchQuery.GrabFocus();
                output.Text = "[b]Project Find / Cross-reference[/b] · searches every tag, block, task, rung, and instruction operand.";
            }
            else if (id == 4)
            {
                RenderInstructionHelp(currentHelpKey, true);
                output.Text = "[b]Instruction help[/b] · context follows the selected instruction and documents offline scan semantics.";
            }
            else if (id == 5)
            {
                bottomPanel.Visible = true;
                reopenBottomDock.Visible = false;
                bottomTabs.CurrentTab = 2;
                RefreshWatchTable(_virtualSnapshot);
                watchSymbol.GrabFocus();
                output.Text = $"[b]Watch table active.[/b] {OfflineDiagnosticStatus()}";
            }
            else if (id == 6) ShowToolDock(0);
        };
        toolsMenu.GetPopup().IdPressed += id =>
        {
            if (id == 0)
            {
                ResolvePendingEditorForms(() =>
                {
                    ShowProjectDock();
                    if (siemens) OpenTagDocument(null);
                    else projectTabs.CurrentTab = 1;
                    tagType.Select(0);
                    tagType.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
                    tagName.Clear();
                    tagInitialValue.Text = "FALSE";
                    tagName.GrabFocus();
                });
            }
            else if (id == 1)
            {
                RenderInstructionHelp(currentHelpKey, true);
                output.Text = "[b]Instruction help[/b] · purpose, operands, scan behavior, restrictions, and examples for the selected instruction.";
            }
            else
            {
                ShowToolDock(1);
                searchQuery.GrabFocus();
                output.Text = siemens
                    ? "[b]Cross-reference[/b] · enter an exact tag or block symbol to list declarations, reads, writes, calls, and schedules."
                    : "[b]Find All[/b] · enter a tag or routine to list declarations, reads, writes, JSRs, and task assignments.";
            }
        };
        void ActivateTiaProgramBlock(int blockIndex)
        {
            var block = document.Blocks[blockIndex];
            var blockCode = block.BlockType switch
            {
                LadderBlockType.OrganizationBlock => "OB",
                LadderBlockType.FunctionBlock => "FB",
                LadderBlockType.DataBlock => "DB",
                _ => "FC",
            };
            var key = BlockDocumentKey(block.Id);
            AddEditorDocumentTab(key, $"{block.Name} [{blockCode}]");
            ActivateEditorDocument(key);
            blockItems[blockIndex].Select(0);
            organization.ScrollToItem(blockItems[blockIndex]);
            if (block.BlockType == LadderBlockType.DataBlock)
                output.Text = $"[color=#18864b]{Escape(block.Name)} global DB opened.[/color] Edit its declarations in the Project inspector.";
            else
            {
                output.Text = $"[color=#18864b]{Escape(block.Name)} opened in the program editor.[/color]";
                canvas.GrabFocus();
            }
        }
        if (siemens)
            _tiaProgramBlockActivator = blockIndex => ResolvePendingEditorForms(() => ActivateTiaProgramBlock(blockIndex));
        organization.ItemActivated += () =>
        {
            if (restoringOrganizationSelection) return;
            var kind = organization.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            if (siemens && kind.StartsWith("instance-db:", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan("instance-db:".Length), out var instanceDbNumber))
            {
                ResolvePendingEditorForms(() => OpenInstanceDbDocument(instanceDbNumber));
                return;
            }
            if (siemens && kind.StartsWith("program:", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan("program:".Length), out var blockIndex)
                && blockIndex >= 0 && blockIndex < document.Blocks.Count)
            {
                _tiaProgramBlockActivator!(blockIndex);
                return;
            }
            if (siemens && kind.StartsWith("tag-table:", StringComparison.Ordinal))
            {
                var tableName = kind["tag-table:".Length..];
                ResolvePendingEditorForms(() => OpenTagDocument(tableName.Length == 0 ? null : tableName));
                return;
            }
            if (siemens && kind.StartsWith("watch-table:", StringComparison.Ordinal)
                && int.TryParse(kind.AsSpan("watch-table:".Length), out var watchIndex)
                && watchIndex >= 0 && watchIndex < document.WatchTables.Count)
            {
                ResolvePendingEditorForms(() => OpenWatchTableDocument(document.WatchTables[watchIndex]));
                return;
            }
            if (kind is not ("add-block" or "add-tag-table" or "add-watch-table")) return;
            ResolvePendingEditorForms(() =>
            {
                if (kind == "add-block") addBlock.EmitSignal(BaseButton.SignalName.Pressed);
                else if (kind == "add-tag-table") OpenTagTableCreation();
                else openWatchTableCreation();
            });
        };
        watchTable.ItemActivated += () =>
        {
            var name = watchTable.GetSelected()?.GetMetadata(0).AsString() ?? string.Empty;
            var index = document.Tags.FindIndex(tag => name == tag.Name || name.StartsWith(tag.Name + ".", StringComparison.Ordinal) || name.StartsWith(tag.Name + "[", StringComparison.Ordinal));
            if (index < 0) return;
            if (siemens) ResolvePendingEditorForms(() =>
            {
                activeTagTableName = null;
                OpenTagDocument(null, index);
                aggregateTagEditor.RevealMember(name, document.Tags[index].Name);
            });
            else
            {
                activeTagTableName = null;
                RefreshEditor();
                ShowProjectDock();
                projectTabs.CurrentTab = 1;
                SelectIndexedTreeRow(tagList, index);
                tagList.EmitSignal(Tree.SignalName.ItemSelected);
                tagList.ScrollToItem(tagList.GetSelected());
                aggregateTagEditor.RevealMember(name, document.Tags[index].Name);
            }
        };
        RefreshTagBindingOptions(SelectedTagType(), SelectedTagRole(SelectedTagType()), string.Empty);
        if (siemens)
        {
            var initialBlock = document.Blocks[0];
            var initialBlockKey = BlockDocumentKey(initialBlock.Id);
            AddEditorDocumentTab(initialBlockKey, $"{initialBlock.Name} [OB]");
            editorDocumentButtons[initialBlockKey].ButtonPressed = true;
        }
        _ladderEditorProjectReplacementResetters.Add(() =>
        {
            if (!siemens) return;
            foreach (var item in editorDocumentItems.Values.ToArray())
            {
                editorDocumentTabs.RemoveChild(item);
                item.QueueFree();
            }
            editorDocumentButtons.Clear();
            editorDocumentItems.Clear();
            editorTagDocuments.Clear();
            editorInstanceDbDocuments.Clear();
            editorWatchTableDocuments.Clear();
            activeTagTableName = null;
            selectedTagIndex = -1;

            if (document.Blocks.Count == 0)
            {
                activeEditorDocumentKey = string.Empty;
                SetEditorDocumentBody(string.Empty);
                return;
            }

            var activeBlockIndex = Math.Clamp(document.ActiveBlockIndex, 0, document.Blocks.Count - 1);
            var activeBlock = document.Blocks[activeBlockIndex];
            activeEditorDocumentKey = BlockDocumentKey(activeBlock.Id);
            var blockCode = activeBlock.BlockType switch
            {
                LadderBlockType.OrganizationBlock => "OB",
                LadderBlockType.FunctionBlock => "FB",
                LadderBlockType.DataBlock => "DB",
                _ => "FC",
            };
            AddEditorDocumentTab(activeEditorDocumentKey, $"{activeBlock.Name} [{blockCode}{activeBlockIndex + 1}]");
            editorDocumentButtons[activeEditorDocumentKey].ButtonPressed = true;
            SetEditorDocumentBody(activeEditorDocumentKey);
        });
        _ladderEditorRefreshers.Add(RefreshEditor);
        _ladderSceneDiagnosticResetters.Add(() =>
        {
            ShowValidationIssues([], openDock: false, verified: false);
            output.Text = "Scene ready. Run loads its supplied ladder; verify and load after editing.";
        });
        _ladderWatchRefreshers.Add(RefreshWatchTable);
        _ladderValidationRefreshers.Add(issues => ShowValidationIssues(issues, openDock: true));
        refreshEditorAfterInitialization = RefreshEditor;
        RefreshEditor();
        static void AddVisibleSplitGrip(SplitContainer split, string text)
        {
            foreach (var dragArea in split.GetDragAreaControls())
            {
                var grip = new Label
                {
                    Name = "VisibleResizeGrip",
                    Text = text,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                grip.AddThemeColorOverride("font_color", new Color("ffffff"));
                grip.AddThemeColorOverride("font_shadow_color", new Color("17242c"));
                grip.AddThemeConstantOverride("shadow_offset_x", 1);
                grip.AddThemeConstantOverride("shadow_offset_y", 1);
                grip.AddThemeFontSizeOverride("font_size", 12);
                grip.SetAnchorsPreset(Control.LayoutPreset.Center);
                grip.OffsetLeft = -14;
                grip.OffsetTop = -10;
                grip.OffsetRight = 14;
                grip.OffsetBottom = 10;
                dragArea.AddChild(grip);
            }
        }
        AddVisibleSplitGrip(work, "|||");
        AddVisibleSplitGrip(editorAndTasks, "|||");
        AddVisibleSplitGrip(editorAndInspector, "— —");
        return root;
    }

    private void BuildApplicationSettingsDialog()
    {
        var settings = LoadApplicationSettings();
        _mcpUiEnabled = new CheckButton
        {
            Text = "Enable RungProof MCP UI integration",
            ButtonPressed = settings.McpUiEnabled,
            TooltipText = "Controls RungProof's local MCP menu affordances; it does not unregister the external Codex MCP server.",
        };
        _showSafetyNotices = new CheckButton
        {
            Text = "Show offline safety boundary notices",
            ButtonPressed = settings.ShowSafetyNotices,
        };
        var body = new VBoxContainer { CustomMinimumSize = new Vector2(560, 0) };
        body.AddChild(new Label
        {
            Text = "These settings are local to this simulator UI. The MCP server remains simulation-only and never creates a physical PLC connection.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        body.AddChild(_mcpUiEnabled);
        body.AddChild(_showSafetyNotices);
        body.AddChild(new Label
        {
            Text = "Codex manages server registration. Use Codex MCP settings to unregister the server; this menu controls only RungProof UI behavior.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        });
        _applicationSettingsDialog = new AcceptDialog
        {
            Name = "ApplicationSettingsDialog",
            Title = "MCP & Application Settings",
            DialogText = string.Empty,
            Exclusive = true,
        };
        _applicationSettingsDialog.AddChild(body);
        _applicationSettingsDialog.Confirmed += SaveApplicationSettings;
        AddChild(_applicationSettingsDialog);
        SetMcpUiEnabled(_mcpUiEnabled.ButtonPressed, persist: false);
        AddReducedMotionSetting(body, settings.ReducedMotion);
    }

    public bool IsExternalMode => _externalMode;

    private void BuildExternalPlcDialog()
    {
        _externalProfileSelector = new OptionButton
        {
            Name = "ExternalProfileSelector",
            CustomMinimumSize = new Vector2(420, 34),
        };
        _externalProfileSelector.AddItem("scene-1-db14-interface.json");
        _externalProfileSelector.AddItem("scene-2-db14-pusher-interface.json");
        _externalProfileSelector.ItemSelected += _ =>
        {
            InvalidateExternalApproval();
            RefreshExternalProfileDetails();
        };
        _externalProfileDetails = new RichTextLabel
        {
            Name = "ExternalProfileDetails",
            BbcodeEnabled = true,
            FitContent = false,
            SizeFlagsVertical = Control.SizeFlags.Fill,
            CustomMinimumSize = new Vector2(700, 260),
        };
        _externalAuthorization = new CheckButton
        {
            Name = "ExternalAuthorization",
            Text = "I verified the active CPU, rack/slot, profile, and exact write scope.",
        };

        var body = new VBoxContainer { Name = "ExternalPlcBody", CustomMinimumSize = new Vector2(760, 0) };
        body.AddChild(new Label
        {
            Text = "The External PLC mode uses the existing guarded S7 runtime and the selected validated profile. It does not modify the PLC project or safety logic.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            // Godot 4.7 autowrap needs a bounded width before the first layout.
            // Otherwise this initially zero-width label inflates the dialog.
            CustomMaximumSize = new Vector2(1040, -1),
        });
        body.AddChild(ConnectionSettingRow("Profile", _externalProfileSelector));
        body.AddChild(_externalProfileDetails);
        body.AddChild(_externalAuthorization);
        var actions = new HBoxContainer();
        var verify = ToolbarButton("VerifyExternalProfile", "VERIFY PROFILE", new Color("3c5968"));
        var diagnostic = ToolbarButton("RunExternalDiagnostic", "READ-ONLY PLC TEST", new Color("3c5968"));
        var connect = ToolbarButton("ConnectExternalPlc", "CONNECT EXTERNAL PLC", new Color("276b89"));
        var disconnect = ToolbarButton("DisconnectExternalPlc", "DISCONNECT", new Color("6d3d42"));
        actions.AddChild(verify);
        actions.AddChild(diagnostic);
        actions.AddChild(connect);
        actions.AddChild(disconnect);
        body.AddChild(actions);
        _externalPlcDialog = new AcceptDialog
        {
            Name = "ExternalPlcDialog",
            Title = "External PLC Settings / Tests",
            DialogText = string.Empty,
            Exclusive = true,
            MinSize = new Vector2I(820, 620),
        };
        _externalPlcDialog.OkButtonText = "CLOSE";
        _externalPlcDialog.AddChild(body);
        verify.Pressed += RefreshExternalProfileDetails;
        diagnostic.Pressed += RunExternalDiagnostic;
        connect.Pressed += ConnectExternalPlc;
        disconnect.Pressed += DisconnectExternalPlc;
        AddChild(_externalPlcDialog);
        RefreshExternalProfileDetails();
    }

    private static Control ConnectionSettingRow(string label, Control editor)
    {
        var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 38) };
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(120, 0), VerticalAlignment = VerticalAlignment.Center });
        row.AddChild(editor);
        return row;
    }

    public void ShowExternalPlcDialog()
    {
        if (RejectChallengeExternalOperation()) return;
        if (RuntimeBuildPolicy.IsOfflineDemo)
        {
            SetWorkspaceStatus("Offline demo · external PLC transport is disabled");
            return;
        }
        SetExternalMode(true);
        RefreshExternalProfileDetails();
        var viewport = GetViewport().GetVisibleRect().Size;
        _externalPlcDialog.PopupCentered(new Vector2I(
            (int)Math.Min(viewport.X - 48, 1040), (int)Math.Min(viewport.Y - 80, 620)));
    }

    private void SetExternalMode(bool enabled)
    {
        // Choosing the already-active local source must not retire a challenge or disconnect it.
        if (IsChallengeDraftActive && !enabled && !_externalMode) return;
        if (RejectChallengeExternalOperation()) return;
        if (enabled && RuntimeBuildPolicy.IsOfflineDemo)
        {
            SetWorkspaceStatus("Offline demo · built-in simulator remains selected");
            return;
        }
        if (!enabled && _connection is ExternalPlcRuntimeClient external
            && external.State != ConnectionState.Disconnected)
        {
            external.Disconnect();
        }
        var modeChanged = _externalMode != enabled;
        if (modeChanged) InvalidateExternalApproval();
        _externalMode = enabled;
        if (modeChanged) ControllerModeChanged?.Invoke(enabled);
        if (modeChanged) RecordOperatorEvent(enabled ? "External PLC source selected" : "Built-in simulator source selected");
        var popup = GetNodeOrNull<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlcMenu")?.GetPopup();
        if (popup is not null)
        {
            popup.SetItemChecked(popup.GetItemIndex(10), !enabled);
            popup.SetItemChecked(popup.GetItemIndex(11), enabled);
        }
        _workspaceStatus.Text = enabled
            ? "EXTERNAL PLC MODE · select a matching profile and connect before RUN"
            : "BUILT-IN SIMULATOR MODE · local virtual controller owns the scene";
        _workspaceStatus.AddThemeColorOverride("font_color", enabled ? new Color("f1aa5b") : new Color("65d49a"));
        RefreshConnection();
    }

    private void RefreshExternalProfileDetails()
    {
        if (RejectChallengeExternalOperation()) return;
        if (_externalProfileDetails is null) return;
        var profile = _externalProfileSelector.GetItemText(_externalProfileSelector.Selected);
        if (_activeScene is null)
        {
            _externalProfileDetails.Text = "[color=#f1aa5b]No scene is loaded.[/color]";
            return;
        }
        if (!string.Equals(_activeScene.PlcTestProfile, profile, StringComparison.OrdinalIgnoreCase))
        {
            _externalProfileDetails.Text = $"[color=#f1aa5b]PROFILE MISMATCH[/color]\nActive scene: {Escape(_activeScene.Id)}\nRequired profile: {Escape(_activeScene.PlcTestProfile ?? "none")}\nSelected: {Escape(profile)}\n\nSelect the profile declared by the scene before connecting.";
            return;
        }
        if (_connection is not ExternalPlcRuntimeClient external)
        {
            _externalProfileDetails.Text = "[color=#ef7777]External runtime bridge is unavailable in this build.[/color]";
            return;
        }
        if (external.IsBusy)
        {
            _externalProfileDetails.Text = "Bridge request in progress…";
            return;
        }
        var generation = _externalSelectionGeneration;
        var scene = _activeScene;
        _externalProfileDetails.Text = "Validating the local profile…";
        external.DescribeProfile(profile, descriptor =>
        {
            if (!CanCompleteExternalRequest(generation)) return;
            ExternalSceneContract.Validate(scene.Simulation, descriptor);
            if (_verifiedExternalDescriptor is JsonElement previous && previous.GetRawText() != descriptor.GetRawText())
                _externalAuthorization.ButtonPressed = false;
            _verifiedExternalDescriptor = descriptor;
            _externalProfileDetails.Text = ExternalProfilePresentation.Format(descriptor);
        }, exception =>
        {
            if (!CanCompleteExternalRequest(generation)) return;
            _verifiedExternalDescriptor = null;
            _externalAuthorization.ButtonPressed = false;
            _externalProfileDetails.Text = $"[color=#ef7777]PROFILE TEST FAILED[/color]\n{Escape(exception.Message)}";
        });
    }

    public bool VerifyExternalDialogBounds(out string result)
    {
        var body = _externalPlcDialog.GetNode<VBoxContainer>("ExternalPlcBody");
        var viewport = GetViewport().GetVisibleRect().Size;
        var size = _externalPlcDialog.Size;
        var actions = (Control)body.GetChild(body.GetChildCount() - 1);
        var controlsFit = _externalAuthorization.GetGlobalRect().End.Y <= size.Y
            && actions.GetGlobalRect().End.Y <= size.Y && _externalPlcDialog.GetOkButton().GetGlobalRect().End.Y <= size.Y;
        var profileValid = _verifiedExternalDescriptor is not null && _externalProfileDetails.Text.Contains("PROFILE VALID", StringComparison.Ordinal);
        var profileFailure = profileValid ? string.Empty : _externalProfileDetails.Text.Replace('\n', ' ');
        result = $"window={size} viewport={viewport} controlsFit={controlsFit} profileValid={profileValid} connection={_connection.State} profileDetail={profileFailure}";
        return size.X <= viewport.X && size.Y <= viewport.Y && controlsFit && profileValid && _connection.State == ConnectionState.Disconnected;
    }

    private void RunExternalDiagnostic()
    {
        if (RejectChallengeExternalOperation()) return;
        if (_connection is not ExternalPlcRuntimeClient external)
        {
            _externalProfileDetails.Text = "[color=#ef7777]External runtime bridge is unavailable in this build.[/color]";
            return;
        }
        var profile = _externalProfileSelector.GetItemText(_externalProfileSelector.Selected);
        if (external.State != ConnectionState.Disconnected)
        {
            _externalProfileDetails.Text = "Disconnect the active session before running a separate read-only PLC test.";
            return;
        }
        if (!CanUseExternalProfile(profile) || external.IsBusy) return;
        var generation = _externalSelectionGeneration;
        _externalProfileDetails.Text = "Read-only PLC test in progress…";
        external.RunReadOnlyDiagnostic(profile, result =>
        {
            if (!CanCompleteExternalRequest(generation)) return;
            var lines = result.GetProperty("items").EnumerateArray().Select(item =>
            {
                var status = item.GetProperty("status").GetString()?.ToUpperInvariant() ?? "INFO";
                var label = item.GetProperty("label").GetString() ?? string.Empty;
                var detail = item.GetProperty("detail").GetString() ?? string.Empty;
                return $"[{status}] {Escape(label)} · {Escape(detail)}";
            });
            _externalProfileDetails.Text =
                $"[b]READ-ONLY PLC TEST[/b]\n{Escape(result.GetProperty("summary").GetString() ?? "No result.")}\n\n" +
                string.Join("\n", lines);
        }, exception =>
        {
            if (!CanCompleteExternalRequest(generation)) return;
            _externalProfileDetails.Text = $"[color=#ef7777]READ-ONLY PLC TEST FAILED[/color]\n{Escape(exception.Message)}";
        });
    }

    private void ConnectExternalPlc()
    {
        if (RejectChallengeExternalOperation()) return;
        if (_activeScene is null || _connection is not ExternalPlcRuntimeClient external)
        {
            SetWorkspaceStatus("External PLC connection unavailable · no matching scene/runtime", isError: true);
            return;
        }
        var profile = _externalProfileSelector.GetItemText(_externalProfileSelector.Selected);
        if (!CanUseExternalProfile(profile) || external.IsBusy) return;
        if (!_externalAuthorization.ButtonPressed)
        {
            _externalProfileDetails.Text = "[color=#f1aa5b]Connection requires exact-scope authorization.[/color]\n\n" + _externalProfileDetails.Text;
            return;
        }
        if (_verifiedExternalDescriptor is not JsonElement verified)
        {
            _externalProfileDetails.Text = "Verify the matching local profile before authorizing a connection.";
            return;
        }
        var generation = _externalSelectionGeneration;
        // Re-read the local profile before connecting. A file edit invalidates
        // approval just as a scene/profile selection change does.
        external.DescribeProfile(profile, descriptor =>
        {
            if (!CanCompleteExternalRequest(generation) || !_externalAuthorization.ButtonPressed) return;
            ExternalSceneContract.Validate(_activeScene!.Simulation, descriptor);
            if (verified.GetRawText() != descriptor.GetRawText())
            {
                InvalidateExternalApproval();
                _externalProfileDetails.Text = "Profile changed after verification. Verify and authorize its new scope.";
                return;
            }
            var scope = descriptor.GetProperty("writeScope").EnumerateArray()
                .Select(item => item.Clone())
                .ToArray();
            if (RejectChallengeExternalOperation()) return;
            SetExternalMode(true);
            var connectionGeneration = _externalSelectionGeneration;
            external.Connect(profile, _activeScene.Id, scope, _ =>
            {
                if (!CanCompleteExternalRequest(connectionGeneration)) return;
                SetWorkspaceStatus("EXTERNAL PLC CONNECTED · waiting for guarded readiness");
                _externalPlcDialog.Hide();
            }, exception => { if (CanCompleteExternalRequest(connectionGeneration)) ReportExternalConnectionFailure(exception); }, descriptor.GetProperty("connectTimeoutMs").GetInt32(), descriptor);
        }, exception => { if (CanCompleteExternalRequest(generation)) ReportExternalConnectionFailure(exception); });
    }

    private bool RejectChallengeExternalOperation()
    {
        if (!IsChallengeDraftActive) return false;
        // Invalidate descriptors even if a dialog/request began before challenge entry.
        // No transport operation is invoked by this rejection path.
        _externalSelectionGeneration++;
        _verifiedExternalDescriptor = null;
        if (_externalAuthorization is not null) _externalAuthorization.ButtonPressed = false;
        _externalPlcDialog?.Hide();
        const string guidance = "Leave the offline challenge before selecting external PLC mode, settings, diagnostics or connection.";
        if (_externalProfileDetails is not null) _externalProfileDetails.Text = guidance;
        SetWorkspaceStatus(guidance);
        return true;
    }

    private bool CanCompleteExternalRequest(int generation)
        => !RejectChallengeExternalOperation() && generation == _externalSelectionGeneration;

    /// <summary>Opt-in native source-boundary probe: every path rejects before a bridge call.</summary>
    public bool VerifyChallengeExternalGuards(out string diagnostic)
    {
        if (!IsChallengeDraftActive || _externalMode || _connection.State != ConnectionState.Disconnected)
        { diagnostic = "requires active local challenge with disconnected transport"; return false; }
        var modeEvents = 0;
        void CountMode(bool _) => modeEvents++;
        ControllerModeChanged += CountMode;
        try
        {
            var generation = _externalSelectionGeneration;
            SetExternalMode(false);
            var sameLocalNoOp = _externalSelectionGeneration == generation && modeEvents == 0;
            _verifiedExternalDescriptor = JsonSerializer.SerializeToElement(new { verified = true });
            _externalAuthorization.ButtonPressed = true;
            SetExternalMode(true);
            ShowExternalPlcDialog();
            RefreshExternalProfileDetails();
            RunExternalDiagnostic();
            ConnectExternalPlc();
            var callbackRejected = !CanCompleteExternalRequest(generation);
            var blocked = !_externalMode && modeEvents == 0 && !_externalPlcDialog.Visible
                && _verifiedExternalDescriptor is null && !_externalAuthorization.ButtonPressed
                && _connection.State == ConnectionState.Disconnected
                && (_connection is not ExternalPlcRuntimeClient external || !external.IsBusy);
            diagnostic = $"sameLocalNoOp={sameLocalNoOp} externalPathsBlocked={blocked} staleCallbackRejected={callbackRejected} modeEvents={modeEvents}; no bridge operation requested";
            return sameLocalNoOp && blocked && callbackRejected;
        }
        finally { ControllerModeChanged -= CountMode; }
    }

    private void ReportExternalConnectionFailure(Exception exception)
    {
        SetWorkspaceStatus($"External PLC connection failed · {exception.Message}", isError: true);
        RefreshConnection();
    }

    private bool CanUseExternalProfile(string profile)
    {
        if (RejectChallengeExternalOperation()) return false;
        if (_activeScene is null || !string.Equals(_activeScene.PlcTestProfile, profile, StringComparison.OrdinalIgnoreCase))
        {
            _externalProfileDetails.Text = "[color=#ef7777]PROFILE MISMATCH · select the profile declared by the active scene.[/color]";
            _externalAuthorization.ButtonPressed = false;
            return false;
        }
        return true;
    }

    private void InvalidateExternalApproval()
    {
        _externalSelectionGeneration++;
        _verifiedExternalDescriptor = null;
        if (_externalAuthorization is not null) _externalAuthorization.ButtonPressed = false;
        if (_connection is ExternalPlcRuntimeClient external && (external.IsBusy || external.State != ConnectionState.Disconnected))
            external.Disconnect();
    }

    private void DisconnectExternalPlc()
    {
        if (_connection is ExternalPlcRuntimeClient external) external.Disconnect();
        SetWorkspaceStatus("External PLC disconnected");
        RefreshConnection();
    }

    public bool VerifyExternalProfileGuards(out string result)
    {
        if (RuntimeBuildPolicy.IsOfflineDemo)
        {
            var popup = GetNode<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlcMenu").GetPopup();
            // Deliberately emit the disabled actions too: presentation alone
            // must not be the policy boundary or stop local simulation.
            popup.EmitSignal(PopupMenu.SignalName.IdPressed, 11);
            popup.EmitSignal(PopupMenu.SignalName.IdPressed, 12);
            ConnectExternalPlc();
            RunExternalDiagnostic();
            var blocked = _connection is DisconnectedRuntimeClient && !_externalMode
                && !_externalPlcDialog.Visible && _connection.State == ConnectionState.Disconnected
                && popup.IsItemDisabled(popup.GetItemIndex(11)) && popup.IsItemDisabled(popup.GetItemIndex(12));
            result = $"offlineBuildBlocked={blocked} sourceRemainsLocal={!_externalMode}";
            return blocked;
        }
        if (_activeScene is null || _connection is not ExternalPlcRuntimeClient external || external.IsBusy)
        {
            result = "guard verification requires an idle offline scene";
            return false;
        }
        var selected = _externalProfileSelector.Selected;
        var details = _externalProfileDetails.Text;
        var wrong = _activeScene.PlcTestProfile == _externalProfileSelector.GetItemText(0) ? 1 : 0;
        _externalProfileSelector.Select(wrong);
        _externalAuthorization.ButtonPressed = true;
        ConnectExternalPlc();
        var mismatchBlocked = !external.IsBusy && external.State == ConnectionState.Disconnected
            && !_externalAuthorization.ButtonPressed && _externalProfileDetails.Text.Contains("PROFILE MISMATCH", StringComparison.Ordinal);
        _externalAuthorization.ButtonPressed = true;
        _verifiedExternalDescriptor = JsonSerializer.SerializeToElement(new { verified = true });
        _externalProfileSelector.EmitSignal(OptionButton.SignalName.ItemSelected, wrong);
        var selectionClearsApproval = !_externalAuthorization.ButtonPressed && _verifiedExternalDescriptor is null && !external.IsBusy;
        RunExternalDiagnostic();
        var diagnosticMismatchBlocked = !external.IsBusy;
        _externalProfileSelector.Select(selected);
        _externalProfileDetails.Text = details;
        result = $"mismatchBlocked={mismatchBlocked} selectionClearsApproval={selectionClearsApproval} diagnosticMismatchBlocked={diagnosticMismatchBlocked} noTransport=True";
        return mismatchBlocked && selectionClearsApproval && diagnosticMismatchBlocked;
    }

    private void SetMcpUiEnabled(bool enabled, bool persist = true)
    {
        _mcpUiEnabled.ButtonPressed = enabled;
        var index = _toolsPopup.GetItemIndex(McpToggleMenuId);
        if (index >= 0) _toolsPopup.SetItemText(index, $"MCP Integration: {(enabled ? "Enabled" : "Disabled")}");
        if (persist) SaveApplicationSettings();
    }

    private void SaveApplicationSettings()
    {
        var payload = new Godot.Collections.Dictionary
        {
            ["schemaVersion"] = 1,
            ["mcpUiEnabled"] = _mcpUiEnabled.ButtonPressed,
            ["showSafetyNotices"] = _showSafetyNotices.ButtonPressed,
            ["reducedMotion"] = ReducedMotion,
        };
        using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Write);
        file?.StoreString(Json.Stringify(payload, "  "));
        SetMcpUiEnabled(_mcpUiEnabled.ButtonPressed, persist: false);
    }

    private static (bool McpUiEnabled, bool ShowSafetyNotices, bool ReducedMotion) LoadApplicationSettings()
    {
        if (!FileAccess.FileExists(SettingsPath)) return (true, true, true);
        try
        {
            using var file = FileAccess.Open(SettingsPath, FileAccess.ModeFlags.Read);
            var parsed = Json.ParseString(file?.GetAsText() ?? string.Empty).AsGodotDictionary();
            return (
                parsed.ContainsKey("mcpUiEnabled") ? parsed["mcpUiEnabled"].AsBool() : true,
                parsed.ContainsKey("showSafetyNotices") ? parsed["showSafetyNotices"].AsBool() : true,
                parsed.ContainsKey("reducedMotion") ? parsed["reducedMotion"].AsBool() : true);
        }
        catch
        {
            return (true, true, true);
        }
    }

    public bool IsLadderView => _productView is "ladder" or "split";

    public bool VerifyWorkspaceLoadMenuBinding(out string result)
    {
        var popup = GetNodeOrNull<MenuButton>(
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/FileMenu")?.GetPopup();
        var feedback = GetNodeOrNull<AcceptDialog>("WorkspaceFeedbackDialog");
        if (popup is null || feedback is null)
        {
            result = "File menu or workspace feedback dialog is unavailable";
            return false;
        }
        feedback.Hide();
        // Verifies that the visible File-menu item is connected to the
        // operator load action. Native mouse/keyboard verification requires
        // an interactive window; headless Godot has no real input surface.
        popup.EmitSignal(PopupMenu.SignalName.IdPressed, 1L);
        var invoked = feedback.Visible;
        feedback.Hide();
        result = invoked
            ? "File -> Load Last Workspace invoked and displayed feedback"
            : "File -> Load Last Workspace produced no visible feedback";
        return invoked;
    }

    public bool VerifyExternalPlcMenu(out string result)
    {
        var popup = GetNodeOrNull<MenuButton>(
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/PlcMenu")?.GetPopup();
        if (popup is null)
        {
            result = "PLC menu is unavailable";
            return false;
        }
        var labels = Enumerable.Range(0, popup.ItemCount)
            .Select(popup.GetItemText)
            .ToHashSet(StringComparer.Ordinal);
        var passed = labels.Contains("Built-in Simulator")
            && labels.Contains("External PLC")
            && labels.Contains("External PLC Settings / Tests…");
        result = passed
            ? "PLC menu exposes Built-in Simulator, External PLC, and External PLC Settings / Tests"
            : "PLC menu is missing one or more runtime mode entries";
        return passed;
    }

    public bool VerifyScenarioMenuSelection(out string result)
    {
        var popup = GetNodeOrNull<MenuButton>(
            "Workspace/Toolbar/ToolbarMargin/ToolbarRow/SceneMenu")?.GetPopup();
        if (popup is null)
        {
            result = "Scenario menu is unavailable";
            return false;
        }
        // Exercise each real menu binding; checking the authored files alone
        // cannot detect a later menu item loading another scene's editor draft.
        var verified = 0;
        // Leave Demo 1 active, as before, for the subsequent cross-scene open check.
        foreach (var expected in AuthoredDemos.Skip(1).Concat(AuthoredDemos.Take(1)))
        {
            var index = Enumerable.Range(0, popup.ItemCount)
                .FirstOrDefault(item => popup.GetItemText(item) == expected.Label, -1);
            if (index < 0)
            {
                result = $"Scenario menu item is missing: {expected.Label}";
                return false;
            }
            popup.EmitSignal(PopupMenu.SignalName.IdPressed, popup.GetItemId(index));
            if (_activeScene?.Id != expected.SceneId
                || _ladderDocument.SourceSceneId != expected.SceneId
                || _sceneTitle.Text != expected.Label)
            {
                result = $"Scenario menu mismatch: {expected.Label}; scene={_activeScene?.Id ?? "<none>"} editor={_ladderDocument.SourceSceneId} title={_sceneTitle.Text}";
                return false;
            }
            var program = _ladderDocument.BuildProgram();
            var compile = LadderCompiler.Compile(program);
            var bindingIssues = SceneIoBindingValidator.Validate(program, ScenePoints());
            if (!compile.IsValid || bindingIssues.Count > 0
                || !AuthoredDemoLadderPrograms.TryCreate(expected.SceneId, out var authored)
                || LadderEditorProjectJson.Save(_ladderDocument) != LadderEditorProjectJson.Save(authored))
            {
                result = $"Scenario menu loaded an invalid or different demo draft: {expected.Label}; compile={compile.IsValid} bindingIssues={bindingIssues.Count}";
                return false;
            }
            verified++;
        }
        result = $"demoSelections={verified}/{AuthoredDemos.Length} scene/editor/title/program/interface match";
        return verified == AuthoredDemos.Length;
    }

    public void ShowLadderSaveDialog() => ResolvePendingEditorForms(() =>
    {
        if (_ladderProjectPath is null) _ladderSaveDialog.PopupCenteredRatio(0.72f);
        else { TrySaveLadderProject(_ladderProjectPath, out var message); _ladderWorkspaceStatus.Text = message; }
    });

    public void ShowLadderSaveAsDialog() => ResolvePendingEditorForms(() => _ladderSaveDialog.PopupCenteredRatio(0.72f));

    public void ShowLadderLoadDialog() => ResolvePendingEditorForms(() => RequestLadderAction(() => _ladderLoadDialog.PopupCenteredRatio(0.72f)));

    public bool TryUndoLadderEdit(out string description)
    {
        description = string.Empty;
        if (ChallengeMutationLocked || !_ladderHistory.CanUndo) return false;
        ChallengeDraftChanged?.Invoke();
        if (!_ladderHistory.Undo(_ladderDocument, out description)) return false;
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        foreach (var refresh in _ladderWatchRefreshers) refresh(_virtualSnapshot);
        _restoreActiveWatchTableTreeRow?.Invoke();
        RefreshLadderMonitorMatch();
        _ladderWorkspaceStatus.Text = $"UNDO · {description.ToUpperInvariant()} · OFFLINE";
        _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("65d49a"));
        return true;
    }

    public bool TryRedoLadderEdit(out string description)
    {
        description = string.Empty;
        if (ChallengeMutationLocked || !_ladderHistory.CanRedo) return false;
        ChallengeDraftChanged?.Invoke();
        if (!_ladderHistory.Redo(_ladderDocument, out description)) return false;
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        foreach (var refresh in _ladderWatchRefreshers) refresh(_virtualSnapshot);
        _restoreActiveWatchTableTreeRow?.Invoke();
        RefreshLadderMonitorMatch();
        _ladderWorkspaceStatus.Text = $"REDO · {description.ToUpperInvariant()} · OFFLINE";
        _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("65d49a"));
        return true;
    }

    public void ShowLadderInstructionHelp()
    {
        var environment = _ladderEnvironmentTabs.CurrentTab == 1 ? "Studio 5000" : "TIA Portal";
        var path = $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{environment}/Workbench/EditorAndTasks/ToolDockHost/InstructionAndTags/ToolBody/ToolTabsHost/ToolTabs";
        var tabs = GetNodeOrNull<TabContainer>(path);
        if (tabs is not null) tabs.CurrentTab = 2;
    }

    private Control BuildVirtualControllerPanel()
    {
        var scroll = new ScrollContainer
        {
            Name = "Virtual Controller",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        var body = new VBoxContainer
        {
            Name = "VirtualControllerBody",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        body.AddThemeConstantOverride("separation", 8);
        scroll.AddChild(body);
        body.AddChild(SectionLabel("IEC 61131-3-ALIGNED SUBSET · OFFLINE"));
        _virtualControllerStatus = Heading(
            "DISABLED · NO PHYSICAL PLC CONNECTED",
            12,
            new Color("f1aa5b"));
        _virtualControllerStatus.Name = "VirtualControllerStatus";
        _virtualControllerStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.AddChild(_virtualControllerStatus);
        var warning = Inspector("VirtualControllerWarning");
        warning.FitContent = true;
        warning.Text = "[color=#f1aa5b]Simulation only.[/color] Not safety validation, commissioning proof, vendor-equivalent execution, or real PLC timing evidence.";
        body.AddChild(warning);

        var loadDemo = ToolbarButton("LoadDemoLadderButton", "LOAD DEMO LADDER", new Color("1f8a58"));
        loadDemo.Pressed += () => VirtualControllerDemoRequested?.Invoke();
        body.AddChild(loadDemo);
        var inputRow = new HBoxContainer { Name = "SimulatedInputs" };
        inputRow.AddThemeConstantOverride("separation", 7);
        var start = ToolbarButton("VirtualStartButton", "SIMULATED START", new Color("1f8a58"));
        var stop = ToolbarButton("VirtualStopButton", "SIMULATED STOP", new Color("8a3b3b"));
        start.TooltipText = "Momentary simulation-only BOOL input; not a force and not physical I/O.";
        stop.TooltipText = "Momentary simulation-only BOOL input; not a force and not physical I/O.";
        start.Pressed += () => VirtualStartRequested?.Invoke();
        stop.Pressed += () => VirtualStopRequested?.Invoke();
        inputRow.AddChild(start);
        inputRow.AddChild(stop);
        body.AddChild(inputRow);
        body.AddChild(SectionLabel("SIMULATOR FORCE TABLE"));
        var forceBoundary = Inspector("ForceBoundary");
        forceBoundary.FitContent = true;
        forceBoundary.Text = "[color=#ef7777][b]SIMULATION FORCE — NOT PHYSICAL I/O[/b][/color]\nBOOL input/output image only. Stop de-energizes outputs; Reset clears all forces. Forces are never saved to the Ladder project.";
        body.AddChild(forceBoundary);
        var forceControls = new VBoxContainer { Name = "ForceControls" };
        forceControls.AddThemeConstantOverride("separation", 5);
        var forceSelectionRow = new HBoxContainer { Name = "ForceSelection" };
        forceSelectionRow.AddThemeConstantOverride("separation", 5);
        _virtualForceVariable = new OptionButton
        {
            Name = "ForceVariable",
            CustomMinimumSize = new Vector2(190, 32),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Only declared BOOL input/output tags from the loaded offline program are eligible.",
        };
        _virtualForceValue = new OptionButton { Name = "ForceValue", CustomMinimumSize = new Vector2(82, 32) };
        _virtualForceValue.AddItem("FALSE");
        _virtualForceValue.AddItem("TRUE");
        var applyForce = ToolbarButton("ApplyForceButton", "FORCE", new Color("a66b20"), 72);
        var removeForce = ToolbarButton("RemoveForceButton", "REMOVE", new Color("65505a"), 78);
        forceSelectionRow.AddChild(_virtualForceVariable);
        forceSelectionRow.AddChild(_virtualForceValue);
        forceControls.AddChild(forceSelectionRow);
        var forceActionRow = new HBoxContainer { Name = "ForceActions" };
        forceActionRow.AddThemeConstantOverride("separation", 5);
        applyForce.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        removeForce.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        forceActionRow.AddChild(applyForce);
        forceActionRow.AddChild(removeForce);
        forceControls.AddChild(forceActionRow);
        body.AddChild(forceControls);
        var clearForces = ToolbarButton("ClearForcesButton", "CLEAR ALL SIMULATOR FORCES", new Color("8a3b3b"));
        body.AddChild(clearForces);
        _virtualForceStatus = Heading("NO SIMULATOR FORCES", 11, new Color("7fa7ba"));
        _virtualForceStatus.Name = "ForceStatus";
        _virtualForceStatus.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.AddChild(_virtualForceStatus);
        applyForce.Pressed += () =>
        {
            if (_virtualForceVariable.ItemCount == 0) return;
            VirtualForceRequested?.Invoke(
                _virtualForceVariable.GetItemMetadata(_virtualForceVariable.Selected).AsString(),
                _virtualForceValue.Selected == 1);
        };
        removeForce.Pressed += () =>
        {
            if (_virtualForceVariable.ItemCount == 0) return;
            VirtualForceRemoveRequested?.Invoke(
                _virtualForceVariable.GetItemMetadata(_virtualForceVariable.Selected).AsString());
        };
        clearForces.Pressed += () => VirtualForceClearRequested?.Invoke();
        var disable = ToolbarButton("DisableVirtualControllerButton", "DISABLE VIRTUAL CONTROLLER", new Color("355d73"));
        disable.Pressed += () => VirtualControllerDisabled?.Invoke();
        body.AddChild(disable);
        body.AddChild(SectionLabel("LADDER LIVE STATE"));
        _virtualControllerLadder = Inspector("VirtualControllerLadder");
        _virtualControllerLadder.FitContent = true;
        _virtualControllerLadder.Text = "Load the bounded conveyor demonstration program.";
        body.AddChild(_virtualControllerLadder);
        body.AddChild(SectionLabel("VARIABLES"));
        _virtualControllerVariables = Inspector("VirtualControllerVariables");
        _virtualControllerVariables.FitContent = true;
        _virtualControllerVariables.Text = "No virtual program loaded.";
        body.AddChild(_virtualControllerVariables);
        return scroll;
    }

    public void AttachVirtualController(LadderProgram program, VirtualControllerSnapshot snapshot)
    {
        _instructionPublication = null; // New attachment must not inherit another session/load envelope.
        _virtualProgram = program;
        _virtualSnapshot = snapshot;
        RefreshLadderMonitorMatch();
        _virtualForceVariable.Clear();
        foreach (var variable in program.Variables.Where(variable =>
                     variable.Type == PlcVariableType.Bool
                     && variable.Role is PlcVariableRole.Input or PlcVariableRole.Output))
        {
            _virtualForceVariable.AddItem($"{variable.Name} · {variable.Role.ToString().ToUpperInvariant()}");
            _virtualForceVariable.SetItemMetadata(_virtualForceVariable.ItemCount - 1, variable.Name);
        }
        UpdateVirtualController(snapshot);
    }

    public bool TryBuildCurrentLadderProgram(
        out LadderProgram program,
        out IReadOnlyList<LadderValidationIssue> issues)
    {
        program = _ladderDocument.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        var emptyIssues = (_ladderDocument.Blocks.All(block => block.Rungs.Count == 0))
            ? new[] { new LadderValidationIssue("EDIT001", "$.blocks",
                "This exercise has no ladder networks. Add your scene logic, then Verify + Load before Run.") }
            : Array.Empty<LadderValidationIssue>();
        var unresolvedInstructionIssues = _ladderDocument.Blocks
            .SelectMany(block => block.Rungs.Select((rung, index) => (block, rung, index)))
            .Where(item => item.rung.HasEmptyInstructionPlaceholder)
            .Select(item => new LadderValidationIssue("EDIT002", $"$.blocks.{item.block.Id}.rungs[{item.index}]",
                "Select an instruction to replace the empty TIA instruction box before Verify + Load."));
        issues = compiled.Issues.Concat(emptyIssues).Concat(unresolvedInstructionIssues)
            .Concat(SceneIoBindingValidator.Validate(program, ScenePoints()))
            .ToArray();
        return compiled.IsValid && issues.Count == 0;
    }

    public void UpdateVirtualController(VirtualControllerSnapshot snapshot)
    {
        if (_virtualSnapshot?.State != snapshot.State)
            RecordOperatorEvent($"Built-in controller {snapshot.State.ToString().ToLowerInvariant()}");
        _virtualSnapshot = snapshot;
        var virtualProgram = _virtualProgram;
        if (virtualProgram is null) return;
        RefreshChallengeIdentity();
        foreach (var refresh in _ladderWatchRefreshers) refresh(snapshot);
        foreach (var ladderCanvas in _ladderCanvases) ApplyInstructionMonitoring(ladderCanvas, snapshot);
        if (_ladderMonitorMatchesLoadedProgram)
        {
            _ladderWorkspaceStatus.Text = snapshot.State == VirtualControllerState.Running
                ? $"MONITORING OFFLINE RUNTIME · SCAN {snapshot.ScanNumber} · NO PHYSICAL PLC"
                : $"OFFLINE RUNTIME STOPPED · SCAN {snapshot.ScanNumber} · NO PHYSICAL PLC";
            _ladderWorkspaceStatus.AddThemeColorOverride("font_color",
                snapshot.State == VirtualControllerState.Running ? new Color("65d49a") : new Color("f1aa5b"));
        }
        var forceBanner = snapshot.Forces.Count > 0 ? $" · {snapshot.Forces.Count} SIM FORCE(S) ACTIVE" : string.Empty;
        _virtualControllerStatus.Text = snapshot.State == VirtualControllerState.Running
            ? $"VIRTUAL CONTROLLER · RUNNING · SCAN {snapshot.ScanNumber}{forceBanner} · NO PHYSICAL PLC"
            : $"VIRTUAL CONTROLLER · STOPPED · SCAN {snapshot.ScanNumber}{forceBanner} · NO PHYSICAL PLC";
        _virtualControllerStatus.AddThemeColorOverride("font_color",
            snapshot.State == VirtualControllerState.Running ? new Color("65d49a") : new Color("f1aa5b"));
        var cycleMs = virtualProgram.ScanPeriod.TotalMilliseconds;
        _cycleStatus.Text = cycleMs > 0.0
            ? $"CYCLE  {cycleMs:0.###} ms  ·  SCAN  {snapshot.ScanNumber}"
            : "CYCLE  —  ·  SCAN  —";
        _cycleStatus.AddThemeColorOverride("font_color",
            snapshot.State == VirtualControllerState.Running ? new Color("65d49a") : new Color("7fa7ba"));

        var ladder = new StringBuilder();
        ladder.AppendLine("[color=#7fa7ba][b]TASK / OB SCHEDULE[/b][/color]");
        foreach (var task in snapshot.Tasks.Values.OrderBy(task => task.Priority).ThenBy(task => task.Name))
        {
            var state = task.Due ? "[color=#65d49a]DUE[/color]" : "idle";
            var schedule = task.Kind == LadderTaskKind.Continuous ? "continuous" : $"{task.Period.TotalMilliseconds:0} ms";
            ladder.AppendLine($"  {Escape(task.Name)} · {schedule} · P{task.Priority} · {state} · runs={task.ExecutionCount}");
        }
        ladder.AppendLine();
        var monitorBlocks = virtualProgram.Blocks is { Count: > 0 }
            ? virtualProgram.Blocks
            : [new LadderBlock("main", virtualProgram.Name, virtualProgram.Networks)];
        foreach (var block in monitorBlocks)
        {
            ladder.AppendLine($"[color=#7fa7ba][b]BLOCK {Escape(block.Name)}[/b][/color]");
            foreach (var network in block.Networks)
            {
            ladder.AppendLine($"[b]{Escape(network.Label)}[/b]");
            AppendLadderNode(ladder, network.Logic, snapshot, "  ");
            if (network.Coil is not null)
            {
                var coilLabel = network.Coil.Mode switch
                {
                    LadderCoilMode.Set => $"  (S/L) {Escape(network.Coil.Variable)}",
                    LadderCoilMode.Reset => $"  (R/U) {Escape(network.Coil.Variable)}",
                    _ => $"  ( {Escape(network.Coil.Variable)} )",
                };
                AppendState(ladder, network.Coil.Id,
                    coilLabel, snapshot);
            }
            else if (network.Timer is not null)
            {
                var mnemonic = network.Timer.Kind switch
                {
                    LadderTimerKind.OffDelay => "TOF",
                    LadderTimerKind.Pulse => "TP",
                    LadderTimerKind.RetentiveOnDelay => "RTO/TONR",
                    _ => "TON",
                };
                AppendState(ladder, network.Timer.Id,
                    $"  {mnemonic} {Escape(network.Timer.Variable)}  PT={network.Timer.Preset.TotalMilliseconds:0} ms", snapshot);
            }
            else if (network.TimerReset is not null)
                AppendState(ladder, network.TimerReset.Id,
                    $"  RT/RES TIMER {Escape(network.TimerReset.Variable)}", snapshot);
            else if (network.Counter is not null)
            {
                var mnemonic = network.Counter.Kind == LadderCounterKind.CountDown ? "CTD" : "CTU";
                AppendState(ladder, network.Counter.Id,
                    $"  {mnemonic} {Escape(network.Counter.Variable)}  PRE={network.Counter.Preset}", snapshot);
            }
            else if (network.CounterReset is not null)
                AppendState(ladder, network.CounterReset.Id,
                    $"  RES {Escape(network.CounterReset.Variable)}", snapshot);
            else if (network.CounterLoad is not null)
                AppendState(ladder, network.CounterLoad.Id,
                    $"  LOAD {Escape(network.CounterLoad.Variable)}  PRE={network.CounterLoad.Preset}", snapshot);
            else if (network.NumericOperation is not null)
            {
                var operation = network.NumericOperation;
                var sourceText = LadderNumericOperationRules.RequiresSourceC(operation.Kind)
                    ? $"{operation.SourceA}, {operation.SourceB}, {operation.SourceC}"
                    : !LadderNumericOperationRules.RequiresSourceB(operation.Kind)
                        ? operation.SourceA
                        : $"{operation.SourceA}, {operation.SourceB}";
                AppendState(ladder, operation.Id,
                    $"  {operation.Kind.ToString().ToUpperInvariant()} {Escape(sourceText)} -> {Escape(operation.Destination)}", snapshot);
            }
            else if (network.Call is not null)
                AppendState(ladder, network.Call.Id,
                    $"  CALL/JSR {Escape(network.Call.TargetBlock)}", snapshot);
            else if (network.Return is not null)
                AppendState(ladder, network.Return.Id,
                    "  RETURN/RET", snapshot);
            ladder.AppendLine();
            }
        }
        _virtualControllerLadder.Text = ladder.ToString();

        var variables = new StringBuilder();
        foreach (var variable in virtualProgram.Variables)
        {
            if (PlcAggregates.IsAggregate(variable.Type))
            {
                variables.AppendLine($"{Escape(variable.Name)} [color=#7fa7ba]{variable.Type} · {variable.Role}[/color] {Escape(AggregateWatchValue(variable, snapshot))}");
                continue;
            }
            if (variable.Type is PlcVariableType.String or PlcVariableType.Enum)
            {
                variables.AppendLine($"{Escape(variable.Name)}  [color=#7fa7ba]{variable.Type.ToString().ToUpperInvariant()} · {variable.Role}[/color]  {Escape(snapshot.TextVariables.GetValueOrDefault(variable.Name) ?? "—")}");
                continue;
            }
            if (variable.Type == PlcVariableType.Timer)
            {
                var timer = snapshot.Timers.GetValueOrDefault(variable.Name);
                if (timer is not null)
                    variables.AppendLine($"{Escape(variable.Name)}  [color=#7fa7ba]TIMER[/color]  ET={timer.Accumulated.TotalMilliseconds:0} ms  Q/DN={timer.Done.ToString().ToUpperInvariant()}");
                continue;
            }
            if (variable.Type == PlcVariableType.Counter)
            {
                var counter = snapshot.Counters.GetValueOrDefault(variable.Name);
                if (counter is not null)
                    variables.AppendLine($"{Escape(variable.Name)}  [color=#7fa7ba]COUNTER[/color]  ACC/CV={counter.Accumulated}  PRE/PV={counter.Preset}  Q/DN={counter.Done.ToString().ToUpperInvariant()}");
                continue;
            }
            if (variable.Type is PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
            {
                var numericValue = snapshot.NumericVariables.GetValueOrDefault(variable.Name);
                var formatted = variable.Type is PlcVariableType.Int or PlcVariableType.DInt
                    ? numericValue.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                    : numericValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                variables.AppendLine($"{Escape(variable.Name)}  [color=#7fa7ba]{variable.Type.ToString().ToUpperInvariant()} · {variable.Role}[/color]  {formatted}");
                continue;
            }
            var value = snapshot.Variables.GetValueOrDefault(variable.Name);
            var color = value ? "65d49a" : "8aa5b4";
            var force = snapshot.Forces.GetValueOrDefault(variable.Name);
            var forceText = force is null
                ? string.Empty
                : $"  [color=#ef7777][b]FORCE={force.Value.ToString().ToUpperInvariant()} · SIM ONLY[/b][/color]";
            variables.AppendLine(
                $"[color=#{color}]{(value ? "TRUE " : "FALSE")}[/color]  " +
                $"{Escape(variable.Name)}  [color=#7fa7ba]{variable.Role} · {variable.Type}[/color]{forceText}");
        }
        _virtualControllerVariables.Text = variables.ToString();
        _virtualForceStatus.Text = snapshot.Forces.Count == 0
            ? "NO SIMULATOR FORCES"
            : $"{snapshot.Forces.Count} SIMULATOR FORCE(S) ACTIVE · NEVER PHYSICAL I/O";
        _virtualForceStatus.AddThemeColorOverride("font_color",
            snapshot.Forces.Count == 0 ? new Color("7fa7ba") : new Color("ef7777"));
        if (snapshot.Diagnostics.Count > 0)
            _virtualControllerLadder.Text += "\n[color=#ef7777][b]RUNTIME DIAGNOSTICS[/b][/color]\n"
                + string.Join("\n", snapshot.Diagnostics.Select(Escape));
        RefreshRuntimeState();
    }

    public void SetVirtualControllerValidation(IReadOnlyList<LadderValidationIssue> issues)
    {
        // Rejected source is an editor result, not a controller Stop or unload.
        // Main retains the previously loaded controller, so its program and
        // snapshot must stay visible here as well. Never monitor invalid edits
        // using instruction IDs from that older loaded program.
        _ladderMonitorMatchesLoadedProgram = false;
        foreach (var ladderCanvas in _ladderCanvases) ladderCanvas.SetMonitorSnapshot(null);
        foreach (var refresh in _ladderValidationRefreshers) refresh(issues);
        SetProductView("ladder");
        _ladderWorkspaceStatus.Text = _virtualProgram is null
            ? "EDIT INVALID · NO CONTROLLER LOADED · SEE ERROR LIST"
            : "EDIT INVALID · PREVIOUS CONTROLLER RETAINED · SEE ERROR LIST";
        _ladderWorkspaceStatus.AddThemeColorOverride("font_color", new Color("ef7777"));
        if (_virtualProgram is not null && _virtualSnapshot is not null)
            UpdateVirtualController(_virtualSnapshot);
        else
        {
            _virtualControllerStatus.Text = "NO CONTROLLER LOADED · EDIT VALIDATION FAILED";
            _virtualControllerStatus.AddThemeColorOverride("font_color", new Color("ef7777"));
            _cycleStatus.Text = "CYCLE  —  ·  NO CONTROLLER LOADED";
            foreach (var refresh in _ladderWatchRefreshers) refresh(null);
        }
        RefreshRuntimeState();
    }

    public void SetVirtualControllerForceError(string message)
    {
        _virtualForceStatus.Text = "FORCE REJECTED · " + message.ToUpperInvariant();
        _virtualForceStatus.AddThemeColorOverride("font_color", new Color("ef7777"));
    }

    public void DetachVirtualController()
    {
        _virtualProgram = null;
        _virtualSnapshot = null;
        _ladderMonitorMatchesLoadedProgram = false;
        foreach (var ladderCanvas in _ladderCanvases) ladderCanvas.SetMonitorSnapshot(null);
        _virtualControllerStatus.Text = "DISABLED · NO PHYSICAL PLC CONNECTED";
        _virtualControllerStatus.AddThemeColorOverride("font_color", new Color("f1aa5b"));
        _virtualControllerLadder.Text = "Load the bounded conveyor demonstration program.";
        _virtualControllerVariables.Text = "No virtual program loaded.";
        _virtualForceVariable.Clear();
        _virtualForceStatus.Text = "NO SIMULATOR FORCES";
        foreach (var refresh in _ladderWatchRefreshers) refresh(null);
        _cycleStatus.Text = "CYCLE  —  ·  LADDER NOT LOADED";
        _cycleStatus.AddThemeColorOverride("font_color", new Color("7fa7ba"));
        RefreshRuntimeState();
    }

    private void RefreshLadderMonitorMatch()
    {
        var matches = false;
        if (_virtualProgram is not null)
        {
            try
            {
                matches = string.Equals(
                    LadderProgramJson.Save(_virtualProgram with { WatchVariables = null }),
                    LadderProgramJson.Save(_ladderDocument.BuildProgram() with { WatchVariables = null }),
                    StringComparison.Ordinal);
            }
            catch
            {
                matches = false;
            }
        }
        _ladderMonitorMatchesLoadedProgram = matches;
        RefreshInstructionMonitorEligibility();
    }

    public bool VerifyVirtualControllerUi(out string result)
    {
        var required = new[]
        {
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/LoadDemoLadderButton",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/SimulatedInputs/VirtualStartButton",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/SimulatedInputs/VirtualStopButton",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/ForceControls/ForceSelection/ForceVariable",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/ForceControls/ForceSelection/ForceValue",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/ForceControls/ForceActions/ApplyForceButton",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/ForceControls/ForceActions/RemoveForceButton",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/ClearForcesButton",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/ForceStatus",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/VirtualControllerLadder",
            "Workspace/RightDock/InspectorTabs/Virtual Controller/VirtualControllerBody/VirtualControllerVariables",
        };
        var missing = required.Where(path => GetNodeOrNull(path) is null).ToArray();
        var hasProgram = _virtualProgram is not null && _virtualSnapshot is not null;
        var hasDiagnostics = _virtualControllerLadder.Text.Contains("contact-photoeye-inhibit", StringComparison.Ordinal)
            || _virtualControllerLadder.Text.Contains("simulated_photoeye", StringComparison.Ordinal);
        var forceVisible = _virtualSnapshot?.Forces.Count > 0
            && _virtualControllerVariables.Text.Contains("SIM ONLY", StringComparison.Ordinal)
            && _virtualForceStatus.Text.Contains("NEVER PHYSICAL I/O", StringComparison.Ordinal);
        result = $"nodes={required.Length - missing.Length}/{required.Length} program={hasProgram} liveLadder={hasDiagnostics} forceVisible={forceVisible}";
        return missing.Length == 0 && hasProgram && hasDiagnostics && forceVisible;
    }

    public bool VerifyLadderEditorInteractions(out string result)
    {
        const string root = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench";
        var projectTree = GetNodeOrNull<Tree>($"{root}/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree");
        var addBlockFromTree = GetNodeOrNull<Button>(
            $"{root}/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTitle/AddBlockFromProjectTreeSlot/AddBlockFromProjectTree");
        var projectMenu = GetNodeOrNull<MenuButton>(
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/VendorChrome/VendorMenuBar/LadderProjectMenu");
        var projectPanelNode = GetNodeOrNull<Control>($"{root}/ProjectDockHost/ProjectOrganization");
        var projectSplit = GetNodeOrNull<HSplitContainer>(root);
        var toolSplit = GetNodeOrNull<HSplitContainer>($"{root}/EditorAndTasks");
        var bottomSplit = GetNodeOrNull<VSplitContainer>($"{root}/EditorAndTasks/EditorAndInspector");
        var editorRoot = $"{root}/EditorAndTasks/EditorAndInspector/ProgramEditor";
        var tiaEditorTabTitle = GetNodeOrNull<Label>($"{editorRoot}/EditorTab/EditorTabTitle");
        var logixEditorTabTitle = GetNodeOrNull<Label>(
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/EditorTab/EditorTabTitle");
        var paletteRoot = $"{editorRoot}/InstructionToolbarChrome/InstructionToolbar/InstructionPalette";
        var bitRoot = $"{paletteRoot}/Bit Logic";
        var timerRoot = $"{paletteRoot}/Timers";
        var counterRoot = $"{paletteRoot}/Counters";
        var localInstructionRoot = $"{paletteRoot}/RungProof local (not Siemens)";
        var addRung = GetNodeOrNull<Button>($"{bitRoot}/AddRung");
        var addNo = GetNodeOrNull<Button>($"{bitRoot}/AddNormallyOpen");
        var addEmptyInstruction = GetNodeOrNull<Button>($"{bitRoot}/AddEmptyInstructionBox");
        var addRisingEdge = GetNodeOrNull<Button>($"{bitRoot}/AddRisingEdge");
        var addFallingEdge = GetNodeOrNull<Button>($"{bitRoot}/AddFallingEdge");
        var addTimer = GetNodeOrNull<Button>($"{timerRoot}/AddTimerOnDelay");
        var addOffDelay = GetNodeOrNull<Button>($"{timerRoot}/AddTimerOffDelay");
        var addPulseTimer = GetNodeOrNull<Button>($"{timerRoot}/AddPulseTimer");
        var addRetentiveTimer = GetNodeOrNull<Button>($"{timerRoot}/AddRetentiveTimer");
        var addTimerReset = GetNodeOrNull<Button>($"{timerRoot}/AddTimerReset");
        var addCounter = GetNodeOrNull<Button>($"{counterRoot}/AddCountUp");
        var addCounterDown = GetNodeOrNull<Button>($"{counterRoot}/AddCountDown");
        var addCounterLoad = GetNodeOrNull<Button>($"{localInstructionRoot}/AddCounterLoad");
        var addCounterReset = GetNodeOrNull<Button>($"{localInstructionRoot}/AddCounterReset");
        var addFormatText = GetNodeOrNull<Button>($"{localInstructionRoot}/AddFormatText");
        var addCompare = GetNodeOrNull<Button>($"{paletteRoot}/Compare/AddComparison");
        var addMove = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddMove");
        var addAdd = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddAdd");
        var addSubtract = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddSubtract");
        var addMultiply = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddMultiply");
        var addDivide = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddDivide");
        var addModulo = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddModulo");
        var addSquareRoot = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddSquareRoot");
        var addExponentiate = GetNodeOrNull<Button>($"{paletteRoot}/Scientific Math/AddExponentiate");
        var addTruncate = GetNodeOrNull<Button>($"{paletteRoot}/Conversion/AddTruncate");
        var addNormalize = GetNodeOrNull<Button>($"{paletteRoot}/Conversion/AddNormalize");
        var addScale = GetNodeOrNull<Button>($"{paletteRoot}/Conversion/AddScale");
        var addConvert = GetNodeOrNull<Button>($"{paletteRoot}/Conversion/AddConvert");
        var addRound = GetNodeOrNull<Button>($"{paletteRoot}/Conversion/AddRound");
        var addCeiling = GetNodeOrNull<Button>($"{paletteRoot}/Conversion/AddCeiling");
        var addFloor = GetNodeOrNull<Button>($"{paletteRoot}/Conversion/AddFloor");
        var addCall = GetNodeOrNull<Button>($"{paletteRoot}/Program Control/AddCall");
        var addReturn = GetNodeOrNull<Button>($"{paletteRoot}/Program Control/AddReturn");
        var addJump = GetNodeOrNull<Button>($"{paletteRoot}/Program Control/AddJump");
        var addLabel = GetNodeOrNull<Button>($"{paletteRoot}/Program Control/AddLabel");
        var inspectorRoot = $"{root}/EditorAndTasks/EditorAndInspector/BottomDockHost/InspectorOutputDock/InspectorOutputBody";
        var toolRoot = $"{root}/EditorAndTasks/ToolDockHost/InstructionAndTags/ToolBody";
        var projectTabsRoot = $"{root}/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags";
        var projectObjectsRoot = $"{projectTabsRoot}/Project objects/ProjectObjectScroll/ObjectContent";
        var blockCommandsRoot = $"{projectObjectsRoot}/BlockCommands";
        var taskCommandsRoot = $"{projectObjectsRoot}/TaskCommands";
        var blockName = GetNodeOrNull<LineEdit>($"{projectObjectsRoot}/BlockName");
        var blockTypeSelector = GetNodeOrNull<OptionButton>($"{projectObjectsRoot}/BlockTypeSelector");
        var interfaceTable = GetNodeOrNull<Tree>($"{projectObjectsRoot}/BlockInterface");
        var interfaceCommandsRoot = $"{projectTabsRoot}/Project objects/InterfaceCommands";
        var addInterface = GetNodeOrNull<Button>($"{interfaceCommandsRoot}/AddInterface");
        var removeInterface = GetNodeOrNull<Button>($"{interfaceCommandsRoot}/RemoveInterface");
        var addBlock = GetNodeOrNull<Button>($"{blockCommandsRoot}/AddBlock");
        var applyBlock = GetNodeOrNull<Button>($"{blockCommandsRoot}/ApplyBlock");
        var removeBlock = GetNodeOrNull<Button>($"{blockCommandsRoot}/RemoveBlock");
        var taskName = GetNodeOrNull<LineEdit>($"{projectObjectsRoot}/TaskName");
        var addTask = GetNodeOrNull<Button>($"{taskCommandsRoot}/AddTask");
        var applyTask = GetNodeOrNull<Button>($"{taskCommandsRoot}/ApplyTask");
        var removeTask = GetNodeOrNull<Button>($"{taskCommandsRoot}/RemoveTask");
        var addSet = GetNodeOrNull<Button>($"{bitRoot}/AddSetLatch");
        var addReset = GetNodeOrNull<Button>($"{bitRoot}/AddResetUnlatch");
        var removeBranchButton = GetNodeOrNull<Button>($"{bitRoot}/RemoveBranch");
        var remove = GetNodeOrNull<Button>($"{bitRoot}/RemoveInstruction");
        var undo = GetNodeOrNull<Button>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/VendorChrome/VendorMenuBar/LadderUndo");
        var redo = GetNodeOrNull<Button>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/VendorChrome/VendorMenuBar/LadderRedo");
        var bottomTabs = GetNodeOrNull<TabContainer>($"{inspectorRoot}/InspectorOutputTabs");
        var validationSummary = GetNodeOrNull<Label>($"{inspectorRoot}/InspectorOutputTabs/Error List/Content/ValidationSummary");
        var validationIssues = GetNodeOrNull<ItemList>($"{inspectorRoot}/InspectorOutputTabs/Error List/Content/ValidationIssues");
        var output = GetNodeOrNull<RichTextLabel>($"{inspectorRoot}/InspectorOutputTabs/Output/Content/OutputWindow");
        const string contextualPropertiesRoot = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/InstructionPropertiesLayer/InstructionPropertiesPopup/InstructionPropertiesFrame/InstructionPropertiesBody";
        var numericSourceBField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/NumericPropertyRow/NumericSourceB");
        var numericSourceCField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/NumericPropertyRow/NumericSourceC");
        var timerPresetOperandField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/TimerPropertyRow/TimerPresetOperand");
        var timerResetOperandField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/TimerPropertyRow/TimerResetOperand");
        var timerPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/TimerPropertyRow");
        var timerInstanceSelector = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/TimerPropertyRow/TimerTagSelector");
        var timerDbPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/TimerInstanceDbPropertyRow");
        var timerDbNumberField = GetNodeOrNull<SpinBox>($"{contextualPropertiesRoot}/TimerInstanceDbPropertyRow/TimerInstanceDbNumber");
        var timerDbNameField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/TimerInstanceDbPropertyRow/TimerInstanceDbName");
        var counterPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/CounterPropertyRow");
        var counterInstanceSelector = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/CounterPropertyRow/CounterTagSelector");
        var counterDataTypeField = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/CounterPropertyRow/CounterDataTypeSelector");
        var counterPresetField = GetNodeOrNull<SpinBox>($"{contextualPropertiesRoot}/CounterPropertyRow/CounterPreset");
        var counterDbPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/CounterInstanceDbPropertyRow");
        var counterDbNumberField = GetNodeOrNull<SpinBox>($"{contextualPropertiesRoot}/CounterInstanceDbPropertyRow/CounterInstanceDbNumber");
        var counterDbNameField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/CounterInstanceDbPropertyRow/CounterInstanceDbName");
        var counterLoadInputRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/CounterLoadInputPropertyRow");
        var counterLoadInputField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/CounterLoadInputPropertyRow/CounterLoadInput");
        var counterResetInputRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/CounterResetInputPropertyRow");
        var counterResetInputField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/CounterResetInputPropertyRow/CounterResetInput");
        var contextualPropertiesPopup = GetNodeOrNull<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/InstructionPropertiesLayer/InstructionPropertiesPopup");
        var contextualPropertyScope = GetNodeOrNull<Label>($"{contextualPropertiesRoot}/InstructionPropertyScope");
        const string logixPropertiesRoot = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000/InstructionPropertiesLayer/InstructionPropertiesPopup/InstructionPropertiesFrame/InstructionPropertiesBody";
        var logixPropertiesPopup = GetNodeOrNull<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000/InstructionPropertiesLayer/InstructionPropertiesPopup");
        var logixPropertyScope = GetNodeOrNull<Label>($"{logixPropertiesRoot}/InstructionPropertyScope");
        var logixContactPropertyRow = GetNodeOrNull<Control>($"{logixPropertiesRoot}/ContactPropertyRow");
        var instructionContextMenu = GetNodeOrNull<PopupMenu>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/InstructionContextMenu");
        var contactPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/ContactPropertyRow");
        var contactTypeSelector = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/ContactPropertyRow/ContactTypeSelector");
        var tagSelector = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/ContactPropertyRow/ContactTagSelector");
        var edgeMemorySelector = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/ContactPropertyRow/EdgeMemoryOperandSelector");
        var coilPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/CoilPropertyRow");
        var coilModeField = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/CoilPropertyRow/CoilModeSelector");
        var coilOperandField = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/CoilPropertyRow/CoilTagSelector");
        var returnPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/ReturnPropertyRow");
        var returnValueSelectorField = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/ReturnPropertyRow/ReturnValueSelector");
        var returnValueOperandField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/ReturnPropertyRow/ReturnValueOperand");
        var applyInstructionProperties = GetNodeOrNull<Button>($"{contextualPropertiesRoot}/InstructionPropertyActions/ApplyInstructionProperties");
        var instructionPropertyStatus = GetNodeOrNull<Label>($"{contextualPropertiesRoot}/InstructionPropertyStatus");
        var numericPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/NumericPropertyRow");
        var jumpLabelPropertyRow = GetNodeOrNull<Control>($"{contextualPropertiesRoot}/JumpLabelPropertyRow");
        var programControlLabel = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/JumpLabelPropertyRow/ProgramControlLabel");
        var watchRoot = $"{inspectorRoot}/InspectorOutputTabs/Watch table 1/Content";
        var watchStatus = GetNodeOrNull<Label>($"{watchRoot}/WatchHeader/WatchStatus");
        var watchTable = GetNodeOrNull<Tree>($"{watchRoot}/WatchTable");
        var clearWatch = GetNodeOrNull<Button>($"{watchRoot}/WatchSymbolActions/ClearWatchSymbols");
        var instructionTree = GetNodeOrNull<Tree>($"{toolRoot}/ToolTabsHost/ToolTabs/Instructions/InstructionTree");
        var toolPanelNode = GetNodeOrNull<Control>($"{root}/EditorAndTasks/ToolDockHost/InstructionAndTags");
        var toolDockHostNode = GetNodeOrNull<Control>($"{root}/EditorAndTasks/ToolDockHost");
        var searchRoot = $"{toolRoot}/ToolTabsHost/ToolTabs/Find and cross-reference";
        var searchQuery = GetNodeOrNull<LineEdit>($"{searchRoot}/ProjectSearchQuery");
        var crossReference = GetNodeOrNull<Button>($"{searchRoot}/SearchActions/CrossReferenceButton");
        var searchResults = GetNodeOrNull<ItemList>($"{searchRoot}/SearchResults");
        var toolTabs = GetNodeOrNull<TabContainer>($"{toolRoot}/ToolTabsHost/ToolTabs");
        var toolRail = GetNodeOrNull<VBoxContainer>($"{toolRoot}/ToolTabsHost/ToolTabRail");
        var projectTabs = GetNodeOrNull<TabContainer>(projectTabsRoot);
        var tagPageRoot = $"{editorRoot}/PLC tags/TagContent";
        var verifyAndLoad = GetNodeOrNull<Button>($"{tagPageRoot}/ValidateAndLoadButton");
        var editorTagList = GetNodeOrNull<Tree>($"{tagPageRoot}/TagTable");
        var editorTagName = GetNodeOrNull<LineEdit>($"{tagPageRoot}/TagNameRow/NewTagName");
        var editorTagInitialValue = GetNodeOrNull<LineEdit>($"{tagPageRoot}/TagInitialValueRow/TagInitialValue");
        var editorTagBinding = GetNodeOrNull<OptionButton>($"{tagPageRoot}/TagBindingRow/NewTagBinding");
        var editorTagBindingStatus = GetNodeOrNull<Label>($"{tagPageRoot}/TagBindingStatus");
        var addTagButton = GetNodeOrNull<Button>($"{tagPageRoot}/AddTag");
        var applyTagButton = GetNodeOrNull<Button>($"{tagPageRoot}/TagEditActions/ApplyTagEdit");
        var deleteTagButton = GetNodeOrNull<Button>($"{tagPageRoot}/TagEditActions/DeleteTag");
        var helpSelector = GetNodeOrNull<OptionButton>($"{toolRoot}/ToolTabsHost/ToolTabs/Instruction help/HelpInstructionSelector");
        var helpDetails = GetNodeOrNull<RichTextLabel>($"{toolRoot}/ToolTabsHost/ToolTabs/Instruction help/InstructionHelpDetails");
        var openHelp = GetNodeOrNull<Button>($"{editorRoot}/InstructionToolbarChrome/InstructionToolbar/Selection/OpenInstructionHelp");
        var editorCanvas = GetNodeOrNull<LadderEditorCanvas>($"{editorRoot}/RoutineView/GraphicalLadderCanvas");
        var logixCanvas = GetNodeOrNull<LadderEditorCanvas>(
            "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor/RoutineView/GraphicalLadderCanvas");
        var moveLeft = GetNodeOrNull<Button>($"{editorRoot}/InstructionToolbarChrome/InstructionToolbar/Selection/MoveSelectedLeft");
        var moveRight = GetNodeOrNull<Button>($"{editorRoot}/InstructionToolbarChrome/InstructionToolbar/Selection/MoveSelectedRight");
        var collapseProject = GetNodeOrNull<Button>($"{root}/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTitle/CollapseProjectDock");
        var reopenProject = GetNodeOrNull<Button>($"{root}/ProjectDockHost/ReopenProjectDock");
        var collapseTools = GetNodeOrNull<Button>($"{toolRoot}/ToolTitle/CollapseToolDock");
        var reopenTools = GetNodeOrNull<Button>($"{root}/EditorAndTasks/ToolDockHost/ReopenToolDock");
        var collapseBottom = GetNodeOrNull<Button>($"{inspectorRoot}/InspectorOutputTitle/CollapseBottomDock");
        var reopenBottom = GetNodeOrNull<Button>($"{root}/EditorAndTasks/EditorAndInspector/BottomDockHost/ReopenBottomDock");
        var bottomPanelNode = GetNodeOrNull<Control>($"{root}/EditorAndTasks/EditorAndInspector/BottomDockHost/InspectorOutputDock");
        var originalProgram = _ladderDocument.BuildProgram();
        var originalEditorSnapshot = _ladderDocument.CaptureSnapshot();
        var startingRungs = _ladderDocument.Rungs.Count;
        if (projectTree is null || projectMenu is null || tiaEditorTabTitle is null || logixEditorTabTitle is null || projectPanelNode is null || projectSplit is null || toolSplit is null || bottomSplit is null || addRung is null || addNo is null || addRisingEdge is null || addFallingEdge is null || addTimer is null || addOffDelay is null || addPulseTimer is null || addRetentiveTimer is null || addTimerReset is null || addCounter is null || addCounterDown is null || addCounterLoad is null || addCounterReset is null || addFormatText is null || addCompare is null || addMove is null || addAdd is null || addSubtract is null || addMultiply is null || addDivide is null || addModulo is null || addSquareRoot is null || addExponentiate is null || addTruncate is null || addNormalize is null || addScale is null || addConvert is null || addRound is null || addCeiling is null || addFloor is null || addCall is null || addReturn is null || addJump is null || addLabel is null || blockName is null || addBlock is null || applyBlock is null || removeBlock is null || blockTypeSelector is null || interfaceTable is null || addInterface is null || removeInterface is null || taskName is null || verifyAndLoad is null || addTask is null || applyTask is null || removeTask is null || addSet is null || addReset is null || removeBranchButton is null || remove is null || undo is null || redo is null || bottomTabs is null || validationSummary is null || validationIssues is null || output is null || numericSourceBField is null || numericSourceCField is null || contextualPropertiesPopup is null || contextualPropertyScope is null || logixPropertiesPopup is null || logixPropertyScope is null || logixContactPropertyRow is null || instructionContextMenu is null || contactPropertyRow is null || contactTypeSelector is null || tagSelector is null || edgeMemorySelector is null || coilPropertyRow is null || coilModeField is null || coilOperandField is null || returnPropertyRow is null || returnValueSelectorField is null || returnValueOperandField is null || timerPresetOperandField is null || timerResetOperandField is null || timerPropertyRow is null || timerInstanceSelector is null || timerDbPropertyRow is null || timerDbNumberField is null || timerDbNameField is null || counterPropertyRow is null || counterInstanceSelector is null || counterDataTypeField is null || counterPresetField is null || counterDbPropertyRow is null || counterDbNumberField is null || counterDbNameField is null || counterLoadInputRow is null || counterLoadInputField is null || counterResetInputRow is null || counterResetInputField is null || applyInstructionProperties is null || instructionPropertyStatus is null || numericPropertyRow is null || jumpLabelPropertyRow is null || programControlLabel is null || watchStatus is null || watchTable is null || clearWatch is null || instructionTree is null || toolPanelNode is null || searchQuery is null || crossReference is null || searchResults is null || toolTabs is null || toolRail is null || editorTagList is null || editorTagName is null || editorTagInitialValue is null || editorTagBinding is null || editorTagBindingStatus is null || addTagButton is null || applyTagButton is null || deleteTagButton is null || helpSelector is null || helpDetails is null || openHelp is null || editorCanvas is null || logixCanvas is null || moveLeft is null || moveRight is null || collapseProject is null || reopenProject is null || collapseTools is null || reopenTools is null || collapseBottom is null || reopenBottom is null || bottomPanelNode is null)
        {
            result = $"projectTree={projectTree is not null} instructionTree={instructionTree is not null} contextualProperties={contextualPropertiesPopup is not null && instructionContextMenu is not null} addRung={addRung is not null} addNo={addNo is not null} addTimer={addTimer is not null} addOffDelay={addOffDelay is not null} addPulseTimer={addPulseTimer is not null} addRetentiveTimer={addRetentiveTimer is not null} addTimerReset={addTimerReset is not null} addCounter={addCounter is not null} addCounterDown={addCounterDown is not null} addCounterLoad={addCounterLoad is not null} addCounterReset={addCounterReset is not null} addCompare={addCompare is not null} addMove={addMove is not null} addCall={addCall is not null} addReturn={addReturn is not null} addJump={addJump is not null} addLabel={addLabel is not null} addBlock={addBlock is not null} verify={verifyAndLoad is not null} errorList={bottomTabs is not null && validationSummary is not null && validationIssues is not null} watchTable={watchStatus is not null && watchTable is not null && clearWatch is not null} bindingBrowser={editorTagBinding is not null && editorTagBindingStatus is not null} addTask={addTask is not null} addSet={addSet is not null} addReset={addReset is not null} remove={remove is not null} undo={undo is not null} redo={redo is not null} search={searchQuery is not null && crossReference is not null && searchResults is not null} help={toolTabs is not null && helpSelector is not null && helpDetails is not null && openHelp is not null} toolRail={toolRail is not null} docks={collapseProject is not null && reopenProject is not null && collapseTools is not null && reopenTools is not null && collapseBottom is not null && reopenBottom is not null && bottomPanelNode is not null && toolDockHostNode is not null}";
            return false;
        }
        var tiaRemoveBranchShortcutHidden = !removeBranchButton.Visible;
        TreeItem? FindInstructionItem(TreeItem? item, string metadata)
        {
            for (var current = item; current is not null; current = current.GetNext())
            {
                if (current.GetMetadata(0).AsString() == metadata) return current;
                var nested = FindInstructionItem(current.GetFirstChild(), metadata);
                if (nested is not null) return nested;
            }
            return null;
        }
        var localCategoryName = "RungProof local (not Siemens)";
        var localTreeItems = new[] { "counter-load", "counter-reset", "compare-6", "numeric-24" }
            .Select(key => FindInstructionItem(instructionTree.GetRoot(), key)).ToArray();
        var tiaLocalInstructionsSeparated = addCounterLoad.GetParent()?.Name == localCategoryName
            && addCounterReset.GetParent()?.Name == localCategoryName
            && addFormatText.GetParent()?.Name == localCategoryName
            && localTreeItems.All(item => item?.GetParent()?.GetText(0) == localCategoryName)
            && addCounter.GetParent()?.Name == "Counters"
            && addCounterDown.GetParent()?.Name == "Counters";
        GD.Print($"TIA_LOCAL_INSTRUCTION_SEPARATION_VERIFY {(tiaLocalInstructionsSeparated ? "PASS" : "FAIL")}");
        var projectPersistenceWorked = false;
        var originalSavedProjectJson = _ladderSavedProjectJson;
        var originalProjectPath = _ladderProjectPath;
        var projectRoundTripPath = ProjectSettings.GlobalizePath("res://build/verify-ladder-project.rpproj.json");
        var validProjectRoundTripPath = ProjectSettings.GlobalizePath("res://build/verify-valid-ladder-project.rpproj.json");
        var priorRuntimeProgram = _virtualProgram;
        var priorRuntimeSnapshot = _virtualSnapshot;
        try
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(projectRoundTripPath)!);
            if (System.IO.File.Exists(projectRoundTripPath)) System.IO.File.Delete(projectRoundTripPath);
            if (System.IO.File.Exists(validProjectRoundTripPath)) System.IO.File.Delete(validProjectRoundTripPath);
            var priorCompilation = LadderCompiler.Compile(originalProgram);
            if (!priorCompilation.IsValid || priorCompilation.Program is null)
                throw new InvalidOperationException("Persistence regression needs a valid loaded program.");
            var persistenceRuntime = new VirtualControllerRuntime(priorCompilation.Program);
            persistenceRuntime.Run();
            AttachVirtualController(originalProgram, persistenceRuntime.Scan(new Dictionary<string, bool>()));

            var validOpenMarker = originalEditorSnapshot.Blocks[originalEditorSnapshot.ActiveBlockIndex].Rungs[0].Label + " valid Open regression";
            _ladderDocument.Rungs[0].Label = validOpenMarker;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            var validSaved = TrySaveLadderProject(validProjectRoundTripPath, out var validSaveMessage);
            _ladderDocument.Rungs[0].Label = originalEditorSnapshot.Blocks[originalEditorSnapshot.ActiveBlockIndex].Rungs[0].Label;
            var validRuntimeProgramBeforeOpen = _virtualProgram;
            var validRuntimeScanBeforeOpen = _virtualSnapshot?.ScanNumber;
            var validOpened = TryOpenLadderProject(validProjectRoundTripPath, out var validOpenMessage);
            var validOpenRetainedRuntime = validSaved && validOpened
                && _ladderDocument.Rungs[0].Label == validOpenMarker
                && _ladderDocument.SourceSceneId == _activeScene?.Id
                && validOpenMessage.Contains("VALID DRAFT · NOT LOADED · VERIFY + LOAD REQUIRED", StringComparison.Ordinal)
                && ReferenceEquals(validRuntimeProgramBeforeOpen, _virtualProgram)
                && validRuntimeScanBeforeOpen == _virtualSnapshot?.ScanNumber
                && _virtualSnapshot?.State == VirtualControllerState.Running
                && _virtualControllerStatus.Text.Contains("RUNNING", StringComparison.Ordinal)
                && _cycleStatus.Text.Contains("SCAN", StringComparison.Ordinal);
            var paddedSceneProject = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(validProjectRoundTripPath));
            if (!paddedSceneProject.IsReadable || paddedSceneProject.Document is null)
                throw new InvalidOperationException("Scene ID normalization regression needs a readable project.");
            paddedSceneProject.Document.SourceSceneId = $" {_activeScene?.Id} ";
            System.IO.File.WriteAllText(validProjectRoundTripPath, LadderEditorProjectJson.Save(paddedSceneProject.Document));
            var paddedSceneOpened = TryOpenLadderProject(validProjectRoundTripPath, out _);
            var paddedSceneIdNormalized = paddedSceneOpened
                && _ladderDocument.SourceSceneId == _activeScene?.Id
                && ReferenceEquals(validRuntimeProgramBeforeOpen, _virtualProgram)
                && validRuntimeScanBeforeOpen == _virtualSnapshot?.ScanNumber
                && _virtualSnapshot?.State == VirtualControllerState.Running;
            var priorExternalMode = _externalMode;
            _externalMode = true;
            verifyAndLoad.EmitSignal(BaseButton.SignalName.Pressed);
            var externalModeBlocksLocalLoadClearly = ReferenceEquals(validRuntimeProgramBeforeOpen, _virtualProgram)
                && validRuntimeScanBeforeOpen == _virtualSnapshot?.ScanNumber
                && output.Text.Contains("NOT LOADED", StringComparison.Ordinal)
                && !output.Text.Contains("loaded into offline runtime", StringComparison.OrdinalIgnoreCase)
                && _ladderWorkspaceStatus.Text.Contains("EXTERNAL PLC MODE", StringComparison.Ordinal);
            _externalMode = priorExternalMode;
            verifyAndLoad.EmitSignal(BaseButton.SignalName.Pressed);
            var verifyLoadReplacedRuntime = validOpenRetainedRuntime
                && !ReferenceEquals(validRuntimeProgramBeforeOpen, _virtualProgram)
                && _virtualProgram?.Networks[0].Label == validOpenMarker
                && _virtualSnapshot?.State == VirtualControllerState.Stopped;

            // Reattach a running offline controller to prove an invalid draft
            // opened afterward also leaves the prior program and scan intact.
            var invalidDraftRuntime = new VirtualControllerRuntime(priorCompilation.Program);
            invalidDraftRuntime.Run();
            AttachVirtualController(originalProgram, invalidDraftRuntime.Scan(new Dictionary<string, bool>()));
            var runtimeProgramBeforeOpen = _virtualProgram;
            var runtimeScanBeforeOpen = _virtualSnapshot?.ScanNumber;
            var draftContact = _ladderDocument.Rungs[0].Branches[0].Contacts[0];
            _ladderDocument.ReplaceContact(0, 0, 0, draftContact with { Variable = "draft_symbol_not_declared" });
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            var dirtyBeforeSave = tiaEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal)
                && logixEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal);
            var invalidDraftPreserved = !LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
            var saved = TrySaveLadderProject(projectRoundTripPath, out var saveMessage);
            var cleanAfterSave = !tiaEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal)
                && !logixEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal)
                && tiaEditorTabTitle.Text.Contains("verify-ladder-project.rpproj.json", StringComparison.Ordinal)
                && logixEditorTabTitle.Text.Contains("verify-ladder-project.rpproj.json", StringComparison.Ordinal);
            _ladderDocument.ReplaceContact(0, 0, 0, draftContact);
            var opened = TryOpenLadderProject(projectRoundTripPath, out var openMessage);
            var restoredContact = _ladderDocument.Rungs[0].Branches[0].Contacts[0];
            projectPersistenceWorked = dirtyBeforeSave && cleanAfterSave && invalidDraftPreserved && saved && opened
                && restoredContact.Id == draftContact.Id
                && restoredContact.Variable == "draft_symbol_not_declared"
                && _ladderDocument.SourceSceneId == _activeScene?.Id
                && !LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid
                && saveMessage.Contains("WORK IN PROGRESS PRESERVED", StringComparison.Ordinal)
                && openMessage.Contains("VERIFY + LOAD REQUIRED", StringComparison.Ordinal)
                && ReferenceEquals(runtimeProgramBeforeOpen, _virtualProgram)
                && runtimeScanBeforeOpen == _virtualSnapshot?.ScanNumber
                && _virtualSnapshot?.State == VirtualControllerState.Running
                && _virtualControllerStatus.Text.Contains("RUNNING", StringComparison.Ordinal)
                && _cycleStatus.Text.Contains("SCAN", StringComparison.Ordinal)
                && !editorCanvas.MonitorActive && !logixCanvas.MonitorActive
                && validationIssues.ItemCount > 0
                && validOpenRetainedRuntime && paddedSceneIdNormalized && verifyLoadReplacedRuntime
                && externalModeBlocksLocalLoadClearly
                && validSaveMessage.Contains("WORK IN PROGRESS PRESERVED", StringComparison.Ordinal);
        }
        finally
        {
            _ladderDocument.RestoreSnapshot(originalEditorSnapshot);
            _ladderSavedProjectJson = originalSavedProjectJson;
            _ladderProjectPath = originalProjectPath;
            _ladderHistory.Clear();
            if (priorRuntimeProgram is not null && priorRuntimeSnapshot is not null)
                AttachVirtualController(priorRuntimeProgram, priorRuntimeSnapshot);
            else DetachVirtualController();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            RefreshLadderMonitorMatch();
            if (System.IO.File.Exists(projectRoundTripPath)) System.IO.File.Delete(projectRoundTripPath);
            if (System.IO.File.Exists(validProjectRoundTripPath)) System.IO.File.Delete(validProjectRoundTripPath);
        }
        _ladderDocument.Rungs[0].Label += " unsaved guard review";
        var guardedJson = LadderEditorProjectJson.Save(_ladderDocument);
        projectMenu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
        var replacementGuardWorked = _unsavedLadderDialog.Visible;
        _unsavedLadderDialog.EmitSignal(AcceptDialog.SignalName.Canceled);
        replacementGuardWorked &= LadderEditorProjectJson.Save(_ladderDocument) == guardedJson;
        projectMenu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
        _unsavedLadderDialog.EmitSignal(AcceptDialog.SignalName.Confirmed);
        var newProjectWorked = replacementGuardWorked && _ladderDocument.Id == "offline-controller"
            && _ladderDocument.Name == "Main"
            && _ladderDocument.ScanPeriod == TimeSpan.FromMilliseconds(20)
            && _ladderDocument.Tags.Select(tag => tag.Name).SequenceEqual(["input_1", "output_1"])
            && _ladderDocument.WatchVariables.Count == 0
            && _ladderDocument.Blocks.Count == 1
            && _ladderDocument.Blocks[0].Id == "block-1"
            && _ladderDocument.Tasks.Count == 1
            && _ladderDocument.Tasks[0].Id == "task-2"
            && _ladderDocument.Rungs.Count == 1
            && tiaEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal)
            && logixEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal)
            && tiaEditorTabTitle.Text.Contains("Untitled.rpproj.json", StringComparison.Ordinal)
            && logixEditorTabTitle.Text.Contains("Untitled.rpproj.json", StringComparison.Ordinal)
            && !_ladderMonitorMatchesLoadedProgram
            && !editorCanvas.MonitorActive && !logixCanvas.MonitorActive;
        _ladderDocument.RestoreSnapshot(originalEditorSnapshot);
        _ladderSavedProjectJson = originalSavedProjectJson;
        _ladderProjectPath = originalProjectPath;
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        RefreshLadderMonitorMatch();
        var firstRungBounds = editorCanvas.GetRungBounds(0);
        var secondRungBounds = editorCanvas.GetRungBounds(1);
        var firstBranchPosition = editorCanvas.GetInsertionPointPosition(0, 0, 0);
        var secondBranchPosition = editorCanvas.GetInsertionPointPosition(0, 1, 0);
        var rungLayoutWorked = secondBranchPosition.Y - firstBranchPosition.Y >= 90
            && secondRungBounds.Position.Y - firstRungBounds.End.Y >= 14;
        editorCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            DoubleClick = true,
            Position = editorCanvas.GetElementPosition(0, 0, 0),
        });
        var contextualPropertiesWorked = contextualPropertiesPopup.Visible
            && contactPropertyRow.Visible
            && !numericPropertyRow.Visible
            && contextualPropertyScope.Text.EndsWith("Network 1", StringComparison.Ordinal)
            && bottomTabs.GetTabCount() == 3
            && Enumerable.Range(0, bottomTabs.GetTabCount())
                .All(index => !bottomTabs.GetTabTitle(index).Equals("Properties", StringComparison.OrdinalIgnoreCase));
        contextualPropertiesPopup.Hide();
        (int Rung, int Branch, int Contact)? FindContactPosition(string nodeId)
        {
            for (var rungIndex = 0; rungIndex < _ladderDocument.Rungs.Count; rungIndex++)
            for (var branchIndex = 0; branchIndex < _ladderDocument.Rungs[rungIndex].Branches.Count; branchIndex++)
            {
                var contactIndex = _ladderDocument.Rungs[rungIndex].Branches[branchIndex].Contacts
                    .FindIndex(contact => contact.Id == nodeId);
                if (contactIndex >= 0) return (rungIndex, branchIndex, contactIndex);
            }
            return null;
        }
        var editableNoContact = _ladderDocument.Rungs
            .SelectMany(rung => EditableLadderTree.Visit(rung.Root))
            .FirstOrDefault(node => node.Kind == LadderNodeKind.Contact
                && node.EdgeMode == LadderEdgeMode.None && !node.NormallyClosed);
        var noContactPropertyEditWorked = false;
        var ncContactPropertyEditWorked = false;
        if (editableNoContact is not null && FindContactPosition(editableNoContact.Id) is { } contactPosition)
        {
            EditableLadderNode? CurrentContact() => _ladderDocument.Rungs
                .Select(rung => EditableLadderTree.Find(rung.Root, editableNoContact.Id))
                .FirstOrDefault(node => node is not null);
            var originalOperand = editableNoContact.Variable;
            var alternateOperandIndex = Enumerable.Range(0, tagSelector.ItemCount)
                .FirstOrDefault(index => tagSelector.GetItemText(index) != originalOperand, -1);
            if (alternateOperandIndex >= 0)
            {
                editorCanvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    Pressed = true,
                    DoubleClick = true,
                    Position = editorCanvas.GetElementPosition(contactPosition.Rung, contactPosition.Branch, contactPosition.Contact),
                });
                var noEditorOpened = contextualPropertiesPopup.Visible && contactPropertyRow.Visible
                    && contactTypeSelector.ItemCount == 4 && contactTypeSelector.Selected == 0
                    && !edgeMemorySelector.Visible;
                contactTypeSelector.Select(1);
                contactTypeSelector.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
                tagSelector.Select(alternateOperandIndex);
                tagSelector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)alternateOperandIndex);
                var editedOperand = tagSelector.GetItemText(alternateOperandIndex);
                applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
                var ncApplied = CurrentContact();
                var noApplyWorked = ncApplied is not null && ncApplied.Id == editableNoContact.Id
                    && ncApplied.NormallyClosed && ncApplied.EdgeMode == LadderEdgeMode.None
                    && ncApplied.Variable == editedOperand && _ladderHistory.CanUndo;
                undo.EmitSignal(BaseButton.SignalName.Pressed);
                var noUndone = CurrentContact();
                var noUndoWorked = noUndone is not null && noUndone.Id == editableNoContact.Id
                    && !noUndone.NormallyClosed && noUndone.EdgeMode == LadderEdgeMode.None
                    && noUndone.Variable == originalOperand;
                redo.EmitSignal(BaseButton.SignalName.Pressed);
                var noRedone = CurrentContact();
                var noRedoWorked = noRedone is not null && noRedone.Id == editableNoContact.Id
                    && noRedone.NormallyClosed && noRedone.EdgeMode == LadderEdgeMode.None
                    && noRedone.Variable == editedOperand;
                noContactPropertyEditWorked = noEditorOpened && noApplyWorked && noUndoWorked && noRedoWorked;

                editorCanvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    Pressed = true,
                    DoubleClick = true,
                    Position = editorCanvas.GetElementPosition(contactPosition.Rung, contactPosition.Branch, contactPosition.Contact),
                });
                var ncEditorOpened = contextualPropertiesPopup.Visible && contactPropertyRow.Visible
                    && contactTypeSelector.Selected == 1 && !edgeMemorySelector.Visible;
                contactTypeSelector.Select(0);
                contactTypeSelector.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
                var originalOperandIndex = Enumerable.Range(0, tagSelector.ItemCount)
                    .FirstOrDefault(index => tagSelector.GetItemText(index) == originalOperand, -1);
                if (originalOperandIndex >= 0)
                {
                    tagSelector.Select(originalOperandIndex);
                    tagSelector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)originalOperandIndex);
                    applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
                    var noApplied = CurrentContact();
                    var ncApplyWorked = noApplied is not null && noApplied.Id == editableNoContact.Id
                        && !noApplied.NormallyClosed && noApplied.EdgeMode == LadderEdgeMode.None
                        && noApplied.Variable == originalOperand;
                    undo.EmitSignal(BaseButton.SignalName.Pressed);
                    var ncUndone = CurrentContact();
                    var ncUndoWorked = ncUndone is not null && ncUndone.Id == editableNoContact.Id
                        && ncUndone.NormallyClosed && ncUndone.EdgeMode == LadderEdgeMode.None
                        && ncUndone.Variable == editedOperand;
                    redo.EmitSignal(BaseButton.SignalName.Pressed);
                    var ncRedone = CurrentContact();
                    var ncRedoWorked = ncRedone is not null && ncRedone.Id == editableNoContact.Id
                        && !ncRedone.NormallyClosed && ncRedone.EdgeMode == LadderEdgeMode.None
                        && ncRedone.Variable == originalOperand;
                    ncContactPropertyEditWorked = ncEditorOpened && ncApplyWorked && ncUndoWorked && ncRedoWorked;
                }
                else contextualPropertiesPopup.Hide();
            }
        }
        GD.Print($"TIA_CONTACT_PROPERTY_EDIT_VERIFY {(noContactPropertyEditWorked && ncContactPropertyEditWorked ? "PASS" : "FAIL")} no={noContactPropertyEditWorked} nc={ncContactPropertyEditWorked}");
        _ladderEnvironmentTabs.CurrentTab = 1;
        logixCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            DoubleClick = true,
            Position = logixCanvas.GetElementPosition(0, 0, 0),
        });
        var logixContextualPropertiesWorked = logixPropertiesPopup.Visible
            && logixContactPropertyRow.Visible
            && logixPropertyScope.Text == "MainProgram  ›  Conveyor_Main  ›  Rung 0";
        logixPropertiesPopup.Hide();
        _ladderEnvironmentTabs.CurrentTab = 0;
        editorCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Right,
            Pressed = true,
            Position = editorCanvas.GetElementPosition(0, 0, 0),
        });
        var instructionContextMenuWorked = instructionContextMenu.Visible
            && instructionContextMenu.GetItemText(0).Contains("Propert", StringComparison.OrdinalIgnoreCase);
        instructionContextMenu.Hide();
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var projectSplitBeforeCollapse = projectSplit.SplitOffsets[0];
        collapseProject.EmitSignal(BaseButton.SignalName.Pressed);
        var projectDockCollapsed = reopenProject.Visible && !projectPanelNode.Visible;
        var projectSpaceReclaimed = projectSplit.SplitOffsets[0] <= reopenProject.CustomMinimumSize.X + 1.0f;
        reopenProject.EmitSignal(BaseButton.SignalName.Pressed);
        var projectDockReopened = projectPanelNode.Visible && !reopenProject.Visible;
        var projectSplitRestored = projectSplit.SplitOffsets[0] == projectSplitBeforeCollapse;
        // Start within the current pane: a preferred 600 px offset can exceed
        // its rendered width after compact layout, making growth impossible.
        toolSplit.SplitOffsets = [Math.Max(0, (int)toolSplit.Size.X / 2)];
        var toolSplitBeforeCollapse = toolSplit.SplitOffsets[0];
        collapseTools.EmitSignal(BaseButton.SignalName.Pressed);
        var toolDockCollapsed = reopenTools.Visible && !toolPanelNode.Visible;
        var toolSpaceReclaimed = toolSplit.SplitOffsets[0] > toolSplitBeforeCollapse;
        reopenTools.EmitSignal(BaseButton.SignalName.Pressed);
        var toolDockReopened = toolPanelNode.Visible && !reopenTools.Visible;
        var toolSplitRestored = toolSplit.SplitOffsets[0] == toolSplitBeforeCollapse;
        // As with the tool dock, test a split inside the rendered pane. A
        // preferred 400px offset can already exceed a short editor's height.
        bottomSplit.SplitOffsets = [Math.Max(0, (int)bottomSplit.Size.Y / 2)];
        var bottomSplitBeforeCollapse = bottomSplit.SplitOffsets[0];
        collapseBottom.EmitSignal(BaseButton.SignalName.Pressed);
        var bottomDockCollapsed = reopenBottom.Visible && !bottomPanelNode.Visible;
        var bottomSpaceReclaimed = bottomSplit.SplitOffsets[0] > bottomSplitBeforeCollapse;
        reopenBottom.EmitSignal(BaseButton.SignalName.Pressed);
        var bottomDockReopened = bottomPanelNode.Visible && !reopenBottom.Visible;
        var bottomSplitRestored = bottomSplit.SplitOffsets[0] == bottomSplitBeforeCollapse;
        var docksWorked = projectDockCollapsed && projectSpaceReclaimed && projectDockReopened && projectSplitRestored
            && toolDockCollapsed && toolSpaceReclaimed && toolDockReopened && toolSplitRestored
            && bottomDockCollapsed && bottomSpaceReclaimed && bottomDockReopened && bottomSplitRestored;
        if (!docksWorked)
            GD.Print($"DOCK_VERIFY projectSpace={projectSpaceReclaimed} projectRestore={projectSplitRestored} toolSpace={toolSpaceReclaimed} toolRestore={toolSplitRestored} bottomSpace={bottomSpaceReclaimed} bottomRestore={bottomSplitRestored}");
        var splitOffsets = new[]
        {
            projectSplit.SplitOffsets[0],
            toolSplit.SplitOffsets[0],
            bottomSplit.SplitOffsets[0],
        };
        projectSplit.SplitOffsets = [splitOffsets[0] + 24];
        toolSplit.SplitOffsets = [splitOffsets[1] - 24];
        bottomSplit.SplitOffsets = [splitOffsets[2] - 24];
        var dockResizeWorked = projectSplit.DraggingEnabled
            && toolSplit.DraggingEnabled
            && bottomSplit.DraggingEnabled
            && projectSplit.DraggerVisibility == SplitContainer.DraggerVisibilityEnum.Visible
            && toolSplit.DraggerVisibility == SplitContainer.DraggerVisibilityEnum.Visible
            && bottomSplit.DraggerVisibility == SplitContainer.DraggerVisibilityEnum.Visible
            && projectSplit.SplitOffsets[0] == splitOffsets[0] + 24
            && toolSplit.SplitOffsets[0] == splitOffsets[1] - 24
            && bottomSplit.SplitOffsets[0] == splitOffsets[2] - 24;
        projectSplit.SplitOffsets = [splitOffsets[0]];
        toolSplit.SplitOffsets = [splitOffsets[1]];
        bottomSplit.SplitOffsets = [splitOffsets[2]];
        var originalValidationTags = _ladderDocument.Tags.ToArray();
        var simulatorInputPoint = ScenePoints().First(point =>
            point.Type.Equals("BOOL", StringComparison.OrdinalIgnoreCase)
            && point.Owner.Equals("PC", StringComparison.OrdinalIgnoreCase));
        var simulatorOutputPoint = ScenePoints().First(point =>
            point.Type.Equals("BOOL", StringComparison.OrdinalIgnoreCase)
            && point.Owner.Equals("PLC", StringComparison.OrdinalIgnoreCase));
        for (var index = 0; index < _ladderDocument.Tags.Count; index++)
        {
            var tag = _ladderDocument.Tags[index];
            if (tag.Name == "simulated_photoeye")
                _ladderDocument.Tags[index] = tag with { Binding = simulatorInputPoint.Name };
            else if (tag.Name == "conveyor_running")
                _ladderDocument.Tags[index] = tag with { Binding = simulatorOutputPoint.Name };
        }
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        bool BindingChoiceExists(string binding) => Enumerable.Range(0, editorTagBinding.ItemCount)
            .Any(index => editorTagBinding.GetItemMetadata(index).AsString() == binding);
        var simulatorInputTagIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == "simulated_photoeye");
        SelectIndexedTreeRow(editorTagList, simulatorInputTagIndex);
        editorTagList.EmitSignal(Tree.SignalName.ItemSelected);
        var inputBindingBrowserWorked = BindingChoiceExists(simulatorInputPoint.Name)
            && !BindingChoiceExists(simulatorOutputPoint.Name)
            && editorTagBindingStatus.Text.Contains("PC", StringComparison.Ordinal);
        var simulatorOutputTagIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == "conveyor_running");
        SelectIndexedTreeRow(editorTagList, simulatorOutputTagIndex);
        editorTagList.EmitSignal(Tree.SignalName.ItemSelected);
        var outputBindingBrowserWorked = BindingChoiceExists(simulatorOutputPoint.Name)
            && !BindingChoiceExists(simulatorInputPoint.Name)
            && editorTagBindingStatus.Text.Contains("PLC", StringComparison.Ordinal);
        var bindingBrowserWorked = inputBindingBrowserWorked && outputBindingBrowserWorked;
        foreach (var form in _pendingEditorForms) if (form.HasPending()) form.Discard();
        var originalValidationCoil = _ladderDocument.Rungs[0].CoilVariable;
        _ladderDocument.Rungs[0].CoilVariable = "start_command";
        verifyAndLoad.EmitSignal(BaseButton.SignalName.Pressed);
        var structuredValidationWorked = validationIssues.ItemCount == 1
            && validationIssues.GetItemText(0).Contains("VC005", StringComparison.Ordinal)
            && validationSummary.Text.StartsWith("1 ERROR", StringComparison.Ordinal)
            && bottomTabs.CurrentTab == 0;
        validationIssues.Select(0);
        validationIssues.EmitSignal(ItemList.SignalName.ItemActivated, 0);
        var validationNavigationWorked = editorCanvas.SelectedRung == 0
            && output.Text.Contains("VC005", StringComparison.Ordinal);
        _ladderDocument.Rungs[0].CoilVariable = originalValidationCoil;
        verifyAndLoad.EmitSignal(BaseButton.SignalName.Pressed);
        var validationCleared = validationIssues.ItemCount == 0
            && validationSummary.Text.StartsWith("0 ERRORS", StringComparison.Ordinal)
            && bottomTabs.CurrentTab == 1;
        _ladderDocument.Tags.Clear();
        _ladderDocument.Tags.AddRange(originalValidationTags);
        addRung.EmitSignal(BaseButton.SignalName.Pressed);
        var added = _ladderDocument.Rungs.Count == startingRungs + 1;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var undoWorked = _ladderDocument.Rungs.Count == startingRungs;
        redo.EmitSignal(BaseButton.SignalName.Pressed);
        var redoWorked = _ladderDocument.Rungs.Count == startingRungs + 1;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var restored = _ladderDocument.Rungs.Count == startingRungs;
        int ContactCount() => _ladderDocument.Rungs.Sum(rung => rung.Branches.Sum(branch => branch.Contacts.Count));
        var startingContacts = ContactCount();
        TreeItem? Find(TreeItem? item, string metadata)
        {
            for (var current = item; current is not null; current = current.GetNext())
            {
                if (current.GetMetadata(0).AsString() == metadata) return current;
                var child = Find(current.GetFirstChild(), metadata);
                if (child is not null) return child;
            }
            return null;
        }
        var noInstruction = Find(instructionTree.GetRoot(), "no");
        noInstruction?.Select(0);
        instructionTree.EmitSignal(Tree.SignalName.ItemActivated);
        var instructionAdded = ContactCount() == startingContacts + 1;
        remove.EmitSignal(BaseButton.SignalName.Pressed);
        var instructionRestored = ContactCount() == startingContacts;
        var edgeMemoryTagStartingCount = _ladderDocument.Tags.Count;
        var alternateEdgeMemoryOperand = _ladderDocument.CreateEdgeMemoryOperand();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        bool VerifyEdgePropertyEdit(string nodeId, LadderEdgeMode expectedMode)
        {
            if (FindContactPosition(nodeId) is not { } position)
            {
                GD.Print($"TIA_EDGE_PROPERTY_EDIT_DETAIL mode={expectedMode} position=False");
                return false;
            }
            EditableLadderNode? CurrentEdge() => _ladderDocument.Rungs
                .Select(rung => EditableLadderTree.Find(rung.Root, nodeId))
                .FirstOrDefault(node => node is not null);
            var original = CurrentEdge();
            if (original is null || original.EdgeMode != expectedMode
                || string.IsNullOrWhiteSpace(original.EdgeMemoryOperand))
            {
                GD.Print($"TIA_EDGE_PROPERTY_EDIT_DETAIL mode={expectedMode} original=False actual={original?.EdgeMode} memory={original?.EdgeMemoryOperand}");
                return false;
            }
            var originalSignal = original.Variable;
            var originalMemory = original.EdgeMemoryOperand;
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                DoubleClick = true,
                Position = editorCanvas.GetElementPosition(position.Rung, position.Branch, position.Contact),
            });
            var expectedTypeIndex = expectedMode == LadderEdgeMode.Rising ? 2 : 3;
            var alternateMemoryIndex = Enumerable.Range(1, Math.Max(0, edgeMemorySelector.ItemCount - 1))
                .FirstOrDefault(index => edgeMemorySelector.GetItemText(index) == alternateEdgeMemoryOperand, -1);
            var opened = contextualPropertiesPopup.Visible && contactPropertyRow.Visible
                && contactTypeSelector.Selected == expectedTypeIndex
                && edgeMemorySelector.Visible && alternateMemoryIndex >= 1;
            if (!opened)
            {
                GD.Print($"TIA_EDGE_PROPERTY_EDIT_DETAIL mode={expectedMode} opened=False popup={contextualPropertiesPopup.Visible} row={contactPropertyRow.Visible} type={contactTypeSelector.Selected}/{expectedTypeIndex} memoryVisible={edgeMemorySelector.Visible} alternateIndex={alternateMemoryIndex} items={edgeMemorySelector.ItemCount} originalMemory={originalMemory} alternateMemory={alternateEdgeMemoryOperand}");
                contextualPropertiesPopup.Hide();
                return false;
            }
            edgeMemorySelector.Select(alternateMemoryIndex);
            edgeMemorySelector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)alternateMemoryIndex);
            applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
            var applied = CurrentEdge();
            var applyWorked = applied is not null && applied.Id == nodeId
                && applied.EdgeMode == expectedMode && applied.Variable == originalSignal
                && applied.EdgeMemoryOperand == alternateEdgeMemoryOperand && _ladderHistory.CanUndo;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var undone = CurrentEdge();
            var undoWorkedForEdge = undone is not null && undone.Id == nodeId
                && undone.EdgeMode == expectedMode && undone.Variable == originalSignal
                && undone.EdgeMemoryOperand == originalMemory;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            var redone = CurrentEdge();
            var redoWorkedForEdge = redone is not null && redone.Id == nodeId
                && redone.EdgeMode == expectedMode && redone.Variable == originalSignal
                && redone.EdgeMemoryOperand == alternateEdgeMemoryOperand;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            GD.Print($"TIA_EDGE_PROPERTY_EDIT_DETAIL mode={expectedMode} opened={opened} apply={applyWorked} undo={undoWorkedForEdge} redo={redoWorkedForEdge} originalMemory={originalMemory} alternateMemory={alternateEdgeMemoryOperand} appliedMemory={applied?.EdgeMemoryOperand} undoneMemory={undone?.EdgeMemoryOperand} redoneMemory={redone?.EdgeMemoryOperand} status={instructionPropertyStatus.Text}");
            return applyWorked && undoWorkedForEdge && redoWorkedForEdge;
        }
        addRisingEdge.EmitSignal(BaseButton.SignalName.Pressed);
        var insertedRisingEdge = _ladderDocument.Rungs.SelectMany(rung => rung.Branches)
            .SelectMany(branch => branch.Contacts).FirstOrDefault(contact => contact.EdgeMode == LadderEdgeMode.Rising);
        var risingEdgeInserted = insertedRisingEdge is not null;
        var risingEdgePropertyEditWorked = insertedRisingEdge is not null
            && VerifyEdgePropertyEdit(insertedRisingEdge.Id, LadderEdgeMode.Rising);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        addFallingEdge.EmitSignal(BaseButton.SignalName.Pressed);
        var insertedFallingEdge = _ladderDocument.Rungs.SelectMany(rung => rung.Branches)
            .SelectMany(branch => branch.Contacts).FirstOrDefault(contact => contact.EdgeMode == LadderEdgeMode.Falling);
        var fallingEdgeInserted = insertedFallingEdge is not null;
        var fallingEdgePropertyEditWorked = insertedFallingEdge is not null
            && VerifyEdgePropertyEdit(insertedFallingEdge.Id, LadderEdgeMode.Falling);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        while (_ladderDocument.Tags.Count > edgeMemoryTagStartingCount)
            _ladderDocument.Tags.RemoveAt(_ladderDocument.Tags.Count - 1);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        GD.Print($"TIA_EDGE_PROPERTY_EDIT_VERIFY {(risingEdgePropertyEditWorked && fallingEdgePropertyEditWorked ? "PASS" : "FAIL")} rising={risingEdgePropertyEditWorked} falling={fallingEdgePropertyEditWorked}");
        var exactInsertionBranch = _ladderDocument.Rungs[0].Branches[0];
        var exactInsertionIds = exactInsertionBranch.Contacts.Select(contact => contact.Id).ToArray();
        editorCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = editorCanvas.GetInsertionPointPosition(0, 0, 1),
        });
        var insertionCursorSelected = editorCanvas.SelectedInsertionIndex == 1
            && editorCanvas.SelectedContact == -1;
        addNo.EmitSignal(BaseButton.SignalName.Pressed);
        var exactInsertionWorked = exactInsertionBranch.Contacts.Count == exactInsertionIds.Length + 1
            && exactInsertionBranch.Contacts[0].Id == exactInsertionIds[0]
            && exactInsertionBranch.Contacts[2].Id == exactInsertionIds[1]
            && editorCanvas.SelectedContact == 1;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var exactInsertionUndone = _ladderDocument.Rungs[0].Branches[0].Contacts.Select(contact => contact.Id)
            .SequenceEqual(exactInsertionIds);
        var dragDropPosition = editorCanvas.GetInsertionPointPosition(0, 0, 1);
        var dragPreviewWorked = editorCanvas.PreviewInsertionAt(dragDropPosition)
            && editorCanvas.SelectedInsertionIndex == 1;
        var dragDropBefore = ContactCount();
        var dragDropCommitted = editorCanvas.DropInstructionAt("no", dragDropPosition);
        var dragDropWorked = dragDropCommitted && ContactCount() == dragDropBefore + 1
            && editorCanvas.SelectedContact == 1;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var dragDropUndone = ContactCount() == dragDropBefore;
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var pointerBranchOriginalProject = LadderEditorProjectJson.Save(_ladderDocument);
        var pointerBranchOriginalIds = EditableLadderTree.Visit(_ladderDocument.Rungs[0].Root)
            .Select(node => node.Id).ToArray();
        var pointerBranchPosition = editorCanvas.GetInsertionPointPosition(0, 0, 1);
        editorCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = pointerBranchPosition,
        });
        var pointerBranchTarget = editorCanvas.CaptureTreeTarget();
        var pointerBranchSlotSelected = pointerBranchTarget.Slot == 1
            && pointerBranchTarget.ParentId == _ladderDocument.Rungs[0].Branches[0].Id;
        var pointerBranchPreviewed = editorCanvas.PreviewInsertionAt(pointerBranchPosition);
        var pointerBranchDropped = editorCanvas.DropInstructionAt("branch", pointerBranchPosition);
        var pointerBranchNewTarget = editorCanvas.CaptureTreeTarget();
        var pointerBranchNewPathId = pointerBranchNewTarget.ParentId;
        var pointerBranchContactDropped = pointerBranchDropped
            && pointerBranchNewTarget.Slot == 0
            && editorCanvas.DropInstructionAt("no", editorCanvas.GetSelectionPosition());
        var pointerBranchNewPath = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, pointerBranchNewPathId);
        var pointerBranchContactId = pointerBranchNewPath?.Children is { Count: 1 }
            ? pointerBranchNewPath.Children[0].Id : string.Empty;
        var pointerBranchEditedProject = LadderEditorProjectJson.Save(_ladderDocument);
        var pointerBranchEditedIds = EditableLadderTree.Visit(_ladderDocument.Rungs[0].Root)
            .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
        var pointerBranchStableIds = pointerBranchOriginalIds.All(pointerBranchEditedIds.Contains)
            && pointerBranchNewPathId.Length > 0 && pointerBranchContactId.Length > 0
            && !pointerBranchOriginalIds.Contains(pointerBranchNewPathId)
            && !pointerBranchOriginalIds.Contains(pointerBranchContactId);
        var pointerBranchCompile = LadderCompiler.Compile(_ladderDocument.BuildProgram());
        var pointerBranchRemainsOpen = pointerBranchNewPath?.OpenEnd == true
            && !pointerBranchCompile.IsValid
            && pointerBranchCompile.Issues.Any(issue => issue.Code == "VC003"
                && issue.Path.EndsWith(".openEnd", StringComparison.Ordinal)
                && issue.Message.Contains("must be closed", StringComparison.OrdinalIgnoreCase));
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var pointerBranchUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == pointerBranchOriginalProject;
        redo.EmitSignal(BaseButton.SignalName.Pressed);
        redo.EmitSignal(BaseButton.SignalName.Pressed);
        var pointerBranchRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == pointerBranchEditedProject;
        var pointerBranchEditWorked = pointerBranchSlotSelected && pointerBranchPreviewed
            && pointerBranchDropped && pointerBranchContactDropped && pointerBranchStableIds
            && pointerBranchRemainsOpen && pointerBranchUndoExact && pointerBranchRedoExact;
        GD.Print($"TIA_POINTER_BRANCH_ROUTE_VERIFY {(pointerBranchEditWorked ? "PASS" : "FAIL")} "
            + $"parent={pointerBranchTarget.ParentId} slot={pointerBranchTarget.Slot} newPath={pointerBranchNewPathId} "
            + $"contact={pointerBranchContactId} stableIds={pointerBranchStableIds} open={pointerBranchRemainsOpen} "
            + $"undo={pointerBranchUndoExact} redo={pointerBranchRedoExact}");
        var pointerBranchSplit = EditableLadderTree.Visit(_ladderDocument.Rungs[0].Root)
            .FirstOrDefault(node => node.Kind == LadderNodeKind.Parallel
                && node.Children?.Any(child => child.Id == pointerBranchNewPathId) == true);
        var pointerBranchMainPath = pointerBranchSplit?.Children is { Count: 2 }
            ? pointerBranchSplit.Children[0] : null;
        var branchCloseStarted = false;
        var branchClosePreviewed = false;
        var branchCloseApplied = false;
        var branchCloseStableIds = false;
        var branchCloseCompiles = false;
        var branchCloseUndoExact = false;
        var branchCloseRedoExact = false;
        var currentJoinCloseWorked = false;
        if (pointerBranchMainPath?.Children is { Count: > 1 } mainChildren)
        {
            var currentJoinStart = editorCanvas.GetTreeOutputWirePosition(0, pointerBranchNewPathId);
            var currentJoinTarget = editorCanvas.GetTreeSlotPosition(0, pointerBranchMainPath.Id, mainChildren.Count);
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = currentJoinStart,
            });
            var currentJoinStarted = editorCanvas.ReviewBranchCloseDragActive;
            editorCanvas._GuiInput(new InputEventMouseMotion
            {
                Position = currentJoinTarget,
                ButtonMask = MouseButtonMask.Left,
            });
            var currentJoinPreviewed = editorCanvas.ReviewBranchCloseDragActive
                && editorCanvas.ReviewBranchCloseCandidateSlot == mainChildren.Count;
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = false,
                Position = currentJoinTarget,
            });
            var currentJoinPath = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, pointerBranchNewPathId);
            var currentJoinProject = LadderEditorProjectJson.Save(_ladderDocument);
            var currentJoinApplied = currentJoinPath?.OpenEnd == false
                && pointerBranchEditedIds.SetEquals(EditableLadderTree.Visit(_ladderDocument.Rungs[0].Root).Select(node => node.Id))
                && LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var currentJoinUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == pointerBranchEditedProject;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            var currentJoinRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == currentJoinProject;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            currentJoinCloseWorked = currentJoinStarted && currentJoinPreviewed && currentJoinApplied
                && currentJoinUndoExact && currentJoinRedoExact;
            GD.Print($"TIA_BRANCH_CLOSE_CURRENT_JOIN_VERIFY {(currentJoinCloseWorked ? "PASS" : "FAIL")} "
                + $"path={pointerBranchNewPathId} slot={mainChildren.Count} started={currentJoinStarted} "
                + $"preview={currentJoinPreviewed} applied={currentJoinApplied} undo={currentJoinUndoExact} redo={currentJoinRedoExact}");

            var trailingId = mainChildren[1].Id;
            var closeStart = editorCanvas.GetTreeOutputWirePosition(0, pointerBranchNewPathId);
            var closeTarget = editorCanvas.GetTreeSlotPosition(0, pointerBranchMainPath.Id, 1);
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = closeStart,
            });
            branchCloseStarted = editorCanvas.ReviewBranchCloseDragActive;
            editorCanvas._GuiInput(new InputEventMouseMotion
            {
                Position = closeTarget,
                ButtonMask = MouseButtonMask.Left,
            });
            branchClosePreviewed = editorCanvas.ReviewBranchCloseDragActive
                && editorCanvas.ReviewBranchCloseCandidateSlot == 1;
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = false,
                Position = closeTarget,
            });
            var closedRoot = _ladderDocument.Rungs[0].Root;
            var closedSplit = EditableLadderTree.Visit(closedRoot)
                .FirstOrDefault(node => node.Id == pointerBranchSplit!.Id);
            var closedContainer = closedSplit is null ? null : EditableLadderTree.Visit(closedRoot)
                .FirstOrDefault(node => node.Kind == LadderNodeKind.Series
                    && node.Children?.Any(child => child.Id == closedSplit.Id) == true);
            var closedIndex = closedContainer?.Children?.ToList().FindIndex(child => child.Id == closedSplit?.Id) ?? -1;
            branchCloseApplied = !editorCanvas.ReviewBranchCloseDragActive
                && closedSplit?.Children?[0].Children?.Count == 1
                && closedIndex >= 0 && closedIndex + 1 < closedContainer!.Children!.Count
                && closedContainer.Children[closedIndex + 1].Id == trailingId;
            var closedIds = EditableLadderTree.Visit(closedRoot).Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            branchCloseStableIds = pointerBranchEditedIds.SetEquals(closedIds);
            branchCloseCompiles = LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
            var branchClosedProject = LadderEditorProjectJson.Save(_ladderDocument);
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            branchCloseUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == pointerBranchEditedProject;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            branchCloseRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == branchClosedProject;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
        }
        var branchCloseWorked = currentJoinCloseWorked && branchCloseStarted && branchClosePreviewed && branchCloseApplied
            && branchCloseStableIds && branchCloseCompiles && branchCloseUndoExact && branchCloseRedoExact;
        GD.Print($"TIA_BRANCH_CLOSE_ROUTE_VERIFY {(branchCloseWorked ? "PASS" : "FAIL")} "
            + $"path={pointerBranchNewPathId} main={pointerBranchMainPath?.Id ?? "missing"} "
            + $"started={branchCloseStarted} preview={branchClosePreviewed} applied={branchCloseApplied} "
            + $"stableIds={branchCloseStableIds} compile={branchCloseCompiles} "
            + $"undo={branchCloseUndoExact} redo={branchCloseRedoExact}");
        var multiPathBranchCloseWorked = false;
        if (pointerBranchSplit is not null
            && pointerBranchMainPath?.Children is { Count: > 1 } multiMainChildren)
        {
            var firstCloseStart = editorCanvas.GetTreeOutputWirePosition(0, pointerBranchNewPathId);
            var firstCloseTarget = editorCanvas.GetTreeSlotPosition(0, pointerBranchMainPath.Id, multiMainChildren.Count);
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = firstCloseStart,
            });
            editorCanvas._GuiInput(new InputEventMouseMotion
            {
                Position = firstCloseTarget,
                ButtonMask = MouseButtonMask.Left,
            });
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = false,
                Position = firstCloseTarget,
            });
            var firstPathClosed = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, pointerBranchNewPathId)?.OpenEnd == false;
            var outerAfterFirstClose = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, pointerBranchSplit.Id);
            var thirdPathId = string.Empty;
            var thirdContactId = string.Empty;
            var thirdOpened = false;
            var thirdContactInserted = false;
            if (firstPathClosed && outerAfterFirstClose?.Children is { Count: >= 2 } outerPaths)
            {
                var newPathPosition = editorCanvas.GetTreeSlotPosition(0, outerAfterFirstClose.Id, outerPaths.Count);
                editorCanvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    Pressed = true,
                    Position = newPathPosition,
                });
                var branchTarget = editorCanvas.CaptureTreeTarget();
                thirdOpened = branchTarget.ParentId == outerAfterFirstClose.Id
                    && branchTarget.Slot == outerPaths.Count
                    && editorCanvas.PreviewInsertionAt(newPathPosition)
                    && editorCanvas.DropInstructionAt("branch", newPathPosition);
                var thirdTarget = editorCanvas.CaptureTreeTarget();
                thirdPathId = thirdTarget.ParentId;
                thirdContactInserted = thirdOpened && thirdTarget.Slot == 0
                    && editorCanvas.DropInstructionAt("no", editorCanvas.GetSelectionPosition());
                var thirdPath = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, thirdPathId);
                thirdContactId = thirdPath?.Children is { Count: 1 } ? thirdPath.Children[0].Id : string.Empty;
            }
            var beforeMultiPathClose = LadderEditorProjectJson.Save(_ladderDocument);
            var idsBeforeMultiPathClose = EditableLadderTree.Visit(_ladderDocument.Rungs[0].Root)
                .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            var multiCloseStarted = false;
            var multiClosePreviewed = false;
            var multiCloseApplied = false;
            var multiCloseUndoExact = false;
            var multiCloseRedoExact = false;
            if (thirdContactInserted && thirdPathId.Length > 0)
            {
                var multiCloseStart = editorCanvas.GetTreeOutputWirePosition(0, thirdPathId);
                var multiCloseTarget = editorCanvas.GetTreeSlotPosition(0, pointerBranchMainPath.Id, 1);
                editorCanvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    Pressed = true,
                    Position = multiCloseStart,
                });
                multiCloseStarted = editorCanvas.ReviewBranchCloseDragActive;
                editorCanvas._GuiInput(new InputEventMouseMotion
                {
                    Position = multiCloseTarget,
                    ButtonMask = MouseButtonMask.Left,
                });
                multiClosePreviewed = editorCanvas.ReviewBranchCloseDragActive
                    && editorCanvas.ReviewBranchCloseCandidateSlot == 1;
                editorCanvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    Pressed = false,
                    Position = multiCloseTarget,
                });
                var multiRoot = _ladderDocument.Rungs[0].Root;
                var multiOuter = EditableLadderTree.Find(multiRoot, pointerBranchSplit.Id);
                var idsAfterMultiPathClose = EditableLadderTree.Visit(multiRoot)
                    .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
                multiCloseApplied = multiOuter?.Children is { Count: 2 } remainingOuterPaths
                    && remainingOuterPaths[0].Children is { Count: 2 } restructuredMain
                    && restructuredMain[0].Kind == LadderNodeKind.Parallel
                    && restructuredMain[0].Children is { Count: 2 } nestedPaths
                    && nestedPaths[1].Id == thirdPathId && !nestedPaths[1].OpenEnd
                    && restructuredMain[1].Id == multiMainChildren[1].Id
                    && remainingOuterPaths[1].Id == pointerBranchNewPathId
                    && idsBeforeMultiPathClose.IsSubsetOf(idsAfterMultiPathClose)
                    && idsAfterMultiPathClose.Count == idsBeforeMultiPathClose.Count + 2
                    && LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
                var afterMultiPathClose = LadderEditorProjectJson.Save(_ladderDocument);
                undo.EmitSignal(BaseButton.SignalName.Pressed);
                multiCloseUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == beforeMultiPathClose;
                redo.EmitSignal(BaseButton.SignalName.Pressed);
                multiCloseRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == afterMultiPathClose;
                undo.EmitSignal(BaseButton.SignalName.Pressed);
            }
            if (thirdContactInserted)
            {
                undo.EmitSignal(BaseButton.SignalName.Pressed);
                undo.EmitSignal(BaseButton.SignalName.Pressed);
            }
            if (firstPathClosed) undo.EmitSignal(BaseButton.SignalName.Pressed);
            var multiPathRestored = LadderEditorProjectJson.Save(_ladderDocument) == pointerBranchEditedProject;
            multiPathBranchCloseWorked = firstPathClosed && thirdOpened && thirdContactInserted
                && thirdPathId.Length > 0 && thirdContactId.Length > 0
                && multiCloseStarted && multiClosePreviewed && multiCloseApplied
                && multiCloseUndoExact && multiCloseRedoExact && multiPathRestored;
            GD.Print($"TIA_MULTIPATH_BRANCH_CLOSE_ROUTE_VERIFY {(multiPathBranchCloseWorked ? "PASS" : "FAIL")} "
                + $"outer={pointerBranchSplit.Id} path={thirdPathId} contact={thirdContactId} "
                + $"opened={thirdOpened} inserted={thirdContactInserted} started={multiCloseStarted} "
                + $"preview={multiClosePreviewed} applied={multiCloseApplied} undo={multiCloseUndoExact} "
                + $"redo={multiCloseRedoExact} restored={multiPathRestored}");
        }
        var branchConnection = editorCanvas.GetTreeInputWirePosition(0, pointerBranchNewPathId);
        editorCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Right,
            Pressed = true,
            Position = branchConnection,
        });
        var branchConnectionSelected = editorCanvas.SelectedNodeId == pointerBranchNewPathId;
        var branchDeleteMenuMatched = editorCanvas.ReviewTreePopupItems().Contains("Delete branch")
            && !editorCanvas.ReviewTreePopupItems().Contains("Remove selected subtree");
        var branchDeleteMenuActivated = editorCanvas.ReviewActivateTreePopup(LadderTreeCommandId.Delete);
        var branchDeleted = branchDeleteMenuActivated
            && EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, pointerBranchNewPathId) is null
            && LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
        var branchDeletedProject = LadderEditorProjectJson.Save(_ladderDocument);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var branchDeleteUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == pointerBranchEditedProject;
        redo.EmitSignal(BaseButton.SignalName.Pressed);
        var branchDeleteRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == branchDeletedProject;
        var branchConnectionDeleteWorked = branchConnectionSelected && branchDeleteMenuMatched
            && branchDeleted && branchDeleteUndoExact && branchDeleteRedoExact && tiaRemoveBranchShortcutHidden;
        GD.Print($"TIA_BRANCH_CONNECTION_DELETE_ROUTE_VERIFY {(branchConnectionDeleteWorked ? "PASS" : "FAIL")} "
            + $"path={pointerBranchNewPathId} pointer={branchConnection} selected={branchConnectionSelected} "
            + $"menu={branchDeleteMenuMatched} compile={branchDeleted} undo={branchDeleteUndoExact} "
            + $"redo={branchDeleteRedoExact} toolbarShortcutHidden={tiaRemoveBranchShortcutHidden}");
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        editorCanvas.SelectTreeNode(0, pointerBranchContactId, notify: false);
        var lastElementPosition = editorCanvas.GetSelectionPosition();
        editorCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Right,
            Pressed = true,
            Position = lastElementPosition,
        });
        var lastElementSelected = editorCanvas.SelectedNodeId == pointerBranchContactId;
        var lastElementMenuMatched = editorCanvas.ReviewTreePopupItems().Contains("Delete LAD element")
            && !editorCanvas.ReviewTreePopupItems().Contains("Delete branch");
        var lastElementMenuActivated = editorCanvas.ReviewActivateTreePopup(LadderTreeCommandId.Delete);
        var lastElementCollapsedBranch = lastElementMenuActivated
            && EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, pointerBranchContactId) is null
            && EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, pointerBranchNewPathId) is null
            && LadderCompiler.Compile(_ladderDocument.BuildProgram()).IsValid;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var lastElementUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == pointerBranchEditedProject;
        redo.EmitSignal(BaseButton.SignalName.Pressed);
        var lastElementRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == branchDeletedProject;
        var lastElementBranchCollapseWorked = lastElementSelected && lastElementMenuMatched
            && lastElementCollapsedBranch && lastElementUndoExact && lastElementRedoExact;
        GD.Print($"TIA_LAST_BRANCH_ELEMENT_DELETE_VERIFY {(lastElementBranchCollapseWorked ? "PASS" : "FAIL")} "
            + $"contact={pointerBranchContactId} path={pointerBranchNewPathId} selected={lastElementSelected} "
            + $"menu={lastElementMenuMatched} collapsed={lastElementCollapsedBranch} "
            + $"undo={lastElementUndoExact} redo={lastElementRedoExact}");
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var reorderBranch = _ladderDocument.Rungs[0].Branches[0];
        var firstContactId = reorderBranch.Contacts[0].Id;
        var secondContactId = reorderBranch.Contacts[1].Id;
        editorCanvas.SelectElement(0, 0, 0);
        moveRight.EmitSignal(BaseButton.SignalName.Pressed);
        var exactMoveWorked = reorderBranch.Contacts[0].Id == secondContactId
            && reorderBranch.Contacts[1].Id == firstContactId
            && editorCanvas.SelectedContact == 1;
        moveLeft.EmitSignal(BaseButton.SignalName.Pressed);
        var exactMoveRestored = reorderBranch.Contacts[0].Id == firstContactId
            && editorCanvas.SelectedContact == 0;
        editorCanvas.SelectElement(0, 0, 0);
        remove.EmitSignal(BaseButton.SignalName.Pressed);
        var exactDeleteWorked = reorderBranch.Contacts.All(contact => contact.Id != firstContactId);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var exactDeleteUndone = _ladderDocument.Rungs[0].Branches[0].Contacts[0].Id == firstContactId;
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var clipboardBranch = _ladderDocument.Rungs[0].Branches[0];
        var clipboardSource = clipboardBranch.Contacts[0];
        var clipboardContactCount = clipboardBranch.Contacts.Count;
        // Exercise shortcut ownership with real Godot focus/visibility state.
        // This isolated surface cannot alter the user's editor document/history.
        var shortcutProbe = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, new LadderEditorDocument());
        AddChild(shortcutProbe);
        var shortcutRequests = 0;
        shortcutProbe.DeleteRequested += () => shortcutRequests++;
        shortcutProbe.ReleaseFocus();
        shortcutProbe._ShortcutInput(new InputEventKey { Keycode = Key.Delete, Pressed = true });
        var unfocusedShortcutIgnored = shortcutRequests == 0;
        shortcutProbe.GrabFocus();
        shortcutProbe._ShortcutInput(new InputEventKey { Keycode = Key.Delete, Pressed = true });
        var focusedShortcutAccepted = shortcutRequests == 1;
        var keyboardInstructionRequests = new List<string>();
        shortcutProbe.KeyboardInstructionRequested += keyboardInstructionRequests.Add;
        shortcutProbe._ShortcutInput(new InputEventKey { Keycode = Key.F9, Pressed = true });
        shortcutProbe._ShortcutInput(new InputEventKey { Keycode = Key.F10, Pressed = true });
        var unmodifiedContactShortcutCount = keyboardInstructionRequests.Count;
        shortcutProbe._ShortcutInput(new InputEventKey { Keycode = Key.F9, CtrlPressed = true, Pressed = true });
        shortcutProbe._ShortcutInput(new InputEventKey { Keycode = Key.F10, ShiftPressed = true, Pressed = true });
        var tiaContactShortcutsWorked = keyboardInstructionRequests.SequenceEqual(new[] { "no", "nc" })
            && unmodifiedContactShortcutCount == 2;
        shortcutProbe.Hide();
        shortcutProbe._ShortcutInput(new InputEventKey { Keycode = Key.Delete, Pressed = true });
        var hiddenShortcutIgnored = shortcutRequests == 1;
        // Also use the actual viewport dispatch route, not only direct callbacks.
        var otherShortcutFocus = new Control { FocusMode = Control.FocusModeEnum.All };
        AddChild(otherShortcutFocus);
        shortcutProbe.Show();
        otherShortcutFocus.GrabFocus();
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Delete, Pressed = true });
        var unrelatedFocusDispatchIgnored = shortcutRequests == 1;
        shortcutProbe.GrabFocus();
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Delete, Pressed = true });
        var focusedDispatchAccepted = shortcutRequests == 2;
        var arrowMoveRequests = 0;
        shortcutProbe.MoveRequested += direction => arrowMoveRequests += direction;
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Right, AltPressed = true, Pressed = true });
        var focusedAltArrowMovesInsteadOfNavigating = arrowMoveRequests == 1 && shortcutProbe.HasFocus();
        shortcutProbe._GuiInput(new InputEventKey { Keycode = Key.Right, AltPressed = true, Pressed = true });
        var guiArrowConsumedBeforeFocusNavigation = arrowMoveRequests == 2 && shortcutProbe.HasFocus();
        shortcutProbe.Hide();
        otherShortcutFocus.GrabFocus();
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Delete, Pressed = true });
        var hiddenDispatchIgnored = shortcutRequests == 2;
        otherShortcutFocus.Free();
        shortcutProbe.Free();
        GD.Print($"LADDER_SHORTCUT_FOCUS_VERIFY unfocusedIgnored={unfocusedShortcutIgnored} focusedAccepted={focusedShortcutAccepted} tiaF9F10={tiaContactShortcutsWorked} hiddenIgnored={hiddenShortcutIgnored} dispatch={unrelatedFocusDispatchIgnored}/{focusedDispatchAccepted}/{hiddenDispatchIgnored} altArrowMoves={focusedAltArrowMovesInsteadOfNavigating}");
        if (!unfocusedShortcutIgnored || !focusedShortcutAccepted || !hiddenShortcutIgnored
            || !tiaContactShortcutsWorked
            || !unrelatedFocusDispatchIgnored || !focusedDispatchAccepted || !hiddenDispatchIgnored
            || !focusedAltArrowMovesInsteadOfNavigating || !guiArrowConsumedBeforeFocusNavigation)
        {
            result = "shortcut focus/visibility ownership failed";
            return false;
        }
        editorCanvas.SelectElement(0, 0, 0);
        editorCanvas.GrabFocus();
        editorCanvas._ShortcutInput(new InputEventKey { Keycode = Key.C, CtrlPressed = true, Pressed = true });
        editorCanvas.SelectInsertionPoint(0, 0, 1);
        editorCanvas._ShortcutInput(new InputEventKey { Keycode = Key.V, CtrlPressed = true, Pressed = true });
        var instructionClipboardWorked = clipboardBranch.Contacts.Count == clipboardContactCount + 1
            && clipboardBranch.Contacts[1].Variable == clipboardSource.Variable
            && clipboardBranch.Contacts[1].Id != clipboardSource.Id;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var instructionClipboardUndone = _ladderDocument.Rungs[0].Branches[0].Contacts.Count == clipboardContactCount;
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        editorCanvas.SelectInsertionPoint(0, 0, 1);
        editorCanvas.GrabFocus();
        editorCanvas._ShortcutInput(new InputEventKey { Keycode = Key.F9, Pressed = true });
        var keyboardNoInserted = _ladderDocument.Rungs[0].Branches[0].Contacts.Count == clipboardContactCount + 1
            && !_ladderDocument.Rungs[0].Branches[0].Contacts[1].NormallyClosed;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        editorCanvas.SelectInsertionPoint(0, 0, 1);
        editorCanvas.GrabFocus();
        editorCanvas._ShortcutInput(new InputEventKey { Keycode = Key.F10, Pressed = true });
        var keyboardNcInserted = _ladderDocument.Rungs[0].Branches[0].Contacts.Count == clipboardContactCount + 1
            && _ladderDocument.Rungs[0].Branches[0].Contacts[1].NormallyClosed;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var clipboardRungCount = _ladderDocument.Rungs.Count;
        var clipboardRung = _ladderDocument.Rungs[0];
        var clipboardRungIds = clipboardRung.Branches.SelectMany(branch => branch.Contacts)
            .Select(contact => contact.Id).ToHashSet(StringComparer.Ordinal);
        editorCanvas._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Right,
            Position = editorCanvas.GetRungBounds(0).Position + new Vector2(20, 15) });
        instructionContextMenu.EmitSignal(PopupMenu.SignalName.IdPressed, 6L);
        instructionContextMenu.EmitSignal(PopupMenu.SignalName.IdPressed, 4L);
        var pastedRung = _ladderDocument.Rungs.Count == clipboardRungCount + 1
            ? _ladderDocument.Rungs[1]
            : null;
        var rungClipboardWorked = pastedRung is not null
            && pastedRung.Id != clipboardRung.Id
            && pastedRung.Branches.SelectMany(branch => branch.Contacts)
                .All(contact => !clipboardRungIds.Contains(contact.Id));
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var rungClipboardUndone = _ladderDocument.Rungs.Count == clipboardRungCount;
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        editorCanvas.SelectElement(0, 0, 0);
        var startingBranches = _ladderDocument.Rungs[0].Branches.Count;
        removeBranchButton.EmitSignal(BaseButton.SignalName.Pressed);
        var branchDeleteWorked = _ladderDocument.Rungs[0].Branches.Count == startingBranches - 1;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var branchDeleteUndone = _ladderDocument.Rungs[0].Branches.Count == startingBranches;
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        foreach (var form in _pendingEditorForms) if (form.HasPending()) form.Discard();
        editorTagName.Text = "verify_unused";
        editorTagName.EmitSignal(LineEdit.SignalName.TextChanged, "verify_unused");
        addTagButton.EmitSignal(BaseButton.SignalName.Pressed);
        var unusedTagIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == "verify_unused");
        if (unusedTagIndex >= 0)
        {
            SelectIndexedTreeRow(editorTagList, unusedTagIndex);
            editorTagList.EmitSignal(Tree.SignalName.ItemSelected);
            deleteTagButton.EmitSignal(BaseButton.SignalName.Pressed);
        }
        var unusedTagDeleted = unusedTagIndex >= 0 && _ladderDocument.Tags.All(tag => tag.Name != "verify_unused");
        var startTagIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == "start_command");
        if (startTagIndex >= 0)
        {
            SelectIndexedTreeRow(editorTagList, startTagIndex);
            editorTagList.EmitSignal(Tree.SignalName.ItemSelected);
            editorTagName.Text = "verify_start"; editorTagName.EmitSignal(LineEdit.SignalName.TextChanged, "verify_start");
            editorTagInitialValue.Text = "TRUE"; editorTagInitialValue.EmitSignal(LineEdit.SignalName.TextChanged, "TRUE");
            applyTagButton.EmitSignal(BaseButton.SignalName.Pressed);
        }
        GD.Print($"TAG_FORM_REVIEW pending={string.Join(",", _pendingEditorForms.ConvertAll(f => f.HasPending()))} status={output.Text}");
        var tagRenameWorked = _ladderDocument.Tags.Any(tag => tag.Name == "verify_start")
            && _ladderDocument.CountTagReferences("verify_start") > 0
            && _ladderDocument.CountTagReferences("start_command") == 0;
        var tagInitialValueWorked = _ladderDocument.Tags.FirstOrDefault(tag => tag.Name == "verify_start")?.InitialValue is true;
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var tagRenameUndone = _ladderDocument.Tags.Any(tag => tag.Name == "start_command")
            && _ladderDocument.CountTagReferences("start_command") > 0
            && _ladderDocument.Tags.Single(tag => tag.Name == "start_command").InitialValue is false;
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var originalTimerOutputs = _ladderDocument.Rungs
            .Select(rung => (Rung: rung, rung.IsTimer, rung.IsTimerReset, rung.TimerVariable, rung.TimerPreset,
                rung.TimerPresetAssigned, rung.TimerPresetOperand, rung.TimerResetOperand, rung.TimerKind,
                rung.TimerInstanceDataBlock, rung.IsCounter, rung.IsCounterReset, rung.IsCounterLoad, rung.CounterKind,
                rung.CounterVariable, rung.CounterPreset, rung.CounterValueType, rung.CounterInstanceDataBlock,
                rung.CounterLoadInput, rung.CounterResetInput))
            .ToArray();
        var startingTags = _ladderDocument.Tags.Count;
        var beforeTimerReset = _ladderDocument.CaptureSnapshot();
        var beforeTimerResetJson = LadderEditorProjectJson.Save(_ladderDocument);
        addTimerReset.EmitSignal(BaseButton.SignalName.Pressed);
        undo.EmitSignal(BaseButton.SignalName.Pressed);
        var firstTimerResetUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == beforeTimerResetJson;
        _ladderDocument.RestoreSnapshot(beforeTimerReset);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        // Undo restores block objects, so capture them again for the checks below.
        originalTimerOutputs = _ladderDocument.Rungs
            .Select(rung => (Rung: rung, rung.IsTimer, rung.IsTimerReset, rung.TimerVariable, rung.TimerPreset,
                rung.TimerPresetAssigned, rung.TimerPresetOperand, rung.TimerResetOperand, rung.TimerKind,
                rung.TimerInstanceDataBlock, rung.IsCounter, rung.IsCounterReset, rung.IsCounterLoad, rung.CounterKind,
                rung.CounterVariable, rung.CounterPreset, rung.CounterValueType, rung.CounterInstanceDataBlock,
                rung.CounterLoadInput, rung.CounterResetInput))
            .ToArray();
        GD.Print($"EDITOR_MUTATION_VERIFY firstTimerResetUndoExact={firstTimerResetUndoExact}");
        addTimer.EmitSignal(BaseButton.SignalName.Pressed);
        var timerInserted = _ladderDocument.Rungs.Any(rung => rung.IsTimer
            && _ladderDocument.Tags.Any(tag => tag.Name == rung.TimerVariable && tag.Type == PlcVariableType.Timer));
        addOffDelay.EmitSignal(BaseButton.SignalName.Pressed);
        var offDelayInserted = _ladderDocument.Rungs.Any(rung =>
            rung.IsTimer && rung.TimerKind == LadderTimerKind.OffDelay);
        addPulseTimer.EmitSignal(BaseButton.SignalName.Pressed);
        var pulseTimerInserted = _ladderDocument.Rungs.Any(rung =>
            rung.IsTimer && rung.TimerKind == LadderTimerKind.Pulse);
        addRetentiveTimer.EmitSignal(BaseButton.SignalName.Pressed);
        var retentiveTimerInserted = _ladderDocument.Rungs.Any(rung =>
            rung.IsTimer && rung.TimerKind == LadderTimerKind.RetentiveOnDelay);
        addTimerReset.EmitSignal(BaseButton.SignalName.Pressed);
        var timerResetInserted = _ladderDocument.Rungs.Any(rung => rung.IsTimerReset);
        foreach (var original in originalTimerOutputs)
        {
            original.Rung.IsTimer = original.IsTimer;
            original.Rung.IsTimerReset = original.IsTimerReset;
            original.Rung.TimerVariable = original.TimerVariable;
            original.Rung.TimerPreset = original.TimerPreset;
            original.Rung.TimerPresetAssigned = original.TimerPresetAssigned;
            original.Rung.TimerPresetOperand = original.TimerPresetOperand;
            original.Rung.TimerResetOperand = original.TimerResetOperand;
            original.Rung.TimerKind = original.TimerKind;
            original.Rung.TimerInstanceDataBlock = original.TimerInstanceDataBlock;
            original.Rung.IsCounter = original.IsCounter;
            original.Rung.IsCounterReset = original.IsCounterReset;
            original.Rung.IsCounterLoad = original.IsCounterLoad;
            original.Rung.CounterKind = original.CounterKind;
            original.Rung.CounterVariable = original.CounterVariable;
            original.Rung.CounterPreset = original.CounterPreset;
            original.Rung.CounterValueType = original.CounterValueType;
            original.Rung.CounterInstanceDataBlock = original.CounterInstanceDataBlock;
            original.Rung.CounterLoadInput = original.CounterLoadInput;
            original.Rung.CounterResetInput = original.CounterResetInput;
        }
        while (_ladderDocument.Tags.Count > startingTags) _ladderDocument.Tags.RemoveAt(_ladderDocument.Tags.Count - 1);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var originalCoilModes = _ladderDocument.Rungs.Select(rung => (Rung: rung, rung.CoilMode)).ToArray();
        var coilEditRungId = _ladderDocument.Rungs[0].Id;
        EditableRung CoilEditRung() => _ladderDocument.Rungs.First(rung => rung.Id == coilEditRungId);
        var originalCoilMode = CoilEditRung().CoilMode;
        var originalCoilOperand = CoilEditRung().CoilVariable;
        var originalCoilTerminalId = CoilEditRung().TerminalActionId;
        var alternateCoilOperandIndex = Enumerable.Range(0, coilOperandField.ItemCount)
            .FirstOrDefault(index => coilOperandField.GetItemId(index) != int.MaxValue
                && coilOperandField.GetItemText(index) != originalCoilOperand, -1);
        var tiaCoilPropertyEditWorked = alternateCoilOperandIndex >= 0;
        foreach (var mode in new[] { LadderCoilMode.Set, LadderCoilMode.Reset })
        {
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                DoubleClick = true,
                Position = editorCanvas.GetElementPosition(0, -1, -1, true),
            });
            var editorOpened = contextualPropertiesPopup.Visible && coilPropertyRow.Visible
                && coilModeField.ItemCount == 3
                && coilModeField.GetItemText(0) == "Assign (=)"
                && coilModeField.GetItemText(1) == "Set (S)"
                && coilModeField.GetItemText(2) == "Reset (R)";
            if (alternateCoilOperandIndex >= 0)
            {
                coilModeField.Select((int)mode);
                coilModeField.EmitSignal(OptionButton.SignalName.ItemSelected, (long)mode);
                coilOperandField.Select(alternateCoilOperandIndex);
                coilOperandField.EmitSignal(OptionButton.SignalName.ItemSelected, (long)alternateCoilOperandIndex);
                var editedOperand = coilOperandField.GetItemText(alternateCoilOperandIndex);
                applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
                var appliedRung = CoilEditRung();
                var applied = appliedRung.CoilMode == mode
                    && appliedRung.CoilVariable == editedOperand
                    && appliedRung.TerminalActionId == originalCoilTerminalId
                    && _ladderHistory.CanUndo;
                undo.EmitSignal(BaseButton.SignalName.Pressed);
                var undoneRung = CoilEditRung();
                var undone = undoneRung.CoilMode == originalCoilMode
                    && undoneRung.CoilVariable == originalCoilOperand
                    && undoneRung.TerminalActionId == originalCoilTerminalId;
                tiaCoilPropertyEditWorked &= editorOpened && applied && undone;
            }
            else
            {
                contextualPropertiesPopup.Hide();
                tiaCoilPropertyEditWorked = false;
            }
        }
        GD.Print($"TIA_COIL_PROPERTY_EDIT_VERIFY {(tiaCoilPropertyEditWorked ? "PASS" : "FAIL")}");
        addSet.EmitSignal(BaseButton.SignalName.Pressed);
        var setInserted = _ladderDocument.Rungs.Any(rung => rung.CoilMode == LadderCoilMode.Set);
        addReset.EmitSignal(BaseButton.SignalName.Pressed);
        var resetInserted = _ladderDocument.Rungs.Any(rung => rung.CoilMode == LadderCoilMode.Reset);
        foreach (var original in originalCoilModes) original.Rung.CoilMode = original.CoilMode;
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var originalCounterOutputs = _ladderDocument.Rungs
            .Select(rung => (Rung: rung, rung.IsTimer, rung.IsCounter, rung.IsCounterReset, rung.IsCounterLoad,
                rung.CounterKind, rung.CounterVariable, rung.CounterPreset, rung.CounterValueType, rung.CounterInstanceDataBlock, rung.CounterLoadInput, rung.CounterResetInput))
            .ToArray();
        var counterStartingTags = _ladderDocument.Tags.Count;
        addCounter.EmitSignal(BaseButton.SignalName.Pressed);
        var counterInserted = _ladderDocument.Rungs.Any(rung => rung.IsCounter
            && _ladderDocument.Tags.Any(tag => tag.Name == rung.CounterVariable && tag.Type == PlcVariableType.Counter));
        addCounterDown.EmitSignal(BaseButton.SignalName.Pressed);
        var counterDownInserted = _ladderDocument.Rungs.Any(rung => rung.IsCounter
            && rung.CounterKind == LadderCounterKind.CountDown);
        addCounterLoad.EmitSignal(BaseButton.SignalName.Pressed);
        var counterLoadInserted = _ladderDocument.Rungs.Any(rung => rung.IsCounterLoad);
        addCounterReset.EmitSignal(BaseButton.SignalName.Pressed);
        var counterResetInserted = _ladderDocument.Rungs.Any(rung => rung.IsCounterReset);
        foreach (var original in originalCounterOutputs)
        {
            original.Rung.IsTimer = original.IsTimer;
            original.Rung.IsCounter = original.IsCounter;
            original.Rung.IsCounterReset = original.IsCounterReset;
            original.Rung.IsCounterLoad = original.IsCounterLoad;
            original.Rung.CounterKind = original.CounterKind;
            original.Rung.CounterVariable = original.CounterVariable;
            original.Rung.CounterPreset = original.CounterPreset;
            original.Rung.CounterValueType = original.CounterValueType;
            original.Rung.CounterInstanceDataBlock = original.CounterInstanceDataBlock;
            original.Rung.CounterLoadInput = original.CounterLoadInput;
            original.Rung.CounterResetInput = original.CounterResetInput;
        }
        while (_ladderDocument.Tags.Count > counterStartingTags) _ladderDocument.Tags.RemoveAt(_ladderDocument.Tags.Count - 1);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var comparisonStartingTags = _ladderDocument.Tags.Count;
        var comparisonStartingContacts = ContactCount();
        _ladderDocument.AddTag("verify_numeric", PlcVariableRole.Memory, type: PlcVariableType.Real);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        editorCanvas.SelectInsertionPoint(0, 0, 1);
        addCompare.EmitSignal(BaseButton.SignalName.Pressed);
        var comparisonInserted = ContactCount() == comparisonStartingContacts + 1
            && _ladderDocument.Rungs[0].Branches[0].Contacts[1].IsComparison;
        var comparator = EditableLadderTree.Visit(_ladderDocument.Rungs[0].Root)
            .FirstOrDefault(node => node.Kind == LadderNodeKind.Compare);
        var comparatorTypeEditUndoRedo = false;
        if (comparisonInserted && comparator is not null)
        {
            editorCanvas.SelectTreeNode(0, comparator.Id);
            var typeSelector = editorCanvas.GetNode<OptionButton>("TiaComparisonValueTypeSelector");
            var oldType = comparator.ComparisonValueType;
            var requestedType = oldType == LadderComparisonValueType.Real
                ? LadderComparisonValueType.DInt : LadderComparisonValueType.Real;
            var requestedIndex = LadderComparisonValueTypePresentation.TiaSelectorIndex(requestedType);
            typeSelector.Select(requestedIndex);
            typeSelector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)requestedIndex);
            var appliedType = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, comparator.Id)?.ComparisonValueType;
            var applyWorked = typeSelector.Visible && appliedType == requestedType
                && _ladderHistory.UndoDescription == "Change comparator data type";
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var undoWorkedForType = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, comparator.Id)?.ComparisonValueType == oldType;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            var redoWorkedForType = EditableLadderTree.Find(_ladderDocument.Rungs[0].Root, comparator.Id)?.ComparisonValueType == requestedType;
            comparatorTypeEditUndoRedo = applyWorked && undoWorkedForType && redoWorkedForType;
        }
        remove.EmitSignal(BaseButton.SignalName.Pressed);
        while (_ladderDocument.Tags.Count > comparisonStartingTags) _ladderDocument.Tags.RemoveAt(_ladderDocument.Tags.Count - 1);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var numericStartingTags = _ladderDocument.Tags.Count;
        var originalNumericOutputs = _ladderDocument.Rungs.Select(rung => (
            Rung: rung,
            rung.IsTimer,
            rung.IsCounter,
            rung.IsCounterReset,
            rung.IsCounterLoad,
            rung.CounterKind,
            rung.IsNumericOperation,
            rung.NumericOperationKind,
            rung.NumericSourceA,
            rung.NumericSourceB,
            rung.NumericSourceC,
            rung.NumericDestination,
            rung.NumericDataType)).ToArray();
        _ladderDocument.AddTag("verify_numeric_write", PlcVariableRole.Memory, type: PlcVariableType.Real);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        addMove.EmitSignal(BaseButton.SignalName.Pressed);
        var numericInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Move);
        var numericSourceAField = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/NumericPropertyRow/NumericSourceA");
        var numericDestinationField = GetNodeOrNull<OptionButton>($"{contextualPropertiesRoot}/NumericPropertyRow/NumericDestination");
        bool VerifyNumericPropertyEdit(LadderNumericOperationKind kind, string editedA, string? editedB)
        {
            if (numericSourceAField is null || numericDestinationField is null) return false;
            var rungIndex = _ladderDocument.Rungs.FindIndex(rung => rung.IsNumericOperation
                && rung.NumericOperationKind == kind);
            if (rungIndex < 0) return false;
            var rungId = _ladderDocument.Rungs[rungIndex].Id;
            EditableRung? CurrentRung() => _ladderDocument.Rungs.FirstOrDefault(rung => rung.Id == rungId);
            var original = CurrentRung();
            if (original is null) return false;
            var originalA = original.NumericSourceA;
            var originalB = original.NumericSourceB;
            var originalDestination = original.NumericDestination;
            var originalTerminalId = original.TerminalActionId;

            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                DoubleClick = true,
                Position = editorCanvas.GetElementPosition(rungIndex, -1, -1, true),
            });
            var opened = contextualPropertiesPopup.Visible && numericPropertyRow.Visible
                && numericSourceAField.Editable && numericSourceBField.Editable == (editedB is not null)
                && !numericSourceCField.Editable && !numericDestinationField.Disabled;
            numericSourceAField.Text = editedA;
            numericSourceAField.EmitSignal(LineEdit.SignalName.TextChanged, editedA);
            if (editedB is not null)
            {
                numericSourceBField.Text = editedB;
                numericSourceBField.EmitSignal(LineEdit.SignalName.TextChanged, editedB);
            }
            var expectedB = editedB ?? originalB;
            applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
            var applied = CurrentRung();
            var appliedWorked = applied is not null
                && applied.NumericSourceA == editedA && applied.NumericSourceB == expectedB
                && applied.NumericDestination == originalDestination
                && applied.TerminalActionId == originalTerminalId
                && _ladderHistory.CanUndo;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var undone = CurrentRung();
            var undoWorked = undone is not null
                && undone.NumericSourceA == originalA && undone.NumericSourceB == originalB
                && undone.NumericDestination == originalDestination
                && undone.TerminalActionId == originalTerminalId;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            var redone = CurrentRung();
            var redoWorked = redone is not null
                && redone.NumericSourceA == editedA && redone.NumericSourceB == expectedB
                && redone.NumericDestination == originalDestination
                && redone.TerminalActionId == originalTerminalId;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            return opened && appliedWorked && undoWorked && redoWorked;
        }
        var movePropertyFieldsWorked = numericInserted && numericSourceAField is { Editable: true }
            && !numericSourceBField.Editable && !numericSourceCField.Editable
            && numericDestinationField is { Disabled: false, ItemCount: > 0 };
        var movePropertyEditWorked = movePropertyFieldsWorked
            && VerifyNumericPropertyEdit(LadderNumericOperationKind.Move, "42", null);
        addAdd.EmitSignal(BaseButton.SignalName.Pressed);
        var addPropertyFieldsWorked = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
                && rung.NumericOperationKind == LadderNumericOperationKind.Add)
            && numericSourceAField is { Editable: true }
            && numericSourceBField.Editable && !numericSourceCField.Editable
            && numericDestinationField is { Disabled: false, ItemCount: > 0 };
        var addPropertyEditWorked = addPropertyFieldsWorked
            && VerifyNumericPropertyEdit(LadderNumericOperationKind.Add, "7", "2");
        addSubtract.EmitSignal(BaseButton.SignalName.Pressed);
        var subtractPropertyFieldsWorked = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
                && rung.NumericOperationKind == LadderNumericOperationKind.Subtract)
            && numericSourceAField is { Editable: true }
            && numericSourceBField.Editable && !numericSourceCField.Editable
            && numericDestinationField is { Disabled: false, ItemCount: > 0 };
        var subtractPropertyEditWorked = subtractPropertyFieldsWorked
            && VerifyNumericPropertyEdit(LadderNumericOperationKind.Subtract, "9", "4");
        addMultiply.EmitSignal(BaseButton.SignalName.Pressed);
        var multiplyPropertyFieldsWorked = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
                && rung.NumericOperationKind == LadderNumericOperationKind.Multiply)
            && numericSourceAField is { Editable: true }
            && numericSourceBField.Editable && !numericSourceCField.Editable
            && numericDestinationField is { Disabled: false, ItemCount: > 0 };
        var multiplyPropertyEditWorked = multiplyPropertyFieldsWorked
            && VerifyNumericPropertyEdit(LadderNumericOperationKind.Multiply, "6", "3");
        addDivide.EmitSignal(BaseButton.SignalName.Pressed);
        var dividePropertyFieldsWorked = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
                && rung.NumericOperationKind == LadderNumericOperationKind.Divide)
            && numericSourceAField is { Editable: true }
            && numericSourceBField.Editable && !numericSourceCField.Editable
            && numericDestinationField is { Disabled: false, ItemCount: > 0 };
        var dividePropertyEditWorked = dividePropertyFieldsWorked
            && VerifyNumericPropertyEdit(LadderNumericOperationKind.Divide, "8", "2");
        addModulo.EmitSignal(BaseButton.SignalName.Pressed);
        var moduloPropertyFieldsWorked = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Modulo)
            && numericSourceAField is { Editable: true }
            && numericSourceBField.Editable && !numericSourceCField.Editable
            && numericDestinationField is { Disabled: false, ItemCount: > 0 };
        var moduloPropertyEditWorked = moduloPropertyFieldsWorked
            && VerifyNumericPropertyEdit(LadderNumericOperationKind.Modulo, "11", "3");
        var moduloInserted = moduloPropertyFieldsWorked;
        addSquareRoot.EmitSignal(BaseButton.SignalName.Pressed);
        var squareRootInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.SquareRoot)
            && !numericSourceBField.Editable;
        addExponentiate.EmitSignal(BaseButton.SignalName.Pressed);
        var exponentiateInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate)
            && numericSourceBField.Editable;
        addTruncate.EmitSignal(BaseButton.SignalName.Pressed);
        var truncateInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Truncate)
            && !numericSourceBField.Editable;
        addNormalize.EmitSignal(BaseButton.SignalName.Pressed);
        var normalizeInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Normalize)
            && numericSourceBField.Editable && numericSourceCField.Editable;
        addScale.EmitSignal(BaseButton.SignalName.Pressed);
        var scaleInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Scale)
            && numericSourceBField.Editable && numericSourceCField.Editable;
        addConvert.EmitSignal(BaseButton.SignalName.Pressed);
        var convertInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Convert);
        addRound.EmitSignal(BaseButton.SignalName.Pressed);
        var roundInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Round);
        addCeiling.EmitSignal(BaseButton.SignalName.Pressed);
        var ceilingInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Ceiling);
        addFloor.EmitSignal(BaseButton.SignalName.Pressed);
        var floorInserted = _ladderDocument.Rungs.Any(rung => rung.IsNumericOperation
            && rung.NumericOperationKind == LadderNumericOperationKind.Floor);
        foreach (var original in originalNumericOutputs)
        {
            original.Rung.IsTimer = original.IsTimer;
            original.Rung.IsCounter = original.IsCounter;
            original.Rung.IsCounterReset = original.IsCounterReset;
            original.Rung.IsCounterLoad = original.IsCounterLoad;
            original.Rung.CounterKind = original.CounterKind;
            original.Rung.IsNumericOperation = original.IsNumericOperation;
            original.Rung.NumericOperationKind = original.NumericOperationKind;
            original.Rung.NumericSourceA = original.NumericSourceA;
            original.Rung.NumericSourceB = original.NumericSourceB;
            original.Rung.NumericSourceC = original.NumericSourceC;
            original.Rung.NumericDestination = original.NumericDestination;
            original.Rung.NumericDataType = original.NumericDataType;
        }
        while (_ladderDocument.Tags.Count > numericStartingTags) _ladderDocument.Tags.RemoveAt(_ladderDocument.Tags.Count - 1);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        // Exercise actual button handlers against an input-only numeric project.
        // Rejection must preserve the complete document and leave Undo empty.
        var numericGuardSnapshot = _ladderDocument.CaptureSnapshot();
        var numericGuardHistory = _ladderHistory;
        var numericInputOnlyRejected = false;
        var textMoveDestinationWorked = false;
        var formatTextDestinationCreated = false;
        try
        {
            var inputOnly = new LadderEditorDocument();
            inputOnly.ResetProject("numeric-destination-guard", "Numeric destination guard", TimeSpan.FromMilliseconds(20));
            inputOnly.AddTag("measurement", PlcVariableRole.Input, type: PlcVariableType.Real);
            inputOnly.AddTag("guard_output", PlcVariableRole.Output);
            inputOnly.AddTag("enable", PlcVariableRole.Memory, initialValue: true);
            inputOnly.AddRung("Keep original coil on rejected insertion", "guard_output");
            inputOnly.AddContact(0, 0, "enable", false);
            _ladderDocument.RestoreSnapshot(inputOnly.CaptureSnapshot());
            _ladderHistory = new LadderEditorHistory();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            editorCanvas.SelectElement(0, 0, -1, output: true);
            var beforeRejectedNumeric = LadderEditorProjectJson.Save(_ladderDocument);
            var addNumericAdd = GetNodeOrNull<Button>($"{paletteRoot}/Math and Move/AddAdd");
            addNumericAdd?.EmitSignal(BaseButton.SignalName.Pressed);
            numericInputOnlyRejected = addNumericAdd is not null
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeRejectedNumeric
                && !_ladderHistory.CanUndo && output.GetParsedText().Contains("No writable destination", StringComparison.Ordinal);

            _ladderDocument.AddTag("state", PlcVariableRole.Memory, type: PlcVariableType.Enum, initialValue: "Stopped");
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            var sourceA = GetNodeOrNull<LineEdit>($"{contextualPropertiesRoot}/NumericPropertyRow/NumericSourceA");
            if (sourceA is not null) sourceA.Text = "\"Running\"";
            addMove.EmitSignal(BaseButton.SignalName.Pressed);
            textMoveDestinationWorked = sourceA is not null && _ladderDocument.Rungs[0].NumericDestination == "state"
                && _ladderDocument.Rungs[0].NumericOperationKind == LadderNumericOperationKind.Move
                && LadderCompiler.Validate(_ladderDocument.BuildProgram()).Count == 0;

            // FORMAT_TEXT must create a writable STRING even though a numeric Input already exists.
            _ladderDocument.RestoreSnapshot(inputOnly.CaptureSnapshot());
            _ladderHistory = new LadderEditorHistory();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            editorCanvas.SelectElement(0, 0, -1, output: true);
            addFormatText.EmitSignal(BaseButton.SignalName.Pressed);
            formatTextDestinationCreated = _ladderDocument.Rungs[0].NumericOperationKind == LadderNumericOperationKind.FormatText
                && _ladderDocument.Tags.Any(tag => tag.Name == _ladderDocument.Rungs[0].NumericDestination
                    && tag.Type == PlcVariableType.String && tag.Role != PlcVariableRole.Input)
                && LadderCompiler.Validate(_ladderDocument.BuildProgram()).Count == 0;
        }
        finally
        {
            _ladderDocument.RestoreSnapshot(numericGuardSnapshot);
            _ladderHistory = numericGuardHistory;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            editorCanvas.SelectElement(0, 0, -1, output: true);
        }
        GD.Print($"NUMERIC_DESTINATION_GUARD_VERIFY rejectedAtomic={numericInputOnlyRejected} textMove={textMoveDestinationWorked} formatCreatesString={formatTextDestinationCreated}");
        var startingBlocks = _ladderDocument.Blocks.Count;
        void ConfirmNamedCreation(string dialogName)
        {
            var form = GetNode<ConfirmationDialog>($"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/{dialogName}");
            form.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
        }
        addBlock.EmitSignal(BaseButton.SignalName.Pressed);
        ConfirmNamedCreation("CreateBlockDialog");
        var blockAdded = _ladderDocument.Blocks.Count == startingBlocks + 1;
        blockName.Text = "VerifierRoutine";
        applyBlock.EmitSignal(BaseButton.SignalName.Pressed);
        var blockRenamed = _ladderDocument.Blocks[^1].Name == "VerifierRoutine";
        removeBlock.EmitSignal(BaseButton.SignalName.Pressed);
        var blockDeleted = _ladderDocument.Blocks.Count == startingBlocks;
        addBlock.EmitSignal(BaseButton.SignalName.Pressed);
        ConfirmNamedCreation("CreateBlockDialog");
        _ladderDocument.SelectBlock(0);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        addCall.EmitSignal(BaseButton.SignalName.Pressed);
        var callInserted = _ladderDocument.Blocks[0].Rungs.Any(rung => rung.IsCall
            && rung.CallTarget == _ladderDocument.Blocks[^1].Id);
        _ladderDocument.ReplaceFromProgram(originalProgram);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        var startingTasks = _ladderDocument.Tasks.Count;
        addTask.EmitSignal(BaseButton.SignalName.Pressed);
        ConfirmNamedCreation("CreateTaskDialog");
        var taskAdded = _ladderDocument.Tasks.Count == startingTasks + 1;
        taskName.Text = "VerifierTask";
        applyTask.EmitSignal(BaseButton.SignalName.Pressed);
        var taskRenamed = _ladderDocument.Tasks[^1].Name == "VerifierTask";
        removeTask.EmitSignal(BaseButton.SignalName.Pressed);
        var taskDeleted = _ladderDocument.Tasks.Count == startingTasks;
        _ladderDocument.ReplaceFromProgram(originalProgram);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        addReturn.EmitSignal(BaseButton.SignalName.Pressed);
        var returnInserted = _ladderDocument.Rungs.Any(rung => rung.IsReturn);
        var returnPropertyEditWorked = false;
        var returnEditIndex = _ladderDocument.Rungs.FindIndex(rung => rung.IsReturn);
        var returnOperand = _ladderDocument.Tags.FirstOrDefault(tag => tag.Type == PlcVariableType.Bool)?.Name;
        if (returnEditIndex >= 0 && returnOperand is not null)
        {
            var returnRungId = _ladderDocument.Rungs[returnEditIndex].Id;
            EditableRung? CurrentReturn() => _ladderDocument.Rungs.FirstOrDefault(rung => rung.Id == returnRungId);
            var originalReturn = CurrentReturn();
            if (originalReturn is not null)
            {
                var originalTerminalId = originalReturn.TerminalActionId;
                editorCanvas.SelectElement(returnEditIndex, -1, -1, output: true);
                editorCanvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    DoubleClick = true,
                    Pressed = true,
                    Position = editorCanvas.GetElementPosition(returnEditIndex, -1, -1, output: true),
                });
                var expectedChoices = new[] { "RLO", "TRUE", "FALSE", "Value" };
                var opened = contextualPropertiesPopup.Visible && returnPropertyRow.Visible
                    && returnValueSelectorField.ItemCount == expectedChoices.Length
                    && Enumerable.Range(0, expectedChoices.Length)
                        .All(index => returnValueSelectorField.GetItemText(index) == expectedChoices[index])
                    && returnValueSelectorField.Selected == (int)LadderReturnValueKind.Rlo
                    && !returnValueOperandField.Visible;
                returnValueSelectorField.Select((int)LadderReturnValueKind.Operand);
                returnValueSelectorField.EmitSignal(
                    OptionButton.SignalName.ItemSelected, (long)LadderReturnValueKind.Operand);
                returnValueOperandField.Text = returnOperand;
                returnValueOperandField.EmitSignal(LineEdit.SignalName.TextChanged, returnOperand);
                var draftStayedIsolated = returnValueOperandField.Visible && returnValueOperandField.Editable
                    && CurrentReturn() is { } draft
                    && draft.ReturnValueKind == LadderReturnValueKind.Rlo
                    && draft.ReturnValueOperand.Length == 0;
                applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
                var applied = CurrentReturn();
                var applyWorkedForReturn = applied is not null
                    && applied.ReturnValueKind == LadderReturnValueKind.Operand
                    && applied.ReturnValueOperand == returnOperand
                    && applied.TerminalActionId == originalTerminalId
                    && _ladderHistory.CanUndo;
                undo.EmitSignal(BaseButton.SignalName.Pressed);
                var undone = CurrentReturn();
                var undoWorkedForReturn = undone is not null
                    && undone.ReturnValueKind == LadderReturnValueKind.Rlo
                    && undone.ReturnValueOperand.Length == 0
                    && undone.TerminalActionId == originalTerminalId;
                redo.EmitSignal(BaseButton.SignalName.Pressed);
                var redone = CurrentReturn();
                var redoWorkedForReturn = redone is not null
                    && redone.ReturnValueKind == LadderReturnValueKind.Operand
                    && redone.ReturnValueOperand == returnOperand
                    && redone.TerminalActionId == originalTerminalId;
                returnPropertyEditWorked = opened && draftStayedIsolated && applyWorkedForReturn
                    && undoWorkedForReturn && redoWorkedForReturn;
                GD.Print($"TIA_RET_PROPERTY_EDIT_VERIFY {(returnPropertyEditWorked ? "PASS" : "FAIL")} opened={opened} draft={draftStayedIsolated} apply={applyWorkedForReturn} undo={undoWorkedForReturn} redo={redoWorkedForReturn} operand={returnOperand} appliedKind={applied?.ReturnValueKind} appliedOperand={applied?.ReturnValueOperand} terminal={applied?.TerminalActionId}/{originalTerminalId} status={instructionPropertyStatus.Text}");
            }
        }
        _ladderDocument.ReplaceFromProgram(originalProgram);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        addLabel.EmitSignal(BaseButton.SignalName.Pressed);
        var labelEditIndex = _ladderDocument.Rungs.FindIndex(rung => rung.IsLabel
            && rung.NetworkLabelName == "Label1" && rung.HasTerminalAction && !string.IsNullOrWhiteSpace(rung.CoilVariable));
        var labelInserted = labelEditIndex >= 0;
        var labelTerminalId = labelInserted ? _ladderDocument.Rungs[labelEditIndex].TerminalActionId : string.Empty;
        var labelCoilVariable = labelInserted ? _ladderDocument.Rungs[labelEditIndex].CoilVariable : string.Empty;
        editorCanvas.SelectElement(Math.Max(0, labelEditIndex), -1, -1, output: true);
        editorCanvas._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, DoubleClick = true, Pressed = true,
            Position = editorCanvas.GetElementPosition(Math.Max(0, labelEditIndex), -1, -1, output: true) });
        var labelPropertiesOpened = contextualPropertiesPopup.Visible && editorCanvas.NetworkLabelSelected;
        var labelBeforePendingEdit = labelInserted ? _ladderDocument.Rungs[labelEditIndex].NetworkLabelName : string.Empty;
        programControlLabel.Text = "finish";
        programControlLabel.EmitSignal(LineEdit.SignalName.TextChanged, "finish");
        var labelTypingIsStaged = labelInserted && _ladderDocument.Rungs[labelEditIndex].NetworkLabelName == labelBeforePendingEdit;
        programControlLabel.EmitSignal(LineEdit.SignalName.TextSubmitted, "finish");
        var labelPropertyEdited = labelTypingIsStaged && _ladderDocument.Rungs[labelEditIndex].IsLabel
            && _ladderDocument.Rungs[labelEditIndex].NetworkLabelName == "finish"
            && _ladderDocument.Rungs[labelEditIndex].TerminalActionId == labelTerminalId
            && _ladderDocument.Rungs[labelEditIndex].CoilVariable == labelCoilVariable
            && _ladderDocument.BuildProgram().Networks[labelEditIndex] is { LabelInstruction: not null, Coil: not null };
        GD.Print($"TIA_LABEL_PROPERTY_EDIT_DETAIL opened={labelPropertiesOpened} staged={labelTypingIsStaged} edited={labelPropertyEdited} selected={editorCanvas.NetworkLabelSelected} before={labelBeforePendingEdit} after={(labelInserted ? _ladderDocument.Rungs[labelEditIndex].NetworkLabelName : "missing")} terminal={(labelInserted ? _ladderDocument.Rungs[labelEditIndex].TerminalActionId : "missing")}/{labelTerminalId} coil={(labelInserted ? _ladderDocument.Rungs[labelEditIndex].CoilVariable : "missing")}/{labelCoilVariable} status={instructionPropertyStatus.Text}");
        editorCanvas._GuiInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            DoubleClick = true,
            Pressed = true,
            Position = editorCanvas.GetElementPosition(Math.Max(0, labelEditIndex), -1, -1, output: true),
        });
        var labelContextualPropertiesWorked = contextualPropertiesPopup.Visible
            && jumpLabelPropertyRow.Visible
            && !contactPropertyRow.Visible
            && !numericPropertyRow.Visible;
        programControlLabel.Text = "1finish";
        programControlLabel.EmitSignal(LineEdit.SignalName.TextChanged, "1finish");
        programControlLabel.EmitSignal(LineEdit.SignalName.TextSubmitted, "1finish");
        var tiaLabelGrammarRejected = labelContextualPropertiesWorked
            && contextualPropertiesPopup.Visible
            && _ladderDocument.Rungs[labelEditIndex].NetworkLabelName == "finish"
            && instructionPropertyStatus.Text == TiaProgramControlLabel.ValidationMessage;
        // Commit the original valid value to close the transaction cleanly.
        programControlLabel.Text = "finish";
        programControlLabel.EmitSignal(LineEdit.SignalName.TextChanged, "finish");
        programControlLabel.EmitSignal(LineEdit.SignalName.TextSubmitted, "finish");
        contextualPropertiesPopup.Hide();
        _ladderDocument.ReplaceFromProgram(originalProgram);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        bool VerifyTimerPropertyEdit(Button insertButton, LadderTimerKind kind, string editedPreset)
        {
            insertButton.EmitSignal(BaseButton.SignalName.Pressed);
            var timerEditIndex = _ladderDocument.Rungs.FindIndex(rung => rung.IsTimer
                && !rung.IsTimerReset && rung.TimerKind == kind);
            if (timerEditIndex < 0) return false;
            var timerRungId = _ladderDocument.Rungs[timerEditIndex].Id;
            EditableRung? CurrentTimer() => _ladderDocument.Rungs.FirstOrDefault(rung => rung.Id == timerRungId);
            var original = CurrentTimer();
            if (original is null) return false;
            var originalVariable = original.TimerVariable;
            var originalPreset = original.TimerPreset;
            var originalPresetOperand = original.TimerPresetOperand;
            var originalResetOperand = original.TimerResetOperand;
            var originalDb = original.TimerInstanceDataBlock;
            var originalTerminalId = original.TerminalActionId;
            editorCanvas.SelectElement(timerEditIndex, -1, -1, output: true);
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                DoubleClick = true,
                Pressed = true,
                Position = editorCanvas.GetElementPosition(timerEditIndex, -1, -1, output: true),
            });
            var resetExpected = kind == LadderTimerKind.RetentiveOnDelay;
            var opened = contextualPropertiesPopup.Visible && timerPropertyRow.Visible
                && timerDbPropertyRow.Visible && timerInstanceSelector.ItemCount > 0
                && timerPresetOperandField.Visible && timerPresetOperandField.Editable
                && timerDbNumberField.Editable && timerDbNameField.Editable
                && timerResetOperandField.Visible == resetExpected;
            var editedDbNumber = Math.Clamp((originalDb?.Number ?? 1) + 100, 1, 65535);
            var editedDbName = $"Verify_{kind}_DB";
            timerPresetOperandField.Text = editedPreset;
            timerPresetOperandField.EmitSignal(LineEdit.SignalName.TextChanged, editedPreset);
            if (resetExpected)
            {
                timerResetOperandField.Text = "TRUE";
                timerResetOperandField.EmitSignal(LineEdit.SignalName.TextChanged, "TRUE");
            }
            timerDbNumberField.Value = editedDbNumber;
            timerDbNameField.Text = editedDbName;
            timerDbNameField.EmitSignal(LineEdit.SignalName.TextChanged, editedDbName);
            var stayedDraft = CurrentTimer() is { } draft
                && draft.TimerPreset == originalPreset
                && draft.TimerPresetOperand == originalPresetOperand
                && draft.TimerResetOperand == originalResetOperand
                && Equals(draft.TimerInstanceDataBlock, originalDb);
            applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
            var applied = CurrentTimer();
            var applyWorked = applied is not null && applied.TimerVariable == originalVariable
                && applied.TimerPresetOperand == editedPreset
                && LadderTimeLiteral.TryParse(editedPreset, out var editedDuration)
                && applied.TimerPreset == editedDuration
                && (!resetExpected || applied.TimerResetOperand == "TRUE")
                && (resetExpected || applied.TimerResetOperand == originalResetOperand)
                && applied.TimerInstanceDataBlock == new LadderInstanceDataBlock(editedDbNumber, editedDbName)
                && applied.TerminalActionId == originalTerminalId && _ladderHistory.CanUndo;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var undone = CurrentTimer();
            var undoWorkedForTimer = undone is not null && undone.TimerVariable == originalVariable
                && undone.TimerPreset == originalPreset && undone.TimerPresetOperand == originalPresetOperand
                && undone.TimerResetOperand == originalResetOperand
                && Equals(undone.TimerInstanceDataBlock, originalDb)
                && undone.TerminalActionId == originalTerminalId;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            var redone = CurrentTimer();
            var redoWorkedForTimer = redone is not null && redone.TimerPresetOperand == editedPreset
                && redone.TimerInstanceDataBlock == new LadderInstanceDataBlock(editedDbNumber, editedDbName)
                && (!resetExpected || redone.TimerResetOperand == "TRUE")
                && redone.TerminalActionId == originalTerminalId;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var worked = opened && stayedDraft && applyWorked && undoWorkedForTimer && redoWorkedForTimer;
            GD.Print($"TIA_TIMER_PROPERTY_EDIT_DETAIL kind={kind} opened={opened} draft={stayedDraft} apply={applyWorked} undo={undoWorkedForTimer} redo={redoWorkedForTimer}");
            _ladderDocument.ReplaceFromProgram(originalProgram);
            _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            return worked;
        }
        var tonPropertyEditWorked = VerifyTimerPropertyEdit(addTimer, LadderTimerKind.OnDelay, "TIME#1s500ms");
        var tofPropertyEditWorked = VerifyTimerPropertyEdit(addOffDelay, LadderTimerKind.OffDelay, "TIME#2s250ms");
        var tpPropertyEditWorked = VerifyTimerPropertyEdit(addPulseTimer, LadderTimerKind.Pulse, "TIME#750ms");
        var tonrPropertyEditWorked = VerifyTimerPropertyEdit(addRetentiveTimer, LadderTimerKind.RetentiveOnDelay, "TIME#3s");
        var timerPresetEditorWorked = tonPropertyEditWorked && tofPropertyEditWorked
            && tpPropertyEditWorked && tonrPropertyEditWorked;
        var timerResetEditorWorked = tonrPropertyEditWorked;
        GD.Print($"TIA_TIMER_PROPERTY_EDIT_VERIFY {(timerPresetEditorWorked ? "PASS" : "FAIL")} ton={tonPropertyEditWorked} tof={tofPropertyEditWorked} tp={tpPropertyEditWorked} tonr={tonrPropertyEditWorked}");
        bool VerifyTimerResetPropertyEdit()
        {
            _ladderDocument.ReplaceFromProgram(originalProgram);
            _ladderHistory.Clear();
            var timerA = _ladderDocument.AddTag("verify_rt_timer_a", PlcVariableRole.Memory, type: PlcVariableType.Timer);
            var timerB = _ladderDocument.AddTag("verify_rt_timer_b", PlcVariableRole.Memory, type: PlcVariableType.Timer);
            var timerARung = _ladderDocument.AddTimerRung("RT verifier timer A", timerA.Name, TimeSpan.FromSeconds(1));
            var timerBRung = _ladderDocument.AddTimerRung("RT verifier timer B", timerB.Name, TimeSpan.FromSeconds(2));
            timerARung.TimerInstanceDataBlock = new LadderInstanceDataBlock(301, "Verify_RT_A_DB");
            timerBRung.TimerInstanceDataBlock = new LadderInstanceDataBlock(302, "Verify_RT_B_DB");
            var resetRung = _ladderDocument.AddTimerResetRung("RT verifier reset", timerA.Name);
            _ladderDocument.EnsureInstanceDataBlocks();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            var resetRungId = resetRung.Id;
            var terminalId = resetRung.TerminalActionId;
            EditableRung? CurrentReset() => _ladderDocument.Rungs.FirstOrDefault(rung => rung.Id == resetRungId);
            var resetIndex = _ladderDocument.Rungs.FindIndex(rung => rung.Id == resetRungId);
            editorCanvas.SelectElement(resetIndex, -1, -1, output: true);
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                DoubleClick = true,
                Pressed = true,
                Position = editorCanvas.GetElementPosition(resetIndex, -1, -1, output: true),
            });
            var timerBIndex = Enumerable.Range(0, timerInstanceSelector.ItemCount)
                .FirstOrDefault(index => timerInstanceSelector.GetItemText(index) == timerB.Name, -1);
            var opened = contextualPropertiesPopup.Visible && timerPropertyRow.Visible
                && timerDbPropertyRow.Visible && timerBIndex >= 0
                && !timerPresetOperandField.Visible && !timerResetOperandField.Visible
                && !timerDbNumberField.Editable && !timerDbNameField.Editable
                && CurrentReset()?.TimerVariable == timerA.Name
                && CurrentReset()?.TimerInstanceDataBlock == timerARung.TimerInstanceDataBlock;
            if (timerBIndex < 0)
            {
                contextualPropertiesPopup.Hide();
                return false;
            }
            timerInstanceSelector.Select(timerBIndex);
            timerInstanceSelector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)timerBIndex);
            var stayedDraft = CurrentReset()?.TimerVariable == timerA.Name
                && CurrentReset()?.TimerInstanceDataBlock == timerARung.TimerInstanceDataBlock;
            applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
            var applied = CurrentReset();
            var applyWorked = applied is not null && applied.TimerVariable == timerB.Name
                && applied.TimerInstanceDataBlock == timerBRung.TimerInstanceDataBlock
                && applied.TerminalActionId == terminalId && _ladderHistory.CanUndo;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var undone = CurrentReset();
            var undoWorkedForReset = undone is not null && undone.TimerVariable == timerA.Name
                && undone.TimerInstanceDataBlock == timerARung.TimerInstanceDataBlock
                && undone.TerminalActionId == terminalId;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            var redone = CurrentReset();
            var redoWorkedForReset = redone is not null && redone.TimerVariable == timerB.Name
                && redone.TimerInstanceDataBlock == timerBRung.TimerInstanceDataBlock
                && redone.TerminalActionId == terminalId;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var worked = opened && stayedDraft && applyWorked && undoWorkedForReset && redoWorkedForReset;
            GD.Print($"TIA_RT_PROPERTY_EDIT_VERIFY {(worked ? "PASS" : "FAIL")} opened={opened} draft={stayedDraft} apply={applyWorked} undo={undoWorkedForReset} redo={redoWorkedForReset}");
            _ladderDocument.ReplaceFromProgram(originalProgram);
            _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            return worked;
        }
        var timerResetPropertyEditWorked = VerifyTimerResetPropertyEdit();
        _ladderDocument.ReplaceFromProgram(originalProgram);
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        bool VerifyCounterPropertyEdit(Button insertButton, LadderCounterKind kind, long editedPreset)
        {
            insertButton.EmitSignal(BaseButton.SignalName.Pressed);
            var counterEditIndex = _ladderDocument.Rungs.FindIndex(rung => rung.IsCounter
                && !rung.IsCounterLoad && !rung.IsCounterReset && rung.CounterKind == kind);
            if (counterEditIndex < 0) return false;
            var counterRungId = _ladderDocument.Rungs[counterEditIndex].Id;
            EditableRung? CurrentCounter() => _ladderDocument.Rungs.FirstOrDefault(rung => rung.Id == counterRungId);
            var original = CurrentCounter();
            if (original is null) return false;
            var originalVariable = original.CounterVariable;
            var originalPreset = original.CounterPreset;
            var originalType = original.CounterValueType;
            var originalDb = original.CounterInstanceDataBlock;
            var originalLoad = original.CounterLoadInput;
            var originalReset = original.CounterResetInput;
            var originalTerminalId = original.TerminalActionId;
            editorCanvas.SelectElement(counterEditIndex, -1, -1, output: true);
            editorCanvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                DoubleClick = true,
                Pressed = true,
                Position = editorCanvas.GetElementPosition(counterEditIndex, -1, -1, output: true),
            });
            var countUp = kind == LadderCounterKind.CountUp;
            var editedType = originalType == LadderCounterValueType.DInt
                ? LadderCounterValueType.UInt : LadderCounterValueType.DInt;
            var opened = contextualPropertiesPopup.Visible && counterPropertyRow.Visible
                && counterDbPropertyRow.Visible && counterInstanceSelector.ItemCount > 0
                && counterDataTypeField.Visible && counterDataTypeField.ItemCount == 9
                && counterPresetField.Visible && counterDbNumberField.Editable && counterDbNameField.Editable
                && counterResetInputRow.Visible == countUp && counterLoadInputRow.Visible == !countUp;
            var editedDbNumber = Math.Clamp((originalDb?.Number ?? 1) + 200, 1, 65535);
            var editedDbName = $"Verify_{kind}_DB";
            counterPresetField.Value = editedPreset;
            counterDataTypeField.Select((int)editedType);
            counterDataTypeField.EmitSignal(OptionButton.SignalName.ItemSelected, (long)editedType);
            if (countUp)
            {
                counterResetInputField.Text = "TRUE";
                counterResetInputField.EmitSignal(LineEdit.SignalName.TextChanged, "TRUE");
            }
            else
            {
                counterLoadInputField.Text = "TRUE";
                counterLoadInputField.EmitSignal(LineEdit.SignalName.TextChanged, "TRUE");
            }
            counterDbNumberField.Value = editedDbNumber;
            counterDbNameField.Text = editedDbName;
            counterDbNameField.EmitSignal(LineEdit.SignalName.TextChanged, editedDbName);
            var stayedDraft = CurrentCounter() is { } draft
                && draft.CounterPreset == originalPreset && draft.CounterValueType == originalType
                && draft.CounterLoadInput == originalLoad && draft.CounterResetInput == originalReset
                && Equals(draft.CounterInstanceDataBlock, originalDb);
            applyInstructionProperties.EmitSignal(BaseButton.SignalName.Pressed);
            var applied = CurrentCounter();
            var applyWorked = applied is not null && applied.CounterVariable == originalVariable
                && applied.CounterPreset == editedPreset && applied.CounterValueType == editedType
                && (countUp ? applied.CounterResetInput == "TRUE" && applied.CounterLoadInput == originalLoad
                    : applied.CounterLoadInput == "TRUE" && applied.CounterResetInput == originalReset)
                && applied.CounterInstanceDataBlock == new LadderInstanceDataBlock(editedDbNumber, editedDbName)
                && applied.TerminalActionId == originalTerminalId && _ladderHistory.CanUndo;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var undone = CurrentCounter();
            var undoWorkedForCounter = undone is not null && undone.CounterVariable == originalVariable
                && undone.CounterPreset == originalPreset && undone.CounterValueType == originalType
                && undone.CounterLoadInput == originalLoad && undone.CounterResetInput == originalReset
                && Equals(undone.CounterInstanceDataBlock, originalDb)
                && undone.TerminalActionId == originalTerminalId;
            redo.EmitSignal(BaseButton.SignalName.Pressed);
            var redone = CurrentCounter();
            var redoWorkedForCounter = redone is not null && redone.CounterPreset == editedPreset
                && redone.CounterValueType == editedType
                && (countUp ? redone.CounterResetInput == "TRUE" : redone.CounterLoadInput == "TRUE")
                && redone.CounterInstanceDataBlock == new LadderInstanceDataBlock(editedDbNumber, editedDbName)
                && redone.TerminalActionId == originalTerminalId;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            var worked = opened && stayedDraft && applyWorked && undoWorkedForCounter && redoWorkedForCounter;
            GD.Print($"TIA_COUNTER_PROPERTY_EDIT_DETAIL kind={kind} opened={opened} draft={stayedDraft} apply={applyWorked} undo={undoWorkedForCounter} redo={redoWorkedForCounter}");
            _ladderDocument.ReplaceFromProgram(originalProgram);
            _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            return worked;
        }
        var ctuPropertyEditWorked = VerifyCounterPropertyEdit(addCounter, LadderCounterKind.CountUp, 17);
        var ctdPropertyEditWorked = VerifyCounterPropertyEdit(addCounterDown, LadderCounterKind.CountDown, 23);
        GD.Print($"TIA_COUNTER_PROPERTY_EDIT_VERIFY {(ctuPropertyEditWorked && ctdPropertyEditWorked ? "PASS" : "FAIL")} ctu={ctuPropertyEditWorked} ctd={ctdPropertyEditWorked}");
        addJump.EmitSignal(BaseButton.SignalName.Pressed);
        var jumpInserted = _ladderDocument.Rungs.Any(rung => rung.IsJump
            && rung.ProgramControlLabel == "Label1");
        _ladderDocument.ReplaceFromProgram(originalProgram);
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        projectTree.GetRoot()?.Select(0);
        projectTree.EmitSignal(Tree.SignalName.ItemSelected);
        var selectionHandled = output.Text.Contains("Offline controller project selected", StringComparison.Ordinal);
        searchQuery.Text = "seal_in";
        crossReference.EmitSignal(BaseButton.SignalName.Pressed);
        var crossReferenceFound = searchResults.ItemCount >= 4;
        var navigableResult = -1;
        for (var index = 0; index < searchResults.ItemCount; index++)
        {
            var metadata = searchResults.GetItemMetadata(index).AsString().Split('\u001f');
            if (metadata.Length == 2 && metadata[1] != "-1")
            {
                navigableResult = index;
                break;
            }
        }
        if (navigableResult >= 0)
        {
            searchResults.Select(navigableResult);
            searchResults.EmitSignal(ItemList.SignalName.ItemActivated, navigableResult);
        }
        var searchNavigated = navigableResult >= 0 && output.Text.Contains("Opened", StringComparison.Ordinal);
        var tonHelpIndex = Enumerable.Range(0, helpSelector.ItemCount)
            .FirstOrDefault(index => helpSelector.GetItemMetadata(index).AsString() == "ton", -1);
        collapseTools.EmitSignal(BaseButton.SignalName.Pressed);
        if (tonHelpIndex >= 0)
        {
            helpSelector.Select(tonHelpIndex);
            helpSelector.EmitSignal(OptionButton.SignalName.ItemSelected, tonHelpIndex);
            openHelp.EmitSignal(BaseButton.SignalName.Pressed);
        }
        var instructionHelpWorked = tonHelpIndex >= 0
            && toolDockHostNode.Visible && toolPanelNode.Visible && !reopenTools.Visible
            && toolTabs.CurrentTab == 2
            && helpDetails.Text.Contains("Non-retentive", StringComparison.Ordinal)
            && helpDetails.Text.Contains("Q and DN", StringComparison.Ordinal);
        var browserMenu = GetNode<MenuButton>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/VendorChrome/VendorMenuBar/LadderViewMenu");
        collapseTools.EmitSignal(BaseButton.SignalName.Pressed);
        browserMenu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 4);
        instructionHelpWorked &= toolDockHostNode.Visible && toolPanelNode.Visible && !reopenTools.Visible
            && toolTabs.CurrentTab == 2;
        GD.Print($"HELP_DOCK_VERIFY visibleFromButtonAndMenu={instructionHelpWorked}");
        var toolHostWasVisible = toolDockHostNode.Visible;
        var toolPanelWasVisible = toolPanelNode.Visible;
        var reopenToolsWasVisible = reopenTools.Visible;
        var browserTabBefore = toolTabs.CurrentTab;
        var browserSplitBefore = toolSplit.SplitOffsets;
        browserMenu.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 6);
        var browserOpened = toolDockHostNode.Visible && toolPanelNode.Visible && toolTabs.CurrentTab == 0;
        collapseTools.EmitSignal(BaseButton.SignalName.Pressed);
        var browserCollapsed = toolDockHostNode.Visible && !toolPanelNode.Visible && reopenTools.Visible;
        toolDockHostNode.Visible = toolHostWasVisible;
        toolPanelNode.Visible = toolPanelWasVisible;
        reopenTools.Visible = reopenToolsWasVisible;
        toolTabs.CurrentTab = browserTabBefore;
        toolSplit.SplitOffsets = browserSplitBefore;
        GD.Print($"INSTRUCTION_BROWSER_VERIFY {(browserOpened && browserCollapsed ? "PASS" : "FAIL")} opened={browserOpened} collapsed={browserCollapsed}");
        var toolRailWorked = browserOpened && browserCollapsed && toolTabs.GetTabCount() == 3
            && !toolTabs.TabsVisible
            && toolDockHostNode is not null
            && projectTabs is not null
            && !projectTabs.TabsVisible
            && projectTabs.GetTabCount() == 4
            && projectTabs.GetTabTitle(0) == "Project tree"
            && projectTabs.GetTabTitle(1) == "PLC tags"
            && projectTabs.GetTabTitle(2) == "Blocks / tasks"
            && projectTabs.GetTabTitle(3) == "PLC tag detail view";
        var selectable = projectTree.MouseFilter != Control.MouseFilterEnum.Ignore
            && projectTree.GetRoot() is not null;
        var monitorProgram = _ladderDocument.BuildProgram();
        var monitorCompilation = LadderCompiler.Compile(monitorProgram);
        var monitorWorked = false;
        var staleMonitorCleared = false;
        var undoMonitorRestored = false;
        var watchTableWorked = false;
        var watchMetadataKeptMonitor = false;
        var watchUndoRestored = false;
        var watchStableAcrossScans = false;
        var rejectedInterfaceKeptMonitor = false;
        var duplicateInterfaceUndoExact = false;
        if (monitorCompilation.IsValid && monitorCompilation.Program is not null)
        {
            var monitorRuntime = new VirtualControllerRuntime(monitorCompilation.Program);
            monitorRuntime.Run();
            var monitorSnapshot = monitorRuntime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["start_command"] = true,
                ["stop_command"] = false,
                ["simulated_photoeye"] = false,
            });
            AttachVirtualController(monitorProgram, monitorSnapshot);
            var firstContact = _ladderDocument.Rungs[0].Branches[0].Contacts[0];
            monitorWorked = editorCanvas.MonitorActive && logixCanvas.MonitorActive
                && editorCanvas.MonitorScanNumber == monitorSnapshot.ScanNumber
                && editorCanvas.IsElementEnergized(firstContact.Id)
                && editorCanvas.IsElementEnergized($"{_ladderDocument.Rungs[0].Id}-coil");
            bottomTabs.CurrentTab = 2;
            var startWatch = watchTable.GetRoot()?.GetFirstChild();
            while (startWatch is not null && startWatch.GetMetadata(0).AsString() != "start_command")
                startWatch = startWatch.GetNext();
            watchTableWorked = startWatch is not null
                && startWatch.GetText(2) == "TRUE"
                && startWatch.GetText(4).Contains("GOOD · SCAN", StringComparison.Ordinal)
                && watchStatus.Text.Contains("SIMULATOR ONLY", StringComparison.Ordinal);
            if (startWatch is not null)
            {
                startWatch.Select(0);
                for (var scan = 0; scan < 3; scan++)
                    UpdateVirtualController(monitorRuntime.Scan(new Dictionary<string, bool>(StringComparer.Ordinal)
                    {
                        ["start_command"] = false,
                        ["stop_command"] = false,
                        ["simulated_photoeye"] = false,
                    }));
                watchStableAcrossScans = ReferenceEquals(watchTable.GetSelected(), startWatch)
                    && startWatch.GetText(2) == "FALSE";
                UpdateVirtualController(monitorSnapshot);
            }
            GD.Print($"WATCH_REFRESH_VERIFY stableSelectedRowAndUpdatedValue={watchStableAcrossScans}");
            var originalWatchCount = _ladderDocument.WatchVariables.Count;
            var baselineBeforeWatch = _ladderSavedProjectJson;
            _ladderSavedProjectJson = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            clearWatch.EmitSignal(BaseButton.SignalName.Pressed);
            watchMetadataKeptMonitor = _ladderDocument.WatchVariables.Count == 0
                && editorCanvas.MonitorActive && logixCanvas.MonitorActive
                && !undo.Disabled && tiaEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal)
                && logixEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal);
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            watchUndoRestored = _ladderDocument.WatchVariables.Count == originalWatchCount
                && editorCanvas.MonitorActive && logixCanvas.MonitorActive
                && !tiaEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal)
                && !logixEditorTabTitle.Text.StartsWith("* ", StringComparison.Ordinal);
            GD.Print($"WATCH_UNDO_DETAIL count={_ladderDocument.WatchVariables.Count}/{originalWatchCount} monitor={editorCanvas.MonitorActive}/{logixCanvas.MonitorActive} dirty={LadderEditorProjectJson.HasUnsavedChanges(_ladderDocument, _ladderSavedProjectJson)} nextId={_ladderDocument.CaptureSnapshot().NextId}/{LadderEditorProjectJson.Load(_ladderSavedProjectJson!).Document?.CaptureSnapshot().NextId}");
            _ladderSavedProjectJson = baselineBeforeWatch;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            var interfaceNameField = GetNode<LineEdit>($"{projectObjectsRoot}/InterfaceFields/InterfaceName");
            var interfaceInitialField = GetNode<LineEdit>($"{projectObjectsRoot}/InterfaceFields/InterfaceInitial");
            _ladderHistory.Clear();
            interfaceNameField.Text = "verify_parameter";
            interfaceInitialField.Text = "not_a_bool";
            addInterface.EmitSignal(BaseButton.SignalName.Pressed);
            rejectedInterfaceKeptMonitor = editorCanvas.MonitorActive && logixCanvas.MonitorActive
                && !_ladderHistory.CanUndo;
            _ladderHistory.Clear();
            interfaceInitialField.Text = "FALSE";
            addInterface.EmitSignal(BaseButton.SignalName.Pressed);
            interfaceNameField.Text = "verify_parameter";
            addInterface.EmitSignal(BaseButton.SignalName.Pressed);
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            duplicateInterfaceUndoExact = !_ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Interface
                .Any(parameter => parameter.Name == "verify_parameter");
            // Retain the verifier's original controller/document after a failing red run.
            _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Interface
                .RemoveAll(parameter => parameter.Name == "verify_parameter");
            _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            GD.Print($"INTERFACE_REJECTION_VERIFY monitorRetained={rejectedInterfaceKeptMonitor} duplicateUndoExact={duplicateInterfaceUndoExact}");
            addRung.EmitSignal(BaseButton.SignalName.Pressed);
            staleMonitorCleared = !editorCanvas.MonitorActive && !logixCanvas.MonitorActive;
            undo.EmitSignal(BaseButton.SignalName.Pressed);
            undoMonitorRestored = editorCanvas.MonitorActive && logixCanvas.MonitorActive
                && editorCanvas.MonitorScanNumber == monitorSnapshot.ScanNumber;
        }
        var editorDocumentTabs = GetNodeOrNull<HBoxContainer>($"{editorRoot}/EditorDocumentsHost/EditorDocumentsScroll/EditorDocuments");
        var centralTagDocument = GetNodeOrNull<ScrollContainer>($"{editorRoot}/PLC tags");
        var centralEditor = GetNodeOrNull<VBoxContainer>(editorRoot);
        var tiaProjectDocumentNavigationWorked = false;
        if (editorDocumentTabs is not null && centralTagDocument is not null && centralEditor is not null
            && projectTree is not null && projectTabs is not null)
        {
            TreeItem? FindMetadata(TreeItem? first, string metadata)
            {
                for (var item = first; item is not null; item = item.GetNext())
                {
                    if (item.GetMetadata(0).AsString() == metadata) return item;
                    var nested = FindMetadata(item.GetFirstChild(), metadata);
                    if (nested is not null) return nested;
                }
                return null;
            }
            var navigationSnapshot = _ladderDocument.BuildProgram();
            _ladderHistory.Clear();
            _ladderDocument.AddBlock("NavigationVerifier");
            _ladderDocument.SelectBlock(0);
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            var secondBlock = FindMetadata(projectTree.GetRoot(), "program:1");
            if (secondBlock is not null)
            {
                secondBlock.Select(0);
                projectTree.EmitSignal(Tree.SignalName.ItemSelected);
                var singleClickStayedInTree = _ladderDocument.ActiveBlockIndex == 0 && !centralTagDocument.Visible;
                projectTree.GrabFocus();
                GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true });
                var enterOpenedBlock = _ladderDocument.ActiveBlockIndex == 1
                    && !centralTagDocument.Visible
                    && editorDocumentTabs.GetChildCount() >= 2
                    && tiaEditorTabTitle.Text.Contains("NavigationVerifier", StringComparison.Ordinal);
                _ladderDocument.SelectBlock(0);
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                secondBlock = FindMetadata(projectTree.GetRoot(), "program:1");
                var activationOpenedBlock = false;
                if (secondBlock is not null)
                {
                    secondBlock.Select(0);
                    projectTree.EmitSignal(Tree.SignalName.ItemActivated);
                    activationOpenedBlock = _ladderDocument.ActiveBlockIndex == 1
                        && !centralTagDocument.Visible
                        && tiaEditorTabTitle.Text.Contains("NavigationVerifier", StringComparison.Ordinal);
                }
                var tableItem = FindMetadata(projectTree.GetRoot(), "tag-table:Default tag table");
                if (tableItem is not null)
                {
                    tableItem.Select(0);
                    projectTree.EmitSignal(Tree.SignalName.ItemSelected);
                    var singleClickStayedInBlock = !centralTagDocument.Visible && _ladderDocument.ActiveBlockIndex == 1;
                    projectTree.EmitSignal(Tree.SignalName.ItemActivated);
                    var doubleClickOpenedTable = centralTagDocument.Visible
                        && centralTagDocument.GetParent() == centralEditor
                        && projectTabs.CurrentTab == 0
                        && !projectTabs.TabsVisible;
                    var allTagsItem = FindMetadata(projectTree.GetRoot(), "tag-table:");
                    if (allTagsItem is not null)
                    {
                        allTagsItem.Select(0);
                        projectTree.EmitSignal(Tree.SignalName.ItemActivated);
                        var allTagsOpenedCentrally = centralTagDocument.Visible
                            && tiaEditorTabTitle.Text == "Show all tags";
                        var initialBlockKey = EditorDocumentIdentity.ForBlock(_ladderDocument.Blocks[0].Id);
                        var initialBlockTab = editorDocumentTabs.GetChildren().OfType<HBoxContainer>()
                            .SelectMany(item => item.GetChildren().OfType<Button>())
                            .FirstOrDefault(button => button.GetMeta("editorDocumentKey").AsString() == initialBlockKey);
                        initialBlockTab?.EmitSignal(BaseButton.SignalName.Pressed);
                        var ladderTabRestored = initialBlockTab is not null
                            && _ladderDocument.ActiveBlockIndex == 0 && !centralTagDocument.Visible;
                        tiaProjectDocumentNavigationWorked = singleClickStayedInTree
                            && enterOpenedBlock
                            && activationOpenedBlock
                            && singleClickStayedInBlock
                            && doubleClickOpenedTable
                            && allTagsOpenedCentrally
                            && ladderTabRestored;
                        GD.Print($"TIA_PROJECT_DOCUMENT_INPUT_DETAIL single={singleClickStayedInTree} physicalEnter={enterOpenedBlock} activation={activationOpenedBlock} tableSingle={singleClickStayedInBlock} tableActivation={doubleClickOpenedTable} allTags={allTagsOpenedCentrally} ladderTab={ladderTabRestored} focus={projectTree.HasFocus()}");
                    }
                }
            }
            _ladderDocument.ReplaceFromProgram(navigationSnapshot);
            _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
        GD.Print($"TIA_PROJECT_DOCUMENT_NAV_VERIFY {(tiaProjectDocumentNavigationWorked ? "PASS" : "FAIL")} centralTag={centralTagDocument?.Visible} leftTabsVisible={projectTabs?.TabsVisible} documentTabs={editorDocumentTabs?.GetChildCount()}");
        _ladderHistory.Clear();
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        result = $"treeSelectable={selectable} addBlockFromTree={addBlockFromTree is not null} projectPersistence={projectPersistenceWorked} newProject={newProjectWorked} rungLayout={rungLayoutWorked} contextualProperties={contextualPropertiesWorked}/{logixContextualPropertiesWorked}/{instructionContextMenuWorked} clipboard={instructionClipboardWorked}/{instructionClipboardUndone}/{rungClipboardWorked}/{rungClipboardUndone} keyboardContactInsert={keyboardNoInserted}/{keyboardNcInserted} monitor={monitorWorked}/{staleMonitorCleared}/{undoMonitorRestored} watchTable={watchTableWorked}/{watchMetadataKeptMonitor}/{watchUndoRestored} edges={risingEdgeInserted}/{fallingEdgeInserted} movePropertyFields={movePropertyFieldsWorked} movePropertyEdit={movePropertyEditWorked} arithmeticPropertyFields={addPropertyFieldsWorked}/{subtractPropertyFieldsWorked}/{multiplyPropertyFieldsWorked}/{dividePropertyFieldsWorked}/{moduloPropertyFieldsWorked} arithmeticPropertyEdits={addPropertyEditWorked}/{subtractPropertyEditWorked}/{multiplyPropertyEditWorked}/{dividePropertyEditWorked}/{moduloPropertyEditWorked} advancedMath={moduloInserted}/{squareRootInserted}/{exponentiateInserted}/{truncateInserted} scaling={normalizeInserted}/{scaleInserted} conversion={convertInserted}/{roundInserted}/{ceilingInserted}/{floorInserted} timers={timerInserted}/{offDelayInserted}/{pulseTimerInserted}/{retentiveTimerInserted}/{timerResetInserted} timerPresetOperand={timerPresetEditorWorked} timerResetOperand={timerResetEditorWorked} bindingBrowser={bindingBrowserWorked} validation={structuredValidationWorked}/{validationNavigationWorked}/{validationCleared} docksWorked={docksWorked}/{dockResizeWorked}[project={projectDockCollapsed}/{projectDockReopened},tools={toolDockCollapsed}/{toolDockReopened},bottom={bottomDockCollapsed}/{bottomDockReopened}] exactInsert={insertionCursorSelected}/{exactInsertionWorked}/{exactInsertionUndone} dragDrop={dragPreviewWorked}/{dragDropWorked}/{dragDropUndone} pointerBranch={pointerBranchEditWorked} branchClose={branchCloseWorked} multiPathBranchClose={multiPathBranchCloseWorked} branchConnectionDelete={branchConnectionDeleteWorked} lastBranchElementDelete={lastElementBranchCollapseWorked} elementMove={exactMoveWorked}/{exactMoveRestored} exactDelete={exactDeleteWorked}/{exactDeleteUndone} branchDelete={branchDeleteWorked}/{branchDeleteUndone} tagEdit={unusedTagDeleted}/{tagRenameWorked}/{tagInitialValueWorked}/{tagRenameUndone} selectionHandled={selectionHandled} crossReferenceFound={crossReferenceFound} searchNavigated={searchNavigated} instructionHelpWorked={instructionHelpWorked} toolRailWorked={toolRailWorked} tiaLocalInstructionsSeparated={tiaLocalInstructionsSeparated} tiaProjectDocuments={tiaProjectDocumentNavigationWorked} rungAdded={added} undoWorked={undoWorked} redoWorked={redoWorked} instructionAdded={instructionAdded} comparisonInserted={comparisonInserted} comparatorTypeEditUndoRedo={comparatorTypeEditUndoRedo} numericInserted={numericInserted} numericDestinationGuard={numericInputOnlyRejected}/{textMoveDestinationWorked}/{formatTextDestinationCreated} blockLifecycle={blockAdded}/{blockRenamed}/{blockDeleted} callInserted={callInserted} returnInserted={returnInserted} jumpLabel={jumpInserted}/{labelInserted}/{labelPropertyEdited}/{labelContextualPropertiesWorked}/{tiaLabelGrammarRejected} taskLifecycle={taskAdded}/{taskRenamed}/{taskDeleted} counterInserted={counterInserted} counterDownInserted={counterDownInserted} counterLoadInserted={counterLoadInserted} counterResetInserted={counterResetInserted} coilPropertyEdit={tiaCoilPropertyEditWorked} setInserted={setInserted} resetInserted={resetInserted} rungRestored={restored} instructionRestored={instructionRestored}";
        result += $" contactPropertyEdits={noContactPropertyEditWorked}/{ncContactPropertyEditWorked}";
        result += $" edgePropertyEdits={risingEdgePropertyEditWorked}/{fallingEdgePropertyEditWorked}";
        result += $" timerPropertyEdits={tonPropertyEditWorked}/{tofPropertyEditWorked}/{tpPropertyEditWorked}/{tonrPropertyEditWorked}";
        result += $" rtPropertyEdit={timerResetPropertyEditWorked}";
        result += $" counterPropertyEdits={ctuPropertyEditWorked}/{ctdPropertyEditWorked}";
        result += $" returnPropertyEdit={returnPropertyEditWorked}";
        return selectable && addBlockFromTree is not null && projectPersistenceWorked && newProjectWorked && rungLayoutWorked && contextualPropertiesWorked && logixContextualPropertiesWorked && instructionContextMenuWorked && docksWorked && dockResizeWorked && selectionHandled && crossReferenceFound && searchNavigated && instructionHelpWorked && toolRailWorked && tiaLocalInstructionsSeparated && tiaProjectDocumentNavigationWorked && added && undoWorked && redoWorked && instructionAdded && timerInserted && offDelayInserted && pulseTimerInserted && retentiveTimerInserted && timerResetInserted
            && noContactPropertyEditWorked && ncContactPropertyEditWorked
            && monitorWorked && staleMonitorCleared && undoMonitorRestored
            && watchTableWorked && watchMetadataKeptMonitor && watchUndoRestored && watchStableAcrossScans
            && firstTimerResetUndoExact && rejectedInterfaceKeptMonitor && duplicateInterfaceUndoExact
            && bindingBrowserWorked
            && structuredValidationWorked && validationNavigationWorked && validationCleared
            && insertionCursorSelected && exactInsertionWorked && exactInsertionUndone
            && dragPreviewWorked && dragDropWorked && dragDropUndone
            && pointerBranchEditWorked
            && branchCloseWorked
            && multiPathBranchCloseWorked
            && branchConnectionDeleteWorked
            && lastElementBranchCollapseWorked
            && exactMoveWorked && exactMoveRestored && exactDeleteWorked && exactDeleteUndone
            && instructionClipboardWorked && instructionClipboardUndone && keyboardNoInserted && keyboardNcInserted && rungClipboardWorked && rungClipboardUndone
            && branchDeleteWorked && branchDeleteUndone
            && unusedTagDeleted && tagRenameWorked && tagInitialValueWorked && tagRenameUndone
            && risingEdgeInserted && fallingEdgeInserted
            && risingEdgePropertyEditWorked && fallingEdgePropertyEditWorked
            && movePropertyFieldsWorked && movePropertyEditWorked
            && addPropertyFieldsWorked && subtractPropertyFieldsWorked
            && multiplyPropertyFieldsWorked && dividePropertyFieldsWorked && moduloPropertyFieldsWorked
            && addPropertyEditWorked && subtractPropertyEditWorked && multiplyPropertyEditWorked
            && dividePropertyEditWorked && moduloPropertyEditWorked
            && numericInputOnlyRejected && textMoveDestinationWorked && formatTextDestinationCreated
            && comparisonInserted && comparatorTypeEditUndoRedo && numericInserted && moduloInserted && squareRootInserted && exponentiateInserted && truncateInserted
            && normalizeInserted && scaleInserted
            && convertInserted && roundInserted && ceilingInserted && floorInserted
            && blockAdded && blockRenamed && blockDeleted && callInserted && returnInserted && returnPropertyEditWorked
            && jumpInserted && labelInserted && labelPropertyEdited && labelContextualPropertiesWorked && tiaLabelGrammarRejected
            && timerPresetEditorWorked && timerResetEditorWorked
            && timerResetPropertyEditWorked
            && ctuPropertyEditWorked && ctdPropertyEditWorked
            && taskAdded && taskRenamed && taskDeleted
            && counterInserted && counterDownInserted && counterLoadInserted && counterResetInserted
            && tiaCoilPropertyEditWorked && setInserted && resetInserted && restored && instructionRestored;
    }

    private static void AppendLadderNode(
        StringBuilder text,
        LadderNode node,
        VirtualControllerSnapshot snapshot,
        string indent)
    {
        switch (node.Kind)
        {
            case LadderNodeKind.Contact:
                var symbol = node.EdgeMode switch
                {
                    LadderEdgeMode.Rising => "[P]",
                    LadderEdgeMode.Falling => "[N]",
                    _ => node.NormallyClosed ? "[/]" : "[ ]",
                };
                AppendState(text, node.Id, $"{indent}{symbol} {Escape(node.Variable)}", snapshot);
                break;
            case LadderNodeKind.Compare:
                AppendState(text, node.Id,
                    $"{indent}CMP {Escape(node.Variable)} {node.CompareOperator} {Escape(node.RightOperand)}", snapshot);
                break;
            case LadderNodeKind.Series:
                AppendState(text, node.Id, $"{indent}SERIES", snapshot);
                foreach (var child in node.Children ?? []) AppendLadderNode(text, child, snapshot, indent + "  ");
                break;
            case LadderNodeKind.Parallel:
                AppendState(text, node.Id, $"{indent}PARALLEL", snapshot);
                var branch = 1;
                foreach (var child in node.Children ?? [])
                {
                    text.AppendLine($"{indent}  [color=#7fa7ba]BRANCH {branch++}[/color]");
                    AppendLadderNode(text, child, snapshot, indent + "    ");
                }
                break;
        }
    }

    private static void AppendState(
        StringBuilder text,
        string id,
        string label,
        VirtualControllerSnapshot snapshot)
    {
        var energized = snapshot.Elements.TryGetValue(id, out var state) && state.Energized;
        var color = energized ? "65d49a" : "657b86";
        text.AppendLine($"[color=#{color}]{label}  {(energized ? "ENERGIZED" : "off")}[/color] [color=#40535d]{Escape(id)}[/color]");
    }

    private void RefreshConnection()
    {
        if (_connectionStatus is null) return;
        _connectionStatus.Text = $"PLC  •  {_connection.State.ToString().ToUpperInvariant()}";
        _connectionStatus.AddThemeColorOverride("font_color",
            _connection.State == ConnectionState.Connected ? new Color("65d49a") : new Color("f1aa5b"));
        _connectionInspector.Text =
            $"[font_size=18][b]GUARDED PLC RUNTIME[/b][/font_size]\n\n" +
            $"[b]State[/b]  {_connection.State.ToString().ToUpperInvariant()}\n" +
            $"[b]Endpoint[/b]  {Escape(_connection.EndpointDescription)}\n\n" +
            $"{Escape(_connection.StatusDetail)}";
        RefreshRuntimeState();
    }

    private void PopulateDiagnostics()
    {
        _diagnosticList.Clear();
        foreach (var issue in _diagnostics
                     .OrderByDescending(issue => issue.Severity)
                     .ThenBy(issue => issue.Scope, StringComparer.Ordinal))
        {
            var icon = issue.Severity switch
            {
                DiagnosticSeverity.Error => "ERROR",
                DiagnosticSeverity.Warning => "WARN ",
                _ => "INFO ",
            };
            _diagnosticList.AddItem($"{icon}  {issue.Code}  |  {issue.Scope}  |  {issue.Message}  →  {issue.Resolution}");
        }
        var errors = _diagnostics.Count(issue => issue.Severity == DiagnosticSeverity.Error);
        var warnings = _diagnostics.Count(issue => issue.Severity == DiagnosticSeverity.Warning);
        _diagnosticSummary.Text = _productView == "operator" ? "SCENE I/O POINTS" : DiagnosticSummaryText();
        _diagnosticSummary.AddThemeColorOverride("font_color",
            errors > 0 ? new Color("ef7777") : warnings > 0 ? new Color("f1aa5b") : new Color("65d49a"));
    }

    private string DiagnosticSummaryText()
    {
        var errors = _diagnostics.Count(issue => issue.Severity == DiagnosticSeverity.Error);
        var warnings = _diagnostics.Count(issue => issue.Severity == DiagnosticSeverity.Warning);
        return $"PROJECT DIAGNOSTICS    {errors} ERRORS    {warnings} WARNINGS    {_diagnostics.Count} TOTAL";
    }

    public void SetWorkspaceStatus(string message, bool isError = false)
    {
        _workspaceStatus.Text = message;
        _workspaceStatus.AddThemeColorOverride("font_color",
            isError ? new Color("ef7777") : new Color("65d49a"));
        RecordOperatorEvent(message);
    }

    public void RecordOperatorEvent(string message)
    {
        // Bound the view and retain only operator/transition events. Scan
        // refreshes do not log, so long runs cannot flood the history or UI.
        _operatorEvents.Insert(0, $"{DateTime.Now:HH:mm:ss}  {Escape(message)}");
        if (_operatorEvents.Count > 32) _operatorEvents.RemoveAt(_operatorEvents.Count - 1);
        if (_operatorEventHistory is not null)
            _operatorEventHistory.Text = string.Join("\n", _operatorEvents);
    }

    public void SetTransformMode(WorkspaceTransformMode mode)
    {
        foreach (var item in _transformModeButtons)
            item.Value.SetPressedNoSignal(item.Key == mode);
    }

    public void SetTransformSpace(WorkspaceTransformSpace space)
    {
        foreach (var item in _transformSpaceButtons)
            item.Value.SetPressedNoSignal(item.Key == space);
    }

    public bool IsWorkspaceDirty { get; private set; }

    public void SetWorkspaceDirty(bool dirty)
    {
        IsWorkspaceDirty = dirty;
        var title = IsChallengeDraftActive ? ChallengeDisplayName : _activeSceneName;
        _sceneTitle.Text = dirty ? $"{title}  •  UNSAVED" : title;
        _sceneTitle.AddThemeColorOverride("font_color",
            dirty ? new Color("f1aa5b") : new Color("bcd3df"));
    }

    /// <summary>
    /// Runtime-safe toolbar rendering of the canonical mark geometry.
    /// The editable SVG remains the source asset; drawing here avoids relying
    /// on an editor import having completed before a packaged shell starts.
    /// </summary>
    private sealed partial class RungProofMark : Control
    {
        public override void _Draw()
        {
            var side = Math.Min(Size.X, Size.Y);
            var origin = new Vector2((Size.X - side) / 2.0f, (Size.Y - side) / 2.0f);
            var scale = side / 256.0f;
            Vector2 P(float x, float y) => origin + new Vector2(x, y) * scale;
            var navy = new Color("071a2a");
            var cyan = new Color("28a9e2");
            var green = new Color("16a34a");

            DrawStyleBox(new StyleBoxFlat
            {
                BgColor = new Color("f7fafb"),
                CornerRadiusTopLeft = 7,
                CornerRadiusTopRight = 7,
                CornerRadiusBottomLeft = 7,
                CornerRadiusBottomRight = 7,
            }, new Rect2(origin, new Vector2(side, side)));

            DrawLine(P(24, 128), P(232, 128), navy, 2.8f, true);
            DrawLine(P(42, 76), P(42, 180), navy, 2.8f, true);
            DrawLine(P(214, 76), P(214, 180), navy, 2.8f, true);
            DrawLine(P(88, 92), P(88, 164), cyan, 2.4f, true);
            DrawLine(P(116, 92), P(116, 164), cyan, 2.4f, true);
            DrawLine(P(140, 92), P(140, 164), cyan, 2.4f, true);
            DrawLine(P(168, 92), P(168, 164), cyan, 2.4f, true);
            DrawLine(P(116, 128), P(140, 128), green, 2.8f, true);
        }
    }

    private static PanelContainer PanelContainer(string name, Color background, Color border)
    {
        var panel = new PanelContainer { Name = name };
        panel.AddThemeStyleboxOverride("panel", BoxStyle(background, border));
        return panel;
    }

    private static StyleBoxFlat BoxStyle(Color background, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5,
            ContentMarginLeft = 10,
            ContentMarginTop = 8,
            ContentMarginRight = 10,
            ContentMarginBottom = 8,
        };
    }

    private static MarginContainer Margin(string name, int left, int right, int top, int bottom)
    {
        var margin = new MarginContainer { Name = name };
        margin.AddThemeConstantOverride("margin_left", left);
        margin.AddThemeConstantOverride("margin_right", right);
        margin.AddThemeConstantOverride("margin_top", top);
        margin.AddThemeConstantOverride("margin_bottom", bottom);
        return margin;
    }

    private static Label Heading(string text, int size, Color color)
    {
        var label = new Label { Text = text, VerticalAlignment = VerticalAlignment.Center };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Label SectionLabel(string text)
    {
        var label = Heading(text, 13, new Color("7fa7ba"));
        label.CustomMinimumSize = new Vector2(0, 28);
        return label;
    }

    private static Label SmallStatus(string text, Color color)
    {
        var label = Heading(text, 12, color);
        label.HorizontalAlignment = HorizontalAlignment.Right;
        return label;
    }

    private static VSeparator VRule() => new() { CustomMinimumSize = new Vector2(8, 0) };

    private static MenuButton TopMenu(string name, string text)
    {
        var menu = new MenuButton
        {
            Name = name,
            Text = text,
            Flat = true,
            CustomMinimumSize = new Vector2(Math.Max(52, text.Length * 10), 32),
        };
        menu.AddThemeColorOverride("font_color", new Color("a9c2cf"));
        menu.AddThemeColorOverride("font_hover_color", new Color("e9f4f8"));
        menu.AddThemeFontSizeOverride("font_size", 13);
        return menu;
    }

    private static Button ToolbarButton(string name, string text, Color color, float width = 104)
    {
        var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(width, 36) };
        button.AddThemeStyleboxOverride("normal", BoxStyle(color.Darkened(0.22f), color));
        button.AddThemeStyleboxOverride("hover", BoxStyle(color.Darkened(0.05f), color.Lightened(0.18f)));
        button.AddThemeStyleboxOverride("pressed", BoxStyle(color.Darkened(0.35f), color));
        button.AddThemeFontSizeOverride("font_size", 13);
        return button;
    }

    private static RichTextLabel Inspector(string name) => new()
    {
        Name = name,
        BbcodeEnabled = true,
        FitContent = false,
        ScrollActive = true,
        Text = "Select a scene or asset to inspect its engineering metadata.",
    };

    private static string Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string Escape(string value) => string.Concat(value.Select(character => character switch
    {
        '[' => "[lb]", ']' => "[rb]", _ => character.ToString(),
    }));
}





