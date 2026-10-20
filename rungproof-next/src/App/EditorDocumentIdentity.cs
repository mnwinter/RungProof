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
    private const string InstanceDbPrefix = "instance-db:";
    private const string WatchTablePrefix = "watch-table:";

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

    public static string ForInstanceDb(string ownerIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerIdentity);
        return InstanceDbPrefix + ownerIdentity;
    }

    public static string ForWatchTable(EditableWatchTable watchTable)
    {
        ArgumentNullException.ThrowIfNull(watchTable);
        return ForWatchTable(watchTable.Id);
    }

    public static string ForWatchTable(string watchTableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(watchTableId);
        return WatchTablePrefix + watchTableId;
    }

    public static bool TryResolveWatchTableIndex(LadderEditorDocument document, string key, out int watchTableIndex)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(key);
        watchTableIndex = -1;
        if (!key.StartsWith(WatchTablePrefix, StringComparison.Ordinal)) return false;

        var watchTableId = key[WatchTablePrefix.Length..];
        if (watchTableId.Length == 0) return false;
        watchTableIndex = document.WatchTables.FindIndex(table => table.Id.Equals(watchTableId, StringComparison.Ordinal));
        return watchTableIndex >= 0;
    }
}
