using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.Challenges;
using RungProof.Next.VirtualController;
using System.Text.Json;
namespace RungProof.Next.ChallengeTests;

internal static class ChallengeContractTests
{
    public static IEnumerable<(string, Action)> Tests()
    {
        yield return ("LogicalDepth16LegacyAndDefaultBlockCanValidateAndHash", () =>
        {
            var original = ChallengeFixtures.OutputSealIn();
            LadderProgram DeepProgram(int depth, bool withBlock)
            {
                var logic = new LadderNode("deep-contact", LadderNodeKind.Contact, "Start");
                for (var index = 1; index < depth; index++)
                    logic = new LadderNode("deep-series-" + index, LadderNodeKind.Series, Children: [logic]);
                var networks = new[] { original.Networks[0] with { Logic = logic } };
                var program = original with { Networks = networks };
                if (!withBlock) return program;
                var block = new LadderBlock("main", "Main", networks, LadderBlockType.OrganizationBlock);
                var task = new LadderTask("main-task", "MainTask", LadderTaskKind.Continuous, TimeSpan.FromMilliseconds(20), 10, "main");
                return program with { Blocks = [block], EntryBlock = block.Id, Tasks = [task] };
            }
            foreach (var withBlock in new[] { false, true })
            {
                var eligible = DeepProgram(16, withBlock);
                ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(eligible).Count == 0,
                    "Logical depth16 rejected during validation, block=" + withBlock);
                ChallengeFixtures.Require(ChallengeProgramIdentity.ProgramSha256(eligible).Length == 64,
                    "Eligible depth16 program cannot be hashed, block=" + withBlock);
                ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(DeepProgram(17, withBlock)).Count > 0,
                    "Logical depth17 must remain ineligible, block=" + withBlock);
            }
        });        yield return ("InternalMemoryAssignSealInAndRenamedFixtureEligible", () => { ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(ChallengeFixtures.InternalSealIn()).Count == 0, "Memory Assign seal-in rejected"); ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(ChallengeFixtures.InternalSealIn(true)).Count == 0, "Renamed memory Assign rejected"); }
        );
        yield return ("EligibilityResourceBoundsAndCyclicGraphFailClosed", () =>
        {
            var p = ChallengeFixtures.OutputSealIn();
            LadderNetwork Network(int i) => new("r" + i, "Request", new("c" + i, LadderNodeKind.Contact, "Start"), new("o" + i, "Run"));
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Networks = Enumerable.Range(0, 64).Select(Network).ToArray()
            }).Count == 0, "64 networks rejected");
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Networks = Enumerable.Range(0, 65).Select(Network).ToArray()
            }).Count > 0, "65 networks accepted");
            var variables = p.Variables.Concat(Enumerable.Range(0, 28).Select(i => new PlcVariable("M" + i, PlcVariableType.Bool, PlcVariableRole.Memory, false))).ToArray();
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Variables = variables
            }).Count == 0, "32 variables rejected");
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Variables = variables.Append(new("M29", PlcVariableType.Bool, PlcVariableRole.Memory, false)).ToArray()
            }).Count > 0, "33 variables accepted");
            LadderNode Wide(int count) => new("wide", LadderNodeKind.Series, Children: Enumerable.Range(0, count).Select(i => new LadderNode("leaf" + i, LadderNodeKind.Contact, "Start")).ToArray());
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Networks = [Network(0) with { Logic = Wide(255) }]
            }).Count == 0, "256 nodes rejected");
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Networks = [Network(0) with { Logic = Wide(256) }]
            }).Count > 0, "257 nodes accepted");
            LadderNode Deep(int depth)
            {
                var node = new LadderNode("leaf", LadderNodeKind.Contact, "Start");
                for (var i = 1; i < depth; i++)
                    node = new("d" + i, LadderNodeKind.Series, Children: [node]);
                return node;
            }
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Networks = [Network(0) with { Logic = Deep(16) }]
            }).Count == 0, "Depth16 rejected");
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Networks = [Network(0) with { Logic = Deep(17) }]
            }).Count > 0, "Depth17 accepted");
            var children = new List<LadderNode>();
            var cycle = new LadderNode("cycle", LadderNodeKind.Series, Children: children);
            children.Add(cycle);
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with
            {
                Networks = [Network(0) with { Logic = cycle }]
            }).Count > 0, "Cycle accepted");
        }
        );
        yield return ("BlockTaskBindingAndInitialPolicyRejectUnsupportedOriginal", () =>
        {
            var p = ChallengeFixtures.OutputSealIn();
            var block = new LadderBlock("main", "Main", p.Networks);
            var task = new LadderTask("task", "Main", LadderTaskKind.Continuous, TimeSpan.FromMilliseconds(20), 10, "main");
            var normal = p with
            {
                Blocks = [block],
                EntryBlock = "main",
                Tasks = [task]
            };
            ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(normal).Count == 0, "Normal Function task rejected");
            foreach (var bad in new[] { normal with { Blocks = [block with { BlockType = LadderBlockType.FunctionBlock }] }, normal with { Tasks = [task with { Priority = 1 }] }, normal with { Tasks = [task with { Kind = LadderTaskKind.Periodic }] }, normal with { Tasks = [task, task with { Id = "task2" }] }, normal with { Blocks = [block, block with { Id = "other" }] }, normal with { EntryBlock = "wrong" }, p with { ScanPeriod = TimeSpan.FromMilliseconds(21) }, p with { Variables = p.Variables.Select((v, i) => i == 0 ? v with { InitialValue = true } : v).ToArray() }, p with { Variables = p.Variables.Select((v, i) => i == 0 ? v with { Binding = "operator.stop" } : v).ToArray() }, p with { Variables = p.Variables.Select((v, i) => i == 2 ? v with { Role = PlcVariableRole.Output } : v).ToArray() } })
                ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(bad).Count > 0, "Unsupported original accepted");
            var cloned = LadderProgramJson.Load(LadderProgramJson.Save(normal with
            {
                Blocks = [block with { BlockType = LadderBlockType.FunctionBlock }]
            }));
            ChallengeFixtures.Require(cloned.Program?.Blocks?[0].BlockType == LadderBlockType.Function, "Fixture must demonstrate serializer loses block type");
        }
        );
        yield return ("DefinitionExactCaseCountTextAndFixedResourceLimits", () =>
        {
            var json = System.Text.Json.Nodes.JsonNode.Parse(ChallengeFixtures.DefinitionJson)!;
            var cases = json["cases"]!.AsArray();
            cases.RemoveAt(6);
            ChallengeFixtures.Require(ChallengeDefinitionJson.Load(json.ToJsonString()).Definition is null, "Six cases accepted");
            json = System.Text.Json.Nodes.JsonNode.Parse(ChallengeFixtures.DefinitionJson)!;
            json["cases"]!.AsArray().Add(json["cases"]![0]!.DeepClone());
            ChallengeFixtures.Require(ChallengeDefinitionJson.Load(json.ToJsonString()).Definition is null, "Eight cases accepted");
            foreach (var property in new[] { "title", "objective" })
            {
                json = System.Text.Json.Nodes.JsonNode.Parse(ChallengeFixtures.DefinitionJson)!;
                json[property] = new string('x', 513);
                ChallengeFixtures.Require(ChallengeDefinitionJson.Load(json.ToJsonString()).Definition is null, "Oversize text accepted");
            }
            json = System.Text.Json.Nodes.JsonNode.Parse(ChallengeFixtures.DefinitionJson)!;
            json["hints"]![0] = new string('x', 2049);
            ChallengeFixtures.Require(ChallengeDefinitionJson.Load(json.ToJsonString()).Definition is null, "Oversize hint accepted");
            json = System.Text.Json.Nodes.JsonNode.Parse(ChallengeFixtures.DefinitionJson)!;
            for (var i = 0; i < 8; i++)
                json["hints"]!.AsArray().Add("extra");
            ChallengeFixtures.Require(ChallengeDefinitionJson.Load(json.ToJsonString()).Definition is null, "Nine hints accepted");
            ChallengeFixtures.Require(ChallengeDefinitionJson.Load(ChallengeFixtures.DefinitionJson.Replace("res://challenges/starters/conveyor-stop.rpproj.json", "../starter.json")).Definition is null, "Starter traversal accepted");
            var definition = ChallengeDefinitionJson.Load(ChallengeFixtures.DefinitionJson).Definition!;
            ChallengeFixtures.Require(ChallengeDefinitionJson.ValidateStarter(definition, new byte[100]).Count > 0, "Wrong starter hash accepted");
        }
        );
        yield return ("CanonicalDuplicateDepthFiniteEnvelopeAndKnownDigest", () =>
        {
            ChallengeFixtures.Require(ChallengeProgramIdentity.CanonicalSha256("{}") == "44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a", "Known digest differs");
            ChallengeFixtures.Require(ChallengeProgramIdentity.CanonicalJson("1e-07") == "1e-7", "Exponent normalization failed");
            foreach (var bad in new[] { "{\"a\":1,\"a\":2}", "1e999", "9007199254740992", new string('[', 33) + "0" + new string(']', 33) })
            {
                try
                {
                    ChallengeProgramIdentity.CanonicalJson(bad);
                    throw new InvalidOperationException("Unbounded canonical JSON accepted");
                }
                catch (Exception error) when (error is ArgumentException or System.Text.Json.JsonException) { }
            }
        }
        );
        yield return ("UnsolvedStarterRemainsEditableAndSaveable", () =>
        {
            var root = Environment.GetEnvironmentVariable("RUNGPROOF_CHALLENGE_SOURCE_ROOT") ?? System.IO.Directory.GetCurrentDirectory();
            var bytes = System.IO.File.ReadAllBytes(System.IO.Path.Combine(root, "challenges/starters/conveyor-stop.rpproj.json"));
            var packet = ChallengeDefinitionJson.Load(System.IO.File.ReadAllText(System.IO.Path.Combine(root, "challenges/conveyor-stop.challenge.json"))).Definition ?? throw new InvalidOperationException("Authored packet unreadable");
            ChallengeFixtures.Require(ChallengeDefinitionJson.ValidateStarter(packet, bytes).Count == 0, "Starter provenance mismatch");
            var result = LadderEditorProjectJson.Load(System.Text.Encoding.UTF8.GetString(bytes));
            ChallengeFixtures.Require(result.IsReadable, "Starter cannot open");
            var draft = result.Document!;
            ChallengeFixtures.Require(LadderCompiler.Validate(draft.BuildProgram()).Count > 0, "Starter includes completed executable solution");
            ChallengeFixtures.Require(LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(draft)).IsReadable, "Invalid draft round trip lost");
            ChallengeFixtures.Require(draft.Tags.Count == 4 && draft.Tags.All(v => v.InitialValue is false), "Starter tags/initials incorrect");
            var catalog = ChallengeDefinitionJson.LoadCatalog(System.IO.File.ReadAllText(System.IO.Path.Combine(root, "challenges/catalog.json")));
            ChallengeFixtures.Require(catalog[packet.Id] == ChallengeDefinitionJson.DefinitionResource, "Catalog lookup mismatch");
        }
        );
        yield return ("DefinitionRequiresExactlySevenCases", () => { ChallengeFixtures.Require(ChallengeDefinitionJson.Load(ChallengeFixtures.DefinitionJson).Definition?.Cases.Count == 7, "Valid packet rejected"); ChallengeFixtures.Require(ChallengeDefinitionJson.Load(ChallengeFixtures.DefinitionJson.Replace("ResetRepeat", "ColdStart")).Definition is null, "Duplicate case accepted"); }
        );
        yield return ("DefinitionRejectsDuplicateUnknownFieldsAndLimits", () => { foreach (var json in new[] { ChallengeFixtures.DefinitionJson.Replace("\"revision\":1", "\"revision\":1,\"revision\":1"), ChallengeFixtures.DefinitionJson.Replace("\"speed\":0.5", "\"speed\":0.5,\"extra\":0"), ChallengeFixtures.DefinitionJson.Replace("\"maximumScans\":250", "\"maximumScans\":251"), ChallengeFixtures.DefinitionJson.Replace("conveyor-v1", "future-v2"), ChallengeFixtures.DefinitionJson + new string(' ', 262144) }) ChallengeFixtures.Require(ChallengeDefinitionJson.Load(json).Definition is null, "Invalid packet accepted"); }
        );
        yield return ("DefaultEditorOrganizationContinuousTaskAndLegacyEligible", () => { var p = ChallengeFixtures.OutputSealIn(); ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p).Count == 0, "Legacy rejected"); var d = new LadderEditorDocument(); d.AddBlock("Main"); d.Tags.AddRange(p.Variables); d.AddRung("Request", "Run"); d.AddContact(0, 0, "Start", false); ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(d.BuildProgram()).Count == 0, "Actual default editor rejected"); }
        );
        yield return ("OriginalHiddenInterfaceRejectedBeforeClone", () => { var p = ChallengeFixtures.OutputSealIn(); var block = new LadderBlock("main", "Main", p.Networks, Interface: [new("Hidden", PlcVariableType.Bool, LadderInterfaceSection.Temp, false)]); p = p with { Blocks = [block], EntryBlock = "main" }; ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p).Count > 0, "Original interface accepted"); try { ChallengeProgramIdentity.ProgramSha256(p); throw new InvalidOperationException("Identity erased unsupported original"); } catch (ArgumentException) { } }
        );
        yield return ("RenamedTagsKeepRequiredBindings", () => ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(ChallengeFixtures.OutputSealIn(true)).Count == 0, "Alias rejected"));
        yield return ("ForbiddenUnusedInstructionAndShadowNetworksRejected", () => { var p = ChallengeFixtures.OutputSealIn(); var n = p.Networks[0]; ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with { Networks = [n with { Timer = new("hidden", "Start", TimeSpan.Zero) }] }).Count > 0, "Hidden timer accepted"); ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with { Networks = [n with { Logic = n.Logic with { RightOperand = "unused" } }] }).Count > 0, "Unused operand accepted"); ChallengeFixtures.Require(ChallengeEligibilityValidator.Validate(p with { Blocks = [new("main", "Main", [n with { Label = "shadow" }])], EntryBlock = "main" }).Count > 0, "Shadow networks accepted"); }
        );
        yield return ("CanonicalIdentityNormalizesPropertiesNumbersAndRootDisplayMetadata", () => { ChallengeFixtures.Require(ChallengeProgramIdentity.CanonicalJson("{\"z\":-0.0,\"a\":1e+02}") == "{\"a\":100,\"z\":0}", "Canonical numbers failed"); var p = ChallengeFixtures.OutputSealIn(); ChallengeFixtures.Require(ChallengeProgramIdentity.ProgramSha256(p) == ChallengeProgramIdentity.ProgramSha256(p with { Id = "display", Name = "Other", WatchVariables = ["Start"] }), "Display changed proof"); }
        );
        yield return ("ExecutionOrderAndAliasesChangeIdentity", () => { var p = ChallengeFixtures.OutputSealIn(); var h = ChallengeProgramIdentity.ProgramSha256(p); ChallengeFixtures.Require(h != ChallengeProgramIdentity.ProgramSha256(p with { Variables = p.Variables.Reverse().ToArray() }), "Order lost"); ChallengeFixtures.Require(h != ChallengeProgramIdentity.ProgramSha256(ChallengeFixtures.OutputSealIn(true)), "Rename lost"); }
        );
    }
}

