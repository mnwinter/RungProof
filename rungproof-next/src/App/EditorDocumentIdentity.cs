using System;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>
/// Stable identities for documents shown in the editor tab strip. Program
/// blocks use their persistent model ID so list insertions and deletions cannot
/// make an existing tab point at a different block.
/// </summary>
public static class EditorDocumentIdentity
{
    private const string BlockPrefix = "block:";

    public static string ForBlock(EditableBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return ForBlock(block.Id);
    }

    public static string ForBlock(string blockId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockId);
        return BlockPrefix + blockId;
    }

    public static bool TryResolveBlockIndex(LadderEditorDocument document, string key, out int blockIndex)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(key);
        blockIndex = -1;
        if (!key.StartsWith(BlockPrefix, StringComparison.Ordinal)) return false;

        var blockId = key[BlockPrefix.Length..];
        if (blockId.Length == 0) return false;
        blockIndex = document.Blocks.FindIndex(block => block.Id.Equals(blockId, StringComparison.Ordinal));
        return blockIndex >= 0;
    }
}
