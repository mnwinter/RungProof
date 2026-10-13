using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RungProof.Next.VirtualController;
namespace RungProof.Next.Challenges;
/// <summary>Conservative content identity, not equivalence of two ladder programs.</summary>
public static class ChallengeProgramIdentity
{
    public const string IdentityFormat = "RP-CANON-1";
    // Each logical branch adds an object and a children array. The allowed
    // logical depth 16 therefore produces up to 36 serialized JSON levels,
    // including program/block/network wrappers. Use the serializer's bounded
    // 64-level allowance only for already policy-validated program graphs.
    // Authored content, catalogs and manifests retain their 32-level limit.
    private const int ProgramJsonMaximumDepth = 64;
    public static string ProgramSha256(LadderProgram program)
    {
        var issues = ChallengeEligibilityValidator.Validate(program);
        if (issues.Count != 0)
            throw new ArgumentException("Original program is ineligible: " + issues[0].Message, nameof(program));
        using var document = StrictDocument(LadderProgramJson.Save(program), 262144, ProgramJsonMaximumDepth);
        return Hash(Canonical(document.RootElement, true));
    }
    public static string CanonicalJson(string json)
    {
        using var document = StrictDocument(json, 1048576);
        return Canonical(document.RootElement, false);
    }
    public static string CanonicalSha256(string json) => Hash(CanonicalJson(json));
    internal static string CanonicalProgramJson(string json)
    {
        using var document = StrictDocument(json, 1048576, ProgramJsonMaximumDepth);
        return Canonical(document.RootElement, false);
    }
    internal static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    internal static JsonDocument StrictDocument(string json, int maximumBytes, int maximumDepth = 32)
    {
        if (json is null || Encoding.UTF8.GetByteCount(json) > maximumBytes)
            throw new ArgumentException("JSON exceeds the byte limit.");
        var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = maximumDepth });
        try
        {
            Inspect(document.RootElement);
            return document;
        }
        catch { document.Dispose(); throw; }
    }
    private static void Inspect(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in value.EnumerateObject())
            {
                if (!names.Add(p.Name))
                    throw new ArgumentException("Duplicate JSON property: " + p.Name);
                Inspect(p.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        foreach (var item in value.EnumerateArray())
            Inspect(item);
        else if (value.ValueKind == JsonValueKind.Number)
            Number(value);
    }
    private static string Canonical(JsonElement root, bool omitDisplay)
    {
        var output = new StringBuilder();
        Write(root, output, omitDisplay);
        return output.ToString();
    }
    private static void Write(JsonElement value, StringBuilder output, bool omitDisplay = false)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                output.Append('{');
                var first = true;
                foreach (var p in value.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (omitDisplay && p.Name is "id" or "name" or "watchVariables")
                        continue;
                    if (!first)
                        output.Append(',');
                    first = false;
                    output.Append(JsonSerializer.Serialize(p.Name)).Append(':');
                    Write(p.Value, output);
                }
                output.Append('}');
                break;
            case JsonValueKind.Array:
                output.Append('[');
                var firstItem = true;
                foreach (var item in value.EnumerateArray())
                {
                    if (!firstItem)
                        output.Append(',');
                    firstItem = false;
                    Write(item, output);
                }
                output.Append(']');
                break;
            case JsonValueKind.String:
                output.Append(JsonSerializer.Serialize(value.GetString()));
                break;
            case JsonValueKind.Number:
                output.Append(Number(value));
                break;
            case JsonValueKind.True:
                output.Append("true");
                break;
            case JsonValueKind.False:
                output.Append("false");
                break;
            case JsonValueKind.Null:
                output.Append("null");
                break;
            default:
                throw new ArgumentException("Unsupported JSON value.");
        }
    }
    private static string Number(JsonElement value)
    {
        if (!value.TryGetDouble(out var number) || !double.IsFinite(number) || Math.Abs(number) > 9007199254740991d)
            throw new ArgumentException("Number is outside the finite binary64 exact-integer envelope.");
        if (number == 0)
            return "0";
        var text = number.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();
        var split = text.IndexOf('e');
        if (split >= 0)
            text = text[..split] + "e" + int.Parse(text[(split + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        return text;
    }
}

