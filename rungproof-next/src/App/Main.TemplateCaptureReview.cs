using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using Godot;
using RungProof.Next.Workspace;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private bool _templateCapturePending;
    private async void PrepareTemplateCaptureReview()
    {
        try
        {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(_capturePath is null||_currentSceneId is null||!WorkspaceTemplatePolicy.Supports(_currentSceneId))throw new InvalidOperationException("Template capture requires a supported starter scene and capture path.");
            var draftState=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--template-draft-state=",StringComparison.Ordinal))?.Split('=',2)[1];
            if(draftState is not null)
            {
                if(_currentSceneId!="lab-4-01-press-count-lamp"||draftState is not ("valid" or "invalid"))throw new InvalidOperationException("Template draft QA state is restricted to exact Lab4.1 valid/invalid.");
                var custom=new LadderEditorDocument();custom.ResetProject("template-draft-QA-"+draftState,"Template_QA_"+draftState,TimeSpan.FromMilliseconds(20));custom.SourceSceneId=_currentSceneId;
                if(draftState=="valid")
                {
                    custom.AddTag("pulse_received",PlcVariableRole.Input,"pulse_received");custom.AddTag("threshold_lamp",PlcVariableRole.Output,"threshold_lamp");
                    custom.AddRung("QA custom draft follows declared pulse","threshold_lamp");custom.AddContact(0,0,"pulse_received",false);
                }
                _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(custom));
            }
            var originalDraft=_simulatorShell!.CaptureIntegratedLadderDraft();
            StopActiveController();
            if(!_simulatorShell.VerifyTemplateAssemblyUiEdit(.4f,15))throw new InvalidOperationException("Actual template inspector edit failed.");
            var path=ProjectSettings.GlobalizePath("res://.tools/template-capture-project.json");
            SaveWorkspaceToPath(path);LoadWorkspaceFromPath(path);
            _simulatorShell!.DismissIntegratedLoadConfirmationForReview();
            if(_virtualController is not null||_simulatorShell.CaptureIntegratedLadderDraft()!=originalDraft)throw new InvalidOperationException("Template roundtrip must retain actual draft and remain stopped.");
            if(OS.GetCmdlineUserArgs().Contains("--template-edit-detail",StringComparer.Ordinal))
            {
                _shellView="workspace";
                _simulatorShell.SetReviewState("workspace",string.Empty);
                _simulatorShell.SetReviewState("inspector",string.Empty);
                SelectTemplateAssembly(WorkspaceTemplatePolicy.SelectionId);
            }
            if(draftState=="invalid")
            {
                RunEditorDraftController();
                if(_virtualController is not null||_simulatorShell.CaptureIntegratedLadderDraft()!=originalDraft)throw new InvalidOperationException("Invalid QA draft was executed or replaced.");
            }
            if(OS.GetCmdlineUserArgs().Contains("--template-editor-detail",StringComparer.Ordinal))
                        {
                _shellView="ladder";_simulatorShell.SetReviewState("ladder",string.Empty);
                var tables=_simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables");
                if(tables.Visible)_simulatorShell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle").EmitSignal(Button.SignalName.Pressed);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(tables.Visible)throw new InvalidOperationException("Template editor QA SceneIO did not collapse.");
                GD.Print("TEMPLATE_EDITOR_DETAIL_READY actualSceneIoCollapsed=true");
            }
            _simulatorShell.SetWorkspaceStatus("EPHEMERAL QA · template assembly edited + joint save/open · STOPPED · product defaults unchanged");
            File.WriteAllText(_capturePath+".template.json",JsonSerializer.Serialize(new{scene=_currentSceneId,provenance="real-workspace-row-inspector-edit-joint-save-open",runtime="stopped",template=CreateWorkspaceDocument().TemplateOverride,draftSha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(originalDraft))),draftPreserved=true,draftState=draftState??"existing",ephemeralQA=true,editorDetail=OS.GetCmdlineUserArgs().Contains("--template-editor-detail",StringComparer.Ordinal)}));
            _templateCapturePending=false;
            GD.Print("TEMPLATE_CAPTURE_READY ephemeralQA=true stopped=true scene="+_currentSceneId);
        }
        catch(Exception error){GD.PushError("TEMPLATE_CAPTURE_FAILED "+error);GetTree().Quit(2);}
    }
}
