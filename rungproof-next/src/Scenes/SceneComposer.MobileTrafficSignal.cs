using Godot;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Scene-local portable installation: retain the authored head/lens names
    // and bindings. This depicts a cart, not an engineered stability envelope.
    private static Node3D CreateMobileTrafficSignalBase(Node3D signal)
    {
        var root = new Node3D();
        var steel = Material(new Color("667a85"), .5f, .45f);
        var dark = Material(new Color("26343d"), .2f, .65f);
        var rubber = Material(new Color("171c20"), 0, .85f);
        var deck = AddBox(root, new Vector3(1.2f,.18f,.8f), new Vector3(0,.30f,0), steel);
        deck.Name = "MOBILE_SIGNAL_cart_deck";
        var enclosure = AddBox(root, new Vector3(.8f,.36f,.22f), new Vector3(0,.57f,.28f), dark);
        enclosure.Name = "MOBILE_SIGNAL_base_enclosure";
        // Illustrative paired antennas depict the declared common-command link.
        // Their presence does not create measured radio/link-health feedback.
        AddCylinder(root, "MOBILE_SIGNAL_illustrative_antenna", new Vector3(.30f,1f,.30f), .02f,.5f,dark);
        foreach (var x in new[] { -.45f,.45f })
        foreach (var z in new[] { -.32f,.32f })
            AddCylinder(root, $"MOBILE_SIGNAL_wheel_{x}_{z}", new Vector3(x,.15f,z),
                .15f,.10f,rubber,new Vector3(0,0,90));
        // Cart deck top meets the original mast base; wheels meet the scene's
        // authored sidewalk elevation via the equipment transform.
        signal.Position = new Vector3(0,.39f,0);
        root.AddChild(signal);
        return root;
    }
}
