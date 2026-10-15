using System;
using Godot;

namespace RungProof.Next.Scenes;

/// <summary>Projects a symbolic selector ordinal onto its configured dial.</summary>
public partial class SelectorSwitchController : Node
{
    private Node3D _handle = null!;
    private int _positionCount;

    public void Configure(Node3D handle, int positionCount, int initialPosition)
    {
        _handle = handle;
        _positionCount = positionCount;
        SetPosition(initialPosition);
    }

    public static float DetentDegrees(int ordinal, int count) => 45f - 90f * ordinal / (count - 1);

    public void SetPosition(float ordinal)
    {
        // A corrupt input must not be silently substituted with a valid mode.
        if (!float.IsFinite(ordinal) || ordinal != MathF.Truncate(ordinal)
            || ordinal < 0 || ordinal >= _positionCount)
        {
            GD.PushWarning($"Selector ordinal {ordinal} is outside its {_positionCount}-position dial.");
            return;
        }
        // Blender's Y axis becomes Z in this delivered GLB. Its front face is
        // Godot X/Y; turning Y swings the handle out of that face.
        _handle.RotationDegrees = new Vector3(0, 0, DetentDegrees((int)ordinal, _positionCount));
    }
}
