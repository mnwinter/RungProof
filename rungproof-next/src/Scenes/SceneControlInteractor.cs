using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace RungProof.Next.Scenes;

/// <summary>
/// Finds only explicitly declared operator controls in a composed scene.
///
/// A scene control is opt-in: its equipment configuration must contain an
/// <c>action</c> value.  This keeps ordinary scene geometry, sensors, and
/// process equipment from becoming accidental commands, and routes accepted
/// clicks to the scene runtime rather than to any physical PLC transport.
/// </summary>
public sealed class SceneControlInteractor
{
    public sealed record Binding(
        string EquipmentId,
        string Label,
        string ActionId,
        Node3D Node,
        float FeedbackTravelM = 0.018f,
        string ReleaseWhenPoint = ""
    );
    private sealed record ActivePulse(Node3D Target, Transform3D AuthoredTransform, double ElapsedSeconds, float TravelM, Vector3 ParentTravelDirection);

    private readonly IReadOnlyList<Binding> _bindings;
    private readonly List<ActivePulse> _activePulses = [];
    private readonly Dictionary<Node3D, (Transform3D AuthoredTransform, string ReleaseWhenPoint, Vector3 ParentTravelDirection)> _latchedFeedback = [];

    public SceneControlInteractor(SceneDefinition scene, Node3D sceneRoot)
    {
        _bindings = scene.Equipment
            .SelectMany(equipment => CreateBindings(equipment, sceneRoot))
            .ToArray();
    }

    public int Count => _bindings.Count;
    public IReadOnlyList<Binding> Bindings => _bindings;

    /// <summary>
    /// Shows a brief, local mechanical press only after the runtime has
    /// accepted a command.  It is feedback for the simulator control, not a
    /// representation of a real device or safety circuit.
    /// </summary>
    public void PlayAcceptedFeedback(Binding binding)
    {
        // A declared zero travel delegates the pose to its point-driven
        // momentary adapter, avoiding competing animation clocks.
        if(binding.FeedbackTravelM<=0)return;
        var target = binding.Node.FindChild("KIN_pushbutton", true, false) as Node3D
            ?? binding.Node.FindChild("KIN_estop", true, false) as Node3D;
        // A numeric input/display is clickable, but its stand is fixed. Only
        // a modeled button member, or an explicitly bound individual mesh
        // such as the conveyor E-stop cap, has mechanical press travel.
        target ??= binding.Node as MeshInstance3D;
        if (target is null) return;

        // A second accepted click can arrive before release. Keep the first
        // authored home; recording the depressed pose would accumulate drift.
        var authored = _activePulses.FirstOrDefault(pulse => pulse.Target == target)?.AuthoredTransform
            ?? target.Transform;
        var travelDirection = PressDirectionInParent(target, binding.Node);
        _activePulses.RemoveAll(pulse => pulse.Target == target);

        if (binding.ReleaseWhenPoint.Length > 0)
        {
            if (!_latchedFeedback.ContainsKey(target))
                _latchedFeedback[target] = (authored, binding.ReleaseWhenPoint, travelDirection);
            var latched = _latchedFeedback[target];
            target.Transform = latched.AuthoredTransform
                .Translated(latched.ParentTravelDirection * binding.FeedbackTravelM);
            return;
        }

        _activePulses.Add(new ActivePulse(target, authored, 0.0, binding.FeedbackTravelM, travelDirection));
    }

    /// <summary>Advances accepted-control feedback while preserving its authored pose.</summary>
    public void AdvanceFeedback(double delta)
    {
        for (var index = _activePulses.Count - 1; index >= 0; index--)
        {
            var pulse = _activePulses[index];
            if (!GodotObject.IsInstanceValid(pulse.Target))
            {
                _activePulses.RemoveAt(index);
                continue;
            }

            var elapsed = pulse.ElapsedSeconds + delta;
            var depth = PressFraction(elapsed);
            // Press along the station face normal, expressed in the target's
            // parent coordinates. Imported mesh pivots can have rotated local
            // axes, so a hard-coded local Z translation may move a cap vertically.
            pulse.Target.Transform = pulse.AuthoredTransform.Translated(
                pulse.ParentTravelDirection * (pulse.TravelM * depth));
            if (elapsed < 0.28)
            {
                _activePulses[index] = pulse with { ElapsedSeconds = elapsed };
                continue;
            }
            pulse.Target.Transform = pulse.AuthoredTransform;
            _activePulses.RemoveAt(index);
        }
    }

    /// <summary>Releases latched controls only when their declared reset/permissive point is true.</summary>
    public void SynchronizeFeedback(IReadOnlyDictionary<string, object?> points)
    {
        foreach (var (target, latch) in _latchedFeedback.ToArray())
        {
            if (!points.TryGetValue(latch.ReleaseWhenPoint, out var value) || value is not true)
                continue;
            if (GodotObject.IsInstanceValid(target)) target.Transform = latch.AuthoredTransform;
            _latchedFeedback.Remove(target);
        }
    }

    public static Vector3 PressDirectionInParent(Node3D target, Node3D controlRoot)
    {
        // The operator-facing label's +Z points outward from the enclosure in
        // the imported station. Press opposite that normal, into the panel.
        // Fall back to the station frame for controls without a face label.
        var face = controlRoot.FindChild("OperatorFaceLabel", true, false) as Node3D;
        var inwardWorld = -(face?.GlobalBasis.Z ?? controlRoot.GlobalBasis.Z).Normalized();
        if (inwardWorld.LengthSquared() < 0.5f) inwardWorld = -controlRoot.GlobalBasis.Z.Normalized();
        var parent = target.GetParent() as Node3D;
        return parent is null ? inwardWorld : parent.GlobalBasis.Inverse() * inwardWorld;
    }

