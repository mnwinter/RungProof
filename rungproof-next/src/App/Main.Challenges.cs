using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Godot;
using RungProof.Next.Challenges;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyConveyorChallenge;
    private bool _captureEnterChallenge;
    private (float Fov, Vector3 Direction)? _challengePreviousCamera;
    private ChallengeDefinition? _challengeDefinition;
    private ChallengeSemanticManifest? _challengeManifest;
    private LadderProgram? _challengeStarter;
    private LadderEditorDocument? _challengeStarterDocument;
    private byte[] _challengeStarterBytes = System.Array.Empty<byte>();
    private ChallengeSessionCoordinator? _challengeCoordinator;
    private readonly Stopwatch _challengeClock = Stopwatch.StartNew();
    private readonly List<string> _challengeHints = new();
    private long _challengeEpoch;
    private bool _challengeChangingScene;
    private Action? _challengePendingNavigation;
    private Action? _challengePendingClose;
    private ChallengePresentationState? _challengePublishedState;

    private void InitializeChallengeShell()
    {
        if (_simulatorShell is null) return;
        SetDemoReducedMotion(_simulatorShell.ReducedMotion);
        _simulatorShell.ReducedMotionChanged += SetDemoReducedMotion;
        _simulatorShell.ChallengeIntentRequested += HandleChallengeIntent;
        _simulatorShell.ChallengeDraftChanged += () => _challengeCoordinator?.DraftChanged();
        _simulatorShell.ChallengeCleanupGuard = action =>
        {
            if (_challengeCoordinator?.State.IsWorkerOutstanding != true) return true;
            if (_challengeCoordinator.State.PendingAction == ChallengePendingAction.None) _challengePendingClose = action;
            _challengeCoordinator.RequestAction(ChallengePendingAction.Close);
            PublishChallenge();
            return false;
        };
        try
        {
            var catalog = ChallengeDefinitionJson.LoadCatalog(ReadChallengeText("res://challenges/catalog.json"));
            _challengeDefinition = ChallengeDefinitionJson.Load(ReadChallengeText(catalog["scene-1-conveyor-stop"])).Definition
                ?? throw new InvalidOperationException("Invalid challenge packet");
            _challengeStarterBytes = Godot.FileAccess.GetFileAsBytes(ChallengeDefinitionJson.StarterResource);
            var starterIssues = ChallengeDefinitionJson.ValidateStarter(_challengeDefinition, _challengeStarterBytes);
            if (starterIssues.Count > 0) throw new InvalidOperationException(string.Join("; ", starterIssues));
            var starter = LadderEditorProjectJson.Load(Encoding.UTF8.GetString(_challengeStarterBytes));
            if (!starter.IsReadable) throw new InvalidOperationException("Unreadable editable starter");
            _challengeStarterDocument = starter.Document!;
            _challengeStarter = _challengeStarterDocument.BuildProgram();
            _challengeManifest = ChallengeSemanticManifest.Load(ReadChallengeText("res://challenges/semantic-manifest.json")).Manifest
                ?? throw new InvalidOperationException("Invalid semantic manifest");
            _simulatorShell.ConfigureChallenge(_challengeDefinition);
        }
        catch (Exception error)
        {
            _challengeDefinition = null;
            _simulatorShell.SetWorkspaceStatus("Challenge Test unavailable: " + error.Message, isError: true);
            GD.PushWarning("CHALLENGE_CONTENT_DISABLED " + error.Message);
        }
    }
    private static string ReadChallengeText(string path)
    {
        if (!Godot.FileAccess.FileExists(path)) throw new InvalidOperationException("Missing trusted resource " + path);
        return Godot.FileAccess.GetFileAsString(path);
    }
    private void EnterConveyorChallenge()
    {
        if (_simulatorShell is null || _challengeDefinition is null || _challengeStarterDocument is null || _challengeManifest is null) return;
        if (_simulatorShell.IsExternalMode)
        { _simulatorShell.SetWorkspaceStatus("Select Built-in Simulator before entering the challenge", true); return; }
        if (_simulatorShell.IsChallengeDraftActive) return;
        _simulatorShell.GuardChallengeDraftReplacement(() =>
        {
            if (_candidateCatalog is null || _mainCamera is null) return;
            _challengeChangingScene = true;
            try { AddMigratedScene(_challengeDefinition.SceneId, _candidateCatalog, _mainCamera, false, false); }
            finally { _challengeChangingScene = false; }
            _challengeCoordinator?.Dispose();
            var progressPath = ProjectSettings.GlobalizePath("user://challenge-progress.json");
            _challengeCoordinator = new(new ChallengeProgressRepository(progressPath),
                (definition, program, identity, token) => ChallengeTestRunner.Run(definition, program, identity,
                    () => new ConveyorChallengePlantAdapter(definition.Plant), token), () => _challengeClock.Elapsed, _challengeDefinition, _challengeStarterBytes, _challengeManifest);
            _challengeCoordinator.PendingActionReady += ExecuteChallengePendingAction;
            _challengeHints.Clear();
            _simulatorShell.BeginChallengeDraft(_challengeStarterDocument);
            InitializeChallengeLabels();
            ApplyChallengePresentationCamera();
            GD.Print($"CHALLENGE_PRESENTATION_READY labels={_challengeLabels.Count} runtime={_sceneRuntime?.RuntimeType} keyboard=false");
            PublishChallenge();
        });
    }
    private void HandleChallengeIntent(ChallengeIntent intent)
    {
        if (_simulatorShell is null) return;
        switch (intent)
        {
            case ChallengeIntent.Enter: EnterConveyorChallenge(); break;
            case ChallengeIntent.Test:
                if (_challengeCoordinator is null || _challengeDefinition is null) return;
                if (!_simulatorShell.TryBuildChallengeDraft(out var draft, out var issues))
                {
                    _simulatorShell.SetChallengeValidationIssues(issues);
                    _simulatorShell.SetWorkspaceStatus("Invalid challenge draft: " + string.Join("; ", issues), true);
                    return;
                }
                _simulatorShell.SetChallengeValidationIssues(System.Array.Empty<string>());
                _challengeCoordinator.Begin(_challengeDefinition, draft, CreateChallengeIdentity(draft, ++_challengeEpoch), _challengeHints);
                PublishChallenge();
                break;
            case ChallengeIntent.Cancel: _challengeCoordinator?.Cancel(); PublishChallenge(); break;
            case ChallengeIntent.Hint:
                if (_challengeDefinition is not null && _challengeHints.Count < _challengeDefinition.Hints.Count)
                    _challengeHints.Add("hint-" + (_challengeHints.Count + 1));
                break;
            case ChallengeIntent.ResetStarter:
                if (_challengeCoordinator?.State.IsWorkerOutstanding == true) return;
                if (_challengeStarterDocument is not null) _simulatorShell.GuardChallengeDraftReplacement(() => _simulatorShell.RevertChallengeStarter(_challengeStarterDocument));
                break;
            case ChallengeIntent.Leave:
                _challengeCoordinator?.RequestAction(ChallengePendingAction.Navigate);
                break;
        }
    }
    private ChallengeRunIdentity CreateChallengeIdentity(LadderProgram draft, long epoch)
    {
        var definition = _challengeDefinition ?? throw new InvalidOperationException("Challenge not loaded");
        var manifest = _challengeManifest ?? throw new InvalidOperationException("Manifest not loaded");
        return new(ChallengeProgramIdentity.IdentityFormat, definition.Id, definition.Revision,
            ChallengeProgramIdentity.ProgramSha256(draft), ChallengeContentIdentity.PacketSha256(definition, _challengeStarterBytes),
            ChallengeContentIdentity.PlantSha256(definition.Plant), manifest.EngineRevision, manifest.EngineSha256,
            manifest.EvaluatorRevision, manifest.EvaluatorSha256, definition.ScanPeriodMs, epoch);
    }
    private void ExecuteChallengePendingAction(ChallengePendingAction action)
    {
        PublishChallenge();
        if (action == ChallengePendingAction.Reset) { ResetActiveController(); return; }
        if (action == ChallengePendingAction.Close)
        {
            var close = _challengePendingClose;
            _challengePendingClose = null;
            close?.Invoke();
            return;
        }
        if (action != ChallengePendingAction.Navigate || _simulatorShell is null) return;
        var next = _challengePendingNavigation;
        _challengePendingNavigation = null;
        _simulatorShell.GuardChallengeDraftReplacement(() =>
        {
            StopActiveController();
            ClearChallengeLabels();
            _simulatorShell.EndChallengeDraft();
            _challengeCoordinator?.Dispose();
            _challengeCoordinator = null;
            _challengePublishedState = null;
            next?.Invoke();
        });
    }
    private void PollChallenge()
    {
        if (_challengeCoordinator is null) return;
        var previousPending = _challengeCoordinator.State.PendingAction;
        _challengeCoordinator.Poll();
        if (_challengeCoordinator is null) return; // Navigation callback may have left.
        if (previousPending != ChallengePendingAction.None && _challengeCoordinator.State.PendingAction == ChallengePendingAction.None
            && _challengeCoordinator.State.IsWorkerOutstanding) { _challengePendingNavigation = null; _challengePendingClose = null; }
        PublishChallenge();
    }
    private void PublishChallenge()
    {
        if (_simulatorShell is null || _challengeCoordinator is null) return;
        var state = _challengeCoordinator.State;
        if (state == _challengePublishedState) return;
        _challengePublishedState = state;
        _simulatorShell.SetChallengePresentationState(state);
        _simulatorShell.SetChallengeProgress(_challengeCoordinator.Progress);
    }
    private bool GuardChallengeSceneReplacement(Action action)
    {
        if (_challengeChangingScene || _simulatorShell?.IsChallengeDraftActive != true) return true;
        if (_challengeCoordinator?.State.PendingAction != ChallengePendingAction.None && _challengeCoordinator?.State.IsWorkerOutstanding == true) return false;
        _challengePendingNavigation = action;
        _challengeCoordinator?.RequestAction(ChallengePendingAction.Navigate);
        return false;
    }
    private void ApplyChallengePresentationCamera()
    {
        if (_mainCamera is null || _sceneCompositionRoot is null) return;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(ReadChallengeText("res://challenges/presentation/conveyor-stop.presentation.json"));
            var camera = document.RootElement.GetProperty("camera");
            var focus = camera.GetProperty("focusEquipment").EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray();
            var allowed = new[] { "scene1_conveyor", "scene1_photoeye", "scene1_station" };
            if (!focus.SequenceEqual(allowed, StringComparer.Ordinal)
                || focus.Any(id => !_sceneCompositionRoot.GetChildren().OfType<Node3D>().Any(node => node.Name == id)))
                throw new InvalidOperationException("Invalid challenge camera focus");
            var xyz = camera.GetProperty("direction").EnumerateArray().Select(e => e.GetSingle()).ToArray();
            var fov = camera.GetProperty("fov").GetSingle();
            if (xyz.Length != 3 || xyz.Any(value => !float.IsFinite(value) || Math.Abs(value) > 50)
                || !float.IsFinite(fov) || fov is < 30 or > 70) throw new InvalidOperationException("Invalid camera parameters");
            var direction = new Vector3(xyz[0], xyz[1], xyz[2]);
            if (direction.LengthSquared() < .01f) throw new InvalidOperationException("Invalid camera direction");
            _challengePreviousCamera ??= (_mainCamera.Fov, _sceneCameraDirection);
            _sceneCameraDirection = direction;
            _mainCamera.Fov = fov;
            // Keep all process geometry in the established framing contract;
            // focus metadata identifies the relevant machinery, not decor.
            FrameComposition(_mainCamera, _sceneCompositionRoot, direction);
        }
        catch (Exception error) when (error is System.Text.Json.JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { GD.PushWarning("CHALLENGE_CAMERA_FALLBACK " + error.Message); }
    }
}
