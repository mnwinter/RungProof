using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Six original instructional settings. No context geometry participates in
    // timing, controller scans, lamp state, picking, or the authored reset.
    private bool TryBuildTeachingTimingSuite(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, Node3D architecture)
    {
        var delay = d.LayoutVariant == "delay-on-overhead-recess";
        var pulse = d.LayoutVariant == "timed-service-low-canopy";
        var flash = d.LayoutVariant == "flasher-side-service-bay";
        var alternate = d.LayoutVariant == "alternation-two-window-room";
        var rate = d.LayoutVariant == "flash-rate-corner-lightwell";
        var sequence = d.LayoutVariant == "running-tower-open-canopy";
        if (!delay && !pulse && !flash && !alternate && !rate && !sequence) return false;
        var bounds = b;
        Aabb? access = null;
        if (OperatorAccessBounds is { } a)
        {
            access = new Aabb(a.Position - Position, a.Size);
            bounds = bounds.Merge(access.Value);
        }
        var center = bounds.GetCenter();
        var cu = sx * center.X; var cv = sz * center.Z;
        var w = MathF.Max(alternate ? 8 : pulse ? 7.4f : sequence ? 7.2f : rate ? 6.8f : delay ? 5.8f : 5.6f, bounds.Size.X + 3.6f);
        var dep = MathF.Max(flash ? 8 : delay ? 7.2f : rate ? 6.6f : sequence ? 6.4f : alternate ? 5.8f : 5.4f, bounds.Size.Z + 3.6f);
        var l = cu - w / 2; var r = cu + w / 2;
        var f = cv - dep / 2; var v = cv + dep / 2;
        Vector3 At(float u, float y, float z) => new(sx * u, y, sz * z);
        var wall = EnvironmentDetailedSurface(delay ? "#A9BEC9" : pulse ? "#B28476" : flash ? "#D5D8D5" : alternate ? "#E0DAD1" : rate ? "#C7C7B9" : "#9F8171", "plaster");
        var feature = EnvironmentDetailedSurface(delay ? "#B79A6D" : pulse ? "#CAD2CF" : flash ? "#758092" : alternate ? "#91838D" : rate ? "#8A9678" : "#A7B0AD", delay ? "wood" : sequence || pulse ? "metal" : "paint");
        var floor = EnvironmentDetailedSurface(delay ? "#697B82" : pulse ? "#A6AAA6" : flash ? "#707A7D" : alternate ? "#A08E73" : rate ? "#B0AA9E" : "#A89F94", alternate ? "wood" : delay || flash ? "paint" : "concrete");
        var metal = EnvironmentDetailedSurface("#99A39F", "metal");
        var ivory = EnvironmentDetailedSurface("#E6E1D5", "paint");
        if (GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } ground) ground.MaterialOverride = floor;
        var extension = pulse ? 1.4f : rate ? 1 : 0;
        Box(architecture, "TimingSuiteContinuousFoundation", new Vector3(w + extension + .4f, .12f, dep + .4f), At(cu + extension / 2, -.07f, cv), floor);

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
        void Board(string name, float u, float z, string text, float width = 2.6f)
        {
            var board = EnvironmentDetailedSurface("#173E59", "paint");
            Box(architecture, name + "BearingPlate", new Vector3(width, 1.05f, .06f), At(u, 1.68f, z - .115f), board);
            architecture.AddChild(new Label3D
            {
                Name = name + "StaticLegend", Text = text,
                FontSize = 52, PixelSize = .00225f, OutlineSize = 5,
                OutlineModulate = new Color("#07141D"), Modulate = new Color("#FFF3B0"),
                Position = At(u, 1.68f, z - .151f), RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            });
        }
        var doorV = f + 1.1f;
        var routeU = r - .8f;
        if (delay)
        {
            var rearH = MathF.Max(3.5f, bounds.End.Y + .8f);
            // Lower paired daylight panes are visible from the teaching view;
            // a solid middle pier carries the reference rather than covering
            // glass. They are actual apertures, not bright painted rectangles.
            Wall("DelayDaylightLeft", l, v, cu - 1.2f, v, rearH, wall,
                window: (l + cu - 1.2f) / 2, ww: (w / 2 - 1.2f) - .3f, wy: 1.05f, wh: 1.65f);
            Wall("DelayReferencePier", cu - 1.2f, v, cu + 1.2f, v, rearH, wall);
            Wall("DelayDaylightRight", cu + 1.2f, v, r, v, rearH, wall,
                window: (cu + 1.2f + r) / 2, ww: (w / 2 - 1.2f) - .3f, wy: 1.05f, wh: 1.65f);
            Wall("DelayEntrySide", r, f, r, v, 2.9f, wall, doorV);
            // The roof ribbon remains in the clear strip behind the full B/A.
            Box(architecture, "DelayRearCeilingRibbon", new Vector3(w, .10f, .8f), At(cu, rearH, v - .4f), feature);
            // The learner rail already states the maintained two-second
            // objective. Keep the wall as architecture instead of repeating
            // those instructions on a second oversized sign behind the bench.
        }
        else if (pulse)
        {
            Wall("PulseRear", l, v, r, v, 2.8f, wall, window: cu + 1.8f, ww: 2, wy: .9f, wh: 1.4f);
            Wall("PulseSideAfterVestibule", r, f + 2.2f, r, v, 2.8f, wall,
                window: (f + 2.2f + v) / 2, ww: dep - 2.8f, wy: .8f, wh: 1.5f);
            Wall("PulseVestibuleBackReturn", r, f + 2.2f, r + 1.4f, f + 2.2f, 2.8f, feature);
            Wall("PulseVestibuleOuterEntry", r + 1.4f, f, r + 1.4f, f + 2.2f, 2.8f, feature, doorV);
            Wall("PulseVestibuleFrontReturn", r, f, r + 1.4f, f, 2.8f, feature);
            Board("MomentaryPulseReference", cu - .7f, v, "MOMENTARY START\nstart_pulse -> timed_lamp\nInterval remains PLC-owned");
            routeU = r + .7f;
        }
        else if (flash)
        {
            Wall("PeriodicRear", l, v, r, v, 3.1f, feature);
            var windowLo = f + 2.5f; var windowHi = v - .5f;
            Wall("PeriodicLongSide", r, f, r, v, 3.1f, wall, doorV, (windowLo + windowHi) / 2, windowHi - windowLo, 1.2f, 1.4f);
            Board("PeriodicModeReference", cu, v, "OFF / FLASH\nflash_mode_selected\nflash_lamp");
        }
        else if (alternate)
        {
            // Two genuine glazed facade bays with a solid central divider bay.
            Wall("AlternationRearLeft", l, v, cu - .65f, v, 2.9f, wall, window: (l + cu - .65f) / 2, ww: 1.8f, wy: 2.05f, wh: .65f);
            Wall("AlternationRearCenter", cu - .65f, v, cu + .65f, v, 2.9f, feature);
            Wall("AlternationRearRight", cu + .65f, v, r, v, 2.9f, wall, window: (cu + .65f + r) / 2, ww: 1.8f, wy: 2.05f, wh: .65f);
            Wall("AlternationEntrySide", r, f, r, v, 2.9f, wall, doorV);
            Wall("AlternationInternalDivider", cu, v - .7f, cu, v, 2.2f, feature);
            // References sit below glazing and cannot be mistaken for lamps.
            Board("ChannelAReference", cu + sx * sz * 2, v, "CHANNEL A\nlamp_a", 1.4f);
            Board("ChannelBReference", cu - sx * sz * 2, v, "CHANNEL B\nlamp_b", 1.4f);
        }
        else if (rate)
        {
            Wall("RateEntrySide", r, f, r, cv - .6f, 3.1f, wall, doorV);
            Wall("RateFirstStep", r, cv - .6f, r + 1, cv - .6f, 3.1f, feature);
            Wall("RateWideAlcoveSide", r + 1, cv - .6f, r + 1, cv + .9f, 3.1f, feature);
            Wall("RateSecondStep", r + .5f, cv + .9f, r + 1, cv + .9f, 3.1f, feature);
            Wall("RateNarrowAlcoveSide", r + .5f, cv + .9f, r + .5f, v, 3.1f, feature, window: (cv + .9f + v) / 2, ww: 1.2f, wy: 2.15f, wh: .65f);
            Wall("RateConnectedRear", l, v, r + .5f, v, 3.1f, wall);
            Board("RateSelectionReference", cu, v, "ENABLE + EXACTLY ONE RATE\nfast_rate_selected / slow_rate_selected\nBoth or neither: no rate request", 2.6f);
        }
        else
        {
            Wall("SequenceRear", l, v, r, v, 2.9f, wall);
            Wall("SequenceShortReturn", r, v - 1.8f, r, v, 2.9f, wall);
            var roofH = MathF.Max(3.5f, bounds.End.Y + .8f);
            foreach (var u in new[] { l + .2f, r - .2f })
            {
                Box(architecture, "SequenceRoofBearingPost", new Vector3(.18f, roofH, .18f), At(u, roofH / 2, v - .8f), metal);
                Box(architecture, "SequenceRoofGroundFoot", new Vector3(.32f, .04f, .32f), At(u, .02f, v - .8f), metal);
            }
            Box(architecture, "SequenceBearingHeader", new Vector3(w, .18f, .20f), At(cu, roofH - .09f, v - .8f), metal);
            Box(architecture, "SequenceOpenRearRoof", new Vector3(w, .10f, 1.1f), At(cu, roofH + .05f, v - .55f), feature);
            Board("SequenceReference", cu, v, "ENABLED STEP SEQUENCE\ntower_red -> tower_amber -> tower_green\nstep_pulse", 2.5f);
        }
        if (access is { } standing)
        {
            var target = standing.GetCenter(); target.Y = 0;
            target.Z = sz > 0 ? standing.Position.Z : standing.End.Z;
            var route = new Node3D { Name = "TimingSuiteActualStandingApproach" };
            var points = new Godot.Collections.Array<Vector3>();
            if (sequence)
                points.Add(At(r - .8f, 0, f + .8f));
            else if (pulse)
            {
                // Crossing the full open vestibule mouth keeps the swept
                // volume off both front/back returns and the entry jambs.
                points.Add(At(routeU, 0, doorV));
                points.Add(At(r - .8f, 0, doorV));
                points.Add(At(r - .8f, 0, f + .8f));
            }
            else { points.Add(At(routeU, 0, doorV)); points.Add(At(routeU, 0, f + .8f)); }
            points.Add(new Vector3(target.X, 0, sz * (f + .8f)));
            points.Add(target);
            route.SetMeta("localApproachPolyline", points);
            route.SetMeta("proposedSweepWidth", 1.2f);
            route.SetMeta("localTerminalStandingRegion", standing);
            architecture.AddChild(route);
        }
        architecture.SetMeta("purposeLayoutImplemented", true);
        architecture.SetMeta("teachingTimingExactLayout", d.LayoutVariant);
        architecture.SetMeta("teachingSuiteContextOnly", true);
        return true;
    }
}
