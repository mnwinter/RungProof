using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private Node3D? _challengeLabelRoot;
    private readonly Dictionary<string, Label3D> _challengeLabels = new(StringComparer.Ordinal);
    private Label3D? _challengeLabelFallback;
    private PanelContainer? _challengeLivePanel;
    private Label? _challengeLiveCommands;
    private Label? _challengeLiveFeedback;
    private Label? _challengeLiveObject;
    private VirtualControllerScanCompleted? _challengeLiveScan;

    // Called only by Main on challenge entry. Resource paths never come from a draft.
    private void InitializeChallengeLabels()
    {
        ClearChallengeLabels();
        if (_sceneCompositionRoot is null || _sceneRuntime?.RuntimeType != "conveyorStop") return;
        _challengeLabelRoot = new Node3D { Name = "ChallengeLiveLabels" };
        _sceneCompositionRoot.AddChild(_challengeLabelRoot);
        if (_simulatorShell is not null)
        {
            _challengeLivePanel = new PanelContainer { Name = "ChallengeLiveIo", ZIndex = 25, MouseFilter = Control.MouseFilterEnum.Ignore };
            _challengeLivePanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("10212be8"), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 8, ContentMarginBottom = 8 });
            var body = new VBoxContainer();
            _challengeLivePanel.AddChild(body);
            foreach (var label in new[] { new Label { Text = "LIVE · OFFLINE MODEL" }, _challengeLiveCommands = new Label(), _challengeLiveFeedback = new Label(), _challengeLiveObject = new Label() })
            {
                label.AddThemeFontSizeOverride("font_size", 16);
                label.AddThemeColorOverride("font_color", new Color("e8f0f3"));
                label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                body.AddChild(label);
            }
            _simulatorShell.AddChild(_challengeLivePanel);
        }
        try
        {
            using var packet = JsonDocument.Parse(ReadChallengeText("res://challenges/presentation/conveyor-stop.presentation.json"));
            var data = packet.RootElement;
            if (data.GetProperty("schemaVersion").GetInt32() != 1 || data.GetProperty("sceneId").GetString() != "scene-1-conveyor-stop")
                throw new InvalidOperationException("Unsupported challenge presentation");
            var expected = new Dictionary<string, string> { ["operator"] = "scene1_station", ["feedback"] = "scene1_photoeye", ["process"] = "scene1_conveyor" };
            foreach (var anchor in data.GetProperty("anchors").EnumerateArray())
            {
                var id = anchor.GetProperty("id").GetString() ?? "";
                var equipmentId = anchor.GetProperty("equipmentId").GetString() ?? "";
                if (!expected.TryGetValue(id, out var allowed) || allowed != equipmentId || _challengeLabels.ContainsKey(id))
                    throw new InvalidOperationException("Invalid label anchor");
                var values = anchor.GetProperty("offset").EnumerateArray().Select(v => v.GetSingle()).ToArray();
                if (values.Length != 3 || values.Any(v => !float.IsFinite(v) || Math.Abs(v) > 4))
                    throw new InvalidOperationException("Invalid label offset");
                // Exact equipment names, never a user-provided NodePath.
                var equipment = _sceneCompositionRoot.GetChildren().OfType<Node3D>().FirstOrDefault(n => n.Name == equipmentId);
                if (equipment is null) continue;
                var label = NewChallengeLabel("Live_" + id);
                _challengeLabelRoot.AddChild(label);
                label.GlobalPosition = equipment.ToGlobal(new Vector3(values[0], values[1], values[2]));
                _challengeLabels.Add(id, label);
            }
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            foreach (var label in _challengeLabels.Values) { _challengeLabelRoot.RemoveChild(label); label.QueueFree(); }
            _challengeLabels.Clear();
            GD.PushWarning("CHALLENGE_LABEL_FALLBACK " + error.Message);
        }
        if (_challengeLabels.Count != 3)
        {
            // Outside the belt/beam corridor. All relevant I/O remains textually available.
            _challengeLabelFallback = NewChallengeLabel("LiveIoFallback");
            _challengeLabelFallback.Position = new Vector3(-3, 3.6f, 2.1f);
            _challengeLabelRoot.AddChild(_challengeLabelFallback);
        }
        UpdateChallengeLabels();
    }

    private static Label3D NewChallengeLabel(string name) => new()
    {
        Name = name, FontSize = 32, PixelSize = .002f, OutlineSize = 8,
        Modulate = Colors.White, OutlineModulate = new Color(.025f, .035f, .045f),
        Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
        NoDepthTest = false, Visible = false,
    };

    private void UpdateChallengeLabels()
    {
        if (_challengeLabelRoot is null || _sceneRuntime is null) return;
        if (_simulatorShell?.IsExternalMode == true)
        {
            _challengeLabelRoot.Visible = false;
            if (_challengeLivePanel is not null) _challengeLivePanel.Visible = false;
            return;
        }
        _challengeLabelRoot.Visible = true;
        if (_challengeLivePanel is not null && _simulatorShell is not null)
        {
            _challengeLivePanel.Visible = true;
            var aperture = _simulatorShell.SceneViewportRect();
            _challengeLivePanel.Position = aperture.Position + new Vector2(8, 8);
            _challengeLivePanel.Size = new Vector2(MathF.Max(240, aperture.Size.X - 16), 108);
        }
        // Capture returns private copies. Neither grading results nor worker traces enter this path.
        var plant = _sceneRuntime.CaptureConveyorChallengeState();
        var program = _virtualProgram;
        var snapshot = _virtualSnapshot;
        var inputs = new Dictionary<string, bool>(StringComparer.Ordinal);
        if (program is not null && snapshot is not null)
            foreach (var variable in program.Variables.Where(v => v.Role == PlcVariableRole.Input && v.Type == PlcVariableType.Bool && v.Binding.Length > 0))
                if ((_challengeLiveScan?.EffectiveInputs.Bools ?? snapshot.Variables).TryGetValue(variable.Name, out var value)) inputs[variable.Binding] = value;
        var outputs = program is not null && snapshot is not null
            ? SceneIoImageMapper.CommitBoolOutputs(program, snapshot.Outputs)
            : new Dictionary<string, bool>();
        static string Bit(IReadOnlyDictionary<string, bool> image, string binding) =>
            image.TryGetValue(binding, out var value) ? (value ? "TRUE" : "FALSE") : "unavailable";
        var operatorText = "OFFLINE · operator inputs\nStart: " + Bit(inputs, "operator.start") + " · Stop: " + Bit(inputs, "operator.stop");
        var feedbackText = "SIMULATED · scene feedback\nPhotoeye: " + Bit(plant.Feedback, "simulated_photoeye");
        var position = plant.Numbers.TryGetValue("object_position", out var meters) && double.IsFinite(meters)
            ? meters.ToString("0.000", CultureInfo.InvariantCulture) + " m" : "unavailable";
        var processText = "OFFLINE · controller command\nConveyor: " + Bit(outputs, "conveyor_running")
            + "\nSIM object leading edge: " + position + "\nObject: " + (plant.ObjectPresent ? "present" : "absent") + " · exits: " + plant.Completed;
        if (_challengeLabels.TryGetValue("operator", out var operatorLabel)) operatorLabel.Text = operatorText;
        if (_challengeLabels.TryGetValue("feedback", out var feedbackLabel)) feedbackLabel.Text = feedbackText;
        if (_challengeLabels.TryGetValue("process", out var processLabel)) processLabel.Text = processText;
        if (_challengeLabelFallback is not null)
            _challengeLabelFallback.Text = "I/O TEXT · unavailable scene anchor\n"
                + (!_challengeLabels.ContainsKey("operator") ? operatorText + "\n" : "")
                + (!_challengeLabels.ContainsKey("feedback") ? feedbackText + "\n" : "")
                + (!_challengeLabels.ContainsKey("process") ? processText : "");
        if (_challengeLiveCommands is not null)
            _challengeLiveCommands.Text = "Process Start " + Bit(inputs,"operator.start") + " · Process Stop " + Bit(inputs,"operator.stop");
        if (_challengeLiveFeedback is not null)
            _challengeLiveFeedback.Text = "SIM photoeye " + Bit(plant.Feedback,"simulated_photoeye") + " · Conveyor command " + Bit(outputs,"conveyor_running");
        if (_challengeLiveObject is not null)
            _challengeLiveObject.Text = "Carton " + position + " · " + (plant.ObjectPresent ? "present" : "absent") + " · exits " + plant.Completed;
    }

    private void ClearChallengeLabels()
    {
        if (_challengePreviousCamera is { } previous && _mainCamera is not null)
        { _mainCamera.Fov = previous.Fov; _sceneCameraDirection = previous.Direction; }
        _challengePreviousCamera = null;
        _challengeLabels.Clear();
        _challengeLabelFallback = null;
        if (_challengeLabelRoot is not null && GodotObject.IsInstanceValid(_challengeLabelRoot))
        {
            _challengeLabelRoot.GetParent()?.RemoveChild(_challengeLabelRoot);
            _challengeLabelRoot.QueueFree();
        }
        _challengeLabelRoot = null;
        if (_challengeLivePanel is not null)
        { _challengeLivePanel.GetParent()?.RemoveChild(_challengeLivePanel); _challengeLivePanel.QueueFree(); }
        _challengeLivePanel = null;
        _challengeLiveCommands = _challengeLiveFeedback = _challengeLiveObject = null;
    }
    private void OnChallengeLiveScan(VirtualControllerScanCompleted scan) => _challengeLiveScan = scan;
    private Rect2 ChallengeSceneAperture(Rect2 aperture) => _challengeLivePanel is null ? aperture :
        new Rect2(aperture.Position + new Vector2(0,128), new Vector2(aperture.Size.X, MathF.Max(1,aperture.Size.Y-128)));
}
