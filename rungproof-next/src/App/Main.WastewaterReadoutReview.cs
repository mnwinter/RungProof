using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool IsWastewaterReadoutFocus(Node3D root) => _visualSceneReview
        && _currentSceneId == "lab-11-06-wastewater-collection"
        && _visualReviewFocusId == "training_accessory_7"
        && ReferenceEquals(root, _sceneCompositionRoot?.GetNodeOrNull<Node3D>("training_accessory_7"));

    private void AddWastewaterReadoutFrameEnvelope(Node3D root, Vector3 authoredDirection, List<Vector3> points)
    {
        if (!IsWastewaterReadoutFocus(root)
            || root.GetNodeOrNull<Label3D>("WastewaterReadout") is not { } label
            || !label.IsVisibleInTree()) return;
        // This diagnostic focus shows the readout and transmitter head. The
        // long immersed probe remains visible in the separately reviewed full
        // scene; including its entire length makes the text unreadably small.
        // Use the delivered mesh's top band, without relying on imported names.
        var headBandBottom = System.Linq.Enumerable.Max(points, point => point.Y) - .8f;
        points.RemoveAll(point => point.Y < headBandBottom);
        // GenerateTriangleMesh already includes FontSize, line spacing and
        // PixelSize. Billboard text rotates toward this view rather than
        // following the transmitter's installed mesh rotation.
        var direction = authoredDirection.Normalized();
        var right = direction.Cross(Vector3.Up).Normalized();
        if (right.LengthSquared() < .001f) right = Vector3.Right;
        var up = right.Cross(direction).Normalized();
        var faces = label.GenerateTriangleMesh().GetFaces();
        foreach (var vertex in faces)
            points.Add(label.GlobalPosition + right * vertex.X * label.GlobalBasis.X.Length()
                + up * vertex.Y * label.GlobalBasis.Y.Length());
    }

    private static float VisibleReviewContentBottom(Control root)
    {
        var bottom = root.GlobalPosition.Y;
        foreach (var node in root.FindChildren("*", "Control", true, false))
        {
            if (node is not Control control || !control.IsVisibleInTree()
                || control is Container) continue;
            // Container allocation can extend far beyond its rendered children.
            // Labels draw their wrapped text at the top of that allocation;
            // content minimum height describes the actual visible rows.
            bottom = MathF.Max(bottom, control.GlobalPosition.Y + control.GetCombinedMinimumSize().Y);
        }
        return bottom;
    }

    private Rect2 WastewaterReadoutReviewAperture(Node3D root, Rect2 aperture)
    {
        if (!IsWastewaterReadoutFocus(root)) return aperture;
        // The review controls overlay the ordinary scene aperture. Keep this
        // focus inside the unobscured region using their actual wrapped height.
        var chromeBottom = aperture.Position.Y;
        if (_visualReviewBar?.IsVisibleInTree() == true)
            chromeBottom = MathF.Max(chromeBottom, VisibleReviewContentBottom(_visualReviewBar) + 12);
        if (_gantryReviewClockBar?.IsVisibleInTree() == true)
            chromeBottom = MathF.Max(chromeBottom, VisibleReviewContentBottom(_gantryReviewClockBar) + 12);
        var inset = MathF.Max(0, chromeBottom - aperture.Position.Y);
        // FrameComposition centers at the original aperture center. A symmetric
        // inset preserves that center while reserving the upper overlay area.
        var height = MathF.Max(aperture.Size.Y * .25f, aperture.Size.Y - inset * 2);
        return new Rect2(aperture.Position + new Vector2(0, (aperture.Size.Y - height) * .5f),
            new Vector2(aperture.Size.X, height));
    }
}
