using System;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>
/// TIA-style tree drag source for program-block calls and PLC tag operands.
/// Navigation metadata remains unchanged; a tag row carries its stable
/// identity separately in metadata column 1.
/// </summary>
public partial class LadderProgramTree : Tree
{
    internal int ReviewDragDataRequests { get; private set; }

    /// <summary>Build the production drag payload from a visible tree row.</summary>
    public static string? DragPayloadForItem(TreeItem? item)
    {
        if (item is null) return null;
        var rowMetadata = item.GetMetadata(0).AsString();
        return ProgramBlockDragPayload.FromTreeItem(rowMetadata, item.GetText(0))
            ?? LadderTagOperandDragPayload.FromTreeMetadata(
                rowMetadata, item.GetMetadata(1).AsString());
    }

    /// <summary>
    /// Exercise the production payload contract without asking Godot to create
    /// a drag preview. Review gates call this outside an active GUI drag.
    /// </summary>
    internal Variant ReviewDragDataForItem(TreeItem? item)
    {
        ReviewDragDataRequests++;
        var payload = DragPayloadForItem(item);
        return payload is null ? default : Variant.From(payload);
    }

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var item = GetItemAtPosition(atPosition);
        var data = ReviewDragDataForItem(item);
        if (item is null || data.VariantType == Variant.Type.Nil) return default;

        var preview = new Label { Text = item.GetText(0), ThemeTypeVariation = "TooltipLabel" };
        SetDragPreview(preview);
        return data;
    }
}
