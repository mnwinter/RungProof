using System;

namespace RungProof.Next.VirtualController;

/// <summary>Validated payload shared by the project-tree drag source and LAD canvas.</summary>
public static class ProgramBlockDragPayload
{
    private const string TreePrefix = "program:";
    private const string DragPrefix = "rungproof:fc-fb-call:";

    public static string? FromTreeMetadata(string metadata)
    {
        if (!metadata.StartsWith(TreePrefix, StringComparison.Ordinal)
            || !int.TryParse(metadata.AsSpan(TreePrefix.Length), out var blockIndex)
            || blockIndex < 0)
            return null;
        return DragPrefix + blockIndex;
    }

    /// <summary>
    /// Creates the payload exposed by the visible Siemens project tree.  The
    /// stable row metadata alone identifies only a program index; the rendered
    /// block code is also required so OB and DB rows cannot masquerade as FC/FB
    /// CALL sources while navigating the same tree.
    /// </summary>
    public static string? FromTreeItem(string metadata, string displayText)
    {
        if (!displayText.Contains("[FC", StringComparison.Ordinal)
            && !displayText.Contains("[FB", StringComparison.Ordinal))
            return null;
        return FromTreeMetadata(metadata);
    }

    public static bool TryGetBlockIndex(string payload, out int blockIndex)
    {
        blockIndex = -1;
        return payload.StartsWith(DragPrefix, StringComparison.Ordinal)
            && int.TryParse(payload.AsSpan(DragPrefix.Length), out blockIndex)
            && blockIndex >= 0;
    }
}
