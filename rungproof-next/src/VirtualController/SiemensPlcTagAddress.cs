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
        @"^(?:%?)(?:(?<dbArea>DB)(?<dbNumber>[0-9]+)\.(?<dbKind>DBX|DBW|DBD)(?<dbByte>[0-9]+)(?:\.(?<dbBit>[0-7]))?|(?<area>[IQM])(?<kind>[BWD]?)(?<byte>[0-9]+)(?:\.(?<bit>[0-7]))?)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly record struct AddressSpan(string Area, int DbNumber, long StartBit, long LengthBits);

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
