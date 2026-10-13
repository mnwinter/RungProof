using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private MotorConveyorZonePlantModel? _motorConveyorZones;
    private readonly double[] _motorZoneAngles = new double[10];
    private readonly double[] _motorZoneBeltTravel = new double[10];
    private bool _motorZonesPaused;
    public bool HasMotorConveyorZones => _sceneRoot.HasNode("MotorConveyorInstallation");
    public (double Center, int Zone, bool AtDischarge) CaptureMotorConveyorZones()
    {
        if (_motorConveyorZones is null) throw new InvalidOperationException("No ten-zone conveyor plant.");
        return (_motorConveyorZones.Center,_motorConveyorZones.Zone,_motorConveyorZones.AtDischarge);
    }
    private void ResetMotorConveyorZones()
    {
        if (!HasMotorConveyorZones) return;
        _motorConveyorZones ??= new MotorConveyorZonePlantModel();
        _motorConveyorZones.Reset();
        _motorZonesPaused=false;
        System.Array.Clear(_motorZoneAngles);
        System.Array.Clear(_motorZoneBeltTravel);
        FreezeMotorConveyorAdapters();
        ProjectMotorConveyorZones();
    }
    private void AdvanceMotorConveyorZones(double delta)
    {
        if (_motorConveyorZones is null || _booleanPreviewStopped || _motorZonesPaused) return;
        var commands=new bool[10];
        for(var i=0;i<10;i++) commands[i]=AsBool(_points.GetValueOrDefault("motor_array_run"))
            || AsBool(_points.GetValueOrDefault($"motor_{i}_run")) || AsBool(_points.GetValueOrDefault($"motor_commands[{i}]"));
        _motorConveyorZones.Step(delta,commands);
        // Fixed reduction illustration:1450 RPM shaft -> .09m drum -> .45m/s.
        // Both original motor and drum integrate only this accepted plant
        // clock. Automatic motor callbacks are disabled, avoiding double ticks.
        for(var i=0;i<10;i++)
        {
            var motor=_sceneRoot.GetNode<Node3D>("motor_"+i);
            var controller=motor.FindChildren("*","",true,false).OfType<EquipmentMotionController>().Single();
            controller._PhysicsProcess(delta);
            if(commands[i])
            {
                _motorZoneAngles[i]=(_motorZoneAngles[i]+delta*MotorConveyorZonePlantModel.Speed/.09)%(Math.PI*2);
                _motorZoneBeltTravel[i]=(_motorZoneBeltTravel[i]+delta*MotorConveyorZonePlantModel.Speed)%1.38;
            }
        }
        ProjectMotorConveyorZones();
        ApplyBindings();
        StateChanged?.Invoke();
    }
    private void FreezeMotorConveyorAdapters()
    {
        if(!HasMotorConveyorZones) return;
        for(var i=0;i<10;i++) foreach(var controller in _sceneRoot.GetNode<Node3D>("motor_"+i).FindChildren("*","",true,false).OfType<EquipmentMotionController>())
            controller.SetPhysicsProcess(false);
    }
    private void ProjectMotorConveyorZones()
    {
        var installation=_sceneRoot.GetNode<Node3D>("MotorConveyorInstallation");
        installation.GetNode<Node3D>("TeachingCarton").Position=new Vector3(-7+(float)_motorConveyorZones!.Center,1.46f,0);
        for(var i=0;i<10;i++) installation.GetNode<Node3D>($"Zone_{i}/KIN_zone_drive").Rotation=new Vector3(MathF.PI/2,0,(float)_motorZoneAngles[i]);
        for(var i=0;i<10;i++)
        {
            var belt=installation.GetNode<MeshInstance3D>($"Zone_{i}/ZoneBeltSurface");
            if(belt.MaterialOverride is StandardMaterial3D material)
                material.Uv1Offset=new Vector3((float)(-_motorZoneBeltTravel[i]/1.38),0,0);
        }
        // PC feedback is a physical sensing-line observation, not a command,
        // whole-zone presence flag, readiness or fabricated diagnostic.
        // Commit after this accepted plant step; next scan samples the image.
        var inverse=_sceneRoot.GlobalTransform.AffineInverse();
        for(var i=0;i<10;i++)
        {
            var point="zone_occupied_"+i;
            if(_pointOwners.GetValueOrDefault(point)!="PC" || _pointTypes.GetValueOrDefault(point)!="BOOL")
                throw new InvalidOperationException("Zone sensing feedback requires declared PC BOOL: "+point);
            var sensor=_sceneRoot.GetNode<Node3D>("zone_sensor_"+i);
            var beams=sensor.FindChildren("KIN_beam*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
                .Where(m=>m.Mesh is not null).Select(m=>(inverse*m.GlobalTransform)*m.GetAabb()).ToArray();
            if(beams.Length==0) throw new InvalidOperationException("Zone sensor has no physical sensing beam.");
            var beam=beams.Aggregate((a,b)=>a.Merge(b));
            if(beam.GetCenter().Y<1.29f || beam.GetCenter().Y>1.63f || beam.Position.Z>0 || beam.End.Z<0)
                throw new InvalidOperationException("Zone beam misses the carton height/corridor.");
            SetExistingPoint(point,_motorConveyorZones!.BeamBlocked(beam.GetCenter().X+7));
        }
    }
}
