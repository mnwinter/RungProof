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
        ArgumentNullException.ThrowIfNull(program);
        static bool HasOpenEnd(LadderNode node) => node.OpenEnd || (node.Children ?? []).Any(HasOpenEnd);
        if ((program.Blocks ?? []).SelectMany(block => block.Networks).Concat(program.Networks)
            .Any(network => HasOpenEnd(network.Logic)))
            throw new InvalidOperationException("Executable program export rejects an open parallel branch; close the branch in the editor first.");
        var hasVersionTwoMetadata = program.SchemaVersion >= 2;
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
                if (!string.IsNullOrWhiteSpace(node.EdgeMemoryOperand))
                    result["edgeMemory"] = node.EdgeMemoryOperand;
            }
            else if (node.Kind == LadderNodeKind.Compare)
            {
                result["left"] = node.Variable;
                result["operator"] = node.CompareOperator.ToString();
                result["right"] = node.RightOperand;
                if (node.ComparisonValueType != LadderComparisonValueType.Unspecified)
                    result["dataType"] = node.ComparisonValueType.ToString();
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
            static Dictionary<string, object?>? InstanceDataBlock(LadderInstanceDataBlock? db) => db is null ? null : new()
            {
                ["number"] = db.Number,
                ["name"] = db.Name,
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
                var timerJson = new Dictionary<string, object?>
                {
                    ["id"] = network.Timer.Id,
                    ["variable"] = network.Timer.Variable,
                    ["presetMs"] = network.Timer.Preset.TotalMilliseconds,
                    ["kind"] = network.Timer.Kind.ToString(),
                };
                if (!network.Timer.PresetAssigned) timerJson["presetAssigned"] = false;
                if (network.Timer.PresetOperand is not null) timerJson["presetOperand"] = network.Timer.PresetOperand;
                if (network.Timer.Kind == LadderTimerKind.RetentiveOnDelay
                    && !string.Equals(network.Timer.ResetOperand ?? "FALSE", "FALSE", StringComparison.OrdinalIgnoreCase))
                    timerJson["resetOperand"] = network.Timer.ResetOperand;
                if (hasVersionTwoMetadata) timerJson["instanceDataBlock"] = InstanceDataBlock(network.Timer.InstanceDataBlock);
                result["timer"] = timerJson;
            }
            if (network.TimerReset is not null)
            {
                var timerResetJson = new Dictionary<string, object?>
                {
                    ["id"] = network.TimerReset.Id,
                    ["variable"] = network.TimerReset.Variable,
                };
                if (hasVersionTwoMetadata) timerResetJson["instanceDataBlock"] = InstanceDataBlock(network.TimerReset.InstanceDataBlock);
                result["timerReset"] = timerResetJson;
            }
            if (network.Counter is not null)
            {
                var counterJson = new Dictionary<string, object?>
                {
                    ["id"] = network.Counter.Id,
                    ["variable"] = network.Counter.Variable,
                    ["preset"] = network.Counter.Preset,
                    ["kind"] = network.Counter.Kind.ToString(),
                };
                if (hasVersionTwoMetadata)
                {
                    counterJson["valueType"] = network.Counter.ValueType.ToString();
                    counterJson["instanceDataBlock"] = InstanceDataBlock(network.Counter.InstanceDataBlock);
                }
                if (network.Counter.Kind == LadderCounterKind.CountDown
                    && !network.Counter.LoadInput.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                    counterJson["loadInput"] = network.Counter.LoadInput;
                if (network.Counter.Kind == LadderCounterKind.CountUp
                    && !network.Counter.ResetInput.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
                    counterJson["resetInput"] = network.Counter.ResetInput;
                result["counter"] = counterJson;
            }
            if (network.CounterReset is not null)
            {
                var counterResetJson = new Dictionary<string, object?>
                {
                    ["id"] = network.CounterReset.Id,
                    ["variable"] = network.CounterReset.Variable,
                };
                if (hasVersionTwoMetadata) counterResetJson["instanceDataBlock"] = InstanceDataBlock(network.CounterReset.InstanceDataBlock);
                result["counterReset"] = counterResetJson;
            }
            if (network.CounterLoad is not null)
            {
                var counterLoadJson = new Dictionary<string, object?>
                {
                    ["id"] = network.CounterLoad.Id,
                    ["variable"] = network.CounterLoad.Variable,
                    ["preset"] = network.CounterLoad.Preset,
                };
                if (hasVersionTwoMetadata)
                {
                    counterLoadJson["valueType"] = network.CounterLoad.ValueType.ToString();
                    counterLoadJson["instanceDataBlock"] = InstanceDataBlock(network.CounterLoad.InstanceDataBlock);
                }
                result["counterLoad"] = counterLoadJson;
            }
            if (network.NumericOperation is not null)
            {
                var numericJson = new Dictionary<string, object?>
                {
                    ["id"] = network.NumericOperation.Id,
                    ["operation"] = network.NumericOperation.Kind.ToString(),
                    ["sourceA"] = network.NumericOperation.SourceA,
                    ["sourceB"] = network.NumericOperation.SourceB,
                    ["sourceC"] = network.NumericOperation.SourceC,
                    ["destination"] = network.NumericOperation.Destination,
                };
                if (network.NumericOperation.DataType != LadderNumericDataType.Unspecified)
                    numericJson["dataType"] = network.NumericOperation.DataType.ToString();
                if (network.NumericOperation.InputDataType != LadderNumericDataType.Unspecified)
                    numericJson["inputDataType"] = network.NumericOperation.InputDataType.ToString();
                if (network.NumericOperation.OutputDataType != LadderNumericDataType.Unspecified)
                    numericJson["outputDataType"] = network.NumericOperation.OutputDataType.ToString();
                result["numericOperation"] = numericJson;
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
                    ["valueKind"] = network.Return.ValueKind.ToString(),
                    ["operand"] = network.Return.Operand,
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
            ["variables"] = program.Variables.Select(variable =>
            {
                var tag = new Dictionary<string, object?>
                {
                    ["name"] = variable.Name,
                    ["type"] = variable.Type.ToString().ToUpperInvariant(),
                    ["role"] = variable.Role.ToString().ToLowerInvariant(),
                    ["initial"] = variable.Type == PlcVariableType.Time && variable.InitialValue is TimeSpan timeValue
                        ? timeValue.TotalMilliseconds : variable.InitialValue,
                    ["binding"] = variable.Binding,
                    ["aggregate"] = variable.Aggregate,
                    ["enumMembers"] = variable.EnumMembers,
                    ["symbolId"] = variable.Id,
                };
                if (hasVersionTwoMetadata)
                {
                    if (!string.IsNullOrWhiteSpace(variable.Address)) tag["address"] = variable.Address;
                    if (!string.IsNullOrWhiteSpace(variable.Comment)) tag["comment"] = variable.Comment;
                }
                return tag;
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
                    if (type == PlcVariableType.Time)
                        initial = TimeSpan.FromMilliseconds(Convert.ToDouble(initial, CultureInfo.InvariantCulture));
                    variables.Add(new PlcVariable(name, type, role, initial, Text(variable, "binding"), variable.TryGetProperty("aggregate", out var schema) && schema.ValueKind == JsonValueKind.Object ? PlcAggregates.ParseSchema(schema) : null,
                        enumMembers, Text(variable, "symbolId"), Address: Text(variable, "address"), Comment: Text(variable, "comment")));
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
                TimeSpan.FromMilliseconds(Number(timerElement, "presetMs", 0)), timerKind,
                ReadInstanceDataBlock(timerElement, path + ".timer", issues),
                Boolean(timerElement, "presetAssigned", true),
                timerElement.TryGetProperty("presetOperand", out var presetOperandElement)
                    && presetOperandElement.ValueKind == JsonValueKind.String ? presetOperandElement.GetString() : null,
                timerElement.TryGetProperty("resetOperand", out var resetOperandElement)
                    && resetOperandElement.ValueKind == JsonValueKind.String ? resetOperandElement.GetString() : "FALSE");
        }
        if (network.TryGetProperty("timerReset", out var timerResetElement)
            && timerResetElement.ValueKind == JsonValueKind.Object)
            timerReset = new LadderTimerReset(Text(timerResetElement, "id"), Text(timerResetElement, "variable"),
                ReadInstanceDataBlock(timerResetElement, path + ".timerReset", issues));
        if (network.TryGetProperty("counter", out var counterElement) && counterElement.ValueKind == JsonValueKind.Object)
        {
            var kindText = Text(counterElement, "kind");
            if (string.IsNullOrWhiteSpace(kindText)) kindText = nameof(LadderCounterKind.CountUp);
            if (!Enum.TryParse<LadderCounterKind>(kindText, true, out var counterKind))
            {
                issues.Add(new("VC100", $"{path}.counter.kind", $"Unknown counter kind '{kindText}'."));
                counterKind = LadderCounterKind.CountUp;
            }
            var valueTypeText = Text(counterElement, "valueType");
            if (string.IsNullOrWhiteSpace(valueTypeText)) valueTypeText = nameof(LadderCounterValueType.Int);
            if (!Enum.TryParse<LadderCounterValueType>(valueTypeText, true, out var counterValueType))
            {
                issues.Add(new("VC100", $"{path}.counter.valueType", $"Unknown counter data type '{valueTypeText}'."));
                counterValueType = LadderCounterValueType.Int;
            }
            counter = new LadderCounter(Text(counterElement, "id"), Text(counterElement, "variable"),
                Long(counterElement, "preset", 0), counterKind, counterValueType,
                ReadInstanceDataBlock(counterElement, path + ".counter", issues),
                counterElement.TryGetProperty("loadInput", out var loadInputElement) && loadInputElement.ValueKind == JsonValueKind.String
                    ? loadInputElement.GetString() ?? "FALSE" : "FALSE",
                counterElement.TryGetProperty("resetInput", out var resetInputElement) && resetInputElement.ValueKind == JsonValueKind.String
                    ? resetInputElement.GetString() ?? "FALSE" : "FALSE");
        }
        if (network.TryGetProperty("counterReset", out var resetElement) && resetElement.ValueKind == JsonValueKind.Object)
            counterReset = new LadderCounterReset(Text(resetElement, "id"), Text(resetElement, "variable"),
                ReadInstanceDataBlock(resetElement, path + ".counterReset", issues));
        if (network.TryGetProperty("counterLoad", out var loadElement) && loadElement.ValueKind == JsonValueKind.Object)
        {
            var valueTypeText = Text(loadElement, "valueType");
            if (string.IsNullOrWhiteSpace(valueTypeText)) valueTypeText = nameof(LadderCounterValueType.Int);
            if (!Enum.TryParse<LadderCounterValueType>(valueTypeText, true, out var loadValueType))
            {
                issues.Add(new("VC100", $"{path}.counterLoad.valueType", $"Unknown counter data type '{valueTypeText}'."));
                loadValueType = LadderCounterValueType.Int;
            }
            counterLoad = new LadderCounterLoad(Text(loadElement, "id"), Text(loadElement, "variable"),
                Long(loadElement, "preset", 0), loadValueType,
                ReadInstanceDataBlock(loadElement, path + ".counterLoad", issues));
        }
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
                Text(numericElement, "sourceB"), Text(numericElement, "destination"), sourceC)
            {
                DataType = ReadEnum(numericElement, "dataType", LadderNumericDataType.Unspecified, path + ".numericOperation", issues),
                InputDataType = ReadEnum(numericElement, "inputDataType", LadderNumericDataType.Unspecified, path + ".numericOperation", issues),
                OutputDataType = ReadEnum(numericElement, "outputDataType", LadderNumericDataType.Unspecified, path + ".numericOperation", issues),
            };
        }
        if (network.TryGetProperty("call", out var callElement) && callElement.ValueKind == JsonValueKind.Object)
            call = new LadderCall(Text(callElement, "id"), Text(callElement, "targetBlock"),
                ReadEnum(callElement, "semanticMode", LadderSemanticMode.Legacy, path + ".call", issues),
                ReadBindings(callElement, path + ".call", issues), Text(callElement, "instanceId"));
        if (network.TryGetProperty("return", out var returnElement) && returnElement.ValueKind == JsonValueKind.Object)
            returnInstruction = new LadderReturn(
                Text(returnElement, "id"),
                ReadEnum(returnElement, "valueKind", LadderReturnValueKind.Rlo, path + ".return", issues),
                Text(returnElement, "operand"));
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

        var comparisonDataType = LadderComparisonValueType.Unspecified;
        var comparisonDataTypeText = Text(element, "dataType");
        if (kind == LadderNodeKind.Compare && comparisonDataTypeText.Length > 0
            && !Enum.TryParse<LadderComparisonValueType>(comparisonDataTypeText, true, out comparisonDataType))
            issues.Add(new("VC100", $"{path}.dataType", $"Unknown comparison data type '{comparisonDataTypeText}'."));

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
            edgeMode,
            comparisonDataType)
        {
            EdgeMemoryOperand = kind == LadderNodeKind.Contact
                && element.TryGetProperty("edgeMemory", out var edgeMemoryElement)
                && edgeMemoryElement.ValueKind == JsonValueKind.String
                    ? edgeMemoryElement.GetString() : null,
        };
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

    private static LadderInstanceDataBlock? ReadInstanceDataBlock(
        JsonElement instruction,
        string path,
        ICollection<LadderValidationIssue> issues)
    {
        if (!instruction.TryGetProperty("instanceDataBlock", out var dataBlock)
            || dataBlock.ValueKind == JsonValueKind.Null)
            return null;
        if (dataBlock.ValueKind != JsonValueKind.Object)
        {
            issues.Add(new("VC100", path + ".instanceDataBlock", "Instance data block must be an object."));
            return null;
        }
        return new LadderInstanceDataBlock(Integer(dataBlock, "number", 0), Text(dataBlock, "name"));
    }

    private static int Integer(JsonElement element, string name, int fallback) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result)
            ? result
            : fallback;

    private static bool Boolean(JsonElement element, string name, bool fallback) =>
        element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
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

