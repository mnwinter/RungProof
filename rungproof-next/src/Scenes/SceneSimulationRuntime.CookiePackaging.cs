using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasCookiePackagingPlant => _definition.TryGetProperty("cookiePackagingPlant", out _);
    private CookiePackagingPlantModel? _cookiePlant;
    private Node3D[] _cookieTrays = [], _cookieFilms = [];
    private Node3D? _cookieHead;
    private ConveyorController? _cookieConveyor;

    private void ResetCookiePackagingPlant()
    {
        if (!HasCookiePackagingPlant) return;
        if (Text(_definition.GetProperty("cookiePackagingPlant"), "model", string.Empty) != "six-cookie-index-seal-v1")
            throw new InvalidOperationException("Unknown cookie packaging plant model.");
        foreach (var point in new[] { "product_present", "packaging_ready", "packaging_busy", "batch_complete", "count_beam_blocked", "packaging_fault" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Cookie plant requires PC-owned BOOL feedback '{point}'.");
        foreach (var point in new[] { "cookie_count", "wrapped_count" })
            if (_pointOwners.GetValueOrDefault(point) != "PC" || _pointTypes.GetValueOrDefault(point) != "INT")
                throw new InvalidOperationException($"Cookie plant requires PC-owned INT feedback '{point}'.");
        foreach (var point in new[] { "infeed_run", "packaging_enable" })
            if (_pointOwners.GetValueOrDefault(point) != "PLC" || _pointTypes.GetValueOrDefault(point) != "BOOL")
                throw new InvalidOperationException($"Cookie plant requires PLC-owned BOOL command '{point}'.");
        _cookiePlant ??= new CookiePackagingPlantModel(); _cookiePlant.Reset();
        _cookieTrays = Enumerable.Range(0, CookiePackagingPlantModel.Capacity).Select(i => _sceneRoot.GetNode<Node3D>($"cookie_{i}")).ToArray();
        _cookieFilms = _cookieTrays.Select(tray => tray.GetNode<Node3D>("COOKIE_package")).ToArray();
        _cookieHead = _sceneRoot.GetNode<Node3D>("machine_1").GetNode<Node3D>("KIN_cookie_seal_head");
        _cookieConveyor = Controllers(_sceneRoot.GetNode<Node3D>("conveyor_0")).OfType<ConveyorController>().Single();
        _cookieConveyor.ResetPlantTravel(); FreezeCookieAdapters(); ProjectCookiePlant();
    }

    private void FreezeCookieAdapters() => _cookieConveyor?.SetPhysicsProcess(false);
    private void PauseCookieClock() => _cookieConveyor?.ApplyPlantTravel(0, 0);

    private void AdvanceCookiePackagingPlant(double seconds)
    {
        if (_cookiePlant is null) return;
        var old = _cookiePlant.BeltDistance; var wasFaulted = _cookiePlant.Faulted;
        _cookiePlant.Step(seconds, AsBool(_points["infeed_run"]), AsBool(_points["packaging_enable"]));
        var distance = (float)(_cookiePlant.BeltDistance - old);
        _cookieConveyor!.ApplyPlantTravel(distance, seconds > 0 ? distance / (float)seconds : 0);
        ProjectCookiePlant();
        if (_cookiePlant.Faulted && !wasFaulted) GD.Print($"COOKIE_PACKAGING_FAULT {_cookiePlant.FaultReason}");
        ApplyBindings(); StateChanged?.Invoke();
    }

    private void ProjectCookiePlant()
    {
        if (_cookiePlant is null) return;
        for (var i = 0; i < _cookieTrays.Length; i++)
        {
            _cookieTrays[i].Position = new Vector3((float)_cookiePlant.X(i), (float)CookiePackagingPlantModel.SurfaceY, 0);
            _cookieFilms[i].Visible = _cookiePlant.IsWrapped(i);
        }
        _cookieHead!.Position = Vector3.Down * (float)(_cookiePlant.HeadFraction * CookiePackagingPlantModel.HeadStroke);
        SetPoint("product_present", _cookiePlant.ProductPresent); SetPoint("packaging_ready", _cookiePlant.Ready);
        SetPoint("packaging_busy", _cookiePlant.Busy); SetPoint("batch_complete", _cookiePlant.Complete);
        SetPoint("count_beam_blocked", _cookiePlant.BeamBlocked); SetPoint("packaging_fault", _cookiePlant.Faulted);
        SetPoint("cookie_count", (long)_cookiePlant.CookieCount); SetPoint("wrapped_count", (long)_cookiePlant.WrappedCount);
    }
}
