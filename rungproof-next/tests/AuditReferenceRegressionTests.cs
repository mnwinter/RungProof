using System;
using System.IO;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RegisterAuditReferenceRegressionTests()
    {
        Test("audit supplied sum-counter reference validates and counts qualified edges", () =>
        {
            var loaded = LadderEditorProjectJson.Load(File.ReadAllText(Path.Combine(FindProjectRoot(),
                "programs", "examples", "09-sum-counter-reference.rpproj.json")));
            True(loaded.IsReadable);
            var runtime = Runtime(loaded.Document!.BuildProgram());
            runtime.Run();
            runtime.Scan(Inputs(("inputs_valid", false), ("calculate_request", false), ("call_complete", false)));
            var qualified = Inputs(("inputs_valid", true), ("calculate_request", true), ("call_complete", true));
            runtime.Scan(qualified, NumericInputs(("operand_a", 2), ("operand_b", 5)));
            Equal(7d, runtime.Snapshot.NumericOutputs["sum_result"]);
            Equal(1L, runtime.Snapshot.Counters["completed_calls"].Accumulated);
            runtime.Scan(qualified);
            Equal(1L, runtime.Snapshot.Counters["completed_calls"].Accumulated);
            runtime.Scan(Inputs(("inputs_valid", true), ("calculate_request", true), ("call_complete", false)));
            runtime.Scan(qualified);
            Equal(2L, runtime.Snapshot.Counters["completed_calls"].Accumulated);
            runtime.Reset();
            Equal(0L, runtime.Snapshot.Counters["completed_calls"].Accumulated);
        });
    }
}

