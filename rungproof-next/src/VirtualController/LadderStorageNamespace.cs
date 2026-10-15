using System;
using System.Collections.Generic;

namespace RungProof.Next.VirtualController;

/// <summary>All names consumed by the scalar/member resolver, not only declared roots.</summary>
internal static class LadderStorageNamespace
{
    public static IReadOnlyList<string> Members(PlcVariableType type) => type switch
    {
        PlcVariableType.Timer => ["Q", "DN", "TT", "ET", "PT"],
        PlcVariableType.Counter => ["Q", "DN", "ACC", "CV", "PRE", "PV"],
        _ => []
    };

    public static IEnumerable<string> Names(string root, PlcVariableType type)
    {
        yield return root;
        foreach (var member in Members(type)) yield return root + "." + member;
    }

    public static string Reserve(HashSet<string> occupied, string preferred, PlcVariableType type)
    {
        var candidate = preferred;
        for (var suffix = 1; Collides(candidate); suffix++) candidate = preferred + "#private" + suffix;
        occupied.UnionWith(Names(candidate, type));
        return candidate;

        bool Collides(string name)
        {
            foreach (var key in Names(name, type)) if (occupied.Contains(key)) return true;
            return false;
        }
    }
}
