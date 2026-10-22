using System;
using RungProof.Next.Scenes;

internal static partial class Program
{
    private static void RegisterServiceDoorPlantTests()
    {
        Test("Service door follows PLC direction and real NC endpoints", () =>
        {
            var door = new ServiceDoorPlantModel(3);
            Equal(1d, door.Position); True(door.OpenLimitNc); False(door.ClosedLimitNc); False(door.CycleComplete);
            door.Advance(.75, true, false); Equal(.75, door.Position);
            True(door.OpenLimitNc); True(door.ClosedLimitNc); False(door.CycleComplete);
            door.Advance(30, false, false); Equal(.75, door.Position); // Stop holds midtravel.
            door.Advance(30, true, true); Equal(.75, door.Position); // Conflict cannot pick a direction.
            door.Advance(30, true, false); Equal(0d, door.Position);
            False(door.OpenLimitNc); True(door.ClosedLimitNc); True(door.CycleComplete);
            door.Advance(30, true, false); Equal(0d, door.Position); // Hard clamp.
            door.Advance(1.5, false, true); Equal(.5, door.Position); False(door.CycleComplete);
            door.Advance(1.5, false, true); Equal(1d, door.Position);
            True(door.OpenLimitNc); False(door.ClosedLimitNc); True(door.CycleComplete);
            door.Reset(); Equal(1d, door.Position); False(door.CycleComplete);
        });
        Test("Service door rejects invalid physical time parameters", () =>
        {
            foreach (var invalid in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            {
                var rejected = false;
                try { _ = new ServiceDoorPlantModel(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
                True(rejected);
            }
            var door = new ServiceDoorPlantModel(3);
            foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity })
            {
                var rejected = false;
                try { door.Advance(invalid, true, false); } catch (ArgumentOutOfRangeException) { rejected = true; }
                True(rejected); Equal(1d, door.Position);
            }
        });
    }
}
