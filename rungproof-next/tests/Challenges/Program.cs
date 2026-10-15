using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RungProof.Next.Challenges;
namespace RungProof.Next.ChallengeTests;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--validate-manifest")
        {
            var result = ChallengeSemanticManifest.Load(File.ReadAllText(args[1]));
            if (result.Manifest is not { } manifest)
            {
                Console.WriteLine("CHALLENGE_MANIFEST_FAIL " + string.Join("; ", result.Issues));
                return 1;
            }
            Console.WriteLine($"CHALLENGE_MANIFEST_PASS engine={manifest.EngineSha256} evaluator={manifest.EvaluatorSha256}");
            return 0;
        }
        if (args.Length != 0)
        {
            Console.WriteLine("Unknown test arguments.");
            return 2;
        }
        var failures = 0;
        foreach (var (name, test) in ChallengeContractTests.Tests().Concat(ChallengeManifestTests.Tests()).Concat(ChallengeBoundaryTests.Tests()).Concat(ChallengeProgressTests.Tests()).Concat(ChallengeBehaviorTests.Tests()).Concat(ChallengeIsolationTests.Tests()))
        {
            try
            {
                test();
                Console.WriteLine("PASS " + name);
            }
            catch (Exception error) { failures++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
        }
        Console.WriteLine(failures == 0 ? "CHALLENGE_TESTS_PASS" : "CHALLENGE_TESTS_FAIL");
        return failures == 0 ? 0 : 1;
    }
}


