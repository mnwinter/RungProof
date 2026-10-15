using System;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;

namespace RungProof.Next.Workspace;

public enum SignalMappingRuntimeState
{
    LiveInput,
    LiveFeedback,
    AuthoredOnly,
}

public sealed record SignalMappingRuntimeStatus(
    SignalMappingRuntimeState State,
    string Label,
    string Detail
);

/// <summary>
/// Explicit bridge between authored catalog signals and simulator-side asset
/// controllers. A mapping is reported as live only when this adapter can apply
/// that exact signal. Catalog metadata alone never implies runtime behavior.
/// </summary>
public static class WorkspaceSignalRuntimeAdapter
{
    private const string BeltConveyorId = "material-handling.belt-conveyor.600x6000.v1";
    private const string RollerConveyorId = "material-handling.pallet-roller-conveyor.1000x4000.v1";

    public static void AttachSupportedController(AssetDefinition asset, Node3D model)
    {
        switch (asset.Id)
        {
            case BeltConveyorId when model.GetNodeOrNull<ConveyorController>("WorkspaceRuntimeAdapter") is null:
                model.AddChild(new ConveyorController { Name = "WorkspaceRuntimeAdapter" });
                break;
            case RollerConveyorId when model.GetNodeOrNull<RollerConveyorController>("WorkspaceRuntimeAdapter") is null:
                model.AddChild(new RollerConveyorController { Name = "WorkspaceRuntimeAdapter" });
                break;
        }
    }

    public static SignalMappingRuntimeStatus Apply(
        AssetDefinition asset,
        Node3D model,
        SignalDefinition signal,
        object? pointValue
    )
    {
        if (!signal.Direction.Equals("input", StringComparison.OrdinalIgnoreCase))
        {
            return new SignalMappingRuntimeStatus(
                SignalMappingRuntimeState.AuthoredOnly,
                "AUTHORED ONLY",
                "Asset output readback is not yet published into scene points."
            );
        }

        if (model.GetNodeOrNull<ConveyorController>("WorkspaceRuntimeAdapter") is { } conveyor)
        {
            return ApplyConveyor(conveyor, signal.Id, pointValue);
        }
        if (model.GetNodeOrNull<RollerConveyorController>("WorkspaceRuntimeAdapter") is { } rollers)
        {
            return ApplyRollerConveyor(rollers, signal.Id, pointValue);
        }

        return new SignalMappingRuntimeStatus(
            SignalMappingRuntimeState.AuthoredOnly,
            "AUTHORED ONLY",
            $"No verified runtime adapter exists for '{asset.Id}'."
        );
    }

    private static SignalMappingRuntimeStatus ApplyConveyor(
        ConveyorController controller,
        string signalId,
        object? value
    )
    {
        switch (signalId)
        {
            case "run_command": controller.RunCommand = Bool(value); break;
            case "estop_ok": controller.EstopOk = Bool(value); break;
            case "speed_setpoint": controller.SpeedSetpointMps = Number(value); break;
            default: return Unimplemented(signalId);
        }
        return Live(signalId);
    }

    private static SignalMappingRuntimeStatus ApplyRollerConveyor(
        RollerConveyorController controller,
        string signalId,
        object? value
    )
    {
        switch (signalId)
        {
            case "run_command": controller.RunCommand = Bool(value); break;
            case "estop_ok": controller.EstopOk = Bool(value); break;
            case "speed_setpoint": controller.SpeedSetpointMps = Number(value); break;
            default: return Unimplemented(signalId);
        }
        return Live(signalId);
    }

    private static SignalMappingRuntimeStatus Live(string signalId) => new(
        SignalMappingRuntimeState.LiveInput,
        "LIVE INPUT",
        $"Scene point currently drives verified asset input '{signalId}'."
    );

    private static SignalMappingRuntimeStatus Unimplemented(string signalId) => new(
        SignalMappingRuntimeState.AuthoredOnly,
        "AUTHORED ONLY",
        $"The runtime adapter does not implement asset input '{signalId}'."
    );

    private static bool Bool(object? value) => value switch
    {
        bool boolean => boolean,
        long integer => integer != 0,
        int integer => integer != 0,
        double number => Math.Abs(number) > 1e-9,
        float number => Math.Abs(number) > 1e-6f,
        string text when bool.TryParse(text, out var parsed) => parsed,
        _ => false,
    };

    private static float Number(object? value)
    {
        try
        {
            return Convert.ToSingle(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception) when (value is null or string or IConvertible)
        {
            return 0.0f;
        }
    }
}
