using System;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditRadarLayout;
    private void AuditRadarLayout()
    {
        var failures=0;
        void Check(bool ok,string name) {if(!ok)failures++;GD.Print($"RADAR_LAYOUT_CHECK {name}={ok}");}
        try {
            VerifyTankPipingGeometry(Check,"tank-radar");
            VerifyRadarMountAndBeam(Check);
            var tank=_sceneCompositionRoot!.GetNode<Node3D>("water_tank_radar");
            var shell=(MeshInstance3D)tank.FindChild("TANK_shell",true,false);
            var roof=(MeshInstance3D)tank.FindChild("TANK_roof",true,false);
            var original=shell.MaterialOverride;var originalRoof=roof.MaterialOverride;
            var transform=shell.GlobalTransform;var distance=_sceneRuntime!.Points["radar_distance"];
            Check(_sceneRuntime.ExecuteAction("toggle-inspection") && shell.MaterialOverride is StandardMaterial3D material
                && material.Transparency==BaseMaterial3D.TransparencyEnum.Alpha && roof.MaterialOverride==shell.MaterialOverride,
                "inspection_view_ghosts_shell_and_roof");
            Check(shell.GlobalTransform==transform && Equals(_sceneRuntime.Points["radar_distance"],distance),"inspection_view_preserves_geometry_and_range");
            var surface=tank.GetNode<MeshInstance3D>("REVIEW_liquid_surface");
            var liquid=(MeshInstance3D)tank.FindChild("KIN_liquid",true,false);
            var surfaceBounds=ReviewBounds(surface);var shellBounds=ReviewBounds(shell);
            Check(surface.Visible && Math.Abs(surfaceBounds.End.Y-ReviewBounds(liquid).End.Y)<.001f
                && surfaceBounds.Position.X>shellBounds.Position.X && surfaceBounds.End.X<shellBounds.End.X
                && surfaceBounds.Position.Z>shellBounds.Position.Z && surfaceBounds.End.Z<shellBounds.End.Z,
                "inspection_surface_matches_sight_glass_datum_and_stays_inside_shell");
            Check(_sceneRuntime.ExecuteAction("toggle-inspection") && shell.MaterialOverride==original && roof.MaterialOverride==originalRoof,
                "inspection_view_restores_authored_opaque_materials");
            Check(!surface.Visible,"inspection_surface_hidden_with_opaque_tank");
            AuditRadarReference(Check);
        } catch(Exception ex) {failures++;GD.PushError(ex.ToString());}
        GD.Print($"RADAR_LAYOUT_VERIFY {(failures==0?"PASS":"FAIL")} geometry and offline PLC reference; native piping, inspection view and full cycle pending");
        GetTree().Quit(failures==0?0:1);
    }
}
