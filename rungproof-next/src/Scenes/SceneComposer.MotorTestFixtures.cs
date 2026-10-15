using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.App;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Install the original motors before Ready and runtime initial-pose capture.
    // Roots remain direct children with their original IDs/basis; only Y changes.
    private static void ConfigureMotorTestFixtures(Node3D root, SceneDefinition scene)
    {
        if (scene.Id is not ("lab-10-04-motor-enum-state" or "lab-10-05-motor-struct-data" or "lab-10-06-ten-motor-array-startup")) return;
        if (scene.Id == "lab-10-06-ten-motor-array-startup") return;
        var motors = scene.Equipment.Where(e => e.Type == "motor")
            .Select(e => root.GetNode<Node3D>(SafeNodeName(e.Id))).ToArray();
        var expected = scene.Id == "lab-10-06-ten-motor-array-startup" ? 10 : 1;
        if (motors.Length != expected) throw new InvalidOperationException("Motor fixture source identity count changed.");
        var installation = new Node3D { Name = "MotorTestInstallation" };
        installation.SetMeta("installationKind", "motorTestRig");
        root.AddChild(installation);
        var frame = StudioSceneMaterials.CreateDetailed("#687C7A", "paint");
        var plate = StudioSceneMaterials.CreateDetailed("#A6B2AF", "metal");
        const float top = .72f;
        var footprints = new Dictionary<Node3D, Aabb>();
        MeshInstance3D Part(string name, Vector3 size, Vector3 position, Material material)
        {
            var mesh = AddBox(installation, size, position, material);
            mesh.Name = name;
            return mesh;
        }
        foreach (var motor in motors)
        {
            if (!motor.Transform.IsFinite() || motor.Scale.X <= 0 || motor.Scale.Y <= 0 || motor.Scale.Z <= 0
                || !motor.Basis.Y.Normalized().IsEqualApprox(Vector3.Up))
                throw new InvalidOperationException("Motor fixture refuses a tilted mounting plane: " + motor.Name);
            var localFeet = ContextMemberBounds(motor, "MOTOR_foot_");
            var feet = motor.Transform * localFeet;
            var before = motor.Transform;
            motor.Position += new Vector3(0, top - feet.Position.Y, 0);
            feet = motor.Transform * localFeet;
            footprints.Add(motor, feet);
            motor.SetMeta("motorTestMounted", true);
            motor.SetMeta("motorTestOriginalTransform", before);
            motor.SetMeta("motorTestMountedTransform", motor.Transform);
            motor.SetMeta("motorTestPlateTop", top);
            motor.SetMeta("motorTestFootBounds", feet);
            motor.SetMeta("motorTestPlatePath", "MotorTestInstallation/MotorPlate_" + motor.Name);
        }
        void Leg(string name, float x, float z, float underside)
        {
            Part(name + "FloorFoot", new Vector3(.22f, .03f, .22f), new Vector3(x, .015f, z), frame);
            Part(name + "GroundLeg", new Vector3(.08f, underside - .03f, .08f), new Vector3(x, (underside + .03f) / 2, z), frame);
        }
        foreach (var row in motors.GroupBy(m => expected == 1 ? 0 : MathF.Round(m.Position.Z * 1000) / 1000))
        {
            var members = row.OrderBy(m => m.Position.X).ToArray();
            var rowFeet = footprints[members[0]];
            foreach (var motor in members.Skip(1)) rowFeet = rowFeet.Merge(footprints[motor]);
            var width = MathF.Max(1.65f, rowFeet.Size.X + .65f);
            var depth = MathF.Max(1.0f, rowFeet.Size.Z + .35f);
            var center = rowFeet.GetCenter();
            var rowName = "MotorRow_" + members[0].Name;
            // Array pads sit on a continuous deck at .68; single plate at .72.
            // The two surfaces meet at different datums, never coplanar overlap.
            var underside = expected == 1 ? .66f : .62f;
            if (expected > 1)
                Part(rowName + "ContinuousDeck", new Vector3(width, .06f, depth), new Vector3(center.X, .65f, center.Z), plate);
            foreach (var motor in members)
            {
                var feet = footprints[motor];
                var c = feet.GetCenter();
                var motorWidth = MathF.Max(1.65f, feet.Size.X + .30f);
                var motorDepth = MathF.Max(1.0f, feet.Size.Z + .30f);
                var thickness = expected == 1 ? .06f : .04f;
                Part("MotorPlate_" + motor.Name, new Vector3(motorWidth, thickness, motorDepth), new Vector3(c.X, top - thickness / 2, c.Z), plate);
                foreach (var side in new[] { -1f, 1f })
                {
                    var x = c.X + side * (motorWidth / 2 - .12f);
                    foreach (var face in new[] { -1f, 1f }) Leg("Mount_" + motor.Name + "_" + side + "_" + face, x, c.Z + face * (depth / 2 - .12f), underside);
                    Part("MotorTransverseBrace_" + motor.Name + "_" + side, new Vector3(.08f, .08f, depth - .24f), new Vector3(x, .22f, center.Z), frame);
                }
                // Four anchor witnesses follow the actual authored mounting
                // axes; existing bolt heads are retained and not duplicated.
                foreach (var mount in motor.FindChildren("MOTOR_mount_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                {
                    var at = (motor.Transform * ContextMeshBounds(motor, mount)).GetCenter();
                    Part("MotorAnchorUnderplate_" + motor.Name + "_" + mount.Name, new Vector3(.07f, .025f, .07f), new Vector3(at.X, top - thickness - .0125f, at.Z), frame);
                }
            }
            foreach (var face in new[] { -1f, 1f })
            {
                var z = center.Z + face * (depth / 2 - .12f);
                Part(rowName + "UpperRail_" + face, new Vector3(width - .16f, .08f, .08f), new Vector3(center.X, underside - .04f, z), frame);
                Part(rowName + "LowerRail_" + face, new Vector3(width - .16f, .08f, .08f), new Vector3(center.X, .22f, z), frame);
            }
        }
        installation.SetMeta("motorTestCount", expected);
        VerifyMotorTestFixtureGeometry(root);
    }

    // Pure actual-mesh check, callable again after Ready/reset by the native
    // harness. Mechanical bearing witnesses are not commissioning/safety proof.
    public static int VerifyMotorTestFixtureGeometry(Node3D root)
    {
        var installation = root.GetNodeOrNull<Node3D>("MotorTestInstallation");
        if (installation is null) return VerifyMotorConveyorGeometry(root);
        var motors = root.GetChildren().OfType<Node3D>().Where(n => n.HasMeta("motorTestMounted")).ToArray();
        if (motors.Length != installation.GetMeta("motorTestCount").AsInt32()) throw new InvalidOperationException("Motor fixture count mismatch.");
        foreach (var motor in motors)
        {
            var original = motor.GetMeta("motorTestOriginalTransform").AsTransform3D();
            if (!motor.Basis.IsEqualApprox(original.Basis)
                || MathF.Abs(motor.Position.X - original.Origin.X) > .001f
                || MathF.Abs(motor.Position.Z - original.Origin.Z) > .001f)
                throw new InvalidOperationException("Motor fixture changed authored horizontal placement/basis.");
            var feet = motor.Transform * ContextMemberBounds(motor, "MOTOR_foot_");
            var carrying = root.GetNode<MeshInstance3D>(motor.GetMeta("motorTestPlatePath").AsString());
            var plate = ContextMeshBounds(root, carrying);
            if (MathF.Abs(feet.Position.Y - plate.End.Y) > .001f
                || feet.Position.X < plate.Position.X - .001f || feet.End.X > plate.End.X + .001f
                || feet.Position.Z < plate.Position.Z - .001f || feet.End.Z > plate.End.Z + .001f)
                throw new InvalidOperationException("Motor feet do not bear their mounting plate: " + motor.Name);
            foreach (var footMesh in motor.FindChildren("MOTOR_foot_*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                if (MathF.Abs((motor.Transform * ContextMeshBounds(motor, footMesh)).Position.Y - plate.End.Y) > .001f)
                    throw new InvalidOperationException("Individual motor foot is off its mounting plane: " + motor.Name);
            if (motors.Length > 1)
            {
                var decks = installation.FindChildren("*ContinuousDeck", "MeshInstance3D", true, false).OfType<MeshInstance3D>();
                if (!decks.Select(d => ContextMeshBounds(root, d)).Any(d => MathF.Abs(d.End.Y - plate.Position.Y) < .001f
                    && plate.Position.X >= d.Position.X - .001f && plate.End.X <= d.End.X + .001f
                    && plate.Position.Z >= d.Position.Z - .001f && plate.End.Z <= d.End.Z + .001f))
                    throw new InvalidOperationException("Motor array mounting pad lacks continuous deck bearing.");
            }
            foreach (var mesh in motor.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            {
                if (mesh.Mesh is null || mesh.Name.ToString().StartsWith("MOTOR_foot_", StringComparison.Ordinal)) continue;
                var body = motor.Transform * ContextMeshBounds(motor, mesh);
                // Exact contact at the foot datum is expected; other motor
                // members must not extend into the mounting slab volume.
                if (body.Position.Y < plate.End.Y - .001f && body.Intersects(plate))
                    throw new InvalidOperationException("Mounting slab intersects a motor member: " + motor.Name + "/" + mesh.Name);
            }
        }
        foreach (var foot in installation.FindChildren("*FloorFoot", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            if (MathF.Abs(ContextMeshBounds(root, foot).Position.Y) > .001f)
                throw new InvalidOperationException("Motor frame foot is not grounded.");
        foreach (var leg in installation.FindChildren("*GroundLeg", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
        {
            var measured = ContextMeshBounds(root, leg);
            if (MathF.Abs(measured.Position.Y - .03f) > .001f || (MathF.Abs(measured.End.Y - .62f) > .001f && MathF.Abs(measured.End.Y - .66f) > .001f))
                throw new InvalidOperationException("Motor frame leg does not bear grounded foot/deck.");
        }
        return motors.Length;
    }
}
