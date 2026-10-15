using System;
using System.Linq;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D AddDrawbridgePivot(Node3D model, string part)
    {
        var (prefix, position) = part switch
        {
            "drawbridgeDeck" => ("KIN_deck_", new Vector3(-2, 1.4f, 0)),
            "drawbridgeGate" => ("KIN_gate_", new Vector3(0, 1.2f, 0)),
            "drawbridgeLimits" => ("KIN_cam_", new Vector3(0, 1.4f, 0)),
            _ => throw new InvalidOperationException("Unknown drawbridge installation part: " + part),
        };
        var pivot = new Node3D { Name = "BridgeMotionPivot", Position = position };
        model.AddChild(pivot);
        pivot.Owner = model;
        var moving = model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
            .Where(mesh => mesh.Name.ToString().StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (moving.Length == 0) throw new InvalidOperationException("Missing drawbridge moving geometry: " + prefix);
        foreach (var mesh in moving)
        {
            // Composition has not entered the tree yet: preserve the mesh pose
            // explicitly in model-local coordinates, avoiding global transforms.
            var transform = mesh.Transform;
            for (var parent = mesh.GetParent(); parent != model; parent = parent.GetParent())
            {
                if (parent is not Node3D ancestor) throw new InvalidOperationException("Drawbridge mesh has a non-spatial ancestor.");
                transform = ancestor.Transform * transform;
            }
            mesh.Owner = null;
            mesh.GetParent().RemoveChild(mesh); pivot.AddChild(mesh);
            mesh.Owner = model;
            mesh.Transform = pivot.Transform.AffineInverse() * transform;
        }
        return model;
    }

    private static Node3D CreateDrawbridgeSignal(Node3D model)
    {
        // There is no amber command in this exercise. Initialize every lens
        // dark; the bound red and green channels subsequently follow PLC bits.
        foreach (var mesh in model.FindChildren("LENS_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            var channel = mesh.Name.ToString().Split('_')[1];
            mesh.MaterialOverride = Material(NamedSignalColor(channel).Darkened(.72f), .03f, .18f);
        }
        return model;
    }
}
