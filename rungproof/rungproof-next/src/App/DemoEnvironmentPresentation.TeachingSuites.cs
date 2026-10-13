using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;

namespace RungProof.Next.App;

public partial class DemoEnvironmentPresentation
{
    // Resolve passive station boards after both sibling roots are attached.
    // Logical identity follows measured world order, never mirrored room u
    // coordinates or the screen projection. No field device is moved.
    public void ConfigureSharedCallBoardAssociation(SceneDefinition scene, Node3D processRoot)
    {
        if (scene.Id != "lab-2-04-two-station-call") return;
        if (SceneEnvironmentArtDirections.ForScene(scene.Id).LayoutVariant != "two-station-wide-return")
            throw new InvalidOperationException("Shared-call board layout contract drift");
        var stations = new[] {
            (Id: "north_call_button", Prefix: "NorthCallBoard", Face: "NORTH CALL", Point: "north_call_pressed", Action: "toggle-north-call"),
            (Id: "south_call_button", Prefix: "SouthCallBoard", Face: "SOUTH CALL", Point: "south_call_pressed", Action: "toggle-south-call"),
        };
        foreach (var station in stations)
        {
            var item = scene.Equipment.Single(e => e.Id == station.Id);
            var actions = scene.Simulation.GetProperty("actions").EnumerateArray()
                .Where(a => a.GetProperty("id").GetString() == station.Action).ToArray();
            var bindings = scene.Simulation.GetProperty("pointBindings").EnumerateArray()
                .Where(b => b.GetProperty("equipmentId").GetString() == station.Id).ToArray();
            var points = scene.Simulation.GetProperty("points").EnumerateArray()
                .Where(p => p.GetProperty("name").GetString() == station.Point).ToArray();
            if (item.Type != "switch" || item.Config.GetProperty("action").GetString() != station.Action
                || item.Config.GetProperty("faceLabel").GetString() != station.Face
                || actions.Length != 1 || actions[0].GetProperty("type").GetString() != "toggle"
                || actions[0].GetProperty("point").GetString() != station.Point
                || bindings.Length != 1 || bindings[0].GetProperty("point").GetString() != station.Point
                || bindings[0].GetProperty("mode").GetString() != "switch"
                || points.Length != 1 || points[0].GetProperty("type").GetString() != "BOOL"
                || points[0].GetProperty("owner").GetString() != "PC")
                throw new InvalidOperationException("Shared-call board source provenance drift: " + station.Id);
        }
        var ordered = stations.OrderBy(s => processRoot.GetNode<Node3D>(s.Id).GlobalPosition.X).ToArray();
        var stationX = ordered.Select(s => processRoot.GetNode<Node3D>(s.Id).GlobalPosition.X).ToArray();
        var architecture = GetNode<Node3D>("LocationArchitecture");
        var suffixes = new[] { "Backplate", "Instruction", "LightBracket", "SupportedFixture" };
        var boards = stations.Select(s => suffixes.Select(suffix =>
            architecture.GetNode<Node3D>(s.Prefix + suffix)).ToArray())
            .OrderBy(parts => parts[0].GlobalPosition.X).ToArray();
        if (!stationX.All(float.IsFinite) || MathF.Abs(stationX[1] - stationX[0]) < .001f
            || !boards.All(parts => float.IsFinite(parts[0].GlobalPosition.X))
            || MathF.Abs(boards[1][0].GlobalPosition.X - boards[0][0].GlobalPosition.X) < .001f)
            throw new InvalidOperationException("Shared-call board world association is ambiguous");
        // Temporary unique names prevent Godot sibling-name collision during
        // a real reassignment; final names, text and tags all agree.
        for (var i = 0; i < boards.Length; i++)
            for (var j = 0; j < suffixes.Length; j++) boards[i][j].Name = "CallAssociationPending_" + i + "_" + j;
        for (var i = 0; i < boards.Length; i++)
        {
            var station = ordered[i];
            for (var j = 0; j < suffixes.Length; j++)
            {
                var part = boards[i][j];
                part.Name = station.Prefix + suffixes[j];
                part.SetMeta("sourceEquipmentId", station.Id);
                part.SetMeta("sourcePoint", station.Point);
                part.SetMeta("sourceAction", station.Action);
                part.SetMeta("associationScope", "measured-world-x-order-passive-presentation");
                part.SetMeta("associatedControlWorldX", stationX[i]);
            }
            ((Label3D)boards[i][1]).Text = station.Face + "\n" + station.Point;
        }
    }

