using System;
using System.Threading.Tasks;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    /// <summary>
    /// Visible native-input gate for the Siemens project tree. This deliberately
    /// routes mouse events through the viewport instead of emitting ItemActivated.
    /// </summary>
    private async void VerifyProjectTreePointer()
    {
        try
        {
            if (_simulatorShell is null)
                throw new InvalidOperationException("Simulator shell is unavailable.");

            _simulatorShell.SetReviewState("ladder", string.Empty);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            var result = await _simulatorShell.VerifyTiaProjectTreePointerAsync();
            GD.Print($"TIA_PROJECT_TREE_POINTER_VERIFY {(result.Passed ? "PASS" : "FAIL")} {result.Detail}");

            if (result.Passed && _capturePath is not null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var capturePath = _capturePath;
                _capturePath = null;
                var error = GetViewport().GetTexture().GetImage().SavePng(capturePath);
                if (error != Error.Ok)
                    throw new InvalidOperationException($"Project-tree pointer screenshot failed: {error}.");
                GD.Print("TIA_PROJECT_TREE_POINTER_CAPTURE " + capturePath);
            }

            GetTree().Quit(result.Passed ? 0 : 1);
        }
        catch (Exception error)
        {
            GD.PushError("TIA_PROJECT_TREE_POINTER_VERIFY FAIL " + error);
            GetTree().Quit(1);
        }
    }
}
