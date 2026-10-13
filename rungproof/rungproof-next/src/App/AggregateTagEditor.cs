using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>Structured scalar-leaf editor; no schema JSON or PLC packing knowledge required.</summary>
public partial class AggregateTagEditor : VBoxContainer
{
    private static readonly PlcVariableType[] Scalars = [PlcVariableType.Bool, PlcVariableType.Int, PlcVariableType.DInt, PlcVariableType.Real];
    private sealed record FieldRow(HFlowContainer Container, LineEdit Name, OptionButton Type, LineEdit Initial);
    private readonly List<FieldRow> _fields = [];
    private readonly VBoxContainer _rows = new();
    private readonly HFlowContainer _arrayControls = new();
    private readonly OptionButton _element = ScalarSelector();
    private readonly SpinBox _lower = new() { MinValue = int.MinValue, MaxValue = int.MaxValue, Step = 1, CustomMinimumSize = new Vector2(105, 30) };
    private readonly SpinBox _length = new() { MinValue = 1, MaxValue = 4096, Step = 1, Value = 10, CustomMinimumSize = new Vector2(75, 30) };
    private readonly Button _add = new() { Text = "+ STRUCT FIELD" };
    private PlcVariableType _kind;
    private bool _loading;
    public event Action? PendingChanged;
    private AggregateArrayDraft? _arrayDraft;

