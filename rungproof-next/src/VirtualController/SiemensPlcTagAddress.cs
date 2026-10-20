using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace RungProof.Next.VirtualController;

/// <summary>
/// Validates the small S7-1500 absolute-address subset represented by the
/// current PLC-tag editor. This is declaration metadata validation only; it
/// does not map addresses to I/O or enable a PLC transport.
/// </summary>
public static class SiemensPlcTagAddress
{
    private static readonly Regex AddressPattern = new(
        @"^(?:%?)(?:(?<dbArea>DB)(?<dbNumber>[0-9]+)\.(?<dbKind>DBX|DBW|DBD)(?<dbByte>[0-9]+)(?:\.(?<dbBit>[0-7]))?|(?<area>[IQM])(?<kind>[BXWD]?)(?<byte>[0-9]+)(?:\.(?<bit>[0-7]))?)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex UnprefixedDbAddressCandidate = new(
        @"^DB[0-9]+\.DB",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly record struct AddressSpan(string Area, int DbNumber, long StartBit, long LengthBits);

    /// <summary>
    /// Identifies persisted addresses that the shared editor project format
    /// can safely treat as Siemens metadata. Non-prefixed DB addresses are
    /// included because they are documented in the Siemens tag editor; other
    /// unprefixed addresses remain opaque for the shared Rockwell project path.
    /// </summary>
    internal static bool IsPersistedS7AddressCandidate(string? address)
    {
        var value = address?.Trim() ?? string.Empty;
        return value.StartsWith('%') || UnprefixedDbAddressCandidate.IsMatch(value);
    }

    /// <summary>
    /// Validates syntax, the supported PLC type and role, S7-1500 process-image
    /// ranges, and overlap against other declared absolute addresses.
    /// Empty addresses are allowed for symbolic/offline-only tags.
    /// </summary>
    public static bool TryValidate(
        string? address,
        PlcVariableType type,
        PlcVariableRole role,
        IEnumerable<PlcVariable> existingTags,
        string? excludedTagName,
        out string error)
    {
        var value = address?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            error = string.Empty;
            return true;
        }

        if (!TryParse(value, type, role, out var candidate, out error)) return false;
        foreach (var existing in existingTags)
        {
            if (excludedTagName is not null
                && existing.Name.Equals(excludedTagName, StringComparison.Ordinal)) continue;
            if (!TryParse(existing.Address, existing.Type, existing.Role, out var occupied, out _)) continue;
            if (candidate.Area.Equals(occupied.Area, StringComparison.Ordinal)
                && candidate.DbNumber == occupied.DbNumber
                && candidate.StartBit < occupied.StartBit + occupied.LengthBits
                && occupied.StartBit < candidate.StartBit + candidate.LengthBits)
            {
                error = $"Address '{value}' overlaps the address assigned to tag '{existing.Name}'.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Validates an absolute address used by an offline watch row against the
    /// scalar S7-1500 address subset modeled by RungProof. A valid address does
    /// not imply that a runtime value exists; the simulator resolves only an
    /// exact match to one declared tag.
    /// </summary>
    public static bool TryValidateWatchAddress(string? address, out string error)
    {
        return TryParseWatchAddress(address, out _, out error);
    }

    /// <summary>Returns a canonical identity for a valid address in the supported watch subset.</summary>
    internal static bool TryGetWatchAddressIdentity(string? address, out string identity)
    {
        if (!TryParseWatchAddress(address, out var span, out _))
        {
            identity = string.Empty;
            return false;
        }
        identity = Identity(span);
        return true;
    }

    /// <summary>Returns a canonical identity for a typed, role-checked tag address.</summary>
    internal static bool TryGetTypedAddressIdentity(
        string? address,
        PlcVariableType type,
        PlcVariableRole role,
        out string identity)
    {
        if (!TryParse(address, type, role, out var span, out _))
        {
            identity = string.Empty;
            return false;
        }
        identity = Identity(span);
        return true;
    }

    /// <summary>Tests whether an absolute watch address exactly identifies the declared tag span.</summary>
    internal static bool MatchesWatchTagAddress(string? watchAddress, PlcVariable tag)
    {
        return IsPersistedS7AddressCandidate(tag.Address)
            && TryGetWatchAddressIdentity(watchAddress, out var watchIdentity)
            && TryGetTypedAddressIdentity(tag.Address, tag.Type, tag.Role, out var tagIdentity)
            && string.Equals(watchIdentity, tagIdentity, StringComparison.Ordinal);
    }

    private static bool TryParseWatchAddress(string? address, out AddressSpan span, out string error)
    {
        span = default;
        var value = address?.Trim() ?? string.Empty;
        var match = AddressPattern.Match(value);
        if (!match.Success)
        {
            error = $"'{value}' is not a supported S7-1500 absolute watch address.";
            return false;
        }

        var isDb = match.Groups["dbArea"].Success;
        var area = isDb ? "DB" : match.Groups["area"].Value.ToUpperInvariant();
        var kind = isDb ? match.Groups["dbKind"].Value[^1].ToString().ToUpperInvariant()
            : match.Groups["kind"].Value.ToUpperInvariant();
        var bitAddress = isDb
            ? match.Groups["dbBit"].Success
            : match.Groups["bit"].Success;
        if (!isDb && bitAddress && kind.Length == 0) kind = "X";
        var type = kind switch
        {
            "X" when bitAddress => PlcVariableType.Bool,
            "W" when !bitAddress => PlcVariableType.Int,
            // DINT/REAL/TIME share the same 32-bit address form. Runtime
            // resolution still requires one exact typed tag declaration.
            "D" when !bitAddress => PlcVariableType.DInt,
            _ => (PlcVariableType?)null,
        };
        if (type is null)
        {
            error = $"'{value}' uses an address width or bit form outside the modeled BOOL/INT/DINT/REAL/TIME subset.";
            return false;
        }
        var role = area switch
        {
            "I" => PlcVariableRole.Input,
            "Q" => PlcVariableRole.Output,
            _ => PlcVariableRole.Memory,
        };
        return TryParse(value, type.Value, role, out span, out error);
    }

    private static string Identity(AddressSpan span) => string.Concat(
        span.Area, ":",
        span.DbNumber.ToString(CultureInfo.InvariantCulture), ":",
        span.StartBit.ToString(CultureInfo.InvariantCulture), ":",
        span.LengthBits.ToString(CultureInfo.InvariantCulture));

    /// <summary>Checks an absolute address against a declared watch tag's type and ownership.</summary>
    public static bool TryValidateWatchAddress(
        string? address,
        PlcVariableType type,
        PlcVariableRole role,
        out string error) => TryParse(address, type, role, out _, out error);

    private static bool TryParse(
        string? address,
        PlcVariableType type,
        PlcVariableRole role,
        out AddressSpan span,
        out string error)
    {
        span = default;
        var value = address?.Trim() ?? string.Empty;
        var match = AddressPattern.Match(value);
        if (!match.Success)
        {
            error = $"'{value}' is not a supported S7-1500 absolute address. Use a typed I/Q/M or DBX/DBW/DBD address, or leave it blank for a symbolic tag.";
            return false;
        }

        var isDb = match.Groups["dbArea"].Success;
        var area = isDb ? "DB" : match.Groups["area"].Value.ToUpperInvariant();
        var numberGroup = isDb ? match.Groups["dbNumber"] : match.Groups["byte"];
        var offsetGroup = isDb ? match.Groups["dbByte"] : match.Groups["byte"];
        if (!int.TryParse(numberGroup.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var addressNumber)
            || !int.TryParse(offsetGroup.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var byteOffset))
        {
            error = $"'{value}' contains an address component outside the supported numeric range.";
            return false;
        }

        var kind = isDb ? match.Groups["dbKind"].Value[^1].ToString().ToUpperInvariant()
            : match.Groups["kind"].Value.ToUpperInvariant();
        var bitGroup = isDb ? match.Groups["dbBit"] : match.Groups["bit"];
        var isBit = bitGroup.Success;
        if (!isDb && isBit && kind.Length == 0) kind = "X";
        var bit = 0;
        if (isBit && !int.TryParse(bitGroup.Value, NumberStyles.None, CultureInfo.InvariantCulture, out bit))
        {
            error = $"'{value}' contains an invalid bit number.";
            return false;
        }

        var expectedKind = type switch
        {
            PlcVariableType.Bool => "X",
            PlcVariableType.Int => "W",
            PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.Time => "D",
            _ => string.Empty,
        };
        var shapeIsValid = expectedKind.Length > 0
            && kind.Equals(expectedKind, StringComparison.Ordinal)
            && (type == PlcVariableType.Bool ? isBit : !isBit);
        if (!shapeIsValid)
        {
            error = $"'{value}' does not match the supported {type.ToString().ToUpperInvariant()} address form.";
            return false;
        }

        var expectedRole = area switch
        {
            "I" => PlcVariableRole.Input,
            "Q" => PlcVariableRole.Output,
            _ => PlcVariableRole.Memory,
        };
        if (role != expectedRole)
        {
            error = $"Address area {area} is inconsistent with the selected {role} tag role.";
            return false;
        }

        if (isDb && addressNumber == 0)
        {
            error = "The data-block number must be greater than zero.";
            return false;
        }

        var widthBytes = type switch
        {
            PlcVariableType.Bool => 0,
            PlcVariableType.Int => 2,
            PlcVariableType.DInt or PlcVariableType.Real or PlcVariableType.Time => 4,
            _ => 0,
        };
        if (!isDb)
        {
            var maxByte = area == "M" ? 16383 : 32767;
            var maxStartByte = widthBytes switch
            {
                0 => maxByte,
                2 => maxByte - 1,
                _ => maxByte - 3,
            };
            if (byteOffset > maxStartByte)
            {
                error = $"Address '{value}' is outside the S7-1500 {area} address range for {type.ToString().ToUpperInvariant()}.";
                return false;
            }
        }

        var dbNumber = isDb ? addressNumber : 0;
        var startBit = (long)byteOffset * 8 + (isBit ? bit : 0);
        span = new AddressSpan(area, dbNumber, startBit, widthBytes == 0 ? 1 : widthBytes * 8L);
        error = string.Empty;
        return true;
    }
}
