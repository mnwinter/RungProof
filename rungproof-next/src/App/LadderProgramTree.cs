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

    public override Variant _GetDragData(Vector2 atPosition)
    {
        ReviewDragDataRequests++;
        var item = GetItemAtPosition(atPosition);
        if (item is null) return default;

        var payload = DragPayloadForItem(item);
        if (payload is null) return default;

        var preview = new Label { Text = item.GetText(0), ThemeTypeVariation = "TooltipLabel" };
        SetDragPreview(preview);
        return Variant.From(payload);
    }
}