    // First bounded teaching-suite batch. Context remains an unbound sibling of
    // the authored plant. Mirroring changes rear-corner signs, never world yaw.
    private bool TryBuildTeachingSuite(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, Node3D architecture)
    {
        if (TryBuildUtilityTeachingSite(d, b, sx, sz, architecture)) return true;
        if (TryBuildServiceTeachingSite(d, b, sx, sz, architecture)) return true;
        if (TryBuildTeachingCounterSuite(d, b, sx, sz, architecture)) return true;
        if (TryBuildTeachingTimingSuite(d, b, sx, sz, architecture)) return true;
        if (TryBuildAdditionalTeachingSuite(d, b, sx, sz, architecture)) return true;
        var handoff = d.LayoutVariant == "handoff-split-window";
        var sharedCall = d.LayoutVariant == "two-station-wide-return";
        var sum = d.LayoutVariant == "sum-horizontal-light-slot";
        if (!handoff && !sharedCall && !sum) return false;

        // Access metadata is in the parent's coordinates, while b is already
        // environment-local. Context must clear both without shrinking either.
        var protectedBounds = b;
        if (OperatorAccessBounds is { } worldAccess)
            protectedBounds = protectedBounds.Merge(new Aabb(worldAccess.Position - Position, worldAccess.Size));
        var c = protectedBounds.GetCenter();
        var cu = sx * c.X;
        var cv = sz * c.Z;
        var width = MathF.Max(sharedCall ? 7.4f : sum ? 7.2f : 6.4f, protectedBounds.Size.X + 2.2f);
        var depth = MathF.Max(sharedCall ? 5.4f : sum ? 5.2f : 5.6f, protectedBounds.Size.Z + 2.2f);
        var left = cu - width / 2;
        var right = cu + width / 2;
        var front = cv - depth / 2;
        var rear = cv + depth / 2;
        const float h = 2.8f;
        var wall = EnvironmentSurface(sum ? "CAD9E0" : sharedCall ? "CEDDD5" : "D3DEDB", "paint");
        var floor = EnvironmentSurface(sum ? "8D9FA8" : sharedCall ? "879E93" : "859B9B", "concrete");
        var trim = EnvironmentSurface(sum ? "4C697A" : sharedCall ? "526F61" : "557477", "metal");
        var board = EnvironmentSurface("ECF0EE", "paint");
        // The existing large slab is the visible finish. Our lower extension
        // carries outward bays without a competing coplanar finish surface.
        if (GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } visibleGround)
            visibleGround.MaterialOverride = floor;
        Vector3 At(float u, float y, float v) => new(sx * u, y, sz * v);

        // A continuous full footprint covers every outward recess and support.
        var rearExtension = handoff ? 1.0f : sharedCall ? .9f : 1.1f;
        var sideExtension = sum ? .9f : 0;
        Box(architecture, "TeachingSuiteContinuousFoundation",
            new Vector3(width + sideExtension + .4f, .12f, depth + rearExtension + .4f),
            At(cu + sideExtension / 2, -.07f, cv + rearExtension / 2), floor);

