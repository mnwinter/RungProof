using System;
using System.Collections.Generic;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasServiceElevator => Text(_definition,"installation","")=="serviceElevator";
    private EquipmentMotionController? _elevatorCarMotion;
    private Node3D? _elevatorShaft;
    private float _elevatorPosition;
    // Offline modeled closure, separate from the manual doors_closed fixture.
    // One second of selected plant-clock time spans the full leaf travel.
    private double _elevatorDoorClosure;

    private void ResetServiceElevator()
    {
        if(!HasServiceElevator) return;
        _elevatorShaft=_sceneRoot.GetNode<Node3D>("liftTable_0");
        _elevatorCarMotion=_elevatorShaft.GetNode<EquipmentMotionController>("ServiceElevatorPosition");
        _elevatorPosition=0;
        _elevatorDoorClosure=0;
        ProjectServiceElevator();
    }

    private void AdvanceServiceElevator(double seconds)
    {
        if(!UsesExternalClock) return;
        if(!double.IsFinite(seconds)||seconds<=0) return;
        var up=AsBool(_points.GetValueOrDefault("lift_up_cmd"));
        var down=AsBool(_points.GetValueOrDefault("lift_down_cmd"));
        // Preserve the controller's output image for diagnostics. Conflicting
        // commands or missing manual permissives inhibit modeled displacement.
        var enabled=AsBool(_points.GetValueOrDefault("call_valid"))
            && AsBool(_points.GetValueOrDefault("doors_closed"))
            && AsBool(_points.GetValueOrDefault("landing_clear"))
            && _elevatorDoorClosure>=1;
        // Evaluate displacement against the previous completed door pose:
        // reaching closure during this step permits travel on the next step.
        var closing=AsBool(_points.GetValueOrDefault("doors_closed"));
        _elevatorDoorClosure=Math.Clamp(_elevatorDoorClosure+(closing?seconds:-seconds),0,1);
        if(enabled && up!=down)
            _elevatorPosition=Math.Clamp(_elevatorPosition+(up?1:-1)*(float)(seconds/6),0,1);
        SetPoint("direction_conflict",up&&down);
        ProjectServiceElevator();
        ApplyBindings();
        StateChanged?.Invoke();
    }

    private void ProjectServiceElevator()
    {
        _elevatorCarMotion!.SetPositionNormalized(_elevatorPosition);
        SceneComposer.ProjectServiceElevatorHoist(_elevatorShaft!);
        SetPoint("car_position_pct",(double)_elevatorPosition*100);
        SetPoint("at_lower_landing",_elevatorPosition<=.00001f);
        SetPoint("at_upper_landing",_elevatorPosition>=.99999f);
        // Fixture selects direction; the rendered pose follows plant time.
        // This closure gate is symbolic model state, not hardware sensing.
        foreach(var part in _elevatorShaft!.GetChildren())
        {
            if(part is not Node3D leaf || !leaf.Name.ToString().StartsWith("ELEVATOR_open_door_leaf_",StringComparison.Ordinal)) continue;
            var side=MathF.Sign(leaf.Position.X);
            leaf.Position=new Vector3(side*(1.72f-(1.72f-.55f)*(float)_elevatorDoorClosure),leaf.Position.Y,leaf.Position.Z);
        }
    }
}
