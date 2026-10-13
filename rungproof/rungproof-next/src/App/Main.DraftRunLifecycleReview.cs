using System;
using System.Reflection;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    // Invoked by the coordinated native verifier, never by normal app startup.
    // Mutates the real shell draft: validation is not replaced with fake results.
    private void VerifyDraftRunLifecycle(Action<bool,string> check)
    {
        AddMigratedScene("conveyor-cell",_candidateCatalog!,_mainCamera!,false,false);
        var shell=_simulatorShell!;
        var field=typeof(SimulatorShell).GetField("_ladderDocument",BindingFlags.Instance|BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Shell draft field unavailable.");
        var original=(LadderEditorDocument)field.GetValue(shell)!;
        var monitorRefresh=typeof(SimulatorShell).GetMethod("RefreshLadderMonitorMatch",BindingFlags.Instance|BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Shell monitor identity seam unavailable.");
        var monitorMatch=typeof(SimulatorShell).GetField("_ladderMonitorMatchesLoadedProgram",BindingFlags.Instance|BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Shell monitor identity unavailable.");
        var draft=new LadderEditorDocument();
        draft.ResetProject("draft-run-review","Current_Draft",TimeSpan.FromMilliseconds(20));
        draft.SourceSceneId="conveyor-cell";
        field.SetValue(shell,draft);
        try
        {
            RunEditorDraftController();
            check(_virtualController is null,"empty_current_draft_does_not_load");
            draft.AddTag("photoeye_blocked",PlcVariableRole.Input,"photoeye_blocked");
            draft.AddTag("conveyor_run",PlcVariableRole.Output,"conveyor_run");
            draft.AddRung("Current draft output","conveyor_run");
            draft.AddContact(0,0,"photoeye_blocked",true);
            var correctProgram=draft.BuildProgram();
            var valid=shell.TryBuildCurrentLadderProgram(out _,out var validation);
            foreach(var issue in validation)GD.Print($"DRAFT_RUN_VALIDATION {issue}");
            check(valid,"current_fixture_matches_actual_scene_io_contract");
            if(!valid)throw new InvalidOperationException("Draft run fixture is invalid: "+string.Join("; ",validation));
            RunEditorDraftController();
            check(_virtualController?.Snapshot.State==VirtualControllerState.Running,"valid_current_draft_runs");
            _PhysicsProcess(.04);
            check(_virtualController!.Snapshot.Outputs["conveyor_run"],"valid_current_logic_drives_command");
            var session=_virtualController!;
            var scene=_sceneRuntime;
            var scan=session.Snapshot.ScanNumber;
            var carton=_sceneCompositionRoot!.GetNode<Node3D>("carton_1");
            StopActiveController();
            RunEditorDraftController();
            check(ReferenceEquals(session,_virtualController)&&_virtualController!.Snapshot.ScanNumber==scan,"unchanged_stop_run_retains_session_memory");
            draft.WatchVariables.Add("conveyor_run");
            monitorRefresh.Invoke(shell,null);
            RunEditorDraftController();
            check(ReferenceEquals(session,_virtualController)&&_virtualController!.Snapshot.ScanNumber==scan,"watch_only_edit_retains_session_and_scan_memory");
            check(monitorMatch.GetValue(shell) is true,"watch_only_edit_ui_monitor_matches_loaded_execution");
            var heldCarton=carton.Transform;
            draft.AddContact(0,0,"photoeye_blocked",false);
            RunEditorDraftController();
            check(!ReferenceEquals(session,_virtualController)&&ReferenceEquals(scene,_sceneRuntime),"changed_valid_draft_reloads_without_scene_replacement");
            check(carton.Transform==heldCarton,"changed_valid_draft_preserves_actual_carton_transform");
            _PhysicsProcess(.04);
            check(!_virtualController!.Snapshot.Outputs["conveyor_run"],"valid_wrong_edit_executes_current_wrong_logic");
            check(LadderProgramJson.Save(_virtualProgram! with { WatchVariables = null })==LadderProgramJson.Save(draft.BuildProgram() with { WatchVariables = null }),"loaded_program_matches_current_draft");
            EnableVirtualControllerProgram(correctProgram,resetScene:false);
            RunActiveController();
            _PhysicsProcess(.04);
            check(_virtualController!.Snapshot.Outputs["conveyor_run"],"explicit_stale_correct_program_running_before_invalid_edit");
            draft.ResetProject("invalid-current","Invalid_Current",TimeSpan.FromMilliseconds(20));
            draft.SourceSceneId="conveyor-cell";
            monitorRefresh.Invoke(shell,null);
            var invalidJson=LadderEditorProjectJson.Save(draft);
            RunEditorDraftController();
            check(_virtualController?.Snapshot.State==VirtualControllerState.Stopped,"invalid_current_draft_stops_loaded_session");
            check(!_virtualController!.Snapshot.Outputs["conveyor_run"],"invalid_current_draft_clears_output_command");
            var stoppedScan=_virtualController!.Snapshot.ScanNumber;
            var stoppedCarton=carton.Transform;
            for(var tick=0;tick<10;tick++)_PhysicsProcess(.02);
            check(_virtualController!.Snapshot.ScanNumber==stoppedScan&&carton.Transform==stoppedCarton,"invalid_run_has_no_subsequent_scans_or_carton_motion");
            check(LadderEditorProjectJson.Save(draft)==invalidJson,"invalid_run_preserves_actual_draft");
            check(monitorMatch.GetValue(shell) is false,"invalid_current_ui_monitor_does_not_show_stale_loaded_execution");
            var path=ProjectSettings.GlobalizePath("res://.tools/draft-run-invalid-review.rpproj.json");
            System.IO.File.WriteAllText(path,LadderEditorProjectJson.Save(draft));
            var open=typeof(SimulatorShell).GetMethod("TryOpenLadderProject",BindingFlags.Instance|BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Shell project-open seam unavailable.");
            object?[] arguments={path,null};
            check(open.Invoke(shell,arguments) is true,"readable_invalid_project_opens_as_draft");
            RunEditorDraftController();
            check(_virtualController!.Snapshot.State==VirtualControllerState.Stopped&&draft.Rungs.Count==0,"reopened_invalid_draft_remains_invalid_and_cannot_run_old_logic");
            // Explicit diagnostic loading remains independent of editor draft.
            RunActiveController();
            check(_virtualController?.Snapshot.State==VirtualControllerState.Running,"explicit_loaded_session_qa_path_preserved");
            StopActiveController();
        }
        finally
        {
            StopActiveController();
            DisableVirtualController();
            field.SetValue(shell,original);
        }
    }
}
