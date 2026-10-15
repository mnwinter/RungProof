using System;
namespace RungProof.Next.Scenes;
/// <summary>Offline measurement, printer and applicator timing. PLC supplies exact label text;
/// this model does not calculate prices, format text or select controller phases.</summary>
public sealed class ChickenLabelPlantModel
{
    public bool ProductPresent { get; private set; }
    public bool WeighStable => ProductPresent && _settle >= .5;
    public double MeasuredKg => WeighStable ? _mass : 0;
    public bool Printing { get; private set; }
    public bool PrintComplete { get; private set; }
    public bool Applying { get; private set; }
    public bool ApplicationComplete { get; private set; }
    public double PrintFraction => Math.Clamp(_print / .6, 0, 1);
    public double ApplyFraction => Math.Clamp(_apply / .5, 0, 1);
    public string PrintedLabelText { get; private set; } = "";
    private double _mass, _settle, _print, _apply;
    private bool _previousPrint, _previousApply;
    public ChickenLabelPlantModel()
    {
        Reset();
    }
    public void Reset()
    {
        ProductPresent = false;
        _mass = 1.237;
        ClearJob();
    }
    public void LoadProduct(double massKg = 1.237)
    {
        if (!double.IsFinite(massKg) || massKg < 0) throw new ArgumentOutOfRangeException(nameof(massKg));
        if (Printing || Applying) throw new InvalidOperationException("Cannot replace an active print/application product.");
        ClearJob();
        _mass = massKg;
        ProductPresent = true;
    }
    public void UnloadProduct()
    {
        if (Printing || Applying) throw new InvalidOperationException("Cannot unload an active print/application product.");
        ProductPresent = false;
        ClearJob();
    }
    private void ClearJob()
    {
        _settle = _print = _apply = 0;
        Printing = PrintComplete = Applying = ApplicationComplete = false;
        PrintedLabelText = "";
        _previousPrint = _previousApply = false;
    }
    public void Advance(double deltaSeconds, bool printRequest, string labelText, bool applyRequest, bool printerReady)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        if (labelText is null || labelText.Length > 255) throw new ArgumentException("Label text must contain at most 255 characters.", nameof(labelText));
        bool printEdge = printRequest && !_previousPrint, applyEdge = applyRequest && !_previousApply;
        _previousPrint = printRequest;
        _previousApply = applyRequest;
        // Accept only feedback already true at the start of this interval. No queued edges.
        if (printEdge && WeighStable && printerReady && !Printing && !PrintComplete && !Applying && labelText.Trim().Length > 0)
        {
            Printing = true;
            PrintedLabelText = labelText;
        }
        if (applyEdge && ProductPresent && PrintComplete && !Printing && !Applying && !ApplicationComplete) Applying = true;
        if (ProductPresent) _settle = Math.Min(.5, _settle + deltaSeconds);
        if (Printing && printerReady)
        {
            _print = Math.Min(.6, _print + deltaSeconds);
            if (_print >= .6 - 1e-12)
            {
                _print = .6;
                Printing = false;
                PrintComplete = true;
            }
        }
        if (Applying)
        {
            _apply = Math.Min(.5, _apply + deltaSeconds);
            if (_apply >= .5 - 1e-12)
            {
                _apply = .5;
                Applying = false;
                ApplicationComplete = true;
            }
        }
    }
}
