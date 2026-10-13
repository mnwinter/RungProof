using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private OptionButton _deviceTraceSelector = null!;
    private ItemList _deviceTraceReferences = null!;
    private readonly List<LadderProjectReference> _deviceTraceRows = [];
    private Action<LadderProjectReference>? _navigateDeviceReference;
    private string _deviceTraceScene = "";
    private string _deviceNavigatedBlock = "";
    private int _deviceNavigatedNetwork = -1;

    private Control BuildDeviceSignalsInspector()
    {
        var box = new VBoxContainer { Name = "Signals" };
        _deviceTraceSelector = new OptionButton { Name = "DeviceSelector" };
        _deviceTraceSelector.ItemSelected += _ => RefreshIoInspector();
        box.AddChild(_deviceTraceSelector);
        box.AddChild(_ioInspector);
        _ioInspector.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _deviceTraceReferences = new ItemList { Name = "DeviceRungReferences", CustomMinimumSize = new Vector2(0,150) };
        _deviceTraceReferences.ItemActivated += i => NavigateDeviceReference((int)i);
        box.AddChild(_deviceTraceReferences);
        return box;
    }

    private void NavigateDeviceReference(int row)
    {
        if (!_ladderMonitorMatchesLoadedProgram || row < 0 || row >= _deviceTraceRows.Count) return;
        _navigateDeviceReference?.Invoke(_deviceTraceRows[row]);
    }

    private void AppendDeviceTrace(StringBuilder text)
    {
        if (_activeScene is null || _deviceTraceSelector is null) return;
        if (_deviceTraceScene != _activeScene.Id)
        {
            _deviceTraceScene = _activeScene.Id;
            _deviceTraceSelector.Clear();
            foreach(var equipment in _activeScene.Equipment)
            {
                _deviceTraceSelector.AddItem(equipment.Id);
                _deviceTraceSelector.SetItemMetadata(_deviceTraceSelector.ItemCount-1,equipment.Id);
            }
        }
        _deviceTraceRows.Clear(); _deviceTraceReferences.Clear();
        if (_deviceTraceSelector.Selected < 0) return;
        var id = _deviceTraceSelector.GetItemMetadata(_deviceTraceSelector.Selected).AsString();
        text.AppendLine($"\n[b]DEVICE TRACE · {Escape(id)}[/b]");
        var points = new HashSet<string>(StringComparer.Ordinal);
        var simulation = _activeScene.Simulation;
        if (simulation.ValueKind == JsonValueKind.Object && simulation.TryGetProperty("pointBindings",out var bindings))
            foreach(var binding in bindings.EnumerateArray())
                if(Text(binding,"equipmentId")==id) points.Add(Text(binding,"point"));
        if(points.Count==0) text.AppendLine("No declared device → point mapping. Geometry is not used to infer one.");
        if(simulation.ValueKind==JsonValueKind.Object && simulation.TryGetProperty("actions",out var actions))
            foreach(var action in actions.EnumerateArray())
                if(points.Contains(Text(action,"point"))) text.AppendLine($"Declared action: {Escape(Text(action,"id"))}");
        var current = _ladderMonitorMatchesLoadedProgram && _virtualProgram is not null && _virtualSnapshot is not null;
        text.AppendLine(current
            ? $"Accepted scan {_virtualSnapshot!.ScanNumber} · {(_virtualSnapshot.State==VirtualControllerState.Stopped ? "STOPPED · last accepted scan" : "RUNNING")}" 
            : "No matching loaded draft snapshot · navigation and evaluation unavailable.");
        foreach(var point in points.OrderBy(p=>p,StringComparer.Ordinal))
        {
            text.AppendLine($"Point: {Escape(point)}");
            if(!current) continue;
            foreach(var variable in PlcAggregates.Expand(_virtualProgram!.Variables).Where(v=>v.Binding==point))
            {
                var snapshot=_virtualSnapshot!;
                var value=snapshot.Variables.TryGetValue(variable.Name,out var bit) ? bit.ToString()
                    : snapshot.NumericVariables.TryGetValue(variable.Name,out var number) ? number.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : snapshot.TextVariables.TryGetValue(variable.Name,out var literal) ? literal : "not present in accepted snapshot";
                text.AppendLine($"  {Escape(variable.Name)} · {variable.Role} · {Escape(value)}");
                foreach(var reference in LadderProjectIndex.Build(_virtualProgram).CrossReference(variable.Name)
                    .Where(r=>r.Kind is LadderReferenceKind.Read or LadderReferenceKind.Write))
                {
                    _deviceTraceRows.Add(reference);
                    var evaluation=snapshot.Elements.TryGetValue(reference.ElementId,out var state)
                        ? (state.Energized ? "energized" : "not energized") : "not evaluated";
                    _deviceTraceReferences.AddItem($"{reference.Kind} {reference.Symbol} · {reference.BlockName}/{reference.NetworkIndex+1} · {evaluation}");
                }
            }
        }
        text.AppendLine("References show readers/writers, not inferred causes. Double-click to open the actual rung.");
    }
    public bool VerifyDeviceIndexedBinding(LadderProgram program, VirtualControllerSnapshot snapshot,string equipmentId,string leaf)
    {
        var variable=PlcAggregates.Expand(program.Variables).FirstOrDefault(v=>v.Name==leaf);
        return variable is not null&&snapshot.ScanNumber>0&&snapshot.Variables.ContainsKey(leaf)
            &&_activeScene!.Simulation.GetProperty("pointBindings").EnumerateArray().Any(b=>Text(b,"equipmentId")==equipmentId&&Text(b,"point")==variable.Binding);
    }
    public bool VerifyDeviceTraceScalar(string equipmentId,string leaf,VirtualControllerSnapshot accepted,bool expectReferences)
    {
        RefreshIoInspector();
        var row=Enumerable.Range(0,_deviceTraceSelector.ItemCount).FirstOrDefault(i=>_deviceTraceSelector.GetItemMetadata(i).AsString()==equipmentId,-1);
        if(row<0)return false;
        _deviceTraceSelector.Select(row);_deviceTraceSelector.EmitSignal(OptionButton.SignalName.ItemSelected,row);
        var variable=_virtualProgram is null?null:PlcAggregates.Expand(_virtualProgram.Variables).SingleOrDefault(v=>v.Name==leaf);
        var hasValue=accepted.Variables.TryGetValue(leaf,out var value);
        var displayed=_ioInspector.GetParsedText();
        var valid=variable is not null&&hasValue&&_virtualSnapshot is not null
            &&JsonSerializer.Serialize(accepted)==JsonSerializer.Serialize(_virtualSnapshot)&&_ladderMonitorMatchesLoadedProgram
            &&VerifyDeviceIndexedBinding(_virtualProgram!,accepted,equipmentId,leaf)
            &&displayed.Contains($"Accepted scan {accepted.ScanNumber} ·",StringComparison.Ordinal)
            &&displayed.Contains($"Point: {variable.Binding}",StringComparison.Ordinal)
            &&displayed.Contains($"  {leaf} · {variable.Role} · {value}",StringComparison.Ordinal)
            &&(expectReferences?_deviceTraceRows.Count>0:_deviceTraceRows.Count==0);
        GD.Print($"WORKSPACE_BELT_TRACE equipment={equipmentId} selected={row} leaf={leaf} value={value} scan={accepted.ScanNumber} shellScan={_virtualSnapshot?.ScanNumber} refs={_deviceTraceRows.Count} expectedRefs={expectReferences} contentIdentity={JsonSerializer.Serialize(accepted)==JsonSerializer.Serialize(_virtualSnapshot)} matchingProgram={_ladderMonitorMatchesLoadedProgram} mapped={(_virtualProgram is not null&&VerifyDeviceIndexedBinding(_virtualProgram,accepted,equipmentId,leaf))} valuePresent={hasValue} scanText={displayed.Contains($"Accepted scan {accepted.ScanNumber} ·",StringComparison.Ordinal)} pointText={(variable is not null&&displayed.Contains($"Point: {variable.Binding}",StringComparison.Ordinal))} scalarText={(variable is not null&&displayed.Contains($"  {leaf} · {variable.Role} · {value}",StringComparison.Ordinal))} valid={valid}");
        return valid;
    }
    public bool VerifyIndexedDeviceTrace(string equipmentId,string leaf,LadderReferenceKind[] expectedKinds,VirtualControllerSnapshot accepted)
    {
        if(!VerifyDeviceTraceScalar(equipmentId,leaf,accepted,true))return false;
        var expected=LadderProjectIndex.Build(_virtualProgram!).CrossReference(leaf)
            .Where(r=>r.Symbol==leaf&&r.Kind is LadderReferenceKind.Read or LadderReferenceKind.Write).ToArray();
        if(expected.Length!=expectedKinds.Length||!expectedKinds.All(k=>expected.Count(r=>r.Kind==k)==expectedKinds.Count(v=>v==k))
            ||_deviceTraceRows.Count!=expected.Length||_deviceTraceRows.Any(r=>r.Symbol!=leaf))return false;
        foreach(var reference in expected)
        {
            var row=_deviceTraceRows.FindIndex(r=>r.ElementId==reference.ElementId&&r.Kind==reference.Kind);
            if(row<0||!accepted.Elements.TryGetValue(reference.ElementId,out var element))return false;
            var evaluation=element.Energized?"energized":"not energized";
            if(!_deviceTraceReferences.GetItemText(row).EndsWith(" · "+evaluation,StringComparison.Ordinal))return false;
            var before=JsonSerializer.Serialize(_virtualSnapshot);
            _deviceNavigatedBlock="";_deviceNavigatedNetwork=-1;
            _deviceTraceReferences.EmitSignal(ItemList.SignalName.ItemActivated,row);
            var navigated=_ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id==reference.BlockId
                &&_deviceNavigatedBlock==reference.BlockId&&_deviceNavigatedNetwork==reference.NetworkIndex
                &&before==JsonSerializer.Serialize(_virtualSnapshot);
            GD.Print($"INDEXED_TRACE equipment={equipmentId} symbol={leaf} kind={reference.Kind} node={reference.ElementId} block={reference.BlockId} network={reference.NetworkIndex} scan={accepted.ScanNumber} evaluation={evaluation} navigated={navigated}");
            if(!navigated)return false;
        }
        return true;
    }
    public bool VerifyIndexedTraceDraftMismatch(string equipmentId,VirtualControllerSnapshot accepted)
    {
        var unchanged=JsonSerializer.Serialize(accepted)==JsonSerializer.Serialize(_virtualSnapshot);
        return unchanged&&!_ladderMonitorMatchesLoadedProgram&&VerifyDeviceTraceSelector(equipmentId,false);
    }
    public bool VerifyDeviceTraceSelector(string equipmentId, bool expectCurrent)
    {
        RefreshIoInspector();
        var row=Enumerable.Range(0,_deviceTraceSelector.ItemCount).FirstOrDefault(i=>_deviceTraceSelector.GetItemMetadata(i).AsString()==equipmentId,-1);
        if(row<0)return false;
        _deviceTraceSelector.Select(row);
        _deviceTraceSelector.EmitSignal(OptionButton.SignalName.ItemSelected,row);
        var before=_ladderDocument.ActiveBlockIndex;
        if(expectCurrent)
        {
            if(_deviceTraceRows.Count==0||!_ladderMonitorMatchesLoadedProgram)return false;
            var expected=_deviceTraceRows[0];
            var accepted=_virtualSnapshot;
            _deviceNavigatedBlock="";_deviceNavigatedNetwork=-1;
            _deviceTraceReferences.EmitSignal(ItemList.SignalName.ItemActivated,0);
            return _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id==expected.BlockId
                &&_deviceNavigatedBlock==expected.BlockId&&_deviceNavigatedNetwork==expected.NetworkIndex
                &&ReferenceEquals(accepted,_virtualSnapshot)&&_ioInspector.Text.Contains("Accepted scan",StringComparison.Ordinal);
        }
        NavigateDeviceReference(0);
        return _deviceTraceRows.Count==0&&_ladderDocument.ActiveBlockIndex==before
            &&_ioInspector.Text.Contains("navigation and evaluation unavailable",StringComparison.Ordinal);
    }    public void ScrollDeviceTraceForReview() => _ioInspector.ScrollToLine(Math.Max(0,_ioInspector.GetLineCount()-10));
    public void ShowDeviceTraceForReview(string equipmentId)
    {
        SetReviewState("signals",string.Empty);
        RefreshIoInspector();
        var row=Enumerable.Range(0,_deviceTraceSelector.ItemCount).FirstOrDefault(i=>_deviceTraceSelector.GetItemMetadata(i).AsString()==equipmentId,-1);
        if(row<0)throw new InvalidOperationException("Unknown diagnostic device.");
        _deviceTraceSelector.Select(row);_deviceTraceSelector.EmitSignal(OptionButton.SignalName.ItemSelected,row);
    }    public bool VerifyDeviceTraceEquipmentInventory(params string[] selectedIds)
    {
        RefreshIoInspector();
        var actual=Enumerable.Range(0,_deviceTraceSelector.ItemCount).Select(i=>_deviceTraceSelector.GetItemMetadata(i).AsString()).ToArray();
        if(_activeScene is null||!actual.SequenceEqual(_activeScene.Equipment.Select(e=>e.Id)))return false;
        foreach(var id in selectedIds)
        {
            var row=Array.IndexOf(actual,id);if(row<0)return false;
            _deviceTraceSelector.Select(row);_deviceTraceSelector.EmitSignal(OptionButton.SignalName.ItemSelected,row);
            if(!_ioInspector.Text.Contains("DEVICE TRACE · "+id,StringComparison.Ordinal))return false;
        }
        return true;
    }}
