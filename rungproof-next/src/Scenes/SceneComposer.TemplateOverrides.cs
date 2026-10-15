using System;
using Godot;
using RungProof.Next.Workspace;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    public static void ApplyTemplateOverride(Node3D root,SceneDefinition scene,WorkspaceTemplateOverride? value)
    {
        if(!WorkspaceTemplatePolicy.Supports(scene.Id))
        {
            if(value is not null)throw new InvalidOperationException("Template scene is locked.");
            return;
        }
        if(value is not null)WorkspaceTemplatePolicy.Validate(scene,value);
        var support=root.GetNode<Node3D>("ControlInstallation");
        if(!support.HasMeta("templateOriginalTransform"))support.SetMeta("templateOriginalTransform",support.Transform);
        var original=support.GetMeta("templateOriginalTransform").AsTransform3D();
        var target=value is null?original:new Transform3D(new Basis(Vector3.Up,Mathf.DegToRad((float)value.YawDegrees)),
            new Vector3((float)value.Position[0],0,(float)value.Position[2]));
        var delta=target*original.AffineInverse();
        // Heads are authored siblings, not children of the generated support.
        // Transform the declared inventory together; IDs/points/controllers stay.
        foreach(var idValue in support.GetMeta("installationEquipmentIds").AsGodotArray())
        {
            var id=idValue.AsString();
            var head=root.GetNode<Node3D>(SafeNodeName(id));
            if(!head.HasMeta("templateOriginalTransform"))head.SetMeta("templateOriginalTransform",head.Transform);
            head.Transform=delta*head.GetMeta("templateOriginalTransform").AsTransform3D();
        }
        support.Transform=target;
        if(support.HasMeta("operatorAccessBounds"))
        {
            if(!support.HasMeta("templateOriginalAccessBounds"))
                support.SetMeta("templateOriginalAccessBounds",support.GetMeta("operatorAccessBounds"));
            support.SetMeta("operatorAccessBounds",delta*support.GetMeta("templateOriginalAccessBounds").AsAabb());
        }
    }
}