        // A real aperture is constructed with ArtWall's hole subtraction and
        // ArchitecturalPortal metadata. Window glass is never emitted in doors.
        void Wall(string name, float u0, float v0, float u1, float v1,
            float height, float? doorCenter = null, float? windowCenter = null, float windowWidth = 1.3f)
        {
            var alongX = Math.Abs(v1 - v0) < .001f;
            var length = alongX ? Math.Abs(u1 - u0) : Math.Abs(v1 - v0);
            var midpoint = alongX ? (u0 + u1) / 2 : (v0 + v1) / 2;
            var orientation = alongX ? sx : -sz;
            var node = new Node3D
            {
                Name = name, Position = At((u0 + u1) / 2, 0, (v0 + v1) / 2),
                RotationDegrees = new Vector3(0, alongX ? 0 : 90, 0),
            };
            architecture.AddChild(node);
            var holes = new List<Rect2>();
            if (doorCenter.HasValue)
            {
                var x = orientation * (doorCenter.Value - midpoint);
                if (Math.Abs(x) + .6f > length / 2 - .08f)
                    throw new InvalidOperationException("Teaching suite door outside retained wall: " + name);
                holes.Add(new Rect2(x - .6f, 0, 1.2f, 2.2f));
            }
            if (windowCenter.HasValue)
            {
                var x = orientation * (windowCenter.Value - midpoint);
                if (Math.Abs(x) + windowWidth / 2 + .06f > length / 2)
                    throw new InvalidOperationException("Teaching suite window outside retained wall: " + name);
                holes.Add(new Rect2(x - windowWidth / 2, sum ? 2.05f : 1.25f, windowWidth, sum ? .65f : 1.0f));
            }
            ArtWall(node, length, height, .16f, wall, holes, true);
            Box(node, "ConnectedWallCornice", new Vector3(length + .16f, .12f, .24f),
                new Vector3(0, height - .06f, 0), trim);
            // Split skirting around the true doorway rather than bridging it.
            if (!doorCenter.HasValue)
                Box(node, "ContinuousWallSkirting", new Vector3(MathF.Max(.01f, length - .20f), .12f, .19f),
                    new Vector3(0, .06f, 0), trim);
            else
                foreach (var sideSign in new[] { -1f, 1f })
                {
                    var doorX = orientation * (doorCenter.Value - midpoint);
                    // Set back exposed strip ends at perpendicular wall joins.
                    // This removes coplanar top overlap instead of concealing it.
                    var edge = sideSign < 0 ? -length / 2 + .10f : length / 2 - .10f;
                    var jamb = doorX + sideSign * .6f;
                    var piece = Math.Abs(edge - jamb);
                    if (piece > .01f)
                        Box(node, "EntrySkirtingReturn", new Vector3(piece, .12f, .19f),
                            new Vector3((edge + jamb) / 2, .06f, 0), trim);
                }
        }

