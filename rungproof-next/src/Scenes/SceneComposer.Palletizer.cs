using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    private static Node3D CreatePalletizerPickTable()
    {
        var root = new Node3D();
        var steel = Material(new Color("80939e"), .65f, .3f);
        AddBox(root, new(1.245f, .06f, .6f), new(-1.2775f, 1.025f, -.4f), steel);
        root.GetChild<Node3D>(0).Name = "PICK_surface";
        // The inner legs bear on the gantry base; they do not pass through it.
        foreach (var z in new[] { -.64f, -.16f })
            AddBox(root, new(.06f, .875f, .06f), new(-.8f, .5575f, z), steel);
        return root;
    }
    private static Node3D CreatePalletizerCarton()
    {
        var root = new Node3D();
        AddBox(root, new(.45f, .4f, .35f), new(0, .2f, 0), Material(new Color("cda667"), 0, .8f));
        root.GetChild<Node3D>(0).Name = "CARTON_body";
        // Front tape stays within the nominal top and bearing-plane envelope.
        AddBox(root, new(.04f, .399f, .001f), new(0, .2f, .175f), Material(new Color("ead5a0"), 0, .8f));
        return root;
    }
}
