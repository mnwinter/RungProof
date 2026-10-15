using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private static LadderEditorDocument CreateGuardedTransferReference()
    {
        var document = new LadderEditorDocument();
        document.ResetProject("review-guarded-transfer", "Guarded_Transfer_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-3-01-guarded-pallet-transfer";
        foreach (var name in new[] { "guard_closed", "entry_clear", "exit_clear", "entry_carton_present", "exit_carton_present", "transfer_complete" })
            document.AddTag(name, PlcVariableRole.Input, name);
        document.AddTag("carton_position", PlcVariableRole.Input, "carton_position", type: PlcVariableType.Real);
        foreach (var name in new[] { "transfer_permissive", "transfer_run" })
            document.AddTag(name, PlcVariableRole.Output, name);
        var permissive = document.Rungs.Count;
        document.AddRung("Manual fixture protection and clear-path inputs agree", "transfer_permissive");
        foreach (var name in new[] { "guard_closed", "entry_clear", "exit_clear" })
            document.AddContact(permissive, 0, name, false);
        var run = document.Rungs.Count;
        document.AddRung("Permissive transfers one carton until supported exit completion", "transfer_run");
        document.AddContact(run, 0, "transfer_permissive", false);
        document.AddContact(run, 0, "transfer_complete", true);
        document.WatchVariables.AddRange(document.Tags.Select(tag => tag.Name));
        return document;
    }

    private void AuditGuardedTransferReference(Action<bool, string> check)
    {
        var document = CreateGuardedTransferReference();
        var compiled = LadderCompiler.Compile(document.BuildProgram());
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join(";", compiled.Issues));
        // Explicit audit-only file; normal exercises keep their blank programs.
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-guarded-transfer.rpproj.json"), LadderEditorProjectJson.Save(document));
        _sceneRuntime!.ResetSimulation();
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            var runtime = _sceneRuntime!;
            var carton = _sceneCompositionRoot!.GetNode<Node3D>("box_1");
            var home = carton.Transform;
            bool On(string name) => runtime.Points[name] is true;
            void Tick(int count = 1) { for (var index = 0; index < count; index++) _PhysicsProcess(.02); }
            var inputs = new[] { "guard_closed", "entry_clear", "exit_clear" };
            void Toggle(string name)
            {
                if (!ExecuteSelectedControllerAction("toggle-" + name))
                    throw new InvalidOperationException($"Guarded transfer fixture action rejected: {name}.");
            }
            for (var mask = 0; mask < 8; mask++)
            {
                ResetActiveController();
                RunActiveController();
                for (var bit = 0; bit < inputs.Length; bit++) if ((mask & (1 << bit)) != 0) Toggle(inputs[bit]);
                Tick(2);
                check(On("transfer_permissive") == (mask == 7) && On("transfer_run") == (mask == 7)
                    && (mask == 7 || carton.Transform == home), $"reference_manual_permissive_truth_table_{mask}");
            }
            foreach (var lost in inputs)
            {
                ResetActiveController(); RunActiveController();
                foreach (var input in inputs) Toggle(input);
                Tick(50);
                Toggle(lost); Tick();
                var held = carton.Transform; Tick(25);
                check(!On("transfer_run") && !On("transfer_permissive") && carton.Transform == held,
                    $"reference_losing_{lost}_withdraws_command_and_holds_carton");
            }
            ResetActiveController(); RunActiveController();
            foreach (var input in inputs) Toggle(input);
            Tick(100);
            StopActiveController();
            var paused = carton.Transform;
            var scans = _virtualController!.Snapshot.ScanNumber;
            Tick(25);
            check(carton.Transform == paused && !On("transfer_run") && _virtualController.Snapshot.ScanNumber == scans,
                "reference_stop_freezes_scan_and_retains_carton");
            RunActiveController(); Tick();
            check(carton.Position.X > paused.Origin.X && On("transfer_run"), "reference_run_resumes_retained_transfer");
            var sawEntry = false; var sawExit = false;
            for (var tick = 0; tick < 700 && !On("transfer_complete"); tick++)
            {
                Tick(); sawEntry |= On("entry_carton_present"); sawExit |= On("exit_carton_present");
            }
            Tick();
            check(sawEntry && sawExit && On("transfer_complete") && !On("transfer_run") && On("transfer_permissive") && carton.Visible,
                "reference_normal_scan_transfers_across_both_routes_and_stops_at_retained_exit");
            var exit = carton.Transform; Tick(50);
            check(carton.Transform == exit && !On("transfer_run"), "reference_completion_prevents_held_input_restart");
            ResetActiveController();
            check(carton.Transform == home && !On("transfer_complete") && !On("transfer_run")
                && inputs.All(name => !On(name)) && _virtualController.Snapshot.ScanNumber == 0,
                "reference_reset_restores_infeed_inputs_and_scan_state");
        }
        finally { DisableVirtualController(); }
    }
}
