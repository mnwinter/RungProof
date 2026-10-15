using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace RungProof.Next.Connections;

/// <summary>Displays validated configuration; never infers live CPU readiness.</summary>
public static class ExternalProfilePresentation
{
    public static string Format(JsonElement descriptor)
    {
        static string Escape(string value) => string.Concat(value.Select(character => character switch
        {
            '[' => "[lb]", ']' => "[rb]", _ => character.ToString(),
        }));
        static string Text(JsonElement item, string key) => item.TryGetProperty(key, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText()
            : "not provided";
        string Value(string key) => Escape(Text(descriptor, key));
        var text = new StringBuilder("[color=#65d49a]LOCAL PROFILE VALID[/color]\n");
        text.AppendLine($"CPU: {Value("cpuFamily")} · IP: {Value("ip")} · rack {Value("rack")} · slot {Value("slot")}");
        text.AppendLine($"Cycle: {Value("cycleMs")} ms · connect timeout: {Value("connectTimeoutMs")} ms");
        text.AppendLine($"Heartbeat: {Value("heartbeatPcTag")} → {Value("heartbeatEchoTag")} · timeout: {Value("heartbeatTimeoutMs")} ms");
        text.AppendLine("Local validation does not verify CPU access permissions, live handshake or watchdog operation.");
        void Scope(string key, string title, bool writes)
        {
            var entries = descriptor.GetProperty(key).EnumerateArray().ToArray();
            text.AppendLine($"\n[b]{title} ({entries.Length})[/b]");
            foreach (var entry in entries)
            {
                text.Append($"{Escape(Text(entry, "name"))} · {Escape(Text(entry, "dataType"))} · {Escape(Text(entry, "address"))} · {Escape(Text(entry, "symbol"))}");
                if (writes) text.Append($" · configured safe value: {Escape(Text(entry, "safeValue"))}");
                text.AppendLine();
            }
        }
        void Points(string key, string title)
        {
            var entries = descriptor.GetProperty(key).EnumerateArray().ToArray();
            text.AppendLine($"\n[b]{title} ({entries.Length})[/b]");
            foreach (var entry in entries)
            {
                text.Append($"{Escape(Text(entry, "name"))} → {Escape(Text(entry, "tag"))} · {Escape(Text(entry, "dataType"))} · {Escape(Text(entry, "address"))}");
                if (entry.TryGetProperty("inverted", out var inverted)) text.Append($" · inverted: {Escape(inverted.GetRawText())}");
                text.AppendLine();
            }
        }
        Scope("writeScope", "PC → PLC write scope", true);
        Scope("readScope", "PLC → PC read scope", false);
        Points("pcPointScope", "PC scene-point mappings");
        Points("plcPointScope", "PLC scene-point mappings");
        text.AppendLine("\nConfigured safe values are profile settings; this display does not prove they were written on Stop or Disconnect.");
        return text.ToString();
    }
}
