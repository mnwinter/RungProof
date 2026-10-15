using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    // QA for an actual completed Chicken job. Root invokes after normal
    // current-editor preparation; this observer never creates a print result.
    private async Task VerifyChickenLabelPresentation(Action<bool, string> check)
    {
        var paper = _sceneCompositionRoot!.GetNode<MeshInstance3D>("ChickenLabelPaper");
        var ink = paper.GetNode<Label3D>("PrintedInk");
        var caption = paper.GetNode<Label3D>("CapturedLabelText");
        check(paper.Visible && _sceneRuntime!.Points["application_complete"] is true
            && ink.Text.Length > 0 && ink.Text == caption.Text, "chicken_presentation_actual_applied_exact_text");
        // GenerateTriangleMesh is the pinned Label3D API for actual configured
        // glyph vertices including PixelSize. No guessed character width.
        var inkFaces = ink.GenerateTriangleMesh().GetFaces();
        var paperBox = paper.Mesh.GetAabb();
        check(inkFaces.Length > 0 && inkFaces.Select(v => ink.Transform * v)
            .All(v => v.X >= paperBox.Position.X + .005f && v.X <= paperBox.End.X - .005f
                && v.Z >= paperBox.Position.Z + .005f && v.Z <= paperBox.End.Z - .005f),
            "chicken_actual_ink_vertices_fit_physical_sheet_margin");
        var camera = _sceneCamera!;
        var original = camera.GlobalTransform;
        var fov = camera.Fov;
        var horizontalOffset = camera.HOffset;
        var verticalOffset = camera.VOffset;
        var controllerProcessing = _cameraController?.IsProcessing() == true;
        Rect2 Projection(Vector3[] vertices)
        {
            var screen = vertices.Select(camera.UnprojectPosition).ToArray();
            return new Rect2(new Vector2(screen.Min(v => v.X), screen.Min(v => v.Y)),
                new Vector2(screen.Max(v => v.X) - screen.Min(v => v.X), screen.Max(v => v.Y) - screen.Min(v => v.Y)));
        }
        void CheckView(string name)
        {
            var corners = Enumerable.Range(0, 8).Select(i => paper.GlobalTransform * new Vector3(
                (i & 1) == 0 ? paperBox.Position.X : paperBox.End.X,
                (i & 2) == 0 ? paperBox.Position.Y : paperBox.End.Y,
                (i & 4) == 0 ? paperBox.Position.Z : paperBox.End.Z)).ToArray();
            var faces = caption.GenerateTriangleMesh().GetFaces();
            // Billboard glyphs use the camera's facing basis; their scene
            // anchor remains the real Label3D GlobalPosition.
            var world = faces.Select(v => caption.GlobalPosition + camera.GlobalBasis * v).ToArray();
            var captionRect = Projection(world).Grow(6);
            var paperRect = Projection(corners).Grow(3);
            check(!captionRect.Intersects(paperRect), "chicken_caption_excludes_actual_sheet_screen_rect_" + name);
            GD.Print($"CHICKEN_PRESENTATION_RECT view={name} caption={captionRect} sheet={paperRect}");
        }
        try
        {
            CheckView("normal");
            // Temporarily isolate this diagnostic projection from the orbit
            // controller's normal aperture offsets and retained distance.
            // Do not rewrite its target/distance or product navigation state.
            _cameraController?.SetProcess(false);
            camera.HOffset = 0; camera.VOffset = 0;
            camera.GlobalPosition = paper.GlobalPosition + new Vector3(.65f, 1.1f, 1f);
            camera.Fov = 32; camera.LookAt(paper.GlobalPosition, Vector3.Up);
            // Camera projection caches are updated by an actual engine frame.
            // Root acquires the diagnostic capture clock lease before calling.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            check(camera.UnprojectPosition(paper.GlobalPosition).DistanceTo(camera.GetViewport().GetVisibleRect().GetCenter()) < 3,
                "chicken_detail_camera_actual_paper_target_centered");
            CheckView("detail");
        }
        finally
        {
            camera.GlobalTransform = original; camera.Fov = fov;
            camera.HOffset = horizontalOffset; camera.VOffset = verticalOffset;
            _cameraController?.SetProcess(controllerProcessing);
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }
}
