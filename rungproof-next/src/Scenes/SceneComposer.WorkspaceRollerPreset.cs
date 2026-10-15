using System;
using System.Linq;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;
using RungProof.Next.Workspace;
namespace RungProof.Next.Scenes;

public sealed record WorkspaceRollerGeometry(float CrownY,float HomeCenter,float TerminalCenter,
    Vector3 PalletHome,Vector3 RayFrom,Vector3 RayTo,float RollerRadius,float PhaseSign);

public static partial class SceneComposer
{
    private static SceneComposition ComposeWorkspaceRollerPreset(SceneDefinition scene,AssetCatalogDocument assets)
    {
        var root=new Node3D{Name="Scene_workspace_finite_roller"};
        try
        {
            foreach(var e in scene.Equipment)
            {
                var asset=assets.Assets.Single(a=>a.Id==Text(e.Config,"assetId",""));
                var model=ResourceLoader.Load<PackedScene>(asset.Model.DeliveryGltf).Instantiate<Node3D>();
                model.Name=e.Id;model.Position=Vector(e.Position);model.Scale=Vector(e.Scale!);root.AddChild(model);
            }
            var relation=WorkspaceFiniteRollerPolicy.Relation();
            var pallet=root.GetNode<Node3D>(relation.PalletId);
            var sensor=root.GetNode<Node3D>(relation.SensorId);
            var sensorMeshes=Meshes(sensor);
            // Complete named tower translations preserve every foot/post/bracket/pigtail.
            foreach(var prefix in new[]{"TX_","RX_"})
            {
                var group=sensorMeshes.Where(m=>m.Name.ToString().StartsWith(prefix,StringComparison.Ordinal)).ToArray();
                var foot=group.Single(m=>m.Name.ToString()==prefix+"foot");
                var shift=Vector3.Back*(Math.Sign(ContextMeshBounds(root,foot).GetCenter().Z-sensor.Position.Z)*(float)WorkspaceFiniteRollerPolicy.TowerOutwardOffsetM);
                foreach(var mesh in group)
                {
                    if(mesh.GetParent()!=sensor)throw new InvalidOperationException("External tower requires the qualified direct-child GLB layout.");
                    mesh.Position+=shift;
                }
            }
            var topBoards=Meshes(pallet).Where(m=>m.Name.ToString().StartsWith("PALLET_top_board",StringComparison.Ordinal)).ToArray();
            var bandBottom=topBoards.Max(m=>ContextMeshBounds(root,m).Position.Y);
            var bandTop=topBoards.Min(m=>ContextMeshBounds(root,m).End.Y);
            if(bandTop-bandBottom<=.02f)throw new InvalidOperationException("Pallet has no common actual topboard interior.");
            var tx=sensorMeshes.Single(m=>m.Name.ToString()=="TX_lens");
            sensor.Position+=Vector3.Up*((bandBottom+bandTop)/2-ContextMeshBounds(root,tx).GetCenter().Y);
            foreach(var foot in sensorMeshes.Where(m=>m.Name.ToString().EndsWith("_foot",StringComparison.Ordinal)))
            {
                var b=ContextMeshBounds(root,foot);
                if(b.Position.Y<=0)throw new InvalidOperationException("External photoeye requires a grounded foot support.");
                var support=AddBox(root,new Vector3(b.Size.X,b.Position.Y,b.Size.Z),new Vector3(b.GetCenter().X,b.Position.Y/2,b.GetCenter().Z),StudioSceneMaterials.Create("#64716C","paint"));
                support.Name="WorkspaceRollerSensorPedestal_"+foot.Name;
            }
            var geometry=MeasureWorkspaceRoller(root);
            ValidateWorkspaceRollerOpticalInstallation(root,geometry,relation);
            var controller=new RollerConveyorController{Name="WorkspaceRuntimeAdapter",SpeedSetpointMps=.45f,AccelerationMps2=.60f};
            controller.ConfigureAcceptedProjection(geometry.RollerRadius,geometry.PhaseSign);
            root.GetNode<Node3D>(relation.RollerId).AddChild(controller);
            return new(root,scene.Equipment.Select(e=>e.Id).ToArray(),[]);
        }
        catch{root.Free();throw;}
    }
    private static MeshInstance3D[] Meshes(Node3D root)=>root.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
    private static Transform3D WorkspaceRollerMeshTransform(Node3D root,MeshInstance3D mesh)
    {
        var transform=mesh.Transform;
        for(var parent=mesh.GetParent();parent!=root;parent=parent.GetParent())if(parent is Node3D n)transform=n.Transform*transform;
        return transform;
    }
    private static void ValidateWorkspaceRollerOpticalInstallation(Node3D root,WorkspaceRollerGeometry g,WorkspaceFiniteRollerPreset p)
    {
        var pallet=root.GetNode<Node3D>(p.PalletId);var sensor=root.GetNode<Node3D>(p.SensorId);var roller=root.GetNode<Node3D>(p.RollerId);
        var lensEndpoints=Meshes(sensor).Where(m=>m.Name.ToString() is "TX_lens" or "RX_lens").ToArray();
        var palletMeshes=Meshes(pallet);var witnesses=Meshes(sensor).Where(m=>m.Name.ToString().StartsWith("KIN_beam_dash",StringComparison.Ordinal)).ToArray();
        var stationary=Meshes(root).Except(palletMeshes).Except(lensEndpoints).Except(witnesses);
        foreach(var mesh in stationary)
        {
            var inverse=WorkspaceRollerMeshTransform(root,mesh).AffineInverse();
            var from=inverse*g.RayFrom;var to=inverse*g.RayTo;var faces=mesh.Mesh.GetFaces();
            for(var i=0;i+2<faces.Length;i+=3)
                if(Geometry3D.SegmentIntersectsTriangle(from,to,faces[i],faces[i+1],faces[i+2]).VariantType!=Variant.Type.Nil)
                    throw new InvalidOperationException("Finite roller optical path is blocked by stationary solid "+mesh.Name);
        }
        var added=Meshes(sensor).Except(witnesses).Concat(Meshes(root).Where(m=>m.Name.ToString().StartsWith("WorkspaceRollerSensorPedestal",StringComparison.Ordinal)));
        foreach(var item in added)
        foreach(var retained in Meshes(roller))
        {
            var overlap=ContextMeshBounds(root,item).Intersection(ContextMeshBounds(root,retained)).Size;
            if(overlap.X>1e-5&&overlap.Y>1e-5&&overlap.Z>1e-5)
                throw new InvalidOperationException($"Finite roller external mounting overlaps retained hardware: {item.Name}/{retained.Name}");
        }
    }
    public static WorkspaceRollerGeometry MeasureWorkspaceRoller(Node3D root)
    {
        var p=WorkspaceFiniteRollerPolicy.Relation();var roller=root.GetNode<Node3D>(p.RollerId);
        var pallet=root.GetNode<Node3D>(p.PalletId);var sensor=root.GetNode<Node3D>(p.SensorId);
        var rollers=Meshes(roller).Where(m=>m.Name.ToString().StartsWith("KIN_roller_",StringComparison.Ordinal)).OrderBy(m=>ContextMeshBounds(root,m).GetCenter().X).ToArray();
        if(rollers.Length!=31)throw new InvalidOperationException("Finite pallet cell requires 31 delivered rollers.");
        var crown=rollers.Max(m=>ContextMeshBounds(root,m).End.Y);
        var pb=Meshes(pallet).Select(m=>ContextMeshBounds(root,m)).Aggregate((a,b)=>a.Merge(b));
        var localMin=pb.Position.X-pallet.Position.X;var localMax=pb.End.X-pallet.Position.X;
        var home=ContextMeshBounds(root,rollers.Last()).GetCenter().X-.05f-localMax;
        var terminal=ContextMeshBounds(root,rollers.First()).GetCenter().X+.05f-localMin;
        if(Math.Abs(pb.Position.Y-crown)>.001||Math.Abs(pallet.Position.X-home)>.003||home<=terminal)
            throw new InvalidOperationException("Unit pallet must be seated at measured fully supported reset home.");
        var sm=Meshes(sensor);var tx=sm.Single(m=>m.Name.ToString()=="TX_lens");var rx=sm.Single(m=>m.Name.ToString()=="RX_lens");
        var tb=ContextMeshBounds(root,tx);var rb=ContextMeshBounds(root,rx);
        var td=(tb.GetCenter()-ContextMeshBounds(root,sm.Single(m=>m.Name.ToString()=="TX_housing")).GetCenter()).Normalized();
        var rd=(rb.GetCenter()-ContextMeshBounds(root,sm.Single(m=>m.Name.ToString()=="RX_housing")).GetCenter()).Normalized();
        var from=new Vector3(tb.GetCenter().X,tb.GetCenter().Y,td.Z<0?tb.Position.Z:tb.End.Z);
        var to=new Vector3(rb.GetCenter().X,rb.GetCenter().Y,rd.Z<0?rb.Position.Z:rb.End.Z);
        if(td.Dot((to-from).Normalized())<.999||rd.Dot((from-to).Normalized())<.999)throw new InvalidOperationException("External pair is not facing inward.");
        // Root has no yaw. Delivered local roller +Y maps to signed world Z.
        var transform=rollers[0].Transform;
        for(var parent=rollers[0].GetParent();parent!=root;parent=parent.GetParent())if(parent is Node3D n)transform=n.Transform*transform;
        var radius=ContextMeshBounds(root,rollers[0]).Size.X/2;
        var sign=transform.Basis.Y.Normalized().Cross(Vector3.Up*radius).Dot(Vector3.Left)>0?1f:-1f;
        if(Math.Abs(radius-.038f)>.001)throw new InvalidOperationException("Actual roller radius is outside the measured contract.");
        return new(crown,home,terminal,pallet.Position,from,to,radius,sign);
    }
}
