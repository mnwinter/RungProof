using System;
using System.Linq;
using Godot;
using RungProof.Next.Workspace;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private WorkspaceDocument? _workspaceFiniteRollerDocument;

    private void CreateFiniteRollerWorkspace()
    {
        if (!GuardWorkspaceReplacement(CreateFiniteRollerWorkspace)) return;
        var rollback = CaptureWorkspaceReplacementRollback();
        try
        {
            var document = WorkspaceFiniteRollerPolicy.Create();
            WorkspaceRepository.ValidateDocument(document, _candidateCatalog!, _sceneCatalog!);
            AddMigratedScene(document.SourceSceneId, _candidateCatalog!, _mainCamera!, false, false,
                finiteRollerWorkspace: document);
            var relation = document.FiniteRollerPreset!;
            var blank = new LadderEditorDocument();
            blank.ResetProject("finite-roller-workspace", "Main", TimeSpan.FromMilliseconds(20));
            blank.SourceSceneId = document.SourceSceneId;
            blank.AddTag("start", PlcVariableRole.Input, relation.StartPoint);
            blank.AddTag("beam_blocked", PlcVariableRole.Input, relation.BeamPoint);
            blank.AddTag("roller_run", PlcVariableRole.Output, relation.RunPoint);
            _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(blank), null, true);
            _simulatorShell.SetIntegratedSceneIdentity();
            _simulatorShell.SetWorkspaceStatus("Finite pallet roller workspace created · stopped · write your own ladder · select conveyor to move the complete cell");
            RefreshWorkspaceDirty();
        }
        catch (Exception error)
        {
            rollback();
            _simulatorShell!.SetWorkspaceStatus("Preset creation rejected · " + error.Message, true);
        }
    }

    private void RegisterFiniteRollerWorkspace(WorkspaceDocument document)
    {
        _workspaceFiniteRollerDocument = document;
        foreach (var placement in document.Placements)
            _workspaceNodes[placement.InstanceId] = (placement.AssetId, _sceneCompositionRoot!.GetNode<Node3D>(placement.InstanceId));
        _signalLinks.AddRange(document.SignalLinks ?? []);
    }

    private bool TryTransformFiniteRollerWorkspace(string id, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        if (_workspaceFiniteRollerDocument is not { } document) return false;
        try
        {
            if (id != document.FiniteRollerPreset!.RollerId || rotation != Vector3.Zero || scale != Vector3.One)
                throw new InvalidOperationException("Select conveyor to translate the complete cell; independent pose, rotation and scale are unsupported.");
            var home = document.Placements.Single(placement => placement.InstanceId == id);
            var delta = position - Vector(home.Position);
            var next = document with
            {
                Placements = document.Placements.Select(placement => placement with
                    { Position = Array(Vector(placement.Position) + delta) }).ToArray()
            };
            WorkspaceRepository.ValidateDocument(next, _candidateCatalog!, _sceneCatalog!);
            RecordUndo();
            RebuildTemplateWorkspace(next);
            _simulatorShell!.SetWorkspaceStatus("Complete roller cell translated · actual draft preserved · stopped");
        }
        catch (Exception error)
        {
            _simulatorShell!.SetWorkspaceStatus("Preset transform rejected · " + error.Message, true);
            RefreshWorkspaceUi();
        }
        return true;
    }

    private bool RejectBoundedWorkspaceGraphEdit()
    {
        if (RejectFiniteBeltGraphEdit()) return true;
        if (_workspaceFiniteRollerDocument is null) return false;
        _simulatorShell?.SetWorkspaceStatus("Finite roller relation is bounded: translate its conveyor or create/open a different workspace; independent asset edits are unsupported", true);
        return true;
    }

    private SignalMappingRuntimeStatus? FiniteRollerMappingStatus(string instanceId, string signalId)
    {
        if (_workspaceFiniteRollerDocument?.FiniteRollerPreset is not { } relation) return null;
        if (instanceId == relation.RollerId && signalId == "run_command")
            return new(SignalMappingRuntimeState.LiveInput, "LIVE INPUT", "Accepted PLC BOOL commands the roller drive; command OFF decelerates while playback runs.");
        if (instanceId == relation.SensorId && signalId == "beam_blocked")
            return new(SignalMappingRuntimeState.LiveFeedback, "LIVE FEEDBACK", "Actual pallet wood intersects the opposed optical segment; genuine gaps can clear this PC BOOL.");
        if (instanceId == relation.OperatorId && signalId == "pressed")
            return new(SignalMappingRuntimeState.LiveFeedback, "LIVE INPUT", "Actual momentary pushbutton action supplies a PC BOOL pulse.");
        return null;
    }
}
