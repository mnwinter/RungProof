using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Counting and commissioning surroundings remain passive siblings. CNC
    // feedback, tower colors and counts belong exclusively to authored runtime.
    private bool TryBuildTeachingCounterSuite(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, Node3D architecture)
    {
        var count = d.LayoutVariant == "pulse-count-wide-rear-slot";
        var reset = d.LayoutVariant == "counter-reset-side-recess";
        var cnc = d.LayoutVariant == "cycle-counter-deep-service-bay";
        var tower = d.LayoutVariant == "sequence-four-color-open-return";
        var dual = d.LayoutVariant == "dual-count-paired-bays";
        var confirmation = d.LayoutVariant == "confirmation-offset-lightwell";
        if (!count && !reset && !cnc && !tower && !dual && !confirmation) return false;
        var bounds = b;
        Aabb? access = null;
        if (OperatorAccessBounds is { } a)
        {
            access = new Aabb(a.Position - Position, a.Size);
            bounds = bounds.Merge(access.Value);
        }
        var center = bounds.GetCenter();
        var cu = sx * center.X; var cv = sz * center.Z;
        var w = MathF.Max(cnc ? 9 : dual ? 8.4f : tower ? 8 : count ? 7.8f : confirmation ? 6.4f : 6, bounds.Size.X + 3.6f);
        var dep = MathF.Max(cnc || confirmation ? 8 : reset ? 7.6f : tower ? 7 : dual ? 6.2f : 5.6f, bounds.Size.Z + 3.6f);
        var l = cu - w / 2; var r = cu + w / 2;
        var f = cv - dep / 2; var v = cv + dep / 2;
        Vector3 At(float u, float y, float z) => new(sx * u, y, sz * z);
        var wall = EnvironmentDetailedSurface(count ? "#E0D9CB" : reset ? "#B7A392" : cnc ? "#A3AFB3" : tower ? "#899BA7" : dual ? "#D5D2C4" : "#C2B7AE", cnc || tower ? "metal" : "plaster");
        var feature = EnvironmentDetailedSurface(count ? "#B59972" : reset ? "#D4D5CB" : cnc ? "#D8DDDA" : tower ? "#A27D6A" : dual ? "#8C7963" : "#B9C5C2", count || dual ? "wood" : confirmation ? "metal" : "paint");
        var floor = EnvironmentDetailedSurface(count ? "#A79274" : reset ? "#92958E" : cnc ? "#757771" : tower ? "#B0AEA3" : dual ? "#8A9490" : "#757F81", count ? "wood" : dual || confirmation ? "paint" : "concrete");
        var metal = EnvironmentDetailedSurface("#929E9B", "metal");
        var ivory = EnvironmentDetailedSurface("#E5E2D6", "paint");
        if (GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } ground) ground.MaterialOverride = floor;
        var extraU = cnc ? 1.8f : 0;
        var extraV = reset ? 1.8f : confirmation ? 1.5f : count ? 1.4f : tower ? 1.3f : 0;
        Box(architecture, "CounterSuiteContinuousFoundation", new Vector3(w + extraU + .4f, .12f, dep + extraV + .4f), At(cu + extraU / 2, -.07f, cv + extraV / 2), floor);
        void Wall(string name, float u0, float z0, float u1, float z1, float h,
            StandardMaterial3D material, float? door = null, float? window = null,
            float ww = 1.4f, float wy = 1.25f, float wh = 1)
        {
            var alongX = MathF.Abs(z1 - z0) < .001f;
            var len = alongX ? MathF.Abs(u1 - u0) : MathF.Abs(z1 - z0);
            var middle = alongX ? (u0 + u1) / 2 : (z0 + z1) / 2;
            var sign = alongX ? sx : -sz;
            var node = new Node3D { Name = name, Position = At((u0 + u1) / 2, 0, (z0 + z1) / 2), RotationDegrees = new Vector3(0, alongX ? 0 : 90, 0) };
            architecture.AddChild(node);
            var holes = new List<Rect2>();
            float? dx = door.HasValue ? sign * (door.Value - middle) : null;
            if (dx.HasValue)
            {
                if (MathF.Abs(dx.Value) + .68f > len / 2) throw new InvalidOperationException("Timing suite doorway outside wall: " + name);
                holes.Add(new Rect2(dx.Value - .6f, 0, 1.2f, 2.2f));
            }
            if (window.HasValue)
            {
                var wx = sign * (window.Value - middle);
                if (MathF.Abs(wx) + ww / 2 + .06f > len / 2 || wy + wh > h)
                    throw new InvalidOperationException("Timing suite glazing outside wall: " + name);
                holes.Add(new Rect2(wx - ww / 2, wy, ww, wh));
            }
            ArtWall(node, len, h, .16f, material, holes, true);
            Box(node, "BearingCornice", new Vector3(len - .02f, .08f, .20f), new Vector3(0, h - .04f, 0), metal);
            void Strip(float lo, float hi)
            {
                if (hi - lo > .01f) Box(node, "SetbackSkirting", new Vector3(hi - lo, .10f, .18f), new Vector3((lo + hi) / 2, .05f, 0), metal);
            }
            if (dx.HasValue) { Strip(-len / 2 + .10f, dx.Value - .6f); Strip(dx.Value + .6f, len / 2 - .10f); }
            else Strip(-len / 2 + .10f, len / 2 - .10f);
        }
        void Board(string name, float u, float z, string text, float width = 2)
        {
            Box(architecture, name + "BearingPlate", new Vector3(width, .8f, .06f), At(u, 1.55f, z - .115f), ivory);
            architecture.AddChild(new Label3D { Name = name + "StaticLegend", Text = text, FontSize = 32, PixelSize = .0018f, OutlineSize = 0,
                Modulate = new Color("#39403B"), Position = At(u, 1.55f, z - .151f), RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0) });
        }
        var doorV = f + 1.1f;
        var routeU = r - .8f;
        Vector3? start = null;
        var h = cnc ? MathF.Max(4, bounds.End.Y + .8f) : tower ? MathF.Max(3.5f, bounds.End.Y + .8f) : 3;
        if (count)
        {
            Wall("CountLibraryBack", l, v + 1.4f, l + 2, v + 1.4f, h, feature);
            Wall("CountLibraryLeft", l, v, l, v + 1.4f, h, feature);
            Wall("CountLibraryRight", l + 2, v, l + 2, v + 1.4f, h, feature);
            Wall("CountMainRear", l + 2, v, r, v, h, wall, window: (l + 2 + r) / 2, ww: w - 2.6f, wy: 2.15f, wh: .7f);
            Wall("CountEntrySide", r, f, r, v, h, wall, doorV);
            Board("CountPulseReference", l + 1, v + 1.4f, "OPERATOR PULSES\npulse_received -> threshold_lamp", 1.75f);
            // Grounded passive teaching-library shelves. No stored live result.
            foreach (var y in new[] { .45f, .8f })
                Box(architecture, "LibrarySupportedShelf", new Vector3(1.7f, .07f, .3f), At(l + 1, y, v + 1.4f - .2f), feature);
            foreach (var u in new[] { l + .2f, l + 1.8f })
                Box(architecture, "LibraryShelfBearing", new Vector3(.10f, .85f, .30f), At(u, .425f, v + 1.4f - .2f), metal);
        }
        else if (reset)
        {
            Wall("ResetVestibuleBack", l, v + 1.8f, l + 1.8f, v + 1.8f, h, feature);
            Wall("ResetVestibuleOuterEntry", l, v, l, v + 1.8f, h, feature, v + .9f);
            Wall("ResetVestibuleRight", l + 1.8f, v, l + 1.8f, v + 1.8f, h, feature);
            Wall("ResetMainRear", l + 1.8f, v, r, v, h, wall);
            Wall("ResetConnectedSide", r, f, r, v, h, wall, window: cv, ww: 2, wy: 1.3f, wh: 1.2f);
            Board("ResetProcedureReference", l + .9f, v + 1.8f, "COUNT / RESET\npulse_received\nreset_pressed", 1.55f);
            routeU = l + .9f;
            start = At(routeU, 0, v + .9f);
        }
        else if (cnc)
        {
            Wall("CncMainRear", l, v, r + 1.8f, v, h, wall, window: cu - 1, ww: 2, wy: h - 1, wh: .6f);
            Wall("CncEntrySide", r, f, r, cv, h, wall, doorV);
            Wall("CncServiceWingFront", r, cv, r + 1.8f, cv, h, feature);
            Wall("CncServiceWingOuter", r + 1.8f, cv, r + 1.8f, v, h, feature);
            Box(architecture, "CncRearBearingGirder", new Vector3(w + 1.8f, .24f, .24f), At(cu + .9f, h - .15f, v - .08f), metal);
            Board("CncFeedbackOwnership", cu + 1.5f, v, "COMMAND / FEEDBACK / COUNT\nmachine_home / machine_busy\ncycle_done -> batch_count", 2.5f);
        }
        else if (tower)
        {
            Wall("SequenceBriefingBack", l, v + 1.3f, l + 2.4f, v + 1.3f, h, feature);
            Wall("SequenceBriefingLeft", l, v, l, v + 1.3f, h, feature);
            Wall("SequenceBriefingRight", l + 2.4f, v, l + 2.4f, v + 1.3f, h, feature);
            Wall("SequenceMainRear", l + 2.4f, v, r, v, h, wall);
            Wall("SequenceShortSide", r, cv, r, v, h, wall);
            Box(architecture, "SequenceBriefingBearingRoof", new Vector3(2.56f, .12f, 1.46f), At(l + 1.2f, h + .06f, v + .65f), metal);
            Board("FourColorReference", l + 1.2f, v + 1.3f, "STEP SEQUENCE\nRED / AMBER / GREEN / BLUE\nPLC owns tower outputs", 2.0f);
        }
        else if (dual)
        {
            Wall("DualCounterRearLeft", l, v, cu - .7f, v, h, wall, window: (l + cu - .7f) / 2, ww: 2, wy: 2.1f, wh: .65f);
            Wall("DualCounterCenter", cu - .7f, v, cu + .7f, v, h, feature);
            Wall("DualCounterRearRight", cu + .7f, v, r, v, h, wall, window: (cu + .7f + r) / 2, ww: 2, wy: 2.1f, wh: .65f);
            Wall("DualCounterEntrySide", r, f, r, v, h, wall, doorV);
            Wall("DualCounterInternalDivider", cu, v - .9f, cu, v, 2.15f, feature);
            foreach (var u in new[] { cu - 2, cu + 2 })
            {
                Box(architecture, "PassiveCounterReviewTable", new Vector3(1.4f, .07f, .5f), At(u, .91f, v - .40f), feature);
                foreach (var x in new[] { -.58f, .58f })
                    Box(architecture, "ReviewTableGroundLeg", new Vector3(.10f, .875f, .4f), At(u + x, .4375f, v - .40f), metal);
                Board(u < cu ? "ChannelAReference" : "ChannelBReference", u, v,
                    u < cu ? "CHANNEL A\nchannel_a_count" : "CHANNEL B\nchannel_b_count", 1.5f);
            }
        }
        else
        {
            Wall("ConfirmationMainRear", l, v, cu, v, h, wall);
            Wall("ConfirmationStepCheek", cu, v, cu, v + 1.5f, h, wall);
            Wall("ConfirmationOffsetRear", cu, v + 1.5f, r, v + 1.5f, h, wall);
            Wall("ConfirmationEntrySide", r, f, r, v + 1.5f, h, feature, doorV,
                v + .65f, 1.4f, .4f, 2.3f);
            Board("JointConfirmationReference", (cu + r) / 2, v + 1.5f,
                "INDEPENDENT COUNTERS\nbutton_a_count / button_b_count\nconfirmation_valid", 2.2f);
        }
        if (access is { } standing)
        {
            var target = standing.GetCenter(); target.Y = 0;
            target.Z = sz > 0 ? standing.Position.Z : standing.End.Z;
            var points = new Godot.Collections.Array<Vector3>();
            if (reset)
            {
                points.Add(start!.Value);
                points.Add(At(routeU, 0, v - .8f));
                // Cross in the rear clear strip, then use the same empty side
                // corridor. B/A determines actual clearance at native audit.
                points.Add(At(r - .8f, 0, v - .8f));
                points.Add(At(r - .8f, 0, f + .8f));
            }
            else if (tower) points.Add(At(r - .8f, 0, f + .8f));
            else { points.Add(At(routeU, 0, doorV)); points.Add(At(routeU, 0, f + .8f)); }
            points.Add(new Vector3(target.X, 0, sz * (f + .8f)));
            points.Add(target);
            var route = new Node3D { Name = "CounterSuiteActualStandingApproach" };
            route.SetMeta("localApproachPolyline", points);
            route.SetMeta("proposedSweepWidth", 1.2f);
            route.SetMeta("localTerminalStandingRegion", standing);
            architecture.AddChild(route);
        }
        architecture.SetMeta("purposeLayoutImplemented", true);
        architecture.SetMeta("teachingCounterExactLayout", d.LayoutVariant);
        architecture.SetMeta("teachingSuiteContextOnly", true);
        return true;
    }
}
