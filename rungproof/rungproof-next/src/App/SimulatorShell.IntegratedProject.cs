using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    public event Action? IntegratedDraftChanged;
    public string CaptureIntegratedLadderDraft()=>LadderEditorProjectJson.Save(_ladderDocument);
    public bool VerifyTemplateAssemblyUiEdit(float xDelta,float yawDelta)
    {
        var row=_workspaceListSelectionTargets.FindIndex(ids=>ids.SequenceEqual(new[]{WorkspaceTemplatePolicy.SelectionId}));
        if(row<0)return false;
        _workspaceList.DeselectAll();_workspaceList.Select(row);
        _workspaceList.EmitSignal(ItemList.SignalName.MultiSelected,row,true);
        if(_selectedPlacementId!=WorkspaceTemplatePolicy.SelectionId)return false;
        _positionInputs[0].Value+=xDelta;
        _rotationInputs[1].Value+=yawDelta;
        return _selectedPlacementId==WorkspaceTemplatePolicy.SelectionId&&_positionInputs[0].Editable&&_rotationInputs[1].Editable
            &&!_positionInputs[1].Editable&&!_rotationInputs[0].Editable&&!_rotationInputs[2].Editable&&_scaleInputs.All(s=>!s.Editable)
            &&_deletePlacement.Disabled;
    }
    public void MarkIntegratedLadderDraftSaved()
    {
        _ladderSavedProjectJson=CaptureIntegratedLadderDraft();
        foreach(var refresh in _ladderEditorRefreshers)refresh();
    }
    public string? CaptureIntegratedLadderSavedBaseline() => _ladderSavedProjectJson;
    public void RestoreIntegratedLadderDraft(string json, string? savedBaseline = null, bool preserveSavedBaseline = false)
    {
        var parsed=LadderEditorProjectJson.Load(json);
        if(!parsed.IsReadable||parsed.Document is null)
            throw new InvalidOperationException("Integrated ladder draft is unreadable.");
        if(parsed.Document.SourceSceneId!=_activeScene?.Id)
            throw new InvalidOperationException("Integrated ladder belongs to another scene.");
        ChallengeDraftChanged?.Invoke();
        _ladderDocument.RestoreSnapshot(parsed.Document.CaptureSnapshot());
        _ladderHistory.Clear();_ladderProjectPath=null;
        _ladderRungClipboard=null;_ladderContactClipboard=null;
        _ladderSavedProjectJson=preserveSavedBaseline ? savedBaseline : CaptureIntegratedLadderDraft();
        foreach(var refresh in _ladderEditorRefreshers)refresh();
        RefreshLadderMonitorMatch();
        _ladderWorkspaceStatus.Text="INTEGRATED PROJECT OPENED · DRAFT RESTORED · STOPPED · RUN VALIDATES CURRENT DRAFT";
        // Existing loaded-starter state is disabled by Main before this restore.
        // No VirtualControllerProgramRequested invocation occurs here.
    }
    public void DismissIntegratedLoadConfirmationForReview()
    {
        if(_workspaceFeedbackDialog.Visible&&_workspaceFeedbackDialog.Title=="Workspace")
            _workspaceFeedbackDialog.GetOkButton().EmitSignal(Button.SignalName.Pressed);
    }
    public void SetIntegratedSceneIdentity()
    {
        if(_activeScene is null)return;
        _activeSceneName=_activeScene.Name;
        _sceneTitle.Text=_activeSceneName;
        _sceneTitle.TooltipText=_activeSceneName;
        GetWindow().Title="RungProof · "+_activeSceneName;
        RefreshRuntimeState();
    }    public Action CaptureIntegratedEditorRollback()
    {
        var saved=CaptureSceneLadderDraft() with { History=_ladderHistory.Copy() };
        var rungClipboard=_ladderRungClipboard;var contactClipboard=_ladderContactClipboard;
        return () =>
        {
            _ladderDocument.RestoreSnapshot(saved.Document);
            _ladderSavedProjectJson=saved.SavedJson;_ladderProjectPath=saved.ProjectPath;
            _ladderHistory=saved.History.Copy();
            _ladderRungClipboard=rungClipboard;_ladderContactClipboard=contactClipboard;
            if(_activeScene is not null)_sceneLadderDrafts[SceneDraftCacheKey(_activeScene)]=CaptureSceneLadderDraft();
            foreach(var refresh in _ladderEditorRefreshers)refresh();RefreshLadderMonitorMatch();
        };
    }
    public bool PrepareRollbackEditorFixture(string path)
    {
        System.IO.File.WriteAllText(path,CaptureIntegratedLadderDraft());
        if(!TryOpenLadderProject(path,out _))return false;
        _ladderHistory.Execute(_ladderDocument,"Rollback custom name edit",()=>_ladderDocument.Name+="_Edited");
        foreach(var refresh in _ladderEditorRefreshers)refresh();
        return _ladderProjectPath==System.IO.Path.GetFullPath(path)&&_ladderHistory.CanUndo;
    }
    public bool VerifyRollbackEditorFixture(string path,string expectedDraft)
    {
        if(_ladderProjectPath!=System.IO.Path.GetFullPath(path)||!_ladderHistory.CanUndo||CaptureIntegratedLadderDraft()!=expectedDraft)return false;
        if(!_ladderHistory.Undo(_ladderDocument,out _)||CaptureIntegratedLadderDraft()==expectedDraft)return false;
        if(!_ladderHistory.Redo(_ladderDocument,out _)||CaptureIntegratedLadderDraft()!=expectedDraft)return false;
        foreach(var refresh in _ladderEditorRefreshers)refresh();return true;
    }}
