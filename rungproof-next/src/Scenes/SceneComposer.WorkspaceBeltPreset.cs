using System;
using System.Linq;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;
using RungProof.Next.Workspace;

namespace RungProof.Next.Scenes;
public static partial class SceneComposer
{
    private static SceneComposition ComposeWorkspaceBeltPreset(SceneDefinition scene,AssetCatalogDocument assets)
    {
        var root=new Node3D{Name="Scene_workspace_finite_belt"};
        try
        {
            foreach(var e in scene.Equipment)
            {
                var asset=assets.Assets.Single(a=>a.Id==Text(e.Config,"assetId",""));
                var model=ResourceLoader.Load<PackedScene>(asset.Model.DeliveryGltf).Instantiate<Node3D>();
                model.Name=e.Id;model.Position=Vector(e.Position);model.Scale=Vector(e.Scale!);root.AddChild(model);
                if(asset.Id==WorkspaceFiniteBeltPolicy.BeltAsset)
                {
                    var controller=new ConveyorController{Name="WorkspaceRuntimeAdapter",EstopOk=true,SpeedSetpointMps=.45f};
                    model.AddChild(controller);controller.SetPhysicsProcess(false);
                }
                if(asset.Id==WorkspaceFiniteBeltPolicy.SensorAsset)
                {
                    // The retained emitter/receiver feet bear on this grounded
                    // pedestal; do not suspend the original sensor assembly.
                    var steel=StudioSceneMaterials.Create("#64716C","paint");
                    var feet=model.FindChildren("*_foot","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
                    if(feet.Length==0)throw new InvalidOperationException("Throughbeam has no measured supporting bases.");
                    foreach(var foot in feet)
                    {
                        var bounds=ContextMeshBounds(root,foot);
                        if(bounds.Position.Y<=0)throw new InvalidOperationException("Sensor pedestal cannot bear its retained foot.");
                        var support=AddBox(root,new Vector3(bounds.Size.X+.04f,bounds.Position.Y,bounds.Size.Z+.04f),
                            new Vector3(bounds.GetCenter().X,bounds.Position.Y/2,bounds.GetCenter().Z),steel);
                        support.Name="WorkspaceSensorPedestal";
                    }
                }
            }
            MeasureWorkspaceBelt(root);
            return new(root,scene.Equipment.Select(e=>e.Id).ToArray(),[]);
        }
        catch{root.Free();throw;}
    }
    public static (Aabb Belt,Aabb Carton,Aabb Beam) MeasureWorkspaceBelt(Node3D root)
    {
        var p=WorkspaceFiniteBeltPolicy.Relation();
        var belt=root.GetNode<Node3D>(p.BeltId);var carton=root.GetNode<Node3D>(p.CartonId);var sensor=root.GetNode<Node3D>(p.SensorId);
        var surface=belt.FindChildren("KIN_belt_surface*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
        var beams=sensor.FindChildren("KIN_beam*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray();
        if(surface.Length==0||beams.Length==0)throw new InvalidOperationException("Finite workspace lacks an actual belt or sensing line.");
        Aabb Union(MeshInstance3D[] meshes)=>meshes.Select(m=>ContextMeshBounds(root,m)).Aggregate((a,b)=>a.Merge(b));
        var b=Union(surface);var c=Union(carton.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().ToArray());var beam=Union(beams);
        if(MathF.Abs(c.Position.Y-b.End.Y)>.015f||c.Position.X<b.Position.X-.002f||c.End.X>b.End.X+.002f
            ||c.Position.Z<b.Position.Z-.002f||c.End.Z>b.End.Z+.002f
            ||beam.GetCenter().Y<c.Position.Y+.01f||beam.GetCenter().Y>c.End.Y-.01f
            ||beam.Position.Z>c.Position.Z||beam.End.Z<c.End.Z
            ||beam.GetCenter().X<c.End.X+.009f||beam.GetCenter().X>b.End.X-.05f-.009f)
            throw new InvalidOperationException("Measured carton support/beam interval does not match the bounded finite-belt preset.");
        return(b,c,beam);
    }
}
