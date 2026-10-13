using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _demoReducedMotion;
    private MeshInstance3D? _demoReplacedFloor;
    private bool _demoOriginalFloorVisible;
    private MeshInstance3D? _demoStyledFloor;
    private Material? _demoOriginalFloorMaterial;

    private void RemoveDemoEnvironment()
    {
        RestoreDispatchLighting();
        if (_demoEnvironment is not null)
        {
            RemoveChild(_demoEnvironment);
            _demoEnvironment.QueueFree();
            _demoEnvironment = null;
        }
        if (_demoReplacedFloor is not null)
            _demoReplacedFloor.Visible = _demoOriginalFloorVisible;
        _demoReplacedFloor = null;
        if (_demoStyledFloor is not null)
            _demoStyledFloor.MaterialOverride = _demoOriginalFloorMaterial;
        _demoStyledFloor = null;
        _demoOriginalFloorMaterial = null;
    }

    private void AttachDemoEnvironment(string sceneId, Node3D processRoot, Vector3 cameraDirection)
    {
        if (_captureWithoutEnvironment) return;
        if (!SceneEnvironmentProfiles.TryGet(sceneId, out var profile))
        {
            GD.PushError($"SCENE_ENVIRONMENT_PROFILE_MISSING {sceneId}");
            return;
        }
        var meshes = ReviewMeshes(processRoot);
        if (meshes.Length == 0) return;
        var bounds = meshes.Select(ReviewBounds).Aggregate((a, b) => a.Merge(b));
        // Authored scene ground is Y=0. A submerged sump or hanging accessory
        // must not drag the entire room floor down to its mesh minimum.
        const float floorElevation = 0;
        // Conservative clearance around translational motion, not a collision
        // or continuous-clearance certificate. Process geometry stays untouched.
        foreach (var motion in processRoot.FindChildren("*", "", true, false).OfType<EquipmentMotionController>())
        {
            if (motion.Kind is not (EquipmentMotionController.MotionKind.LinearX
                or EquipmentMotionController.MotionKind.LinearY
                or EquipmentMotionController.MotionKind.PneumaticPusher
                or EquipmentMotionController.MotionKind.ScissorLift
                or EquipmentMotionController.MotionKind.RollerShutter
                or EquipmentMotionController.MotionKind.CartesianGantry)) continue;
            var parent = motion.GetParent<Node3D>();
            var equipmentMeshes = ReviewMeshes(parent);
            if (equipmentMeshes.Length == 0) continue;
            var equipmentBounds = equipmentMeshes.Select(ReviewBounds).Aggregate((a, b) => a.Merge(b));
            var margin = MathF.Abs(motion.TravelM);
            if (motion.Kind == EquipmentMotionController.MotionKind.CartesianGantry)
                margin += 0.45f + 0.12f;
            bounds = bounds.Merge(equipmentBounds.Grow(margin));
        }
        // Additional per-scene clearance for custom robot/vehicle/bridge paths.
        // This is presentation separation, not a certified motion envelope.
        if (profile.MotionMargin > 0) bounds = bounds.Grow(profile.MotionMargin);
        // Main's transform is identity in the application scene. Convert the
        // complete world AABB into this parent's coordinates for the sibling.
        var localBounds = GlobalTransform.AffineInverse() * bounds;
        var localFloor = (GlobalTransform.AffineInverse() * new Vector3(0, floorElevation, 0)).Y;
        Aabb? localAccess = null;
        var installation = processRoot.GetNodeOrNull<Node3D>("ControlInstallation");
        if (installation is not null && installation.HasMeta("operatorAccessBounds"))
            localAccess = GlobalTransform.AffineInverse() * (processRoot.GlobalTransform * installation.GetMeta("operatorAccessBounds").AsAabb());
        _demoEnvironment = DemoEnvironmentPresentation.Create(sceneId, localBounds, cameraDirection, localFloor, localAccess);
        if (_demoEnvironment is not null)
        {
            AddChild(_demoEnvironment);
            ApplyDispatchLook(processRoot, profile, sceneId == "scene-1-conveyor-stop", sceneId);
            // The application's 40m visual plane otherwise covers the authored
            // slab. This mesh has no collider. Preserve its prior visibility
            // and restore it when this particular environment is removed.
            _demoReplacedFloor = profile.ReplaceFloor ? GetNodeOrNull<MeshInstance3D>("Floor") : null;
            if (_demoReplacedFloor is not null)
            {
                _demoOriginalFloorVisible = _demoReplacedFloor.Visible;
                _demoReplacedFloor.Visible = false;
            }
            else
            {
                // Retain the exact existing plane and authored pits/water/levels;
                // only its dull global finish changes. No new surface covers a
                // process cutout, and the original material restores on departure.
                _demoStyledFloor = GetNodeOrNull<MeshInstance3D>("Floor");
                if (_demoStyledFloor is not null)
                {
                    _demoOriginalFloorMaterial = _demoStyledFloor.MaterialOverride;
                    var outdoor = profile.Family is SceneEnvironmentFamily.OutdoorTraffic or SceneEnvironmentFamily.OutdoorUtility;
                    _demoStyledFloor.MaterialOverride = CreateRetainedGroundFinish(profile, processRoot, outdoor);
                }
            }
        }
    }

    private void SetDemoReducedMotion(bool reduced)
    {
        _demoReducedMotion = reduced;
        if (reduced) _demoEnvironment?.SetAmbientMotionEnabled(false);
    }
}
