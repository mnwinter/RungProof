using System;
using RungProof.Next.Scenes;

internal static class VisionSorterTests
{
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
    private static void Tick(VisionSorterPlantModel p, int cls = 1)
        => p.Step(.02, true, true, true, true, true, cls);
    private static (double X, double Z, double Yaw, VisionSorterPlantModel.SortPhase Phase) Pose(VisionSorterPlantModel p)
        => (p.X, p.Z, p.YawDegrees, p.Phase);
    private static int Main()
    {
        for (var cls = 1; cls <= 4; cls++)
        {
            var p = new VisionSorterPlantModel();
            Tick(p, cls);
            Require(p.LatchedClass == cls, "Class was not latched");
            var priorX = p.X; var priorZ = p.Z; var priorYaw = p.YawDegrees;
            for (var scan = 0; scan < 2000 && !p.Done; scan++)
            {
                // Change the fixture every scan: active route must retain its class.
                Tick(p, cls == 4 ? 1 : cls + 1);
                var travel = Math.Sqrt(Math.Pow(p.X-priorX, 2) + Math.Pow(p.Z-priorZ, 2));
                Require(travel <= .009000001, "Carton teleported at a phase boundary");
                Require(Math.Abs(p.YawDegrees-priorYaw) <= .900000001, "Table indexed instantaneously");
                Require(p.LatchedClass == cls, "Mid-cycle class redirected the carton");
                priorX=p.X; priorZ=p.Z; priorYaw=p.YawDegrees;
            }
            Require(p.Done && Math.Abs(p.OutfeedRadius-6.1) < 1e-9, "Route failed to reach receiving endpoint");
            var end = Pose(p); Tick(p, 1); Require(Pose(p)==end, "Completed carton recycled/disappeared");
            p.Reset(); Require(p.X == -1.1 && p.Z == 0 && p.LatchedClass == 0 && !p.Done, "Reset failed");
        }
        var held = new VisionSorterPlantModel(); Tick(held);
        var home = Pose(held);
        held.Step(.5,false,true,true,true,true,1); Require(Pose(held)==home,"Conveyor loss moved infeed");
        held.Step(.5,true,true,false,true,true,1); Require(Pose(held)==home,"Package permissive loss moved carton");
        held.Step(.5,true,true,true,false,true,1); Require(Pose(held)==home,"Invalid result moved carton");
        held.Step(.5,true,true,true,true,false,1); Require(Pose(held)==home,"Blocked destination moved carton");
        while (held.Phase == VisionSorterPlantModel.SortPhase.Infeed) Tick(held);
        var index = Pose(held); held.Step(.5,true,false,true,true,true,1);
        Require(Pose(held)==index,"Disabled diverter indexed table");
        while (held.Phase == VisionSorterPlantModel.SortPhase.Indexing) Tick(held);
        var discharge = Pose(held); held.Step(.5,false,true,true,true,true,1);
        Require(Pose(held)==discharge,"Conveyor loss moved discharge");
        held.Step(.5,true,false,true,true,true,1); Require(Pose(held)==discharge,"Diverter loss moved discharge");
        foreach(var cls in new[]{0,5})
        { var p=new VisionSorterPlantModel(); Tick(p,cls); Require(p.Phase==VisionSorterPlantModel.SortPhase.Awaiting,"Invalid class accepted"); }
        foreach(var seconds in new[]{-1d,double.NaN,double.PositiveInfinity})
        { bool rejected=false; try{held.Step(seconds,true,true,true,true,true,1);}catch(ArgumentOutOfRangeException){rejected=true;} Require(rejected,"Invalid time accepted"); }
        Console.WriteLine("VISION_SORTER_MODEL PASS four routes, bounded motion, class latch, permissive/command holds, endpoint retention, reset, invalid class/time");
        return 0;
    }
}
