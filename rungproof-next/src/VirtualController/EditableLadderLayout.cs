using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>Editor zoom is a view transform, independent of Windows DPI or document semantics.</summary>
public readonly record struct LadderEditorZoomGeometry
{
    public float Factor { get; }
    public LadderEditorZoomGeometry(int percent)
    {
        if (percent is not (75 or 100 or 150)) throw new ArgumentOutOfRangeException(nameof(percent), "Supported ladder zoom levels are 75%, 100%, and 150%.");
        Factor = percent / 100.0f;
    }
    public float ToCanvas(float logical) => logical * Factor;
    public float ToLogical(float canvas) => canvas / Factor;
    public static int Reveal(float current, float page, float start, float end, float padding = 18)
    {
        if (end - start > Math.Max(0, page - padding * 2)) return Math.Max(0, (int)Math.Floor(start - padding));
        if (start < current + padding) return Math.Max(0, (int)Math.Floor(start - padding));
        if (end > current + page - padding) return Math.Max(0, (int)Math.Ceiling(end - page + padding));
        return Math.Max(0, (int)current);
    }
}

public sealed record LadderEditorViewState(string NetworkId, string NodeId, bool Output,
    string ParentId, int Slot, int HorizontalScroll, int VerticalScroll, int ZoomPercent);

/// <summary>View navigation is independent of document undo and resolves by persistent identities.</summary>
public sealed class LadderEditorViewMemory
{
    private string _projectId = "";
    private readonly Dictionary<string, LadderEditorViewState> _blocks = new(StringComparer.Ordinal);
    private void UseProject(string id) { if (_projectId == id) return; _projectId = id; _blocks.Clear(); }
    public void Remember(string projectId, string blockId, LadderEditorViewState state)
    { UseProject(projectId); _blocks[blockId] = state; }
    public bool TryRestore(string projectId, string blockId, out LadderEditorViewState? state)
    { UseProject(projectId); return _blocks.TryGetValue(blockId, out state); }
}

public sealed record LadderTreeBounds(float X, float Y, float Width, float Height)
{
    public bool Contains(float x, float y) => x >= X && x <= X + Width && y >= Y && y <= Y + Height;
}
public sealed record LadderTreePlacement(EditableLadderNode Node, string ParentId, int ChildIndex, LadderTreeBounds Bounds, bool IgnoredLegacy = false)
{
    public float ContinuityY => Bounds.Y + Bounds.Height / 2;
}
public enum LadderTreeWireFlowSide { None, Input, Output }
public sealed record LadderTreeWire(float X1, float Y1, float X2, float Y2,
    string FlowNodeId = "", LadderTreeWireFlowSide FlowSide = LadderTreeWireFlowSide.None);
public sealed record LadderTreeSlot(string ParentId, int ChildIndex, float X, float Y);
public sealed record LadderTreeNavigationTarget(string NodeId, string ParentId = "", int Slot = -1, bool IsTerminal = false)
{
    public bool IsInsertion => Slot >= 0;
}

/// <summary>One measured topology drives drawing, hit testing, traversal and selection reveal.</summary>
public sealed class EditableLadderLayout
{
    public IReadOnlyList<LadderTreePlacement> Nodes { get; }
    public IReadOnlyList<LadderTreeWire> Wires { get; }
    public IReadOnlyList<LadderTreeSlot> Slots { get; }
    public float Width { get; }
    public float Height { get; }
    private EditableLadderLayout(List<LadderTreePlacement> nodes, List<LadderTreeWire> wires,
        List<LadderTreeSlot> slots, float width, float height)
    { Nodes = nodes.AsReadOnly(); Wires = wires.AsReadOnly(); Slots = slots.AsReadOnly(); Width = width; Height = height; }

