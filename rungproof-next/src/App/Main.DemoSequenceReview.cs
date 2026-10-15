using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyDemoSequence;

    // Exercise the authored Demo 3 through the same input/session/plant route
    // as the application. No force, direct output write or position fixture.
    private void VerifyDemoSequence()
    {
        var failures = 0;
        void Check(bool value, string name)
        {
            if (!value) failures++;
            GD.Print($"DEMO_SEQUENCE_CHECK {name}={value}");
        }
        try
        {
            const string scene = "scene-1-conveyor-stop";
            AddMigratedScene(scene, _candidateCatalog!, _mainCamera!, false, false);
            if (!AuthoredDemoLadderPrograms.TryCreate(scene, out var document))
                throw new InvalidOperationException("Authored Demo 3 missing.");
            EnableVirtualControllerProgram(document.BuildProgram());
            var runtime = _sceneRuntime!;
            var product = _sceneCompositionRoot!.GetNode<Node3D>("scene1_product");
            var sensor = _sceneCompositionRoot.GetNode<Node3D>("scene1_photoeye");
            var beams = sensor.FindChildren("KIN_beam*", "", true, false).OfType<Node3D>().ToArray();
            var home = product.Transform;
            void Tick(int count = 1) { for (var i = 0; i < count; i++) _PhysicsProcess(.02); }
            bool Output() => _virtualController!.Snapshot.Outputs["conveyor_running"];
            bool Eye() => runtime.Points["simulated_photoeye"] is true;
            bool Seal() => _virtualController!.Snapshot.Variables["seal_in"];
            Check(beams.Length > 0 && beams.All(b => b.Visible), "initial_beam_visible");
            RunActiveController(); Tick(5);
            Check(!Output() && !Seal() && product.Transform == home, "run_alone_idle_no_motion");
            Check(ExecuteSelectedControllerAction("scene-toggle"), "clean_start_action_accepted");
            Tick();
            Check(Output() && Seal() && product.Transform != home
                && runtime.Points["conveyor_running"] is true, "start_scan_seal_output_and_actual_carton_motion");
            Tick();
            Check(!_virtualController!.Snapshot.Variables["start_command"] && Seal()
                && Output() && _virtualController.Snapshot.Forces.Count == 0,
                "start_pulse_released_latch_runs_without_forces");
            var ticks = 0;
            while (!Eye() && ticks++ < 200) Tick();
            Check(Eye() && ticks < 200 && Output() && Seal() && product.Transform != home && beams.All(b => !b.Visible),
                "actual_carton_motion_reaches_sensor_and_hides_beam");
            // Feedback projects after plant travel. LD consumes that image on
            // the next scan, consistent with ordinary fixed scan ordering.
            Tick(); var stoppedAtBeam = product.Transform;
            Check(!Output() && !Seal() && runtime.Points["conveyor_running"] is false,
                "next_scan_photoeye_breaks_seal_and_removes_output");
            Tick(50);
            Check(product.Transform == stoppedAtBeam && Eye() && !Output(), "sensor_stop_retains_actual_carton_pose");
            ResetActiveController();
            Check(product.Transform == home && !Eye() && !Output() && !Seal()
                && beams.All(b => b.Visible) && _virtualController!.Snapshot.ScanNumber == 0,
                "reset_restores_carton_sensor_beam_and_controller_image");
            RunActiveController();
            Check(ExecuteSelectedControllerAction("scene-toggle"), "second_clean_start_accepted"); Tick(8);
            Check(Output() && !Eye() && product.Transform != home, "early_motion_before_photoeye");
            Check(ExecuteSelectedControllerAction("scene-toggle"), "clean_stop_pulse_accepted");
            Tick(); var earlyStopped = product.Transform;
            Check(!Output() && !Seal() && _virtualController!.Snapshot.Variables["stop_command"],
                "stop_pulse_breaks_latch_before_sensor");
            Tick(20);
            Check(product.Transform == earlyStopped && !Eye() && !Output()
                && !_virtualController!.Snapshot.Variables["stop_command"], "released_stop_pulse_and_pose_hold");
            StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; Tick(20);
            Check(product.Transform == earlyStopped && _virtualController.Snapshot.ScanNumber == scan,
                "controller_stop_freezes_scan_and_pose");
            ResetActiveController();
            Check(product.Transform == home && !Eye() && !Output() && !Seal()
                && _virtualController!.Snapshot.ScanNumber == 0
                && _virtualController.Snapshot.State == VirtualControllerState.Stopped,
                "final_reset_stopped_initial_state");
        }
        catch (Exception error) { failures++; GD.PushError($"DEMO_SEQUENCE_EXCEPTION {error}"); }
        GD.Print($"DEMO_SEQUENCE_VERIFY {(failures == 0 ? "PASS" : "FAIL")} failures={failures} offline-only; mesh/feedback integration, not native visual acceptance");
        DisableVirtualController();
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
