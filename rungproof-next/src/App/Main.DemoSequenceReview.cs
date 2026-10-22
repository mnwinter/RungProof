using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyDemoSequence;

    // Exercise the authored height-sort Demo 3 through the same
    // input/session/plant route as the application. No force, direct output
    // write or position fixture is used.
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
            const string scene = "scene-2-conveyor-pusher";
            AddMigratedScene(scene, _candidateCatalog!, _mainCamera!, false, false);
            if (!AuthoredDemoLadderPrograms.TryCreate(scene, out var document))
                throw new InvalidOperationException("Authored Demo 3 missing.");
            EnableVirtualControllerProgram(document.BuildProgram());
            var runtime = _sceneRuntime!;
            var product = _sceneCompositionRoot!.GetNode<Node3D>("scene2_product");
            var sensor = _sceneCompositionRoot.GetNode<Node3D>("scene2_photoeye");
            var beams = sensor.FindChildren("KIN_beam*", "", true, false).OfType<Node3D>().ToArray();
            var home = product.Transform;
            void Tick(int count = 1) { for (var i = 0; i < count; i++) _PhysicsProcess(.02); }
            bool Conveyor() => _virtualController!.Snapshot.Outputs["conveyor_running"];
            bool Pusher() => _virtualController!.Snapshot.Outputs["pusher_extend"];
            Check(beams.Length > 0 && beams.All(b => b.Visible), "initial_beam_visible");
            RunActiveController(); Tick(5);
            Check(!Conveyor() && !Pusher() && product.Transform == home, "app_run_alone_does_not_start_machine");
            Check(ExecuteSelectedControllerAction("toggle-system_enable"), "system_enable_action_accepted");
            Check(ExecuteSelectedControllerAction("pulse-start_command"), "machine_start_action_accepted");
            Tick(2);
            Check(_virtualController!.Snapshot.Variables["machine_run"] && Conveyor()
                && _virtualController.Snapshot.Forces.Count == 0, "machine_start_seals_and_runs_without_forces");

            var routes = new System.Collections.Generic.List<(long Height, string Destination)>();
            long measuredHeight = 0;
            var tallPusherCommandSeen = false;
            var ticks = 0;
            while (routes.Count < 3 && ticks++ < 6000)
            {
                Tick();
                if (runtime.Points["part_at_pusher"] is true)
                {
                    measuredHeight = Convert.ToInt64(runtime.Points["carton_height_mm"]);
                    if (measuredHeight == 820 && Pusher()) tallPusherCommandSeen = true;
                }
                if (runtime.Points["main_line_exit"] is true
                    && !routes.Any(route => route.Height == measuredHeight && route.Destination == "main"))
                    routes.Add((measuredHeight, "main"));
                if (runtime.Points["side_line_exit"] is true
                    && !routes.Any(route => route.Height == measuredHeight && route.Destination == "side"))
                    routes.Add((measuredHeight, "side"));
            }
            Check(ticks < 6000, "three_cartons_reach_measured_exit_feedback");
            Check(routes.Contains((420L, "main")), "short_420mm_carton_passes_main_line");
            Check(routes.Contains((820L, "side")), "tall_820mm_carton_diverts_side_line");
            Check(routes.Contains((600L, "main")), "boundary_600mm_carton_passes_main_line");
            Check(tallPusherCommandSeen, "tall_carton_uses_plc_pusher_command");

            Check(ExecuteSelectedControllerAction("pulse-stop_command"), "machine_stop_action_accepted");
            Tick(2); var machineStopped = product.Transform;
            Check(_virtualController!.Snapshot.Variables["machine_run"] is false && !Conveyor() && !Pusher(),
                "machine_stop_drops_run_and_actuator_commands");
            Tick(20);
            Check(product.Transform == machineStopped, "machine_stop_holds_carton_pose");
            Check(ExecuteSelectedControllerAction("pulse-start_command"), "fresh_start_after_machine_stop_accepted");
            Tick(2);
            Check(_virtualController!.Snapshot.Variables["machine_run"] is true, "machine_stop_recovery_requires_and_accepts_fresh_start");

            Check(ExecuteSelectedControllerAction("toggle-safety_trip"), "simulated_trip_action_accepted");
            Tick(2);
            Check(_virtualController!.Snapshot.Variables["machine_run"] is false && !Conveyor() && !Pusher(),
                "simulated_trip_drops_machine_commands");
            Check(ExecuteSelectedControllerAction("toggle-safety_trip"), "simulated_trip_clear_action_accepted");
            Tick(2);
            Check(_virtualController!.Snapshot.Variables["machine_run"] is false, "trip_clear_does_not_auto_restart");

            Check(ExecuteSelectedControllerAction("pulse-reset_command"), "machine_reset_action_accepted");
            Tick(2);
            Check(_virtualController!.Snapshot.Variables["machine_run"] is false && product.Transform.IsEqualApprox(home)
                && Convert.ToInt64(runtime.Points["parts_completed"]) == 0 && !Conveyor() && !Pusher(),
                "machine_reset_restores_repeatable_plant_and_idle_logic");
            Check(ExecuteSelectedControllerAction("pulse-start_command"), "repeat_start_action_accepted");
            Tick(2);
            Check(_virtualController!.Snapshot.Variables["machine_run"] is true && Conveyor(), "fresh_start_runs_again_after_machine_reset");

            StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; var appStopped = product.Transform; Tick(20);
            Check(product.Transform == appStopped && _virtualController.Snapshot.ScanNumber == scan,
                "app_stop_separately_freezes_scan_and_plant");
            ResetActiveController();
            Check(product.Transform.IsEqualApprox(home) && !Conveyor() && !Pusher()
                && _virtualController!.Snapshot.ScanNumber == 0
                && _virtualController.Snapshot.State == VirtualControllerState.Stopped,
                "app_reset_returns_stopped_initial_state");
        }
        catch (Exception error) { failures++; GD.PushError($"DEMO_SEQUENCE_EXCEPTION {error}"); }
        GD.Print($"DEMO_SEQUENCE_VERIFY {(failures == 0 ? "PASS" : "FAIL")} failures={failures} offline-only; mesh/feedback integration, not native visual acceptance");
        DisableVirtualController();
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
