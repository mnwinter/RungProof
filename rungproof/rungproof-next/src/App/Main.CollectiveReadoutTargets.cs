using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private readonly List<MeshInstance3D> _collectiveReadoutHeads = new();

    private List<Label3D>? PrepareCollectiveReadoutTargets()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--capture-collective-readouts", StringComparer.Ordinal)) return null;
        var root = _sceneCompositionRoot ?? throw new InvalidOperationException("Collective capture composition missing");
        var runtime = _sceneRuntime ?? throw new InvalidOperationException("Collective capture runtime missing");
        var shell = _simulatorShell ?? throw new InvalidOperationException("Collective capture shell missing");
        if (shell.IsExternalMode || _virtualController is not null || _virtualProgram is not null
            || (_externalConnection is not null && (_externalConnection.State.ToString() != "Disconnected" || _externalConnection.IsBusy))
            || runtime.IsRunning || _virtualController?.Snapshot.State == VirtualControllerState.Running
            || shell.IsChallengeDraftActive)
            throw new InvalidOperationException("Collective capture requires stopped local nonchallenge state");
        // Reset before the navigation routine takes its immutable field/point snapshots.
        ResetActiveController();
        string[] ids;
        int expectedLabels;
        switch (_currentSceneId)
        {
            case "lab-2-19-service-door":
                ids = new[] { "door_open_button", "door_stop_button", "door_close_button", "door_open_lamp", "door_closed_lamp" };
                expectedLabels = 5;
                break;
            case "lab-2-18-pallet-pickup":
                ids = new[] { "pickup_mode", "auto_start", "manual_jog" };
                expectedLabels = 3;
                break;
            case "lab-9-03-sum-and-counter-function":
                ids = new[] { "switch_1", "indicator_2", "switch_3", "switch_4", "indicator_5",
                    "numeric_display_0", "numeric_display_1", "numeric_display_2", "numeric_display_3" };
                expectedLabels = 13;
                break;
            default: throw new InvalidOperationException("Unsupported collective capture scene: " + _currentSceneId);
        }
        _collectiveReadoutHeads.Clear();
        var group = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture-readout-group=", StringComparison.Ordinal))?
            .Substring("--capture-readout-group=".Length);
        if (group is not null)
        {
            if (_currentSceneId != "lab-9-03-sum-and-counter-function")
                throw new InvalidOperationException("Readout groups require exact dense station");
            ids = group switch {
                "manual" => new[] { "switch_1", "switch_3", "switch_4" },
                "numeric" => new[] { "numeric_display_0", "numeric_display_1" },
                "results" => new[] { "numeric_display_2", "numeric_display_3" },
                "status" => new[] { "indicator_2", "indicator_5" },
                _ => throw new InvalidOperationException("Unknown dense station group")
            };
            expectedLabels = group is "numeric" or "results" ? 4 : ids.Length;
        }
        var labels = new List<Label3D>();
        var installation = root.GetNode<Node3D>("ControlInstallation");
        foreach (var id in ids)
        {
            var device = root.GetNode<Node3D>(id);
            Label3D identity;
            if (device.HasMeta("installationIdentification"))
                identity = installation.GetNode<Label3D>(device.GetMeta("installationIdentification").AsString() + "_Ink");
            else if (_currentSceneId == "lab-2-19-service-door" && id.EndsWith("_button", StringComparison.Ordinal))
                identity = device.GetNode<Label3D>("OperatorFaceLabel");
            else throw new InvalidOperationException("Required collective identity missing: " + id);
            if (!identity.IsVisibleInTree()) throw new InvalidOperationException("Collective identity hidden: " + id);
            labels.Add(identity);
            if (id.StartsWith("numeric_display_", StringComparison.Ordinal))
                labels.Add(device.GetNode<Label3D>("NumericReadout"));
            var meshes = device.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()
                .Where(m => m.IsVisibleInTree() && m.Mesh is not null && CollectiveHeadMesh(m)).ToArray();
            if (meshes.Length == 0) throw new InvalidOperationException("Collective head geometry missing: " + id);
            _collectiveReadoutHeads.AddRange(meshes);
            if (id == "pickup_mode")
            {
                // Explicit required selector parts: never accept a surviving housing alone.
                foreach (var name in new[] { "SELECTOR_bezel", "SELECTOR_dial_plate" })
                    if (device.FindChild(name, true, false) is not MeshInstance3D m || !m.IsVisibleInTree())
                        throw new InvalidOperationException("Required selector geometry missing: " + name);
                if (device.FindChild("KIN_selector_handle", true, false) is not Node3D handle
                    || !handle.GetChildren().OfType<MeshInstance3D>().Any(m => m.IsVisibleInTree()))
                    throw new InvalidOperationException("Required selector handle missing");
            }
        }
        if (labels.Count != expectedLabels || labels.Distinct().Count() != expectedLabels)
            throw new InvalidOperationException("Collective exact label-count contract failed");
        return labels;
    }

    private void VerifyCollectiveReadoutHeadBounds()
    {
        if (!OS.GetCmdlineUserArgs().Contains("--capture-collective-readouts", StringComparer.Ordinal)) return;
        var camera = _sceneCamera ?? throw new InvalidOperationException("Collective camera missing");
        var inset = (_simulatorShell ?? throw new InvalidOperationException("Collective shell missing")).SceneViewportRect().Grow(-12);
        if (_collectiveReadoutHeads.Count == 0) throw new InvalidOperationException("Collective head list empty");
        foreach (var mesh in _collectiveReadoutHeads)
        {
            var bounds = mesh.GetAabb();
            for (var i = 0; i < 8; i++)
            {
                var local = bounds.Position + new Vector3((i & 1) == 0 ? 0 : bounds.Size.X,
                    (i & 2) == 0 ? 0 : bounds.Size.Y, (i & 4) == 0 ? 0 : bounds.Size.Z);
                var world = mesh.GlobalTransform * local;
                if (camera.IsPositionBehind(world) || !inset.HasPoint(camera.UnprojectPosition(world)))
                    throw new InvalidOperationException("Collective visible head violates front/aperture margin: " + mesh.GetPath());
            }
        }
        GD.Print("COLLECTIVE_READOUT_HEAD_BOUNDS PASS meshes=" + _collectiveReadoutHeads.Count
            + " scope=projected-envelope-not-occlusion");
    }

    private static bool CollectiveHeadMesh(MeshInstance3D mesh)
    {
        // Retained head/housing geometry only; authored stands and conduits are
        // not the readability target. Match the installation helper's actual
        // named support families rather than estimating a height cutoff.
        var name = mesh.Name.ToString();
        return name is not ("FOOT" or "POLE")
            && !name.StartsWith("BASE_", StringComparison.Ordinal)
            && !name.StartsWith("PEDESTAL", StringComparison.Ordinal)
            && !name.StartsWith("CTRL_rear_conduit", StringComparison.Ordinal)
            && !name.EndsWith("_base", StringComparison.Ordinal)
            && !name.EndsWith("_mast", StringComparison.Ordinal)
            && !name.EndsWith("_foot", StringComparison.Ordinal);
    }
}

