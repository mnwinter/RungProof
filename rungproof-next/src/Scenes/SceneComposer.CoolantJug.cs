using System;
using Godot;
using RungProof.Next.App;
using RungProof.Next.Catalog;

namespace RungProof.Next.Scenes;

public static partial class SceneComposer
{
    // Opt-in installation for the jug lesson. The reusable standalone filler
    // keeps its original geometry in every other scene. Dimensions below are
    // visual mounting datums, not structural or hydraulic ratings.
    private static Node3D CreateConveyorJugFiller(SceneEquipment equipment,
        AssetCatalogDocument candidates, bool runCommand)
    {
        var model = CreateMappedAsset(equipment, candidates, "process.packaging.filler.tote-volumetric.v1");
        MeshInstance3D Part(string name) => model.FindChild(name, true, false) as MeshInstance3D
            ?? throw new InvalidOperationException($"Conveyor jug filler is missing imported part '{name}'.");
        var offset = new Vector3(0.2f, 0.68f, 3.2f);
        foreach (var name in new[] { "DOSING_VALVE_BLOCK", "KIN_fill_nozzle", "NOZZLE_TIP",
            "NOZZLE_GUIDE_0_14", "NOZZLE_GUIDE_-0_14", "PRODUCT_INLET", "VISIBLE_LIQUID_STREAM" })
            Part(name).Position += offset;

        var column = Part("FILLER_COLUMN");
        column.Position = new Vector3(0.66f, 1.5875f, -0.4f);
        column.Scale = new Vector3(1, 3.025f / 2.45f, 1);
        var frontColumn = new MeshInstance3D
        {
            Name = "JUG_FILL_front_column", Mesh = column.Mesh,
            MaterialOverride = column.GetActiveMaterial(0),
            Position = new Vector3(0.66f, 1.5875f, 5.4f), Scale = column.Scale,
        };
        model.AddChild(frontColumn);
        var steel = column.GetActiveMaterial(0)!;
        AddBox(model, new Vector3(0.5f, 0.12f, 0.5f), new Vector3(0.66f, 0.06f, 5.4f), steel)
            .Name = "JUG_FILL_front_foot";
        AddBox(model, new Vector3(0.3f, 0.26f, 6.1f), new Vector3(0.66f, 3.02f, 2.5f), steel)
            .Name = "JUG_FILL_portal_beam";
        // Retain the imported short boom as the nozzle's mounting bridge to
        // the portal beam. Both floor columns support the longer cross-belt span.
        var boom = Part("NOZZLE_BOOM");
        boom.Position = new Vector3(0.43f, 3.02f, 3.2f);
        boom.Scale = new Vector3(0.76f / 1.1f, 1, 1);

        // Keep a connected elevated feed path from the retained reservoir to
        // the inlet. Reuse cylindrical hose meshes without changing diameter.
        PoseTube(Part("PRODUCT_HOSE"), new Vector3(-0.15f, 2.45f, -0.12f),
            new Vector3(-0.15f, 3.39f, -0.12f));
        PoseTube(Part("PRODUCT_HOSE_DROP"), new Vector3(-0.15f, 3.39f, -0.12f),
            new Vector3(0.48f, 3.39f, 3.1f));
        Part("SUPPLY_RESERVOIR").Position += Vector3.Down * 0.035f;
        Part("SUPPLY_SIGHT_GLASS").Position += Vector3.Down * 0.035f;
        PoseTube(Part("SUPPLY_PIPE_RISER"), new Vector3(-0.62f, 1.37f, -0.36f),
            new Vector3(-0.62f, 2.45f, -0.36f));

        // Tip and visual stream must travel with the imported nozzle, rather
        // than remaining behind as independent sibling meshes.
        var nozzle = Part("KIN_fill_nozzle");
        var stream = Part("VISIBLE_LIQUID_STREAM");
        stream.Name = "JUG_FILL_stream";
        stream.Visible = false;
        foreach (var attachment in new[] { Part("NOZZLE_TIP"), stream })
        {
            var local = nozzle.Transform.AffineInverse() * attachment.Transform;
            // These are runtime instance attachments. Clear packed-scene
            // ownership before changing their imported parent hierarchy.
            attachment.Owner = null;
            attachment.GetParent().RemoveChild(attachment);
            nozzle.AddChild(attachment);
            attachment.Transform = local;
        }
        model.AddChild(new EquipmentMotionController
        {
            Name = "JugFillNozzleController", Kind = EquipmentMotionController.MotionKind.LinearY,
            TargetPrefix = "KIN_fill_nozzle", RunCommand = runCommand,
            TravelM = -0.12f, TravelTimeSeconds = 0.35f,
            PositionVisibilityChildName = "JUG_FILL_stream",
        });
        return model;
    }

    private static void PoseTube(MeshInstance3D member, Vector3 start, Vector3 end)
    {
        var delta = end - start;
        var y = delta.Normalized();
        var x = Vector3.Up.Cross(y);
        if (x.LengthSquared() < 0.0001f) x = Vector3.Right;
        x = x.Normalized();
        var basis = new Basis(x, y, x.Cross(y));
        member.Transform = new Transform3D(basis * Basis.FromScale(
            new Vector3(1, delta.Length() / member.GetAabb().Size.Y, 1)), (start + end) * 0.5f);
    }

    private static void OpenJugForFilling(Node3D model)
    {
        foreach (var node in model.FindChildren("JUG_CAP*", "", true, false))
        {
            node.GetParent().RemoveChild(node);
            node.Free();
        }
        var neck = model.FindChild("JUG_NECK", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("Fillable jug is missing its neck.");
        var material = neck.GetActiveMaterial(0);
        // The delivery neck is a solid cylinder. Replace only this instance
        // with an annular neck so the uncapped mouth actually has an opening.
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        Vector3 Ring(int segment, float radius, float height)
        {
            var angle = MathF.Tau * segment / 64;
            return new Vector3(radius * MathF.Cos(angle), height, radius * MathF.Sin(angle));
        }
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            foreach (var vertex in new[] { a, b, c, a, c, d }) surface.AddVertex(vertex);
        }
        for (var i = 0; i < 64; i++)
        {
            var ob = Ring(i, 0.105f, -0.08f); var ot = Ring(i, 0.105f, 0.08f);
            var nb = Ring(i + 1, 0.105f, -0.08f); var nt = Ring(i + 1, 0.105f, 0.08f);
            var ib = Ring(i, 0.075f, -0.08f); var it = Ring(i, 0.075f, 0.08f);
            var jb = Ring(i + 1, 0.075f, -0.08f); var jt = Ring(i + 1, 0.075f, 0.08f);
            Quad(ob, nb, nt, ot); Quad(ib, it, jt, jb);
            Quad(ot, nt, jt, it); Quad(ob, ib, jb, nb);
        }
        surface.GenerateNormals();
        neck.Mesh = surface.Commit();
        neck.MaterialOverride = material;
    }
}
