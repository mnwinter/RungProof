using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace RungProof.Next.VirtualController;

public sealed record LadderProgramLoadResult(
    LadderProgram? Program,
    IReadOnlyList<LadderValidationIssue> Issues
)
{
    public bool IsValid => Program is not null && Issues.Count == 0;
}

public static partial class LadderProgramJson
{
    public static string Save(LadderProgram program)
    {
        static Dictionary<string, object?> Node(LadderNode node)
        {
            var result = new Dictionary<string, object?>
            {
                ["id"] = node.Id,
                ["kind"] = node.Kind.ToString().ToLowerInvariant(),
            };
            if (node.Kind == LadderNodeKind.Contact)
            {
                result["variable"] = node.Variable;
                result["contact"] = node.NormallyClosed ? "normallyClosed" : "normallyOpen";
                if (node.EdgeMode != LadderEdgeMode.None)
                    result["edge"] = node.EdgeMode.ToString().ToLowerInvariant();
            }
            else if (node.Kind == LadderNodeKind.Compare)
            {
                result["left"] = node.Variable;
                result["operator"] = node.CompareOperator.ToString();
                result["right"] = node.RightOperand;
            }
            else
            {
                result["children"] = (node.Children ?? []).Select(Node).ToArray();
            }
            return result;
        }
        Dictionary<string, object?> Network(LadderNetwork network)
        {
            var result = new Dictionary<string, object?>
            {
                ["id"] = network.Id,
                ["label"] = network.Label,
                ["logic"] = Node(network.Logic),
            };
            if (network.Coil is not null)
            {
                result["coil"] = new Dictionary<string, object?>
                {
                    ["id"] = network.Coil.Id,
                    ["variable"] = network.Coil.Variable,
                    ["mode"] = network.Coil.Mode.ToString().ToLowerInvariant(),
                };
            }
            if (network.Timer is not null)
            {
                result["timer"] = new Dictionary<string, object?>
                {
                    ["id"] = network.Timer.Id,
                    ["variable"] = network.Timer.Variable,
                    ["presetMs"] = network.Timer.Preset.TotalMilliseconds,
                    ["kind"] = network.Timer.Kind.ToString(),
                };
            }
            if (network.TimerReset is not null)
            {
                result["timerReset"] = new Dictionary<string, object?>
                {
                    ["id"] = network.TimerReset.Id,
                    ["variable"] = network.TimerReset.Variable,
                };
            }
            if (network.Counter is not null)
            {
                result["counter"] = new Dictionary<string, object?>
                {
                    ["id"] = network.Counter.Id,
                    ["variable"] = network.Counter.Variable,
                    ["preset"] = network.Counter.Preset,
                    ["kind"] = network.Counter.Kind.ToString(),
                };
            }
            if (network.CounterReset is not null)
            {
                result["counterReset"] = new Dictionary<string, object?>
                {
                    ["id"] = network.CounterReset.Id,
                    ["variable"] = network.CounterReset.Variable,
                };
            }
            if (network.CounterLoad is not null)
            {
                result["counterLoad"] = new Dictionary<string, object?>
                {
                    ["id"] = network.CounterLoad.Id,
                    ["variable"] = network.CounterLoad.Variable,
                    ["preset"] = network.CounterLoad.Preset,
                };
            }
            if (network.NumericOperation is not null)
            {
                result["numericOperation"] = new Dictionary<string, object?>
                {
                    ["id"] = network.NumericOperation.Id,
                    ["operation"] = network.NumericOperation.Kind.ToString(),
                    ["sourceA"] = network.NumericOperation.SourceA,
                    ["sourceB"] = network.NumericOperation.SourceB,
                    ["sourceC"] = network.NumericOperation.SourceC,
                    ["destination"] = network.NumericOperation.Destination,
                };
            }
            if (network.Call is not null)
            {
                result["call"] = new Dictionary<string, object?>
                {
                    ["id"] = network.Call.Id,
                    ["targetBlock"] = network.Call.TargetBlock,
                    ["semanticMode"] = network.Call.SemanticMode.ToString(),
                    ["bindings"] = network.Call.Bindings,
                    ["instanceId"] = network.Call.InstanceId,
                };
            }
            if (network.Return is not null)
            {
                result["return"] = new Dictionary<string, object?>
                {
                    ["id"] = network.Return.Id,
                };
            }
            if (network.Jump is not null)
            {
                result["jump"] = new Dictionary<string, object?>
                {
                    ["id"] = network.Jump.Id,
                    ["targetLabel"] = network.Jump.TargetLabel,
                };
            }
            if (network.LabelInstruction is not null)
            {
                result["label"] = new Dictionary<string, object?>
                {
                    ["id"] = network.LabelInstruction.Id,
                    ["name"] = network.LabelInstruction.Name,
                };
            }
            return result;
        }

        var document = new Dictionary<string, object?>
        {
            ["schemaVersion"] = program.SchemaVersion,
            ["id"] = program.Id,
            ["name"] = program.Name,
            ["language"] = program.Language,
            ["scanPeriodMs"] = program.ScanPeriod.TotalMilliseconds,
            ["variables"] = program.Variables.Select(variable => new Dictionary<string, object?>
            {
                ["name"] = variable.Name,
                ["type"] = variable.Type.ToString().ToUpperInvariant(),
                ["role"] = variable.Role.ToString().ToLowerInvariant(),
                ["initial"] = variable.InitialValue,
                ["binding"] = variable.Binding,
                ["aggregate"] = variable.Aggregate,
                ["enumMembers"] = variable.EnumMembers,
                ["symbolId"] = variable.Id,
            }).ToArray(),
            ["watchVariables"] = (program.WatchVariables ?? []).ToArray(),
            ["networks"] = program.Networks.Select(Network).ToArray(),
            ["instances"] = program.Instances,
        };
        if (program.Blocks is { Count: > 0 })
        {
            document["entryBlock"] = program.EntryBlock;
            document["blocks"] = program.Blocks.Select(block => new Dictionary<string, object?>
            {
                ["id"] = block.Id,
                ["name"] = block.Name,
                ["blockType"] = block.BlockType.ToString(),
                ["semanticMode"] = block.SemanticMode.ToString(),
                ["interface"] = block.Interface,
                ["networks"] = block.Networks.Select(Network).ToArray(),
            }).ToArray();
        }
        if (program.Tasks is { Count: > 0 })
        {
            document["tasks"] = program.Tasks.Select(task => new Dictionary<string, object?>
            {
                ["id"] = task.Id,
                ["name"] = task.Name,
                ["kind"] = task.Kind.ToString().ToLowerInvariant(),
                ["periodMs"] = task.Period.TotalMilliseconds,
                ["priority"] = task.Priority,
                ["entryBlock"] = task.EntryBlock,
            }).ToArray();
        }
        if (program.SchemaVersion == 1) RemoveVersionTwoMetadata(document);
        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true, MaxDepth = 256 });
    }

    public static LadderProgramLoadResult Load(string json)
    {
        var issues = new List<LadderValidationIssue>();
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 256 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Invalid("VC100", "$", "Program document must be a JSON object.");

            var variables = new List<PlcVariable>();
            if (!root.TryGetProperty("variables", out var variableArray)
                || variableArray.ValueKind != JsonValueKind.Array)
            {
                issues.Add(new("VC100", "$.variables", "variables must be an array."));
            }
            else
            {
                var index = 0;
                foreach (var variable in variableArray.EnumerateArray())
                {
                    var path = $"$.variables[{index++}]";
                    if (variable.ValueKind != JsonValueKind.Object)
                    {
                        issues.Add(new("VC100", path, "Variable must be an object."));
                        continue;
                    }
                    var name = Text(variable, "name");
                    var type = ParseType(Text(variable, "type"), path, issues);
                    var role = ParseRole(Text(variable, "role"), path, issues);
                    var initial = variable.TryGetProperty("initial", out var initialValue)
                        ? JsonValue(initialValue)
                        : false;
                    string[]? enumMembers = null;
                    if (variable.TryGetProperty("enumMembers", out var members) && members.ValueKind != JsonValueKind.Null)
                    {
                        if (members.ValueKind != JsonValueKind.Array
                            || members.EnumerateArray().Any(member => member.ValueKind != JsonValueKind.String))
                            issues.Add(new("VC100", path + ".enumMembers", "enumMembers must be an array of strings."));
                        else
                            enumMembers = members.EnumerateArray().Select(member => member.GetString()!).ToArray();
                    }
                    variables.Add(new PlcVariable(name, type, role, initial, Text(variable, "binding"), variable.TryGetProperty("aggregate", out var schema) && schema.ValueKind == JsonValueKind.Object ? PlcAggregates.ParseSchema(schema) : null,
                        enumMembers, Text(variable, "symbolId")));
                }
            }

            var networks = new List<LadderNetwork>();
            if (!root.TryGetProperty("networks", out var networkArray)
                || networkArray.ValueKind != JsonValueKind.Array)
            {
                issues.Add(new("VC100", "$.networks", "networks must be an array."));
            }
            else
            {
                var index = 0;
                foreach (var network in networkArray.EnumerateArray())
                {
                    var path = $"$.networks[{index++}]";
                    networks.Add(ParseNetwork(network, path, issues));
                }
            }

            var blocks = new List<LadderBlock>();
            if (root.TryGetProperty("blocks", out var blockArray) && blockArray.ValueKind == JsonValueKind.Array)
            {
                var blockIndex = 0;
                foreach (var block in blockArray.EnumerateArray())
                {
                    var path = $"$.blocks[{blockIndex++}]";
                    var blockNetworks = new List<LadderNetwork>();
                    if (!block.TryGetProperty("networks", out var blockNetworkArray)
                        || blockNetworkArray.ValueKind != JsonValueKind.Array)
                        issues.Add(new("VC100", $"{path}.networks", "Block networks must be an array."));
                    else
                    {
                        var networkIndex = 0;
                        foreach (var network in blockNetworkArray.EnumerateArray())
                            blockNetworks.Add(ParseNetwork(network, $"{path}.networks[{networkIndex++}]", issues));
                    }
                    blocks.Add(new LadderBlock(Text(block, "id"), Text(block, "name"), blockNetworks,
                        ReadEnum(block, "blockType", LadderBlockType.Function, path, issues),
                        ReadParameters(block, path, issues),
                        ReadEnum(block, "semanticMode", LadderSemanticMode.Legacy, path, issues)));
                }
            }
            var tasks = new List<LadderTask>();
            if (root.TryGetProperty("tasks", out var taskArray) && taskArray.ValueKind == JsonValueKind.Array)
            {
                var taskIndex = 0;
                foreach (var task in taskArray.EnumerateArray())
                {
                    var path = $"$.tasks[{taskIndex++}]";
                    var kindText = Text(task, "kind");
                    if (!Enum.TryParse<LadderTaskKind>(kindText, true, out var kind))
                    {
                        issues.Add(new("VC100", $"{path}.kind", $"Unknown task kind '{kindText}'."));
                        kind = LadderTaskKind.Continuous;
                    }
                    tasks.Add(new LadderTask(
                        Text(task, "id"),
                        Text(task, "name"),
                        kind,
                        TimeSpan.FromMilliseconds(Number(task, "periodMs", 0)),
                        Integer(task, "priority", 0),
                        Text(task, "entryBlock")));
                }
            }
            var watchVariables = new List<string>();
            if (root.TryGetProperty("watchVariables", out var watchArray))
            {
                if (watchArray.ValueKind != JsonValueKind.Array)
                    issues.Add(new("VC100", "$.watchVariables", "watchVariables must be an array."));
                else
                {
                    var watchIndex = 0;
                    foreach (var item in watchArray.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                            issues.Add(new("VC100", $"$.watchVariables[{watchIndex}]", "Watch-table symbol must be a string."));
                        else
                            watchVariables.Add(item.GetString() ?? string.Empty);
                        watchIndex++;
                    }
                }
            }

            if (issues.Count > 0) return new(null, issues);
            var scanMs = Number(root, "scanPeriodMs", 20.0);
            var program = new LadderProgram(
                Integer(root, "schemaVersion", 0),
                Text(root, "id"),
                Text(root, "name"),
                Text(root, "language"),
                TimeSpan.FromMilliseconds(scanMs),
                variables,
                networks,
                blocks,
                Text(root, "entryBlock"),
                tasks,
                watchVariables, ReadRecords<LadderInstance>(root, "instances", "$", issues));
            if (issues.Count > 0) return new(null, issues);
            var validation = LadderCompiler.Validate(program);
            return validation.Count == 0 ? new(program, []) : new(null, validation);
        }
        catch (JsonException exception)
        {
            return Invalid("VC100", "$", $"Malformed JSON: {exception.Message}");
        }
    }

    private static LadderNetwork ParseNetwork(
        JsonElement network,
        string path,
        ICollection<LadderValidationIssue> issues)
    {
        if (network.ValueKind != JsonValueKind.Object)
        {
            issues.Add(new("VC100", path, "Network must be an object."));
            return new LadderNetwork(string.Empty, string.Empty,
                InvalidNode($"{path}.logic", "Network requires logic.", issues));
        }
        var logic = network.TryGetProperty("logic", out var logicElement)
            ? ParseNode(logicElement, $"{path}.logic", issues)
            : InvalidNode($"{path}.logic", "Network requires logic.", issues);
        LadderCoil? coil = null;
        LadderTimer? timer = null;
        LadderTimerReset? timerReset = null;
        LadderCounter? counter = null;
        LadderCounterReset? counterReset = null;
        LadderCounterLoad? counterLoad = null;
        LadderNumericOperation? numericOperation = null;
        LadderCall? call = null;
        LadderReturn? returnInstruction = null;
        LadderJump? jump = null;
        LadderLabel? label = null;
        if (network.TryGetProperty("coil", out var coilElement) && coilElement.ValueKind == JsonValueKind.Object)
        {
            var modeText = Text(coilElement, "mode");
            var mode = modeText.ToLowerInvariant() switch
            {
                "" or "assign" => LadderCoilMode.Assign,
                "set" => LadderCoilMode.Set,
                "reset" => LadderCoilMode.Reset,
                _ => LadderCoilMode.Assign,
            };
            if (modeText is not ("" or "assign" or "set" or "reset"))
                issues.Add(new("VC100", $"{path}.coil.mode", $"Unknown coil mode '{modeText}'."));
            coil = new LadderCoil(Text(coilElement, "id"), Text(coilElement, "variable"), mode);
        }
        if (network.TryGetProperty("timer", out var timerElement) && timerElement.ValueKind == JsonValueKind.Object)
        {
            var kindText = Text(timerElement, "kind");
            if (string.IsNullOrWhiteSpace(kindText)) kindText = nameof(LadderTimerKind.OnDelay);
            if (!Enum.TryParse<LadderTimerKind>(kindText, true, out var timerKind))
            {
                issues.Add(new("VC100", $"{path}.timer.kind", $"Unknown timer kind '{kindText}'."));
                timerKind = LadderTimerKind.OnDelay;
            }
            timer = new LadderTimer(Text(timerElement, "id"), Text(timerElement, "variable"),
                TimeSpan.FromMilliseconds(Number(timerElement, "presetMs", 0)), timerKind);
        }
        if (network.TryGetProperty("timerReset", out var timerResetElement)
            && timerResetElement.ValueKind == JsonValueKind.Object)
            timerReset = new LadderTimerReset(Text(timerResetElement, "id"), Text(timerResetElement, "variable"));
        if (network.TryGetProperty("counter", out var counterElement) && counterElement.ValueKind == JsonValueKind.Object)
        {
            var kindText = Text(counterElement, "kind");
            if (string.IsNullOrWhiteSpace(kindText)) kindText = nameof(LadderCounterKind.CountUp);
            if (!Enum.TryParse<LadderCounterKind>(kindText, true, out var counterKind))
            {
                issues.Add(new("VC100", $"{path}.counter.kind", $"Unknown counter kind '{kindText}'."));
                counterKind = LadderCounterKind.CountUp;
            }
            counter = new LadderCounter(Text(counterElement, "id"), Text(counterElement, "variable"),
                Long(counterElement, "preset", 0), counterKind);
        }
        if (network.TryGetProperty("counterReset", out var resetElement) && resetElement.ValueKind == JsonValueKind.Object)
            counterReset = new LadderCounterReset(Text(resetElement, "id"), Text(resetElement, "variable"));
        if (network.TryGetProperty("counterLoad", out var loadElement) && loadElement.ValueKind == JsonValueKind.Object)
            counterLoad = new LadderCounterLoad(Text(loadElement, "id"), Text(loadElement, "variable"),
                Long(loadElement, "preset", 0));
        if (network.TryGetProperty("numericOperation", out var numericElement) && numericElement.ValueKind == JsonValueKind.Object)
        {
            var operationText = Text(numericElement, "operation");
            if (!Enum.TryParse<LadderNumericOperationKind>(operationText, true, out var operationKind))
            {
                issues.Add(new("VC100", $"{path}.numericOperation.operation", $"Unknown numeric operation '{operationText}'."));
                operationKind = LadderNumericOperationKind.Move;
            }
            var sourceC = numericElement.TryGetProperty("sourceC", out var sourceCElement)
                ? sourceCElement.GetString() ?? "0"
                : "0";
            numericOperation = new LadderNumericOperation(
                Text(numericElement, "id"), operationKind, Text(numericElement, "sourceA"),
                Text(numericElement, "sourceB"), Text(numericElement, "destination"), sourceC);
        }
        if (network.TryGetProperty("call", out var callElement) && callElement.ValueKind == JsonValueKind.Object)
            call = new LadderCall(Text(callElement, "id"), Text(callElement, "targetBlock"),
                ReadEnum(callElement, "semanticMode", LadderSemanticMode.Legacy, path + ".call", issues),
                ReadBindings(callElement, path + ".call", issues), Text(callElement, "instanceId"));
        if (network.TryGetProperty("return", out var returnElement) && returnElement.ValueKind == JsonValueKind.Object)
            returnInstruction = new LadderReturn(Text(returnElement, "id"));
        if (network.TryGetProperty("jump", out var jumpElement) && jumpElement.ValueKind == JsonValueKind.Object)
            jump = new LadderJump(Text(jumpElement, "id"), Text(jumpElement, "targetLabel"));
        if (network.TryGetProperty("label", out var labelElement) && labelElement.ValueKind == JsonValueKind.Object)
            label = new LadderLabel(Text(labelElement, "id"), Text(labelElement, "name"));
        return new LadderNetwork(
            Text(network, "id"), Text(network, "label"), logic, coil, timer, counter,
            counterReset, numericOperation, call, returnInstruction, counterLoad, timerReset, jump, label);
    }

    private static LadderNode ParseNode(
        JsonElement element,
        string path,
        ICollection<LadderValidationIssue> issues)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return InvalidNode(path, "Logic node must be an object.", issues);
        var kindText = Text(element, "kind");
        var kind = kindText.ToLowerInvariant() switch
        {
            "contact" => LadderNodeKind.Contact,
            "compare" => LadderNodeKind.Compare,
            "series" => LadderNodeKind.Series,
            "parallel" => LadderNodeKind.Parallel,
            _ => LadderNodeKind.Series,
        };
        if (kindText is not ("contact" or "compare" or "series" or "parallel"))
            issues.Add(new("VC100", $"{path}.kind", $"Unknown logic kind '{kindText}'."));

        var children = new List<LadderNode>();
        if (kind is not (LadderNodeKind.Contact or LadderNodeKind.Compare))
        {
            if (!element.TryGetProperty("children", out var childArray)
                || childArray.ValueKind != JsonValueKind.Array)
            {
                issues.Add(new("VC003", $"{path}.children", $"{kindText} requires a children array."));
            }
            else
            {
                var index = 0;
                foreach (var child in childArray.EnumerateArray())
                    children.Add(ParseNode(child, $"{path}.children[{index++}]", issues));
            }
        }

        var contact = Text(element, "contact");
        if (kind == LadderNodeKind.Contact
            && !contact.Equals("normallyOpen", StringComparison.OrdinalIgnoreCase)
            && !contact.Equals("normallyClosed", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new("VC100", $"{path}.contact",
                $"Unknown contact type '{contact}'; expected normallyOpen or normallyClosed."));
        }

        var comparisonText = Text(element, "operator");
        var comparison = LadderCompareOperator.Equal;
        if (kind == LadderNodeKind.Compare
            && !Enum.TryParse(comparisonText, true, out comparison))
            issues.Add(new("VC100", $"{path}.operator", $"Unknown comparison operator '{comparisonText}'."));

        var edgeText = Text(element, "edge");
        var edgeMode = LadderEdgeMode.None;
        if (kind == LadderNodeKind.Contact && edgeText.Length > 0
            && !Enum.TryParse(edgeText, true, out edgeMode))
            issues.Add(new("VC100", $"{path}.edge", $"Unknown edge mode '{edgeText}'; expected rising or falling."));

        return new LadderNode(
            Text(element, "id"),
            kind,
            kind == LadderNodeKind.Compare ? Text(element, "left") : Text(element, "variable"),
            contact.Equals("normallyClosed", StringComparison.OrdinalIgnoreCase),
            children,
            comparison,
            Text(element, "right"),
            edgeMode);
    }

    private static LadderNode InvalidNode(
        string path,
        string message,
        ICollection<LadderValidationIssue> issues)
    {
        issues.Add(new("VC100", path, message));
        return new LadderNode(string.Empty, LadderNodeKind.Series, Children: []);
    }

    private static PlcVariableType ParseType(
        string value,
        string path,
        ICollection<LadderValidationIssue> issues)
    {
        if (Enum.TryParse<PlcVariableType>(value, true, out var result)) return result;
        issues.Add(new("VC100", $"{path}.type", $"Unknown variable type '{value}'."));
        return PlcVariableType.Bool;
    }

    private static PlcVariableRole ParseRole(
        string value,
        string path,
        ICollection<LadderValidationIssue> issues)
    {
        if (Enum.TryParse<PlcVariableRole>(value, true, out var result)) return result;
        issues.Add(new("VC100", $"{path}.role", $"Unknown variable role '{value}'."));
        return PlcVariableRole.Input;
    }

    private static LadderProgramLoadResult Invalid(string code, string path, string message) =>
        new(null, [new LadderValidationIssue(code, path, message)]);

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static int Integer(JsonElement element, string name, int fallback) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result)
            ? result
            : fallback;

    private static double Number(JsonElement element, string name, double fallback) =>
        element.TryGetProperty(name, out var value) && value.TryGetDouble(out var result)
            ? result
            : fallback;

    private static long Long(JsonElement element, string name, long fallback) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result)
            ? result
            : fallback;

    private static object JsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Object or JsonValueKind.Array => value.Clone(),
        _ => value.GetRawText(),
    };
}

