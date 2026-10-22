using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public sealed record LadderOperandSymbol(string Name, PlcVariableType Type, bool Writable, string DisplayName = "");

/// <summary>Operand lookup uses actual declarations, including expanded aggregate leaves and runtime instance members.</summary>
public static class LadderOperandSymbols
{
    public static IReadOnlyList<LadderOperandSymbol> ForDocument(LadderEditorDocument document)
    {
        var symbols = new List<LadderOperandSymbol>();
        var program = document.BuildProgram();
        var owner = program.Blocks![document.ActiveBlockIndex];
        void Add(string reference, string displayName, PlcVariable declaration)
        {
            symbols.Add(new(reference, declaration.Type, declaration.Role != PlcVariableRole.Input, displayName));
            if (declaration.Type is not (PlcVariableType.Timer or PlcVariableType.Counter)) return;
            foreach (var member in declaration.Type == PlcVariableType.Timer ? new[] { "Q", "DN", "TT" } : new[] { "Q", "DN" })
                symbols.Add(new(reference + "." + member, PlcVariableType.Bool, false, displayName + "." + member));
            foreach (var member in declaration.Type == PlcVariableType.Timer ? new[] { "ET", "PT" } : new[] { "CV", "PV", "ACC", "PRE" })
                symbols.Add(new(reference + "." + member, PlcVariableType.DInt, false, displayName + "." + member));
        }
        foreach (var tag in PlcAggregates.Expand(document.Tags))
        {
            if (owner.SemanticMode == LadderSemanticMode.Legacy) { Add(tag.Name, tag.Name, tag); continue; }
            var aggregate = document.Tags.FirstOrDefault(root => PlcAggregates.IsAggregate(root.Type)
                && (tag.Name.StartsWith(root.Name + ".", StringComparison.Ordinal) || tag.Name.StartsWith(root.Name + "[", StringComparison.Ordinal)));
            var id = aggregate is null ? LadderCallBindings.GlobalId(tag) : LadderCallBindings.GlobalId(aggregate) + tag.Name[aggregate.Name.Length..];
            var reference = "global:" + id;
            var declaration = LadderCallBindings.ResolveDeclaration(program, owner, reference);
            if (declaration is not null) Add(reference, tag.Name, declaration);
        }
        if (owner.SemanticMode == LadderSemanticMode.Parameterized)
        {
            foreach (var formal in owner.Interface ?? [])
            {
                var reference = "local:" + formal.Id;
                var declaration = LadderCallBindings.ResolveDeclaration(program, owner, reference);
                if (declaration is not null) Add(reference, formal.Name + " (" + formal.Section + ")", declaration);
            }
        }
        foreach (var constant in document.UserConstants)
            symbols.Add(new(constant.Name, constant.Type, false, constant.Name + " (constant)"));
        return symbols;
    }

    public static string? Validate(IReadOnlyList<LadderOperandSymbol> symbols, string operand, PlcVariableType type, bool writable, bool allowLiteral)
    {
        var symbol = symbols.FirstOrDefault(item => item.Name == operand);
        if (symbol is not null)
        {
            if (symbol.Type != type) return $"'{operand}' is {symbol.Type}; this field requires {type}.";
            if (writable && !symbol.Writable) return $"'{operand}' is read-only.";
            return null;
        }
        if (!writable && allowLiteral && (PlcTextValues.IsText(type)
            ? PlcTextValues.TryLiteral(operand, out _)
            : LadderEditorDocument.TryParseInitialValue(type, operand, out _, out _))) return null;
        return $"'{operand}' is not a declared {type} operand. Choose a declaration or explicitly create a tag.";
    }
}