    /// <summary>Returns the front-most declared scene control under the pointer.</summary>
    public bool TryPick(Camera3D camera, Vector2 screenPosition, out Binding? binding)
    {
        var origin = camera.ProjectRayOrigin(screenPosition);
        var direction = camera.ProjectRayNormal(screenPosition).Normalized();
        Binding? closest = null;
        var nearestDistance = float.PositiveInfinity;
        foreach (var candidate in _bindings)
        {
            if (!candidate.Node.IsVisibleInTree())
                continue;
            foreach (var mesh in VisibleMeshes(candidate.Node))
            {
                // A union of head, mast and base bounds also includes the air
                // between them. Test each part instead, in its own coordinates,
                // so rotations do not inflate the clickable region either.
                if (MathF.Abs(mesh.GlobalBasis.Determinant()) < 0.0000001f) continue;
                var inverse = mesh.GlobalTransform.AffineInverse();
                var localOrigin = inverse * origin;
                // Do not normalize after transforming: t must stay in world
                // ray units so differently scaled controls sort correctly.
                var localDirection = inverse.Basis * direction;
                if (!TryRayAabb(localOrigin, localDirection, mesh.GetAabb(), out var distance)
                    || distance >= nearestDistance) continue;
                closest = candidate;
                nearestDistance = distance;
            }
        }
        binding = closest;
        return binding is not null;
    }

    private static IEnumerable<Binding> CreateBindings(SceneEquipment equipment, Node3D sceneRoot)
    {
        var action = Text(equipment.Config, "action");
        var node = sceneRoot.GetNodeOrNull<Node3D>(SafeNodeName(equipment.Id));
        if (node is null) yield break;
        if (action.Length > 0) yield return new Binding(equipment.Id, equipment.Label, action, node);

        if (equipment.Config.ValueKind != JsonValueKind.Object
            || !equipment.Config.TryGetProperty("sceneControls", out var controls)
            || controls.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }
        foreach (var control in controls.EnumerateArray())
        {
            var nodeName = Text(control, "nodeName");
            var controlAction = Text(control, "action");
            var target = nodeName.Length == 0 ? null : node.FindChild(nodeName, true, false) as Node3D;
            if (target is null || controlAction.Length == 0) continue;
            yield return new Binding(
                equipment.Id,
                Text(control, "label") is { Length: > 0 } label ? label : equipment.Label,
                controlAction,
                target,
                (float)Number(control, "feedbackTravelM", 0.018),
                Text(control, "releaseWhenPoint")
            );
        }
    }

    private static IEnumerable<MeshInstance3D> VisibleMeshes(Node3D root)
    {
        var meshes = root is MeshInstance3D self ? new[] { self }
            .Concat(root.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            : root.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>();
        return meshes.Where(mesh => mesh.Mesh is not null && mesh.IsVisibleInTree());
    }

    private static bool TryRayAabb(Vector3 origin, Vector3 direction, Aabb bounds, out float distance)
    {
        var tNear = 0.0f;
        var tFar = float.PositiveInfinity;
        var minimum = bounds.Position;
        var maximum = bounds.End;
        for (var axis = 0; axis < 3; axis++)
        {
            var rayDirection = direction[axis];
            if (MathF.Abs(rayDirection) < 0.00001f)
            {
                if (origin[axis] < minimum[axis] || origin[axis] > maximum[axis])
                {
                    distance = 0.0f;
                    return false;
                }
                continue;
            }
            var first = (minimum[axis] - origin[axis]) / rayDirection;
            var second = (maximum[axis] - origin[axis]) / rayDirection;
            if (first > second) (first, second) = (second, first);
            tNear = MathF.Max(tNear, first);
            tFar = MathF.Min(tFar, second);
            if (tNear > tFar)
            {
                distance = 0.0f;
                return false;
            }
        }
        distance = tNear;
        return true;
    }

    private static string Text(JsonElement config, string name) =>
        config.ValueKind == JsonValueKind.Object
        && config.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static double Number(JsonElement config, string name, double fallback) =>
        config.ValueKind == JsonValueKind.Object
        && config.TryGetProperty(name, out var value)
        && value.TryGetDouble(out var number)
            ? number
            : fallback;

    private static string SafeNodeName(string value) => value.Replace("-", "_", StringComparison.Ordinal);

    private static float PressFraction(double elapsedSeconds)
    {
        const double pressSeconds = 0.055;
        const double holdUntilSeconds = 0.135;
        const double releaseUntilSeconds = 0.28;
        if (elapsedSeconds <= pressSeconds) return EaseInOut((float)(elapsedSeconds / pressSeconds));
        if (elapsedSeconds <= holdUntilSeconds) return 1.0f;
        if (elapsedSeconds >= releaseUntilSeconds) return 0.0f;
        return 1.0f - EaseInOut((float)((elapsedSeconds - holdUntilSeconds) / (releaseUntilSeconds - holdUntilSeconds)));
    }

    private static float EaseInOut(float value) => value * value * (3.0f - 2.0f * value);
}
