using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private sealed record DemoPresentationProfile(
        Vector3 CameraDirection,
        float FieldOfView,
        float FramingMargin,
        float DepthAllowance,
        IReadOnlySet<string> FocusEquipment);

    private static readonly IReadOnlyDictionary<string, DemoPresentationProfile> DemoPresentationProfiles =
        new Dictionary<string, DemoPresentationProfile>(StringComparer.Ordinal)
        {
            // Each lesson gets its own working viewpoint. These directions are
            // intentionally not five copies of the same elevated room corner.
            ["lab-4-01-press-count-lamp"] = new(
                // Close, near-level operator view. Frame the four working
                // devices, not the surrounding room or unused floor.
                new Vector3(0, .55f, 7.8f), 38f, 1.04f, .08f,
                new HashSet<string>(StringComparer.Ordinal)
                { "switch_0", "indicator_1", "switch_2", "training_accessory_3" }),
            ["lab-5-01-delayed-lamp"] = new(
                // Low opposite-side operator view. Keep every bench device in
                // the shot without reverting to another elevated room corner.
                new Vector3(3.3f, 1.05f, 8.4f), 35f, 1.05f, .12f,
                new HashSet<string>(StringComparer.Ordinal)
                { "switch_0", "indicator_1", "switch_2", "switch_3", "training_accessory_4" }),
            ["scene-2-conveyor-pusher"] = new(
                // Tight operator-height view: show the entry opening and
                // machinery from inside the room.  A shallow vertical angle
                // prevents the shot from looking over wall tops like a model.
                new Vector3(8.8f, .72f, 5.2f), 35f, 1.17f, .22f,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "scene2_conveyor", "scene2_product", "scene2_height_portal", "scene2_photoeye",
                    "scene2_pusher", "scene2_receiver", "scene2_main_exit", "scene2_side_exit",
                    "scene2_stacklight", "scene2_height_display", "scene2_completed_display",
                }),
            ["lab-9-11-pallet-counting"] = new(
                // Keep the operator at the inspection machine. The long infeed
                // conveyor may enter the shot, but it must not force a distant
                // room-wide view that makes the cabinet legends unreadable.
                // This lower, tighter lens keeps the active carton, machine,
                // and adjacent operator cabinet as the complete teaching shot.
                // Approach from the cabinet side so the new material-entry
                // wall reads face-on in the background. The former negative-X
                // view sat beside the wall plane and turned it into a large
                // foreground slab that obscured the conveyor.
                new Vector3(6.0f, 1.45f, 8.8f), 34f, 1.08f, .18f,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    // Passive pallet inventory and the long conveyor remain in
                    // context only when they naturally enter this close shot;
                    // they no longer determine the camera distance.
                    "box_1", "machine_2",
                }),
            ["lab-11-13-xy-palletizing"] = new(
                // Low cabinet-side view keeps the controls readable while the
                // infeed opening remains face-on behind the conveyor. This is
                // intentionally distinct from the prior elevated room-corner
                // view and from Demo 4's inspection-station composition.
                new Vector3(5.6f, 1.15f, 9.0f), 36f, 1.08f, .24f,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    // The long infeed and pickup support remain visible when
                    // they enter the working shot, but do not push the camera
                    // away from the gantry and operator cabinet.
                    "box_2", "training_accessory_4", "training_accessory_6",
                }),
            ["lab-11-19-powder-batch-mixer"] = new(
                // Work from the batch operator's side of the process. The
                // active feeder, mixer drive, valve, chute and consolidated
                // panel define the shot; unused room floor and outer sample
                // vessels must not force another miniature full-room view.
                new Vector3(6.4f, 1.1f, 8.0f), 35f, 1.10f, .18f,
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "tank_1", "motor_3", "valve_4", "training_accessory_6",
                    "training_accessory_7", "training_accessory_10",
                }),
        };

    private DemoPresentationProfile? GetDemoPresentationProfile()
    {
        var sceneId = (_activeSceneDefinition?.Id ?? _currentSceneId)?.Trim();
        return !string.IsNullOrEmpty(sceneId) && DemoPresentationProfiles.TryGetValue(sceneId, out var profile)
            ? profile : null;
    }
}