    public AggregateTagEditor()
    {
        Name = "AggregateTagEditor";
        _element.Name = "ArrayElementType"; _lower.Name = "ArrayLowerBound"; _length.Name = "ArrayLength"; _add.Name = "AddAggregateField";
        AddChild(Caption("AGGREGATE SCHEMA / TYPED INITIAL VALUES"));
        void ArrayControl(string label, Control control)
        {
            var pair = new HBoxContainer(); pair.AddChild(Caption(label)); pair.AddChild(control); _arrayControls.AddChild(pair);
        }
        ArrayControl("TYPE", _element); ArrayControl("FROM", _lower); ArrayControl("COUNT", _length); AddChild(_arrayControls);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 140), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _rows.SizeFlagsHorizontal = SizeFlags.ExpandFill; scroll.AddChild(_rows); AddChild(scroll); AddChild(_add);
        _add.Pressed += () => { AddField("field_" + (_fields.Count + 1), PlcVariableType.Bool, "FALSE", true); PendingChanged?.Invoke(); };
        _length.ValueChanged += _ => { if (!_loading && _kind == PlcVariableType.Array) RebuildArray(); };
        _lower.ValueChanged += _ => { if (!_loading && _kind == PlcVariableType.Array) RebuildArray(); };
        _element.ItemSelected += _ => { if (!_loading && _kind == PlcVariableType.Array) RebuildArray(); };
        Visible = false;
    }

    private static Label Caption(string text)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeColorOverride("font_color", new Color("344851"));
        label.AddThemeFontSizeOverride("font_size", 11);
        return label;
    }

    private static OptionButton ScalarSelector()
    {
        var selector = new OptionButton { CustomMinimumSize = new Vector2(72, 30) };
        foreach (var type in Scalars) selector.AddItem(type.ToString().ToUpperInvariant());
        return selector;
    }
    private void ClearRows()
    {
        foreach (var row in _fields) { _rows.RemoveChild(row.Container); row.Container.QueueFree(); }
        _fields.Clear();
    }
    private void AddField(string name, PlcVariableType type, string value, bool editable)
    {
        var row = new HFlowContainer { Name = "AggregateFieldRow" + _fields.Count };
        var fieldName = new LineEdit { Name = "FieldName", Text = name, Editable = editable, CustomMinimumSize = new Vector2(100, 30), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var selector = ScalarSelector(); selector.Name = "FieldType"; selector.Select(Math.Max(0, Array.IndexOf(Scalars, type))); selector.Disabled = !editable;
        var initial = new LineEdit { Name = "FieldInitial", Text = value, PlaceholderText = "Typed initial", CustomMinimumSize = new Vector2(95, 30), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(fieldName); row.AddChild(selector); row.AddChild(initial);
        fieldName.TextChanged += _ => { if (!_loading) PendingChanged?.Invoke(); };
        initial.TextChanged += _ => { if (!_loading) PendingChanged?.Invoke(); };
        selector.ItemSelected += _ => { if (!_loading) PendingChanged?.Invoke(); };
        var field = new FieldRow(row, fieldName, selector, initial); _fields.Add(field); _rows.AddChild(row);
        if (editable)
        {
            var remove = new Button { Text = "−", TooltipText = "Remove field; Verify reports affected operands" };
            row.AddChild(remove); remove.Pressed += () => { _fields.Remove(field); _rows.RemoveChild(row); row.QueueFree(); PendingChanged?.Invoke(); };
        }
    }
    private void RebuildArray()
    {
        if (_arrayDraft is null) return;
        PendingChanged?.Invoke();
        for (var i = 0; i < _fields.Count; i++) _arrayDraft.Values[i] = _fields[i].Initial.Text;
        _arrayDraft.TryChangeType(Scalars[_element.Selected], true);
        try { _arrayDraft.Resize((int)_lower.Value, (int)_length.Value); RenderArray(); }
        catch (ArgumentException error) { _lower.TooltipText = error.Message; _length.TooltipText = error.Message; }
    }
    private void RenderArray()
    {
        if (_arrayDraft is null) return;
        ClearRows();
        for (var i = 0; i < _arrayDraft.Values.Length; i++)
            AddField($"[{(long)_arrayDraft.LowerBound + i}]", _arrayDraft.Type, _arrayDraft.Values[i], false);
    }
    public void Load(PlcVariableType type, PlcAggregateSchema? schema = null, object? initial = null)
    {
        _loading = true; _kind = type; Visible = PlcAggregates.IsAggregate(type);
        _arrayControls.Visible = type == PlcVariableType.Array; _add.Visible = type == PlcVariableType.Struct; ClearRows();
        if (!Visible) { _loading = false; return; }
        var json = initial is JsonElement value ? value : initial is null ? default : JsonSerializer.SerializeToElement(initial);
        if (type == PlcVariableType.Struct)
        {
            foreach (var field in schema?.Fields ?? [new("field_1", PlcVariableType.Bool)])
                AddField(field.Name, field.Type, json.ValueKind == JsonValueKind.Object && json.TryGetProperty(field.Name, out var item) ? Format(item) : field.Type == PlcVariableType.Bool ? "FALSE" : "0", true);
        }
        else
        {
            _element.Select(Math.Max(0, Array.IndexOf(Scalars, schema?.ElementType ?? PlcVariableType.Bool)));
            _lower.Value = schema?.LowerBound ?? 0; _length.Value = schema?.Length ?? 10;
            for (var i = 0; i < (int)_length.Value; i++) AddField($"[{(long)_lower.Value + i}]", Scalars[_element.Selected], json.ValueKind == JsonValueKind.Array && i < json.GetArrayLength() ? Format(json[i]) : Scalars[_element.Selected] == PlcVariableType.Bool ? "FALSE" : "0", false);
        }
        _arrayDraft = type == PlcVariableType.Array
            ? new AggregateArrayDraft(Scalars[_element.Selected], (int)_lower.Value, _fields.Select(f => f.Initial.Text).ToArray()) : null;
        _loading = false;
    }
    private static string Format(JsonElement value) => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() ? "TRUE" : "FALSE" : value.GetRawText();
    public bool RevealMember(string symbol, string root)
    {
        var member = symbol.StartsWith(root + ".", StringComparison.Ordinal) ? symbol[(root.Length + 1)..]
            : symbol.StartsWith(root + "[", StringComparison.Ordinal) ? symbol[root.Length..] : string.Empty;
        var row = _fields.Find(field => field.Name.Text == member);
        if (row is null) return false;
        if (_rows.GetParent() is ScrollContainer scroll) scroll.EnsureControlVisible(row.Container);
        row.Initial.GrabFocus();
        row.Initial.SelectAll();
        return true;
    }
    public bool TryRead(PlcVariableType type, out PlcAggregateSchema schema, out object initial, out string error)
    {
        schema = new(); initial = false; error = "";
        if (type == PlcVariableType.Array && (_arrayDraft is null || Scalars[_element.Selected] != _arrayDraft.Type
            || (int)_lower.Value != _arrayDraft.LowerBound || (int)_length.Value != _arrayDraft.Values.Length))
        { error = "Resolve the pending type change or correct the array range before applying."; return false; }
        var values = new List<object>();
        var fields = new List<PlcAggregateField>();
        foreach (var field in _fields)
        {
            var scalar = Scalars[field.Type.Selected];
            if (!LadderEditorDocument.TryParseInitialValue(scalar, field.Initial.Text, out var parsed, out error)) { error = field.Name.Text + ": " + error; return false; }
            fields.Add(new(field.Name.Text.Trim(), scalar)); values.Add(parsed);
        }
        try
        {
            if (type == PlcVariableType.Struct)
            {
                schema = new(fields); var record = new Dictionary<string, object>(StringComparer.Ordinal);
                for (var i = 0; i < fields.Count; i++) if (!record.TryAdd(fields[i].Name, values[i])) throw new ArgumentException("Duplicate STRUCT field name.");
                initial = record;
            }
            else { schema = new(ElementType: Scalars[_element.Selected], LowerBound: (int)_lower.Value, Length: (int)_length.Value); initial = values.ToArray(); }
            PlcAggregates.Expand([new("preview", type, PlcVariableRole.Memory, initial, Aggregate: schema)]); return true;
        }
        catch (ArgumentException exception) { error = exception.Message; return false; }
    }
}