internal static class ChallengeManifestTests
{
    public static IEnumerable<(string, Action)> Tests()
    {
        yield return ("ManifestVerifiesCanonicalEntryHashesAndRequiredSubsets", () =>
        {
            var entries = new[] { new ChallengeSemanticSourceEntry("src/Challenges/ChallengeEvaluator.cs", new string('1', 64), 1), new ChallengeSemanticSourceEntry("src/Challenges/ChallengeProgramIdentity.cs", new string('2', 64), 2), new ChallengeSemanticSourceEntry("src/Challenges/ChallengeTestRunner.cs", new string('3', 64), 3), new ChallengeSemanticSourceEntry("src/Challenges/ConveyorChallengeProtocol.cs", new string('4', 64), 4), new ChallengeSemanticSourceEntry("src/Scenes/ConveyorPlantModel.cs", new string('5', 64), 5), new ChallengeSemanticSourceEntry("src/VirtualController/LadderProgram.cs", new string('6', 64), 6) };
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var evaluator = entries.Where(e => e.RelativePath.EndsWith("ChallengeEvaluator.cs", StringComparison.Ordinal) || e.RelativePath.EndsWith("ChallengeTestRunner.cs", StringComparison.Ordinal) || e.RelativePath.EndsWith("ConveyorChallengeProtocol.cs", StringComparison.Ordinal)).ToArray();
            var json = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                identityFormat = "RP-CANON-1",
                engineRevision = "conveyor-engine-v1",
                engineSha256 = ChallengeProgramIdentity.CanonicalSha256(JsonSerializer.Serialize(entries, options)),
                evaluatorRevision = "conveyor-evaluator-v1",
                evaluatorSha256 = ChallengeProgramIdentity.CanonicalSha256(JsonSerializer.Serialize(evaluator, options)),
                generatorSha256 = new string('a', 64),
                engineInputs = entries,
                evaluatorInputs = evaluator
            }, options);
            ChallengeFixtures.Require(ChallengeSemanticManifest.Load(json).Manifest is not null, "Valid synthetic complete manifest rejected");
            ChallengeFixtures.Require(ChallengeSemanticManifest.Load(json.Replace("src/Scenes/ConveyorPlantModel.cs", "../ConveyorPlantModel.cs")).Manifest is null, "Traversal accepted");
            ChallengeFixtures.Require(ChallengeSemanticManifest.Load(json.Replace("conveyor-engine-v1", "future")).Manifest is null, "Future revision accepted");
            ChallengeFixtures.Require(ChallengeSemanticManifest.Load(json.Replace(new string('a', 64), new string('A', 64))).Manifest is null, "Noncanonical hash accepted");
        }
        );
        yield return ("ContractsOwnPrivateCollectionCopies", () => { var hints = new List<string> { "original" }; var d = ChallengeDefinitionJson.Load(ChallengeFixtures.DefinitionJson).Definition!; var copy = new ChallengeDefinition(d.SchemaVersion, d.Id, d.Revision, d.SceneId, d.ProtocolRevision, d.ScanPeriodMs, d.Title, d.Objective, hints, d.Plant, d.Cases, d.Glossary, d.EligibilityPolicy, d.Starter); hints[0] = "mutated"; ChallengeFixtures.Require(copy.Hints[0] == "original", "Aliased packet list"); }
        );
    }
}







