using System.Diagnostics.CodeAnalysis;
using System.Text;
using Darp.Ble.Data;
using Darp.Ble.Data.AssignedNumbers;

namespace Darp.Ble.Gap;

/// <summary> Extensions for advertising data </summary>
public static class AdvertisingDataExtensions
{
    /// <summary> Get the AD Flags if it's contained in the given data. </summary>
    /// <param name="data"> The data to be looked at </param>
    /// <param name="flags"> The resulting flags if the return is true </param>
    /// <returns> True, if the data type and bytes were present </returns>
    public static bool TryGetFlags(this AdvertisingData data, out AdvertisingDataFlags flags)
    {
        ArgumentNullException.ThrowIfNull(data);
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
        ArgumentNullException.ThrowIfNull(data);
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
        ArgumentNullException.ThrowIfNull(data);
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
        ArgumentNullException.ThrowIfNull(data);
        return GetServicesInt(data);

        IEnumerable<BleUuid> GetServicesInt(AdvertisingData d)
        {
            foreach ((AdTypes sectionType, ReadOnlyMemory<byte> bytes) in d)
            {
                int guidLength = sectionType switch
                {
                    AdTypes.IncompleteListOf16BitServiceOrServiceClassUuids or AdTypes.CompleteListOf16BitServiceOrServiceClassUuids => 2,
                    AdTypes.IncompleteListOf32BitServiceOrServiceClassUuids or AdTypes.CompleteListOf32BitServiceOrServiceClassUuids => 4,
                    AdTypes.IncompleteListOf128BitServiceOrServiceClassUuids or AdTypes.CompleteListOf128BitServiceOrServiceClassUuids => 16,
                    _ => -1,
                };
                if (guidLength < 0)
                    continue;
                // Using length - 1 to avoid crashing if invalid lengths were transmitted
                for (var i = 0; i < bytes.Length + 1 - guidLength; i += guidLength)
                    yield return new BleUuid(bytes[i..(i + guidLength)].Span);
            }
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
        out (CompanyIdentifiers Company, byte[] Bytes) manufacturerData)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (!data.TryGetFirstType(AdTypes.ManufacturerSpecificData, out ReadOnlyMemory<byte> bytes)
            || bytes.Length < 2)
        {
            manufacturerData = default;
            return false;
        }
        var companyUuid = (CompanyIdentifiers)BitConverter.ToUInt16(bytes.Span);
        manufacturerData = (companyUuid, bytes.Span[2..].ToArray());
        return true;
    }
}