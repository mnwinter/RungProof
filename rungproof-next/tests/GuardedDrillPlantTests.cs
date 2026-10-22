using RungProof.Next.Scenes;

internal static partial class Program
{
    private static void RegisterGuardedDrillPlantTests()
    {
        Test("Guarded drill has explicit feed and retract with real limits", () =>
        {
            var plant = new GuardedDrillPlantModel(1.4);
            plant.Advance(5, true, false, false, true, true, true);
            Equal(0d, plant.Position); True(plant.AtTop); // Spindle alone cannot feed.
            plant.Advance(.7, true, true, false, true, true, true); Equal(.5, plant.Position);
            plant.Advance(5, true, true, false, false, true, true); Equal(.5, plant.Position); True(plant.MotionInhibited);
            plant.Advance(5, true, true, true, true, true, true); Equal(.5, plant.Position); True(plant.MotionInhibited);
            plant.Advance(5, true, false, false, true, true, true); Equal(.5, plant.Position); False(plant.MotionInhibited);
            plant.Advance(5, true, true, false, true, true, true); True(plant.AtBottom); False(plant.CycleComplete);
            plant.Advance(5, false, false, false, false, false, false); True(plant.AtBottom); // No automatic return.
            plant.Advance(.7, false, false, true, false, false, false); Equal(.5, plant.Position);
            plant.Advance(5, false, false, true, false, false, false); True(plant.AtTop); True(plant.CycleComplete);
            plant.Reset(); True(plant.AtTop); False(plant.CycleComplete); False(plant.MotionInhibited);
        });
    }
}
