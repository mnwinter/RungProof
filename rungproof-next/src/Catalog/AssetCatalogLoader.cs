using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;
using GodotFileAccess = Godot.FileAccess;

namespace RungProof.Next.Catalog;

public static class AssetCatalogLoader
{
    // Match an actual Siemens data-block token such as DB14 or DB14.DBX0.0.
    // A raw Contains("DB") also matches legitimate symbolic IDs such as
    // position_feedback, which must remain valid simulator contracts.
    private static readonly Regex PhysicalDbAddress = new(
        @"(?:^|[^A-Za-z0-9])DB\d+(?:[^A-Za-z0-9]|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static AssetCatalogDocument LoadFromProject(
        string resourcePath,
        bool requireApproval = true
    )
    {
        using var file = GodotFileAccess.Open(
            resourcePath,
            GodotFileAccess.ModeFlags.Read
        );
        if (file is null)
        {
            throw new InvalidOperationException($"Catalog is unavailable: {resourcePath}");
        }

        var document = JsonSerializer.Deserialize<AssetCatalogDocument>(
            file.GetAsText(),
            JsonOptions
        ) ?? throw new InvalidOperationException("Catalog JSON produced no document.");

        Validate(document, requireApproval);
        return document;
    }

    public static AssetCatalogDocument Merge(
        AssetCatalogDocument production,
        AssetCatalogDocument candidates
    )
    {
        if (production.Version != candidates.Version)
        {
            throw new InvalidOperationException("Asset catalog versions do not match.");
        }
        var duplicate = production.Assets
            .Concat(candidates.Assets)
            .GroupBy(asset => asset.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"Asset exists in both production and candidate catalogs: {duplicate.Key}"
            );
        }
        return new AssetCatalogDocument(
            production.Version,
            "rungproof-runtime",
            production.Assets.Concat(candidates.Assets).ToArray()
        );
    }

    private static void Validate(AssetCatalogDocument document, bool requireApproval)
    {
        if (document.Version != 1)
        {
            throw new InvalidOperationException($"Unsupported catalog version {document.Version}.");
        }

        var duplicate = document.Assets
            .GroupBy(asset => asset.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate asset ID: {duplicate.Key}");
        }

        foreach (var asset in document.Assets)
        {
            if (requireApproval
                && !string.Equals(asset.Quality.Status, "approved", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Production catalog contains unapproved asset {asset.Id}."
                );
            }
            if (requireApproval && asset.Quality.RecognitionConfidence is null or < 0.80)
            {
                throw new InvalidOperationException(
                    $"Asset {asset.Id} failed blind-recognition confidence."
                );
            }
            if (asset.Signals.Any(signal => ContainsPhysicalPlcAddress(signal.Id)))
            {
                throw new InvalidOperationException(
                    $"Asset {asset.Id} leaks physical PLC addressing into its signal contract."
                );
            }
        }
    }

    private static bool ContainsPhysicalPlcAddress(string signalId)
    {
        return signalId.Contains("%I", StringComparison.OrdinalIgnoreCase)
            || signalId.Contains("%Q", StringComparison.OrdinalIgnoreCase)
            || PhysicalDbAddress.IsMatch(signalId);
    }
}
