using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

internal enum TiaSweepTargetKind { Node, Terminal, Label, Network }

internal sealed record TiaSweepFixture(
    LadderEditorDocument Document,
    int RungIndex,
    TiaSweepTargetKind TargetKind,
    string TargetId,
    IReadOnlyList<(string Operand, string Target)> EditableFields);

/// <summary>
/// Focused, geometry-only renderer acceptance. This deliberately proves the local
/// production canvas, not visual parity with an installed copy of TIA Portal.
/// </summary>
public partial class Main
{
    internal const int TiaSweepExpectedKeys = 50;
    internal static readonly HashSet<string> TiaSweepLocalOnlyKeys = new(StringComparer.Ordinal)
        { "counter-load", "counter-reset", "compare-6", "numeric-24" };

    private async void VerifyTiaUniversalRendererSweep()
    {
        var layer = new CanvasLayer { Name = "TiaUniversalRendererSweep", Layer = 100 };
        AddChild(layer);
        try
        {
            async Task Frames()
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            static void Check(bool condition, string message)
            {
                if (!condition) throw new InvalidOperationException(message);
            }

            var catalogKeys = LadderInstructionCatalog.Entries.Select(item => item.Key).ToArray();
            var duplicateKeys = catalogKeys.GroupBy(key => key, StringComparer.Ordinal).Where(group => group.Count() != 1).Select(group => group.Key).ToArray();
            Check(duplicateKeys.Length == 0, "Catalog contains duplicate keys: " + string.Join(", ", duplicateKeys));
            Check(TiaSweepLocalOnlyKeys.All(key => catalogKeys.Contains(key, StringComparer.Ordinal)),
                "One or more explicit local-only exclusions are missing from the authoritative catalog.");
            var keys = catalogKeys.Where(key => !TiaSweepLocalOnlyKeys.Contains(key)).ToArray();
            Check(keys.Length == TiaSweepExpectedKeys,
                $"Expected {TiaSweepExpectedKeys} Siemens-facing catalog keys after four explicit exclusions; found {keys.Length}.");

            var modes = new[]
            {
                (Name: "symbolic-hidden-on", Operand: TiaOperandRepresentation.Symbolic,
                    Info: TiaTagInformationDisplay.Hidden, Position: TiaTagInformationPosition.OnOperands),
                (Name: "symbolic-absolute-show-on", Operand: TiaOperandRepresentation.SymbolicAndAbsolute,
                    Info: TiaTagInformationDisplay.Show, Position: TiaTagInformationPosition.OnOperands),
                (Name: "absolute-comments-below", Operand: TiaOperandRepresentation.Absolute,
                    Info: TiaTagInformationDisplay.HierarchicalComments, Position: TiaTagInformationPosition.BelowNetwork),
            };
            var expectedCases = keys.Length * 3 * 2 * modes.Length;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var failures = new List<string>();

            foreach (var key in keys)
            {
                var fixture = BuildTiaSweepFixture(key);
                var compile = LadderCompiler.Compile(fixture.Document.BuildProgram());
                Check(compile.IsValid, $"{key}: representative document is invalid: " +
                    string.Join(" | ", compile.Issues.Select(issue => issue.Path + " " + issue.Message)));

                var canvas = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, fixture.Document)
                {
                    Size = new Vector2(1800, 1100),
                };
                layer.AddChild(canvas);
                canvas.RefreshDocument();
                await Frames();

                foreach (var zoom in new[] { 75, 100, 150 })
                foreach (var selected in new[] { false, true })
                foreach (var mode in modes)
                {
                    var caseId = $"{key}|z{zoom}|{(selected ? "selected" : "unselected")}|{mode.Name}";
                    try
                    {
                        Check(seen.Add(caseId), "Duplicate renderer case: " + caseId);
                        canvas.SetZoomPercent(zoom);
                        canvas.SetTiaDisplaySettings(mode.Operand, mode.Info, mode.Position);
                        if (selected) SelectTiaSweepTarget(canvas, fixture);
                        else canvas.ClearElementSelection();
                        canvas.RefreshDocument();
                        await Frames();

                        Check(canvas.ZoomPercent == zoom, caseId + ": requested zoom was not applied.");
                        Check(canvas.OperandRepresentation == mode.Operand && canvas.TagInformationDisplay == mode.Info
                            && canvas.TagInformationPosition == mode.Position, caseId + ": TIA display mode was not retained.");
                        Check(TiaSweepSelectionStable(canvas, fixture, selected), caseId + ": selection target changed during refresh.");

                        if (key == "call")
                        {
                            var expected = mode.Operand switch
                            {
                                TiaOperandRepresentation.Symbolic => new[] { "enable" },
                                TiaOperandRepresentation.Absolute => new[] { "%I0.0" },
                                _ => new[] { "enable", "%I0.0" },
                            };
                            var inputReference = fixture.Document.Rungs[fixture.RungIndex].CallBindings
                                .Single(binding => binding.ParameterId == fixture.Document.Blocks
                                    .Single(block => block.Name == "ReviewFB").Interface
                                    .Single(formal => formal.Name == "EnableIn").Id).Symbol;
                            var rendered = canvas.ReviewOperandDisplayLines(inputReference);
                            Check(rendered.SequenceEqual(expected), caseId + ": CALL actual leaked its internal reference or lost its address: "
                                + string.Join(" / ", rendered));
                            Check(rendered.All(line => !line.StartsWith("global:", StringComparison.Ordinal)),
                                caseId + ": CALL actual exposed an internal global identity.");
                            Check(canvas.ReviewTiaCallOutputOperandRowsClearConductors(fixture.RungIndex),
                                caseId + ": a CALL output actual text row intersects its pin conductor.");
                        }

                        var geometry = canvas.ReviewTiaSweepGeometry(fixture.RungIndex, fixture.TargetKind, fixture.TargetId);
                        Check(geometry.NetworkBounds.Encloses(geometry.InstructionBounds),
                            caseId + $": instruction bounds {geometry.InstructionBounds} escape network {geometry.NetworkBounds}.");
                        Check(geometry.InputWire && geometry.OutputWire,
                            caseId + ": production layout does not expose both input and output conductor endpoints.");
                        Check(Math.Abs(geometry.TerminalClearance.X) < 0.01f && Math.Abs(geometry.TerminalClearance.Y) < 0.01f,
                            caseId + $": conductor/body endpoint clearance is {geometry.TerminalClearance}; zero is required.");

                        if (selected)
                        {
                            var fields = canvas.ReviewTiaSweepFieldBounds(fixture);
                            Check(fields.All(field => geometry.NetworkBounds.Encloses(field)),
                                caseId + ": an operand/formal/instance/value region escapes its network.");
                            for (var i = 0; i < fields.Count; i++)
                            for (var j = i + 1; j < fields.Count; j++)
                                Check(!fields[i].Intersects(fields[j], includeBorders: false),
                                    caseId + $": measurable editor regions overlap: {fields[i]} and {fields[j]}.");
                        }
                        GD.Print("TIA_RENDERER_SWEEP_CASE PASS " + caseId);
                    }
                    catch (Exception error)
                    {
                        failures.Add(caseId + ": " + error.Message);
                        GD.PushError("TIA_RENDERER_SWEEP_CASE FAIL " + caseId + " " + error.Message);
                    }
                }
                canvas.QueueFree();
                await Frames();
            }

            Check(seen.Count == expectedCases, $"Expected {expectedCases} exact cases; executed {seen.Count}.");
            Check(failures.Count == 0, failures.Count == 0 ? string.Empty
                : $"{failures.Count} renderer states failed; first failure: {failures[0]}");
            var sheetArgument = OS.GetCmdlineUserArgs().FirstOrDefault(argument =>
                argument.StartsWith("--contact-sheet-dir=", StringComparison.Ordinal));
            if (sheetArgument is not null)
                await CaptureTiaRendererContactSheets(keys, sheetArgument["--contact-sheet-dir=".Length..],
                    selected: true, layer, Frames, Check);
            var unselectedSheetArgument = OS.GetCmdlineUserArgs().FirstOrDefault(argument =>
                argument.StartsWith("--unselected-contact-sheet-dir=", StringComparison.Ordinal));
            if (unselectedSheetArgument is not null)
                await CaptureTiaRendererContactSheets(keys,
                    unselectedSheetArgument["--unselected-contact-sheet-dir=".Length..],
                    selected: false, layer, Frames, Check);
            GD.Print($"TIA_RENDERER_SWEEP_PASS keys={keys.Length} cases={seen.Count} externalTia=false");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("TIA_RENDERER_SWEEP_FAIL externalTia=false " + error);
            GetTree().Quit(1);
        }
        finally { layer.QueueFree(); }
    }

    private async Task CaptureTiaRendererContactSheets(
        IReadOnlyList<string> keys,
        string outputDirectory,
        bool selected,
        CanvasLayer layer,
        Func<Task> frames,
        Action<bool, string> check)
    {
        const int tileWidth = 1600;
        const int tileHeight = 1080;
        const int columns = 2;
        const int rows = 4;
        const int pageSize = columns * rows;
        var zoomLevels = new[] { 75, 100, 150 };
        var output = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(output);
        var detailDirectory = Path.Combine(output, "tiles");
        Directory.CreateDirectory(detailDirectory);
        var sheets = new List<object>();
        var details = new List<object>();
        var captured = new HashSet<string>(StringComparer.Ordinal);
        var keyOrdinals = keys.Select((key, index) => (key, ordinal: index + 1))
            .ToDictionary(item => item.key, item => item.ordinal, StringComparer.Ordinal);

        static string Slug(string value) => string.Join("-", value
            .ToLowerInvariant()
            .Split(new[] { ' ', '/', '_' }, StringSplitOptions.RemoveEmptyEntries));

        static string Family(string key)
        {
            var category = LadderInstructionCatalog.Get(key).Category;
            return category.StartsWith("Program ", StringComparison.Ordinal) ? "Program" : category;
        }

        var groups = keys.GroupBy(Family)
            .Select(group => (Category: group.Key, Keys: group.ToArray()))
            .ToArray();

        foreach (var zoom in zoomLevels)
        foreach (var group in groups)
        for (var pageIndex = 0; pageIndex * pageSize < group.Keys.Length; pageIndex++)
        {
            var pageKeys = group.Keys.Skip(pageIndex * pageSize).Take(pageSize).ToArray();
            var pageCount = (int)Math.Ceiling(group.Keys.Length / (double)pageSize);
            var fileName = selected
                ? $"tia-renderer-contact-{Slug(group.Category)}-z{zoom:D3}-p{pageIndex + 1:D2}-of-{pageCount:D2}.png"
                : $"tia-renderer-unselected-contact-{Slug(group.Category)}-z{zoom:D3}-p{pageIndex + 1:D2}-of-{pageCount:D2}.png";
            var sheet = Image.CreateEmpty(tileWidth * columns, tileHeight * rows, false, Image.Format.Rgba8);
            sheet.Fill(new Color("dfe5e8"));

            for (var tileIndex = 0; tileIndex < pageKeys.Length; tileIndex++)
            {
                var key = pageKeys[tileIndex];
                check(captured.Add($"{zoom}|{key}"), $"Duplicate contact-sheet tile for {key} at {zoom}%.");
                var fixture = BuildTiaSweepFixture(key);
                var compile = LadderCompiler.Compile(fixture.Document.BuildProgram());
                check(compile.IsValid, $"{key}: contact-sheet fixture did not compile.");

                var canvas = new LadderEditorCanvas(LadderVendorStyle.SiemensTia, fixture.Document)
                {
                    Size = new Vector2(tileWidth, tileHeight),
                };
                layer.AddChild(canvas);
                canvas.SetZoomPercent(zoom);
                canvas.SetTiaDisplaySettings(TiaOperandRepresentation.SymbolicAndAbsolute,
                    TiaTagInformationDisplay.Show, TiaTagInformationPosition.OnOperands);
                if (selected) SelectTiaSweepTarget(canvas, fixture);
                canvas.RefreshDocument();
                await frames();
                check(TiaSweepSelectionStable(canvas, fixture, selected),
                    $"{key} at {zoom}%: {(selected ? "selected" : "unselected")} capture state changed during refresh.");

                // Keep the production canvas at its normal origin. Moving the
                // entire control to center tall witnesses also moved its shell,
                // producing zoom-dependent black bands and missing headers.
                // A full native viewport is large enough for every selected
                // witness and target network, so fail closed if either escapes.
                var geometry = canvas.ReviewTiaSweepGeometry(
                    fixture.RungIndex, fixture.TargetKind, fixture.TargetId);
                var witness = geometry.InstructionBounds;
                const float reviewTop = 0.0f;
                const float reviewBottom = tileHeight;
                const float witnessMargin = 1.0f;
                var availableHeight = reviewBottom - reviewTop;
                check(witness.Size.Y + witnessMargin * 2.0f <= availableHeight,
                    $"{key} at {zoom}% is {witness.Size.Y:0.##} px high and cannot fit the {availableHeight:0.##} px review area with margin.");
                var framedWitness = witness.Grow(witnessMargin);
                check(framedWitness.Position.Y >= reviewTop - 0.01f && framedWitness.End.Y <= reviewBottom + 0.01f,
                    $"{key} at {zoom}% was not fully framed: {framedWitness} outside Y={reviewTop:0}-{reviewBottom:0}.");
                var framedNetwork = geometry.NetworkBounds.Grow(witnessMargin);
                check(framedNetwork.Position.Y >= reviewTop - 0.01f && framedNetwork.End.Y <= reviewBottom + 0.01f,
                    $"{key} at {zoom}% target network was not fully framed: {framedNetwork} outside Y={reviewTop:0}-{reviewBottom:0}.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var viewportImage = GetViewport().GetTexture().GetImage();
                if (viewportImage is null || viewportImage.IsEmpty())
                    throw new InvalidOperationException(
                        "Contact sheets require the normal renderer; no viewport image was available.");
                if (viewportImage.GetFormat() != Image.Format.Rgba8)
                    viewportImage.Convert(Image.Format.Rgba8);
                check(viewportImage.GetWidth() >= tileWidth && viewportImage.GetHeight() >= tileHeight,
                    $"Contact-sheet viewport is only {viewportImage.GetWidth()}x{viewportImage.GetHeight()}.");
                var tile = viewportImage.GetRegion(new Rect2I(0, 0, tileWidth, tileHeight));
                var detailFileName = selected
                    ? $"tia-renderer-{keyOrdinals[key]:D2}-{key}-z{zoom:D3}-selected-symbolic-absolute-show-on.png"
                    : $"tia-renderer-{keyOrdinals[key]:D2}-{key}-z{zoom:D3}-unselected-symbolic-absolute-show-on.png";
                var detailPath = Path.Combine(detailDirectory, detailFileName);
                var detailError = tile.SavePng(detailPath);
                check(detailError == Error.Ok, $"Unable to save renderer detail '{detailPath}': {detailError}.");
                var detailHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(detailPath)));
                details.Add(new
                {
                    file = "tiles/" + detailFileName,
                    key,
                    tiaName = LadderInstructionCatalog.Get(key).TiaName,
                    category = group.Category,
                    zoomPercent = zoom,
                    width = tile.GetWidth(),
                    height = tile.GetHeight(),
                    sha256 = detailHash,
                    sheet = fileName,
                    sheetTile = tileIndex + 1,
                });
                var destination = new Vector2I((tileIndex % columns) * tileWidth, (tileIndex / columns) * tileHeight);
                sheet.BlitRect(tile, new Rect2I(0, 0, tileWidth, tileHeight), destination);

                canvas.QueueFree();
                await frames();
            }

            var path = Path.Combine(output, fileName);
            var saveError = sheet.SavePng(path);
            check(saveError == Error.Ok, $"Unable to save contact sheet '{path}': {saveError}.");
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            sheets.Add(new
            {
                file = fileName,
                category = group.Category,
                zoomPercent = zoom,
                page = pageIndex + 1,
                width = sheet.GetWidth(),
                height = sheet.GetHeight(),
                sha256 = hash,
                keys = pageKeys,
            });
            GD.Print($"TIA_RENDERER_CONTACT_SHEET file={fileName} zoom={zoom} keys={pageKeys.Length} selected={selected.ToString().ToLowerInvariant()} sha256={hash}");
        }

        check(captured.Count == keys.Count * zoomLevels.Length,
            $"Expected {keys.Count * zoomLevels.Length} contact-sheet tiles; captured {captured.Count}.");
        check(details.Count == captured.Count,
            $"Expected one retained detail image per contact-sheet tile; saved {details.Count} for {captured.Count} tiles.");
        check(sheets.Count == 24, $"Expected 24 deterministic contact sheets; saved {sheets.Count}.");
        foreach (var zoom in zoomLevels)
            check(keys.All(key => captured.Contains($"{zoom}|{key}")), $"Contact-sheet coverage is incomplete at {zoom}%.");

        var manifestPath = Path.Combine(output, "tia-renderer-contact-sheets-manifest.json");
        var manifest = new
        {
            schemaVersion = 1,
            resultKind = "tia-renderer-contact-sheets",
            createdUtc = DateTimeOffset.UtcNow.ToString("O"),
            offlineOnly = true,
            installedTiaExecuted = false,
            livePlcExecuted = false,
            selected,
            operandRepresentation = TiaOperandRepresentation.SymbolicAndAbsolute.ToString(),
            tagInformationDisplay = TiaTagInformationDisplay.Show.ToString(),
            tagInformationPosition = TiaTagInformationPosition.OnOperands.ToString(),
            zoomLevels,
            keyCount = keys.Count,
            tileCount = captured.Count,
            sheetCount = sheets.Count,
            detailCount = details.Count,
            humanReviewStatus = "pending",
            sheets,
            details,
        };
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print($"TIA_RENDERER_CONTACT_SHEETS_PASS keys={keys.Count} tiles={captured.Count} sheets={sheets.Count} selected={selected.ToString().ToLowerInvariant()} externalTia=false manifest={manifestPath}");
    }

    private static void SelectTiaSweepTarget(LadderEditorCanvas canvas, TiaSweepFixture fixture)
    {
        switch (fixture.TargetKind)
        {
            case TiaSweepTargetKind.Node: canvas.SelectTreeNode(fixture.RungIndex, fixture.TargetId); break;
            case TiaSweepTargetKind.Terminal: canvas.SelectElement(fixture.RungIndex, -1, -1, output: true); break;
            case TiaSweepTargetKind.Label: canvas.SelectNetworkLabel(fixture.RungIndex); break;
            case TiaSweepTargetKind.Network: canvas.SelectRung(fixture.RungIndex); break;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private static bool TiaSweepSelectionStable(LadderEditorCanvas canvas, TiaSweepFixture fixture, bool selected)
    {
        if (!selected)
            return !canvas.OutputSelected && !canvas.NetworkLabelSelected && !canvas.NetworkSelected
                && string.IsNullOrEmpty(canvas.SelectedNodeId);
        return fixture.TargetKind switch
        {
            TiaSweepTargetKind.Node => canvas.SelectedNodeId == fixture.TargetId && !canvas.OutputSelected,
            TiaSweepTargetKind.Terminal => canvas.OutputSelected && !canvas.NetworkLabelSelected,
            TiaSweepTargetKind.Label => canvas.OutputSelected && canvas.NetworkLabelSelected,
            TiaSweepTargetKind.Network => canvas.NetworkSelected && !canvas.OutputSelected,
            _ => false,
        };
    }

    internal static TiaSweepFixture BuildTiaSweepFixture(string key)
    {
        var document = new LadderEditorDocument();
        var witnessName = LadderInstructionCatalog.Get(key).TiaName;
        document.ResetProject("tia-render-" + key, "TIA renderer " + witnessName, TimeSpan.FromMilliseconds(20));
        document.AddTag("enable", PlcVariableRole.Input, address: "%I0.0", comment: "Representative input");
        document.AddTag("alternate", PlcVariableRole.Input, address: "%I0.1", comment: "Representative alternate input");
        document.AddTag("reset_request", PlcVariableRole.Input, address: "%I0.2", comment: "Representative reset input");
        document.AddTag("load_request", PlcVariableRole.Input, address: "%I0.3", comment: "Representative load input");
        document.AddTag("bool_memory", PlcVariableRole.Memory, address: "%M0.0", comment: "Representative memory");
        document.AddTag("bool_memory_io", PlcVariableRole.Memory, address: "%M0.1", comment: "Representative in-out memory");
        document.AddTag("bool_output", PlcVariableRole.Output, address: "%Q0.0", comment: "Representative output");
        document.AddTag("timer_value", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        document.AddTag("counter_value", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("int_a", PlcVariableRole.Memory, address: "%MW2", type: PlcVariableType.Int, initialValue: 2L);
        document.AddTag("int_b", PlcVariableRole.Memory, address: "%MW4", type: PlcVariableType.Int, initialValue: 3L);
        document.AddTag("int_out", PlcVariableRole.Memory, address: "%MW6", type: PlcVariableType.Int);
        document.AddTag("dint_out", PlcVariableRole.Memory, address: "%MD8", type: PlcVariableType.DInt);
        document.AddTag("real_a", PlcVariableRole.Memory, address: "%MD12", type: PlcVariableType.Real, initialValue: 2.0);
        document.AddTag("real_b", PlcVariableRole.Memory, address: "%MD16", type: PlcVariableType.Real, initialValue: 3.0);
        document.AddTag("real_c", PlcVariableRole.Memory, address: "%MD20", type: PlcVariableType.Real, initialValue: 10.0);
        document.AddTag("real_out", PlcVariableRole.Memory, address: "%MD24", type: PlcVariableType.Real);

        TiaSweepFixture Node(EditableRung rung, EditableContact node, params (string Operand, string Target)[] fields) =>
            new(document, document.Rungs.IndexOf(rung), TiaSweepTargetKind.Node, node.Id, fields);
        TiaSweepFixture Terminal(EditableRung rung, params (string Operand, string Target)[] fields) =>
            new(document, document.Rungs.IndexOf(rung), TiaSweepTargetKind.Terminal, rung.TerminalActionId, fields);
        EditableContact Enable(EditableRung rung) => document.AddContact(document.Rungs.IndexOf(rung), 0, "enable", false);

        if (key is "no" or "nc")
        {
            var rung = document.AddRung(witnessName, "bool_output");
            var node = document.AddContact(0, 0, "enable", key == "nc");
            return Node(rung, node, ("enable", ""));
        }
        if (key is "edge-rising" or "edge-falling")
        {
            var rung = document.AddRung(witnessName, "bool_output");
            var node = document.InsertEdgeContact(0, 0, 0, "enable",
                key == "edge-rising" ? LadderEdgeMode.Rising : LadderEdgeMode.Falling);
            document.UpdateTag(node.EdgeMemoryOperand!, node.EdgeMemoryOperand!, PlcVariableType.Bool,
                PlcVariableRole.Memory, address: "%M0.2", comment: "TIA P/N edge memory");
            return Node(rung, node, ("enable", ""), (node.EdgeMemoryOperand ?? "", "edge-memory"));
        }
        if (key.StartsWith("compare-", StringComparison.Ordinal))
        {
            var rung = document.AddRung(witnessName, "bool_output");
            var node = document.AddComparison(0, 0, "int_a", (LadderCompareOperator)int.Parse(key[8..]), "int_b", LadderComparisonValueType.Int);
            return Node(rung, node, ("int_a", ""), ("int_b", "compare-right"));
        }
        if (key == "branch")
        {
            var rung = document.AddRung(witnessName, "bool_output");
            document.AddContact(0, 0, "enable", false);
            document.AddParallelBranch(0);
            document.AddContact(0, 1, "alternate", false);
            return new(document, 0, TiaSweepTargetKind.Node, rung.Root.Id, []);
        }
        if (key == "rung")
        {
            var rung = document.AddRung(witnessName, "bool_output"); Enable(rung);
            return new(document, 0, TiaSweepTargetKind.Network, rung.Id, []);
        }
        if (key is "coil" or "set" or "reset")
        {
            var rung = document.AddRung(witnessName, "bool_output"); Enable(rung);
            rung.CoilMode = key == "set" ? LadderCoilMode.Set : key == "reset" ? LadderCoilMode.Reset : LadderCoilMode.Assign;
            return Terminal(rung, ("bool_output", ""));
        }
        if (key is "ton" or "tof" or "tp" or "rto")
        {
            var kind = key switch { "tof" => LadderTimerKind.OffDelay, "tp" => LadderTimerKind.Pulse,
                "rto" => LadderTimerKind.RetentiveOnDelay, _ => LadderTimerKind.OnDelay };
            var rung = document.AddTimerRung(witnessName, "timer_value", TimeSpan.FromSeconds(1), kind); Enable(rung);
            rung.TimerResetOperand = kind == LadderTimerKind.RetentiveOnDelay ? "reset_request" : "FALSE";
            var fields = kind == LadderTimerKind.RetentiveOnDelay
                ? new[] { ("timer_value", ""), ("reset_request", "terminal-timer-reset"), (rung.TimerPresetOperand, "terminal-timer-pt") }
                : new[] { ("timer_value", ""), (rung.TimerPresetOperand, "terminal-timer-pt") };
            return Terminal(rung, fields);
        }
        if (key == "timer-reset")
        {
            var owner = document.AddTimerRung("owning timer instance", "timer_value", TimeSpan.FromSeconds(1)); Enable(owner);
            var rung = document.AddTimerResetRung(witnessName, "timer_value"); Enable(rung);
            document.EnsureInstanceDataBlocks();
            if (owner.TimerInstanceDataBlock != rung.TimerInstanceDataBlock)
                throw new InvalidOperationException("RT fixture did not reuse the owning timer instance DB.");
            return Terminal(rung, ("timer_value", ""));
        }
        if (key is "ctu" or "ctd")
        {
            var kind = key == "ctd" ? LadderCounterKind.CountDown : LadderCounterKind.CountUp;
            var rung = document.AddCounterRung(witnessName, "counter_value", 10, kind: kind, valueType: LadderCounterValueType.Int); Enable(rung);
            if (kind == LadderCounterKind.CountUp) rung.CounterResetInput = "reset_request";
            else rung.CounterLoadInput = "load_request";
            return kind == LadderCounterKind.CountUp
                ? Terminal(rung, ("counter_value", ""), ("reset_request", "terminal-counter-r"), ("10", "terminal-counter-pv"))
                : Terminal(rung, ("counter_value", ""), ("load_request", "terminal-counter-load"), ("10", "terminal-counter-pv"));
        }
        if (key == "call")
        {
            var target = document.AddBlock("ReviewFB", LadderBlockType.FunctionBlock);
            target.SemanticMode = LadderSemanticMode.Parameterized;
            document.AddInterfaceParameter(target.Id, "EnableIn", PlcVariableType.Bool, LadderInterfaceSection.Input);
            document.AddInterfaceParameter(target.Id, "EnableOut", PlcVariableType.Bool, LadderInterfaceSection.Output);
            document.AddInterfaceParameter(target.Id, "PermitInOut", PlcVariableType.Bool, LadderInterfaceSection.InOut);
            var rung = document.AddCallRung(witnessName, target.Id); Enable(rung);
            rung.CallSemanticMode = LadderSemanticMode.Parameterized;
            var edit = new LadderCallAuthoring(document, document.Blocks[0].Id, rung.Id);
            edit.SelectTarget(target.Id);
            var instanceId = edit.CreateInstance("ReviewFB_Instance");
            var input = edit.Target!.Interface.Single(formal => formal.Name == "EnableIn");
            var output = edit.Target.Interface.Single(formal => formal.Name == "EnableOut");
            var inOut = edit.Target.Interface.Single(formal => formal.Name == "PermitInOut");
            string Global(string name) => "global:" + LadderCallBindings.GlobalId(edit.Draft.Tags.Single(tag => tag.Name == name));
            edit.Bind(new LadderCallBinding(input.Id, LadderActualKind.Symbol, Global("enable")));
            edit.Bind(new LadderCallBinding(output.Id, LadderActualKind.Symbol, Global("bool_memory")));
            edit.Bind(new LadderCallBinding(inOut.Id, LadderActualKind.Symbol, Global("bool_memory_io")));
            if (!edit.Commit(new LadderEditorHistory(), false, out var error))
                throw new InvalidOperationException("FB CALL authoring failed: " + error);
            rung = document.Rungs.Single(item => item.Id == rung.Id);
            if (document.Instances.Single(item => item.Id == instanceId).InstanceDataBlock is null)
                throw new InvalidOperationException("FB CALL fixture has no assigned instance DB.");
            return Terminal(rung);
        }
        if (key == "return")
        {
            var rung = document.AddReturnRung(witnessName); Enable(rung);
            rung.ReturnValueKind = LadderReturnValueKind.Operand; rung.ReturnValueOperand = "enable";
            return Terminal(rung, ("enable", ""));
        }
        if (key == "jump")
        {
            var rung = document.AddJumpRung(witnessName, "finish"); Enable(rung);
            var label = document.AddLabelRung("destination", "finish");
            label.HasTerminalAction = true; label.CoilVariable = "bool_output";
            return Terminal(rung, ("finish", ""));
        }
        if (key == "label")
        {
            var rung = document.AddLabelRung(witnessName, "finish");
            rung.HasTerminalAction = true; rung.CoilVariable = "bool_output"; Enable(rung);
            return new(document, 0, TiaSweepTargetKind.Label, rung.NetworkLabelId, [("finish", "")]);
        }
        if (key.StartsWith("numeric-", StringComparison.Ordinal))
        {
            var kind = (LadderNumericOperationKind)int.Parse(key[8..]);
            var integer = kind is LadderNumericOperationKind.Move or LadderNumericOperationKind.Add
                or LadderNumericOperationKind.Subtract or LadderNumericOperationKind.Multiply
                or LadderNumericOperationKind.Divide or LadderNumericOperationKind.Modulo
                or LadderNumericOperationKind.Absolute or LadderNumericOperationKind.Negate;
            var conversion = kind is LadderNumericOperationKind.Convert or LadderNumericOperationKind.Truncate
                or LadderNumericOperationKind.Round or LadderNumericOperationKind.Ceiling or LadderNumericOperationKind.Floor;
            var sourceA = integer ? "int_a" : "real_a";
            var sourceB = integer ? "int_b" : "real_b";
            var destination = integer ? "int_out" : conversion ? "dint_out" : "real_out";
            var rung = document.AddNumericOperationRung(witnessName, kind, sourceA, sourceB, destination, integer ? "10" : "real_c"); Enable(rung);
            if (kind == LadderNumericOperationKind.Exponentiate) rung.NumericDataType = LadderNumericDataType.Real;
            if (conversion || kind is LadderNumericOperationKind.Normalize or LadderNumericOperationKind.Scale)
            {
                rung.NumericInputDataType = LadderNumericDataType.Real;
                rung.NumericOutputDataType = conversion ? LadderNumericDataType.DInt : LadderNumericDataType.Real;
            }
            var fields = new List<(string Operand, string Target)> { (sourceA, "terminal-numeric-a") };
            if (LadderNumericOperationRules.RequiresSourceB(kind)) fields.Add((sourceB, "terminal-numeric-b"));
            if (LadderNumericOperationRules.RequiresSourceC(kind)) fields.Add((rung.NumericSourceC, "terminal-numeric-c"));
            fields.Add((destination, ""));
            return Terminal(rung, fields.ToArray());
        }
        throw new InvalidOperationException($"No honest representative renderer fixture exists for catalog key '{key}'.");
    }
}

internal sealed record TiaSweepGeometry(Rect2 NetworkBounds, Rect2 InstructionBounds,
    bool InputWire, bool OutputWire, Vector2 TerminalClearance);

public partial class LadderEditorCanvas
{
    internal TiaSweepGeometry ReviewTiaSweepGeometry(int rungIndex, TiaSweepTargetKind kind, string targetId)
    {
        var rung = _document.Rungs[rungIndex];
        var layout = TreeLayout(rung);
        Rect2 bounds;
        var input = false;
        var output = false;
        if (kind == TiaSweepTargetKind.Node)
        {
            var placed = layout.Nodes.SingleOrDefault(item => item.Node.Id == targetId)
                ?? throw new InvalidOperationException($"Target node '{targetId}' is absent from production layout.");
            bounds = ToCanvas(new Rect2(TreeOrigin(rungIndex) + new Vector2(placed.Bounds.X, placed.Bounds.Y),
                new Vector2(placed.Bounds.Width, placed.Bounds.Height)));
            // Flow ownership changes at a Series/Parallel boundary, so endpoint
            // continuity is geometric: a production wire must meet each side of
            // the measured placement on its continuity row.
            static bool Meets(LadderTreeWire wire, float x, float y) =>
                (Math.Abs(wire.X1 - x) < 0.01f && Math.Abs(wire.Y1 - y) < 0.01f)
                || (Math.Abs(wire.X2 - x) < 0.01f && Math.Abs(wire.Y2 - y) < 0.01f);
            if (placed.Node.Kind == LadderNodeKind.Parallel && placed.Node.Children is { Count: > 1 } paths)
            {
                var pathPlacements = paths.Select(path => layout.Nodes.Single(item => item.Node.Id == path.Id)).ToArray();
                input = pathPlacements.All(path => layout.Wires.Any(wire => Meets(wire, placed.Bounds.X, path.ContinuityY)));
                output = pathPlacements.All(path => layout.Wires.Any(wire => Meets(wire,
                    placed.Bounds.X + placed.Bounds.Width, path.ContinuityY)));
            }
            else
            {
                input = layout.Wires.Any(wire => Meets(wire, placed.Bounds.X, placed.ContinuityY));
                output = layout.Wires.Any(wire => Meets(wire, placed.Bounds.X + placed.Bounds.Width, placed.ContinuityY));
            }
        }
        else if (kind == TiaSweepTargetKind.Label)
        {
            bounds = ToCanvas(LabelBounds(rungIndex));
            input = output = layout.Wires.Count > 0;
        }
        else if (kind == TiaSweepTargetKind.Network)
        {
            bounds = GetTiaNetworkSelectionBounds(rungIndex);
            input = output = layout.Wires.Count > 0;
        }
        else
        {
            bounds = ToCanvas(TerminalSelectionBounds(rungIndex));
            input = output = layout.Wires.Count > 0;
        }
        var clearance = rung.HasTerminalAction ? GetTerminalConductorClearance(rungIndex) : Vector2.Zero;
        return new(GetTiaNetworkSelectionBounds(rungIndex), bounds, input, output, clearance);
    }

    internal IReadOnlyList<Rect2> ReviewTiaSweepFieldBounds(TiaSweepFixture fixture)
    {
        var regions = new List<Rect2>();
        foreach (var field in fixture.EditableFields)
        {
            if (field.Target == "edge-memory")
            {
                var placed = TreeLayout(_document.Rungs[fixture.RungIndex]).Nodes
                    .Single(item => item.Node.Id == fixture.TargetId);
                var origin = TreeOrigin(fixture.RungIndex);
                var centerX = origin.X + placed.Bounds.X + placed.Bounds.Width / 2;
                var baselineY = origin.Y + placed.ContinuityY + 32;
                regions.Add(ToCanvas(new Rect2(centerX - (placed.Bounds.Width - 8) / 2,
                    baselineY - 12, placed.Bounds.Width - 8, 22)));
            }
            else regions.Add(GetOperandEditorBounds(field.Operand, field.Target));
        }
        var rung = _document.Rungs[fixture.RungIndex];
        if (rung.IsCall)
        {
            var bounds = TerminalBounds(fixture.RungIndex);
            var inputCount = CallFormals(rung, input: true).Length;
            var outputCount = CallFormals(rung, input: false).Length;
            var centerY = TreeOutputPosition(fixture.RungIndex).Y;
            for (var i = 0; i < inputCount; i++)
                regions.Add(ToCanvas(new Rect2(bounds.Position.X - 126,
                    centerY - Math.Max(0, inputCount - 1) * TiaCallRowPitch / 2 + i * TiaCallRowPitch - 15, 116, 22)));
            for (var i = 0; i < outputCount; i++)
                regions.Add(ToCanvas(new Rect2(bounds.End.X + 10,
                    centerY - Math.Max(0, outputCount) * TiaCallRowPitch / 2 + i * TiaCallRowPitch - 15, 116, 22)));
        }
        return regions;
    }
}