    public static EditableLadderLayout Measure(EditableLadderNode root, float contactWidth = 144, float leafHeight = 84,
        float seriesGap = 48, float branchGap = 36, float junctionInset = 28, bool legacyEmptyBranchSemantics = false)
    {
        EditableLadderTree.Validate(root);
        var sizes = new Dictionary<string, (float Width, float Height)>(StringComparer.Ordinal);
        (float Width, float Height) Size(EditableLadderNode n)
        {
            (float Width, float Height) result;
            if (n.IsLeaf || (n.Children?.Count ?? 0) == 0)
            {
                var edgeMemoryHeight = n.Kind == LadderNodeKind.Contact && n.EdgeMode != LadderEdgeMode.None ? 72 : 0;
                result = (contactWidth, leafHeight + edgeMemoryHeight);
            }
            else
            {
                var children = n.Children!.Select(Size).ToArray();
                result = n.Kind == LadderNodeKind.Series
                    ? (children.Sum(c => c.Width) + seriesGap * (children.Length + 1), children.Max(c => c.Height))
                    : (children.Max(c => c.Width) + 2 * junctionInset,
                        children.Sum(c => c.Height) + branchGap * (children.Length - 1));
            }
            sizes.Add(n.Id, result); return result;
        }
        var size = Size(root);
        var nodes = new List<LadderTreePlacement>(); var wires = new List<LadderTreeWire>(); var slots = new List<LadderTreeSlot>();
        void Place(EditableLadderNode n, string parent, int slot, float x, float y)
        {
            var measured = sizes[n.Id]; var middle = y + measured.Height / 2;
            var ignored = legacyEmptyBranchSemantics && n.Kind == LadderNodeKind.Series && (n.Children?.Count ?? 0) == 0;
            nodes.Add(new(n, parent, slot, new(x, y, measured.Width, measured.Height), ignored));
            var children = n.Children ?? [];
            if (n.IsLeaf) return;
            if (children.Count == 0)
            {
                if (n.Kind == LadderNodeKind.Series && !ignored)
                    wires.Add(new(x, middle, x + measured.Width, middle, n.Id, LadderTreeWireFlowSide.Output));
                slots.Add(new(n.Id, 0, x + measured.Width / 2, middle)); return;
            }
            if (n.Kind == LadderNodeKind.Series)
            {
                var cursor = x;
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i]; var childSize = sizes[child.Id];
                    wires.Add(new(cursor, middle, cursor + seriesGap, middle, child.Id, LadderTreeWireFlowSide.Input));
                    slots.Add(new(n.Id, i, cursor + seriesGap / 2, middle));
                    Place(child, n.Id, i, cursor + seriesGap, middle - childSize.Height / 2);
                    cursor += seriesGap + childSize.Width;
                }
                wires.Add(new(cursor, middle, x + measured.Width, middle, n.Id, LadderTreeWireFlowSide.Output));
                slots.Add(new(n.Id, children.Count, cursor + seriesGap / 2, middle));
            }
            else
            {
                var cursorY = y; float first = 0, last = 0;
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i]; var childSize = sizes[child.Id]; var childMiddle = cursorY + childSize.Height / 2;
                    if (i == 0) first = childMiddle; last = childMiddle;
                    Place(child, n.Id, i, x + junctionInset, cursorY);
                    wires.Add(new(x, childMiddle, x + junctionInset, childMiddle, child.Id, LadderTreeWireFlowSide.Input));
                    if (!child.OpenEnd)
                        wires.Add(new(x + junctionInset + childSize.Width, childMiddle, x + measured.Width, childMiddle, child.Id, LadderTreeWireFlowSide.Output));
                    cursorY += childSize.Height + branchGap;
                }
                wires.Add(new(x, first, x, last, n.Id, LadderTreeWireFlowSide.Input));
                var closedPaths = children.Select((child, index) => (child, index))
                    .Where(item => !item.child.OpenEnd)
                    .Select(item => nodes.First(placed => placed.Node.Id == item.child.Id).ContinuityY)
                    .ToArray();
                if (closedPaths.Length > 1)
                    wires.Add(new(x + measured.Width, closedPaths[0], x + measured.Width, closedPaths[^1], n.Id, LadderTreeWireFlowSide.Output));
                // Parallel slots add complete paths, never branch-local terminal actions.
                for (var i = 0; i <= children.Count; i++)
                    slots.Add(new(n.Id, i, x, i == 0 ? y : i == children.Count ? y + measured.Height : nodes.First(p => p.Node.Id == children[i].Id).Bounds.Y - branchGap / 2));
            }
        }
        Place(root, string.Empty, -1, 0, 0);
        return new(nodes, wires, slots, size.Width, size.Height);
    }
    public LadderTreePlacement? HitNode(float x, float y) => Nodes.LastOrDefault(n => n.Bounds.Contains(x, y));
    public LadderTreeSlot? HitSlot(float x, float y, float radius = 20) =>
        Slots.FirstOrDefault(s => Math.Abs(s.X - x) <= radius && Math.Abs(s.Y - y) <= radius);
    public LadderTreeWire? HitWire(float x, float y, float radius = 6)
    {
        static float DistanceSquared(float px, float py, LadderTreeWire wire)
        {
            var dx = wire.X2 - wire.X1; var dy = wire.Y2 - wire.Y1;
            var lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= float.Epsilon)
            {
                var pointDx = px - wire.X1; var pointDy = py - wire.Y1;
                return pointDx * pointDx + pointDy * pointDy;
            }
            var position = Math.Clamp(((px - wire.X1) * dx + (py - wire.Y1) * dy) / lengthSquared, 0, 1);
            var nearestX = wire.X1 + position * dx; var nearestY = wire.Y1 + position * dy;
            var offsetX = px - nearestX; var offsetY = py - nearestY;
            return offsetX * offsetX + offsetY * offsetY;
        }
        var radiusSquared = radius * radius;
        return Wires.LastOrDefault(wire => wire.FlowNodeId.Length > 0
            && DistanceSquared(x, y, wire) <= radiusSquared);
    }

    public IReadOnlyList<LadderTreeNavigationTarget> Navigation(bool includeTerminal = true)
    {
        var navigation = new List<LadderTreeNavigationTarget>();
        void Walk(EditableLadderNode node)
        {
            navigation.Add(new(node.Id));
            if (node.IsLeaf) return;
            var children = node.Children ?? [];
            for (var i = 0; i <= children.Count; i++)
            {
                navigation.Add(new(node.Id, node.Id, i));
                if (i < children.Count) Walk(children[i]);
            }
        }
        Walk(Nodes[0].Node);
        if (includeTerminal) navigation.Add(new("", IsTerminal: true));
        return navigation.AsReadOnly();
    }
}
