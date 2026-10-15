using System;
using System.IO;

namespace RungProof.Next.VirtualController;

public static class DemoPrograms
{
    public const string ConveyorSceneId = "scene-1-conveyor-stop";

    public static LadderProgramLoadResult LoadConveyor(string json) => LadderProgramJson.Load(json);
}
