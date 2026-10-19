using System;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>Stable project-tag identity carried from the tag table to LAD operands.</summary>
public static class LadderTagOperandDragPayload
{
    private const string TreePrefix = "tag-id:";
    private const string DragPrefix = "rungproof:tag-operand:";

    public static string? FromTreeMetadata(string rowMetadata, string identityMetadata)
    {
        if (!rowMetadata.StartsWith("tag:", StringComparison.Ordinal)
            || !int.TryParse(rowMetadata.AsSpan("tag:".Length), out var rowIndex)
            || rowIndex < 0
            || !identityMetadata.StartsWith(TreePrefix, StringComparison.Ordinal))
            return null;

        var identity = identityMetadata[TreePrefix.Length..];
        return string.IsNullOrWhiteSpace(identity) ? null : FromIdentity(identity);
    }

    public static string? FromIdentity(string identity) => string.IsNullOrWhiteSpace(identity)
        ? null
        : DragPrefix + Uri.EscapeDataString(identity);

    public static bool TryGetIdentity(string payload, out string identity)
    {
        identity = string.Empty;
        if (!payload.StartsWith(DragPrefix, StringComparison.Ordinal)) return false;
        var encoded = payload[DragPrefix.Length..];
        if (encoded.Length == 0) return false;
        var decoded = Uri.UnescapeDataString(encoded);
        if (string.IsNullOrWhiteSpace(decoded)) return false;
        identity = decoded;
        return true;
    }

    /// <summary>Resolve only scalar declared tags; aggregate members need their own visible row identity.</summary>
    public static bool TryResolveScalarTag(LadderEditorDocument document, string identity,
        out PlcVariable tag, out string operand)
    {
        var matches = document.Tags.Where(candidate =>
            string.Equals(LadderCallBindings.GlobalId(candidate), identity, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1 || PlcAggregates.IsAggregate(matches[0].Type))
        {
            tag = null!;
            operand = string.Empty;
            return false;
        }

        tag = matches[0];
        var owner = document.Blocks[document.ActiveBlockIndex];
        operand = owner.SemanticMode == LadderSemanticMode.Parameterized
            ? "global:" + LadderCallBindings.GlobalId(tag)
            : tag.Name;
        return true;
    }
}
