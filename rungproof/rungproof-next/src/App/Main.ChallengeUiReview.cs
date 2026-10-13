using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.Challenges;
namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyChallengeUi;
    private async void VerifyChallengeUi()
    {
        ManualResetEventSlim? release = null;
        try
        {
            if (_simulatorShell is null || _challengeStarterDocument is null || _challengeDefinition is null || _challengeManifest is null)
                throw new InvalidOperationException("Trusted native challenge shell unavailable");
            var evidence = Path.Combine(Path.GetTempPath(), "RungProof-native-challenge-ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(evidence);
            // The preservation boundary is Demo 3 -> challenge -> Demo 3.
            // Saving intentionally binds a draft to the active scene, so the
            // verifier must enter the actual challenge scene before this probe.
            if (_candidateCatalog is null || _mainCamera is null) throw new InvalidOperationException("Scene catalog unavailable");
            AddMigratedScene(_challengeDefinition.SceneId,_candidateCatalog,_mainCamera,false,false);
            if (!_simulatorShell.VerifyChallengeDraftPreservation(_challengeStarterDocument,Path.Combine(evidence,"invalid-draft.rpproj.json"),out var preservation))
                throw new InvalidOperationException(preservation);
            GD.Print("CHALLENGE_UI_DRAFT_PRESERVATION PASS " + preservation);
            var previousFov = _mainCamera.Fov;
            var previousDirection = _sceneCameraDirection;
            EnterConveyorChallenge();
            if (!_simulatorShell.IsChallengeDraftActive) throw new InvalidOperationException("Challenge entry did not complete");
            if (!_simulatorShell.VerifyChallengeExternalGuards(out var externalGuards)) throw new InvalidOperationException(externalGuards);
            GD.Print("CHALLENGE_UI_EXTERNAL_GUARDS PASS " + externalGuards);
            HandleChallengeIntent(ChallengeIntent.Test);
            if (!_simulatorShell.VerifyChallengeValidationVisible(out var validation))
                throw new InvalidOperationException(validation);
            if (_challengeCoordinator is null || _challengeCoordinator.State.IsWorkerOutstanding
                || _challengeCoordinator.State.IsCurrentProof || _challengeCoordinator.Progress.Count != 0)
                throw new InvalidOperationException("Invalid starter reserved worker or awarded progress");
            GD.Print("CHALLENGE_UI_INVALID_STARTER PASS " + validation + " noWorker=true noProgress=true");
            var uiFixture = ChallengeReviewProgram();
            var uiLogic = new RungProof.Next.VirtualController.LadderNode("ui-parallel",RungProof.Next.VirtualController.LadderNodeKind.Parallel,Children: new[] {
                new RungProof.Next.VirtualController.LadderNode("ui-start-series",RungProof.Next.VirtualController.LadderNodeKind.Series,Children:new[] {new RungProof.Next.VirtualController.LadderNode("stop-start",RungProof.Next.VirtualController.LadderNodeKind.Contact,"Stop",true),new RungProof.Next.VirtualController.LadderNode("eye-start",RungProof.Next.VirtualController.LadderNodeKind.Contact,"Eye",true),new RungProof.Next.VirtualController.LadderNode("start",RungProof.Next.VirtualController.LadderNodeKind.Contact,"Start")}),
                new RungProof.Next.VirtualController.LadderNode("ui-hold-series",RungProof.Next.VirtualController.LadderNodeKind.Series,Children:new[] {new RungProof.Next.VirtualController.LadderNode("stop-hold",RungProof.Next.VirtualController.LadderNodeKind.Contact,"Stop",true),new RungProof.Next.VirtualController.LadderNode("eye-hold",RungProof.Next.VirtualController.LadderNodeKind.Contact,"Eye",true),new RungProof.Next.VirtualController.LadderNode("hold",RungProof.Next.VirtualController.LadderNodeKind.Contact,"Run")}) });
            uiFixture = uiFixture with { Networks = new[] { uiFixture.Networks[0] with { Logic = uiLogic } } };
            _simulatorShell.RevertChallengeStarter(uiFixture);
            _challengeCoordinator?.Dispose();
            var repository = new ChallengeProgressRepository(Path.Combine(evidence,"fault-progress.json"),_=>throw new IOException("Native fixture write fault"));
            _challengeCoordinator = new(repository,(definition,program,identity,token)=>ChallengeTestRunner.Run(definition,program,identity,
                ()=>new ConveyorChallengePlantAdapter(definition.Plant),token),()=>_challengeClock.Elapsed,_challengeDefinition,_challengeStarterBytes,_challengeManifest);
            _challengeCoordinator.PendingActionReady += ExecuteChallengePendingAction;
            _challengePublishedState = null;
            if (!_simulatorShell.TryBuildChallengeDraft(out _,out var nativeIssues)) throw new InvalidOperationException("Native fixture draft invalid: " + string.Join("; ",nativeIssues));
            HandleChallengeIntent(ChallengeIntent.Test);
            await WaitForChallenge(() => _challengeCoordinator.State.IsWorkerOutstanding == false);
            PublishChallenge();
            var status = _simulatorShell.GetNode<ChallengePresenter>("ChallengeWorkspace").GetChild<VBoxContainer>(0)
                .GetNode<HBoxContainer>("ChallengeStatusRow").GetChild<Label>(0).Text;
            if (!_challengeCoordinator.State.IsCurrentProof || _challengeCoordinator.State.LastResult?.Outcome != ChallengeOutcome.Passed
                || _challengeCoordinator.State.PersistenceStatus != ChallengePersistenceStatus.NotSaved
                || !status.Contains("not saved",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Native UI concealed Passed / progress not saved: proof={_challengeCoordinator.State.IsCurrentProof} outcome={_challengeCoordinator.State.LastResult?.Outcome} persistence={_challengeCoordinator.State.PersistenceStatus} status={status} coordinator={_challengeCoordinator.State.StatusText}");
            GD.Print("CHALLENGE_UI_PASSED_NOT_SAVED PASS actualStatusLabel=true");
            var incorrectFixture = uiFixture with { Networks = new[] { uiFixture.Networks[0] with {
                Logic = new RungProof.Next.VirtualController.LadderNode("missing-hold-start",RungProof.Next.VirtualController.LadderNodeKind.Contact,"Start") } } };
            _simulatorShell.RevertChallengeStarter(incorrectFixture);
            if (!_simulatorShell.TryBuildChallengeDraft(out _,out var incorrectIssues)) throw new InvalidOperationException("Eligible incorrect fixture rejected: " + string.Join("; ",incorrectIssues));
            HandleChallengeIntent(ChallengeIntent.Test);
            await WaitForChallenge(() => !_challengeCoordinator.State.IsWorkerOutstanding);
            PublishChallenge();
            if (_challengeCoordinator.State.LastResult?.Outcome != ChallengeOutcome.Failed || _challengeCoordinator.State.IsCurrentProof
                || _challengeCoordinator.Progress.Count != 0) throw new InvalidOperationException("Wired incorrect learner program did not fail without progress");
            var firstFailed = _challengeCoordinator.State.LastResult.Cases.FirstOrDefault(item=>item.Outcome==ChallengeOutcome.Failed);
            if (firstFailed is null || firstFailed.FirstFailure is null) throw new InvalidOperationException("Wired failure has no actionable case evidence");
            GD.Print($"CHALLENGE_UI_INCORRECT_PROGRAM PASS wiredTest=true outcome=Failed noProgress=true case={firstFailed.CaseId} code={firstFailed.Code}");
            _simulatorShell.RevertChallengeStarter(uiFixture);
            _challengeCoordinator.Dispose();
            var savedRepository = new ChallengeProgressRepository(Path.Combine(evidence,"saved-progress.json"));
            _challengeCoordinator = new(savedRepository,(definition,program,identity,token)=>ChallengeTestRunner.Run(definition,program,identity,
                ()=>new ConveyorChallengePlantAdapter(definition.Plant),token),()=>_challengeClock.Elapsed,_challengeDefinition,_challengeStarterBytes,_challengeManifest);
            _challengeCoordinator.PendingActionReady += ExecuteChallengePendingAction;
            _challengePublishedState = null;
            HandleChallengeIntent(ChallengeIntent.Test);
            await WaitForChallenge(() => !_challengeCoordinator.State.IsWorkerOutstanding);
            PublishChallenge();
            var reloaded = new ChallengeProgressRepository(Path.Combine(evidence,"saved-progress.json")).Load();
            if (!_challengeCoordinator.State.IsCurrentProof || _challengeCoordinator.State.PersistenceStatus != ChallengePersistenceStatus.Saved
                || reloaded.Issues.Count != 0 || reloaded.Records.Count != 1 || reloaded.Records[0].Identity.RunEpoch != 0)
                throw new InvalidOperationException("Wired passing proof was not saved and reloaded as historical progress");
            GD.Print("CHALLENGE_UI_PROGRESS_SAVE_RELOAD PASS wiredTest=true saved=true historicalEpoch=0 isolatedTemp=true");
            _challengeCoordinator.Dispose();
            release = new ManualResetEventSlim();
            using var computed = new ManualResetEventSlim();
            var gate = release;
            _challengeCoordinator = new(new ChallengeProgressRepository(Path.Combine(evidence,"cancel-progress.json")),
                (definition,program,identity,token)=> {
                    var result=ChallengeTestRunner.Run(definition,program,identity,()=>new ConveyorChallengePlantAdapter(definition.Plant),token);
                    computed.Set(); gate.Wait(); return result;
                },()=>_challengeClock.Elapsed,_challengeDefinition,_challengeStarterBytes,_challengeManifest);
            _challengeCoordinator.PendingActionReady += ExecuteChallengePendingAction;
            _challengePublishedState=null;
            if (!_simulatorShell.TryBuildChallengeDraft(out _,out var nativeIssuesSecond)) throw new InvalidOperationException("Native fixture draft invalid: " + string.Join("; ",nativeIssuesSecond));
            HandleChallengeIntent(ChallengeIntent.Test);
            await WaitForChallenge(() => computed.IsSet);
            PublishChallenge();
            if (!_simulatorShell.VerifyChallengeLockedDraftSave(_challengeStarterDocument,Path.Combine(evidence,"locked-save.rpproj.json"),out var locked))
                throw new InvalidOperationException(locked);
            HandleChallengeIntent(ChallengeIntent.Cancel);
            if (!_challengeCoordinator.State.IsWorkerOutstanding || !_challengeCoordinator.State.MutationLocked)
                throw new InvalidOperationException("Native cancellation released occupied worker slot");
            gate.Set();
            await WaitForChallenge(() => _challengeCoordinator.State.IsWorkerOutstanding == false);
            PublishChallenge();
            if (_challengeCoordinator.State.IsCurrentProof || _challengeCoordinator.Progress.Count != 0)
                throw new InvalidOperationException("Cancelled computed pass awarded native progress");
            GD.Print("CHALLENGE_UI_CANCEL_AND_LOCKED_SAVE PASS stalePassRejected=true " + locked);
            ClearChallengeLabels();
            if (_mainCamera.Fov != previousFov || _sceneCameraDirection != previousDirection)
                throw new InvalidOperationException("Challenge camera leaked into restored demo");
            GD.Print("CHALLENGE_UI_CAMERA_RESTORE PASS");
            _simulatorShell.EndChallengeDraft();
            _challengeCoordinator.Dispose();
            _challengeCoordinator=null;
            GD.Print("CONVEYOR_CHALLENGE_UI_VERIFY PASS keyboard=false screenReader=false transport=none evidence=" + evidence);
            GetTree().Quit();
        }
        catch(Exception error)
        { GD.PushError("CONVEYOR_CHALLENGE_UI_VERIFY FAIL " + error); GetTree().Quit(1); }
        finally { release?.Set(); }
    }
    private async Task WaitForChallenge(Func<bool> completed)
    {
        var watch=Stopwatch.StartNew();
        while(!completed())
        {
            if(watch.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Native review worker did not finish");
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            PollChallenge();
        }
    }
}
