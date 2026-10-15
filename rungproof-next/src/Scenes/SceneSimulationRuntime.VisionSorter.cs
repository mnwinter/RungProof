using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasVisionSorter => Text(_definition, "plantModel", "") == "visionSorter";
    private readonly VisionSorterPlantModel _visionSorter = new();
    private Node3D? _sorterCarton, _sorterPlaten;
    private Vector3 _sorterCartonHome, _sorterCartonRotation, _sorterPlatenRotation;
    private readonly List<ConveyorController> _sorterBelts = [];

    private void ResetVisionSorter()
    {
        if (!HasVisionSorter) return;
        _sorterCarton = _sceneRoot.GetNode<Node3D>("box_1");
        _sorterCartonHome = _initialEquipmentPositions["box_1"];
        _sorterCartonRotation = _initialEquipmentRotations["box_1"];
        _sorterPlaten = _sceneRoot.GetNode<Node3D>("rotaryTable_3")
            .FindChildren("KIN_table*", "Node3D", true, false).OfType<Node3D>().Single();
        _sorterPlatenRotation = _sorterPlaten.Rotation;
        _sorterBelts.Clear();
        foreach (var id in new[] { "conveyor_0", "destination_lane_1", "destination_lane_2", "destination_lane_3", "destination_lane_4" })
        {
            var drive = Controllers(_sceneRoot.GetNode<Node3D>(id)).OfType<ConveyorController>().Single();
            drive.ResetPlantTravel(); _sorterBelts.Add(drive);
        }
        _visionSorter.Reset(); FreezeVisionSorterAdapters(); ProjectVisionSorter();
    }

    private void FreezeVisionSorterAdapters()
    {
        if (!HasVisionSorter) return;
        foreach (var belt in _sorterBelts) belt.SetPhysicsProcess(false);
        var table = _sceneRoot.GetNodeOrNull<Node3D>("rotaryTable_3");
        if (table is not null)
            foreach (var controller in Controllers(table)) controller.SetPhysicsProcess(false);
    }

    private void AdvanceVisionSorter(double seconds)
    {
        // Exercises move only from accepted loaded-controller scans.
        if (!UsesExternalClock) return;
        var beforeX = _visionSorter.InfeedX;
        var beforeRadius = _visionSorter.OutfeedRadius;
        _visionSorter.Step(seconds, AsBool(_points["sort_conveyor_run"]), AsBool(_points["diverter_enable"]),
            AsBool(_points["package_present"]), AsBool(_points["vision_result_valid"]),
            AsBool(_points["destination_clear"]), Convert.ToInt32(_points["vision_class"], CultureInfo.InvariantCulture));
        if (seconds > 0)
        {
            var incoming = (float)(_visionSorter.InfeedX-beforeX);
            _sorterBelts[0].ApplyPlantTravel(incoming, incoming/(float)seconds);
            for (var lane=1; lane<=4; lane++)
            {
                var travel = lane==_visionSorter.LatchedClass ? (float)(_visionSorter.OutfeedRadius-beforeRadius) : 0;
                _sorterBelts[lane].ApplyPlantTravel(travel,travel/(float)seconds);
            }
        }
        ProjectVisionSorter(); ApplyBindings(); StateChanged?.Invoke();
    }

    private void ProjectVisionSorter()
    {
        _sorterCarton!.Position = new Vector3((float)_visionSorter.X, _sorterCartonHome.Y, (float)_visionSorter.Z);
        _sorterCarton.RotationDegrees = _sorterCartonRotation + Vector3.Up*(float)_visionSorter.YawDegrees;
        _sorterPlaten!.Rotation = _sorterPlatenRotation + Vector3.Up*Mathf.DegToRad((float)_visionSorter.YawDegrees);
        _sorterCarton.Visible = true;
        SetPoint("sort_complete",_visionSorter.Done);
        SetPoint("sort_latched_class",_visionSorter.LatchedClass);
        SetPoint("sort_phase",_visionSorter.Phase.ToString());
    }
}
