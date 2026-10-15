using System;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>
/// TIA-style project-tree source for dragging an FC/FB program block onto a
/// network's terminal instruction position. Tree rows keep their existing
/// navigation metadata; the drag payload is a stable, validated block index
/// for the duration of the drag.
/// </summary>
public partial class LadderProgramTree : Tree
{
    internal int ReviewDragDataRequests { get; private set; }
    public override Variant _GetDragData(Vector2 atPosition)
    {
        ReviewDragDataRequests++;
        var item = GetItemAtPosition(atPosition);
        if (item is null) return default;

        var payload = ProgramBlockDragPayload.FromTreeItem(
            item.GetMetadata(0).AsString(), item.GetText(0));
        if (payload is null) return default;

        var preview = new Label { Text = item.GetText(0), ThemeTypeVariation = "TooltipLabel" };
        SetDragPreview(preview);
        return Variant.From(payload);
    }
}
