using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class Main
{
    // Explicit output-projection fixture, not an authored-program acceptance
    // claim. Existing authored-case and controller-scan gates remain separate.
    private void VerifyMotorTestFixtures(SceneDefinition definition, Node3D root, SceneSimulationRuntime runtime)
    {
        var count = SceneComposer.VerifyMotorTestFixtureGeometry(root);
        if (count == 0) return;
        var motors = root.GetChildren().OfType<Node3D>().Where(n => n.HasMeta("motorTestMounted")).ToArray();
        var controllers = motors.ToDictionary(m => m, m => m.FindChildren("*", "", true, false)
            .OfType<EquipmentMotionController>().Single(c => c.Kind == EquipmentMotionController.MotionKind.ContinuousRotation));
        var shafts = motors.ToDictionary(m => m, m => m.FindChildren("KIN_motor_*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>().ToDictionary(n => n, n => n.Transform));
        var stationary = motors.ToDictionary(m => m, m => m.FindChildren("MOTOR_*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>().ToDictionary(n => n, n => n.Transform));
        void Check(bool condition, string failure)
        {
            if (!condition) throw new InvalidOperationException("Motor test fixture " + definition.Id + ": " + failure);
        }
        void Tick() { foreach (var controller in controllers.Values) controller._PhysicsProcess(.017); }
        void Reset()
        {
            runtime.ResetSimulation();
            Check(SceneComposer.VerifyMotorTestFixtureGeometry(root) == count, "reset lost supported roots");
            foreach (var motor in motors)
            {
                Check(motor.Transform.IsEqualApprox(motor.GetMeta("motorTestMountedTransform").AsTransform3D()), "reset lost mounted root " + motor.Name);
                Check(shafts[motor].All(p => p.Key.Transform.IsEqualApprox(p.Value)), "reset lost local shaft home " + motor.Name);
            }
        }
        var aliases = 0;
        try
        {
            foreach (var motor in motors)
            {
                var index = int.Parse(motor.Name.ToString().Split('_').Last());
                var points = count == 10 ? new[] { $"motor_{index}_run", $"motor_commands[{index}]" }
                    : definition.Id == "lab-10-05-motor-struct-data" ? new[] { "motor_enable", "motor_command.enable" }
                    : new[] { "motor_running" };
                foreach (var point in points)
                {
                    Reset();
                    Check(runtime.Points.ContainsKey(point), "missing declared command alias " + point);
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = true });
                    Check(controllers[motor].Running, "alias did not start selected shaft " + point);
                    Check(motors.Where(m => m != motor).All(m => !controllers[m].Running), "individual alias started another motor " + point);
                    Tick();
                    Check(shafts[motor].Count > 0 && shafts[motor].Any(p => !p.Key.Transform.IsEqualApprox(p.Value)), "accepted command produced no actual shaft motion " + point);
                    Check(stationary.All(s => s.Value.All(p => p.Key.Transform.IsEqualApprox(p.Value))), "stationary motor body moved");
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = false });
                    var stopped = shafts[motor].ToDictionary(p => p.Key, p => p.Key.Transform);
                    Tick();
                    Check(!controllers[motor].Running && stopped.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "false command failed to freeze shaft " + point);
                    runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { [point] = true });
                    runtime.StopSimulation();
                    stopped = shafts[motor].ToDictionary(p => p.Key, p => p.Key.Transform);
                    Tick();
                    Check(controllers.Values.All(c => !c.Running) && stopped.All(p => p.Key.Transform.IsEqualApprox(p.Value)), "Stop did not freeze shaft " + point);
                    Reset();
                    aliases++;
                    GD.Print($"MOTOR_TEST_ALIAS_PASS scene={definition.Id} equipment={motor.Name} point={point} fixture=explicit-output-projection motion=actual-local-shaft false=freeze stop=freeze reset=mounted-home");
                }
            }
            if (count == 10)
            {
                Reset();
                runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["motor_array_run"] = true });
                Tick();
                Check(motors.All(m => controllers[m].Running && shafts[m].Any(p => !p.Key.Transform.IsEqualApprox(p.Value))), "shared group alias did not move every shaft");
                runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["motor_array_run"] = false });
                Check(controllers.Values.All(c => !c.Running), "shared group false did not stop all motors");
                aliases++;
                Reset();
                var first = motors.Single(m => m.Name == "motor_0");
                void MixedImage(Dictionary<string, bool> image, bool all, bool firstOnly, string step)
                {
                    runtime.CommitVirtualControllerOutputs(image);
                    var before = motors.ToDictionary(m => m, m => shafts[m].ToDictionary(p => p.Key, p => p.Key.Transform));
                    Tick();
                    foreach (var motor in motors)
                    {
                        var expectedRunning = all || (firstOnly && motor == first);
                        Check(controllers[motor].Running == expectedRunning, "mixed OR command mismatch " + step + "/" + motor.Name);
                        Check(before[motor].Any(p => !p.Key.Transform.IsEqualApprox(p.Value)) == expectedRunning,
                            "mixed OR actual shaft step mismatch " + step + "/" + motor.Name);
                    }
                    GD.Print($"MOTOR_TEST_MIXED_OR_PASS scene={definition.Id} step={step} all={all} firstOnly={firstOnly} fixture=explicit-output-projection actualShaftStep=true");
                }
                MixedImage(new() { ["motor_0_run"] = true, ["motor_commands[0]"] = false }, false, true, "scalar-only");
                MixedImage(new() { ["motor_commands[0]"] = true }, false, true, "both-individual-aliases");
                MixedImage(new() { ["motor_0_run"] = false }, false, true, "clear-scalar-retain-array");
                MixedImage(new() { ["motor_commands[0]"] = false }, false, false, "clear-last-individual");
                MixedImage(new() { ["motor_array_run"] = true }, true, false, "group-with-individual-false");
                MixedImage(new() { ["motor_0_run"] = true }, true, false, "group-plus-one-individual");
                MixedImage(new() { ["motor_array_run"] = false }, false, true, "clear-group-retain-one");
                MixedImage(new() { ["motor_0_run"] = false }, false, false, "clear-final-source");
                Reset();
            }
            GD.Print($"MOTOR_TEST_FIXTURE_PASS scene={definition.Id} motors={count} aliases={aliases} geometry=actual-foot-pad-deck-floor-contact controllerProof=output-projection-only picking=existing-exact-control-centroid-gate");
        }
        finally { Reset(); }
    }
}
