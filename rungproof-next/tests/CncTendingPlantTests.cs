using System;
using System.Numerics;
using RungProof.Next.Scenes;

internal static partial class Program
{
    private static void RegisterCncTendingPlantTests()
    {
        Test("CNC commanded targets grip support and cut interlocks", () =>
        {
            var park = new Vector3(0, 2.8f, -1.4f);
            var plant = new CncTendingPlantModel(park);
            CncTendingPlantModel.Commands Command(Vector3 target, bool grip = false, bool access = true, bool clamp = false, bool spindle = false)
                => new(false, false, true, target, grip, access, clamp, spindle);
            void Move(Vector3 target, bool grip = false)
            {
                for (var i = 0; i < 1000; i++)
                {
                    plant.Advance(.02, Command(target, grip), p => p);
                    if (plant.AtTarget && (grip == plant.Gripped)) return;
                }
                throw new InvalidOperationException("Commanded target was not reached.");
            }
            plant.Advance(6, new(true, false, false, park, false, false, false, false), p => p);
            True(plant.BlankAtPickup); False(plant.CutPermitted);
            plant.Advance(1, Command(new(0, 1.83f, -1.985f), access: false), p => p);
            True(plant.Inhibited); // Closed access prevents entering enclosure.
            Move(new(-1.8f, 1.97f, -1.3f)); Move(new(-1.8f, 1.97f, .55f));
            Move(CncTendingPlantModel.Pickup + CncTendingPlantModel.GripOffset, true); True(plant.Gripped);
            var held = plant.Stock;
            plant.Advance(.02, Command(plant.Tool, grip: false), p => p);
            True(plant.Gripped); True(plant.Inhibited); Equal(held, plant.Stock); // Unsupported release.
            Move(new(-1.8f, 1.97f, .55f), true); Move(new(-1.8f, 1.97f, -1.3f), true);
            Move(new(0, 1.97f, -1.3f), true); Move(new(0, 1.97f, -1.985f), true);
            Move(CncTendingPlantModel.Vise + CncTendingPlantModel.GripOffset, true);
            Move(plant.Tool, false); True(plant.StockOnVise);
            Move(new(0, 1.83f, -1.3f)); Move(park);
            plant.Advance(1.1, Command(park, access: false, clamp: true), p => p);
            True(plant.CutPermitted);
            plant.Advance(1, Command(park, access: false, clamp: true, spindle: true), p => p);
            False(plant.MachiningComplete);
            plant.Advance(1, Command(park, access: false, clamp: true, spindle: true), p => p);
            True(plant.MachiningComplete);
            var position = plant.Tool;
            plant.Advance(2, new(false, false, false, new(2, 2, .5f), false, false, true, false), p => p);
            Equal(position, plant.Tool); // Servo disable holds actual robot.
            plant.Advance(1.1, Command(park, access: true, clamp: false), p => p);
            Move(new(0, 1.83f, -1.3f)); Move(CncTendingPlantModel.Vise + CncTendingPlantModel.GripOffset);
            Move(plant.Tool, true); True(plant.Gripped); False(plant.StockOnVise);
            Move(new(0, 1.97f, -1.985f), true); Move(new(0, 1.97f, -1.3f), true);
            Move(new(1.8f, 1.97f, -1.3f), true); Move(new(1.8f, 1.97f, .55f), true);
            Move(CncTendingPlantModel.Outfeed + CncTendingPlantModel.GripOffset, true);
            Move(plant.Tool, false); True(plant.StockOnOutfeed); False(plant.Gripped);
            Move(new(1.8f, 1.97f, .55f)); Move(new(1.8f, 1.97f, -1.3f)); Move(park);
            plant.Advance(6, new(false, true, false, park, false, false, false, false), p => p);
            True(plant.CycleComplete); True(plant.StockOnOutfeed);
            plant.Reset(); Equal(park, plant.Tool); False(plant.Gripped); False(plant.MachiningComplete);
        });
        Test("CNC rejected IK cannot fabricate target completion", () =>
        {
            var park = new Vector3(0, 2.8f, -1.4f);
            var plant = new CncTendingPlantModel(park);
            plant.Advance(1, new(false, false, true, new(-1, 2, -1.3f), false, true, false, false), _ => null);
            Equal(park, plant.Tool); False(plant.AtTarget); True(plant.Inhibited);
        });
        Test("CNC grip cannot attach across millimetres or snap stationary stock", () =>
        {
            var park = new Vector3(0, 2.8f, -1.4f);
            var plant = new CncTendingPlantModel(park);
            plant.Advance(6, new(true, false, false, park, false, false, false, false), p => p);
            var offsetTarget = CncTendingPlantModel.Pickup + CncTendingPlantModel.GripOffset + Vector3.UnitY * .004f;
            for (var i = 0; i < 1000; i++)
                plant.Advance(.02, new(false, false, true, offsetTarget, true, true, false, false), p => p);
            True(plant.AtTarget); False(plant.Gripped); True(plant.Inhibited);
            Equal(CncTendingPlantModel.Pickup, plant.Stock);
            var target = CncTendingPlantModel.Pickup + CncTendingPlantModel.GripOffset;
            for (var i = 0; i < 10; i++)
                plant.Advance(.02, new(false, false, true, target, true, true, false, false), p => p);
            True(plant.Gripped);
            var stock = plant.Stock;
            plant.Advance(.02, new(false, false, true, plant.Tool, false, true, false, false), p => p);
            True(plant.Gripped); True(plant.Inhibited); Equal(stock, plant.Stock);
        });
    }
}
