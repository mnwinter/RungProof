using System;
using Godot;

namespace RungProof.Next.App;
public partial class SimulatorShell
{
    public event Action<string>? WorkspaceDriveConnectionRequested;
    private Button? _workspaceDriveConnectionButton;

    private void AddWorkspaceDriveConnectionButton(VBoxContainer body)
    {
        var button=ToolbarButton("ConnectBeltDrive","CONNECT BELT DRIVE · OFFLINE",new Color("276b89"));
        _workspaceDriveConnectionButton=button;
        button.Visible=false;
        button.Disabled=true;
        button.Pressed+=()=>
        {
            // UI eligibility is supplied by Main. Guard programmatic emissions
            // too: an unavailable/connected button cannot request another relation.
            if(button.Disabled||!button.Visible||_selectedPlacementId is null)return;
            WorkspaceDriveConnectionRequested?.Invoke(_selectedPlacementId);
        };
        body.AddChild(button);
    }

    public void SetWorkspaceDriveConnectionAvailability(bool eligible,bool connected)
    {
        if(_workspaceDriveConnectionButton is null)return;
        _workspaceDriveConnectionButton.Visible=eligible||connected;
        _workspaceDriveConnectionButton.Disabled=!eligible||connected;
        _workspaceDriveConnectionButton.TooltipText=connected
            ? "An offline belt drive connection is already registered in this workspace."
            : eligible?"Connect the selected belt's offline run command and measured running/speed feedback."
            : "Select an eligible added belt in the supported offline workspace.";
    }
}