        // Static lesson-support boards use actual authored tag names. They add
        // no I/O or simulated machine. The fascia/lighting has real bearing.
        void Instruction(string name, float u, float v, string text, float boardWidth)
        {
            Box(architecture, name + "Backplate", new Vector3(boardWidth, .82f, .06f),
                At(u, 1.65f, v - .11f), board);
            architecture.AddChild(new Label3D
            {
                Name = name + "Instruction", Text = text, FontSize = 34,
                PixelSize = .0019f, OutlineSize = 0, Modulate = new Color("29424A"),
                Position = At(u, 1.65f, v - .148f),
                RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0),
            });
            Box(architecture, name + "LightBracket", new Vector3(.13f, .20f, .25f),
                At(u, 2.42f, v - .13f), trim);
            Box(architecture, name + "SupportedFixture", new Vector3(boardWidth * .72f, .09f, .26f),
                At(u, 2.34f, v - .17f), board);
        }

        // The side entrance is on a retained full wall and its approach stays
        // in the outer side strip, outside the actual process/access rectangle.
        var sideDoor = front + 1.1f;
        var route = new Node3D { Name = "TeachingSuiteIllustrativeApproach" };
        route.SetMeta("localRouteReserve", new Aabb(
            new Vector3(MathF.Min(sx * (right - .75f), sx * (right - .15f)), 0,
                MathF.Min(sz * front, sz * rear)),
            new Vector3(.60f, 2.0f, depth)));
        if (sharedCall)
            route.SetMeta("localRouteReserve", new Aabb(
                new Vector3(MathF.Min(sx * left, sx * right), 0,
                    MathF.Min(sz * (rear - .65f), sz * (rear - .05f))),
                new Vector3(width, 2.0f, .60f)));
        route.SetMeta("routeScope", sharedCall
            ? "Illustrative rear shared-entry corridor; continue around the outer side to existing bench standing region. Native full-route checks required."
            : "Illustrative side-entry approach; continue along open front to existing bench standing region. Native full-route checks required.");
        architecture.AddChild(route);

        if (handoff)
        {
            // One deep, connected witness recess; two observation roles share
            // a single room. Outward cheek walls join its back and main wall.
            Wall("HandoffRearLeft", left, rear, cu - 1.3f, rear, h);
            Wall("HandoffRecessLeftReturn", cu - 1.3f, rear, cu - 1.3f, rear + 1, h);
            Wall("HandoffWitnessBack", cu - 1.3f, rear + 1, cu + 1.3f, rear + 1, h);
            Wall("HandoffRecessRightReturn", cu + 1.3f, rear, cu + 1.3f, rear + 1, h);
            Wall("HandoffRearRight", cu + 1.3f, rear, right, rear, h);
            Wall("HandoffSide", right, front, right, rear, h, sideDoor,
                cv + .5f, 1.2f);
            Instruction("OperatorWitness", cu - .65f, rear + 1,
                "OPERATOR\noperator_confirmed", 1.14f);
            Instruction("QualityWitness", cu + .65f, rear + 1,
                "QUALITY\nquality_confirmed", 1.14f);
        }
        else if (sharedCall)
        {
            // Twin connected outward bays, with a usable shared personnel
            // opening between them. This changes actual building footprint.
            var centers = new[] { cu - 1.65f, cu + 1.65f };
            var cursor = left;
            foreach (var bayCenter in centers)
            {
                var lo = bayCenter - .8f;
                var hi = bayCenter + .8f;
                Wall("SharedCallMainRear", cursor, rear, lo, rear, h,
                    cursor < cu && lo > cu ? cu : null);
                Wall("CallBayLeftReturn", lo, rear, lo, rear + .9f, h);
                Wall("CallBayBack", lo, rear + .9f, hi, rear + .9f, h);
                Wall("CallBayRightReturn", hi, rear, hi, rear + .9f, h);
                Instruction(bayCenter < cu ? "NorthCallBoard" : "SouthCallBoard", bayCenter, rear + .9f,
                    bayCenter < cu ? "NORTH CALL\nnorth_call_pressed" : "SOUTH CALL\nsouth_call_pressed", 1.4f);
                cursor = hi;
            }
            Wall("SharedCallFinalRear", cursor, rear, right, rear, h);
            Wall("SharedCallConnectedSide", right, front, right, rear, h, null, cv, 1.5f);
        }
        else
        {
            // L-shaped instrumentation envelope: a long raised clerestory back
            // and an outward side gallery, all on one connected foundation.
            Wall("SumRearLeftStep", left, rear + 1.1f, cu - .8f, rear + 1.1f, 3.2f,
                null, (left + cu - .8f) / 2, 1.6f);
            Wall("SumRearStepReturn", cu - .8f, rear, cu - .8f, rear + 1.1f, 3.2f);
            Wall("SumRearRight", cu - .8f, rear, right, rear, 3.2f,
                null, (cu - .8f + right) / 2, 2.0f);
            Wall("SumSideEntry", right, front, right, cv + .5f, h, sideDoor);
            Wall("SumGalleryFrontReturn", right, cv + .5f, right + .9f, cv + .5f, h);
            Wall("SumGalleryOuterSide", right + .9f, cv + .5f, right + .9f, rear, h);
            Wall("SumGalleryRearReturn", right, rear, right + .9f, rear, h);
            Instruction("SumOperandReference", (left + cu - .8f) / 2, rear + 1.1f,
                "SUM INSTRUMENTATION\noperand_a + operand_b\n= sum_result", 2.0f);
            // Close the stepped rear-left cutaway edge with a short connected
            // return; this is a finished exposed wall end, not an isolated panel.
            Wall("SumRearLeftCorner", left, rear, left, rear + 1.1f, 3.2f);
        }
        architecture.SetMeta("purposeLayoutImplemented", true);
        architecture.SetMeta("teachingSuiteExactLayout", d.LayoutVariant);
        architecture.SetMeta("teachingSuiteContextOnly", true);
        return true;
    }

    // Separate construction path preserves the first three accepted candidates.
    // These are instructional surroundings, never additional operating equipment.
    private bool TryBuildAdditionalTeachingSuite(SceneEnvironmentArtDirection d, Aabb b,
        int sx, int sz, Node3D architecture)
    {
        var marker = d.LayoutVariant == "service-marker-recess";
        var lighting = d.LayoutVariant == "lighting-high-side-slot";
        var attention = d.LayoutVariant == "attention-service-recess";
        var contacts = d.LayoutVariant == "contacts-corner-portal";
        var beacon = d.LayoutVariant == "beacon-tall-single-recess";
        var product = d.LayoutVariant == "product-deep-side-window";
        if (!marker && !lighting && !attention && !contacts && !beacon && !product) return false;
        var bounds = b;
        Aabb? access = null;
        if (OperatorAccessBounds is { } worldAccess)
        {
            access = new Aabb(worldAccess.Position - Position, worldAccess.Size);
            bounds = bounds.Merge(access.Value);
        }
        var c = bounds.GetCenter();
        var cu = sx * c.X; var cv = sz * c.Z;
        var width = MathF.Max(attention ? 6.6f : contacts ? 6.2f : beacon ? 6 : marker ? 5.8f : 5.6f, bounds.Size.X + 3.2f);
        var depth = MathF.Max(product ? 7 : lighting ? 6.6f : marker || beacon ? 6 : contacts ? 5.8f : 5, bounds.Size.Z + 3.2f);
        var l = cu - width / 2; var r = cu + width / 2;
        var f = cv - depth / 2; var v = cv + depth / 2;
        var h = lighting ? 3.3f : product ? 3.1f : marker ? 3 : contacts ? 2.9f : 2.8f;
        var wall = EnvironmentDetailedSurface(marker ? "#A19D94" : lighting ? "#668071" : attention ? "#E1DDD2" : contacts ? "#DCDDD5" : beacon ? "#D9D8CB" : "#DFE3DE", "plaster");
        var lining = EnvironmentDetailedSurface(marker || lighting ? "#A58D70" : attention ? "#B88773" : beacon ? "#92766B" : "#AEBAB7", marker || lighting ? "wood" : attention || beacon ? "plaster" : "metal");
        var floor = EnvironmentDetailedSurface(marker ? "#727E80" : lighting ? "#9CABA4" : attention ? "#B7B8AD" : contacts ? "#8A8780" : beacon ? "#ABA399" : "#A49278", product ? "wood" : "paint");
        var metal = EnvironmentDetailedSurface("#8E9895", "metal");
        var panel = EnvironmentDetailedSurface("#ECE8DB", "paint");
        if (GetNodeOrNull<MeshInstance3D>("CatalogContinuousGround") is { } ground) ground.MaterialOverride = floor;
        Vector3 At(float u, float y, float z) => new(sx * u, y, sz * z);
        var extX = lighting || contacts || product ? 1.1f : 0;
        var extZ = marker ? 1.2f : attention ? .9f : beacon ? 1.1f : 0;
        Box(architecture, "TeachingSuiteContinuousFoundation", new Vector3(width + extX + .4f, .12f, depth + extZ + .4f), At(cu + extX / 2, -.07f, cv + extZ / 2), floor);

        void Wall(string name, float u0, float z0, float u1, float z1, float height,
            StandardMaterial3D finish, float? door = null, float? window = null,
            float windowWidth = 1.4f, float windowBottom = 1.25f, float windowHeight = 1)
        {
            var alongX = MathF.Abs(z1 - z0) < .001f;
            var length = alongX ? MathF.Abs(u1 - u0) : MathF.Abs(z1 - z0);
            var middle = alongX ? (u0 + u1) / 2 : (z0 + z1) / 2;
            var sign = alongX ? sx : -sz;
            var node = new Node3D { Name = name, Position = At((u0 + u1) / 2, 0, (z0 + z1) / 2), RotationDegrees = new Vector3(0, alongX ? 0 : 90, 0) };
            architecture.AddChild(node);
            var holes = new List<Rect2>();
            float? doorX = door.HasValue ? sign * (door.Value - middle) : null;
            if (doorX.HasValue)
            {
                if (MathF.Abs(doorX.Value) + .68f > length / 2) throw new InvalidOperationException("Teaching doorway outside retained wall: " + name);
                holes.Add(new Rect2(doorX.Value - .6f, 0, 1.2f, 2.2f));
            }
            if (window.HasValue)
            {
                var x = sign * (window.Value - middle);
                if (MathF.Abs(x) + windowWidth / 2 + .06f > length / 2 || windowBottom + windowHeight > height)
                    throw new InvalidOperationException("Teaching glazing outside retained wall: " + name);
                holes.Add(new Rect2(x - windowWidth / 2, windowBottom, windowWidth, windowHeight));
            }
            ArtWall(node, length, height, .16f, finish, holes, true);
            Box(node, "SupportedWallCap", new Vector3(length + .16f, .08f, .20f), new Vector3(0, height - .04f, 0), metal);
            var start = -length / 2 + .10f; var end = length / 2 - .10f;
            void Strip(float a, float e)
            {
                if (e - a > .01f) Box(node, "SetbackSkirting", new Vector3(e - a, .10f, .18f), new Vector3((a + e) / 2, .05f, 0), metal);
            }
            if (doorX.HasValue) { Strip(start, doorX.Value - .6f); Strip(doorX.Value + .6f, end); }
            else Strip(start, end);
        }
        void Board(string name, float u, float z, string text, float w = 1.6f)
        {
            Box(architecture, name + "SupportedPlate", new Vector3(w, .75f, .06f), At(u, 1.55f, z - .115f), panel);
            architecture.AddChild(new Label3D { Name = name + "StaticLegend", Text = text, FontSize = 32, PixelSize = .0018f, OutlineSize = 0,
                Modulate = new Color("#38413E"), Position = At(u, 1.55f, z - .151f), RotationDegrees = new Vector3(0, sz > 0 ? 180 : 0, 0) });
            Box(architecture, name + "FixtureBearing", new Vector3(.14f, .16f, .24f), At(u, 2.4f, z - .10f), metal);
            Box(architecture, name + "TaskDiffuser", new Vector3(w * .65f, .07f, .20f), At(u, 2.3f, z - .14f), panel);
        }
        var doorV = f + 1.1f;
        if (marker)
        {
            Wall("MarkerVestibuleBack", l, v + 1.2f, l + 2, v + 1.2f, h, lining, window: l + 1, windowWidth: 1.4f, windowBottom: 2.2f, windowHeight: .6f);
            Wall("MarkerVestibuleLeft", l, v, l, v + 1.2f, h, lining);
            Wall("MarkerVestibuleRight", l + 2, v, l + 2, v + 1.2f, h, lining);
            Wall("MarkerMainRear", l + 2, v, r, v, h, wall);
            Wall("MarkerEntrySide", r, f, r, v, h, wall, doorV);
            Board("MarkerReference", l + 1, v + 1.2f, "NORMAL / INHIBITED\nservice_marker_on\nmarker_inhibit_pressed", 1.7f);
        }
        else if (attention || beacon)
        {
            var half = beacon ? 1.5f : 1.2f; var reach = beacon ? 1.1f : .9f; var recessH = beacon ? 3.4f : h;
            Wall("ServiceRearLeft", l, v, cu - half, v, h, wall);
            Wall("ServiceLeftCheek", cu - half, v, cu - half, v + reach, recessH, lining);
            Wall("ServiceRecessBack", cu - half, v + reach, cu + half, v + reach, recessH, lining,
                window: cu, windowWidth: beacon ? 2 : 1.8f, windowBottom: beacon ? 2.4f : 1.05f, windowHeight: beacon ? .7f : 1);
            Wall("ServiceRightCheek", cu + half, v, cu + half, v + reach, recessH, lining);
            Wall("ServiceRearRight", cu + half, v, r, v, h, wall);
            Wall("ServiceEntrySide", r, f, r, v, h, wall, doorV);
            if (attention)
            {
                Box(architecture, "PassiveServiceCounterBearing", new Vector3(2.1f, 1.0f, .18f), At(cu, .5f, v + reach - .13f), lining);
                Box(architecture, "PassiveServiceCounterTop", new Vector3(2.1f, .07f, .36f), At(cu, 1.015f, v + reach - .14f), panel);
                // Mount the static legend on a solid rear flank, never across glazing.
                Board("RequestReference", (l + cu - half) / 2, v, "REQUEST HELD\nready_light / attention_light", MathF.Min(1.6f, (cu - half - l) - .2f));
            }
            else Board("BeaconReference", cu, v + reach, "OFF / RED / AMBER / GREEN\nbeacon_position", 2.2f);
        }
        else
        {
            var mouth = contacts ? v - 2 : cv + (product ? .2f : -.2f);
            Wall("TeachingSideEntry", r, f, r, mouth, h, wall, doorV);
            Wall("TeachingSideBayFront", r, mouth, r + 1.1f, mouth, h, lining);
            Wall("TeachingSideBayOuter", r + 1.1f, mouth, r + 1.1f, v, h, lining,
                window: contacts ? null : (mouth + v) / 2, windowWidth: product ? MathF.Min(2.4f, v - mouth - .3f) : 1.5f,
                windowBottom: product ? .4f : 2.2f, windowHeight: product ? 2.4f : .75f);
            Wall("TeachingFullRear", l, v, r + 1.1f, v, h, wall,
                window: contacts ? l + 1.2f : null, windowWidth: 1.4f, windowBottom: 2.05f, windowHeight: .6f);
            Board(contacts ? "ContactSection" : product ? "FactorReference" : "LightingReference", contacts ? cu + .3f : product ? cu : r + .55f, v,
                contacts ? "STATIC CONTACT SECTIONS\nNO / NC\nstop_contact_nc / permit_output" : product ? "FACTOR A x FACTOR B\nfactor_a / factor_b\nproduct_result" : "A / B\nCOMMANDS", contacts || product ? 2.0f : .85f);
            if (lighting)
                foreach (var offset in new[] { -.48f, .48f })
                {
                    // Actual side-bay sample recesses with supported lining;
                    // no powered lamps, photometric claim or new process I/O.
                    var z = (mouth + v) / 2 + offset;
                    Box(architecture, "ReflectanceSampleBacking", new Vector3(.06f, .72f, .75f), At(r + 1.1f - .115f, 1.45f, z), offset < 0 ? panel : wall);
                    Box(architecture, "ReflectanceSampleSill", new Vector3(.28f, .07f, .8f), At(r + 1.1f - .16f, 1.055f, z), lining);
                    foreach (var edge in new[] { -.4f, .4f })
                        Box(architecture, "ReflectanceSampleCheek", new Vector3(.28f, .75f, .06f), At(r + 1.1f - .16f, 1.45f, z + edge), lining);
                }
        }
        // Explicit interior route: portal -> outer side -> open front -> bench.
        // The last point sits at the camera-front edge of the actual standing
        // reserve; native swept-volume tests decide clearance, not this prose.
        if (access is { } a)
        {
            var terminal = a.GetCenter(); terminal.Y = 0;
            terminal.Z = sz > 0 ? a.Position.Z : a.End.Z;
            var route = new Node3D { Name = "TeachingSuiteMeasuredApproach" };
            route.SetMeta("localApproachPolyline", new Godot.Collections.Array<Vector3> {
                At(r - .75f, 0, doorV), At(r - .75f, 0, f + .75f),
                new Vector3(terminal.X, 0, sz * (f + .75f)), terminal });
            route.SetMeta("proposedSweepWidth", 1.2f);
            route.SetMeta("localTerminalStandingRegion", a);
            architecture.AddChild(route);
        }
        architecture.SetMeta("purposeLayoutImplemented", true);
        architecture.SetMeta("teachingSuiteExactLayout", d.LayoutVariant);
        architecture.SetMeta("teachingSuiteContextOnly", true);
        return true;
    }
}

