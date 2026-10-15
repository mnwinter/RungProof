using System;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    // Observe actual motor meshes after the existing QA ladder's accepted
    // scans. No point injection and no authored-lesson-program claim.
    private (Action<int, string> Step, Action<string> StopReset) CaptureMotorProgramMotion(Action<bool, string> check)
    {
        var root = _sceneCompositionRoot!;
        var motors = root.GetChildren().OfType<Node3D>().Where(n => n.HasMeta("motorTestMounted"))
            .OrderBy(m => int.Parse(m.Name.ToString().Split('_').Last())).ToArray();
        var controllers = motors.ToDictionary(m => m, m => m.FindChildren("*", "", true, false)
            .OfType<EquipmentMotionController>().Single(c => c.Kind == EquipmentMotionController.MotionKind.ContinuousRotation));
        var homes = motors.ToDictionary(m => m, m => m.Transform);
        var shaftHomes = motors.ToDictionary(m => m, m => m.FindChildren("KIN_motor_*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>().ToDictionary(n => n, n => n.Transform));
        var bodies = motors.SelectMany(m => m.FindChildren("MOTOR_*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>()).ToDictionary(n => n, n => n.Transform);
        void Step(int runningCount, string label)
        {
            var before = shaftHomes.ToDictionary(p => p.Key, p => p.Value.ToDictionary(n => n.Key, n => n.Key.Transform));
            foreach (var motor in motors) controllers[motor]._PhysicsProcess(.017);
            check(motors.Length > 0 && motors.Select((motor, index) => controllers[motor].Running == (index < runningCount)
                && before[motor].Count > 0
                && before[motor].Any(n => !n.Key.Transform.IsEqualApprox(n.Value)) == (index < runningCount)).All(v => v), label + "_actual_shaft_prefix");
            check(homes.All(p => p.Key.Transform.IsEqualApprox(p.Value)) && bodies.All(p => p.Key.Transform.IsEqualApprox(p.Value)), label + "_stationary_root_stator");
        }
        void StopReset(string label)
        {
            StopActiveController();
            var stopped = shaftHomes.SelectMany(p => p.Value.Keys).ToDictionary(n => n, n => n.Transform);
            foreach (var controller in controllers.Values) controller._PhysicsProcess(.017);
            check(controllers.Values.All(c => !c.Running) && stopped.All(p => p.Key.Transform.IsEqualApprox(p.Value)), label + "_actual_Main_Stop_freezes");
            ResetActiveController();
            check(homes.All(p => p.Key.Transform.IsEqualApprox(p.Value)) && shaftHomes.All(p => p.Value.All(n => n.Key.Transform.IsEqualApprox(n.Value))), label + "_actual_Main_Reset_mounted_root_local_shaft");
            check(SceneComposer.VerifyMotorTestFixtureGeometry(root) == motors.Length, label + "_reset_actual_foot_bearing");
            GD.Print($"MOTOR_AGGREGATE_MESH_PROOF scene={_activeSceneDefinition!.Id} motors={motors.Length} provenance=existing-explicit-QA-ladder acceptedScanOutputs=true actualShaft=true MainStopReset=true");
        }
        return (Step, StopReset);
    }
}
