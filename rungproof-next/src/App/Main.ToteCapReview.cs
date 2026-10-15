using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyToteFillCapLadder(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
        var project = LadderEditorProjectJson.Load(System.IO.File.ReadAllText(
            "programs/examples/tote-fill-cap-review.rpproj.json"));
        if (project.Document is null) throw new InvalidOperationException("Tote fill/cap QA could not be loaded.");
        EnableVirtualControllerProgram(project.Document.BuildProgram());
        try
        {
            RunActiveController();
            ExecuteSelectedControllerAction("start-line");
            for (var scan = 0; scan < 1500; scan++) _PhysicsProcess(.02);
            GD.Print($"TOTE_QA_FINAL position={_sceneRuntime!.Points["tote_position"]} fill={_sceneRuntime.Points["fill_complete"]} applied={_sceneRuntime.Points["cap_applied"]} home={_sceneRuntime.Points["cap_home"]} inhibited={_sceneRuntime.Points["cap_inhibited"]}");
            check(_sceneRuntime.Points["fill_complete"] is true
                && _sceneRuntime.Points["cap_applied"] is true
                && _sceneRuntime.Points["cap_home"] is true
                && _sceneRuntime.Points["tote_at_exit"] is true,
                "editor_ladder_fills_caps_retracts_and_retains_supported_exit");
            StopActiveController();
            check(_sceneRuntime.Points["cap_applied"] is true
                && _sceneRuntime.Points["conveyor_run"] is false
                && _sceneRuntime.Points["capper_run"] is false,
                "editor_ladder_stop_preserves_applied_cap_and_clears_commands");
            ResetActiveController();
            check(_sceneRuntime.Points["fill_complete"] is false
                && _sceneRuntime.Points["cap_applied"] is false
                && _sceneRuntime.Points["cap_home"] is true,
                "editor_ladder_reset_restores_empty_open_tote_and_chuck_home");
        }
        finally { DisableVirtualController(); }
    }

    private void VerifyToteCap(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        runtime.UsesExternalClock = true; runtime.SetControllerPlaybackRunning(true);
        void Command(bool travel, bool fill, bool cap) => runtime.CommitVirtualControllerOutputs(
            new Dictionary<string,bool> { ["conveyor_run"]=travel, ["fill_valve_open"]=fill, ["capper_run"]=cap });
        var screenStroke=false;var strokeClear=true;var routeClear=true;
        void Tick(int count, double dt=.02)
        {
            for(var i=0;i<count;i++)
            {
                runtime.AdvanceSimulation(dt);
                if(screenStroke) ScreenMotion();
            }
        }
        bool On(string point) => runtime.Points[point] is true;
        double Number(string point) => Convert.ToDouble(runtime.Points[point]);
        var tote = _sceneCompositionRoot!.GetNode<Node3D>("finishing_tote");
        var cap = (MeshInstance3D)tote.FindChild("IBC_fill_cap",true,false);
        var station = _sceneCompositionRoot.GetNode<Node3D>("capper");
        var held = (MeshInstance3D)station.FindChild("CAP_UNDER_CHUCK",true,false);
        var spindle = (Node3D)station.FindChild("KIN_capper_spindle",true,false);
        var home = spindle.GlobalPosition;
        var homeBasis = spindle.GlobalBasis;
        var neck=(MeshInstance3D)tote.FindChild("IBC_open_fill_neck",true,false);
        bool Clear(MeshInstance3D a,MeshInstance3D b)
        {
            var overlap=ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            if(overlap.X<=.001f || overlap.Y<=.001f || overlap.Z<=.001f || !OrientedBoxesPenetrate(a,b)) return true;
            // The held cap surrounds an empty bore. Accept only measured
            // radial clearance and an inner roof at/above the mating rim.
            if((a==neck && b==held) || (a==held && b==neck))
            {
                var heldBounds=ReviewBounds(held);var center=heldBounds.GetCenter();
                var wall=held.Mesh.GetFaces().Select(v=>held.GlobalTransform*v)
                    .Where(v=>v.Y<heldBounds.Position.Y+heldBounds.Size.Y*.35f).ToArray();
                var inner=wall.Min(v=>new Vector2(v.X-center.X,v.Z-center.Z).Length());
                var radiallyClear=neck.Mesh.GetFaces().All(v=>
                {
                    var w=neck.GlobalTransform*v;
                    return new Vector2(w.X-center.X,w.Z-center.Z).Length()<inner-.001f;
                });
                var roofThickness=ReviewBounds(cap).End.Y-ReviewBounds(neck).End.Y;
                if(radiallyClear && heldBounds.End.Y-roofThickness>=ReviewBounds(neck).End.Y-.001f) return true;
            }
            GD.Print($"TOTE_CAP_CLEARANCE {a.Name}/{b.Name} a={ReviewBounds(a)} b={ReviewBounds(b)}");
            return false;
        }
        void ScreenMotion()
        {
            foreach(var part in ReviewMeshes(tote).Where(m=>m.Name!="IBC_fill_witness"))
            foreach(var other in ReviewMeshes(station)) strokeClear &= Clear(part,other);
            if(cap.Visible)
                foreach(var node in _sceneCompositionRoot.GetChildren().OfType<Node3D>().Where(n=>n!=tote))
                foreach(var other in ReviewMeshes(node).Where(m=>m.Name!="FINISHING_stream")) routeClear &= Clear(cap,other);
        }
        Command(false,false,true);Tick(20);
        check(On("cap_inhibited") && !On("cap_applied") && Number("cap_extension_percent")==0,
            "off_station_command_cannot_apply");
        Command(true,false,false);Tick(428,.01);Command(false,true,false);Tick(501,.01);
        check(On("fill_complete"),"cap_fixture_has_actual_normalized_fill_complete");
        Command(true,false,false);Tick(345,.01);screenStroke=true;Command(false,false,true);Tick(10);
        check(On("tote_at_cap") && On("cap_busy") && !On("cap_applied") && spindle.GlobalPosition.Y<home.Y,
            "eligible_cap_command_lowers_actual_spindle_and_attached_chuck");
        var lowered=spindle.GlobalPosition;var extension=Number("cap_extension_percent");
        runtime.SetControllerPlaybackRunning(false);Tick(30);
        check(spindle.GlobalPosition.IsEqualApprox(lowered) && Number("cap_extension_percent")==extension && !On("cap_applied"),
            "stop_holds_actual_partial_cap_stroke");
        runtime.SetControllerPlaybackRunning(true);
        var positionBeforeConflict=Number("tote_position");
        Command(true,false,true);Tick(1);
        check(Number("tote_position")==positionBeforeConflict && On("cap_inhibited") && On("conveyor_run") && !On("cap_applied"),
            "conveyor_request_during_extension_holds_tote_and_aborts_cap_without_rewriting_command");
        Command(false,false,false);Tick(30);
        check(On("cap_home") && !On("cap_applied") && !cap.Visible && held.Visible && spindle.GlobalPosition.IsEqualApprox(home),
            "withdrawal_returns_retained_cap_without_application");
        Command(false,false,true);Tick(80);
        check(On("cap_applied") && On("cap_home") && cap.Visible && !held.Visible && spindle.GlobalPosition.IsEqualApprox(home),
            "accepted_cycle_releases_tote_cap_and_retracts_chuck");
        check(!spindle.GlobalBasis.IsEqualApprox(homeBasis),
            "accepted_seated_cycle_projects_declared_rotation_to_actual_spindle");
        var capLocal=cap.Transform;Command(true,false,true);Tick(500);
        check(On("tote_at_exit") && On("cap_applied") && cap.Visible && cap.Transform.IsEqualApprox(capLocal) && !held.Visible,
            "applied_cap_remains_with_tote_at_supported_exit");
        check(strokeClear,"sampled_full_axial_cycle_has_no_screened_cross_solid_tote_capper_interference");
        check(routeClear,"retained_cap_clears_all_station_solids_along_actual_discharge_ticks");
        runtime.ResetSimulation();
        check(!On("cap_applied") && On("cap_home") && !cap.Visible && held.Visible && spindle.GlobalPosition.IsEqualApprox(home),
            "reset_restores_open_tote_and_held_cap_home");
        runtime.SetControllerPlaybackRunning(false);
    }
}
