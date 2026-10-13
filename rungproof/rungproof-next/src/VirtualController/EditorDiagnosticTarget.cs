using System;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>Capture compiler provenance once; later navigation never clamps a path onto another instruction.</summary>
public sealed record EditorDiagnosticTarget(EditorTarget Target, string OperandRole)
{
    public static EditorDiagnosticTarget? Capture(LadderEditorDocument document, string path)
    {
        static int Index(string source, string collection)
        {
            var marker = "." + collection + "[";
            var start = source.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return -1;
            start += marker.Length;
            var end = source.IndexOf(']', start);
            return end > start && int.TryParse(source.AsSpan(start, end - start), out var index) ? index : -1;
        }
        var blockIndex = Index(path, "blocks");
        var networkIndex = Index(path, "networks");
        if (blockIndex < 0 || blockIndex >= document.Blocks.Count) return null;
        var block = document.Blocks[blockIndex];
        if (networkIndex < 0 || networkIndex >= block.Rungs.Count) return null;
        var network = block.Rungs[networkIndex];
        var operand = path[(path.LastIndexOf('.') + 1)..];
        if (!path.Contains(".logic", StringComparison.Ordinal))
            return new(new(document.Revision, block.Id, network.Id, network.TerminalActionId,
                string.Empty, EditorOperandRole.Output), operand);
        var node = network.Root;
        var cursor = path.IndexOf(".logic", StringComparison.Ordinal) + ".logic".Length;
        while (path.AsSpan(cursor).StartsWith(".children[", StringComparison.Ordinal))
        {
            cursor += ".children[".Length;
            var end = path.IndexOf(']', cursor);
            if (end < 0 || !int.TryParse(path.AsSpan(cursor, end - cursor), out var child)
                || node.Children is null || child < 0 || child >= node.Children.Count) return null;
            node = node.Children[child]; cursor = end + 1;
        }
        // Branch adapters only locate the exact leaf's parent; IDs come from
        // the canonical tree and no Boolean path expansion is performed.
        var branch = network.Branches.FirstOrDefault(item => item.Contacts.Any(contact => contact.Id == node.Id));
        var role = node.IsLeaf ? EditorOperandRole.Contact : EditorOperandRole.Network;
        return new(new(document.Revision, block.Id, network.Id, node.Id,
            branch?.Id ?? string.Empty, role), operand);
    }
}
