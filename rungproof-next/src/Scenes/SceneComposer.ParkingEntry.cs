using System;
using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreateParkingEntryPart(string installation)
    {
        Node3D Load(string part) => ResourceLoader.Load<PackedScene>($"res://assets/scene_installations/parking_entry/{part}/delivery/parking_{part}.glb")?.Instantiate<Node3D>()
            ?? throw new InvalidOperationException($"Missing parking-entry installation model: {part}.");
        if (installation == "parkingVehicle") return Load("vehicle");
        if (installation == "parkingPad") return Load("pad");
        var root = Load("cabinet");
        var pivot = new Node3D { Name = "BoomPivot", Position = new Vector3(0, 1.08f, 0) };
        root.AddChild(pivot); pivot.AddChild(Load("boom"));
        return root;
    }
}
