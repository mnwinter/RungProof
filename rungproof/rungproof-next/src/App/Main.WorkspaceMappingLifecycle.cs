using System.Collections.Generic;
using Godot;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    // Ownership is acquired only after a generic adapter actually accepts an input.
    // Node identity survives link replacement; a replacement node does not inherit ownership.
    private readonly Dictionary<(ulong NodeId,string Signal),Node3D> _genericLiveInputs=[];
    private void ClearFormerGenericLiveInputs()
    {
        foreach(var entry in _genericLiveInputs)
        {
            var target=entry.Value;
            if(target==_workspaceDriveNode)continue; // Accepted drive mapping owns its sole clock.
            if(!GodotObject.IsInstanceValid(target)||target.IsQueuedForDeletion())continue;
            var belt=target.GetNodeOrNull<ConveyorController>("WorkspaceRuntimeAdapter");
            var roller=target.GetNodeOrNull<RollerConveyorController>("WorkspaceRuntimeAdapter");
            switch(entry.Key.Signal)
            {
                case "run_command": if(belt is not null)belt.RunCommand=false;if(roller is not null)roller.RunCommand=false;break;
                case "estop_ok": if(belt is not null)belt.EstopOk=false;if(roller is not null)roller.EstopOk=false;break;
                case "speed_setpoint": if(belt is not null)belt.SpeedSetpointMps=0;if(roller is not null)roller.SpeedSetpointMps=0;break;
            }
        }
        _genericLiveInputs.Clear();
    }
    private void OwnGenericLiveInput(Node3D target,string signal,SignalMappingRuntimeStatus status)
    {
        if(status.State==SignalMappingRuntimeState.LiveInput&&GodotObject.IsInstanceValid(target))
            _genericLiveInputs[(target.GetInstanceId(),signal)]=target;
    }
}
