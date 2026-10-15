using System;
using System.IO;
using System.Text.Json;
using RungProof.Next.Scenes;

internal static class Program
{
    private static void Require(bool condition, string detail)
    { if (!condition) throw new Exception(detail); }

    private static void Main()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText("tests/Plant/python-model-reference.json"));
        var sourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            File.ReadAllBytes("../vendor/siemens-plc-pc-interface/siemens_plc_pc_interface/components.py"))).ToLowerInvariant();
        Require(sourceHash == reference.RootElement.GetProperty("sourceSha256").GetString(), "Pinned Python reference changed; regenerate its traces.");
        var cases = 0;
        var frames = 0;
        foreach (var test in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var model = new ConveyorPlantModel(1, 0.5, 0.2, 0.5);
            foreach (var frame in test.GetProperty("frames").EnumerateArray())
            {
                model.Step(frame.GetProperty("seconds").GetDouble(), frame.GetProperty("run").GetBoolean(),
                    frame.GetProperty("extend").GetBoolean(), test.GetProperty("hasPusher").GetBoolean());
                var expectedEdge = frame.GetProperty("leadingEdge");
                Require(expectedEdge.ValueKind == JsonValueKind.Null ? model.LeadingEdge is null
                    : model.LeadingEdge.HasValue && Math.Abs(model.LeadingEdge.Value - expectedEdge.GetDouble()) < 1e-9,
                    $"{test.GetProperty("name")} leading edge at frame {frames}");
                Require(model.PhotoeyeBlocked == frame.GetProperty("photoeye").GetBoolean()
                    && model.Completed == frame.GetProperty("completed").GetInt64()
                    && Math.Abs(model.PusherPosition - frame.GetProperty("stroke").GetDouble()) < 1e-9
                    && model.PusherExtended == frame.GetProperty("extended").GetBoolean()
                    && model.PusherRetracted == frame.GetProperty("retracted").GetBoolean()
                    && model.ObjectTransferred == frame.GetProperty("transferred").GetBoolean()
                    && model.State == frame.GetProperty("state").GetString(), $"{test.GetProperty("name")} snapshot at frame {frames}");
                frames++;
            }
            Console.WriteLine($"PASS Python reference: {test.GetProperty("name")}");
            cases++;
        }
        foreach (var delta in new[] { -1.0, double.NaN, double.PositiveInfinity })
        {
            var rejected = false;
            try { new ConveyorPlantModel(1, 0.5, 0.2, 0.5).Step(delta, true); }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "Invalid elapsed time must be rejected.");
        }
        Console.WriteLine($"PLANT_REFERENCE_PASS {cases} cases; {frames} Python snapshot comparisons; invalid time rejected; no PLC transport.");
    }
}
