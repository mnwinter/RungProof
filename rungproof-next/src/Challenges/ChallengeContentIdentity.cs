using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace RungProof.Next.Challenges;

/// <summary>Identity for the complete validated learning packet and fixed plant.</summary>
public static class ChallengeContentIdentity
{
    private static JsonSerializerOptions Options => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };
    public static string PlantSha256(ConveyorPlantParameters plant) =>
        ChallengeProgramIdentity.CanonicalSha256(JsonSerializer.Serialize(plant, Options));
    public static string PacketSha256(ChallengeDefinition definition, ReadOnlySpan<byte> actualStarter)
    {
        var issues = ChallengeDefinitionJson.ValidateStarter(definition, actualStarter);
        if (issues.Count > 0) throw new ArgumentException("Starter bytes do not match packet: " + issues[0]);
        var starterHash = Convert.ToHexStringLower(SHA256.HashData(actualStarter));
        // Packet parser rejects unknown/duplicate fields. The typed projection
        // retains every supported field; exact starter bytes are independently
        // checked against its declared hash/length before entering this envelope.
        return ChallengeProgramIdentity.CanonicalSha256(JsonSerializer.Serialize(new
        {
            definition,
            starterByteLength = actualStarter.Length,
            starterSha256 = starterHash,
        }, Options));
    }
}
