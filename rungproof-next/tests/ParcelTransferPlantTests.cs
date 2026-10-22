using System;
using System.Linq;
using RungProof.Next.Scenes;

internal static partial class Program
{
    private static ParcelTransferPlantModel ParcelPlant() => new(
        new[] { -4.6, -5.8, -6.65 }, new[] { 1.3, .9, .6 }, new[] { 1.0, .8, .6 }, new[] { 1.1, .8, .5 },
        .9, -2.7, -2.75, 1.8, 1.2, new[] { .245, .595, .995 }, new[] { 4.935, 10.635, 4.935 });

    private static void RegisterParcelTransferPlantTests()
    {
        Test("Parcel escapement and independent drives do not run a hidden route", () =>
        {
            var plant = ParcelPlant();
            plant.Advance(5, false, true, true, true, 1); Equal(-4.6, plant.Parcels[0].X);
            for (var i = 0; i < 200; i++) plant.Advance(.02, true, true, true, false, 0);
            True(Math.Abs(plant.Parcels[0].X + 3.35) < 1e-8); Equal(1, plant.DetectedClass);
            False(plant.TableLoaded); Equal(0, plant.SortedCount);
            var queued = plant.Parcels.Select(p => p.X).ToArray();
            plant.Advance(2, true, false, false, true, 0);
            True(queued.SequenceEqual(plant.Parcels.Select(p => p.X))); // Gate opening is not a drive command.
        });
        Test("Parcel learner-owned commands deliver all three geometric routes", () =>
        {
            var plant = ParcelPlant();
            void Until(Func<bool> ready, bool feed, bool transfer, bool gate, double target)
            {
                for (var i = 0; i < 4000 && !ready(); i++) plant.Advance(.02, true, feed, transfer, gate, target);
                True(ready());
            }
            for (var parcel = 0; parcel < 3; parcel++)
            {
                Until(() => plant.DetectedClass == parcel + 1, true, false, false, 0);
                plant.Advance(.02, true, true, true, true, 0); // Each new opening resets the rear-clear event.
                Until(() => plant.GateClear, true, true, true, 0);
                Until(() => plant.TableLoaded, true, true, false, 0);
                var before = plant.Parcels[parcel].X;
                plant.Advance(.5, false, false, false, false, parcel / 2.0);
                Equal(before, plant.Parcels[parcel].X); // Stop freezes the accepted mechanics.
                Until(() => plant.TableAligned && Math.Abs(plant.TablePosition - parcel / 2.0) < 1e-8,
                    false, false, false, parcel / 2.0);
                Until(() => plant.TableClear, false, true, false, parcel / 2.0);
                Until(() => plant.TableAligned && plant.TablePosition == 0, false, false, false, 0);
            }
            Until(() => plant.Complete, false, false, false, 0);
            Equal(3, plant.SortedCount); False(plant.Misroute);
            plant.Advance(10, true, false, true, false, 1); Equal(3, plant.SortedCount); // No repeated arrival counting.
            plant.Reset(); Equal(0, plant.SortedCount); True(plant.TableClear); False(plant.Complete);
        });
        Test("Parcel support refusal holds the physical load without changing routing", () =>
        {
            var plant = ParcelPlant(); plant.Supported = _ => false;
            plant.Advance(.02, true, true, true, true, 0);
            Equal(-4.6, plant.Parcels[0].X); True(plant.Inhibited); Equal(-1, plant.Parcels[0].Route);
        });
    }
}
