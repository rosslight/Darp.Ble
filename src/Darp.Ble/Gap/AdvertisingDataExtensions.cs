using System.Diagnostics.CodeAnalysis;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Gap;

/// <summary> Extensions for advertising data </summary>
public static class AdvertisingDataExtensions
{
    /// <summary> Get the AD Flags if its contained in the given data. </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="flags"> The resulting flags if the return is true </param>
    /// <returns> True, if the data type and bytes were present </returns>
    public static bool TryGetFlags(this AdvertisingData data, out AdvertisingDataFlags flags)
    {
        flags = default;
        var adTypeFound = false;
        foreach ((AdTypes adTypes, ReadOnlyMemory<byte> bytes) in data)
        {
            if (adTypes is not AdTypes.Flags || bytes.Length == 0) continue;
            adTypeFound = true;
            flags |= (AdvertisingDataFlags)bytes.Span[0];
        }
        return adTypeFound;
    }

    private static bool TryGetFirstType(this AdvertisingData data, AdTypes type, out ReadOnlyMemory<byte> buffer)
    {
        foreach ((AdTypes adTypes, ReadOnlyMemory<byte> bytes) in data)
        {
            if (adTypes != type) continue;
            buffer = bytes;
            return true;
        }
        buffer = ReadOnlyMemory<byte>.Empty;
        return false;
    }

    /// <summary>
    /// Get the AD Complete Local Name if its contained in the given data.
    /// Will return only first name if specified multiple times
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="name"> The resulting name if the return is true </param>
    /// <returns> True, if the data type was present </returns>
    public static bool TryGetCompleteLocalName(this AdvertisingData data, [NotNullWhen(true)] out string? name)
    {
        if (data.TryGetFirstType(AdTypes.CompleteLocalName, out ReadOnlyMemory<byte> buffer))
        {
            name = Encoding.UTF8.GetString(buffer.Span);
            return true;
        }
        name = null;
        return false;
    }

    /// <summary>
    /// Get the AD Shortened Local Name if its contained in the given data.
    /// Will return only first name if specified multiple times
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="name"> The resulting name if the return is true </param>
    /// <returns> True, if the data type was present </returns>
    public static bool TryGetShortenedLocalName(this AdvertisingData data, [NotNullWhen(true)] out string? name)
    {
        if (data.TryGetFirstType(AdTypes.ShortenedLocalName, out ReadOnlyMemory<byte> buffer))
        {
            name = Encoding.UTF8.GetString(buffer.Span);
            return true;
        }
        name = null;
        return false;
    }

    /// <summary>
    /// Gets the AD Complete Local Name if possible. Looks for Shortened Local Name afterwards.
    /// Will only return first name is specified multiple times
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="name"> The resulting name if the return is true </param>
    /// <returns> True, if the data type was present </returns>
    public static bool TryGetLocalName(this AdvertisingData data, [NotNullWhen(true)] out string? name)
    {
        if (data.TryGetCompleteLocalName(out name)) return true;
        if (data.TryGetShortenedLocalName(out name)) return true;
        name = null;
        return false;
    }

    /// <summary> Accumulate any services </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <returns> An array with services </returns>
    public static IEnumerable<BleUuid> GetServices(this AdvertisingData data)
    {
        foreach ((AdTypes sectionType, ReadOnlyMemory<byte> bytes) in data)
        {
            int guidLength = sectionType switch
            {
                AdTypes.IncompleteListOf16BitServiceClassUuids or AdTypes.CompleteListOf16BitServiceClassUuids => 2,
                AdTypes.IncompleteListOf32BitServiceClassUuids or AdTypes.CompleteListOf32BitServiceClassUuids => 4,
                AdTypes.IncompleteListOf128BitServiceClassUuids or AdTypes.CompleteListOf128BitServiceClassUuids => 16,
                _ => -1,
            };
            if (guidLength < 0)
                continue;
            // Using length - 1 to avoid crashing if invalid lengths were transmitted
            for (var i = 0; i < bytes.Length + 1 - guidLength; i += guidLength)
                yield return new BleUuid(bytes[i..(i + guidLength)].Span);
        }
    }

    /// <summary>
    /// Get the AD Manufacturer Specific Data if its contained in the given advertising data.
    /// Will return only first data found
    /// </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="manufacturerData"> The resulting manufacturer specific data if the return is true </param>
    /// <returns> True, if the data type was present and AD data at least 2 bytes long </returns>
    public static bool TryGetManufacturerSpecificData(this AdvertisingData data,
        [NotNullWhen(true)] out (CompanyIdentifiers Company, byte[] Bytes)? manufacturerData)
    {
        if (!data.TryGetFirstType(AdTypes.ManufacturerSpecificData, out ReadOnlyMemory<byte> bytes)
            || bytes.Length < 2)
        {
            manufacturerData = null;
            return false;
        }
        var companyUuid = (CompanyIdentifiers)BitConverter.ToUInt16(bytes.Span);
        manufacturerData = (companyUuid, bytes.Span[2..].ToArray());
        return true;
    }
}