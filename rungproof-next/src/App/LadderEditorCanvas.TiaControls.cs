using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class LadderEditorCanvas
{
    private OptionButton? _tiaCounterTypeSelector;
    private bool _syncingTiaCounterTypeSelector;
    private OptionButton? _tiaCompareTypeSelector;
    private bool _syncingTiaCompareTypeSelector;
    private OptionButton? _tiaNumericTypeSelector;
    private bool _syncingTiaNumericTypeSelector;
    private OptionButton? _tiaConvertInputTypeSelector;
    private OptionButton? _tiaConvertOutputTypeSelector;
    private bool _syncingTiaConvertTypeSelectors;

    public event System.Action<int, LadderCounterValueType>? CounterValueTypeRequested;
    public event System.Action<int, string, LadderComparisonValueType>? ComparisonValueTypeRequested;
    public event System.Action<int, LadderNumericDataType>? NumericDataTypeRequested;
    public event System.Action<int, bool, LadderNumericDataType>? NumericConvertDataTypeRequested;

    private void InitializeTiaCounterTypeSelector()
    {
        _tiaCounterTypeSelector = new OptionButton
        {
            Name = "TiaCounterValueTypeSelector",
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            TooltipText = "Counter value type. ULInt values above 9,223,372,036,854,775,807 exceed the simulator's signed 64-bit range.",
        };
        foreach (var valueType in Enum.GetValues<LadderCounterValueType>())
            _tiaCounterTypeSelector.AddItem(LadderCounterValueTypePresentation.TiaLabel(valueType));
        _tiaCounterTypeSelector.AddThemeColorOverride("font_color", new Color("17242c"));
        _tiaCounterTypeSelector.AddThemeColorOverride("font_hover_color", new Color("17242c"));
        _tiaCounterTypeSelector.AddThemeFontSizeOverride("font_size", 11);
        var selectorStyle = new StyleBoxFlat
        {
            BgColor = new Color("e9edef"), BorderColor = new Color("87949a"),
            BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
            ContentMarginLeft = 5, ContentMarginRight = 20, ContentMarginTop = 1, ContentMarginBottom = 1,
        };
        _tiaCounterTypeSelector.AddThemeStyleboxOverride("normal", selectorStyle);
        _tiaCounterTypeSelector.AddThemeStyleboxOverride("hover", selectorStyle);
        _tiaCounterTypeSelector.AddThemeStyleboxOverride("pressed", selectorStyle);
        _tiaCounterTypeSelector.ItemSelected += selected =>
        {
            if (_syncingTiaCounterTypeSelector || _document.Rungs.Count == 0) return;
            CounterValueTypeRequested?.Invoke(SelectedRung, (LadderCounterValueType)(int)selected);
        };
        AddChild(_tiaCounterTypeSelector);
        _tiaCounterTypeSelector.Visible = false;

        _tiaCompareTypeSelector = new OptionButton
        {
            Name = "TiaComparisonValueTypeSelector",
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            TooltipText = "Comparator data type. Current offline simulator support: INT, DINT, REAL.",
        };
        foreach (var valueType in LadderComparisonValueTypePresentation.TiaSelectableTypes)
            _tiaCompareTypeSelector.AddItem(LadderComparisonValueTypePresentation.TiaLabel(valueType));
        _tiaCompareTypeSelector.AddThemeColorOverride("font_color", new Color("17242c"));
        _tiaCompareTypeSelector.AddThemeColorOverride("font_hover_color", new Color("17242c"));
        _tiaCompareTypeSelector.AddThemeFontSizeOverride("font_size", 11);
        _tiaCompareTypeSelector.AddThemeStyleboxOverride("normal", selectorStyle);
        _tiaCompareTypeSelector.AddThemeStyleboxOverride("hover", selectorStyle);
        _tiaCompareTypeSelector.AddThemeStyleboxOverride("pressed", selectorStyle);
        _tiaCompareTypeSelector.ItemSelected += selected =>
        {
            if (_syncingTiaCompareTypeSelector || _document.Rungs.Count == 0 || string.IsNullOrEmpty(SelectedNodeId)) return;
            ComparisonValueTypeRequested?.Invoke(SelectedRung, SelectedNodeId,
                LadderComparisonValueTypePresentation.FromTiaSelector((int)selected));
        };
        AddChild(_tiaCompareTypeSelector);
        _tiaCompareTypeSelector.Visible = false;

        _tiaNumericTypeSelector = new OptionButton
        {
            Name = "TiaNumericDataTypeSelector",
            FocusMode = FocusModeEnum.All,
            MouseDefaultCursorShape = CursorShape.PointingHand,
            TooltipText = "Instruction type choices follow the selected Siemens instruction. This offline model supports INT, DINT, and REAL only; wider Siemens types remain unsupported.",
        };
        foreach (var valueType in LadderNumericDataTypePresentation.TiaSelectableTypes)
            _tiaNumericTypeSelector.AddItem(LadderNumericDataTypePresentation.TiaLabel(valueType));
        _tiaNumericTypeSelector.AddThemeColorOverride("font_color", new Color("17242c"));
        _tiaNumericTypeSelector.AddThemeColorOverride("font_hover_color", new Color("17242c"));
        _tiaNumericTypeSelector.AddThemeFontSizeOverride("font_size", 11);
        _tiaNumericTypeSelector.AddThemeStyleboxOverride("normal", selectorStyle);
        _tiaNumericTypeSelector.AddThemeStyleboxOverride("hover", selectorStyle);
        _tiaNumericTypeSelector.AddThemeStyleboxOverride("pressed", selectorStyle);
        _tiaNumericTypeSelector.ItemSelected += selected =>
        {
            if (_syncingTiaNumericTypeSelector || _document.Rungs.Count == 0) return;
            var kind = _document.Rungs[SelectedRung].NumericOperationKind;
            NumericDataTypeRequested?.Invoke(SelectedRung,
                LadderNumericDataTypePresentation.FromTiaSelector(kind, (int)selected));
        };
        AddChild(_tiaNumericTypeSelector);
        _tiaNumericTypeSelector.Visible = false;

        _tiaConvertInputTypeSelector = CreateSideTypeSelector("TiaConvertInputTypeSelector");
        _tiaConvertInputTypeSelector.ItemSelected += selected =>
        {
            if (_syncingTiaConvertTypeSelectors || _document.Rungs.Count == 0) return;
            var kind = _document.Rungs[SelectedRung].NumericOperationKind;
            NumericConvertDataTypeRequested?.Invoke(SelectedRung, true,
                LadderNumericDataTypePresentation.FromTiaSideSelector(kind, true, (int)selected));
        };
        AddChild(_tiaConvertInputTypeSelector);
        _tiaConvertInputTypeSelector.Visible = false;

        _tiaConvertOutputTypeSelector = CreateSideTypeSelector("TiaConvertOutputTypeSelector");
        _tiaConvertOutputTypeSelector.ItemSelected += selected =>
        {
            if (_syncingTiaConvertTypeSelectors || _document.Rungs.Count == 0) return;
            var kind = _document.Rungs[SelectedRung].NumericOperationKind;
            NumericConvertDataTypeRequested?.Invoke(SelectedRung, false,
                LadderNumericDataTypePresentation.FromTiaSideSelector(kind, false, (int)selected));
        };
        AddChild(_tiaConvertOutputTypeSelector);
        _tiaConvertOutputTypeSelector.Visible = false;

        OptionButton CreateSideTypeSelector(string name)
        {
            var sideSelectorStyle = new StyleBoxFlat
            {
                BgColor = new Color("e9edef"), BorderColor = new Color("87949a"),
                BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                ContentMarginLeft = 2, ContentMarginRight = 2, ContentMarginTop = 1, ContentMarginBottom = 1,
            };
            var selector = new OptionButton
            {
                Name = name,
                FitToLongestItem = false,
                FocusMode = FocusModeEnum.All,
                MouseDefaultCursorShape = CursorShape.PointingHand,
                TooltipText = "Instruction input/output type. The offline simulator supports INT, DINT, and REAL only.",
            };
            foreach (var valueType in LadderNumericDataTypePresentation.TiaSelectableTypesFor(LadderNumericOperationKind.Convert))
                selector.AddItem(LadderNumericDataTypePresentation.TiaLabel(valueType));
            selector.AddThemeColorOverride("font_color", new Color("17242c"));
            selector.AddThemeColorOverride("font_hover_color", new Color("17242c"));
            selector.AddThemeFontSizeOverride("font_size", 11);
            selector.AddThemeStyleboxOverride("normal", sideSelectorStyle);
            selector.AddThemeStyleboxOverride("hover", sideSelectorStyle);
            selector.AddThemeStyleboxOverride("pressed", sideSelectorStyle);
            return selector;
        }
    }

    private void RefreshTiaCounterTypeSelector()
    {
        if (_tiaCounterTypeSelector is null) return;
        var visible = _style == LadderVendorStyle.SiemensTia && _document.Rungs.Count > 0
            && OutputSelected && !NetworkLabelSelected && _document.Rungs[SelectedRung].IsCounter;
        _tiaCounterTypeSelector.Visible = visible;
        if (!visible) return;

        var rung = _document.Rungs[SelectedRung];
        var bounds = TerminalBounds(SelectedRung);
        // TIA places the counter's datatype control directly below the block
        // title and above CU/CD; it remains part of the block when selected.
        var selectorWidth = Math.Max(58, _tiaCounterTypeSelector.GetCombinedMinimumSize().X);
        var origin = new Vector2(bounds.Position.X + (bounds.Size.X - selectorWidth) / 2, bounds.Position.Y + 22);
        _tiaCounterTypeSelector.Position = ToCanvas(origin);
        // Center the native-width data-type control and keep it above the Q row.
        _tiaCounterTypeSelector.Size = ToCanvas(new Vector2(selectorWidth, 18));
        _syncingTiaCounterTypeSelector = true;
        _tiaCounterTypeSelector.Select((int)rung.CounterValueType);
        _syncingTiaCounterTypeSelector = false;
    }

    private void RefreshTiaComparisonTypeSelector()
    {
        if (_tiaCompareTypeSelector is null) return;
        var selected = _style == LadderVendorStyle.SiemensTia && _document.Rungs.Count > 0
            && !OutputSelected && !string.IsNullOrEmpty(SelectedNodeId)
            ? EditableLadderTree.Find(_document.Rungs[SelectedRung].Root, SelectedNodeId)
            : null;
        var visible = selected is { Kind: LadderNodeKind.Compare, CompareOperator: not LadderCompareOperator.ContainsCode };
        _tiaCompareTypeSelector.Visible = visible;
        if (!visible || selected is null) return;
        var placed = TreeLayout(_document.Rungs[SelectedRung]).Nodes.FirstOrDefault(n => n.Node.Id == selected.Id);
        if (placed is null) return;
        var origin = TreeOrigin(SelectedRung);
        var centerX = origin.X + placed.Bounds.X + placed.Bounds.Width / 2;
        var centerY = origin.Y + placed.ContinuityY;
        var size = new Vector2(50, 20);
        _tiaCompareTypeSelector.Position = ToCanvas(new Vector2(centerX - size.X / 2, centerY - 7));
        _tiaCompareTypeSelector.Size = ToCanvas(size);
        _syncingTiaCompareTypeSelector = true;
        var selectorValueType = selected.ComparisonValueType;
        if (selectorValueType == LadderComparisonValueType.Unspecified)
        {
            var declaredType = FindTagForOperand(selected.Variable)?.Type
                ?? FindTagForOperand(selected.RightOperand)?.Type;
            selectorValueType = declaredType switch
            {
                PlcVariableType.Int => LadderComparisonValueType.Int,
                PlcVariableType.DInt => LadderComparisonValueType.DInt,
                PlcVariableType.Real => LadderComparisonValueType.Real,
                _ => LadderComparisonValueType.Unspecified,
            };
        }
        var index = LadderComparisonValueTypePresentation.TiaSelectorIndex(selectorValueType);
        _tiaCompareTypeSelector.Select(Math.Max(0, index));
        _syncingTiaCompareTypeSelector = false;
    }

    private void RefreshTiaNumericTypeSelector()
    {
        var selector = _tiaNumericTypeSelector;
        if (selector is null) return;
        var selectedNumeric = _style == LadderVendorStyle.SiemensTia && _document.Rungs.Count > 0
            && OutputSelected && !NetworkLabelSelected && _document.Rungs[SelectedRung].IsNumericOperation
            && LadderNumericOperationRules.HasTiaDataTypeSelector(_document.Rungs[SelectedRung].NumericOperationKind);
        var selectedDual = selectedNumeric
            && LadderNumericOperationRules.HasTiaDualDataTypeSelectors(_document.Rungs[SelectedRung].NumericOperationKind);
        selector.Visible = selectedNumeric && !selectedDual;
        if (_tiaConvertInputTypeSelector is not null) _tiaConvertInputTypeSelector.Visible = selectedDual;
        if (_tiaConvertOutputTypeSelector is not null) _tiaConvertOutputTypeSelector.Visible = selectedDual;
        if (!selectedNumeric) return;

        var rung = _document.Rungs[SelectedRung];
        if (selectedDual)
        {
            RefreshTiaDualTypeSelectors(rung);
            return;
        }
        var choices = LadderNumericDataTypePresentation.TiaSelectableTypesFor(rung.NumericOperationKind);
        var optionsChanged = selector.ItemCount != choices.Count;
        for (var i = 0; !optionsChanged && i < choices.Count; i++)
            optionsChanged = selector.GetItemText(i) != LadderNumericDataTypePresentation.TiaLabel(choices[i]);
        if (optionsChanged)
        {
            _syncingTiaNumericTypeSelector = true;
            selector.Clear();
            foreach (var valueType in choices)
                selector.AddItem(LadderNumericDataTypePresentation.TiaLabel(valueType));
            _syncingTiaNumericTypeSelector = false;
        }
        var bounds = TerminalBounds(SelectedRung);
        // EXPT has a fixed REAL base chip, a visible ** separator, and an
        // exponent selector. Its body reserves the selector's effective theme
        // minimum so the control does not move left over the operator token.
        var selectorWidth = rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate
            ? Math.Max(54, selector.GetCombinedMinimumSize().X)
            : Math.Max(68, selector.GetCombinedMinimumSize().X);
        var origin = rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate
            ? new Vector2(bounds.End.X - selectorWidth - 8, bounds.Position.Y + 21)
            : new Vector2(bounds.Position.X + (bounds.Size.X - selectorWidth) / 2, bounds.Position.Y + 30);
        selector.Position = ToCanvas(origin);
        selector.Size = ToCanvas(new Vector2(selectorWidth, 18));

        var selectedType = rung.NumericDataType;
        var inferredType = LadderNumericDataTypePresentation.InferredTypeFor(rung.NumericOperationKind,
            FindTagForOperand(rung.NumericOperationKind == LadderNumericOperationKind.Exponentiate
                ? rung.NumericSourceB : rung.NumericDestination)?.Type);
        if (selectedType == LadderNumericDataType.Unspecified && inferredType != LadderNumericDataType.Unspecified)
            selectedType = inferredType;
        var autoIndex = LadderNumericDataTypePresentation.TiaSelectorIndex(rung.NumericOperationKind, LadderNumericDataType.Auto);
        if (autoIndex >= 0)
            selector.SetItemText(autoIndex,
                LadderNumericDataTypePresentation.TiaLabel(LadderNumericDataType.Auto, inferredType));
        _syncingTiaNumericTypeSelector = true;
        selector.Select(Math.Max(0,
            LadderNumericDataTypePresentation.TiaSelectorIndex(rung.NumericOperationKind, selectedType)));
        _syncingTiaNumericTypeSelector = false;
    }

    private void RefreshTiaDualTypeSelectors(EditableRung rung)
    {
        var inputSelector = _tiaConvertInputTypeSelector;
        var outputSelector = _tiaConvertOutputTypeSelector;
        if (inputSelector is null || outputSelector is null) return;
        var bounds = TerminalBounds(SelectedRung);
        // Siemens presents two compact chips with a visible "to" separator.
        // Keep both controls disjoint inside the 132 px scaling/conversion box.
        const float selectorWidth = 48;
        const float selectorHeight = 20;
        inputSelector.Position = ToCanvas(new Vector2(bounds.Position.X + 4, bounds.Position.Y + 20));
        outputSelector.Position = ToCanvas(new Vector2(bounds.End.X - selectorWidth - 4, bounds.Position.Y + 20));
        inputSelector.Size = ToCanvas(new Vector2(selectorWidth, selectorHeight));
        outputSelector.Size = ToCanvas(new Vector2(selectorWidth, selectorHeight));

        SyncChoices(inputSelector, LadderNumericDataTypePresentation.TiaInputTypesFor(rung.NumericOperationKind));
        SyncChoices(outputSelector, LadderNumericDataTypePresentation.TiaOutputTypesFor(rung.NumericOperationKind));

        var inputOperand = rung.NumericOperationKind == LadderNumericOperationKind.Scale
            ? rung.NumericSourceB : rung.NumericSourceA;
        var inputType = rung.NumericInputDataType;
        if (inputType == LadderNumericDataType.Unspecified)
            inputType = rung.NumericOperationKind == LadderNumericOperationKind.Scale
                ? LadderNumericDataType.Real
                : LadderNumericDataTypePresentation.FromVariableType(FindTagForOperand(inputOperand)?.Type);
        var outputType = rung.NumericOutputDataType;
        if (outputType == LadderNumericDataType.Unspecified)
            outputType = rung.NumericOperationKind == LadderNumericOperationKind.Normalize
                ? LadderNumericDataType.Real
                : LadderNumericDataTypePresentation.FromVariableType(FindTagForOperand(rung.NumericDestination)?.Type);
        _syncingTiaConvertTypeSelectors = true;
        inputSelector.Select(Math.Max(0,
            LadderNumericDataTypePresentation.TiaSideSelectorIndex(rung.NumericOperationKind, true, inputType)));
        outputSelector.Select(Math.Max(0,
            LadderNumericDataTypePresentation.TiaSideSelectorIndex(rung.NumericOperationKind, false, outputType)));
        _syncingTiaConvertTypeSelectors = false;

        void SyncChoices(OptionButton target, System.Collections.Generic.IReadOnlyList<LadderNumericDataType> choices)
        {
            var changed = target.ItemCount != choices.Count;
            for (var i = 0; !changed && i < choices.Count; i++)
                changed = target.GetItemText(i) != LadderNumericDataTypePresentation.TiaLabel(choices[i]);
            if (!changed) return;
            _syncingTiaConvertTypeSelectors = true;
            target.Clear();
            foreach (var choice in choices) target.AddItem(LadderNumericDataTypePresentation.TiaLabel(choice));
            _syncingTiaConvertTypeSelectors = false;
        }
    }
}
