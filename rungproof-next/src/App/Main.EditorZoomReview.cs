using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    /// <summary>Scripted native routing checks; physical pointer/keyboard acceptance is separate.</summary>
    private async void CaptureEditorZoomReview(string[] arguments)
    {
        try
        {
            string Option(string name, string fallback) => arguments.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.Ordinal))?.Split('=', 2)[1] ?? fallback;
            var requestedStyle = Option("--review-style", "tia");
            var requestedZoom = int.Parse(Option("--review-zoom", "100"));
            _ = new LadderEditorZoomGeometry(requestedZoom);
            var fixture = Option("--review-fixture", "nested");
            async Task Frames() { await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
            void Check(bool condition, string name)
            {
                GD.Print($"EDITOR_ZOOM_CHECK {name}={condition}");
                if (!condition) throw new InvalidOperationException("Zoom check failed: " + name);
            }
            foreach (var style in new[] { "tia", "logix" })
            {
                var document = RecursiveEditorReviewDocument("nested");
                var layer = new CanvasLayer { Name = "EditorZoomReview", Layer = 100 };
                var background = new PanelContainer(); background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
                layer.AddChild(background); AddChild(layer);
                var column = new VBoxContainer(); background.AddChild(column);
                var title = new Label { Text = "PRODUCTION LADDER ZOOM · " + style + " · scripted target/reveal checks" }; column.AddChild(title);
                var scroll = new ScrollContainer { Name = "RoutineView", SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                var canvas = new LadderEditorCanvas(style == "tia" ? LadderVendorStyle.SiemensTia : LadderVendorStyle.RockwellLogix, document);
                var zoomRow = LadderEditorZoom.CreateControls(canvas, scroll); column.AddChild(zoomRow); column.AddChild(scroll); scroll.AddChild(canvas);
                var selector = zoomRow.GetNode<OptionButton>("LadderZoomSelector");
                canvas.RefreshDocument(); await Frames();
                var content = LadderEditorProjectJson.Save(document);
                var logicalWidth = canvas.CustomMinimumSize.X;
                foreach (var percent in new[] { 75, 100, 150 })
                {
                    var item = percent == 75 ? 0 : percent == 100 ? 1 : 2;
                    selector.Select(item); selector.EmitSignal(OptionButton.SignalName.ItemSelected, item); await Frames();
                    scroll.ScrollHorizontal = scroll.ScrollVertical = 0; await Frames();
                    Check(canvas.ZoomPercent == percent, style + percent + "SelectorApplied");
                    Check(Math.Abs(canvas.CustomMinimumSize.X - logicalWidth * percent / 100f) < 0.1f, style + percent + "ScaledScrollExtent");
                    Check(LadderEditorProjectJson.Save(document) == content, style + percent + "DocumentUnchanged");
                    var branch = document.Rungs[0].Branches.FindIndex(b => b.Id == "upper-path");
                    canvas._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = canvas.GetElementPosition(0, branch, 0) });
                    Check(canvas.SelectedNodeId == "start", style + percent + "LeafPointerIdentity");
                    var wire = canvas.GetInsertionPointPosition(0, branch, 0);
                    canvas._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = wire });
                    var target = canvas.CaptureTreeTarget();
                    Check(target.ParentId == "upper-path" && target.Slot == 0, style + percent + "WirePointerIdentity");
                    Check(canvas.PreviewInsertionAt(wire) && canvas.CaptureTreeTarget().Slot == 0, style + percent + "DragPreviewIdentity");
                    canvas.SelectTreeNode(0, "root"); canvas.GrabFocus();
                    canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.End }); await Frames();
                    Check(canvas.OutputSelected, style + percent + "KeyboardTerminal");
                    LadderEditorZoom.RevealSelection(canvas, scroll); await Frames();
                    var bounds = canvas.GetSelectionBounds();
                    Check(bounds.Position.X >= scroll.ScrollHorizontal && bounds.End.X <= scroll.ScrollHorizontal + scroll.Size.X,
                        style + percent + "TerminalRevealed");
                    canvas._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, Position = canvas.GetElementPosition(0, -1, -1, true) });
                    Check(canvas.OutputSelected, style + percent + "OutputPointerIdentity");
                    if (percent == 100)
                    {
                        var contexts = 0; var exactOutputTarget = true;
                        canvas.ContextMenuRequested += (r, b, c, output) => { contexts++; exactOutputTarget &= r == 0 && b == -1 && c == -1 && output; };
                        canvas.GrabFocus();
                        canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.Menu });
                        Check(contexts == 1 && exactOutputTarget, style + "MenuKeyExactOutputTarget");
                        canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F10, ShiftPressed = true });
                        Check(contexts == 2 && exactOutputTarget, style + "MenuAndShiftF10SameTarget");
                    }
                    var viewport = new Rect2(Vector2.Zero, GetViewport().GetVisibleRect().Size);
                    Check(viewport.Encloses(selector.GetGlobalRect()) && selector.IsVisibleInTree(), style + percent + "SelectorVisible");
                    Check(canvas.GetRungBounds(0).Position.Y >= 89.9f, style + percent + "ChromeDoesNotOverlapFirstRung");
                }
                layer.QueueFree(); await Frames();
            }
            // Preserve one real rendered case for root visual inspection.
            var reviewDocument = RecursiveEditorReviewDocument(fixture);
            if (fixture == "timer-missing-pt")
                Check(reviewDocument.Rungs.Single().IsTimer && !reviewDocument.Rungs.Single().TimerPresetAssigned,
                    "TimerUnassignedPTFixture");
            var captureLayer = new CanvasLayer { Layer = 100 }; var capturePanel = new PanelContainer();
            capturePanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); captureLayer.AddChild(capturePanel); AddChild(captureLayer);
            var stack = new VBoxContainer(); capturePanel.AddChild(stack);
            stack.AddChild(new Label { Text = $"ACTUAL LADDER ZOOM · {requestedStyle} · {requestedZoom}% · {fixture}" });
            var captureScroll = new ScrollContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
            var captureCanvas = new LadderEditorCanvas(requestedStyle == "logix" ? LadderVendorStyle.RockwellLogix : LadderVendorStyle.SiemensTia, reviewDocument);
            T EnumOption<T>(string name, T fallback) where T : struct, Enum =>
                Enum.TryParse<T>(Option(name, string.Empty), true, out var parsed) ? parsed : fallback;
            captureCanvas.SetTiaDisplaySettings(
                EnumOption("--review-operand", TiaOperandRepresentation.Symbolic),
                EnumOption("--review-tag-info", TiaTagInformationDisplay.Hidden),
                EnumOption("--review-tag-position", TiaTagInformationPosition.OnOperands));
            var controls = LadderEditorZoom.CreateControls(captureCanvas, captureScroll); stack.AddChild(controls); stack.AddChild(captureScroll); captureScroll.AddChild(captureCanvas);
            captureCanvas.RefreshDocument(); await Frames();
            var captureSelector = controls.GetNode<OptionButton>("LadderZoomSelector");
            var captureItem = requestedZoom == 75 ? 0 : requestedZoom == 100 ? 1 : 2;
            captureSelector.Select(captureItem); captureSelector.EmitSignal(OptionButton.SignalName.ItemSelected, captureItem); await Frames();
            if (arguments.Contains("--review-terminal")) { captureCanvas.SelectElement(0, -1, -1, true); LadderEditorZoom.RevealSelection(captureCanvas, captureScroll); }
            else if (!arguments.Contains("--review-no-selection") && fixture is not ("tag-info" or "timer-missing-pt"))
            { captureCanvas.SelectTreeNode(0, "root"); captureScroll.ScrollHorizontal = captureScroll.ScrollVertical = 0; }
            if (arguments.Contains("--review-operand-entry") && reviewDocument.Rungs.Count > 0
                && reviewDocument.Rungs[0].Branches.Count > 0 && reviewDocument.Rungs[0].Branches[0].Contacts.Count > 0)
            {
                var outputTarget = arguments.Contains("--review-operand-output") && reviewDocument.Rungs[0].HasTerminalAction;
                var target = reviewDocument.Rungs[0].Branches[0].Contacts[0];
                if (outputTarget) captureCanvas.SelectElement(0, -1, -1, output: true);
                else captureCanvas.SelectTreeNode(0, target.Id);
                var operandEditor = new LadderOperandEditor(); captureCanvas.AddChild(operandEditor);
                var operand = outputTarget ? reviewDocument.Rungs[0].CoilVariable : target.Variable;
                operandEditor.Begin(operand, LadderOperandSymbols.ForDocument(reviewDocument), (_, _) => "Visual review only", () => { });
                operandEditor.PlaceInline(captureCanvas.GetOperandEditorBounds(operand));
            }
            await Frames();
            if (_capturePath is not null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var error = GetViewport().GetTexture().GetImage().SavePng(_capturePath);
                if (error != Error.Ok) throw new InvalidOperationException("Zoom screenshot failed: " + error);
                GD.Print("EDITOR_ZOOM_CAPTURE " + _capturePath);
            }
            GD.Print("EDITOR_ZOOM_REVIEW_PASS scripted=true physicalInput=false"); GetTree().Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
}
