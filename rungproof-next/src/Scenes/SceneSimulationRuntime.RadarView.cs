using System.Collections.Generic;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private readonly Dictionary<MeshInstance3D,Material?> _radarOpaqueMaterials=[];
    private StandardMaterial3D? _radarInspectionMaterial;
    private void ProjectRadarInspectionView(Node3D? tank)
    {
        if(tank is null || !_points.ContainsKey("tank_inspection_view"))return;
        // Explicit presentation-only inspection: retain the real shell/roof
        // meshes and their transforms, but reveal internal beam/surface geometry.
        // This SIM point is never a PLC command or simulated physical opening.
        _radarInspectionMaterial ??= new StandardMaterial3D {
            AlbedoColor=new Color(.55f,.68f,.75f,.12f),
            Transparency=BaseMaterial3D.TransparencyEnum.Alpha,
            CullMode=BaseMaterial3D.CullModeEnum.Disabled,
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        foreach(var name in new[]{"TANK_shell","TANK_roof"}) {
            if(tank.FindChild(name,true,false) is not MeshInstance3D mesh)continue;
            if(!_radarOpaqueMaterials.ContainsKey(mesh))_radarOpaqueMaterials[mesh]=mesh.MaterialOverride;
            mesh.MaterialOverride=AsBool(_points["tank_inspection_view"]) ? _radarInspectionMaterial : _radarOpaqueMaterials[mesh];
        }
        // The delivered KIN_liquid is the external sight-glass column. Its
        // top supplies the range datum, but ghosting the shell previously
        // revealed only the fixed tank bottom. Show that same datum inside
        // the tank explicitly; this disk is presentation, not process physics.
        if(tank.FindChild("TANK_shell",true,false) is not MeshInstance3D shell
            || tank.FindChild("KIN_liquid",true,false) is not MeshInstance3D liquid)return;
        var surface=tank.GetNodeOrNull<MeshInstance3D>("REVIEW_liquid_surface");
        if(surface is null) {
            surface=new MeshInstance3D {
                Name="REVIEW_liquid_surface",
                Mesh=new CylinderMesh { Height=.01f,RadialSegments=64 },
                MaterialOverride=new StandardMaterial3D {
                    AlbedoColor=new Color(.025f,.48f,.65f),
                    ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,
                    CullMode=BaseMaterial3D.CullModeEnum.Disabled,
                },
                CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,
            };
            tank.AddChild(surface);
        }
        var bounds=shell.GetAabb();
        var low=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
        var high=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
        for(var corner=0;corner<8;corner++) {
            var point=shell.GlobalTransform*(bounds.Position+bounds.Size*new Vector3(
                (corner&1)==0?0:1,(corner&2)==0?0:1,(corner&4)==0?0:1));
            low=low.Min(point);high=high.Max(point);
        }
        var radius=Mathf.Min(high.X-low.X,high.Z-low.Z)*.47f;
        var disk=(CylinderMesh)surface.Mesh;
        disk.TopRadius=radius;disk.BottomRadius=radius;
        surface.GlobalTransform=new Transform3D(Basis.Identity,new Vector3(
            (low.X+high.X)*.5f,WorldVerticalRange(liquid).Top-.005f,(low.Z+high.Z)*.5f));
        surface.Visible=AsBool(_points["tank_inspection_view"]);
    }
}
