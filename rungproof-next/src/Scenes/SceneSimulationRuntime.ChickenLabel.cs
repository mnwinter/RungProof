using System;
using Godot;
using System.Linq;
namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasChickenLabelPlant => RuntimeType == "chickenLabel";
    private ChickenLabelPlantModel? _chicken;
    private Node3D? _chickenTray;
    private MeshInstance3D? _chickenPaper;
    private Label3D? _chickenText, _chickenWeight, _chickenInk;
    private double _chickenLoadElapsed;
    private Vector2 _chickenWeightScreenSize;
    private Vector3 _chickenPaperStart, _chickenPaperPrinted;
    private void ResetChickenLabelPlant()
    {
        if (!HasChickenLabelPlant) return;
        // The runtime owns actual paper and measured text;
        // Static demo props must not imply completion.
        foreach (var mesh in _sceneRoot.FindChildren("*", string.Empty, true, false).OfType<MeshInstance3D>())
        {
            var name = mesh.Name.ToString();
            if (name is "FOOD_TRAY_demo_label" or "FOOD_TRAY_demo_text" or "PRINTER_demo_paper"
                or "PRINTER_demo_paper_text" or "WEIGH_readout_static_text") mesh.Visible = false;
        }
        _chicken = new ChickenLabelPlantModel();
        _chickenLoadElapsed = 0;
        _chickenTray = _sceneRoot.GetNode<Node3D>("training_accessory_4");
        var printer = _sceneRoot.GetNode<Node3D>("training_accessory_6");
        var bounds = DeliveredEquipmentBounds(printer);
        // Use delivered world envelope;
        // This is an illustrative paper transfer, not fit certification.
        _chickenPaperStart = new Vector3(bounds.GetCenter().X, bounds.End.Y + .03f, bounds.End.Z);
        _chickenPaperPrinted = _chickenPaperStart + new Vector3(0, 0, .35f);
        _chickenPaper = _sceneRoot.GetNodeOrNull<MeshInstance3D>("ChickenLabelPaper");
        if (_chickenPaper is null)
        {
            _chickenPaper = new MeshInstance3D { Name = "ChickenLabelPaper", Mesh = new BoxMesh { Size = new Vector3(.42f, .008f, .28f) }, MaterialOverride = new StandardMaterial3D { AlbedoColor = Colors.White } };
            _sceneRoot.AddChild(_chickenPaper);
        }
        _chickenText = _chickenPaper.GetNodeOrNull<Label3D>("CapturedLabelText");
        if (_chickenText is null)
        {
            _chickenText = new Label3D { Name = "CapturedLabelText", FontSize = 28, PixelSize = .002f, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Position = new Vector3(0, .16f, .24f) };
            _chickenPaper.AddChild(_chickenText);
        }
        _chickenInk = _chickenPaper.GetNodeOrNull<Label3D>("PrintedInk");
        if (_chickenInk is null)
        {
            // Actual paper ink follows the physical sheet. The separate billboard
            // caption remains available for reading at normal scene scale.
            _chickenInk = new Label3D
            {
                Name = "PrintedInk",
                FontSize = 32,
                PixelSize = .0013f,
                Modulate = Colors.Black,
                OutlineSize = 0,
                RotationDegrees = new Vector3(-90, 0, 0),
                Position = new Vector3(0, .006f, 0)
            };
            _chickenPaper.AddChild(_chickenInk);
        }
        var weigh = _sceneRoot.GetNode<Node3D>("training_accessory_5");
        _chickenWeight = weigh.GetNodeOrNull<Label3D>("MeasuredWeight");
        if (_chickenWeight is null)
        {
            _chickenWeight = new Label3D { Name = "MeasuredWeight" };
            weigh.AddChild(_chickenWeight);
        }
        var screen = weigh.FindChild("WEIGH_readout_screen", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("Chicken measurement requires the retained scale screen");
        if (!screen.Visible || screen.Mesh is null)
            throw new InvalidOperationException("Chicken measurement requires a visible scale screen");
        var screenBounds = screen.GetAabb();
        _chickenWeightScreenSize = new Vector2(screenBounds.Size.X, screenBounds.Size.Y);
        var screenPose = screen.Transform;
        for (var parent = screen.GetParent(); parent != weigh; parent = parent.GetParent())
            if (parent is Node3D spatial) screenPose = spatial.Transform * screenPose;
        // Keep the existing child path. The screen's delivered outward face
        // is +Z; use its full relative transform so template poses still follow.
        _chickenWeight.Transform = screenPose * new Transform3D(Basis.Identity,
            new Vector3(screenBounds.GetCenter().X, screenBounds.GetCenter().Y, screenBounds.End.Z + .0015f));
        _chickenWeight.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
        _chickenWeight.FontSize = 36;
        _chickenWeight.OutlineSize = 0;
        _chickenWeight.Modulate = Colors.White;
        _chickenWeight.HorizontalAlignment = HorizontalAlignment.Center;
        _chickenWeight.VerticalAlignment = VerticalAlignment.Center;
        _chickenWeight.SetMeta("sourcePoint", "weight_kg");
        _chickenWeight.SetMeta("readoutScreen", "WEIGH_readout_screen");
        PublishChickenLabelPlant();
    }
    private bool LoadChickenProduct()
    {
        if (!HasChickenLabelPlant || _chicken is null || _chicken.Printing || _chicken.Applying) return false;
        _chicken.LoadProduct();
        _chickenLoadElapsed = 0;
        PublishChickenLabelPlant();
        ApplyBindings();
        StateChanged?.Invoke();
        return true;
    }
    private void AdvanceChickenLabelPlant(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (_chicken is null || !PlantPlaybackRunning) return;
        _chicken.Advance(seconds, AsBool(_points["print_request"]), Convert.ToString(_points["label_text"]) ?? "", AsBool(_points["apply_request"]), AsBool(_points["printer_ready"]) && AsBool(_points["label_data_valid"]));
        if (_chicken.ProductPresent) _chickenLoadElapsed += seconds;
        PublishChickenLabelPlant();
        ApplyBindings();
        StateChanged?.Invoke();
    }
    private void PublishChickenLabelPlant()
    {
        var m = _chicken!;
        SetPoint("product_present", m.ProductPresent);
        SetPoint("product_weighed", m.WeighStable);
        SetPoint("weight_kg", m.MeasuredKg);
        SetPoint("print_complete", m.PrintComplete);
        SetPoint("application_complete", m.ApplicationComplete);
        _chickenTray!.Position = new Vector3((float)(-2 + 2 * Math.Clamp(_chickenLoadElapsed / .3, 0, 1)), .9f, -1.3f);
        _chickenPaper!.Visible = m.Printing || m.PrintComplete;
        _chickenText!.Text = m.PrintedLabelText;
        _chickenInk!.Text = m.PrintedLabelText;
        var visibleTrayMeshes = _chickenTray.FindChildren("*", "MeshInstance3D", true, false)
            .OfType<MeshInstance3D>().Where(mesh => mesh.IsVisibleInTree()).ToArray();
        var trayBounds = visibleTrayMeshes.Length == 0
            ? DeliveredEquipmentBounds(_chickenTray)
            : visibleTrayMeshes[0].GlobalTransform * visibleTrayMeshes[0].GetAabb();
        foreach (var mesh in visibleTrayMeshes.Skip(1))
            trayBounds = trayBounds.Merge(mesh.GlobalTransform * mesh.GetAabb());
        var applied = new Vector3(trayBounds.GetCenter().X, trayBounds.End.Y + .004f, trayBounds.GetCenter().Z);
        _chickenPaper.GlobalPosition = m.Applying || m.ApplicationComplete ? _chickenPaperPrinted.Lerp(applied, (float)m.ApplyFraction) : _chickenPaperStart.Lerp(_chickenPaperPrinted, (float)m.PrintFraction);
        // Keep the readable exact-text caption beside the weighing station.
        // Its anchor belongs to the settled tray geometry, rather than the
        // moving sheet, so it cannot cover the real printed ink at application.
        // Preserve the child path used by the native checks and saved scene;
        // only the visual caption placement changes, never plant/job state.
        _chickenText.GlobalPosition = applied + new Vector3(-1f, .45f, 0);
        _chickenWeight!.Text = m.WeighStable ? FormattableString.Invariant($"MEASURED {m.MeasuredKg:F3} kg") : m.ProductPresent ? "WEIGHING..." : "LOAD PRODUCT";
        FitChickenWeightReadout();
    }

    private void FitChickenWeightReadout()
    {
        // Fit every existing state at one scale, including the actual measured
        // value. No text shortening, invented numeric binding or duplicate value.
        var label = _chickenWeight!;
        var font = label.Font ?? ThemeDB.FallbackFont;
        var texts = new[] { "LOAD PRODUCT", "WEIGHING...", "MEASURED 1.237 kg", label.Text };
        var sizes = texts.Select(text => font.GetStringSize(text, HorizontalAlignment.Left, -1, label.FontSize)).ToArray();
        var width = sizes.Max(size => size.X);
        var height = MathF.Max(sizes.Max(size => size.Y),
            font.GetStringSize("Mg", HorizontalAlignment.Left, -1, label.FontSize).Y);
        var available = _chickenWeightScreenSize - new Vector2(.02f, .02f);
        if (available.X <= 0 || available.Y <= 0 || width <= 0 || height <= 0)
            throw new InvalidOperationException("Chicken measurement screen has no usable text area");
        label.PixelSize = MathF.Min(.0018f, MathF.Min(available.X / width, available.Y / height));
        label.SetMeta("measuredInkWidth", width * label.PixelSize);
        label.SetMeta("measuredInkHeight", height * label.PixelSize);
    }
}
